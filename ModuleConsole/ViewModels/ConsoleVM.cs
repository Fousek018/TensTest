using AuxUtils.Dialogs;
using BaseUtils;
using BaseUtils.Messages;
using BaseUtils.Mvvm;
using BaseUtils.SaveState;
using BaseUtils.UpdateView;
using BaseUtils.WindowManagement;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Globals.DataTypes;
using LabBase.Actives;
using LabBase.IActives;
using LabBase.ILogging;
using LabBase.Messages;
using LabLogger.Models;
using ModuleConsole.Models;
using ModuleConsole.Views;
using ModuleDatabase.Models;
using ModuleOptions.ViewModels;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Vbloky.Translation;

namespace ModuleConsole.ViewModels
{
	[DataContract]
	public partial class ConsoleVM : BaseViewModel, IConsoleVM
	{
		private OptionsUserVM _optionsUserVM;
		private Logger _logger;
		public MachineStatus MachineStatus { get; set; }
		public ActiveUser ActiveUser { get; }
		public Movement Movement { get; }
		public TestDef TestDef => Movement?.TestDef;
		public Method Method => TestDef?.ID_MethodNavigation;

		public bool IsManualControllerVisible => _manualControllerWnd?.IsVisible == true;
		public string BtnShowHideManualCaption => IsManualControllerVisible ? Tx.T("Zavřít ruční ovládání") : Tx.T("Zobrazit ruční ovládání");


		#region --- výsledná síla - přepočet mezi N a kg - musí být přes property changed ---
		public double F_N
		{
			get => Method?.F_N ?? 0;
			set
			{
				if (Method != null)
				{
					Method.F_N = value;
					OnPropertyChanged(nameof(F_N));
					OnPropertyChanged(nameof(F_kgf));
				}
			}
		}
		public double F_kgf
		{
			get => Method?.F_kgf ?? 0;
			set
			{
				if (Method != null)
				{
					Method.F_kgf = value;
					OnPropertyChanged(nameof(F_N));
					OnPropertyChanged(nameof(F_kgf));
				}
			}
		}
		#endregion

		#region --- Edc / Eft ---
		public double ActPosition => Movement.ActPosition;
		public double ActLoad => Movement.ActLoad;
		public bool IsOn => Movement.IsOn;
		public bool IsReferenceOk => Movement.IsReferenceOk;

		public double BasePosSpeed { get => Movement.BasePosSpeed; set => Movement.BasePosSpeed = value; }
		public double BasePosPosition { get => Movement.BasePosPosition; set => Movement.BasePosPosition = value; }
		public double WorkPosSpeed { get => Movement.WorkPosSpeed; set => Movement.WorkPosSpeed = value; }
		public double MaxPosition { get => Movement.MaxPosition; set => Movement.MaxPosition = value; }
		public double UnloadTime_s { get => Movement.UnloadTime_s; set => Movement.UnloadTime_s = value; }
		public double? WorkPosPosition { get => Method?.WPos; set { if (Method != null) Method.WPos = value; } }

		public bool IsDbChanged => Movement?.DbContext?.ChangeTracker?.HasChanges() == true;
		private bool CanExecutDbCommands() => IsDbChanged;
		#endregion

		#region --- Simatic ---
		public int MachineSpeed_perc => Movement.SimaticComm.MachineSpeed_perc;
		public double StopperPos => Movement.StopperPos;
		public double MillingWidth => Movement.MillingWidth;
		public double MillingPosCorrection => Movement.MillingPosCorrection;

		public bool IsMillingPos => Movement.IsMillingPos;
		public bool IsIndentPos => Movement.IsIndentPos;
		public bool IsCameraPos => Movement.IsCameraPos;
		public bool IsLaserPos => Movement.IsLaserPos;
		public bool IsAutomatMode => Movement.IsAutomatMode;
		public bool IsPartInserted => Movement.SimaticComm.InPartInserted.Value;
		#endregion

		#region --- Kontrola kalibrace síly ---
		[DataMember] public double CalibF { get; set; } = 100;
		[DataMember] public double CalibT { get; set; } = 10;
		[DataMember] public double CalibTDelay { get; set; } = 10;
		#endregion

		[JsonConstructor] private ConsoleVM() { }
		public ConsoleVM(ISaveState iSaveState, IOptionsUserVM iOptionsUserVM, ILogger iLogger, IMachineStatus iMachineStatus, IActiveUser iActiveUser,
			IMovement iMovement)
		{
			_optionsUserVM = iOptionsUserVM as OptionsUserVM;
			_logger = iLogger as Logger;
			MachineStatus = iMachineStatus as MachineStatus;
			ActiveUser = iActiveUser as ActiveUser;
			Movement = iMovement as Movement;

			Messenger.Register<AfterTestDefSelectedMessage>(this, (r, m) =>
			{
				OnPropertyChanged(nameof(TestDef));
				OnPropertyChanged(nameof(Method));
				OnPropertyChanged(nameof(F_kgf));
				OnPropertyChanged(nameof(F_N));
			});
			Messenger.Register<MachineIsMovingChangedMessage>(this, (r, m) => IsMachineMoving = m.Value);
			iSaveState.Add("ConsoleVM", this);
		}

		[ObservableProperty]
		[NotifyPropertyChangedFor(nameof(IsMachineStopped))]
		//pro EDC
		[NotifyCanExecuteChangedFor(nameof(EdcReferenceCommand))]
		[NotifyCanExecuteChangedFor(nameof(EdcBasePositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(EdcWorkingPositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(EdcPreloadPositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(EdcMainLoadPositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(EdcDoIndentCommand))]
		[NotifyCanExecuteChangedFor(nameof(CmdCalibMainLoadCommand))]
		[NotifyCanExecuteChangedFor(nameof(CmdCalibIndentCommand))]
		//pro Simatic
		[NotifyCanExecuteChangedFor(nameof(VSupportUpCommand))]
		[NotifyCanExecuteChangedFor(nameof(VSupportDownCommand))]
		[NotifyCanExecuteChangedFor(nameof(LifterUpCommand))]
		[NotifyCanExecuteChangedFor(nameof(LifterDownCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommDoMillingCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommDoMillingAndIndentCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommDoToolChangeCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommToBasePositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommToCalibPositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommToMillingPositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommToIndentPositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommToCamPositionCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommMeasMillingWidthCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommRotatePartCommand))]
		[NotifyCanExecuteChangedFor(nameof(CommRotatePart_CorrIndentCommand))]

		public partial bool IsMachineMoving { get; set; }
		public bool IsMachineStopped => !IsMachineMoving;
		#region --- EDC / EFT ---
		[RelayCommand] private void TurnOn() => Movement.TurnOn();
		[RelayCommand] private void TurnOff() => Movement.TurnOff();
		[RelayCommand] private void TareLoad() => Movement.TareLoad();

		[RelayCommand(CanExecute = nameof(IsMachineStopped))]
		private void EdcReference()
		{
			if (fMsg.Show(Tx.T("Spustit referencování vtisku?"), true))
				Movement.EdcReference();
		}
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void EdcBasePosition() => Movement.EdcBasePosition(true);
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void EdcWorkingPosition() => Movement.EdcWorkingPosition(true);
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void EdcPreloadPosition() => Movement.EdcPreloadPosition(true);
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void EdcMainLoadPosition() => Movement.EdcMainLoadPosition(true);
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void EdcDoIndent() => Movement.EdcDoIndent(false);


		ManualControllerVM _manualControllerVM = null;
		ManualControllerWnd _manualControllerWnd = null;
		[RelayCommand]
		public void ShowHideManual()
		{
			if (!IsManualControllerVisible)
			{
				_manualControllerVM ??= Ioc.Default.GetService<ManualControllerVM>();
				_manualControllerWnd = WindowLocator.Current.Resolve(_manualControllerVM) as ManualControllerWnd;
				_manualControllerWnd.Show();
			}
			else
			{
				_manualControllerWnd?.Close();
				_manualControllerWnd = null;
				_manualControllerVM = null;
			}
		}

		[RelayCommand(CanExecute = nameof(IsMachineStopped))]
		private void CmdCalibMainLoad()
		{
			if (!fMsg.Show(Tx.T("Provést nájezd na zadanou sílu?")
				+ $"\n\n{Tx.T("Síla")} {CalibF:F1} {Tx.T("N")}"
				+ $"\n{Tx.T("Čas nárůstu síly")} {CalibT:F1} {Tx.T("s")}", true))
				return;
			CalibMainLoad();
		}
		[RelayCommand] private void CmdUnload() => Unload();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CmdCalibIndent() => CalibIndent();

		//[RelayCommand] private void CamFocus() => Movement.EdcToCamFocus();
		//[RelayCommand] private void CamMeasuring() => Movement.EdcDoCamMeasuring();
		#endregion

		#region  --- Simatic ---
		//--- Přítlak shora
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void VSupportUp() => Movement.VSupportUp(true);
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void VSupportDown() => Movement.VSupportDown(true);


		//--- podpěry dole pod měřeným dílem
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void LifterUp() => Movement.SimLifterUp(true);
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void LifterDown() => Movement.SimLifterDown(true);

		//--- sdružené příkazy
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommRotatePart() => Movement.CommRotatePart();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommRotatePart_CorrIndent() => Movement.CommRotatePart_CorrIndent();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommDoMilling() => Movement.CommDoMilling();
		[RelayCommand] private void CommStopMovement() => Movement.CommStopMovement();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommDoMillingAndIndent() => Movement.CommDoMillingAndIndent();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommDoControlMeasurement() => Movement.DoControlMeasurement();

		[RelayCommand(CanExecute = nameof(IsMachineStopped))]
		private void CommDoToolChange()
		{
			var vm = Ioc.Default.GetService<ToolChangeVM>();
			var wnd = WindowLocator.Current.Resolve(vm);
			wnd?.Show();
			while (wnd?.IsVisible == true)
				Utils.SleepNoFreeze(100);
		}

		//--- Horizontální suport - pozice - toto jsou sdružené pozice! ---
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommToBasePosition() => Movement.CommToBasePosition();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommToCalibPosition() => Movement.CommToCalibPosition();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommToMillingPosition() => Movement.CommToMillingPosition();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommToIndentPosition() => Movement.CommToIndentPosition();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommToCamPosition() => Movement.CommToCamPosition();
		[RelayCommand(CanExecute = nameof(IsMachineStopped))] private void CommMeasMillingWidth() => Movement.CommMeasMillingWidth();
		#endregion

		#region --- ukládání do databáze ---
		[RelayCommand(CanExecute = nameof(CanExecutDbCommands))] private void DbSave() => Movement.DbSaveChanges(true);
		[RelayCommand(CanExecute = nameof(CanExecutDbCommands))] private void DbCancel() => Movement.DbCancelChanges();
		[RelayCommand] private void DbRefresh() => fMsg.Show("Not implemented", false);

		#endregion

		#region --- pomocné funkce pro kalibraci síly ---
		private int CalibMainLoad()
		{
			using var msg = new MsgWrap(Tx.T("Nájezd na zadanou sílu"));

			//1. předpětí
			msg.Err = Movement.EdcPreloadPosition(true);
			//2. nájezd na sílu
			if (msg.Ok)
				msg.Err = Movement.EdcMainLoadPosition(CalibT, CalibF, true);

			return msg.Err;
		}
		private int Unload()
		{
			using var msg = new MsgWrap(Tx.T("Odlehčení na nulovou sílu"));
			msg.Err = Movement.EdcUnload(CalibT, 0, true);
			return msg.Err;
		}
		private int CalibIndent()
		{
			string baseMsg = Tx.T("Provedení vtisku");
			using var msg = new MsgWrap(baseMsg);
			if (!fMsg.Show(Tx.TC("Provést zkušební vtisk se zadanými hodnotami?")
				+ $"\n\n{Tx.T("Síla")} {CalibF:F1} {Tx.T("N")}"
				+ $"\n{Tx.T("Čas nárůstu síly")} {CalibT:F1} {Tx.T("s")}"
				+ $"\n{Tx.T("Výdrž na síle")} {CalibTDelay:F1} {Tx.T("s")}", true))
				return msg.Err;

			msg.Err = CalibMainLoad();

			if (msg.Ok)
			{
				msg.SetMessage(baseMsg + $": {Tx.T("Výdrž na síle")}  {CalibT:F1} {Tx.T("s")}");
				BaseUtils.Utils.SleepNoFreeze_s(CalibT);

				msg.Err = Unload();
			}
			return msg.Err;
		}
		#endregion

		#region --- UpdateView ---
		[RelayCommand] private void Loaded() => Messenger.Register<UpdateViewMessage>(this, (r, m) => UpdateView(m.Value));
		[RelayCommand] private void Unloaded() => Messenger.Unregister<UpdateViewMessage>(this);
		private void UpdateView(DisableUpdateView value)
		{
			if (value.IsEnabled)
			{
				OnPropertyChanged(nameof(ActPosition));
				OnPropertyChanged(nameof(ActLoad));
				OnPropertyChanged(nameof(IsOn));
				OnPropertyChanged(nameof(IsReferenceOk));

				OnPropertyChanged(nameof(IsMillingPos));
				OnPropertyChanged(nameof(IsIndentPos));
				OnPropertyChanged(nameof(IsCameraPos));
				OnPropertyChanged(nameof(IsLaserPos));
				OnPropertyChanged(nameof(IsAutomatMode));
				OnPropertyChanged(nameof(IsPartInserted));

				OnPropertyChanged(nameof(MachineSpeed_perc));
				OnPropertyChanged(nameof(BtnShowHideManualCaption));

				if (Glb.Current.IsHwRotationUsed)
				{
					OnPropertyChanged(nameof(MillingWidth));
					OnPropertyChanged(nameof(MillingPosCorrection));
				}
				else
				{
					OnPropertyChanged(nameof(StopperPos));
				}

				OnPropertyChanged(nameof(IsDbChanged));
				DbSaveCommand.NotifyCanExecuteChanged();
				dbCancelCommand.NotifyCanExecuteChanged();
			}
		}
		#endregion
	}
}
