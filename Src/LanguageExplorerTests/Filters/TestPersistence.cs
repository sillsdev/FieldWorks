// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.IO;
using System.Xml.Linq;
using LanguageExplorer.Filters;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.WritingSystems;

namespace LanguageExplorerTests.Filters
{
	/// <summary>
	/// Test persisting Filters objects using IPersistAsXml
	/// </summary>
	[TestFixture]
	public class TestFilterPersistence : MemoryOnlyBackendProviderTestBase
	{
		private ISilDataAccess m_sda;
		private DisposableObjectsSet<object> m_objectsToDispose;

		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}

			base.FixtureSetup();

			m_sda = Cache.DomainDataByFlid;
			m_objectsToDispose = new DisposableObjectsSet<object>();
		}

		public override void FixtureTeardown()
		{
			m_objectsToDispose.Dispose();

			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}
		}

		// Checklist of classes we need to test

		// Sorters:
		// RecordSorter (covered by many concrete classes, including GenRecordSorter in PersistSimpleSorter)
		// GenRecordSorter (PersistSimpleSorter)
		// PropertyRecordSorter (SortersEtc)

		// Comparers:
		// IcuComparer (PersistSimpleSorter)
		// LcmCompare (created only temporarily in PropertyRecordSorter, does not need persistence)
		// StringFinderCompare (SortersEtc)
		// ReverseComparer (SortersEtc)
		// IntStringComparer (SortersEtc)

		// Filters:
		// RecordFilter (abstract)
		// ProblemAnnotationFilter (PersistMatchersEtc)
		// FilterBarCellFilter (PersistMatchersEtc)
		// AndFilter (PersistMatchersEtc)
		// NullFilter (PersistMatchersEtc)

		// StringFinders:
		// StringFinderBase (abstract)
		// OwnIntPropFinder (PersistMatchersEtc)
		// OwnMlPropFinder (PersistMatchersEtc)
		// OwnMonoPropFinder (PersistMatchersEtc)
		// OneIndirectMlPropFinder (PersistMatchersEtc)
		// MultiIndirectMlPropFinder (PersistMatchersEtc)
		// OneIndirectAtomMlPropFinder (PersistMatchersEtc)
		// LayoutFinder (Todo: test in XmlViewsTests)

		// Matchers: (all in PersistMatchersEtc)

		/// <summary>
		/// A simple test to get us started, covers IcuComparer, GenRecordSorter, hence RecordSorter.
		/// </summary>
		[Test]
		public void PersistSimpleSorter()
		{
			string xml;
			IcuComparer icomp = new IcuComparer("fr");
			GenRecordSorter grs = new GenRecordSorter(icomp);
			xml = DynamicLoader.PersistObject(grs, "sorter");
			var doc = XDocument.Parse(xml);
			Assert.IsTrue("sorter" == doc.Root.Name);
			object obj = DynamicLoader.RestoreObject(doc.Root);
			try
			{
				Assert.IsInstanceOf<GenRecordSorter>(obj);
				GenRecordSorter grsOut = obj as GenRecordSorter;
				IComparer compOut = grsOut.Comparer;
				Assert.IsTrue(compOut is IcuComparer);
				Assert.AreEqual("fr", (compOut as IcuComparer).WsCode);
			}
			finally
			{
				var disposable = obj as IDisposable;
				disposable?.Dispose();
			}
		}

		/// <summary>
		/// Tests storing multiple sorters using the AndSorter class
		/// </summary>
		[Test]
		public void PersistAndSorter()
		{
			IcuComparer icomp1 = new IcuComparer("fr"), icomp2 = new IcuComparer("en");
			GenRecordSorter grs1 = new GenRecordSorter(icomp1), grs2 = new GenRecordSorter(icomp2);
			var sorters = new ArrayList();
			sorters.Add(grs1);
			sorters.Add(grs2);
			var asorter = new AndSorter(sorters);
			var xml = DynamicLoader.PersistObject(asorter, "sorter");
			var doc = XDocument.Parse(xml);
			Assert.IsTrue("sorter" == doc.Root.Name);
			var obj = DynamicLoader.RestoreObject(doc.Root);
			m_objectsToDispose.Add(obj);
			Assert.IsInstanceOf<AndSorter>(obj);
			var sortersOut = (obj as AndSorter).Sorters;
			var grsOut1 = sortersOut[0] as GenRecordSorter;
			var grsOut2 = sortersOut[1] as GenRecordSorter;
			var compOut1 = grsOut1.Comparer;
			var compOut2 = grsOut2.Comparer;
			Assert.IsTrue(compOut1 is IcuComparer);
			Assert.IsTrue(compOut2 is IcuComparer);
			Assert.AreEqual("fr", (compOut1 as IcuComparer).WsCode);
			Assert.AreEqual("en", (compOut2 as IcuComparer).WsCode);
		}

		/// <summary>
		/// Get the matcher from the FilterBarCellFilter which is the index'th filter of the AndFilter
		/// </summary>
		private static IMatcher GetMatcher(AndFilter andFilter, int index)
		{
			return ((FilterBarCellFilter)andFilter.Filters[index]).Matcher;
		}

		/// <summary>
		/// Get the finder from the FilterBarCellFilter which is the index'th filter of the AndFilter
		/// </summary>
		IStringFinder GetFinder(AndFilter andFilter, int index)
		{
			return (andFilter.Filters[index] as FilterBarCellFilter).Finder;
		}

		/// <summary>
		/// This covers all kinds of matchers
		/// </summary>
		[Test]
		public void PersistMatchersEtc()
		{
			var defAnalWs = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			// BaseMatcher is abstract
			// IntMatcher is abstract
			var rangeIntMatch = new RangeIntMatcher(5, 23);
			rangeIntMatch.WritingSystemFactory = Cache.WritingSystemFactory;
			var tssLabel = TsStringUtils.MakeString("label1", defAnalWs.Handle);
			rangeIntMatch.Label = tssLabel;
			var ownIntFinder = new OwnIntPropFinder(m_sda, 551);

			var rangeIntFilter = new FilterBarCellFilter(ownIntFinder, rangeIntMatch);
			m_objectsToDispose.Add(rangeIntFilter);
			var andFilter = new AndFilter();
			m_objectsToDispose.Add(andFilter);

			andFilter.Add(rangeIntFilter);

			var ws = defAnalWs.Handle;
			IVwPattern pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;

			var otherFilter = new FilterBarCellFilter(ownIntFinder, new NotEqualIntMatcher(77));
			m_objectsToDispose.Add(otherFilter);

			andFilter.Add(otherFilter);

			var mlPropFinder = new OwnMlPropFinder(m_sda, 788, 23);
			pattern.Pattern = TsStringUtils.MakeString("hello", ws);
			var filter = new FilterBarCellFilter(mlPropFinder, new ExactMatcher(pattern));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			var monoPropFinder = new OwnMonoPropFinder(m_sda, 954);
			pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;
			pattern.Pattern = TsStringUtils.MakeString("goodbye", ws);
			filter = new FilterBarCellFilter(monoPropFinder, new BeginMatcher(pattern));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			var oneIndMlPropFinder = new OneIndirectMlPropFinder(m_sda, 221, 222, 27);
			pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;
			pattern.Pattern = TsStringUtils.MakeString("exit", ws);
			filter = new FilterBarCellFilter(oneIndMlPropFinder, new EndMatcher(pattern));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			var mimlPropFinder = new MultiIndirectMlPropFinder(m_sda, new[] {444, 555}, 666, 87);
			pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;
			pattern.Pattern = TsStringUtils.MakeString("whatever", ws);
			filter = new FilterBarCellFilter(mimlPropFinder, new AnywhereMatcher(pattern));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			var oneIndAtomFinder = new OneIndirectAtomMlPropFinder(m_sda, 543, 345, 43);
			filter = new FilterBarCellFilter(oneIndAtomFinder, new BlankMatcher());
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			filter = new FilterBarCellFilter(oneIndAtomFinder, new NonBlankMatcher());
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;
			pattern.Pattern = TsStringUtils.MakeString("pattern", ws);
			filter = new FilterBarCellFilter(oneIndAtomFinder, new InvertMatcher(new RegExpMatcher(pattern)));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			andFilter.Add(new NullFilter());

			var docPaf = XDocument.Parse("<root targetClasses=\"LexEntry, LexSense\"></root>");
			var paf = new ProblemAnnotationFilter();
			paf.Init(Cache, docPaf.Root);
			andFilter.Add(paf);

			// Save and restore!
			var xml = DynamicLoader.PersistObject(andFilter, "filter");
			var doc = XDocument.Parse(xml);

			// And check all the pieces...
			var andFilterOut = DynamicLoader.RestoreObject(doc.Root) as AndFilter;
			m_objectsToDispose.Add(andFilterOut);
			andFilterOut.Cache = Cache;

			Assert.IsNotNull(andFilterOut);

			var rangeIntFilterOut = andFilterOut.Filters[0] as FilterBarCellFilter;
			// todo
			Assert.IsNotNull(rangeIntFilterOut);

			var ownIntFinderOut = rangeIntFilterOut.Finder as OwnIntPropFinder;
			Assert.IsNotNull(ownIntFinderOut);
			Assert.AreEqual(551, ownIntFinderOut.Flid);

			var rangeIntMatchOut = rangeIntFilterOut.Matcher as RangeIntMatcher;
			Assert.IsNotNull(rangeIntMatchOut);
			Assert.AreEqual(5, rangeIntMatchOut.Min);
			Assert.AreEqual(23, rangeIntMatchOut.Max);
			Assert.IsTrue(tssLabel.Equals(rangeIntMatchOut.Label));

			var notEqualMatchOut = GetMatcher(andFilter, 1) as NotEqualIntMatcher;
			Assert.IsNotNull(notEqualMatchOut);
			Assert.AreEqual(77, notEqualMatchOut.NotEqualValue);

			var exactMatchOut = GetMatcher(andFilter, 2) as ExactMatcher;
			Assert.IsNotNull(exactMatchOut);
			Assert.AreEqual("hello", exactMatchOut.Pattern.Pattern.Text);

			var beginMatchOut = GetMatcher(andFilter, 3) as BeginMatcher;
			Assert.IsNotNull(beginMatchOut);
			Assert.AreEqual("goodbye", beginMatchOut.Pattern.Pattern.Text);

			var endMatchOut = GetMatcher(andFilter, 4) as EndMatcher;
			Assert.IsNotNull(endMatchOut);
			Assert.AreEqual("exit", endMatchOut.Pattern.Pattern.Text);

			var anywhereMatchOut = GetMatcher(andFilter, 5) as AnywhereMatcher;
			Assert.IsNotNull(anywhereMatchOut);
			Assert.AreEqual("whatever", anywhereMatchOut.Pattern.Pattern.Text);

			var blankMatchOut = GetMatcher(andFilter, 6) as BlankMatcher;
			Assert.IsNotNull(blankMatchOut);

			var nonBlankMatchOut = GetMatcher(andFilter, 7) as NonBlankMatcher;
			Assert.IsNotNull(nonBlankMatchOut);

			var invertMatchOut = GetMatcher(andFilter, 8) as InvertMatcher;
			Assert.IsNotNull(invertMatchOut);

			var mlPropFinderOut = GetFinder(andFilter, 2) as OwnMlPropFinder;
			Assert.AreEqual(m_sda, mlPropFinderOut.DataAccess);
			Assert.AreEqual(788, mlPropFinderOut.Flid);
			Assert.AreEqual(23, mlPropFinderOut.Ws);

			var monoPropFinderOut = GetFinder(andFilter, 3) as OwnMonoPropFinder;
			Assert.AreEqual(m_sda, monoPropFinderOut.DataAccess);
			Assert.AreEqual(954, monoPropFinderOut.Flid);

			var oneIndMlPropFinderOut = GetFinder(andFilter, 4) as OneIndirectMlPropFinder;
			Assert.AreEqual(m_sda, oneIndMlPropFinderOut.DataAccess);
			Assert.AreEqual(221, oneIndMlPropFinderOut.FlidVec);
			Assert.AreEqual(222, oneIndMlPropFinderOut.FlidString);
			Assert.AreEqual(27, oneIndMlPropFinderOut.Ws);

			var mimlPropFinderOut = GetFinder(andFilter, 5) as MultiIndirectMlPropFinder;
			Assert.AreEqual(m_sda, mimlPropFinderOut.DataAccess);
			Assert.AreEqual(444, mimlPropFinderOut.VecFlids[0]);
			Assert.AreEqual(555, mimlPropFinderOut.VecFlids[1]);
			Assert.AreEqual(666, mimlPropFinderOut.FlidString);
			Assert.AreEqual(87, mimlPropFinderOut.Ws);

			var oneIndAtomFinderOut = GetFinder(andFilter, 6) as OneIndirectAtomMlPropFinder;
			Assert.AreEqual(m_sda, oneIndAtomFinderOut.DataAccess);
			Assert.AreEqual(543, oneIndAtomFinderOut.FlidAtom);
			Assert.AreEqual(345, oneIndAtomFinderOut.FlidString);
			Assert.AreEqual(43, oneIndAtomFinderOut.Ws);

			// 7, 8 are duplicates

			var nullFilterOut = andFilter.Filters[9] as NullFilter;
			Assert.IsNotNull(nullFilterOut);

			var pafOut = andFilter.Filters[10] as ProblemAnnotationFilter;
			Assert.IsNotNull(pafOut);
			Assert.AreEqual(5002, pafOut.ClassIds[0]);
			Assert.AreEqual(5016, pafOut.ClassIds[1]);
		}

		[Test]
		public void SortersEtc()
		{
			var prs = new PropertyRecordSorter("longName");
			// Save and restore!
			var xml = DynamicLoader.PersistObject(prs, "sorter");
			var doc = XDocument.Parse(xml);

			// And check all the pieces...
			var prsOut = DynamicLoader.RestoreObject(doc.Root) as PropertyRecordSorter;
			prsOut.Cache = Cache;
			Assert.AreEqual("longName", prsOut.PropertyName);
		}

		[Test]
		public void PersistReverseComparer()
		{
			// Putting an IntStringComparer here is utterly bizarre, but it tests out one more class.
			var sfComp = new StringFinderCompare(new OwnMonoPropFinder(m_sda, 445), new ReverseComparer(new IntStringComparer()));
			sfComp.SortedFromEnd = true;
			// Save and restore!
			var xml = DynamicLoader.PersistObject(sfComp, "comparer");
			var doc = XDocument.Parse(xml);
			// And check all the pieces...
			var sfCompOut = DynamicLoader.RestoreObject(doc.Root) as StringFinderCompare;
			m_objectsToDispose.Add(sfCompOut);
			sfCompOut.Cache = Cache;

			Assert.IsTrue(sfCompOut.Finder is OwnMonoPropFinder);
			Assert.IsTrue(sfCompOut.SubComparer is ReverseComparer);
			Assert.IsTrue(sfCompOut.SortedFromEnd);

			ReverseComparer rcOut = sfCompOut.SubComparer as ReverseComparer;
			Assert.IsTrue(rcOut.SubComp is IntStringComparer);
		}
	}

	/// <summary>
	/// Tests persisting a list of ManyOnePathSortItems
	/// </summary>
	[TestFixture]
	public class ManyOnePathSortItemsPersistenceTests : MemoryOnlyBackendProviderTestBase
	{
		private ArrayList m_list;
		private ILexEntry m_le1;
		private ILexEntry m_le2;

		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}

			base.FixtureSetup();
		}

		public override void FixtureTeardown()
		{
			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}
		}

		public override void TestSetup()
		{
			base.TestSetup();

			IManyOnePathSortItem mopsi = new ManyOnePathSortItem(Cache.LangProject);
			m_list = new ArrayList();
			m_list.Add(mopsi);
			var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			UndoableUnitOfWorkHelper.Do("undoit", "redoit", Cache.ActionHandlerAccessor, () =>
			{
				m_le1 = leFactory.Create();
				m_le2 = leFactory.Create();
			});
			mopsi = new ManyOnePathSortItem(Cache.LangProject.LexDbOA.Hvo, new int[] { m_le1.Hvo, m_le2.Hvo }, new int[] { 2, 3 });
			m_list.Add(mopsi);
		}

		/// <summary>
		/// Test persisting a list of ManyOnePathSortItems.
		/// </summary>
		[Test]
		public void PersistMopsiList()
		{
			var mopsi = (IManyOnePathSortItem)m_list[m_list.Count - 1];
			using (var stream = new MemoryStream())
			{
				var objRepo = Cache.ServiceLocator.ObjectRepository;
				var originalPersistData = mopsi.PersistData(objRepo);
				using (var writer = new StreamWriter(stream))
				{
					ManyOnePathSortItem.WriteItems(m_list, writer, objRepo);
					stream.Seek(0, SeekOrigin.Begin);
					using (var reader = new StreamReader(stream))
					{
						var items = ManyOnePathSortItem.ReadItems(reader, objRepo);
						Assert.That(items.Count, Is.EqualTo(m_list.Count));
						mopsi = (IManyOnePathSortItem)items[0];
						Assert.That(mopsi.KeyObject, Is.EqualTo(Cache.LangProject.Hvo));
						Assert.That(mopsi.PathLength, Is.EqualTo(0));
						// Root object is key object, if no path.
						Assert.That(mopsi.RootObjectHvo, Is.EqualTo(Cache.LangProject.Hvo));
						Assert.That(mopsi.RootObjectUsing(Cache), Is.EqualTo(Cache.LangProject));
						// PathObject(0) is also the key, if no path.
						Assert.That(mopsi.PathObject(0), Is.EqualTo(Cache.LangProject.Hvo));
						mopsi = (IManyOnePathSortItem)items[1];
						Assert.That(mopsi.KeyObject, Is.EqualTo(Cache.LangProject.LexDbOA.Hvo));
						Assert.That(mopsi.PathLength, Is.EqualTo(2));
						Assert.That(mopsi.PathFlid(0), Is.EqualTo(2));
						Assert.That(mopsi.PathFlid(1), Is.EqualTo(3));
						Assert.That(mopsi.PathObject(0), Is.EqualTo(m_le1.Hvo));
						Assert.That(mopsi.PathObject(1), Is.EqualTo(m_le2.Hvo));
						Assert.That(mopsi.PathObject(2), Is.EqualTo(Cache.LangProject.LexDbOA.Hvo), "Index one too large yields key object.");
						Assert.That(mopsi.RootObjectHvo, Is.EqualTo(m_le1.Hvo));
						Assert.That(mopsi.RootObjectUsing(Cache), Is.EqualTo(m_le1));
						Assert.That(mopsi.KeyObjectUsing(Cache), Is.EqualTo(Cache.LangProject.LexDbOA));
						Assert.That(mopsi.PersistData(objRepo), Is.EqualTo(originalPersistData));
					}
				}
			}
		}

		/// <summary>
		/// Test persisting a list of ManyOnePathSortItems.
		/// </summary>
		[Test]
		public void PersistMopsiList_BadGUID()
		{
			// Now make one containing a bad GUID.
			using (var stream = new MemoryStream())
			{
				var objRepo = Cache.ServiceLocator.ObjectRepository;
				using (var writer = new StreamWriter(stream))
				{
					ManyOnePathSortItem.WriteItems(m_list, writer, objRepo);
					writer.WriteLine(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
					// fake item, bad guid
					writer.Flush();
					stream.Seek(0, SeekOrigin.Begin);
					using (var reader = new StreamReader(stream))
					{
						var items = ManyOnePathSortItem.ReadItems(reader, objRepo);
						Assert.That(items, Is.Null);
					}
				}
			}
		}
	}
}
