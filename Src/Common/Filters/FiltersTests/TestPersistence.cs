using System;
using System.Collections;
using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.Filters
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
			base.FixtureSetup();

			m_sda = Cache.DomainDataByFlid;
			m_objectsToDispose = new DisposableObjectsSet<object>();
		}

		public override void FixtureTeardown()
		{
			m_objectsToDispose.Dispose();

			base.FixtureTeardown();
		}

		// Checklist of classes we need to test

		// Sorters:
		// RecordSorter (covered by many concrete classes, including GenRecordSorter in PersistSimpleSorter)
		// GenRecordSorter (PersistSimpleSorter)
		// PropertyRecordSorter (SortersEtc)

		// Comparers:
		// IcuComparer (PersistSimpleSorter)
		// FdoCompare (created only temporarily in PropertyRecordSorter, does not need persistence)
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
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			Assert.AreEqual("sorter", doc.DocumentElement.Name);
			object obj = DynamicLoader.RestoreObject(doc.DocumentElement);
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
				if (disposable != null)
					disposable.Dispose();
			}
		}

		/// <summary>
		/// Tests storing multiple sorters using the AndSorter class
		/// </summary>
		[Test]
		public void PersistAndSorter()
		{
			string xml;
			IcuComparer icomp1 = new IcuComparer("fr"), icomp2 = new IcuComparer("en");
			GenRecordSorter grs1 = new GenRecordSorter(icomp1), grs2 = new GenRecordSorter(icomp2);
			ArrayList sorters = new ArrayList();
			sorters.Add(grs1);
			sorters.Add(grs2);
			AndSorter asorter = new AndSorter(sorters);
			xml = DynamicLoader.PersistObject(asorter, "sorter");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			Assert.AreEqual("sorter", doc.DocumentElement.Name);
			object obj = DynamicLoader.RestoreObject(doc.DocumentElement);
			m_objectsToDispose.Add(obj);
			Assert.IsInstanceOf<AndSorter>(obj);
			ArrayList sortersOut = (obj as AndSorter).Sorters;
			GenRecordSorter grsOut1 = sortersOut[0] as GenRecordSorter;
			GenRecordSorter grsOut2 = sortersOut[1] as GenRecordSorter;
			IComparer compOut1 = grsOut1.Comparer;
			IComparer compOut2 = grsOut2.Comparer;
			Assert.IsTrue(compOut1 is IcuComparer);
			Assert.IsTrue(compOut2 is IcuComparer);
			Assert.AreEqual("fr", (compOut1 as IcuComparer).WsCode);
			Assert.AreEqual("en", (compOut2 as IcuComparer).WsCode);
		}

		/// <summary>
		/// Get the matcher from the FilterBarCellFilter which is the index'th filter of the AndFilter
		/// </summary>
		/// <param name="andFilter"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		IMatcher GetMatcher(AndFilter andFilter, int index)
		{
			FilterBarCellFilter filter = andFilter.Filters[index] as FilterBarCellFilter;
			return filter.Matcher;
		}

		/// <summary>
		/// Get the finder from the FilterBarCellFilter which is the index'th filter of the AndFilter
		/// </summary>
		/// <param name="andFilter"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		IStringFinder GetFinder(AndFilter andFilter, int index)
		{
			FilterBarCellFilter filter = andFilter.Filters[index] as FilterBarCellFilter;
			return filter.Finder;
		}

		/// <summary>
		/// This covers all kinds of matchers
		/// </summary>
		[Test]
		public void PersistMatchersEtc()
		{
			WritingSystem defAnalWs = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			// BaseMatcher is abstract
			// IntMatcher is abstract
			RangeIntMatcher rangeIntMatch = new RangeIntMatcher(5, 23);
			rangeIntMatch.WritingSystemFactory = Cache.WritingSystemFactory;
			ITsString tssLabel = Cache.TsStrFactory.MakeString("label1", defAnalWs.Handle);
			rangeIntMatch.Label = tssLabel;
			OwnIntPropFinder ownIntFinder = new OwnIntPropFinder(m_sda, 551);

			var rangeIntFilter = new FilterBarCellFilter(ownIntFinder, rangeIntMatch);
			m_objectsToDispose.Add(rangeIntFilter);
			AndFilter andFilter = new AndFilter();
			m_objectsToDispose.Add(andFilter);

			andFilter.Add(rangeIntFilter);

			ITsStrFactory tsf = Cache.TsStrFactory;
			int ws = defAnalWs.Handle;
			IVwPattern m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;

			var otherFilter = new FilterBarCellFilter(ownIntFinder, new NotEqualIntMatcher(77));
			m_objectsToDispose.Add(otherFilter);

			andFilter.Add(otherFilter);

			OwnMlPropFinder mlPropFinder = new OwnMlPropFinder(m_sda, 788, 23);
			m_pattern.Pattern = tsf.MakeString("hello", ws);
			var filter = new FilterBarCellFilter(mlPropFinder, new ExactMatcher(m_pattern));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			OwnMonoPropFinder monoPropFinder = new OwnMonoPropFinder(m_sda, 954);
			m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString("goodbye", ws);
			filter = new FilterBarCellFilter(monoPropFinder, new BeginMatcher(m_pattern));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			OneIndirectMlPropFinder oneIndMlPropFinder =
				new OneIndirectMlPropFinder(m_sda, 221, 222, 27);
			m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString("exit", ws);
			filter = new FilterBarCellFilter(oneIndMlPropFinder, new EndMatcher(m_pattern));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			MultiIndirectMlPropFinder mimlPropFinder = new MultiIndirectMlPropFinder(
				m_sda, new int[] {444, 555}, 666, 87);
			m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString("whatever", ws);
			filter = new FilterBarCellFilter(mimlPropFinder, new AnywhereMatcher(m_pattern));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			OneIndirectAtomMlPropFinder oneIndAtomFinder =
				new OneIndirectAtomMlPropFinder(m_sda, 543, 345, 43);
			filter = new FilterBarCellFilter(oneIndAtomFinder, new BlankMatcher());
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			filter = new FilterBarCellFilter(oneIndAtomFinder, new NonBlankMatcher());
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString("pattern", ws);
			filter = new FilterBarCellFilter(oneIndAtomFinder, new InvertMatcher(new RegExpMatcher(m_pattern)));
			m_objectsToDispose.Add(filter);
			andFilter.Add(filter);

			andFilter.Add(new NullFilter());

			XmlDocument docPaf = new XmlDocument();
			docPaf.LoadXml("<root targetClasses=\"LexEntry, LexSense\"></root>");
			ProblemAnnotationFilter paf = new ProblemAnnotationFilter();
			paf.Init(Cache, docPaf.DocumentElement);
			andFilter.Add(paf);

			// Save and restore!
			string xml = DynamicLoader.PersistObject(andFilter, "filter");

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);

			// And check all the pieces...
			var andFilterOut = DynamicLoader.RestoreObject(doc.DocumentElement) as AndFilter;
			m_objectsToDispose.Add(andFilterOut);
			andFilterOut.Cache = Cache;

			Assert.IsNotNull(andFilterOut);

			FilterBarCellFilter rangeIntFilterOut =
				andFilterOut.Filters[0] as FilterBarCellFilter;
			// todo
			Assert.IsNotNull(rangeIntFilterOut);

			OwnIntPropFinder ownIntFinderOut = rangeIntFilterOut.Finder as OwnIntPropFinder;
			Assert.IsNotNull(ownIntFinderOut);
			Assert.AreEqual(551, ownIntFinderOut.Flid);

			RangeIntMatcher rangeIntMatchOut = rangeIntFilterOut.Matcher as RangeIntMatcher;
			Assert.IsNotNull(rangeIntMatchOut);
			Assert.AreEqual(5, rangeIntMatchOut.Min);
			Assert.AreEqual(23, rangeIntMatchOut.Max);
			Assert.IsTrue(tssLabel.Equals(rangeIntMatchOut.Label));

			NotEqualIntMatcher notEqualMatchOut = GetMatcher(andFilter, 1) as NotEqualIntMatcher;
			Assert.IsNotNull(notEqualMatchOut);
			Assert.AreEqual(77, notEqualMatchOut.NotEqualValue);

			ExactMatcher exactMatchOut = GetMatcher(andFilter, 2) as ExactMatcher;
			Assert.IsNotNull(exactMatchOut);
			Assert.AreEqual("hello", exactMatchOut.Pattern.Pattern.Text);

			BeginMatcher beginMatchOut = GetMatcher(andFilter, 3) as BeginMatcher;
			Assert.IsNotNull(beginMatchOut);
			Assert.AreEqual("goodbye", beginMatchOut.Pattern.Pattern.Text);

			EndMatcher endMatchOut = GetMatcher(andFilter, 4) as EndMatcher;
			Assert.IsNotNull(endMatchOut);
			Assert.AreEqual("exit", endMatchOut.Pattern.Pattern.Text);

			AnywhereMatcher anywhereMatchOut = GetMatcher(andFilter, 5) as AnywhereMatcher;
			Assert.IsNotNull(anywhereMatchOut);
			Assert.AreEqual("whatever", anywhereMatchOut.Pattern.Pattern.Text);

			BlankMatcher blankMatchOut = GetMatcher(andFilter, 6) as BlankMatcher;
			Assert.IsNotNull(blankMatchOut);

			NonBlankMatcher nonBlankMatchOut = GetMatcher(andFilter, 7) as NonBlankMatcher;
			Assert.IsNotNull(nonBlankMatchOut);

			InvertMatcher invertMatchOut = GetMatcher(andFilter, 8) as InvertMatcher;
			Assert.IsNotNull(invertMatchOut);

			OwnMlPropFinder mlPropFinderOut = GetFinder(andFilter, 2) as OwnMlPropFinder;
			Assert.AreEqual(m_sda, mlPropFinderOut.DataAccess);
			Assert.AreEqual(788, mlPropFinderOut.Flid);
			Assert.AreEqual(23, mlPropFinderOut.Ws);

			OwnMonoPropFinder monoPropFinderOut = GetFinder(andFilter, 3) as OwnMonoPropFinder;
			Assert.AreEqual(m_sda, monoPropFinderOut.DataAccess);
			Assert.AreEqual(954, monoPropFinderOut.Flid);

			OneIndirectMlPropFinder oneIndMlPropFinderOut =
				GetFinder(andFilter, 4) as OneIndirectMlPropFinder;
			Assert.AreEqual(m_sda, oneIndMlPropFinderOut.DataAccess);
			Assert.AreEqual(221, oneIndMlPropFinderOut.FlidVec);
			Assert.AreEqual(222, oneIndMlPropFinderOut.FlidString);
			Assert.AreEqual(27, oneIndMlPropFinderOut.Ws);

			MultiIndirectMlPropFinder mimlPropFinderOut =
				GetFinder(andFilter, 5) as MultiIndirectMlPropFinder;
			Assert.AreEqual(m_sda, mimlPropFinderOut.DataAccess);
			Assert.AreEqual(444, mimlPropFinderOut.VecFlids[0]);
			Assert.AreEqual(555, mimlPropFinderOut.VecFlids[1]);
			Assert.AreEqual(666, mimlPropFinderOut.FlidString);
			Assert.AreEqual(87, mimlPropFinderOut.Ws);

			OneIndirectAtomMlPropFinder oneIndAtomFinderOut =
				GetFinder(andFilter, 6) as OneIndirectAtomMlPropFinder;
			Assert.AreEqual(m_sda, oneIndAtomFinderOut.DataAccess);
			Assert.AreEqual(543, oneIndAtomFinderOut.FlidAtom);
			Assert.AreEqual(345, oneIndAtomFinderOut.FlidString);
			Assert.AreEqual(43, oneIndAtomFinderOut.Ws);

			// 7, 8 are duplicates

			NullFilter nullFilterOut = andFilter.Filters[9] as NullFilter;
			Assert.IsNotNull(nullFilterOut);

			ProblemAnnotationFilter pafOut = andFilter.Filters[10] as ProblemAnnotationFilter;
			Assert.IsNotNull(pafOut);
			Assert.AreEqual(5002, pafOut.ClassIds[0]);
			Assert.AreEqual(5016, pafOut.ClassIds[1]);
		}

		[Test]
		public void SortersEtc()
		{
			PropertyRecordSorter prs = new PropertyRecordSorter("longName");
			// Save and restore!
			string xml = DynamicLoader.PersistObject(prs, "sorter");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);

			// And check all the pieces...
			PropertyRecordSorter prsOut = DynamicLoader.RestoreObject(doc.DocumentElement) as PropertyRecordSorter;
			prsOut.Cache = Cache;
			Assert.AreEqual("longName", prsOut.PropertyName);
		}

		[Test]
		public void PersistReverseComparer()
		{
			string xml;
			// Putting an IntStringComparer here is utterly bizarre, but it tests out one more class.
			StringFinderCompare sfComp = new StringFinderCompare(new OwnMonoPropFinder(m_sda, 445),
				new ReverseComparer(new IntStringComparer()));
			sfComp.SortedFromEnd = true;
			// Save and restore!
			xml = DynamicLoader.PersistObject(sfComp, "comparer");
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			// And check all the pieces...
			var sfCompOut = DynamicLoader.RestoreObject(doc.DocumentElement) as StringFinderCompare;
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
		private ISilDataAccess m_sda;
		private ArrayList m_list;
		private ILexEntry m_le1;
		private ILexEntry m_le2;

		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_sda = Cache.DomainDataByFlid;
		}

		public override void FixtureTeardown()
		{
			m_sda = null;

			base.FixtureTeardown();
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
					ManyOnePathSortItem.WriteItems(m_list, writer, objRepo, null);
					stream.Seek(0, SeekOrigin.Begin);
					using (var reader = new StreamReader(stream))
					{
						string versionStamp;
						var items = ManyOnePathSortItem.ReadItems(reader, objRepo, out versionStamp);
						Assert.That(items.Count, Is.EqualTo(m_list.Count));
						Assert.That(versionStamp, Is.Null);
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
					ManyOnePathSortItem.WriteItems(m_list, writer, objRepo, "123");
					writer.WriteLine(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
					// fake item, bad guid
					writer.Flush();
					stream.Seek(0, SeekOrigin.Begin);
					using (var reader = new StreamReader(stream))
					{
						string versionStamp;
						var items = ManyOnePathSortItem.ReadItems(reader, objRepo, out versionStamp);
						Assert.That(items, Is.Null);
						Assert.That(versionStamp, Is.EqualTo("123"));
					}
				}
			}
		}
	}
}
