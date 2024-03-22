using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    public interface ITag
    {
        ObservableCollection<IEndpoint>? Endpoints { get; set; }
        string TagName { get; set; }
    }
}