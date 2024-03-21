using OATFG.Core.DataModels;
using System.Collections.ObjectModel;

namespace OATFG.CSXUnitGenerator.DataModels
{
    public class CSXAPI : API
    {
        /// <summary>
        /// This is the folder that will be used for the C# Framework files.
        /// </summary>
        public string SolutionFolder { get; set; }

        /// <summary>
        /// This is the namespace for the C# Framework.
        /// </summary>
        public string Namespace { get; set; }

        public ObservableCollection<CSXTag> CSXTags { get; set; }
    }
}