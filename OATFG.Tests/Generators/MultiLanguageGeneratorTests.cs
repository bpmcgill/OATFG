using OATFG.CSXUnitGenerator;
using OATFG.CSXUnitGenerator.DataModels;
using OATFG.Generators;
using Xunit;

namespace OATFG.Tests.Generators;

public class MultiLanguageGeneratorTests : IDisposable
{
    private const string TestServer = "http://localhost:5000";
    private readonly string _root;
    private readonly string _specPath;
    private readonly API _api;

    public MultiLanguageGeneratorTests()
    {
        _specPath = TestSpecs.WritePetStoreSpec(TestServer);
        _root = Path.Combine(Path.GetTempPath(), "oatfg-gen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);

        var transformer = new OpenAPItoCSXTransformer();
        _api = transformer.CreateCSXAPIFromOpenAPIDocument(_specPath, projectName: "PetStore").Result;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
        TestSpecs.DeleteSpecFile(_specPath);
    }

    private string PythonDir => Path.Combine(_root, "python", "PetStore");
    private string RobotDir => Path.Combine(_root, "robot", "PetStore");
    private string JsDir => Path.Combine(_root, "javascript", "PetStore");

    [Fact]
    public void PythonGenerator_CreatesPytestSuite()
    {
        var files = PythonGenerator.Generate(_api, _root);

        Assert.Contains(Path.Combine(PythonDir, "requirements.txt"), files);
        Assert.Contains(Path.Combine(PythonDir, "conftest.py"), files);

        string conftest = File.ReadAllText(Path.Combine(PythonDir, "conftest.py"));
        Assert.Contains($"DEFAULT_BASE_URL = \"{TestServer}\"", conftest);
        Assert.Contains("def base_url() -> str:", conftest);

        Assert.Contains(Path.Combine(PythonDir, "tests", "test_getUserById.py"), files);
        Assert.Contains(Path.Combine(PythonDir, "tests", "test_createUser.py"), files);
        Assert.Contains(Path.Combine(PythonDir, "tests", "test_getHealth.py"), files);
    }

    [Fact]
    public void PythonGenerator_EndpointFile_WithPathAndQuery()
    {
        PythonGenerator.Generate(_api, _root);
        string content = File.ReadAllText(Path.Combine(PythonDir, "tests", "test_getUserById.py"));

        Assert.Contains("def test_get_user_by_id(base_url, session):", content);
        Assert.Contains("url = f\"{base_url}/users/1?verbose=true\"", content);
        Assert.Contains("response = session.get(url, params={\"verbose\": \"true\"})", content);
        Assert.Contains("assert response.status_code in [200, 404]", content);
    }

    [Fact]
    public void PythonGenerator_EndpointFile_WithBody()
    {
        PythonGenerator.Generate(_api, _root);
        string content = File.ReadAllText(Path.Combine(PythonDir, "tests", "test_createUser.py"));

        Assert.Contains("response = session.post(url, json={})", content);
        Assert.Contains("assert response.status_code in [201, 400]", content);
    }

    [Fact]
    public void RobotGenerator_CreatesRobotSuite()
    {
        var files = RobotGenerator.Generate(_api, _root);

        Assert.Contains(Path.Combine(RobotDir, "tests", "getUserById.robot"), files);
        Assert.Contains(Path.Combine(RobotDir, "tests", "usersOptions.robot"), files);

        string content = File.ReadAllText(Path.Combine(RobotDir, "tests", "getUserById.robot"));
        Assert.Contains("Library    RequestsLibrary", content);
        Assert.Contains($"${{BASE_URL}}    {TestServer}", content);
        Assert.Contains("${response}=    GET    ${BASE_URL}/users/1?verbose=true    expected_status=200", content);

        string options = File.ReadAllText(Path.Combine(RobotDir, "tests", "usersOptions.robot"));
        Assert.Contains("${response}=    OPTIONS    ${BASE_URL}/users    expected_status=204", options);
    }

    [Fact]
    public void JavaScriptGenerator_CreatesNodeTestSuite()
    {
        var files = JavaScriptGenerator.Generate(_api, _root);

        Assert.Contains(Path.Combine(JsDir, "package.json"), files);
        Assert.Contains(Path.Combine(JsDir, "tests", "getUserById.test.mjs"), files);

        string package = File.ReadAllText(Path.Combine(JsDir, "package.json"));
        Assert.Contains("\"petstore-tests\"", package);
        Assert.Contains("\"test\": \"node --test\"", package);
    }

    [Fact]
    public void JavaScriptGenerator_EndpointFile_WithPathAndQuery()
    {
        JavaScriptGenerator.Generate(_api, _root);
        string content = File.ReadAllText(Path.Combine(JsDir, "tests", "getUserById.test.mjs"));

        Assert.Contains("const baseUrl = process.env.OATFG_BASE_URL ?? 'http://localhost:5000';", content);
        Assert.Contains("`${baseUrl}/users/1?verbose=true`", content);
        Assert.Contains("fetch(url, {})", content);
        Assert.Contains("assert.ok([200, 404].includes(response.status)", content);
    }

    [Fact]
    public void JavaScriptGenerator_EndpointFile_WithBodyAndDelete()
    {
        JavaScriptGenerator.Generate(_api, _root);

        string create = File.ReadAllText(Path.Combine(JsDir, "tests", "createUser.test.mjs"));
        Assert.Contains("fetch(url, { method: 'POST', body: '{}', headers: { 'content-type': 'application/json' } })", create);

        string del = File.ReadAllText(Path.Combine(JsDir, "tests", "deleteUser.test.mjs"));
        Assert.Contains("fetch(url, { method: 'DELETE' })", del);
        Assert.Contains("assert.ok([204].includes(response.status)", del);
    }
}