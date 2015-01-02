using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using DreamWorks.TddHelper.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace DreamWorks.MakeFriendAssembly.ViewModel
{
	public class ItemSelected
	{
	};

	public class DisplayPathHelper
	{
		private bool _isSelected;
		public string Path { get; set; }
		public string DisplayPath { get; set; }

		public bool IsSelected
		{
			get { return _isSelected; }
			set
			{
				_isSelected = value;
				Messenger.Default.Send(new ItemSelected());
			}
		}
	}

	public class MakeFriendAssemblyViewModel : ViewModelBase
	{
		private readonly RelayCommand _okCommand;
		private readonly RelayCommand _cancelCommand;
		private readonly ICanClose _view;
		private readonly ObservableCollection<DisplayPathHelper> _friendGiver;
		private readonly ObservableCollection<DisplayPathHelper> _friendRequestor;
		private DisplayPathHelper _selectedGiver = new DisplayPathHelper();

		public MakeFriendAssemblyViewModel(ICanClose view, IEnumerable<string> projects )
		{
			_okCommand = new RelayCommand(OnOk, IsOKEnabled);
			_cancelCommand = new RelayCommand(OnCancel);
			_view = view;
			Messenger.Default.Register<ItemSelected>(this, OnItemSelected);
			var list = new List<DisplayPathHelper>();
			foreach (var file in projects)
			{
				var display = new DisplayPathHelper();
				display.Path = file;
				display.DisplayPath = Path.GetFileNameWithoutExtension(file);
				list.Add(display);
			}
			_friendGiver = new ObservableCollection<DisplayPathHelper>(list);
			_friendRequestor = new ObservableCollection<DisplayPathHelper>();
		}

		public void OnItemSelected(ItemSelected item)
		{
			_okCommand.RaiseCanExecuteChanged();
		}

		private bool IsOKEnabled()
		{
			var anySelected = _friendRequestor.Any(x=>x.IsSelected);
			return (_selectedGiver != null) && anySelected;
		}

		public ICommand CancelCommand
		{
			get { return _cancelCommand; }
		}

		public ICommand OkCommand
		{
			get { return _okCommand; }
		}

		public ObservableCollection<DisplayPathHelper> FriendGiver
		{
			get { return _friendGiver; }
		}

		public ObservableCollection<DisplayPathHelper> FriendRequestor
		{
			get { return _friendRequestor; }
		}

		public DisplayPathHelper SelectedGiver
		{
			get { return _selectedGiver; }
			set
			{
				_selectedGiver = value;
				SetRequestorListBox(value);
				RaisePropertyChanged(()=>SelectedGiver);
			}
		}

		private void SetRequestorListBox(DisplayPathHelper selectedGiver)
		{
			_friendRequestor.Clear();
			foreach (var giver in _friendGiver)
			{
				if (giver.Path == selectedGiver.Path)
					continue;
				_friendRequestor.Add(giver);
			}
			ClearRequestorSelections();
			RaisePropertyChanged(()=>FriendRequestor);
			_okCommand.RaiseCanExecuteChanged();
		}

		private void OnCancel()
		{
			_view.CloseWindow(true);
		}

		private void OnOk()
		{
			_view.CloseWindow();
		}

		public void OnLoaded()
		{
		}

		private void ClearRequestorSelections()
		{
			foreach (var entry in FriendRequestor)
			{
				entry.IsSelected = false;
			}
			RaisePropertyChanged(()=>FriendRequestor);
		}
	}
}