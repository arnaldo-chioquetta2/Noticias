using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NewsImpactRanker.WinForms.Models;
using NewsImpactRanker.WinForms.Storage;

namespace NewsImpactRanker.WinForms.Services
{
    public static class CacheService
    {
        private static readonly object _lock = new object();
        private static List<NewsItem> _cache = new List<NewsItem>();
        private const int CacheMinAgeHours = 20;
        private const int CacheMaxDays = 30;

        static CacheService()
        {
            Load();
        }

        private static void Load()
        {
            if (!File.Exists(StorageManager.CachePath)) return;
            try
            {
                string json = File.ReadAllText(StorageManager.CachePath);
                _cache = JsonConvert.DeserializeObject<List<NewsItem>>(json) ?? new List<NewsItem>();
            }
            catch (Exception ex)
            {
                LogService.Error("Erro ao carregar cache", ex);
            }
        }

        public static NewsItem Get(string url, string textHash)
        {
            lock (_lock)
            {
                CleanupOldCache(); // 🔥 Limpa antigos automaticamente

                var item = _cache.FirstOrDefault(i => i.Url == url && i.TextHash == textHash);

                if (item == null)
                    return null;

                var age = DateTime.UtcNow - item.ProcessedAt.ToUniversalTime();

                // 🚫 Se rodou recentemente, NÃO usar cache
                if (age.TotalHours < CacheMinAgeHours)
                {
                    LogService.Info($"Execução recente ({age.TotalHours:F1}h). Reprocessando...");
                    return null;
                }

                // ✅ Se passou de 20h, pode usar
                LogService.Info($"Usando cache do dia anterior ({age.TotalHours:F1}h)");
                return item;
            }
        }

        private static void CleanupOldCache()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-CacheMaxDays);

            int beforeCount = _cache.Count;

            _cache.RemoveAll(i => i.ProcessedAt.ToUniversalTime() < cutoffDate);

            int removed = beforeCount - _cache.Count;

            if (removed > 0)
            {
                LogService.Info($"Limpeza automática: {removed} itens removidos (>30 dias)");

                try
                {
                    string json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                    File.WriteAllText(StorageManager.CachePath, json);
                }
                catch (Exception ex)
                {
                    LogService.Error("Erro ao salvar cache após limpeza", ex);
                }
            }
        }

        public static void Save(NewsItem item)
        {
            lock (_lock)
            {
                var existing = _cache.FirstOrDefault(i => i.Url == item.Url);
                if (existing != null) _cache.Remove(existing);
                
                _cache.Add(item);
                
                try
                {
                    string json = JsonConvert.SerializeObject(_cache, Formatting.Indented);
                    File.WriteAllText(StorageManager.CachePath, json);
                }
                catch (Exception ex)
                {
                    LogService.Error("Erro ao salvar cache", ex);
                }
            }
        }
    }
}
