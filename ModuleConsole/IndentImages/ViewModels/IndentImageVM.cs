using BaseUtils.Messages;
using BaseUtils.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using Globals.DataTypes;
using Globals.Hardness;
using Globals.Messages;
using LabBase.ILogging;
using ModuleDatabase.Models;
using System.Collections.ObjectModel;
using Vbloky.Translation;

namespace ModuleConsole.IndentImages.ViewModels
{
	public partial class ImageItem : BaseViewModel
	{
		[ObservableProperty] public partial string Title { get; set; }
		[ObservableProperty] public partial string ImagePath { get; set; }
	}

	public interface IIndentImageVM { }
	public partial class IndentImageVM : BaseViewModel, IIndentImageVM
	{
		private IHardnessService _iHardnesService;
		private ILogger _iLogger;
		private MeasData _measData;
		[ObservableProperty] public partial int ColumnsCount { get; set; } = 3;
		[ObservableProperty] public partial ObservableCollection<ImageItem> Images { get; set; } = new();

		public IndentImageVM(IHardnessService iHardnessService, ILogger iLogger)
		{
			_iHardnesService = iHardnessService;
			_iLogger = iLogger;

			Messenger.Register<ShowDataFromDbMessage>(this, (r, m) => Init(m.Value as MeasData));
			Messenger.Register<ShowDataFromTestMessage>(this, (r, m) => Init(m.Value as MeasData));
		}

		private void Init(MeasData measData)
		{
			_measData = measData;
			Images.Clear();
			if (measData == null)
				return;

			int numImages = _measData.GetNumPerformedIndents();
			ColumnsCount = numColumns();

			//pro tvrdoměr s rotací je zobrazení názvů vtisků ve formátu A1, A2, A3....
			//if (Glb.Current.IsHwRotationUsed)
			//pro tvrdoměr bez rotace zobrazení názvů vtisků Vtisk1, Vtisk2 ...
			//else
			for (int i = 0; i < numImages; i++)
			{
				string title;
				if (Glb.Current.IsHwRotationUsed)
				{
					int abc = _measData.Td_NumIndentsAlong;
					int rot = _measData.Td_NumIndentsRot;
					title = $"{((char)('A' + i / rot))}{1 + i % rot}";
				}
				else
					title = Tx.T("Vtisk") + $" {1 + i}";

				Images.Add(new ImageItem
				{
					//názvy vtisků ve formátu: Vtisk1, Vtisk2,.. NEBO  A1, A2.., B1, .. C4
					Title = title,
					//image path podle toho, zda jsou data zobrazena během zkoušky (ID=0) nebo z databáze (ID>0)
					ImagePath = _measData.ID > 0 ? Glb.GetFullPath_Images(1 + i, _measData) : Glb.GetTempFullPath_Image(1 + i)
				});
			}

			int numColumns()
			{
				//return 1 + (numImages - 1) % 3;
				return numImages switch
				{
					<= 3 => numImages,
					<= 4 => 4,
					<= 8 => 4,
					<= 9 => 3,
					_ => 4
				};
			}
		}
	}
}
