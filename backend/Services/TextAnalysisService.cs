using CvAnalysis.Server.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.ClientModel;
using OpenAI;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using Azure.Identity;

namespace CvAnalysis.Server.Services
{
    public class TextAnalysisService : ITextAnalysisService
    {
        private readonly ChatClient _chatClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TextAnalysisService> _logger;

        public TextAnalysisService(IConfiguration configuration, ILogger<TextAnalysisService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            try
            {
                var endpoint = configuration["AzureAi:OpenAiEndpoint"];
                var key = configuration["AzureAi:OpenAiKey"];
                var deployment = configuration["AzureAi:OpenAiDeployment"];

                _logger.LogInformation($"OpenAI servisi başlatılıyor. Endpoint: {endpoint?.Substring(0, Math.Min(50, endpoint?.Length ?? 0))}...");

                if (string.IsNullOrWhiteSpace(endpoint))
                    throw new InvalidOperationException("AzureAi:OpenAiEndpoint appsettings.json'da tanımlı değil!");
                if (string.IsNullOrWhiteSpace(key))
                    throw new InvalidOperationException("AzureAi:OpenAiKey appsettings.json'da tanımlı değil!");
                if (string.IsNullOrWhiteSpace(deployment))
                    throw new InvalidOperationException("AzureAi:OpenAiDeployment appsettings.json'da tanımlı değil!");

                var clientOptions = new OpenAIClientOptions 
                { 
                    Endpoint = new Uri(endpoint) 
                };

                _chatClient = new ChatClient(deployment, new ApiKeyCredential(key), clientOptions);
                
                _logger.LogInformation("OpenAI ChatClient başarıyla başlatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI ChatClient başlatılırken hata oluştu");
                throw;
            }
        }

        public async Task<AnalysisReport> AnalyzeTextAsync(string cvText, string jobDescription, string lang = "tr")
        {
            var report = new AnalysisReport { ExtractedCvText = cvText };
            
            try
            {
                _logger.LogInformation($"Metin analizi başlatılıyor. CV uzunluğu: {cvText.Length}, Dil: {lang}");

                if (string.IsNullOrWhiteSpace(cvText))
                {
                    _logger.LogWarning("CV metni boş");
                    report.Suggestions.Add(lang == "en" ? "No text could be read from the CV or the CV is empty." : "CV'den metin okunamadı veya CV boş.");
                    return report;
                }

                // Environment variable veya appsettings'ten endpoint al
                var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? _configuration["AzureAi:OpenAiEndpoint"];
                if (string.IsNullOrWhiteSpace(endpoint))
                    throw new InvalidOperationException("Azure OpenAI endpoint environment variable veya appsettings'te tanımlı değil!");

                // Entra ID authentication (DefaultAzureCredential)
                var credential = new DefaultAzureCredential();
                var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
                var deploymentName = _configuration["AzureAi:OpenAiDeployment"] ?? "model-router";
                var chatClient = azureClient.GetChatClient(deploymentName);

                string prompt = lang == "en"
                    ? @"Below is a resume (CV) text and a job description.\n1- Score the CV's ATS compatibility between 0-100 and return only the numeric score.\n2- Then, generate 5 personalized, bullet-pointed suggestions in English to improve the CV's ATS compatibility and overall quality.\nRespond in this format:\nScore: <number>\nSuggestions:\n- ...\n- ..."
                    : @"Aşağıda bir özgeçmiş (CV) ve iş ilanı metni verilmiştir.\n1- CV'nin ATS uyumluluğunu 0-100 arasında puanla ve sadece sayısal puanı döndür.\n2- Ayrıca, CV'nin ATS uyumluluğunu ve genel kalitesini artırmak için kişiye özel, Türkçe ve maddeler halinde 5 öneri üret.\nYanıtı şu formatta ver:\nPuan: <sayı>\nÖneriler:\n- ...\n- ...";

                prompt += $"\n\nCV metni:\n{cvText}\n\nİş ilanı metni:\n{jobDescription}";

                _logger.LogInformation("OpenAI API'sine istek gönderiliyor...");

                var messages = new List<ChatMessage> { new UserChatMessage(prompt) };
                var options = new ChatCompletionOptions
                {
                    Temperature = 0.7f,
                    MaxOutputTokenCount = 1024,
                    TopP = 0.95f,
                    FrequencyPenalty = 0.0f,
                    PresencePenalty = 0.0f
                };

                var completion = await chatClient.CompleteChatAsync(messages, options);
                var result = completion.Value.Content[0].Text;

                _logger.LogInformation($"OpenAI API'den yanıt alındı. Yanıt uzunluğu: {result.Length}");

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

                // ATS geliştirme ipuçlarını ekle
                report.AtsImprovementTips = GetAtsImprovementTips(lang);

                _logger.LogInformation($"Analiz tamamlandı. Puan: {puan}, Öneri sayısı: {oneriler.Count}");
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Metin analizi sırasında hata oluştu");
                
                // Hata durumunda varsayılan yanıt döndür
                report.AtsScore = 0;
                report.Suggestions.Add(lang == "en" 
                    ? $"Analysis failed due to technical error: {ex.Message}"
                    : $"Teknik hata nedeniyle analiz başarısız oldu: {ex.Message}");
                
                return report;
            }
        }

        private List<string> GetAtsImprovementTips(string lang)
        {
            return lang == "en"
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
        }

        public AnalysisReport AnalyzeText(string cvText, string jobDescription)
        {
            throw new NotImplementedException("Lütfen AnalyzeTextAsync fonksiyonunu kullanın.");
        }
    }
}