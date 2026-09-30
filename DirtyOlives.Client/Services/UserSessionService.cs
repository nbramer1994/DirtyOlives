using System.Net.Http.Json;
using DirtyOlives.Core.Models;
using Microsoft.JSInterop;

namespace DirtyOlives.Client.Services
{
    /// <summary>
    /// Tracks who is currently "signed in" and remembers them across refreshes.
    /// Name based only - this separates ratings, it does not secure anything.
    /// </summary>
    public class UserSessionService
    {
        private const string BaseUrl = "api/Users";
        private const string StorageIdKey = "currentUserId";
        private const string StorageNameKey = "currentUserName";

        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public UserSessionService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        public AppUser? CurrentUser { get; private set; }

        public bool IsSignedIn => CurrentUser is not null;

        /// <summary>Raised whenever the signed in user changes.</summary>
        public event Action? Changed;

        public async Task<List<AppUser>> GetUsersAsync()
        {
            var users = await _http.GetFromJsonAsync<List<AppUser>>(BaseUrl);
            return users ?? new List<AppUser>();
        }

        /// <summary>Restores the previous session from localStorage, if there is one.</summary>
        public async Task InitializeAsync()
        {
            if (CurrentUser is not null)
            {
                return;
            }

            try
            {
                var id = await _js.InvokeAsync<string>("localStorage.getItem", StorageIdKey);
                var name = await _js.InvokeAsync<string>("localStorage.getItem", StorageNameKey);

                if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id, out var parsed) && !string.IsNullOrWhiteSpace(name))
                {
                    CurrentUser = new AppUser { Id = parsed, Name = name };
                    Changed?.Invoke();
                }
            }
            catch
            {
                // Prerendering has no localStorage; the session simply stays empty.
            }
        }

        /// <summary>Signs in by name, creating the user the first time that name is used.</summary>
        public async Task<AppUser> SignInAsync(string name)
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, new { Name = name });
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<AppUser>()
                       ?? throw new InvalidOperationException("The server did not return a user.");

            CurrentUser = user;
            await PersistAsync(user);
            Changed?.Invoke();

            return user;
        }

        public async Task SignOutAsync()
        {
            CurrentUser = null;

            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", StorageIdKey);
                await _js.InvokeVoidAsync("localStorage.removeItem", StorageNameKey);
            }
            catch
            {
                // Nothing to clear when storage is unavailable.
            }

            Changed?.Invoke();
        }

        private async Task PersistAsync(AppUser user)
        {
            try
            {
                await _js.InvokeVoidAsync("localStorage.setItem", StorageIdKey, user.Id.ToString());
                await _js.InvokeVoidAsync("localStorage.setItem", StorageNameKey, user.Name);
            }
            catch
            {
                // Session just won't survive a refresh.
            }
        }
    }
}
