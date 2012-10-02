// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2007' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ControlCreateInfo.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information necessary to create a control hosted in a DataGridViewControlCell.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ControlCreateInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ControlCreateInfo"/> class.
		/// </summary>
		/// <param name="group">The group.</param>
		/// <param name="viewProxy">The view proxy.</param>
		/// <param name="fScrollingContainer"><c>true</c> if this control should control
		/// scrolling (and display a scroll bar)</param>
		/// ------------------------------------------------------------------------------------
		public ControlCreateInfo(IRootSiteGroup group, ViewProxy viewProxy, bool fScrollingContainer)
		{
			Group = group;
			ViewProxy = viewProxy;
			IsScrollingController = fScrollingContainer;
		}

		/// <summary>The root site group this control will belong to</summary>
		public IRootSiteGroup Group { get; private set; }

		/// <summary>Information the client provided to create the control.</summary>
		public ViewProxy ViewProxy { get; private set; }

		/// <summary><c>true</c> if this control should control scrolling (and display a
		/// scroll bar)</summary>
		public bool IsScrollingController { get; private set; }

		/// <summary>Holds the control while it is awaiting initialization.</summary>
		public Control Control { get; set; }
	}
}
