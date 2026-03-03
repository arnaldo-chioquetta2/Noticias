using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using NewsImpactRanker.WinForms.Utils;
using NewsImpactRanker.WinForms.Models;
using NewsImpactRanker.WinForms.Storage;
using NewsImpactRanker.WinForms.Services;
// using static System.Net.Mime.MediaTypeNames;

namespace NewsImpactRanker.WinForms.Forms
{
    public partial class MainForm : Form
    {
        private readonly ScrapingService _scrapingService;
        // private readonly GeminiService _groqService;
        private readonly GroqService _groqService;
        private CancellationTokenSource _cts;
        private bool _limitToFive = true;   // 🔥 mude para false quando quiser liberar geral
        private bool _currentExecutionUsesFile = false;

        // ✅ NOVO: Lista para rastrear falhas de scraping por domínio
        private readonly List<string> _failedDomains = new List<string>();
        private readonly object _failedDomainsLock = new object();

        public MainForm()
        {
            InitializeComponent();
            _scrapingService = new ScrapingService();
            // _geminiService = new GeminiService();
            _groqService = new GroqService();

            dgvResults.SortCompare += DgvResults_SortCompare;

            // ✅ Log de inicialização
            LogService.Info("=== NewsImpactRanker Iniciado ===");
            LogService.Info($"Config path: {StorageManager.ConfigPath}");
            LogService.Info($"Cache path: {StorageManager.CachePath}");
            LogService.Info($"Logs path: {StorageManager.LogsPath}");
        }

        private void DgvResults_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (e.Column.Name == "colImpact")
            {
                double v1 = double.Parse(e.CellValue1?.ToString() ?? "0");
                double v2 = double.Parse(e.CellValue2?.ToString() ?? "0");
                e.SortResult = v1.CompareTo(v2);
                e.Handled = true;
            }
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            using (var configForm = new ConfigForm())
            {
                configForm.ShowDialog();
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            var config = StorageManager.LoadConfig();

            if (string.IsNullOrEmpty(config.AiApiKey))
            {
                MessageBox.Show("Configure a API Key antes de iniciar.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                btnConfig_Click(null, null);
                return;
            }

            List<string> validUrls;
            bool usingFile = false;

            // 🔹 1️⃣ Verificar se há URLs digitadas
            var typedUrls = txtUrls.Lines
                .Select(l => l.Trim())
                .Where(l => UrlValidator.IsValid(l))
                .Distinct()
                .ToList();

            if (typedUrls.Any())
            {
                validUrls = typedUrls;

                if (_limitToFive)
                    validUrls = validUrls.Take(5).ToList();

                LogService.Info("Modo: URLs digitadas manualmente");
            }
            else
            {
                // 🔹 2️⃣ Caso contrário usar arquivo configurado
                if (string.IsNullOrWhiteSpace(config.NewsFilePath) ||
                    !File.Exists(config.NewsFilePath))
                {
                    MessageBox.Show("Configure o arquivo de notícias nas Configurações.",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    btnConfig_Click(null, null);
                    return;
                }

                validUrls = File.ReadAllLines(config.NewsFilePath)
                    .Select(l => l.Trim())
                    .Where(l => UrlValidator.IsValid(l))
                    .Distinct()
                    .ToList();

                if (_limitToFive)
                    validUrls = validUrls.Take(5).ToList();

                usingFile = true;

                LogService.Info("Modo: Arquivo configurado");
                LogService.Info($"Arquivo: {config.NewsFilePath}");
            }

            if (validUrls.Count == 0)
            {
                MessageBox.Show("Nenhuma URL válida encontrada.",
                    "Aviso",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _currentExecutionUsesFile = usingFile;

            LogService.Info($"URLs carregadas: {validUrls.Count}" +
                (_limitToFive ? " (modo limite 5 ativo)" : ""));

            // 🔹 Resetar falhas
            lock (_failedDomainsLock)
            {
                _failedDomains.Clear();
            }

            ToggleUI(false);
            dgvResults.Rows.Clear();
            progressBar.Maximum = validUrls.Count;
            progressBar.Value = 0;
            lblProgress.Text = $"0/{validUrls.Count}";

            _cts = new CancellationTokenSource();

            int parallelism = (int)nudParallelism.Value;
            if (parallelism > 2)
            {
                LogService.Info($"Paralelismo reduzido de {parallelism} para 2 (API Free Tier)");
                parallelism = 2;
            }

            try
            {
                await ProcessUrlsAsync(validUrls, parallelism, _cts.Token);
                SaveFinalRankingToFile();
                //ShowFailedDomainsReport();
            }
            catch (OperationCanceledException)
            {
                LogService.Info("Processamento cancelado pelo usuário.");
                MessageBox.Show("Processamento cancelado.",
                    "Informação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogService.Error("Erro fatal no processamento", ex);
                MessageBox.Show($"Ocorreu um erro: {ex.Message}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                ToggleUI(true);
            }
        }

        private string _lastReportPath;

        private void SaveFinalRankingToFile()
        {

            try
            {
                if (dgvResults.Rows.Count == 0)
                    return;

                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "NewsImpactRanker");

                Directory.CreateDirectory(folder);

                string fileName = $"Ranking_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string fullPath = Path.Combine(folder, fileName);

                int total = dgvResults.Rows.Count;

                int successCount = dgvResults.Rows
                    .Cast<DataGridViewRow>()
                    .Count(r => r.Cells["colStatus"].Value?.ToString() == "Sucesso");

                int failCount = total - successCount;

                double successRate = total > 0
                    ? (successCount * 100.0 / total)
                    : 0;

                var rankingLines = dgvResults.Rows
                    .Cast<DataGridViewRow>()
                    .Where(r => r.Cells["colImpact"].Value != null)
                    .OrderByDescending(r => Convert.ToDouble(r.Cells["colImpact"].Value))
                    .Select(r =>
                    {
                        string score = r.Cells["colImpact"].Value?.ToString();
                        string url = r.Cells["colUrl"].Value?.ToString();
                        return $"{score} | {url}";
                    })
                    .ToList();

                List<string> fileContent = new List<string>();

                fileContent.Add("=== NEWS IMPACT RANKER REPORT ===");
                fileContent.Add($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                fileContent.Add("");
                fileContent.Add($"Total de URLs: {total}");
                fileContent.Add($"Sucesso: {successCount}");
                fileContent.Add($"Falha: {failCount}");
                fileContent.Add($"Taxa de sucesso: {successRate:F1}%");
                fileContent.Add("");

                fileContent.Add("=== RANKING ===");
                fileContent.AddRange(rankingLines);
                fileContent.Add("");

                // 🔥 Domínios com falha
                lock (_failedDomainsLock)
                {
                    if (_failedDomains.Any())
                    {
                        fileContent.Add("=== DOMÍNIOS COM FALHA ===");

                        foreach (var domain in _failedDomains)
                            fileContent.Add(domain);

                        fileContent.Add("");
                    }
                }

                int tokensLast24h = TokenUsageService.GetLast24hTokens();
                int requestsLast24h = TokenUsageService.GetLast24hRequests();

                fileContent.Add("=== CONSUMO API ===");
                fileContent.Add($"Execução atual: {GroqService.GetTotalTokensToday()} tokens");
                fileContent.Add($"Últimas 24h: {tokensLast24h} tokens");
                fileContent.Add($"Requests últimas 24h: {requestsLast24h}");
                fileContent.Add("");

                File.WriteAllLines(fullPath, fileContent);

                _lastReportPath = fullPath;

                LogService.Info($"Relatório completo salvo em: {fullPath}");
            }
            catch (Exception ex)
            {
                LogService.Error("Erro ao salvar relatório final", ex);
            }
        }

        private async Task ProcessUrlsAsync(List<string> urls, int parallelism, CancellationToken ct)
        {
            using (var semaphore = new SemaphoreSlim(parallelism))
            {
                var tasks = urls.Select(async url =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        if (ct.IsCancellationRequested) return;
                        await ProcessSingleUrlAsync(url);
                    }
                    finally
                    {
                        semaphore.Release();
                        UpdateProgress();
                    }
                });

                await Task.WhenAll(tasks);
            }
        }

        private async Task ProcessSingleUrlAsync(string url)
        {
            try
            {
                // 1. Scraping
                var item = await _scrapingService.ScrapeAsync(url);

                // ✅ Rastrear falhas de scraping por domínio
                if (item.Status != "Sucesso")
                {
                    string domain = ExtractDomain(url);
                    lock (_failedDomainsLock)
                    {
                        if (!_failedDomains.Contains(domain))
                        {
                            _failedDomains.Add(domain);
                            LogService.Warn($"Domínio adicionado à lista de falhas: {domain} (Status: {item.Status})");
                        }
                    }
                }

                if (item.Status == "Sucesso")
                {
                    // 2. Verificar Cache
                    var cached = CacheService.Get(url, item.TextHash);
                    if (cached != null)
                    {
                        LogService.Info($"Usando cache para {url}");
                        AddOrUpdateGrid(cached);
                        return;
                    }

                    LogService.Info($"🧾 URL atual: {url}"); // passe a url pra este método ou logue no caller

                    // 3. Gemini - Classificação por IA
                    try
                    {
                        // ✅ Chamada corrigida: sem CancellationToken (removido para simplificar)
                        var analysis = await _groqService.ClassifyNewsAsync(item.RawText);

                        // ✅ Preencher dados da classificação
                        item.ImpactScore = analysis.impactScore;
                        item.ImpactReason = analysis.impactReason;
                        item.Category = analysis.category;
                        item.Status = "Sucesso"; // ✅ Garantir status correto após classificação

                        // ✅ Salvar no cache para reutilização futura
                        CacheService.Save(item);

                        LogService.Info($"Classificação concluída para {url}: Score={analysis.impactScore}, Categoria={analysis.category}");
                    }
                    catch (Exception ex) when (ex.Message.Contains("429") || ex.Message.Contains("RESOURCE_EXHAUSTED"))
                    {
                        // ✅ Tratamento específico para rate limit da API Gemini
                        item.Status = "Limite API";
                        LogService.Warn($"Rate limit Gemini para {url}. Tente novamente mais tarde.");
                    }
                    catch (Exception ex)
                    {
                        // ✅ Tratamento para outros erros da API Gemini
                        item.Status = "Erro Gemini";
                        LogService.Error($"Erro Gemini para {url}", ex);
                    }
                }

                // ✅ Adicionar item ao DataGridView (sucesso ou falha)
                AddOrUpdateGrid(item);
            }
            catch (Exception ex) when (ex.Message.Contains("conjunto de caracteres") || ex.Message.Contains("charset"))
            {
                // ✅ Tratamento específico para erro de charset no scraping
                LogService.Warn($"Erro de charset ao processar {url}. Tentando fallback...");

                string domain = ExtractDomain(url);
                lock (_failedDomainsLock)
                {
                    if (!_failedDomains.Contains(domain))
                    {
                        _failedDomains.Add(domain);
                    }
                }

                AddOrUpdateGrid(new NewsItem { Url = url, Status = "Erro Charset", ProcessedAt = DateTime.Now });
            }
            catch (Exception ex)
            {
                // ✅ Tratamento para erros genéricos no processamento
                LogService.Error($"Erro ao processar URL {url}", ex);

                // ✅ Rastrear falha genérica por domínio
                string domain = ExtractDomain(url);
                lock (_failedDomainsLock)
                {
                    if (!_failedDomains.Contains(domain))
                    {
                        _failedDomains.Add(domain);
                    }
                }

                AddOrUpdateGrid(new NewsItem { Url = url, Status = "Erro", ProcessedAt = DateTime.Now });
            }
        }

        // ✅ Adicionar este método na classe MainForm se ainda não existir
        private string ExtractDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host.ToLower();
            }
            catch
            {
                // Fallback: extrair domínio manualmente se a URL for malformada
                try
                {
                    var uri = new UriBuilder(url).Host.ToLower();
                    return uri;
                }
                catch
                {
                    return url;
                }
            }
        }

        private void ShowFailedDomainsReport()
        {
            List<string> failedDomainsCopy;
            int successCount, failCount;

            lock (_failedDomainsLock)
            {
                failedDomainsCopy = new List<string>(_failedDomains);
            }

            // ✅ Contar sucessos e falhas no DataGridView
            successCount = dgvResults.Rows.Cast<DataGridViewRow>()
                .Count(r => r.Cells["colStatus"].Value?.ToString() == "Sucesso");
            failCount = dgvResults.Rows.Count - successCount;

            // ✅ Log de estatísticas
            double successRate = dgvResults.Rows.Count > 0
                ? (successCount * 100.0 / dgvResults.Rows.Count)
                : 0;

            LogService.Info($"=== Relatório Final de Processamento ===");
            LogService.Info($"Total de URLs: {dgvResults.Rows.Count}");
            LogService.Info($"✅ Sucesso: {successCount}");
            LogService.Info($"❌ Falha: {failCount}");
            LogService.Info($"📊 Taxa de sucesso: {successRate:F1}%");

            if (failedDomainsCopy.Count > 0)
            {
                LogService.Warn($"=== Domínios com Falha de Leitura ({failedDomainsCopy.Count}) ===");

                string reportMessage = $"O processo foi concluído, mas {failedDomainsCopy.Count} domínio(s) não puderam ser lidos:\n\n";
                reportMessage += string.Join("\n", failedDomainsCopy.Select((d, i) => $"{i + 1}. {d}"));
                reportMessage += $"\n\n📊 Resumo: {successCount} sucesso(s) / {failCount} falha(s) / {successRate:F1}% taxa de sucesso";

                LogService.Warn("Domínios falhos: " + string.Join(", ", failedDomainsCopy));

                MessageBox.Show(reportMessage, "Domínios com Falha de Leitura", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                LogService.Info("🎉 Todos os domínios foram processados com sucesso!");
                MessageBox.Show($"Processamento concluído com sucesso!\n\n{successCount} URL(s) processada(s) com {successRate:F1}% de taxa de sucesso.",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddOrUpdateGrid(NewsItem item)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AddOrUpdateGrid(item)));
                return;
            }

            dgvResults.Rows.Add(
                item.ImpactScore,
                item.Title,
                item.Url,
                item.Category,
                item.ImpactReason,
                item.Status,
                item.ProcessedAt.ToString("g")
            );

            // 🔥 Ordenar imediatamente após inserir
            dgvResults.Sort(dgvResults.Columns["colImpact"],
                System.ComponentModel.ListSortDirection.Descending);
        }

        private void UpdateProgress()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateProgress));
                return;
            }

            if (progressBar.Value < progressBar.Maximum)
            {
                progressBar.Value++;
                lblProgress.Text = $"{progressBar.Value}/{progressBar.Maximum}";
            }
        }

        private void SortByImpact()
        {
            dgvResults.Sort(dgvResults.Columns["colImpact"], System.ComponentModel.ListSortDirection.Descending);
        }

        private void ToggleUI(bool enabled)
        {
            btnStart.Enabled = enabled;
            btnConfig.Enabled = enabled;
            txtUrls.Enabled = enabled;
            nudParallelism.Enabled = enabled;
        }

        private void dgvResults_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // ✅ Verificar se o clique foi na coluna de URL e em uma linha válida
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvResults.Columns["colUrl"].Index)
            {
                string url = dgvResults.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();

                if (!string.IsNullOrEmpty(url))
                {
                    try
                    {
                        // ✅ COPIAR URL PARA ÁREA DE TRANSFERÊNCIA
                        Clipboard.SetText(url);

                        // ✅ FEEDBACK: Log da ação
                        LogService.Info($"URL copiada para clipboard: {url}");

                        // ✅ Referências da linha/célula
                        var row = dgvResults.Rows[e.RowIndex];
                        var cell = row.Cells[e.ColumnIndex];

                        // ✅ Guardar estado original da célula (para restaurar texto e cor do texto)
                        var originalValue = cell.Value;
                        var originalForeColor = cell.Style.ForeColor;

                        // ✅ Guardar estado original da linha (opcional, caso queira restaurar no futuro)
                        // Aqui NÃO vamos restaurar a linha, porque você quer manter a cor laranja.
                        // var originalRowBackColor = row.DefaultCellStyle.BackColor;

                        // ✅ FEEDBACK VISUAL NA CÉLULA (temporário)
                        cell.Value = "✓ Copiado!";
                        cell.Style.ForeColor = Color.Green;

                        // ✅ REMOVER A URL DO ARQUIVO (mas NÃO remove do grid)
                        RemoveUrlFromConfiguredFile(url);

                        // ✅ MARCAR A LINHA COM LARANJA FRACO (permanente)
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 200); // laranja fraco
                        row.DefaultCellStyle.ForeColor = Color.Black;

                        // ✅ Restaurar APENAS a célula (texto/cor do texto) após 1.5 segundos
                        var timer = new System.Windows.Forms.Timer();
                        timer.Interval = 1500;
                        timer.Tick += (s, args) =>
                        {
                            timer.Stop();
                            timer.Dispose();

                            if (this.IsDisposed) return;

                            // Thread-safe: garantir que a atualização da UI seja na thread principal
                            if (this.InvokeRequired)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    cell.Value = originalValue;
                                    cell.Style.ForeColor = originalForeColor;
                                }));
                            }
                            else
                            {
                                cell.Value = originalValue;
                                cell.Style.ForeColor = originalForeColor;
                            }
                        };
                        timer.Start();
                    }
                    catch (Exception ex)
                    {
                        // ✅ Tratamento de erro caso o clipboard não esteja acessível
                        LogService.Error($"Erro ao copiar URL para clipboard: {url}", ex);
                        MessageBox.Show(
                            "Não foi possível copiar o link para a área de transferência.\n\n" +
                            "Dica: Verifique se outro aplicativo não está bloqueando o clipboard.",
                            "Erro ao Copiar",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }
                }
            }
        }

        private void RemoveUrlFromConfiguredFile(string url)
{
    try
    {
        var config = StorageManager.LoadConfig();

        if (string.IsNullOrWhiteSpace(config.NewsFilePath) ||
            !File.Exists(config.NewsFilePath))
            return;

        var lines = File.ReadAllLines(config.NewsFilePath).ToList();

        int removed = lines.RemoveAll(l => l.Trim() == url.Trim());

        if (removed > 0)
        {
            File.WriteAllLines(config.NewsFilePath, lines);
            LogService.Info($"URL removida do arquivo: {url}");
        }
    }
    catch (Exception ex)
    {
        LogService.Error($"Erro ao remover URL do arquivo: {url}", ex);
    }
}

        private void btnOpenReport_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_lastReportPath) ||
                    !File.Exists(_lastReportPath))
                {
                    MessageBox.Show("Nenhum relatório foi gerado ainda.",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _lastReportPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogService.Error("Erro ao abrir relatório", ex);
                MessageBox.Show("Erro ao abrir o relatório.",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnOpenLog_Click(object sender, EventArgs e)
        {
            try
            {
                string path = LogService.GetLogPath();

                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    MessageBox.Show("Log ainda não foi gerado.",
                        "Aviso",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogService.Error("Erro ao abrir log", ex);
            }
        }

    }
}