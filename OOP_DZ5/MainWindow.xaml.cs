using DZ5_TvApp.Services;
using Newtonsoft.Json;
using OOP_DZ5.Components;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace OOP_DZ5
{
    public partial class MainWindow : Window
    {
        private readonly TvMazeService _service = new TvMazeService();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnSearchClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowsView.ItemsSource = null;
                SeasonsView.ItemsSource = null;
                EpisodesView.ItemsSource = null;

                var query = QueryTextBox.Text?.Trim();

                if (string.IsNullOrEmpty(query))
                {
                    MessageBox.Show("Please enter a show name.");
                    return;
                }

                var results = await _service.SearchShowsAsync(query);
                ShowsView.ItemsSource = results;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private async void OnShowSelected(object sender, SelectionChangedEventArgs e)
        {
            if (ShowsView.SelectedItem is not Show selectedShow)
                return;

            ShowTitleText.Text = selectedShow.name;
            ShowInfoText.Text = $"{selectedShow.language} | {selectedShow.status}";
            ShowSummaryText.Text = RemoveHtml(selectedShow.summary);

            var episodes = await _service.GetEpisodesAsync(selectedShow.id);

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

        private void OnSeasonSelected(object sender, SelectionChangedEventArgs e)
        {
            if (SeasonsView.SelectedItem is not Season selectedSeason)
                return;

            EpisodesView.ItemsSource = selectedSeason.ToList();
        }

        private string RemoveHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return Regex.Replace(text, "<.*?>", string.Empty);
        }
    }
}