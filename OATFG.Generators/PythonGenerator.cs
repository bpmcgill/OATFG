using System.Text;
using OATFG.CSXUnitGenerator.DataModels;

namespace OATFG.Generators;

/// <summary>
/// Generates a pytest + requests test suite for the API. Each endpoint gets one test that
/// calls the endpoint with sample values and asserts an expected status code.
/// </summary>
public static class PythonGenerator
{
    public static IReadOnlyList<string> Generate(API api, string outputRoot)
    {
        string projectName = GeneratorSupport.SafeProjectName(api);
        string projectDir = Path.Combine(outputRoot, "python", projectName);
        string testsDir = Path.Combine(projectDir, "tests");
        Directory.CreateDirectory(testsDir);

        var files = new List<string>
        {
            Write(Path.Combine(projectDir, "requirements.txt"), "pytest>=8.0\nrequests>=2.31\n"),
            Write(Path.Combine(projectDir, "conftest.py"), Conftest(projectName, api.Server))
        };

        foreach (var endpoint in api.Endpoints)
        {
            files.Add(Write(Path.Combine(testsDir, $"test_{endpoint.OperationId}.py"), TestFile(endpoint)));
        }

        return files;
    }

    private static string Write(string path, string content)
    {
        File.WriteAllText(path, content);
        return path;
    }

    private static string Conftest(string projectName, string server)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\"\"\"Shared fixtures for the generated {projectName} test suite.\"\"\"");
        sb.AppendLine();
        sb.AppendLine("import os");
        sb.AppendLine();
        sb.AppendLine("import pytest");
        sb.AppendLine("import requests");
        sb.AppendLine();
        sb.AppendLine($"DEFAULT_BASE_URL = \"{server}\"");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("@pytest.fixture(scope=\"session\")");
        sb.AppendLine("def base_url() -> str:");
        sb.AppendLine("    return os.environ.get(\"OATFG_BASE_URL\", DEFAULT_BASE_URL)");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("@pytest.fixture(scope=\"session\")");
        sb.AppendLine("def session() -> requests.Session:");
        sb.AppendLine("    return requests.Session()");
        return sb.ToString();
    }

    private static string TestFile(Endpoint endpoint)
    {
        string method = GeneratorSupport.EndpointMethod(endpoint);
        string urlSuffix = GeneratorSupport.UrlSuffix(endpoint);
        var sb = new StringBuilder();

        sb.AppendLine($"\"\"\"Generated test for {endpoint.OperationId}.\"\"\"");
        sb.AppendLine();
        sb.AppendLine("import requests");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine($"def test_{GeneratorSupport.ToSnakeCase(endpoint.OperationId)}(base_url, session):");
        sb.AppendLine($"    url = f\"{{base_url}}{urlSuffix}\"");

        sb.AppendLine($"    response = session.{method}(url{RequestsArguments(endpoint)})");
        sb.AppendLine(StatusAssertion(endpoint));

        return sb.ToString();
    }

    private static string RequestsArguments(Endpoint endpoint)
    {
        if (endpoint.QueryParameters is { Count: > 0 })
        {
            string entries = string.Join(", ", endpoint.QueryParameters.Select(p => $"\"{p.Name}\": \"{GeneratorSupport.SampleValue(p.Type)}\""));
            return $", params={{{entries}}}";
        }
        if (endpoint.RequestBody != null)
        {
            return ", json={}";
        }
        return "";
    }

    private static string StatusAssertion(Endpoint endpoint)
    {
        var codes = GeneratorSupport.StatusCodes(endpoint);
        if (codes.Count == 0)
        {
            return "    assert response.ok";
        }
        string list = string.Join(", ", codes);
        return $"    assert response.status_code in [{list}]";
    }
}