using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KBMS.Server;

public class ConfigManager
{
    public string Host { get; private set; } = "127.0.0.1";
    public int Port { get; private set; } = 3307;
    public string DataDir { get; private set; } = Path.GetFullPath("data");
    public string MasterKey { get; private set; } = "KBMS_V3_MASTER_SECRET_2026";
    public int MaxConnections { get; private set; } = 100;
    public string Version { get; private set; } = "3.4.0-stable";

    public string RootUsername { get; private set; } = "root";
    public string RootPassword { get; private set; } = "root";

    public Dictionary<string, string> SystemSettings { get; } = new();

    public static ConfigManager Load(string filePath)
    {
        var config = new ConfigManager();
        
        string fullPath = filePath;
        if (!Path.IsPathRooted(filePath))
        {
            fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            if (!File.Exists(fullPath))
            {
                // Fallback to CWD
                fullPath = Path.GetFullPath(filePath);
            }
        }

        if (!File.Exists(fullPath)) 
        {
            return config;
        }

        string currentSection = "";
        foreach (var line in File.ReadAllLines(fullPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                continue;

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSection = trimmed[1..^1].ToUpper();
                continue;
            }

            var parts = trimmed.Split('=', 2);
            if (parts.Length < 2) continue;

            string key = parts[0].Trim();
            string val = parts[1].Trim();

            switch (currentSection)
            {
                case "SERVER":
                    if (key.Equals("Host", StringComparison.OrdinalIgnoreCase) || key.Equals("host", StringComparison.OrdinalIgnoreCase)) config.Host = val;
                    else if ((key.Equals("Port", StringComparison.OrdinalIgnoreCase) || key.Equals("port", StringComparison.OrdinalIgnoreCase)) && int.TryParse(val, out var p)) config.Port = p;
                    else if (key.Equals("DataDir", StringComparison.OrdinalIgnoreCase) || key.Equals("data_dir", StringComparison.OrdinalIgnoreCase)) config.DataDir = Path.GetFullPath(val);
                    else if (key.Equals("MasterKey", StringComparison.OrdinalIgnoreCase) || key.Equals("master_key", StringComparison.OrdinalIgnoreCase)) config.MasterKey = val;
                    else if ((key.Equals("MaxConnections", StringComparison.OrdinalIgnoreCase) || key.Equals("max_connections", StringComparison.OrdinalIgnoreCase)) && int.TryParse(val, out var mc)) config.MaxConnections = mc;
                    else if (key.Equals("Version", StringComparison.OrdinalIgnoreCase) || key.Equals("version", StringComparison.OrdinalIgnoreCase)) config.Version = val;
                    break;

                case "ROOT":
                    if (key.Equals("Username", StringComparison.OrdinalIgnoreCase) || key.Equals("username", StringComparison.OrdinalIgnoreCase)) config.RootUsername = val;
                    else if (key.Equals("Password", StringComparison.OrdinalIgnoreCase) || key.Equals("password", StringComparison.OrdinalIgnoreCase)) config.RootPassword = val;
                    break;

                case "SETTINGS":
                    if (key.Equals("PageSize", StringComparison.OrdinalIgnoreCase)) continue;
                    config.SystemSettings[key] = val;
                    break;
            }
        }

        return config;
    }
}
