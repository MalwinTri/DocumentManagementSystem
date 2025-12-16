namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    public class GeminiOptions
    {
        public string ApiKey { get; set; } = string.Empty;  
        public string Model { get; set; } = "models/gemini-2.5-flash";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    }
}
