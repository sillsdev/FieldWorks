// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// Services for shared backends
	/// </summary>
	public static class SharedBackendServices
	{
		/// <summary>
		/// Indicates if there are multiple applications that are currently using this project.
		/// </summary>
		public static bool AreMultipleApplicationsConnected(FdoCache cache)
		{
			var sharedBep = cache.ServiceLocator.GetInstance<IDataStorer>() as SharedXMLBackendProvider;
			if (sharedBep != null)
				return sharedBep.OtherApplicationsConnectedCount > 0;
			return false;
		}
	}
}
