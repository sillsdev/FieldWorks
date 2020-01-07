// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SilEncConverters40;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public partial class SfmToTextsAndWordsMappingDlg : Form
	{
		protected string m_helpTopicID;
		private LcmCache m_cache;
		private string m_orginalLabel;
		private readonly string m_blankEC = SfmToXml.SfmToXmlServices.AlreadyInUnicode;
		private Sfm2FlexTextMappingBase m_mapping; // the object we are editing.
		private IEnumerable<InterlinDestination> m_destinationsToDisplay; // applied filter for Destinations
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;

		public SfmToTextsAndWordsMappingDlg()
		{
			InitializeComponent();
			m_helpTopicID = "khtpField-InterlinearSfmImportWizard-Step2";
			m_orginalLabel = m_destinationLabel.Text;
		}

		private void SfmInterlinearMappingDlg_WritingSystemAdded(object sender, EventArgs e)
		{
			var ws = ((AddWritingSystemButton)m_addWritingSystemButton).NewWritingSystem;
			if (ws != null)
			{
				m_writingSystemCombo.InitializeWritingSystemCombo(m_cache, ws.Id);
			}
		}

		public void SetupDlg(IHelpTopicProvider helpTopicProvider, IApp app, LcmCache cache, Sfm2FlexTextMappingBase mappingToModify, IEnumerable<InterlinDestination> destinationsToDisplay)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_cache = cache;
			m_mapping = mappingToModify;
			m_destinationsToDisplay = destinationsToDisplay;
			SuspendLayout();
			// Update the label to show what marker we are modifying
			m_destinationLabel.Text = string.Format(m_orginalLabel, mappingToModify.Marker);
			// Replace the Add button with a specialized add writing system button
			var loc = m_addWritingSystemButton.Location;
			var tabIndex = m_addWritingSystemButton.TabIndex;
			var text = m_addWritingSystemButton.Text;
			Controls.Remove(m_addWritingSystemButton);
			m_addWritingSystemButton = new AddWritingSystemButton();
			m_addWritingSystemButton.Location = loc;
			m_addWritingSystemButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			Controls.Add(m_addWritingSystemButton);
			m_addWritingSystemButton.TabIndex = tabIndex;
			m_addWritingSystemButton.Text = text;
			var addWritingSystemButton = ((AddWritingSystemButton)m_addWritingSystemButton);
			addWritingSystemButton.Initialize(m_cache, helpTopicProvider, app, cache.ServiceLocator.WritingSystems.AllWritingSystems);
			addWritingSystemButton.WritingSystemAdded += SfmInterlinearMappingDlg_WritingSystemAdded;
			m_destinationsListBox.SelectedIndexChanged += new EventHandler(m_destinationsListBox_SelectedIndexChanged);
			LoadConverters(mappingToModify.Converter);
			LoadDestinations();
			ResumeLayout();
		}

		private void m_destinationsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			OnDestinationListBox_SelectedIndexChanged();
		}

		protected virtual void OnDestinationListBox_SelectedIndexChanged()
		{
			var oldWs = GetOldWs();
			if (m_destinationsListBox.SelectedItem is DestinationItem && ((DestinationItem)m_destinationsListBox.SelectedItem).Dest == InterlinDestination.Baseline)
			{
				// Baseline can only use vernacular writing systems.
				if (!m_writingSystemCombo.InitializeWritingSystemCombo(m_cache, oldWs, m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.ToArray()))
				{
					m_writingSystemCombo.SelectedIndex = 0; // if old one is not in list, pick one that is.
				}
			}
			else
			{
				m_writingSystemCombo.InitializeWritingSystemCombo(m_cache);
			}
		}

		protected string GetOldWs()
		{
			var oldWs = m_mapping.WritingSystem;
			if (m_writingSystemCombo.SelectedItem is CoreWritingSystemDefinition)
			{
				oldWs = ((CoreWritingSystemDefinition)m_writingSystemCombo.SelectedItem).Id;
			}
			return oldWs;
		}

		protected virtual string GetDestinationName(InterlinDestination destEnum)
		{
			return GetDestinationNameFromResource(destEnum, LanguageExplorerControls.ResourceManager);
		}

		public static string GetDestinationNameFromResource(InterlinDestination dest, ResourceManager rm)
		{
			var stid = "ksFld" + dest;
			return rm.GetString(stid) ?? dest.ToString();
		}

		private void LoadDestinations()
		{
			m_destinationsListBox.BeginUpdate();
			m_destinationsListBox.Items.Clear();
			var items = m_destinationsToDisplay
				.Select(dest => new { dest, name = GetDestinationName(dest) })
				.Where(@t => @t.dest != InterlinDestination.Ignored)
				.Select(@t => new DestinationItem() { Name = @t.name, Dest = @t.dest }).ToList();
			// Sort most of the names, but force 'Ignored' to come first
			items.Sort((item1, item2) => item1.Name.CompareTo(item2.Name));
			items.Insert(0, new DestinationItem() { Name = GetDestinationName(InterlinDestination.Ignored), Dest = InterlinDestination.Ignored });
			foreach (var item in items)
			{
				m_destinationsListBox.Items.Add(item);
			}
			m_destinationsListBox.SelectedItem = items.First(item => item.Dest == m_mapping.Destination);
			m_destinationsListBox.EndUpdate();
		}

		private void LoadConverters(string converter)
		{
			LoadEncodingConverters();
			var index = 0;
			if (!string.IsNullOrEmpty(converter))
			{
				index = m_converterCombo.FindStringExact(converter);
				if (index < 0)
				{
					index = 0;
				}
			}
			m_converterCombo.SelectedIndex = index;
		}

		/// <summary>
		/// update the 'encoding converters' combo box with the current values
		/// </summary>
		private void LoadEncodingConverters()
		{
			var encConv = new EncConverters();
			var de = encConv.GetEnumerator();
			m_converterCombo.BeginUpdate();
			m_converterCombo.Items.Clear();
			m_converterCombo.Sorted = true;
			while (de.MoveNext())
			{
				var name = de.Key as string;
				if (name != null)
				{
					m_converterCombo.Items.Add(name);
				}
			}
			m_converterCombo.Sorted = false;
			m_converterCombo.Items.Insert(0, m_blankEC);
			m_converterCombo.EndUpdate();
		}

		private void m_addConverterButton_Click(object sender, EventArgs e)
		{
			try
			{
				var prevEC = m_converterCombo.Text;
				using (var dlg = new AddCnvtrDlg(m_helpTopicProvider, m_app, null, m_converterCombo.Text, null, false))
				{
					dlg.ShowDialog();
					// Reload the converter list in the combo to reflect the changes.
					LoadEncodingConverters();
					// Either select the new one or select the old one
					if (dlg.DialogResult == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedConverter))
					{
						m_converterCombo.SelectedItem = dlg.SelectedConverter;
					}
					else if (m_converterCombo.Items.Count > 0)
					{
						m_converterCombo.SelectedItem = prevEC; // preserve selection if possible
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format(LanguageExplorerControls.ksErrorAccessingEncodingConverters, ex.Message));
			}
		}

		private void m_okButton_Click(object sender, EventArgs e)
		{
			var dest = ((DestinationItem)m_destinationsListBox.SelectedItem).Dest;
			m_mapping.WritingSystem = dest == InterlinDestination.Ignored ? null : ((CoreWritingSystemDefinition)m_writingSystemCombo.SelectedItem).Id;
			m_mapping.Converter = m_converterCombo.SelectedIndex <= 0 ? string.Empty : m_converterCombo.Text;
			m_mapping.Destination = dest;
		}

		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopicID);
		}

		private sealed class DestinationItem
		{
			public string Name;
			public InterlinDestination Dest;
			public override string ToString()
			{
				return Name;
			}
		}
	}
}