// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DispPropOverrideFactory.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Factory to create DispPropOverride objects which are initialized .
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class DispPropOverrideFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a (default) DispPropOverride that does nothing for the specified range of
		/// characters.
		/// </summary>
		/// <param name="ichOverrideMin">The index (in logical characters, relative to the
		/// paragraph as laid out in the view) of the first character whose properties will be
		/// overridden.</param>
		/// <param name="ichOverrideLim">The character "limit" (in logical characters, relative
		/// to the paragraph as laid out in the view) of the text whose properties will be
		/// overridden.</param>
		/// ------------------------------------------------------------------------------------
		public static DispPropOverride Create(int ichOverrideMin, int ichOverrideLim)
		{
			DispPropOverride prop = new DispPropOverride();
			unchecked
			{
				prop.chrp.clrBack = prop.chrp.clrFore = prop.chrp.clrUnder = (uint)FwTextPropConstants.knNinch;
			}
			prop.chrp.dympOffset = -1;
			prop.chrp.ssv = -1;
			prop.chrp.unt = -1;
			prop.chrp.ttvBold = -1;
			prop.chrp.ttvItalic = -1;
			prop.chrp.dympHeight = -1;
			prop.chrp.szFaceName = null;
			prop.chrp.szFontVar = null;
			prop.ichMin = ichOverrideMin;
			prop.ichLim = ichOverrideLim;
			return prop;
		}
	}
}
