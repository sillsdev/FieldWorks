// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks.Search
{
	/// <summary>
	/// Records the current preview and apply behavior of bulk replacement over citation forms.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class BulkEditReplaceCharacterizationTests : BulkEditBarTestsBase
	{
		private sealed class ReplacementCase
		{
			public ReplacementCase(string name, string input, string pattern, string replacement,
				string expected, bool useRegularExpressions = false)
			{
				Name = name;
				Input = input;
				Pattern = pattern;
				Replacement = replacement;
				Expected = expected;
				UseRegularExpressions = useRegularExpressions;
			}

			public string Name { get; }
			public string Input { get; }
			public string Pattern { get; }
			public string Replacement { get; }
			public string Expected { get; }
			public bool UseRegularExpressions { get; }

			public override string ToString()
			{
				return Name;
			}
		}

		private static readonly ReplacementCase[] ReplacementCases =
		{
			new ReplacementCase("literal all occurrences", "old old old", "old", "new",
				"new new new"),
			new ReplacementCase("canonical pattern over decomposed text", "cafe\u0301 cafe\u0301",
				"caf\u00e9", "\u00e9", "e\u0301 e\u0301"),
			new ReplacementCase("replacement longer than match", "a a", "a", "long",
				"long long"),
			new ReplacementCase("replacement shorter than match", "long long", "long", "x",
				"x x"),
			new ReplacementCase("regular expression capture replacement", "ab ab", "(a)(b)",
				"$2$1", "ba ba", true),
			new ReplacementCase("start zero width insertion", "abc", "^", "X", "Xabc", true),
			new ReplacementCase("end zero width insertion", "abc", "$", "X", "abcX", true)
		};

		/// <summary>
		/// Applies every replacement case through preview and apply and requires identical result
		/// text.
		/// </summary>
		[Test]
		public void PreviewAndApply_ProduceTheSameTextAndWritingSystem()
		{
			foreach (var replacementCase in ReplacementCases)
			{
				var entry = CreateEntry(replacementCase.Input);
				var method = CreateReplaceMethod(replacementCase);
				var hvoList = new List<int> { entry.Hvo };

				method.FakeDoit(hvoList, XMLViewsDataCache.ktagAlternateValue,
					XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

				var preview = m_bv.SpecialCache.get_StringProp(entry.Hvo,
					XMLViewsDataCache.ktagAlternateValue);
				AssertResult(preview, replacementCase.Expected, replacementCase.Name + " preview");
				Assert.That(m_bv.SpecialCache.get_IntProp(entry.Hvo,
					XMLViewsDataCache.ktagItemEnabled), Is.EqualTo(1), replacementCase.Name);

				Cache.DomainDataByFlid.BeginUndoTask("characterization apply",
					"characterization apply");
				method.Doit(entry.Hvo);
				Cache.DomainDataByFlid.EndUndoTask();

				var applied = entry.CitationForm.get_String(Cache.DefaultVernWs);
				AssertResult(applied, replacementCase.Expected, replacementCase.Name + " apply");
				Assert.That(applied.Text, Is.EqualTo(preview.Text), replacementCase.Name);
				Cache.ActionHandlerAccessor.Undo();
			}
		}

		/// <summary>
		/// An unmatched citation form is disabled and does not receive a preview value.
		/// </summary>
		[Test]
		public void UnmatchedRow_IsDisabledAndHasNoPreviewReplacement()
		{
			var entry = CreateEntry("abc");
			var replacementCase = new ReplacementCase("unmatched", "abc", "z", "new", null);
			var method = CreateReplaceMethod(replacementCase);

			method.FakeDoit(new[] { entry.Hvo }, XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

			Assert.That(m_bv.SpecialCache.get_IntProp(entry.Hvo,
				XMLViewsDataCache.ktagItemEnabled), Is.EqualTo(0));
			Assert.That(m_bv.SpecialCache.get_IsPropInCache(entry.Hvo,
				XMLViewsDataCache.ktagAlternateValue, 0, Cache.DefaultVernWs), Is.False);

			Cache.DomainDataByFlid.BeginUndoTask("characterization unmatched apply",
				"characterization unmatched apply");
			method.Doit(entry.Hvo);
			Cache.DomainDataByFlid.EndUndoTask();
			Assert.That(entry.CitationForm.get_String(Cache.DefaultVernWs).Text, Is.EqualTo("abc"));
		}

		/// <summary>
		/// Every case must come out the same through the bulk ReplaceAllIn path and through the
		/// repeated-FindIn fallback, so the two engines cannot drift apart unnoticed.
		/// </summary>
		[Test]
		public void BulkAndFallbackEngines_ProduceTheSameResult()
		{
			foreach (var replacementCase in ReplacementCases)
			{
				var bulk = RunCase(replacementCase, false);
				var fallback = RunCase(replacementCase, true);
				AssertSameString(bulk.Item1, fallback.Item1, replacementCase.Name + " preview");
				AssertSameString(bulk.Item2, fallback.Item2, replacementCase.Name + " apply");
			}
		}

		private Tuple<ITsString, ITsString> RunCase(ReplacementCase replacementCase, bool fallback)
		{
			var entry = CreateEntry(replacementCase.Input);
			var method = CreateReplaceMethod(replacementCase, fallback);

			method.FakeDoit(new[] { entry.Hvo }, XMLViewsDataCache.ktagAlternateValue,
				XMLViewsDataCache.ktagItemEnabled, new NullProgressState());
			var preview = m_bv.SpecialCache.get_StringProp(entry.Hvo,
				XMLViewsDataCache.ktagAlternateValue);

			Cache.DomainDataByFlid.BeginUndoTask("characterization engine apply",
				"characterization engine apply");
			method.Doit(entry.Hvo);
			Cache.DomainDataByFlid.EndUndoTask();
			var applied = entry.CitationForm.get_String(Cache.DefaultVernWs);
			Cache.ActionHandlerAccessor.Undo();

			return new Tuple<ITsString, ITsString>(preview, applied);
		}

		private static void AssertSameString(ITsString expected, ITsString actual, string message)
		{
			Assert.That(actual.Text, Is.EqualTo(expected.Text), message);
			Assert.That(actual.RunCount, Is.EqualTo(expected.RunCount), message);
			for (var run = 0; run < expected.RunCount; run++)
			{
				Assert.That(actual.get_RunText(run), Is.EqualTo(expected.get_RunText(run)),
					message);
				Assert.That(TsStringUtils.GetWsOfRun(actual, run),
					Is.EqualTo(TsStringUtils.GetWsOfRun(expected, run)), message);
			}
		}

		private ILexEntry CreateEntry(string text)
		{
			ILexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				entry.CitationForm.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString(text, Cache.DefaultVernWs));
				m_createdObjectList.Add(entry);
			});
			return entry;
		}

		private ReplaceWithMethod CreateReplaceMethod(ReplacementCase replacementCase,
			bool fallback = false)
		{
			var document = new XmlDocument();
			document.LoadXml("<column transduce=\"LexEntry.CitationForm\" ws=\"$ws=vernacular\" />");
			var accessor = FieldReadWriter.Create(document.DocumentElement, Cache);
			var pattern = VwPatternClass.Create();
			pattern.Pattern = TsStringUtils.MakeString(replacementCase.Pattern, Cache.DefaultVernWs);
			pattern.MatchCase = true;
			pattern.MatchDiacritics = true;
			pattern.MatchWholeWord = false;
			pattern.UseRegularExpressions = replacementCase.UseRegularExpressions;
			pattern.ReplaceWith = TsStringUtils.MakeString(replacementCase.Replacement,
				Cache.DefaultVernWs);
			if (fallback)
			{
				return new FallbackReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
					document.DocumentElement, pattern, pattern.ReplaceWith);
			}
			return new ReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith);
		}

		/// <summary>
		/// Declines the bulk replacement capability so the caller runs on the repeated-FindIn
		/// path instead.
		/// </summary>
		private sealed class FallbackReplaceWithMethod : ReplaceWithMethod
		{
			internal FallbackReplaceWithMethod(LcmCache cache, ISilDataAccessManaged sda,
				FieldReadWriter accessor, XmlNode spec, IVwPattern pattern, ITsString replacement)
				: base(cache, sda, accessor, spec, pattern, replacement)
			{
			}

			protected override bool TryReplaceAllIn(ITsString source, out ITsString result,
				out int matchCount)
			{
				result = null;
				matchCount = 0;
				return false;
			}
		}

		private void AssertResult(ITsString result, string expectedText, string message)
		{
			Assert.That(result, Is.Not.Null, message);
			Assert.That(result.Text, Is.EqualTo(expectedText), message);
			Assert.That(TsStringUtils.GetWsOfRun(result, 0), Is.EqualTo(Cache.DefaultVernWs),
				message);
		}
	}
}
