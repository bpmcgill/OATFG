using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    public interface ITag
    {
        ObservableCollection<string>? DistinctEndpointPathVariables { get; set; }
        ObservableCollection<string>? DistinctEndpointQueryVariables { get; set; }
        ObservableCollection<IEndpoint> Endpoints { get; set; }
        string TagName { get; set; }
    }
}