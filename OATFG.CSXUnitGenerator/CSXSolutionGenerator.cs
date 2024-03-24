using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using OATFG.CSXUnitGenerator.DataModels;
using Microsoft.Build.Logging;
using Microsoft.Build;
using System.Xml;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATFG.CSXUnitGenerator
{
    public class CSXSolutionGenerator
    {
        public API _api { get; set; }

        public string coreProjectFolder { get; set; }
        public string testProjectFolder { get; set; }
        public string xunitExtensionProjectFolder { get; set; }
        public string testDataFolder { get; set; }
        public string requestBodiesFolder { get; set; }
        public string solutionFolder { get; set; }
        

        public CSXSolutionGenerator(API api)
        {
            _api = api;
            
            solutionFolder = Path.Combine(api.RootFolderName, api.SolutionFolder);
            coreProjectFolder = Path.Combine(solutionFolder, "Core");
            testProjectFolder = Path.Combine(solutionFolder, "Tests");
            testDataFolder = Path.Combine(testProjectFolder, "TestData");
            requestBodiesFolder = Path.Combine(testDataFolder, "RequestBodies");
            xunitExtensionProjectFolder = Path.Combine(solutionFolder, "XUnitExtensions");
        }

        // This class will be used to genrate a new Visual Studio project for the user.
        // It uses Roslyn AST tp generate the code.
        // The output will be a solution file and a project file and all the necessary files for the testing framework.
        // The input will be a CSXAPI
        // The output will be a Visual Studio project
        
        public void GenerateSolution(API api)
        {
            var loggers = new List<ILogger> { new ConsoleLogger() };

            // Generate the solution file using MSBuild
            // The solution file will be generated using the MSBuild API
            var solutionPath = Path.Combine(api.SolutionFolder, api.ProjectName + ".sln");

            var projectCollection = new ProjectCollection();
            projectCollection.RegisterLoggers(loggers);   

            // Create a new solution file
            var solutionProject = projectCollection.LoadProject(solutionPath);

            // Create the core project. The output project will be a .NET Core project. The csproj file will be generated using the MSBuild API and put in the Core folder.
            string coreProjectName = api.ProjectName + ".Core";
            var coreProject = new Project($"./{coreProjectFolder}/{coreProjectName}.csproj");

            coreProject.SetProperty("Configuration", "Debug");
            coreProject.SetProperty("Platform", "AnyCPU");
            coreProject.SetProperty("OutputType", "Library");
            coreProject.SetProperty("RootNamespace", coreProjectName);
            coreProject.SetProperty("AssemblyName", coreProjectName);
            coreProject.SetProperty("TargetFramework", "net8.0");
            coreProject.SetProperty("ImplicitUsings", "enable");
            coreProject.SetProperty("Nullable", "enable");

            coreProject.AddItem("PackageReference", "Microsoft.NET.Test.Sdk", new Dictionary<string, string> { { "Version", "17.10.0-preview-24080.9" } });
            coreProject.AddItem("PackageReference", "xunit", new Dictionary<string, string> { { "Version", "2.7.0" } });
            coreProject.AddItem("PackageReference", "xunit.abstractions", new Dictionary<string, string> { { "Version", "2.0.3" } });
            coreProject.AddItem("PackageReference", "xunit.analyzers", new Dictionary<string, string> { { "Version", "1.11.0" } });
            coreProject.AddItem("PackageReference", "xunit.assert", new Dictionary<string, string> { { "Version", "2.7.0" } });
            coreProject.AddItem("PackageReference", "xunit.core", new Dictionary<string, string> { { "Version", "2.7.0" } });
            coreProject.AddItem("PackageReference", "xunit.runner.visualstudio", new Dictionary<string, string> { { "Version", "2.7.0" }, { "IncludeAssets", "runtime; build; native; contentfiles; analyzers" }, { "PrivateAssets", "all" } });
            coreProject.AddItem("PackageReference", "xunit.runner.console", new Dictionary<string, string> { { "Version", "2.7.0" }, { "IncludeAssets", "runtime; build; native; contentfiles; analyzers" }, { "PrivateAssets", "all" } });
            
            // Create the xunit project. The output project will be a .NET Core project. The csproj file will be generated using the MSBuild API and put in the XUnit folder.
            string xunitProjectName = api.ProjectName + ".XUnitExtensions";
            var xunitProject = new Project($"./{xunitExtensionProjectFolder}/{xunitProjectName}.csproj");

            xunitProject.SetProperty("Configuration", "Debug");
            xunitProject.SetProperty("Platform", "AnyCPU");
            xunitProject.SetProperty("OutputType", "Library");
            xunitProject.SetProperty("RootNamespace", xunitProjectName);
            xunitProject.SetProperty("AssemblyName", xunitProjectName);
            xunitProject.SetProperty("TargetFramework", "net8.0");
            xunitProject.SetProperty("ImplicitUsings", "enable");
            xunitProject.SetProperty("Nullable", "enable");

            xunitProject.AddItem("ProjectReference", coreProject.FullPath);
            xunitProject.AddItem("PackageReference", "Newtonsoft.Json", new Dictionary<string, string> { { "Version", "13.0.3" } });
            xunitProject.AddItem("PackageReference", "xunit.extensibility.core", new Dictionary<string, string> { { "Version", "2.7.0" } });

            // Create the test project. The output project will be a .NET Core project. The csproj file will be generated using the MSBuild API and put in the Test folder.
            string testProjectName = api.ProjectName + ".Tests";
            var testProject = new Project($"./{testProjectFolder}/{testProjectName}.csproj");

            testProject.SetProperty("Configuration", "Debug");
            testProject.SetProperty("Platform", "AnyCPU");
            testProject.SetProperty("OutputType", "Library");
            testProject.SetProperty("RootNamespace", api.Namespace);
            testProject.SetProperty("AssemblyName", api.ProjectName + ".Tests");
            testProject.SetProperty("TargetFramework", "net8.0");
            testProject.SetProperty("ImplicitUsings", "enable");
            testProject.SetProperty("Nullable", "enable");

            testProject.AddItem("ProjectReference", coreProject.FullPath);
            testProject.AddItem("PackageReference", "Microsoft.NET.Test.Sdk", new Dictionary<string, string> { { "Version", "17.10.0-preview-24080.9" } });
            testProject.AddItem("PackageReference", "xunit", new Dictionary<string, string> { { "Version", "2.7.0" } });
            testProject.AddItem("PackageReference", "xunit.abstractions", new Dictionary<string, string> { { "Version", "2.0.3" } });
            testProject.AddItem("PackageReference", "xunit.analyzers", new Dictionary<string, string> { { "Version", "1.11.0" } });
            testProject.AddItem("PackageReference", "xunit.assert", new Dictionary<string, string> { { "Version", "2.7.0" } });
            testProject.AddItem("PackageReference", "xunit.core", new Dictionary<string, string> { { "Version", "2.7.0" } });
            testProject.AddItem("PackageReference", "xunit.runner.visualstudio", new Dictionary<string, string> { { "Version", "2.7.0" }, { "IncludeAssets", "runtime; build; native; contentfiles; analyzers" }, { "PrivateAssets", "all" } });
            testProject.AddItem("PackageReference", "xunit.runner.console", new Dictionary<string, string> { { "Version", "2.7.0" }, { "IncludeAssets", "runtime; build; native; contentfiles; analyzers" }, { "PrivateAssets", "all" } });

            // Add the projects to the solution
            solutionProject.AddItem(coreProjectName,coreProject.FullPath);
            solutionProject.AddItem(xunitProjectName, xunitProject.FullPath);
            solutionProject.AddItem(testProjectName, testProject.FullPath);

            // Save the solution file
            solutionProject.Save(solutionPath);

            projectCollection.UnregisterAllLoggers();
        }

        public void GenerateFolderStructure()
        {
            if (!Directory.Exists(solutionFolder))
            {
                Directory.CreateDirectory(solutionFolder);
            }

            if (!Directory.Exists(coreProjectFolder))
            {
                Directory.CreateDirectory(coreProjectFolder);
            }

            if (!Directory.Exists(testProjectFolder))
            {
                Directory.CreateDirectory(testProjectFolder);
            }

            if (!Directory.Exists(testDataFolder))
            {
                Directory.CreateDirectory(testDataFolder);
            }

            if (!Directory.Exists(requestBodiesFolder))
            {
                Directory.CreateDirectory(requestBodiesFolder);
            }

            if (!Directory.Exists(xunitExtensionProjectFolder))
            {
                Directory.CreateDirectory(xunitExtensionProjectFolder);
            }
        }
    }
}
