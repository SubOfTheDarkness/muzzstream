using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using muzzstream.Models;

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
}
