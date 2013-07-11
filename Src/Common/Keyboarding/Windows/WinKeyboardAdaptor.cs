// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if !__MonoCS__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Common.Keyboarding.Interfaces;
using SIL.FieldWorks.Common.Keyboarding.InternalInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Keyboarding.Windows
{
	/// <summary>
	/// Class for handling Windows system keyboards
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "m_Timer gets disposed in Close() which gets called from KeyboardControllerImpl.Dispose")]
	internal class WinKeyboardAdaptor: IKeyboardAdaptor
	{
		private List<IKeyboardErrorDescription> m_BadLocales;
		private Timer m_Timer;
		private InputLanguage m_ExpectedInputLanguage;
		private WinKeyboardDescription m_ExpectedKeyboard;
		private bool m_fSwitchedLanguages;

		private void GetLocales()
		{
			m_BadLocales = new List<IKeyboardErrorDescription>();
			// ENHANCE: For "Chinese (Simplified, PRC)" we always get back
			// "Chinese (Simplified) - US Keyboard" no matter what IME the
			// user added in the system settings. And it's reported only once
			// even when the user adds multiple IMEs.
			foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
			{
				// NOTE: InputLanguage.LayoutName has a bug in that it always returns the name
				// of the first layout even when a culture has multiple layouts assigned.
				// Therefore we use GetLayoutNameEx to retrieve the information from the registry.
				var layoutName = GetLayoutNameEx(lang.Handle);
				//var cultureId = string.Format("{0:X4}", (int)(lang.Handle) & 0xFFFF);
				string displayName;
				string locale;
				try
				{
					displayName = lang.Culture.DisplayName;
					locale = lang.Culture.Name;
				}
				catch (CultureNotFoundException)
				{
					// we get an exception for non-supported cultures, probably because of a
					// badly applied .NET patch.
					// http://www.ironspeed.com/Designer/3.2.4/WebHelp/Part_VI/Culture_ID__XXX__is_not_a_supported_culture.htm and others
					displayName = "[Unknown Language]";
					locale = "en-US";
				}
				KeyboardController.Manager.RegisterKeyboard(new WinKeyboardDescription(
					GetDisplayName(displayName, layoutName), locale, this,
					(int)lang.Handle));
			}
		}

		private static string GetDisplayName(string cultureName, string layoutName)
		{
			return string.Format("{1} - {0}", cultureName, layoutName);
		}

		private string GetLayoutNameEx(IntPtr handle)
		{
			// InputLanguage.LayoutName is not to be trusted, especially where there are mutiple
			// layouts (input methods) associated with a language. This function also provides
			// the additional benefit that it does not matter whether a user switches from using
			// InKey in Portable mode to using it in Installed mode (perhaps as the project is
			// moved from one computer to another), as this function will identify the correct
			// input language regardless, rather than (unhelpfully ) calling an InKey layout in
			// portable mode the "US" layout. The layout is identified soley by the high-word of
			// the HKL (a.k.a. InputLanguage.Handle).  (The low word of the HKL identifies the
			// language.)
			// This function determines an HKL's LayoutName based on the following order of
			// precedence:
			// - Look up HKL in HKCU\\Software\\InKey\\SubstituteLayoutNames
			// - Look up basic (non-extended) layout in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts
			// -Scan for ID of extended layout in HKLM\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts
			var hkl = string.Format("{0:X8}", (int)handle);
			var layoutName = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\InKey\SubstituteLayoutNames", hkl, null);
			if (!string.IsNullOrEmpty(layoutName))
				return layoutName;

			layoutName = (string)Registry.GetValue(string.Concat(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Keyboard Layouts\0000",
				hkl.Substring(0, 4)), "Layout Text", null);

			if (!string.IsNullOrEmpty(layoutName))
				return layoutName;

			using (var regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts"))
			{
				string layoutId = "0" + hkl.Substring(1, 3);
				foreach (string subKeyName in regKey.GetSubKeyNames().Reverse())  // Scan in reverse order for efficiency, as the extended layouts are at the end.
				{
					using (var klid = regKey.OpenSubKey(subKeyName))
					{
						if (((string)klid.GetValue("Layout ID")).Equals(layoutId, StringComparison.InvariantCultureIgnoreCase))
							return (string)klid.GetValue("Layout Text");
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the InputLanguage that has the same layout as <paramref name="keyboardDescription"/>.
		/// </summary>
		private InputLanguage GetInputLanguage(IKeyboardDescription keyboardDescription)
		{
			InputLanguage sameLayout = null;
			InputLanguage sameCulture = null;
			foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
			{
				// TODO: write some tests
				try
				{
					if (GetLayoutNameEx(lang.Handle) == keyboardDescription.Name)
					{
						if (keyboardDescription.Locale == lang.Culture.Name)
							return lang;
						if (sameLayout == null)
							sameLayout = lang;
					}
					else if (keyboardDescription.Locale == lang.Culture.Name && sameCulture == null)
						sameCulture = lang;
				}
				catch (CultureNotFoundException)
				{
					// we get an exception for non-supported cultures, probably because of a
					// badly applied .NET patch.
					// http://www.ironspeed.com/Designer/3.2.4/WebHelp/Part_VI/Culture_ID__XXX__is_not_a_supported_culture.htm and others
				}
			}
			return sameLayout ?? sameCulture;
		}

		/// <summary>
		/// Gets the keyboard description for the layout of <paramref name="inputLanguage"/>.
		/// </summary>
		private WinKeyboardDescription GetKeyboardDescription(InputLanguage inputLanguage)
		{
			WinKeyboardDescription sameLayout = null;
			WinKeyboardDescription sameCulture = null;
			// TODO: write some tests
			foreach (WinKeyboardDescription keyboardDescription in KeyboardController.InstalledKeyboards)
			{
				try
				{
					if (GetLayoutNameEx(inputLanguage.Handle) == keyboardDescription.Name)
					{
						if (keyboardDescription.Locale == inputLanguage.Culture.Name)
							return keyboardDescription;
						if (sameLayout == null)
							sameLayout = keyboardDescription;
					}
					else if (keyboardDescription.Locale == inputLanguage.Culture.Name && sameCulture == null)
						sameCulture = keyboardDescription;
				}
				catch (CultureNotFoundException)
				{
					// we get an exception for non-supported cultures, probably because of a
					// badly applied .NET patch.
					// http://www.ironspeed.com/Designer/3.2.4/WebHelp/Part_VI/Culture_ID__XXX__is_not_a_supported_culture.htm and others
				}
			}
			return sameLayout ?? sameCulture;
		}

		private void OnTimerTick(object sender, EventArgs eventArgs)
		{
			if (m_ExpectedInputLanguage == null || m_ExpectedKeyboard == null)
				return;

			if (!m_fSwitchedLanguages)
			{
				m_Timer.Enabled = false;
				return;
			}

			if (InputLanguage.CurrentInputLanguage.Handle == m_ExpectedInputLanguage.Handle)
			{
				m_ExpectedInputLanguage = null;
				m_ExpectedKeyboard = null;
				m_fSwitchedLanguages = false;
				return;
			}

			SwitchKeyboard(m_ExpectedKeyboard, m_ExpectedInputLanguage);
		}

		private void SwitchKeyboard(WinKeyboardDescription winKeyboard, InputLanguage inputLanguage)
		{
			m_ExpectedKeyboard = winKeyboard;
			m_ExpectedInputLanguage = inputLanguage;
			try
			{
				InputLanguage.CurrentInputLanguage = inputLanguage;
			}
			catch (ArgumentException)
			{
				// throws exception for non-supported culture, though seems to set it OK.
			}

			KeyboardController.ActiveKeyboard = winKeyboard;

			// The following two lines help to work around a Windows bug (happens at least on
			// XP-SP3): When you set the current input language (by any method), if there is more
			// than one loaded input language associated with that same culture, Windows may
			// initially go along with your request, and even respond to an immediate query of
			// the current input language with the answer you expect.  However, within a fraction
			// of a second, it often takes the initiative to again change the input language to
			// the _other_ input language having that same culture. We check that the proper
			// input language gets set by enabling a timer so that we can re-set the input
			// language if necessary.
			m_fSwitchedLanguages = true;
			// stop timer first so that the 0.5s interval restarts.
			m_Timer.Stop();
			m_Timer.Start();
		}

		#region IKeyboardAdaptor Members
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "m_Timer gets disposed in Close() which gets called from KeyboardControllerImpl.Dispose")]
		public void Initialize()
		{
			m_Timer = new Timer { Interval = 500 };
			m_Timer.Tick += OnTimerTick;

			GetLocales();

			// Form.ActiveForm can be null when running unit tests
			if (Form.ActiveForm != null)
				Form.ActiveForm.InputLanguageChanged += ActiveFormOnInputLanguageChanged;
		}

		/// <summary>
		/// Save the state of the conversion and sentence mode for the current IME
		/// so that we can restore it later.
		/// </summary>
		private void SaveImeConversionStatus(WinKeyboardDescription winKeyboard)
		{
			if (winKeyboard == null)
				return;

			var windowHandle = new HandleRef(this,
				winKeyboard.WindowHandle != IntPtr.Zero ? winKeyboard.WindowHandle : Win32.GetFocus());
			var contextPtr = Win32.ImmGetContext(windowHandle);
			if (contextPtr == IntPtr.Zero)
				return;

			var contextHandle = new HandleRef(this, contextPtr);
			int conversionMode;
			int sentenceMode;
			Win32.ImmGetConversionStatus(contextHandle, out conversionMode, out sentenceMode);
			winKeyboard.ConversionMode = conversionMode;
			winKeyboard.SentenceMode = sentenceMode;
			Win32.ImmReleaseContext(windowHandle, contextHandle);
		}

		/// <summary>
		/// Restore the conversion and sentence mode to the states they had last time
		/// we activated this keyboard (unless we never activated this keyboard since the app
		/// got started, in which case we use sensible default values).
		/// </summary>
		private void RestoreImeConversionStatus(WinKeyboardDescription winKeyboard)
		{
			if (winKeyboard == null)
				return;

			// Restore the state of the new keyboard to the previous value. If we don't do
			// that e.g. in Chinese IME the input mode will toggle between English and
			// Chinese (LT-7487 et al).
			var windowPtr = winKeyboard.WindowHandle != IntPtr.Zero ? winKeyboard.WindowHandle : Win32.GetFocus();
			var windowHandle = new HandleRef(this, windowPtr);
			var contextPtr = Win32.ImmGetContext(windowHandle);
			if (contextPtr == IntPtr.Zero)
				return;

			var contextHandle = new HandleRef(this, contextPtr);
			Win32.ImmSetConversionStatus(contextHandle, winKeyboard.ConversionMode, winKeyboard.SentenceMode);
			Win32.ImmReleaseContext(windowHandle, contextHandle);
			winKeyboard.WindowHandle = windowPtr;
		}

		private void ActiveFormOnInputLanguageChanged(object sender, InputLanguageChangedEventArgs inputLanguageChangedEventArgs)
		{
			RestoreImeConversionStatus(GetKeyboardDescription(inputLanguageChangedEventArgs.InputLanguage));
		}

		public void Close()
		{
			if (m_Timer != null)
			{
				m_Timer.Dispose();
				m_Timer = null;
			}
		}

		public List<IKeyboardErrorDescription> ErrorKeyboards
		{
			get { return m_BadLocales; }
		}

		public void ActivateKeyboard(IKeyboardDescription keyboard)
		{
			SwitchKeyboard(keyboard as WinKeyboardDescription, GetInputLanguage(keyboard));
		}

		public void DeactivateKeyboard(IKeyboardDescription keyboard)
		{
			var winKeyboard = keyboard as WinKeyboardDescription;
			Debug.Assert(winKeyboard != null);

			SaveImeConversionStatus(winKeyboard);
		}
		#endregion
	}
}
#endif
