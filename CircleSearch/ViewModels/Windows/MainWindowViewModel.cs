namespace CircleSearch.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private bool _isInitialized = false;

        private readonly INavigationService _navigationService;

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }

        [ObservableProperty]
        private string _applicationTitle = "CircleSearch";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems;

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems;

        public MainWindowViewModel(INavigationService navigationService)
        {
            NavigationHandle.NavigationService = navigationService;
            _navigationService = navigationService;
            _menuItems = NavigationHandle.GetNavCardsInNamespace("CircleSearch.Views.Pages");
            _footerMenuItems = NavigationHandle.GetNavCardsInNamespace("CircleSearch.Views.PagesBottom");

            LanguageBase.LanguageChanged += (lang) =>
            {
                ConfluxManager.cfsCircleSearchCore.Send("main-event", "OnLanguageChanged");
            };
        }
    }
}
