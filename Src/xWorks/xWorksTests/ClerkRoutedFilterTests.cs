// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// REAL-DOMAIN headless integration test for the rendering-cutover's clerk-routed filter (F1): a real
	/// <see cref="RecordClerk"/> over an in-memory LCModel Entries list, driven through the same
	/// <see cref="RecordClerk.OnChangeFilter"/> path <c>ClerkBrowseRowSource</c> now uses — proving the
	/// actual list NARROWS on filter and RESTORES on clear (the behavior previously deferred to live
	/// verification), with no FieldWorks app window and no on-disk project. This is the domain-fidelity
	/// companion to the Avalonia-surface workflow tests (FwAvaloniaTests): together they cover the
	/// cutover end to end. The MockFwX(App|Window) bootstrap here is the reusable setup other phases copy
	/// for real-clerk integration tests.
	/// </summary>
	[TestFixture]
	public class ClerkRoutedFilterTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FwXApp m_application;
		private FwXWindow m_window;
		private PropertyTable m_propertyTable;
		private Mediator m_mediator;

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_mediator = m_window.Mediator;
			m_window.LoadUI(configFilePath);
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			m_window?.Dispose();
			m_application?.Dispose();
			m_window = null;
			m_application = null;
			m_propertyTable = null;
			m_mediator = null;
			FwRegistrySettings.Release();
			base.FixtureTeardown();
		}

		private ILexEntry MakeEntry(string lexeme)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = morph;
			morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(lexeme, Cache.DefaultVernWs));
			return entry;
		}

		// A real entries RecordClerk over LexDb.Entries, activated and loaded — the same list the lexicon
		// browse shows. Mirrors the proven ConfiguredXHTMLGeneratorTests/RecordListTests setup.
		private RecordClerk CreateLoadedEntriesClerk()
		{
			const string clerkXml = @"<?xml version='1.0' encoding='UTF-8'?>
				<root>
					<clerks>
						<clerk id='entries'><recordList owner='LexDb' property='Entries'/></clerk>
					</clerks>
					<tools>
						<tool label='Lexicon Edit' value='lexiconEdit' icon='DocumentView'>
							<control>
								<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.XhtmlDocView'/>
								<parameters area='lexicon' clerk='entries' layout='Bartholomew' editable='false'/>
							</control>
						</tool>
					</tools>
				</root>";
			var doc = new XmlDocument();
			doc.LoadXml(clerkXml);
			var clerkNode = doc.SelectSingleNode("//tools/tool[@label='Lexicon Edit']//parameters[@area='lexicon']");
			var clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false, false);
			clerk.ActivateUI(false);
			var list = (RecordList)clerk.GetType()
				.GetField("m_list", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(clerk);
			list.SetSuppressingLoadList(false);
			list.ReloadList();
			return clerk;
		}

		// A real RecordFilter that accepts entries whose lexeme form contains the needle — the clerk plumbing
		// (OnChangeFilter → ReloadList → Accept per item) is exactly what the browse cutover drives.
		private sealed class LexemeContainsFilter : RecordFilter
		{
			private readonly LcmCache _cache;
			private readonly string _needle;
			public LexemeContainsFilter(LcmCache cache, string needle) { _cache = cache; _needle = needle; }
			public override bool IsUserVisible => true;
			public override bool Accept(IManyOnePathSortItem item)
			{
				var entry = _cache.ServiceLocator.GetObject(item.RootObjectHvo) as ILexEntry;
				var form = entry?.LexemeFormOA?.Form?.get_String(_cache.DefaultVernWs)?.Text ?? string.Empty;
				return form.IndexOf(_needle, StringComparison.OrdinalIgnoreCase) >= 0;
			}
		}

		[Test]
		public void ClerkFilter_NarrowsTheRealList_AndClearingRestoresIt()
		{
			// The restored-for-each-test base keeps an undoable task open, so create objects directly
			// (a nested NonUndoableUnitOfWorkHelper would throw "Nested tasks are not supported").
			MakeEntry("cat");
			MakeEntry("car");
			MakeEntry("dog");

			var clerk = CreateLoadedEntriesClerk();
			try
			{
				Assert.That(clerk.ListSize, Is.EqualTo(3), "all entries load before filtering");

				var filter = new LexemeContainsFilter(Cache, "ca");
				clerk.OnChangeFilter(new FilterChangeEventArgs(filter, null));
				Assert.That(clerk.ListSize, Is.EqualTo(2), "the real list narrows to entries matching the filter (cat, car)");

				clerk.OnChangeFilter(new FilterChangeEventArgs(null, filter));
				Assert.That(clerk.ListSize, Is.EqualTo(3), "clearing the filter restores the full list");
			}
			finally
			{
				clerk.Dispose();
			}
		}

		[Test]
		public void ClerkFilter_NoMatches_EmptiesTheList()
		{
			MakeEntry("cat");
			MakeEntry("dog");

			var clerk = CreateLoadedEntriesClerk();
			try
			{
				clerk.OnChangeFilter(new FilterChangeEventArgs(new LexemeContainsFilter(Cache, "zzz"), null));
				Assert.That(clerk.ListSize, Is.EqualTo(0), "a filter matching nothing yields an empty list");
			}
			finally
			{
				clerk.Dispose();
			}
		}
	}
}
