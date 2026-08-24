using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.IdentityModel.Tokens;
using NICE.Platform.FileSync.Api;
using NICE.Platform.FileSync.Api.Hubs;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthorization();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.StreamBufferCapacity = 10;
    options.MaximumReceiveMessageSize = 250 * 1024 * 1024;
    options.ClientTimeoutInterval = TimeSpan.FromHours(4); // matches 14400 sec
    options.KeepAliveInterval = TimeSpan.FromSeconds(20);  // frequent ping to avoid proxy drop
});

#region CORS
//configure CORS to allow the client and the hub to run under different domains
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7018") // Your client app URL
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Critical for SignalR
    });
});
#endregion

#region Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        // Local dev: do not require mTLS so desktop client can connect.
        // Non-development: keep mTLS enabled.
        httpsOptions.ClientCertificateMode = builder.Environment.IsDevelopment()
            ? ClientCertificateMode.NoCertificate
            : ClientCertificateMode.RequireCertificate;

        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});
#endregion

#region Inject Services
builder.Services.AddSingleton<TokenRequest>();
builder.Services.AddSingleton<ServerCertificateService>();
builder.Services.AddSingleton<ConnectionTracker>();
builder.Services.AddSingleton<ClientRegistry>();
#endregion

#region Add Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Allow token in query for WebSockets
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.Value?.Contains("v1/hubs/FileSync") == true)
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<ServerCertificateService>((options, certService) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "nice.nass.usda.gov",
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new X509SecurityKey(certService.Certificate)
        };
    });

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<FileSyncHub>("/v1/hubs/FileSync");

app.Run();
