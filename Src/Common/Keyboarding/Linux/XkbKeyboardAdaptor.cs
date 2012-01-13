// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using System.Collections.Generic;
using System.Diagnostics;
using X11.XKlavier;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Keyboarding.Linux
{
	/// <summary>
	/// Class for handling xkb keyboards on Linux
	/// </summary>
	internal class XkbKeyboardAdaptor: IKeyboardAdaptor
	{
		private static Type XklEngineType = typeof(XklEngine);

		private List<IKeyboardErrorDescription> m_BadLocales;
		private XklEngine m_engine;

		/// <summary>
		/// Sets the type of the XklEngine. This is useful for unit tests.
		/// </summary>
		internal static void SetXklEngineType<T>() where T: XklEngine
		{
			XklEngineType = typeof(T);
		}

		public XkbKeyboardAdaptor()
		{
			m_engine = Activator.CreateInstance(XklEngineType) as XklEngine;
		}

		/// <summary>
		/// Gets the IcuLocales by language and country. The 3-letter language and country codes
		/// are concatenated with an underscore in between, e.g. fra_BEL
		/// </summary>
		private Dictionary<string, IcuLocale> IcuLocalesByLanguageCountry
		{
			get
			{
				var localesByLanguageCountry = new Dictionary<string, IcuLocale>();
				for (int i = 0; i < Icu.CountAvailableLocales(); i++)
				{
					var localeId = string.Copy(Icu.GetAvailableLocale(i));
					var icuLocale = new IcuLocale(localeId);
					if (string.IsNullOrEmpty(icuLocale.LanguageCountry) ||
						localesByLanguageCountry.ContainsKey(icuLocale.LanguageCountry))
					{
						continue;
					}
					localesByLanguageCountry[icuLocale.LanguageCountry] = icuLocale;
				}
				return localesByLanguageCountry;
			}
		}

		private void InitLocales()
		{
			if (m_BadLocales != null)
				return;

			m_BadLocales = new List<IKeyboardErrorDescription>();

			var configRegistry = XklConfigRegistry.Create(m_engine);
			var layouts = configRegistry.Layouts;
			var icuLocales = IcuLocalesByLanguageCountry;

			for (int iGroup = 0; iGroup < m_engine.GroupNames.Length; iGroup++)
			{
				// a group in a xkb keyboard is a keyboard layout. This can be used with
				// multiple languages - which language is ambigious. Here we just add all
				// of them.
				var groupName = m_engine.GroupNames[iGroup];
				List<XklConfigRegistry.LayoutDescription> layoutList;
				if (!layouts.TryGetValue(groupName, out layoutList))
				{
					// No language in layouts uses the groupName keyboard layout.
					m_BadLocales.Add(new KeyboardErrorDescription(groupName));
					continue;
				}

				string unrecognizedLayout = null;
				for (int iLayout = 0; iLayout < layoutList.Count; iLayout++)
				{
					var layout = layoutList[iLayout];
					string description;
					if (string.IsNullOrEmpty(layout.LayoutVariant))
						description = string.Format("{0} ({1})", layout.Language, layout.Country);
					else
					{
						description = string.Format("{0} ({1}) - {2}", layout.Language,
							layout.Country, layout.LayoutVariant);
					}

					IcuLocale icuLocale;
					int lcid;
					if (icuLocales.TryGetValue(layout.Locale, out icuLocale))
						lcid = icuLocale.LCID;
					else
						lcid = Icu.GetLCID(layout.Locale);

					if (lcid <= 0)
					{
						if (iLayout == 0)
							unrecognizedLayout = groupName;
					}
					else
					{
						// if we find the LCID for at least one layout, we don't report
						// the other failing variations of this layout as error.
						unrecognizedLayout = null;
						var keyboard = new XkbKeyboardDescription(lcid, description, this, iGroup);
						KeyboardController.Manager.RegisterKeyboard(lcid, keyboard);
					}
				}
				if (unrecognizedLayout != null)
					m_BadLocales.Add(new KeyboardErrorDescription(unrecognizedLayout));
			}
		}

		public List<IKeyboardErrorDescription> ErrorKeyboards
		{
			get
			{
				InitLocales();
				return m_BadLocales;
			}
		}

		public void Initialize()
		{
			InitLocales();
		}

		public void Close()
		{
		}

		public void ActivateKeyboard(IKeyboardDescription keyboard,
			IKeyboardDescription ignored)
		{
			Debug.Assert(keyboard.Engine == this);
			Debug.Assert(keyboard is XkbKeyboardDescription);
			var xkbKeyboard = keyboard as XkbKeyboardDescription;
			if (xkbKeyboard == null)
				throw new ArgumentException();

			m_engine.SetGroup(xkbKeyboard.GroupIndex);
		}

		public void DeactivateKeyboard(IKeyboardDescription keyboard)
		{
		}
	}
}
#endif
