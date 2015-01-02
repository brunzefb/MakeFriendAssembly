using System;

namespace DreamWorks.MakeFriendAssembly.Utility
{
	internal static class ExceptionLogHelper
	{
		private static readonly log4net.ILog Logger = log4net.LogManager.
			GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void LogException(Exception ex)
		{
			Logger.Warn(string.Format("Exception Logger: Time {0} Exception: {1} ", DateTime.Now, ex.Message));
			if (!string.IsNullOrEmpty(ex.StackTrace))
				Logger.Warn(ex.StackTrace);
			if (ex.InnerException != null)
			{
				var ex2 = ex.InnerException;
				Logger.Warn(string.Format("Inner Exception Logger: Time {0} Exception: {1} ", DateTime.Now, ex2.Message));
				if (!string.IsNullOrEmpty(ex2.StackTrace))
					Logger.Warn(ex2.StackTrace);
			}
		}
	}
}