// --------------------------------------------------------------------------------------------
// Copyright (C) 2010 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
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
			IFdoUI ui) : base(cache, identityMap, surrogateFactory, mdc, dataMigrationManager, ui)
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
