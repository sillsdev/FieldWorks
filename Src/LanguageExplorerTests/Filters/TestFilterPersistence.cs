// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using LanguageExplorer;
using LanguageExplorer.Filters;
using LanguageExplorer.Impls;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.Filters
{
	/// <summary>
	/// Test persisting Filters objects using IPersistAsXml
	/// </summary>
	[TestFixture]
	public class TestFilterPersistence : MemoryOnlyBackendProviderTestBase
	{
		private ISilDataAccess _sda;
		private IPersistAsXmlFactory _persistAsXmlFactory;

		public override void FixtureSetup()
		{
			base.FixtureSetup();

			_sda = Cache.DomainDataByFlid;
			_persistAsXmlFactory = new PersistAsXmlFactory();
		}

		// Checklist of classes we need to test

		// Sorters:
		// IRecordSorter (covered by many concrete classes, including GenRecordSorter in PersistSimpleSorter)
		// GenRecordSorter (PersistSimpleSorter)
		// PropertyRecordSorter (SortersEtc)

		// Comparers:
		// IcuComparer (PersistSimpleSorter)
		// LcmCompare (created only temporarily in PropertyRecordSorter, does not need persistence)
		// StringFinderCompare (SortersEtc)
		// ReverseComparer (SortersEtc)
		// IntStringComparer (SortersEtc)

		// Filters:
		// IRecordFilter (abstract)
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
		/// A simple test to get us started, covers IcuComparer, GenRecordSorter, hence IRecordSorter.
		/// </summary>
		[Test]
		public void PersistSimpleSorter()
		{
			var icomp = new IcuComparer("fr");
			var grs = new GenRecordSorter(icomp);
			var xml = LanguageExplorerServices.PersistObject(grs, "sorter");
			var doc = XDocument.Parse(xml);
			Assert.IsTrue("sorter" == doc.Root.Name);
			var obj = DynamicLoader.RestoreObject<GenRecordSorter>(_persistAsXmlFactory, doc.Root);
			try
			{
				var compOut = obj.Comparer;
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
			var sorters = new List<IRecordSorter> { grs1, grs2 };
			var asorter = new AndSorter(sorters);
			var xml = LanguageExplorerServices.PersistObject(asorter, "sorter");
			var doc = XDocument.Parse(xml);
			Assert.IsTrue("sorter" == doc.Root.Name);
			var andSorter = DynamicLoader.RestoreObject<AndSorter>(_persistAsXmlFactory, doc.Root);
			var sortersOut = andSorter.Sorters;
			var grsOut1 = (GenRecordSorter)sortersOut[0];
			var grsOut2 = (GenRecordSorter)sortersOut[1];
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
		static IStringFinder GetFinder(AndFilter andFilter, int index)
		{
			return ((FilterBarCellFilter)andFilter.Filters[index]).Finder;
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
			var ownIntFinder = new OwnIntPropFinder(_sda, 551);

			var rangeIntFilter = new FilterBarCellFilter(ownIntFinder, rangeIntMatch);
			var andFilter = new AndFilter();

			andFilter.Add(rangeIntFilter);

			var ws = defAnalWs.Handle;
			IVwPattern pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;

			var otherFilter = new FilterBarCellFilter(ownIntFinder, new NotEqualIntMatcher(77));

			andFilter.Add(otherFilter);

			var mlPropFinder = new OwnMlPropFinder(_sda, 788, 23);
			pattern.Pattern = TsStringUtils.MakeString("hello", ws);
			var filter = new FilterBarCellFilter(mlPropFinder, new ExactMatcher(pattern));
			andFilter.Add(filter);

			var monoPropFinder = new OwnMonoPropFinder(_sda, 954);
			pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;
			pattern.Pattern = TsStringUtils.MakeString("goodbye", ws);
			filter = new FilterBarCellFilter(monoPropFinder, new BeginMatcher(pattern));
			andFilter.Add(filter);

			var oneIndMlPropFinder = new OneIndirectMlPropFinder(_sda, 221, 222, 27);
			pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;
			pattern.Pattern = TsStringUtils.MakeString("exit", ws);
			filter = new FilterBarCellFilter(oneIndMlPropFinder, new EndMatcher(pattern));
			andFilter.Add(filter);

			var mimlPropFinder = new MultiIndirectMlPropFinder(_sda, new[] { 444, 555 }, 666, 87);
			pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;
			pattern.Pattern = TsStringUtils.MakeString("whatever", ws);
			filter = new FilterBarCellFilter(mimlPropFinder, new AnywhereMatcher(pattern));
			andFilter.Add(filter);

			var oneIndAtomFinder = new OneIndirectAtomMlPropFinder(_sda, 543, 345, 43);
			filter = new FilterBarCellFilter(oneIndAtomFinder, new BlankMatcher());
			andFilter.Add(filter);

			filter = new FilterBarCellFilter(oneIndAtomFinder, new NonBlankMatcher());
			andFilter.Add(filter);

			pattern = VwPatternClass.Create();
			pattern.MatchOldWritingSystem = false;
			pattern.MatchDiacritics = false;
			pattern.MatchWholeWord = false;
			pattern.MatchCase = false;
			pattern.UseRegularExpressions = false;
			pattern.Pattern = TsStringUtils.MakeString("pattern", ws);
			filter = new FilterBarCellFilter(oneIndAtomFinder, new InvertMatcher(new RegExpMatcher(pattern)));
			andFilter.Add(filter);

			andFilter.Add(new NullFilter());

			var docPaf = XDocument.Parse("<root targetClasses=\"LexEntry, LexSense\"></root>");
			var paf = new ProblemAnnotationFilter();
			paf.Init(Cache, docPaf.Root);
			andFilter.Add(paf);

			// Save and restore!
			var xml = LanguageExplorerServices.PersistObject(andFilter, "filter");
			var doc = XDocument.Parse(xml);

			// And check all the pieces...
			var andFilterOut = DynamicLoader.RestoreObject<AndFilter>(_persistAsXmlFactory, doc.Root);
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

			var mlPropFinderOut = (OwnMlPropFinder)GetFinder(andFilter, 2);
			Assert.AreEqual(_sda, mlPropFinderOut.DataAccess);
			Assert.AreEqual(788, mlPropFinderOut.Flid);
			Assert.AreEqual(23, mlPropFinderOut.Ws);

			var monoPropFinderOut = (OwnMonoPropFinder)GetFinder(andFilter, 3);
			Assert.AreEqual(_sda, monoPropFinderOut.DataAccess);
			Assert.AreEqual(954, monoPropFinderOut.Flid);

			var oneIndMlPropFinderOut = (OneIndirectMlPropFinder)GetFinder(andFilter, 4);
			Assert.AreEqual(_sda, oneIndMlPropFinderOut.DataAccess);
			Assert.AreEqual(221, oneIndMlPropFinderOut.FlidVec);
			Assert.AreEqual(222, oneIndMlPropFinderOut.FlidString);
			Assert.AreEqual(27, oneIndMlPropFinderOut.Ws);

			var mimlPropFinderOut = (MultiIndirectMlPropFinder)GetFinder(andFilter, 5);
			Assert.AreEqual(_sda, mimlPropFinderOut.DataAccess);
			Assert.AreEqual(444, mimlPropFinderOut.VecFlids[0]);
			Assert.AreEqual(555, mimlPropFinderOut.VecFlids[1]);
			Assert.AreEqual(666, mimlPropFinderOut.FlidString);
			Assert.AreEqual(87, mimlPropFinderOut.Ws);

			var oneIndAtomFinderOut = (OneIndirectAtomMlPropFinder)GetFinder(andFilter, 6);
			Assert.AreEqual(_sda, oneIndAtomFinderOut.DataAccess);
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
			var xml = LanguageExplorerServices.PersistObject(prs, "sorter");
			var doc = XDocument.Parse(xml);

			// And check all the pieces...
			var prsOut = DynamicLoader.RestoreObject<PropertyRecordSorter>(_persistAsXmlFactory, doc.Root);
			prsOut.Cache = Cache;
			Assert.AreEqual("longName", prsOut.PropertyName);
		}

		[Test]
		public void PersistReverseComparer()
		{
			// Putting an IntStringComparer here is utterly bizarre, but it tests out one more class.
			var sfComp = new StringFinderCompare(new OwnMonoPropFinder(_sda, 445), new ReverseComparer(new IntStringComparer()));
			sfComp.SortedFromEnd = true;
			// Save and restore!
			var xml = LanguageExplorerServices.PersistObject(sfComp, "comparer");
			var doc = XDocument.Parse(xml);
			// And check all the pieces...
			var sfCompOut = _persistAsXmlFactory.Create<StringFinderCompare>(doc.Root);
			sfCompOut.Cache = Cache;

			Assert.IsTrue(sfCompOut.Finder is OwnMonoPropFinder);
			Assert.IsTrue(sfCompOut.SubComparer is ReverseComparer);
			Assert.IsTrue(sfCompOut.SortedFromEnd);

			var rcOut = (ReverseComparer)sfCompOut.SubComparer;
			Assert.IsTrue(rcOut.SubComp is IntStringComparer);
		}
	}
}