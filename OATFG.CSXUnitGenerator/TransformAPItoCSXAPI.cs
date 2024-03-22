using OATFG.Core.DataModels;
using OATFG.Core.Utilities;
using OATFG.CSXUnitGenerator.DataModels;
using System.Collections.ObjectModel;

namespace OATFG.CSXUnitGenerator
{
    public static class TransformAPItoCSXAPI
    {
        private static ObservableCollection<CSXTag> TransformTags(API api)
        {
            var csxTags = new ObservableCollection<CSXTag>();

            foreach (var tag in api.Tags)
            {
                CSXTag csxTag = TransformTag(tag);

                csxTags.Add(csxTag);
            }

            return csxTags;
        }

        public static CSXAPI TransformAPI(API api)
        {
            var csxAPI = new CSXAPI
            {
                RootFolderName = api.RootFolderName,
                SolutionFolder = Path.Combine(api.RootFolderName, api.ProjectName),
                ProjectName = api.ProjectName,
                CoreProjectName = api.ProjectName + "-Core",
                TestProjectName = api.ProjectName + "-Tests",
                XUnitExtensionProjectName = "XUnitExtensions",
                CSXTags = TransformTags(api),
            };

            return csxAPI;
        }

        public static CSXEndpoint TransformEndpoint(IEndpoint endpoint)
        {
            return new CSXEndpoint
            {
                GeneratedMethodName = endpoint.Summary.MakePascalCase(),
                GeneratedPrivateFieldName = endpoint.Summary.MakePascalCase().LowerCaseFirstLetter(),
                EndpointPath = endpoint.EndpointPath,
                Method = endpoint.Method,
                RequestBody = endpoint.RequestBody,
                PathVariables = endpoint.PathVariables,
                QueryVariables = endpoint.QueryVariables,
                OperationId = endpoint.OperationId,
                Summary = endpoint.Summary,
                Responses = endpoint.Responses
            };
        }

        public static ObservableCollection<CSXEndpoint> TransformEndpoints(ObservableCollection<IEndpoint>? endpoints)
        {
            ObservableCollection<CSXEndpoint> csxEndpoints = [];

            if (endpoints != null)
            {
                foreach (var endpoint in endpoints)
                {
                    CSXEndpoint csxEndpoint = TransformEndpoint(endpoint);

                    csxEndpoints.Add(csxEndpoint);
                }
            }

            return csxEndpoints;
        }

        public static CSXTag TransformTag(ITag tag)
        {
            return new CSXTag
            {
                TagName = tag.TagName,
                ClassAndFileName = tag.TagName.MakePascalCase(),
                DistinctEndpointPathVariables = tag.DistinctEndpointPathVariables,
                DistinctEndpointQueryVariables = tag.DistinctEndpointQueryVariables,
                CSXEndpoints = TransformEndpoints(tag.Endpoints),
                Endpoints = tag.Endpoints
            };
        }
    }
}