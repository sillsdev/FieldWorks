using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
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
		/// Get the style with the given name. Returns null if there is no such style.
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
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
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
