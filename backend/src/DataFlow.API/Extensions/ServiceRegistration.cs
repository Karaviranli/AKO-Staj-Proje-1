using System.Text;
using DataFlow.Business.Abstract;
using DataFlow.Business.Concrete.Parsers;
using DataFlow.Business.Concrete.Rules;
using DataFlow.Business.Concrete.Services;
using DataFlow.Business.Factories;
using DataFlow.DataAccess.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace DataFlow.API.Extensions;

/// <summary>
/// Program.cs'i sade tutmak için tüm servis kayıtları burada toplanır.
/// </summary>
public static class ServiceRegistration
{
    public const string CorsPolicy = "DataFlowCors";

    public static IServiceCollection AddDataFlowServices(
        this IServiceCollection services, IConfiguration config)
    {
        // --- Veri katmanı ---------------------------------------------------
        // Provider değişimi yalnızca bu satırı ilgilendirir (SQLite -> PostgreSQL).
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("DefaultConnection")
                              ?? "Data Source=dataflow.db"));

        // --- Dosya okuyucular (Factory Pattern) -----------------------------
        services.AddScoped<IFileParser, CsvFileParser>();
        services.AddScoped<IFileParser, ExcelFileParser>();
        services.AddScoped<IFileParser, JsonFileParser>();
        services.AddScoped<IFileParserFactory, FileParserFactory>();

        // --- İş katmanı -----------------------------------------------------
        services.AddScoped<IRuleEngine, RuleEngine>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDataService, DataService>();

        return services;
    }

    public static IServiceCollection AddDataFlowAuth(
        this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key ayarı eksik veya 32 karakterden kısa. appsettings.json dosyasını kontrol edin.");

        services.AddSingleton(jwt);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    // Süresi dolan token anında geçersiz olsun (varsayılan 5 dk tolerans kapatıldı).
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddDataFlowCors(
        this IServiceCollection services, IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:3000" };

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

        return services;
    }

    public static IServiceCollection AddDataFlowSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "DataFlow API",
                Version = "v1",
                Description = "Veri yükleme, kalite analizi ve dinamik kural motoru servisleri."
            });

            // Swagger UI üzerinden "Authorize" butonuyla token girilebilsin.
            var scheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT token'ı doğrudan yapıştırın ('Bearer ' ön eki gerekmez).",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, scheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
        });

        return services;
    }
}
