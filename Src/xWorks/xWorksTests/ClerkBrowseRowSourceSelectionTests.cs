// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// REAL-DOMAIN headless test pinning the browse seam's INDEX CONTRACT (load-bearing — a wrong map
	/// navigates to the wrong record). The selection mirror is bidirectional and the two directions MUST
	/// agree on what a row index means: <c>RecordBrowseView.MirrorClerkSelectionToAvalonia</c> pushes the
	/// clerk's current index AS the table row index (<c>SelectRow(Clerk.CurrentIndex)</c>), while a user
	/// click feeds the table row index back through <see cref="ClerkBrowseRowSource.HvoAt"/>. With the
	/// projection deleted and the seam made honestly pass-through (row index == clerk index), the round
	/// trip resolves the SAME object. This test would fail the moment a non-pass-through remap is
	/// re-introduced at the seam — exactly the latent wrong-record bug the cutover removed.
	/// </summary>
	[TestFixture]
	public class ClerkBrowseRowSourceSelectionTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
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

		// A minimal column source: the round trip exercises only the index→hvo path (RowCount, HvoAt),
		// not cell content, so the cell/sort/filter members are never reached here.
		private sealed class StubColumnSource : IBrowseColumnSource
		{
			public int ColumnCount => 0;
			public string GetColumnName(int icol) => null;
			public void GetColumnEditAttributes(int icol, out string field, out string ws, out string transduce)
			{ field = null; ws = null; transduce = null; }
			public bool IsColumnEditable(int icol) => false;
			public IReadOnlyList<BrowseColumnInfo> GetAvailableColumns() => Array.Empty<BrowseColumnInfo>();
			public string GetColumnKey(int icol) => null;
			public IReadOnlyList<string> GetRowCellStrings(IManyOnePathSortItem item) => Array.Empty<string>();
			public ITsString GetRowCellTsString(IManyOnePathSortItem item, int icol) => null;
			public RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending) => null;
			public RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending, bool sortedFromEnd, bool sortedByLength) => null;
			public RecordFilter MakeColumnFilter(int dataColumnIndex, BrowseColumnFilterKind kind, string text) => null;
			public RecordFilter MakePatternColumnFilter(int dataColumnIndex, string pattern, BrowsePatternMatchType matchType, bool matchCase) => null;
			public RecordFilter MakeStringListColumnFilter(int dataColumnIndex, string value, bool exclude) => null;
			public string[] GetColumnStringList(int dataColumnIndex) => null;
			public string GetColumnSpecAttribute(int icol, string attrName) => null;
			public string GetBulkEditSpecAttribute(string attrName) => null;
			public RecordFilter MakeDateColumnFilter(int dataColumnIndex, BrowseDateMatchKind kind, System.DateTime start, System.DateTime end, bool handleGenDate) => null;
			public IReadOnlyList<BrowseChooserItem> GetColumnChooserList(int dataColumnIndex) => null;
			public RecordFilter MakeListChoiceColumnFilter(int dataColumnIndex, IReadOnlyList<string> chosenKeys) => null;
			public bool ColumnSupportsSpellingFilter(int dataColumnIndex) => false;
			public RecordFilter MakeSpellingErrorColumnFilter(int dataColumnIndex) => null;
		}

		[Test]
		public void Selection_RoundTrips_RowIndexEqualsClerkIndex_ResolvesSameObject()
		{
			MakeEntry("alpha");
			MakeEntry("bravo");
			MakeEntry("charlie");

			var clerk = CreateLoadedEntriesClerk();
			try
			{
				var source = new ClerkBrowseRowSource(clerk, new StubColumnSource(), Cache);
				Assert.That(source.RowCount, Is.EqualTo(clerk.ListSize), "the seam is pass-through: row count == clerk list size");

				for (var clerkIndex = 0; clerkIndex < clerk.ListSize; clerkIndex++)
				{
					// Simulate the clerk→Avalonia mirror: navigate the clerk, then the mirror pushes
					// SelectRow(Clerk.CurrentIndex) — i.e. the table row index IS the clerk index.
					clerk.JumpToIndex(clerkIndex);
					var pushedRowIndex = clerk.CurrentIndex;
					Assert.That(pushedRowIndex, Is.EqualTo(clerkIndex), "the clerk index is what the mirror selects as the row");

					// Simulate the Avalonia→clerk direction: the user clicks that same row; OnAvaloniaRowSelected
					// reads it back through HvoAt. The hvo must be the clerk's current object — no remap.
					var hvoFromRow = source.HvoAt(pushedRowIndex);
					Assert.That(hvoFromRow, Is.EqualTo(clerk.CurrentObject.Hvo),
						"HvoAt(pushed row) resolves the SAME object the clerk is on — the two directions agree");
				}
			}
			finally
			{
				clerk.Dispose();
			}
		}

		[Test]
		public void HvoAt_OutOfRange_ReturnsZero()
		{
			MakeEntry("solo");
			var clerk = CreateLoadedEntriesClerk();
			try
			{
				var source = new ClerkBrowseRowSource(clerk, new StubColumnSource(), Cache);
				Assert.That(source.HvoAt(-1), Is.EqualTo(0));
				Assert.That(source.HvoAt(source.RowCount), Is.EqualTo(0));
			}
			finally
			{
				clerk.Dispose();
			}
		}
	}
}
