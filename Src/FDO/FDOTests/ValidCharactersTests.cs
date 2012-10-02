using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ValidCharaters class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ValidCharactersTests
	{
		private const string ksXmlHeader = "<?xml version=\"1.0\" encoding=\"utf-16\"?>";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			ReflectionHelper.SetField(typeof(ValidCharacters), "s_fTestingMode", true);
			ReflectionHelper.SetField(typeof(ValidCharacters), "s_lastException", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Class to facilitate getting at private members of the ValidCharacters class using
		/// Reflection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class ValidCharsWrapper
		{
			ValidCharacters m_validChars;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="ValidCharsWrapper"/> class.
			/// </summary>
			/// <param name="validCharacters">An instance of the valid characters class.</param>
			/// --------------------------------------------------------------------------------
			public ValidCharsWrapper(ValidCharacters validCharacters)
			{
				m_validChars = validCharacters;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the word forming characters list.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public List<string> WordFormingCharacters
			{
				get
				{
					return (List<string>)ReflectionHelper.GetField(m_validChars,
						"m_WordFormingCharacters");
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the numeric characters list.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public List<string> NumericCharacters
			{
				get
				{
					return (List<string>)ReflectionHelper.GetField(m_validChars,
						"m_NumericCharacters");
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the punctuation/symbols/etc. characters list.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public List<string> OtherCharacters
			{
				get
				{
					return (List<string>)ReflectionHelper.GetField(m_validChars,
						"m_OtherCharacters");
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from an old-style list of valid characters.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromOldValidCharsList()
		{
			ValidCharacters validChars = ValidCharacters.Load(" a b c d . 1 2 3", "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(4, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("a"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("b"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("c"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("d"));
			Assert.AreEqual(3, validCharsW.NumericCharacters.Count);
			Assert.IsTrue(validCharsW.NumericCharacters.Contains("1"));
			Assert.IsTrue(validCharsW.NumericCharacters.Contains("2"));
			Assert.IsTrue(validCharsW.NumericCharacters.Contains("3"));
			Assert.AreEqual(2, validCharsW.OtherCharacters.Count);
			Assert.IsTrue(validCharsW.OtherCharacters.Contains(" "));
			Assert.IsTrue(validCharsW.OtherCharacters.Contains("."));

			string spaceReplacer = ReflectionHelper.GetField(typeof(ValidCharacters),
				"kSpaceReplacment") as string;

			Assert.AreEqual(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc\uFFFCd</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3</Numeric>" +
				"<Other>" + spaceReplacer + "\uFFFC.</Other>" +
				"</ValidCharacters>",
				validChars.XmlString.Replace(Environment.NewLine, string.Empty).Replace(">  <", "><"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string of valid characters.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_Valid()
		{
			string sXml = ksXmlHeader + "<ValidCharacters><WordForming>e\uFFFCf\uFFFCg\uFFFCh</WordForming>" +
				"<Numeric>4\uFFFC5</Numeric>" +
				"<Other>,\uFFFC!\uFFFC*</Other>" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(sXml, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(4, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("e"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("f"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("g"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("h"));
			Assert.AreEqual(2, validCharsW.NumericCharacters.Count);
			Assert.IsTrue(validCharsW.NumericCharacters.Contains("4"));
			Assert.IsTrue(validCharsW.NumericCharacters.Contains("5"));
			Assert.AreEqual(3, validCharsW.OtherCharacters.Count);
			Assert.IsTrue(validCharsW.OtherCharacters.Contains(","));
			Assert.IsTrue(validCharsW.OtherCharacters.Contains("!"));
			Assert.IsTrue(validCharsW.OtherCharacters.Contains("*"));
			Assert.AreEqual(sXml,
				validChars.XmlString.Replace(Environment.NewLine, string.Empty).Replace(">  <", "><"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string which defines no valid characters.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_ValidEmpty()
		{
			string sXml = ksXmlHeader + "<ValidCharacters><WordForming />" +
				"<Numeric />" +
				"<Other />" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(sXml, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(0, validCharsW.WordFormingCharacters.Count);
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
			Assert.AreEqual(sXml,
				validChars.XmlString.Replace(Environment.NewLine, string.Empty).Replace(">  <", "><"));
			Assert.IsNull(ReflectionHelper.GetField(typeof(ValidCharacters), "s_lastException"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a null string.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_ValidNull()
		{
			ValidCharacters validChars = ValidCharacters.Load(null, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(0, validCharsW.WordFormingCharacters.Count);
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
			Assert.IsNull(ReflectionHelper.GetField(typeof(ValidCharacters), "s_lastException"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from an empty string.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_ValidEmptyString()
		{
			ValidCharacters validChars = ValidCharacters.Load(String.Empty, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(0, validCharsW.WordFormingCharacters.Count);
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
			Assert.IsNull(ReflectionHelper.GetField(typeof(ValidCharacters), "s_lastException"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from XML string of valid characters containing U+2028 (Line
		/// Separator/ Hard Line Break) in the "Other" list. LT-9985
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_AllowHardLineBreakCharacter()
		{
			string sXml = ksXmlHeader + "<ValidCharacters><WordForming></WordForming>" +
				"<Numeric></Numeric>" +
				"<Other>\u2028</Other>" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(sXml, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(1, validCharsW.OtherCharacters.Count);
			Assert.IsTrue(validCharsW.OtherCharacters.Contains("\u2028"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string with a bogus format (TE-8304).
		/// Ideally, I think we'd want this to throw an exception, but the deserializer just
		/// ignores the extraneous data (which is supposed to be the contents of the Numeric
		/// element), so the only way we could notice this problem would be to write very
		/// specific code to detect it or have a DTD that would declare it to be illegal.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_NumericElementClosedTooEarly()
		{
			LanguageDefinition langDef = new LanguageDefinition();
			langDef.LocaleName = "xmum";
			langDef.LocaleScript = "IPA";
			langDef.LocaleCountry = "usa";
			langDef.LocaleVariant = "south";
			langDef.ValidChars = ksXmlHeader + "<ValidCharacters><WordForming>e\uFFFCf\uFFFCg\uFFFCh</WordForming>" +
				"<Numeric/>4\uFFFC5" +
				"<Other>,\uFFFC!\uFFFC*</Other>" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(langDef);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(4, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("e"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("f"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("g"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("h"));
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(3, validCharsW.OtherCharacters.Count);
			Assert.IsTrue(validCharsW.OtherCharacters.Contains(","));
			Assert.IsTrue(validCharsW.OtherCharacters.Contains("!"));
			Assert.IsTrue(validCharsW.OtherCharacters.Contains("*"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string with a bogus format, e.g., missing
		/// the closing tag for one of the elements (TE-8304).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_BogusFormat()
		{
			LanguageDefinition langDef = new LanguageDefinition();
			langDef.LocaleName = "xmum";
			langDef.LocaleScript = "IPA";
			langDef.LocaleCountry = "usa";
			langDef.LocaleVariant = "south";
			langDef.ValidChars = ksXmlHeader + "<ValidCharacters><WordForming>e\uFFFCf\uFFFCg\uFFFCh" +
				"</WordForming>" +
				"<Numeric>4\uFFFC5" +
				"<Other>,\uFFFC!\uFFFC*</Other>" +
				"</ValidCharacters>";

			ValidCharacters validChars = ValidCharacters.Load(langDef);
			VerifyDefaultWordFormingCharacters(validChars);

			Exception e = (Exception)ReflectionHelper.GetField(typeof(ValidCharacters), "s_lastException");

			Assert.AreEqual("Invalid ValidChars field while loading the xmum (IPA, usa, south) writing system:" +
				Environment.NewLine + "\t" + langDef.ValidChars +
				Environment.NewLine + "Parameter name: xmlSrc",
				e.Message);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string with only bogus characters (actually
		/// only one) (LT-9985).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_SingleBogusCharacter()
		{
			LanguageDefinition langDef = new LanguageDefinition();
			langDef.LocaleName = "xmum";
			langDef.LocaleScript = "IPA";
			langDef.LocaleCountry = "usa";
			langDef.LocaleVariant = "south";
			langDef.ValidChars = ksXmlHeader + "<ValidCharacters><WordForming>\u05F6</WordForming>" +
				"<Numeric></Numeric>" +
				"<Other></Other>" +
				"</ValidCharacters>";

			ValidCharacters validChars = ValidCharacters.Load(langDef);
			VerifyDefaultWordFormingCharacters(validChars);
			Exception e = (Exception)ReflectionHelper.GetField(typeof(ValidCharacters), "s_lastException");

			Assert.AreEqual("Invalid ValidChars field while loading the xmum (IPA, usa, south) writing system. " +
				"The following characters are invalid:" +
				Environment.NewLine + "\t\u05F6 (U+05F6)" +
				Environment.NewLine + "Parameter name: xmlSrc",
				e.Message);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string with a bogus character that consists
		/// of a base character and a diacritic (TE-8380).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_SingleCompoundBogusCharacter()
		{
			LanguageDefinition langDef = new LanguageDefinition();
			langDef.LocaleName = "xmum";
			langDef.LocaleScript = "IPA";
			langDef.LocaleCountry = "usa";
			langDef.LocaleVariant = "south";
			langDef.ValidChars = ksXmlHeader + "<ValidCharacters><WordForming>\u200c\u0301</WordForming>" +
				"<Numeric></Numeric>" +
				"<Other></Other>" +
				"</ValidCharacters>";

			ValidCharacters validChars = ValidCharacters.Load(langDef);
			VerifyDefaultWordFormingCharacters(validChars);
			Exception e = (Exception)ReflectionHelper.GetField(typeof(ValidCharacters), "s_lastException");

			Assert.AreEqual("Invalid ValidChars field while loading the xmum (IPA, usa, south) writing system. " +
				"The following characters are invalid:" +
				Environment.NewLine + "\t\u200c\u0301 (U+200C, U+0301)" +
				Environment.NewLine + "Parameter name: xmlSrc",
				e.Message);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string with a mix of valid and bogus
		/// characters (TE-8322).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_ValidAndBogusCharacters()
		{
			LanguageDefinition langDef = new LanguageDefinition();
			langDef.LocaleName = "xmum";
			langDef.LocaleScript = "IPA";
			langDef.LocaleCountry = "usa";
			langDef.LocaleVariant = "south";
			langDef.ValidChars = ksXmlHeader + "<ValidCharacters><WordForming>\u05F6\uFFFCg\uFFFC\u05F7\uFFFCh</WordForming>" +
				"<Numeric>1</Numeric>" +
				"<Other></Other>" +
				"</ValidCharacters>";

			ValidCharacters validChars = ValidCharacters.Load(langDef);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(2, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("g"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("h"));
			Assert.AreEqual(1, validCharsW.NumericCharacters.Count);
			Assert.IsTrue(validCharsW.NumericCharacters.Contains("1"));
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);

			Exception e = (Exception)ReflectionHelper.GetField(typeof(ValidCharacters), "s_lastException");

			Assert.AreEqual("Invalid ValidChars field while loading the xmum (IPA, usa, south) writing system. " +
				"The following characters are invalid:" +
				Environment.NewLine + "\t\u05F6 (U+05F6)" +
				Environment.NewLine + "\t\u05F7 (U+05F7)" +
				Environment.NewLine + "Parameter name: xmlSrc",
				e.Message);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string where the same character occurs in
		/// both the word-forming and punctuation XML lists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_SameCharacterInWordFormingAndPunctuationXMLLists()
		{
			string sXml = ksXmlHeader + "<ValidCharacters><WordForming>'</WordForming>" +
				"<Numeric></Numeric>" +
				"<Other>'</Other>" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(sXml, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(1, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("'"));
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string where the same character occurs in
		/// both the word-forming and numeric XML lists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_SameCharacterInWordFormingAndNumbericXMLLists()
		{
			string sXml = ksXmlHeader + "<ValidCharacters><WordForming>1</WordForming>" +
				"<Numeric>1</Numeric>" +
				"<Other></Other>" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(sXml, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(1, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("1"));
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string where the same character occurs in
		/// both the numeric and punctuation XML lists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_SameCharacterInNumericAndPunctuationXMLLists()
		{
			string sXml = ksXmlHeader + "<ValidCharacters><WordForming></WordForming>" +
				"<Numeric>1</Numeric>" +
				"<Other>1</Other>" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(sXml, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(0, validCharsW.WordFormingCharacters.Count);
			Assert.AreEqual(1, validCharsW.NumericCharacters.Count);
			Assert.IsTrue(validCharsW.NumericCharacters.Contains("1"));
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a new-style XML string where the same character occurs
		/// more than once in the same list.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromXml_DuplicateCharacters()
		{
			string sXml = ksXmlHeader + "<ValidCharacters><WordForming>a\uFFFCa</WordForming>" +
				"<Numeric>4\uFFFC4</Numeric>" +
				"<Other>'\uFFFC'</Other>" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(sXml, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(1, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("a"));
			Assert.AreEqual(1, validCharsW.NumericCharacters.Count);
			Assert.IsTrue(validCharsW.NumericCharacters.Contains("4"));
			Assert.AreEqual(1, validCharsW.OtherCharacters.Count);
			Assert.IsTrue(validCharsW.OtherCharacters.Contains("'"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests initialization from a null string.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void TestInitializeFromNullString()
		{
			ValidCharacters validChars = ValidCharacters.Load(string.Empty, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(0, validCharsW.WordFormingCharacters.Count);
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
			Assert.AreEqual(ksXmlHeader +
				"<ValidCharacters><WordForming /><Numeric /><Other /></ValidCharacters>",
				validChars.XmlString.Replace(Environment.NewLine, string.Empty).Replace(">  <", "><"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddCharacter method when attempting to add a duplicate character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddCharacter_Duplicate()
		{
			ValidCharacters validChars = ValidCharacters.Load(string.Empty, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			validChars.AddCharacter("a");
			validChars.AddCharacter("a");
			Assert.AreEqual(1, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("a"));
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
			Assert.AreEqual(ksXmlHeader +
				"<ValidCharacters><WordForming>a</WordForming>" +
				"<Numeric /><Other /></ValidCharacters>",
				validChars.XmlString.Replace(Environment.NewLine, string.Empty).Replace(">  <", "><"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddCharacter method when attempting to add a punctuation character which
		/// is already in the list of word-forming characters (as an override).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddCharacter_DuplicateOfOverriddenWordFormingChar()
		{
			string sXml = ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFC-</WordForming>" +
				"<Numeric/>" +
				"<Other>{</Other>" +
				"</ValidCharacters>";
			ValidCharacters validChars = ValidCharacters.Load(sXml, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(2, validCharsW.WordFormingCharacters.Count);
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(1, validCharsW.OtherCharacters.Count);
			Assert.IsTrue(validChars.IsWordForming("-"));
			Assert.IsFalse(validChars.IsWordForming("{"));
			validChars.AddCharacter("-");
			Assert.AreEqual(2, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("a"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("-"));
			Assert.IsTrue(validChars.IsWordForming("-"));
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(1, validCharsW.OtherCharacters.Count);
			Assert.IsTrue(validCharsW.OtherCharacters.Contains("{"));
			Assert.IsFalse(validChars.IsWordForming("{"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddCharacter method when adding a superscripted numeric character (i.e., a
		/// word-forming tone mark that ICU doesn't normally consider to be a letter). TE-8384
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddCharacter_SuperscriptedToneNumber()
		{
			ValidCharacters validChars = ValidCharacters.Load(string.Empty, "Test WS", null);
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			validChars.AddCharacter("\u00b9");
			validChars.AddCharacter("\u2079");
			Assert.AreEqual(2, validCharsW.WordFormingCharacters.Count);
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("\u00b9"));
			Assert.IsTrue(validCharsW.WordFormingCharacters.Contains("\u2079"));
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetNaturalCharType method.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetNaturalCharType()
		{
			ValidCharacters validChars = ValidCharacters.Load(string.Empty, "Test WS", null);
			DummyCharPropEngine cpe = new DummyCharPropEngine();
			ReflectionHelper.SetField(validChars, "m_cpe", cpe);
			Assert.AreEqual(ValidCharacterType.WordForming,
				ReflectionHelper.GetResult(validChars, "GetNaturalCharType", (int)'a'));
			Assert.AreEqual(ValidCharacterType.WordForming,
				ReflectionHelper.GetResult(validChars, "GetNaturalCharType", 0x00B2));
			Assert.AreEqual(ValidCharacterType.WordForming,
				ReflectionHelper.GetResult(validChars, "GetNaturalCharType", 0x2079));
			Assert.AreEqual(ValidCharacterType.Numeric,
				ReflectionHelper.GetResult(validChars, "GetNaturalCharType", (int)'1'));
			Assert.AreEqual(ValidCharacterType.Other,
				ReflectionHelper.GetResult(validChars, "GetNaturalCharType", (int)' '));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsWordForming method when using a symbol not defined as word forming in ICU
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsWordFormingChar()
		{
			ValidCharacters validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc\uFFFCd\uFFFCe\uFFFC#</WordForming>" +
				"<Numeric></Numeric>" +
				"<Other></Other>" +
				"</ValidCharacters>", "Test WS", null);

			Assert.IsTrue(validChars.IsWordForming('#'));
			Assert.IsTrue(validChars.IsWordForming("#"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that lists are sorted after adding characters one-at-a-time.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SortAfterAddSingles()
		{
			ValidCharacters validChars = ValidCharacters.Load(string.Empty, "Test WS", null);

			try
			{
				validChars.InitSortComparer("en");
				validChars.AddCharacter("z");
				validChars.AddCharacter("c");
				validChars.AddCharacter("t");
				validChars.AddCharacter("b");
				validChars.AddCharacter("8");
				validChars.AddCharacter("7");
				validChars.AddCharacter("6");
				validChars.AddCharacter("5");
				VerifySortOrder(validChars);
			}
			finally
			{
				validChars.Dispose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that lists are sorted after adding a range of characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SortAfterAddRange()
		{
			ValidCharacters validChars = ValidCharacters.Load(string.Empty, "Test WS", null);

			try
			{
				validChars.InitSortComparer("en");
				List<string> list =
					new List<string>(new string[] { "z", "c", "t", "b", "8", "7", "6", "5" });

				validChars.AddCharacters(list);
				VerifySortOrder(validChars);
			}
			finally
			{
				validChars.Dispose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the sort order of characters added to the specified valid characters
		/// object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifySortOrder(ValidCharacters validChars)
		{
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual("b", validCharsW.WordFormingCharacters[0]);
			Assert.AreEqual("c", validCharsW.WordFormingCharacters[1]);
			Assert.AreEqual("t", validCharsW.WordFormingCharacters[2]);
			Assert.AreEqual("z", validCharsW.WordFormingCharacters[3]);

			validChars.AddCharacter("8");
			validChars.AddCharacter("7");
			validChars.AddCharacter("6");
			validChars.AddCharacter("5");

			Assert.AreEqual("5", validCharsW.NumericCharacters[0]);
			Assert.AreEqual("6", validCharsW.NumericCharacters[1]);
			Assert.AreEqual("7", validCharsW.NumericCharacters[2]);
			Assert.AreEqual("8", validCharsW.NumericCharacters[3]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the default word forming characters.
		/// </summary>
		/// <param name="validChars">The valid chars.</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyDefaultWordFormingCharacters(ValidCharacters validChars)
		{
			string[] expectedWordFormingChars = (string[])ReflectionHelper.GetField(
				typeof(ValidCharacters), "s_defaultWordformingChars");
			ValidCharsWrapper validCharsW = new ValidCharsWrapper(validChars);
			Assert.AreEqual(expectedWordFormingChars, validCharsW.WordFormingCharacters.ToArray(),
				"We expect the load method to have a fallback to the default word-forming characters");
			Assert.AreEqual(0, validCharsW.NumericCharacters.Count);
			Assert.AreEqual(0, validCharsW.OtherCharacters.Count);
		}
	}
}
