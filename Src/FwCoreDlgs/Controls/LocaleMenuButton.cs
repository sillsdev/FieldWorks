// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Icu;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary />
	public class LocaleMenuButton : Button
	{
		private Container components;

		// Key - language ID.
		// Value - collection of locales starting with that language id.
		private Dictionary<string, List<LocaleMenuItemData>> m_locales;

		// Items are LocaleMenuItemData; an entry is added for each main menu
		// (name and ID are taken from the unmarked locale for that language);
		// an entry is also added for each locale where there is no unmarked
		// locale for its language.
		private List<LocaleMenuItemData> m_mainItems;

		// Key - MenuItem
		// Value - LocaleMenuItemData
		private Dictionary<ToolStripMenuItem, LocaleMenuItemData> m_itemData;

		// ID of locale to use for getting display names.
		private string m_selectedLocale;

		/// <summary />
		public LocaleMenuButton()
		{
			components = new Container();
			Image = ResourceHelper.ButtonMenuArrowIcon;
			ImageAlign = ContentAlignment.MiddleRight;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets or sets the selected locale id.
		/// </summary>
		public string SelectedLocaleId
		{
			get
			{
				return m_selectedLocale;
			}
			set
			{
				// This gets called during initialization with null.
				if (value == null)
				{
					return; // Probably initialization.
				}
				string displayName;
				if (TryGetDisplayName(value, out displayName))
				{
					Text = displayName;
					m_selectedLocale = value;
				}
				else
				{
					// Client should make sure it is a valid id. Failing this, make a warning.
					Text = Strings.kstidError;
				}
			}
		}

		private bool TryGetDisplayName(string locale, out string displayName)
		{
			try
			{
				displayName = new Locale().GetDisplayName(locale);
				return true;
			}
			catch
			{
				displayName = string.Empty;
				return false;
			}
		}

		/// <summary>
		/// Determine whether the specified locale is a custom one the user is allowed to modify.
		/// </summary>
		public bool IsCustomLocale(string localeId)
		{
			using (var rbroot = new ResourceBundle(null, "en"))
			using (var rbCustom = rbroot["Custom"])
			using (var rbCustomLocales = rbCustom["LocalesAdded"])
			{
				if (rbCustomLocales.GetStringContents().Contains(localeId))
				{
					return true;
				}
			}

			// Next, check if ICU knows about this locale. If ICU doesn't know about it, it is considered custom.
			string dummyDisplayName;
			return !TryGetDisplayName(localeId, out dummyDisplayName);
		}

		/// <summary>
		/// The locale to use for getting display names. If this is left blank the
		/// system default locale is used.
		/// </summary>
		public string DisplayLocaleId { get; set; }

		/// <summary>Event that occurs when the user chooses a locale.</summary>
		public event EventHandler LocaleSelected;

		/// <summary>
		/// Handle the LocaleSelected event; by default just calls delegates.
		/// </summary>
		protected virtual void OnLocaleSelected(EventArgs ea)
		{
			LocaleSelected?.Invoke(this, ea);
		}

		/// <summary>
		/// Get a dictionary, keyed by language ID, with values being a list of locale IDs and names.
		/// This allows "en-GB" and "en-US" to be grouped together under "English" in a menu, for example.
		/// </summary>
		/// <param name="displayLocale">Locale ID in which to display the locale names (e.g., if this is "fr", then the "en" locale will have the display name "anglais").</param>
		/// <returns>A dictionary whose keys are language IDs (2- or 3-letter ISO codes) and whose values are a list of the IcuIdAndName objects that GetLocaleIdsAndNames returns.</returns>
		private static IDictionary<string, IList<IcuIdAndName>> GetLocalesByLanguage(string displayLocale = null)
		{
			var result = new Dictionary<string, IList<IcuIdAndName>>();
			foreach (var locale in Locale.AvailableLocales)
			{
				var name = locale.GetDisplayName(displayLocale);
				IList<IcuIdAndName> entries;
				if (!result.TryGetValue(locale.Language, out entries))
				{
					entries = new List<IcuIdAndName>();
					result[locale.Language] = entries;
				}
				entries.Add(new IcuIdAndName(locale.Id, name));
			}
			return result;
		}

		/// <inheritdoc />
		protected override void OnClick(EventArgs e)
		{
			var menu = components.ContextMenuStrip("contextMenu");

			// Create the various collections we use in the process of assembling
			// the menu.
			m_locales = new Dictionary<string, List<LocaleMenuItemData>>();
			m_mainItems = new List<LocaleMenuItemData>();
			m_itemData = new Dictionary<ToolStripMenuItem, LocaleMenuItemData>();

			var localeDataByLanguage = GetLocalesByLanguage(DisplayLocaleId);
			foreach (var localeData in localeDataByLanguage)
			{
				var langid = localeData.Key;
				var items = localeData.Value;
				m_locales[langid] = items.Select(idAndName => new LocaleMenuItemData(idAndName.Id, idAndName.Name)).ToList();
			}

			// Generate the secondary items. For each key in m_locales,
			// 1. If there is just one item in the List and its id is equal to the key,
			//		then this language has only one locale,
			//		the unmodified one that appears in the main list.
			// 2. If there is more than one item and one of them has an id equal to the key,
			//		make a single item in main list. It is a copy of the item whose id is
			//		equal to the key, that is, the most basic locale for this language.
			//		It has the arraylist for mainItems.
			// 3. Otherwise, we have a list of locales for which there is no basic locale
			//		whose id is equal to the language id. In this case, we make an item
			//		for each thing in the List, whether many or just one.
			foreach (var kvp in m_locales)
			{
				var langid = kvp.Key;
				var items = kvp.Value;
				var lmdRootItem = items.FirstOrDefault(lmd => lmd.m_id == langid);
				// See if there is an item in the array list that matches the langid
				if (lmdRootItem == null)
				{
					// case 3
					foreach (var lmd in items)
					{
						m_mainItems.Add(lmd);
					}
				}
				else
				{
					if (items.Count == 1)
					{
						// case 1
						var lmdMenu = new LocaleMenuItemData(lmdRootItem.m_id, lmdRootItem.m_displayName);
						m_mainItems.Add(lmdMenu);
					}
					else
					{
						// case 2
						var lmdMenu = new LocaleMenuItemData(lmdRootItem.m_id, lmdRootItem.m_displayName) { m_subitems = items };
						m_mainItems.Add(lmdMenu);
					}
				}
			}

			// Sort the items in each menu.
			m_mainItems.Sort();
			var noneData = new LocaleMenuItemData(Strings.kstid_None, Strings.kstid_None);
			var noneItem = new ToolStripMenuItem(noneData.m_displayName, null, ItemClickHandler);
			menu.Items.Add(noneItem); // This goes strictly at the beginning, irrespective of sorting

			foreach (var lmd in m_mainItems)
			{
				var mi = new ToolStripMenuItem(lmd.m_displayName, null, ItemClickHandler);
				menu.Items.Add(mi);
				m_itemData[mi] = lmd;
				if (lmd.m_subitems != null)
				{
					mi.DropDownOpened += mi_Popup;
					lmd.m_subitems.Sort();
					// To make the system realize this item is a submenu, we have to
					// add at least one item. To save time and space, we don't add the others
					// until it pops up.
					var lmdSub = lmd.m_subitems[0];
					var miSub = new ToolStripMenuItem(lmdSub.m_displayName, null, ItemClickHandler);
					mi.DropDownItems.Add(miSub);
					m_itemData[miSub] = lmdSub;

					// Turns out popup events don't happen in a .NET submenu, only for the top-level
					// menu. DavidO has a workaround that should soon be checked in. In the meantime,
					// generate the submenu at once. This may actually be fast enough to keep permanently.
					mi_Popup(mi, new EventArgs());
				}
			}

			if (MiscUtils.IsUnix)
			{
				menu.ShowWithOverflow(this, new Point(0, Height));
			}
			else
			{
				menu.Show(this, new Point(0, Height));
			}

			base.OnClick(e); // Review JohnT: is this useful or harmful or neither? MS recommends it.
		}

		private void ItemClickHandler(object sender, EventArgs e)
		{
			var mi = (ToolStripMenuItem)sender;
			if (mi.Text == Strings.kstid_None)
			{
				m_selectedLocale = null;
				Text = Strings.kstid_None;
				OnLocaleSelected(new EventArgs());
				return;
			}
			var lmd = m_itemData[mi];
			m_selectedLocale = lmd.m_id;
			Text = lmd.m_displayName;
			OnLocaleSelected(new EventArgs());
		}

		private void mi_Popup(object sender, EventArgs e)
		{
			var miBase = sender as ToolStripMenuItem;
			Debug.Assert(miBase != null);
			if (miBase.DropDownItems.Count > 1)
			{
				return; // already popped up, has items.
			}
			var lmd = m_itemData[miBase];
			var lmdFirst = lmd.m_subitems[0];
			foreach (var lmdSub in lmd.m_subitems)
			{
				// Skip the first item as it was added earlier.
				if (lmdSub == lmdFirst)
				{
					continue;
				}
				var miSub = new ToolStripMenuItem(lmdSub.m_displayName, null, ItemClickHandler);
				miBase.DropDownItems.Add(miSub);
				m_itemData[miSub] = lmdSub;
			}
		}
	}
}