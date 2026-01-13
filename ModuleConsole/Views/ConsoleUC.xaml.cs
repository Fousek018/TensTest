using BaseUtils.Mvvm;
using System.Windows.Controls;

namespace ModuleConsole.Views
{
	/// <summary>
	/// Interaction logic for ConsoleUC
	/// </summary>
	public partial class ConsoleUC : UserControl
	{
		public ConsoleUC()
		{
			InitializeComponent();
			this.SetDataContext();
		}
	}
}
