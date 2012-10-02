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
// File: StStyle.cs
// Responsibility: FieldWorks Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Formatting style
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class StStyle
	{
		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this style is a footnote style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsFootnoteStyle
		{
			get
			{
				return Context == ContextValues.Note;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether the style is in use. Note: "In use" generally means that the
		/// style is being used to mark up some text somewhere in the project, but it's
		/// possible (probable, even) that this will return <c>true</c> even when a style was
		/// used at some time and is no longer being used.
		/// </summary>
		/// <remarks>Virtual to allow dynamic mocks to override it</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool InUse
		{
			get
			{
				return (UserLevel <= 0);
			}
			set
			{
				if ((value && UserLevel > 0) || (!value && UserLevel < 0))
					UserLevel *= -1;
			}
		}
		#endregion

		#region static style properties
		// The following static property may eventually change to return an integer ID
		// representing the style name. In any event, this values are NOT localizable.
		// It represents the absolute representation of the style and will not
		// necessarily be viewable by the user (though at least for now they are).

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the string "Normal", to be used wherever the normal style's name is needed.
		/// Do not use a resource or hard-code for this name, as it is used in the database, and
		/// any change should be possible from as few places as possible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string NormalStyleName
		{
			//TODO BryanW: use resx or constant? must be localizable
			// REVIEW: THIS NEEDS MORE THOUGHT, BECAUSE STYLE NAMES (AS EMBEDDED INSIDE STRING FORMATS)
			// ARE UNICODE, NOT MULTIUNICODE!
			get
			{
				return "Normal";
			}
		}

		/// <summary>Internal style used for hyperlinks</summary>
		public const string Hyperlink = "Hyperlink";	// CANNOT BE LOCALIZED.

		/// <summary>Magic style name that resolves to the default font for a particular writing system</summary>
		public const string DefaultFont = "<default serif>";	// CANNOT BE LOCALIZED AS PUBLIC CONST STRING.

		/// <summary>Magic style name that resolves to the default heading font for a particular writing system</summary>
		public const string DefaultHeadingFont = "<default sans serif>";	// CANNOT BE LOCALIZED AS PUBLIC CONST STRING.

		/// <summary>Magic style name that resolves to the default body publication font for a particular writing system</summary>
		public const string DefaultPubFont = "<default pub font>";	// CANNOT BE LOCALIZED AS PUBLIC CONST STRING.

		/// <summary>Magic style name that resolves to the default monospace font for a particular writing system</summary>
		public const string DefaultMonospace = "<default monospace>";	// CANNOT BE LOCALIZED AS PUBLIC CONST STRING.
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the given text props to see if they specify the given style
		/// </summary>
		/// <param name="ttp">Text props</param>
		/// <param name="sStyle">Style</param>
		/// <returns>true if the given text props use the given named style</returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsStyle(ITsTextProps ttp, string sStyle)
		{
			return (ttp.GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle) == sStyle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified font is one of the "magic" font names.
		/// </summary>
		/// <param name="fontName">Name of the font.</param>
		/// ------------------------------------------------------------------------------------
		public static bool IsMagicFontName(string fontName)
		{
			return (fontName == DefaultHeadingFont || fontName == DefaultFont ||
				fontName == DefaultMonospace || fontName == DefaultPubFont);
		}
		#endregion
	}
}
