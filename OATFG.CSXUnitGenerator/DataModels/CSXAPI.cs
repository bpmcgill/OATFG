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

        /// <summary>
        /// This is the collection of tags that will be used for the C# Framework.
        /// </summary>
        public ObservableCollection<CSXTag> CSXTags { get; set; }

        /// <summary>
        /// This is the name of the XUnit Extension project.
        /// </summary>
        public string XUnitExtensionProjectName { get; internal set; }

        /// <summary>
        /// This is the name of the Test project where all the tests will be.
        /// </summary>
        public string TestProjectName { get; internal set; }

        /// <summary>
        /// This is the name of the Core project where all the code will be.
        /// </summary>
        public string CoreProjectName { get; internal set; }
    }
}