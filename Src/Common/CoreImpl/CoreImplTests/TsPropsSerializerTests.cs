using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.CoreImpl
{
	[TestFixture]
	public class TsPropsSerializerTests : TsSerializerTestsBase
	{
		#region SerializePropsToXml tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with an 'align' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Align()
		{
			CheckSerializeIntProp(FwTextPropType.ktptAlign, (int) FwTextAlign.ktalCenter, FwTextPropVar.ktpvEnum, "align", "center");
			CheckSerializeIntProp(FwTextPropType.ktptAlign, (int) FwTextAlign.ktalJustify, FwTextPropVar.ktpvEnum, "align", "justify");
			CheckSerializeIntProp(FwTextPropType.ktptAlign, (int) FwTextAlign.ktalLeading, FwTextPropVar.ktpvEnum, "align", "leading");
			CheckSerializeIntProp(FwTextPropType.ktptAlign, (int) FwTextAlign.ktalLeft, FwTextPropVar.ktpvEnum, "align", "left");
			CheckSerializeIntProp(FwTextPropType.ktptAlign, (int) FwTextAlign.ktalRight, FwTextPropVar.ktpvEnum, "align", "right");
			CheckSerializeIntProp(FwTextPropType.ktptAlign, (int) FwTextAlign.ktalTrailing, FwTextPropVar.ktpvEnum, "align", "trailing");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'backcolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Backcolor()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBackColor, 0x0000FF, "backcolor", "red");
			CheckSerializeIntProp(FwTextPropType.ktptBackColor, 0xFFFFFF, "backcolor", "white");
			CheckSerializeIntProp(FwTextPropType.ktptBackColor, 0xFF0000, "backcolor", "blue");
			CheckSerializeIntProp(FwTextPropType.ktptBackColor, 0x05F1F8, "backcolor", "f8f105");
			CheckSerializeIntProp(FwTextPropType.ktptBackColor, (int) FwTextColor.kclrTransparent, "backcolor", "transparent");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'bold' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Bold()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBold, (int) FwTextToggleVal.kttvOff, FwTextPropVar.ktpvEnum, "bold", "off");
			CheckSerializeIntProp(FwTextPropType.ktptBold, (int) FwTextToggleVal.kttvForceOn, FwTextPropVar.ktpvEnum, "bold", "on");
			CheckSerializeIntProp(FwTextPropType.ktptBold, (int) FwTextToggleVal.kttvInvert, FwTextPropVar.ktpvEnum, "bold", "invert");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'borderBottom' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BorderBottom()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBorderBottom, 0, FwTextPropVar.ktpvMilliPoint, "borderBottom", "0");
			CheckSerializeIntProp(FwTextPropType.ktptBorderBottom, 12, FwTextPropVar.ktpvMilliPoint, "borderBottom", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'borderColor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BorderColor()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBorderColor, 0x0000FF, "borderColor", "red");
			CheckSerializeIntProp(FwTextPropType.ktptBorderColor, 0xFFFFFF, "borderColor", "white");
			CheckSerializeIntProp(FwTextPropType.ktptBorderColor, 0xFF0000, "borderColor", "blue");
			CheckSerializeIntProp(FwTextPropType.ktptBorderColor, 0x05F1F8, "borderColor", "f8f105");
			CheckSerializeIntProp(FwTextPropType.ktptBorderColor, (int) FwTextColor.kclrTransparent, "borderColor", "transparent");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'borderLeading' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BorderLeading()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBorderLeading, 0, FwTextPropVar.ktpvMilliPoint, "borderLeading", "0");
			CheckSerializeIntProp(FwTextPropType.ktptBorderLeading, 12, FwTextPropVar.ktpvMilliPoint, "borderLeading", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'borderTop' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BorderTop()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBorderTop, 0, FwTextPropVar.ktpvMilliPoint, "borderTop", "0");
			CheckSerializeIntProp(FwTextPropType.ktptBorderTop, 12, FwTextPropVar.ktpvMilliPoint, "borderTop", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'borderTrailing' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BorderTrailing()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBorderTrailing, 0, FwTextPropVar.ktpvMilliPoint, "borderTrailing", "0");
			CheckSerializeIntProp(FwTextPropType.ktptBorderTrailing, 12, FwTextPropVar.ktpvMilliPoint, "borderTrailing", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'bulNumFontInfo' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BulNumFontInfo()
		{
			ITsPropsBldr tpb = TsPropsFactory.GetPropsBldr();
			tpb.SetStringValue(FwTextPropType.ktptBulNumFontInfo,
				(char) FwTextPropType.ktptItalic + "\u0001\u0000" +
				(char) FwTextPropType.ktptBold + "\u0001\u0000" +
				(char) FwTextPropType.ktptSuperscript + "\u0001\u0000" +
				(char) FwTextPropType.ktptUnderline + "\u0002\u0000" +
				(char) FwTextPropType.ktptFontSize + "\u0014\u0000" +
				(char) FwTextPropType.ktptOffset + "\u000A\u0000" +
				(char) FwTextPropType.ktptForeColor + "\u0000\u00FF" +
				(char) FwTextPropType.ktptBackColor + "\uFFFF\u00FF" +
				(char) FwTextPropType.ktptUnderColor + "\u00FF\u0000" +
				(char) FwTextPropType.ktptFontFamily + "Times New Roman\u0000");

			string xml = TsPropsSerializer.SerializePropsToXml(tpb.GetTextProps(), WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo("<Prop><BulNumFontInfo backcolor=\"white\" bold=\"on\" fontsize=\"20mpt\""
				+ " forecolor=\"blue\" italic=\"on\" offset=\"10mpt\" superscript=\"super\" undercolor=\"red\""
				+ " underline=\"dashed\" fontFamily=\"Times New Roman\"></BulNumFontInfo></Prop>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'bulNumScheme' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BulNumScheme()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBulNumScheme, 0, FwTextPropVar.ktpvEnum, "bulNumScheme", "0");
			CheckSerializeIntProp(FwTextPropType.ktptBulNumScheme, 3, FwTextPropVar.ktpvEnum, "bulNumScheme", "3");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'bulNumStartAt' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BulNumStartAt()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBulNumStartAt, 0, "bulNumStartAt", "0");
			CheckSerializeIntProp(FwTextPropType.ktptBulNumStartAt, 12, "bulNumStartAt", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'bulNumTxtAft' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BulNumTxtAft()
		{
			CheckSerializeStrProp(FwTextPropType.ktptBulNumTxtAft, "my", "bulNumTxtAft", "my");
			CheckSerializeStrProp(FwTextPropType.ktptBulNumTxtAft, "TexT", "bulNumTxtAft", "TexT");
			CheckSerializeStrProp(FwTextPropType.ktptBulNumTxtAft, null, "bulNumTxtAft", string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'bulNumTxtBef' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_BulNumTxtBef()
		{
			CheckSerializeStrProp(FwTextPropType.ktptBulNumTxtBef, "my", "bulNumTxtBef", "my");
			CheckSerializeStrProp(FwTextPropType.ktptBulNumTxtBef, "TexT", "bulNumTxtBef", "TexT");
			CheckSerializeStrProp(FwTextPropType.ktptBulNumTxtBef, null, "bulNumTxtBef", string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'contextString' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_ContextString()
		{
			CheckSerializeStrObjProp("contextString", FwObjDataTypes.kodtContextString, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'firstIndent' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_FirstIndent()
		{
			CheckSerializeIntProp(FwTextPropType.ktptFirstIndent, 0, FwTextPropVar.ktpvMilliPoint, "firstIndent", "0");
			CheckSerializeIntProp(FwTextPropType.ktptFirstIndent, 12, FwTextPropVar.ktpvMilliPoint, "firstIndent", "12");
			CheckSerializeIntProp(FwTextPropType.ktptFirstIndent, -12, FwTextPropVar.ktpvMilliPoint, "firstIndent", "-12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'fontFamily' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_FontFamily()
		{
			CheckSerializeStrProp(FwTextPropType.ktptFontFamily, "Times New Roman", "fontFamily", "Times New Roman");
			CheckSerializeStrProp(FwTextPropType.ktptFontFamily, "Courier", "fontFamily", "Courier");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'fontsize' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Fontsize()
		{
			CheckSerializeIntProp(FwTextPropType.ktptFontSize, 5, FwTextPropVar.ktpvDefault, "fontsize=\"5\"");
			CheckSerializeIntProp(FwTextPropType.ktptFontSize, 10, FwTextPropVar.ktpvMilliPoint, "fontsize=\"10\" fontsizeUnit=\"mpt\"");
			CheckSerializeIntProp(FwTextPropType.ktptFontSize, 15, FwTextPropVar.ktpvRelative, "fontsize=\"15\" fontsizeUnit=\"rel\"");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'fontVariations' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_FontVariations()
		{
			CheckSerializeStrProp(FwTextPropType.ktptFontVariations, "156=1,896=2,84=21", "fontVariations", "156=1,896=2,84=21");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'forecolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Forecolor()
		{
			CheckSerializeIntProp(FwTextPropType.ktptForeColor, 0x0000FF, "forecolor", "red");
			CheckSerializeIntProp(FwTextPropType.ktptForeColor, 0xFFFFFF, "forecolor", "white");
			CheckSerializeIntProp(FwTextPropType.ktptForeColor, 0xFF0000, "forecolor", "blue");
			CheckSerializeIntProp(FwTextPropType.ktptForeColor, 0x05F1F8, "forecolor", "f8f105");
			CheckSerializeIntProp(FwTextPropType.ktptForeColor, (int) FwTextColor.kclrTransparent, "forecolor", "transparent");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'italic' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Italic()
		{
			CheckSerializeIntProp(FwTextPropType.ktptItalic, (int) FwTextToggleVal.kttvOff, FwTextPropVar.ktpvEnum, "italic", "off");
			CheckSerializeIntProp(FwTextPropType.ktptItalic, (int) FwTextToggleVal.kttvForceOn, FwTextPropVar.ktpvEnum, "italic", "on");
			CheckSerializeIntProp(FwTextPropType.ktptItalic, (int) FwTextToggleVal.kttvInvert, FwTextPropVar.ktpvEnum, "italic", "invert");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'keepTogether' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_KeepTogether()
		{
			CheckSerializeIntProp(FwTextPropType.ktptKeepTogether, 1, FwTextPropVar.ktpvEnum, "keepTogether", "1");
			CheckSerializeIntProp(FwTextPropType.ktptKeepTogether, 0, FwTextPropVar.ktpvEnum, "keepTogether", "0");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'keepWithNext' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_KeepWithNext()
		{
			CheckSerializeIntProp(FwTextPropType.ktptKeepWithNext, 1, FwTextPropVar.ktpvEnum, "keepWithNext", "1");
			CheckSerializeIntProp(FwTextPropType.ktptKeepWithNext, 0, FwTextPropVar.ktpvEnum, "keepWithNext", "0");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'leadingIndent' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_LeadingIndent()
		{
			CheckSerializeIntProp(FwTextPropType.ktptLeadingIndent, 0, FwTextPropVar.ktpvMilliPoint, "leadingIndent", "0");
			CheckSerializeIntProp(FwTextPropType.ktptLeadingIndent, 12, FwTextPropVar.ktpvMilliPoint, "leadingIndent", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'lineHeight' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_LineHeight()
		{
			CheckSerializeIntProp(FwTextPropType.ktptLineHeight, 10, FwTextPropVar.ktpvMilliPoint, "lineHeight=\"10\" lineHeightUnit=\"mpt\" lineHeightType=\"atLeast\"");
			CheckSerializeIntProp(FwTextPropType.ktptLineHeight, -15, FwTextPropVar.ktpvMilliPoint, "lineHeight=\"15\" lineHeightUnit=\"mpt\" lineHeightType=\"exact\"");
			CheckSerializeIntProp(FwTextPropType.ktptLineHeight, 20, FwTextPropVar.ktpvRelative, "lineHeight=\"20\" lineHeightUnit=\"rel\"");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'link' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Link()
		{
			CheckSerializeStrObjProp("link", FwObjDataTypes.kodtNameGuidHot, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'marginBottom' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_MarginBottom()
		{
			CheckSerializeIntProp(FwTextPropType.ktptMarginBottom, 0, FwTextPropVar.ktpvMilliPoint, "spaceAfter", "0");
			CheckSerializeIntProp(FwTextPropType.ktptMarginBottom, 12, FwTextPropVar.ktpvMilliPoint, "spaceAfter", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'marginLeading' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_MarginLeading()
		{
			CheckSerializeIntProp(FwTextPropType.ktptMarginLeading, 0, FwTextPropVar.ktpvMilliPoint, "leadingIndent", "0");
			CheckSerializeIntProp(FwTextPropType.ktptMarginLeading, 12, FwTextPropVar.ktpvMilliPoint, "leadingIndent", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'marginTop' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_MarginTop()
		{
			CheckSerializeIntProp(FwTextPropType.ktptMarginTop, 0, FwTextPropVar.ktpvMilliPoint, "MarginTop", "0");
			CheckSerializeIntProp(FwTextPropType.ktptMarginTop, 12, FwTextPropVar.ktpvMilliPoint, "MarginTop", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'marginTrailing' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_MarginTrailing()
		{
			CheckSerializeIntProp(FwTextPropType.ktptMarginTrailing, 0, FwTextPropVar.ktpvMilliPoint, "trailingIndent", "0");
			CheckSerializeIntProp(FwTextPropType.ktptMarginTrailing, 12, FwTextPropVar.ktpvMilliPoint, "trailingIndent", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'moveableObj' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_MoveableObj()
		{
			CheckSerializeStrObjProp("moveableObj", FwObjDataTypes.kodtGuidMoveableObjDisp, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with an 'offset' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Offset()
		{
			CheckSerializeIntProp(FwTextPropType.ktptOffset, 10, FwTextPropVar.ktpvMilliPoint, "offset=\"10\" offsetUnit=\"mpt\"");
			CheckSerializeIntProp(FwTextPropType.ktptOffset, 15, FwTextPropVar.ktpvRelative, "offset=\"15\" offsetUnit=\"rel\"");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'offset' attribute with a negative
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_NegativeOffset()
		{
			CheckSerializeIntProp(FwTextPropType.ktptOffset, -10, FwTextPropVar.ktpvMilliPoint, "offset=\"-10\" offsetUnit=\"mpt\"");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'ownlink' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Ownlink()
		{
			CheckSerializeStrObjProp("ownlink", FwObjDataTypes.kodtOwnNameGuidHot, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'padBottom' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_PadBottom()
		{
			CheckSerializeIntProp(FwTextPropType.ktptPadBottom, 0, FwTextPropVar.ktpvMilliPoint, "padBottom", "0");
			CheckSerializeIntProp(FwTextPropType.ktptPadBottom, 12, FwTextPropVar.ktpvMilliPoint, "padBottom", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'padLeading' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_PadLeading()
		{
			CheckSerializeIntProp(FwTextPropType.ktptPadLeading, 0, FwTextPropVar.ktpvMilliPoint, "padLeading", "0");
			CheckSerializeIntProp(FwTextPropType.ktptPadLeading, 12, FwTextPropVar.ktpvMilliPoint, "padLeading", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'padTop' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_PadTop()
		{
			CheckSerializeIntProp(FwTextPropType.ktptPadTop, 0, FwTextPropVar.ktpvMilliPoint, "padTop", "0");
			CheckSerializeIntProp(FwTextPropType.ktptPadTop, 12, FwTextPropVar.ktpvMilliPoint, "padTop", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'padTrailing' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_PadTrailing()
		{
			CheckSerializeIntProp(FwTextPropType.ktptPadTrailing, 0, FwTextPropVar.ktpvMilliPoint, "padTrailing", "0");
			CheckSerializeIntProp(FwTextPropType.ktptPadTrailing, 12, FwTextPropVar.ktpvMilliPoint, "padTrailing", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'paracolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Paracolor()
		{
			CheckSerializeIntProp(FwTextPropType.ktptParaColor, 0x0000FF, "paracolor", "red");
			CheckSerializeIntProp(FwTextPropType.ktptParaColor, 0xFFFFFF, "paracolor", "white");
			CheckSerializeIntProp(FwTextPropType.ktptParaColor, 0xFF0000, "paracolor", "blue");
			CheckSerializeIntProp(FwTextPropType.ktptParaColor, 0x05F1F8, "paracolor", "f8f105");
			CheckSerializeIntProp(FwTextPropType.ktptParaColor, (int) FwTextColor.kclrTransparent, "paracolor", "transparent");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'rightToLeft' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_RightToLeft()
		{
			CheckSerializeIntProp(FwTextPropType.ktptRightToLeft, 0, FwTextPropVar.ktpvEnum, "rightToLeft", "0");
			CheckSerializeIntProp(FwTextPropType.ktptRightToLeft, 1, FwTextPropVar.ktpvEnum, "rightToLeft", "1");
			CheckSerializeIntProp(FwTextPropType.ktptRightToLeft, 5, FwTextPropVar.ktpvEnum, "rightToLeft", "5");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'spaceAfter' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_SpaceAfter()
		{
			CheckSerializeIntProp(FwTextPropType.ktptSpaceAfter, 0, FwTextPropVar.ktpvMilliPoint, "spaceAfter", "0");
			CheckSerializeIntProp(FwTextPropType.ktptSpaceAfter, 12, FwTextPropVar.ktpvMilliPoint, "spaceAfter", "12");
			CheckSerializeIntProp(FwTextPropType.ktptSpaceAfter, -12, FwTextPropVar.ktpvMilliPoint, "spaceAfter", "-12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'spaceBefore' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_SpaceBefore()
		{
			CheckSerializeIntProp(FwTextPropType.ktptSpaceBefore, 0, FwTextPropVar.ktpvMilliPoint, "spaceBefore", "0");
			CheckSerializeIntProp(FwTextPropType.ktptSpaceBefore, 12, FwTextPropVar.ktpvMilliPoint, "spaceBefore", "12");
			CheckSerializeIntProp(FwTextPropType.ktptSpaceBefore, -12, FwTextPropVar.ktpvMilliPoint, "spaceBefore", "-12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'spellcheck' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Spellcheck()
		{
			CheckSerializeIntProp(FwTextPropType.ktptSpellCheck, (int) SpellingModes.ksmNormalCheck, FwTextPropVar.ktpvEnum, "spellcheck", "normal");
			CheckSerializeIntProp(FwTextPropType.ktptSpellCheck, (int) SpellingModes.ksmDoNotCheck, FwTextPropVar.ktpvEnum, "spellcheck", "doNotCheck");
			CheckSerializeIntProp(FwTextPropType.ktptSpellCheck, (int) SpellingModes.ksmForceCheck, FwTextPropVar.ktpvEnum, "spellcheck", "forceCheck");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'superscript' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Superscript()
		{
			CheckSerializeIntProp(FwTextPropType.ktptSuperscript, (int) FwSuperscriptVal.kssvOff, FwTextPropVar.ktpvEnum, "superscript", "off");
			CheckSerializeIntProp(FwTextPropType.ktptSuperscript, (int) FwSuperscriptVal.kssvSuper, FwTextPropVar.ktpvEnum, "superscript", "super");
			CheckSerializeIntProp(FwTextPropType.ktptSuperscript, (int) FwSuperscriptVal.kssvSub, FwTextPropVar.ktpvEnum, "superscript", "sub");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'tags' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Tags()
		{
			Guid expectedGuid1 = new Guid("3B88FBA7-10C7-4e14-9EE0-3F0DDA060A0D");
			Guid expectedGuid2 = new Guid("C4F03ECA-BC03-4175-B5AA-13F3ECB7F481");

			ITsPropsBldr tpb = TsPropsFactory.GetPropsBldr();
			tpb.SetStringValue(FwTextPropType.ktptTags, "\uFBA7\u3B88\u10C7\u4e14\uE09E\u0D3F\u06DA\u0D0A"
				+ "\u3ECA\uC4F0\uBC03\u4175\uAAB5\uF313\uB7EC\u81F4");

			string xml = TsPropsSerializer.SerializePropsToXml(tpb.GetTextProps(), WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(string.Format("<Prop tags=\"{0} {1}\"></Prop>", expectedGuid1, expectedGuid2)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'trailingIndent' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_TrailingIndent()
		{
			CheckSerializeIntProp(FwTextPropType.ktptTrailingIndent, 0, FwTextPropVar.ktpvMilliPoint, "trailingIndent", "0");
			CheckSerializeIntProp(FwTextPropType.ktptTrailingIndent, 12, FwTextPropVar.ktpvMilliPoint, "trailingIndent", "12");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'undercolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Undercolor()
		{
			CheckSerializeIntProp(FwTextPropType.ktptUnderColor, 0x0000FF, "undercolor", "red");
			CheckSerializeIntProp(FwTextPropType.ktptUnderColor, 0xFFFFFF, "undercolor", "white");
			CheckSerializeIntProp(FwTextPropType.ktptUnderColor, 0xFF0000, "undercolor", "blue");
			CheckSerializeIntProp(FwTextPropType.ktptUnderColor, 0x05F1F8, "undercolor", "f8f105");
			CheckSerializeIntProp(FwTextPropType.ktptUnderColor, (int) FwTextColor.kclrTransparent, "undercolor", "transparent");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'underline' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_Underline()
		{
			CheckSerializeIntProp(FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntNone, FwTextPropVar.ktpvEnum, "underline", "none");
			CheckSerializeIntProp(FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntSingle, FwTextPropVar.ktpvEnum, "underline", "single");
			CheckSerializeIntProp(FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntDouble, FwTextPropVar.ktpvEnum, "underline", "double");
			CheckSerializeIntProp(FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntDotted, FwTextPropVar.ktpvEnum, "underline", "dotted");
			CheckSerializeIntProp(FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntDashed, FwTextPropVar.ktpvEnum, "underline", "dashed");
			CheckSerializeIntProp(FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntStrikethrough, FwTextPropVar.ktpvEnum, "underline", "strikethrough");
			CheckSerializeIntProp(FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntSquiggle, FwTextPropVar.ktpvEnum, "underline", "squiggle");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'widowOrphan' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_WidowOrphan()
		{
			CheckSerializeIntProp(FwTextPropType.ktptWidowOrphanControl, 1, FwTextPropVar.ktpvEnum, "widowOrphan", "1");
			CheckSerializeIntProp(FwTextPropType.ktptWidowOrphanControl, 0, FwTextPropVar.ktpvEnum, "widowOrphan", "0");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'wsBase' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_WsBase()
		{
			CheckSerializeIntProp(FwTextPropType.ktptBaseWs, EnWS, "wsBase", "en");
			CheckSerializeIntProp(FwTextPropType.ktptBaseWs, EsWS, "wsBase", "es");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'WsStyles9999' element inside.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_WsStyles9999()
		{
			CheckSerializeWsStyleProp("\u87c1\u3b8b" // Writing system HVO for 'en'
				+ "\u0000" // Length of font family string (zero)
				+ "\u0002" // integer property count (2)
				+ (char) FwTextPropType.ktptFontSize + (char) FwTextPropVar.ktpvMilliPoint + "\u0050\u0000"
				+ (char) FwTextPropType.ktptForeColor + (char) FwTextPropVar.ktpvDefault + "\u602F\u00FF",
				"<WsProp ws=\"en\" fontsize=\"80\" fontsizeUnit=\"mpt\" forecolor=\"2f60ff\"></WsProp>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SerializePropsToXml method with a 'WsStyles9999' element inside with
		/// string properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SerializePropsToXml_WsStyles9999_WithStrProps()
		{
			CheckSerializeWsStyleProp("\u87c1\u3b8b" // Writing system HVO for 'en'
				+ "\u000F" // Length of font family string (15)
				+ "Times New Roman" // Font family string
				+ "\uFFFF" // string property count as negative (-1)
				+ (char) FwTextPropType.ktptFontVariations
				+ "\u0011" // Length of the string in the string prop (17)
				+ "185=1,851=2,57=12" // String value
				+ "\u0002" // integer property count (2)
				+ (char) FwTextPropType.ktptFontSize + (char) FwTextPropVar.ktpvMilliPoint + "\u0050\u0000"
				+ (char) FwTextPropType.ktptForeColor + (char) FwTextPropVar.ktpvDefault + "\u602F\u00FF",
				"<WsProp ws=\"en\" fontFamily=\"Times New Roman\" fontVariations=\"185=1,851=2,57=12\""
				+ " fontsize=\"80\" fontsizeUnit=\"mpt\" forecolor=\"2f60ff\"></WsProp>");
		}

		#endregion

		#region DeserializePropsFromXml tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with an 'align' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Align()
		{
			CheckDeserializeIntProp("align", "center", FwTextPropType.ktptAlign, (int) FwTextAlign.ktalCenter, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("align", "justify", FwTextPropType.ktptAlign, (int) FwTextAlign.ktalJustify, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("align", "leading", FwTextPropType.ktptAlign, (int) FwTextAlign.ktalLeading, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("align", "left", FwTextPropType.ktptAlign, (int) FwTextAlign.ktalLeft, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("align", "right", FwTextPropType.ktptAlign, (int) FwTextAlign.ktalRight, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("align", "trailing", FwTextPropType.ktptAlign, (int) FwTextAlign.ktalTrailing, FwTextPropVar.ktpvEnum);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with an 'align' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Align_Fail()
		{
			CheckDeserializeIntProp("align", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'backcolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Backcolor()
		{
			CheckDeserializeIntProp("backcolor", "red", FwTextPropType.ktptBackColor, 0x0000FF);
			CheckDeserializeIntProp("backcolor", "white", FwTextPropType.ktptBackColor, 0xFFFFFF);
			CheckDeserializeIntProp("backcolor", "blue", FwTextPropType.ktptBackColor, 0xFF0000);
			CheckDeserializeIntProp("backcolor", "F8F105", FwTextPropType.ktptBackColor, 0x05F1F8);
			CheckDeserializeIntProp("backcolor", "transparent", FwTextPropType.ktptBackColor, (int) FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'backcolor' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Backcolor_Fail()
		{
			CheckDeserializeIntProp("backcolor", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bold' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Bold()
		{
			CheckDeserializeIntProp("bold", "off", FwTextPropType.ktptBold, (int) FwTextToggleVal.kttvOff, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("bold", "on", FwTextPropType.ktptBold, (int) FwTextToggleVal.kttvForceOn, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("bold", "invert", FwTextPropType.ktptBold, (int) FwTextToggleVal.kttvInvert, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bold' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Bold_Fail()
		{
			CheckDeserializeIntProp("bold", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderBottom' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderBottom()
		{
			CheckDeserializeIntProp("borderBottom", "0", FwTextPropType.ktptBorderBottom, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("borderBottom", "12", FwTextPropType.ktptBorderBottom, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderBottom' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderBottom_Fail()
		{
			CheckDeserializeIntProp("borderBottom", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderColor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderColor()
		{
			CheckDeserializeIntProp("borderColor", "red", FwTextPropType.ktptBorderColor, 0x0000FF);
			CheckDeserializeIntProp("borderColor", "white", FwTextPropType.ktptBorderColor, 0xFFFFFF);
			CheckDeserializeIntProp("borderColor", "blue", FwTextPropType.ktptBorderColor, 0xFF0000);
			CheckDeserializeIntProp("borderColor", "F8F105", FwTextPropType.ktptBorderColor, 0x05F1F8);
			CheckDeserializeIntProp("borderColor", "transparent", FwTextPropType.ktptBorderColor, (int) FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderColor' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderColor_Fail()
		{
			CheckDeserializeIntProp("borderColor", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderLeading' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderLeading()
		{
			CheckDeserializeIntProp("borderLeading", "0", FwTextPropType.ktptBorderLeading, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("borderLeading", "12", FwTextPropType.ktptBorderLeading, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderLeading' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderLeading_Fail()
		{
			CheckDeserializeIntProp("borderLeading", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderTop' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderTop()
		{
			CheckDeserializeIntProp("borderTop", "0", FwTextPropType.ktptBorderTop, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("borderTop", "12", FwTextPropType.ktptBorderTop, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderTop' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderTop_Fail()
		{
			CheckDeserializeIntProp("borderTop", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderTrailing' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BorderTrailing()
		{
			CheckDeserializeIntProp("borderTrailing", "0", FwTextPropType.ktptBorderTrailing, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("borderTrailing", "12", FwTextPropType.ktptBorderTrailing, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'borderTrailing' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BorderTrailing_Fail()
		{
			CheckDeserializeIntProp("borderTrailing", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumFontInfo' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumFontInfo()
		{
			CheckDeserializeBulFontInfoProp("<BulNumFontInfo backcolor=\"white\" bold=\"on\" fontsize=\"20mpt\"" +
				" forecolor=\"blue\" italic=\"on\" offset=\"10mpt\" superscript=\"super\" undercolor=\"red\"" +
				" underline=\"dashed\" fontFamily=\"Times New Roman\"></BulNumFontInfo>",
				(char) FwTextPropType.ktptItalic + "\u0001\u0000" +
				(char) FwTextPropType.ktptBold + "\u0001\u0000" +
				(char) FwTextPropType.ktptSuperscript + "\u0001\u0000" +
				(char) FwTextPropType.ktptUnderline + "\u0002\u0000" +
				(char) FwTextPropType.ktptFontSize + "\u0014\u0000" +
				(char) FwTextPropType.ktptOffset + "\u000A\u0000" +
				(char) FwTextPropType.ktptForeColor + "\u0000\u00FF" +
				(char) FwTextPropType.ktptBackColor + "\uFFFF\u00FF" +
				(char) FwTextPropType.ktptUnderColor + "\u00FF\u0000" +
				(char) FwTextPropType.ktptFontFamily + "Times New Roman\u0000");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumScheme' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumScheme()
		{
			CheckDeserializeIntProp("bulNumScheme", "0", FwTextPropType.ktptBulNumScheme, 0, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("bulNumScheme", "3", FwTextPropType.ktptBulNumScheme, 3, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumScheme' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BulNumScheme_Fail()
		{
			CheckDeserializeIntProp("bulNumScheme", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumStartAt' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumStartAt()
		{
			CheckDeserializeIntProp("bulNumStartAt", "0", FwTextPropType.ktptBulNumStartAt, 0);
			CheckDeserializeIntProp("bulNumStartAt", "12", FwTextPropType.ktptBulNumStartAt, 12);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumStartAt' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_BulNumStartAt_Fail()
		{
			CheckDeserializeIntProp("bulNumStartAt", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumTxtAft' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumTxtAft()
		{
			CheckDeserializeStrProp("bulNumTxtAft", "my", FwTextPropType.ktptBulNumTxtAft, "my");
			CheckDeserializeStrProp("bulNumTxtAft", "TexT", FwTextPropType.ktptBulNumTxtAft, "TexT");
			CheckDeserializeStrProp("bulNumTxtAft", string.Empty, FwTextPropType.ktptBulNumTxtAft, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'bulNumTxtBef' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_BulNumTxtBef()
		{
			CheckDeserializeStrProp("bulNumTxtBef", "my", FwTextPropType.ktptBulNumTxtBef, "my");
			CheckDeserializeStrProp("bulNumTxtBef", "TexT", FwTextPropType.ktptBulNumTxtBef, "TexT");
			CheckDeserializeStrProp("bulNumTxtBef", string.Empty, FwTextPropType.ktptBulNumTxtBef, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'contextString' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_ContextString()
		{
			CheckDeserializeStrObjProp("contextString", FwObjDataTypes.kodtContextString, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'firstIndent' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_FirstIndent()
		{
			CheckDeserializeIntProp("firstIndent", "0", FwTextPropType.ktptFirstIndent, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("firstIndent", "12", FwTextPropType.ktptFirstIndent, 12, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("firstIndent", "-12", FwTextPropType.ktptFirstIndent, -12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontFamily' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_FontFamily()
		{
			CheckDeserializeStrProp("fontFamily", "Times New Roman", FwTextPropType.ktptFontFamily, "Times New Roman");
			CheckDeserializeStrProp("fontFamily", "Courier", FwTextPropType.ktptFontFamily, "Courier");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontsize' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Fontsize()
		{
			CheckDeserializeIntProp("fontsize='5'", FwTextPropType.ktptFontSize, 5, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("fontsize='10' fontsizeUnit='mpt'", FwTextPropType.ktptFontSize, 10, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("fontsize='15' fontsizeUnit='rel'", FwTextPropType.ktptFontSize, 15, FwTextPropVar.ktpvRelative);
			CheckDeserializeIntProp("fontsize='20mpt'", FwTextPropType.ktptFontSize, 20, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontsize' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Fontsize_Fail1()
		{
			CheckDeserializeIntProp("fontsize='-10' fontsizeUnit='mpt'", FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontsize' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Fontsize_Fail2()
		{
			CheckDeserializeIntProp("fontsize='10' fontsizeUnit='monkey'", FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'fontVariations' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_FontVariations()
		{
			CheckDeserializeStrProp("fontVariations", "156=1,896=2,84=21", FwTextPropType.ktptFontVariations, "156=1,896=2,84=21");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'forecolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Forecolor()
		{
			CheckDeserializeIntProp("forecolor", "red", FwTextPropType.ktptForeColor, 0x0000FF);
			CheckDeserializeIntProp("forecolor", "white", FwTextPropType.ktptForeColor, 0xFFFFFF);
			CheckDeserializeIntProp("forecolor", "blue", FwTextPropType.ktptForeColor, 0xFF0000);
			CheckDeserializeIntProp("forecolor", "F8F105", FwTextPropType.ktptForeColor, 0x05F1F8);
			CheckDeserializeIntProp("forecolor", "transparent", FwTextPropType.ktptForeColor,
				(int)FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'forecolor' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Forecolor_Fail()
		{
			CheckDeserializeIntProp("forecolor", "monkey", FwTextPropType.ktptForeColor, 0x0000FF);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'italic' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Italic()
		{
			CheckDeserializeIntProp("italic", "off", FwTextPropType.ktptItalic, (int) FwTextToggleVal.kttvOff, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("italic", "on", FwTextPropType.ktptItalic, (int) FwTextToggleVal.kttvForceOn, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("italic", "invert", FwTextPropType.ktptItalic, (int) FwTextToggleVal.kttvInvert, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'italic' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Italic_Fail()
		{
			CheckDeserializeIntProp("italic", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'keepTogether' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_KeepTogether()
		{
			CheckDeserializeIntProp("keepTogether", "1", FwTextPropType.ktptKeepTogether, 1, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("keepTogether", "0", FwTextPropType.ktptKeepTogether, 0, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'keepWithNext' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_KeepTogether_Fail()
		{
			CheckDeserializeIntProp("keepWithNext", "monkey", FwTextPropType.ktptKeepTogether, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'keepWithNext' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_KeepWithNext()
		{
			CheckDeserializeIntProp("keepWithNext", "1", FwTextPropType.ktptKeepWithNext, 1, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("keepWithNext", "0", FwTextPropType.ktptKeepWithNext, 0, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'keepWithNext' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_KeepWithNext_Fail()
		{
			CheckDeserializeIntProp("keepWithNext", "monkey", FwTextPropType.ktptKeepWithNext, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'leadingIndent' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_LeadingIndent()
		{
			CheckDeserializeIntProp("leadingIndent", "0", FwTextPropType.ktptLeadingIndent, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("leadingIndent", "12", FwTextPropType.ktptLeadingIndent, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'leadingIndent' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_LeadingIndent_Fail()
		{
			CheckDeserializeIntProp("leadingIndent", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'lineHeight' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_LineHeight()
		{
			CheckDeserializeIntProp("lineHeight='5'", FwTextPropType.ktptLineHeight, 5, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("lineHeight='10' lineHeightUnit='mpt'", FwTextPropType.ktptLineHeight, 10, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("lineHeight='15' lineHeightType='exact'", FwTextPropType.ktptLineHeight, -15, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("lineHeight='20' lineHeightUnit='rel'", FwTextPropType.ktptLineHeight, 20, FwTextPropVar.ktpvRelative);
			CheckDeserializeIntProp("lineHeight='25' lineHeightUnit='rel' lineHeightType='exact'", FwTextPropType.ktptLineHeight, 25, FwTextPropVar.ktpvRelative);
			CheckDeserializeIntProp("lineHeight='30' lineHeightType='atLeast'", FwTextPropType.ktptLineHeight, 30, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'lineHeight' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_LineHeight_Fail1()
		{
			CheckDeserializeIntProp("lineHeight='-10' lineHeightUnit='mpt'", FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'lineHeight' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_LineHeight_Fail2()
		{
			CheckDeserializeIntProp("lineHeight='10' lineHeightUnit='monkey'", FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'lineHeight' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_LineHeight_Fail3()
		{
			CheckDeserializeIntProp("lineHeight='10' lineHeightType='monkey'", FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'link' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Link()
		{
			CheckDeserializeStrObjProp("link", FwObjDataTypes.kodtNameGuidHot, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginBottom' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MarginBottom()
		{
			CheckDeserializeIntProp("marginBottom", "0", FwTextPropType.ktptMarginBottom, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("marginBottom", "12", FwTextPropType.ktptMarginBottom, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginBottom' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_MarginBottom_Fail()
		{
			CheckDeserializeIntProp("marginBottom", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginLeading' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MarginLeading()
		{
			CheckDeserializeIntProp("marginLeading", "0", FwTextPropType.ktptMarginLeading, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("marginLeading", "12", FwTextPropType.ktptMarginLeading, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginLeading' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_MarginLeading_Fail()
		{
			CheckDeserializeIntProp("marginLeading", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginTop' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MarginTop()
		{
			CheckDeserializeIntProp("marginTop", "0", FwTextPropType.ktptMarginTop, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("marginTop", "12", FwTextPropType.ktptMarginTop, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginTop' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_MarginTop_Fail()
		{
			CheckDeserializeIntProp("marginTop", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginTrailing' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MarginTrailing()
		{
			CheckDeserializeIntProp("marginTrailing", "0", FwTextPropType.ktptMarginTrailing, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("marginTrailing", "12", FwTextPropType.ktptMarginTrailing, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'marginTrailing' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_MarginTrailing_Fail()
		{
			CheckDeserializeIntProp("marginTrailing", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'moveableObj' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_MoveableObj()
		{
			CheckDeserializeStrObjProp("moveableObj", FwObjDataTypes.kodtGuidMoveableObjDisp, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with an 'offset' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Offset()
		{
			CheckDeserializeIntProp("offset='5'", FwTextPropType.ktptOffset, 5, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("offset='10' offsetUnit='mpt'", FwTextPropType.ktptOffset, 10, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("offset='15' offsetUnit='rel'", FwTextPropType.ktptOffset, 15, FwTextPropVar.ktpvRelative);
			CheckDeserializeIntProp("offset='20mpt'", FwTextPropType.ktptOffset, 20, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'offset' attribute with a negative
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_NegativeOffset()
		{
			CheckDeserializeIntProp("offset='-10' offsetUnit='mpt'", FwTextPropType.ktptOffset, -10, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'offset' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Offset_Fail2()
		{
			CheckDeserializeIntProp("offset='10' offsetUnit='monkey'", FwTextPropType.ktptFontSize, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'ownlink' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Ownlink()
		{
			CheckDeserializeStrObjProp("ownlink", FwObjDataTypes.kodtOwnNameGuidHot, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padBottom' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_PadBottom()
		{
			CheckDeserializeIntProp("padBottom", "0", FwTextPropType.ktptPadBottom, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("padBottom", "12", FwTextPropType.ktptPadBottom, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padBottom' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_PadBottom_Fail()
		{
			CheckDeserializeIntProp("padBottom", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padLeading' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_PadLeading()
		{
			CheckDeserializeIntProp("padLeading", "0", FwTextPropType.ktptPadLeading, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("padLeading", "12", FwTextPropType.ktptPadLeading, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padLeading' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_PadLeading_Fail()
		{
			CheckDeserializeIntProp("padLeading", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padTop' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_PadTop()
		{
			CheckDeserializeIntProp("padTop", "0", FwTextPropType.ktptPadTop, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("padTop", "12", FwTextPropType.ktptPadTop, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padTop' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_PadTop_Fail()
		{
			CheckDeserializeIntProp("padTop", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padTrailing' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_PadTrailing()
		{
			CheckDeserializeIntProp("padTrailing", "0", FwTextPropType.ktptPadTrailing, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("padTrailing", "12", FwTextPropType.ktptPadTrailing, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'padTrailing' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_PadTrailing_Fail()
		{
			CheckDeserializeIntProp("padTrailing", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'paracolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Paracolor()
		{
			CheckDeserializeIntProp("paracolor", "red", FwTextPropType.ktptParaColor, 0x0000FF);
			CheckDeserializeIntProp("paracolor", "white", FwTextPropType.ktptParaColor, 0xFFFFFF);
			CheckDeserializeIntProp("paracolor", "blue", FwTextPropType.ktptParaColor, 0xFF0000);
			CheckDeserializeIntProp("paracolor", "F8F105", FwTextPropType.ktptParaColor, 0x05F1F8);
			CheckDeserializeIntProp("paracolor", "transparent", FwTextPropType.ktptParaColor,
				(int)FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'paracolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Paracolor_Fail()
		{
			CheckDeserializeIntProp("paracolor", "monkey", FwTextPropType.ktptParaColor, 0x0000FF);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'rightToLeft' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_RightToLeft()
		{
			CheckDeserializeIntProp("rightToLeft", "0", FwTextPropType.ktptRightToLeft, 0, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("rightToLeft", "1", FwTextPropType.ktptRightToLeft, 1, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("rightToLeft", "5", FwTextPropType.ktptRightToLeft, 5, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'rightToLeft' attribute with an
		/// invalid attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_RightToLeft_Fail()
		{
			CheckDeserializeIntProp("rightToLeft", "-1", FwTextPropType.ktptRightToLeft, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'spaceAfter' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_SpaceAfter()
		{
			CheckDeserializeIntProp("spaceAfter", "0", FwTextPropType.ktptSpaceAfter, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("spaceAfter", "12", FwTextPropType.ktptSpaceAfter, 12, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("spaceAfter", "-12", FwTextPropType.ktptSpaceAfter, -12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'spaceBefore' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_SpaceBefore()
		{
			CheckDeserializeIntProp("spaceBefore", "0", FwTextPropType.ktptSpaceBefore, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("spaceBefore", "12", FwTextPropType.ktptSpaceBefore, 12, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("spaceBefore", "-12", FwTextPropType.ktptSpaceBefore, -12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'spellcheck' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Spellcheck()
		{
			CheckDeserializeIntProp("spellcheck", "normal", FwTextPropType.ktptSpellCheck, (int) SpellingModes.ksmNormalCheck, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("spellcheck", "doNotCheck", FwTextPropType.ktptSpellCheck, (int) SpellingModes.ksmDoNotCheck, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("spellcheck", "forceCheck", FwTextPropType.ktptSpellCheck, (int) SpellingModes.ksmForceCheck, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'spellcheck' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Spellcheck_Fail()
		{
			CheckDeserializeIntProp("spellcheck", "monkey", FwTextPropType.ktptSpellCheck, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'superscript' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Superscript()
		{
			CheckDeserializeIntProp("superscript", "off", FwTextPropType.ktptSuperscript, (int) FwSuperscriptVal.kssvOff, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("superscript", "super", FwTextPropType.ktptSuperscript, (int) FwSuperscriptVal.kssvSuper, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("superscript", "sub", FwTextPropType.ktptSuperscript, (int) FwSuperscriptVal.kssvSub, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'superscript' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Superscript_Fail()
		{
			CheckDeserializeIntProp("superscript", "monkey", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'tags' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Tags()
		{
			Guid expectedGuid1 = new Guid("3B88FBA7-10C7-4e14-9EE0-3F0DDA060A0D");
			Guid expectedGuid2 = new Guid("C4F03ECA-BC03-4175-B5AA-13F3ECB7F481");
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop tags='" + expectedGuid1 +
				" " + expectedGuid2 + "'></Prop>", WritingSystemManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);

			Assert.AreEqual("\uFBA7\u3B88\u10C7\u4e14\uE09E\u0D3F\u06DA\u0D0A" +
				"\u3ECA\uC4F0\uBC03\u4175\uAAB5\uF313\uB7EC\u81F4",
				ttp.GetStrPropValue((int) FwTextPropType.ktptTags));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'tags' attribute when the GUIDs are
		/// separated by the letter 'I' (old-style tag strings) (FWR-1118).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Tags_SeparatedWithLetterI()
		{
			Guid expectedGuid1 = new Guid("3B88FBA7-10C7-4e14-9EE0-3F0DDA060A0D");
			Guid expectedGuid2 = new Guid("C4F03ECA-BC03-4175-B5AA-13F3ECB7F481");
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop tags='I" + expectedGuid1 +
				" I" + expectedGuid2 + "'></Prop>", WritingSystemManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);

			Assert.AreEqual("\uFBA7\u3B88\u10C7\u4e14\uE09E\u0D3F\u06DA\u0D0A" +
				"\u3ECA\uC4F0\uBC03\u4175\uAAB5\uF313\uB7EC\u81F4",
				ttp.GetStrPropValue((int) FwTextPropType.ktptTags));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'trailingIndent' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_TrailingIndent()
		{
			CheckDeserializeIntProp("trailingIndent", "0", FwTextPropType.ktptTrailingIndent, 0, FwTextPropVar.ktpvMilliPoint);
			CheckDeserializeIntProp("trailingIndent", "12", FwTextPropType.ktptTrailingIndent, 12, FwTextPropVar.ktpvMilliPoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'trailingIndent' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_TrailingIndent_Fail()
		{
			CheckDeserializeIntProp("trailingIndent", "-1", 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'undercolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Undercolor()
		{
			CheckDeserializeIntProp("undercolor", "red", FwTextPropType.ktptUnderColor, 0x0000FF);
			CheckDeserializeIntProp("undercolor", "white", FwTextPropType.ktptUnderColor, 0xFFFFFF);
			CheckDeserializeIntProp("undercolor", "blue", FwTextPropType.ktptUnderColor, 0xFF0000);
			CheckDeserializeIntProp("undercolor", "F8F105", FwTextPropType.ktptUnderColor, 0x05F1F8);
			CheckDeserializeIntProp("undercolor", "transparent", FwTextPropType.ktptUnderColor,
				(int) FwTextColor.kclrTransparent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'undercolor' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Undercolor_Fail()
		{
			CheckDeserializeIntProp("undercolor", "monkey", FwTextPropType.ktptUnderColor, 0x0000FF);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'underline' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_Underline()
		{
			CheckDeserializeIntProp("underline", "none", FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntNone, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("underline", "single", FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntSingle, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("underline", "double", FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntDouble, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("underline", "dotted", FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntDotted, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("underline", "dashed", FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntDashed, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("underline", "strikethrough", FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntStrikethrough, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("underline", "squiggle", FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntSquiggle, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'underline' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_Underline_Fail()
		{
			CheckDeserializeIntProp("underline", "monkey", FwTextPropType.ktptUnderline, (int) FwUnderlineType.kuntNone, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'widowOrphan' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_WidowOrphan()
		{
			CheckDeserializeIntProp("widowOrphan", "1", FwTextPropType.ktptWidowOrphanControl, 1, FwTextPropVar.ktpvEnum);
			CheckDeserializeIntProp("widowOrphan", "0", FwTextPropType.ktptWidowOrphanControl, 0, FwTextPropVar.ktpvEnum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'widowOrphan' attribute with an
		/// invalid value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_WidowOrphan_Fail()
		{
			CheckDeserializeIntProp("widowOrphan", "monkey", FwTextPropType.ktptWidowOrphanControl, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'wsBase' attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_WsBase()
		{
			CheckDeserializeIntProp("wsBase", "en", FwTextPropType.ktptBaseWs, EnWS);
			CheckDeserializeIntProp("wsBase", "es", FwTextPropType.ktptBaseWs, EsWS);

			// An empty string is supposedly valid (according to the documentation we found),
			// however it looks like the C++ code would, indeed, throw an exception if
			// the value was empty, so we duplicated that behavior instead (since it makes
			// more sense).
			//CheckIntProp("wsBase", string.Empty, (int)FwTextPropType.ktptBaseWs, m_esWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'wsBase' attribute with an invalid
		/// value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(XmlSchemaException))]
		public void DeserializePropsFromXml_WsBase_Fail()
		{
			CheckDeserializeIntProp("wsBase", "fr__X_ETIC", FwTextPropType.ktptBaseWs, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'WsStyles9999' element inside.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_WsStyles9999()
		{
			CheckDeserializeWsStyleProp("<WsProp ws='en' fontsize='80' fontsizeUnit='mpt' forecolor='2f60ff'></WsProp>",
				"\u87c1\u3b8b" + // Writing system HVO for 'en'
				"\u0000" + // Length of font family string (zero)
				"\u0002" + // integer property count (2)
				(char) FwTextPropType.ktptFontSize + (char) FwTextPropVar.ktpvMilliPoint + "\u0050\u0000" +
				(char) FwTextPropType.ktptForeColor + (char) FwTextPropVar.ktpvDefault + "\u602F\u00FF");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DeserializePropsFromXml method with a 'WsStyles9999' element inside with
		/// string properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeserializePropsFromXml_WsStyles9999_WithStrProps()
		{
			CheckDeserializeWsStyleProp("<WsProp ws='en' fontFamily='Times New Roman' fontsize='80' fontsizeUnit='mpt'" +
				" forecolor='2f60ff' fontVariations='185=1,851=2,57=12'></WsProp>",
				"\u87c1\u3b8b" + // Writing system HVO for 'en'
				"\u000F" + // Length of font family string (15)
				"Times New Roman" + // Font family string
				"\uFFFF" + // string property count as negative (-1)
				(char) FwTextPropType.ktptFontVariations +
				"\u0011" + // Length of the string in the string prop (17)
				"185=1,851=2,57=12" + // String value
				"\u0002" + // integer property count (2)
				(char) FwTextPropType.ktptFontSize + (char) FwTextPropVar.ktpvMilliPoint + "\u0050\u0000" +
				(char) FwTextPropType.ktptForeColor + (char) FwTextPropVar.ktpvDefault + "\u602F\u00FF");
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
		private void CheckDeserializeBulFontInfoProp(string bulText, string expectedValue)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop>" + bulText + "</Prop>", WritingSystemManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);
			Assert.AreEqual(expectedValue, ttp.GetStrPropValue((int) FwTextPropType.ktptBulNumFontInfo));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the writing system style prop.
		/// </summary>
		/// <param name="wsStyleText">The writing system style XML text.</param>
		/// <param name="expectedValue">The expected value.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckDeserializeWsStyleProp(string wsStyleText, string expectedValue)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop><WsStyles9999>" +
				wsStyleText + "</WsStyles9999></Prop>", WritingSystemManager);
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
		private void CheckDeserializeStrObjProp(string propName, FwObjDataTypes propType, Guid expectedGuid)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop " + propName + "='" + expectedGuid + "'></Prop>", WritingSystemManager);
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
		private void CheckDeserializeStrProp(string propName, string propValue, FwTextPropType propType, string expectedValue)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml("<Prop " + propName + "='" +
				propValue + "'></Prop>", WritingSystemManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(0, ttp.IntPropCount);
			Assert.AreEqual(1, ttp.StrPropCount);
			Assert.AreEqual(expectedValue, ttp.GetStrPropValue((int) propType));
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
		private void CheckDeserializeIntProp(string propName, string propValue, FwTextPropType propType, int expectedValue, FwTextPropVar expectedVar = FwTextPropVar.ktpvDefault)
		{
			CheckDeserializeIntProp(string.Format("{0}=\"{1}\"", propName, propValue), propType, expectedValue, expectedVar);
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
		private void CheckDeserializeIntProp(string propText, FwTextPropType propType, int expectedValue, FwTextPropVar expectedVar)
		{
			ITsTextProps ttp = TsPropsSerializer.DeserializePropsFromXml(string.Format("<Prop {0}></Prop>", propText), WritingSystemManager);
			Assert.IsNotNull(ttp);
			Assert.AreEqual(1, ttp.IntPropCount);
			Assert.AreEqual(0, ttp.StrPropCount);
			int var;
			Assert.AreEqual(expectedValue, ttp.GetIntPropValues((int) propType, out var));
			Assert.AreEqual((int) expectedVar, var);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the writing system style prop.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="expectedWSStyleText">The expected writing system style XML text.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckSerializeWsStyleProp(string value, string expectedWSStyleText)
		{
			ITsPropsBldr tpb = TsPropsFactory.GetPropsBldr();
			tpb.SetStringValue(FwTextPropType.ktptWsStyle, value);

			string xml = TsPropsSerializer.SerializePropsToXml(tpb.GetTextProps(), WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(string.Format("<Prop><WsStyles9999>{0}</WsStyles9999></Prop>", expectedWSStyleText)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an object property stored in the string props.
		/// </summary>
		/// <param name="propName">Name of the property.</param>
		/// <param name="propType">Type of the object property.</param>
		/// <param name="expectedGuid">The expected GUID in the object property.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckSerializeStrObjProp(string propName, FwObjDataTypes propType, Guid expectedGuid)
		{
			ITsPropsBldr tpb = TsPropsFactory.GetPropsBldr();
			tpb.SetStringValue(FwTextPropType.ktptObjData, CreateObjData(propType, expectedGuid.ToByteArray()));

			string xml = TsPropsSerializer.SerializePropsToXml(tpb.GetTextProps(), WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(string.Format("<Prop {0}=\"{1}\"></Prop>", propName, expectedGuid)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks a string text property.
		/// </summary>
		/// <param name="propType">Type of the property that is put in the text props.</param>
		/// <param name="value">The value in the text props.</param>
		/// <param name="expectedPropName">The expected name of the property in the XML.</param>
		/// <param name="expectedPropValue">The expected value of the property in the XML.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckSerializeStrProp(FwTextPropType propType, string value, string expectedPropName, string expectedPropValue)
		{
			ITsPropsBldr tpb = TsPropsFactory.GetPropsBldr();
			tpb.SetStringValue(propType, value);
			ITsTextProps textProps = tpb.GetTextProps();
			string xml = TsPropsSerializer.SerializePropsToXml(textProps, WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(textProps.StrPropCount == 0 ? "<Prop></Prop>"
				: string.Format("<Prop {0}=\"{1}\"></Prop>", expectedPropName, expectedPropValue)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an integer text property.
		/// </summary>
		/// <param name="propType">Type of the property that is put in the text props.</param>
		/// <param name="value">The value in the text props.</param>
		/// <param name="expectedPropName">The expected name of the property in the XML.</param>
		/// <param name="expectedPropValue">The expected value of the property in the XML.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckSerializeIntProp(FwTextPropType propType, int value, string expectedPropName, string expectedPropValue)
		{
			CheckSerializeIntProp(propType, value, FwTextPropVar.ktpvDefault, string.Format("{0}=\"{1}\"", expectedPropName, expectedPropValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an integer text property.
		/// </summary>
		/// <param name="propType">Type of the property that is put in the text props.</param>
		/// <param name="value">The value in the text props.</param>
		/// <param name="var">The variant in the text props.</param>
		/// <param name="expectedPropName">The expected name of the property in the XML.</param>
		/// <param name="expectedPropValue">The expected value of the property in the XML.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckSerializeIntProp(FwTextPropType propType, int value, FwTextPropVar var, string expectedPropName, string expectedPropValue)
		{
			CheckSerializeIntProp(propType, value, var, string.Format("{0}=\"{1}\"", expectedPropName, expectedPropValue));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks an integer text property.
		/// </summary>
		/// <param name="propType">Type of the property that is put in the text props.</param>
		/// <param name="value">The value in the text props.</param>
		/// <param name="var">The variant in the text props.</param>
		/// <param name="expectedPropText">The string containing the expected property and value in the XML.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckSerializeIntProp(FwTextPropType propType, int value, FwTextPropVar var, string expectedPropText)
		{
			ITsPropsBldr tpb = TsPropsFactory.GetPropsBldr();
			tpb.SetIntValue(propType, var, value);

			string xml = TsPropsSerializer.SerializePropsToXml(tpb.GetTextProps(), WritingSystemManager);
			Assert.That(StripNewLines(xml), Is.EqualTo(string.Format("<Prop {0}></Prop>", expectedPropText)));
		}
		#endregion
	}
}
