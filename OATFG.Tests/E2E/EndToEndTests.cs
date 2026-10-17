using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using OATFG.CSXUnitGenerator;
using Xunit;

namespace OATFG.Tests.E2E;

/// <summary>
/// End-to-end: stands up a live HTTP server implementing the Pet Store spec, generates the
/// test framework from that spec, injects end-to-end callers that exercise every generated
/// endpoint fixture, then builds and runs the generated solution with `dotnet test`.
/// </summary>
public class EndToEndTests
{
    [Fact]
    public async Task GeneratedFramework_BuildsAndPassesEndToEndTests_AgainstLiveServer()
    {
        int port = GetFreePort();
        string serverUrl = $"http://localhost:{port}";

        var app = TestServer.Create(port);
        await app.StartAsync();
        try
        {
            string root = Path.Combine(Path.GetTempPath(), "oatfg-e2e-" + Guid.NewGuid().ToString("N"));
            try
            {
                string specPath = TestSpecs.WritePetStoreSpec(serverUrl);
                try
                {
                    var transformer = new OpenAPItoCSXTransformer();
                    var api = await transformer.CreateCSXAPIFromOpenAPIDocument(specPath, projectName: "E2E");
                    api.RootFolderName = root;
                    new CSXFileAndFolderGenerator(api).GenerateFrameworkFiles();

                    Assert.Equal(9, api.Endpoints.Count);
                }
                finally
                {
                    TestSpecs.DeleteSpecFile(specPath);
                }

                // Inject callers that hit the live server through the generated fixtures
                string solutionFolder = Path.Combine(root, "E2E");
                string solutionFile = Path.Combine(solutionFolder, "E2E.sln");
                Assert.True(File.Exists(solutionFile), "Generated solution missing");
                File.WriteAllText(
                    Path.Combine(solutionFolder, "Tests", "EndToEndCallers.cs"),
                    CallersSource.Replace("{SERVER}", serverUrl));

                (int exitCode, string output) = await RunDotnetAsync(solutionFolder, "test E2E.sln --nologo", timeoutSeconds: 420);

                Assert.True(exitCode == 0, $"Generated solution test run failed (exit {exitCode}).\n{output}");
                Assert.Contains("Passed!", output);
                Assert.DoesNotContain("error CS", output);

                int passed = ParsePassedCount(output);
                Assert.True(passed >= 10, $"Expected at least 10 passing generated tests, got {passed}.\n{output}");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static int ParsePassedCount(string output)
    {
        Match match = Regex.Match(output, @"Passed:\s+(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetAsync(string workingDirectory, string arguments, int timeoutSeconds)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        process.Start();
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(timeoutSeconds * 1000);
        string stdout = await stdoutTask;
        string stderr = await stderrTask;

        if (!exited)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"dotnet {arguments} timed out after {timeoutSeconds}s.\n{stdout}\n{stderr}");
        }

        return (process.ExitCode, stdout + Environment.NewLine + stderr);
    }

    /// <summary>
    /// Tests injected into the generated solution's test project. They exercise every common
    /// endpoint shape through the generated fixtures (path/query parameters, request bodies,
    /// content and no-content responses, config-driven API host).
    /// </summary>
    private const string CallersSource = """
        using E2E.Core.Client;
        using E2E.Core.Endpoints;
        using Xunit;

        namespace E2E.Tests;

        public class PetStoreEndToEndTests
        {
            private const string ServerUrl = "{SERVER}";

            [Fact]
            public async Task AccessClient_ReadsApiHostFromAppSettings()
            {
                using var client = new AccessClient();
                Assert.Equal(ServerUrl, client.APIHost);
            }

            [Fact]
            public async Task ListUsers_SendsQueryParameter_ReturnsOk()
            {
                var response = await new ListUsers().Request("10");
                Assert.Equal("OK", response.StatusCode);
                Assert.Contains("\"id\"", response.Body);
            }

            [Fact]
            public async Task CreateUser_SendsBody_ReturnsCreated()
            {
                string bodyPath = Path.Combine(Path.GetTempPath(), "oatfg-body-" + Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllText(bodyPath, "{\"name\":\"Ada\"}");
                try
                {
                    var response = await new CreateUser().Request(bodyPath);
                    Assert.Equal("Created", response.StatusCode);
                    Assert.Contains("Ada", response.Body);
                }
                finally
                {
                    File.Delete(bodyPath);
                }
            }

            [Fact]
            public async Task UsersOptions_ReturnsNoContent()
            {
                var response = await new UsersOptions().Request();
                Assert.Equal("NoContent", response.StatusCode);
            }

            [Fact]
            public async Task GetUserById_SendsPathAndQueryParameters_ReturnsOk()
            {
                var response = await new GetUserById().Request("42", "true");
                Assert.Equal("OK", response.StatusCode);
                Assert.Contains("42", response.Body);
            }

            [Fact]
            public async Task GetUserById_UnknownId_ReturnsNotFound()
            {
                var response = await new GetUserById().Request("999", "false");
                Assert.Equal("NotFound", response.StatusCode);
            }

            [Fact]
            public async Task UpdateUser_SendsBody_ReturnsOk()
            {
                string bodyPath = Path.Combine(Path.GetTempPath(), "oatfg-body-" + Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllText(bodyPath, "{\"name\":\"Grace\"}");
                try
                {
                    var response = await new UpdateUser().Request("42", bodyPath);
                    Assert.Equal("OK", response.StatusCode);
                    Assert.Contains("Grace", response.Body);
                }
                finally
                {
                    File.Delete(bodyPath);
                }
            }

            [Fact]
            public async Task PatchUser_SendsBody_ReturnsOk()
            {
                string bodyPath = Path.Combine(Path.GetTempPath(), "oatfg-body-" + Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllText(bodyPath, "{\"name\":\"Linus\"}");
                try
                {
                    var response = await new PatchUser().Request("42", bodyPath);
                    Assert.Equal("OK", response.StatusCode);
                    Assert.Contains("Linus", response.Body);
                }
                finally
                {
                    File.Delete(bodyPath);
                }
            }

            [Fact]
            public async Task DeleteUser_ReturnsNoContent()
            {
                var response = await new DeleteUser().Request("42");
                Assert.Equal("NoContent", response.StatusCode);
            }

            [Fact]
            public async Task ListPets_SendsQueryParameter_ReturnsOk()
            {
                var response = await new ListPets().Request("5");
                Assert.Equal("OK", response.StatusCode);
                Assert.Contains("kind", response.Body);
            }

            [Fact]
            public async Task GetHealth_ReturnsOk()
            {
                var response = await new GetHealth().Request();
                Assert.Equal("OK", response.StatusCode);
            }
        }
        """;
}