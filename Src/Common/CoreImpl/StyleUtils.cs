// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StyleUtils.cs
// Responsibility: FW Team

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Formatting style
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class StyleUtils
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns run textprops representing a character style of the given name.
		/// </summary>
		/// <param name="styleName">The name of the character style, or null to get props for
		/// "Default Paragraph Characters"</param>
		/// <param name="ws">The writing system</param>
		/// <returns>requested text props</returns>
		/// <remarks>If styleName is not given, the resulting props contains only the
		/// vernacular writing system.</remarks>
		/// <remarks>For char style, props should contain ws (writing system) and char style
		/// name. Without a char style, props should contain ws only.
		/// We'll use the vernacular ws.</remarks>
		/// <remarks>Useful for setting run props for an <see cref="ITsString"/>.</remarks>
		/// ------------------------------------------------------------------------------------
		public static ITsTextProps CharStyleTextProps(string styleName, int ws)
		{
			// Build props for the given writing system.
			Debug.Assert(ws != 0);
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, ws == -1 ? -1 : 0, ws);
			// If a style name is given, set that too.
			if (!String.IsNullOrEmpty(styleName))
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleName);
			return bldr.GetTextProps();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns paragraph textprops representing a paragraph style of the given name.
		/// </summary>
		/// <param name="styleName">The name of the paragraph style</param>
		/// <returns>A TsTextProps object wich will have exactly zero int props and one string
		/// prop: the named style, whose value will be the given styleName</returns>
		/// <remarks>This is useful for setting BaseStPara.StyleRules.</remarks>
		/// ------------------------------------------------------------------------------------
		public static ITsTextProps ParaStyleTextProps(string styleName)
		{
			Debug.Assert(!string.IsNullOrEmpty(styleName));

			// Build props for the given para style name
			ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleName);
			return tsPropsBldr.GetTextProps();
		}
	}
}
