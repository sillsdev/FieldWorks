using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Various services for StStyle
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class StyleServices
	{
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
		public const string DefaultFont = "<default font>";	// CANNOT BE LOCALIZED AS PUBLIC CONST STRING.

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified font is one of the "magic" font names.
		/// </summary>
		/// <param name="fontName">Name of the font.</param>
		/// ------------------------------------------------------------------------------------
		public static bool IsMagicFontName(string fontName)
		{
			return fontName == DefaultFont;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified style is considered internal (i.e. the user can't
		/// apply it).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsInternal(this IStStyle style)
		{
			return IsContextInternal(style.Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified context is considered internal (i.e. the user can't
		/// apply it).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsContextInternal(ContextValues context)
		{
			return (context == ContextValues.Internal ||
				context == ContextValues.InternalMappable ||
				context == ContextValues.InternalConfigureView ||
				context == ContextValues.Note);
		}
	}
}
