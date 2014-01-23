// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ClientServerBackendProvider.cs
// Responsibility: Randy Regnier, Steve Miller
// Last reviewed: Never
// --------------------------------------------------------------------------------------------

/* This class may well replace ClientServerBackend, which currently is used only by the MySQL classes */

using System.Collections.Generic;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	abstract class ClientServerBackendProvider : FDOBackendProvider, IClientServerDataManager
	{
		protected ClientServerBackendProvider(FdoCache cache,
			IdentityMap identityMap,
			ICmObjectSurrogateFactory surrogateFactory,
			IFwMetaDataCacheManagedInternal mdc,
			IDataMigrationManager dataMigrationManager,
			IFdoUI ui, IFdoDirectories dirs) : base(cache, identityMap, surrogateFactory, mdc, dataMigrationManager, ui, dirs)
		{
		}

		/// <summary>
		/// Get changes we haven't seen.
		/// </summary>
		public abstract bool GetUnseenForeignChanges(out List<ICmObjectSurrogate> foreignNewbies,
			out List<ICmObjectSurrogate> foreignDirtballs,
			out List<ICmObjectId> foreignGoners, bool fGetCommitLock);

		public abstract string VersionStamp { get; }
		public abstract bool NewObjectsSinceVersion(string versionStamp, string classname);
	}
}
