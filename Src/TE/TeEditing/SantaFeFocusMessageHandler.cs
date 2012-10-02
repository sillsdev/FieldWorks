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
// File: SantaFeFocusMessageHandler.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles Santa Fe synchronized scrolling/focus messages
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class SantaFeFocusMessageHandler
	{
		#region Windows API methods
		/// <summary>The RegisterWindowMessage function defines a new window message that is
		/// guaranteed to be unique throughout the system. The message value can be used when
		/// sending or posting messages.</summary>
		/// <param name="name">unique name of a message</param>
		/// <returns>message identifier in the range 0xC000 through 0xFFFF, or 0 if an error
		/// occurs</returns>
		[DllImport("User32.dll")]
		private static extern uint RegisterWindowMessage(string name);

		/// <summary></summary>
		[DllImport("User32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool PostMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);
		#endregion

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Types of "focus sharing" supported by TE (must match SanatFe spec)
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private enum FocusTypes
		{
			/// <summary></summary>
			ScriptureReferenceFocus = 1,
		}

		#region Member variables
		/// <summary>
		/// The registry key for synchronizing apps to a Scripture reference (must match SanatFe spec)
		/// </summary>
		private static readonly RegistryKey s_SantaFeRefKey =
			Registry.CurrentUser.CreateSubKey(@"Software\SantaFe\Focus\ScriptureReference");

		/// <summary>
		/// The Windows message used for synchronized scrolling (must match SanatFe spec)
		/// </summary>
		private static int s_FocusMsg = (int)RegisterWindowMessage("SantaFeFocus");
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notify all Santa Fe windows that a Scripture Reference focus change has occured.
		/// </summary>
		/// <param name="sRef">The string representation of the reference (e.g. MAT 1:1)</param>
		/// ------------------------------------------------------------------------------------
		public static void SendFocusMessage(string sRef)
		{
			BCVRef bcvRef = new BCVRef(sRef);
			if (!bcvRef.Valid)
				return;
			s_SantaFeRefKey.SetValue(null, sRef);
			PostMessage(new IntPtr(-1), s_FocusMsg, (uint)FocusTypes.ScriptureReferenceFocus, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Receives the focus message.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <returns>The string representation of the reference (e.g. MAT 1:1)</returns>
		/// ------------------------------------------------------------------------------------
		public static string ReceiveFocusMessage(Message msg)
		{
			int focusType = msg.WParam.ToInt32();
			if (focusType != (int)FocusTypes.ScriptureReferenceFocus)
				return string.Empty;

			return s_SantaFeRefKey.GetValue(null).ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the focus message.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int FocusMsg
		{
			get { return s_FocusMsg; }
		}
	}
}
