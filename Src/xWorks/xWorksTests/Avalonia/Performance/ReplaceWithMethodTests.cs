// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks.Performance
{
	[TestFixture]
	[Category("BulkReplacement")]
	[Apartment(ApartmentState.STA)]
	public class ReplaceWithMethodPreviewTests : BulkEditBarTestsBase
	{
		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(4)]
		public void FakeDoit_UsesOneReplacementPassAndProducesTheImmediateApplyResult(int matchingRows)
		{
			var entries = CreateEntries(4, matchingRows, "old", "keep");
			var document = BuildColumnSpec();
			var accessor = FieldReadWriter.Create(document.DocumentElement, Cache);
			var pattern = BuildPattern("old", "new");
			var method = new CountingReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith);
			var sentinels = entries.ToDictionary(entry => entry.Hvo,
				entry => TsStringUtils.MakeString("previous preview " + entry.Hvo, Cache.DefaultVernWs));

			foreach (var entry in entries)
				m_bv.SpecialCache.SetString(entry.Hvo, XMLViewsDataCache.ktagAlternateValue, sentinels[entry.Hvo]);

			method.FakeDoit(entries.Select(entry => entry.Hvo), XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

			Assert.That(method.FindInCount, Is.Zero,
				"the bulk replacement capability must not restart the public search for each match");
			Assert.That(method.ReplaceAllInCount, Is.EqualTo(entries.Count),
				"each eligible value must use exactly one bulk replacement call");

			Cache.DomainDataByFlid.BeginUndoTask("preview apply parity", "preview apply parity");
			try
			{
				try
				{
					foreach (var entry in entries)
						method.Doit(entry.Hvo);
				}
				finally
				{
					Cache.DomainDataByFlid.EndUndoTask();
				}

				foreach (var entry in entries)
				{
					var matched = entries.IndexOf(entry) < matchingRows;
					var enabled = m_bv.SpecialCache.get_IntProp(entry.Hvo, XMLViewsDataCache.ktagItemEnabled);
					var preview = m_bv.SpecialCache.get_StringProp(entry.Hvo, XMLViewsDataCache.ktagAlternateValue);
					Assert.That(enabled, Is.EqualTo(matched ? 1 : 0));
					Assert.That(accessor.CurrentValue(entry.Hvo).Text, Is.EqualTo(matched ? "new" : "keep"));
					if (!matched)
						Assert.That(preview, Is.SameAs(sentinels[entry.Hvo]));
					else
						Assert.That(preview.Text, Is.EqualTo(accessor.CurrentValue(entry.Hvo).Text));
				}
				Assert.That(method.ReplaceAllInCount, Is.EqualTo(entries.Count * 2));
				Assert.That(method.FindInCount, Is.Zero);
			}
			finally
			{
				if (matchingRows > 0)
					Cache.ActionHandlerAccessor.Undo();
			}
		}

		[TestCase("old-old", "old", "new", false, false, false, false, "new-new")]
		[TestCase("ab-12 cd-34", "([a-z]+)-([0-9]+)", "$2:$1", true, false, false, false, "12:ab 34:cd")]
		[TestCase("cafeteria cafe", "cafe", "tea", false, true, true, true, "cafeteria tea")]
		[TestCase("CAF\u00C9 cafe", "cafe", "tea", false, false, false, false, "tea tea")]
		[TestCase("cafe\u0301", "caf\u00e9", "tea", false, false, true, true, "tea")]
		[TestCase("old", "^", "new", true, false, true, true, "newold")]
		public void FakeDoit_MatchesImmediateApplyAcrossPatternModes(string source, string find,
			string replace, bool regularExpression, bool wholeWord, bool matchCase, bool matchDiacritics,
			string expected)
		{
			var entry = CreateEntries(1, 1, source, source).Single();
			var document = BuildColumnSpec();
			var accessor = FieldReadWriter.Create(document.DocumentElement, Cache);
			var pattern = BuildPattern(find, replace, regularExpression, wholeWord, matchCase, matchDiacritics);
			var method = new CountingReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith);

			method.FakeDoit(new[] { entry.Hvo }, XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

			Assert.That(method.ReplaceAllInCount, Is.EqualTo(1));
			Assert.That(method.FindInCount, Is.Zero);
			Assert.That(m_bv.SpecialCache.get_IntProp(entry.Hvo, XMLViewsDataCache.ktagItemEnabled), Is.EqualTo(1));
			Assert.That(m_bv.SpecialCache.get_StringProp(entry.Hvo, XMLViewsDataCache.ktagAlternateValue).Text,
				Is.EqualTo(expected));

			Cache.DomainDataByFlid.BeginUndoTask("preview apply parity", "preview apply parity");
			try
			{
				try
				{
					method.Doit(entry.Hvo);
				}
				finally
				{
					Cache.DomainDataByFlid.EndUndoTask();
				}

				Assert.That(accessor.CurrentValue(entry.Hvo).Text, Is.EqualTo(expected));
				Assert.That(method.ReplaceAllInCount, Is.EqualTo(2));
				Assert.That(method.FindInCount, Is.Zero);
			}
			finally
			{
				Cache.ActionHandlerAccessor.Undo();
			}
		}

		[Test]
		public void FakeDoit_MatchesImmediateApplyForMultiStringRichRuns()
		{
			ILexEntry entry = null;
			ILexSense sense = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(entry, null, string.Empty);
				m_createdObjectList.Add(entry);
			});
			var document = BuildColumnSpec();
			var accessor = new OwnMlPropReadWriter(Cache, LexSenseTags.kflidDefinition, Cache.DefaultAnalWs);
			var sourceBuilder = TsStringUtils.MakeString("old keep", Cache.DefaultAnalWs).GetBldr();
			sourceBuilder.SetIntPropValues(4, 8, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
			sourceBuilder.SetStrPropValue(4, 8, (int)FwTextPropType.ktptNamedStyle, "Emphasis");
			sourceBuilder.SetStrPropValue(4, 8, (int)FwTextPropType.ktptObjData,
				(char)FwObjDataTypes.kodtExternalPathName + "https://example.test/keep");
			var source = sourceBuilder.GetString();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => accessor.SetNewValue(sense.Hvo, source));
			Assert.That(accessor.CurrentValue(sense.Hvo).RunCount, Is.EqualTo(2));

			var pattern = BuildPattern("old", "new");
			var method = new CountingReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith);
			method.FakeDoit(new[] { sense.Hvo }, XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());
			var preview = m_bv.SpecialCache.get_StringProp(sense.Hvo, XMLViewsDataCache.ktagAlternateValue);

			Assert.That(method.ReplaceAllInCount, Is.EqualTo(1));
			Assert.That(method.FindInCount, Is.Zero);
			Assert.That(preview.Text, Is.EqualTo("new keep"));
			var keepRun = Enumerable.Range(0, preview.RunCount).Single(i => preview.get_RunText(i) == "keep");
			Assert.That(TsStringUtils.GetWsOfRun(preview, keepRun), Is.EqualTo(Cache.DefaultVernWs));
			Assert.That(preview.get_Properties(keepRun).GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				Is.EqualTo("Emphasis"));
			Assert.That(preview.get_Properties(keepRun).GetStrPropValue((int)FwTextPropType.ktptObjData),
				Is.EqualTo((char)FwObjDataTypes.kodtExternalPathName + "https://example.test/keep"));

			Cache.DomainDataByFlid.BeginUndoTask("preview apply parity", "preview apply parity");
			try
			{
				try
				{
					method.Doit(sense.Hvo);
				}
				finally
				{
					Cache.DomainDataByFlid.EndUndoTask();
				}

				Assert.That(preview.Equals(accessor.CurrentValue(sense.Hvo)), Is.True);
				Assert.That(method.ReplaceAllInCount, Is.EqualTo(2));
				Assert.That(method.FindInCount, Is.Zero);
			}
			finally
			{
				Cache.ActionHandlerAccessor.Undo();
			}
		}

		[TestCase("old-old", true, "new-new", 3)]
		[TestCase("keep", false, null, 1)]
		public void FakeDoit_FallsBackWhenBulkReplacementIsUnavailable(string source,
			bool matched, string expected, int expectedFindInCount)
		{
			var entry = CreateEntries(1, 1, source, source).Single();
			var document = BuildColumnSpec();
			var accessor = FieldReadWriter.Create(document.DocumentElement, Cache);
			var pattern = BuildPattern("old", "new");
			var method = new CountingReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith, false);
			var sentinel = TsStringUtils.MakeString("previous preview", Cache.DefaultVernWs);
			m_bv.SpecialCache.SetString(entry.Hvo, XMLViewsDataCache.ktagAlternateValue, sentinel);

			method.FakeDoit(new[] { entry.Hvo }, XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

			Assert.That(method.FindInCount, Is.EqualTo(expectedFindInCount));
			Assert.That(m_bv.SpecialCache.get_IntProp(entry.Hvo, XMLViewsDataCache.ktagItemEnabled),
				Is.EqualTo(matched ? 1 : 0));
			var preview = m_bv.SpecialCache.get_StringProp(entry.Hvo,
				XMLViewsDataCache.ktagAlternateValue);
			if (matched)
				Assert.That(preview.Text, Is.EqualTo(expected));
			else
				Assert.That(preview, Is.SameAs(sentinel));
		}

		[Test]
		public void FakeDoit_PropagatesBulkReplacementFailureWithoutFallback()
		{
			var entry = CreateEntries(1, 1, "old", "old").Single();
			var document = BuildColumnSpec();
			var accessor = FieldReadWriter.Create(document.DocumentElement, Cache);
			var pattern = BuildPattern("old", "new");
			var failure = new COMException("bulk replacement failed", unchecked((int)0x80004005));
			var method = new CountingReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith, true, failure);

			var actual = Assert.Throws<COMException>(() => method.FakeDoit(new[] { entry.Hvo },
				XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled,
				new NullProgressState()));

			Assert.That(actual, Is.SameAs(failure));
			Assert.That(method.ReplaceAllInCount, Is.EqualTo(1));
			Assert.That(method.FindInCount, Is.Zero);
		}

		[Test]
		public void FakeDoit_NormalizesBulkReplacementResultToNfd()
		{
			var entry = CreateEntries(1, 1, "old caf\u00e9", "old caf\u00e9").Single();
			var document = BuildColumnSpec();
			var accessor = FieldReadWriter.Create(document.DocumentElement, Cache);
			var pattern = BuildPattern("old", "new");
			var method = new CountingReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith);

			method.FakeDoit(new[] { entry.Hvo }, XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

			var preview = m_bv.SpecialCache.get_StringProp(entry.Hvo,
				XMLViewsDataCache.ktagAlternateValue);
			Assert.That(method.ReplaceAllInCount, Is.EqualTo(1));
			Assert.That(method.FindInCount, Is.Zero);
			Assert.That(preview.Text, Is.EqualTo("new cafe\u0301"));
			Assert.That(preview.Text.IsNormalized(NormalizationForm.FormD), Is.True);
		}

		[Test]
		public void FakeDoit_BaseGateRejectionDoesNotSearchOrChangePreview()
		{
			var entry = CreateEntries(1, 1, "old", "old").Single();
			var document = BuildColumnSpec();
			var accessor = FieldReadWriter.Create(document.DocumentElement, Cache);
			var pattern = BuildPattern("old", "new");
			var method = new CountingReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith, canChange: false);
			var sentinel = TsStringUtils.MakeString("previous preview", Cache.DefaultVernWs);
			m_bv.SpecialCache.SetString(entry.Hvo, XMLViewsDataCache.ktagAlternateValue, sentinel);

			method.FakeDoit(new[] { entry.Hvo }, XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

			Assert.That(method.ReplaceAllInCount, Is.Zero);
			Assert.That(method.FindInCount, Is.Zero);
			Assert.That(m_bv.SpecialCache.get_IntProp(entry.Hvo,
				XMLViewsDataCache.ktagItemEnabled), Is.Zero);
			Assert.That(m_bv.SpecialCache.get_StringProp(entry.Hvo,
				XMLViewsDataCache.ktagAlternateValue), Is.SameAs(sentinel));
			Assert.That(accessor.CurrentValue(entry.Hvo).Text, Is.EqualTo("old"));
		}

		private List<ILexEntry> CreateEntries(int count, int matchingRows, string matchedValue,
			string unmatchedValue)
		{
			var entries = new List<ILexEntry>(count);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				for (var index = 0; index < count; index++)
				{
					var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
					entry.CitationForm.set_String(Cache.DefaultVernWs,
						index < matchingRows ? matchedValue : unmatchedValue);
					entries.Add(entry);
					m_createdObjectList.Add(entry);
				}
			});
			return entries;
		}

		private static XmlDocument BuildColumnSpec()
		{
			var document = new XmlDocument();
			document.LoadXml("<column transduce=\"LexEntry.CitationForm\" ws=\"$ws=vernacular\" />");
			return document;
		}

		private IVwPattern BuildPattern(string find, string replace, bool regularExpression = false,
			bool wholeWord = false, bool matchCase = true, bool matchDiacritics = true)
		{
			var pattern = VwPatternClass.Create();
			pattern.Pattern = TsStringUtils.MakeString(find, Cache.DefaultVernWs);
			pattern.ReplaceWith = TsStringUtils.MakeString(replace, Cache.DefaultVernWs);
			pattern.UseRegularExpressions = regularExpression;
			pattern.MatchWholeWord = wholeWord;
			pattern.MatchCase = matchCase;
			pattern.MatchDiacritics = matchDiacritics;
			return pattern;
		}

		internal sealed class CountingReplaceWithMethod : ReplaceWithMethod
		{
			private readonly bool m_exposeBulkReplacement;
			private readonly Exception m_bulkReplacementFailure;
			private readonly bool? m_canChange;

			internal CountingReplaceWithMethod(LcmCache cache, ISilDataAccessManaged sda,
				FieldReadWriter accessor, XmlNode spec, IVwPattern pattern, ITsString replacement,
				bool exposeBulkReplacement = true, Exception bulkReplacementFailure = null,
				bool? canChange = null)
				: base(cache, sda, accessor, spec, pattern, replacement)
			{
				m_exposeBulkReplacement = exposeBulkReplacement;
				m_bulkReplacementFailure = bulkReplacementFailure;
				m_canChange = canChange;
			}

			internal int FindInCount { get; private set; }
			internal int ReplaceAllInCount { get; private set; }

			protected override bool OkToChange(int hvo)
			{
				return m_canChange ?? base.OkToChange(hvo);
			}

			protected override bool TryReplaceAllIn(ITsString source, out ITsString result,
				out int matchCount)
			{
				ReplaceAllInCount++;
				if (m_bulkReplacementFailure != null)
					throw m_bulkReplacementFailure;
				if (!m_exposeBulkReplacement)
				{
					result = null;
					matchCount = 0;
					return false;
				}
				return base.TryReplaceAllIn(source, out result, out matchCount);
			}

			protected override void FindIn(int ichStart, int ichEnd, out int ichMin, out int ichLim)
			{
				FindInCount++;
				base.FindIn(ichStart, ichEnd, out ichMin, out ichLim);
			}
		}
	}

	[TestFixture]
	[Category("BulkReplacement")]
	[Apartment(ApartmentState.STA)]
	public class ReplaceAllInDecoratorCorrectnessTests : BulkEditBarTestsBase
	{
		[Test]
		public void FakeDoit_MatchesOracleForDecoratorBackedUnnormalizedRichValue()
		{
			ILexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				m_createdObjectList.Add(entry);
			});
			var sourceBuilder = TsStringUtils.MakeString("old cafe\u0301 old keep",
				Cache.DefaultVernWs).GetBldr();
			sourceBuilder.SetIntPropValues(4, 9, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
			sourceBuilder.SetStrPropValue(4, 9, (int)FwTextPropType.ktptNamedStyle,
				"Unnormalized Source");
			sourceBuilder.SetStrPropValue(14, 18, (int)FwTextPropType.ktptObjData,
				(char)FwObjDataTypes.kodtExternalPathName + "https://example.test/keep");
			m_bv.SpecialCache.SetString(entry.Hvo, XMLViewsDataCache.ktagAlternateValue,
				sourceBuilder.GetString());
			var decoratedValue = m_bv.SpecialCache.get_StringProp(entry.Hvo,
				XMLViewsDataCache.ktagAlternateValue);
			Assert.That(decoratedValue.Text.IsNormalized(NormalizationForm.FormC), Is.False);
			Assert.That(decoratedValue.RunCount, Is.GreaterThan(1));

			var pattern = VwPatternClass.Create();
			pattern.Pattern = TsStringUtils.MakeString("old", Cache.DefaultVernWs);
			pattern.ReplaceWith = TsStringUtils.MakeString("new", Cache.DefaultVernWs);
			pattern.MatchCase = true;
			pattern.MatchDiacritics = true;
			var replacementBuilder = pattern.ReplaceWith.GetBldr();
			replacementBuilder.SetStrPropValue(0, 3, (int)FwTextPropType.ktptNamedStyle,
				"Decorator Replacement");
			pattern.ReplaceWith = replacementBuilder.GetString();
			var oracleInit = VwStringTextSourceClass.Create();
			oracleInit.SetString(decoratedValue);
			var oracleSource = (IVwTextSource)oracleInit;
			var expectedBuilder = decoratedValue.GetBldr();
			var expectedCount = 0;
			var ichStart = 0;
			var delta = 0;
			while (ichStart <= decoratedValue.Length)
			{
				pattern.FindIn(oracleSource, ichStart, decoratedValue.Length, true,
					out var ichMin, out var ichLim, null);
				if (ichMin < 0)
					break;
				var replacement = pattern.ReplacementText;
				expectedBuilder.ReplaceTsString(ichMin + delta, ichLim + delta, replacement);
				delta += replacement.Length - (ichLim - ichMin);
				expectedCount++;
				ichStart = ichLim;
			}

			Assert.That(expectedCount, Is.EqualTo(2));
			var document = new XmlDocument();
			document.LoadXml("<column transduce=\"LexEntry.CitationForm\" ws=\"$ws=vernacular\" />");
			var accessor = new DecoratorStringReadWriter(m_bv.SpecialCache,
				XMLViewsDataCache.ktagAlternateValue, Cache.DefaultVernWs);
			var method = new ReplaceWithMethodPreviewTests.CountingReplaceWithMethod(Cache,
				m_bv.SpecialCache, accessor, document.DocumentElement, pattern, pattern.ReplaceWith);

			method.FakeDoit(new[] { entry.Hvo }, XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

			var actual = m_bv.SpecialCache.get_StringProp(entry.Hvo,
				XMLViewsDataCache.ktagAlternateValue);
			var expected = expectedBuilder.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD);
			Assert.That(method.ReplaceAllInCount, Is.EqualTo(1));
			Assert.That(method.FindInCount, Is.Zero);
			Assert.That(m_bv.SpecialCache.get_IntProp(entry.Hvo,
				XMLViewsDataCache.ktagItemEnabled), Is.EqualTo(1));
			Assert.That(actual.Text, Is.EqualTo("new cafe\u0301 new keep"));
			Assert.That(actual.Text.IsNormalized(NormalizationForm.FormD), Is.True);
			Assert.That(TsStringHelper.TsStringsAreEqual(expected, actual,
				out var differences), Is.True, differences);
		}

		private sealed class DecoratorStringReadWriter : FieldReadWriter
		{
			private readonly int m_tag;
			private readonly int m_writingSystem;

			internal DecoratorStringReadWriter(ISilDataAccess dataAccess, int tag, int writingSystem)
				: base(dataAccess)
			{
				m_tag = tag;
				m_writingSystem = writingSystem;
			}

			public override ITsString CurrentValue(int hvo)
			{
				return m_sda.get_StringProp(hvo, m_tag);
			}

			public override void SetNewValue(int hvo, ITsString tss)
			{
				m_sda.SetString(hvo, m_tag, tss);
			}

			public override int WritingSystem
			{
				get { return m_writingSystem; }
			}
		}
	}
}
