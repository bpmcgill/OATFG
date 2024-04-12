namespace OATFG.Core.DataModels
{
    public interface IEndpoint
    {
        string EndpointPath { get; set; }
        string Method { get; set; }
        string OperationId { get; set; }
        string? RequestBody { get; set; }        
        string Summary { get; set; }
    }
}