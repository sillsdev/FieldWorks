// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UserInterfaceChooser.cs
// Responsibility: Steve McConnel
//
// <remarks>
// This implements a control suitable for placing on a tab of the Tools/Options
// dialog.  The control is a combobox that lists the languages (writing systems) into
// which the program has been (at least partially) localized.  Each language name is given in
// its own language and script.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;

using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class UserInterfaceChooser : ComboBox
	{
		private string m_sUserWs;
		private string m_sNewUserWs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UserInterfaceChooser"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UserInterfaceChooser()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initilizess the control, setting the specified user interface writing
		/// system as the current one.
		/// </summary>
		/// <param name="sUserWs">The s user ws.</param>
		/// <param name="additionalWsLocales">Optional additional writing system locales to add
		/// to the list of available writing systems.</param>
		/// ------------------------------------------------------------------------------------
		public void Init(string sUserWs, IEnumerable<string> additionalWsLocales)
		{
			m_sUserWs = sUserWs;
			m_sNewUserWs = sUserWs;
			PopulateLanguagesCombo(additionalWsLocales);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the new user writing system.
		/// </summary>
		/// <value>The new user ws.</value>
		/// ------------------------------------------------------------------------------------
		public string NewUserWs
		{
			get { return (m_sNewUserWs == "en-US" ? "en" : m_sNewUserWs); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate the User Interface Languages combobox with the available languages.
		/// </summary>
		/// <param name="additionalWsLocales">Optional additional writing system locales to add
		/// to the list of available writing systems.</param>
		/// ------------------------------------------------------------------------------------
		private void PopulateLanguagesCombo(IEnumerable<string> additionalWsLocales)
		{
			SuspendLayout();

			// First, find those languages having satellite resource DLLs.
			AddAvailableLangsFromSatelliteDlls();

			if (additionalWsLocales != null)
			{
				foreach (string locale in additionalWsLocales)
					AddLanguage(locale);
			}

			// If no English locale was added, then add generic English. Otherwise,
			// if another, non US version of English, was added (e.g. en_GB), then add
			// US English since that is the locale of the fallback resources. This
			// will prevent a region of English existing in the list at the same time
			// with generic English. In other words, generic English and US English
			// are considered to be identical, but they show up in the list
			// differently depending upon whether or not another region of English
			// is also in the list.
			bool genericEnglishAdded = (IndexInList("en", false) >= 0);
			bool englishRegionAdded = (IndexInList("en", true) >= 0);

			if (!genericEnglishAdded)
			{
				if (!englishRegionAdded)
					AddLanguage("en");
				else if (IndexInList("en-US", false) < 0)
					AddLanguage("en-US");
			}

			bool userWsIsEnglish = m_sUserWs.StartsWith("en");

			// Add the user's UI writing system if it's not an English one,
			// since English writing systems have already been added.
			if (!userWsIsEnglish)
				AddLanguage(m_sUserWs);

			int index = IndexInList(m_sUserWs, false);
			if (index >= 0)
				SelectedIndex = index;
			else if (userWsIsEnglish)
			{
				index = IndexInList("en-US", true);
				if (index >= 0)
					SelectedIndex = index;
			}

			ResumeLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the available languages from satellite resource DLLs found off the
		/// application's folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddAvailableLangsFromSatelliteDlls()
		{
			// Get the folder in which the program file is stored.
			string sDllLocation = Path.GetDirectoryName(Application.ExecutablePath);

			// Get all the sub-folder in the program file's folder.
			string[] rgsDirs = Directory.GetDirectories(sDllLocation);

			// Go through each sub-folder and if at least one file in a sub-folder ends
			// with ".resource.dll", we know the folder stores localized resources and the
			// name of the folder is the culture ID for which the resources apply. The
			// name of the folder is stripped from the path and used to add a language
			// to the list.
			foreach (string dir in rgsDirs.Where(dir => Directory.GetFiles(dir, "*.resources.dll").Length > 0))
				AddLanguage(Path.GetFileName(dir));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the language.
		/// </summary>
		/// <param name="sLocale">The locale.</param>
		/// ------------------------------------------------------------------------------------
		private void AddLanguage(string sLocale)
		{
			Debug.Assert(!String.IsNullOrEmpty(sLocale));

			if (IndexInList(sLocale, false) < 0)
			{
				var ldi = new LanguageDisplayItem(sLocale);
				if (ldi.Name.Length == 0)
					ldi.Name = ldi.Locale;	// TODO: find a better fallback.

				Items.Add(ldi);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified locale is in the chooser's list.
		/// </summary>
		/// <param name="locale">locale (i.e. culture) to look for.</param>
		/// <param name="allowMatchOnParentCulture">True to allow matches on a locale's parent
		/// culture (i.e. "en-US" = "en". Otherwise, don't match on just the parent
		/// culture.</param>
		/// ------------------------------------------------------------------------------------
		private int IndexInList(string locale, bool allowMatchOnParentCulture)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				var ldi = Items[i] as LanguageDisplayItem;
				if (ldi != null)
				{
					if (ldi.Locale == locale)
						return i;
					if (allowMatchOnParentCulture && ldi.Locale.StartsWith(locale))
						return i;
				}
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSelectedIndexChanged(EventArgs e)
		{
			base.OnSelectedIndexChanged(e);
			// Change the current dialog on the fly, but don't touch the main program.
			var ldi = (LanguageDisplayItem) SelectedItem;
			m_sNewUserWs = ldi.Locale;
		}

		#region LanguageDisplayItem class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class LanguageDisplayItem
		{
			private string m_sName;
			private readonly string m_sLocale;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="LanguageDisplayItem"/> class.
			/// </summary>
			/// <param name="sLocale">The s locale.</param>
			/// --------------------------------------------------------------------------------
			public LanguageDisplayItem(string sLocale)
			{
				m_sLocale = sLocale;
				m_sName = DisplayName(sLocale);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Displays the name.
			/// </summary>
			/// <param name="sLocale">The s locale.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			private static string DisplayName(string sLocale)
			{
				string sName;
				try
				{
					var ci = new CultureInfo(sLocale);
					sName = ci.NativeName;
				}
				catch
				{
					Icu.UErrorCode uerr;
					Icu.GetDisplayName(sLocale, sLocale, out sName, out uerr);
				}
				return sName;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
			/// </summary>
			/// <returns>
			/// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public override string ToString()
			{
				return m_sName;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
			/// </summary>
			/// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
			/// <returns>
			/// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public override bool Equals(object obj)
			{
				var ldi = obj as LanguageDisplayItem;
				if (ldi != null)
					return (ldi.Locale == Locale && ldi.Name == Name);

				return false;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
			/// </summary>
			/// <returns>
			/// A hash code for the current <see cref="T:System.Object"></see>.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the name.
			/// </summary>
			/// <value>The name.</value>
			/// --------------------------------------------------------------------------------
			internal string Name
			{
				get { return m_sName; }
				set { m_sName = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the locale.
			/// </summary>
			/// <value>The locale.</value>
			/// --------------------------------------------------------------------------------
			internal string Locale
			{
				get { return m_sLocale; }
			}
		}

		#endregion
	}
}
