// Copyright (c) 2009-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using System.Media;
using System.Runtime.InteropServices;
#if __MonoCS__
using System.Collections.Generic;
using System.IO;
#endif
using SIL.CoreImpl.WritingSystems;

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
		public const string ksFullFlexAppObjectName = "SIL.FieldWorks.XWorks.LexText.LexTextApp";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a name suitable for use as a pipe name from the specified project handle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GeneratePipeHandle(string handle)
		{
			const string ksSuiteIdPrefix = ksSuiteName + ":";
			return (handle.StartsWith(ksSuiteIdPrefix) ? String.Empty : ksSuiteIdPrefix) +
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Takes a string in the form "{X=l,Y=t,Width=w,Height=h}" (where l, t, w, and h are
		/// X, Y, width and height values) and returns a corresponding rectangle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Rectangle GetRcFromString(string str)
		{
			str = str.Replace("{", String.Empty);
			str = str.Replace("}", String.Empty);
			str = str.Replace("X=", String.Empty);
			str = str.Replace("Y=", String.Empty);
			str = str.Replace("Width=", String.Empty);
			str = str.Replace("Height=", String.Empty);

			string[] strVals = str.Split(",".ToCharArray(), 4);
			Rectangle rc = Rectangle.Empty;

			if (strVals != null)
			{
				int val;
				if (strVals.Length > 0 && Int32.TryParse(strVals[0], out val))
					rc.X = val;
				if (strVals.Length > 1 && Int32.TryParse(strVals[1], out val))
					rc.Y = val;
				if (strVals.Length > 2 && Int32.TryParse(strVals[2], out val))
					rc.Width = val;
				if (strVals.Length > 3 && Int32.TryParse(strVals[3], out val))
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

		/// <summary>
		/// Unit tests can set this to true to suppress error beeps
		/// </summary>
		public static bool SuppressErrorBeep { get; set; }

		/// ------------------------------------------------------------------------------------
		public static void ErrorBeep()
		{
			if (!SuppressErrorBeep)
				SystemSounds.Beep.Play();
		}

		#region Advapi32.dll
		// Requires Windows NT 3.1 or later
		// From http://www.codeproject.com/useritems/processownersid.asp

		private const int TOKEN_QUERY = 0X00000008;

		private const int ERROR_NO_MORE_ITEMS = 259;

		private enum TOKEN_INFORMATION_CLASS
		{
			TokenUser = 1,
			TokenGroups,
			TokenPrivileges,
			TokenOwner,
			TokenPrimaryGroup,
			TokenDefaultDacl,
			TokenSource,
			TokenType,
			TokenImpersonationLevel,
			TokenStatistics,
			TokenRestrictedSids,
			TokenSessionId
		}

		/// <summary>The TOKEN_USER structure identifies the user associated with an access token.</summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct TOKEN_USER
		{
			/// <summary>Specifies a SID_AND_ATTRIBUTES structure representing the user
			/// associated with the access token. There are currently no attributes defined for
			/// user security identifiers (SIDs).</summary>
			public SID_AND_ATTRIBUTES User;
		}

		/// <summary>The SID_AND_ATTRIBUTES structure represents a security identifier (SID) and
		/// its attributes. SIDs are used to uniquely identify users or groups.</summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct SID_AND_ATTRIBUTES
		{
			/// <summary>Pointer to a SID structure. </summary>
			public IntPtr Sid;
			/// <summary>Specifies attributes of the SID. This value contains up to 32 one-bit
			/// flags. Its meaning depends on the definition and use of the SID.</summary>
			public int Attributes;
		}

#if !__MonoCS__
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The OpenProcessToken function opens the access token associated with a process.
		/// </summary>
		/// <param name="processHandle">[in] Handle to the process whose access token is opened.
		/// The process must have the PROCESS_QUERY_INFORMATION access permission. </param>
		/// <param name="desiredAccess">[in] Specifies an access mask that specifies the
		/// requested types of access to the access token. These requested access types are
		/// compared with the discretionary access control list (DACL) of the token to determine
		/// which accesses are granted or denied. </param>
		/// <param name="tokenHandle">[out] Pointer to a handle that identifies the newly opened
		/// access token when the function returns. </param>
		/// <returns>If the function succeeds, the return value is <c>true</c>.
		/// If the function fails, the return value is <c>false</c>. To get extended error
		/// information, call GetLastError.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("advapi32")]
		private static extern bool OpenProcessToken(
			IntPtr processHandle, // handle to process
			int desiredAccess, // desired access to process
			out IntPtr tokenHandle); // handle to open access token

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The GetTokenInformation function retrieves a specified type of information about an
		/// access token. The calling process must have appropriate access rights to obtain the
		/// information.
		/// </summary>
		/// <param name="hToken">[in] Handle to an access token from which information is
		/// retrieved. If TokenInformationClass specifies TokenSource, the handle must have
		/// TOKEN_QUERY_SOURCE access. For all other TokenInformationClass values, the handle
		/// must have TOKEN_QUERY access.</param>
		/// <param name="tokenInfoClass">[in] Specifies a value from the TOKEN_INFORMATION_CLASS
		/// enumerated type to identify the type of information the function retrieves.</param>
		/// <param name="tokenInformation">[out] Pointer to a buffer the function fills with the
		/// requested information. The structure put into this buffer depends upon the type of
		/// information specified by the TokenInformationClass parameter (see MSDN).</param>
		/// <param name="tokeInfoLength">[in] Specifies the size, in bytes, of the buffer
		/// pointed to by the TokenInformation parameter. If TokenInformation is <c>null</c>,
		/// this parameter must be zero.</param>
		/// <param name="returnLength"><para>[out] Pointer to a variable that receives the number of
		/// bytes needed for the buffer pointed to by the TokenInformation parameter. If this
		/// value is larger than the value specified in the TokenInformationLength parameter,
		/// the function fails and stores no data in the buffer.</para>
		/// <para>If the value of the TokenInformationClass parameter is TokenDefaultDacl and
		/// the token has no default DACL, the function sets the variable pointed to by
		/// ReturnLength to sizeof(TOKEN_DEFAULT_DACL) and sets the DefaultDacl member of the
		/// TOKEN_DEFAULT_DACL structure to NULL.</para></param>
		/// <returns>If the function succeeds, the return value is <c>true</c>.
		/// If the function fails, the return value is <c>false</c>. To get extended error
		/// information, call GetLastError.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("advapi32", CharSet = CharSet.Auto)]
		private static extern bool GetTokenInformation(
			IntPtr hToken,
			TOKEN_INFORMATION_CLASS tokenInfoClass,
			IntPtr tokenInformation,
			int tokeInfoLength,
			out int returnLength);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The CloseHandle function closes an open object handle.
		/// </summary>
		/// <param name="handle">[in] Handle to an open object. This parameter can be a pseudo
		/// handle or INVALID_HANDLE_VALUE. </param>
		/// <returns><c>true</c> if the function succeeds, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("kernel32")]
		private static extern bool CloseHandle(IntPtr handle);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The ConvertSidToStringSid function converts a security identifier (SID) to a string
		/// format suitable for display, storage, or transmission.
		/// </summary>
		/// <param name="sid">[in] Pointer to the SID structure to convert.</param>
		/// <param name="stringSid">[out] The SID string.</param>
		/// <returns><c>true</c> if the function succeeds, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[DllImport("advapi32", CharSet = CharSet.Auto)]
		private static extern bool ConvertSidToStringSid(
			IntPtr sid,
			[MarshalAs(UnmanagedType.LPTStr)] out string stringSid);
#endif
		#endregion

		#region Helper methods
#if !__MonoCS__
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the user SID for the given process token.
		/// </summary>
		/// <param name="hToken">The process token.</param>
		/// <returns>The SID of the user that owns the process, or <c>IntPtr.Zero</c> if we
		/// can't get the user information (e.g. insufficient permissions to retrieve this
		/// information)</returns>
		/// ------------------------------------------------------------------------------------
		private static IntPtr GetSidForProcessToken(IntPtr hToken)
		{
			int bufferLen = 256;
			IntPtr buffer = Marshal.AllocHGlobal(bufferLen);

			try
			{
				int returnLen;
				if (GetTokenInformation(hToken, TOKEN_INFORMATION_CLASS.TokenUser, buffer,
					bufferLen, out returnLen))
				{
					TOKEN_USER tokUser;
					tokUser = (TOKEN_USER)Marshal.PtrToStructure(buffer, typeof(TOKEN_USER));
					return tokUser.User.Sid;
				}
				return IntPtr.Zero;
			}
			catch (Exception ex)
			{
				// Just ignore exceptions and return false
				Debug.WriteLine("ProcessTokenToSid failed: " + ex.Message);
				return IntPtr.Zero;
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}
#endif
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the user for the given process.
		/// </summary>
		/// <param name="process">The process.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetUserForProcess(Process process)
		{
			try
			{
#if !__MonoCS__
				IntPtr procToken;
				string sidString = null;
				if (OpenProcessToken(process.Handle, TOKEN_QUERY, out procToken))
				{
					IntPtr sid = GetSidForProcessToken(procToken);
					if (sid != IntPtr.Zero)
						ConvertSidToStringSid(sid, out sidString);

					CloseHandle(procToken);
				}
				return sidString;
#else
				return process.StartInfo.UserName;
#endif
			}
			catch (Exception ex)
			{
				Debug.WriteLine("GetUserForProcess failed: " + ex.Message);
				return "";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the name of the TestLangProj database.
		/// This is to allow multiple users to test with same database server and different
		/// named TestLangProj files.
		/// </summary>
		/// <returns>TestLangProj name</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetTestLangProjDataBaseName()
		{
			string dbName = Environment.GetEnvironmentVariable("TE_DATABASE");
			if (string.IsNullOrEmpty(dbName))
				return "TestLangProj";
			return dbName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the exception <paramref name="e"/> is caused by an unsupported
		/// culture, i.e. a culture that neither Windows nor .NET support and there is no
		/// custom culture (see LT-8248).
		/// </summary>
		/// <param name="e">The exception.</param>
		/// <returns><c>true</c> if the exception is caused by an unsupported culture;
		/// otherwise, <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsUnsupportedCultureException(Exception e)
		{
			return e is CultureNotFoundException;
		}
	}

}
