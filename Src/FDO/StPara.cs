// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StPara.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base Paragraph class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class StPara
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the StyleName
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string StyleName
		{
			get
			{
				return StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			}
			set
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentException("StyleName cannot be set to null or empty string.");
				ITsPropsBldr bldr = StyleRules != null ? StyleRules.GetBldr() : TsPropsBldrClass.Create();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, value);
				StyleRules = bldr.GetTextProps();
			}
		}
	}
}
