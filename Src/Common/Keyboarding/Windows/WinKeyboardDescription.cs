// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

#if !__MonoCS__
using System;
using System.Diagnostics.CodeAnalysis;
using SIL.FieldWorks.Common.Keyboarding;
using SIL.FieldWorks.Common.Keyboarding.Interfaces;
using SIL.FieldWorks.Common.Keyboarding.InternalInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Keyboarding.Windows
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Keyboard description for a Windows system keyboard
	/// </summary>
	/// <remarks>Holds information about a specific keyboard, especially for IMEs (e.g. whether
	/// English input mode is selected) in addition to the default keyboard description. This
	/// is necessary to restore the current setting when switching between fields with
	/// differing keyboards. The user expects that a keyboard keeps its state between fields.
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithNativeFieldsShouldBeDisposableRule",
		Justification = "WindowHandle is a reference to a control")]
	internal class WinKeyboardDescription : KeyboardDescription
	{
		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.Windows.WinKeyboardDescription"/> class.
		/// </summary>
		public WinKeyboardDescription(string name, string locale, IKeyboardAdaptor engine, int keyboardHandle)
			: base(name, locale, engine, KeyboardType.System)
		{
			ConversionMode = (int)(Win32.IME_CMODE.NATIVE | Win32.IME_CMODE.SYMBOL);
			KeyboardHandle = keyboardHandle;
		}

		public int ConversionMode { get; set; }
		public int SentenceMode { get; set; }
		public IntPtr WindowHandle { get; set; }

		// MS defines the Handle as IntPtr, but in reality it is always a 32-bit value,
		// so Int32 is better suited. A keyboard handle is just a numeric value, not
		// a pointer to some internal datastructure.
		public int KeyboardHandle { get; private set; }
	}
}
#endif
