using System.Collections;
using System.Collections.Generic;
using System.Windows;
using DreamWorks.MakeFriendAssembly.ViewModel;
using DreamWorks.TddHelper.View;

namespace DreamWorks.MakeFriendAssembly.View
{
	/// <summary>
	/// Interaction logic for MakeFriendAssemblyDialog.xaml
	/// </summary>
	public partial class MakeFriendAssemblyDialog : Window, ICanClose
	{
		public MakeFriendAssemblyViewModel ViewModel;
		public MakeFriendAssemblyDialog(IEnumerable<string> projectList)
		{
			InitializeComponent();
			ViewModel = new MakeFriendAssemblyViewModel(this, projectList);
			DataContext = ViewModel;
		}

		public void CloseWindow(bool isCancel = false)
		{
			DialogResult = !isCancel;
			Close();
		}

		private void MakeFriendAssemblyDialog_OnLoaded(object sender, RoutedEventArgs e)
		{
			ViewModel.OnLoaded();
		}
	}
}