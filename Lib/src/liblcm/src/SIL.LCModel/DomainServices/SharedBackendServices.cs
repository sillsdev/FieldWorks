// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Infrastructure;
using SIL.LCModel.Infrastructure.Impl;

namespace SIL.LCModel.DomainServices
{
	/// <summary>
	/// Services for shared backends
	/// </summary>
	public static class SharedBackendServices
	{
		/// <summary>
		/// Indicates if there are multiple applications that are currently using this project.
		/// </summary>
		public static bool AreMultipleApplicationsConnected(LcmCache cache)
		{
			if (cache == null) // Can happen when creating a new project and adding a new writing system. (LT-15624)
				return false;

			var sharedBep = cache.ServiceLocator.GetInstance<IDataStorer>() as SharedXMLBackendProvider;
			if (sharedBep != null)
				return sharedBep.OtherApplicationsConnectedCount > 0;
			return false;
		}
	}
}
