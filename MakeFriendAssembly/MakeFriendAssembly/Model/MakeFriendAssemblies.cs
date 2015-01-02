using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using EnvDTE80;
using SnkHelper;
using VSLangProj80;
using DreamWorks.MakeFriendAssembly.ViewModel;

namespace DreamWorks.MakeFriendAssembly.Model
{
	public class MakeFriendAssemblies
	{
		private readonly MakeFriendAssemblyViewModel _data;
		private readonly ProjectModel _projectModel;
		private static readonly log4net.ILog Logger = log4net.LogManager.
			GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private const int KeyLineLength = 80;
		private const string SignAssemblyPropertyName = "SignAssembly";
		private const string AssemblyOriginatorKeyFilePropertyName = "AssemblyOriginatorKeyFile";
		private const string AssemblyInfoCsFile = "AssemblyInfo.cs";
		private readonly DTE2 _dte;

		public MakeFriendAssemblies(MakeFriendAssemblyViewModel viewModel, ProjectModel projectModel, DTE2 dte)
		{
			_data = viewModel;
			_projectModel= projectModel;
			_dte = dte;
		}

		public void Execute()
		{
			Logger.Info("MakeFriendAssemblies.Execute-Starts");
			var friendshipGiverProjectPath = _data.SelectedGiver.Path;
			var friendshipRequestorProjectPaths = new List<string>();
			foreach (var s in _data.FriendRequestor)
			{
				if (s.IsSelected && File.Exists(s.Path))
					friendshipRequestorProjectPaths.Add(s.Path);
			}
			if (friendshipRequestorProjectPaths.Count < 1)
				return;

			var giverProject = ProjectFromPath(friendshipGiverProjectPath);
			foreach (var requestor in friendshipRequestorProjectPaths)
			{
				var requestorProject = ProjectFromPath(requestor);
				Logger.InfoFormat("MakeFriendAssemblies.Execute- Project:{0} gives access to project:{1}", 
					friendshipGiverProjectPath, requestor);
				MakeFriendAssembly(requestorProject, giverProject);
			}
			Logger.Info("MakeFriendAssemblies.Execute-Ends");
		}

		private void MakeFriendAssembly(Project friendshipRequestorProject, Project friendshipGiverProject)
		{
			string patchString;
			var fullPathToSnk = GetFullPathToSnkFile(friendshipRequestorProject);
			if (!string.IsNullOrEmpty(fullPathToSnk))
			{
				Logger.InfoFormat("MakeFriendAssemblies.MakeFriendAssembly, fullPathToSnk={0}", fullPathToSnk);
				var publicKeyAsString = Helper.PublicKeyFromSnkFile(fullPathToSnk);
				if (string.IsNullOrEmpty(publicKeyAsString))
				{
					Logger.Info("MakeFriendAssemblies.MakeFriendAssembly - PublicKeyFromSnkFile (c++) failed");
					return;
				}
				string assemblyName = friendshipRequestorProject.Name;
				patchString = GetInternalsVisibleToString(assemblyName, publicKeyAsString);
			}
			else
				patchString = string.Format("[assembly: InternalsVisibleTo (\"{0}\")]", friendshipRequestorProject.Name);
			PatchAssemblyInfoCsWithInternalsVisibleTo(friendshipGiverProject, patchString);
		}

		private void PatchAssemblyInfoCsWithInternalsVisibleTo(Project friendshipGiverProject, string patchString)
		{
			Logger.InfoFormat("MakeFriendAssemblies.PatchAssemblyInfoCsWithInternalsVisibleTo, giverProject={0}", friendshipGiverProject.Name);
			Logger.InfoFormat("patchString={0}", patchString);

			string fullPathToAssemblyInfoCs = string.Empty;
			foreach (var fullPathToFile in _projectModel.CsharpFilesInProject)
			{
				var fileName = Path.GetFileName(fullPathToFile);
				var projectPathFromFilePath = _projectModel.ProjectPathFromFilePath(fullPathToFile);
				if (String.Equals(fileName, AssemblyInfoCsFile, StringComparison.OrdinalIgnoreCase))
				{
					if (File.Exists(fullPathToFile) &&
						String.Equals(projectPathFromFilePath, friendshipGiverProject.FullName, StringComparison.OrdinalIgnoreCase))
					{
						fullPathToAssemblyInfoCs = fullPathToFile;
						break;
					}
				}
			}
			if (string.IsNullOrEmpty(fullPathToAssemblyInfoCs))
			{
				Logger.Info(
					"MakeFriendAssemblies.PatchAssemblyInfoCsWithInternalsVisibleTo, could not find path to AssemblyInfo.cs");
				return;
			}
			using (var sw = File.AppendText(fullPathToAssemblyInfoCs))
			{
				sw.WriteLine("\r\n");
				sw.WriteLine(patchString);
			}
		}

		private string GetFullPathToSnkFile(Project project)
		{
			var sourceProj = project.Object as VSProject2;
			if (sourceProj == null)
			{
				Logger.Info("MakeFriendAssemblies.GetFullPathToSnkFile sourceProj is null");
				return null;
			}
			var props = sourceProj.Project.Properties;
			if (props == null)
			{
				Logger.Info("MakeFriendAssemblies.GetFullPathToSnkFile props is null");
				return null;
			}
			var isSigned = props.Item(SignAssemblyPropertyName).Value as bool?;
			var keyFile = props.Item(AssemblyOriginatorKeyFilePropertyName).Value as string;
			var projectDirectoryName = Path.GetDirectoryName(project.FullName);
			if (string.IsNullOrEmpty(projectDirectoryName) || string.IsNullOrEmpty(keyFile))
			{
				if (string.IsNullOrEmpty(keyFile))
					Logger.Info("MakeFriendAssemblies.GetFullPathToSnkFile keyFile is null");
				else
					Logger.Info("MakeFriendAssemblies.GetFullPathToSnkFile projectDirectory is null");
				return null;
			}
			Logger.InfoFormat("MakeFriendAssemblies.GetFullPathToSnkFile isSigned={0}, keyFile={1}, projectDirectoryName={2}",
				isSigned, keyFile, projectDirectoryName);
			string fullPathToSnk = Path.Combine(projectDirectoryName, keyFile);
			if (!isSigned.HasValue || !isSigned.Value)
			{
				Logger.Info("MakeFriendAssemblies.GetFullPathToSnkFile not signed");
				return null;
			}
			if (!File.Exists(fullPathToSnk))
			{
				Logger.Info("MakeFriendAssemblies.GetFullPathToSnkFile No key file (fullPathToSnk) on disk");
				return null;
			}
			Logger.InfoFormat("MakeFriendAssemblies.GetFullPathToSnkFile returning fullPathToSnk={0}", fullPathToSnk);
			return fullPathToSnk;
		}

		private static string GetInternalsVisibleToString(string assemblyName, string publicKey)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < publicKey.Length; i += KeyLineLength)
			{
				if (i < 240)
					sb.AppendFormat("\t\t\"{0}\" + \r\n", publicKey.Substring(i, KeyLineLength));
				else
					sb.AppendFormat("\t\t\"{0}\" \r\n", publicKey.Substring(i));
			}
			const string template = "[assembly: InternalsVisibleTo (\r\n" +
									"\t\"{0}, PublicKey=\" + \r\n{1}" + ")]";
			return string.Format(template, assemblyName, sb);
		}

		private Project ProjectFromPath(string path)
		{
			foreach (Project project in _dte.Solution.Projects)
			{
				try
				{
					if (!string.IsNullOrEmpty(project.FullName) &&
						string.Equals(project.FullName, path, StringComparison.CurrentCultureIgnoreCase))
						return project;
				}
				// ReSharper disable once EmptyGeneralCatchClause
				catch
				{
				}
			}
			Logger.InfoFormat("CreateClassHelper.ProjectFromPath with {0} arg not found", path);
			return null;
		}
	}
}