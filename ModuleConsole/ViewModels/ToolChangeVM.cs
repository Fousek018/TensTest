using AuxUtils.Dialogs;
using BaseUtils;
using BaseUtils.Messages;
using BaseUtils.Mvvm;
using BaseUtils.SaveState;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Globals.Hardness;
using LabBase.IActives;
using LabBase.Messages;
using LabLogger.Models;
using MathNet.Numerics;
using ModuleConsole.Models;
using Newtonsoft.Json;
using Simatic.Models;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using Vbloky.Translation;

namespace ModuleConsole.ViewModels
{
	interface IToolChangeVM { }
	[DataContract]
	public partial class ToolChangeVM : BaseViewModel, IToolChangeVM
	{
		private SimaticComm _simaticComm;
		private Movement _movement;

		public IActiveUser ActiveUser { get; }
		[ObservableProperty] public partial BitmapSource? ImageSource { get; set; }

		private IHardnessService _iHardnesService;

		[DataMember][ObservableProperty] public partial double AddStep { get; set; } = 0.01;
		[DataMember][ObservableProperty] public partial double MinRange { get; set; } = -1;
		[DataMember][ObservableProperty] public partial double MaxRange { get; set; } = 2;
		[DataMember][ObservableProperty] public partial double BallChangePosition { get; set; } = 0.0;

		public double MillingPosRel
		{
			get => _simaticComm.St_MillingRelDepth.Value.Round(3);
			set
			{
				double newPos = value.CheckMinMax(MinRange, MaxRange);
				_simaticComm.St_MillingRelDepth.Value = newPos;
				OnPropertyChanged(nameof(MillingPosRel));
			}
		}

		[JsonConstructor] protected ToolChangeVM() { }
		public ToolChangeVM(ISimaticComm iSimaticComm, IMovement iMovement, IActiveUser iActiveUser, IHardnessService iHardnessService, ISaveState iSaveState)
		{
			_simaticComm = iSimaticComm as SimaticComm;
			_movement = iMovement as Movement;
			ActiveUser = iActiveUser;
			_iHardnesService = iHardnessService;

			iSaveState.AddOrUpdate("ToolChangeVM", this);
		}

		[RelayCommand] private void Add() { Add(AddStep); addPosition = false; }
		[RelayCommand] private void Sub() { Add(-AddStep); addPosition = false; }
		[RelayCommand] private void Zeroing() { MillingPosRel = 0; addPosition = false; }


		//pokud někdo provede změnu v nastavení výšky pomoci šipek, pak při prvním spuštění frézování -> kamera nepřidávat polohu
		private bool addPosition = false;
		[RelayCommand]
		private void DoMillingAndCam()
		{
			_iHardnesService.StopLiveImage();
			ImageSource = null;
			using var msg = new MsgWrap(Tx.T("Frézování a přejezd na kameru"));
			//automatické přidávání chtěl Jirka zrušit
			//if (addPosition)
			//	Add();
			if (msg.Ok) msg.Err = _movement.CommDoMillingAndCam();
			if (msg.Ok) msg.Err = _movement.CommToCamPosition();

			msg.Err = _iHardnesService.GrabImage(out BitmapSource? img, false);
			ImageSource = img;

			addPosition = msg.Ok;
		}

		[RelayCommand]
		private void ToToolChangePos()
		{
			//--- dotaz zda opravdu najet na polohu pro výměnu nástroje
			if (!fMsg.Show(Tx.T("Najet na polohu pro výměnu nástroje?"), true))
				return;
			//--- info během nájezdu na polohu pro výměnu nástroje
			var wnd = fMsg.ShowNoModal(Tx.T("Nájezd na polohu pro výměnu nástroje"), false, false);
			_movement.CommDoToolChange();
			wnd.Close();
			//--- upozornění na bezpečnost
			fMsg.ShowAlarm(Tx.T("Režim výměny nástroje")
				+ "\n"
				+ "\n" + Tx.T("Během výměny nástroje vypněte pohon")
				+ "\n"
				+ "\n" + Tx.T("Dokončení výměny nástroje potvrďte tlačítkem") + " " + Tx.T("Ok")
				, false);

			Zeroing();
			Messenger.Send<ChangedToolMessage>();
		}

		[RelayCommand]
		private void ToBallChangePos()
		{
			if (!fMsg.Show(Tx.T("Nájezd na polohu pro výměnu kuličky"), true))
				return;

			var wnd = fMsg.ShowNoModal(Tx.T("Nájezd na polohu pro výměnu kuličky"), false, false);
			_movement.CommDoBallChange(BallChangePosition);
			wnd.Close();

			fMsg.ShowAlarm(Tx.T("Režim výměny kuličky")
				+ "\n"
				+ "\n" + Tx.T("Během výměny kuličky vypněte pohon")
				+ "\n"
				+ "\n" + Tx.T("Dokončení výměny kuličky potvrďte tlačítkem") + " " + Tx.T("Ok")
				, false);
		}

		[RelayCommand] private void VSupportDown() => _movement.VSupportDown(true);
		[RelayCommand] private void VSupportUp() => _movement.VSupportUp(true);

		private void Add(double delta) => MillingPosRel += delta;
	}
}
