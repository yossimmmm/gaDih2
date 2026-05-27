using TriviaGame.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// רישום Controllers כדי לחשוף API פנימי מלא לאפליקציית המובייל.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddCors();

// שירותים עסקיים משותפים ל-endpoints (האורקסטרציה נשארת בשכבת שירות).
builder.Services.AddScoped<SessionTokenService>();
builder.Services.AddScoped<AuthDomainService>();
builder.Services.AddScoped<RoomsDomainService>();
builder.Services.AddScoped<GameDomainService>();
builder.Services.AddScoped<UsersDomainService>();
builder.Services.AddScoped<AssistantDomainService>();
builder.Services.AddScoped(sp =>
{
    // קריאת הגדרות SMTP מ-config/env כדי לתמוך ב-forgot-password גם ב-API העצמאי.
    var cfg = sp.GetRequiredService<IConfiguration>();
    return SmtpSettingsFactory.Build(cfg);
});
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ל-mobile/emulator צריך CORS פתוח יחסית; בפרודקשן ניתן לצמצם origins.
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

app.UseAuthorization();
app.MapControllers();
app.Run();
