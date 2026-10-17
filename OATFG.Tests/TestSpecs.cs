namespace OATFG.Tests;

/// <summary>
/// A comprehensive OpenAPI v3 specification covering the common endpoint shapes and
/// configuration options exercised by the test suite:
/// GET/POST/PUT/PATCH/DELETE/OPTIONS, path variables, optional query parameters,
/// $ref request bodies, $ref and inline response bodies, multiple tags, and a server URL.
/// </summary>
public static class TestSpecs
{
    public const string PetStoreTitle = "Pet Store API";

    /// <summary>
    /// Returns the Pet Store spec as JSON. Pass <paramref name="server"/> to include a
    /// servers block (the common case); pass null to verify the transformer's fallback.
    /// <paramref name="openapiVersion"/> selects the declared specification version so
    /// every published 3.0.x / 3.1.x / 3.2.x flavor can be exercised.
    /// </summary>
    public static string PetStoreJson(string? server, string openapiVersion = "3.0.1")
    {
        string serverLine = server is null
            ? ""
            : $"  \"servers\": [ {{ \"url\": \"{server}\" }} ],{Environment.NewLine}";

        return $$"""
        {
          "openapi": "{{openapiVersion}}",
          "info": { "title": "Pet Store API", "version": "1.0.0" },
          {{serverLine}}  "paths": {
            "/users": {
              "get": {
                "operationId": "listUsers",
                "summary": "List all users",
                "tags": [ "Users" ],
                "parameters": [ { "name": "limit", "in": "query", "schema": { "type": "integer" } } ],
                "responses": {
                  "200": { "description": "OK", "content": { "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/User" } } } } },
                  "401": { "description": "Unauthorized" }
                }
              },
              "post": {
                "operationId": "createUser",
                "summary": "Create a user",
                "tags": [ "Users" ],
                "requestBody": { "required": true, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/NewUser" } } } },
                "responses": {
                  "201": { "description": "Created", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/User" } } } },
                  "400": { "description": "Bad Request" }
                }
              },
              "options": {
                "operationId": "usersOptions",
                "summary": "CORS preflight",
                "tags": [ "Users" ],
                "responses": { "204": { "description": "No Content" } }
              }
            },
            "/users/{id}": {
              "get": {
                "operationId": "getUserById",
                "summary": "Get a user by id",
                "tags": [ "Users" ],
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "integer" } },
                  { "name": "verbose", "in": "query", "schema": { "type": "boolean" } }
                ],
                "responses": {
                  "200": { "description": "OK", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/User" } } } },
                  "404": { "description": "Not Found", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Error" } } } }
                }
              },
              "put": {
                "operationId": "updateUser",
                "summary": "Update a user",
                "tags": [ "Users" ],
                "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "integer" } } ],
                "requestBody": { "required": true, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/UserUpdate" } } } },
                "responses": {
                  "200": { "description": "OK", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/User" } } } }
                }
              },
              "patch": {
                "operationId": "patchUser",
                "summary": "Partially update a user",
                "tags": [ "Users" ],
                "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "integer" } } ],
                "requestBody": { "required": true, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/UserUpdate" } } } },
                "responses": {
                  "200": { "description": "OK", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/User" } } } }
                }
              },
              "delete": {
                "operationId": "deleteUser",
                "summary": "Delete a user",
                "tags": [ "Users" ],
                "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "integer" } } ],
                "responses": { "204": { "description": "No Content" } }
              }
            },
            "/pets": {
              "get": {
                "operationId": "listPets",
                "summary": "List all pets",
                "tags": [ "Pets" ],
                "parameters": [ { "name": "limit", "in": "query", "schema": { "type": "integer" } } ],
                "responses": {
                  "200": { "description": "OK", "content": { "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Pet" } } } } }
                }
              }
            },
            "/health": {
              "get": {
                "operationId": "getHealth",
                "summary": "Health check",
                "tags": [ "System" ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          },
          "components": {
            "schemas": {
              "User": { "type": "object", "properties": { "id": { "type": "integer" }, "name": { "type": "string" } } },
              "NewUser": { "type": "object", "required": [ "name" ], "properties": { "name": { "type": "string" } } },
              "UserUpdate": { "type": "object", "properties": { "name": { "type": "string" } } },
              "Pet": { "type": "object", "properties": { "id": { "type": "integer" }, "kind": { "type": "string" } } },
              "Error": { "type": "object", "properties": { "message": { "type": "string" } } }
            }
          }
        }
        """;
    }

    /// <summary>
    /// Writes the spec to a unique temp file and returns its path. The parent directory is
    /// unique per call so tests can clean it up safely.
    /// </summary>
    public static string WritePetStoreSpec(string? server, string openapiVersion = "3.0.1")
    {
        string directory = Path.Combine(Path.GetTempPath(), "oatfg-spec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "petstore.json");
        File.WriteAllText(path, PetStoreJson(server, openapiVersion));
        return path;
    }

    /// <summary>Deletes the directory a WritePetStoreSpec result lives in.</summary>
    public static void DeleteSpecFile(string specPath)
    {
        string? directory = Path.GetDirectoryName(specPath);
        if (directory != null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}