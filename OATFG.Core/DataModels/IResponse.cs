namespace OATFG.Core.DataModels
{
    public interface IResponse
    {
        string? Body { get; set; }
        string Description { get; set; }
        int StatusCode { get; set; }
    }
}