// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ControlCreateInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	#region FixedControlCreateInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds already created controls. This can be used to insert regular controls like labels
	/// and buttons in a SplitGrid.
	/// </summary>
	/// <example>For an example how this is used, see TE\DiffView\DiffViewWrapper.cs.</example>
	/// ----------------------------------------------------------------------------------------
	public struct FixedControlCreateInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FixedControlCreateInfo"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		/// ------------------------------------------------------------------------------------
		public FixedControlCreateInfo(Control control)
		{
			Control = control;
		}

		/// <summary>The control</summary>
		public Control Control;
	}
	#endregion

	#region Internally used class ControlCreateInfo
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
		/// <param name="controlInfo">The control info.</param>
		/// <param name="fScrollingContainer"><c>true</c> if this control should control
		/// scrolling (and display a scroll bar)</param>
		/// ------------------------------------------------------------------------------------
		public ControlCreateInfo(IRootSiteGroup group, object controlInfo, bool fScrollingContainer)
		{
			Group = group;
			ClientControlInfo = controlInfo;
			IsScrollingController = fScrollingContainer;
		}

		/// <summary>The root site group this control will belong to</summary>
		public IRootSiteGroup Group;

		/// <summary>Information the client provided to create the control.</summary>
		public object ClientControlInfo;

		/// <summary><c>true</c> if this control should control scrolling (and display a
		/// scroll bar)</summary>
		public bool IsScrollingController;

		/// <summary>Holds the control while it is awaiting initialization.</summary>
		public Control Control;
	}
	#endregion
}
