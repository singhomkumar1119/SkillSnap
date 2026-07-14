using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

// This is the app's central piece of client-side state management: it decides
// who Blazor thinks is logged in (via AuthenticationStateProvider) AND holds
// the JWT used for every API call. Keeping both in one scoped service means
// login/logout instantly updates every component that depends on auth state,
// without manually notifying each page.
public class AuthStateService : AuthenticationStateProvider
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "skillsnap_token";

    public AuthStateService(HttpClient http, ILocalStorageService localStorage)
    {
        _http = http;
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(TokenKey);

        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task<(bool success, string? error)> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginDto { Email = email, Password = password });

        if (!response.IsSuccessStatusCode)
        {
            return (false, "Invalid email or password.");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result is null) return (false, "Unexpected server response.");

        await _localStorage.SetItemAsync(TokenKey, result.Token);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

        return (true, null);
    }

    public async Task<(bool success, string? error)> RegisterAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", new RegisterDto { Email = email, Password = password });

        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadAsStringAsync();
            return (false, "Registration failed. Check your password meets the requirements.");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (result is null) return (false, "Unexpected server response.");

        await _localStorage.SetItemAsync(TokenKey, result.Token);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

        return (true, null);
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TokenKey);
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    // Minimal JWT payload decoder — just enough to read the claims Blazor's
    // AuthorizeView needs, without pulling in a full JWT library on the client.
    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
        var json = System.Text.Json.JsonDocument.Parse(bytes);

        return json.RootElement.EnumerateObject()
            .Select(prop => new Claim(prop.Name, prop.Value.ToString()));
    }
}
