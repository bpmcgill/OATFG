using System.Text;
using OATFG.CSXUnitGenerator.DataModels;

namespace OATFG.Generators;

/// <summary>
/// Generates a Robot Framework (RequestsLibrary) test suite. Each endpoint gets one .robot
/// file with a single test case asserting an expected status code.
/// </summary>
public static class RobotGenerator
{
    public static IReadOnlyList<string> Generate(API api, string outputRoot)
    {
        string projectName = GeneratorSupport.SafeProjectName(api);
        string testsDir = Path.Combine(outputRoot, "robot", projectName, "tests");
        Directory.CreateDirectory(testsDir);

        var files = new List<string>();
        foreach (var endpoint in api.Endpoints)
        {
            files.Add(Write(Path.Combine(testsDir, $"{endpoint.OperationId}.robot"), RobotFile(endpoint, api.Server)));
        }
        return files;
    }

    private static string Write(string path, string content)
    {
        File.WriteAllText(path, content);
        return path;
    }

    private static string RobotFile(Endpoint endpoint, string server)
    {
        string method = GeneratorSupport.EndpointMethod(endpoint).ToUpperInvariant();
        string urlSuffix = GeneratorSupport.UrlSuffix(endpoint);
        var codes = GeneratorSupport.StatusCodes(endpoint);
        string expectedStatus = codes.Count > 0 ? codes.First().ToString() : "anything";

        var sb = new StringBuilder();
        sb.AppendLine("*** Settings ***");
        sb.AppendLine("Library    RequestsLibrary");
        sb.AppendLine();
        sb.AppendLine("*** Variables ***");
        sb.AppendLine($"${{BASE_URL}}    {server}");
        sb.AppendLine();
        sb.AppendLine("*** Test Cases ***");
        sb.AppendLine($"{endpoint.OperationId} Should Return Expected Status");
        sb.AppendLine($"    ${{response}}=    {method}    ${{BASE_URL}}{urlSuffix}    expected_status={expectedStatus}");

        return sb.ToString();
    }
}