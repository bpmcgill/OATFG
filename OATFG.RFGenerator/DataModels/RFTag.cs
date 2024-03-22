using OATFG.Core.DataModels;
using System.Collections.ObjectModel;

namespace OATFG.RFGenerator.DataModels
{
    internal class RFTag : Tag
    {
        /// <summary>
        /// This is the list of the unique path variables for this tag.
        /// </summary>
        public ObservableCollection<string>? DistinctEndpointPathVariables { get; set; }

        /// <summary>
        /// This is the list of the unique query variables for this tag.
        /// </summary>
        public ObservableCollection<string>? DistinctEndpointQueryVariables { get; set; }

        /// <summary>
        /// This is the name of the file that will be generated for the actions file. It is used for the Robot Framework.
        /// </summary>
        public string RobotTagActionFileName { get; set; }

        /// <summary>
        /// This is the name of the BDD file that will be generated. It is used for the Robot Framework.
        /// </summary>
        public string RobotTagBDDFileName { get; set; }

        /// <summary>
        /// This is the name of the file that will be generated for the data file. It is used for the Robot Framework.
        /// </summary>
        public string RobotTagDataFileName { get; set; }

        /// <summary>
        /// This is the name of the endpoints file that will be generated. It is used for the Robot Framework.
        /// </summary>
        public string RobotTagEndpointsFileName { get; set; }

        /// <summary>
        /// This is the name of the keywords file that will be generated. It is used for the Robot Framework.
        /// </summary>
        public string RobotTagKeywordsFileName { get; set; }
    }
}