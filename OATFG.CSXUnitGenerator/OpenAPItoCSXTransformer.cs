using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NSwag;
using OATFG.Core.DataModels;
using OATFG.Core.Utilities;
using OATFG.CSXUnitGenerator.DataModels;
using System.Collections.ObjectModel;

namespace OATFG.CSXUnitGenerator
{
    public class OpenAPItoCSXTransformer
    {
        private ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        private IConfigurationRoot configurationRoot;

        private JObject jsonDoc;

        public OpenAPItoCSXTransformer()
        {
            configurationBuilder.AddJsonFile("csxug.appsettings.json");
            configurationRoot = configurationBuilder.Build();
        }

        private ObservableCollection<Tag> CreateCSXTagsAndEnpointsFromDocument(OpenApiDocument document)
        {
            try
            {
                // Create a list of CSXTags from the OpenAPI document
                ObservableCollection<Tag> csxTags = [];

                foreach (var operation in document.Operations)
                {
                    if (csxTags.Any(t => t.TagName == operation.Operation.Tags.FirstOrDefault()))
                    {
                        // Add the endpoint to the existing tag

                        csxTags.First(t => t.TagName == operation?.Operation?.Tags?.FirstOrDefault()).Endpoints.Add(GenerateCSXEndpoint(operation, document));
                    }
                    else
                    {
                        // Create a new tag and add the endpoint
                        csxTags.Add(new Tag()
                        {
                            TagName = operation.Operation.Tags.FirstOrDefault(),
                            ClassAndFileName = operation.Operation.Tags.FirstOrDefault().MakeFileSafeName().RemoveUnderscores(),
                            Endpoints = [GenerateCSXEndpoint(operation, document)]
                        });
                    }
                }

                return csxTags;
            }
            catch (Exception ex)
            {
                //TODO: Add logging & Exception handling
                return [];
            }
        }

        private Endpoint GenerateCSXEndpoint(OpenApiOperationDescription operation, OpenApiDocument document)
        {
            try
            {
                return new Endpoint
                {
                    EndpointPath = operation.Path,
                    GeneratedMethodName = operation.Operation.OperationId.MakePascalCase(),
                    GeneratedPrivateFieldName = operation.Operation.OperationId.MakePascalCase().LowerCaseFirstLetter(),
                    Method = operation.Method.ToString(),
                    OperationId = operation.Operation.OperationId ?? Resources.Resources.NoOperationIdForEndpoint,
                    PathVariables = GetPathVariables(operation),
                    QueryParameters = GetQueryParameters(operation),
                    RequestBody = GetRequestBody(operation),
                    Responses = GetResponses(operation),
                    Summary = operation.Operation.Summary ?? Resources.Resources.NoSummaryForEndpoint,
                };
            }
            catch (Exception)
            {
                //TODO: Add logging & Exception handling
                return new();
            }
        }

        private PathVariable GetPathVariable(OpenApiParameter parameter)
        {
            try
            {
                return new()
                {
                    IsRequired = parameter.IsRequired,
                    Name = parameter.Name,
                    Type = parameter.Schema.Type.ToString()
                };
            }
            catch (Exception)
            {
                //TODO: Add logging & Exception handling
                throw;
            }
        }

        private ObservableCollection<PathVariable> GetPathVariables(OpenApiOperationDescription operation)
        {
            try
            {
                ObservableCollection<PathVariable> pathVariables = new();

                foreach (var parameter in operation.Operation.Parameters.Where(p => p.Kind == OpenApiParameterKind.Path))
                {
                    pathVariables.Add(GetPathVariable(parameter));
                }

                return pathVariables;
            }
            catch (Exception)
            {
                //TODO: Add logging & Exception handling
                return [];
            }
        }

        private QueryParameter GetQueryParameter(OpenApiParameter parameter)
        {
            try
            {
                return new()
                {
                    IsRequired = parameter.IsRequired,
                    Name = parameter.Name,
                    Type = parameter.Schema.Type.ToString()
                };
            }
            catch (Exception)
            {
                //TODO: Add logging & Exception handling
                throw;
            }
        }

        private ObservableCollection<QueryParameter> GetQueryParameters(OpenApiOperationDescription operation)
        {
            try
            {
                ObservableCollection<QueryParameter> queryParameters = [];

                foreach (var parameter in operation.Operation.Parameters.Where(p => p.Kind == OpenApiParameterKind.Query))
                {
                    queryParameters.Add(GetQueryParameter(parameter));
                }

                return queryParameters;
            }
            catch (Exception)
            {
                //TODO: Add logging & Exception handling
                return [];
            }
        }

        private string? GetRequestBody(OpenApiOperationDescription operation)
        {
            try
            {
                if (operation.Operation.RequestBody == null)
                {
                    return null;
                }
                else
                {
                    //I need the operationID stored in a variable
                    var operationId = operation.Operation.OperationId;

                    string requestBodySchema = string.Empty;
                    bool found = false;

                    foreach (JProperty path in jsonDoc["paths"])
                    {
                        foreach (JProperty method in path.Value)
                        {
                            if (method.First["operationId"].ToString() == operationId)
                            {
                                if (method.Value["requestBody"] == null)
                                {
                                    return null;
                                }

                                requestBodySchema = method.Value["requestBody"]?["content"]?["application/json"]?["schema"]?["$ref"]?.ToString()
                                                 ?? method.Value["requestBody"]?["content"]?["application/json"]?["schema"]?["items"]?["$ref"]?.ToString()
                                                 ?? null;

                                found = true;
                                break;
                            }
                        }

                        if (found)
                        {
                            break;
                        }
                    }

                    return requestBodySchema != null ? GetBodySchema(requestBodySchema, jsonDoc) : null;
                }
            }
            catch (Exception)
            {
                //TODO: Add logging & Exception handling
                return null;
            }
        }

        private ObservableCollection<Response> GetResponses(OpenApiOperationDescription operation)
        {
            ObservableCollection<Response> responses = [];

            foreach (var response in operation.Operation.Responses)
            {
                responses.Add(new Response
                {
                    StatusCode = int.Parse(response.Key),
                    Description = response.Value.Description,
                    Body = response.Key == "200" ? GetResponseBody(operation) : null
                });
            }
            Console.WriteLine();
            return responses;
        }

        public static string GetBodySchema(string? bodySchemaPath, JObject swagger)
        {
            if (bodySchemaPath == null)
            {
                return null;
            }

            string schema = swagger["components"]?["schemas"]?[bodySchemaPath.Replace("#/components/schemas/", "")]?.ToString();

            if (schema == null)
            {
                return null;
            }

            //if the schema contains a $ref, replace it with the actual schema
            //This needs to be recursive to handle nested schemas
            while (schema.Contains("$ref"))
            {
                string refSchema = schema.Split("\"$ref\":")[1].Split("\"")[1];

                string refSchemaName = refSchema.Replace("#/components/schemas/", "");

                string refSchemaValue = swagger["components"]?["schemas"]?[refSchemaName]?.ToString();

                schema = schema.Replace($"\"$ref\": \"{refSchema}\"", refSchemaValue);
            }

            return schema;
        }

        public async Task<API> CreateCSXAPIFromOpenAPIDocument(string sourcePath, string environment = null, string projectName = null, AuthenticationTypes authenticationType = AuthenticationTypes.None)
        {
            OpenApiDocument document;

            if (sourcePath.StartsWith("http") || sourcePath.Contains(@"\\"))
            {
                document = await OpenApiDocument.FromUrlAsync(sourcePath);
            }
            else
            {
                FileInfo fileInfo = new FileInfo(sourcePath);

                if (fileInfo.Extension == ".json")
                {
                    document = await OpenApiDocument.FromFileAsync(sourcePath);
                }
                else if (fileInfo.Extension == ".yaml")
                {
                    document = await OpenApiYamlDocument.FromFileAsync(sourcePath);
                }
                else
                {
                    throw new Exception("Invalid file type. Please provide a valid OpenAPI document.");
                }
            }

            if (document == null)
            {
                throw new Exception("Invalid OpenAPI document. Please provide a valid OpenAPI document.");
            }

            API csxAPI = CreateCSXAPIFromOpenAPIDocument(document, environment, projectName, authenticationType).Result;

            return csxAPI;
        }

        public async Task<API> CreateCSXAPIFromOpenAPIDocument(OpenApiDocument document,
            string environment = null,
            string projectName = null,
            AuthenticationTypes authenticationType = AuthenticationTypes.None)
        {
            if (document == null)
            {
                throw new Exception("Invalid OpenAPI document. Please provide a valid OpenAPI document.");
            }

            jsonDoc = JObject.Parse(document.ToJson());

            API csxAPI = new()
            {
                RootFolderName = configurationRoot.GetSection("RootFolderName").Value ?? "CSXUG-Output",

                ProjectName = projectName ?? document.Info.Title.MakeFileSafeName().RemoveSpaces(),
                Environment = environment ?? "Development",
                SolutionFolder = projectName?.MakeFileSafeName().RemoveSpaces()
                    ?? document?.Info?.Title?.MakeFileSafeName().RemoveSpaces()
                    ?? configurationRoot?.GetSection("DefaultSolutionFolderName").Value,
                AuthenticationType = authenticationType,
                CoreProjectName = projectName + "Core",
                TestProjectName = projectName + "Tests",
                Tags = CreateCSXTagsAndEnpointsFromDocument(document),
                Namespace = projectName?.MakeFileSafeName() ?? document.Info.Title.MakeFileSafeName(),
                Server = document.Servers.FirstOrDefault()?.Url ?? "https://localhost:5000",
                XUnitExtensionProjectName = configurationRoot.GetSection("XUnitExtensionProjectName").Value ?? "XUnitExtensions"
            };

            return csxAPI;
        }

        public string? GetResponseBody(OpenApiOperationDescription operation)
        {
            try
            {
                //I need the operationID stored in a variable
                var operationId = operation.Operation.OperationId;

                string requestBodySchema = string.Empty;
                bool found = false;

                foreach (JProperty path in jsonDoc["paths"])
                {
                    foreach (JProperty method in path.Value)
                    {
                        if (method.First["operationId"].ToString() == operationId)
                        {
                            List<JProperty> responseProperties = method.Value["responses"].Value<JObject>().Properties().ToList();

                            foreach (JProperty? response in responseProperties)
                            {
                                if (response.Value["content"] == null)
                                {
                                    continue;
                                }

                                requestBodySchema = response.Value["content"]?["application/json"]?["schema"]?["$ref"]?.ToString()
                                          ?? response.Value["content"]?["application/json"]?["schema"]?["items"]?["$ref"]?.ToString()
                                          ?? null;

                                found = true;
                                break;
                            }

                            if (found)
                            {
                                break;
                            }
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }

                return requestBodySchema != null ? GetBodySchema(requestBodySchema, jsonDoc) : null;
            }
            catch (Exception)
            {
                //TODO: Add logging & Exception handling
                return null;
            }
        }
    }
}