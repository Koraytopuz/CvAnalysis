using CvAnalysis.Server.Services;
using Microsoft.AspNetCore.Mvc;
using CvAnalysis.Server.Models;

namespace CvAnalysis.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CvAnalysisController : ControllerBase
    {
        private readonly ICvAnalysisService _cvAnalysisService;
        private readonly ITextAnalysisService _textAnalysisService;

        public CvAnalysisController(ICvAnalysisService cvAnalysisService, ITextAnalysisService textAnalysisService)
        {
            _cvAnalysisService = cvAnalysisService;
            _textAnalysisService = textAnalysisService;
        }
        [Consumes("multipart/form-data")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadCv([FromForm] CvUploadRequest request, [FromForm] string lang = "tr")
        {
            var file = request.File;
            var jobDescription = request.JobDescription;
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Lütfen bir dosya seçin." });
            }

            // Dosya türü kontrolü
            var allowedTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
            if (!allowedTypes.Contains(file.ContentType))
            {
                return BadRequest(new { error = "Sadece PDF ve DOCX dosyaları desteklenmektedir." });
            }

            try
            {
                string extractedText = await _cvAnalysisService.AnalyzeCvAsync(file);

                var analysisReport = await _textAnalysisService.AnalyzeTextAsync(extractedText, jobDescription ?? "", lang);

                return Ok(analysisReport);
            }
            catch (Exception ex)
            {
                // Loglama ekleyin
                Console.WriteLine($"CV analiz hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { error = "CV analiz edilirken bir sunucu hatası oluştu.", details = ex.Message });
            }
        }

        // Test endpoint'i
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "CV Analysis API çalışıyor!", timestamp = DateTime.Now });
        }

        // CORS preflight için OPTIONS endpoint
        [HttpOptions("upload")]
        public IActionResult PreflightUpload()
        {
            return Ok();
        }
    }
}