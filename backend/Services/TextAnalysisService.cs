using CvAnalysis.Server.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.ClientModel;
using OpenAI;
using Azure;
using Azure.AI.OpenAI;

namespace CvAnalysis.Server.Services
{
    public class TextAnalysisService : ITextAnalysisService
    {
        private readonly ChatClient _chatClient;
        private readonly IConfiguration _configuration;

        public TextAnalysisService(IConfiguration configuration)
        {
            _configuration = configuration;
            var endpoint = configuration["AzureAi:OpenAiEndpoint"];
            var key = configuration["AzureAi:OpenAiKey"];
            var deployment = configuration["AzureAi:OpenAiDeployment"];

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("Azure OpenAI endpoint appsettings veya environment değişkenlerinde tanımlı değil!");
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Azure OpenAI key appsettings veya environment değişkenlerinde tanımlı değil!");
            if (string.IsNullOrWhiteSpace(deployment))
                throw new InvalidOperationException("Azure OpenAI deployment adı appsettings veya environment değişkenlerinde tanımlı değil!");

            _chatClient = new ChatClient(deployment, new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = new Uri(endpoint) });
        }

        public async Task<AnalysisReport> AnalyzeTextAsync(string cvText, string jobDescription, string lang = "tr")
        {
            var report = new AnalysisReport { ExtractedCvText = cvText };
            if (string.IsNullOrWhiteSpace(cvText))
            {
                report.Suggestions.Add(lang == "en" ? "No text could be read from the CV or the CV is empty." : "CV'den metin okunamadı veya CV boş.");
                return report;
            }

            string prompt = lang == "en"
                ? @"Below is a resume (CV) text and a job description.
1- Score the CV's ATS compatibility between 0-100 and return only the numeric score.
2- Then, generate 5 personalized, bullet-pointed suggestions in English to improve the CV's ATS compatibility and overall quality.
Respond in this format:
Score: <number>
Suggestions:
- ...
- ..."
                : @"Aşağıda bir özgeçmiş (CV) ve iş ilanı metni verilmiştir.
1- CV'nin ATS uyumluluğunu 0-100 arasında puanla ve sadece sayısal puanı döndür.
2- Ayrıca, CV'nin ATS uyumluluğunu ve genel kalitesini artırmak için kişiye özel, Türkçe ve maddeler halinde 5 öneri üret.
Yanıtı şu formatta ver:
Puan: <sayı>
Öneriler:
- ...
- ...";

            prompt += $"\n\nCV metni:\n{cvText}\n\nİş ilanı metni:\n{jobDescription}";

            // Prompt ve mesajlarınızı oluşturun
            var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
            var requestOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1024,
                Temperature = 0.7f,
                TopP = 0.95f,
            };
            // Sadece DI ile gelen _chatClient'ı kullanın!
            var response = await _chatClient.CompleteChatAsync(messages, requestOptions);
            var result = response.Value.Content[0].Text;

            // Yanıtı parse et: puan ve öneriler
            int puan = 0;
            var oneriler = new List<string>();
            var lines = result.Split('\n');
            foreach (var line in lines)
            {
                if (lang == "en" && line.Trim().StartsWith("Score:"))
                {
                    int.TryParse(line.Replace("Score:", "").Trim(), out puan);
                }
                else if (lang != "en" && line.Trim().StartsWith("Puan:"))
                {
                    int.TryParse(line.Replace("Puan:", "").Trim(), out puan);
                }
                else if (line.Trim().StartsWith("-"))
                {
                    oneriler.Add(line.Trim().TrimStart('-').Trim());
                }
            }
            report.AtsScore = puan;
            report.ExtraAdvice = oneriler;
            report.Suggestions.AddRange(oneriler);
            if (puan >= 70)
            {
                report.PositiveFeedback.Add(lang == "en"
                    ? "Your CV's ATS compatibility is high!"
                    : "CV'nizin ATS uyumluluğu yüksek!");
            }
            else
            {
                report.Suggestions.Add(lang == "en"
                    ? "Consider improving your CV based on the suggestions above."
                    : "Yukarıdaki önerilere göre CV'nizi geliştirmeyi düşünün.");
            }
            return report;
        }

        public AnalysisReport AnalyzeText(string cvText, string jobDescription)
        {
            throw new NotImplementedException("Lütfen AnalyzeTextAsync fonksiyonunu kullanın.");
        }

        private List<string> ExtractKeywordsFromJobDescription(string jobDescription)
        {
            var potentialSkills = new List<string>
            {
                "C#", "ASP.NET", ".NET Core", ".NET", "Python", "Java", "Go", "Ruby", "PHP",
                "SQL", "NoSQL", "PostgreSQL", "MySQL", "MongoDB", "Redis", "Elasticsearch",
                "Azure", "AWS", "Google Cloud", "GCP", "Docker", "Kubernetes", "Terraform", "CI/CD",
                "React", "Angular", "Vue", "JavaScript", "TypeScript", "HTML", "CSS", "Node.js", "jQuery",
                "Microservices", "API", "REST", "Agile", "Scrum", "Git", "Jira", "TDD",
                "Machine Learning", "Data Science", "AI", "Artificial Intelligence", "Deep Learning", "NLP"
            };

            var foundSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var skill in potentialSkills)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(jobDescription, $@"\b{System.Text.RegularExpressions.Regex.Escape(skill)}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    foundSkills.Add(skill);
                }
            }
            return foundSkills.Any() ? foundSkills.ToList() : new List<string> { "C#", ".NET", "React", "SQL", "Azure" };
        }
    }
}