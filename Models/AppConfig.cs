namespace NewsImpactRanker.WinForms.Models
{

    public class AppConfig
    {
        // --- Configurações do Gemini ---
        public string GeminiApiKey { get; set; }
        public string Model { get; set; } = "gemini-2.5-flash";

        // --- Configurações do Groq ---
        public string GroqApiKey { get; set; }
        public string GroqModel { get; set; } = "llama-3.1-8b-instant";
    }

    //public class AppConfig
    //{
    //    // ✅ Renomeado para ser genérico (suporta Groq, OpenAI, etc.)
    //    public string AiApiKey { get; set; }

    //    // ✅ Modelo agora é para Groq/OpenAI-compatible
    //    // Exemplos Groq: "llama-3.1-70b-versatile", "mixtral-8x7b-32768", "gemma2-9b-it"
    //    public string AiModel { get; set; } = "llama-3.1-70b-versatile";

    //    // ✅ Provider para diferenciar entre serviços (opcional, mas recomendado)
    //    public string AiProvider { get; set; } = "groq"; // "groq", "openai", "gemini"

    //    public string NewsFilePath { get; set; }
    //}
}