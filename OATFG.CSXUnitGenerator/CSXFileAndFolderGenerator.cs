using OATFG.Core.Utilities;
using OATFG.CSXUnitGenerator.DataModels;
using System.Text;

namespace OATFG.CSXUnitGenerator
{
    public class CSXFileAndFolderGenerator
    {
        private API _api { get; set; }
        private string coreProjectFolder { get; set; }
        private string testProjectFolder { get; set; }
        private string testDataFolder { get; set; }
        private string requestBodiesFolder { get; set; }
        private string solutionFolder { get; set; }


        public CSXFileAndFolderGenerator(API api)
        {
            _api = api;

            solutionFolder = Path.Combine(api.RootFolderName, api.SolutionFolder);
            coreProjectFolder = Path.Combine(solutionFolder, "Core");
            testProjectFolder = Path.Combine(solutionFolder, "Tests");
            testDataFolder = "./Tests/TestData";
            requestBodiesFolder = $"{testDataFolder}/RequestBodies";

            GenerateFolderStructure();
        }

        public void GenerateFrameworkFiles()
        {
            //Create the namespaces for the files
            string coreNamespace = _api.Namespace + ".Core";
            string testNamespace = _api.Namespace + ".Tests";
            string xunitExtensionNamespace = _api.Namespace + ".XUnitExtensions";

            // Generate the access client stub class
            GenerateAccessClientStub(coreNamespace, _api.Server);

            // Generate the endpoint base class
            GenerateEndpointBaseClass(coreNamespace);

            // Generate the endpoint classes
            foreach (var endpoint in _api.Endpoints)
            {
                GenerateEndpointFileFromAPI(endpoint, coreProjectFolder, coreNamespace, requestBodiesFolder, endpoint.OperationId.MakeFileSafeName());
            }

            // Generate the project files (csproj, appsettings and solution) so the framework is buildable
            GenerateProjectFiles(coreNamespace);
        }

        public static void GenerateEndpointFileFromAPI(Endpoint endpoint, string coreProjectFolder, string coreNamespace, string requestBodiesFolder, string classAndFileName)
        {
            string endpointClassFile = Path.Combine(coreProjectFolder, "Endpoints", $"{classAndFileName}.cs");

            if (!Directory.Exists(Path.Combine(coreProjectFolder, "Endpoints")))
            {
                Directory.CreateDirectory(Path.Combine(coreProjectFolder, "Endpoints"));
            }

            // Create the file
            File.WriteAllText(endpointClassFile, string.Empty);

            StringBuilder sb = new();

            // Add the using statements
            sb.AppendLine($"using {coreNamespace}.Client;".PrependSpaces(0));
            sb.AppendLine($"using {coreNamespace}.Data;".PrependSpaces(0));
            sb.AppendLine("using System.Net.Http;".PrependSpaces(0));
            sb.AppendLine();

            // Add the namespace
            sb.AppendLine($"namespace {coreNamespace}.Endpoints".PrependSpaces(0));
            sb.AppendLine("{".PrependSpaces(0));

            // Add the class
            sb.AppendLine($"public class {endpoint.GeneratedMethodName} : EndpointBase".PrependSpaces(4));
            sb.AppendLine("{".PrependSpaces(4));

            // Add the AccessClient field
            sb.AppendLine("private AccessClient _accessClient;".PrependSpaces(8));
            sb.AppendLine();

            // Add the dependency injection constructor
            sb.AppendLine($"public {endpoint.GeneratedMethodName}() : base()".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));

            // Add the AccessClient instantiation
            sb.AppendLine("_accessClient = new AccessClient".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("log = _log".PrependSpaces(16));
            sb.AppendLine("};".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            sb.AppendLine(GeneratePrivateMethodForEndpointStringInterpolation(endpoint));
            sb.AppendLine();

            if (endpoint.RequestBody != null)
            {
                sb.AppendLine(CreateEndpointAPIRequestMethod(endpoint, requestBodiesFolder));
            }
            else
            {
                sb.AppendLine(CreateEndpointAPIRequestExpressionBodyMethod(endpoint));
            }

            //close the class
            sb.AppendLine("}".PrependSpaces(4));

            //close the namespace
            sb.AppendLine("}".PrependSpaces(0));

            // Write the file
            File.WriteAllText(endpointClassFile, sb.ToString());
        }

        /// <summary>
        /// The method to generate the private method for the endpoint path using string interpolation
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public static string GeneratePrivateMethodForEndpointStringInterpolation(Endpoint endpoint)
        {
            StringBuilder sb = new();

            // add the documentation
            sb.AppendLine($"// Method: {endpoint.Method}".PrependSpaces(8));
            sb.AppendLine($"// Summary: {endpoint.Summary}".PrependSpaces(8));
            sb.AppendLine($"// OperationId: {endpoint.OperationId}".PrependSpaces(8));

            if (endpoint.PathVariables.Count > 0)
            {
                foreach (var pathVariable in endpoint.PathVariables)
                {
                    sb.AppendLine($"// Path Variable: {pathVariable.Name} - Type: {pathVariable.Type} - Required: {pathVariable.IsRequired}".PrependSpaces(8));
                }
            }

            if (endpoint.QueryParameters.Count > 0)
            {
                foreach (var queryParameter in endpoint.QueryParameters)
                {
                    sb.AppendLine($"// Query Parameter: {queryParameter.Name} - Type: {queryParameter.Type} - Required: {queryParameter.IsRequired}".PrependSpaces(8));
                }
            }

            if (endpoint.RequestBody != null)
            {
                sb.AppendLine($"// Request Body: Required".PrependSpaces(8));
            }


            sb.Append($"private static string {endpoint.GeneratedPrivateFieldName}".PrependSpaces(8));

            sb.Append('(');

            // add the path variables and query parameters to the method signature
            List<string> parameterNames = new();
            parameterNames.AddRange(endpoint.PathVariables.Select(pathVariable => pathVariable.Name));
            parameterNames.AddRange(endpoint.QueryParameters.Select(queryParameter => queryParameter.Name));

            sb.Append(string.Join(", ", parameterNames.Select(name => $"string {name}")));

            sb.AppendLine(")");

            // append any query parameters to the interpolated endpoint path
            string interpolationPath = endpoint.EndpointPath;
            if (endpoint.QueryParameters.Count > 0)
            {
                string queryString = string.Join("&", endpoint.QueryParameters.Select(queryParameter => $"{queryParameter.Name}={{{queryParameter.Name}}}"));
                interpolationPath = $"{interpolationPath}?{queryString}";
            }

            sb.AppendLine($"=> $\"{interpolationPath}\";".PrependSpaces(12));

            return sb.ToString();
        }

        /// <summary>
        /// The method to create the expression body API Request method for the endpoint
        /// </summary>
        /// <param name="endpoint">The endpoint that will be used to generate an expression body request method </param>
        /// <returns>A string representing the expression method</returns>
        public static string CreateEndpointAPIRequestExpressionBodyMethod(Endpoint endpoint)
        {
            StringBuilder sb = new();

            CreateMethodDocumentation(endpoint, sb);

            sb.Append($"public async Task<APIResponse> Request(".PrependSpaces(8));

            // add the path variables and query parameters to the method signature
            List<string> parameterNames = new();
            parameterNames.AddRange(endpoint.PathVariables.Select(pathVariable => pathVariable.Name));
            parameterNames.AddRange(endpoint.QueryParameters.Select(queryParameter => queryParameter.Name));

            sb.Append(string.Join(", ", parameterNames.Select(name => $"string {name}")));

            sb.AppendLine(")");

            var HttpMethod = endpoint.Method switch
            {
                "get" => "HttpMethod.Get",
                "post" => "HttpMethod.Post",
                "put" => "HttpMethod.Put",
                "delete" => "HttpMethod.Delete",
                "options" => "HttpMethod.Options",
                "patch" => "HttpMethod.Patch",
                _ => "HttpMethod.Get"
            };


            sb.Append($"=> await CreateRequest({HttpMethod}, {endpoint.GeneratedPrivateFieldName}(".PrependSpaces(12));

            if (endpoint.PathVariables.Count > 0)
            {
                foreach (var pathVariable in endpoint.PathVariables)
                {
                    sb.Append($"{pathVariable.Name}");

                    //if the path variable is not the last one, add a comma
                    if ((endpoint.PathVariables.IndexOf(pathVariable) != endpoint.PathVariables.Count - 1) || (endpoint.QueryParameters.Count > 0))
                    {
                        sb.Append(", ");
                    }
                }
            }

            if (endpoint.QueryParameters.Count > 0)
            {
                foreach (var queryParameter in endpoint.QueryParameters)
                {
                    sb.Append($"{queryParameter.Name}: {queryParameter.Name}");

                    //if the query parameter is not the last one, add a comma
                    if (endpoint.QueryParameters.IndexOf(queryParameter) != endpoint.QueryParameters.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
            }

            sb.Append(")");

            sb.AppendLine(");");

            return sb.ToString();
        }

        /// <summary>
        /// Creates the documentation for the endpoint API Request method
        /// </summary>
        /// <param name="endpoint">The endpoint containing the method to be documented</param>
        /// <param name="sb">The string builder for writing the documentation to the file</param>
        private static void CreateMethodDocumentation(Endpoint endpoint, StringBuilder sb)
        {
            // add the documentation
            sb.AppendLine($"/// <summary>".PrependSpaces(8));
            sb.AppendLine($"/// {endpoint.Summary}".PrependSpaces(8));
            sb.AppendLine($"/// </summary>".PrependSpaces(8));

            // add the path variables to the documentation
            if (endpoint.PathVariables.Count > 0)
            {
                foreach (var pathVariable in endpoint.PathVariables)
                {
                    sb.AppendLine($"/// <param name=\"{pathVariable.Name}\">The value of the {pathVariable.Name} path variable</param>".PrependSpaces(8));
                }
            }

            // add the query parameters to the documentation

            if (endpoint.QueryParameters.Count > 0)
            {
                foreach (var queryParameter in endpoint.QueryParameters)
                {
                    sb.AppendLine($"/// <param name=\"{queryParameter.Name}\">The value of the {queryParameter.Name} query parameter</param>".PrependSpaces(8));
                }
            }


            if (endpoint.RequestBody != null)
            {
                sb.AppendLine($"/// <param name=\"pathToBody\">The path to the request body file</param>".PrependSpaces(8));
            }


        }


        /// <summary>
        /// The method to create the API request method for the endpoint
        /// </summary>
        /// <param name="endpoint">The endpoint object to create an api request file</param>
        /// <param name="requestBodyFolder">The folder the default request body will be stored in.</param>
        /// <returns></returns>
        public static string CreateEndpointAPIRequestMethod(Endpoint endpoint, string requestBodyFolder)
        {
            StringBuilder sb = new();

            CreateMethodDocumentation(endpoint, sb);

            sb.Append($"public async Task<APIResponse> Request(".PrependSpaces(8));

            if (endpoint.PathVariables.Count > 0)
            {
                foreach (var pathVariable in endpoint.PathVariables)
                {
                    sb.Append($"string {pathVariable.Name}");

                    //if the path variable is not the last one, add a comma
                    if ((endpoint.PathVariables.IndexOf(pathVariable) != endpoint.PathVariables.Count - 1) || (endpoint.QueryParameters.Count > 0))
                    {
                        sb.Append(", ");
                    }
                }
            }

            if (endpoint.QueryParameters.Count > 0)
            {
                foreach (var queryParameter in endpoint.QueryParameters)
                {
                    sb.Append($"string {queryParameter.Name}");

                    //if the query parameter is not the last one, add a comma
                    if (endpoint.QueryParameters.IndexOf(queryParameter) != endpoint.QueryParameters.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
            }


            if (endpoint.RequestBody != null)
            {
                if (endpoint.PathVariables.Count > 0 || endpoint.QueryParameters.Count > 0)
                {
                    sb.AppendLine(", string? pathToBody = null)");
                }
                else
                {
                    sb.AppendLine("string? pathToBody = null)");
                }
            }
            else
            {
                sb.AppendLine(")");
            }


            var HttpMethod = endpoint.Method switch
            {
                "get" => "HttpMethod.Get",
                "post" => "HttpMethod.Post",
                "put" => "HttpMethod.Put",
                "delete" => "HttpMethod.Delete",
                "options" => "HttpMethod.Options",
                "patch" => "HttpMethod.Patch",
                _ => "HttpMethod.Get"
            };

            sb.AppendLine("{".PrependSpaces(8));

            if (endpoint.RequestBody != null)
            {
                sb.AppendLine($"pathToBody ??= $\"{requestBodyFolder}/{endpoint.GeneratedMethodName}.json\";".PrependSpaces(12));
                sb.Append($"return await CreateRequestWithBody({HttpMethod}, {endpoint.GeneratedPrivateFieldName}(".PrependSpaces(12));
            }
            else
            {
                sb.Append($"return await CreateRequest({HttpMethod}, {endpoint.GeneratedPrivateFieldName}(".PrependSpaces(12));
            }

            bool firstArgument = true;
            foreach (var pathVariable in endpoint.PathVariables)
            {
                if (!firstArgument)
                {
                    sb.Append(", ");
                }
                sb.Append(pathVariable.Name);
                firstArgument = false;
            }

            foreach (var queryParameter in endpoint.QueryParameters)
            {
                if (!firstArgument)
                {
                    sb.Append(", ");
                }
                sb.Append($"{queryParameter.Name}: {queryParameter.Name}");
                firstArgument = false;
            }

            // close the path method call
            sb.Append(')');

            if (endpoint.RequestBody != null)
            {
                sb.Append(", pathToBody");
            }

            sb.AppendLine(");");

            sb.AppendLine("}".PrependSpaces(8));

            return sb.ToString();

        }

        private void GenerateAccessClientStub(string coreNamespace, string server)
        {
            string accessClientStubFile = Path.Combine(coreProjectFolder, "Client", "AccessClient.cs");

            if (!Directory.Exists(Path.Combine(coreProjectFolder, "Client")))
            {
                Directory.CreateDirectory(Path.Combine(coreProjectFolder, "Client"));
            }

            // Create the file
            File.WriteAllText(accessClientStubFile, string.Empty);

            StringBuilder sb = new StringBuilder();

            // Add the using statements
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using Xunit.Abstractions;");
            sb.AppendLine();

            // Add the namespace
            sb.AppendLine($"namespace {coreNamespace}.Client");
            sb.AppendLine("{");
            sb.AppendLine("public class AccessClient : IDisposable".PrependSpaces(4));
            sb.AppendLine("{".PrependSpaces(4));
            sb.AppendLine("private ConfigurationBuilder _builder;".PrependSpaces(8));
            sb.AppendLine("private IConfigurationRoot _config;".PrependSpaces(8));
            sb.AppendLine("private readonly HttpClient _httpClient = new();".PrependSpaces(8));
            sb.AppendLine();

            // Add the APIHost property, defaulting to the server from the OpenAPI document
            sb.AppendLine("public string APIHost { get; }".PrependSpaces(8));
            sb.AppendLine();

            // Add the log field
            sb.AppendLine("public ITestOutputHelper? log;".PrependSpaces(8));
            sb.AppendLine();

            // Add the constructor
            sb.AppendLine("public AccessClient()".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            sb.AppendLine("_builder = new ConfigurationBuilder();".PrependSpaces(12));
            sb.AppendLine("_builder.AddJsonFile(\"appsettings.json\");".PrependSpaces(12));
            sb.AppendLine("_config = _builder.Build();".PrependSpaces(12));
            sb.AppendLine($"APIHost = _config[\"APIHost\"] ?? \"{server}\";".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the send method
            sb.AppendLine("public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)".PrependSpaces(8));
            sb.AppendLine("=> _httpClient.SendAsync(request);".PrependSpaces(8));
            sb.AppendLine();

            // Add the dispose method
            sb.AppendLine("public void Dispose() => _httpClient.Dispose();".PrependSpaces(8));

            // close the file out
            sb.AppendLine("}".PrependSpaces(4));
            sb.AppendLine("}".PrependSpaces(0));

            // Write the file
            File.WriteAllText(accessClientStubFile, sb.ToString());
        }

        private void GenerateEndpointBaseClass(string coreNamespace)
        {
            // Create the file
            string endpointBaseClassFile = Path.Combine(coreProjectFolder, "Endpoints", "EndpointBase.cs");

            if (!Directory.Exists(Path.Combine(coreProjectFolder, "Endpoints")))
            {
                Directory.CreateDirectory(Path.Combine(coreProjectFolder, "Endpoints"));
            }

            File.WriteAllText(endpointBaseClassFile, string.Empty);

            StringBuilder sb = new();

            // Add the using statements
            sb.AppendLine($"using {coreNamespace}.Client;");
            sb.AppendLine($"using {coreNamespace}.Data;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using Xunit.Abstractions;");
            sb.AppendLine();

            // Add the namespace
            sb.AppendLine($"namespace {coreNamespace}.Endpoints");
            sb.AppendLine("{");

            //Add the class as IDisposable
            sb.AppendLine("public class EndpointBase : IDisposable".PrependSpaces(4));
            sb.AppendLine("{".PrependSpaces(4));

            // Add the private fields
            sb.AppendLine("private AccessClient _accessClient = new();".PrependSpaces(8));
            sb.AppendLine();

            // Add the public fields
            sb.AppendLine("public ITestOutputHelper? _log;".PrependSpaces(8));
            sb.AppendLine();

            // Add the constructor
            sb.AppendLine("public EndpointBase(ITestOutputHelper log)".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            sb.AppendLine("_accessClient = new AccessClient();".PrependSpaces(12));
            sb.AppendLine("_accessClient.log = log;".PrependSpaces(12));
            sb.AppendLine("_log = log;".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the empty constructor
            sb.AppendLine("public EndpointBase()".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the Dispose method
            sb.AppendLine("public void Dispose()".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            sb.AppendLine("_accessClient?.Dispose();".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the APIRequest method
            sb.AppendLine("public virtual async Task<APIResponse> APIRequest(HttpRequestMessage httpRequestMessage)".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            //add the try block
            sb.AppendLine("try".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("HttpResponseMessage response = await _accessClient.SendAsync(httpRequestMessage);".PrependSpaces(16));
            sb.AppendLine();
            sb.AppendLine("string responseCode = response.StatusCode.ToString();".PrependSpaces(16));
            sb.AppendLine("string body = await response.Content.ReadAsStringAsync();".PrependSpaces(16));
            sb.AppendLine();
            sb.AppendLine("return new APIResponse(responseCode, body);".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            //add the catch block
            sb.AppendLine("catch (Exception ex)".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("Log($\"Exception: {ex.Message}\");".PrependSpaces(16));
            sb.AppendLine("throw;".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the GetBodyContent method
            sb.AppendLine("public string GetBodyContent(string pathToBody)".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            // check if the file exists
            sb.AppendLine("if (!File.Exists(pathToBody))".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("throw new FileNotFoundException(\"The file does not exist.\");".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            sb.AppendLine();
            sb.AppendLine("return File.ReadAllText(pathToBody);".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));

            // Add the CreateRequest method
            sb.AppendLine("public virtual async Task<APIResponse> CreateRequest(HttpMethod method, string endpointPath)".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            // add the try block
            sb.AppendLine("try".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("HttpRequestMessage request = new(){Method = method, RequestUri = new Uri(_accessClient.APIHost + endpointPath)};".PrependSpaces(16));
            sb.AppendLine();
            sb.AppendLine("return await APIRequest(request);".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            // add the catch block
            sb.AppendLine("catch (Exception ex)".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("Log($\"Exception: {ex.Message}\");".PrependSpaces(16));
            sb.AppendLine("throw;".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the CreateRequestWithBody method
            sb.AppendLine("public virtual async Task<APIResponse> CreateRequestWithBody(HttpMethod method, string endpointPath, string pathToBody)".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            // add the try block
            sb.AppendLine("try".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("HttpRequestMessage request = new()".PrependSpaces(16));
            sb.AppendLine("{".PrependSpaces(16));
            sb.AppendLine("Method = method,".PrependSpaces(20));
            sb.AppendLine("RequestUri = new Uri(_accessClient.APIHost + endpointPath),".PrependSpaces(20));
            sb.AppendLine("Content = new StringContent(GetBodyContent(pathToBody), Encoding.UTF8, \"application/json\")".PrependSpaces(20));
            sb.AppendLine("};".PrependSpaces(16));
            sb.AppendLine();
            sb.AppendLine("return await APIRequest(request);".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            // add the catch block
            sb.AppendLine("catch (Exception ex)".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("Log($\"Exception: {ex.Message}\");".PrependSpaces(16));
            sb.AppendLine("throw;".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the Log method
            sb.AppendLine("public void Log(string message) => _log?.WriteLine(message);".PrependSpaces(8));

            // Close the class
            sb.AppendLine("}".PrependSpaces(4));
            sb.AppendLine("}".PrependSpaces(0));

            // Write the file
            File.WriteAllText(endpointBaseClassFile, sb.ToString());
        }

        private void GenerateProjectFiles(string coreNamespace)
        {
            GenerateResponsesFile(coreNamespace);
            GenerateCoreProjectFile();
            GenerateTestProjectFile();
            GenerateTestAppSettings();
            GenerateSolutionFile();
        }

        private void GenerateResponsesFile(string coreNamespace)
        {
            string responsesFile = Path.Combine(coreProjectFolder, "Data", "Responses.cs");

            if (!Directory.Exists(Path.Combine(coreProjectFolder, "Data")))
            {
                Directory.CreateDirectory(Path.Combine(coreProjectFolder, "Data"));
            }

            StringBuilder sb = new();

            sb.AppendLine($"namespace {coreNamespace}.Data");
            sb.AppendLine("{");
            sb.AppendLine("/// <summary>The response returned by an endpoint request</summary>".PrependSpaces(4));
            sb.AppendLine("public class APIResponse".PrependSpaces(4));
            sb.AppendLine("{".PrependSpaces(4));
            sb.AppendLine("public string StatusCode { get; }".PrependSpaces(8));
            sb.AppendLine("public string Body { get; }".PrependSpaces(8));
            sb.AppendLine();
            sb.AppendLine("public APIResponse(string statusCode, string body)".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            sb.AppendLine("StatusCode = statusCode;".PrependSpaces(12));
            sb.AppendLine("Body = body;".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine("}".PrependSpaces(4));
            sb.AppendLine("}");

            File.WriteAllText(responsesFile, sb.ToString());
        }

        private void GenerateCoreProjectFile()
        {
            string coreProjectFile = Path.Combine(coreProjectFolder, $"{_api.CoreProjectName}.csproj");

            StringBuilder sb = new();

            sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            sb.AppendLine();
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
            sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
            sb.AppendLine("    <Nullable>enable</Nullable>");
            sb.AppendLine("    <IsTestProject>false</IsTestProject>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine();
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine("    <PackageReference Include=\"Microsoft.Extensions.Configuration.Json\" Version=\"10.0.13\" />");
            sb.AppendLine("    <PackageReference Include=\"xunit\" Version=\"2.9.3\" />");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
            sb.AppendLine("</Project>");

            File.WriteAllText(coreProjectFile, sb.ToString());
        }

        private void GenerateTestProjectFile()
        {
            string testProjectFile = Path.Combine(testProjectFolder, $"{_api.TestProjectName}.csproj");

            StringBuilder sb = new();

            sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            sb.AppendLine();
            sb.AppendLine("  <PropertyGroup>");
            sb.AppendLine("    <TargetFramework>net10.0</TargetFramework>");
            sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
            sb.AppendLine("    <Nullable>enable</Nullable>");
            sb.AppendLine("    <IsPackable>false</IsPackable>");
            sb.AppendLine("    <IsTestProject>true</IsTestProject>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine();
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine("    <PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"18.11.0\" />");
            sb.AppendLine("    <PackageReference Include=\"xunit\" Version=\"2.9.3\" />");
            sb.AppendLine("    <PackageReference Include=\"xunit.runner.visualstudio\" Version=\"4.0.1\">");
            sb.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>");
            sb.AppendLine("      <PrivateAssets>all</PrivateAssets>");
            sb.AppendLine("    </PackageReference>");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine($"    <ProjectReference Include=\"..\\Core\\{_api.CoreProjectName}.csproj\" />");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine("    <None Update=\"appsettings.json\">");
            sb.AppendLine("      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
            sb.AppendLine("    </None>");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine();
            sb.AppendLine("</Project>");

            File.WriteAllText(testProjectFile, sb.ToString());
        }

        private void GenerateTestAppSettings()
        {
            string appSettingsFile = Path.Combine(testProjectFolder, "appsettings.json");

            StringBuilder sb = new();

            sb.AppendLine("{");
            sb.AppendLine($"  \"APIHost\": \"{_api.Server}\"");
            sb.AppendLine("}");

            File.WriteAllText(appSettingsFile, sb.ToString());
        }

        private void GenerateSolutionFile()
        {
            string solutionFile = Path.Combine(solutionFolder, $"{_api.SolutionFolder}.sln");

            string coreGuid = Guid.NewGuid().ToString().ToUpperInvariant();
            string testGuid = Guid.NewGuid().ToString().ToUpperInvariant();

            StringBuilder sb = new();

            sb.AppendLine();
            sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            sb.AppendLine("# Visual Studio Version 17");
            sb.AppendLine("VisualStudioVersion = 17.9.34714.143");
            sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
            sb.AppendLine($"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"{_api.CoreProjectName}\", \"Core\\{_api.CoreProjectName}.csproj\", \"{{{coreGuid}}}\"");
            sb.AppendLine("EndProject");
            sb.AppendLine($"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"{_api.TestProjectName}\", \"Tests\\{_api.TestProjectName}.csproj\", \"{{{testGuid}}}\"");
            sb.AppendLine("EndProject");
            sb.AppendLine("Global");
            sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
            sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
            sb.AppendLine("\tEndGlobalSection");
            sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");

            foreach (string guid in new[] { coreGuid, testGuid })
            {
                sb.AppendLine($"\t\t{{{guid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
                sb.AppendLine($"\t\t{{{guid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
                sb.AppendLine($"\t\t{{{guid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
                sb.AppendLine($"\t\t{{{guid}}}.Release|Any CPU.Build.0 = Release|Any CPU");
            }

            sb.AppendLine("\tEndGlobalSection");
            sb.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
            sb.AppendLine("\t\tHideSolutionNode = FALSE");
            sb.AppendLine("\tEndGlobalSection");
            sb.AppendLine("EndGlobal");

            File.WriteAllText(solutionFile, sb.ToString());
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

            // The embedded default request-body path stays relative (portable), but the folders
            // themselves belong to the generated tests project
            string absoluteTestDataFolder = Path.Combine(testProjectFolder, "TestData");
            string absoluteRequestBodiesFolder = Path.Combine(absoluteTestDataFolder, "RequestBodies");

            if (!Directory.Exists(absoluteTestDataFolder))
            {
                Directory.CreateDirectory(absoluteTestDataFolder);
            }

            if (!Directory.Exists(absoluteRequestBodiesFolder))
            {
                Directory.CreateDirectory(absoluteRequestBodiesFolder);
            }
        }
    }
}
