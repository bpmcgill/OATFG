using OATFG.Core.DataModels;

namespace OATFG.CSXUnitGenerator.DataModels
{
    public class CSXTag : Tag
    {
        /// <summary>
        /// This is the class and file name used for the endpoint file. It is used for the C# Framework.
        /// </summary>
        public string ClassAndFileName { get; set; }
    }
}