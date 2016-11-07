// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TsStringSerializerTests.cs
// Responsibility: TE Team

using System;
using System.Text;
using System.Xml.Schema;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TsStringSerializerTests : TsSerializerTestsBase
	{
		private TssAssertClass TssAssert { get; set; }

		[SetUp]
		public void Setup()
		{
			TssAssert = new TssAssertClass();
		}

		#region SerializeTsStringToXml tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with only a single run that contains
		/// a writing system.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_Simple()
		{
			ITsString tss = TsStringUtils.MakeString("This is a test!", EnWS);
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo("<Str><Run ws=\"en\">This is a test!</Run></Str>"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with a MultiString. This should
		/// be handled the same way as a Str element.
		/// /// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_MultiStringSameAsStr()
		{
			ITsString tss = TsStringUtils.MakeString("This is a test!", EnWS);
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager, EnWS);
			Assert.That(StripNewLines(xml), Is.EqualTo("<AStr ws=\"en\"><Run ws=\"en\">This is a test!</Run></AStr>"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with a MultiString. This should
		/// be handled the same way as a Str element.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_MultiStringHandlesDifferentWS()
		{
			ITsString tss = TsStringUtils.MakeString("This is a test!", EsWS);
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager, EnWS);
			Assert.That(StripNewLines(xml), Is.EqualTo("<AStr ws=\"en\"><Run ws=\"es\">This is a test!</Run></AStr>"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with composed Unicode data. The writing
		/// of the XML should change it to composed data.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_Compose()
		{
			ITsString tss = TsStringUtils.MakeString("Laa yra la me\u0301n", EnWS);
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo("<Str><Run ws=\"en\">Laa yra la m\u00E9n</Run></Str>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with only a single run that contains a
		/// writing system and a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_WithStyle()
		{
			ITsString tss = TsStringUtils.MakeString("This is a test!", TsStringUtils.MakeProps("Monkey", EnWS));
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo("<Str><Run ws=\"en\" namedStyle=\"Monkey\">This is a test!</Run></Str>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with a MultiString with only a single
		/// run that contains a writing system and a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_MultiStringWithStyle()
		{
			ITsString tss = TsStringUtils.MakeString("This is a test!", TsStringUtils.MakeProps("Monkey", EnWS));
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager, EnWS);
			Assert.That(StripNewLines(xml), Is.EqualTo("<AStr ws=\"en\"><Run ws=\"en\" namedStyle=\"Monkey\">This is a test!</Run></AStr>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with only a single run that contains a
		/// writing system and an embedded serialized footnote (as from a copied footnote on
		/// the clipboard).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_WithEmbeddedData()
		{
			ITsPropsBldr tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tpb.SetStringValue(FwTextPropType.ktptObjData, CreateObjData(FwObjDataTypes.kodtEmbeddedObjectData, "<FN><M>a</M></FN>"));
			ITsString tss = TsStringUtils.MakeString("a", tpb.GetTextProps());
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo("<Str><Run ws=\"en\" embedded=\"&lt;FN&gt;&lt;M&gt;a&lt;/M&gt;&lt;/FN&gt;\">a</Run></Str>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with only a single run that contains a
		/// writing system and an unowned footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_WithLink()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsPropsBldr tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tpb.SetStringValue(FwTextPropType.ktptObjData, CreateObjData(FwObjDataTypes.kodtNameGuidHot, expectedGuid.ToByteArray()));
			ITsString tss = TsStringUtils.MakeString(StringUtils.kChObject.ToString(), tpb.GetTextProps());
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(string.Format("<Str><Run ws=\"en\" link=\"{0}\"></Run></Str>", expectedGuid)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml where object data shouldn't be written with
		/// only a single run that contains a writing system and an unowned footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_WithLinkDoesNotWriteObjData()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsIncStrBldr tisb = TsStringUtils.MakeIncStrBldr();
			tisb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tisb.Append("This is a link:");
			tisb.ClearProps();
			tisb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tisb.SetStringValue(FwTextPropType.ktptObjData, CreateObjData(FwObjDataTypes.kodtNameGuidHot, expectedGuid.ToByteArray()));
			tisb.Append(StringUtils.kChObject.ToString());
			ITsString tss = tisb.GetString();
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager, writeObjData: false);
			Assert.That(StripNewLines(xml), Is.EqualTo("<Str><Run ws=\"en\">This is a link:</Run></Str>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with only a single run that contains a
		/// writing system and a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_WithOwningLink()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsPropsBldr tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tpb.SetStringValue(FwTextPropType.ktptObjData, CreateObjData(FwObjDataTypes.kodtOwnNameGuidHot, expectedGuid.ToByteArray()));
			ITsString tss = TsStringUtils.MakeString(StringUtils.kChObject.ToString(), tpb.GetTextProps());
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(string.Format("<Str><Run ws=\"en\" ownlink=\"{0}\"></Run></Str>", expectedGuid)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with only a single run that contains a
		/// writing system and an external link.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_WithExternalLink()
		{
			ITsPropsBldr tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tpb.SetStringValue(FwTextPropType.ktptObjData, CreateObjData(FwObjDataTypes.kodtExternalPathName, "C:\\Idont\\exist\\here.doc"));
			ITsString tss = TsStringUtils.MakeString("document", tpb.GetTextProps());
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo("<Str><Run ws=\"en\" externalLink=\"C:\\Idont\\exist\\here.doc\">document</Run></Str>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with multiple runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_MultipleRuns()
		{
			ITsIncStrBldr tisb = TsStringUtils.MakeIncStrBldr();
			tisb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tisb.Append("This is a ");
			tisb.ClearProps();
			tisb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EsWS);
			tisb.SetStringValue(FwTextPropType.ktptNamedStyle, "Monkey");
			tisb.Append("Laa yra la m\u00E9n");
			ITsString tss = tisb.GetString();
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo("<Str><Run ws=\"en\">This is a </Run><Run ws=\"es\" namedStyle=\"Monkey\">Laa yra la m\u00E9n</Run></Str>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with a MultiString with multiple runs
		/// where later runs lack any value for props used in an earlier one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_MultiStringMultipleRuns()
		{
			ITsIncStrBldr tisb = TsStringUtils.MakeIncStrBldr();
			tisb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tisb.Append("This is a ");
			tisb.ClearProps();
			tisb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EsWS);
			tisb.SetStringValue(FwTextPropType.ktptNamedStyle, "Monkey");
			tisb.Append("Laa yra la m\u00E9n");
			ITsString tss = tisb.GetString();
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager, EnWS);
			Assert.That(StripNewLines(xml), Is.EqualTo("<AStr ws=\"en\"><Run ws=\"en\">This is a </Run><Run ws=\"es\" namedStyle=\"Monkey\">Laa yra la m\u00E9n</Run></AStr>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SerializeTsStringToXml with a MultiString with multiple runs
		/// where later runs lack any value for props used in an earlier one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializeTsStringToXml_MultiStringLaterRunsLessProps()
		{
			ITsIncStrBldr tisb = TsStringUtils.MakeIncStrBldr();
			tisb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EnWS);
			tisb.SetStringValue(FwTextPropType.ktptNamedStyle, "Monkey");
			tisb.Append("This is a ");
			tisb.ClearProps();
			tisb.SetIntValue(FwTextPropType.ktptWs, FwTextPropVar.ktpvDefault, EsWS);
			tisb.Append("Laa yra la m\u00E9n");
			ITsString tss = tisb.GetString();
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager, EnWS);
			Assert.That(StripNewLines(xml), Is.EqualTo("<AStr ws=\"en\"><Run ws=\"en\" namedStyle=\"Monkey\">This is a </Run><Run ws=\"es\">Laa yra la m\u00E9n</Run></AStr>"));
		}

		#endregion

		#region Stacked diacritics tests ported from C++
		private const string COMBINING_GRAVE_ACCENT = "\u0300";        // cc 230
		private const string COMBINING_CIRCUMFLEX_ACCENT = "\u0302";   // cc 230
		private const string COMBINING_TILDE = "\u0303";               // cc 230
		private const string COMBINING_MACRON = "\u0304";              // cc 230
		private const string COMBINING_DOT_ABOVE = "\u0307";           // cc 230
		private const string COMBINING_DIAERESIS = "\u0308";           // cc 230
		private const string COMBINING_DOUBLE_ACUTE_ACCENT = "\u030B"; // cc 230
		private const string COMBINING_INVERTED_BREVE = "\u0311";      // cc 230
		private const string COMBINING_OVERLINE = "\u0305";            // not involved in any compositions with characters; cc 230
		private const string COMBINING_GRAVE_ACCENT_BELOW = "\u0316";  // cc 220
		private const string COMBINING_ACUTE_ACCENT_BELOW = "\u0317";  // cc 220
		private const string COMBINING_LEFT_TACK_BELOW = "\u0318";     // cc 220
		private const string COMBINING_DOWN_TACK_BELOW = "\u031E";     // cc 220
		private const string COMBINING_MINUS_SIGN_BELOW = "\u0320";    // cc 220
		private const string COMBINING_RING_BELOW = "\u0325";          // cc 220
		private const string COMBINING_TILDE_BELOW = "\u0330";         // cc 220
		private const string COMBINING_SQUARE_BELOW = "\u033B";        // cc 220
		private const string COMBINING_SEAGULL_BELOW = "\u033C";       // cc 220
		private const string a_WITH_DOT_ABOVE  = "\u0227";             // composition of a + COMBINING_DOT_ABOVE
		private const string o_WITH_DOT_ABOVE  = "\u022F";             // composition of o + COMBINING_DOT_ABOVE
		private const string o_WITH_CIRCUMFLEX = "\u00F4";             // composition of o + COMBINING_CIRCUMFLEX_ACCENT
		private const string o_WITH_DIAERESIS  = "\u00F6";             // composition of o + COMBINING_DIAERESIS
		private const string e_WITH_GRAVE = "\u00E8";                  // composition of e + COMBINING_GRAVE_ACCENT

		private TsString CreateStackedDiacriticsInput(bool singleRun)
		{
			string stackedDiacriticsInput = "Stacked diacritics: W" +
				"e" + COMBINING_DOUBLE_ACUTE_ACCENT + COMBINING_RING_BELOW + COMBINING_GRAVE_ACCENT_BELOW +
				"lc" + COMBINING_LEFT_TACK_BELOW + COMBINING_MINUS_SIGN_BELOW +
				"o" + COMBINING_CIRCUMFLEX_ACCENT +
				"m" + COMBINING_SEAGULL_BELOW + COMBINING_GRAVE_ACCENT + COMBINING_DIAERESIS + COMBINING_MACRON +
				"e" + COMBINING_GRAVE_ACCENT +
				" to" + COMBINING_DIAERESIS + COMBINING_CIRCUMFLEX_ACCENT +
				" Wo" + COMBINING_DOT_ABOVE + COMBINING_INVERTED_BREVE +
				"r" + COMBINING_SQUARE_BELOW +
				"l" + COMBINING_TILDE +
				"d" + COMBINING_DOWN_TACK_BELOW + COMBINING_TILDE_BELOW +
				"Pa" + COMBINING_DOT_ABOVE + COMBINING_OVERLINE + COMBINING_DOUBLE_ACUTE_ACCENT +
				"d" + COMBINING_ACUTE_ACCENT_BELOW +
				"!";

			var temp = new TsString(stackedDiacriticsInput, EnWS);
			var builder = new TsStrBldr();
			builder.ReplaceTsString(0, 0, temp);
			builder.SetIntPropValues(0, stackedDiacriticsInput.Length, (int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 20000);
			builder.SetIntPropValues(0, stackedDiacriticsInput.Length, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrGreen);
			if (singleRun)
				return (TsString)builder.GetString();

			// green from 0-22
			builder.SetIntPropValues(22, 23, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			builder.SetIntPropValues(23, 24, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, 0x00ff602f);
			// green from 24-30
			builder.SetIntPropValues(30, 31, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlue);
			// green from 31-33
			builder.SetIntPropValues(33, 34, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			// green from 34-42
			builder.SetIntPropValues(42, 43, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlack);
			// green from 43-47
			builder.SetIntPropValues(47, 48, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlack);
			// green from 48-51
			builder.SetIntPropValues(51, 52, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlue);
			// green from 52-53
			builder.SetIntPropValues(53, 54, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			// green from 54-58
			builder.SetIntPropValues(58, 59, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			builder.SetIntPropValues(59, 60, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlack);
			// green from 60-61
			builder.SetIntPropValues(61, 62, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlack);
			// green from 62-63

			return (TsString)builder.GetString();
		}


		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test that strings with stacked diacritics in a single run serialize properly.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Serialization_StackedDiacriticsSingleRun_ProducesCorrectXML()
		{
			string expectedXML = "<Str>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">" +
				"Stacked diacritics: W" +
				"e" + COMBINING_RING_BELOW + COMBINING_GRAVE_ACCENT_BELOW + COMBINING_DOUBLE_ACUTE_ACCENT +
				"lc" + COMBINING_LEFT_TACK_BELOW + COMBINING_MINUS_SIGN_BELOW +
				o_WITH_CIRCUMFLEX +
				"m" + COMBINING_SEAGULL_BELOW + COMBINING_GRAVE_ACCENT + COMBINING_DIAERESIS + COMBINING_MACRON +
				e_WITH_GRAVE +
				" t" + o_WITH_DIAERESIS + COMBINING_CIRCUMFLEX_ACCENT +
				" W" + o_WITH_DOT_ABOVE + COMBINING_INVERTED_BREVE +
				"r" + COMBINING_SQUARE_BELOW +
				"l" + COMBINING_TILDE +
				"d" + COMBINING_DOWN_TACK_BELOW + COMBINING_TILDE_BELOW +
				"P" + a_WITH_DOT_ABOVE + COMBINING_OVERLINE + COMBINING_DOUBLE_ACUTE_ACCENT +
				"d" + COMBINING_ACUTE_ACCENT_BELOW +
				"!" +
				"</Run>" +
				"</Str>";

			TsString tss = CreateStackedDiacriticsInput(singleRun: true);
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(StripNewLines(expectedXML)));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test that strings with stacked diacritics in multiple runs serialize properly.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Serialization_StackedDiacriticsMultipleRuns_ProducesCorrectXML()
		{
			string expectedXML = "<Str>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">Stacked diacritics: We</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"2f60ff\">" + COMBINING_RING_BELOW + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">" + COMBINING_GRAVE_ACCENT_BELOW + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"red\">" + COMBINING_DOUBLE_ACUTE_ACCENT + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">lc" + COMBINING_LEFT_TACK_BELOW + COMBINING_MINUS_SIGN_BELOW + "o</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"blue\">" + COMBINING_CIRCUMFLEX_ACCENT + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">m" + COMBINING_SEAGULL_BELOW + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"red\">" + COMBINING_GRAVE_ACCENT + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">" + COMBINING_DIAERESIS + COMBINING_MACRON + e_WITH_GRAVE + " t" + o_WITH_DIAERESIS + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"black\">" + COMBINING_CIRCUMFLEX_ACCENT + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\"> W" + o_WITH_DOT_ABOVE + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"black\">" + COMBINING_INVERTED_BREVE + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">r" + COMBINING_SQUARE_BELOW + "l</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"blue\">" + COMBINING_TILDE + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">d</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"red\">" + COMBINING_DOWN_TACK_BELOW + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">" + COMBINING_TILDE_BELOW + "P" + a_WITH_DOT_ABOVE + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"red\">" + COMBINING_OVERLINE + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"black\">" + COMBINING_DOUBLE_ACUTE_ACCENT + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">d</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"black\">" + COMBINING_ACUTE_ACCENT_BELOW + "</Run>" +
				"<Run ws=\"en\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">!</Run>" +
				"</Str>";

			TsString tss = CreateStackedDiacriticsInput(singleRun: false);
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(expectedXML));
		}

		#endregion

		#region DeserializeTsStringFromXml tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with only a single run that contains
		/// a writing system.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_Simple()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<Str><Run ws='en'>This is a test!</Run></Str>", WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.NoStrProps(tss);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with a MultiString. This should
		/// be handled the same way as a Str element
		/// /// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_MultiStringSameAsStr()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<AStr ws='en'><Run ws='en'>This is a test!</Run></AStr>", WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.NoStrProps(tss);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with a MultiString. This should
		/// be handled the same way as a Str element
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_MultiStringHandlesDifferentWS()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<AStr ws='en'><Run ws='es'>This is a test!</Run></AStr>", WritingSystemManager);

			Assert.IsNotNull(tss);
			Assert.AreEqual(1, tss.RunCount);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(EsWS, tss);
			TssAssert.NoStrProps(tss);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with a MultiString that contains
		/// no run elements.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_MultiStringWithNoRunElement()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<AStr ws='en'>This is a test!</AStr>", WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.NoStrProps(tss);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with composed Unicode data. The reading
		/// of the XML should change it to decomposed data. (FWR-148)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_Decompose()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<Str><Run ws=\"en\">Laa yra la m\u00E9n</Run></Str>", WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("Laa yra la me\u0301n", tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.NoStrProps(tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with only a single run that contains a
		/// writing system and a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_WithStyle()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<Str><Run ws='en' namedStyle='Monkey'>This is a test!</Run></Str>", WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.StyleEqual("Monkey", tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with a MultiString with only a single
		/// run that contains a writing system and a style. Jira issue FWR-902.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_MultiStringWithStyle()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<AStr ws='en'><Run ws='en' namedStyle='Monkey'>This is a test!</Run></AStr>",
				WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.StyleEqual("Monkey", tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with a String with only a single
		/// run that contains no writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(ExpectedException = typeof(XmlSchemaException),
			ExpectedMessage = "Run element must contain a ws attribute. Run text: This is a test!")]
		public void DeserializeTsStringFromXml_NoWsInRun()
		{
			TsStringSerializer.DeserializeTsStringFromXml(
				"<Str><Run>This is a test!</Run></Str>", WritingSystemManager);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with a MultiString with only a single
		/// run that contains a style but no writing system. FW will currently never write out
		/// an element like this, but it is the sort of thing a well-meaning techie could do
		/// when mucking with the data manually.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(ExpectedException = typeof(XmlSchemaException),
			ExpectedMessage = "Run element must contain a ws attribute. Run text: This is a test!")]
		public void DeserializeTsStringFromXml_MultiStringWithStyleAndNoWsInRun()
		{
			TsStringSerializer.DeserializeTsStringFromXml(
				"<AStr ws='en'><Run namedStyle='Monkey'>This is a test!</Run></AStr>",
				WritingSystemManager);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with only a single run that contains a
		/// writing system and an embedded serialized footnote (as from a copied footnote on
		/// the clipboard).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_WithEmbeddedData()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<Str><Run ws='en' embedded='&lt;FN&gt;&lt;M&gt;a&lt;/M&gt;&lt;/FN&gt;'>a</Run></Str>",
				WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("a", tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.ObjDataEqual((char)FwObjDataTypes.kodtEmbeddedObjectData + "<FN><M>a</M></FN>", tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with only a single run that contains a
		/// writing system and an unowned footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_WithLink()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<Str><Run ws='en' link='" + expectedGuid + "'></Run></Str>",
				WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual(StringUtils.kChObject.ToString(), tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.FootnoteEqual(expectedGuid, FwObjDataTypes.kodtNameGuidHot, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with only a single run that contains a
		/// writing system and a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_WithOwningLink()
		{
			Guid expectedGuid = Guid.NewGuid();
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<Str><Run ws='en' ownlink='" + expectedGuid + "'></Run></Str>",
				WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual(StringUtils.kChObject.ToString(), tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.FootnoteEqual(expectedGuid, FwObjDataTypes.kodtOwnNameGuidHot, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with only a single run that contains a
		/// writing system and an external link.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_WithExternalLink()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				@"<Str><Run ws='en' externalLink='C:\Idont\exist\here.doc'>document</Run></Str>",
				WritingSystemManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("document", tss);
			TssAssert.WsEqual(EnWS, tss);
			TssAssert.ObjDataEqual((char)FwObjDataTypes.kodtExternalPathName + @"C:\Idont\exist\here.doc", tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with multiple runs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_MultipleRuns()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<Str><Run ws='en'>This is a </Run><Run ws='es' namedStyle='Monkey'>Laa yra la m\u00E9n</Run></Str>",
				WritingSystemManager);

			Assert.IsNotNull(tss);
			Assert.AreEqual(2, tss.RunCount);
			// Check run one
			TssAssert.RunEqual("This is a ", tss, 0, 0);
			TssAssert.WsEqual(EnWS, tss, 0);
			TssAssert.NoStrProps(tss, 0);
			// Check run two
			TssAssert.RunEqual("Laa yra la me\u0301n", tss, 1, 10);
			TssAssert.WsEqual(EsWS, tss, 1);
			TssAssert.StyleEqual("Monkey", tss, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with a MultiString with multiple runs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_MultiStringMultipleRuns()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<AStr><Run ws='en'>This is a </Run><Run ws='es' namedStyle='Monkey'>Laa yra la m\u00E9n</Run></AStr>",
				WritingSystemManager);

			Assert.IsNotNull(tss);
			Assert.AreEqual(2, tss.RunCount);
			Assert.AreEqual("This is a Laa yra la me\u0301n", tss.Text);
			// Check run one
			TssAssert.RunEqual("This is a ", tss, 0, 0);
			TssAssert.WsEqual(EnWS, tss, 0);
			TssAssert.NoStrProps(tss, 0);
			// Check run two
			TssAssert.RunEqual("Laa yra la me\u0301n", tss, 1, 10);
			TssAssert.WsEqual(EsWS, tss, 1);
			TssAssert.StyleEqual("Monkey", tss, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeserializeTsStringFromXml with a MultiString with multiple runs
		/// where later runs lack any value for props used in an earlier one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializeTsStringFromXml_MultiStringLaterRunsLessProps()
		{
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<AStr><Run ws='en' namedStyle='Monkey'>This is a </Run><Run ws='es'>Laa yra la m\u00E9n</Run></AStr>",
				WritingSystemManager);

			Assert.IsNotNull(tss);
			Assert.AreEqual(2, tss.RunCount);
			Assert.AreEqual("This is a Laa yra la me\u0301n", tss.Text);
			// Check run one
			TssAssert.RunEqual("This is a ", tss, 0, 0);
			TssAssert.WsEqual(EnWS, tss, 0);
			TssAssert.StyleEqual("Monkey", tss, 0);
			// Check run two
			TssAssert.RunEqual("Laa yra la me\u0301n", tss, 1, 10);
			TssAssert.WsEqual(EsWS, tss, 1);
			TssAssert.NoStrProps(tss, 1);
		}
		#endregion

		#region TssAssert class
		private class TssAssertClass
		{
			private ITsTextProps m_props;
			// the ITsString and run that was used to retrieve the props
			private ITsString m_tssForProps;
			private int m_iRun;

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Gets the properties from the TsString. Since this is a COM call we cache the
			/// properties and if we get called again for the same run we re-use them.
			/// </summary>
			///----------------------------------------------------------------------------------
			private ITsTextProps GetProperties(ITsString tss, int iRun)
			{
				if (m_tssForProps == tss && m_props != null && iRun == m_iRun)
					return m_props;

				m_tssForProps = tss;
				m_iRun = iRun;
				m_props = tss.get_Properties(iRun);
				return m_props;
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the run is equal to the expected TsString.
			/// </summary>
			///----------------------------------------------------------------------------------
			public void RunEqual(string expectedText, ITsString tss, int iRun, int iMin)
			{
				var runText = tss.get_RunText(iRun);
				Assert.AreEqual(expectedText, runText);
				Assert.IsTrue(runText.IsNormalized(NormalizationForm.FormD));
				Assert.AreEqual(iMin, tss.get_MinOfRun(iRun));
				Assert.AreEqual(iMin + expectedText.Length, tss.get_LimOfRun(iRun));
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the string is equal to the expected TsString and contains only one
			/// run.
			/// </summary>
			///----------------------------------------------------------------------------------
			public void StringEqual(string expectedText, ITsString tss)
			{
				Assert.AreEqual(1, tss.RunCount);
				RunEqual(expectedText, tss, 0, 0);
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the writing system is equal to the expected WS.
			/// </summary>
			///----------------------------------------------------------------------------------
			public void WsEqual(int expectedWs, ITsString tss)
			{
				WsEqual(expectedWs, tss, 0);
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the writing system is equal to the expected WS for the specified run.
			/// </summary>
			///----------------------------------------------------------------------------------
			public void WsEqual(int expectedWs, ITsString tss, int iRun)
			{
				ITsTextProps props = GetProperties(tss, iRun);
				Assert.AreEqual(1, props.IntPropCount);
				int var;
				Assert.AreEqual(expectedWs, props.GetIntPropValues((int)FwTextPropType.ktptWs, out var));
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the named style is equal to the expected style.
			/// </summary>
			///----------------------------------------------------------------------------------
			public void StyleEqual(string expectedStyle, ITsString tss)
			{
				StyleEqual(expectedStyle, tss, 0);
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the named style is equal to the expected style.
			/// </summary>
			///----------------------------------------------------------------------------------
			public void StyleEqual(string expectedStyle, ITsString tss, int iRun)
			{
				ITsTextProps props = GetProperties(tss, iRun);
				Assert.AreEqual(1, props.StrPropCount);
				int var;
				Assert.AreEqual(expectedStyle, props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the TsString doesn't have any string props assigned
			/// </summary>
			///----------------------------------------------------------------------------------
			public void NoStrProps(ITsString tss)
			{
				NoStrProps(tss, 0);
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the TsString doesn't have any string props assigned
			/// </summary>
			///----------------------------------------------------------------------------------
			public void NoStrProps(ITsString tss, int iRun)
			{
				ITsTextProps props = GetProperties(tss, iRun);
				Assert.AreEqual(0, props.StrPropCount);
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the footnote is as expected
			/// </summary>
			///----------------------------------------------------------------------------------
			public void FootnoteEqual(Guid expectedGuid, FwObjDataTypes expectedType, ITsString tss)
			{
				ITsTextProps props = GetProperties(tss, 0);
				Assert.AreEqual(1, props.StrPropCount);
				FwObjDataTypes type;
				Guid guid = TsStringUtils.GetGuidFromProps(props, null, out type);
				Assert.AreEqual(expectedGuid, guid);
				Assert.AreEqual(expectedType, type);
			}

			///----------------------------------------------------------------------------------
			/// <summary>
			/// Asserts that the object data is as expected
			/// </summary>
			///----------------------------------------------------------------------------------
			public void ObjDataEqual(string expectedObjData, ITsString tss)
			{
				ITsTextProps props = GetProperties(tss, 0);
				Assert.AreEqual(1, props.StrPropCount);
				Assert.AreEqual(expectedObjData,
					props.GetStrPropValue((int)FwTextPropType.ktptObjData));
			}
		}
		#endregion
	}
}
