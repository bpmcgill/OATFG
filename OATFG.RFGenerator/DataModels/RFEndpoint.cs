using OATFG.Core.DataModels;

namespace OATFG.RFGenerator.DataModels
{
    internal class RFEndpoint : Endpoint
    {
        /// <summary>
        /// This is the name of the body tag for the endpoint. It is used by the Robot Framework.
        /// </summary>
        public string? BodyTag { get; set; }

        /// <summary>
        /// This is the name of the method that will be generated for the endpoint for the Robot Framework in the BDD File.
        /// </summary>
        public string GeneratedBDDKeyword { get; set; }

        /// <summary>
        /// This is the name of the method that will be generated for the endpoint for the Robot Framework in the endpoints file.
        /// </summary>
        public string GeneratedEndpointName { get; set; }

        /// <summary>
        /// This is the name of the method that will be generated for the endpoint for the Robot Framework in the keywords file.
        /// </summary>
        public string GeneratedKeyword { get; set; }

        /// <summary>
        /// this is the name of the path variable for the endpoint. It is used by the Robot Framework.
        /// </summary>
        public string RobotPathVariableName { get; set; }
    }
}