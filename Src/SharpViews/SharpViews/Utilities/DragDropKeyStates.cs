// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Utilities
{
	/// <summary>
	/// This enumeration gives the possible values of the DragEventArgs.KeyState member.
	/// It is modeled on System.Windows.DragDropKeyStates (part of the WPF).
	/// </summary>
	[Flags]
	public enum DragDropKeyStates
	{
		None = 0, // No modifier keys or mouse buttons are pressed.
		LeftMouseButton = 1, // The left mouse button is pressed.
		RightMouseButton = 2, // The right mouse button is pressed.
		ShiftKey = 4, // The shift (SHIFT) key is pressed.
		ControlKey = 8, // The control (CTRL) key is pressed.
		MiddleMouseButton = 16, // The middle mouse button is pressed.
		AltKey = 32
	}
}
