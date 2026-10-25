using System;
using System.IO;

namespace muzzstream.Services;

public static class Supports {
    private static string _deviceName = string.Empty;

    public static string DeviceName => GetDynamicDeviceName();

    private static string GetDynamicDeviceName() {
        if (string.IsNullOrEmpty(_deviceName)) {
            try {
                string osUser = Environment.UserName;

                string hostName = Environment.MachineName;

                string physicalCompName = "UnknownBoard";

                if (OperatingSystem.IsLinux()) {
                    string boardPath = "/sys/class/dmi/id/board_name";
                    if (File.Exists(boardPath)) {
                        physicalCompName = File.ReadAllText(boardPath).Trim();
                    }
                }
                else if (OperatingSystem.IsWindows()) {
                    string regPath = @"HARDWARE\DESCRIPTION\System\BIOS";
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(regPath);
                    if (key != null) {
                        physicalCompName = key.GetValue("BaseBoardProduct")?.ToString()?.Trim() 
                                           ?? key.GetValue("SystemProductName")?.ToString()?.Trim() 
                                           ?? "WindowsDevice";
                    }
                }

                _deviceName = $"{osUser}@{hostName}-{physicalCompName}";
            }
            catch {
                _deviceName = $"{Environment.UserName}@{Environment.MachineName}-GenericDevice";
            }
        }
        return _deviceName;
    }
}
