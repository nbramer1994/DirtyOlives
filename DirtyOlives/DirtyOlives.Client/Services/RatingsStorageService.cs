using System.Net.Http.Json;
using DirtyOlives.Core.Models;

namespace DirtyOlives.Client.Services
{
    /// <summary>
    /// Talks to the server API, which persists ratings per user in a SQLite database.
    /// </summary>
    public class RatingsStorageService
    {
        private const string BaseUrl = "api/MartiniRatings";

        private readonly HttpClient _http;

        public RatingsStorageService(HttpClient http) => _http = http;

        public async Task<List<MartiniRating>> LoadAsync(int userId)
        {
            var ratings = await _http.GetFromJsonAsync<List<MartiniRating>>($"{BaseUrl}?userId={userId}");
            return ratings ?? new List<MartiniRating>();
        }

        public async Task<MartiniRating> AddAsync(MartiniRating rating)
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, rating);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<MartiniRating>() ?? rating;
        }

        public async Task DeleteAsync(Guid id, int userId)
        {
            var response = await _http.DeleteAsync($"{BaseUrl}/{id}?userId={userId}");
            response.EnsureSuccessStatusCode();
        }
    }
}
