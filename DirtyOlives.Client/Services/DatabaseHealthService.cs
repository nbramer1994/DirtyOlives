using System.Net.Http.Json;
using DirtyOlives.Core.Models;

namespace DirtyOlives.Client.Services
{
    /// <summary>
    /// Queries the server's database health endpoints.
    /// </summary>
    public class DatabaseHealthService
    {
        private const string BaseUrl = "api/DatabaseHealth";

        private readonly HttpClient _http;

        public DatabaseHealthService(HttpClient http) => _http = http;

        public Task<DatabaseStatus> GetStartupStatusAsync(CancellationToken cancellationToken = default)
            => SendAsync(() => _http.GetAsync($"{BaseUrl}/startup", cancellationToken), cancellationToken);

        public Task<DatabaseStatus> RunWriteTestAsync(CancellationToken cancellationToken = default)
            => SendAsync(() => _http.PostAsync($"{BaseUrl}/write-test", content: null, cancellationToken), cancellationToken);

        private static async Task<DatabaseStatus> SendAsync(
            Func<Task<HttpResponseMessage>> send,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await send();

                var status = await response.Content.ReadFromJsonAsync<DatabaseStatus>(cancellationToken);
                if (status is not null)
                {
                    return status;
                }

                return new DatabaseStatus
                {
                    IsHealthy = false,
                    Message = "The server returned an empty response.",
                    Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }
            catch (Exception ex)
            {
                // The API itself is unreachable, which is still useful to show.
                return new DatabaseStatus
                {
                    IsHealthy = false,
                    Message = "Could not reach the server.",
                    Error = $"{ex.GetType().Name}: {ex.Message}"
                };
            }
        }
    }
}
