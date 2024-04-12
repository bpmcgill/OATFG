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
        private string xunitExtensionProjectFolder { get; set; }
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
            xunitExtensionProjectFolder = Path.Combine(solutionFolder, "Extensions");

            GenerateFolderStructure();
        }

        public void GenerateFrameworkFiles()
        {
            //Create the namespaces for the files
            string coreNamespace = _api.Namespace + ".Core";
            string testNamespace = _api.Namespace + ".Tests";
            string xunitExtensionNamespace = _api.Namespace + ".XUnitExtensions";

            // Generate the access client stub class
            GenerateAccessClientStub(coreNamespace);

            // Generate the endpoint base class
            GenerateEndpointBaseClass(coreNamespace);

            // Generate the endpoint classes

            foreach (var tag in _api.Tags)
            {
                GenerateEndpointClassFromTag(tag, coreProjectFolder, coreNamespace, requestBodiesFolder, tag.ClassAndFileName);
            }
        }

        public static void GenerateEndpointClassFromTag(Tag tag, string coreProjectFolder, string coreNamespace, string requestBodiesFolder, string classAndFileName)
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
            sb.AppendLine($"using Xunit.Abstractions;".PrependSpaces(0));
            sb.AppendLine();

            // Add the namespace
            sb.AppendLine($"namespace {coreNamespace}.Endpoints".PrependSpaces(0));
            sb.AppendLine("{".PrependSpaces(0));

            // Add the class
            sb.AppendLine($"public class {classAndFileName} : EndpointBase".PrependSpaces(4));
            sb.AppendLine("{".PrependSpaces(4));

            // Add the TestOutputHelper field
            sb.AppendLine("private ITestOutputHelper _log;".PrependSpaces(8));
            sb.AppendLine();

            // Add the AccessClient field
            sb.AppendLine("private AccessClient _accessClient;".PrependSpaces(8));
            sb.AppendLine();

            // if any of the endpoints have a request body, add the pathToBodies field and the path to the bodies folder. The path will be ./testData/requestBodies/{classAndFileName}

            if (tag.Endpoints.Any(e => e.RequestBody != null))
            {
                sb.AppendLine($"private string _pathToBodies = \"{Path.Combine(requestBodiesFolder)};".PrependSpaces(8));
                sb.AppendLine();
            }

            // Add the dependency injection constructor
            sb.AppendLine($"public {classAndFileName}() : base()".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));

            // Add the AccessClient instantiation
            sb.AppendLine("_accessClient = new AccessClient".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("log = _log".PrependSpaces(16));
            sb.AppendLine("};".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // For each endpoint, generate the private method for the endpoint string interpolation
            foreach (var endpoint in tag.Endpoints)
            {
                sb.AppendLine(GeneratePrivateMethodForEndpointStringInterpolation(endpoint));
                sb.AppendLine();
            }

            // For each endpoint, generate the APIRequest method

            foreach (var endpoint in tag.Endpoints)
            {
                if (endpoint.RequestBody != null)
                {
                    sb.AppendLine(CreateEndpointAPIRequestMethod(endpoint));
                }
                else
                {
                    sb.AppendLine(CreateEndpointAPIRequestExpressionBodyMethod(endpoint));
                }
            }

            //close the class
            sb.AppendLine("}".PrependSpaces(4));

            //close the namespace
            sb.AppendLine("}".PrependSpaces(0));

            // Write the file
            File.WriteAllText(endpointClassFile, sb.ToString());
        }

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

            // loop through the path varaibles and add them to the method signature

            if (endpoint.PathVariables.Count > 0)
            {
                foreach (var pathVariable in endpoint.PathVariables)
                {
                    sb.Append($"string {pathVariable.Name}");

                    //if the path variable is not the last one, add a comma
                    if (endpoint.PathVariables.IndexOf(pathVariable) != endpoint.PathVariables.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
            }

            sb.AppendLine(")");

            sb.AppendLine($"=> $\"{endpoint.EndpointPath}\";".PrependSpaces(12));

            return sb.ToString();
        }

        public static string CreateEndpointAPIRequestExpressionBodyMethod(Endpoint endpoint)
        {
            StringBuilder sb = new();

            sb.Append($"public async Task<APIResponse> {endpoint.GeneratedMethodName}(".PrependSpaces(8));

            if (endpoint.PathVariables.Count > 0)
            {
                foreach (var pathVariable in endpoint.PathVariables)
                {
                    sb.Append($"string {pathVariable.Name}");

                    //if the path variable is not the last one, add a comma
                    if (endpoint.PathVariables.IndexOf(pathVariable) != endpoint.PathVariables.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
            }

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

        public static string CreateEndpointAPIRequestMethod(Endpoint endpoint)
        {
            StringBuilder sb = new();

            sb.Append($"public async Task<APIResponse> {endpoint.GeneratedMethodName}WithBody(".PrependSpaces(8));

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
                sb.AppendLine(", string? pathToBody = null)");
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
                sb.AppendLine($"pathToBody ??= $\"{{_pathToBodies}}{endpoint.GeneratedMethodName}.json\";".PrependSpaces(12));
                sb.Append($"return await CreateRequestWithBody({HttpMethod}, {endpoint.GeneratedPrivateFieldName}(".PrependSpaces(12));
            }
            else
            {
                sb.Append($"return await CreateRequest({HttpMethod}, {endpoint.GeneratedPrivateFieldName}(".PrependSpaces(12));

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
            }

            sb.AppendLine("));");

            sb.AppendLine("}".PrependSpaces(8));

            return sb.ToString();

        }

        private void GenerateAccessClientStub(string coreNamespace)
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
            sb.AppendLine("using System.Net;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using Xunit.Abstractions;");
            sb.AppendLine();

            // Add the namespace
            sb.AppendLine($"namespace {coreNamespace}.Client");
            sb.AppendLine("{");
            sb.AppendLine("public class AccessClient".PrependSpaces(4));
            sb.AppendLine("{".PrependSpaces(4));
            sb.AppendLine("private ConfigurationBuilder _builder;".PrependSpaces(8));
            sb.AppendLine("private IConfigurationRoot _config;".PrependSpaces(8));
            sb.AppendLine("public ITestOutputHelper? log".PrependSpaces(8));

            // Add the constructor
            sb.AppendLine("public AccessClient()".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            sb.AppendLine("_builder = new ConfigurationBuilder();".PrependSpaces(12));
            sb.AppendLine("_builder.AddJsonFile(\"appsettings.json\");".PrependSpaces(12));
            sb.AppendLine("_config = _builder.Build();".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            sb.AppendLine("// TODO: Complete the access client class".PrependSpaces(8));

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
            sb.AppendLine($"using Xunit.Abstractions;");
            sb.AppendLine();

            // Add the namespace
            sb.AppendLine($"namespace {coreNamespace}.Endpoints");
            sb.AppendLine("{");

            //Add the class as IDisposable
            sb.AppendLine("public class EndpointBase : IDisposable".PrependSpaces(4));
            sb.AppendLine("{".PrependSpaces(4));

            // Add the private fields
            sb.AppendLine("private AccessClient _client;".PrependSpaces(8));
            sb.AppendLine();

            // Add the public fields
            sb.AppendLine("public ITestOutputHelper? _log".PrependSpaces(8));
            sb.AppendLine();

            // Add the constructor
            sb.AppendLine("public EndpointBase(ITestOutputHelper log)".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            sb.AppendLine("_client = new AccessClient();".PrependSpaces(12));
            sb.AppendLine("_client.log = log;".PrependSpaces(12));
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
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the APIRequest method
            sb.AppendLine("public virtyal async Task<APIResponse> APIRequest(HttpRequestMessage httpRequestMessage)".PrependSpaces(8));
            sb.AppendLine("{".PrependSpaces(8));
            //add the try block
            sb.AppendLine("try".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("HttpClient client = _client ?? throw new Exception($\"Client unavailable\");".PrependSpaces(16));
            sb.AppendLine();
            sb.AppendLine("HttpResponseMessage response = client.SendAsync(httpRequestMessage).Result;".PrependSpaces(16));
            sb.AppendLine("string responseCode = response.StatusCode.ToString();".PrependSpaces(16));
            sb.AppendLine("Task<string> body = response.Content.ReadAsStringAsync();".PrependSpaces(16));
            sb.AppendLine();
            sb.AppendLine("return new APIResponse{responseCode, body};".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            //add the catch block
            sb.AppendLine("catch (Exception ex)".PrependSpaces(12));
            sb.AppendLine("{".PrependSpaces(12));
            sb.AppendLine("Log($\"Exception: {{ex.Message}}\");".PrependSpaces(16));
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
            sb.AppendLine("public virtual async Task<ApiRequest> CreateRequest(HttpMethod method, string endpointPath)".PrependSpaces(8));
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
            sb.AppendLine("Log($\"Exception: {{ex.Message}}\");".PrependSpaces(16));
            sb.AppendLine("throw;".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the CreateRequestWithBody method
            sb.AppendLine("public virtual async Task<ApiRequest> CreateRequestWithBody(HttpMethod method, string endpointPath, string pathToBody)".PrependSpaces(8));
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
            sb.AppendLine("Log($\"Exception: {{ex.Message}}\");".PrependSpaces(16));
            sb.AppendLine("throw;".PrependSpaces(16));
            sb.AppendLine("}".PrependSpaces(12));
            sb.AppendLine("}".PrependSpaces(8));
            sb.AppendLine();

            // Add the Log method
            sb.AppendLine("public void Log(string message) => _log.WriteLine(message);".PrependSpaces(8));

            // Close the class
            sb.AppendLine("}".PrependSpaces(4));
            sb.AppendLine("}".PrependSpaces(0));

            // Write the file
            File.WriteAllText(endpointBaseClassFile, sb.ToString());
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
