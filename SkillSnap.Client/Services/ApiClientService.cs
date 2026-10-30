using System.Net.Http.Json;
using SkillSnap.Shared.Models;

namespace SkillSnap.Client.Services;

// Centralizes every API call in one place so pages don't scatter raw
// HttpClient calls and duplicate error handling everywhere.
public class ApiClientService
{
    private readonly HttpClient _http;

    public ApiClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ProfileDto?> GetProfileAsync() =>
        await _http.GetFromJsonAsync<ProfileDto>("api/profile");

    public async Task<List<ProjectDto>> GetProjectsAsync() =>
        await _http.GetFromJsonAsync<List<ProjectDto>>("api/projects") ?? new();

    public async Task<List<SkillDto>> GetSkillsAsync() =>
        await _http.GetFromJsonAsync<List<SkillDto>>("api/skills") ?? new();

    public async Task<HttpResponseMessage> CreateProjectAsync(ProjectDto dto) =>
        await _http.PostAsJsonAsync("api/projects", dto);

    public async Task<HttpResponseMessage> UpdateProjectAsync(int id, ProjectDto dto) =>
        await _http.PutAsJsonAsync($"api/projects/{id}", dto);

    public async Task<HttpResponseMessage> DeleteProjectAsync(int id) =>
        await _http.DeleteAsync($"api/projects/{id}");

    public async Task<HttpResponseMessage> CreateSkillAsync(SkillDto dto) =>
        await _http.PostAsJsonAsync("api/skills", dto);

    public async Task<HttpResponseMessage> UpdateSkillAsync(int id, SkillDto dto) =>
        await _http.PutAsJsonAsync($"api/skills/{id}", dto);

    public async Task<HttpResponseMessage> DeleteSkillAsync(int id) =>
        await _http.DeleteAsync($"api/skills/{id}");
}
