using OATFG.CSXUnitGenerator;
using OATFG.CSXUnitGenerator.DataModels;
using OATFG.Generators;
using Xunit;

namespace OATFG.Tests.Generators;

/// <summary>
/// Integration: every generated file must pass its language's syntax linter. Tests skip when
/// the toolchain is not installed so the suite stays runnable anywhere.
/// </summary>
public class LintIntegrationTests
{
    private const string TestServer = "http://localhost:5000";

    private static API Api()
    {
        string specPath = TestSpecs.WritePetStoreSpec(TestServer);
        try
        {
            var transformer = new OpenAPItoCSXTransformer();
            return transformer.CreateCSXAPIFromOpenAPIDocument(specPath, projectName: "PetStore").Result;
        }
        finally
        {
            TestSpecs.DeleteSpecFile(specPath);
        }
    }

    private static async Task WithGeneratedAsync(LintTarget target, Func<string, IReadOnlyList<string>, Task> assertLinted)
    {
        Skip.IfNot(LintService.IsToolAvailable(target), $"{target} toolchain is not installed; skipping lint integration test");

        API api = Api();
        string root = Path.Combine(Path.GetTempPath(), "oatfg-lint-" + Guid.NewGuid().ToString("N"));
        try
        {
            IReadOnlyList<string> files = target switch
            {
                LintTarget.Python => PythonGenerator.Generate(api, root),
                LintTarget.JavaScript => JavaScriptGenerator.Generate(api, root),
                LintTarget.Robot => RobotGenerator.Generate(api, root),
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
            IReadOnlyList<string> lintable = target switch
            {
                LintTarget.Python => files.Where(f => f.EndsWith(".py")).ToArray(),
                LintTarget.JavaScript => files.Where(f => f.EndsWith(".mjs")).ToArray(),
                LintTarget.Robot => files.Where(f => f.EndsWith(".robot")).ToArray(),
                _ => files
            };

            await assertLinted(root, lintable);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [SkippableFact]
    public async Task PythonSuite_PassesPyCompile()
        => await WithGeneratedAsync(LintTarget.Python, async (_, files) =>
        {
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                LintResult result = await LintService.LintAsync(file, LintTarget.Python);
                Assert.True(result.Passed, $"{file} failed py_compile:\n{result.Output}");
            }
        });

    [SkippableFact]
    public async Task JavaScriptSuite_PassesNodeCheck()
        => await WithGeneratedAsync(LintTarget.JavaScript, async (_, files) =>
        {
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                LintResult result = await LintService.LintAsync(file, LintTarget.JavaScript);
                Assert.True(result.Passed, $"{file} failed node --check:\n{result.Output}");
            }
        });

    [SkippableFact]
    public async Task RobotSuite_PassesRobotLint()
        => await WithGeneratedAsync(LintTarget.Robot, async (_, files) =>
        {
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                LintResult result = await LintService.LintAsync(file, LintTarget.Robot);
                Assert.True(result.Passed, $"{file} failed robot --lint:\n{result.Output}");
            }
        });

    [Fact]
    public void LintService_ReportsMissingTool()
        => Assert.False(LintService.IsToolAvailable((LintTarget)999)); // no such target → nothing runnable
}