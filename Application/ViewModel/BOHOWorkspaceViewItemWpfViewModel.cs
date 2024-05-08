using BOHO.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BOHO.Application.ViewModel
{
    public partial class BOHOWorkspaceViewItemWpfViewModel : ObservableObject
    {
        private string _homeUrl;
        public string HomeUrl
        {
            get => _homeUrl;
            set => SetProperty(ref _homeUrl, value);
        }

        public BOHOWorkspaceViewItemWpfViewModel(BOHOConfiguration configuration)
        {
            HomeUrl = $"http://{configuration.IP}:{configuration.WebPort}";
        }
    }
}
