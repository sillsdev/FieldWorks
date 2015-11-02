// Copyright (c) 2008-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is an almost-obsolete class with one function left. There is still one place where
	/// we need to know a Keyman keyboard is active.
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
			private readonly ILgKeymanHandler m_keymanHandler;

			#region Constructor

			public KeyboardHelperImpl()
			{
				m_keymanHandler = LgKeymanHandlerClass.Create();
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
				IsDisposed = true;
			}
			#endregion

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
					if (MiscUtils.IsUnix)
						return "";
					string sKeymanKbd = null;
					try
					{
						sKeymanKbd = m_keymanHandler.ActiveKeyboardName;
					}
					catch(COMException)
					{
						Logger.WriteEvent("COMException thrown trying to access the ActiveKeyboardName. Bad Keyman installation?");
					}

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active Keyman keyboard or an empty string if there is no active Keyman
		/// keyboard
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "KeyboardHelperObject returns a singleton")]
		public static string ActiveKeymanKeyboard
		{
			get { return KeyboardHelperObject.ActiveKeymanKeyboard; }
		}
	}
}
