using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NewsImpactRanker.WinForms.Models;

namespace NewsImpactRanker.WinForms.Services
{
    public class ScrapingService
    {
        private readonly HttpClient _httpClient;
        private readonly List<string> _userAgents = new List<string>
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:122.0) Gecko/20100101 Firefox/122.0",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36"
        };

        public ScrapingService()
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
        }

        // ✅ Verifique se o ScrapeAsync está retornando os status corretamente
        // Estas são as modificações necessárias no ScrapingService.cs:

        public async Task<NewsItem> ScrapeAsync(string url)
        {
            LogService.Info($"Iniciando scraping: {url}");

            string html = await GetHtmlWithRetry(url);

            // ✅ PRIMEIRO: validar null ou vazio
            if (string.IsNullOrWhiteSpace(html))
            {
                LogService.Warn($"HTML vazio ou nulo para {url}");

                return new NewsItem
                {
                    Url = url,
                    Status = "Sem Conteúdo",
                    ProcessedAt = DateTime.Now
                };
            }

            // ✅ SÓ DEPOIS logar início do HTML
            LogService.Info("🌐 HTML início: " +
                html.Substring(0, Math.Min(120, html.Length)));

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Verificar bloqueio
            if (IsBlocked(html))
            {
                LogService.Warn($"Bloqueio detectado para {url}");

                return new NewsItem
                {
                    Url = url,
                    Status = "Bloqueado",
                    ProcessedAt = DateTime.Now
                };
            }

            string title = ExtractTitle(doc);
            string text = ExtractText(doc);

            if (string.IsNullOrWhiteSpace(text))
            {
                LogService.Warn($"Texto não extraído para {url}");

                return new NewsItem
                {
                    Url = url,
                    Status = "Sem Conteúdo",
                    ProcessedAt = DateTime.Now
                };
            }

            // Normalização
            text = NormalizeText(text);

            if (text.Length > 12000)
                text = text.Substring(0, 12000);

            return new NewsItem
            {
                Url = url,
                Title = title,
                RawText = text,
                TextHash = ComputeHash(text),
                Status = "Sucesso",
                ProcessedAt = DateTime.Now
            };
        }

        private async Task<string> GetHtmlWithRetry(string url, int retries = 2)
        {
            for (int i = 0; i <= retries; i++)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);

                    request.Headers.Add("User-Agent", _userAgents[new Random().Next(_userAgents.Count)]);
                    request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    request.Headers.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");

                    // ✅ NÃO pedir br (Brotli) no .NET Framework
                    request.Headers.Remove("Accept-Encoding");
                    request.Headers.Add("Accept-Encoding", "gzip, deflate");

                    request.Headers.Add("Connection", "keep-alive");
                    request.Headers.Add("Upgrade-Insecure-Requests", "1");
                    request.Headers.Add("Cache-Control", "max-age=0");

                    var response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        // ✅ Agora que o handler descomprime gzip/deflate, podemos ler como string
                        string htmlContent = await response.Content.ReadAsStringAsync();

                        // ✅ Remover BOM/invisíveis
                        htmlContent = (htmlContent ?? "")
                            .TrimStart('\uFEFF', '\u200B', '\u200C', '\u200D', '\u00A0')
                            .Trim();

                        return htmlContent;
                    }

                    if (i < retries)
                    {
                        int delay = (i + 1) * 2000;
                        LogService.Warn($"Retry {i + 1} para {url} após {delay}ms (Status: {response.StatusCode})");
                        await Task.Delay(delay);
                    }
                }
                catch (Exception ex)
                {
                    if (i == retries) throw;
                    LogService.Warn($"Erro no scraping (tentativa {i + 1}): {ex.Message}");
                    await Task.Delay(1000);
                }
            }

            return null;
        }

        private bool IsBlocked(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return true;

            string lowerHtml = html.ToLowerInvariant();

            // 1) HTML muito curto (erro/redirecionamento/bloqueio)
            if (html.Length < 300)
            {
                LogService.Warn($"HTML suspeitamente curto: {html.Length} chars");
                return true;
            }

            // 2) Página parece ser erro HTTP
            if (Regex.IsMatch(lowerHtml, @"<title>\s*(404|403|500|503|error|not found)", RegexOptions.IgnoreCase) ||
                (lowerHtml.Contains("page not found") && lowerHtml.Contains("<h1>404")))
            {
                LogService.Warn("Página parece ser um erro HTTP");
                return true;
            }

            return false;
        }
        private string ExtractTitle(HtmlDocument doc)
        {
            var h1 = doc.DocumentNode.SelectSingleNode("//h1");
            if (h1 != null) return WebUtility.HtmlDecode(h1.InnerText.Trim());

            var title = doc.DocumentNode.SelectSingleNode("//title");
            if (title != null) return WebUtility.HtmlDecode(title.InnerText.Trim());

            return "Sem Título";
        }

        private string ExtractText(HtmlDocument doc)
        {
            // Remover elementos indesejados
            var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//nav|//header|//footer|//aside|//iframe|//form|//noscript");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove) node.Remove();
            }

            // ✅ EXPANDIR seletores para estruturas modernas
            var articleSelectors = new[]
            {
        "//article",
        "//main",
        "//*[contains(@class, 'content') or contains(@class, 'post') or contains(@class, 'entry') or contains(@class, 'article') or contains(@class, 'body') or contains(@data-testid, 'article')]",
        "//div[@role='main']",
        "//section[contains(@class, 'article')]"
    };

            HtmlNode target = null;
            foreach (var selector in articleSelectors)
            {
                var node = doc.DocumentNode.SelectSingleNode(selector);
                if (node != null)
                {
                    target = node;
                    break;
                }
            }

            target = target ?? doc.DocumentNode; // Fallback para body

            // ✅ Extrair parágrafos de forma mais flexível
            var paragraphs = target.SelectNodes(".//p[not(ancestor::script) and not(ancestor::style) and normalize-space(text()) != '']");

            if (paragraphs == null || paragraphs.Count == 0)
            {
                // ✅ Último recurso: pegar todo o texto visível do target
                string allText = WebUtility.HtmlDecode(target.InnerText);
                if (!string.IsNullOrWhiteSpace(allText) && allText.Length > 100)
                {
                    return allText;
                }
                return null;
            }

            var sb = new StringBuilder();
            foreach (var p in paragraphs)
            {
                string pText = WebUtility.HtmlDecode(p.InnerText.Trim());
                if (pText.Length > 30) // Reduzido de 20 para 30 para filtrar melhor
                    sb.AppendLine(pText);
            }

            return sb.ToString();
        }

        private string NormalizeText(string text)
        {
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}
