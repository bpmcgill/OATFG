using Newtonsoft.Json.Linq;
using OATFG.Core.DataModels;
using OATFG.CSXUnitGenerator;
using OATFG.CSXUnitGenerator.DataModels;
using Xunit;

namespace OATFG.Tests.Transformer;

public class TransformerTests
{
    private const string TestServer = "http://localhost:5000";

    private static async Task<API> Transform(string specPath, string? projectName = null, string? environment = null, AuthenticationTypes authenticationType = AuthenticationTypes.None)
    {
        var transformer = new OpenAPItoCSXTransformer();
        return await transformer.CreateCSXAPIFromOpenAPIDocument(specPath, environment, projectName, authenticationType);
    }

    [Fact]
    public async Task CreateCSXAPIFromOpenAPIDocument_ParsesAllEndpointsAndDefaults()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");

            Assert.Equal("PetStore", api.ProjectName);
            Assert.Equal("PetStore", api.SolutionFolder);
            Assert.Equal("PetStore", api.Namespace);
            Assert.Equal("PetStoreCore", api.CoreProjectName);
            Assert.Equal("PetStoreTests", api.TestProjectName);
            Assert.Equal(TestServer, api.Server);
            Assert.Equal("Development", api.Environment);
            Assert.Equal(AuthenticationTypes.None, api.AuthenticationType);
            Assert.Equal("CSXUG-Output", api.RootFolderName);

            // 9 operations: listUsers, createUser, usersOptions, getUserById, updateUser, patchUser, deleteUser, listPets, getHealth
            Assert.Equal(9, api.Endpoints.Count);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task CreateCSXAPIFromOpenAPIDocument_UsesTitleWhenProjectNameIsNull()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath);

            Assert.Equal("Pet_Store_API", api.ProjectName);
            Assert.Equal("Pet_Store_API", api.SolutionFolder);
            Assert.Equal("Pet_Store_API", api.Namespace);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task CreateCSXAPIFromOpenAPIDocument_AppliesEnvironmentAndAuthenticationOverrides()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore", environment: "Production", authenticationType: AuthenticationTypes.Bearer);

            Assert.Equal("Production", api.Environment);
            Assert.Equal(AuthenticationTypes.Bearer, api.AuthenticationType);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task CreateCSXAPIFromOpenAPIDocument_FallsBackToLocalhostWhenNoServersDeclared()
    {
        string specPath = TestSpecs.WritePetStoreSpec(null);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");

            Assert.Equal("https://localhost:5000", api.Server);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task GetEndpoint_ParsesPathAndQueryParametersAndResponses()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");
            Endpoint endpoint = api.Endpoints.Single(e => e.OperationId == "getUserById");

            Assert.Equal("/users/{id}", endpoint.EndpointPath);
            Assert.Equal("get", endpoint.Method);
            Assert.Equal("GetUserById", endpoint.GeneratedMethodName);
            Assert.Equal("getUserById", endpoint.GeneratedPrivateFieldName);
            Assert.Equal("Get a user by id", endpoint.Summary);
            Assert.Equal("Users", endpoint.TagName);
            Assert.Null(endpoint.RequestBody);

            PathVariable pathVariable = Assert.Single(endpoint.PathVariables);
            Assert.Equal("id", pathVariable.Name);
            Assert.Equal("Integer", pathVariable.Type);
            Assert.True(pathVariable.IsRequired);

            QueryParameter queryParameter = Assert.Single(endpoint.QueryParameters);
            Assert.Equal("verbose", queryParameter.Name);
            Assert.Equal("Boolean", queryParameter.Type);
            Assert.False(queryParameter.IsRequired);

            Assert.Contains(endpoint.Responses, r => r.StatusCode == 200 && r.Description == "OK" && r.Body != null);
            Assert.Contains(endpoint.Responses, r => r.StatusCode == 404 && r.Description == "Not Found");
            Assert.Null(endpoint.Responses.Single(r => r.StatusCode == 404).Body);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task OptionalQueryParameter_IsParsedAsNotRequired()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");
            Endpoint listUsers = api.Endpoints.Single(e => e.OperationId == "listUsers");

            QueryParameter limit = Assert.Single(listUsers.QueryParameters);
            Assert.Equal("limit", limit.Name);
            Assert.False(limit.IsRequired); // no explicit "required" on the query param
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task PostEndpoint_ResolvesRequestBodySchemaAnd201Response()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");
            Endpoint createUser = api.Endpoints.Single(e => e.OperationId == "createUser");

            Assert.NotNull(createUser.RequestBody);
            Assert.Contains("\"name\"", createUser.RequestBody); // the NewUser $ref was inlined
            // the transformer only resolves bodies for 200 responses by design
            Assert.Contains(createUser.Responses, r => r.StatusCode == 201 && r.Description == "Created" && r.Body == null);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task DeleteEndpoint_HasNoContentResponseWithoutBody()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");
            Endpoint deleteUser = api.Endpoints.Single(e => e.OperationId == "deleteUser");

            Response response = Assert.Single(deleteUser.Responses);
            Assert.Equal(204, response.StatusCode);
            Assert.Null(response.Body);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task OptionsEndpoint_IsSupported()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");
            Endpoint options = api.Endpoints.Single(e => e.OperationId == "usersOptions");

            Assert.Equal("options", options.Method);
            Assert.Equal(204, Assert.Single(options.Responses).StatusCode);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task BodyFiles_OnlyCarryBodyFor200Responses()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");
            Endpoint getUserById = api.Endpoints.Single(e => e.OperationId == "getUserById");

            Assert.NotNull(getUserById.Responses.Single(r => r.StatusCode == 200).Body);
            Assert.Null(getUserById.Responses.Single(r => r.StatusCode == 404).Body);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Theory]
    [InlineData("3.0.0")]
    [InlineData("3.0.1")]
    [InlineData("3.0.2")]
    [InlineData("3.0.3")]
    [InlineData("3.0.4")]
    [InlineData("3.1.0")]
    [InlineData("3.1.1")]
    [InlineData("3.1.2")]
    [InlineData("3.2.0")]
    [InlineData("3.2.1")]
    public async Task ParsesSpecsAcrossAllPublishedVersions(string openapiVersion)
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer, openapiVersion);
        try
        {
            API api = await Transform(specPath, projectName: "PetStore");

            Assert.Equal(9, api.Endpoints.Count);
            Assert.Equal(TestServer, api.Server);

            Endpoint getUserById = api.Endpoints.Single(e => e.OperationId == "getUserById");
            Assert.Equal("/users/{id}", getUserById.EndpointPath);
            Assert.Equal("Integer", Assert.Single(getUserById.PathVariables).Type);
            Assert.Equal("Boolean", Assert.Single(getUserById.QueryParameters).Type);
            Assert.Contains(getUserById.Responses, r => r.StatusCode == 200 && r.Body != null);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task ParsesYamlSpecifications()
    {
        string yaml = """
            openapi: 3.0.1
            info:
              title: Yaml API
              version: 1.0.0
            servers:
              - url: http://localhost:5000
            paths:
              /users/{id}:
                get:
                  operationId: getYamlUser
                  summary: Get a yaml user
                  tags:
                    - Users
                  parameters:
                    - name: id
                      in: path
                      required: true
                      schema:
                        type: integer
                  responses:
                    "200":
                      description: OK
                      content:
                        application/json:
                          schema:
                            $ref: "#/components/schemas/User"
            components:
              schemas:
                User:
                  type: object
                  properties:
                    id:
                      type: integer
            """;

        string directory = Path.Combine(Path.GetTempPath(), "oatfg-spec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string specPath = Path.Combine(directory, "petstore.yaml");
        File.WriteAllText(specPath, yaml);
        try
        {
            API api = await Transform(specPath, projectName: "YamlApi");

            Endpoint endpoint = Assert.Single(api.Endpoints);
            Assert.Equal("getYamlUser", endpoint.OperationId);
            Assert.Equal("/users/{id}", endpoint.EndpointPath);
            Assert.Equal("Integer", Assert.Single(endpoint.PathVariables).Type);
            Assert.Contains(endpoint.Responses, r => r.StatusCode == 200 && r.Body != null);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task TaglessOperation_ParsesAndGeneratesWithoutCrashing()
    {
        string spec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Tagless API", "version": "1.0.0" },
          "servers": [ { "url": "http://localhost:5000" } ],
          "paths": {
            "/ping": {
              "get": {
                "operationId": "ping",
                "summary": "No tags here",
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

        string directory = Path.Combine(Path.GetTempPath(), "oatfg-spec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string specPath = Path.Combine(directory, "tagless.json");
        File.WriteAllText(specPath, spec);
        string outputRoot = Path.Combine(Path.GetTempPath(), "oatfg-gen-" + Guid.NewGuid().ToString("N"));
        try
        {
            API api = await Transform(specPath, projectName: "Tagless");
            Endpoint endpoint = Assert.Single(api.Endpoints);

            Assert.Equal("ping", endpoint.OperationId);
            Assert.Null(endpoint.TagName);

            // generation must survive the tagless endpoint (used to throw here)
            api.RootFolderName = outputRoot;
            new CSXFileAndFolderGenerator(api).GenerateFrameworkFiles();
            Assert.True(File.Exists(Path.Combine(outputRoot, "Tagless", "Core", "Endpoints", "ping.cs")));
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
            if (Directory.Exists(outputRoot))
            {
                Directory.Delete(outputRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DefaultResponseKey_IsSkippedWithoutLosingOtherResponses()
    {
        string spec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "DefaultResponse API", "version": "1.0.0" },
          "servers": [ { "url": "http://localhost:5000" } ],
          "paths": {
            "/things": {
              "get": {
                "operationId": "listThings",
                "tags": [ "Things" ],
                "responses": {
                  "200": { "description": "OK" },
                  "default": { "description": "Unexpected error" }
                }
              }
            }
          }
        }
        """;

        string directory = Path.Combine(Path.GetTempPath(), "oatfg-spec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string specPath = Path.Combine(directory, "default-response.json");
        File.WriteAllText(specPath, spec);
        try
        {
            API api = await Transform(specPath, projectName: "DefaultApi");
            Endpoint endpoint = Assert.Single(api.Endpoints);

            Response response = Assert.Single(endpoint.Responses);
            Assert.Equal(200, response.StatusCode); // "default" was skipped, 200 was kept
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public async Task OnlyDefaultResponseKey_LeavesResponsesEmptyWithoutThrowing()
    {
        string spec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "OnlyDefault API", "version": "1.0.0" },
          "servers": [ { "url": "http://localhost:5000" } ],
          "paths": {
            "/things": {
              "get": {
                "operationId": "getThing",
                "tags": [ "Things" ],
                "responses": { "default": { "description": "Anything" } }
              }
            }
          }
        }
        """;

        string directory = Path.Combine(Path.GetTempPath(), "oatfg-spec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string specPath = Path.Combine(directory, "only-default.json");
        File.WriteAllText(specPath, spec);
        try
        {
            API api = await Transform(specPath, projectName: "DefaultApi");
            Endpoint endpoint = Assert.Single(api.Endpoints);

            Assert.Empty(endpoint.Responses);
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    [Fact]
    public void GetBodySchema_InlinesRefAndReturnsNullForUnknownRef()
    {
        string spec = """
        {
          "components": {
            "schemas": {
              "User": { "type": "object", "properties": { "id": { "type": "integer" } } },
              "Wrapper": { "type": "object", "properties": { "user": { "$ref": "#/components/schemas/User" } } }
            }
          }
        }
        """;
        JObject swagger = JObject.Parse(spec);

        string? userSchema = OpenAPItoCSXTransformer.GetBodySchema("#/components/schemas/User", swagger);
        Assert.NotNull(userSchema);
        Assert.Contains("\"id\"", userSchema);

        // a nested $ref inside the schema is recursively replaced
        string? wrapperSchema = OpenAPItoCSXTransformer.GetBodySchema("#/components/schemas/Wrapper", swagger);
        Assert.NotNull(wrapperSchema);
        Assert.DoesNotContain("$ref", wrapperSchema);

        Assert.Null(OpenAPItoCSXTransformer.GetBodySchema("#/components/schemas/Missing", swagger));
        Assert.Null(OpenAPItoCSXTransformer.GetBodySchema(null, swagger));
    }
}