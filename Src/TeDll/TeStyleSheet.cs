// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeStyleSheet.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Summary description for TeStyleSheet.
	/// </summary>
	public class TeStyleSheet : FwStyleSheet
	{
		#region Overrides of FwStyleSheet
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default line spacing in millipoints (exactly 12 pts).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int DefaultLineSpacing
		{
			get { return -12000; }
		}
		#endregion

		#region Methods of IVwStylesheet
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default paragraph style to use as the base for new styles
		/// (Usually "Normal")
		/// </summary>
		/// <returns>"Paragraph"</returns>
		/// ------------------------------------------------------------------------------------
		public override string GetDefaultBasedOnStyleName()
		{
			return ScrStyleNames.NormalParagraph;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style name that is the default style to use for the given context
		/// </summary>
		/// <param name="nContext">the context</param>
		/// <param name="fCharStyle">set to <c>true</c> for character styles; otherwise
		/// <c>false</c>.</param>
		/// <returns>
		/// Name of the style that is the default for the context
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string GetDefaultStyleForContext(int nContext, bool fCharStyle)
		{
			return TeEditingHelper.GetDefaultStyleForContext((ContextValues)nContext, fCharStyle);
		}
		#endregion
	}
}
