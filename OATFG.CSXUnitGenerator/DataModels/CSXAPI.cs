using OATFG.Core.DataModels;
using System.Collections.ObjectModel;

namespace OATFG.CSXUnitGenerator.DataModels
{
    internal class CSXAPI : API
    {
        /// <summary>
        /// This is the folder that will be used for the C# Framework files.
        /// </summary>
        public string CSharpFrameworkFolder { get; set; }

        /// <summary>
        /// This is the namespace for the C# Framework.
        /// </summary>
        public string CSNamespace { get; set; }

        public ObservableCollection<CSXTag> CSXTags { get; set; }
    }
}