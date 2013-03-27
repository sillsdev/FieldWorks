using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_DebugProces gets disposed in FixtureTeardown")]
	public class PalasoWritingSystemTests// can't derive from BaseTest, but instantiate DebugProcs instead
	{
		private DebugProcs m_DebugProcs;

		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			// This needs to be set for ICU
			RegistryHelper.CompanyName = "SIL";
			Icu.InitIcuDataDir();
			m_DebugProcs = new DebugProcs();
		}

		/// <summary>
		/// Cleans up some resources that were used during the test
		/// </summary>
		[TestFixtureTearDown]
		public virtual void FixtureTeardown()
		{
			m_DebugProcs.Dispose();
			m_DebugProcs = null;
		}

		void VerifySubtagCodes(PalasoWritingSystem ws, string langCode, string scriptCode, string regionCode, string variantCode, string id)
		{
			Assert.That(ws.LanguageSubtag.Code, Is.EqualTo(langCode));
			if (scriptCode == null)
				Assert.That(ws.ScriptSubtag, Is.Null);
			else
				Assert.That(ws.ScriptSubtag.Code, Is.EqualTo(scriptCode));
			if (regionCode == null)
				Assert.That(ws.RegionSubtag, Is.Null);
			else
				Assert.That(ws.RegionSubtag.Code, Is.EqualTo(regionCode));
			if (variantCode == null)
				Assert.That(ws.VariantSubtag, Is.Null);
			else
				Assert.That(ws.VariantSubtag.Code, Is.EqualTo(variantCode));

			// Now check that we can get the same tags by parsing the ID.

			LanguageSubtag languageSubtag;
			ScriptSubtag scriptSubtag;
			RegionSubtag regionSubtag;
			VariantSubtag variantSubtag;
			LangTagUtils.GetSubtags(id, out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag);
			Assert.That(languageSubtag.Code, Is.EqualTo(langCode));
			if (scriptCode == null)
				Assert.That(scriptSubtag, Is.Null);
			else
				Assert.That(scriptSubtag.Code, Is.EqualTo(scriptCode));
			if (regionCode == null)
				Assert.That(regionSubtag, Is.Null);
			else
				Assert.That(regionSubtag.Code, Is.EqualTo(regionCode));
			if (variantCode == null)
				Assert.That(variantSubtag, Is.Null);
			else
				Assert.That(variantSubtag.Code, Is.EqualTo(variantCode));
		}

		void VerifyComponents(PalasoWritingSystem ws, string lang, string script, string region, string variant, string id)
		{
			Assert.That(ws.Language, Is.EqualTo(lang));
			Assert.That(ws.Script, Is.EqualTo(script));
			Assert.That(ws.Region, Is.EqualTo(region));
			Assert.That(ws.Variant, Is.EqualTo(variant));
			Assert.That(ws.Id, Is.EqualTo(id));
		}

		[Test]
		public void LanguageAndVariantTags()
		{
			// A new writing system has a Language tag of qaa. This is also its language tag. The others are null.
			var ws = new PalasoWritingSystem();
			VerifySubtagCodes(ws, "qaa", null, null, null, "qaa");
			VerifyComponents(ws, "qaa", "", "", "", "qaa");

			ws.LanguageSubtag = LangTagUtils.GetLanguageSubtag("en");
			VerifySubtagCodes(ws, "en", null, null, null, "en");
			VerifyComponents(ws, "en", "", "", "", "en");
			Assert.That(ws.LanguageName, Is.EqualTo("English"));

			ws.LanguageSubtag = new LanguageSubtag("kal", "Kalaba", true, "");
			Assert.That(ws.LanguageName, Is.EqualTo("Kalaba"));
			VerifySubtagCodes(ws, "kal", null, null, null, "qaa-x-kal");
			VerifyComponents(ws, "qaa", "", "", "x-kal", "qaa-x-kal");
			Assert.That(ws.LanguageSubtag.Name, Is.EqualTo("Kalaba"));

			// This is a region code that is valid, so we don't store it in the private-use area of our code.
			ws.RegionSubtag = LangTagUtils.GetRegionSubtag("QN");
			VerifySubtagCodes(ws, "kal", null, "QN", null, "qaa-QN-x-kal");
			VerifyComponents(ws, "qaa", "", "QN", "x-kal", "qaa-QN-x-kal");

			// This is a standard region (Norway).
			ws.RegionSubtag = LangTagUtils.GetRegionSubtag("NO");
			VerifySubtagCodes(ws, "kal", null, "NO", null, "qaa-NO-x-kal");
			VerifyComponents(ws, "qaa", "", "NO", "x-kal", "qaa-NO-x-kal");

			// A private region
			ws.RegionSubtag = LangTagUtils.GetRegionSubtag("ZD");
			VerifySubtagCodes(ws, "kal", null, "ZD", null, "qaa-QM-x-kal-ZD");
			VerifyComponents(ws, "qaa", "", "QM", "x-kal-ZD", "qaa-QM-x-kal-ZD");

			// Add a private script
			ws.ScriptSubtag = LangTagUtils.GetScriptSubtag("Zfdr");
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", null, "qaa-Qaaa-QM-x-kal-Zfdr-ZD");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "x-kal-Zfdr-ZD", "qaa-Qaaa-QM-x-kal-Zfdr-ZD");

			// Change it to a standard one
			ws.ScriptSubtag = LangTagUtils.GetScriptSubtag("Phnx");
			VerifySubtagCodes(ws, "kal", "Phnx", "ZD", null, "qaa-Phnx-QM-x-kal-ZD");
			VerifyComponents(ws, "qaa", "Phnx", "QM", "x-kal-ZD", "qaa-Phnx-QM-x-kal-ZD");

			// To the standard private-use marker
			ws.ScriptSubtag = LangTagUtils.GetScriptSubtag("Qaaa");
			VerifySubtagCodes(ws, "kal", "Qaaa", "ZD", null, "qaa-Qaaa-QM-x-kal-Qaaa-ZD");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "x-kal-Qaaa-ZD", "qaa-Qaaa-QM-x-kal-Qaaa-ZD");


			// Back to the special one
			ws.ScriptSubtag = LangTagUtils.GetScriptSubtag("Zfdr");
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", null, "qaa-Qaaa-QM-x-kal-Zfdr-ZD");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "x-kal-Zfdr-ZD", "qaa-Qaaa-QM-x-kal-Zfdr-ZD");

			// Add a standard variant
			ws.VariantSubtag = LangTagUtils.GetVariantSubtag("fonipa");
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", "fonipa", "qaa-Qaaa-QM-fonipa-x-kal-Zfdr-ZD");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "fonipa-x-kal-Zfdr-ZD", "qaa-Qaaa-QM-fonipa-x-kal-Zfdr-ZD");

			// Change it to a combination one
			ws.VariantSubtag = LangTagUtils.GetVariantSubtag("fonipa-x-etic");
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", "fonipa-x-etic", "qaa-Qaaa-QM-fonipa-x-kal-Zfdr-ZD-etic");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "fonipa-x-kal-Zfdr-ZD-etic", "qaa-Qaaa-QM-fonipa-x-kal-Zfdr-ZD-etic");

			// Back to no variant.
			ws.VariantSubtag = null;
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", null, "qaa-Qaaa-QM-x-kal-Zfdr-ZD");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "x-kal-Zfdr-ZD", "qaa-Qaaa-QM-x-kal-Zfdr-ZD");

			// Try a double combination
			ws.VariantSubtag = LangTagUtils.GetVariantSubtag("fonipa-1996-x-etic-emic");
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", "fonipa-1996-x-etic-emic", "qaa-Qaaa-QM-fonipa-1996-x-kal-Zfdr-ZD-etic-emic");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "fonipa-1996-x-kal-Zfdr-ZD-etic-emic", "qaa-Qaaa-QM-fonipa-1996-x-kal-Zfdr-ZD-etic-emic");

			// Drop a piece out of each
			ws.VariantSubtag = LangTagUtils.GetVariantSubtag("fonipa-x-etic");
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", "fonipa-x-etic", "qaa-Qaaa-QM-fonipa-x-kal-Zfdr-ZD-etic");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "fonipa-x-kal-Zfdr-ZD-etic", "qaa-Qaaa-QM-fonipa-x-kal-Zfdr-ZD-etic");

			// Soemthing totally unknown
			ws.VariantSubtag = LangTagUtils.GetVariantSubtag("fonipa-x-blah");
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", "fonipa-x-blah", "qaa-Qaaa-QM-fonipa-x-kal-Zfdr-ZD-blah");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "fonipa-x-kal-Zfdr-ZD-blah", "qaa-Qaaa-QM-fonipa-x-kal-Zfdr-ZD-blah");

			// Drop just the standard part
			ws.VariantSubtag = LangTagUtils.GetVariantSubtag("x-blah");
			VerifySubtagCodes(ws, "kal", "Zfdr", "ZD", "x-blah", "qaa-Qaaa-QM-x-kal-Zfdr-ZD-blah");
			VerifyComponents(ws, "qaa", "Qaaa", "QM", "x-kal-Zfdr-ZD-blah", "qaa-Qaaa-QM-x-kal-Zfdr-ZD-blah");

			// No longer a custom language
			ws.LanguageSubtag = LangTagUtils.GetLanguageSubtag("en");
			VerifySubtagCodes(ws, "en", "Zfdr", "ZD", "x-blah", "en-Qaaa-QM-x-Zfdr-ZD-blah");
			VerifyComponents(ws, "en", "Qaaa", "QM", "x-Zfdr-ZD-blah", "en-Qaaa-QM-x-Zfdr-ZD-blah");

			// No longer a custom script
			ws.ScriptSubtag = null;
			VerifySubtagCodes(ws, "en", null, "ZD", "x-blah", "en-QM-x-ZD-blah");
			VerifyComponents(ws, "en", "", "QM", "x-ZD-blah", "en-QM-x-ZD-blah");

			// No longer a custom region
			ws.RegionSubtag = null;
			VerifySubtagCodes(ws, "en", null, null, "x-blah", "en-x-blah");
			VerifyComponents(ws, "en", "", "", "x-blah", "en-x-blah");

			// No more variant
			ws.VariantSubtag = null;
			VerifySubtagCodes(ws, "en", null, null, null, "en");
			VerifyComponents(ws, "en", "", "", "", "en");
		}
	}
}
