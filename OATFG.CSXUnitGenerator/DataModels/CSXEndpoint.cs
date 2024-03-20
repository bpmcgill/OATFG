using OATFG.Core.DataModels;

namespace OATFG.CSXUnitGenerator.DataModels
{
    internal class CSXEndpoint : Endpoint
    {
        /// <summary>
        /// This is the name of the method that will be generated for the endpoint for the C# Framework.
        /// </summary>
        public string GeneratedMethodName { get; set; }

        /// <summary>
        /// This is the name of the private field that will be generated for the endpoint for the C# Framework.
        /// </summary>
        public string GeneratedPrivateFieldName { get; set; }
    }
}