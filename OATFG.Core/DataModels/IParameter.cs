namespace OATFG.Core.DataModels
{
    public interface IParameter
    {
        string Name { get; set; }
        string Type { get; set; }
        bool IsRequired { get; set; }
    }
}
