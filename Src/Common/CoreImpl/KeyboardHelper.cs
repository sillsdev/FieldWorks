// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2008' to='2009' company='SIL International'>
//    Copyright (c) 2009, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: KeyboardHelper.cs
// Responsibility: TE Team
//
// <remarks>
// Implementation of KeyboardHelper. The class contains static methods for switching keyboards.
// </remarks>
// --------------------------------------------------------------------------------------------
#define DEBUGGINGDISPOSE
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class assists with keyboard switching.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class KeyboardHelper
	{
		#region Implementation of KeyboardHelper
		/// <summary>
		/// Keyboard helper implementation. We provide a separate non-static implementation so
		/// that we can explicitly dispose of it when running tests.
		/// </summary>
		private sealed class KeyboardHelperImpl: IDisposable
		{
			private readonly ILgTextServices m_lts;
			private readonly ILgKeymanHandler m_keymanHandler;

			#region Constructor
			public KeyboardHelperImpl()
		{
				m_keymanHandler = LgKeymanHandlerClass.Create();

			if (!MiscUtils.IsUnix)
					m_lts = LgTextServicesClass.Create();
			}
			#endregion

			#region Disposable stuff
#if DEBUGGINGDISPOSE
			// NOTE: KeyboardHelperImpl is implemented as a singleton. It is usually ok that
			// this finalizer is called in production code. However, in tests we might get
			// hangs if we don't properly dispose. Ideally we would also dispose in production
			// code, but for now we're doing it the pragmatic way...
			// This finalizer is here only for tracking down dispose issues in tests. It will
			// cause the "Missing Dispose()" message to be written out.
			// If you're running the application and getting this message you should comment
			// the #define DEBUGGINGDISPOSE above.
			~KeyboardHelperImpl()
			{
				Dispose(false);
			}
#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				// If you get this message see note above ~KeyboardHelperImpl
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					if (m_keymanHandler != null)
						m_keymanHandler.Close();
				}
				if (m_keymanHandler != null && Marshal.IsComObject(m_keymanHandler))
					Marshal.ReleaseComObject(m_keymanHandler);
				if (m_lts != null && Marshal.IsComObject(m_lts))
					Marshal.ReleaseComObject(m_lts);
				IsDisposed = true;
			}
			#endregion

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Activates the specified keyboard.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public bool ActivateKeyboard(int lcid, string keymanKbd, ref int activeLangId,
				ref string activeKeymanKbd)
			{
				if (MiscUtils.IsUnix)
					return false;

				//System.Diagnostics.Debug.WriteLine(
				//    "KeyboardHelper.ActivateKeyboard() -> ILgTextServices::SetKeyboard(" + lcid + ")");
				bool fSelectLangPending = false;
				m_lts.SetKeyboard(lcid, keymanKbd, ref activeLangId, ref activeKeymanKbd,
					ref fSelectLangPending);

				return fSelectLangPending;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the active Keyman keyboard or an empty string if there is no active Keyman
			/// keyboard
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public string ActiveKeymanKeyboard
			{
				get
				{
					string sKeymanKbd = m_keymanHandler.ActiveKeyboardName;

					// This constant '(None)' can not be localized until the C++ version is localized.
					// Even then they should use the same resource.
					if (sKeymanKbd == null || sKeymanKbd == "(None)")
						sKeymanKbd = string.Empty;
					return sKeymanKbd;
				}
			}
		}
		#endregion

		#region Member variables
		private static KeyboardHelperImpl s_keyboardHelper;
		private static readonly object s_syncRoot = new object();
		#endregion

		private static KeyboardHelperImpl KeyboardHelperObject
		{
			get
			{
				if (s_keyboardHelper == null)
				{
					lock (s_syncRoot)
					{
						if (s_keyboardHelper == null)
							s_keyboardHelper = new KeyboardHelperImpl();
					}
				}
				return s_keyboardHelper;
			}
		}

		/// <summary>
		/// Release the KeyboardHelper singleton object
		/// </summary>
		public static void Release()
		{
			lock (s_syncRoot)
			{
				if (s_keyboardHelper != null)
				{
					s_keyboardHelper.Dispose();
					s_keyboardHelper = null;
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Activate the given keyboard.
		/// </summary>
		/// <remarks>On Windows 98, sending this message unnecessarily destroys
		/// the current keystroke context, so only do it when we're actually switching
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		public static void ActivateDefaultKeyboard()
		{
			InputLanguage inputLng = InputLanguage.DefaultInputLanguage;
			ActivateKeyboard(inputLng.Culture.LCID);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the specified keyboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ActivateKeyboard(int lcid)
		{
			return ActivateKeyboard(lcid, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the specified keyboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ActivateKeyboard(int lcid, string keymanKbd)
		{
			int langId = 0;
			string activeKeymanKbd = null;
			return ActivateKeyboard(lcid, keymanKbd, ref langId, ref activeKeymanKbd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the specified keyboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ActivateKeyboard(int lcid, ref int activeLangId,
			ref string activeKeymanKbd)
		{
			return ActivateKeyboard(lcid, null, ref activeLangId, ref activeKeymanKbd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the specified keyboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool ActivateKeyboard(int lcid, string keymanKbd, ref int activeLangId,
			ref string activeKeymanKbd)
		{
			return KeyboardHelperObject.ActivateKeyboard(lcid, keymanKbd, ref activeLangId, ref activeKeymanKbd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active Keyman keyboard or an empty string if there is no active Keyman
		/// keyboard
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string ActiveKeymanKeyboard
		{
			get { return KeyboardHelperObject.ActiveKeymanKeyboard; }
		}
	}
}
