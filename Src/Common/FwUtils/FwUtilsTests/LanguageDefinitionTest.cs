// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LanguageDefinitionTest.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;	// for ILgWritingSystemFactory

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for LanguageDefinitionTest.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LanguageDefinitionTest
	{
		private ILgWritingSystemFactory m_wsf;
		private IWritingSystem m_wsEn;
		private int m_wsIdEn;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_wsf = LgWritingSystemFactoryClass.Create();
			// This is typically run during the build process before InstallLanguage.exe has
			// been built, so we want to disable InstallLanguage for this test.
			m_wsf.BypassInstall = true;

			m_wsEn = m_wsf.get_Engine("en");
			m_wsIdEn = m_wsf.GetWsFromStr("en");
			m_wsEn.set_Name(m_wsIdEn, "English");
			m_wsEn.set_Abbr(m_wsIdEn, "ENG");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends a test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_wsf.Shutdown();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the abbreviations from the ICU locale if it contains only the
		/// locale abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAbbreviationsNameOnly()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en";

			Assert.AreEqual("en", langDef.LocaleAbbr);
			Assert.AreEqual("", langDef.CountryAbbr);
			Assert.AreEqual("", langDef.VariantAbbr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the abbreviations from the ICU locale if it contains the
		/// locale and country abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAbbreviationsNameAndCountry()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en_US";

			Assert.AreEqual("en", langDef.LocaleAbbr);
			Assert.AreEqual("US", langDef.CountryAbbr);
			Assert.AreEqual("", langDef.VariantAbbr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the abbreviations from the ICU locale if it contains the
		/// locale, country and variant abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAbbreviationsNameCountryVariant()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en_US_X_ETIC";

			Assert.AreEqual("en", langDef.LocaleAbbr);
			Assert.AreEqual("US", langDef.CountryAbbr);
			Assert.AreEqual("X_ETIC", langDef.VariantAbbr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the abbreviations from the ICU locale if it contains the
		/// locale and variant but no country abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAbbreviationsNameAndVariant()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en__X_ETIC";

			Assert.AreEqual("en", langDef.LocaleAbbr);
			Assert.AreEqual("", langDef.CountryAbbr);
			Assert.AreEqual("X_ETIC", langDef.VariantAbbr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the abbreviations from the ICU locale if it contains an underline.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAbbreviationsUnderline()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en_Latn_US_X_ETIC_x";
			Assert.AreEqual("en", langDef.LocaleAbbr);
			Assert.AreEqual("Latn", langDef.ScriptAbbr);
			Assert.AreEqual("US", langDef.CountryAbbr);
			Assert.AreEqual("X_ETIC_X", langDef.VariantAbbr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the name abbreviation where all 3 parts exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetAbbreviationsChangeAll()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en_Latn_US_X_ETIC";

			langDef.LocaleAbbr = "fr";
			langDef.ScriptAbbr = "Hant";
			langDef.CountryAbbr = "UK";
			langDef.VariantAbbr = "NONE";
			Assert.AreEqual("fr_Hant_UK_NONE", langDef.XmlWritingSystem.WritingSystem.IcuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the script abbreviation where only name and variant exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetAbbreviationsInsertScript()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en__X_ETIC";

			langDef.ScriptAbbr = "Latn";
			Assert.AreEqual("en_Latn__X_ETIC", langDef.XmlWritingSystem.WritingSystem.IcuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the country abbreviation where only name and variant exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetAbbreviationsInsertCountry()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en__X_ETIC";

			langDef.CountryAbbr = "UK";
			Assert.AreEqual("en_UK_X_ETIC", langDef.XmlWritingSystem.WritingSystem.IcuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the variant abbreviation where only name and script exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetAbbreviationsAddScript()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en_Latn";

			langDef.VariantAbbr = "X_ETIC";
			Assert.AreEqual("en_Latn__X_ETIC", langDef.XmlWritingSystem.WritingSystem.IcuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the variant abbreviation where only name and country exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetAbbreviationsAddVariant()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en_US";

			langDef.VariantAbbr = "X_ETIC";
			Assert.AreEqual("en_US_X_ETIC", langDef.XmlWritingSystem.WritingSystem.IcuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the variant abbreviation where only name exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetAbbreviationsNameOnlyAddVariant()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en";

			langDef.VariantAbbr = "X_ETIC";
			Assert.AreEqual("en__X_ETIC", langDef.XmlWritingSystem.WritingSystem.IcuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the country abbreviation where only name exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetAbbreviationsNameOnlyAddCountry()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.XmlWritingSystem.WritingSystem.IcuLocale = "en";

			langDef.CountryAbbr = "US";
			Assert.AreEqual("en_US", langDef.XmlWritingSystem.WritingSystem.IcuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the display name if only name is set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDisplayNameNameOnly()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.LocaleName = "English";

			Assert.AreEqual("English", langDef.DisplayName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the display name if name and country are set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDisplayNameNameAndScript()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.LocaleName = "English";
			langDef.LocaleScript = "Latin";

			Assert.AreEqual("English (Latin)", langDef.DisplayName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the display name if name and country are set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDisplayNameNameAndCountry()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.LocaleName = "English";
			langDef.LocaleCountry = "United States";

			Assert.AreEqual("English (United States)", langDef.DisplayName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the display name if name, country and variant are set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDisplayNameAll()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.LocaleName = "English";
			langDef.LocaleScript = "Latin";
			langDef.LocaleCountry = "United States";
			langDef.LocaleVariant = "Phonetic";

			Assert.AreEqual("English (Latin, United States, Phonetic)", langDef.DisplayName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the display name if name and variant are set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDisplayNameNameAndVariant()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.LocaleName = "English";
			langDef.LocaleVariant = "Phonetic";

			Assert.AreEqual("English (Phonetic)", langDef.DisplayName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the PuaDefinitions to null. Should just clear the collection. TE-8642.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetPuaDefinitionsToNull()
		{
			LanguageDefinition langDef = new LanguageDefinition(m_wsEn);
			langDef.PuaDefinitions = new CharDef[] {new CharDef(0xF170, "COMBINING SNAKE BELOW;Mn;202;NSM;;;;")} ;
			Assert.AreEqual(1, langDef.PuaDefinitionCount);
			langDef.PuaDefinitions = null;
			Assert.AreEqual(0, langDef.PuaDefinitionCount);
		}
	}
}
