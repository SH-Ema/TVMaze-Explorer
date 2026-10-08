using OOP_DZ5.Components;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DZ5_TvApp.Services
{
    public class TvMazeService
    {
        private readonly HttpClient httpClient = new HttpClient();

        public async Task<List<Show>> SearchShowsAsync(string search)
        {
            string url = $"https://api.tvmaze.com/search/shows?q={search}";
            string json = await httpClient.GetStringAsync(url);

            List<ApiSearchResult> results = JsonConvert.DeserializeObject<List<ApiSearchResult>>(json);

            List<Show> shows = new List<Show>();
            foreach (var result in results)
                shows.Add(result.show);

            return shows;
        }

        public async Task<List<Episode>> GetEpisodesAsync(int showId)
        {
            string url = $"https://api.tvmaze.com/shows/{showId}/episodes";
            string json = await httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<Episode>>(json);
        }


        private class ApiSearchResult
        {
            public double score { get; set; }
            public Show show { get; set; }
        }
    }
}

