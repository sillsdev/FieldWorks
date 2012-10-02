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
// File: StyleUtils.cs
// Responsibility: FieldWorks Team
//
// <remarks>
// FwUtils project is to be dependent on COMInterfaces only, not on FDO, so that FDO can use it
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Formatting style
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StyleUtils
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
		static public ITsTextProps CharStyleTextProps(string styleName, int ws)
		{
			// Build props for the given writing system.
			Debug.Assert(ws != 0);
			ITsPropsBldr tsPropertiesBldr = TsPropsBldrClass.Create();
			tsPropertiesBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				ws == -1 ? -1 : 0, ws);
			// If a style name is given, set that too.
			if (!String.IsNullOrEmpty(styleName))
			{
				tsPropertiesBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
					styleName);
			}
			return tsPropertiesBldr.GetTextProps();
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
		static public ITsTextProps ParaStyleTextProps(string styleName)
		{
			Debug.Assert(styleName != null && styleName.Length > 0);

			// Build props for the given para style name
			ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
			//ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				styleName);
			return tsPropsBldr.GetTextProps();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the writing system of the specified ITsTextProps of a character style
		/// </summary>
		/// <param name="charStyleTextProps">The ITsTextProps</param>
		/// <returns>The writing system value</returns>
		/// ------------------------------------------------------------------------------------
		public static int WritingSystem(ITsTextProps charStyleTextProps)
		{
			int nVar = 0;
			return charStyleTextProps.GetIntPropValues((int)FwTextPropType.ktptWs,
				out nVar);
		}
	}
}
