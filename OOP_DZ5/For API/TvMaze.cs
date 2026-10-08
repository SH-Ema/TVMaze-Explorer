using OOP_DZ5.Components;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading;

namespace DZ5_TvApp.Services
{
    public class TvMazeService
    {
        private readonly HttpClient httpClient;

        public TvMazeService() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(30) }) { }

        // Accept a client so the API behavior can be tested without real network calls.
        public TvMazeService(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<Show>> SearchShowsAsync(string search, CancellationToken cancellationToken = default)
        {
            string url = $"https://api.tvmaze.com/search/shows?q={Uri.EscapeDataString(search)}";
            string json = await httpClient.GetStringAsync(url, cancellationToken);
            var results = ReadList<ApiSearchResult>(json);

            if (results.Any(result => result.show == null))
                throw new JsonSerializationException("A search result is missing its show.");

            return results.Select(result => result.show!).ToList();
        }

        public async Task<List<Episode>> GetEpisodesAsync(int showId, CancellationToken cancellationToken = default)
        {
            string url = $"https://api.tvmaze.com/shows/{showId}/episodes";
            string json = await httpClient.GetStringAsync(url, cancellationToken);
            return ReadList<Episode>(json);
        }

        private static List<T> ReadList<T>(string json) where T : class
        {
            var items = JsonConvert.DeserializeObject<List<T>>(json);
            if (items == null || items.Any(item => item == null))
                throw new JsonSerializationException("The API returned an invalid list.");

            return items;
        }

        private class ApiSearchResult
        {
            public double score { get; set; }
            public Show? show { get; set; }
        }
    }
}
