using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using muzzstream.Models;
using System.Net;

namespace muzzstream.Services;

public static class ApiService {
    // HTTP и инициализация коннекта к серверу
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

    public static void SetAuthHeader(string token) {
        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Add("Authorization", token);
    }

    public static void ClearAuthHeader() {
        _client.DefaultRequestHeaders.Remove("Authorization");
    }

    public static async Task<int> LoginAsync(string identity, string password) {
        try {
            var payload = new { 
                identity = identity, 
                password = password, 
                device_name = Supports.DeviceName 
            };
            
            var response = await _client.PostAsJsonAsync("auth/login", payload);
            
            if (response.IsSuccessStatusCode) {
                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var root = doc.RootElement;
                
                AppSession.CurrentUser = new UserModel {
                    token = root.GetProperty("token").GetString() ?? "",
                    nickname = root.GetProperty("nickname").GetString() ?? "",
                    username = root.GetProperty("username").GetString() ?? "",
                    email = root.GetProperty("email").GetString() ?? "",
                    role_id = root.GetProperty("role_id").GetInt32()
                };
                
                SetAuthHeader(AppSession.CurrentUser.token);
                SessionManager.SaveSession(AppSession.CurrentUser); 
                return 200;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) {
                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                string detail = doc.RootElement.GetProperty("detail").GetString() ?? "";
                if (detail == "UNVERIFIED_EMAIL") {
                    return 403;
                }
            }
            
            return (int)response.StatusCode;
        }
        catch {
            return 500;
        }
    }

    public static async Task<string?> ResetPasswordAsync(string identity) {
        try {
            var payload = new { identity = identity };
            var response = await _client.PostAsJsonAsync("auth/reset-password", payload);
            if (response.IsSuccessStatusCode) return null;
            
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> ChangePasswordAsync(string oldPassword, string newPassword) {
        try {
            var payload = new { 
                old_password = oldPassword, 
                new_password = newPassword 
            };
            
            var response = await _client.PatchAsJsonAsync("auth/password/change", payload);
            
            if (response.IsSuccessStatusCode) return null;
            
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var detailProp = doc.RootElement.GetProperty("detail");
        
            if (detailProp.ValueKind == JsonValueKind.Array) {
                foreach (var errorElement in detailProp.EnumerateArray()) {
                    if (errorElement.TryGetProperty("msg", out var msgProp)) {
                        string serverMsg = msgProp.GetString() ?? "";
                        
                        string fieldLocation = "";
                        if (errorElement.TryGetProperty("loc", out var locProp) && locProp.ValueKind == JsonValueKind.Array) {
                            var locArray = locProp.EnumerateArray();
                            foreach (var loc in locArray) {
                                fieldLocation = loc.GetString() ?? "";
                            }
                        }
                        
                        return $"Ошибка валидации [{fieldLocation}]: {serverMsg}";
                    }
                }
                return "Ошибка валидации параметров.";
            }
            
            return detailProp.GetString();
        }
        catch (Exception ex) {
            return ex.Message;
        }
    }

    public static async Task<string?> RegisterAsync(string nickname, string username, string email, string password) {
        try {
            var payload = new { 
                nickname = nickname,
                username = username,
                email = email, 
                password = password 
            };
            
            var response = await _client.PostAsJsonAsync("auth/register", payload);
            if (response.IsSuccessStatusCode) return null;
            
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var detailProp = doc.RootElement.GetProperty("detail");
        
            if (detailProp.ValueKind == JsonValueKind.Array) {
                foreach (var errorElement in detailProp.EnumerateArray()) {
                    if (errorElement.TryGetProperty("msg", out var msgProp)) {
                        string serverMsg = msgProp.GetString() ?? "";
                        
                        if (serverMsg.Contains("value is not a valid email")) {
                            return "Неверный формат электронной почты.";
                        }
                        if (serverMsg.Contains("field required")) {
                            return "Заполните все обязательные поля.";
                        }
                        return $"Ошибка валидации: {serverMsg}";
                    }
                }
                return "Ошибка валидации переданных данных.";
            }
            
            return detailProp.GetString();
        }
        catch (Exception ex) {
            return ex.Message;
        }
    }

    public static async Task<string?> ConfirmEmailAsync(string email, string code) {
        try {
            var payload = new { email = email, code = code };
            var response = await _client.PostAsJsonAsync("auth/confirm", payload);
            if (response.IsSuccessStatusCode) return null;
            
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) {
            return ex.Message;
        }
    }
    public static async Task<List<PlaylistModel>> GetMyPlaylistsAsync() {
        try {
            if (AppSession.CurrentUser == null) return new List<PlaylistModel>();

            var response = await _client.GetAsync("playlists/my");
            if (!response.IsSuccessStatusCode) return new List<PlaylistModel>();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var playlists = new List<PlaylistModel>();

            foreach (var pElement in doc.RootElement.EnumerateArray()) {
                var playlist = new PlaylistModel {
                    playlist_id = pElement.GetProperty("playlist_id").GetInt32(),
                    user_id = pElement.GetProperty("user_id").GetInt32(),
                    playlist_name = pElement.GetProperty("playlist_name").GetString() ?? "Без названия",
                    is_public = pElement.GetProperty("is_public").GetBoolean(),
                    tracks_count = pElement.GetProperty("tracks_count").GetInt32(),
                    total_duration = pElement.GetProperty("total_duration").GetInt32()
                };

                if (pElement.TryGetProperty("tracks", out var tracksArray) && tracksArray.ValueKind == JsonValueKind.Array) {
                    playlist.tracks = ParseJsonTracks(tracksArray);
                }

                playlists.Add(playlist);
            }
            return playlists;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка при получении плейлистов: {ex.Message}");
            return new List<PlaylistModel>();
        }
    }

    public static async Task<PlaylistModel?> GetPlaylistByIdAsync(int playlistId) {
        try {
            var response = await _client.GetAsync($"playlists/{playlistId}");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var playlist = new PlaylistModel {
                playlist_id = root.GetProperty("playlist_id").GetInt32(),
                user_id = root.GetProperty("user_id").GetInt32(),
                playlist_name = root.GetProperty("playlist_name").GetString() ?? "Без названия",
                is_public = root.GetProperty("is_public").GetBoolean(),
                tracks_count = root.GetProperty("tracks_count").GetInt32(),
                total_duration = root.GetProperty("total_duration").GetInt32(),
                is_own_playlist = true 
            };

            if (root.TryGetProperty("tracks", out var tracksArray) && tracksArray.ValueKind == JsonValueKind.Array) {
                playlist.tracks = ParseJsonTracks(tracksArray);
            }

            return playlist;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"Ошибка при получении плейлиста по ID: {ex.Message}");
            return null;
        }
    }
    private static List<TrackModel> ParseJsonTracks(JsonElement tracksArray) {
        var tracks = new List<TrackModel>();
        foreach (var element in tracksArray.EnumerateArray()) {
            var comp = element.GetProperty("composition");
            string compName = comp.GetProperty("composition_name").GetString() ?? "Без названия";

            string albName = "";
            if (element.TryGetProperty("album", out var albumElement) && albumElement.ValueKind == JsonValueKind.Object) {
                if (albumElement.TryGetProperty("album_name", out var nameProp)) {
                    albName = nameProp.GetString() ?? "";
                }
            }

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

    public static async Task<string?> CreatePlaylistAsync(string name, bool isPublic) {
        try {
            var payload = new { playlist_name = name, is_public = isPublic };
            var response = await _client.PostAsJsonAsync("playlists/create", payload);
            if (response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> DeletePlaylistAsync(int playlistId) {
        try {
            var response = await _client.DeleteAsync($"playlists/{playlistId}/delete");
            if (response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> AddTrackToPlaylistAsync(int trackId, int playlistId) {
        try {
            var payload = new { track_id = trackId, playlist_id = playlistId };
            var response = await _client.PostAsJsonAsync("playlists/add-track", payload);
            
            if (response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;

            if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity) {
                if (root.TryGetProperty("detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.Array) {
                    var firstError = detailProp.EnumerateArray().FirstOrDefault();
                    if (firstError.TryGetProperty("msg", out var msgProp)) {
                        return $"Ошибка валидации сервера: {msgProp.GetString()}";
                    }
                }
                return "Некорректные параметры запроса.";
            }

            if (root.TryGetProperty("detail", out var dProp)) {
                return dProp.GetString();
            }

            return $"Ошибка сервера: {(int)response.StatusCode}";
        }
        catch (Exception ex) {
            return $"Сбой сети: {ex.Message}";
        }
    }
    public static async Task<string?> RemoveTrackFromPlaylistAsync(int trackId, int playlistId) {
        try {
            var payload = new { track_id = trackId, playlist_id = playlistId };
            
            var request = new HttpRequestMessage(HttpMethod.Delete, "playlists/remove-track") {
                Content = JsonContent.Create(payload)
            };
            
            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) {
            return ex.Message;
        }
    }

    public static async Task<List<PlaylistModel>> SearchPublicPlaylistsAsync(string query) {
        try {
            var response = await _client.GetAsync($"playlists/search/public?query={Uri.EscapeDataString(query)}");
            if (!response.IsSuccessStatusCode) return new List<PlaylistModel>();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var playlists = new List<PlaylistModel>();

            foreach (var pElement in doc.RootElement.EnumerateArray()) {
                var playlist = new PlaylistModel {
                    playlist_id = pElement.GetProperty("playlist_id").GetInt32(),
                    playlist_name = pElement.GetProperty("playlist_name").GetString() ?? "Без названия",
                    tracks_count = pElement.GetProperty("tracks_count").GetInt32(),
                    tracks = ParseJsonTracks(pElement.GetProperty("tracks")),
                    is_own_playlist = false 
                };
                playlists.Add(playlist);
            }
            return playlists;
        }
        catch { return new List<PlaylistModel>(); }
    }

    public static async Task<string?> AdminDeleteUserAsync(int userId) {
        try {
            var response = await _client.DeleteAsync($"moderation/users/{userId}/delete");
            if (response.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> AdminChangeUserRoleAsync(int userId, int newRoleId) {
        try {
            var payload = new { user_id = userId, new_role_id = newRoleId };
            var response = await _client.PatchAsJsonAsync("moderation/users/role/change", payload);
            if (response.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> AdminForceChangePasswordAsync(int userId, string newPassword) {
        try {
            var payload = new { user_id = userId, new_password = newPassword };
            var response = await _client.PatchAsJsonAsync("moderation/users/password/force-change", payload);
            if (response.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> ModeratorCreateUserAsync(string nickname, string username, string email, string password, bool isVerified) {
        try {
            var payload = new { 
                nickname = nickname, username = username, 
                email = email, password = password, is_verified = isVerified 
            };
            var response = await _client.PostAsJsonAsync("moderation/users/create-verified", payload);
            if (response.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> ModeratePlaylistVisibilityAsync(int playlistId, bool isPublic) {
        try {
            var response = await _client.PatchAsync($"moderation/playlists/{playlistId}/visibility?is_public={isPublic}", null);
            if (response.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> ModerateDeleteTrackAsync(int trackId) {
        try {
            var response = await _client.DeleteAsync($"moderation/tracks/{trackId}/delete");
            if (response.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> ModerateApplySanctionAsync(int authorId, int targetStatus) {
        try {
            var response = await _client.PatchAsync($"moderation/authors/{authorId}/status-sanction?target_status={targetStatus}", null);
            if (response.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<List<UserModel>> AdminGetAllUsersAsync() {
        try {
            return await _client.GetFromJsonAsync<List<UserModel>>("moderation/users/all") ?? new();
        }
        catch { return new List<UserModel>(); }
    }

    public static async Task<List<PlaylistModel>> AdminGetAllPlaylistsAsync() {
        try {
            return await _client.GetFromJsonAsync<List<PlaylistModel>>("moderation/playlists/all") ?? new();
        }
        catch { return new List<PlaylistModel>(); }
    }

    public static async Task<List<AlbumModel>> AdminGetAllAlbumsAsync() {
    try { return await _client.GetFromJsonAsync<List<AlbumModel>>("moderation/albums/all-raw") ?? new(); } catch { return new(); }
    }
    public static async Task<List<AuthorModel>> AdminGetAllAuthorsAsync() {
        try { return await _client.GetFromJsonAsync<List<AuthorModel>>("moderation/authors/all-raw") ?? new(); } catch { return new(); }
    }
    public static async Task<string?> AdminDeleteAlbumAsync(int id) {
        try { var r = await _client.DeleteAsync($"moderation/albums/{id}/delete"); return r.IsSuccessStatusCode ? null : "Ошибка"; } catch (Exception ex) { return ex.Message; }
    }
    public static async Task<string?> AdminDeleteAuthorAsync(int id) {
        try { var r = await _client.DeleteAsync($"moderation/authors/{id}/delete-force"); return r.IsSuccessStatusCode ? null : "Ошибка"; } catch (Exception ex) { return ex.Message; }
    }
    public static async Task<string?> AdminDeleteGenreAsync(int id) {
        try { var r = await _client.DeleteAsync($"moderation/genres/{id}/delete"); return r.IsSuccessStatusCode ? null : "Ошибка"; } catch (Exception ex) { return ex.Message; }
    }

    public static async Task<List<PlaylistModel>> AdminGetAllPlaylistsWithAuthorsAsync() {
        try {
            var response = await _client.GetAsync("moderation/playlists/all-details");
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var list = new List<PlaylistModel>();
            foreach (var el in doc.RootElement.EnumerateArray()) {
                list.Add(new PlaylistModel {
                    playlist_id = el.GetProperty("playlist_id").GetInt32(),
                    playlist_name = el.GetProperty("playlist_name").GetString() ?? "",
                    is_public = el.GetProperty("is_public").GetBoolean(),
                    user_id = el.GetProperty("user_id").GetInt32(),
                    ui_creator_nick = el.GetProperty("creator_nick").GetString() ?? "Системный"
                });
            }
            return list;
        }
        catch { return new(); }
    }

    public static async Task<string?> AdminAddGenreAsync(string name) {
        try {
            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("genre_name", name) });
            var res = await _client.PostAsync("moderation/genres/add", content);
            return res.IsSuccessStatusCode ? null : "Ошибка добавления жанра";
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> AdminAddAlbumAsync(string name, int year, List<int> authorIds, List<List<int>> genreIds) {
        try {
            var pairs = new List<KeyValuePair<string, string>> {
                new("album_name", name),
                new("release_year", year.ToString())
            };

            foreach (var id in authorIds) {
                pairs.Add(new KeyValuePair<string, string>("author_ids", id.ToString()));
            }

            foreach (var id in genreIds.SelectMany(g => g)) {
                pairs.Add(new KeyValuePair<string, string>("genre_ids", id.ToString()));
            }

            var content = new FormUrlEncodedContent(pairs);
            var res = await _client.PostAsync("moderation/albums/add", content);
            
            if (res.IsSuccessStatusCode) return null;
            
            using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> AdminUploadTrackAsync(int authorId, string compName, int year, int duration, int genreId, string version, int? albumId, string localPath) {
        try {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(authorId.ToString()), "author_id");
            content.Add(new StringContent(compName), "composition_name");
            content.Add(new StringContent(year.ToString()), "release_year");
            content.Add(new StringContent(duration.ToString()), "duration");
            content.Add(new StringContent(genreId.ToString()), "genre_id");
            content.Add(new StringContent(version), "version_type");
            if (albumId.HasValue) content.Add(new StringContent(albumId.Value.ToString()), "album_id");

            var fileBytes = System.IO.File.ReadAllBytes(localPath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            content.Add(fileContent, "audio_file", System.IO.Path.GetFileName(localPath));

            var res = await _client.PostAsync("moderation/tracks/upload", content);
            if (res.IsSuccessStatusCode) return null;
            using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { return ex.Message; }
    }

    public static async Task<string?> AdminAddSyntheticAuthorAsync(string name, string country) {
        try {
            var payload = new { 
                author_name = name, 
                country = string.IsNullOrWhiteSpace(country) ? null : country 
            };
            
            var response = await _client.PostAsJsonAsync("moderation/authors/create-synthetic", payload);
            
            if (response.IsSuccessStatusCode) return null;
            
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return doc.RootElement.GetProperty("detail").GetString();
        }
        catch (Exception ex) { 
            return ex.Message; 
        }
    }
}
