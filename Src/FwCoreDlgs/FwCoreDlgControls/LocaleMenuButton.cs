using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Resources;
using System.Diagnostics;
using System.Reflection; // for Assembly class to get resources.

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>
	/// Summary description for LocaleMenuButton.
	/// </summary>
	public class LocaleMenuButton : Button, IFWDisposable
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private ResourceManager m_res =
			new System.Resources.ResourceManager("SIL.FieldWorks.FwCoreDlgControls.FwCoreDlgControls",
			Assembly.GetExecutingAssembly());
		// Key - language ID.
		// Value - collection of locales starting with that language id.
		Dictionary<string, List<LocaleMenuItemData>> m_locales;

		// Items are LocaleMenuItemData; an entry is added for each mainmenu
		// (name and ID are taken from the unmarked locale for that language);
		// an entry is also added for each locale where there is no unmarked
		// locale for its language.
		List<LocaleMenuItemData> m_mainItems;

		// Key - MenuItem
		// Value - LocaleMenuItemData
		Dictionary<MenuItem, LocaleMenuItemData> m_itemData;

		string m_selectedLocale;
		// ID of locale to use for getting display names.
		string m_displayLocaleId;

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the selected locale id.
		/// </summary>
		/// <value>The selected locale id.</value>
		/// ------------------------------------------------------------------------------------
		public string SelectedLocaleId
		{
			get
			{
				CheckDisposed();
				return m_selectedLocale;
			}
			set
			{
				CheckDisposed();

				// This gets called during initialization with null. If we get
				// an Enumerator, it locks root.res. For some reason the
				// destructor for the enumerator doesn't get called for a long
				// time.
				if (value == null)
					return; // Probably initialization.
				ILgIcuLocaleEnumerator locEnum = LgIcuLocaleEnumeratorClass.Create();

				try
				{
					int cloc = locEnum.Count;
					for (int iloc = 0; iloc < cloc; iloc++)
					{
						if (locEnum.get_Name(iloc) == value)
						{
							Text = locEnum.get_DisplayName(iloc, m_displayLocaleId);
							m_selectedLocale = value;
							return;
						}
					}
					// Client should make sure it is a valid id. Failing this, make a warning.
					Text = FwCoreDlgControls.kstidError;
				}
				finally
				{
					System.Runtime.InteropServices.Marshal.ReleaseComObject(locEnum);
				}
			}
		}

		/// <summary>
		/// Determine whether the specified locale is a custom one the user is allowed to modify.
		/// </summary>
		/// <param name="localeId"></param>
		/// <param name="rbroot"></param>
		/// <returns></returns>
		public bool IsCustomLocale(string localeId, ILgIcuResourceBundle rbroot)
		{
			ILgIcuResourceBundle rbCustom = rbroot.get_GetSubsection("Custom");
			if (rbCustom != null)
			{
				ILgIcuResourceBundle rbCustomLocales = rbCustom.get_GetSubsection("LocalesAdded");
				System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCustom);
				if (rbCustomLocales == null)
					return false;	// Should never be.
				while (rbCustomLocales.HasNext)
				{
					ILgIcuResourceBundle rbItem = rbCustomLocales.Next;
					if (rbItem.String == localeId)
					{
						System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
						System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCustomLocales);
						return true;
					}
					System.Runtime.InteropServices.Marshal.ReleaseComObject(rbItem);
				}
				System.Runtime.InteropServices.Marshal.ReleaseComObject(rbCustomLocales);
			}
			// Now, compare the locale againt all known locales -- it may not exist at all yet!
			// If not, it is considered custom.
			ILgIcuLocaleEnumerator locEnum = LgIcuLocaleEnumeratorClass.Create();

			int cloc = locEnum.Count;
			for(int iloc = 0; iloc < cloc; iloc++)
			{
				if (localeId == locEnum.get_Name(iloc))
				{
					System.Runtime.InteropServices.Marshal.ReleaseComObject(locEnum);
					return false;
				}
			}
			//Didn't find in either list...custom.
			System.Runtime.InteropServices.Marshal.ReleaseComObject(locEnum);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We are going to be used to choose a 'similar locale' for the specified
		/// locale. If this is a standard, built-in locale, disable the button and
		/// set its text to 'built-in'. If it's a custom locale, enable the button,
		/// and if the text used to be 'built-in' (that is, the previous target locale
		/// was built-in), change it to 'None'.
		/// </summary>
		/// <param name="localeName">Name of the locale.</param>
		/// <param name="rbroot">The rbroot.</param>
		/// ------------------------------------------------------------------------------------
		public void SetupForSimilarLocale(string localeName, ILgIcuResourceBundle rbroot)
		{
			CheckDisposed();

			if (IsCustomLocale(localeName, rbroot))
			{
				// Since it's a custom locale the user is allowed to control it.
				// If it used to say "built in" change it to "none", otherwise,
				// they are changing from one custom locale to another, leave the
				// current selection alone.
				if (Text == m_res.GetString("kstidBuiltIn"))
					Text = m_res.GetString("kstid_None");
				Enabled = true;
			}
			else
			{
				Text = m_res.GetString("kstidBuiltIn");
				Enabled = false;
			}
		}

		/// <summary>
		/// The locale to use for getting display names. If this is left blank the
		/// system defaut locale is used.
		/// </summary>
		public string DisplayLocaleId
		{
			get
			{
				CheckDisposed();
				return m_displayLocaleId;
			}
			set
			{
				CheckDisposed();
				m_displayLocaleId = value;
			}
		}

		/// <summary>Event that occurs when the user chooses a locale.</summary>
		public event EventHandler LocaleSelected;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the LocaleSelected event; by default just calls delegates.
		/// </summary>
		/// <param name="ea">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnLocaleSelected(EventArgs ea)
		{
			if (LocaleSelected != null)
				LocaleSelected(this, ea);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LocaleMenuButton"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LocaleMenuButton()
		{
			CheckDisposed();

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			this.Image = SIL.FieldWorks.Resources.ResourceHelper.ButtonMenuArrowIcon;
			this.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the paint event.
		/// </summary>
		/// <param name="pe">The <see cref="T:System.Windows.Forms.PaintEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs pe)
		{
			// TODO: Add custom paint code here

			// Calling the base class OnPaint
			base.OnPaint(pe);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the click event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClick(EventArgs e)
		{
			ContextMenu menu = new ContextMenu();

			ILgIcuLocaleEnumerator locEnum = LgIcuLocaleEnumeratorClass.Create();

			// Create the various collections we use in the process of assembling
			// the menu.
			m_locales = new Dictionary<string, List<LocaleMenuItemData>>();
			m_mainItems = new List<LocaleMenuItemData>();
			m_itemData = new Dictionary<MenuItem, LocaleMenuItemData>();

			int cloc = locEnum.Count;
			for(int iloc = 0; iloc < cloc; iloc++)
			{
				string langid = locEnum.get_Language(iloc);
				string localeid = locEnum.get_Name(iloc);
				string displayName = locEnum.get_DisplayName(iloc, m_displayLocaleId);
				List<LocaleMenuItemData> mainItems = null;
				if (!m_locales.TryGetValue(langid, out mainItems))
				{
					mainItems = new List<LocaleMenuItemData>();
					m_locales[langid] = mainItems;
				}
				// Todo: second arg should be display name.
				LocaleMenuItemData data = new LocaleMenuItemData(localeid, displayName);
				mainItems.Add(data);
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
			foreach (KeyValuePair<string, List<LocaleMenuItemData>> kvp in m_locales)
			{
				string langid = kvp.Key;
				List<LocaleMenuItemData> items = kvp.Value;
				LocaleMenuItemData lmdRootItem = null;
				// See if there is an item in the array list that matches the langid
				foreach (LocaleMenuItemData lmd in items)
				{
					if (lmd.m_id == langid)
					{
						lmdRootItem = lmd;
						break;
					}
				}
				if (lmdRootItem == null)
				{
					// case 3
					foreach(LocaleMenuItemData lmd in items)
					{
						m_mainItems.Add(lmd);
					}
				}
				else
				{
					if (items.Count == 1)
					{
						// case 1
						LocaleMenuItemData lmdMenu =
							new LocaleMenuItemData(lmdRootItem.m_id,lmdRootItem.m_displayName);
						m_mainItems.Add(lmdMenu);
					}
					else
					{
						// case 2
						LocaleMenuItemData lmdMenu =
							new LocaleMenuItemData(lmdRootItem.m_id,lmdRootItem.m_displayName);
						lmdMenu.m_subitems = items;
						m_mainItems.Add(lmdMenu);
					}
				}
			}

			// Sort the items in each menu.
			m_mainItems.Sort();
			LocaleMenuItemData NoneData = new LocaleMenuItemData(
				m_res.GetString("kstid_None"), m_res.GetString("kstid_None"));
			MenuItem NoneItem = new MenuItem(NoneData.m_displayName,
					new System.EventHandler(ItemClickHandler));
			menu.MenuItems.Add(NoneItem); // This goes strictly at the beginning, irrespective of sorting

			foreach(LocaleMenuItemData lmd in m_mainItems)
			{
				MenuItem mi = new MenuItem(lmd.m_displayName,
					new System.EventHandler(ItemClickHandler));
				menu.MenuItems.Add(mi);
				m_itemData[mi] = lmd;
				if (lmd.m_subitems != null)
				{
					mi.Popup += new EventHandler(mi_Popup);
					lmd.m_subitems.Sort();
					// To make the system realize this item is a submenu, we have to
					// add at least one item. To save time and space, we don't add the others
					// until it pops up.
					LocaleMenuItemData lmdSub = lmd.m_subitems[0];
					MenuItem miSub = new MenuItem(lmdSub.m_displayName,
						new System.EventHandler(ItemClickHandler));
					mi.MenuItems.Add(miSub);
					m_itemData[miSub] = lmdSub;

					// Turns out popup events don't happen in a .NET submenu, only for the top-level
					// menu. DavidO has a workaround that should soon be checked in. In the meantime,
					// generate the submenu at once. This may actually be fast enough to keep permanently.
					mi_Popup(mi, new EventArgs());
				}
			}

			menu.Show(this, new Point(0, this.Height));
			System.Runtime.InteropServices.Marshal.ReleaseComObject(locEnum);
			base.OnClick(e); // Review JohnT: is this useful or harmful or neither? MS recommends it.
		}

		private void ItemClickHandler(Object sender, System.EventArgs e)
		{
			MenuItem mi = sender as MenuItem;
			if (mi.Text == m_res.GetString("kstid_None"))
			{
				m_selectedLocale = null;
				this.Text = m_res.GetString("kstid_None");
				OnLocaleSelected(new EventArgs());
				return;
			}
			LocaleMenuItemData lmd = m_itemData[mi];
			m_selectedLocale = lmd.m_id;
			this.Text = lmd.m_displayName;
			OnLocaleSelected(new EventArgs());
		}

		private void mi_Popup(object sender, EventArgs e)
		{
			MenuItem miBase = sender as MenuItem;
			Debug.Assert(miBase != null);
			if (miBase.MenuItems.Count > 1)
				return; // already popped up, has items.
			LocaleMenuItemData lmd = m_itemData[miBase];
			LocaleMenuItemData lmdFirst = lmd.m_subitems[0];
			foreach (LocaleMenuItemData lmdSub in lmd.m_subitems)
			{
				// Skip the first item as it was added earlier.
				if (lmdSub == lmdFirst)
					continue;
				MenuItem miSub = new MenuItem(lmdSub.m_displayName,
					new EventHandler(ItemClickHandler));
				miBase.MenuItems.Add(miSub);
				m_itemData[miSub] = lmdSub;
			}
		}
	}

	internal class LocaleMenuItemData : IComparable<LocaleMenuItemData>
	{
		internal LocaleMenuItemData(string id, string displayName)
		{
			m_id = id;
			m_displayName = displayName;
		}
		// Locale id, as returned by Locale.getName. For submenus, this is the id
		// of the language, which is also the id of the base locale.
		internal string m_id;
		internal string m_displayName; // corresponding display name, from Locale.getDisplayName.

		internal List<LocaleMenuItemData> m_subitems;

		public int CompareTo(LocaleMenuItemData obj)
		{
			Debug.Assert(obj != null);
			return m_displayName.CompareTo(obj.m_displayName);
		}
	}
}
