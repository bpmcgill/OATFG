using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    public class Tag : ITag
    {
        /// <summary>
        /// This is the list of endpoints for this tag.
        /// </summary>
        public virtual ObservableCollection<IEndpoint>? Endpoints { get; set; }

        /// <summary>
        /// This is the tag used for sorting the endpoints into groups.
        /// </summary>
        public string TagName { get; set; }
    }
}