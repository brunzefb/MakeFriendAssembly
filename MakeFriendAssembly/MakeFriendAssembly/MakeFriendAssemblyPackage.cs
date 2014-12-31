using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
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
			mcs.AddCommand(menuItem);
		}

		private void MenuItemCallback(object sender, EventArgs e)
		{
			var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
			Guid clsid = Guid.Empty;
			int result;
			ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
				0,
				ref clsid,
				"MakeFriendAssembly",
				string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", ToString()),
				string.Empty,
				0,
				OLEMSGBUTTON.OLEMSGBUTTON_OK,
				OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
				OLEMSGICON.OLEMSGICON_INFO,
				0, // false
				out result));
		}
	}
}