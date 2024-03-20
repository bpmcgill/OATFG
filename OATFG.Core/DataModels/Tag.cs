using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    public abstract class Tag : ITag
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
        /// This is the list of endpoints for this tag.
        /// </summary>
        public virtual ObservableCollection<IEndpoint> Endpoints { get; set; }

        /// <summary>
        /// This is the tag used for sorting the endpoints into groups.
        /// </summary>
        public string TagName { get; set; }
    }
}