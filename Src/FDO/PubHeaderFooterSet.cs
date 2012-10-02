// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Publication.cs
// Responsibility: FieldWorks Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FDO.Cellar
{
	/// <summary>
	/// PubHFSet class coded manually to add some methods.
	/// </summary>
	public partial class PubHFSet : CmObject
	{
		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the name of the set.
		/// </summary>
		/// <returns>the name of the set</returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name;;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy the details from the given pub H/F set to this one.
		/// </summary>
		/// <param name="copyFrom">The set to clone</param>
		/// ------------------------------------------------------------------------------------
		public void CloneDetails(IPubHFSet copyFrom)
		{
			Description.UnderlyingTsString = copyFrom.Description.UnderlyingTsString;
			Name = copyFrom.Name;

			// if the header and the footer were null (if they are missing in the database)
			// then create them
			if (DefaultHeaderOA == null)
				DefaultHeaderOA = new PubHeader();
			if (DefaultFooterOA == null)
				DefaultFooterOA = new PubHeader();
			DefaultHeaderOA.OutsideAlignedText.UnderlyingTsString = copyFrom.DefaultHeaderOA.OutsideAlignedText.UnderlyingTsString;
			DefaultHeaderOA.CenteredText.UnderlyingTsString = copyFrom.DefaultHeaderOA.CenteredText.UnderlyingTsString;
			DefaultHeaderOA.InsideAlignedText.UnderlyingTsString = copyFrom.DefaultHeaderOA.InsideAlignedText.UnderlyingTsString;
			DefaultFooterOA.OutsideAlignedText.UnderlyingTsString = copyFrom.DefaultFooterOA.OutsideAlignedText.UnderlyingTsString;
			DefaultFooterOA.CenteredText.UnderlyingTsString = copyFrom.DefaultFooterOA.CenteredText.UnderlyingTsString;
			DefaultFooterOA.InsideAlignedText.UnderlyingTsString = copyFrom.DefaultFooterOA.InsideAlignedText.UnderlyingTsString;


			PubDivision owningDiv = new PubDivision(Cache, OwnerHVO);
			if (owningDiv.DifferentFirstHF)
			{
				// if the header and the footer were null (if they are the same as the odd page)
				// then create them
				if (FirstHeaderOA == null)
					FirstHeaderOA = new PubHeader();
				if (FirstFooterOA == null)
					FirstFooterOA = new PubHeader();
				FirstHeaderOA.OutsideAlignedText.UnderlyingTsString = copyFrom.FirstHeaderOA.OutsideAlignedText.UnderlyingTsString;
				FirstHeaderOA.CenteredText.UnderlyingTsString = copyFrom.FirstHeaderOA.CenteredText.UnderlyingTsString;
				FirstHeaderOA.InsideAlignedText.UnderlyingTsString = copyFrom.FirstHeaderOA.InsideAlignedText.UnderlyingTsString;
				FirstFooterOA.OutsideAlignedText.UnderlyingTsString = copyFrom.FirstFooterOA.OutsideAlignedText.UnderlyingTsString;
				FirstFooterOA.CenteredText.UnderlyingTsString = copyFrom.FirstFooterOA.CenteredText.UnderlyingTsString;
				FirstFooterOA.InsideAlignedText.UnderlyingTsString = copyFrom.FirstFooterOA.InsideAlignedText.UnderlyingTsString;
			}
			else
			{
				FirstHeaderOA = null;
				FirstFooterOA = null;
			}
			if (owningDiv.DifferentEvenHF)
			{
				// if the header and the footer were null (if they are the same as the odd page)
				// then create them
				if (EvenHeaderOA == null)
					EvenHeaderOA = new PubHeader();
				if (EvenFooterOA == null)
					EvenFooterOA = new PubHeader();
				EvenHeaderOA.OutsideAlignedText.UnderlyingTsString = copyFrom.EvenHeaderOA.OutsideAlignedText.UnderlyingTsString;
				EvenHeaderOA.CenteredText.UnderlyingTsString = copyFrom.EvenHeaderOA.CenteredText.UnderlyingTsString;
				EvenHeaderOA.InsideAlignedText.UnderlyingTsString = copyFrom.EvenHeaderOA.InsideAlignedText.UnderlyingTsString;
				EvenFooterOA.OutsideAlignedText.UnderlyingTsString = copyFrom.EvenFooterOA.OutsideAlignedText.UnderlyingTsString;
				EvenFooterOA.CenteredText.UnderlyingTsString = copyFrom.EvenFooterOA.CenteredText.UnderlyingTsString;
				EvenFooterOA.InsideAlignedText.UnderlyingTsString = copyFrom.EvenFooterOA.InsideAlignedText.UnderlyingTsString;
			}
			else
			{
				EvenHeaderOA = null;
				EvenFooterOA = null;
			}
		}
		#endregion
	}
}