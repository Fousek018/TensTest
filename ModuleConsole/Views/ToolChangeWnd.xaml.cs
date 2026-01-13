using System.Windows;

namespace ModuleConsole.Views
{
	/// <summary>
	/// Interaction logic for ToolChangeWnd.xaml
	/// </summary>
	public partial class ToolChangeWnd : Window
	{
		public ToolChangeWnd()
		{
			InitializeComponent();
		}

		private void IconButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
