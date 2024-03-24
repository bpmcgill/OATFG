using System.Collections.ObjectModel;

namespace OATFG.Core.DataModels
{
    public interface IAPI
    {
        AuthenticationTypes AuthenticationType { get; set; }
        string Environment { get; set; }
        string ProjectName { get; set; }
        string RootFolderName { get; set; }
        string Server { get; set; }        
    }
}