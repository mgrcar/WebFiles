using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
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

app.UseMiddleware<BasicAuthMiddleware>();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/"))
    {
        context.Response.Redirect("pub/", permanent: false);
        return;
    }
    await next();
});

var fileProvider = new PhysicalFileProvider(builder.Environment.WebRootPath);
var requestPath = "";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = requestPath,
    ContentTypeProvider = new AllContentTypeProvider()
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

public class AllContentTypeProvider : IContentTypeProvider
{
    private readonly IContentTypeProvider baseProvider
        = new FileExtensionContentTypeProvider();

    public bool TryGetContentType(string subpath, out string contentType)
    {
        if (!baseProvider.TryGetContentType(subpath, out contentType!))
        {
            contentType = "application/octet-stream";
        }
        return true;
    }
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
        if (context.Request.Path.StartsWithSegments("/priv"))
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
        else 
        {
            await next(context);
        }
    }
}