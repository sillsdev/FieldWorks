// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.DictionaryConfiguration;
using NUnit.Framework;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	[TestFixture]
	public class DictionaryNodeOptionsTests
	{
		[Test]
		public void CanDeepCloneSenseOptions()
		{
			var orig = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "BeforeNumber",
				NumberingStyle = "%O",
				AfterNumber = "AfterNumber",
				NumberStyle = "Dictionary-SenseNumber",
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = true,
				DisplayEachSenseInAParagraph = true,
				DisplayFirstSenseInline = true
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeSenseOptions;
			Assert.NotNull(clone, "Incorrect subclass returned; expected DictionaryNodeSenseOptions");
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.BeforeNumber, clone.BeforeNumber);
			Assert.AreEqual(orig.NumberingStyle, clone.NumberingStyle);
			Assert.AreEqual(orig.AfterNumber, clone.AfterNumber);
			Assert.AreEqual(orig.NumberStyle, clone.NumberStyle);
			Assert.AreEqual(orig.NumberEvenASingleSense, clone.NumberEvenASingleSense);
			Assert.AreEqual(orig.ShowSharedGrammarInfoFirst, clone.ShowSharedGrammarInfoFirst);
			Assert.AreEqual(orig.DisplayEachSenseInAParagraph, clone.DisplayEachSenseInAParagraph);
			Assert.AreEqual(orig.DisplayFirstSenseInline, clone.DisplayFirstSenseInline);
		}

		[Test]
		public void CanDeepCloneListOptions()
		{
			var orig = new DictionaryNodeListOptions
			{
				ListId = ListIds.Sense,
				Options = new List<DictionaryNodeOption>
				{
					new DictionaryNodeOption { Id = "Optn1", IsEnabled = true },
					new DictionaryNodeOption { Id = "Optn2", IsEnabled = false },
					new DictionaryNodeOption { Id = "Optn3", IsEnabled = true },
				}
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeListOptions;
			Assert.NotNull(clone, "Incorrect subclass returned; expected DictionaryNodeListOptions");
			Assert.Null(clone as DictionaryNodeListAndParaOptions, "Incorrect subclass returned; did not expect DictionaryNodeListAndParaOptions");
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.ListId, clone.ListId);
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		[Test]
		public void CanDeepCloneComplexFormOptions()
		{
			var orig = new DictionaryNodeListAndParaOptions
			{
				ListId = ListIds.Minor,
				Options = new List<DictionaryNodeOption>
				{
					new DictionaryNodeOption { Id = "Optn1", IsEnabled = true },
					new DictionaryNodeOption { Id = "Optn2", IsEnabled = false },
					new DictionaryNodeOption { Id = "Optn3", IsEnabled = true },
				},
				DisplayEachInAParagraph = true
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeListAndParaOptions;
			Assert.NotNull(clone, "Incorrect subclass returned; expected DictionaryNodeListAndParaOptions");
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.ListId, clone.ListId);
			Assert.AreEqual(orig.DisplayEachInAParagraph, clone.DisplayEachInAParagraph);
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		[Test]
		public void CanDeepCloneWritingSystemOptions()
		{
			var orig = new DictionaryNodeWritingSystemOptions
			{
				WsType = WritingSystemType.Vernacular,
				Options = new List<DictionaryNodeOption>
				{
					new DictionaryNodeOption { Id = "ws1", IsEnabled = true },
					new DictionaryNodeOption { Id = "ws2", IsEnabled = false },
					new DictionaryNodeOption { Id = "ws3", IsEnabled = true },
				},
				DisplayWritingSystemAbbreviations = true
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeWritingSystemOptions;
			Assert.NotNull(clone, "Incorrect subclass returned; expected DictionaryNodeWritingSystemOptions");
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.WsType, clone.WsType);
			Assert.AreEqual(orig.DisplayWritingSystemAbbreviations, clone.DisplayWritingSystemAbbreviations);
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		internal static void AssertListWasDeepCloned(List<DictionaryNodeOption> orig, List<DictionaryNodeOption> clone)
		{
			Assert.AreNotSame(orig, clone, "Not deep cloned; shallow cloned");
			Assert.AreEqual(orig.Count, clone.Count);
			for (var i = 0; i < orig.Count; i++)
			{
				Assert.AreNotSame(orig[i], clone[i], "Not deep cloned; shallow cloned");
				Assert.AreEqual(orig[i].Id, clone[i].Id);
				Assert.AreEqual(orig[i].IsEnabled, clone[i].IsEnabled);
			}
		}
	}
}