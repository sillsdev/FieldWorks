// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DockableUsfmBrowser.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ControlExtenders;
using Paratext;
using Paratext.ScriptureEditor;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using Microsoft.Win32;
using Paratext.TextCollection;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using ScrVers = Paratext.ScrVers;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class provides a dockable/floating control which displays the shared Paratext
	/// control for viewing USFM resources.
	/// TODO JohnT: explain exactly how to use it.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class DockableUsfmBrowser : UserControl
	{
		#region Data members
		/// <summary>
		/// Interface giving us access to the code that allows docking and floating.
		/// </summary>
		private IFloaty m_floaty;
		private DockExtender m_extender;
		private Rectangle m_lastFloatLocation;
		//private bool m_fShowing; // Whether the floaty has been shown or hidden
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DockableUsfmBrowser()
		{
			// need to force Paratext libraries to initialize before versification files
			// are loaded
			string dummy = ParatextHelper.ProjectsDirectory;
			InitializeComponent();
			m_textCollection.Setup(ScriptureViewSource.Instance);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Install the control.
		/// </summary>
		/// <param name="dockHost">The control that hosts the browser</param>
		/// <param name="app">The application</param>
		/// <returns>
		/// 	<c>true</c> if the browser was installed successfully; <c>false</c>
		/// otherwise.
		/// </returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		public bool Install(Control dockHost, IApp app)
		{
			while (true)
			{
				try
				{
					RegistrationInfo.AllowParatext6Code = true;
					RegistrationInfo.AllowAccessToResources();
					break;
				}
				catch (Exception e)
				{
					try
					{
						ReflectionHelper.SetField(typeof(ScrTextCollection), "initialized", false);
					}
					catch (Exception reflectionHelperException)
					{
						throw new ContinuableErrorException("Paratext resource browser failed to initialize." +
							Environment.NewLine + reflectionHelperException.Message, e);
					}
					if (MessageBox.Show(dockHost.FindForm(), String.Format(
						Properties.Resources.kstidCannotDisplayResourcePane,
						app.ApplicationName, e.Message), app.ApplicationName,
						MessageBoxButtons.RetryCancel, MessageBoxIcon.Error,
						MessageBoxDefaultButton.Button2) != DialogResult.Retry)
					{
						return false;
					}
				}
			}

			m_toolStrip.Text = "USFM Resource Browser";
			m_extender = new DockExtender(dockHost);
			dockHost.Controls.Add(this);
			m_floaty = m_extender.Attach(this, m_toolStrip, true);
			this.SendToBack();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save settings (currently the list of texts displayed)
		/// </summary>
		/// <param name="key">The key.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveSettings(RegistryKey key)
		{
			try
			{
				StringBuilder bldr = new StringBuilder();
				foreach (TextCollectionItem item in m_textCollection.Items)
					bldr.AppendFormat("{0},", item.ScrText.Name);

				key.SetValue("UsfmTexts", bldr.ToString().TrimEnd(','));
				string curItemName = string.Empty;

				if (m_textCollection.CurItem >= 0 && m_textCollection.CurItem < m_textCollection.Items.Count)
					curItemName = m_textCollection.Items[m_textCollection.CurItem].ScrText.Name;

				key.SetValue("UsfmCurItem", curItemName);
				key.SetValue("UsfmCurRef", m_textCollection.Reference.ToString());
				key.SetValue("UsfmCurSel", m_textCollection.CurItem);
				m_floaty.SaveSettings(key);
			}
			catch
			{
				// Ignore any problems
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restore settings (currently the list of texts displayed).
		/// </summary>
		/// <param name="key">The key.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadSettings(RegistryKey key)
		{
			try
			{
				m_floaty.LoadSettings(key);

				string texts = key.GetValue("UsfmTexts", "") as string;
				if (string.IsNullOrEmpty(texts))
					return; // foreach below does one iteration for empty string, which is bad.

				List<TextCollectionItem> items = new List<TextCollectionItem>();
				foreach (string name in texts.Split(','))
				{
					ScrText text = null;
					try
					{
						foreach (ScrText st in ScrTextCollection.ScrTexts(true, true))
						{
							if (st.Name == name)
							{
								text = st;
								break;
							}
						}
					}
					catch (Exception)
					{
						// If we for some reason can't make that text, carry on with the others.
						continue;
					}
					if (text == null)
						continue; // forget any saved item that is no longer an option.
					items.Add(new TextCollectionItem(text, 1.0));
				}
				m_textCollection.Items = items;
				string curItemName = key.GetValue("UsfmCurItem", "") as string;
				if (string.IsNullOrEmpty(curItemName))
				{
					for (int i = 0; i < m_textCollection.Items.Count; i++)
					{
						if (m_textCollection.Items[i].ScrText.Name == curItemName)
						{
							m_textCollection.CurItem = i;
							break;
						}
					}
				}

				// Paratext now requires a versification for Reload(),
				// so we'll default to English at this point.
				m_textCollection.Reference = new VerseRef("MAT 1:1", ScrVers.English);
				tryReloadTextCollection();

				VerseRef vr = new VerseRef("MAT 1:1"); // default
				string reference = key.GetValue("UsfmCurRef", null) as string;
				if (!string.IsNullOrEmpty(reference))
					vr = new VerseRef(reference);
				m_textCollection.CurItem = (int)key.GetValue("UsfmCurSel", -1);
				SetScriptureReference(vr);
			}
			catch
			{
				// ignore any problems.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When becoming visible, make sure at back of Z-order, as this seems to be necessary
		/// to make the old-style splitting work properly instead of overlaying this window on
		/// top of the one it is supposed to share with.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (this.Visible)
				this.SendToBack();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True if currently displayed. Differs from visible in that it is tied to hiding and showing
		/// the floaty, which may be different from our own visibility when floating.
		/// </summary>
		/// <value><c>true</c> if showing; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool Showing
		{
			get {
				if (m_floaty == null)
					return false;
				if (m_floaty.DockMode == DockStyle.None)
					return m_floaty.Visible;
				return this.Visible;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowFloaty()
		{
			//m_fShowing = true;
			m_floaty.Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hide it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void HideFloaty()
		{
			//m_fShowing = false;
			m_floaty.Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the location in scripture we are to show.
		/// </summary>
		/// <param name="reference">The reference.</param>
		/// ------------------------------------------------------------------------------------
		public void SetScriptureReference(VerseRef reference)
		{
			if (reference.ChapterNum == 0)
			{
				// When the chapter is zero, we are trying to select a reference in a title
				// (only place this should happen). Paratext doesn't handle this well (crashes).
				reference.ChapterNum = 1;
			}

			m_textCollection.Reference = reference;
			tryReloadTextCollection();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the floaty object available so the client can tweak things like where to dock.
		/// </summary>
		/// <value>The floaty.</value>
		/// ------------------------------------------------------------------------------------
		public IFloaty Floaty
		{
			get { return m_floaty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set scripture reference using TE's object.
		/// </summary>
		/// <param name="reference">The reference.</param>
		/// ------------------------------------------------------------------------------------
		public void SetTeScriptureReference(ScrReference reference)
		{
			VerseRef ref2 = new VerseRef(reference.Book, reference.Chapter, reference.Verse);
			ref2.Versification = new ScrVers(reference.Versification.ToString());
			SetScriptureReference(ref2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set how the control is docked. Only Top, Bottom, and None (floating) are
		/// currently supported.
		/// </summary>
		/// <param name="how">The how.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		public void DockFloaty(DockStyle how)
		{
			switch(how)
			{
				case DockStyle.Top:
				case DockStyle.Bottom:
					if (m_floaty.DockMode == DockStyle.None && m_floaty.Visible)
						m_lastFloatLocation = (m_floaty as Control).FindForm().Bounds;
					m_floaty.Dock(how);
					this.SendToBack();
					break;
				case DockStyle.None:
					m_floaty.Float();
					if (m_lastFloatLocation.Height > 10) // mainly making sure not 0,0,0,0
					{
						bool fVisible = false;
						foreach (Screen screen in Screen.AllScreens)
						{
							// We'll accept it as 'visible' if the screen contains the center top and the top is at least
							// 30 pixels above the bottom of the screen. The number is arbitrary, but should make at least
							// part of the title bar available to drag it by, without restricting people who want a very
							// small one at the bottom of the screen.
							if (screen.WorkingArea.Contains(new Point(m_lastFloatLocation.Left + m_lastFloatLocation.Width / 2,
								m_lastFloatLocation.Top))
								&& m_lastFloatLocation.Top + 30 < screen.WorkingArea.Bottom)
							{
								fVisible = true;
								break;
							}
						}
						if (fVisible)
							(m_floaty as Control).FindForm().Bounds = m_lastFloatLocation;
					}
					break;
				default:
					return; // ignored.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the bottomToolStripMenuItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void bottomToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DockFloaty(DockStyle.Bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the topToolStripMenuItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void topToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DockFloaty(DockStyle.Top);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the floatToolStripMenuItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void floatToolStripMenuItem_Click(object sender, EventArgs e)
		{
			DockFloaty(DockStyle.None);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the textsToolStripMenuItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void textsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				SelectTexts();
			}
			catch
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method adapted from TextCollectionControl method of same name, to use our dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ScrText gets added to ScrTextCollection")]
		private void SelectTexts()
		{
			// Create list of current items
			List<ScrText> currentItems = new List<ScrText>();
			foreach (TextCollectionItem item in m_textCollection.Items)
				currentItems.Add(item.ScrText);
			TextCollectionItem currentItem = null;
			if (m_textCollection.CurItem >= 0 && m_textCollection.CurItem < m_textCollection.Items.Count)
				currentItem = m_textCollection.Items[m_textCollection.CurItem];

			// Re-initialize, just in case.
			// We pass the directory (rather than passing no arguments, and letting the paratext dll figure
			// it out) because the figuring out goes wrong on Linux, where both programs are simulating
			// the registry.
			ScrTextCollection.Initialize(ParatextHelper.ProjectsDirectory, false);
			List<string> textNames = ScrTextCollection.ScrTextNames;

			foreach (string nameVar in textNames)
			{
				if (nameVar.Length < 1)
					continue;

				string name = nameVar.ToLower();

				// Ignore P6 source language texts.
				if (name == "lxx" || name == "grk" || name == "heb")
					continue;

				try
				{
					if (ScrTextCollection.Get(nameVar) == null)
					{
						// REVIEW: I'm not sure how/if ScrTextCollection gets disposed
						ScrText scrText = new ScrText(nameVar);
						ScrTextCollection.Add(scrText);
					}
				}
				catch (Exception)
				{
					//MessageBox.Show(name + ": " + exception.Message);
					// TODO What will happen if a text can't be loaded?
				}
			}

			// the two booleans indicate we want all the available texts, including resources (part of the Paratext distribution)
			// and non-scriptural materials (things like commentaries maybe?)
			using (ScrTextListSelectionForm selForm = new ScrTextListSelectionForm(
				ScrTextCollection.ScrTexts(true, true), currentItems))
			{
				if (selForm.ShowDialog(this) == DialogResult.OK)
				{
					// Create new list of items, keeping data from old one (to preserve zoom etc)
					List<TextCollectionItem> newItems = new List<TextCollectionItem>();
					foreach (ScrText scrText in selForm.Selections)
					{
						// Attempt to find in old list
						bool found = false;
						foreach (TextCollectionItem item in m_textCollection.Items)
						{
							if (item.ScrText == scrText)
							{
								newItems.Add(item);
								found = true;
								break;
							}
						}
						if (!found)
							newItems.Add(new TextCollectionItem(scrText, 1));
					}
					m_textCollection.Items = newItems;
					int curItemIndex = -1; // none selected
					for (int i = 0; i < newItems.Count; i++)
					{
						if (newItems[i] == currentItem)
						{
							curItemIndex = i;
							break;
						}
					}
					// select some current item if possible; out of range does not cause problem.
					// Currently it seems to cause a crash if the item is out of range for the OLD items;
					// I think this is a bug in the Paratext code.
					if (curItemIndex == -1 && m_textCollection.Items.Count > 0 && currentItems.Count > 0)
						curItemIndex = 0;
					m_textCollection.CurItem = curItemIndex;

					tryReloadTextCollection();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries to reload the text collection view, ignoring any errors that happen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void tryReloadTextCollection()
		{
			try
			{
				m_textCollection.Reload();
			}
			catch
			{
				// Ignore any problems refreshing the view
			}
		}
	}
}
