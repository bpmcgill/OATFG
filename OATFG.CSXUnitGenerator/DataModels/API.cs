using OATFG.Core.DataModels;
using System.Collections.ObjectModel;

namespace OATFG.CSXUnitGenerator.DataModels
{
    public class API : IAPI
    {
        /// <summary>
        /// This is the authentication method used for the API.
        /// </summary>
        public AuthenticationTypes AuthenticationType { get; set; }

        /// <summary>
        /// This is the name of the Core project where all the code will be.
        /// </summary>
        public string CoreProjectName { get; internal set; }

        ///// <summary>
        ///// This is the collection of tags that will be used for the C# Framework.
        ///// </summary>
        //public ObservableCollection<Tag> Tags { get; set; }

        /// <summary>
        /// This is the collection of endpoints for the C# Framework.
        /// </summary>
        public ObservableCollection<Endpoint> Endpoints { get; set; }


        /// <summary>
        /// This is the environment for the API.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// This is the namespace for the C# Framework.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// This is the name of the project.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// This is the name of the folder that will be generated for the files
        /// </summary>
        public string RootFolderName { get; set; }

        /// <summary>
        /// This is the base URL for the API.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// This is the folder that will be used for the C# Framework files.
        /// </summary>
        public string SolutionFolder { get; set; }

        /// <summary>
        /// This is the name of the Test project where all the tests will be.
        /// </summary>
        public string TestProjectName { get; internal set; }

        /// <summary>
        /// This is the name of the XUnit Extension project.
        /// </summary>
        public string XUnitExtensionProjectName { get; internal set; }
    }
}