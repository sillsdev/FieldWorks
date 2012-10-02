// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrBookControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ScrBookControl.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrBookControl : ScrPassageControl
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrBookControl() : base(null, null, false)
		{
			txtScrRef.ReadOnly = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Reference
		{
			get {return base.Reference;}
			set
			{
				// If value consists of more than one space-delimited token, only use the first.
				base.Reference = value.Split(new char[] {' '}, 2)[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="book"></param>
		/// ------------------------------------------------------------------------------------
		protected override void DropDownBookSelected(int book)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override ScrPassageDropDown CreateScrPassageDropDown(ScrPassageControl owner)
		{
			return new ScrPassageDropDown(owner, true, Versification);
		}
	}
}
