// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StyleInfoTableTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System.Drawing;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for BaseStyleInfo class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StyleInfoTableTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ability to connect the styles in the table, setting the inherited values
		/// and the "based-on" and "next" styles properly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConnectStyles()
		{
			IStStyle normal = AddTestStyle("Normal", ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, false);
			IStStyle normalParaStyle = AddTestStyle("Paragraph", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false);
			IStStyle sectionHead = AddTestStyle("Section Head", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			IStStyle verseNumberStyle = AddTestStyle("Verse Number", ContextValues.Text, StructureValues.Body, FunctionValues.Verse, true);
			IStStyle mainTitleStyle = AddTestStyle("Title Main", ContextValues.Title, StructureValues.Body, FunctionValues.Prose, false);
			IStStyle footnoteParaStyle = AddTestStyle("Note General Paragraph", ContextValues.Note, StructureValues.Undefined, FunctionValues.Prose, false);
			IStStyle footnoteMarkerStyle = AddTestStyle("Note Marker", ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, true);

			StyleInfoTable table = new StyleInfoTable(string.Empty,
				Cache.ServiceLocator.WritingSystemManager);
			ITsPropsBldr props;

			props = normal.Rules.GetBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 20000);
			props.SetIntPropValues((int)FwTextPropType.ktptLineHeight,
				(int)FwTextPropVar.ktpvMilliPoint, 16000);
			props.SetIntPropValues((int)FwTextPropType.ktptOffset,
				(int)FwTextPropVar.ktpvMilliPoint, 10000);
			normal.Rules = props.GetTextProps();
			table.Add("Normal", new DummyStyleInfo(normal));

			props = normalParaStyle.Rules.GetBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptBorderLeading,
				(int)FwTextPropVar.ktpvMilliPoint, 1000);
			props.SetIntPropValues((int)FwTextPropType.ktptBorderTop,
				(int)FwTextPropVar.ktpvMilliPoint, 2000);
			props.SetIntPropValues((int)FwTextPropType.ktptBorderTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 3000);
			props.SetIntPropValues((int)FwTextPropType.ktptBorderBottom,
				(int)FwTextPropVar.ktpvMilliPoint, 4000);
			props.SetIntPropValues((int)FwTextPropType.ktptBorderColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Thistle)));
			normalParaStyle.Rules = props.GetTextProps();
			normalParaStyle.BasedOnRA = normal;
			normalParaStyle.NextRA = normalParaStyle;
			table.Add("Paragraph", new DummyStyleInfo(normalParaStyle));

			props = footnoteParaStyle.Rules.GetBldr();
			footnoteParaStyle.BasedOnRA = normalParaStyle;
			footnoteParaStyle.NextRA = null;
			table.Add("Note General Paragraph", new DummyStyleInfo(footnoteParaStyle));

			props = sectionHead.Rules.GetBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptItalic,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvOff);
			props.SetIntPropValues((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Aquamarine)));
			props.SetStrPropValue((int)FwTextPropType.ktptFontFamily, StyleServices.DefaultFont);
			sectionHead.Rules = props.GetTextProps();
			sectionHead.BasedOnRA = normal;
			sectionHead.NextRA = normalParaStyle;
			DummyStyleInfo sectionInfo = new DummyStyleInfo(sectionHead);
			int wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			sectionInfo.FontInfoForWs(wsEn).m_fontName.ExplicitValue = "Arial";
			table.Add("Section Head", sectionInfo);

			props = mainTitleStyle.Rules.GetBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvForceOn);
			props.SetIntPropValues((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvDefault, (int)FwTextAlign.ktalCenter);
			props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 16000);
			props.SetIntPropValues((int)FwTextPropType.ktptFirstIndent,
				(int)FwTextPropVar.ktpvMilliPoint, 0);
			mainTitleStyle.Rules = props.GetTextProps();
			mainTitleStyle.BasedOnRA = sectionHead;
			mainTitleStyle.NextRA = mainTitleStyle;
			table.Add("Title Main", new DummyStyleInfo(mainTitleStyle));

			props = verseNumberStyle.Rules.GetBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvDefault,
				(int)FwTextToggleVal.kttvForceOn);
			props.SetIntPropValues((int)FwTextPropType.ktptItalic,
				(int)FwTextPropVar.ktpvDefault,
				(int)FwTextToggleVal.kttvForceOn);
			props.SetIntPropValues((int)FwTextPropType.ktptSuperscript,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwSuperscriptVal.kssvSuper);
			props.SetIntPropValues((int)FwTextPropType.ktptUnderline,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwUnderlineType.kuntDouble);
			props.SetIntPropValues((int)FwTextPropType.ktptUnderColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.SteelBlue)));
			props.SetIntPropValues((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Tomato)));
			props.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Courier");
			verseNumberStyle.Rules = props.GetTextProps();
			verseNumberStyle.BasedOnRA = null;
			verseNumberStyle.NextRA = null;
			table.Add("Verse Number", new DummyStyleInfo(verseNumberStyle));

			footnoteMarkerStyle.BasedOnRA = verseNumberStyle;
			footnoteMarkerStyle.NextRA = null;
			table.Add("Note Marker", new DummyStyleInfo(footnoteMarkerStyle));

			table.ConnectStyles();

			// Check "Paragraph" style
			//		Explicit properties
			DummyStyleInfo entry = (DummyStyleInfo)table["Paragraph"];
			Assert.AreEqual("Paragraph", entry.Name);
			Assert.IsTrue(entry.IsParagraphStyle);
			Assert.AreEqual(table["Normal"].StyleNumber, entry.BasedOnStyleNumber);
			Assert.AreEqual(entry.StyleNumber, entry.NextStyleNumber);
			Assert.AreEqual(1000, entry.BorderLeading);
			Assert.AreEqual(2000, entry.BorderTop);
			Assert.AreEqual(3000, entry.BorderTrailing);
			Assert.AreEqual(4000, entry.BorderBottom);
			Assert.AreEqual((Color.FromKnownColor(KnownColor.Thistle)).ToArgb(), entry.BorderColor.ToArgb());
			//		Inherited properties
			FontInfo fontInfo = entry.FontInfoForWs(-1);
			Assert.IsNotNull(fontInfo);
			Assert.IsFalse(entry.ExplicitRightToLeftStyle);
			Assert.IsFalse(fontInfo.m_italic.Value);
			Assert.IsFalse(fontInfo.m_bold.Value);
			Assert.AreEqual(20000, fontInfo.m_fontSize.Value);
			Assert.AreEqual(10000, fontInfo.m_offset.Value);
			Assert.AreEqual(FwUnderlineType.kuntNone, fontInfo.m_underline.Value);
			Assert.IsTrue(fontInfo.m_underlineColor.IsInherited);
			Assert.AreEqual(Color.Black, fontInfo.m_underlineColor.Value);
			Assert.IsTrue(fontInfo.m_features.IsInherited);
			Assert.AreEqual(null, fontInfo.m_features.Value);
			Assert.IsTrue(fontInfo.m_backColor.IsInherited);
			Assert.AreEqual(Color.Empty, fontInfo.m_backColor.Value);
			Assert.IsTrue(fontInfo.m_fontColor.IsInherited);
			Assert.AreEqual(Color.Black, fontInfo.m_fontColor.Value);
			Assert.AreEqual("<default font>", fontInfo.m_fontName.Value);
			Assert.AreEqual(FwSuperscriptVal.kssvOff, fontInfo.m_superSub.Value);
			Assert.AreEqual(FwTextAlign.ktalLeading, entry.Alignment);
			Assert.AreEqual(0, entry.FirstLineIndent);
			Assert.AreEqual(16000, entry.LineSpacing.m_lineHeight);
			Assert.AreEqual(0, entry.LeadingIndent);
			Assert.AreEqual(0, entry.TrailingIndent);
			Assert.AreEqual(0, entry.SpaceAfter);
			Assert.AreEqual(0, entry.SpaceBefore);

			// Check Normal Footnote Paragraph style
			//		Explicit properties
			entry = (DummyStyleInfo)table["Note General Paragraph"];
			Assert.AreEqual("Note General Paragraph", entry.Name);
			Assert.IsTrue(entry.IsParagraphStyle);
			Assert.AreEqual(table["Paragraph"].StyleNumber, entry.BasedOnStyleNumber);
			Assert.AreEqual("Note General Paragraph", entry.NextStyle.Name);
			//		Inherited properties
			fontInfo = entry.FontInfoForWs(-1);
			Assert.IsNotNull(fontInfo);
			Assert.IsFalse(entry.ExplicitRightToLeftStyle);
			Assert.IsFalse(fontInfo.m_italic.Value);
			Assert.IsFalse(fontInfo.m_bold.Value);
			Assert.AreEqual(20000, fontInfo.m_fontSize.Value);
			Assert.AreEqual(10000, fontInfo.m_offset.Value);
			Assert.AreEqual(FwUnderlineType.kuntNone, fontInfo.m_underline.Value);
			Assert.AreEqual(Color.Black, fontInfo.m_underlineColor.Value);
			Assert.IsNull(fontInfo.m_features.Value);
			Assert.AreEqual(Color.Empty, fontInfo.m_backColor.Value);
			Assert.AreEqual(Color.Black, fontInfo.m_fontColor.Value);
			Assert.AreEqual("<default font>", fontInfo.m_fontName.Value);
			Assert.AreEqual(FwSuperscriptVal.kssvOff, fontInfo.m_superSub.Value);
			Assert.AreEqual(FwTextAlign.ktalLeading, entry.Alignment);
			Assert.AreEqual(0, entry.FirstLineIndent);
			Assert.AreEqual(16000, entry.LineSpacing.m_lineHeight);
			Assert.AreEqual(0, entry.LeadingIndent);
			Assert.AreEqual(0, entry.TrailingIndent);
			Assert.AreEqual(0, entry.SpaceAfter);
			Assert.AreEqual(0, entry.SpaceBefore);
			Assert.AreEqual(1000, entry.BorderLeading);
			Assert.AreEqual(2000, entry.BorderTop);
			Assert.AreEqual(3000, entry.BorderTrailing);
			Assert.AreEqual(4000, entry.BorderBottom);
			Assert.AreEqual((Color.FromKnownColor(KnownColor.Thistle)).ToArgb(), entry.BorderColor.ToArgb());

			// Check Title Main style
			//		Explicit properties
			entry = (DummyStyleInfo)table["Title Main"];
			Assert.AreEqual("Title Main", entry.Name);
			Assert.IsTrue(entry.IsParagraphStyle);
			Assert.AreEqual(FwTextAlign.ktalCenter, entry.Alignment);
			Assert.AreEqual(0, entry.FirstLineIndent);
			fontInfo = entry.FontInfoForWs(-1);
			Assert.IsNotNull(fontInfo);
			Assert.IsTrue(fontInfo.m_bold.Value);
			Assert.AreEqual(16000, fontInfo.m_fontSize.Value);
			Assert.AreEqual(table["Section Head"].StyleNumber, entry.BasedOnStyleNumber);
			Assert.AreEqual(entry.StyleNumber, entry.NextStyleNumber);
			//		Inherited properties
			Assert.IsFalse(entry.ExplicitRightToLeftStyle);
			Assert.IsFalse(fontInfo.m_italic.Value);
			Assert.AreEqual(10000, fontInfo.m_offset.Value);
			Assert.AreEqual(FwUnderlineType.kuntNone, fontInfo.m_underline.Value);
			Assert.AreEqual(Color.Black, fontInfo.m_underlineColor.Value);
			Assert.IsNull(fontInfo.m_features.Value);
			Assert.AreEqual((Color.FromKnownColor(KnownColor.Aquamarine)).ToArgb(), fontInfo.m_backColor.Value.ToArgb());
			Assert.AreEqual(Color.Black, fontInfo.m_fontColor.Value);
			Assert.AreEqual("<default font>", fontInfo.m_fontName.Value);
			Assert.AreEqual(16000, entry.LineSpacing.m_lineHeight);
			Assert.AreEqual(0, entry.LeadingIndent);
			Assert.AreEqual(0, entry.TrailingIndent);
			Assert.AreEqual(0, entry.SpaceAfter);
			Assert.AreEqual(0, entry.SpaceBefore);
			Assert.AreEqual(FwSuperscriptVal.kssvOff, fontInfo.m_superSub.Value);
			fontInfo = entry.FontInfoForWs(wsEn);
			Assert.AreEqual("Arial", fontInfo.m_fontName.Value);


			// Check Section Head style
			//		Explicit properties
			entry = (DummyStyleInfo)table["Section Head"];
			Assert.AreEqual("Section Head", entry.Name);
			Assert.IsTrue(entry.IsParagraphStyle);
			fontInfo = entry.FontInfoForWs(-1);
			Assert.IsNotNull(fontInfo);
			Assert.IsFalse(fontInfo.m_italic.Value);
			Assert.AreEqual(FwUnderlineType.kuntNone, fontInfo.m_underline.Value);
			Assert.AreEqual(Color.Black, fontInfo.m_underlineColor.Value);
			Assert.IsNull(fontInfo.m_features.Value);
			Assert.AreEqual((Color.FromKnownColor(KnownColor.Aquamarine)).ToArgb(), fontInfo.m_backColor.Value.ToArgb());
			Assert.AreEqual("<default font>", fontInfo.m_fontName.Value);
			Assert.AreEqual(table["Normal"].StyleNumber, entry.BasedOnStyleNumber);
			Assert.AreEqual(table["Paragraph"].StyleNumber, entry.NextStyleNumber);
			//		Inherited properties
			Assert.IsFalse(entry.ExplicitRightToLeftStyle);
			Assert.AreEqual(FwTextAlign.ktalLeading, entry.Alignment);
			Assert.AreEqual(0, entry.FirstLineIndent);
			Assert.IsFalse(fontInfo.m_bold.Value);
			Assert.AreEqual(20000, fontInfo.m_fontSize.Value);
			Assert.AreEqual(10000, fontInfo.m_offset.Value);
			Assert.AreEqual(Color.Black, fontInfo.m_fontColor.Value);
			Assert.AreEqual(16000, entry.LineSpacing.m_lineHeight);
			Assert.AreEqual(0, entry.LeadingIndent);
			Assert.AreEqual(0, entry.TrailingIndent);
			Assert.AreEqual(0, entry.SpaceAfter);
			Assert.AreEqual(0, entry.SpaceBefore);
			Assert.AreEqual(FwSuperscriptVal.kssvOff, fontInfo.m_superSub.Value);

			// Check Verse Number style
			//		Explicit properties
			entry = (DummyStyleInfo)table["Verse Number"];
			Assert.AreEqual("Verse Number", entry.Name);
			Assert.IsFalse(entry.IsParagraphStyle);
			fontInfo = entry.FontInfoForWs(-1);
			Assert.IsNotNull(fontInfo);
			Assert.IsTrue(fontInfo.m_bold.Value);
			Assert.IsTrue(fontInfo.m_italic.Value);
			Assert.AreEqual(FwSuperscriptVal.kssvSuper, fontInfo.m_superSub.Value);
			Assert.AreEqual(FwUnderlineType.kuntDouble, fontInfo.m_underline.Value);
			Assert.AreEqual((Color.FromKnownColor(KnownColor.SteelBlue)).ToArgb(), fontInfo.m_underlineColor.Value.ToArgb());
			Assert.AreEqual((Color.FromKnownColor(KnownColor.Tomato)).ToArgb(), fontInfo.m_fontColor.Value.ToArgb());
			Assert.AreEqual("Courier", fontInfo.m_fontName.Value);
			Assert.AreEqual(0, entry.BasedOnStyleNumber);
			//		Inherited properties
			Assert.IsTrue(fontInfo.m_fontSize.IsInherited);
			Assert.IsTrue(fontInfo.m_offset.IsInherited);

			// Check Footnote Marker style
			//		Explicit properties
			entry = (DummyStyleInfo)table["Note Marker"];
			Assert.AreEqual("Note Marker", entry.Name);
			Assert.IsFalse(entry.IsParagraphStyle);
			Assert.AreEqual(table["Verse Number"].StyleNumber, entry.BasedOnStyleNumber);
			//		Inherited properties
			fontInfo = entry.FontInfoForWs(-1);
			Assert.IsNotNull(fontInfo);
			Assert.IsTrue(fontInfo.m_bold.Value);
			Assert.IsTrue(fontInfo.m_italic.Value);
			Assert.AreEqual(FwSuperscriptVal.kssvSuper, fontInfo.m_superSub.Value);
			Assert.AreEqual(FwUnderlineType.kuntDouble, fontInfo.m_underline.Value);
			Assert.AreEqual((Color.FromKnownColor(KnownColor.SteelBlue)).ToArgb(), fontInfo.m_underlineColor.Value.ToArgb());
			Assert.AreEqual((Color.FromKnownColor(KnownColor.Tomato)).ToArgb(), fontInfo.m_fontColor.Value.ToArgb());
			Assert.AreEqual("Courier", fontInfo.m_fontName.Value);
			//		Inherited properties
			Assert.IsTrue(fontInfo.m_fontSize.IsInherited);
			Assert.IsTrue(fontInfo.m_offset.IsInherited);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style and add it to the Language Project stylesheet.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <returns>The style</returns>
		/// ------------------------------------------------------------------------------------
		public IStStyle AddTestStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle)
		{
			return AddTestStyle(name, context, structure, function, isCharStyle, Cache.LangProject.StylesOC);
		}
	}
}
