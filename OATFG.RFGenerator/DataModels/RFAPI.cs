using OATFG.Core.DataModels;
using System.Collections.ObjectModel;

namespace OATFG.RFGenerator.DataModels
{
    internal class RFAPI : API
    {
        /// <summary>
        /// This is the list of groups for the API.
        /// </summary>
        public ObservableCollection<RFTag> RFTags { get; set; }

        /// <summary>
        /// This is the file path of the common library for the Robot Framework.
        /// </summary>
        public string RobotCommonLibraryRelativeFilePath { get; set; }

        /// <summary>
        /// This is the folder that will be used for the Robot Framework files.
        /// </summary>
        public string RobotFrameworkFolder { get; set; }
    }
}