using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using System.Text;
using Microsoft.AspNetCore.Http; // IFormFile için eklendi
using Microsoft.Extensions.Configuration; // IConfiguration için eklendi

namespace CvAnalysis.Server.Services
{
    public class AzureCvAnalysisService : ICvAnalysisService
    {
        private readonly string _endpoint;
        private readonly string _apiKey;

        public AzureCvAnalysisService(IConfiguration configuration)
        {
            _endpoint = configuration["AzureAi:DocumentIntelligenceEndpoint"] ?? throw new ArgumentNullException(nameof(configuration));
            _apiKey = configuration["AzureAi:DocumentIntelligenceKey"] ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<string> AnalyzeCvAsync(IFormFile cvFile)
        {
            var credential = new AzureKeyCredential(_apiKey);
            var client = new DocumentAnalysisClient(new Uri(_endpoint), credential);

            await using var stream = cvFile.OpenReadStream();
            
            AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-read", stream);
            AnalyzeResult result = operation.Value;

            var cvText = new StringBuilder();
            
            foreach (var page in result.Pages)
            {
                foreach (var line in page.Lines)
                {
                    cvText.AppendLine(line.Content);
                }
            }
            return cvText.ToString();
        }
    }
}