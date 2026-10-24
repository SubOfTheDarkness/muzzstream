using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using muzzstream.Models;

namespace muzzstream.Services;

public static class ApiService {
    private static readonly HttpClient _client = new()  { 
        BaseAddress = new Uri(AppConfig.BaseApiUrl),
        Timeout = TimeSpan.FromSeconds(5)
    };

    public static async Task<bool> PingServerAsync() {
        try {
            var response = await _client.GetAsync("");
            return response.IsSuccessStatusCode;
        }
        catch {
            return false;
        }
    }

    public static async Task LoadAllSystemLookupsAsync() {
        try {
            AppSession.StaticRoles = await _client.GetFromJsonAsync<List<RoleModel>>("roles/all") ?? new();
            AppSession.StaticGenres = await _client.GetFromJsonAsync<List<GenreModel>>("genres/all") ?? new();
            AppSession.StaticGroupRoles = await _client.GetFromJsonAsync<List<GroupRoleModel>>("group_roles/all") ?? new();
            AppSession.StaticAuthorStatuses = await _client.GetFromJsonAsync<List<AuthorStatusModel>>("author_statuses/all") ?? new();
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка при получении справочников с сервера: {ex.Message}");
            throw;
        }
    }

    public static async Task<List<TrackModel>> GetAllTracksAsync() {
        try {
            var response = await _client.GetAsync("tracks/all");
            if (!response.IsSuccessStatusCode) return new List<TrackModel>();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var tracks = new List<TrackModel>();

            foreach (var element in doc.RootElement.EnumerateArray()) {
                var comp = element.GetProperty("composition");
                string compName = comp.GetProperty("composition_name").GetString() ?? "Без названия";

                bool hasAlbum = element.TryGetProperty("album", out var albumElement) && 
                                albumElement.ValueKind != JsonValueKind.Null;
                string albName = hasAlbum ? (albumElement.GetProperty("album_name").GetString() ?? "Неизвестен") : "";

                var authorsList = new List<string>();
                if (comp.TryGetProperty("authors", out var authorsArray) && authorsArray.ValueKind == JsonValueKind.Array) {
                    foreach (var authorObj in authorsArray.EnumerateArray()) {
                        if (authorObj.TryGetProperty("author_name", out var nameProp)) {
                            string name = nameProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(name)) authorsList.Add(name);
                        }
                    }
                }

                string cleanAuthorsStr = authorsList.Count > 0 ? string.Join(", ", authorsList) : "Неизвестен";

                tracks.Add(new TrackModel {
                    track_id = element.GetProperty("track_id").GetInt32(),
                    duration = element.GetProperty("duration").GetInt32(),
                    version_type = element.GetProperty("version_type").GetString() ?? "Studio",
                    
                    composition_name = compName,
                    authors_list = cleanAuthorsStr,
                    album_name = albName
                });
            }
            return tracks;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка при получении списка треков с сервера: {ex.Message}");
            return new List<TrackModel>();
        }
    }

    public static async Task LogListeningAsync(int trackId, int duration){
        try {
            if (AppSession.CurrentUser == null) return;
            var payload = new { user_id = AppSession.CurrentUser.user_id, track_id = trackId, listened_duration = duration };
            await _client.PostAsJsonAsync("tracks/listen", payload);
        }
        catch {  }
    }
}
