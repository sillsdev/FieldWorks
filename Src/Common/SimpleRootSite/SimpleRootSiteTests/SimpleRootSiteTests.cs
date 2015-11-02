using NUnit.Framework;
using SIL.CoreImpl;
using SIL.Keyboarding;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	[TestFixture]
	public class SimpleRootSiteTests
	{
		[Test]
		public void GetWSForInputMethod_GetsMatchingWSByInputMethod()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsFr = new CoreWritingSystemDefinition("fr");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("French", "fr-FR");
			wsFr.LocalKeyboard = kbdFr;

			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEn, wsEn, new[] { wsEn, wsFr }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEn, wsFr, new[] { wsEn, wsFr }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEn, wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEn, wsFr, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdFr, wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdFr, wsEn, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdFr, null, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
		}

		[Test]
		public void GetWSForInputMethod_PrefersCurrentLayoutIfTwoMatch()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsFr = new CoreWritingSystemDefinition("fr");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("French", "fr-FR");
			wsFr.LocalKeyboard = kbdFr;
			DefaultKeyboardDefinition kbdDe = CreateKeyboard("German", "de-DE");

			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsEn, new[] { wsEn, wsFr }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsFr, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsFr, new[] { wsFr, wsEn }), Is.EqualTo(wsFr));
		}

		[Test]
		public void GetWsForInputMethod_CorrectlyPrioritizesInputMethod()
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
			DefaultKeyboardDefinition kbdDe = CreateKeyboard("German", "de-DE");
			wsDe.LocalKeyboard = kbdDe;

			CoreWritingSystemDefinition[] wss = {wsEn, wsFr, wsDe, wsEnIpa};

			// Exact match selects correct one, even though there are other matches for layout and/or culture
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEn, wsFr, wss), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEnIpa, wsEn, wss), Is.EqualTo(wsEnIpa));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdFr, wsDe, wss), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsEn, wss), Is.EqualTo(wsDe));
		}

		[Test]
		public void GetWSForInputMethod_PrefersWSCurrentIfEqualMatches()
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
			DefaultKeyboardDefinition kbdDe = CreateKeyboard("German", "de-DE");
			wsDe.LocalKeyboard = kbdDe;

			CoreWritingSystemDefinition[] wss = {wsEn, wsFr, wsDe, wsEnIpa, wsEnUS};

			// Exact matches
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEn, wsFr, wss), Is.EqualTo(wsEn)); // first of 2
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEn, wsEn, wss), Is.EqualTo(wsEn)); // prefer default
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdEn, wsEnUS, wss), Is.EqualTo(wsEnUS)); // prefer default
		}

		[Test]
		public void GetWSForInputMethod_ReturnsCurrentIfNoneMatches()
		{
			var wsEn = new CoreWritingSystemDefinition("en");
			var wsFr = new CoreWritingSystemDefinition("fr");
			DefaultKeyboardDefinition kbdEn = CreateKeyboard("English", "en-US");
			wsEn.LocalKeyboard = kbdEn;
			DefaultKeyboardDefinition kbdFr = CreateKeyboard("French", "fr-FR");
			wsFr.LocalKeyboard = kbdFr;
			DefaultKeyboardDefinition kbdDe = CreateKeyboard("German", "de-DE");

			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsEn, new[] { wsEn, wsFr }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsFr, new[] { wsEn, wsFr }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsEn, new[] { wsFr, wsEn }), Is.EqualTo(wsEn));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, wsFr, new[] { wsFr, wsEn }), Is.EqualTo(wsFr));
			Assert.That(SimpleRootSite.GetWSForInputMethod(kbdDe, null, new[] { wsFr, wsEn }), Is.Null);
		}

		private static DefaultKeyboardDefinition CreateKeyboard(string layout, string locale)
		{
			return new DefaultKeyboardDefinition(string.Format("{1}_{0}", layout, locale), layout, layout, locale, true);
		}
	}
}
