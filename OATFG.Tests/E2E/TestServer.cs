using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace OATFG.Tests.E2E;

/// <summary>
/// A minimal live server implementing the Pet Store spec used by the end-to-end tests.
/// Responses mirror what the OpenAPI document declares for each endpoint.
/// </summary>
public static class TestServer
{
    /// <summary>
    /// Creates the server bound to an OS-assigned port (port 0). Call
    /// <see cref="GetAddress"/> after StartAsync to learn the actual address;
    /// binding directly on port 0 avoids the TOCTOU probe-socket race of
    /// "reserve a free port, close it, then rebind".
    /// </summary>
    public static WebApplication Create()
    {
        var app = WebApplication.Create();
        app.Urls.Add("http://127.0.0.1:0");

        app.MapGet("/users", () => Results.Json(new[] { new { id = 1, name = "Ada" } }));
        app.MapPost("/users", async (HttpContext context) =>
        {
            string? name = await ReadJsonStringAsync(context, "name");
            return Results.Json(new { id = 1, name }, statusCode: 201);
        });
        app.MapMethods("/users", new[] { "OPTIONS" }, () => Results.NoContent());
        app.MapGet("/users/{id:int}", (int id) =>
            id == 999
                ? Results.Json(new { message = "not found" }, statusCode: 404)
                : Results.Json(new { id, name = $"User {id}" }));
        app.MapPut("/users/{id:int}", async (int id, HttpContext context) =>
        {
            string? name = await ReadJsonStringAsync(context, "name");
            return Results.Json(new { id, name });
        });
        app.MapPatch("/users/{id:int}", async (int id, HttpContext context) =>
        {
            string? name = await ReadJsonStringAsync(context, "name");
            return Results.Json(new { id, name });
        });
        app.MapDelete("/users/{id:int}", () => Results.NoContent());
        app.MapGet("/pets", () => Results.Json(new[] { new { id = 1, kind = "dog" } }));
        app.MapGet("/health", () => Results.Json(new { status = "ok" }));

        return app;
    }

    private static async Task<string?> ReadJsonStringAsync(HttpContext context, string property)
    {
        using var document = await System.Text.Json.JsonDocument.ParseAsync(context.Request.Body);
        return document.RootElement.TryGetProperty(property, out System.Text.Json.JsonElement value)
            ? value.GetString()
            : null;
    }

    /// <summary>Actual bound address (host:port) once the app has started.</summary>
    public static string GetAddress(WebApplication app)
    {
        var feature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        return feature?.Addresses.First()
            ?? throw new InvalidOperationException("Server address unavailable after start.");
    }
}