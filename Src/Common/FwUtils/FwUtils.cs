// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwUtils.cs
// Responsibility: TE Team

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Drawing;

#if __MonoCS__
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
#endif
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Collection of miscellaneous utility methods needed for FieldWorks
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static partial class FwUtils
	{
		/// <summary>
		/// The name of the overarching umbrella application that will one day conquer the world:
		/// "FieldWorks"
		/// </summary>
		public const string ksSuiteName = "FieldWorks";
		/// <summary>
		/// The name of the Language Explorer folder (Even though this is the same as
		/// FwDirectoryFinder.ksFlexFolderName and FwSubKey.LexText, PLEASE do not use them interchangeably.
		/// Use the one that is correct for your context, in case they need to be changed later.)
		/// </summary>
		public const string ksFlexAppName = "Language Explorer";
		/// <summary>
		/// The fully-qualified (with namespace) C# object name for LexTextApp
		/// </summary>
		public const string ksFullFlexAppObjectName = "LanguageExplorer.Impls.LexTextApp";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a name suitable for use as a pipe name from the specified project handle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GeneratePipeHandle(string handle)
		{
			const string ksSuiteIdPrefix = ksSuiteName + ":";
			return (handle.StartsWith(ksSuiteIdPrefix) ? string.Empty : ksSuiteIdPrefix) +
				handle.Replace('/', ':').Replace('\\', ':');
		}

#if __MonoCS__
		// On Linux, the default string output does not choose a font based on the characters in
		// the string, but on the current user interface locale.  At times, we want to display,
		// for example, Korean when the user interface locale is English.  By default, this
		// nicely displays boxes instead of characters.  Thus, we need some way to obtain the
		// correct font in such situations.  See FWNX-1069 for the obvious place this is needed.

		// These constants and enums are borrowed from fontconfig.h
		const string FC_FAMILY = "family";			/* String */
		const string FC_LANG = "lang";				/* String - RFC 3066 langs */
		enum FcMatchKind
		{
			FcMatchPattern,
			FcMatchFont,
			FcMatchScan
		};
		enum FcResult
		{
			FcResultMatch,
			FcResultNoMatch,
			FcResultTypeMismatch,
			FcResultNoId,
			FcResultOutOfMemory
		}
		[DllImport ("libfontconfig.so.1")]
		static extern IntPtr FcPatternCreate();
		[DllImport ("libfontconfig.so.1")]
		static extern int FcPatternAddString(IntPtr pattern, string fcObject, string stringValue);
		[DllImport ("libfontconfig.so.1")]
		static extern void FcDefaultSubstitute(IntPtr pattern);
		[DllImport ("libfontconfig.so.1")]
		static extern void FcPatternDestroy(IntPtr pattern);
		[DllImport ("libfontconfig.so.1")]
		static extern int FcConfigSubstitute(IntPtr config, IntPtr pattern, FcMatchKind kind);
		[DllImport ("libfontconfig.so.1")]
		static extern IntPtr FcFontMatch(IntPtr config, IntPtr pattern, out FcResult result);
		// Note that the output string from this method must NOT be freed, so we have to play games
		// with deferred marshalling.
		[DllImport ("libfontconfig.so.1")]
		static extern FcResult FcPatternGetString(IntPtr pattern, string fcObject, int index, out IntPtr stringValue);

		/// <summary>
		/// Store the mapping from language to font to save future computation.
		/// </summary>
		static Dictionary<string, string> m_mapLangToFont = new Dictionary<string, string>();

		/// <summary>
		/// Get the name of the default font for the given language tag.
		/// </summary>
		/// <param name="lang">ISO 3066 tag for the language (e.g., "en", "es", "zh-CN", etc.)</param>
		/// <returns>Name of the font, or <c>null</c> if not found.</returns>
		public static string GetFontNameForLanguage(string lang)
		{
			string fontName = null;
			if (m_mapLangToFont.TryGetValue(lang, out fontName))
				return fontName;
			IntPtr pattern = FcPatternCreate();
			int isOk = FcPatternAddString(pattern, FC_LANG, lang);
			if (isOk == 0)
			{
				FcPatternDestroy(pattern);
				return null;
			}
			isOk = FcConfigSubstitute(IntPtr.Zero, pattern, FcMatchKind.FcMatchPattern);
			if (isOk == 0)
			{
				FcPatternDestroy(pattern);
				return null;
			}
			FcDefaultSubstitute(pattern);
			FcResult result;
			IntPtr fullPattern = FcFontMatch(IntPtr.Zero, pattern, out result);
			if (result != FcResult.FcResultMatch)
			{
				FcPatternDestroy(pattern);
				FcPatternDestroy(fullPattern);
				return null;
			}
			FcPatternDestroy(pattern);
			IntPtr res;
			FcResult fcRes = FcPatternGetString(fullPattern, FC_FAMILY, 0, out res);
			if (fcRes == FcResult.FcResultMatch)
			{
				fontName = Marshal.PtrToStringAuto(res);
				m_mapLangToFont.Add(lang, fontName);
			}
			FcPatternDestroy(fullPattern);
			return fontName;
		}
#endif

		/// <summary>
		/// Whenever possible use this in place of new PalasoWritingSystemManager.
		/// It sets the TemplateFolder, which unfortunately the constructor cannot do because
		/// the direction of our dependencies does not allow it to reference FwUtils and access FwDirectoryFinder.
		/// </summary>
		/// <returns></returns>
		public static WritingSystemManager CreateWritingSystemManager()
		{
			return new WritingSystemManager {TemplateFolder = FwDirectoryFinder.TemplateDirectory};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Translate mouse buttons received from a Win API mouse message to .NET mouse buttons
		/// </summary>
		/// <param name="winMouseButtons">Windows mouse buttons</param>
		/// <returns>.NET mouse buttons</returns>
		/// ------------------------------------------------------------------------------------
		public static MouseButtons TranslateMouseButtons(Win32.MouseButtons winMouseButtons)
		{
			MouseButtons mouseButton = MouseButtons.None;
			if ((winMouseButtons & Win32.MouseButtons.MK_LBUTTON) == Win32.MouseButtons.MK_LBUTTON)
				mouseButton |= MouseButtons.Left;
			if ((winMouseButtons & Win32.MouseButtons.MK_RBUTTON) == Win32.MouseButtons.MK_RBUTTON)
				mouseButton |= MouseButtons.Right;
			if ((winMouseButtons & Win32.MouseButtons.MK_MBUTTON) == Win32.MouseButtons.MK_MBUTTON)
				mouseButton |= MouseButtons.Middle;
			if ((winMouseButtons & Win32.MouseButtons.MK_XBUTTON1) == Win32.MouseButtons.MK_XBUTTON1)
				mouseButton |= MouseButtons.XButton1;
			if ((winMouseButtons & Win32.MouseButtons.MK_XBUTTON2) == Win32.MouseButtons.MK_XBUTTON2)
				mouseButton |= MouseButtons.XButton2;

			return mouseButton;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the window handle is a child window of the specified parent form.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="hWnd">The window handle.</param>
		/// <returns><c>true</c> if the window is a child window of the parent form; otherwise,
		/// <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsChildWindowOfForm(Form parent, IntPtr hWnd)
		{
			if (parent == null)
				return false;

			// Try to get the managed window for the handle. We will get one only if hWnd is
			// a child window of this application, so we can return true.
			if (Control.FromHandle(hWnd) != null)
				return true;

			// Otherwise hWnd might be the handle of an unmanaged window. We look at all
			// parents and grandparents... to see if we eventually belong to the application
			// window.
			IntPtr hWndParent = hWnd;
			while (hWndParent != IntPtr.Zero)
			{
				hWnd = hWndParent;
				hWndParent = Win32.GetParent(hWnd);
			}
			return (hWnd == parent.Handle);
		}

		/// <summary>
		/// Finds the first control of the given name under the parentControl.
		/// </summary>
		/// <param name="parentControl"></param>
		/// <param name="nameOfChildToFocus"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "controls contains references")]
		public static Control FindControl(Control parentControl, string nameOfChildToFocus)
		{
			if (string.IsNullOrEmpty(nameOfChildToFocus))
			{
				return null;
			}
			if (parentControl.Name == nameOfChildToFocus)
			{
				return parentControl;
			}
			var controls = parentControl.Controls.Find(nameOfChildToFocus, true);
			return controls.Length > 0 ? controls[0] : null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Takes a string in the form "{X=l,Y=t,Width=w,Height=h}" (where l, t, w, and h are
		/// X, Y, width and height values) and returns a corresponding rectangle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Rectangle GetRcFromString(string str)
		{
			str = str.Replace("{", string.Empty);
			str = str.Replace("}", string.Empty);
			str = str.Replace("X=", string.Empty);
			str = str.Replace("Y=", string.Empty);
			str = str.Replace("Width=", string.Empty);
			str = str.Replace("Height=", string.Empty);

			string[] strVals = str.Split(",".ToCharArray(), 4);
			Rectangle rc = Rectangle.Empty;

			if (strVals != null)
			{
				int val;
				if (strVals.Length > 0 && int.TryParse(strVals[0], out val))
					rc.X = val;
				if (strVals.Length > 1 && int.TryParse(strVals[1], out val))
					rc.Y = val;
				if (strVals.Length > 2 && int.TryParse(strVals[2], out val))
					rc.Width = val;
				if (strVals.Length > 3 && int.TryParse(strVals[3], out val))
					rc.Height = val;
			}

			return rc;
		}

		/// <summary>
		/// calls Disposes on a Winforms Control, Calling BeginInvoke if needed.
		/// If BeginInvoke is needed then the Control handle is created if needed.
		/// </summary>
		public static void DisposeOnGuiThread(this Control control)
		{
			if (control.InvokeRequired)
				control.SafeBeginInvoke(new MethodInvoker(control.Dispose));
			else
				control.Dispose();
		}

		/// <summary>
		/// a BeginInvoke extenstion that causes the Control handle to be created if needed before calling BeginInvoke
		/// </summary>
		public static IAsyncResult SafeBeginInvoke(this Control control, Delegate method)
		{
			return SafeBeginInvoke(control, method, null);
		}

		/// <summary>
		/// a BeginInvoke that extenstion causes the Control handle to be created if needed before calling BeginInvoke
		/// </summary>
		public static IAsyncResult SafeBeginInvoke(this Control control, Delegate method, Object[] args)
		{
			// JohnT: I found this method without the first if statement, and added it because if an invoke
			// is required...the usual reason for calling this...and it already has a handle, calling control.Handle crashes.
			// I'm still nervous about the method because there are possible race conditions; for example, if some other
			// thread gives it a handle between when we test IsHandleCreated and when we call Handle, we'll crash.
			// Also, although it works, it's not supposed to be safe to call IsHandleCreated without Invoke.
			// I'm reluctantly leaving it like this because I don't see how to make it safe.
			// Given that it mostly worked before, it seems existng callers must typically call it with controls
			// that don't have handles, and expect to get them created.
			// There is however at least one case...disposing a progress bar when TE is creating key terms while
			// opening a new project...where control.IsHandleCreated is true.
			if (control.IsHandleCreated)
			{
				return control.BeginInvoke(method, args);
			}

			else if (control.Handle != IntPtr.Zero) // will typically create the handle, since it doesn't already have one.
			{
				// now the handle is created in THIS thread!
				return control.BeginInvoke(method, args);
			}

			// this should never happen.
			return null;
		}

	}

}
