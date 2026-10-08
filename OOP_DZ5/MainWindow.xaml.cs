using DZ5_TvApp.Services;
using Newtonsoft.Json;
using OOP_DZ5.Components;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace OOP_DZ5
{
    public partial class MainWindow : Window
    {
        private readonly TvMazeService _service = new TvMazeService();
        private CancellationTokenSource? _searchCancellation;
        private CancellationTokenSource? _episodeCancellation;
        private int _searchVersion;
        private int _episodeVersion;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnSearchClick(object sender, RoutedEventArgs e)
        {
            var query = QueryTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Please enter a show name.");
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

            try
            {
                var results = await _service.SearchShowsAsync(query, cancellation.Token);
                if (version == _searchVersion && !cancellation.IsCancellationRequested)
                    ShowsView.ItemsSource = results;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // A newer request or closing the window intentionally cancelled this one.
            }
            catch (Exception error)
            {
                if (version == _searchVersion && !cancellation.IsCancellationRequested)
                    ShowRequestError(error);
            }
            finally
            {
                if (ReferenceEquals(_searchCancellation, cancellation))
                    _searchCancellation = null;
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

            try
            {
                ShowTitleText.Text = selectedShow.name;
                ShowInfoText.Text = $"{selectedShow.language} | {selectedShow.status}";
                ShowSummaryText.Text = RemoveHtml(selectedShow.summary);

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
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Selection changes intentionally cancel the previous request.
            }
            catch (Exception error)
            {
                if (version == _episodeVersion && !cancellation.IsCancellationRequested)
                    ShowRequestError(error);
            }
            finally
            {
                if (ReferenceEquals(_episodeCancellation, cancellation))
                    _episodeCancellation = null;
            }
        }

        private void ClearShowDetails()
        {
            ShowTitleText.Text = string.Empty;
            ShowInfoText.Text = string.Empty;
            ShowSummaryText.Text = string.Empty;
            SeasonsView.ItemsSource = null;
            EpisodesView.ItemsSource = null;
        }

        private void OnSeasonSelected(object sender, SelectionChangedEventArgs e)
        {
            EpisodesView.ItemsSource = SeasonsView.SelectedItem is Season season
                ? season.ToList()
                : null;
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
            MessageBox.Show(this, message, "TVMaze Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            return string.IsNullOrEmpty(text) ? string.Empty : Regex.Replace(text, "<.*?>", string.Empty);
        }
    }
}
