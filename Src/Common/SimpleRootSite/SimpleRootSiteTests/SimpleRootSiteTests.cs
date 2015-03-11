using System.Globalization;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.Keyboarding;
using SIL.WritingSystems;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	[TestFixture]
	public class SimpleRootSiteTests
	{
		[Test]
		public void GetWsForInputLanguage_GetsMatchingWsByCulture()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsFr = new CoreWritingSystemDefinition("fr");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("French", "fr-FR");
			wsFr.LocalKeyboard = kbdFr;

			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("en-US"), wsEn, new[] {wsEn, wsFr}), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("en-US"), wsFr, new[] { wsEn, wsFr }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("en-US"), wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("en-US"), wsFr, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("fr-FR"), wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("fr-FR"), wsEn, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("fr-FR"), null, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
		}

		[Test]
		public void GetWsForInputLanguage_PrefersCurrentCultureIfTwoMatch()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsFr = new CoreWritingSystemDefinition("fr");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("French", "en-US");
			wsFr.LocalKeyboard = kbdFr;

			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("en-US"), wsEn, new[] { wsEn, wsFr }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("en-US"), wsFr, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("en-US"), wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("en-US"), wsFr, new[] { wsFr, wsEn }), Is.EqualTo(wsFr));
		}

		[Test]
		public void GetWsForInputLanguage_PrefersCurrentLayoutIfTwoMatch()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsFr = new CoreWritingSystemDefinition("fr");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("English", "fr-US");
			wsFr.LocalKeyboard = kbdFr;

			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("de-DE"), wsEn, new[] { wsEn, wsFr }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("de-DE"), wsFr, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("de-DE"), wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("de-DE"), wsFr, new[] { wsFr, wsEn }), Is.EqualTo(wsFr));
		}

		[Test]
		public void GetWsForInputLanguage_CorrectlyPrioritizesLayoutAndCulture()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsEnIpa = new CoreWritingSystemDefinition("en-fonipa");
			var wsFr = new CoreWritingSystemDefinition("fr");
			var wsDe = new CoreWritingSystemDefinition("de");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdEnIpa = CreateKeyboard("English-IPA", "en-US");
			wsEnIpa.LocalKeyboard = kbdEnIpa;
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("French", "fr-FR");
			wsFr.LocalKeyboard = kbdFr;
			DefaultKeyboardDefinition kbdDe = CreateKeyboard("English", "de-DE");
			wsDe.LocalKeyboard = kbdDe;

			CoreWritingSystemDefinition[] wss = {wsEn, wsFr, wsDe, wsEnIpa};

			// Exact match selects correct one, even though there are other matches for layout and/or culture
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("en-US"), wsFr, wss), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English-IPA", new CultureInfo("en-US"), wsEn, wss), Is.EqualTo(wsEnIpa));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("French", new CultureInfo("fr-FR"), wsDe, wss), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("de-DE"), wsEn, wss), Is.EqualTo(wsDe));

			// If there is no exact match, but there are matches by both layout and culture, we prefer layout (even though there is a
			// culture match for the default WS)
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("fr-FR"), wsFr, wss), Is.EqualTo(wsEn)); // first of two equally good matches
		}

		[Test]
		public void GetWsForInputLanguage_PrefersWsCurrentIfEqualMatches()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsEnUS = new CoreWritingSystemDefinition("en-US");
			var wsEnIpa = new CoreWritingSystemDefinition("en-fonipa");
			var wsFr = new CoreWritingSystemDefinition("fr");
			var wsDe = new CoreWritingSystemDefinition("de");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdEnIpa = CreateKeyboard("English-IPA", "en-US");
			wsEnIpa.LocalKeyboard = kbdEnIpa;
			wsEnUS.LocalKeyboard = kbdEn; // exact same keyboard used!
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("French", "fr-FR");
			wsFr.LocalKeyboard = kbdFr;
			DefaultKeyboardDefinition kbdDe = CreateKeyboard("English", "de-DE");
			wsDe.LocalKeyboard = kbdDe;

			CoreWritingSystemDefinition[] wss = {wsEn, wsFr, wsDe, wsEnIpa, wsEnUS};

			// Exact matches
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("en-US"), wsFr, wss), Is.EqualTo(wsEn)); // first of 2
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("en-US"), wsEn, wss), Is.EqualTo(wsEn)); // prefer default
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("en-US"), wsEnUS, wss), Is.EqualTo(wsEnUS)); // prefer default

			// Match on Layout only
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("fr-FR"), wsFr, wss), Is.EqualTo(wsEn)); // first of 3
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("fr-FR"), wsEn, wss), Is.EqualTo(wsEn)); // prefer default
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("fr-FR"), wsEnUS, wss), Is.EqualTo(wsEnUS)); // prefer default
			Assert.That(SimpleRootSite.GetWSForInputLanguage("English", new CultureInfo("fr-FR"), wsDe, wss), Is.EqualTo(wsDe)); // prefer default

			// Match on culture only
			Assert.That(SimpleRootSite.GetWSForInputLanguage("Nonsence", new CultureInfo("en-US"), wsDe, wss), Is.EqualTo(wsEn)); // first of 3
			Assert.That(SimpleRootSite.GetWSForInputLanguage("Nonsence", new CultureInfo("en-US"), wsEn, wss), Is.EqualTo(wsEn)); // prefer default
			Assert.That(SimpleRootSite.GetWSForInputLanguage("Nonsence", new CultureInfo("en-US"), wsEnUS, wss), Is.EqualTo(wsEnUS)); // prefer default
			Assert.That(SimpleRootSite.GetWSForInputLanguage("Nonsence", new CultureInfo("en-US"), wsEnIpa, wss), Is.EqualTo(wsEnIpa)); // prefer default
		}

		[Test]
		public void GetWsForInputLanguage_ReturnsCurrentIfNoneMatches()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsFr = new CoreWritingSystemDefinition("fr");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("French", "en-US");
			wsFr.LocalKeyboard = kbdFr;

			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("fr-FR"), wsEn, new[] { wsEn, wsFr }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("fr-FR"), wsFr, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("fr-FR"), wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("fr-FR"), wsFr, new[] { wsFr, wsEn }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputLanguage("", new CultureInfo("fr-FR"), null, new[] { wsFr, wsEn }), Is.Null);
		}

		private static DefaultKeyboardDefinition CreateKeyboard(string layout, string locale)
		{
			return new DefaultKeyboardDefinition(string.Format("{1}_{0}", layout, locale), layout, layout, locale, true);
		}
	}
}
