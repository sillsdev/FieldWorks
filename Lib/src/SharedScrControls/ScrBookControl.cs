// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrBookControl.cs
// ---------------------------------------------------------------------------------------------
using SILUBS.SharedScrUtils;

namespace SILUBS.SharedScrControls
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
		/// Default constructor (hard-coded to use English Versification)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrBookControl() : this(ScrVers.English)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor that takes a versification to use for references
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrBookControl(ScrVers versification) : base(null, null, versification)
		{
			txtScrRef.ReadOnly = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor that takes a "Scripture project" that provides versification info on
		/// the fly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrBookControl(IScrProjMetaDataProvider scrProj) : base(null, scrProj, ScrVers.English)
		{
			txtScrRef.ReadOnly = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the reference. In the setter, if the value consists of more than one
		/// space-delimited token, only use the first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Reference
		{
			get { return base.Reference; }
			set { base.Reference = value.Split(new [] {' '}, 2)[0]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DropDownBookSelected(int book)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new <see cref="ScrPassageDropDown"/> object
		/// </summary>
		/// <param name="owner">The ScrPassageControl that will own the drop-down control</param>
		/// ------------------------------------------------------------------------------------
		protected override ScrPassageDropDown CreateScrPassageDropDown(ScrPassageControl owner)
		{
			return new ScrPassageDropDown(owner, true, Versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the specified book num.
		/// </summary>
		/// <param name="bookNum">The canonical number of the book to select initially.</param>
		/// <param name="availableBooks">The canonical numbers of the available books.</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(int bookNum, int[] availableBooks)
		{
			base.Initialize(new ScrReference(bookNum, 1, 1, Versification), availableBooks);
		}
	}
}