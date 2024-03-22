using NSwag;
using OATFG.Core.DataModels;
using OATFG.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATFG.Core
{
    public static class GenerateBaseAPI
    {
        // This is a method that will generate a base API from an OpenAPI document from NSwag
        public static async Task<OpenApiDocument> LoadOpenAPIDocumentFromJsonFile(FileInfo file)
        {
            return await OpenApiDocument.FromFileAsync(file.FullName);
        }

        public static async Task<OpenApiDocument> LoadOpenAPIDocumentFromJsonString(string json)
        {
            return await OpenApiDocument.FromJsonAsync(json);
        }

        public static async Task<OpenApiDocument> LoadOpenAPIDocumentFromURL(string url)
        {
            return await OpenApiDocument.FromUrlAsync(url);
        }

        public static async Task<OpenApiDocument> LoadOpenAPIDocumentFromURL(Uri url)
        {
            return await OpenApiDocument.FromUrlAsync(url.AbsoluteUri);
        }

        public static async Task<OpenApiDocument> LoadOpenAPIDocumentFromYamlFile(FileInfo file)
        {
            return await OpenApiYamlDocument.FromFileAsync(file.FullName);
        }

        public static async Task<API> GenerateAPI(string source)
        {
            OpenApiDocument document;

            // Check if the source is a URL, network location, or a file
            if (source.Contains("http") || source.Contains(@"\\"))
            {
                document = await LoadOpenAPIDocumentFromURL(source);
            }
            else
            {
                FileInfo file = new(source);

                if (file.Extension == ".yaml")
                {
                    document = await LoadOpenAPIDocumentFromYamlFile(file);
                }
                else
                {
                    document = await LoadOpenAPIDocumentFromJsonFile(file);
                }
            }

            if (document == null)
            {
                throw new Exception("Could not load OpenAPI document");
            }

            API api = new()
            {
                RootFolderName = "change to the root folder name",
                ProjectName = "change to the project name",
                Environment = "change to the environment",
                Server = document.Servers.FirstOrDefault().Url,
                Tags = GenerateTagsAndEndpoints(document),
            };

            return api;
        }

        private static ObservableCollection<ITag> GenerateTagsAndEndpoints(OpenApiDocument document)
        {
            ObservableCollection<ITag> tags = new();

            // The tags are stored in the document.operations which is the same as the paths

            foreach (var operation in document.Operations)
            {
                

                // Check if the tag already exists
                if (tags.Any(x => x.TagName == operation.Operation.Tags.FirstOrDefault()))
                {
                    // Add the endpoint to the existing tag
                    tags.First(x => x.TagName == operation.Operation.Tags.FirstOrDefault()).Endpoints.Add(GenerateEndpoint(operation, document));
                }
                else
                {
                    // Create a new tag and add the endpoint
                    tags.Add(new Tag
                    {
                        TagName = operation.Operation.Tags.FirstOrDefault(),
                        Endpoints = [GenerateEndpoint(operation, document)]
                    });
                }
            }

            return tags;
        }

        private static IEndpoint GenerateEndpoint(OpenApiOperationDescription operation, OpenApiDocument document)
        {
            return new Endpoint
            {
                EndpointPath = operation.Path,
                Method = operation.Method,
                Summary = operation.Operation.Summary,
                OperationId = operation.Operation.OperationId,
                PathVariables = GetPathvariablesAsync(operation).Result,
                QueryVariables = GetQueryVariablesAsync(operation).Result,
                RequestBody = GetRequestBodyParameters(operation).Result,
                Responses = GetResponses(operation, document).Result
            };
        }

        private static async Task<ObservableCollection<IResponse>> GetResponses(OpenApiOperationDescription operation, OpenApiDocument document)
        {
            ObservableCollection<IResponse> responses = [];

            foreach (var response in operation.Operation.Responses)
            {
                Console.WriteLine(response.Key);
                responses.Add(new Response
                {
                    StatusCode = int.Parse(response.Key),
                    Description = response.Value.Description,
                    Body = GetResponseBodyAsJson(operation, document).Result
                });
            }

            return responses ?? [];
        }

        private static async Task<string> GetResponseBodyAsJson(OpenApiOperationDescription operation, OpenApiDocument document)
        {
            //The document contains the response body schemas so we can use that to generate the response body as json
            StringBuilder sb = new();

            sb.AppendLine("{");

            foreach (var response in operation.Operation.Responses)
            {
                if (response.Value.Schema != null)
                {
                    // find the name of the schema
                    string bodyName = response.Value.ActualResponse.Content.First().Value.Schema.ActualSchema.ActualTypeSchema.ActualSchema.ActualProperties.First().Key;

                    if(bodyName == null)
                    {
                        continue;
                    }

                    Console.WriteLine(bodyName);
                    // find the schema in the document
                    var schema = document.Definitions.Where(def => def.Value.ActualProperties.Keys.FirstOrDefault() == bodyName).FirstOrDefault();

                    if (schema.Value != null)
                    {
                        sb.AppendLine("{");

                        foreach (var property in schema.Value.ActualProperties)
                        {
                            sb.Append($"\"{property.Key}\": \"{property.Value.Type}\"".PrependSpaces(2));

                            if (property.Key != schema.Value.ActualProperties.Last().Key)
                            {
                                sb.Append(",");
                            }
                        }
                        sb.AppendLine("}");
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            sb.AppendLine("}");

            if (sb.Length > 2)
            {
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }

        }

        private static async Task<ObservableCollection<PathVariable>> GetPathvariablesAsync(OpenApiOperationDescription operation)
        {
            if (operation.Operation.Parameters == null)
            {
                return [];
            }
            else
            {
                //The path variables are found in operation.Operation.Parameters
                ObservableCollection<PathVariable> pathVariables = new();

                foreach (var parameter in operation.Operation.Parameters)
                {
                    if (parameter.Kind == OpenApiParameterKind.Path)
                    {
                        pathVariables.Add(new PathVariable
                        {
                            Name = parameter.Name,
                            Type = parameter.Schema.Type.ToString()
                        });
                    }
                }

                return pathVariables;
            }
        }

        private static async Task<ObservableCollection<QueryVariable>> GetQueryVariablesAsync(OpenApiOperationDescription operation)
        {
            if (operation.Operation.Parameters == null)
            {
                return [];
            }
            else
            {
                //The query variables are found in operation.Operation.Parameters
                ObservableCollection<QueryVariable> queryVariables = new();

                foreach (var parameter in operation.Operation.Parameters)
                {
                    if (parameter.Kind == OpenApiParameterKind.Query)
                    {
                        queryVariables.Add(new QueryVariable
                        {
                            Name = parameter.Name,
                            Type = parameter.Schema.Type.ToString()
                        });
                    }
                }

                return queryVariables;
            }
        }

        private static async Task<string> GetRequestBodyParameters(OpenApiOperationDescription operation)
        {
            if (operation.Operation.RequestBody == null)
            {
                return "";
            }
            else
            {
                //The body parameters are found in operation.Operation.(list)ActualParameters.Schema.ActualSchema.(list)ActualProperties

                StringBuilder sb = new();

                sb.AppendLine("{");
                foreach (var actualParameter in operation.Operation.ActualParameters)
                {
                    for (int i = 0; i < actualParameter.ActualSchema.ActualProperties.Count; i++)
                    {
                        sb.Append($"\"{actualParameter.ActualSchema.ActualProperties.ElementAt(i).Key}\": \"{actualParameter.ActualSchema.ActualProperties.ElementAt(i).Value.Type}\"".PrependSpaces(2));

                        if (i != actualParameter.ActualSchema.ActualProperties.Count - 1)
                        {
                            sb.Append(",");
                        }
                    }
                }
                sb.AppendLine("}");

                return sb.ToString();
            }
        }

    }
}