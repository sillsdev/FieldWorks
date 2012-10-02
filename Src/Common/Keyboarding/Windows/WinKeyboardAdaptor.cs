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
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Keyboarding.Windows
{
	internal class WinKeyboardAdaptor: IKeyboardAdaptor
	{
		private List<IKeyboardErrorDescription> m_BadLocales;

		public WinKeyboardAdaptor()
		{
		}

		public void Initialize()
		{
			GetLocales();
		}

		public void Close()
		{
		}

		public List<IKeyboardErrorDescription> ErrorKeyboards
		{
			get { return m_BadLocales; }
		}

		private void GetLocales()
		{
			// REVIEW (EberhardB): Could we use InputLanguage instead of LgLanguageEnumerator?

			ILgLanguageEnumerator lenum = LgLanguageEnumeratorClass.Create();
			int id = 0;
			m_BadLocales = new List<IKeyboardErrorDescription>();
			try
			{
				lenum.Init();
				for (; ;)
				{
					string name;
					try
					{
						lenum.Next(out id, out name);
					}
					catch (OutOfMemoryException)
					{
						throw;
					}
					catch
					{ // if we fail to get a language, skip this one, but display once in error message.
						m_BadLocales.Add(new KeyboardErrorDescription(KeyboardType.System, id));
						// Under certain conditions it can happen that lenum.Next() returns
						// E_UNEXPECTED right away. We're then stuck in an infinite loop.
						if (m_BadLocales.Count > 1000 || id == 0)
							break;
						continue;
					}
					if (id == 0)
						break;
					KeyboardController.Manager.RegisterKeyboard(id, new KeyboardDescription(id, name, this));
				}
			}
			finally
			{
				// LT-8465 when Windows and Language Options changes are made lenum does not
				// always get updated correctly so we are ensuring the memory for this
				// ComObject gets released.
				Marshal.FinalReleaseComObject(lenum);
			}
		}

		public void ActivateKeyboard(IKeyboardDescription keyboard, IKeyboardDescription systemKeyboard)
		{
			// TODO
			throw new NotImplementedException();
		}

		public void DeactivateKeyboard(IKeyboardDescription keyboard)
		{
		}
	}
}
#endif
