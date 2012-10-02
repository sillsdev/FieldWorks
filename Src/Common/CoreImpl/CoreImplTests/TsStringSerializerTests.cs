// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TsStringSerializerTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Xml.Schema;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TsStringSerializerTests : FwCOMTestBase
		// can't derive from BaseTest, but instantiate DebugProcs instead
	{
		#region Member Variables
		private int m_enWs;
		private int m_esWs;
		private DebugProcs m_DebugProcs;
		private IWritingSystemManager m_wsManager;
		private TssAssertClass TssAssert { get; set; }
		#endregion

		#region Test Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_DebugProcs = new DebugProcs();
			RegistryHelper.CompanyName = "SIL";

			Icu.InitIcuDataDir();
			m_wsManager = new PalasoWritingSystemManager();

			IWritingSystem enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_enWs = enWs.Handle;

			IWritingSystem esWs;
			m_wsManager.GetOrSet("es", out esWs);
			m_esWs = esWs.Handle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up the writing system factory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			m_DebugProcs.Dispose();
			m_DebugProcs = null;
		}

		[SetUp]
		public void Setup()
		{
			TssAssert = new TssAssertClass();
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
				"<Str><Run ws='en'>This is a test!</Run></Str>", m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				"<AStr ws='en'><Run ws='en'>This is a test!</Run></AStr>", m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				"<AStr ws='en'><Run ws='es'>This is a test!</Run></AStr>", m_wsManager);

			Assert.IsNotNull(tss);
			Assert.AreEqual(1, tss.RunCount);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(m_esWs, tss);
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
				"<AStr ws='en'>This is a test!</AStr>", m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				"<Str><Run ws=\"en\">Laa yra la m\u00E9n</Run></Str>", m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("Laa yra la me\u0301n", tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				"<Str><Run ws='en' namedStyle='Monkey'>This is a test!</Run></Str>", m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("This is a test!", tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				"<Str><Run>This is a test!</Run></Str>", m_wsManager);
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
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(
				"<AStr ws='en'><Run namedStyle='Monkey'>This is a test!</Run></AStr>",
				m_wsManager);

			// ENHANCE: Allow this case to succeed by getting the writing system of the run
			// from the AStr element.
			//Assert.IsNotNull(tss);
			//TssAssert.RunEqual("This is a test!", tss);
			//TssAssert.WsEqual(m_enWs, tss);
			//TssAssert.StyleEqual("Monkey", tss);
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
				m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("a", tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual(StringUtils.kChObject.ToString(), tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual(StringUtils.kChObject.ToString(), tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				m_wsManager);

			Assert.IsNotNull(tss);
			TssAssert.StringEqual("document", tss);
			TssAssert.WsEqual(m_enWs, tss);
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
				m_wsManager);

			Assert.IsNotNull(tss);
			Assert.AreEqual(2, tss.RunCount);
			// Check run one
			TssAssert.RunEqual("This is a ", tss, 0, 0);
			TssAssert.WsEqual(m_enWs, tss, 0);
			TssAssert.NoStrProps(tss, 0);
			// Check run two
			TssAssert.RunEqual("Laa yra la me\u0301n", tss, 1, 10);
			TssAssert.WsEqual(m_esWs, tss, 1);
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
				m_wsManager);

			Assert.IsNotNull(tss);
			Assert.AreEqual(2, tss.RunCount);
			Assert.AreEqual("This is a Laa yra la me\u0301n", tss.Text);
			// Check run one
			TssAssert.RunEqual("This is a ", tss, 0, 0);
			TssAssert.WsEqual(m_enWs, tss, 0);
			TssAssert.NoStrProps(tss, 0);
			// Check run two
			TssAssert.RunEqual("Laa yra la me\u0301n", tss, 1, 10);
			TssAssert.WsEqual(m_esWs, tss, 1);
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
				m_wsManager);

			Assert.IsNotNull(tss);
			Assert.AreEqual(2, tss.RunCount);
			Assert.AreEqual("This is a Laa yra la me\u0301n", tss.Text);
			// Check run one
			TssAssert.RunEqual("This is a ", tss, 0, 0);
			TssAssert.WsEqual(m_enWs, tss, 0);
			TssAssert.StyleEqual("Monkey", tss, 0);
			// Check run two
			TssAssert.RunEqual("Laa yra la me\u0301n", tss, 1, 10);
			TssAssert.WsEqual(m_esWs, tss, 1);
			TssAssert.NoStrProps(tss, 1);
		}

		#endregion

		#region DeserializePropsFromXml tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with an 'align' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Align()
		{
			CheckIntProp("align", "center", (int)FwTextPropType.ktptAlign, (int)FwTextAlign.ktalCenter, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("align", "justify", (int)FwTextPropType.ktptAlign, (int)FwTextAlign.ktalJustify, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("align", "leading", (int)FwTextPropType.ktptAlign, (int)FwTextAlign.ktalLeading, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("align", "left", (int)FwTextPropType.ktptAlign, (int)FwTextAlign.ktalLeft, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("align", "right", (int)FwTextPropType.ktptAlign, (int)FwTextAlign.ktalRight, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("align", "trailing", (int)FwTextPropType.ktptAlign, (int)FwTextAlign.ktalTrailing, (int)FwTextPropVar.ktpvEnum);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with an 'align' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Align_Fail()
		{
			CheckIntProp("align", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'backcolor' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Backcolor()
		{
			CheckIntProp("backcolor", "red", (int)FwTextPropType.ktptBackColor, 0x0000FF);
			CheckIntProp("backcolor", "white", (int)FwTextPropType.ktptBackColor, 0xFFFFFF);
			CheckIntProp("backcolor", "blue", (int)FwTextPropType.ktptBackColor, 0xFF0000);
			CheckIntProp("backcolor", "F8F105", (int)FwTextPropType.ktptBackColor, 0x05F1F8);
			CheckIntProp("backcolor", "transparent", (int)FwTextPropType.ktptBackColor,
				(int)FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'backcolor' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Backcolor_Fail()
		{
			CheckIntProp("backcolor", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bold' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Bold()
		{
			CheckIntProp("bold", "off", (int)FwTextPropType.ktptBold, (int)FwTextToggleVal.kttvOff, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("bold", "on", (int)FwTextPropType.ktptBold, (int)FwTextToggleVal.kttvForceOn, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("bold", "invert", (int)FwTextPropType.ktptBold, (int)FwTextToggleVal.kttvInvert, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bold' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Bold_Fail()
		{
			CheckIntProp("bold", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderBottom' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderBottom()
		{
			CheckIntProp("borderBottom", "0", (int)FwTextPropType.ktptBorderBottom, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("borderBottom", "12", (int)FwTextPropType.ktptBorderBottom, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderBottom' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderBottom_Fail()
		{
			CheckIntProp("borderBottom", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderColor' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderColor()
		{
			CheckIntProp("borderColor", "red", (int)FwTextPropType.ktptBorderColor, 0x0000FF);
			CheckIntProp("borderColor", "white", (int)FwTextPropType.ktptBorderColor, 0xFFFFFF);
			CheckIntProp("borderColor", "blue", (int)FwTextPropType.ktptBorderColor, 0xFF0000);
			CheckIntProp("borderColor", "F8F105", (int)FwTextPropType.ktptBorderColor, 0x05F1F8);
			CheckIntProp("borderColor", "transparent", (int)FwTextPropType.ktptBorderColor,
				(int)FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderColor' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderColor_Fail()
		{
			CheckIntProp("borderColor", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderLeading' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderLeading()
		{
			CheckIntProp("borderLeading", "0", (int)FwTextPropType.ktptBorderLeading, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("borderLeading", "12", (int)FwTextPropType.ktptBorderLeading, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderLeading' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderLeading_Fail()
		{
			CheckIntProp("borderLeading", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderTop' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderTop()
		{
			CheckIntProp("borderTop", "0", (int)FwTextPropType.ktptBorderTop, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("borderTop", "12", (int)FwTextPropType.ktptBorderTop, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderTop' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderTop_Fail()
		{
			CheckIntProp("borderTop", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderTrailing' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderTrailing()
		{
			CheckIntProp("borderTrailing", "0", (int)FwTextPropType.ktptBorderTrailing, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("borderTrailing", "12", (int)FwTextPropType.ktptBorderTrailing, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderTrailing' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderTrailing_Fail()
		{
			CheckIntProp("borderTrailing", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumFontInfo' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumFontInfo()
		{
			CheckBulFontInfoProp("<BulNumFontInfo backcolor=\"white\" bold=\"on\" fontsize=\"20mpt\"" +
				" forecolor=\"blue\" italic=\"on\" offset=\"10mpt\" superscript=\"super\" undercolor=\"red\"" +
				" underline=\"dashed\" fontFamily=\"Times New Roman\"></BulNumFontInfo>",
				(char)FwTextPropType.ktptItalic + "\u0001\u0000" +
				(char)FwTextPropType.ktptBold + "\u0001\u0000" +
				(char)FwTextPropType.ktptSuperscript + "\u0001\u0000" +
				(char)FwTextPropType.ktptUnderline + "\u0002\u0000" +
				(char)FwTextPropType.ktptFontSize + "\u0014\u0000" +
				(char)FwTextPropType.ktptOffset + "\u000A\u0000" +
				(char)FwTextPropType.ktptForeColor + "\u0000\u00FF" +
				(char)FwTextPropType.ktptBackColor + "\uFFFF\u00FF" +
				(char)FwTextPropType.ktptUnderColor + "\u00FF\u0000" +
				(char)FwTextPropType.ktptFontFamily + "Times New Roman\u0000");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumScheme' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumScheme()
		{
			CheckIntProp("bulNumScheme", "0", (int)FwTextPropType.ktptBulNumScheme, 0, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("bulNumScheme", "3", (int)FwTextPropType.ktptBulNumScheme, 3, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumScheme' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BulNumScheme_Fail()
		{
			CheckIntProp("bulNumScheme", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumStartAt' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumStartAt()
		{
			CheckIntProp("bulNumStartAt", "0", (int)FwTextPropType.ktptBulNumStartAt, 0);
			CheckIntProp("bulNumStartAt", "12", (int)FwTextPropType.ktptBulNumStartAt, 12);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumStartAt' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BulNumStartAt_Fail()
		{
			CheckIntProp("bulNumStartAt", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumTxtAft' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumTxtAft()
		{
			CheckStrProp("bulNumTxtAft", "my", (int)FwTextPropType.ktptBulNumTxtAft, "my");
			CheckStrProp("bulNumTxtAft", "TexT", (int)FwTextPropType.ktptBulNumTxtAft, "TexT");
			CheckStrProp("bulNumTxtAft", string.Empty, (int)FwTextPropType.ktptBulNumTxtAft, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumTxtBef' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumTxtBef()
		{
			CheckStrProp("bulNumTxtBef", "my", (int)FwTextPropType.ktptBulNumTxtBef, "my");
			CheckStrProp("bulNumTxtBef", "TexT", (int)FwTextPropType.ktptBulNumTxtBef, "TexT");
			CheckStrProp("bulNumTxtBef", string.Empty, (int)FwTextPropType.ktptBulNumTxtBef, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'contextString' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_ContextString()
		{
			CheckStrObjProp("contextString", FwObjDataTypes.kodtContextString, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'firstIndent' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_FirstIndent()
		{
			CheckIntProp("firstIndent", "0", (int)FwTextPropType.ktptFirstIndent, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("firstIndent", "12", (int)FwTextPropType.ktptFirstIndent, 12, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("firstIndent", "-12", (int)FwTextPropType.ktptFirstIndent, -12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontFamily' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_FontFamily()
		{
			CheckStrProp("fontFamily", "Times New Roman", (int)FwTextPropType.ktptFontFamily, "Times New Roman");
			CheckStrProp("fontFamily", "Courier", (int)FwTextPropType.ktptFontFamily, "Courier");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontsize' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Fontsize()
		{
			CheckIntProp("fontsize='5'", (int)FwTextPropType.ktptFontSize, 5, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("fontsize='10' fontsizeUnit='mpt'", (int)FwTextPropType.ktptFontSize, 10, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("fontsize='15' fontsizeUnit='rel'", (int)FwTextPropType.ktptFontSize, 15, (int)FwTextPropVar.ktpvRelative);
			CheckIntProp("fontsize='20mpt'", (int)FwTextPropType.ktptFontSize, 20, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontsize' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Fontsize_Fail1()
		{
			CheckIntProp("fontsize='-10' fontsizeUnit='mpt'", (int)FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontsize' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Fontsize_Fail2()
		{
			CheckIntProp("fontsize='10' fontsizeUnit='monkey'", (int)FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontVariations' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_FontVariations()
		{
			CheckStrProp("fontVariations", "156=1,896=2,84=21", (int)FwTextPropType.ktptFontVariations, "156=1,896=2,84=21");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'forecolor' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Forecolor()
		{
			CheckIntProp("forecolor", "red", (int)FwTextPropType.ktptForeColor, 0x0000FF);
			CheckIntProp("forecolor", "white", (int)FwTextPropType.ktptForeColor, 0xFFFFFF);
			CheckIntProp("forecolor", "blue", (int)FwTextPropType.ktptForeColor, 0xFF0000);
			CheckIntProp("forecolor", "F8F105", (int)FwTextPropType.ktptForeColor, 0x05F1F8);
			CheckIntProp("forecolor", "transparent", (int)FwTextPropType.ktptForeColor,
				(int)FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'forecolor' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Forecolor_Fail()
		{
			CheckIntProp("forecolor", "monkey", (int)FwTextPropType.ktptForeColor, 0x0000FF);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'italic' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Italic()
		{
			CheckIntProp("italic", "off", (int)FwTextPropType.ktptItalic, (int)FwTextToggleVal.kttvOff, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("italic", "on", (int)FwTextPropType.ktptItalic, (int)FwTextToggleVal.kttvForceOn, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("italic", "invert", (int)FwTextPropType.ktptItalic, (int)FwTextToggleVal.kttvInvert, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'italic' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Italic_Fail()
		{
			CheckIntProp("italic", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'keepTogether' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_KeepTogether()
		{
			CheckIntProp("keepTogether", "1", (int)FwTextPropType.ktptKeepTogether, 1, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("keepTogether", "0", (int)FwTextPropType.ktptKeepTogether, 0, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'keepWithNext' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_KeepTogether_Fail()
		{
			CheckIntProp("keepWithNext", "monkey", (int)FwTextPropType.ktptKeepTogether, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'keepWithNext' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_KeepWithNext()
		{
			CheckIntProp("keepWithNext", "1", (int)FwTextPropType.ktptKeepWithNext, 1, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("keepWithNext", "0", (int)FwTextPropType.ktptKeepWithNext, 0, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'keepWithNext' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_KeepWithNext_Fail()
		{
			CheckIntProp("keepWithNext", "monkey", (int)FwTextPropType.ktptKeepWithNext, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'leadingIndent' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_LeadingIndent()
		{
			CheckIntProp("leadingIndent", "0", (int)FwTextPropType.ktptLeadingIndent, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("leadingIndent", "12", (int)FwTextPropType.ktptLeadingIndent, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'leadingIndent' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_LeadingIndent_Fail()
		{
			CheckIntProp("leadingIndent", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'lineHeight' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_LineHeight()
		{
			CheckIntProp("lineHeight='5'", (int)FwTextPropType.ktptLineHeight, 5, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("lineHeight='10' lineHeightUnit='mpt'", (int)FwTextPropType.ktptLineHeight, 10, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("lineHeight='15' lineHeightType='exact'", (int)FwTextPropType.ktptLineHeight, -15, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("lineHeight='20' lineHeightUnit='rel'", (int)FwTextPropType.ktptLineHeight, 20, (int)FwTextPropVar.ktpvRelative);
			CheckIntProp("lineHeight='25' lineHeightUnit='rel' lineHeightType='exact'", (int)FwTextPropType.ktptLineHeight, 25, (int)FwTextPropVar.ktpvRelative);
			CheckIntProp("lineHeight='30' lineHeightType='atLeast'", (int)FwTextPropType.ktptLineHeight, 30, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'lineHeight' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_LineHeight_Fail1()
		{
			CheckIntProp("lineHeight='-10' lineHeightUnit='mpt'", (int)FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'lineHeight' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_LineHeight_Fail2()
		{
			CheckIntProp("lineHeight='10' lineHeightUnit='monkey'", (int)FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'lineHeight' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_LineHeight_Fail3()
		{
			CheckIntProp("lineHeight='10' lineHeightType='monkey'", (int)FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'link' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Link()
		{
			CheckStrObjProp("link", FwObjDataTypes.kodtNameGuidHot, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginBottom' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MarginBottom()
		{
			CheckIntProp("marginBottom", "0", (int)FwTextPropType.ktptMarginBottom, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("marginBottom", "12", (int)FwTextPropType.ktptMarginBottom, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginBottom' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_MarginBottom_Fail()
		{
			CheckIntProp("marginBottom", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginLeading' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MarginLeading()
		{
			CheckIntProp("marginLeading", "0", (int)FwTextPropType.ktptMarginLeading, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("marginLeading", "12", (int)FwTextPropType.ktptMarginLeading, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginLeading' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_MarginLeading_Fail()
		{
			CheckIntProp("marginLeading", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginTop' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MarginTop()
		{
			CheckIntProp("marginTop", "0", (int)FwTextPropType.ktptMarginTop, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("marginTop", "12", (int)FwTextPropType.ktptMarginTop, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginTop' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_MarginTop_Fail()
		{
			CheckIntProp("marginTop", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginTrailing' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MarginTrailing()
		{
			CheckIntProp("marginTrailing", "0", (int)FwTextPropType.ktptMarginTrailing, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("marginTrailing", "12", (int)FwTextPropType.ktptMarginTrailing, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginTrailing' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_MarginTrailing_Fail()
		{
			CheckIntProp("marginTrailing", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'moveableObj' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MoveableObj()
		{
			CheckStrObjProp("moveableObj", FwObjDataTypes.kodtGuidMoveableObjDisp, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with an 'offset' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Offset()
		{
			CheckIntProp("offset='5'", (int)FwTextPropType.ktptOffset, 5, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("offset='10' offsetUnit='mpt'", (int)FwTextPropType.ktptOffset, 10, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("offset='15' offsetUnit='rel'", (int)FwTextPropType.ktptOffset, 15, (int)FwTextPropVar.ktpvRelative);
			CheckIntProp("offset='20mpt'", (int)FwTextPropType.ktptOffset, 20, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'offset' attribute with a negative
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_NegativeOffset()
		{
			CheckIntProp("offset='-10' offsetUnit='mpt'", (int)FwTextPropType.ktptOffset, -10, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'offset' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Offset_Fail2()
		{
			CheckIntProp("offset='10' offsetUnit='monkey'", (int)FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'ownlink' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Ownlink()
		{
			CheckStrObjProp("ownlink", FwObjDataTypes.kodtOwnNameGuidHot, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padBottom' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_PadBottom()
		{
			CheckIntProp("padBottom", "0", (int)FwTextPropType.ktptPadBottom, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("padBottom", "12", (int)FwTextPropType.ktptPadBottom, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padBottom' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_PadBottom_Fail()
		{
			CheckIntProp("padBottom", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padLeading' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_PadLeading()
		{
			CheckIntProp("padLeading", "0", (int)FwTextPropType.ktptPadLeading, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("padLeading", "12", (int)FwTextPropType.ktptPadLeading, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padLeading' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_PadLeading_Fail()
		{
			CheckIntProp("padLeading", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padTop' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_PadTop()
		{
			CheckIntProp("padTop", "0", (int)FwTextPropType.ktptPadTop, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("padTop", "12", (int)FwTextPropType.ktptPadTop, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padTop' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_PadTop_Fail()
		{
			CheckIntProp("padTop", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padTrailing' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_PadTrailing()
		{
			CheckIntProp("padTrailing", "0", (int)FwTextPropType.ktptPadTrailing, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("padTrailing", "12", (int)FwTextPropType.ktptPadTrailing, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padTrailing' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_PadTrailing_Fail()
		{
			CheckIntProp("padTrailing", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'paracolor' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Paracolor()
		{
			CheckIntProp("paracolor", "red", (int)FwTextPropType.ktptParaColor, 0x0000FF);
			CheckIntProp("paracolor", "white", (int)FwTextPropType.ktptParaColor, 0xFFFFFF);
			CheckIntProp("paracolor", "blue", (int)FwTextPropType.ktptParaColor, 0xFF0000);
			CheckIntProp("paracolor", "F8F105", (int)FwTextPropType.ktptParaColor, 0x05F1F8);
			CheckIntProp("paracolor", "transparent", (int)FwTextPropType.ktptParaColor,
				(int)FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'paracolor' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Paracolor_Fail()
		{
			CheckIntProp("paracolor", "monkey", (int)FwTextPropType.ktptParaColor, 0x0000FF);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'rightToLeft' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_RightToLeft()
		{
			CheckIntProp("rightToLeft", "0", (int)FwTextPropType.ktptRightToLeft, 0, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("rightToLeft", "1", (int)FwTextPropType.ktptRightToLeft, 1, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("rightToLeft", "5", (int)FwTextPropType.ktptRightToLeft, 5, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'rightToLeft' attribute with an
		/// invalid attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_RightToLeft_Fail()
		{
			CheckIntProp("rightToLeft", "-1", (int)FwTextPropType.ktptRightToLeft, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'spaceAfter' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_SpaceAfter()
		{
			CheckIntProp("spaceAfter", "0", (int)FwTextPropType.ktptSpaceAfter, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("spaceAfter", "12", (int)FwTextPropType.ktptSpaceAfter, 12, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("spaceAfter", "-12", (int)FwTextPropType.ktptSpaceAfter, -12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'spaceBefore' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_SpaceBefore()
		{
			CheckIntProp("spaceBefore", "0", (int)FwTextPropType.ktptSpaceBefore, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("spaceBefore", "12", (int)FwTextPropType.ktptSpaceBefore, 12, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("spaceBefore", "-12", (int)FwTextPropType.ktptSpaceBefore, -12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'spellcheck' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Spellcheck()
		{
			CheckIntProp("spellcheck", "normal", (int)FwTextPropType.ktptSpellCheck, (int)SpellingModes.ksmNormalCheck, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("spellcheck", "doNotCheck", (int)FwTextPropType.ktptSpellCheck, (int)SpellingModes.ksmDoNotCheck, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("spellcheck", "forceCheck", (int)FwTextPropType.ktptSpellCheck, (int)SpellingModes.ksmForceCheck, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'spellcheck' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Spellcheck_Fail()
		{
			CheckIntProp("spellcheck", "monkey", (int)FwTextPropType.ktptSpellCheck, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'superscript' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Superscript()
		{
			CheckIntProp("superscript", "off", (int)FwTextPropType.ktptSuperscript, (int)FwSuperscriptVal.kssvOff, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("superscript", "super", (int)FwTextPropType.ktptSuperscript, (int)FwSuperscriptVal.kssvSuper, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("superscript", "sub", (int)FwTextPropType.ktptSuperscript, (int)FwSuperscriptVal.kssvSub, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'superscript' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Superscript_Fail()
		{
			CheckIntProp("superscript", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'tags' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Tags()
		{
			Guid expectedGuid1 = new Guid("3B88FBA7-10C7-4e14-9EE0-3F0DDA060A0D");
			Guid expectedGuid2 = new Guid("C4F03ECA-BC03-4175-B5AA-13F3ECB7F481");
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop tags='" + expectedGuid1 +
				" " + expectedGuid2 + "'></Prop>", m_wsManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);

			Assert.AreEqual("\uFBA7\u3B88\u10C7\u4e14\uE09E\u0D3F\u06DA\u0D0A" +
				"\u3ECA\uC4F0\uBC03\u4175\uAAB5\uF313\uB7EC\u81F4",
				ttp.GetStrPropValue((int)FwTextPropType.ktptTags));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'tags' attribute when the GUIDs are
		/// separated by the letter 'I' (old-style tag strings) (FWR-1118)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Tags_SeparatedWithLetterI()
		{
			Guid expectedGuid1 = new Guid("3B88FBA7-10C7-4e14-9EE0-3F0DDA060A0D");
			Guid expectedGuid2 = new Guid("C4F03ECA-BC03-4175-B5AA-13F3ECB7F481");
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop tags='I" + expectedGuid1 +
				" I" + expectedGuid2 + "'></Prop>", m_wsManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);

			Assert.AreEqual("\uFBA7\u3B88\u10C7\u4e14\uE09E\u0D3F\u06DA\u0D0A" +
				"\u3ECA\uC4F0\uBC03\u4175\uAAB5\uF313\uB7EC\u81F4",
				ttp.GetStrPropValue((int)FwTextPropType.ktptTags));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'trailingIndent' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_TrailingIndent()
		{
			CheckIntProp("trailingIndent", "0", (int)FwTextPropType.ktptTrailingIndent, 0, (int)FwTextPropVar.ktpvMilliPoint);
			CheckIntProp("trailingIndent", "12", (int)FwTextPropType.ktptTrailingIndent, 12, (int)FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'trailingIndent' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_TrailingIndent_Fail()
		{
			CheckIntProp("trailingIndent", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'undercolor' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Undercolor()
		{
			CheckIntProp("undercolor", "red", (int)FwTextPropType.ktptUnderColor, 0x0000FF);
			CheckIntProp("undercolor", "white", (int)FwTextPropType.ktptUnderColor, 0xFFFFFF);
			CheckIntProp("undercolor", "blue", (int)FwTextPropType.ktptUnderColor, 0xFF0000);
			CheckIntProp("undercolor", "F8F105", (int)FwTextPropType.ktptUnderColor, 0x05F1F8);
			CheckIntProp("undercolor", "transparent", (int)FwTextPropType.ktptUnderColor,
				(int)FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'undercolor' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Undercolor_Fail()
		{
			CheckIntProp("undercolor", "monkey", (int)FwTextPropType.ktptUnderColor, 0x0000FF);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'underline' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Underline()
		{
			CheckIntProp("underline", "none", (int)FwTextPropType.ktptUnderline, (int)FwUnderlineType.kuntNone, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("underline", "single", (int)FwTextPropType.ktptUnderline, (int)FwUnderlineType.kuntSingle, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("underline", "double", (int)FwTextPropType.ktptUnderline, (int)FwUnderlineType.kuntDouble, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("underline", "dotted", (int)FwTextPropType.ktptUnderline, (int)FwUnderlineType.kuntDotted, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("underline", "dashed", (int)FwTextPropType.ktptUnderline, (int)FwUnderlineType.kuntDashed, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("underline", "strikethrough", (int)FwTextPropType.ktptUnderline, (int)FwUnderlineType.kuntStrikethrough, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("underline", "squiggle", (int)FwTextPropType.ktptUnderline, (int)FwUnderlineType.kuntSquiggle, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'underline' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Underline_Fail()
		{
			CheckIntProp("underline", "monkey", (int)FwTextPropType.ktptUnderline, (int)FwUnderlineType.kuntNone, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'widowOrphan' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_WidowOrphan()
		{
			CheckIntProp("widowOrphan", "1", (int)FwTextPropType.ktptWidowOrphanControl, 1, (int)FwTextPropVar.ktpvEnum);
			CheckIntProp("widowOrphan", "0", (int)FwTextPropType.ktptWidowOrphanControl, 0, (int)FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'widowOrphan' attribute with an
		/// invalid value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_WidowOrphan_Fail()
		{
			CheckIntProp("widowOrphan", "monkey", (int)FwTextPropType.ktptWidowOrphanControl, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'wsBase' attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_WsBase()
		{
			CheckIntProp("wsBase", "en", (int)FwTextPropType.ktptBaseWs, m_enWs);
			CheckIntProp("wsBase", "es", (int)FwTextPropType.ktptBaseWs, m_esWs);

			// An empty string is supposedly valid (according to the documentation we found),
			// however it looks like the C++ code would, indeed, throw an exception if
			// the value was empty, so we duplicated that behavior instead (since it makes
			// more sense).
			//CheckIntProp("wsBase", string.Empty, (int)FwTextPropType.ktptBaseWs, m_esWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'wsBase' attribute with an invalid
		/// value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_WsBase_Fail()
		{
			CheckIntProp("wsBase", "fr__X_ETIC", (int)FwTextPropType.ktptBaseWs, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'WsStyles9999' element inside
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_WsStyles9999()
		{
			CheckWsStyleProp("<WsProp ws='en' fontsize='80' fontsizeUnit='mpt' forecolor='2f60ff'></WsProp>",
				"\u87c1\u3b8b" + // Writing system HVO for 'en'
				"\u0000" + // Length of font family string (zero)
				"\u0002" + // integer property count (2)
				(char)FwTextPropType.ktptFontSize + (char)FwTextPropVar.ktpvMilliPoint + "\u0050\u0000" +
				(char)FwTextPropType.ktptForeColor + (char)FwTextPropVar.ktpvDefault + "\u602F\u00FF");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'WsStyles9999' element inside with
		/// string properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_WsStyles9999_WithStrProps()
		{
			CheckWsStyleProp("<WsProp ws='en' fontFamily='Times New Roman' fontsize='80' fontsizeUnit='mpt'" +
				" forecolor='2f60ff' fontVariations='185=1,851=2,57=12'></WsProp>",
				"\u87c1\u3b8b" + // Writing system HVO for 'en'
				"\u000F" + // Length of font family string (15)
				"Times New Roman" + // Font family string
				"\uFFFF" + // string property count as negative (-1)
				(char)FwTextPropType.ktptFontVariations +
				"\u0011" + // Length of the string in the string prop (17)
				"185=1,851=2,57=12" + // String value
				"\u0002" + // integer property count (2)
				(char)FwTextPropType.ktptFontSize + (char)FwTextPropVar.ktpvMilliPoint + "\u0050\u0000" +
				(char)FwTextPropType.ktptForeColor + (char)FwTextPropVar.ktpvDefault + "\u602F\u00FF");
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the bullet font information prop.
		/// </summary>
		/// <param name="bulText">The bullet font information XML text.</param>
		/// <param name="expectedValue">The expected value.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckBulFontInfoProp(string bulText, string expectedValue)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop>" + bulText + "</Prop>", m_wsManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);
			Assert.AreEqual(expectedValue, ttp.GetStrPropValue((int)FwTextPropType.ktptBulNumFontInfo));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the writing system style prop.
		/// </summary>
		/// <param name="wsStyleText">The writing system style XML text.</param>
		/// <param name="expectedValue">The expected value.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckWsStyleProp(string wsStyleText, string expectedValue)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop><WsStyles9999>" +
				wsStyleText + "</WsStyles9999></Prop>", m_wsManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);
			Assert.AreEqual(expectedValue, ttp.GetStrPropValue((int) FwTextPropType.ktptWsStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an object property stored in the string props.
		/// </summary>
		/// <param name="propName">Name of the property.</param>
		/// <param name="propType">Type of the object property.</param>
		/// <param name="expectedGuid">The expected GUID in the object property.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckStrObjProp(string propName, FwObjDataTypes propType, Guid expectedGuid)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop " + propName + "='" + expectedGuid + "'></Prop>", m_wsManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);

			FwObjDataTypes type;
			Guid guid = TsStringUtils.GetGuidFromProps(ttp, null, out type);
			Assert.AreEqual(expectedGuid, guid);
			Assert.AreEqual(propType, type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an integer text property. Expects a variant of FwTextPropVar.ktpvDefault.
		/// </summary>
		/// <param name="propName">Name of the property in the XML.</param>
		/// <param name="propValue">The value of the property in the XML.</param>
		/// <param name="propType">Type of the property that is put in the text props.</param>
		/// <param name="expectedValue">The expected value in the text props.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckStrProp(string propName, string propValue, int propType, string expectedValue)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop " + propName + "='" +
				propValue + "'></Prop>", m_wsManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);
			Assert.AreEqual(expectedValue, ttp.GetStrPropValue(propType));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an integer text property. Expects a variant of FwTextPropVar.ktpvDefault.
		/// </summary>
		/// <param name="propName">Name of the property in the XML.</param>
		/// <param name="propValue">The value of the property in the XML.</param>
		/// <param name="propType">Type of the property that is put in the text props.</param>
		/// <param name="expectedValue">The expected value in the text props.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckIntProp(string propName, string propValue, int propType, int expectedValue)
		{
			CheckIntProp(propName, propValue, propType, expectedValue, (int)FwTextPropVar.ktpvDefault);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an integer text property.
		/// </summary>
		/// <param name="propName">Name of the property in the XML.</param>
		/// <param name="propValue">The value of the property in the XML.</param>
		/// <param name="propType">Type of the property that is put in the text props.</param>
		/// <param name="expectedValue">The expected value in the text props.</param>
		/// <param name="expectedVar">The expected variant in the text props.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckIntProp(string propName, string propValue, int propType, int expectedValue, int expectedVar)
		{
			CheckIntProp(propName + "='" + propValue + "'", propType, expectedValue, expectedVar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an integer text property.
		/// </summary>
		/// <param name="propText">String containing the property and value in the XML</param>
		/// <param name="propType">Type of the property that is put in the text props.</param>
		/// <param name="expectedValue">The expected value in the text props.</param>
		/// <param name="expectedVar">The expected variant in the text props.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckIntProp(string propText, int propType, int expectedValue, int expectedVar)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop " + propText + "></Prop>", m_wsManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(1, ttp.IntPropCount);
			Assert.AreEqual(0, ttp.StrPropCount);
			int var;
			Assert.AreEqual(expectedValue, ttp.GetIntPropValues(propType, out var));
			Assert.AreEqual(expectedVar, var);
		}
		#endregion
	}
}
