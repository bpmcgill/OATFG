using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    public interface IEndpoint
    {
        string EndpointPath { get; set; }
        string Method { get; set; }
        string OperationId { get; set; }
        ObservableCollection<string>? PathVariables { get; set; }
        ObservableCollection<string>? QueryVariables { get; set; }
        string? RequestBody { get; set; }
        ObservableCollection<IResponse> Responses { get; set; }
        string Summary { get; set; }
    }
}