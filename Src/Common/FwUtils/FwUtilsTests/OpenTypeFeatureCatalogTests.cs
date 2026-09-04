// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class OpenTypeFeatureCatalogTests
	{
		// The exact set the pre-LT-22638 FontFeaturesButton hid via its s_nonUserConfigurableTags
		// blocklist. Every one must stay hidden so the catalog does not un-hide shaping features.
		private static readonly string[] LegacyBlocklist =
		{
			"abvf", "abvm", "abvs", "akhn", "blwf", "blwm", "blws", "ccmp",
			"cjct", "curs", "dist", "fina", "haln", "half", "init", "isol",
			"ljmo", "locl", "mark", "medi", "mkmk", "nukt", "pref", "pres",
			"pstf", "psts", "rclt", "rkrf", "rlig", "tjmo", "vjmo"
		};

		[Test]
		public void LegacyBlocklistTags_AreAllHidden()
		{
			foreach (var tag in LegacyBlocklist)
			{
				Assert.That(OpenTypeFeatureCatalog.IsKnown(tag), Is.True, $"'{tag}' should be catalogued");
				Assert.That(OpenTypeFeatureCatalog.IsHidden(tag), Is.True, $"'{tag}' should remain hidden");
			}
		}

		[Test]
		public void DiscretionaryLigatures_AreUserVisible()
		{
			// Audit correction: Paratext hides dlig, but it is the canonical user-facing ligature feature.
			Assert.That(OpenTypeFeatureCatalog.IsHidden("dlig"), Is.False);
		}

		[Test]
		public void AccessAllAlternates_IsHidden()
		{
			// Audit correction: aalt is a glyph palette, not a meaningful on/off toggle.
			Assert.That(OpenTypeFeatureCatalog.IsHidden("aalt"), Is.True);
		}

		[TestCase("liga")]
		[TestCase("clig")]
		[TestCase("calt")]
		[TestCase("kern")]
		public void CommonlyDefaultOnFeatures_AreDefaultOn(string tag)
		{
			Assert.That(OpenTypeFeatureCatalog.IsHidden(tag), Is.False);
			Assert.That(OpenTypeFeatureCatalog.IsDefaultOn(tag), Is.True);
		}

		[Test]
		public void DefaultOnFeatures_AreOnlyTheDocumentedFour()
		{
			// A default-on feature is applied to text the user never opted into, and is written to
			// the writing system only when they turn it off, so the set is a policy decision rather
			// than a per-tag judgement. Docs/opentype-font-features.md names these four; asserting
			// membership one tag at a time cannot catch a fifth being added, which is how "rand",
			// "chws", "cpsp", "halt" and "size" got in. LT-22638.
			//
			// The set deliberately diverges from the OpenType registry, which suggests cpsp, rand,
			// chws and halt be on by default. Unlike the Graphite provider, which reads defaultValue
			// out of the font, this one asserts it from a static table, because OpenType has no
			// per-feature default to read; a wrong assertion here inverts the user's first click.
			// Docs/opentype-font-features.md has the full reasoning, and LT-22774 measures what
			// Uniscribe actually applies. Do not widen this set without that measurement.
			var defaultOn = OpenTypeFeatureCatalog.AllTags
				.Where(OpenTypeFeatureCatalog.IsDefaultOn)
				.OrderBy(tag => tag, StringComparer.Ordinal)
				.ToArray();

			Assert.That(defaultOn, Is.EqualTo(new[] { "calt", "clig", "kern", "liga" }));
		}

		[Test]
		public void UnknownTag_IsNeitherKnownNorHidden()
		{
			Assert.That(OpenTypeFeatureCatalog.IsKnown("zzzz"), Is.False);
			Assert.That(OpenTypeFeatureCatalog.IsHidden("zzzz"), Is.False);
			Assert.That(OpenTypeFeatureCatalog.GetEnglishName("zzzz"), Is.Null);
		}

		[Test]
		public void VisibleFeatures_HaveEnglishNames()
		{
			foreach (var tag in OpenTypeFeatureCatalog.AllTags.Where(t => !OpenTypeFeatureCatalog.IsHidden(t)))
				Assert.That(OpenTypeFeatureCatalog.GetEnglishName(tag), Is.Not.Null.And.Not.Empty,
					$"visible feature '{tag}' needs a friendly name");
		}
	}
}
