using CvAnalysis.Server.Models;
using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Configuration;

namespace CvAnalysis.Server.Services
{
    public class TextAnalysisService : ITextAnalysisService
    {
        private readonly OpenAIClient _openAiClient;
        private readonly string _openAiDeployment;

        public TextAnalysisService(IConfiguration configuration)
        {
            var endpoint = configuration["AzureAi:OpenAiEndpoint"];
            var key = configuration["AzureAi:OpenAiKey"];
            _openAiDeployment = configuration["AzureAi:OpenAiDeployment"] ?? "gpt-35-turbo";
            _openAiClient = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
        }

        public async Task<AnalysisReport> AnalyzeTextAsync(string cvText, string jobDescription, string lang = "tr")
        {
            var report = new AnalysisReport { ExtractedCvText = cvText };
            if (string.IsNullOrWhiteSpace(cvText))
            {
                report.Suggestions.Add(lang == "en" ? "No text could be read from the CV or the CV is empty." : "CV'den metin okunamadı veya CV boş.");
                return report;
            }

            List<string> targetKeywords = string.IsNullOrWhiteSpace(jobDescription)
                ? new List<string> { "C#", ".NET", "React", "SQL", "Azure" }
                : ExtractKeywordsFromJobDescription(jobDescription);

            var foundKeywords = new List<string>();
            var lowerCvText = cvText.ToLowerInvariant();

            foreach (var keyword in targetKeywords)
            {
                if (Regex.IsMatch(lowerCvText, $@"\b{Regex.Escape(keyword.ToLowerInvariant())}\b"))
                {
                    foundKeywords.Add(keyword);
                }
            }

            report.FoundKeywords = foundKeywords;
            report.MissingKeywords = targetKeywords.Except(foundKeywords).ToList();

            if (targetKeywords.Any())
            {
                report.AtsScore = Math.Round((double)foundKeywords.Count / targetKeywords.Count * 100);
            }

            if (report.AtsScore < 50)
            {
                if(report.MissingKeywords.Any())
                    report.Suggestions.Add(lang == "en"
                        ? $"Consider adding these keywords to improve your ATS compatibility: {string.Join(", ", report.MissingKeywords)}"
                        : $"ATS uyumluluğunuzu artırmak için şu anahtar kelimeleri eklemeyi düşünün: {string.Join(", ", report.MissingKeywords)}");
            }
            else
            {
                report.PositiveFeedback.Add(lang == "en"
                    ? "You have successfully included important keywords from the job description in your CV!"
                    : "İş ilanıyla ilgili önemli anahtar kelimeleri başarıyla CV'nize eklemişsiniz!");
            }

            // Dinamik öneri için OpenAI prompt
            string prompt = lang == "en"
                ? $@"Below is a resume (CV) text and a job description. Generate 5 personalized, bullet-pointed suggestions in English to improve the CV's ATS compatibility and overall quality. Give advice on missing keywords, format, content, language, detail, development, and general job application success. Only output the suggestions as a list, no explanations.

CV text:
{cvText}

Job description:
{jobDescription}
"
                : $@"Aşağıda bir özgeçmiş (CV) metni ve iş ilanı metni verilmiştir. CV'nin ATS uyumluluğunu ve genel kalitesini artırmak için kişiye özel, Türkçe ve maddeler halinde 5 öneri/tavsiye üret. Eksik anahtar kelimeleri, format, içerik, dil, detay, gelişim ve genel iş başvurusu başarısı açısından öneriler ver. Sadece öneri maddelerini üret, açıklama ekleme.

CV metni:
{cvText}

İş ilanı metni:
{jobDescription}
";
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages = { new ChatMessage(ChatRole.System, prompt) },
                MaxTokens = 512,
                Temperature = 0.7f
            };
            var response = await _openAiClient.GetChatCompletionsAsync(_openAiDeployment, chatCompletionsOptions);
            var openAiResult = response.Value.Choices.FirstOrDefault()?.Message.Content ?? "";
            // Satırlara böl ve boşları at
            report.ExtraAdvice = openAiResult.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            // OpenAI'dan gelen önerileri Suggestions'a da ekle
            report.Suggestions.AddRange(report.ExtraAdvice);

            // ATS puanını yükseltmek için temel maddeler (statik, istersen OpenAI ile de üretebiliriz)
            report.AtsImprovementTips = lang == "en"
                ? new List<string>
                {
                    "Add keywords from the job description to your CV.",
                    "Use a simple and clean format, avoid tables and shapes.",
                    "Standardize section titles (Education, Experience, Skills, etc.).",
                    "Save as PDF or DOCX format.",
                    "Be concise, avoid unnecessarily long sentences.",
                    "Pay attention to spelling and grammar.",
                    "Include your contact information completely.",
                    "Avoid unnecessary personal information (ID, marital status, religion, etc.).",
                    "Specify dates and positions for each job experience.",
                    "List education and certificates in chronological order."
                }
                : new List<string>
                {
                    "İş ilanındaki anahtar kelimeleri CV'nize ekleyin.",
                    "Sade ve düz bir format kullanın, tablo ve şekillerden kaçının.",
                    "Başlıkları standartlaştırın (Eğitim, Deneyim, Yetenekler vb.).",
                    "PDF veya DOCX formatında kaydedin.",
                    "Kısa ve öz yazın, gereksiz uzun cümlelerden kaçının.",
                    "İmla ve dil bilgisine dikkat edin.",
                    "İletişim bilgilerinizi eksiksiz yazın.",
                    "Gereksiz kişisel bilgilerden kaçının (TC kimlik, medeni durum, din vb.).",
                    "Her iş deneyimi için tarih ve pozisyon belirtin.",
                    "Eğitim ve sertifikaları kronolojik sırayla yazın."
                };

            return report;
        }

        // Eski sync fonksiyon (artık kullanılmıyor)
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
                if (Regex.IsMatch(jobDescription, $@"\b{Regex.Escape(skill)}\b", RegexOptions.IgnoreCase))
                {
                    foundSkills.Add(skill);
                }
            }
            return foundSkills.Any() ? foundSkills.ToList() : new List<string> { "C#", ".NET", "React", "SQL", "Azure" };
        }
    }
}