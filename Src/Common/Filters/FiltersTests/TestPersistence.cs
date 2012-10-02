using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Filters;
using SIL.Utils;

using NUnit.Framework;

namespace FiltersTests
{
	/// <summary>
	/// Test persisting Filters objects using IPersistAsXml
	/// </summary>
	[TestFixture]
	public class TestPersistence
	{
		FdoCache m_cache;
		ISilDataAccess m_sda;
		public TestPersistence()
		{

		}

		[TestFixtureSetUp]
		public void Setup()
		{
			m_cache = FdoCache.Create("TestLangProj");
			m_sda = m_cache.MainCacheAccessor;
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			m_sda = null;
			m_cache.Dispose();
			m_cache = null;
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
		// WordSetFilter (currenly not tested, I can't find that this is used anywhere)

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
			IcuComparer icomp = new IcuComparer("fr");
			GenRecordSorter grs = new GenRecordSorter(icomp);
			string xml = DynamicLoader.PersistObject(grs, "sorter");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			Assert.AreEqual("sorter", doc.DocumentElement.Name);
			object obj = DynamicLoader.RestoreObject(doc.DocumentElement);
			Assert.IsTrue(obj is GenRecordSorter);
			GenRecordSorter grsOut = obj as GenRecordSorter;
			IComparer compOut = grsOut.Comparer;
			Assert.IsTrue(compOut is IcuComparer);
			Assert.AreEqual("fr", (compOut as IcuComparer).WsCode);
		}

		/// <summary>
		/// Tests storing multiple sorters using the AndSorter class
		/// </summary>
		[Test]
		public void PersistAndSorter()
		{
			IcuComparer icomp1 = new IcuComparer("fr");
			IcuComparer icomp2 = new IcuComparer("en");
			GenRecordSorter grs1 = new GenRecordSorter(icomp1);
			GenRecordSorter grs2 = new GenRecordSorter(icomp2);
			ArrayList sorters = new ArrayList();
			sorters.Add(grs1);
			sorters.Add(grs2);
			AndSorter asorter = new AndSorter(sorters);
			string xml = DynamicLoader.PersistObject(asorter, "sorter");
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			Assert.AreEqual("sorter", doc.DocumentElement.Name);
			object obj = DynamicLoader.RestoreObject(doc.DocumentElement);
			Assert.IsTrue(obj is AndSorter);
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

		// Did this preliminary experiment to see what it would be like to use .NET serialization
		// to persist filtering-related objects. Failed because objects have public properties
		// whose types are interfaces, which built-in serialization can't handle.
		//		string Persist(object obj)
		//		{
		//			MemoryStream stream = new MemoryStream();
		//			XmlSerializer serializer = new XmlSerializer(obj.GetType());
		//			serializer.Serialize(stream, obj);
		//			stream.Seek(0, SeekOrigin.Begin);
		//			UnicodeEncoding uenc = new UnicodeEncoding();
		//			return uenc.GetString(stream.GetBuffer(), 0, (int)stream.Length);
		//		}
		//
		//		object Restore(string val, Type type)
		//		{
		//			UnicodeEncoding uenc = new UnicodeEncoding();
		//			byte[] input = uenc.GetBytes(val);
		//			MemoryStream stream = new MemoryStream(input);
		//			XmlSerializer serializer = new XmlSerializer(type);
		//			return serializer.Deserialize(stream);
		//		}
		//
		//		[Test]
		//		public void PersistSimpleSorter2()
		//		{
		//			IcuComparer icomp = new IcuComparer("fr");
		//			GenRecordSorter grs = new GenRecordSorter(icomp);
		//			string persistForm = Persist(grs);
		//
		//			object obj = Restore(persistForm, typeof(GenRecordSorter));
		//			Assert.IsTrue(obj is GenRecordSorter);
		//			GenRecordSorter grsOut = obj as GenRecordSorter;
		//			IComparer compOut = grsOut.Comparer;
		//			Assert.IsTrue(compOut is IcuComparer);
		//			Assert.AreEqual("fr", (compOut as IcuComparer).WsCode);
		//		}

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
			// BaseMatcher is abstract
			// IntMatcher is abstract
			RangeIntMatcher rangeIntMatch = new RangeIntMatcher(5, 23);
			rangeIntMatch.WritingSystemFactory = m_cache.LanguageWritingSystemFactoryAccessor;
			ITsString tssLabel = m_cache.MakeAnalysisTss("label1");
			rangeIntMatch.Label = tssLabel;
			OwnIntPropFinder ownIntFinder = new OwnIntPropFinder(m_sda, 551);
			FilterBarCellFilter rangeIntFilter = new FilterBarCellFilter(ownIntFinder, rangeIntMatch);
			AndFilter andFilter = new AndFilter();
			andFilter.Add(rangeIntFilter);

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			int ws = m_cache.DefaultAnalWs;
			IVwPattern m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;

			andFilter.Add(new FilterBarCellFilter(ownIntFinder, new NotEqualIntMatcher(77)));

			OwnMlPropFinder mlPropFinder = new OwnMlPropFinder(m_cache.MainCacheAccessor, 788, 23);
			m_pattern.Pattern = tsf.MakeString("hello", ws);
			andFilter.Add(new FilterBarCellFilter(mlPropFinder, new ExactMatcher(m_pattern)));

			OwnMonoPropFinder monoPropFinder = new OwnMonoPropFinder(m_cache.MainCacheAccessor, 954);
			m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString("goodbye", ws);
			andFilter.Add(new FilterBarCellFilter(monoPropFinder, new BeginMatcher(m_pattern)));

			OneIndirectMlPropFinder oneIndMlPropFinder =
				new OneIndirectMlPropFinder(m_cache.MainCacheAccessor, 221, 222, 27);
			m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString("exit", ws);
			andFilter.Add(new FilterBarCellFilter(oneIndMlPropFinder, new EndMatcher(m_pattern)));

			MultiIndirectMlPropFinder mimlPropFinder = new MultiIndirectMlPropFinder(
				m_cache.MainCacheAccessor, new int[] {444, 555}, 666, 87);
			m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString("whatever", ws);
			andFilter.Add(new FilterBarCellFilter(mimlPropFinder, new AnywhereMatcher(m_pattern)));

			OneIndirectAtomMlPropFinder oneIndAtomFinder =
				new OneIndirectAtomMlPropFinder(m_cache.MainCacheAccessor, 543, 345, 43);
			andFilter.Add(new FilterBarCellFilter(oneIndAtomFinder, new BlankMatcher()));

			andFilter.Add(new FilterBarCellFilter(oneIndAtomFinder, new NonBlankMatcher()));

			m_pattern = VwPatternClass.Create();
			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
			m_pattern.Pattern = tsf.MakeString("pattern", ws);
			andFilter.Add(new FilterBarCellFilter(oneIndAtomFinder,
				new InvertMatcher(new RegExpMatcher(m_pattern))));

			andFilter.Add(new NullFilter());

			XmlDocument docPaf = new XmlDocument();
			docPaf.LoadXml("<root targetClasses=\"LexEntry, LexSense\"></root>");
			ProblemAnnotationFilter paf = new ProblemAnnotationFilter();
			paf.Init(m_cache, docPaf.DocumentElement);
			andFilter.Add(paf);

			// Save and restore!
			string xml = DynamicLoader.PersistObject(andFilter, "filter");

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);

			// And check all the pieces...
			AndFilter andFilterOut =
				DynamicLoader.RestoreObject(doc.DocumentElement) as AndFilter;
			andFilterOut.Cache = m_cache;

			Assert.IsNotNull(andFilterOut);

			FilterBarCellFilter rangeIntFilterOut =
				andFilterOut.Filters[0] as FilterBarCellFilter; // todo
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
			Assert.AreEqual(m_cache.MainCacheAccessor, mlPropFinderOut.DataAccess);
			Assert.AreEqual(788, mlPropFinderOut.Flid);
			Assert.AreEqual(23, mlPropFinderOut.Ws);

			OwnMonoPropFinder monoPropFinderOut = GetFinder(andFilter, 3) as OwnMonoPropFinder;
			Assert.AreEqual(m_cache.MainCacheAccessor, monoPropFinderOut.DataAccess);
			Assert.AreEqual(954, monoPropFinderOut.Flid);

			OneIndirectMlPropFinder oneIndMlPropFinderOut =
				GetFinder(andFilter, 4) as OneIndirectMlPropFinder;
			Assert.AreEqual(m_cache.MainCacheAccessor, oneIndMlPropFinderOut.DataAccess);
			Assert.AreEqual(221, oneIndMlPropFinderOut.FlidVec);
			Assert.AreEqual(222, oneIndMlPropFinderOut.FlidString);
			Assert.AreEqual(27, oneIndMlPropFinderOut.Ws);

			MultiIndirectMlPropFinder mimlPropFinderOut =
				GetFinder(andFilter, 5) as MultiIndirectMlPropFinder;
			Assert.AreEqual(m_cache.MainCacheAccessor, mimlPropFinderOut.DataAccess);
			Assert.AreEqual(444, mimlPropFinderOut.VecFlids[0]);
			Assert.AreEqual(555, mimlPropFinderOut.VecFlids[1]);
			Assert.AreEqual(666, mimlPropFinderOut.FlidString);
			Assert.AreEqual(87, mimlPropFinderOut.Ws);

			OneIndirectAtomMlPropFinder oneIndAtomFinderOut =
				GetFinder(andFilter, 6) as OneIndirectAtomMlPropFinder;
			Assert.AreEqual(m_cache.MainCacheAccessor, oneIndAtomFinderOut.DataAccess);
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
			prsOut.Cache = m_cache;
			Assert.AreEqual("longName", prsOut.PropertyName);

			// Putting an IntStringComparer here is utterly bizarre, but it tests out one more class.
			StringFinderCompare sfComp = new StringFinderCompare(new OwnMonoPropFinder(m_cache.MainCacheAccessor, 445),
				new ReverseComparer(new IntStringComparer()));
			sfComp.SortedFromEnd = true;
			// Save and restore!
			xml = DynamicLoader.PersistObject(sfComp, "comparer");
			doc = new XmlDocument();
			doc.LoadXml(xml);
			// And check all the pieces...
			StringFinderCompare sfCompOut = DynamicLoader.RestoreObject(doc.DocumentElement) as StringFinderCompare;
			sfCompOut.Cache = m_cache;

			Assert.IsTrue(sfCompOut.Finder is OwnMonoPropFinder);
			Assert.IsTrue(sfCompOut.SubComparer is ReverseComparer);
			Assert.IsTrue(sfCompOut.SortedFromEnd);

			ReverseComparer rcOut = sfCompOut.SubComparer as ReverseComparer;
			Assert.IsTrue(rcOut.SubComp is IntStringComparer);
		}
	}
}
