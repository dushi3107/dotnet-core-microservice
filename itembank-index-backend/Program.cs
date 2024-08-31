using System.Text.Json;
using System.Threading.RateLimiting;
using itembank_index_backend.Middlewares;
using itembank_index_backend.Models.Settings;
using RepositoryElastic = itembank_index_backend.Repositories.Elasticsearch;
using RepositorySql = itembank_index_backend.Repositories.Sql;
using itembank_index_backend.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
string concurrencyPolicy = "Concurrency";
var myOptions = new RateLimitOptions();
builder.Configuration.GetSection(RateLimitOptions.RateLimitSection).Bind(myOptions);
builder.Services.AddRateLimiter(_ => _
    .AddConcurrencyLimiter(policyName: concurrencyPolicy, options =>
    {
        options.PermitLimit = myOptions.PermitLimit;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = myOptions.QueueLimit;
    }));

var appSettings = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettings);
// elastic repo
builder.Services.AddTransient<RepositoryElastic.EmptyRepository>();
builder.Services.AddTransient<RepositoryElastic.ItemRepository>();
// sql repo
builder.Services.AddTransient<RepositorySql.EmptyRepository>();
builder.Services.AddTransient<RepositorySql.ItemRepository>();
builder.Services.AddTransient<RepositorySql.MetadataRepository>();
builder.Services.AddTransient<RepositorySql.CatalogRepository>();
// services
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<MetadataService>();
builder.Services.AddScoped<CatalogService>();

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();
builder.Services.AddCors();

var app = builder.Build();
app.Logger.LogInformation("App settings: " + JsonSerializer.Serialize(
    appSettings.Get<AppSettings>(), new JsonSerializerOptions() { WriteIndented = true }));
app.UseRateLimiter();
static string GetTicks() => (DateTime.Now.Ticks & 0x11111).ToString("00000");
app.MapGet("/", async () =>
{
    await Task.Delay(10);
    return Results.Ok($"Concurrency Limiter {GetTicks()}");
}).RequireRateLimiting(concurrencyPolicy);

app.UsePathBase("/itembank-index-backend");
app.UseRouting();

app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Itembank-Index-Backend API V1"); });
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseAuthorization();
app.MapControllers();

app.UseMiddleware<HttpLoggingMiddleware>();

app.Run();