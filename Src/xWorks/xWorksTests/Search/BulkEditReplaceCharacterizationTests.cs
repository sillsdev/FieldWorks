// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
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
		/// Applies every replacement case through preview and apply and requires identical result text.
		/// </summary>
		[Test]
		public void PreviewAndApply_ProduceTheSameTextAndWritingSystem()
		{
			for (var runIndex = 0; runIndex < ReplacementCases.Length; runIndex++)
			{
				var replacementCase = ReplacementCases[runIndex];
				var entry = CreateEntry(replacementCase.Input);
				var method = CreateReplaceMethod(replacementCase);
				var hvoList = new List<int> { entry.Hvo };

				method.FakeDoit(hvoList, XMLViewsDataCache.ktagAlternateValue,
					XMLViewsDataCache.ktagItemEnabled, new NullProgressState());

				var preview = m_bv.SpecialCache.get_StringProp(entry.Hvo,
					XMLViewsDataCache.ktagAlternateValue);
				AssertResult(preview, replacementCase.Expected, replacementCase.Name, runIndex, "preview");
				Assert.That(m_bv.SpecialCache.get_IntProp(entry.Hvo,
					XMLViewsDataCache.ktagItemEnabled), Is.EqualTo(1), replacementCase.Name);

				var undoTaskStarted = false;
				try
				{
					Cache.DomainDataByFlid.BeginUndoTask("characterization apply",
						"characterization apply");
					undoTaskStarted = true;
					method.Doit(entry.Hvo);
				}
				finally
				{
					if (undoTaskStarted)
						Cache.DomainDataByFlid.EndUndoTask();
				}

				var applied = entry.CitationForm.get_String(Cache.DefaultVernWs);
				AssertResult(applied, replacementCase.Expected, replacementCase.Name, runIndex, "apply");
				Assert.That(applied.Text, Is.EqualTo(preview.Text),
					$"{replacementCase.Name}, run {runIndex}: apply must equal preview");
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

			var undoTaskStarted = false;
			try
			{
				Cache.DomainDataByFlid.BeginUndoTask("characterization unmatched apply",
					"characterization unmatched apply");
				undoTaskStarted = true;
				method.Doit(entry.Hvo);
			}
			finally
			{
				if (undoTaskStarted)
					Cache.DomainDataByFlid.EndUndoTask();
			}
			AssertResult(entry.CitationForm.get_String(Cache.DefaultVernWs), "abc",
				replacementCase.Name, 0, "apply");
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

		private ReplaceWithMethod CreateReplaceMethod(ReplacementCase replacementCase)
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
			return new ReplaceWithMethod(Cache, m_bv.SpecialCache, accessor,
				document.DocumentElement, pattern, pattern.ReplaceWith);
		}

		private void AssertResult(ITsString result, string expectedText, string caseName,
			int runIndex, string phase)
		{
			var context = $"{caseName}, run {runIndex}, {phase}";
			Assert.That(result, Is.Not.Null, context);
			Assert.That(result.Text, Is.EqualTo(expectedText), context);
			for (var run = 0; run < result.RunCount; run++)
			{
				Assert.That(TsStringUtils.GetWsOfRun(result, run), Is.EqualTo(Cache.DefaultVernWs),
					$"{context}, writing-system run {run}");
			}
		}
	}
}
