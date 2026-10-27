using System;
using System.IO;
using System.Text.Json;
using muzzstream.Models;

namespace muzzstream.Services;

public static class SessionManager {
    private static readonly string SessionPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "muzzstream", "session.json"
    );

    private class SessionData {
        public UserModel? User { get; set; }
        public float Volume { get; set; } = 0.5f;
    }

    public static void SaveSession(UserModel? user, float volume) {
        try {
            var dir = Path.GetDirectoryName(SessionPath);
            if (dir != null) Directory.CreateDirectory(dir);

            var data = new SessionData { User = user, Volume = volume };
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText(SessionPath, json);
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
        }
    }

    public static void SaveSession(UserModel? user) {
        try {
            float currentVolume = 0.5f;
            if (File.Exists(SessionPath)) {
                string json = File.ReadAllText(SessionPath);
                var oldData = JsonSerializer.Deserialize<SessionData>(json);
                if (oldData != null) currentVolume = oldData.Volume;
            }

            SaveSession(user, currentVolume);
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
        }
    }

    public static UserModel? LoadSession(out float savedVolume) {
        savedVolume = 0.5f;
        try {
            if (!File.Exists(SessionPath)) return null;

            string json = File.ReadAllText(SessionPath);
            var data = JsonSerializer.Deserialize<SessionData>(json);
            
            if (data != null) {
                savedVolume = data.Volume;
                return data.User;
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
        }
        return null;
    }

    public static void ClearSession() {
        try {
            float currentVolume = 0.5f;
            
            if (File.Exists(SessionPath)) {
                string json = File.ReadAllText(SessionPath);
                var oldData = JsonSerializer.Deserialize<SessionData>(json);
                if (oldData != null) currentVolume = oldData.Volume;
            }

            SaveSession(null, currentVolume);
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
        }
    }
}
