// --------------------------------------------------------------------------------------------
// <copyright from='2008' to='2008' company='SIL International'>
//    Copyright (c) 2008, SIL International. All Rights Reserved.
// </copyright>
//
// File: LCIDHelper.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// Implementation of KeyboardHelper. The class contains static methods for switching keyboards.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class assists with keyboard switching.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class KeyboardHelper
	{
		private static readonly ILgTextServices s_lts = LgTextServicesClass.Create();

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
			//System.Diagnostics.Debug.WriteLine(
			//    "KeyboardHelper.ActivateKeyboard() -> ILgTextServices::SetKeyboard(" + lcid + ")");
			bool fSelectLangPending = false;
			s_lts.SetKeyboard(lcid, keymanKbd, ref activeLangId, ref activeKeymanKbd,
				ref fSelectLangPending);

			return fSelectLangPending;
		}
	}
}
