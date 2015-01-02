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
				MakeFriendAssembly(requestorProject, giverProject);
			}
		}

		private void MakeFriendAssembly(Project requestorProject, Project friendshipGiverProject)
		{
			string patchString;
			var fullPathToSnk = GetFullPathToSnkFile(requestorProject);
			if (!string.IsNullOrEmpty(fullPathToSnk))
			{
				Logger.InfoFormat("CreateProjectHelper.MakeFriendAssembly, fullPathToSnk={0}", fullPathToSnk);
				var publicKeyAsString = Helper.PublicKeyFromSnkFile(fullPathToSnk);
				if (string.IsNullOrEmpty(publicKeyAsString))
				{
					Logger.Info("CreateProjectHelper.MakeFriendAssembly - PublicKeyFromSnkFile (c++) failed");
					return;
				}
				string assemblyName = friendshipGiverProject.Name;
				patchString = GetInternalsVisibleToString(assemblyName, publicKeyAsString);
			}
			else
				patchString = string.Format("[assembly: InternalsVisibleTo (\"{0}\")]", requestorProject.Name);
			PatchAssemblyInfoCsWithInternalsVisibleTo(friendshipGiverProject, patchString);
		}

		private void PatchAssemblyInfoCsWithInternalsVisibleTo(Project friendshipGiverProject, string patchString)
		{
			Logger.InfoFormat("CreateProjectHelper.PatchAssemblyInfoCsWithInternalsVisibleTo, giverProject={0}", friendshipGiverProject.Name);
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
					"CreateProjectHelper.PatchAssemblyInfoCsWithInternalsVisibleTo, could not find path to AssemblyInfo.cs");
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
				Logger.Info("CreateProjectHelper.GetFullPathToSnkFile sourceProj is null");
				return null;
			}
			var props = sourceProj.Project.Properties;
			if (props == null)
			{
				Logger.Info("CreateProjectHelper.GetFullPathToSnkFile props is null");
				return null;
			}
			var isSigned = props.Item(SignAssemblyPropertyName).Value as bool?;
			var keyFile = props.Item(AssemblyOriginatorKeyFilePropertyName).Value as string;
			var projectDirectoryName = Path.GetDirectoryName(project.FullName);
			if (string.IsNullOrEmpty(projectDirectoryName) || string.IsNullOrEmpty(keyFile))
			{
				if (string.IsNullOrEmpty(keyFile))
					Logger.Info("CreateProjectHelper.GetFullPathToSnkFile keyFile is null");
				else
					Logger.Info("CreateProjectHelper.GetFullPathToSnkFile projectDirectory is null");
				return null;
			}
			Logger.InfoFormat("CreateProjectHelper.GetFullPathToSnkFile isSigned={0}, keyFile={1}, projectDirectoryName={2}",
				isSigned, keyFile, projectDirectoryName);
			string fullPathToSnk = Path.Combine(projectDirectoryName, keyFile);
			if (!isSigned.HasValue || !isSigned.Value)
			{
				Logger.Info("CreateProjectHelper.GetFullPathToSnkFile not signed");
				return null;
			}
			if (!File.Exists(fullPathToSnk))
			{
				Logger.Info("CreateProjectHelper.GetFullPathToSnkFile No key file (fullPathToSnk) on disk");
				return null;
			}
			Logger.InfoFormat("CreateProjectHelper.GetFullPathToSnkFile returning fullPathToSnk={0}", fullPathToSnk);
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