// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class AssembledStylesTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		delegate T PropReader<T>(AssembledStyles styles);

		private delegate AssembledStyles StyleDeriver<T>(AssembledStyles styles, T val);
		/// <summary>
		/// Tests that we can set and retrieve some basic formatting properties, that appropriate objects are
		/// distinct, and that appropriate ones are the same. Tests inheritance.
		/// </summary>
		[Test]
		public void BasicProps()
		{
			AssembledStyles astyles = new AssembledStyles();
			var changedStyles = new HashSet<AssembledStyles>();
			changedStyles.Add(astyles);
			TestDeriverEffect(astyles, "<default font>", "Times New Roman", "Arial", (styles) => styles.FaceName,
				(styles, val) => styles.WithFaceName(val), "font face", changedStyles);
			TestDeriverEffect(astyles, (int)VwFontWeight.kvfwNormal, 700, 200, (styles) => styles.FontWeight,
				(styles, val) => styles.WithFontWeight(val), "weight", changedStyles);
			TestDeriverEffect(astyles, 0, 1, 2, (styles) => styles.Ws, (styles, val) => styles.WithWs(val), "Ws", changedStyles);
			TestDeriverEffect(astyles, 10000, 12000, 24000, (styles) => styles.FontSize,
				(styles, val) => styles.WithFontSize(val), "Font Height", changedStyles);
			TestDeriverEffect(astyles, 0, 10000, 2000, (styles) => styles.BaselineOffset,
				(styles, val) => styles.WithBaselineOffset(val), "Baseline Offset", changedStyles);
			TestDeriverEffect(astyles, 0, 10000, 20000, (styles) => styles.LineHeight,
				(styles, val) => styles.WithLineHeight(val), "Line Spacing", changedStyles);
			TestDeriverEffect(astyles, FwUnderlineType.kuntNone, FwUnderlineType.kuntSingle, FwUnderlineType.kuntSquiggle,
				(styles) => styles.Underline,
				(styles, val) => styles.WithUnderline(val), "Underline", changedStyles);
			TestDeriverEffect(astyles, Color.Black.ToArgb(), Color.Red.ToArgb(), Color.Blue.ToArgb(),
				(styles => styles.ForeColor.ToArgb()),
				(styles, val) => styles.WithForeColor(Color.FromArgb(val)), "foreground color", changedStyles);
			TestDeriverEffect(astyles, Color.Transparent.ToArgb(), Color.Red.ToArgb(), Color.Blue.ToArgb(),
				(styles => styles.BackColor.ToArgb()),
				(styles, val) => styles.WithBackColor(Color.FromArgb(val)), "background color", changedStyles);
			TestDeriverEffect(astyles, Color.Black.ToArgb(), Color.Red.ToArgb(), Color.Blue.ToArgb(),
				(styles => styles.UnderlineColor.ToArgb()),
				(styles, val) => styles.WithUnderlineColor(Color.FromArgb(val)), "underline color", changedStyles);
			TestDeriverEffect(astyles, Color.Black.ToArgb(), Color.Red.ToArgb(), Color.Blue.ToArgb(),
				(styles => styles.BorderColor.ToArgb()),
				(styles, val) => styles.WithBorderColor(Color.FromArgb(val)), "border color", changedStyles);
			TestDeriverEffect(astyles, Thickness.Default, new Thickness(1.0), new Thickness(2.0),
				styles => styles.Margins, (styles, val) => styles.WithMargins(val), "margins", changedStyles);
			TestDeriverEffect(astyles, Thickness.Default, new Thickness(1.0), new Thickness(2.0),
				styles => styles.Pads, (styles, val) => styles.WithPads(val), "pads", changedStyles);
			TestDeriverEffect(astyles, Thickness.Default, new Thickness(1.0), new Thickness(2.0),
				styles => styles.Borders, (styles, val) => styles.WithBorders(val), "borders", changedStyles);
			TestDeriverEffect(astyles, false, true, false,
				styles => styles.RightToLeft, (styles, val) => styles.WithRightToLeft(val), "right to left", changedStyles);
			// Todo: many other properties, including using Thickness to implement Border, Margin, and Padding.

			// Italic is special because of handling invert, also because only two values are possible for the result.
			Assert.That(astyles.FontItalic, Is.False, "default italics");
			var derived = astyles.WithFontItalic(FwTextToggleVal.kttvForceOn);
			Assert.That(derived.FontItalic, Is.True);
			var derived2 = astyles.WithFontItalic(FwTextToggleVal.kttvInvert);
			Assert.That(derived2.FontItalic, Is.True);
			var derived3 = derived.WithFontItalic(FwTextToggleVal.kttvInvert);
			Assert.That(derived3.FontItalic, Is.False);
			Assert.That(ReferenceEquals(derived, derived2), Is.True);
			Assert.That(ReferenceEquals(astyles, derived3), Is.True);
			int oldCount = changedStyles.Count;
			changedStyles.Add(derived2);
			Assert.That(changedStyles.Count, Is.EqualTo(oldCount + 1), "italic styles not equal to any other");

			// Check the conversion of colors.
			var colored = astyles.WithForeColor(Color.Red).WithBackColor(Color.Yellow).WithUnderlineColor(Color.Green);
			var chrp = colored.Chrp;
			Assert.That(chrp.clrFore, Is.EqualTo(ColorUtil.ConvertColorToBGR(Color.Red)));
			Assert.That(chrp.clrBack, Is.EqualTo(ColorUtil.ConvertColorToBGR(Color.Yellow)));
			Assert.That(chrp.clrUnder, Is.EqualTo(ColorUtil.ConvertColorToBGR(Color.Green)));

			// Check the special deriver for FontBold
			var bold = astyles.WithFontBold(FwTextToggleVal.kttvForceOn);
			Assert.That(bold.Chrp.ttvBold, Is.EqualTo((int)FwTextToggleVal.kttvForceOn));
			Assert.That(bold.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));
			var normal = bold.WithFontBold(FwTextToggleVal.kttvInvert);
			Assert.That(normal.Chrp.ttvBold, Is.EqualTo((int)FwTextToggleVal.kttvOff));
			Assert.That(normal.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwNormal));
			Assert.That(normal, Is.EqualTo(astyles));
			Assert.That(astyles.WithFontBold(FwTextToggleVal.kttvInvert), Is.EqualTo(bold));
		}

		void TestDeriverEffect<T>(AssembledStyles astyles, T defVal, T newVal, T otherVal,
			PropReader<T> reader, StyleDeriver<T> deriver, string label, HashSet<AssembledStyles> outputStyles)
		{
			Assert.AreEqual(defVal, reader(astyles), "default " + label + " should be normal");
			AssembledStyles derivedStyles = deriver(astyles, newVal);
			int oldCount = outputStyles.Count;
			outputStyles.Add(derivedStyles);
			Assert.AreEqual(oldCount + 1, outputStyles.Count, "new derived styles should not be equal to any earlier one");
			Assert.AreEqual(newVal, reader(derivedStyles), "derived AS should have " + label + " property set");
			Assert.AreEqual(defVal, reader(astyles), "original AS should not be changed by With... - " + label);
			Assert.AreNotEqual(astyles, derivedStyles, "two different AS's should not be equal");

			AssembledStyles derivedStyles2 = deriver(astyles, newVal);
			Assert.IsTrue(Object.ReferenceEquals(derivedStyles2, derivedStyles), "two astyles derived the same way should be the same object - " + label);

			AssembledStyles derivedStyles3 = deriver(derivedStyles2, otherVal);
			AssembledStyles derivedStyles4 = deriver(astyles, otherVal);
			Assert.IsTrue(Object.ReferenceEquals(derivedStyles3, derivedStyles4), "two equivalent astyles derived differently should be the same object - " + label);
			var rawDerived = new AssembledStyles(derivedStyles2);
			Assert.AreEqual(newVal, reader(rawDerived), "copy consructor should copy " + label + " property");
		}

		[Test]
		public void ApplyingTtp()
		{
			AssembledStyles astyles = new AssembledStyles();
			Assert.That(astyles.Chrp.ttvBold, Is.EqualTo((int)FwTextToggleVal.kttvOff));
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 47);
			bldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
			ITsTextProps props = bldr.GetTextProps();
			AssembledStyles result = astyles.ApplyTextProps(props);
			Assert.AreEqual(47, result.Ws);
			Assert.AreEqual(700, result.FontWeight);
			Assert.That(result.Chrp.ttvBold, Is.EqualTo((int)FwTextToggleVal.kttvForceOn));
		}

		/// <summary>
		/// Validates the Thicknesses class.
		/// Enhance JohnT: should do something about Leading, Trailing.
		/// </summary>
		[Test]
		public void ThicknessesTest()
		{
			var thickness = new Thickness(1.0); // 1.0 96th of inch on all 4 sides
			Assert.AreEqual(1.0, thickness.Leading);
			Assert.AreEqual(1.0, thickness.Top);
			Assert.AreEqual(1.0, thickness.Trailing);
			Assert.AreEqual(1.0, thickness.Bottom);

			var thickness2 = new Thickness(1.0, 2.0, 3.0, 4.0); // 1.0 96th of inch on all 4 sides
			Assert.AreEqual(1.0, thickness2.Leading);
			Assert.AreEqual(2.0, thickness2.Top);
			Assert.AreEqual(3.0, thickness2.Trailing);
			Assert.AreEqual(4.0, thickness2.Bottom);

			Assert.IsFalse(thickness2.Equals(thickness));

			var thickness3 = new Thickness(1.0, 2.0, 3.0, 4.0); // same as thickness2
			Assert.IsTrue(thickness2.Equals(thickness3));
			Assert.IsTrue(thickness3.Equals(thickness2));

			Assert.IsFalse(thickness.Equals(new Thickness(100.0, 1.0, 1.0, 1.0)));
			Assert.IsFalse(thickness.Equals(new Thickness(1.0, 1.3, 1.0, 1.0)));
			Assert.IsFalse(thickness.Equals(new Thickness(1.0, 1.0, 1.01, 1.0)));
			Assert.IsFalse(thickness.Equals(new Thickness(1.0, 1.0, 1.0, 1.0001)));

			Assert.IsTrue(thickness3.GetHashCode() == thickness2.GetHashCode());

			Assert.IsFalse(thickness2.Equals(34));
			object temp = thickness3;
			Assert.IsTrue(thickness2.Equals(temp));

			Assert.IsTrue(thickness2 == thickness3);
			Assert.IsFalse(thickness2 != thickness3);

			Assert.IsTrue(thickness != thickness3);
			Assert.IsFalse(thickness == thickness3);
		}

		[Test]
		public void PropSetters()
		{
			// Simple check of two kinds of setters
			var setter = AssembledStyles.FontWeightSetter(700);
			var baseStyle = new AssembledStyles();
			var derivedStyle = baseStyle.WithProperties(setter);
			Assert.That(derivedStyle.FontWeight, Is.EqualTo(700));
			Assert.That(baseStyle.WithProperties(null), Is.EqualTo(baseStyle));

			var setterRed = AssembledStyles.ForeColorSetter(Color.Red);
			var derivedStyle2 = baseStyle.WithProperties(setterRed);
			Assert.That(derivedStyle2.ForeColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));

			// Can't modify a setter that has been used.
			Assert.Throws<ArgumentException>(()=> setter.AppendProp(setterRed));

			// Can combine two setters.
			setter = AssembledStyles.FontWeightSetter(700);
			setter.AppendProp(setterRed);
			var derivedStyle3 = baseStyle.WithProperties(setter);
			Assert.That(derivedStyle3.ForeColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
			Assert.That(derivedStyle3.FontWeight, Is.EqualTo(700));

			// Or even three.
			setter = AssembledStyles.FontWeightSetter(700);
			setterRed = AssembledStyles.ForeColorSetter(Color.Red);
			var setterGreen = AssembledStyles.BackColorSetter(Color.Green);
			setter.AppendProp(setterRed);
			setter.AppendProp(setterGreen);
			var derivedStyle4 = baseStyle.WithProperties(setter);
			Assert.That(derivedStyle4.ForeColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
			Assert.That(derivedStyle4.FontWeight, Is.EqualTo(700));
			Assert.That(derivedStyle4.BackColor.ToArgb(), Is.EqualTo(Color.Green.ToArgb()));

			// All three should be unavailable for modification.
			var anotherSetter = AssembledStyles.FontWeightSetter(600);
			Assert.Throws<ArgumentException>(() => setter.AppendProp(anotherSetter));
			Assert.Throws<ArgumentException>(() => setterRed.AppendProp(anotherSetter));
			Assert.Throws<ArgumentException>(() => setterGreen.AppendProp(anotherSetter));

			// For efficiency we want custom equality on setters.
			var setterCopy = AssembledStyles.FontWeightSetter(700);
			var setterRedCopy = AssembledStyles.ForeColorSetter(Color.Red);
			var setterGreenCopy = AssembledStyles.BackColorSetter(Color.Green);
			setterCopy.AppendProp(setterRedCopy);
			setterCopy.AppendProp(setterGreenCopy);
			Assert.That(setterGreen, Is.EqualTo(setterGreenCopy));
			Assert.That(setter, Is.EqualTo(setterCopy));
			Assert.That(setter.GetHashCode(), Is.EqualTo(setterCopy.GetHashCode()));

			// Minimal testing on the rest
			Assert.That(baseStyle.WithProperties(AssembledStyles.UnderlineColorSetter(Color.Yellow)).UnderlineColor.ToArgb(),
				Is.EqualTo(Color.Yellow.ToArgb()));
			Assert.That(baseStyle.WithProperties(AssembledStyles.UnderlineSetter(FwUnderlineType.kuntStrikethrough)).Underline,
				Is.EqualTo(FwUnderlineType.kuntStrikethrough));
			Assert.That(baseStyle.WithProperties(AssembledStyles.FontSizeSetter(13000)).FontSize,
				Is.EqualTo(13000));
			Assert.That(baseStyle.WithProperties(AssembledStyles.BaselineOffsetSetter(2000)).BaselineOffset,
				Is.EqualTo(2000));
			Assert.That(baseStyle.WithProperties(AssembledStyles.FontItalicSetter(FwTextToggleVal.kttvForceOn)).FontItalic,
				Is.True);
			var italicStyle = baseStyle.WithProperties(AssembledStyles.FontItalicSetter(FwTextToggleVal.kttvInvert));
			Assert.That(italicStyle.FontItalic, Is.True);
			Assert.That(italicStyle.WithProperties(AssembledStyles.FontItalicSetter(FwTextToggleVal.kttvInvert)).FontItalic,
				Is.False);
			Assert.That(baseStyle.WithProperties(AssembledStyles.FaceNameSetter("Symbol")).FaceName,
				Is.EqualTo("Symbol"));
			Assert.That(baseStyle.WithProperties(AssembledStyles.RtlSetter(true)).RightToLeft, Is.True);
		}

		/// <summary>
		/// Test interpreting styles
		/// </summary>
		[Test]
		public void StylesheetTests()
		{
			var stylesheet = new MockStylesheet();
			var styles = new AssembledStyles(stylesheet);
			var styleBold = stylesheet.AddStyle("bold", false);
			var boldFontInfo = new MockCharStyleInfo();
			styleBold.DefaultCharacterStyleInfo = boldFontInfo;
			boldFontInfo.Bold = new MockStyleProp<bool>() {Value = true, ValueIsSet = true};
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "bold");
			var props = bldr.GetTextProps();
			var styles2 = styles.ApplyTextProps(props);
			Assert.That(styles2.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));

			// Another style with the same properties but different name does NOT produce the same AssembledStyles
			var styleBold2 = stylesheet.AddStyle("bold2", false);
			styleBold2.DefaultCharacterStyleInfo = boldFontInfo;
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "bold2");
			props = bldr.GetTextProps();
			var styles2b = styles.ApplyTextProps(props);
			Assert.That(styles2b.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));
			Assert.That(styles2b.StyleName, Is.EqualTo("bold2"));
			Assert.That(styles2b, Is.Not.EqualTo(styles2));

			var styleSize14 = stylesheet.AddStyle("size14", false);
			var size14FontInfo = new MockCharStyleInfo();
			styleSize14.DefaultCharacterStyleInfo = size14FontInfo;
			size14FontInfo.FontSize = new MockStyleProp<int>() {Value = 14000, ValueIsSet = true};
			size14FontInfo.Bold = new MockStyleProp<bool>(); // no value set, should ignore
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "size14");
			props = bldr.GetTextProps();
			var styles3 = styles.ApplyTextProps(props);
			Assert.That(styles3.StyleName, Is.EqualTo("size14"));
			Assert.That(styles3.FontSize, Is.EqualTo(14000));
			Assert.That(styles3.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwNormal));

			// Since styleSize14 does NOT affect bold, applying it to styles2 should yield 14-pt bold
			var styles4 = styles2.ApplyTextProps(props);
			Assert.That(styles4.FontSize, Is.EqualTo(14000));
			Assert.That(styles4.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));

			// Check that we can explicitly turn bold off.
			var styleNotBold = stylesheet.AddStyle("notBold", false);
			var notBoldInfo = new MockCharStyleInfo();
			styleNotBold.DefaultCharacterStyleInfo = notBoldInfo;
			notBoldInfo.Bold = new MockStyleProp<bool>() { Value = false, ValueIsSet = true };
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "notBold");
			props = bldr.GetTextProps();
			var styles5 = styles4.ApplyTextProps(props);
			Assert.That(styles3.FontSize, Is.EqualTo(14000));
			Assert.That(styles3.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwNormal));

			// now try an override font size. size14_16bold is 14 point for most writing systems, 16 point for 37.
			// It is also bold for all WSs.
			var styleSize14_16bold = stylesheet.AddStyle("size14_16bold", false);
			var size14boldFontInfo = new MockCharStyleInfo();
			styleSize14_16bold.DefaultCharacterStyleInfo = size14boldFontInfo;
			size14boldFontInfo.FontSize = new MockStyleProp<int>() { Value = 14000, ValueIsSet = true };
			size14boldFontInfo.Bold = new MockStyleProp<bool>() {Value = true, ValueIsSet = true};
			var size16FontInfo = new MockCharStyleInfo();
			styleSize14_16bold.Overrides[37] = size16FontInfo; // ws 37 should be 16 point
			size16FontInfo.FontSize = new MockStyleProp<int>() { Value = 16000, ValueIsSet = true };
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "size14_16bold");
			props = bldr.GetTextProps();
			var styles6 = styles.ApplyTextProps(props);
			Assert.That(styles6.FontSize, Is.EqualTo(14000));
			Assert.That(styles6.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 33);
			props = bldr.GetTextProps();
			var styles7 = styles.ApplyTextProps(props);
			Assert.That(styles7.FontSize, Is.EqualTo(14000)); // other wss not affected
			Assert.That(styles7.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, 37);
			props = bldr.GetTextProps();
			var styles8 = styles.ApplyTextProps(props);
			Assert.That(styles8.FontSize, Is.EqualTo(16000)); // ws 37 overridden
			Assert.That(styles8.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold)); // bold inherited

			// Test for other font-info properties.
			var stylesFonts = stylesheet.AddStyle("allFontProps", false);
			var allFontPropsFontInfo = new MockCharStyleInfo();
			stylesFonts.DefaultCharacterStyleInfo = allFontPropsFontInfo;
			allFontPropsFontInfo.BackColor = new MockStyleProp<Color>() { Value = Color.Red, ValueIsSet = true };
			allFontPropsFontInfo.FontSize = new MockStyleProp<int>() { Value = 14000, ValueIsSet = true };
			allFontPropsFontInfo.FontColor = new MockStyleProp<Color>() { Value = Color.Blue, ValueIsSet = true };
			allFontPropsFontInfo.Italic = new MockStyleProp<bool>() { Value = true, ValueIsSet = true };
			allFontPropsFontInfo.Offset = new MockStyleProp<int>() { Value = 2000, ValueIsSet = true };
			allFontPropsFontInfo.UnderlineColor = new MockStyleProp<Color>() { Value = Color.Green, ValueIsSet = true };
			allFontPropsFontInfo.FontName = new MockStyleProp<string>() { Value = "MyFont", ValueIsSet = true };
			allFontPropsFontInfo.Underline = new MockStyleProp<FwUnderlineType>() { Value = FwUnderlineType.kuntDashed, ValueIsSet = true };
			//allFontPropsFontInfo.SuperSub = new MockStyleProp<FwSuperscriptVal>() { Value = FwSuperscriptVal.kssvSuper, ValueIsSet = true };
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "allFontProps");
			props = bldr.GetTextProps();
			var styles9 = styles.ApplyTextProps(props);
			Assert.That(styles9.BackColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
			Assert.That(styles9.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwNormal)); // did not set this one, make sure not affected
			Assert.That(styles9.FontSize, Is.EqualTo(14000));
			Assert.That(styles9.ForeColor.ToArgb(), Is.EqualTo(Color.Blue.ToArgb()));
			Assert.That(styles9.FontItalic, Is.EqualTo(true));
			Assert.That(styles9.BaselineOffset, Is.EqualTo(2000));
			Assert.That(styles9.UnderlineColor.ToArgb(), Is.EqualTo(Color.Green.ToArgb()));
			Assert.That(styles9.FaceName, Is.EqualTo("MyFont"));
			Assert.That(styles9.Underline, Is.EqualTo(FwUnderlineType.kuntDashed));
			// Todo: make some use of SuperSub and FontFeatures, when we have the applicable capabilities in AssembledStyles

			// Todo: test for non-font-related properties (in paragraph styles).

			// Todo: test special case of paragraph style that sets ws-dependent properties (and
			// possible ws-dependent (or not) character style that overrides them).

			// Todo: test that style is ignored cleanly if no stylesheet.
		}
	}
}
