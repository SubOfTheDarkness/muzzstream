using System;
using System.IO;
using System.Text.Json;
using muzzstream.Models;

namespace muzzstream.Services;

public static class SessionManager {
    private static readonly string SessionFolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "muzzstream"
    );
    
    private static readonly string SessionFilePath = Path.Combine(SessionFolderPath, "session.json");

    public static void SaveSession(UserModel user) {
        try {
            if (!Directory.Exists(SessionFolderPath)) {
                Directory.CreateDirectory(SessionFolderPath);
            }

            var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SessionFilePath, json);
            System.Diagnostics.Debug.WriteLine("Сессия успешно сохранена на диск.");
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
        }
    }

    public static UserModel? LoadSession() {
        try {
            if (!File.Exists(SessionFilePath)) return null;

            var json = File.ReadAllText(SessionFilePath);
            var user = JsonSerializer.Deserialize<UserModel>(json);
            
            System.Diagnostics.Debug.WriteLine(user != null 
                ? "Найдена сохраненная сессия на диске." 
                : "Файл сессии пуст.");
                
            return user;
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
            return null;
        }
    }

    public static void ClearSession() {
        try {
            if (File.Exists(SessionFilePath)) {
                File.Delete(SessionFilePath);
                System.Diagnostics.Debug.WriteLine("Файл сессии удален с диска.");
            }
        }
        catch (Exception ex) {
            System.Diagnostics.Debug.WriteLine($"{ex.Message}");
        }
    }
}
