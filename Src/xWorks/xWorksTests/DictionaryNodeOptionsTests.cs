// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryNodeOptionsTests
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
			Assert.That(clone, Is.Not.Null, "Incorrect subclass returned; expected DictionaryNodeSenseOptions");
			Assert.That(clone, Is.Not.SameAs(orig), "Not deep cloned; shallow cloned");
			Assert.That(clone.BeforeNumber, Is.EqualTo(orig.BeforeNumber));
			Assert.That(clone.NumberingStyle, Is.EqualTo(orig.NumberingStyle));
			Assert.That(clone.AfterNumber, Is.EqualTo(orig.AfterNumber));
			Assert.That(clone.NumberStyle, Is.EqualTo(orig.NumberStyle));
			Assert.That(clone.NumberEvenASingleSense, Is.EqualTo(orig.NumberEvenASingleSense));
			Assert.That(clone.ShowSharedGrammarInfoFirst, Is.EqualTo(orig.ShowSharedGrammarInfoFirst));
			Assert.That(clone.DisplayEachSenseInAParagraph, Is.EqualTo(orig.DisplayEachSenseInAParagraph));
			Assert.That(clone.DisplayFirstSenseInline, Is.EqualTo(orig.DisplayFirstSenseInline));
		}

		[Test]
		public void CanDeepCloneListOptions()
		{
			var orig = new DictionaryNodeListOptions
			{
				ListId = DictionaryNodeListOptions.ListIds.Sense,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn1", IsEnabled = true },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn2", IsEnabled = false },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn3", IsEnabled = true },
				}
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeListOptions;
			Assert.That(clone, Is.Not.Null, "Incorrect subclass returned; expected DictionaryNodeListOptions");
			Assert.Null(clone as DictionaryNodeListAndParaOptions, "Incorrect subclass returned; did not expect DictionaryNodeListAndParaOptions");
			Assert.That(clone, Is.Not.SameAs(orig), "Not deep cloned; shallow cloned");
			Assert.That(clone.ListId, Is.EqualTo(orig.ListId));
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		[Test]
		public void CanDeepCloneComplexFormOptions()
		{
			var orig = new DictionaryNodeListAndParaOptions
			{
				ListId = DictionaryNodeListOptions.ListIds.Minor,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn1", IsEnabled = true },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn2", IsEnabled = false },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "Optn3", IsEnabled = true },
				},
				DisplayEachInAParagraph = true
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeListAndParaOptions;
			Assert.That(clone, Is.Not.Null, "Incorrect subclass returned; expected DictionaryNodeListAndParaOptions");
			Assert.That(clone, Is.Not.SameAs(orig), "Not deep cloned; shallow cloned");
			Assert.That(clone.ListId, Is.EqualTo(orig.ListId));
			Assert.That(clone.DisplayEachInAParagraph, Is.EqualTo(orig.DisplayEachInAParagraph));
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		[Test]
		public void CanDeepCloneWritingSystemOptions()
		{
			var orig = new DictionaryNodeWritingSystemOptions
			{
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "ws1", IsEnabled = true },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "ws2", IsEnabled = false },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "ws3", IsEnabled = true },
				},
				DisplayWritingSystemAbbreviations = true
			};

			// SUT
			var genericClone = orig.DeepClone();

			var clone = genericClone as DictionaryNodeWritingSystemOptions;
			Assert.That(clone, Is.Not.Null, "Incorrect subclass returned; expected DictionaryNodeWritingSystemOptions");
			Assert.That(clone, Is.Not.SameAs(orig), "Not deep cloned; shallow cloned");
			Assert.That(clone.WsType, Is.EqualTo(orig.WsType));
			Assert.That(clone.DisplayWritingSystemAbbreviations, Is.EqualTo(orig.DisplayWritingSystemAbbreviations));
			AssertListWasDeepCloned(orig.Options, clone.Options);
		}

		internal static void AssertListWasDeepCloned(List<DictionaryNodeListOptions.DictionaryNodeOption> orig,
			List<DictionaryNodeListOptions.DictionaryNodeOption> clone)
		{
			Assert.That(clone, Is.Not.SameAs(orig), "Not deep cloned; shallow cloned");
			Assert.That(clone.Count, Is.EqualTo(orig.Count));
			for (int i = 0; i < orig.Count; i++)
			{
				Assert.That(clone[i], Is.Not.SameAs(orig[i]), "Not deep cloned; shallow cloned");
				Assert.That(clone[i].Id, Is.EqualTo(orig[i].Id));
				Assert.That(clone[i].IsEnabled, Is.EqualTo(orig[i].IsEnabled));
			}
		}
	}
}
