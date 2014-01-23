// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

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
