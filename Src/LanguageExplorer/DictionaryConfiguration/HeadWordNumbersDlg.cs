// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Handles the display and manipulation of HeadWordNumbers
	/// </summary>
	public partial class HeadwordNumbersDlg : Form, IHeadwordNumbersView
	{
		private FwTextBox[] _digitBoxes;
		private IHelpTopicProvider m_helpTopicProvider;
		protected HelpProvider m_helpProvider;
		protected string m_helpTopic = string.Empty; // Default help topic ID

		public HeadwordNumbersDlg()
		{
			InitializeComponent();
			// Initially we want the combo box to show the user what the style is but not let them change it.
			// Allowing the user to pick the style will be added only at user request.
			_homographStyleCombo.Items.Add(HomographConfiguration.ksHomographNumberStyle);
			_homographStyleCombo.SelectedIndex = 0;
			_homographStyleCombo.Enabled = false;
			_senseStyleCombo.Items.Add(HomographConfiguration.ksSenseReferenceNumberStyle);
			_senseStyleCombo.SelectedIndex = 0;
			_senseStyleCombo.Enabled = false;
			_digitBoxes = new[]
			{
				m_digitZero, m_digitOne, m_digitTwo, m_digitThree, m_digitFour, m_digitFive,
				m_digitSix, m_digitSeven, m_digitEight, m_digitNine
			};
			Shown += (sender, args) => { UpdateWritingSystemCodeInDigits(); };
		}

		/// <summary>
		/// convert the sender to the styles combo box for processing by the controller
		/// </summary>
		private EventHandler HomographStyleButtonOnClick(EventHandler value)
		{
			return (sender, e) => value(_homographStyleCombo, e);
		}

		/// <summary>
		/// convert the sender to the styles combo box for processing by the controller
		/// </summary>
		private EventHandler SenseStyleButtonClick(EventHandler value)
		{
			return (sender, e) => value(_senseStyleCombo, e);
		}

		/// <summary>Fired when the Styles... button is clicked. Object sender is the Style ComboBox so it can be updated</summary>
		public event EventHandler RunStylesDialog
		{
			add
			{
				_homographStyleButton.Click += HomographStyleButtonOnClick(value);
				_senseNumberStyleBtn.Click += SenseStyleButtonClick(value);
			}
			remove
			{
				_homographStyleButton.Click -= HomographStyleButtonOnClick(value);
				_senseNumberStyleBtn.Click -= SenseStyleButtonClick(value);
			}
		}

		public DictionaryHomographConfiguration HomographConfig { get; private set; }

		public bool HomographBefore
		{
			get { return m_radioBefore.Checked; }
			set
			{
				m_radioBefore.Checked = value;
				m_radioAfter.Checked = !value;
				m_radioNone.Checked = false;
			}
		}

		public bool ShowHomographOnCrossRef
		{
			get { return m_chkShowHomographNumInDict.Checked; }
			set
			{
				m_chkShowHomographNumInDict.Checked = value;
				m_chkShowSenseNumber.Enabled = value;
			}
		}

		public bool ShowSenseNumber
		{
			get { return m_chkShowSenseNumber.Checked; }
			set { m_chkShowSenseNumber.Checked = value; }
		}

		public bool ShowHomograph
		{
			get { return !m_radioNone.Checked; }
			set
			{
				if (!value)
				{
					m_radioBefore.Checked = false;
					m_radioAfter.Checked = false;
					m_radioNone.Checked = true;
				}
				else if (!m_radioBefore.Checked && !m_radioAfter.Checked)
				{
					// If we turn the numbers on and it isn't set to before or after default to after
					m_radioAfter.Checked = true;
				}
			}
		}

		public string Description
		{
			get { return m_configurationDescription.Text; }
			set { m_configurationDescription.Text = value; }
		}

		public string CurrentHomographStyle
		{
			get { return _homographStyleCombo.Items[0].ToString(); }
			set { _homographStyleCombo.Items[0] = value; }
		}

		public void SetupDialog(IHelpTopicProvider helpTopicProvider)
		{
			SetHelpTopic("khtpConfigureHeadwordNumbers"); // Default help topic ID
			m_helpProvider = new HelpProvider();
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);
			m_helpTopicProvider = helpTopicProvider;
			if (m_helpTopicProvider != null)
			{
				m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				SetHelpButtonEnabled();
			}
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			m_helpTopic = helpTopic;
			if (m_helpTopicProvider != null)
			{
				SetHelpButtonEnabled();
			}
		}

		private void SetHelpButtonEnabled()
		{
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
			m_btnHelp.Enabled = !string.IsNullOrEmpty(m_helpTopic);
		}

		/// <summary>
		/// Display help for this dialog.
		/// </summary>
		protected void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		private void m_chkShowHomographNumInDict_CheckedChanged(object sender, EventArgs e)
		{
			ShowHomographOnCrossRef = m_chkShowHomographNumInDict.Checked;
		}

		public string HomographWritingSystem
		{
			get { return m_writingSystemCombo.Text; }
			set
			{
				m_writingSystemCombo.SelectedIndex = m_writingSystemCombo.FindString(value);
				if (m_writingSystemCombo.SelectedIndex < 0)
				{
					m_writingSystemCombo.SelectedIndex = 0;
				}
			}
		}

		/// <summary>
		/// Set the writing system code in each digit textbox
		/// </summary>
		private void UpdateWritingSystemCodeInDigits()
		{
			var wsHandle = ((CoreWritingSystemDefinition)m_writingSystemCombo.SelectedItem).Handle;
			foreach (var digit in _digitBoxes)
			{
				digit.WritingSystemCode = wsHandle;
				digit.SelectAll();
				digit.ApplyWS(wsHandle);
				digit.ApplyStyle("UiElement");
				digit.RemoveSelection();
			}
		}

		public IEnumerable<CoreWritingSystemDefinition> AvailableWritingSystems
		{
			set
			{
				m_writingSystemCombo.Items.Clear();
				m_writingSystemCombo.Items.AddRange((value.Select(item => item as object)).ToArray());
			}
		}

		public IEnumerable<string> CustomDigits
		{
			get { return _digitBoxes.Select(db => db.Text).Where(text => !string.IsNullOrEmpty(text)); }
			set
			{
				var digitsArray = value.ToArray();
				if (digitsArray.Length == 0)
				{
					return;
				}
				if (digitsArray.Length != 10)
				{
					return;
				}
				for (var i = 0; i < 10; ++i)
				{
					_digitBoxes[i].Text = digitsArray[i];
				}
			}
		}

		public event EventHandler CustomDigitsChanged
		{
			add
			{
				foreach (var textBox in _digitBoxes)
				{
					textBox.TextChanged += value;
				}
			}
			remove
			{
				foreach (var textBox in _digitBoxes)
				{
					textBox.TextChanged -= value;
				}
			}
		}

		public bool OkButtonEnabled { get { return m_btnOk.Enabled; } set { m_btnOk.Enabled = value; } }

		public LcmStyleSheet SetStyleSheet
		{
			set
			{
				for (var i = 0; i < 10; ++i)
				{
					_digitBoxes[i].StyleSheet = value;
				}
			}
		}

		public void SetWsFactoryForCustomDigits(ILgWritingSystemFactory factory)
		{
			for (var i = 0; i < 10; ++i)
			{
				_digitBoxes[i].WritingSystemFactory = factory;
			}
		}

		private void m_writingSystemCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateWritingSystemCodeInDigits();
		}
	}
}