namespace NewsImpactRanker.WinForms.Models
{
    public class AppConfig
    {
        // --- Configurações de API ---
        public string GeminiApiKey { get; set; }
        public string Model { get; set; } = "gemini-2.5-flash";

        public string GroqApiKey { get; set; }
        public string GroqModel { get; set; } = "llama-3.1-8b-instant";

        // --- Configurações de Arquivos e Caminhos (V2) ---

        // Caminho do arquivo .txt com a lista de URLs de entrada
        public string NewsFilePath { get; set; }

        // Caminho do arquivo .txt que contém as instruções (Prompt System) para a IA
        public string PromptFilePath { get; set; }

        // Etapa 1.1: Caminho do arquivo onde serão gravadas as URLs científicas detectadas
        public string ScientificNewsFilePath { get; set; }
    }
}