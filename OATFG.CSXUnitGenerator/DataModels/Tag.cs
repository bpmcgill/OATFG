using OATFG.Core.DataModels;
using System.Collections.ObjectModel;

namespace OATFG.CSXUnitGenerator.DataModels
{
    public class Tag : ITag
    {
        /// <summary>
        /// This is the class and file name used for the endpoint file. It is used for the C# Framework.
        /// </summary>
        public string ClassAndFileName { get; set; }

        /// <summary>
        /// This is the collection of endpoints for the C# Framework.
        /// </summary>
        public ObservableCollection<Endpoint> Endpoints { get; set; }

        /// <summary>
        /// This is the tag used for sorting the endpoints into groups.
        /// </summary>
        public string TagName { get; set; }
    }
}