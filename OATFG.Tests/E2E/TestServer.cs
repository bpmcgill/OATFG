using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace OATFG.Tests.E2E;

/// <summary>
/// A minimal live server implementing the Pet Store spec used by the end-to-end tests.
/// Responses mirror what the OpenAPI document declares for each endpoint.
/// </summary>
public static class TestServer
{
    public static WebApplication Create(int port)
    {
        var app = WebApplication.Create();
        app.Urls.Add($"http://localhost:{port}");

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
}