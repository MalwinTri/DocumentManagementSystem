using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentManagementSystem.Infrastructure.Services.GenAI
{
    public class GeminiOptions
    {
        public string ApiKey { get; set; } = string.Empty;  // hier key rein
        public string Model { get; set; } = "models/gemini-1.5-flash";
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    }
}
