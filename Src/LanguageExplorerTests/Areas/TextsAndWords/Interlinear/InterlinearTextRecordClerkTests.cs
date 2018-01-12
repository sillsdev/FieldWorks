// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
#if RANDYTODO
	[TestFixture]
	public class InterlinearTextRecordClerkTests : MemoryOnlyBackendProviderTestBase, IDisposable
	{
		private IStText m_stText;
		private MockFwXApp m_application;
		private MockFwXWindow m_window;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DoFixtureSetup);
		}

		private void DoFixtureSetup()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_propertyTable = m_window.PropTable;
		}

	#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				m_application?.Dispose();
				m_window?.Dispose();
				m_propertyTable?.Dispose();
			}
			m_application = null;
			m_window = null;
			m_propertyTable = null;
		}

		~InterlinearTextRecordClerkTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
	#endregion disposal

		[Test]
		public void CreateStTextShouldAlsoCreateDsConstChart()
		{
			using (var interlinTextRecordClerk = new InterlinearTextRecordClerkDerived(m_mediator, m_propertyTable))
			{
				var discourseData = Cache.LangProject.DiscourseDataOA;
				Assert.IsNull(discourseData);
				interlinTextRecordClerk.CreateStText(Cache);
				Assert.True(Cache.LangProject.DiscourseDataOA.ChartsOC.Any());
			}
		}

	#region Test Classes
		private class InterlinearTextRecordClerkDerived : InterlinearTextsRecordClerk
		{
			public InterlinearTextRecordClerkDerived(Mediator mediator, PropertyTable propertyTable)
			{
				m_mediator = mediator;
				m_propertyTable = propertyTable;
			}

			public void CreateStText(LcmCache cache)
			{
				CreateAndInsertStText createAndInsertStText = new NonUndoableCreateAndInsertStText(cache, this);
				XmlDocument xmlDoc = new XmlDocument();
				// xml taken from a dummy project
				string outerXmlFromProject = "<recordList owner=\"LangProject\" property=\"InterestingTexts\"><!-- We use a decorator here so it can override certain virtual properties and limit occurrences to interesting texts. --><decoratorClass assemblyPath=\"xWorks.dll\" class=\"LanguageExplorer.Works.InterestingTextsDecorator\" /></recordList>";
				xmlDoc.LoadXml(outerXmlFromProject);
				XmlNode root = xmlDoc.FirstChild;
				XmlNode attr = xmlDoc.CreateNode(XmlNodeType.Attribute, "property", "InterestingTexts");
				root.Attributes.SetNamedItem(attr);
				m_list = RecordList.Create(cache, m_mediator, m_propertyTable, root);
				createAndInsertStText.Create();
			}
		}
	#endregion Test Classes
	}
#endif
}