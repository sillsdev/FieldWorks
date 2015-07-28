// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// IStylesheet defines the interface of a stylesheet purely from the point of view of a client.
	/// Unlike IVwStyleSheet, it does not include anything for edting the contents of the stylesheet.
	/// It is kept in a separate file in the expectation that it might be helpful to move it out of
	/// FDO eventually.
	/// </summary>
	public interface IStylesheet
	{
		/// <summary>
		/// Get the style with the given name. Returns null if there is no such name.
		/// </summary>
		IStyle Style(string name);
	}

	/// <summary>
	/// Styles are the contents of stylesheets.
	/// </summary>
	public interface IStyle
	{
		/// <summary>
		/// The name of the style
		/// </summary>
		string Name { get;}

		// May want to add Usage, Context

		/// <summary>
		/// Gets whether a style is a paragraph style (if not it is a character style).
		/// </summary>
		bool IsParagraphStyle { get; }

		/// <summary>
		///  Paragraph style info
		/// </summary>
		IParaStyleInfo ParagraphStyleInfo { get; }

		// Eventually many of the public property values of BaseStyleInfo like FirstLineIndent will probably
		// be exposed in this interface, but for now, the interface only serves SharpViews, and there is no
		// point in including more stuff than it can use.

		/// <summary>
		/// Get the default character style info (common to all writing systems, unless overridden).
		/// </summary>
		ICharacterStyleInfo DefaultCharacterStyleInfo { get; }

		/// <summary>
		/// Character style info overridden for a specific writing system. To get the actual character style
		/// info for a particular WS, apply the default and then the appropriate override.
		/// </summary>
		ICharacterStyleInfo OverrideCharacterStyleInfo(int ws);
	}

	/// <summary>
	/// The information that character styles can have (mostly font-related) that is writing-system dependent.
	/// Eventually we may move IStyleProp out of DomainServices
	/// </summary>
	public interface ICharacterStyleInfo
	{
		/// <summary>Name of font to use</summary>
		IStyleProp<string> FontName { get; }
		/// <summary>Size in millipoints</summary>
		IStyleProp<int> FontSize { get; }
		/// <summary>Fore color (ARGB)</summary>
		IStyleProp<Color> FontColor { get; }
		/// <summary>Background color (ARGB)</summary>
		IStyleProp<Color> BackColor { get; }
		/// <summary>Indicates whether font is bold or not</summary>
		IStyleProp<bool> Bold { get; }
		/// <summary>Indicates whether font is italic or not</summary>
		IStyleProp<bool> Italic { get; }
		/// <summary>Superscript, Subscript, or normal</summary>
		IStyleProp<FwSuperscriptVal> SuperSub { get; }
		/// <summary>Indicates that this style is Underline</summary>
		IStyleProp<FwUnderlineType> Underline { get; }
		/// <summary>Underline color (ARGB)</summary>
		IStyleProp<Color> UnderlineColor { get; }
		/// <summary>Vertical offset</summary>
		IStyleProp<int> Offset { get; }
		/// <summary>Font features (used for Graphite fonts)</summary>
		IStyleProp<string> Features { get; }
	}

	/// <summary>
	/// The information that paragraph styles can have.
	/// </summary>
	public interface IParaStyleInfo
	{
		/// <summary>Alignment left, right, center, justified</summary>
		IStyleProp<FwTextAlign> Alignment { get; }
		/// <summary>Gets the inter-line spacing in millipoints</summary>
		IStyleProp<LineHeightInfo> LineHeight { get; }
		/// <summary>Gets the space above paragraph in millipoints</summary>
		IStyleProp<int> SpaceBefore { get; }
		/// <summary>Gets the space below paragraph in millipoints</summary>
		IStyleProp<int> SpaceAfter { get; }
		/// <summary>Gets the indentation of first line in millipoints</summary>
		IStyleProp<int> FirstLineIndent { get; }
		/// <summary>Gets the indentation of paragraph from leading edge in millipoints</summary>
		IStyleProp<int> LeadingIndent { get; }
		/// <summary>Gets the indentation of paragraph from trailing edge in millipoints</summary>
		IStyleProp<int> TrailingIndent { get; }
		/// <summary>Gets the ARGB Color of borders</summary>
		IStyleProp<Color> BorderColor { get; }
		/// <summary>Gets the thickness of leading border in millipoints</summary>
		IStyleProp<int> BorderLeading { get; }
		/// <summary>Gets the thickness of trailing border in millipoints</summary>
		IStyleProp<int> BorderTrailing { get; }
		/// <summary>Gets the thickness of top border in millipoints</summary>
		IStyleProp<int> BorderTop { get; }
		/// <summary>Gets the thickness of bottom border in millipoints</summary>
		IStyleProp<int> BorderBottom { get; }
		/// <summary>Gets the thickness of leading margin in millipoints; an alias for LeadingIndent (more in CSS style)</summary>
		IStyleProp<int> MarginLeading { get; }
		/// <summary>Gets the thickness of trailing margin in millipoints; an alias for TrailingIndent (more in CSS style)</summary>
		IStyleProp<int> MarginTrailing { get; }
		/// <summary>Gets the thickness of top margin in millipoints.
		/// Review JohnT: should this be the same as SpaceBefore, or do we want the same trick as in the old Views, or can we do without the MSWord-style
		/// margin top here??</summary>
		IStyleProp<int> MarginTop { get; }
		/// <summary>Gets the thickness of bottom margin in millipoints
		/// Review JohnT: should this be the same as SpaceAfter, or do we want the same trick as in the old Views, or can we do without the MSWord-style
		/// margin top here??</summary>
		IStyleProp<int> MarginBottom { get; }
		/// <summary>Gets the thickness of leading pad in millipoints</summary>
		IStyleProp<int> PadLeading { get; }
		/// <summary>Gets the thickness of trailing pad in millipoints</summary>
		IStyleProp<int> PadTrailing { get; }
		/// <summary>Gets the thickness of top pad in millipoints</summary>
		IStyleProp<int> PadTop { get; }
		/// <summary>Gets the thickness of bottom pad in millipoints</summary>
		IStyleProp<int> PadBottom { get; }
	}

	/// <summary>
	///
	/// </summary>
	public interface IStyleProp<T>
	{
		/// <summary>
		/// Gets the value of the style property regardless of whether it is inherited or
		/// explicit.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if this is an inherited
		/// property and the inherited value has not been set.
		/// </exception>
		T Value { get; }

		/// <summary>
		/// Gets a value indicating whether it is okay to access the <see cref="Value"/>
		/// property.
		/// </summary>
		bool ValueIsSet { get; }
	}
}
