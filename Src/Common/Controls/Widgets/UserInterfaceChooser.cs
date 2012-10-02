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
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using System.IO;

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
		/// Initializes a new instance of the <see cref="T:UserInterfaceChooser"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UserInterfaceChooser() : base()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initilizess the control, setting the specified user interface writing
		/// system as the current one.
		/// </summary>
		/// <param name="sUserWs">The s user ws.</param>
		/// ------------------------------------------------------------------------------------
		public void Init(string sUserWs)
		{
			m_sUserWs = sUserWs;
			m_sNewUserWs = sUserWs;
			PopulateLanguagesCombo();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the new user writing system.
		/// </summary>
		/// <value>The new user ws.</value>
		/// ------------------------------------------------------------------------------------
		public string NewUserWs
		{
			get { return (m_sNewUserWs == "en_US" ? "en" : m_sNewUserWs); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate the User Interface Languages combobox with the available languages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PopulateLanguagesCombo()
		{
			this.SuspendLayout();

			// First, find those languages having satellite resource DLLs.
			AddAvailableLangsFromSatelliteDlls();

			// If someone renames the flex EXE, then, of course, this won't work.
			if (Application.ExecutablePath.ToLower().EndsWith("flex.exe"))
				AddAvailableFLExUILanguages();

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
				else if (IndexInList("en_US", false) < 0)
					AddLanguage("en_US");
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
				index = IndexInList("en_US", true);
				if (index >= 0)
					SelectedIndex = index;
			}

			this.ResumeLayout();
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
			foreach (string dir in rgsDirs)
			{
				string[] rgsFiles = Directory.GetFiles(dir, "*.resources.dll");
				if (rgsFiles.Length > 0)
				{
					int i = dir.LastIndexOf('\\');
					Debug.Assert(i > 0);
					if (i > 0)
						AddLanguage(dir.Substring(i + 1));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the available FLEx UI languages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddAvailableFLExUILanguages()
		{
			// Now, find which languages have a localized strings-{locale}.xml file.
			// Allow only those languages which have valid .NET locales to be displayed.
			string sConfigDir =
				DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer\Configuration");

			if (!Directory.Exists(sConfigDir))
				return;

			string[] rgsFiles = Directory.GetFiles(sConfigDir, "strings-*.xml");
			foreach (string file in rgsFiles)
			{
				string locale = Path.GetFileNameWithoutExtension(file);
				int i = locale.LastIndexOf('-');
				if (i >= 0)
				{
					locale = locale.Substring(i + 1);
					if (MiscUtils.WsHasValidCulture(locale))
						AddLanguage(locale);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the language.
		/// </summary>
		/// <param name="sLocale">The locale.</param>
		/// ------------------------------------------------------------------------------------
		private void AddLanguage(string sLocale)
		{
			if (IndexInList(sLocale, false) < 0)
			{
				LanguageDisplayItem ldi = new LanguageDisplayItem(sLocale);
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
				LanguageDisplayItem ldi = Items[i] as LanguageDisplayItem;
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
			LanguageDisplayItem ldi = (LanguageDisplayItem)SelectedItem;
			if (ldi.Locale != m_sNewUserWs)
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
			private string m_sLocale;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="LanguageDisplayItem"/> class.
			/// </summary>
			/// <param name="sLocale">The s locale.</param>
			/// --------------------------------------------------------------------------------
			public LanguageDisplayItem(string sLocale)
			{
				m_sLocale = sLocale.Replace('-', '_');
				m_sName = DisplayName(sLocale);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Displays the name.
			/// </summary>
			/// <param name="sLocale">The s locale.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			internal static string DisplayName(string sLocale)
			{
				string sName;
				try
				{
					CultureInfo ci = new CultureInfo(sLocale.Replace('_', '-'));
					sName = ci.NativeName;
				}
				catch
				{
					Icu.UErrorCode uerr = Icu.UErrorCode.U_ZERO_ERROR;
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
				LanguageDisplayItem ldi = obj as LanguageDisplayItem;
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
