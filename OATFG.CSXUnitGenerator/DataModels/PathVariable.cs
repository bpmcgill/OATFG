using OATFG.Core.DataModels;

namespace OATFG.CSXUnitGenerator.DataModels
{
    public class PathVariable : IParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
    }
}
