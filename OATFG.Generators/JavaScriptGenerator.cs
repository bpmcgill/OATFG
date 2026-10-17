using System.Text;
using OATFG.CSXUnitGenerator.DataModels;

namespace OATFG.Generators;

/// <summary>
/// Generates a JavaScript test suite for Node's built-in test runner and global fetch —
/// zero external dependencies. Each endpoint gets one .test.mjs file.
/// </summary>
public static class JavaScriptGenerator
{
    public static IReadOnlyList<string> Generate(API api, string outputRoot)
    {
        string projectName = GeneratorSupport.SafeProjectName(api);
        string projectDir = Path.Combine(outputRoot, "javascript", projectName);
        string testsDir = Path.Combine(projectDir, "tests");
        Directory.CreateDirectory(testsDir);

        var files = new List<string>
        {
            Write(Path.Combine(projectDir, "package.json"), PackageJson(projectName))
        };

        foreach (var endpoint in api.Endpoints)
        {
            files.Add(Write(Path.Combine(testsDir, $"{endpoint.OperationId}.test.mjs"), TestFile(endpoint, api.Server)));
        }
        return files;
    }

    private static string Write(string path, string content)
    {
        File.WriteAllText(path, content);
        return path;
    }

    private static string PackageJson(string projectName) => $$"""
        {
          "name": "{{projectName.ToLowerInvariant()}}-tests",
          "private": true,
          "type": "module",
          "scripts": {
            "test": "node --test"
          }
        }
        """;

    private static string TestFile(Endpoint endpoint, string server)
    {
        string method = GeneratorSupport.EndpointMethod(endpoint).ToUpperInvariant();
        string urlSuffix = GeneratorSupport.UrlSuffix(endpoint);
        var codes = GeneratorSupport.StatusCodes(endpoint);

        string options = method switch
        {
            "GET" => "{}",
            _ when endpoint.RequestBody != null => "{ method: 'POST', body: '{}', headers: { 'content-type': 'application/json' } }",
            _ => $"{{ method: '{method}' }}"
        };

        string assertion = codes.Count > 0
            ? $"assert.ok([{string.Join(", ", codes)}].includes(response.status), `unexpected status ${{response.status}}`);"
            : "assert.ok(response.ok, `unexpected status ${response.status}`);";

        var sb = new StringBuilder();
        sb.AppendLine("import test from 'node:test';");
        sb.AppendLine("import assert from 'node:assert/strict';");
        sb.AppendLine();
        sb.AppendLine($"const baseUrl = process.env.OATFG_BASE_URL ?? '{server}';");
        sb.AppendLine();
        sb.AppendLine($"test('{endpoint.OperationId} returns an expected status', async () => {{");
        sb.AppendLine($"  const url = `${{baseUrl}}{urlSuffix}`;");
        sb.AppendLine($"  const response = await fetch(url, {options});");
        sb.AppendLine($"  {assertion}");
        sb.AppendLine("});");

        return sb.ToString();
    }
}