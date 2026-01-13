using BaseUtils.Mvvm;
using System.Windows.Controls;

namespace ModuleConsole.IndentImages.Views
{
	/// <summary>
	/// Interaction logic for IndentImageUC.xaml
	/// </summary>
	public partial class IndentImageUC : UserControl
	{
		public IndentImageUC()
		{
			InitializeComponent();
			this.SetDataContext();
		}
	}
}
