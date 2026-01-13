namespace ModuleConsole.Models
{
	public interface IMachineStatus
	{

		bool IsMachineMoving { get; }
		bool IsTestRunning { get; set; }
		bool IsPcTestRunning { get; set; }
		bool IsSimaticTestRunning { get; set; }
		bool IsCalibrationMode { get; set; }
		void UpdateStatus();
	}
}