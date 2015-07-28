// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using Paratext;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Class copied and slightly modified (different localization mechanism) from ParaTextShared.
	/// This dialog is used by the DockableUsfmBrowser in place of the one built into the TextCollectionControl,
	/// which uses the ParaText localization strategy.
	/// </summary>
	public partial class ScrTextListSelectionForm : Form, IDisposable
	{
		List<ScrText> leftItems = new List<ScrText>();
		List<ScrText> rightItems = new List<ScrText>();

		/// <summary>
		/// Private constructor for localization support
		/// </summary>
		private ScrTextListSelectionForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="availableTexts"></param>
		/// <param name="initialSelections"></param>
		public ScrTextListSelectionForm(IEnumerable<ScrText> availableTexts,
			List<ScrText> initialSelections)
		{
			InitializeComponent();

			foreach (ScrText scrText in availableTexts)
			{
				if (!initialSelections.Contains(scrText))
					leftItems.Add(scrText);
			}
			foreach (ScrText scrText in initialSelections)
				rightItems.Add(scrText);

			UpdateLeftList();
			UpdateRightList();
			UpdateSavedSelectionLinks();
		}

		/// <summary>
		/// Return the texts currently selected.
		/// </summary>
		public List<ScrText> Selections
		{
			get
			{
				return new List<ScrText>(rightItems);
			}
		}

		private static int CompareScrTexts(ScrText a, ScrText b)
		{
			return a.Name.CompareTo(b.Name);
		}

		private void UpdateLeftList()
		{
			leftItems.Sort(CompareScrTexts);

			uiLeftList.BeginUpdate();
			uiLeftList.Items.Clear();
			foreach (ScrText item in leftItems)
				uiLeftList.Items.Add(item);
			uiLeftList.EndUpdate();
		}

		private void UpdateRightList()
		{
			// Save selected value
			ScrText scrText = uiRightList.SelectedItem as ScrText;

			uiRightList.BeginUpdate();
			uiRightList.Items.Clear();
			foreach (ScrText item in rightItems)
				uiRightList.Items.Add(item);
			uiRightList.EndUpdate();

			uiRightList.SelectedIndex = rightItems.IndexOf(scrText);
		}

		private void uiAdd_Click(object sender, EventArgs e)
		{
			// Make a copy to prevent concurrency issues with enumerator
			List<ScrText> toMove = new List<ScrText>();
			foreach (ScrText item in uiLeftList.SelectedItems)
				toMove.Add(item);

			foreach (ScrText item in toMove)
			{
				rightItems.Add(item);
				leftItems.Remove(item);
				uiLeftList.Items.Remove(item);
			}
			UpdateRightList();
		}

		private void uiRemove_Click(object sender, System.EventArgs e)
		{
			// Make a copy to prevent concurrency issues with enumerator
			List<ScrText> toMove = new List<ScrText>();
			foreach (ScrText item in uiRightList.SelectedItems)
				toMove.Add(item);

			foreach (ScrText item in toMove)
			{
				leftItems.Add(item);
				rightItems.Remove(item);
				uiRightList.Items.Remove(item);
			}
			UpdateLeftList();
		}

		private void uiLeftList_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			uiAdd.PerformClick();
		}

		private void uiRightList_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			uiRemove.PerformClick();
		}

		private void uiLeftList_MouseDown(object sender, MouseEventArgs e)
		{
			// Select clicked item on right click before popup is shown
			if (e.Button == MouseButtons.Right)
			{
				int clickedIndex = uiLeftList.IndexFromPoint(e.X, e.Y);
				if (clickedIndex != ListBox.NoMatches)
					uiLeftList.SelectedIndex = clickedIndex;

				uiLeftListMenu.Show(uiLeftList, e.X, e.Y);
			}
		}

		private void uiRightListRemove_Click(object sender, EventArgs e)
		{
			uiRemove.PerformClick();
		}

		private void uiLeftListAdd_Click(object sender, EventArgs e)
		{
			uiAdd.PerformClick();
		}

		private void uiRightList_MouseDown(object sender, MouseEventArgs e)
		{
			// Select clicked item on right click before popup is shown
			if (e.Button == MouseButtons.Right)
			{
				int clickedIndex = uiRightList.IndexFromPoint(e.X, e.Y);
				if (clickedIndex != ListBox.NoMatches)
					uiRightList.SelectedIndex = clickedIndex;

				uiRightListMenu.Show(uiRightList, e.X, e.Y);
			}
		}

		private void uiMoveUp_Click(object sender, EventArgs e)
		{
			if (uiRightList.SelectedIndex == -1)
				return;
			if (uiRightList.SelectedIndex == 0)
				return;
			rightItems.Insert(uiRightList.SelectedIndex - 1, rightItems[uiRightList.SelectedIndex]);
			rightItems.RemoveAt(uiRightList.SelectedIndex + 1);
			UpdateRightList();
		}

		private void uiMoveDown_Click(object sender, EventArgs e)
		{
			if (uiRightList.SelectedIndex == -1)
				return;
			if (uiRightList.SelectedIndex == rightItems.Count - 1)
				return;

			rightItems.Insert(uiRightList.SelectedIndex + 2, rightItems[uiRightList.SelectedIndex]);
			rightItems.RemoveAt(uiRightList.SelectedIndex);
			UpdateRightList();
		}

		void UpdateSavedSelectionLinks()
		{
			bool savedPresent = ((Settings.Default.SavedScrTextLists != null)
				&& (Settings.Default.SavedScrTextLists.Count > 0));
			uiLoadSelections.Enabled = savedPresent;
			uiDeleteSelections.Enabled = savedPresent;
		}

		private void uiLoadSelections_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// Populate dropdown menu
			uiSavedSelectionsMenu.Items.Clear();
			foreach (SavedScrTextList textList in Settings.Default.SavedScrTextLists)
			{
				ToolStripMenuItem item = new ToolStripMenuItem(textList.Name);
				item.Tag = textList;
				item.Click += new EventHandler(LoadSavedSelectionItem_Click);
				uiSavedSelectionsMenu.Items.Add(item);
			}
			uiSavedSelectionsMenu.Show(uiLoadSelections, 0, uiLoadSelections.Height);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="REVIEW: ScrTextCollection.Get returns a reference (?)")]
		private void LoadSavedSelectionItem_Click(object sender, EventArgs e)
		{
			SavedScrTextList savedList =
				(sender as ToolStripMenuItem).Tag as SavedScrTextList;

			rightItems = new List<ScrText>();
			foreach (string scrTextName in savedList.ScrTextNames)
			{
				ScrText scrText = ScrTextCollection.Get(scrTextName);
				if (scrText != null)
					rightItems.Add(scrText);
			}
			UpdateRightList();
			UpdateSavedSelectionLinks();
		}

		private void uiSaveSelections_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			string name = InputBoxForm.Show(this, Properties.Resources.kstidTlEnterNameSavedSel,
				Properties.Resources.kstidSaveSelections, "");

			if (name != null)
			{
				SavedScrTextList savedList = new SavedScrTextList();
				savedList.Name = name;
				foreach (ScrText scrText in rightItems)
					savedList.ScrTextNames.Add(scrText.Name);

				if (Settings.Default.SavedScrTextLists == null)
					Settings.Default.SavedScrTextLists = new SavedScrTextLists();

				Settings.Default.SavedScrTextLists.Add(savedList);
				Settings.Default.Save();
				UpdateSavedSelectionLinks();
			}
		}

		private void uiDeleteSelections_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// Populate dropdown menu
			uiSavedSelectionsMenu.Items.Clear();
			foreach (SavedScrTextList textList in Settings.Default.SavedScrTextLists)
			{
				ToolStripMenuItem item = new ToolStripMenuItem(textList.Name);
				item.Tag = textList;
				item.Click += new EventHandler(DeleteSavedSelectionItem_Click);
				uiSavedSelectionsMenu.Items.Add(item);
			}
			uiSavedSelectionsMenu.Show(uiDeleteSelections, 0, uiDeleteSelections.Height);
		}

		void DeleteSavedSelectionItem_Click(object sender, EventArgs e)
		{
			Settings.Default.SavedScrTextLists.Remove(
				(sender as ToolStripMenuItem).Tag as SavedScrTextList);
			Settings.Default.Save();
			UpdateSavedSelectionLinks();
		}
	}

	/// <summary>
	/// A nicer name for a list of saved scriptures.
	/// </summary>
	public class SavedScrTextLists : List<SavedScrTextList>
	{
	}

	/// <summary>
	/// A struct, really, to hold a named list of Scripture resource names.
	/// </summary>
	public class SavedScrTextList
	{
		/// <summary>
		/// The name of the list
		/// </summary>
		public string Name = "";
		/// <summary>
		/// The list
		/// </summary>
		public List<string> ScrTextNames = new List<string>();
	}


	/// <summary>
	/// This class was adapted from a (partly designer-made) version used in ParatextShared.
	/// This class allows you to handle specific events on the settings class:
	///  The SettingChanging event is raised before a setting's value is changed.
	///  The PropertyChanged event is raised after a setting's value is changed.
	///  The SettingsLoaded event is raised after the setting values are loaded.
	///  The SettingsSaving event is raised before the setting values are saved.
	/// </summary>
	internal sealed class Settings : System.Configuration.ApplicationSettingsBase
	{

		public Settings()
		{
			// // To add event handlers for saving and changing settings, uncomment the lines below:
			//
			// this.SettingChanging += this.SettingChangingEventHandler;
			//
			// this.SettingsSaving += this.SettingsSavingEventHandler;
			//
		}

		private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e)
		{
			// Add code to handle the SettingChangingEvent event here.
		}

		private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Add code to handle the SettingsSaving event here.
		}
		private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

		/// <summary>
		/// get the default instance.
		/// </summary>
		public static Settings Default
		{
			get
			{
				return defaultInstance;
			}
		}

		/// <summary>
		/// Get/Set the list we used to save USFM resource lists.
		/// </summary>
		[global::System.Configuration.UserScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		public SavedScrTextLists SavedScrTextLists
		{
			get
			{
				return ((SavedScrTextLists)(this["SavedScrTextLists"]));
			}
			set
			{
				this["SavedScrTextLists"] = value;
			}
		}
	}
}