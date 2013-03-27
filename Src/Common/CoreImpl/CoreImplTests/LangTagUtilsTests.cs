using System;
using System.Diagnostics.CodeAnalysis;

using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Test fixture for LangTagUtils class.
	/// </summary>
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_DebugProces gets disposed in FixtureTeardown")]
	public class LangTagUtilsTests // can't derive from BaseTest, but instantiate DebugProcs instead
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

		/// <summary>
		/// Tests the ToLangTag method.
		/// </summary>
		[Test]
		public void ToLangTag()
		{
			// language
			Assert.AreEqual("en", LangTagUtils.ToLangTag("en"));
			// language, script
			Assert.AreEqual("en-Latn", LangTagUtils.ToLangTag("en_Latn"));
			// language, region
			Assert.AreEqual("en-US", LangTagUtils.ToLangTag("en_US"));
			// language, script, region, ICU variant
			Assert.AreEqual("en-Latn-US-fonipa-x-etic", LangTagUtils.ToLangTag("en_Latn_US_X_ETIC"));
			// language, ICU variant
			Assert.AreEqual("en-fonipa-x-emic", LangTagUtils.ToLangTag("en__X_EMIC"));
			// language, region, ICU variant
			Assert.AreEqual("zh-CN-pinyin", LangTagUtils.ToLangTag("zh_CN_X_PY"));
			// private use language
			Assert.AreEqual("qaa-x-kal", LangTagUtils.ToLangTag("xkal"));
			// private use language, custom ICU variant
			Assert.AreEqual("qaa-fonipa-x-kal", LangTagUtils.ToLangTag("xkal__IPA"));
			// private use language, (standard) private use region
			Assert.AreEqual("qaa-XA-x-kal", LangTagUtils.ToLangTag("xkal_XA"));
			// private use language, (non-standard) private use script
			Assert.AreEqual("qaa-Qaaa-x-kal-Fake", LangTagUtils.ToLangTag("xkal_Fake"));
			// language, private use script
			Assert.AreEqual("en-Qaaa-x-Fake", LangTagUtils.ToLangTag("en_Fake"));
			// language, private use script, private use region
			Assert.AreEqual("en-Qaaa-QM-x-Fake-QD", LangTagUtils.ToLangTag("en_Fake_QD"));
			// private use language, script
			Assert.AreEqual("qaa-Latn-x-zzz", LangTagUtils.ToLangTag("zzz_Latn"));
			// convert older FW language tags
			Assert.AreEqual("slu", LangTagUtils.ToLangTag("eslu"));
			// other possibilities from FW6.0.6
			Assert.AreEqual("qaa-x-bcd", LangTagUtils.ToLangTag("x123"));
			Assert.AreEqual("qaa-x-kac", LangTagUtils.ToLangTag("xka2"));

			// following are already lang tags
			Assert.AreEqual("en-US", LangTagUtils.ToLangTag("en-US"));
			Assert.AreEqual("en-Latn-US-fonipa-x-etic", LangTagUtils.ToLangTag("en-Latn-US-fonipa-x-etic"));
		}

		/// <summary>
		/// Tests the ToIcuLocale method.
		/// </summary>
		[Test]
		public void ToIcuLocale()
		{
			// language
			Assert.AreEqual("en", LangTagUtils.ToIcuLocale("en"));
			// language, script
			Assert.AreEqual("en_Latn", LangTagUtils.ToIcuLocale("en-Latn"));
			// language, region
			Assert.AreEqual("en_US", LangTagUtils.ToIcuLocale("en-US"));
			// language, script, region, ICU variant
			Assert.AreEqual("en_Latn_US_X_ETIC", LangTagUtils.ToIcuLocale("en-Latn-US-fonipa-x-etic"));
			// language, ICU variant
			Assert.AreEqual("en__X_EMIC", LangTagUtils.ToIcuLocale("en-fonipa-x-emic"));
			// language, region, ICU variant
			Assert.AreEqual("zh_CN_X_PY", LangTagUtils.ToIcuLocale("zh-CN-pinyin"));
			// private use language
			Assert.AreEqual("xkal", LangTagUtils.ToIcuLocale("qaa-x-kal"));
			// private use language, ICU variant
			Assert.AreEqual("xkal__X_ETIC", LangTagUtils.ToIcuLocale("qaa-fonipa-x-kal-etic"));
			// private use language, private use region
			Assert.AreEqual("xkal_XA", LangTagUtils.ToIcuLocale("qaa-QM-x-kal-XA"));
			// private use language, private use script
			Assert.AreEqual("xkal_Fake", LangTagUtils.ToIcuLocale("qaa-Qaaa-x-kal-Fake"));
			// language, private use script
			Assert.AreEqual("en_Fake", LangTagUtils.ToIcuLocale("en-Qaaa-x-Fake"));
			// language, private use script, private use region
			Assert.AreEqual("en_Fake_QD", LangTagUtils.ToIcuLocale("en-Qaaa-QM-x-Fake-QD"));
			// private use language, script
			Assert.AreEqual("xzzz_Latn", LangTagUtils.ToIcuLocale("qaa-Latn-x-zzz"));
		}

		/// <summary>
		/// Tests the ToIcuLocale method with an invalid language tag.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void ToIcuLocale_InvalidLangTag()
		{
			LangTagUtils.ToIcuLocale("en_Latn_US_X_ETIC");
		}

		/// <summary>
		/// Tests the GetSubtags method.
		/// </summary>
		[Test]
		public void GetSubtags()
		{
			LanguageSubtag languageSubtag;
			ScriptSubtag scriptSubtag;
			RegionSubtag regionSubtag;
			VariantSubtag variantSubtag;
			Assert.IsTrue(LangTagUtils.GetSubtags("en", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
			Assert.AreEqual("en", languageSubtag.Code);
			Assert.IsFalse(languageSubtag.IsPrivateUse);
			Assert.IsNull(scriptSubtag);
			Assert.IsNull(regionSubtag);
			Assert.IsNull(variantSubtag);

			Assert.IsTrue(LangTagUtils.GetSubtags("en-Latn", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
			Assert.AreEqual("en", languageSubtag.Code);
			Assert.IsFalse(languageSubtag.IsPrivateUse);
			Assert.AreEqual("Latn", scriptSubtag.Code);
			Assert.IsFalse(scriptSubtag.IsPrivateUse);
			Assert.IsNull(regionSubtag);
			Assert.IsNull(variantSubtag);

			Assert.IsTrue(LangTagUtils.GetSubtags("en-US", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
			Assert.AreEqual("en", languageSubtag.Code);
			Assert.IsFalse(languageSubtag.IsPrivateUse);
			Assert.IsNull(scriptSubtag);
			Assert.AreEqual("US", regionSubtag.Code);
			Assert.IsFalse(regionSubtag.IsPrivateUse);
			Assert.IsNull(variantSubtag);

			Assert.IsTrue(LangTagUtils.GetSubtags("en-Latn-US-fonipa-x-etic", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
			Assert.AreEqual("en", languageSubtag.Code);
			Assert.IsFalse(languageSubtag.IsPrivateUse);
			Assert.AreEqual("Latn", scriptSubtag.Code);
			Assert.IsFalse(scriptSubtag.IsPrivateUse);
			Assert.AreEqual("US", regionSubtag.Code);
			Assert.IsFalse(regionSubtag.IsPrivateUse);
			Assert.AreEqual("fonipa-x-etic", variantSubtag.Code);
			Assert.IsFalse(variantSubtag.IsPrivateUse);

			Assert.IsTrue(LangTagUtils.GetSubtags("qaa-x-kal", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
			Assert.AreEqual("kal", languageSubtag.Code);
			Assert.IsTrue(languageSubtag.IsPrivateUse);
			Assert.IsNull(scriptSubtag);
			Assert.IsNull(regionSubtag);
			Assert.IsNull(variantSubtag);

			Assert.IsTrue(LangTagUtils.GetSubtags("qaa-Qaaa-x-kal-Fake", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
			Assert.AreEqual("kal", languageSubtag.Code);
			Assert.IsTrue(languageSubtag.IsPrivateUse);
			Assert.AreEqual("Fake", scriptSubtag.Code);
			Assert.IsTrue(scriptSubtag.IsPrivateUse);
			Assert.IsNull(regionSubtag);
			Assert.IsNull(variantSubtag);

			Assert.IsTrue(LangTagUtils.GetSubtags("qaa-QM-x-kal-XA", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
			Assert.AreEqual("kal", languageSubtag.Code);
			Assert.IsTrue(languageSubtag.IsPrivateUse);
			Assert.IsNull(scriptSubtag);
			Assert.AreEqual("XA", regionSubtag.Code);
			Assert.IsTrue(regionSubtag.IsPrivateUse);
			Assert.IsNull(variantSubtag);

			Assert.IsTrue(LangTagUtils.GetSubtags("en-Qaaa-QM-x-Fake-QD", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
			Assert.AreEqual("en", languageSubtag.Code);
			Assert.IsFalse(languageSubtag.IsPrivateUse);
			Assert.AreEqual("Fake", scriptSubtag.Code);
			Assert.IsTrue(scriptSubtag.IsPrivateUse);
			Assert.AreEqual("QD", regionSubtag.Code);
			Assert.IsTrue(regionSubtag.IsPrivateUse);
			Assert.IsNull(variantSubtag);

			Assert.IsFalse(LangTagUtils.GetSubtags("en_Latn_US_X_ETIC", out languageSubtag, out scriptSubtag, out regionSubtag, out variantSubtag));
		}
	}
}
