using BaseUtils.Messages;
using Globals.Hardness.Messages;
using LabLogger.Models;
using Vbloky.Translation;

namespace ModuleConsole.Models
{
	public partial class Movement
	{
		public int CommToBasePosition()
		{
			var msg = new MsgWrap(Tx.T("Na základní polohu"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimHSupportToBasePosition(true);
			return msg.Err;
		}
		public int CommToCalibPosition()
		{
			var msg = new MsgWrap(Tx.T("Na polohu kalibrace"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimToCalibPosition(true);
			return msg.Err;
		}
		public int CommToMillingPosition()
		{
			var msg = new MsgWrap(Tx.T("Na polohu frézování"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimHSupportToMillingPosition(true);
			return msg.Err;
		}
		public int CommToIndentPosition()
		{
			var msg = new MsgWrap(Tx.T("Na polohu vtisku"));
			if (msg.Ok) msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimHSupportToIndentPosition(true);
			return msg.Err;
		}
		public int CommToCamPosition()
		{
			bool isCalib = _isCalibrationMode;
			string strCalib = isCalib ? (". " + Tx.T("Pro kalibraci")) : "";
			var msg = new MsgWrap(Tx.T("Na polohu kamery") + strCalib);
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimHSupportToCamPosition(true);
			if (msg.Ok) msg.Err = EdcToCamFocus();
			return msg.Err;
		}
		public int CommMeasMillingWidth()
		{
			var msg = new MsgWrap(Tx.T("Měření šířky zábrusu"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimMeasMillingWidth(true);
			return msg.Err;
		}
		public int CommDoMilling()
		{
			var msg = new MsgWrap(Tx.T("Frézování"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimDoMilling(true);
			return msg.Err;
		}
		public int CommDoMillingAndIndent()
		{
			var msg = new MsgWrap(Tx.T("Frézování a následný přesun na polohu vtisku"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimDoMillingAndIndent(true);
			return msg.Err;
		}
		public int CommDoMillingAndCam()
		{
			var msg = new MsgWrap(Tx.T("Frézování a následný přesun na polohu kamery"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimDoMillinaAndCam(true);
			return msg.Err;
		}
		public int CommDoToolChange()
		{
			var msg = new MsgWrap(Tx.T("Výměna nástroje"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimDoToolChange(true);
			return msg.Err;
		}
		public int CommDoBallChange(double? position = null)
		{
			var msg = new MsgWrap(Tx.T("Výměna kuličky"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimDoBallChange(true);
			if (msg.Ok && position.HasValue)
				msg.Err = EdcPosExt(WorkPosSpeed / 2.0, position ?? 0, CamSoftLimit ?? 0, true);
			return msg.Err;
		}
		public int CommStopMovement()
		{
			var msg = new MsgWrap(Tx.T("Zastavení pohybu"));
			SHalt();
			msg.Err = SimStopMovement(true);
			return msg.Err;
		}
		public int DoControlMeasurement()
		{
			var msg = new MsgWrap(Tx.T("Kontrolní měření"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = VSupportDown(true);
			if (msg.Ok) msg.Err = CommDoMillingAndIndent();
			if (msg.Ok) msg.Err = EdcDoIndent(false);
			if (msg.Ok) msg.Err = CommToCamPosition();
			if (msg.Ok)
			{
				var _measValues = Messenger.Send<MeasureAndCalcHardnessMessage>();
				Messenger.Send(new DisplayValuesChangedMessage(_measValues));
			}
			return msg.Err;
		}

		int angle => TestDef.NumIndentsRot switch
		{
			1 => 360,
			2 => 180,
			3 => 120,
			4 => 90,
			_ => 0
		};
		public int CommRotatePart()
		{
			var msg = new MsgWrap(Tx.TC("Otočení dílu") + $"{angle}°");
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimDoPartRotation(true);
			return msg.Err;
		}
		public int CommRotatePart_CorrIndent()
		{
			var msg = new MsgWrap(Tx.T("Otočení dílu pro opravný vtisk"));
			msg.Err = EdcBasePosition(true);
			if (msg.Ok) msg.Err = SimDoPartRotation_CorrIndent(true);
			return msg.Err;
		}
	}
}
