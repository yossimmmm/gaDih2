using TriviaGame.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// רושמים controllers. כל endpoint ב-API נחשף דרך controller.
builder.Services.AddControllers();

// מאפשרים לראות את ה-API בזמן פיתוח דרך OpenAPI.
builder.Services.AddOpenApi();

// רושמים HttpClient כדי ששירותים יוכלו לגשת ל-HTTP APIs חיצוניים.
builder.Services.AddHttpClient();

// מאפשרים ללקוח MAUI לדבר עם ה-API ממקור אחר.
builder.Services.AddCors();

// רושמים את שכבות השירות העסקיות.
builder.Services.AddScoped<AuthDomainService>();
builder.Services.AddScoped<RoomsDomainService>();
builder.Services.AddScoped<GameDomainService>();
builder.Services.AddScoped<UsersDomainService>();
builder.Services.AddScoped<AssistantDomainService>();

// בונים הגדרות SMTP מתוך הקונפיגורציה ומזריקים אותן לשירות המיילים.
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return SmtpSettingsFactory.Build(cfg);
});

// שירות המיילים משתמש בהגדרות SMTP כדי לשלוח מייל איפוס סיסמה.
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// OpenAPI מופעל רק בפיתוח כדי שלא ייחשף בפרודקשן בלי צורך.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS פתוח כדי לאפשר תקשורת מקומית ובדיקות מהלקוח.
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

// שכבת אבטחה בסיסית לכל API:
// כל בקשה ל-/api חייבת לשאת X-App-Code נכון, חוץ מ-health check.
app.Use(async (context, next) =>
{
    // נתיבים שאינם API לא עוברים את הבדיקה הזו.
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    // health check נשאר פתוח כדי שאפשר יהיה לבדוק שהשרת חי.
    if (context.Request.Path.Equals("/api/health", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    // הקוד המצופה מגיע מהקונפיגורציה, עם ברירת מחדל לפיתוח מקומי.
    var expectedCode = builder.Configuration["Api:AppCode"] ?? "TRIVIA-DEV-123";

    // הלקוח שולח את הקוד הזה בכל קריאה ל-API.
    var providedCode = context.Request.Headers["X-App-Code"].ToString();

    // אם הקוד חסר או לא נכון, עוצרים כאן ומחזירים 401.
    if (string.IsNullOrWhiteSpace(providedCode) || !string.Equals(providedCode, expectedCode, StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { ok = false, message = "Invalid app code." });
        return;
    }

    await next();
});

// מחברים את כל ה-controllers לצינור הבקשות של השרת.
app.MapControllers();

// מפעילים את השרת.
app.Run();
