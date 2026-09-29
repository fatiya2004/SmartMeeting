using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartMeeting.Api.AI;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.Email;
using SmartMeeting.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chaîne de connexion PostgreSQL manquante.");

var jwtSettings = new JwtSettings
{
    Secret = Environment.GetEnvironmentVariable("JWT_SECRET")
             ?? builder.Configuration["Jwt:Secret"]
             ?? throw new InvalidOperationException("JWT_SECRET manquant."),
    Issuer = builder.Configuration["Jwt:Issuer"] ?? "SmartMeetingApi",
    Audience = builder.Configuration["Jwt:Audience"] ?? "SmartMeetingClient",
    ExpiresInHours = int.TryParse(builder.Configuration["Jwt:ExpiresInHours"], out var hours) ? hours : 8
};

var emailSettings = new EmailSettings
{
    Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? builder.Configuration["Email:Host"] ?? "mailhog",
    Port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? builder.Configuration["Email:Port"], out var port) ? port : 1025,
    UseSsl = bool.TryParse(Environment.GetEnvironmentVariable("SMTP_USE_SSL") ?? builder.Configuration["Email:UseSsl"], out var ssl) && ssl,
    UserName = Environment.GetEnvironmentVariable("SMTP_USER") ?? builder.Configuration["Email:UserName"],
    Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? builder.Configuration["Email:Password"],
    FromName = builder.Configuration["Email:FromName"] ?? "SmartMeeting CHU",
    FromAddress = Environment.GetEnvironmentVariable("SMTP_FROM") ?? builder.Configuration["Email:FromAddress"] ?? "no-reply@chu-demo.ma"
};

var llmSettings = new LlmSettings
{
    BaseUrl = Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? builder.Configuration["Llm:BaseUrl"] ?? "https://api.openai.com/v1",
    ApiKey = Environment.GetEnvironmentVariable("LLM_API_KEY") ?? builder.Configuration["Llm:ApiKey"] ?? string.Empty,
    Model = Environment.GetEnvironmentVariable("LLM_MODEL") ?? builder.Configuration["Llm:Model"] ?? "gpt-4o-mini"
};

var allowedOrigins = (Environment.GetEnvironmentVariable("CORS_ORIGINS")
                      ?? "http://localhost:4200").Split(',', StringSplitOptions.RemoveEmptyEntries);

// ---------- Services ----------
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<IUnavailabilityService, UnavailabilityService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// ---------- Intelligence artificielle ----------
builder.Services.AddSingleton(llmSettings);
builder.Services.AddHttpClient<ILlmClient, LlmClient>();
builder.Services.AddScoped<IAiToolExecutor, AiToolExecutor>();
builder.Services.AddScoped<IAiAssistantService, AiAssistantService>();

// ---------- Rappels automatiques ----------
builder.Services.AddHostedService<ReminderBackgroundService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartMeeting CHU API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Saisir uniquement le token JWT."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------- Migration + données de démonstration ----------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(context);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AngularApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
