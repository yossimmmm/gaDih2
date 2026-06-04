using TriviaGame.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// רושמים את ה־MVC controllers. כל endpoint ב־API הזה נחשף דרך controller.
builder.Services.AddControllers();

// מפעילים OpenAPI כדי שיהיה אפשר לראות את החוזה של ה־API בזמן פיתוח.
builder.Services.AddOpenApi();

// רושמים HttpClient כדי ששירותים יוכלו לקרוא ל־HTTP APIs חיצוניים כמו Gemini.
builder.Services.AddHttpClient();

// מאפשרים ללקוח ה־MAUI לדבר עם ה־API ממקור אחר בזמן פיתוח.
builder.Services.AddCors();

// שירותי הדומיין מחזיקים את כל חוקי העסק. ה־controllers רק מעבירים אליהם את העבודה.
builder.Services.AddScoped<AuthDomainService>();
builder.Services.AddScoped<RoomsDomainService>();
builder.Services.AddScoped<GameDomainService>();
builder.Services.AddScoped<UsersDomainService>();
builder.Services.AddScoped<AssistantDomainService>();

// בונים את הגדרות ה־SMTP מתוך הקונפיגורציה ומזריקים אותן ל־EmailService.
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return SmtpSettingsFactory.Build(cfg);
});

// EmailService שולח מיילי איפוס סיסמה בעזרת הגדרות ה־SMTP.
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// OpenAPI ממופה רק בפיתוח כדי שב־production לא לחשוף את זה בלי צורך.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// בפרויקט הזה ה־CORS פתוח בכוונה לצורך פיתוח מקומי ובדיקת MAUI.
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

// שכבת אבטחה לכל ה־API:
// כל בקשה ל־/api חייבת לשאת את X-App-Code המתאים, חוץ מ־health check.
app.Use(async (context, next) =>
{
    // מסלולים שאינם API לא עוברים את בדיקת הקוד.
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    // משאירים את /api/health פתוח כדי שבדיקות פריסה והפעלה ימשיכו לעבוד.
    if (context.Request.Path.Equals("/api/health", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    // הקוד המצופה מגיע מהקונפיגורציה, עם ערך ברירת מחדל לפיתוח מקומי.
    var expectedCode = builder.Configuration["Api:AppCode"] ?? "TRIVIA-DEV-123";

    // לקוח ה־MAUI שולח את ה־header הזה בכל קריאה ל־API.
    var providedCode = context.Request.Headers["X-App-Code"].ToString();

    // אם הקוד חסר או שגוי, דוחים את הבקשה לפני שהיא מגיעה ל־controller.
    if (string.IsNullOrWhiteSpace(providedCode) || !string.Equals(providedCode, expectedCode, StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { ok = false, message = "Invalid app code." });
        return;
    }

    await next();
});

// מחברים את ה־controllers ל־HTTP pipeline.
app.MapControllers();

// מפעילים את השרת.
app.Run();
