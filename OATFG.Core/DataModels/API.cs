using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    public abstract class API : IAPI
    {
        /// <summary>
        /// This is the authentication method used for the API.
        /// </summary>
        public AuthenticationTypes AuthenticationType { get; set; }

        /// <summary>
        /// This is the environment for the API.
        /// </summary>
        public string Environment { get; set; }

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
        /// This is the list of groups for the API.
        /// </summary>
        public virtual ObservableCollection<ITag> Tags { get; set; }
    }
}