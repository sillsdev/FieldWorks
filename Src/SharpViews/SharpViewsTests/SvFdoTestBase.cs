using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// This class has an absolutely minimal set of functionality for running tests that require FDO-based objects,
	/// such as those that use ML string properties. I am deliberately not using the full FdoTestBase, because I want
	/// minimal dependency on FDO. In fact, I may break SharpViews into two assemblies, one dependent on FDO, and one not.
	/// </summary>
	public class SvFdoTestBase
	{
		private FdoCache m_cache;
		/// <summary></summary>
		protected bool m_internalRestart = false;

		/// <summary>
		/// Initializes a new instance of the <c>FdoTestBase</c> class.
		/// </summary>
		public SvFdoTestBase()
		{
			m_cache = FdoCache.CreateCache(FDOBackendProviderType.kMemoryOnly);
			// Init backend data provider
			var dataSetup = m_cache.ServiceLocator.GetInstance<IDataSetup>();
			dataSetup.StartupExtantLanguageProject(null);
			dataSetup.LoadDomain(BackendBulkLoadDomain.All);
			//(m_cache.MetaDataCacheAccessor as FdoMetaDataCache).AddVirtualClassesAndProps(
			//    new List<Type>(Assembly.GetExecutingAssembly().GetTypes()));

			m_cache.DomainDataByFlid.BeginNonUndoableTask();
			// Make French the vern WS.
			int frWs = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var french = Cache.ServiceLocator.GetInstance<ILgWritingSystemRepository>().GetObject(frWs);
			Cache.LanguageProject.VernWssRC.Add(french);
			Cache.LanguageProject.CurVernWssRS.Add(french);

			// Make English the analysis WS.
			int enWs = Cache.WritingSystemFactory.GetWsFromStr("en");
			var english = Cache.ServiceLocator.GetInstance<ILgWritingSystemRepository>().GetObject(enWs);
			Cache.LanguageProject.AnalysisWssRC.Add(english);
			Cache.LanguageProject.CurAnalysisWssRS.Add(english);
			m_cache.DomainDataByFlid.EndNonUndoableTask();
		}

		/// <summary>
		/// Get the FdoCache.
		/// </summary>
		protected FdoCache Cache
		{
			get { return m_cache; }
		}
	}
}
