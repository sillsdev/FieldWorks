// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
#if !__MonoCS__
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Keyboarding.Windows
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for handling Keyman keyboards
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class KeymanKeyboardAdapter: IKeyboardAdaptor
	{
		private List<IKeyboardErrorDescription> m_BadLocales;
		private ILgTextServices m_lts;
		private ILgKeymanHandler m_keymanHandler;

		public KeymanKeyboardAdapter()
		{
			m_keymanHandler = LgKeymanHandlerClass.Create();
			m_lts = LgTextServicesClass.Create();
		}

		#region IKeyboardAdaptor Members

		public void Initialize()
		{
			m_BadLocales = new List<IKeyboardErrorDescription>();
			try
			{
				// Update handler with any new/removed keyman keyboards
				m_keymanHandler.Init(true);
			}
			catch (Exception e)
			{
				m_BadLocales.Add(new KeyboardErrorDescription(KeyboardType.OtherIm, e));
				return;
			}

			for (int i = 0; i < m_keymanHandler.NLayout; i++)
			{
				var name = m_keymanHandler.get_Name(i);

				// JohnT: haven't been able to reproduce FWR-1935, but apparently there's some bizarre
				// circumstance where one of the names comes back null. If so, leave it out.
				if (!string.IsNullOrEmpty(name))
				{
					var desc = GetKeyboardDescription(name);
					KeyboardController.Manager.RegisterKeyboard(desc.Id, desc);
				}
			}
		}

		public void Close()
		{
			if (m_keymanHandler != null)
			{
				m_keymanHandler.Close();
				if (Marshal.IsComObject(m_keymanHandler))
					Marshal.ReleaseComObject(m_keymanHandler);
			}
			m_keymanHandler = null;

			if (m_lts != null && Marshal.IsComObject(m_lts))
				Marshal.ReleaseComObject(m_lts);
			m_lts = null;
		}

		public List<IKeyboardErrorDescription> ErrorKeyboards
		{
			get { return m_BadLocales; }
		}

		public void ActivateKeyboard(IKeyboardDescription keyboard, IKeyboardDescription systemKeyboard)
		{
			var fSelectLangPending = false;
			var activeLangId = 0;
			var keymanKeyboardName = string.Empty;

			m_lts.SetKeyboard(systemKeyboard.Id, keyboard.Name, ref activeLangId, ref keymanKeyboardName,
				ref fSelectLangPending);
		}

		public void DeactivateKeyboard(IKeyboardDescription keyboard)
		{
			m_keymanHandler.ActiveKeyboardName = null;
		}

		#endregion

		private IKeyboardDescription GetKeyboardDescription(string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			var id = name.GetHashCode();

			return new KeyboardDescription(id, name, this, KeyboardType.OtherIm);
		}

		public IKeyboardDescription ActiveKeyboard
		{
			get { return GetKeyboardDescription(m_keymanHandler.ActiveKeyboardName); }
		}
	}
}
#endif
