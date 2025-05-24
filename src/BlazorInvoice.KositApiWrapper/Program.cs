using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5 * 1024 * 1024;
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback;
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,                    // 1 request...
            Window = TimeSpan.FromSeconds(10), // ...every 10 seconds
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0                     // no queuing
        });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var kositUri = builder.Environment.IsDevelopment() ? new Uri("http://localhost:8080")
    : new Uri("http://validator:9090");
builder.Services.AddHttpClient("KositValidator", client =>
{
    client.BaseAddress = kositUri;
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
});

// Add services to the container.

var app = builder.Build();

app.MapPost("/validate", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var xmlText = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(xmlText))
            return Results.BadRequest("Request body must contain XML.");

        using var scope = app.Services.CreateAsyncScope();
        var clientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        using var client = clientFactory.CreateClient("KositValidator");

        using var content = new StringContent(xmlText, Encoding.UTF8, "application/xml");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

        var response = await client.PostAsync(null as Uri, content);
        var result = await response.Content.ReadAsStringAsync();
        ArgumentNullException.ThrowIfNull(result);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Configure the HTTP request pipeline.
app.UseCors();
//app.UseHttpsRedirection();
app.UseRateLimiter();
app.Run();


