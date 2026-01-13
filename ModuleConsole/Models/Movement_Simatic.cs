using AuxUtils.Dialogs;
using LabBase.DataStructures;
using LabSimatic.Utils;
using ModuleDatabase.Models;
using Simatic.Models;
using Simatic.ViewModels;
using Vbloky.Translation;

namespace ModuleConsole.Models
{
	public partial class Movement
	{
		private SimaticVM _simaticVM;
		public SimaticComm SimaticComm => _simaticVM.Comm;
		private SimaticByte _simErr => SimaticComm.InErrorAlarmNumber;

		public bool IsMillingPos => SimaticComm.InIsMillingPosition.Value;
		public bool IsIndentPos => SimaticComm.InIsIndentPosition.Value;
		public bool IsCameraPos => SimaticComm.InIsCameraPosition.Value;
		public bool IsLaserPos => SimaticComm.InIsOnBasePosition.Value;
		public bool IsAutomatMode => SimaticComm.InIsAutomatMode.Value;
		public double StopperPos => SimaticComm.InStopperPos.Value;
		public double MillingWidth => SimaticComm.InMillingWidth.Value;
		public double MillingPosCorrection => SimaticComm.InMillingPosCorrection.Value;

		public int IndCamPosOffset
		{
			get => SimaticComm.OutIndentAndCamPositionOffset.Value;
			set => SimaticComm.OutIndentAndCamPositionOffset.Value = value;
		}

		public int Sim_Reference()
		{
			fMsg.Show("Není implementováno", false);
			return 0;
		}


		//--- přítlak shora
		public int VSupportUp(bool wait) => SimaticComm.CmdVSupportRelease.Execute(wait, _simErr);
		public int VSupportDown(bool wait) => SimaticComm.CmdVSupportPress.Execute(wait, _simErr);

		//--- pozice
		public int SimSendBtnBasePos() => SimaticComm.CmdSendBasePosButton.Execute(true, _simErr);
		public int SimHSupportToBasePosition(bool wait) => SimaticComm.CmdHSupportBasePos.Execute(wait, _simErr);
		public int SimToCalibPosition(bool wait) => SimaticComm.CmdCalibrationPos.Execute(wait, _simErr);
		public int SimHSupportToMillingPosition(bool wait) => SimaticComm.CmdHSupportMillingPos.Execute(wait, _simErr);
		public int SimHSupportToIndentPosition(bool wait) => SimaticComm.CmdHSupportIndentPos.Execute(wait, _simErr);
		public int SimHSupportToCamPosition(bool wait) => SimaticComm.CmdHSupportCameraPos.Execute(wait, _simErr);
		public int SimMeasMillingWidth(bool wait) => SimaticComm.Cmd_MeasureMillingWidth.Execute(wait, _simErr);

		//--- podpěry pod měřeným dílem
		public int SimLifterUp(bool wait) => SimaticComm.CmdLifterUp.Execute(wait, _simErr);
		public int SimLifterDown(bool wait) => SimaticComm.CmdLifterDown.Execute(wait, _simErr);

		//--- sdružené příkazy
		public int SimDoMilling(bool wait) => SimaticComm.CmdDoMilling.Execute(wait, _simErr);
		public int SimDoToolChange(bool wait) => SimaticComm.Cmd_ChangeTool.Execute(wait, _simErr);
		public int SimDoBallChange(bool wait) => SimaticComm.Cmd_ChangeBall.Execute(wait, _simErr);
		public int SimDoPartRotation(bool wait) => SimaticComm.Cmd_RotatePart.Execute(wait, _simErr);
		public int SimDoPartRotation_CorrIndent(bool wait) => SimaticComm.Cmd_RotatePart_CorrIndent.Execute(wait, _simErr);
		//zastavuje i EDC!
		public int SimStopMovement(bool wait)
		{
			_edcMovement.SHalt();
			return SimaticComm.CmdStop.Execute(wait, _simErr);
		}
		public int SimDoMillingAndIndent(bool wait) => SimaticComm.CmdMillingAndIndent.Execute(wait, _simErr);
		public int SimDoMillinaAndCam(bool wait) => SimaticComm.CmdMillingAndCamera.Execute(wait, _simErr);
		public int SimShiftToNextABC_pos(int posAlong, bool wait)
		{
			_log.Add(Tx.T("Posun na další pozici") + $" {(char)('@' + posAlong)}");
			return SimaticComm.Cmd_ShiftToNextABC_pos.Execute(wait, _simErr);
		}
		public void SimSetFirstXYStagePos(TestDef? testDef) => SimaticComm.SetFirstXYStagePos(testDef);
		public void SimSetActiveXYStagePos(double x, double y) => SimaticComm.SetActiveXYStagePos(x, y);
		public void SimSetActiveStopperPos(double pos) => SimSetActiveXYStagePos(pos, 0);

		public int Sim_MoveToNextStopperPos_AndDoMilling(double pos, bool wait)
		{
			_log.Add(Tx.T("Posun na další pozici") + $" {pos} mm");
			SimSetActiveStopperPos(pos);
			return SimaticComm.Cmd_ShiftToNextABC_pos.Execute(wait, _simErr);
		}

		public void SimClearTestResult() => SimaticComm.ClearTestResult();
		public void SimSetTestResult(int idMeasData, OkNok result, double hardness, double diameter) => SimaticComm.SetTestResult(idMeasData, result, hardness, diameter);
	}
}
