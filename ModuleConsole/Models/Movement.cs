using BaseUtils.Messages;
using BaseUtils.SaveState;
using CommunityToolkit.Mvvm.ComponentModel;
using DbFramework.Utils;
using LabBase.IActives;
using LabBase.ILogging;
using LabBase.Messages;
using LabEdc.ViewModels;
using ModuleDatabase.Events;
using ModuleDatabase.Models;
using Newtonsoft.Json;
using Simatic.Interfaces;
using Simatic.ViewModels;
using System.Runtime.Serialization;

namespace ModuleConsole.Models
{
	public interface IMovement
	{
	}

	[DataContract]
	public partial class Movement : IMovement
	{
		[DataMember] public double BasePosSpeed { get; set; } = 100;
		[DataMember] public double BasePosPosition { get; set; } = 0;
		[DataMember] public double WorkPosSpeed { get; set; } = 100;
		public double CamFocusSpeed => WorkPosSpeed;
		//maximální poloha dole těsně nad koncákem [mm]
		[DataMember] public double MaxPosition { get; set; } = 20;
		//maximální síly během pohybu (kromě vtisku) [N]
		[DataMember] public double MaxMovementLoad { get; set; } = 200;
		//Čas odlehčení z maximální síly
		[DataMember] public double UnloadTime_s { get; set; } = 2;
		//Zaostření při kalibraci
		[DataMember][ObservableProperty] public partial double CamFocusCalibration { get; set; } = 0;


		#region --- hodnoty z definice zkoušky ---
		private IActiveTestDef _iActiveTestDef { get; }
		public TestDef TestDef => _iActiveTestDef?.ITestDef as TestDef;
		public DbEntities DbContext => _iActiveTestDef?.DbContext as DbEntities;

		private ILogger _log;

		public double WorkPosPosition => TestDef?.WPos ?? 0;

		public double? CamLastTouch
		{
			get => TestDef?.CamLastTouch;
			set { if (TestDef?.ID_MethodNavigation != null) TestDef.ID_MethodNavigation.CamLastTouch = value; OnPropertyChanged(nameof(CamLastTouch)); }
		}
		public double? CamFocus
		{
			get => TestDef?.CamFocus;
			set { if (TestDef?.ID_MethodNavigation != null) TestDef.ID_MethodNavigation.CamFocus = value; OnPropertyChanged(nameof(CamFocus)); }
		}
		public double? CamSoftLimit
		{
			get => TestDef?.CamSoftLimit ?? 0;
			set { if (TestDef?.ID_MethodNavigation != null) TestDef.ID_MethodNavigation.CamSoftLimit = value; }
		}

		#endregion
		[JsonConstructor] protected Movement() { }

		public Movement(IActiveTestDef iActiveTestDef, ILogger iLogger, ISimaticVM iSimaticVM, IEdcVM iEdcVM, ISaveState iSaveState)
		{
			_iActiveTestDef = iActiveTestDef;
			_log = iLogger;
			_simaticVM = iSimaticVM as SimaticVM;
			_edcVM = iEdcVM as EdcVM;
			_iSaveState = iSaveState;
			_iSaveState.AddOrUpdate("Movement", this);

			Messenger.Register<CalibrationModeChangedMessage>(this, (r, m) => _isCalibrationMode = m.Value);
		}


		internal void DbSaveChanges(bool doUpdateChangesToMethodAndTestDef)
		{
			DbContext.SaveChanges();
			if (doUpdateChangesToMethodAndTestDef)
				updateChangesToMethodAndTestDef();
		}

		internal void DbCancelChanges()
		{
			DbContext.Cancel();
			updateChangesToMethodAndTestDef();
		}
		private void updateChangesToMethodAndTestDef()
		{
			//toto aplikuje změny do metody - stačí aktivní metoda
			Messenger.Send(new RefreshMethodMessage(ReselectType.RefreshActive));
			//a aplikace změn do testDef + reselect testDef - musí se updatovat všechny definice testů - kterýkoliv může obsahovat odkaz na tuto metodu
			Messenger.Send(new ReselectTestDefMessage(ReselectType.RefreshAll));

			OnPropertyChanged(nameof(CamFocus));
			OnPropertyChanged(nameof(CamLastTouch));
		}
	}
}
