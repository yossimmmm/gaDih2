using TriviaGame.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// כאן נבנה ה-API עצמו: זו נקודת ההתחלה שממנה נרשמים כל השירותים והנתיבים.
// אין כאן UI. הקובץ הזה רק מגדיר איך השרת יפעל ואיך הוא יקבל בקשות.
builder.Services.AddControllers();

// OpenAPI נותן תיעוד אוטומטי של ה-endpoints, כדי להבין מה השרת יודע לקבל ולהחזיר.
builder.Services.AddOpenApi();

// רושמים HttpClient כי חלק מהשירותים בצד השרת צריכים לדבר עם שירותים חיצוניים.
builder.Services.AddHttpClient();

// CORS נשאר פתוח כדי שה-MAUI יוכל לדבר עם ה-API גם כשהם רצים כתהליכים נפרדים.
builder.Services.AddCors();

// שירותי הדומיין מחזיקים את הלוגיקה העסקית: API רק מפנה אליהם ואוסף את התשובה.
builder.Services.AddScoped<AuthDomainService>();
builder.Services.AddScoped<RoomsDomainService>();
builder.Services.AddScoped<GameDomainService>();
builder.Services.AddScoped<UsersDomainService>();
builder.Services.AddScoped<AssistantDomainService>();

// SMTP צריך להיבנות מה-configuration, כי איפוס סיסמה עדיין שולח מייל דרך שרת חיצוני.
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return SmtpSettingsFactory.Build(cfg);
});

// השירות ששולח את המייל בפועל מקבל את הגדרות ה-SMTP מה-container.
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// בסביבת פיתוח נחשוף גם את OpenAPI כדי שיהיה קל לראות ולבדוק את כל המסלולים.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS פתוח לכל origin/header/method, כי זה פרויקט מקומי ופשוט ולא מערכת מרובת דומיינים.
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

// זה השער הראשי של האבטחה הפשוטה שלנו:
// כל בקשה ל-/api חייבת לשאת X-App-Code, אחרת היא נעצרת עוד לפני ה-controller.
app.Use(async (context, next) =>
{
    // אם הבקשה בכלל לא ל-API, לא צריך לבדוק app code.
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    // health נשאר פתוח כדי שיהיה אפשר לבדוק שהשירות חי גם בלי כותרת מיוחדת.
    if (context.Request.Path.Equals("/api/health", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    // הקוד המצופה מגיע מה-configuration, עם ברירת מחדל מקומית אם אין הגדרה אחרת.
    var expectedCode = builder.Configuration["Api:AppCode"] ?? "TRIVIA-DEV-123";

    // זה הערך שה-MAUI שולח בכל בקשה.
    var providedCode = context.Request.Headers["X-App-Code"].ToString();

    // אם הקוד חסר או שגוי, מחזירים 401 ולא ממשיכים ל-controller.
    if (string.IsNullOrWhiteSpace(providedCode) || !string.Equals(providedCode, expectedCode, StringComparison.Ordinal))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { ok = false, message = "Invalid app code." });
        return;
    }

    await next();
});

// אחרי שעברנו את הבדיקה, ה-controllers מחוברים למסלולים שלהם.
app.MapControllers();

// מפעילים את השרת בפועל.
app.Run();
