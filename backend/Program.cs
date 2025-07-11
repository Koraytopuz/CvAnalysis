using CvAnalysis.Server.Services;
using CvAnalysis.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Logging konfigürasyonu ekleyin
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICvAnalysisService, AzureCvAnalysisService>();
builder.Services.AddScoped<ITextAnalysisService, TextAnalysisService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel", policy =>
    {
        policy
            .SetIsOriginAllowed(origin => origin != null && origin.EndsWith(".vercel.app"))
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Development ortamında detaylı hata sayfalarını göster
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowVercel");

app.UseAuthorization();

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CV Analysis API V1");
    c.RoutePrefix = string.Empty; 
});

app.Run();