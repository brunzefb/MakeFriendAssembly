using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using DreamWorks.MakeFriendAssembly.Model;
using DreamWorks.MakeFriendAssembly.Utility;
using DreamWorks.MakeFriendAssembly.View;
using EnvDTE;
using EnvDTE80;
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
		private ProjectModel _projectModel;
		private static readonly log4net.ILog Logger = log4net.LogManager.
			GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		public MakeFriendAssemblyPackage()
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += currentDomain_UnhandledException;
		}

		protected override void Initialize()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
			base.Initialize();
			SetupLogging();
			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (null == mcs) 
				return;
			
			var menuCommandID = new CommandID(GuidList.guidMakeFriendAssemblyCmdSet, 
				(int)PkgCmdIDList.cmdidMakeFriendAssembly);
			var menuItem = new MenuCommand(MakeFriendsMenuCallback, menuCommandID);
			_shell = (IVsUIShell)GetService(typeof(SVsUIShell));
			mcs.AddCommand(menuItem);
			var dte = (DTE2)GetService(typeof(DTE));
			_projectModel = new ProjectModel(dte);
		}

		private void MakeFriendsMenuCallback(object sender, EventArgs e)
		{
			var dte = (DTE2)GetService(typeof(DTE));
			if (dte.Solution == null || !dte.Solution.IsOpen)
			{
				Logger.Warn("MakeFriendsMenuCallback: No solution, aborting");
				Console.Beep();
				return;
			}
			_projectModel.Clean();
			_projectModel.GetCSharpFilesFromSolution();
			if (_projectModel.ProjectPathsList.Count < 2)
			{
				Logger.Warn("MakeFriendsMenuCallback: Less than 2 projects, aborting");
				Console.Beep();
				return;
			}
			var makeFriendAssemblyDlg = new MakeFriendAssemblyDialog(_projectModel.ProjectPathsList);
			SetModalDialogOwner(makeFriendAssemblyDlg);
			var dlgResult = makeFriendAssemblyDlg.ShowDialog();
			if (!dlgResult.HasValue || dlgResult != true)
			{
				Logger.Warn("MakeFriendsMenuCallback: User cancelled dialog");
				return;
			}
			var viewModel = makeFriendAssemblyDlg.ViewModel;
			var makeFriends = new MakeFriendAssemblies(viewModel, _projectModel, dte);
			makeFriends.Execute();
		}

		public void SetModalDialogOwner(System.Windows.Window targetWindow)
		{
			IntPtr hWnd;
			_shell.GetDialogOwnerHwnd(out hWnd);
			// ReSharper disable once PossibleNullReferenceException
			var parent = HwndSource.FromHwnd(hWnd).RootVisual;
			targetWindow.Owner = (System.Windows.Window)parent;
		}

		private void SetupLogging()
		{
			const string defaultLogConfigTemplate =
				@"
  <log4net>
    <appender name='LogFileAppender' type='log4net.Appender.RollingFileAppender'>
      <file value='{REPLACE}' />
      <appendToFile value='true' />
      <rollingStyle value='Size' />
      <maxSizeRollBackups value='2' />
      <maximumFileSize value='20MB' />
      <datePattern value='yyyy-MM-dd.HH-mm' />
      <staticLogFileName value='true' />
      <immediateFlush value='true' />
      <lockingModel type='log4net.Appender.FileAppender+MinimalLock' />
      <layout type='log4net.Layout.PatternLayout'>
        <conversionPattern value='%date{ISO8601}&#9;%-4thread&#9;%level&#9;%message%newline' />
      </layout>
    </appender>
    <appender name='ConsoleAppender' type='log4net.Appender.ConsoleAppender'>
      <target value='Console.Error' />
      <layout type='log4net.Layout.PatternLayout'>
        <conversionPattern value='%date{ISO8601}&#9;%-4thread&#9;%level&#9;%message%newline' />
      </layout>
    </appender>
    <root>
      <level value='ALL' />
      <appender-ref ref='ConsoleAppender' />
      <appender-ref ref='LogFileAppender' />
    </root>
  </log4net>
";
			var commonApps = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			var folder = Path.Combine(commonApps, @"MakeFriendAssembly");
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
			string pathToLogfile = Path.Combine(folder, "MakeFriendAssembly.log");
			var logConfig = defaultLogConfigTemplate.Replace("{REPLACE}", pathToLogfile);
			var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(logConfig));
			log4net.Config.XmlConfigurator.Configure(stream);
			Logger.Info("\r\nNEW SESSION - MakeFriendAssembly logging at " + DateTime.Now);
		}

		private void currentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = e.ExceptionObject as Exception;
			if (ex == null)
				return;
			ExceptionLogHelper.LogException(ex);
		}
	}
}