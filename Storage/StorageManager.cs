using NewsImpactRanker.WinForms.Models;
using Newtonsoft.Json;
using System;
using System.IO;

namespace NewsImpactRanker.WinForms.Storage
{
    public static class StorageManager
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NewsImpactRanker"
        );

        public static readonly string ConfigPath = Path.Combine(AppDataPath, "config.json");
        public static readonly string CachePath = Path.Combine(AppDataPath, "cache.json");
        public static readonly string LogsPath = Path.Combine(AppDataPath, "logs");

        static StorageManager()
        {
            if (!Directory.Exists(AppDataPath)) Directory.CreateDirectory(AppDataPath);
            if (!Directory.Exists(LogsPath)) Directory.CreateDirectory(LogsPath);
        }

        public static AppConfig LoadConfig()
        {
            if (!File.Exists(ConfigPath)) return new AppConfig();
            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void SaveConfig(AppConfig config)
        {
            // Usa o Newtonsoft.Json para converter o objeto em texto formatado (identado)
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }

    }
}
