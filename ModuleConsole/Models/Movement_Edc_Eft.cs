using AuxUtils.Dialogs;
using BaseUtils.Mvvm;
using BaseUtils.SaveState;
using Doli.DoPE10;
using Globals.DataTypes;
using LabBase.Models;
using LabEdc.Models;
using LabEdc.ViewModels;
using LabLogger.Models;
using Simatic.Models;
using System;
using Vbloky.Translation;

namespace ModuleConsole.Models
{
	public partial class Movement : BaseViewModel
	{
		private EdcVM _edcVM;
		private ISaveState _iSaveState;

		private bool _isCalibrationMode { get; set; } = false;
		private string _calibModeStr => _isCalibrationMode ? Tx.T("Pro kalibraci") : "";
		private string _calibModeStrPrefix(string prefix) => _isCalibrationMode ? prefix + _calibModeStr : "";

		private EdcReferencing _edcReferencing => _edcVM.EdcBase.EdcReferencing;
		private EdcMovement _edcMovement => _edcVM.EdcBase.EdcMovement;

		public bool IsEdcMoving => _edcVM.EdcBase.IsMovingAny;
		public bool IsOn => _edcVM.EdcBase.IsOn;
		public double ActPosition => _edcVM.EdcBase.EdcData.GetPosition();
		public double ActLoad => _edcVM.EdcBase.EdcData.GetLoad();
		public bool IsReferenceOk => _edcVM.EdcBase.EdcReferencing.IsReferenceOk;
		public bool IsLimitAny => _edcVM.EdcBase.IsLimitAny;
		public bool IsWorkPosition => IsReferenceOk && Math.Abs(ActPosition - WorkPosPosition) <= 0.2;
		public bool IsBasePosition => IsReferenceOk && Math.Abs(ActPosition - BasePosPosition) <= 0.2;
		public bool IsCamFocusPosition => IsReferenceOk && Math.Abs(ActPosition - CamAbsFocusPosition) <= 0.2;

		public double TouchLoad => TestDef?.FPreload ?? 0;
		public double TouchSpeed => TestDef?.VPreload ?? 0;
		public double MainLoad => TestDef?.F_N ?? 0;
		public double MainLoad_Time => TestDef?.T ?? 0;
		public double MainLoad_Delay => TestDef?.TDelay ?? 0;
		//private double _mainLoadSpeed_N_s => MainLoad / MainLoad_Time;
		public double CamAbsFocusPosition
		{
			get
			{
				if (_isCalibrationMode)
					return (CamLastTouch - CamFocusCalibration) ?? 0;
				else
					return (CamLastTouch - CamFocus) ?? 0;
			}
		}
		public double CamAbsFocusPrePosition => CamAbsFocusPosition - Glb.Current.CamRelFocusPrePosition;


		public int TurnOn() => _edcVM.EdcBase.TurnOn();
		public int TurnOff() => _edcVM.EdcBase.TurnOff();
		public int SHalt() => _edcMovement.SHalt();
		public int TareLoad() => (int)_edcVM.EdcBase.SetTare_Load();


		#region --- regulace na dráhu ---
		public int FMove(DoPE.MOVE direction, double speed) => _edcMovement.FMove(direction, DoPE.CTRL.POS, speed);
		public int EdcReference()
		{
			//při referencování ještě není známá dráha h-supportu a navíc pohyb směruje vzhůru, proto se zde netestuje poloha h-suportu
			using var msg = new MsgWrap(Tx.T("Referencování vtisku"));
			if (msg.Ok) msg.Err = _edcReferencing.DoReferencing();
			return msg.Err;
		}
		public int EdcBasePosition(bool wait)
		{
			//na základní polohu vtisku je možno zajet kdykoliv
			using var msg = new MsgWrap(Tx.T("Základní poloha vtisku")/* + $": {BasePosSpeed:F1} mm/min {Tx.T("na")} {BasePosPosition:F3} mm"*/);
			if (!IsReferenceOk || IsLimitAny)
				msg.Err = EdcReference();

			if (!IsOn && msg.Ok)
				msg.Err = TurnOn();
			if (!IsBasePosition && msg.Ok)
				msg.Err = EdcPosExt(BasePosSpeed, BasePosPosition, MaxMovementLoad, wait);

			return msg.Err;
		}
		public int EdcWorkingPosition(bool wait)
		{
			//pro provedení pracovní polohy musí být h-support v poloze na
			using var msg = new MsgWrap(Tx.T("Pracovní poloha vtisku") /*+ $": {WorkPosSpeed:F1} mm/min {Tx.T("na")} {WorkPosPosition:F3} mm"*/);
			if (!IsIndentPos)
				msg.Err = ErrSim.NoOnIndentPosition.AsGlobalErr();
			if (msg.Ok) msg.Err = EdcPosExt(WorkPosSpeed, WorkPosPosition, MaxMovementLoad, wait);

			return msg.Err;
		}
		public int EdcPreloadPosition(bool wait)
		{
			double speed = Glb.CalcSpeed(TouchSpeed);
			using var msg = new MsgWrap(Tx.T("Předpětí") + $": {speed:F1} mm/min {TouchLoad:F1} N");
			if (!IsIndentPos)
				msg.Err = ErrSim.NoOnIndentPosition.AsGlobalErr();
			if (msg.Ok) msg.Err = msg.Err = _edcMovement.PosExt(DoPE.CTRL.POS, speed,
				DoPE.LIMITMODE.ABSOLUTE, MaxPosition, DoPE.CTRL.LOAD, TouchLoad, DoPE.DESTMODE.DEST_POSITION, wait);
			if (msg.Ok)
			{
				BaseUtils.Utils.SleepNoFreeze(20);
				double? bkTouchPos = CamLastTouch;
				CamLastTouch = ActPosition;
				DbSaveChanges(false);
				_log.Add(Tx.TC("Poloha doteku změněna") + $"{bkTouchPos:F3} -> {CamLastTouch:F3} {Tx.T("mm")}");
			}
			return msg.Err;
		}

		//provádí standardní Pos - směrem dolů, hlídá maximální polohu a maximální sílu.
		//Směrem nahoru se to nejspíš ani nerozjede, v tomto případě se použije standardní Pos
		public int EdcPosExt(double rawSpeed, double pos, double maxLoad, bool wait = true)
		{
			if (pos > MaxPosition)
			{
				_log.Add(Tx.TC("Přesun na polohu") + Tx.T("Příliš velká dráha") + $", {Tx.T("omezeno")} {pos:F3} -> {MaxPosition:F3}");
				pos = MaxPosition;
			}

			if (ActPosition < pos)
			{
				double speed = Glb.CalcSpeed(rawSpeed);
				return _edcMovement.PosExt(DoPE.CTRL.POS, speed,
					DoPE.LIMITMODE.ABSOLUTE, pos, DoPE.CTRL.LOAD, maxLoad, DoPE.DESTMODE.APPROACH, wait);
			}
			else
				return EdcPosToBaseDir(rawSpeed, pos, wait);
		}
		private int EdcPosToBaseDir(double rawSpeed, double pos, bool wait = true)
		{
			double speed = Glb.CalcSpeed(rawSpeed);
			return _edcMovement.Pos(DoPE.CTRL.POS, speed, pos, wait);
		}
		#endregion

		#region --- regulace na sílu ---
		public int EdcMainLoadPosition(bool wait) => EdcMainLoadPosition(MainLoad_Time, MainLoad, wait);
		public int EdcMainLoadPosition(double raw_loadTime, double mainLoad, bool wait)
		{
			double loadTime = Glb.CalcLoadTime(raw_loadTime);
			double loadSpeed = mainLoad / loadTime;
			using var msg = new MsgWrap(Tx.T("Hlavní zatížení") + $" {Tx.T("Síla")} {mainLoad:F1} N  {Tx.T("Čas nárůstu síly")} {loadTime:F1} {Tx.T("s")}  ({loadSpeed:F1} {Tx.T("N/s")})");
			if (!IsIndentPos)
				msg.Err = ErrSim.NoOnIndentPosition.AsGlobalErr();
			if (msg.Ok)
			{
				if (mainLoad > ActLoad)
					msg.Err = _edcMovement.PosExt(DoPE.CTRL.LOAD, loadSpeed,
						DoPE.LIMITMODE.ABSOLUTE, MaxPosition, DoPE.CTRL.LOAD, mainLoad, DoPE.DESTMODE.DEST_MAINTAIN, wait);
				else
					msg.Err = EdcUnload(raw_loadTime, mainLoad, wait);
			}

			return msg.Err;
		}
		public int EdcUnload(double raw_unloadTime, double load, bool wait = true)
		{
			double unloadTime = Glb.CalcLoadTime(raw_unloadTime);
			double unloadSpeed = (ActLoad - load) / unloadTime;
			return _edcMovement.Pos(DoPE.CTRL.LOAD, unloadSpeed, load, wait);
		}
		#endregion

		#region --- celá sekvence ---
		public int EdcDoIndent(bool combinedCommand)
		{
			bool wait = true;
			string baseMsg = Tx.T("Vtisk");
			using var msg = new MsgWrap(baseMsg);
			if (!IsIndentPos)
				msg.Err = ErrSim.NoOnIndentPosition.AsGlobalErr();
			//--- nájezd na předpětí a pak na hlavní zatížení
			if (msg.Ok)
				msg.Err = EdcMainLoadSekvence(MainLoad_Time, MainLoad, wait, combinedCommand);
			//--- výdrž na zadané síle
			if (msg.Ok)
			{
				msg.SetMessage(baseMsg + $": {Tx.T("Výdrž na síle")}  {MainLoad_Delay:F1} {Tx.T("s")}");
				BaseUtils.Utils.SleepNoFreeze_s(MainLoad_Delay);
			}
			//--- odlehčení na nulovou sílu - kvůli grafu
			if (msg.Ok)
				msg.Err = EdcUnload(UnloadTime_s, 0, wait);
			//--- odjezd na základní polohu
			if (msg.Ok)
				msg.Err = EdcBasePosition(wait);

			return msg.Err;
		}

		//toto je pouze pro provedení vtisku, musí startovat z WorkPosition
		public int EdcMainLoadSekvence(double raw_loadTime_s, double load, bool wait = true, bool combinedCommand = false)
		{
			short tanCombined = 0;
			double loadTime_s = Glb.CalcLoadTime(raw_loadTime_s);
			using var err = new MsgWrap(Tx.TC("Vtisk") + $"{Tx.T("Síla")} {load:F1} N  {Tx.T("Čas nárůstu síly")} {loadTime_s:F1} {Tx.T("s")}");
			if (err.Ok)
				err.Err = EdcWorkingPosition(wait);

			//kombinovaný příkaz - start
			if (err.Ok && combinedCommand)
				err.Err = _edcMovement.CmdBlockHeader(0);

			//1. příkaz - nájezd na dotek
			if (err.Ok)
				//pokud je aktivní combinedCommand, pak nepoužívat wait! - použije se až celkovém blockExecute
				err.Err = EdcPreloadPosition(wait && !combinedCommand);

			//2. příkaz - nájezd na koncovou sílu
			if (err.Ok)
				err.Err = EdcMainLoadPosition(wait && !combinedCommand);

			//kombinovaný příkaz - ukončení
			if (err.Ok && combinedCommand)
				err.Err = _edcMovement.CmdBlockExecute(true, tanCombined, true);

			return err.Err;
		}
		#endregion

		#region --- ostření kamery ---
		public int EdcToCamFocus()
		{
			//zaostření funguje kvůli bezpečnosti a aby se najíždělo vždy 
			//ze stejného směru tak, že se nejdřív najede o zadanou polohu
			//nad a pak se sjede na polohu ostření
			using var msg = new MsgWrap(Tx.T("Zaostření kamery") + _calibModeStrPrefix(". "));
			msg.Err = EdcCheckFocusValues(true);
			//nejdříve zajet na polohu trochu výš
			if (msg.Ok) msg.Err = EdcPosExt(CamFocusSpeed, CamAbsFocusPrePosition, CamSoftLimit ?? 0, true);
			//a teprve nyní doostřit kameru - vždy směrem shora dolů
			if (msg.Ok) msg.Err = EdcPosExt(CamFocusSpeed, CamAbsFocusPosition, CamSoftLimit ?? 0, true);
			return msg.Err;
		}

		internal void EdcSaveFocusPosition()
		{
			bool isCalib = _isCalibrationMode;
			using var msg = new MsgWrap(Tx.T("Uložení polohy zaostření kamery") + _calibModeStrPrefix(". "));
			msg.Err = EdcCheckFocusValues(false);
			if (!msg.Ok)
			{
				fMsg.Show(Tx.T("Při výpočtu polohy kamery došlo k chybě") + ":\n" +
					AppErrMsgs.GetItem(msg.Err), false);
				return;
			}
			double newIndent_Cam_Dist = (CamLastTouch ?? 0) - ActPosition;
			//ostření kamery musí být směrem nahoru!
			if (newIndent_Cam_Dist < 0)
			{
				fMsg.Show(Tx.T("Poloha zaostření kamery je níž než vtisk") + ".\n" + Tx.T("Hodnota nebude uložena") + ".", false);
				return;
			}

			double focus = isCalib ? CamFocusCalibration : (CamFocus ?? 0);
			if (fMsg.Show(Tx.T("Uložit aktuální polohu pro zaostření kamery?")
				+ $"{_calibModeStrPrefix("\n")}"
				+ $"\n{Tx.T("Poloha")} {ActPosition:F3} {Tx.T("mm")}"
				+ $"\n{Tx.T("Rozdíl")} {Tx.T("vtisk")} - {Tx.T("kamera")}"
				+ $"\n{focus:F3} -> {newIndent_Cam_Dist:F3} {Tx.T("mm")}", true))
			{
				if (isCalib)
					CamFocusCalibration = newIndent_Cam_Dist;
				else
					CamFocus = newIndent_Cam_Dist;
				DbSaveChanges(true);
				_iSaveState.Save();
			}
		}

		private int EdcCheckFocusValues(bool checkCamFocus)
		{
			if (CamSoftLimit == null)
				return AppErr.CamFocusNoSoftLimit.AsGlbErr();
			if (CamLastTouch == null)
				return AppErr.CamFocusNoIndent.AsGlbErr();

			//pokud se nastavuje focus kamery, tak tyto polohy nekontrolovat
			if (checkCamFocus)
			{
				if (_isCalibrationMode && CamFocusCalibration <= 0)
					return AppErr.CamNoFocuslPosition.AsGlbErr();
				if (!_isCalibrationMode && CamFocus == null)
					return AppErr.CamNoFocuslPosition.AsGlbErr();

				//Zadaná poloha kamery je příliš vysoko (nižší hodnota než základní poloha)
				if (CamAbsFocusPosition <= BasePosPosition)
					return ret(AppErr.CamFocusAboveBasePos);

				if (CamAbsFocusPosition > CamLastTouch)
					return ret(AppErr.CamFocusBelowIndent);
			}

			return 0;

			int ret(AppErr errCode)
			{
				_log.Add(Tx.TC("Kontrola polohy kamery") + $"{CamAbsFocusPosition:F3} {Tx.T("mm")}");
				return errCode.AsGlbErr();
			}
		}
		#endregion
	}
}
