using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NewsImpactRanker.WinForms.Storage;

namespace NewsImpactRanker.WinForms.Services
{
    public static class TokenUsageService
    {
        private static readonly object _lock = new object();

        private static string UsageFile =>
            Path.Combine(StorageManager.LogsPath, "token_usage.json");

        public static void RegisterUsage(int tokens)
        {
            lock (_lock)
            {
                var list = Load();

                list.Add(new TokenUsageEntry
                {
                    Timestamp = DateTime.Now,
                    Tokens = tokens
                });

                Save(list);
            }
        }

        public static int GetLast24hTokens()
        {
            lock (_lock)
            {
                var list = Load();

                DateTime cutoff = DateTime.Now.AddHours(-24);

                list = list
                    .Where(x => x.Timestamp >= cutoff)
                    .ToList();

                Save(list); // limpa registros antigos

                return list.Sum(x => x.Tokens);
            }
        }

        public static int GetLast24hRequests()
        {
            lock (_lock)
            {
                var list = Load();

                DateTime cutoff = DateTime.Now.AddHours(-24);

                list = list
                    .Where(x => x.Timestamp >= cutoff)
                    .ToList();

                Save(list);

                return list.Count;
            }
        }

        private static List<TokenUsageEntry> Load()
        {
            if (!File.Exists(UsageFile))
                return new List<TokenUsageEntry>();

            return JsonConvert.DeserializeObject<List<TokenUsageEntry>>(
                File.ReadAllText(UsageFile)
            ) ?? new List<TokenUsageEntry>();
        }

        private static void Save(List<TokenUsageEntry> list)
        {
            File.WriteAllText(
                UsageFile,
                JsonConvert.SerializeObject(list, Formatting.Indented)
            );
        }
    }
}