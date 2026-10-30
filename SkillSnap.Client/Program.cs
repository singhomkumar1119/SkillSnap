using Microsoft.AspNetCore.Components.Web;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SkillSnap.Client;
using SkillSnap.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API runs on a different origin during local development, so the base
// address is read from configuration rather than hardcoded.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7100";

builder.Services.AddBlazoredLocalStorage();

// AuthStateService is our single source of truth for "who is logged in and
// what's their token" — everything else (HttpClient, AuthStateProvider,
// pages) reads from this one scoped service instead of duplicating state.
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateService>());
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ApiClientService>();

await builder.Build().RunAsync();
