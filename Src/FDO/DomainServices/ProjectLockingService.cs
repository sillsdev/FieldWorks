// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// Provides methods for locking/unlocking a project.
	/// </summary>
	public class ProjectLockingService
	{
		/// <summary>
		/// This method will lock the current project in the cache given this service
		/// </summary>
		public static void LockCurrentProject(FdoCache cache)
		{
			//Make sure all the changes the user has made are on the disc before we begin.
			// Make sure any changes we want backup are saved.
			var ds = cache.ServiceLocator.GetInstance<IDataStorer>() as XMLBackendProvider;
			if (ds != null)
				ds.LockProject();
		}

		/// <summary>
		/// This method will unlock the current project in the cache given this service.
		/// </summary>
		public static void UnlockCurrentProject(FdoCache cache)
		{

			//Make sure all the changes the user has made are on the disc before we begin.
			// Make sure any changes we want backup are saved.
			var ds = cache.ServiceLocator.GetInstance<IDataStorer>() as XMLBackendProvider;
			if (ds != null)
			{
				cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
				ds.CompleteAllCommits();
				ds.UnlockProject();
			}
		}

		/// <summary>
		/// This method will check if the project is an fwdata project, that can be locked.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns>true if the IDataStorer is an XMLBackendProvider, false otherwise.</returns>
		public static bool CanLockProject(FdoCache cache)
		{
			var ds = cache.ServiceLocator.GetInstance<IDataStorer>() as XMLBackendProvider;
			if (ds != null)
				return false;
			return true;
		}

		/// <summary>
		/// Determines if the project file at the specified path is locked.
		/// </summary>
		public static bool IsProjectLocked(string projectPath)
		{
			string ext = Path.GetExtension(projectPath);
			return ext != null && ext.Equals(FdoFileHelper.ksFwDataXmlFileExtension, StringComparison.InvariantCultureIgnoreCase)
				&& XMLBackendProvider.IsFileLocked(projectPath);
		}
	}
}
