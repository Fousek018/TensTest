using BaseUtils.Messages;
using BaseUtils.Mvvm;
using BaseUtils.SaveState;
using BaseUtils.UpdateView;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Doli.DoPE10;
using Globals.Hardness;
using ModuleConsole.Models;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace ModuleConsole.ViewModels
{
    [DataContract]
    public partial class ManualControllerVM : BaseViewModel
    {
        private ConsoleVM _consoleVM;
        private IHardnessService _iHardnessService;

        private Movement _movement => _consoleVM?.Movement;

        [DataMember][ObservableProperty] public partial double FastUpSpeed { get; set; } = 100;
        [DataMember][ObservableProperty] public partial double UpSpeed { get; set; } = 10;
        [DataMember][ObservableProperty] public partial double DownSpeed { get; set; } = 10;
        [DataMember][ObservableProperty] public partial double FastDownSpeed { get; set; } = 100;

        #region --- Pouze pomocné proměnné pro View ---
        public bool IsOn => _movement.IsOn;
        public double Load => _movement.ActLoad;
        public double Position => _movement.ActPosition;
        #endregion

        [JsonConstructor] protected ManualControllerVM() { }
        public ManualControllerVM(ISaveState iSaveState, IConsoleVM iConsoleVM, IHardnessService iHardnessService)
        {
            _consoleVM = iConsoleVM as ConsoleVM;
            _iHardnessService = iHardnessService;
            Messenger.Register<UpdateViewMessage>(this, (r, m) => UpdateView(m.Value));
            iSaveState.AddOrUpdate("ManualController", this);
        }

        [RelayCommand] private void TurnOff() => _movement.TurnOff();
        [RelayCommand] private void TurnOn() => _movement.TurnOn();
        [RelayCommand] private void TareLoad() => _movement.TareLoad();
        [RelayCommand] private void FastUp() => _movement.FMove(DoPE.MOVE.UP, FastUpSpeed);
        [RelayCommand] private void Up() => _movement.FMove(DoPE.MOVE.UP, UpSpeed);
        [RelayCommand] private void Stop() => _movement.SHalt();
        [RelayCommand] private void Down() => _movement.FMove(DoPE.MOVE.DOWN, DownSpeed);
        [RelayCommand] private void FastDown() => _movement.FMove(DoPE.MOVE.DOWN, FastDownSpeed);

        [RelayCommand]
        private void CommToCamera()
        {
            int err = _movement.CommToCamPosition();
            if (err == 0 && !_iHardnessService.IsLiveFeedOn)
                _iHardnessService.StartLiveImage();
        }
        [RelayCommand] private void CommToIndent() => _movement.CommToIndentPosition();
        [RelayCommand] private void SaveFocusPosition() => _movement.EdcSaveFocusPosition();


        #region --- UpdateView ---
        //[RelayCommand] private void Loaded() => Messenger.Register<UpdateViewMessage>(this, (r, m) => UpdateView(m.Value));
        //[RelayCommand] private void Unloaded() => Messenger.Unregister<UpdateViewMessage>(this);
        private void UpdateView(DisableUpdateView value)
        {
            if (value.IsEnabled)
            {
                OnPropertyChanged(nameof(IsOn));
                OnPropertyChanged(nameof(Load));
                OnPropertyChanged(nameof(Position));
            }
        }
        #endregion
    }
}
