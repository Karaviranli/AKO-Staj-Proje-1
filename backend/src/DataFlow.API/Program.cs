using DataFlow.API.Extensions;
using DataFlow.API.Middleware;
using DataFlow.API.Seed;
using DataFlow.Business.Common;
using DataFlow.DataAccess.Context;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDataFlowServices(builder.Configuration)
    .AddDataFlowAuth(builder.Configuration)
    .AddDataFlowCors(builder.Configuration)
    .AddDataFlowSwagger();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonDefaults.Options.PropertyNamingPolicy;
        options.JsonSerializerOptions.Encoder = JsonDefaults.Options.Encoder;
    });

// Yükleme boyutu sınırı appsettings üzerinden yönetilir.
var maxMb = builder.Configuration.GetValue("Upload:MaxFileSizeMb", 25);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = (long)maxMb * 1024 * 1024);

var app = builder.Build();

// Veritabanını oluştur/güncelle ve demo verilerini yükle.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (builder.Configuration.GetValue("Seed:Enabled", true))
        await DbSeeder.SeedAsync(db, builder.Configuration);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "DataFlow API v1");
        o.DocumentTitle = "DataFlow API";
    });
}

app.UseCors(ServiceRegistration.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "DataFlow API",
    version = "1.0.0",
    docs = "/swagger"
}));

app.Run();
