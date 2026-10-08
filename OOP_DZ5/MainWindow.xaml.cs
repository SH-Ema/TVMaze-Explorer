using DZ5_TvApp.Services;
using Newtonsoft.Json;
using OOP_DZ5.Components;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OOP_DZ5
{
    public partial class MainWindow : Window
    {
        private readonly TvMazeService _service = new TvMazeService();
        private CancellationTokenSource? _searchCancellation;
        private CancellationTokenSource? _episodeCancellation;
        private int _searchVersion;
        private int _episodeVersion;
        private bool _isSearching;
        private bool _isLoadingEpisodes;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnQueryKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                OnSearchClick(sender, e);
            }
        }

        private async void OnSearchClick(object sender, RoutedEventArgs e)
        {
            var query = QueryTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                SetStatus("Please enter a show name.");
                QueryTextBox.Focus();
                return;
            }

            _searchCancellation?.Cancel();
            using var cancellation = new CancellationTokenSource();
            _searchCancellation = cancellation;
            int version = ++_searchVersion;

            // A new search also invalidates any pending episode request.
            _episodeCancellation?.Cancel();
            ++_episodeVersion;
            ShowsView.ItemsSource = null;
            ClearShowDetails();
            _isSearching = true;
            UpdateLoadingIndicator();
            SetEmptyState(ShowsEmptyText, "Searching for shows…");
            SetStatus($"Searching for “{query}”…");

            try
            {
                var results = await _service.SearchShowsAsync(query, cancellation.Token);
                if (version == _searchVersion && !cancellation.IsCancellationRequested)
                {
                    ShowsView.ItemsSource = results;
                    SetEmptyState(ShowsEmptyText, results.Count == 0
                        ? "No shows found. Try another title." : null);
                    SetStatus(results.Count == 0 ? "No matching shows found."
                        : $"Found {results.Count} shows. Select one to explore.");
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // A newer request or closing the window intentionally cancelled this one.
            }
            catch (Exception error)
            {
                if (version == _searchVersion && !cancellation.IsCancellationRequested)
                {
                    SetEmptyState(ShowsEmptyText, "Search could not be completed. Please try again.");
                    ShowRequestError(error);
                }
            }
            finally
            {
                if (ReferenceEquals(_searchCancellation, cancellation))
                {
                    _searchCancellation = null;
                    _isSearching = false;
                    UpdateLoadingIndicator();
                }
            }
        }

        private async void OnShowSelected(object sender, SelectionChangedEventArgs e)
        {
            _episodeCancellation?.Cancel();
            int version = ++_episodeVersion;
            ClearShowDetails();

            if (ShowsView.SelectedItem is not Show selectedShow)
                return;

            using var cancellation = new CancellationTokenSource();
            _episodeCancellation = cancellation;
            _isLoadingEpisodes = true;
            UpdateLoadingIndicator();
            SetEmptyState(SeasonsEmptyText, "Loading seasons…");
            SetEmptyState(EpisodesEmptyText, "Loading episodes…");
            SetStatus($"Loading episodes for “{selectedShow.name}”…");

            try
            {
                ShowTitleText.Text = selectedShow.name;
                ShowInfoText.Text = $"{selectedShow.language} | {selectedShow.status}";
                var summary = RemoveHtml(selectedShow.summary);
                ShowSummaryText.Text = string.IsNullOrWhiteSpace(summary)
                    ? "No description available for this show." : summary;

                var episodes = await _service.GetEpisodesAsync(selectedShow.id, cancellation.Token);
                if (version != _episodeVersion || cancellation.IsCancellationRequested ||
                    !ReferenceEquals(ShowsView.SelectedItem, selectedShow))
                    return;

                var seasonList = episodes
                    .GroupBy(ep => ep.season)
                    .Select(group =>
                    {
                        var season = new Season(group.Key);
                        foreach (var episode in group)
                            season.Add(episode);
                        return season;
                    })
                    .OrderBy(s => s.SeasonNumber)
                    .ToList();

                selectedShow.Seasons = seasonList;
                SeasonsView.ItemsSource = seasonList;
                SetEmptyState(SeasonsEmptyText, seasonList.Count == 0 ? "No seasons available." : null);
                SetEmptyState(EpisodesEmptyText, episodes.Count == 0
                    ? "No episodes available for this show." : "Select a season to see its episodes.");
                SetStatus(episodes.Count == 0 ? "This show has no episodes available."
                    : $"Loaded {seasonList.Count} seasons. Select a season to see its episodes.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Selection changes intentionally cancel the previous request.
            }
            catch (Exception error)
            {
                if (version == _episodeVersion && !cancellation.IsCancellationRequested)
                {
                    SetEmptyState(SeasonsEmptyText, "Seasons could not be loaded.");
                    SetEmptyState(EpisodesEmptyText, "Search again or select another show to retry.");
                    ShowRequestError(error);
                }
            }
            finally
            {
                if (ReferenceEquals(_episodeCancellation, cancellation))
                {
                    _episodeCancellation = null;
                    _isLoadingEpisodes = false;
                    UpdateLoadingIndicator();
                }
            }
        }

        private void ClearShowDetails()
        {
            // Detach the previous request so its finally block cannot reset a newer loading state.
            _episodeCancellation = null;
            _isLoadingEpisodes = false;
            UpdateLoadingIndicator();
            ShowTitleText.Text = "Select a show";
            ShowInfoText.Text = string.Empty;
            ShowSummaryText.Text = "Show information will appear here.";
            SeasonsView.ItemsSource = null;
            EpisodesView.ItemsSource = null;
            SetEmptyState(SeasonsEmptyText, "Select a show to explore its seasons.");
            SetEmptyState(EpisodesEmptyText, "Select a season to see its episodes.");
        }

        private void OnSeasonSelected(object sender, SelectionChangedEventArgs e)
        {
            if (SeasonsView.SelectedItem is not Season season)
            {
                EpisodesView.ItemsSource = null;
                return;
            }

            var episodes = season.ToList();
            EpisodesView.ItemsSource = episodes;
            SetEmptyState(EpisodesEmptyText, episodes.Count == 0 ? "No episodes in this season." : null);
            SetStatus($"Season {season.SeasonNumber} · {episodes.Count} episodes");
        }

        private static void SetEmptyState(TextBlock textBlock, string? message)
        {
            textBlock.Text = message ?? string.Empty;
            textBlock.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateLoadingIndicator()
        {
            LoadingIndicator.Visibility = _isSearching || _isLoadingEpisodes
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError
                ? System.Windows.Media.Brushes.LightSalmon
                : (System.Windows.Media.Brush)FindResource("MutedBrush");
        }

        private void ShowRequestError(Exception error)
        {
            string message = error switch
            {
                OperationCanceledException => "TVMaze took too long to respond. Please try again.",
                HttpRequestException => "Could not load data from TVMaze. Check your internet connection and try again.",
                JsonException => "TVMaze returned data that could not be read. Please try again later.",
                _ => "Something went wrong while loading TVMaze data. Please try again."
            };
            SetStatus(message, isError: true);
        }

        protected override void OnClosed(EventArgs e)
        {
            ++_searchVersion;
            ++_episodeVersion;
            _searchCancellation?.Cancel();
            _episodeCancellation?.Cancel();
            base.OnClosed(e);
        }

        private string RemoveHtml(string? text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : WebUtility.HtmlDecode(Regex.Replace(text, "<.*?>", string.Empty));
        }
    }
}
