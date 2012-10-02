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
// File: IControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Can be implemented by a class that doesn't behave like a standard control but is close
	/// enough. Several of the methods and properties of Control are not virtual, so can't be
	/// overridden directly. Therefore we provide similar methods and properties in this
	/// interface.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IControl
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of focusable controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<Control> FocusableControls { get; }
	}
}
