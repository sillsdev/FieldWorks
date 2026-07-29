// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Activates the per-writing-system keyboard for editor rows on the Avalonia surface; lives
	/// in xWorks because it needs the LCModel cache (the FwAvalonia view layer is intentionally
	/// LCModel-free).
	/// </summary>
	public static class WritingSystemKeyboards
	{
		/// <summary>
		/// Activates the writing system's configured keyboard (Keyman/Windows IME) when its editor
		/// row gains focus on the Avalonia surface — the behavior legacy slices get from
		/// <c>EditingHelper.SetKeyboardForWs</c>. Unknown tags fall back to the default keyboard.
		/// </summary>
		public static void Activate(LcmCache cache, string wsTag)
		{
			try
			{
				foreach (var ws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
				{
					if (ws.Id == wsTag)
					{
						ws.LocalKeyboard?.Activate();
						return;
					}
				}

				SIL.Keyboarding.Keyboard.Controller.ActivateDefaultKeyboard();
			}
			catch (Exception)
			{
				// Keyboard switching must never take down editing; legacy swallows comparable failures.
			}
		}
	}
}
