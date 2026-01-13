using BaseUtils.Messages;
using BaseUtils.Mvvm;
using BaseUtils.UpdateView;
using Globals.Messages;
using LabBase.ILogging;
using LabBase.Messages;
using ModuleOptions.ViewModels;
using Simatic.Interfaces;
using Simatic.Models;
using Simatic.ViewModels;

namespace ModuleConsole.Models
{
	public class MachineStatus : BaseViewModel, IMachineStatus
	{
		private OptionsUserVM _optionsUserVM;
		private Movement _movement;
		private ILogger _iLogger;
		private SimaticVM _simaticVM;
		private DisableUpdateView _disableUpdateView;

		private SimaticComm Comm => _simaticVM.Comm;


		public bool IsPcTestRunning
		{
			get => field;
			set
			{
				if (SetProperty(ref field, value))
				{
					IsTestRunning = IsPcTestRunning || IsSimaticTestRunning;
					Comm.OutTestRunning.Value = value;
				}
			}
		}
		public bool IsSimaticTestRunning
		{
			get; set
			{
				if (SetProperty(ref field, value))
					IsTestRunning = IsPcTestRunning || IsSimaticTestRunning;
			}
		}

		public bool IsTestRunning
		{
			get;
			set
			{
				if (SetProperty(ref field, value))
				{
					OnPropertyChanged(nameof(IsMachineMoving));
					_disableUpdateView.EnableDisable(!field);

					//hlášení se odešle pouze pokud došlo k celkové změně isTestRunning
					Messenger.Send(new OnTestRunningChangedMessage(IsTestRunning));
				}
			}
		}
		public bool IsEdcMoving => _movement.IsEdcMoving;
		public bool IsSimaticMoving => Comm.InMovementActive.Value;

		public bool IsMachineMoving => IsEdcMoving || IsTestRunning || IsSimaticMoving;

		public bool IsCalibrationMode
		{
			get => Comm.OutCalibrationMode.Value;
			set
			{
				bool bk = Comm.OutCalibrationMode.Value;
				Comm.OutCalibrationMode.Value = value;
				if (bk != value)
					Messenger.Send(new CalibrationModeChangedMessage(value));
			}
		}

		public MachineStatus(IOptionsUserVM iOptionsUserVM, IMovement iMovement, ILogger iLogger, ISimaticVM iSimaticVM, IDisableUpdateView iDisableUpdateView)
		{
			_optionsUserVM = iOptionsUserVM as OptionsUserVM;
			_movement = iMovement as Movement;
			_iLogger = iLogger;
			_simaticVM = iSimaticVM as SimaticVM;
			_disableUpdateView = iDisableUpdateView as DisableUpdateView;

			//!nastavení default bitů je v rutině v CalcHardness
			Messenger.Register<ErrConfirm_BeginMessage>(this, (r, m) => IsPcTestRunning = false);
			Messenger.Register<ErrConfirm_EndMessage>(this, (r, m) => ResetSimaticErrOutputs());
			Messenger.Register<ErrPoolMessage>(this, (r, m) => UpdateStatus());
			Messenger.Register<SimaticTestRunningMessage>(this, (r, m) => IsSimaticTestRunning = m.Value);
		}


		private void ResetSimaticErrOutputs()
		{
			Comm.OutError.Value = false;
		}



		private BoolWatcher _isMoving = new();
		//--- Řídící rutina průběhu zkoušky, voláno z ErrPool
		//Popis je v modulu Operation.Models.CalcHardness
		public void UpdateStatus()
		{
			bool error = _iLogger.IsError;
			bool alarm = _iLogger.IsAlarm;

			//pokud je aktivní reset chyby, neposílat jej do simatiku
			if (!_iLogger.IsErrConfirmActive)
				Comm.OutError.Value = error;
			Comm.OutOnBasePos.Value = _movement.IsBasePosition;

			_isMoving.LastValue = IsMachineMoving;
			if (_isMoving.IsAnySlope)
			{
				Messenger.Send(new MachineIsMovingChangedMessage(_isMoving.IsPosSlope));
				_isMoving.ClearSlopes();
			}
		}
	}
}
