using BaseUtils.SaveState;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using LabBase.ILogging;
using LabEft.Interfaces;
using Navigation.Models;
using Navigation.Services;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace TensTest
{
    public interface IMainVM
    {
    }

    [DataContract]
    public partial class MainVM : ObservableObject, IMainVM
    {
        private ILogger _logger;
        [JsonConstructor] private MainVM() { }
        public MainVM(ISaveState saveState, NavigationFactory navigationFactory, ILogger iLogger)
        {
            NavigationService = navigationFactory.GetNavigationService(this);
            _logger = iLogger;

            var eft = Ioc.Default.GetService<IEftBase>();

            saveState.Add("EftBase", eft);
            saveState.Add("MainVM", this);
        }

        #region ----- Navigation -----
        public INavigationService NavigationService { get; }

        [RelayCommand]
        private void Navigate(string navigatePath)
        {
            NavigationService.NavigateAbs(navigatePath);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }
        #endregion
    }
}
