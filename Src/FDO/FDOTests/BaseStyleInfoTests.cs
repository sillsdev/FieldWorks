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
// File: BaseStyleInfoTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region DummyStyleInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class lives to expose a gazillion protected properties
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyStyleInfo : BaseStyleInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyStyleInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyStyleInfo() : base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyStyleInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyStyleInfo(IStStyle style) : base(style)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyStyleInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyStyleInfo(FdoCache cache)
			: base(cache)
		{
		}

		/// <summary>Alignment</summary>
		public InheritableStyleProp<FwTextAlign> InheritableAlignment
		{
			get { return base.m_alignment; }
		}

		/// <summary>Inter-line spacing in millipoints</summary>
		public InheritableStyleProp<LineHeightInfo> InheritableLineSpacing
		{
			get { return base.m_lineSpacing; }
		}

		/// <summary>Space above paragraph in millipoints</summary>
		public InheritableStyleProp<int> InheritableSpaceBefore
		{
			get { return base.m_spaceBefore; }
		}

		/// <summary>Space below paragraph in millipoints</summary>
		public InheritableStyleProp<int> InheritableSpaceAfter
		{
			get { return base.m_spaceAfter; }
		}

		/// <summary>Indentation of first line in millipoints</summary>
		public InheritableStyleProp<int> InheritableFirstLineIndent
		{
			get { return base.m_firstLineIndent; }
		}

		/// <summary>Indentation of paragraph from leading edge in millipoints</summary>
		public InheritableStyleProp<int> InheritableLeadingIndent
		{
			get { return base.m_leadingIndent; }
		}

		/// <summary>Indentation of paragraph from trailing edge in millipoints</summary>
		public InheritableStyleProp<int> InheritableTrailingIndent
		{
			get { return base.m_trailingIndent; }
		}

		/// <summary>Thickness of borders</summary>
		public InheritableStyleProp<BorderThicknesses> InheritableBorder
		{
			get { return base.m_border; }
		}

		/// <summary>Color of borders (ARGB)</summary>
		public InheritableStyleProp<Color> InheritableBorderColor
		{
			get { return base.m_borderColor; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the process ws specific overrides.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<FontOverrideInfo> CallProcessWsSpecificOverrides(byte[] source)
		{
			return base.ProcessWsSpecificOverrides(MakeStringFromBuffer(source));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a string from buffer.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string MakeStringFromBuffer(byte[] buffer)
		{
			StringBuilder bldr = new StringBuilder(buffer.Length / 2);
			for (int i = 0; i < buffer.Length; i += 2)
				bldr.Append((char)(buffer[i] + (buffer[i + 1] << 8)));
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The description of the usage of the style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new string Usage
		{
			get { return m_usage; }
			set { m_usage = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Context
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new ContextValues Context
		{
			get { return m_context; }
			set { m_context = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Structure
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new StructureValues Structure
		{
			get { return m_structure; }
			set { m_structure = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Function
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new FunctionValues Function
		{
			get { return m_function; }
			set { m_function = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates the derived (from inheritance) value of whether this style is
		/// right-to-left or not
		/// </summary>
		/// <value><c>true</c> if right-to-left; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ExplicitRightToLeftStyle
		{
			get { return m_rtl.Value == TriStateBool.triTrue; }
			set { m_rtl.ExplicitValue = (value ? TriStateBool.triTrue : TriStateBool.triFalse); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the based on style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new BaseStyleInfo BasedOnStyle
		{
			get { return m_basedOnStyle; }
			set { m_basedOnStyle = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the next style.
		/// </summary>
		/// <value>The next style.</value>
		/// ------------------------------------------------------------------------------------
		public new BaseStyleInfo NextStyle
		{
			get { return m_nextStyle; }
			set { m_nextStyle = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets a value indicating whether this style is built in.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool IsBuiltIn
		{
			get { return m_isBuiltIn; }
			set { m_isBuiltIn = value; }
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for BaseStyleInfo class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BaseStyleInfoTests : ScrInMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the copy.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateCopy()
		{
			IStStyle testStyle = AddTestStyle("Title Main", ContextValues.Title,
				StructureValues.Body, FunctionValues.Prose, false, m_scr.StylesOC);

			BaseStyleInfo basedOnInfo = new BaseStyleInfo();
			DummyStyleInfo origInfo = new DummyStyleInfo();
			origInfo.Name = "original";
			origInfo.Usage = "This is the original style";
			origInfo.BasedOnStyle = basedOnInfo;
			origInfo.NextStyle = origInfo;
			origInfo.IsParagraphStyle = true;
			origInfo.Context = ContextValues.Publication;
			origInfo.Structure = StructureValues.Heading;
			origInfo.Function = FunctionValues.List;
			origInfo.ExplicitRightToLeftStyle = false;
			origInfo.UserLevel = 2;
			origInfo.RealStyle = testStyle;
			origInfo.IsBuiltIn = true;

			BaseStyleInfo newInfo = new BaseStyleInfo(origInfo, "new");
			Assert.AreEqual("new", newInfo.Name);
			Assert.AreEqual(origInfo.Usage, newInfo.Usage);
			Assert.AreEqual(origInfo.BasedOnStyle, newInfo.BasedOnStyle);
			Assert.AreEqual(origInfo.NextStyle, newInfo.NextStyle);
			Assert.AreEqual(origInfo.IsParagraphStyle, newInfo.IsParagraphStyle);
			Assert.AreEqual(origInfo.Context, newInfo.Context);
			Assert.AreEqual(origInfo.Structure, newInfo.Structure);
			Assert.AreEqual(origInfo.Function, newInfo.Function);
			Assert.AreEqual(TriStateBool.triFalse, newInfo.DirectionIsRightToLeft);
			Assert.AreEqual(origInfo.UserLevel, newInfo.UserLevel);

			Assert.AreEqual(null, newInfo.RealStyle, "a copy of a style should not have a DB style backing it");
			Assert.AreEqual(false, newInfo.IsBuiltIn, "Copies of styles should not be considered built in");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ability to construct a style info object based on an StStyle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConstructBasedOnStyle()
		{
			ITsPropsBldr props;

			IStStyle mainTitleStyle = AddTestStyle("Title Main", ContextValues.Title,
				StructureValues.Body, FunctionValues.Prose, false, Cache.LangProject.StylesOC);
			props = mainTitleStyle.Rules.GetBldr();
			props.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvDefault, 1);
			props.SetIntPropValues((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvDefault,
				(int)FwTextAlign.ktalCenter);
			props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 20000);
			props.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");
			mainTitleStyle.Rules = props.GetTextProps();

			DummyStyleInfo entry = new DummyStyleInfo(mainTitleStyle);
			Assert.AreEqual("Title Main", entry.Name);
			// Was: Assert.AreEqual(mainTitleStyle.Usage.UserDefaultWritingSystem, entry.Usage);
			// Since nothing in our test has initialized mainTitleStyle.Usage, it used to be null, like
			// entry.Usage. A change to the implementation of UserDefaultWritingSystem causes it to always
			// return something.
			Assert.AreEqual(mainTitleStyle.Usage.RawUserDefaultWritingSystem.Text, entry.Usage);
			Assert.IsTrue(entry.IsParagraphStyle);
			Assert.AreEqual(FwTextAlign.ktalCenter, entry.Alignment);
			FontInfo fontInfo = entry.FontInfoForWs(-1);
			Assert.IsNotNull(fontInfo);
			Assert.IsTrue(fontInfo.m_bold.Value);
			Assert.AreEqual(20000, fontInfo.m_fontSize.Value);
			Assert.AreEqual("Arial", (string)fontInfo.m_fontName.Value);
			Assert.AreEqual(ContextValues.Title, entry.Context);
			Assert.AreEqual(StructureValues.Body, entry.Structure);
			Assert.AreEqual(FunctionValues.Prose, entry.Function);

			// Check that everything else is inherited
			Assert.IsTrue(fontInfo.m_backColor.IsInherited);
			Assert.IsTrue(fontInfo.m_fontColor.IsInherited);
			Assert.IsTrue(fontInfo.m_italic.IsInherited);
			Assert.IsTrue(fontInfo.m_superSub.IsInherited);
			Assert.IsTrue(fontInfo.m_underline.IsInherited);
			Assert.IsTrue(fontInfo.m_underlineColor.IsInherited);
			Assert.IsTrue(fontInfo.m_features.IsInherited);
			Assert.IsTrue(entry.InheritableLineSpacing.IsInherited);
			Assert.IsTrue(entry.InheritableSpaceBefore.IsInherited);
			Assert.IsTrue(entry.InheritableFirstLineIndent.IsInherited);
			Assert.IsTrue(entry.InheritableLeadingIndent.IsInherited);
			Assert.IsTrue(entry.InheritableTrailingIndent.IsInherited);
			Assert.IsTrue(entry.InheritableBorder.IsInherited);
			Assert.IsTrue(entry.InheritableBorderColor.IsInherited);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving WS specific overrides from string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WsSpecificOverrides_OneWs()
		{
			byte[] buffer = new byte[] {
				0x34, 0x12, 0x00, 0x00, // WS
				0x00, 0x00, // FF length
				0x03, 0x00, // Count of int props

				0x06, 0x00, // First int prop: type
				0x01, 0x00, // Variant
				0xe0, 0x2e, 0x00, 0x00, // value

				0x08, 0x00, // Second int prop: type
				0x00, 0x00, // Variant
				0xff, 0xff, 0x00, 0x00, // value

				0x09, 0x00, // Third int prop: type
				0x00, 0x00, // Variant
				0x00, 0x00, 0x00, 0x00, // value
			};

			DummyStyleInfo entry = new DummyStyleInfo(Cache);

			List<FontOverrideInfo> overrideInfo = entry.CallProcessWsSpecificOverrides(buffer);

			Assert.AreEqual(1, overrideInfo.Count);
			Assert.AreEqual(0x1234, overrideInfo[0].m_ws);
			Assert.AreEqual(0, overrideInfo[0].m_fontFamily.Length);
			Assert.AreEqual(0, overrideInfo[0].m_stringProps.Count);
			Assert.AreEqual(3, overrideInfo[0].m_intProps.Count);
			Assert.AreEqual(6, overrideInfo[0].m_intProps[0].m_textPropType);
			Assert.AreEqual(1, overrideInfo[0].m_intProps[0].m_variant);
			Assert.AreEqual(0x2ee0, overrideInfo[0].m_intProps[0].m_value);
			Assert.AreEqual(8, overrideInfo[0].m_intProps[1].m_textPropType);
			Assert.AreEqual(0, overrideInfo[0].m_intProps[1].m_variant);
			Assert.AreEqual(0xffff, overrideInfo[0].m_intProps[1].m_value);
			Assert.AreEqual(9, overrideInfo[0].m_intProps[2].m_textPropType);
			Assert.AreEqual(0, overrideInfo[0].m_intProps[2].m_variant);
			Assert.AreEqual(0, overrideInfo[0].m_intProps[2].m_value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving WS specific overrides from string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WsSpecificOverrides_TwoWs()
		{
			byte[] buffer = new byte[] {
				0x34, 0x12, 0x00, 0x00, // WS
				0x00, 0x00, // FF length
				0x03, 0x00, // Count of int props

				0x06, 0x00, // First int prop: type
				0x01, 0x00, // Variant
				0xe0, 0x2e, 0x00, 0x00, // value

				0x08, 0x00, // Second int prop: type
				0x00, 0x00, // Variant
				0xff, 0xff, 0x00, 0x00, // value

				0x09, 0x00, // Third int prop: type
				0x00, 0x00, // Variant
				0x00, 0x00, 0x00, 0x00, // value

				// Second writing system
				0x35, 0x12, 0x00, 0x00, // WS
				0x00, 0x00, // FF length
				0x01, 0x00, // Count of int props

				0x08, 0x00, // First int prop: type
				0x00, 0x00, // Variant
				0xee, 0xee, 0x00, 0x00, // value
			};

			DummyStyleInfo entry = new DummyStyleInfo(Cache);

			List<FontOverrideInfo> overrideInfo = entry.CallProcessWsSpecificOverrides(buffer);

			Assert.AreEqual(2, overrideInfo.Count);
			Assert.AreEqual(0x1234, overrideInfo[0].m_ws);
			Assert.AreEqual(0, overrideInfo[0].m_fontFamily.Length);
			Assert.AreEqual(0, overrideInfo[0].m_stringProps.Count);
			Assert.AreEqual(3, overrideInfo[0].m_intProps.Count);
			Assert.AreEqual(6, overrideInfo[0].m_intProps[0].m_textPropType);
			Assert.AreEqual(1, overrideInfo[0].m_intProps[0].m_variant);
			Assert.AreEqual(0x2ee0, overrideInfo[0].m_intProps[0].m_value);
			Assert.AreEqual(8, overrideInfo[0].m_intProps[1].m_textPropType);
			Assert.AreEqual(0, overrideInfo[0].m_intProps[1].m_variant);
			Assert.AreEqual(0xffff, overrideInfo[0].m_intProps[1].m_value);
			Assert.AreEqual(9, overrideInfo[0].m_intProps[2].m_textPropType);
			Assert.AreEqual(0, overrideInfo[0].m_intProps[2].m_variant);
			Assert.AreEqual(0, overrideInfo[0].m_intProps[2].m_value);

			Assert.AreEqual(0x1235, overrideInfo[1].m_ws);
			Assert.AreEqual(0, overrideInfo[1].m_fontFamily.Length);
			Assert.AreEqual(0, overrideInfo[1].m_stringProps.Count);
			Assert.AreEqual(1, overrideInfo[1].m_intProps.Count);
			Assert.AreEqual(8, overrideInfo[1].m_intProps[0].m_textPropType);
			Assert.AreEqual(0, overrideInfo[1].m_intProps[0].m_variant);
			Assert.AreEqual(0xeeee, overrideInfo[1].m_intProps[0].m_value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving WS specific overrides from string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WsSpecificOverrides_FontFamily()
		{
			byte[] buffer = new byte[] {
				0x34, 0x12, 0x00, 0x00, // WS
				0x09, 0x00, // FF length
				(byte)'F', 0,
				(byte)'o', 0,
				(byte)'n', 0,
				(byte)'t', 0,
				(byte)'a', 0,
				(byte)'s', 0,
				(byte)'t', 0,
				(byte)'i', 0,
				(byte)'c', 0,

				0x01, 0x00, // Count of int props

				0x06, 0x00, // First int prop: type
				0x01, 0x00, // Variant
				0xe0, 0x2e, 0x00, 0x00, // value
			};

			DummyStyleInfo entry = new DummyStyleInfo(Cache);

			List<FontOverrideInfo> overrideInfo = entry.CallProcessWsSpecificOverrides(buffer);

			Assert.AreEqual(1, overrideInfo.Count);
			Assert.AreEqual(0x1234, overrideInfo[0].m_ws);
			Assert.AreEqual("Fontastic", overrideInfo[0].m_fontFamily);
			Assert.AreEqual(0, overrideInfo[0].m_stringProps.Count);
			Assert.AreEqual(1, overrideInfo[0].m_intProps.Count);
			Assert.AreEqual(6, overrideInfo[0].m_intProps[0].m_textPropType);
			Assert.AreEqual(1, overrideInfo[0].m_intProps[0].m_variant);
			Assert.AreEqual(0x2ee0, overrideInfo[0].m_intProps[0].m_value);
		}
	}
}
