using CvAnalysis.Server.Services;
using CvAnalysis.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Entity Framework (veritabanı için)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// CV Analysis Services
builder.Services.AddScoped<ICvAnalysisService, AzureCvAnalysisService>();
builder.Services.AddScoped<ITextAnalysisService, TextAnalysisService>();

// CORS policy (React için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// HTTPS redirection - bu satırı kaldırın veya comment yapın
// app.UseHttpsRedirection();

// CORS'u routing'den önce ekleyin
app.UseCors("AllowAll");

app.UseAuthorization();

// Controller'ları map edin
app.MapControllers();

// Swagger UI'yi her durumda göstermek için
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CV Analysis API V1");
    c.RoutePrefix = string.Empty; // Swagger UI'yi root'ta göster
});

app.Run();