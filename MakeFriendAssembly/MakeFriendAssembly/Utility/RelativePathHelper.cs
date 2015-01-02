using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DreamWorks.MakeFriendAssembly.Utility
{
	// borrowed from http://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
	public static class RelativePathHelper
	{
		public static string BasePath { get; set; }

		public static string GetRelativePath(string toPath)
		{
			int fromAttr = GetPathAttribute(BasePath);
			int toAttr = GetPathAttribute(toPath);

			var path = new StringBuilder(260); // MAX_PATH
			if (NativeMethods.PathRelativePathTo(
				path,
				BasePath,
				fromAttr,
				toPath,
				toAttr) == 0)
			{
				throw new ArgumentException("Paths must have a common prefix");
			}
			return path.ToString();
		}

		public static string GetRelativePath(string fromPath, string toPath)
		{
			int fromAttr = GetPathAttribute(fromPath);
			int toAttr = GetPathAttribute(toPath);

			var path = new StringBuilder(260); // MAX_PATH
			if (NativeMethods.PathRelativePathTo(
				path,
				BasePath,
				fromAttr,
				toPath,
				toAttr) == 0)
			{
				throw new ArgumentException("Paths must have a common prefix");
			}
			return path.ToString();
		}

		private static int GetPathAttribute(string path)
		{
			var di = new DirectoryInfo(path);
			if (di.Exists)
			{
				return FileAttributeDirectory;
			}

			var fi = new FileInfo(path);
			if (fi.Exists)
			{
				return FileAttributeNormal;
			}

			throw new FileNotFoundException();
		}

		private const int FileAttributeDirectory = 0x10;
		private const int FileAttributeNormal = 0x80;

		private static class NativeMethods
		{
			[DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
			internal static extern int PathRelativePathTo(StringBuilder pszPath,
				string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);
			
		}
	}
}