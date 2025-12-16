namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    public class GeminiOptions
    {
        public string ApiKey { get; set; } = string.Empty;

        // generateContent Modell (für Summary + Extraction)
        public string Model { get; set; } = "models/gemini-2.5-flash";

        // embedContent Modell (für Vektoren)
        public string EmbeddingModel { get; set; } = "models/gemini-embedding-001";

        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    }
}
