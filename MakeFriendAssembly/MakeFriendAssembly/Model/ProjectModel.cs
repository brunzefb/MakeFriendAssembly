using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DreamWorks.MakeFriendAssembly.Utility;
using EnvDTE;
using EnvDTE80;

namespace DreamWorks.MakeFriendAssembly.Model
{
	public class ProjectModel 
	{
		private readonly DTE2 _dte;
		private readonly List<string> _projectPathsList = new List<string>();
		private readonly List<ProjectItem> _projectItemList = new List<ProjectItem>();
		private readonly List<ProjectItem> _subItemList = new List<ProjectItem>();

		private readonly Dictionary<string, string> _fileToProjectDictionary =
			new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public const string FullPathPropertyName = "FullPath";
		private const string CsprojExtension = ".csproj";
		private const string CsharpFileExtension = ".cs";
		private static readonly log4net.ILog Logger = log4net.LogManager.
			GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ProjectModel(DTE2 dte)
		{
			_dte = dte;
		}

		public List<string> ProjectPathsList
		{
			get
			{
				return _projectPathsList;
			}
		}

		
		public List<string> CsharpFilesInProject
		{
			get { return _fileToProjectDictionary.Keys.ToList(); }
		}

		public string ProjectPathFromFilePath(string path)
		{
			if (!string.IsNullOrEmpty(path) &&
				_fileToProjectDictionary.Keys.Contains(path))
			{
				return _fileToProjectDictionary[path];
			}
			return string.Empty;
		}

		public void Clean()
		{
			_fileToProjectDictionary.Clear();
			_projectPathsList.Clear();
			_projectItemList.Clear();
			_subItemList.Clear();
		}

		public void GetCSharpFilesFromSolution()
		{
			Logger.Info("ProjectModel.GetCSharpFilesFromSolution() - Getting project files");
			var solution = _dte.Solution;

			if (solution == null || solution.Projects == null)
				return;

			var solutionProjects = solution.Projects;
			RelativePathHelper.BasePath = Path.GetDirectoryName(solution.FullName);

			_fileToProjectDictionary.Clear();

			foreach (var p in solutionProjects)
			{
				var project = p as Project;
				if (project == null)
					continue;

				var props = project.Properties;

				if (!HasProperty(props, (FullPathPropertyName)))
					continue;

				if (!project.FileName.EndsWith(CsprojExtension))
					continue;

				_projectItemList.Clear();
				foreach (ProjectItem item in project.ProjectItems)
				{
					_subItemList.Clear();
					var mainItem = RecursiveGetProjectItem(item);
					_projectItemList.Add(mainItem);
					_projectItemList.AddRange(_subItemList);
				}
				foreach (var item in _projectItemList)
					GetFilesFromProjectItem(item, project);
			}

			GetProjectPathsList();
			Logger.Info("ProjectModel.GetCSharpFilesFromSolution() - Done!");
		}

		private void GetProjectPathsList()
		{
			var solution = _dte.Solution;

			if (solution == null || solution.Projects == null)
				return;

			var solutionProjects = solution.Projects;

			_projectPathsList.Clear();
			foreach (Project project in solutionProjects)
			{
				if (!project.FileName.EndsWith(CsprojExtension))
					continue;
				_projectPathsList.Add(project.FileName);
			}
		}
		private ProjectItem RecursiveGetProjectItem(ProjectItem item)
		{
			if (item.ProjectItems == null)
				return item;

			foreach (ProjectItem innerItem in item.ProjectItems)
			{
				_subItemList.Add(RecursiveGetProjectItem(innerItem));
			}
			return item;
		}

		private void GetFilesFromProjectItem(ProjectItem item, Project project)
		{
			if (item.FileCount == 0)
				return;
			if (item.FileCount == 1)
			{
				// ReSharper disable once UseIndexedProperty
				var filePath = item.get_FileNames(0);
				if (filePath.ToLower().EndsWith(CsharpFileExtension))
					_fileToProjectDictionary.Add(filePath, project.FullName);
				return;
			}

			Logger.Warn("ProjectModel.GetFilesFromProjectItem more than 1 project item!");
		}

		public void AddFileToProjectAssociation(string file, string project)
		{
			_fileToProjectDictionary.Add(file, project);
		}

		private bool HasProperty(Properties properties, string propertyName)
		{
			if (properties != null)
			{
				foreach (Property item in properties)
				{
					if (item != null && item.Name == propertyName)
						return true;
				}
			}
			return false;
		}
	}
}