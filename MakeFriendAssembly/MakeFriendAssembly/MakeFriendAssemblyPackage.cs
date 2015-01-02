using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using DreamWorks.MakeFriendAssembly.View;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace DreamWorks.MakeFriendAssembly
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(GuidList.guidMakeFriendAssemblyPkgString)]
	public sealed class MakeFriendAssemblyPackage : Package
	{
		private IVsUIShell _shell;
		public MakeFriendAssemblyPackage()
		{
		}

		protected override void Initialize()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
			base.Initialize();

			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (null == mcs) 
				return;
			var menuCommandID = new CommandID(GuidList.guidMakeFriendAssemblyCmdSet, (int)PkgCmdIDList.cmdidMakeFriendAssembly);
			var menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
			_shell = (IVsUIShell)GetService(typeof(SVsUIShell));
			mcs.AddCommand(menuItem);
		}

		private void MenuItemCallback(object sender, EventArgs e)
		{
			var resolveFileConflictDialog = new MakeFriendAssemblyDialog(new List<string>{"hi", "there", "good"});
			SetModalDialogOwner(resolveFileConflictDialog);

			var dlgResult = resolveFileConflictDialog.ShowDialog();
			if (!dlgResult.HasValue || dlgResult != true)
				return;
			
		}

		public void SetModalDialogOwner(System.Windows.Window targetWindow)
		{
			IntPtr hWnd;
			_shell.GetDialogOwnerHwnd(out hWnd);
			// ReSharper disable once PossibleNullReferenceException
			var parent = HwndSource.FromHwnd(hWnd).RootVisual;
			targetWindow.Owner = (System.Windows.Window)parent;
		}
	}
}