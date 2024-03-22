using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    //TODO: Move response code to its own class file and create interface for it

    public class Response : IResponse
    {
        /// <summary>
        /// The body of the response code provided by swagger. If it is null then there is no body.
        /// </summary>
        public string? Body { get; set; }

        /// <summary>
        /// The description of the response code provided by swagger.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The status code of the response code provided by swagger.
        /// </summary>
        public int StatusCode { get; set; }
    }
}