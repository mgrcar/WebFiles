using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDirectoryBrowser();

builder.Services.Configure<AuthConfig>(
    builder.Configuration.GetSection("AuthConfig"));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var fileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "pub"));
var requestPath = "";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = requestPath
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = fileProvider,
    RequestPath = requestPath
});

app.UseMiddleware<BasicAuthMiddleware>();

fileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "priv"));
requestPath = "/priv";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = requestPath
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = fileProvider,
    RequestPath = requestPath
});

app.Run();

public class AuthConfig
{
    public string AuthRealm { get; set; }
        = "";
    public string[] Tokens { get; set; }
        = Array.Empty<string>();
}

public class BasicAuthMiddleware
{
    private readonly RequestDelegate next;
    private readonly AuthConfig config;

    public BasicAuthMiddleware(RequestDelegate next, IOptions<AuthConfig> config)
    {
        this.next = next;
        this.config = config.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        string? username = null, password = null;
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers["Authorization"]);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? "");
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            username = credentials[0];
            password = credentials[1];
        }
        catch
        {
        }
        if (username != password || !config.Tokens.Contains(password))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{config.AuthRealm}\""); // ask for credentials
        }
        else
        {
            await next(context);
        }
    }
}