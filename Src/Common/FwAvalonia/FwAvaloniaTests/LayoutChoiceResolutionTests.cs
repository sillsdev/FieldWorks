// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// §20.1.4 (F-1) — the 4-key (class,type,name,choiceGuid) layout resolution that lets a record's
	/// layoutChoiceField pick the right layout variant. Legacy DataTree distinguishes e.g. the 11
	/// RnGenericRec/detail/Normal layouts ONLY by choiceGuid; the old 3-key first-wins index collapsed them
	/// all to the document-first (Analysis) layout. These pure tests pin the index + selector behavior,
	/// including the cache-collision negative case the plan review flagged.
	/// </summary>
	[TestFixture]
	public class LayoutChoiceResolutionTests
	{
		private const string GuidA = "82290763-1633-4998-8317-0ec3f5027fbd";
		private const string GuidB = "11111111-2222-3333-4444-555555555555";

		// Two choiceGuid variants of the SAME (class,type,name) plus a choiceGuid-less fallback, in a second
		// file to also prove cross-file aggregation.
		private static List<XElement> Files() => new List<XElement>
		{
			XElement.Parse($@"<LayoutInventory>
  <layout class='RnGenericRec' type='detail' name='Normal' choiceGuid='{GuidA}'><part ref='A'/></layout>
  <layout class='RnGenericRec' type='detail' name='Normal' choiceGuid='{GuidB}'><part ref='B'/></layout>
</LayoutInventory>"),
			XElement.Parse(@"<LayoutInventory>
  <layout class='RnGenericRec' type='detail' name='Normal'><part ref='Fallback'/></layout>
  <layout class='LexEntry' type='detail' name='Normal'><part ref='Lex'/></layout>
</LayoutInventory>")
		};

		private static string Ref(XElement layout) => (string)layout.Element("part").Attribute("ref");

		[Test]
		public void IndexLayoutsByChoice_KeepsAllVariantsForOneKey()
		{
			var index = LayoutSourceLoader.IndexLayoutsByChoice(Files());
			Assert.That(index.TryGetValue(("RnGenericRec", "detail", "Normal"), out var variants), Is.True);
			Assert.That(variants, Has.Count.EqualTo(3), "all three Normal variants (2 choiceGuid + 1 fallback) are kept, not collapsed first-wins");
			Assert.That(index.TryGetValue(("LexEntry", "detail", "Normal"), out var lex), Is.True);
			Assert.That(lex, Has.Count.EqualTo(1));
		}

		[Test]
		public void SelectLayoutForChoice_ExactGuidMatchWins()
		{
			var variants = LayoutSourceLoader.IndexLayoutsByChoice(Files())[("RnGenericRec", "detail", "Normal")];
			Assert.That(Ref(LayoutSourceLoader.SelectLayoutForChoice(variants, GuidB)), Is.EqualTo("B"));
			Assert.That(Ref(LayoutSourceLoader.SelectLayoutForChoice(variants, GuidA)), Is.EqualTo("A"));
		}

		[Test]
		public void SelectLayoutForChoice_IsCaseInsensitiveOnGuid()
		{
			var variants = LayoutSourceLoader.IndexLayoutsByChoice(Files())[("RnGenericRec", "detail", "Normal")];
			Assert.That(Ref(LayoutSourceLoader.SelectLayoutForChoice(variants, GuidA.ToUpperInvariant())), Is.EqualTo("A"),
				"legacy GetTemplateForObjLayout matches the choiceGuid case-insensitively");
		}

		[Test]
		public void SelectLayoutForChoice_UnknownOrBlankGuid_FallsBackToChoicelessVariant()
		{
			var variants = LayoutSourceLoader.IndexLayoutsByChoice(Files())[("RnGenericRec", "detail", "Normal")];
			Assert.That(Ref(LayoutSourceLoader.SelectLayoutForChoice(variants, "no-such-guid")), Is.EqualTo("Fallback"));
			Assert.That(Ref(LayoutSourceLoader.SelectLayoutForChoice(variants, null)), Is.EqualTo("Fallback"));
			Assert.That(Ref(LayoutSourceLoader.SelectLayoutForChoice(variants, "")), Is.EqualTo("Fallback"));
		}

		[Test]
		public void SelectLayoutForChoice_NoChoicelessFallbackAndNoMatch_ReturnsFirst()
		{
			var onlyGuided = new List<XElement>
			{
				XElement.Parse($"<layout class='X' type='detail' name='Normal' choiceGuid='{GuidA}'><part ref='A'/></layout>"),
				XElement.Parse($"<layout class='X' type='detail' name='Normal' choiceGuid='{GuidB}'><part ref='B'/></layout>")
			};
			Assert.That(Ref(LayoutSourceLoader.SelectLayoutForChoice(onlyGuided, "no-match")), Is.EqualTo("A"),
				"with no choiceGuid-less fallback and no match, the first variant is used (never null/crash)");
		}

		[Test]
		public void SelectLayoutForChoice_EmptyOrNullVariants_ReturnsNull()
		{
			Assert.That(LayoutSourceLoader.SelectLayoutForChoice(new List<XElement>(), GuidA), Is.Null);
			Assert.That(LayoutSourceLoader.SelectLayoutForChoice(null, GuidA), Is.Null);
		}

		// Plan-review "missing block": two different choiceGuids on the SAME class must yield two DISTINCT
		// layouts (the selector is the cache-discriminator; the composer keys CompiledModels by choiceGuid).
		[Test]
		public void TwoChoiceGuids_OnSameKey_SelectDistinctLayouts_NoCollision()
		{
			var variants = LayoutSourceLoader.IndexLayoutsByChoice(Files())[("RnGenericRec", "detail", "Normal")];
			var a = LayoutSourceLoader.SelectLayoutForChoice(variants, GuidA);
			var b = LayoutSourceLoader.SelectLayoutForChoice(variants, GuidB);
			Assert.That(a, Is.Not.SameAs(b), "distinct choiceGuids must not collide on one layout");
			Assert.That(Ref(a), Is.EqualTo("A"));
			Assert.That(Ref(b), Is.EqualTo("B"));
		}
	}
}
