using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    public interface IResponse
    {
        string? Body { get; set; }

        /// <summary>
        /// The description of the response code provided by swagger.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// The status code of the response code provided by swagger.
        /// </summary>
        int StatusCode { get; set; }
    }
}