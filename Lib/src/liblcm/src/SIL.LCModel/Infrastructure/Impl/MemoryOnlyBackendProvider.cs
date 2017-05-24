// Copyright (c) 2008-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel.DomainServices.DataMigration;
using SIL.Lexicon;

namespace SIL.LCModel.Infrastructure.Impl
{
	/// <summary>
	/// A subclass of the BackendProvider
	/// which handles a strictly in-memory system, as might be used for most LCM tests.
	/// </summary>
	internal sealed class MemoryOnlyBackendProvider : BackendProvider
	{
		private readonly ISettingsStore m_projectSettingsStore;
		private readonly ISettingsStore m_userSettingsStore;

		/// <summary>
		/// Constructor.
		/// </summary>
		internal MemoryOnlyBackendProvider(LcmCache cache, IdentityMap identityMap, ICmObjectSurrogateFactory surrogateFactory,
			IFwMetaDataCacheManagedInternal mdc, IDataMigrationManager dataMigrationManager, ILcmUI ui, ILcmDirectories dirs, LcmSettings settings)
			: base(cache, identityMap, surrogateFactory, mdc, dataMigrationManager, ui, dirs, settings)
		{
			m_projectSettingsStore = new MemorySettingsStore();
			m_userSettingsStore = new MemorySettingsStore();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the BEP.
		/// </summary>
		/// <param name="currentModelVersion">The current model version.</param>
		/// <returns>The data store's current version number.</returns>
		/// ------------------------------------------------------------------------------------
		protected override int StartupInternal(int currentModelVersion)
		{
			// Nothing to worry about on the model version number,
			// since the data here must be using the current one.
			return ModelVersion;
		}

		/// <summary>
		/// Shutdown  the BEP.
		/// </summary>
		protected override void ShutdownInternal()
		{

		}

		//protected override void RemoveBackEnd()
		//{
		//    throw new NotImplementedException();
		//}

		///// <summary>
		///// Restore from a data migration, which used an XML BEP.
		///// </summary>
		///// <param name="xmlBepPathname"></param>
		//protected override void RestoreWithoutMigration(string xmlBepPathname)
		//{
		//    throw new NotImplementedException();
		//}

		/// <summary>
		/// Create a LangProject with the BEP.
		/// </summary>
		protected override void CreateInternal()
		{
			// Nothing to worry about on the model version number,
			// since the data here must be using the current one.
		}

		///// <summary>
		///// Gets the project path.  We need this so tests will pass.
		///// </summary>
		///// <value>The project path.</value>
		//public override string LocalProjectFolder
		//{
		//    get
		//    {
		//        string baseDir;
		//        if (MiscUtils.IsUnix)
		//            baseDir = "/DummyProjectDirectory";
		//        else
		//            baseDir = @"c:\FwProjects";

		//        return Path.Combine(baseDir, m_cache.ServiceLocator.DataSetup.DatabaseName);
		//    }
		//}

		/// <summary>
		/// Update the version number.
		/// </summary>
		protected override void UpdateVersionNumber()
		{
			// Do nothing.
		}

		/// <summary>
		/// Rename the database, which means renaming the files.
		/// </summary>
		public override bool RenameDatabase(string sNewBasename)
		{
			return RenameMyFiles(sNewBasename);
		}

		public override ISettingsStore ProjectSettingsStore
		{
			get { return m_projectSettingsStore; }
		}

		public override ISettingsStore UserSettingsStore
		{
			get { return m_userSettingsStore; }
		}

		/// <summary>
		/// Rather trivial implementation since there are no files involved...
		/// </summary>
		/// <returns>true</returns>
		private bool RenameMyFiles(string sNewBasename)
		{
			ProjectId.Path = sNewBasename + ".mem";
			return true;
		}

		public override bool Commit(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			IEnumerable<CustomFieldInfo> cfiList;
			if (!HaveAnythingToCommit(newbies, dirtballs, goners, out cfiList)) return true;
			return base.Commit(newbies, dirtballs, goners);
		}
	}
}
