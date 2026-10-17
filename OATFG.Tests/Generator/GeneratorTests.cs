using OATFG.CSXUnitGenerator;
using OATFG.CSXUnitGenerator.DataModels;
using Xunit;

namespace OATFG.Tests.Generator;

public class GeneratorTests : IDisposable
{
    private const string TestServer = "http://localhost:5000";
    private readonly string _root;
    private readonly API _api;
    private readonly string _specPath;

    public GeneratorTests()
    {
        _specPath = TestSpecs.WritePetStoreSpec(TestServer);
        _root = Path.Combine(Path.GetTempPath(), "oatfg-gen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        // Transformer still needs the spec file; the generator writes under the overridden root
        var transformer = new OpenAPItoCSXTransformer();
        _api = transformer.CreateCSXAPIFromOpenAPIDocument(_specPath, projectName: "PetStore").Result;
        _api.RootFolderName = _root;

        new CSXFileAndFolderGenerator(_api).GenerateFrameworkFiles();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
        TestSpecs.DeleteSpecFile(_specPath);
    }

    private string SolutionPath => Path.Combine(_root, "PetStore");
    private string CoreFolder => Path.Combine(SolutionPath, "Core");
    private string TestsFolder => Path.Combine(SolutionPath, "Tests");

    private static string Read(string path) => File.ReadAllText(path);

    [Fact]
    public void GenerateFrameworkFiles_CreatesSolutionProjectAndFolderStructure()
    {
        Assert.True(File.Exists(Path.Combine(SolutionPath, "PetStore.sln")));
        Assert.True(File.Exists(Path.Combine(CoreFolder, "PetStoreCore.csproj")));
        Assert.True(File.Exists(Path.Combine(TestsFolder, "PetStoreTests.csproj")));
        Assert.True(File.Exists(Path.Combine(TestsFolder, "appsettings.json")));
        Assert.True(File.Exists(Path.Combine(CoreFolder, "Client", "AccessClient.cs")));
        Assert.True(File.Exists(Path.Combine(CoreFolder, "Data", "Responses.cs")));
        Assert.True(File.Exists(Path.Combine(CoreFolder, "Endpoints", "EndpointBase.cs")));
        Assert.True(Directory.Exists(Path.Combine(TestsFolder, "TestData", "RequestBodies")));

        foreach (string operationId in new[] { "listUsers", "createUser", "usersOptions", "getUserById", "updateUser", "patchUser", "deleteUser", "listPets", "getHealth" })
        {
            Assert.True(File.Exists(Path.Combine(CoreFolder, "Endpoints", $"{operationId}.cs")), $"{operationId}.cs missing");
        }
    }

    [Fact]
    public void EndpointFile_GetWithPathAndQueryParameters()
    {
        string content = Read(Path.Combine(CoreFolder, "Endpoints", "getUserById.cs"));

        Assert.Contains("public class GetUserById : EndpointBase", content);
        Assert.Contains("private static string getUserById(string id, string verbose)", content);
        Assert.Contains("=> $\"/users/{id}?verbose={verbose}\";", content);
        Assert.Contains("public async Task<APIResponse> Request(string id, string verbose)", content);
        Assert.Contains("=> await CreateRequest(HttpMethod.Get, getUserById(id, verbose: verbose));", content);
    }

    [Fact]
    public void EndpointFile_PostWithBody()
    {
        string content = Read(Path.Combine(CoreFolder, "Endpoints", "createUser.cs"));

        Assert.Contains("public class CreateUser : EndpointBase", content);
        Assert.Contains("public async Task<APIResponse> Request(string? pathToBody = null)", content);
        Assert.Contains("pathToBody ??= $\"./Tests/TestData/RequestBodies/CreateUser.json\";", content);
        Assert.Contains("return await CreateRequestWithBody(HttpMethod.Post, createUser(), pathToBody);", content);
    }

    [Fact]
    public void EndpointFile_PutWithPathVariableAndBody()
    {
        string content = Read(Path.Combine(CoreFolder, "Endpoints", "updateUser.cs"));

        Assert.Contains("public async Task<APIResponse> Request(string id, string? pathToBody = null)", content);
        Assert.Contains("return await CreateRequestWithBody(HttpMethod.Put, updateUser(id), pathToBody);", content);
    }

    [Fact]
    public void EndpointFile_DeleteAndOptions()
    {
        string deleteContent = Read(Path.Combine(CoreFolder, "Endpoints", "deleteUser.cs"));
        Assert.Contains("=> await CreateRequest(HttpMethod.Delete, deleteUser(id));", deleteContent);

        string optionsContent = Read(Path.Combine(CoreFolder, "Endpoints", "usersOptions.cs"));
        Assert.Contains("=> await CreateRequest(HttpMethod.Options, usersOptions());", optionsContent);
    }

    [Fact]
    public void EndpointBase_HasWorkingRequestPipeline()
    {
        string content = Read(Path.Combine(CoreFolder, "Endpoints", "EndpointBase.cs"));

        Assert.Contains("public class EndpointBase : IDisposable", content);
        Assert.Contains("public virtual async Task<APIResponse> APIRequest(HttpRequestMessage httpRequestMessage)", content);
        Assert.Contains("await _accessClient.SendAsync(httpRequestMessage)", content);
        Assert.Contains("new APIResponse(responseCode, body)", content);
        Assert.Contains("new Uri(_accessClient.APIHost + endpointPath)", content);
        Assert.Contains("public virtual async Task<APIResponse> CreateRequestWithBody(HttpMethod method, string endpointPath, string pathToBody)", content);
        Assert.Contains("_accessClient?.Dispose();", content);
        Assert.Contains("public void Log(string message) => _log?.WriteLine(message);", content);
    }

    [Fact]
    public void AccessClient_IsConfigDrivenAndSendsRequests()
    {
        string content = Read(Path.Combine(CoreFolder, "Client", "AccessClient.cs"));

        Assert.Contains("public class AccessClient : IDisposable", content);
        Assert.Contains("public string APIHost { get; }", content);
        Assert.Contains($"APIHost = _config[\"APIHost\"] ?? \"{TestServer}\";", content);
        Assert.Contains("public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)", content);
    }

    [Fact]
    public void ResponsesModel_IsGeneratedInDataNamespace()
    {
        string content = Read(Path.Combine(CoreFolder, "Data", "Responses.cs"));

        Assert.Contains("namespace PetStore.Core.Data", content);
        Assert.Contains("public class APIResponse", content);
        Assert.Contains("public string StatusCode { get; }", content);
        Assert.Contains("public string Body { get; }", content);
    }

    [Fact]
    public void CoreProject_TargetsNet10AndIsNotATestProject()
    {
        string content = Read(Path.Combine(CoreFolder, "PetStoreCore.csproj"));

        Assert.Contains("<TargetFramework>net10.0</TargetFramework>", content);
        Assert.Contains("<IsTestProject>false</IsTestProject>", content);
        Assert.Contains("Microsoft.Extensions.Configuration.Json\" Version=\"10.0.13\"", content);
        Assert.Contains("xunit\" Version=\"2.9.3\"", content);
    }

    [Fact]
    public void TestProject_IsAnXunitTestProjectReferencingCore()
    {
        string content = Read(Path.Combine(TestsFolder, "PetStoreTests.csproj"));

        Assert.Contains("<IsTestProject>true</IsTestProject>", content);
        Assert.Contains("Microsoft.NET.Test.Sdk\" Version=\"18.11.0\"", content);
        Assert.Contains("xunit.runner.visualstudio\" Version=\"4.0.1\"", content);
        Assert.Contains("<ProjectReference Include=\"..\\Core\\PetStoreCore.csproj\" />", content);
        Assert.Contains("<None Update=\"appsettings.json\">", content);
    }

    [Fact]
    public void AppSettings_IsSeededWithApiHost()
    {
        string content = Read(Path.Combine(TestsFolder, "appsettings.json"));

        Assert.Contains($"\"APIHost\": \"{TestServer}\"", content);
    }

    [Fact]
    public void SolutionFile_ReferencesBothProjects()
    {
        string content = Read(Path.Combine(SolutionPath, "PetStore.sln"));

        Assert.Contains("\"PetStoreCore\", \"Core\\PetStoreCore.csproj\"", content);
        Assert.Contains("\"PetStoreTests\", \"Tests\\PetStoreTests.csproj\"", content);
        Assert.Contains("Debug|Any CPU", content);
        Assert.Contains("Release|Any CPU", content);
    }
}