// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Resources;
using SIL.Keyboarding;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.Windows.Forms.WritingSystems;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	public partial class FwWritingSystemSetupDlg : Form
	{
		private FwWritingSystemSetupModel _model;
		private IHelpTopicProvider _helpTopicProvider;
		private IApp _app;

		/// <summary/>
		public FwWritingSystemSetupDlg(FwWritingSystemSetupModel model = null, IHelpTopicProvider helpTopicProvider = null, IApp app = null) : base()
		{
			InitializeComponent();
			_helpTopicProvider = helpTopicProvider;
			_app = app;
			if (model != null)
			{
				BindToModel(model);
			}
		}

		#region Model binding methods
		private void BindToModel(FwWritingSystemSetupModel model)
		{
			SuspendLayout();
			model.ShowMessageBox = ShowMessageBox;
			model.AcceptSharedWsChangeWarning = ShowSharedWsChangeWarning;
			Text = model.Title;
			model.OnCurrentWritingSystemChanged -= OnCurrentWritingSystemChangedHandler;
			model.CurrentWsSetupModel.CurrentItemUpdated -= OnCurrentItemUpdated;
			BindGeneralTab(model);
			_sortControl.BindToModel(model.CurrentWsSetupModel);
			_keyboardControl.BindToModel(model.CurrentWsSetupModel);
			BindCurrentWSList(model);
			BindHeader(model);
			BindFontTab(model);
			BindCharactersTab(model);
			BindNumbersTab(model.CurrentWsSetupModel);
			BindConverterTab(model);
			_model = model;
			model.OnCurrentWritingSystemChanged += OnCurrentWritingSystemChangedHandler;
			model.CurrentWsSetupModel.CurrentItemUpdated += OnCurrentItemUpdated;
			ResumeLayout();
		}

		private void BindNumbersTab(WritingSystemSetupModel model)
		{
			numberSettingsCombo.SelectedIndexChanged -= NumberSettingsComboOnSelectedIndexChanged;
			var standardNumberingSystems = CLDRNumberingSystems.StandardNumberingSystems.ToArray();
			numberSettingsCombo.Items.Add(Strings.CustomNumberingSystem);
			var defaultDigits = CLDRNumberingSystems.GetDigitsForID(NumberingSystemDefinition.Default.Id);
			numberSettingsCombo.Items.Add(defaultDigits);
			foreach (var standardNumberingSystem in standardNumberingSystems)
			{
				if (standardNumberingSystem != defaultDigits)
				{
					numberSettingsCombo.Items.Add(standardNumberingSystem);
				}
			}
			if (model.CurrentNumberingSystemDefinition.IsCustom)
			{
				numberSettingsCombo.SelectedItem = Strings.CustomNumberingSystem;
			}
			else
			{
				numberSettingsCombo.SelectedItem = CLDRNumberingSystems.GetDigitsForID(model.CurrentNumberingSystemDefinition.Id);
			}
			customDigits.SetDigits(model.CurrentNumberingSystemDefinition.Digits, string.IsNullOrWhiteSpace(model.CurrentDefaultFontName) ? "Segoe" : model.CurrentDefaultFontName,
				model.CurrentDefaultFontSize == 0.0f ? 12 : model.CurrentDefaultFontSize);

			numberSettingsCombo.Enabled = numberSettingsCombo.Visible = true;
			customDigits.Enabled = model.CurrentNumberingSystemDefinition.IsCustom;
			numberSettingsCombo.SelectedIndexChanged += NumberSettingsComboOnSelectedIndexChanged;
		}

		private void BindHeader(FwWritingSystemSetupModel model)
		{
			_languageNameTextbox.TextChanged -= LanguageNameTextboxOnTextChanged;
			_toolTip.SetToolTip(_shareWithSldrCheckbox, FwCoreDlgs.WritingSystemSetup_SharingDataWithSldr);
			_shareWithSldrCheckbox.CheckedChanged -= ShareWithSldrCheckboxCheckChanged;
			_ethnologueLink.Text = model.EthnologueLabel;
			_languageCode.Text = model.LanguageCode;
			_languageNameTextbox.Text = model.LanguageName;
			_shareWithSldrCheckbox.Visible = model.ShowSharingWithSldr;
			_shareWithSldrCheckbox.Checked = model.IsSharingWithSldr;
			model.ShowChangeLanguage = ShowChangeLanguage;
			_shareWithSldrCheckbox.CheckedChanged += ShareWithSldrCheckboxCheckChanged;
			_languageNameTextbox.TextChanged += LanguageNameTextboxOnTextChanged;
		}

		private void BindGeneralTab(FwWritingSystemSetupModel model)
		{
			if (Keyboard.Controller != null && model.Cache != null)
			{
				IKeyboardDefinition userInterfaceKeyboard;
				if (Keyboard.Controller.TryGetKeyboard(model.Cache.DefaultUserWs,
					out userInterfaceKeyboard))
				{
					userInterfaceKeyboard.Activate();
				}
			}
			m_FullCode.Text = model.CurrentWsSetupModel.CurrentLanguageTag;
			_spellingCombo.Items.Clear();
			// ReSharper disable once CoVariantArrayConversion -- No writes occur in AddRange
			_spellingCombo.Items.AddRange(model.CurrentWsSetupModel.GetSpellCheckComboBoxItems().ToArray());
			_spellingCombo.SelectedItem = model.CurrentWsSetupModel.CurrentSpellChecker;
			_rightToLeftCheckbox.CheckedChanged -= RightToLeftCheckChanged;
			_rightToLeftCheckbox.Checked = model.CurrentWsSetupModel.CurrentRightToLeftScript;
			_rightToLeftCheckbox.CheckedChanged += RightToLeftCheckChanged;

			if (model.ShowAdvancedScriptRegionVariantView)
			{
				_generalTab.Controls.Remove(_identifiersControl);
				_generalTab.Controls.Add(_advancedIdentifiersControl);
				_identifiersControl.Visible = false;
				_advancedIdentifiersControl.Visible = true;
				_advancedIdentifiersControl.Enabled = true;
				_advancedIdentifiersControl.BindToModel(new AdvancedScriptRegionVariantModel(model));
				_advancedIdentifiersControl.Selected();
			}
			else
			{
				_generalTab.Controls.Remove(_advancedIdentifiersControl);
				_generalTab.Controls.Add(_identifiersControl);
				_identifiersControl.Visible = true;
				_advancedIdentifiersControl.Visible = false;
				_identifiersControl.UnwireBeforeClosing();
				_identifiersControl.BindToModel(model.CurrentWsSetupModel);
				_identifiersControl.Selected();
			}
			_enableAdvanced.CheckedChanged -= EnableAdvancedOnCheckedChanged;
			_enableAdvanced.Visible = model.ShowAdvancedScriptRegionVariantCheckBox;
			_enableAdvanced.Checked = model.ShowAdvancedScriptRegionVariantView;
			_enableAdvanced.CheckedChanged += EnableAdvancedOnCheckedChanged;
		}

		private void EnableAdvancedOnCheckedChanged(object sender, EventArgs e)
		{
			_model.ShowAdvancedScriptRegionVariantView = _enableAdvanced.Checked;
			BindGeneralTab(_model);
		}

		private void BindFontTab(FwWritingSystemSetupModel model)
		{
			_defaultFontControl.WritingSystem = model.WorkingList[model.CurrentWritingSystemIndex].WorkingWs;
		}

		private void BindConverterTab(FwWritingSystemSetupModel model)
		{
			m_lblEncodingConverter.Text = string.Format(FwCoreDlgs.WritingSystemSetup_EncodingConverterForImporting, model.WritingSystemName);
			model.ShowModifyEncodingConverters = ShowModifyEncodingConverter;

			BindEncodingConverterCombo(model);
		}

		private void BindEncodingConverterCombo(FwWritingSystemSetupModel model)
		{
			_encodingConverterCombo.SelectedIndexChanged -= EncodingConverterComboSelectedIndexChanged;
			var encConverters = model.GetEncodingConverters();
			_encodingConverterCombo.Items.Clear();
			foreach (string convName in encConverters)
			{
				_encodingConverterCombo.Items.Add(convName);
			}

			if (!string.IsNullOrEmpty(model.CurrentLegacyConverter))
			{
				_encodingConverterCombo.SelectedItem = model.CurrentLegacyConverter;
			}
			else
			{
				_encodingConverterCombo.SelectedItem = FwCoreDlgs.kstidNone;
			}
			_encodingConverterCombo.SelectedIndexChanged += EncodingConverterComboSelectedIndexChanged;
		}

		private void BindCharactersTab(FwWritingSystemSetupModel model)
		{
			m_lblValidCharacters.Text = string.Format(FwCoreDlgs.WritingSystemSetup_SpecifyValidChars, model.WritingSystemName);
			model.ShowValidCharsEditor = ShowValidCharsEditor;
		}

		private void BindCurrentWSList(FwWritingSystemSetupModel model)
		{
			model.ConfirmDeleteWritingSystem = ShowConfirmDeleteDialog;
			model.ImportListForNewWs = ImportTranslatedList;
			model.ConfirmMergeWritingSystem = ConfirmMergeWritingSystem;
			model.ShouldChangeHomographWs = ShouldChangeHomographWs;
			model.ConfirmClearAdvanced = ConfirmClearAdvancedData;
			_writingSystemList.ItemCheck -= WritingSystemListItemCheck;
			_writingSystemList.Items.Clear();
			var uniqueLabels = new HashSet<string>();
			foreach (var ws in model.WorkingList)
			{
				var label = ws.WorkingWs.DisplayLabel;
				if (uniqueLabels.Contains(label))
				{
					if (ws.OriginalWs != null && ws.OriginalWs.DisplayLabel == label)
					{
						label = string.Format(FwCoreDlgs.xOriginal, label);
					}
					else
					{
						do
						{
							label = string.Format(FwCoreDlgs.xCopy, label);
						} while (uniqueLabels.Contains(label));
					}

				}
				_writingSystemList.Items.Add(new WsListItem(label, ws.WorkingWs.LanguageTag), ws.InCurrentList);
				uniqueLabels.Add(label);
			}
			_writingSystemList.SelectedIndex = model.CurrentWritingSystemIndex;
			_writingSystemList.ItemCheck += WritingSystemListItemCheck;
			// Clear the problem highlight color
			_writingSystemList.BackColor = Color.Empty;
			_toolTip.SetToolTip(_writingSystemList, FwCoreDlgs.WritingSystemList_NormalTooltip);
			// Set move up and move down states
			moveUp.Enabled = model.CanMoveUp();
			moveDown.Enabled = model.CanMoveDown();
		}

		private bool ShouldChangeHomographWs(string newHomographWs)
		{
			var msg = ResourceHelper.GetResourceString("kstidChangeHomographNumberWs");
			var changeWs = MessageBox.Show(this, string.Format(msg, newHomographWs),
				ResourceHelper.GetResourceString("kstidChangeHomographNumberWsTitle"),
				MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			return changeWs == DialogResult.Yes;
		}

		#endregion

		#region Delegates (called by presentation model)
		private void ShowMessageBox(string msg)
		{
			MessageBox.Show(msg, Text, MessageBoxButtons.OK);
		}

		private bool ShowSharedWsChangeWarning(string originalLanguageName)
		{
			var caption = FwCoreDlgs.ksPossibleDataLoss;
			// REVIEW (Hasso) 2019.05: the LanguageName should not be used as the key here; the Code should be used.
			var msg = string.Format(FwCoreDlgs.ksWSChangeWarning, _model.WorkingList.Select(ws => ws.OriginalWs?.LanguageName == originalLanguageName),
				originalLanguageName, Environment.NewLine);
			return MessageBox.Show(msg, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.OK;

		}

		private void ShowValidCharsEditor()
		{
			var currentWs = _model.WorkingList[_model.CurrentWritingSystemIndex].WorkingWs;
			using (var dlg = new ValidCharactersDlg(_model.Cache, null, _helpTopicProvider,
				_app, currentWs, _model.CurrentWsSetupModel.CurrentDisplayLabel))
			{
				dlg.ShowDialog(this);
			}
		}

		private bool ShowChangeLanguage(out LanguageInfo info)
		{
			using (var langPicker = new LanguageLookupDialog())
			{
				var result = langPicker.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					info = langPicker.SelectedLanguage;
					return true;
				}
			}
			info = null;
			return false;
		}

		private bool ShowModifyEncodingConverter(string originalConverter, out string selectedConverter)
		{
			selectedConverter = null;
			using (var dlg = new AddCnvtrDlg(_helpTopicProvider, _app, null, originalConverter, null, false))
			{
				dlg.ShowDialog();

				// Either select the new one or select the old one
				if (dlg.DialogResult == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedConverter))
				{
					selectedConverter = dlg.SelectedConverter;
					return true;
				}
			}

			return false;
		}

		private bool ShowConfirmDeleteDialog(string wsDisplayLabel)
		{
			// If there is no cache then we are probably creating a new language project.
			// In any case, we don't need to warn about data being deleted if there is no project.
			if (_model.Cache == null)
			{
				return true;
			}
			using (var dlg = new DeleteWritingSystemWarningDialog())
			{
				dlg.SetWsName(wsDisplayLabel);
				return dlg.ShowDialog() == DialogResult.Yes;
			}
		}

		private bool ConfirmMergeWritingSystem(string wsToMerge, out CoreWritingSystemDefinition mergeTarget)
		{
			mergeTarget = null;
			if (DialogResult.No == MessageBox.Show(FwCoreDlgs.ksWSWarnWhenMergingWritingSystems,
				FwCoreDlgs.ksWarning, MessageBoxButtons.YesNo))
			{
				return false;
			}
			using (var dlg = new MergeWritingSystemDlg(_model.Cache, wsToMerge, _model.MergeTargets, _helpTopicProvider))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					mergeTarget = dlg.SelectedWritingSystem;
					return true;
				}
			}

			return false;
		}

		private bool ConfirmClearAdvancedData()
		{
			if (DialogResult.No == MessageBox.Show("Clearing the Advanced check box will remove all your advanced choices. Do you want to continue?",
				"Clear Advanced", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2))
			{
				return false;
			}

			return true;
		}

		private void ImportTranslatedList(string iculocaletoimport)
		{
			ProgressDialogWithTask.ImportTranslatedListsForWs(this, _model.Cache, iculocaletoimport);
		}
		#endregion

		#region Event handlers
		private void NumberSettingsComboOnSelectedIndexChanged(object sender, EventArgs e)
		{
			var selectedNumberingSystem = (string)((ComboBox)sender).SelectedItem;
			if (!selectedNumberingSystem.Equals(Strings.CustomNumberingSystem))
			{
				_model.CurrentWsSetupModel.CurrentNumberingSystemDefinition = new NumberingSystemDefinition(CLDRNumberingSystems.FindNumberingSystemID(selectedNumberingSystem));
				customDigits.Enabled = false;
			}
			else
			{
				_model.CurrentWsSetupModel.CurrentNumberingSystemDefinition = NumberingSystemDefinition.CreateCustomSystem(customDigits.GetDigits());
				customDigits.Enabled = true;
			}

			BindNumbersTab(_model.CurrentWsSetupModel);
		}

		private void OnCurrentWritingSystemChangedHandler(object sender, EventArgs args)
		{
			BindToModel(_model);
		}

		private void WritingSystemListSelectedIndexChanged(object sender, EventArgs e)
		{
			if (_model != null)
			{
				_model.SelectWs(_writingSystemList.SelectedIndex);
			}
		}

		private void EthnologueLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (Process.Start(_model.EthnologueLink))
			{
			}
		}

		private void ChangeCodeLinkClick(object sender, EventArgs e)
		{
			_model.ChangeLanguage();
			BindToModel(_model);
		}

		private void OkButtonClick(object sender, EventArgs e)
		{
			if (_model.IsListValid && customDigits.AreAllDigitsValid())
			{
				_model.Save();
				Close();
			}
			else
			{
				if (!_model.IsAtLeastOneSelected)
				{
					_toolTip.SetToolTip(_writingSystemList, FwCoreDlgs.WritingSystemList_SelectAtLeastOneWs);
					_wsListPanel.Refresh();
					MessageBox.Show(FwCoreDlgs.WritingSystemList_SelectAtLeastOneWs,
						FwCoreDlgs.FwWritingSystemSetupDlg_InvalidWsList, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				var dupWsName = _model.FirstDuplicateWs;
				if (!string.IsNullOrEmpty(dupWsName))
				{
					MessageBox.Show(string.Format(FwCoreDlgs.FwWritingSystemSetupDlg_RemoveOrDistinguishDuplicateWsX, dupWsName),
						FwCoreDlgs.FwWritingSystemSetupDlg_InvalidWsList, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				if (!customDigits.AreAllDigitsValid())
				{
					customDigits.HighlightProblemDigits();
					_tabControl.SelectedTab = _numbersTab;
				}
			}
		}

		private void AddWsButtonClick(object sender, EventArgs e)
		{
			var disposeThese = new List<ToolStripMenuItem>();
			foreach (ToolStripMenuItem item in _addMenuStrip.Items)
			{
				disposeThese.Add(item);
			}
			_addMenuStrip.Items.Clear();
			foreach (var toolStripMenuItem in disposeThese)
			{
				toolStripMenuItem.Dispose();
			}

			foreach (var item in _model.GetAddMenuItems())
			{
				_addMenuStrip.Items.Add(new ToolStripMenuItem(item.MenuText, null, item.ClickHandler));
				_addMenuStrip.Show(_addWsButton, new Point(0, _addWsButton.Height));
			}
		}

		private void OnValidCharsButtonClick(object sender, EventArgs e)
		{
			_model.EditValidCharacters();
		}

		private void EncodingConverterComboSelectedIndexChanged(object sender, EventArgs e)
		{
			// save the selected encoding converter
			var str = _encodingConverterCombo.SelectedItem as string;
			if (str == FwCoreDlgs.kstidNone)
				str = null;

			_model.CurrentLegacyConverter = str;
		}

		private void EncodingConverterButtonClick(object sender, EventArgs e)
		{
			_model.ModifyEncodingConverters();
			BindEncodingConverterCombo(_model);
		}

		private void MoveUpClick(object sender, EventArgs e)
		{
			_model.MoveUp();
			BindCurrentWSList(_model);
		}

		private void MoveDownClick(object sender, EventArgs e)
		{
			_model.MoveDown();
			BindCurrentWSList(_model);
		}

		private void WritingSystemListItemCheck(object sender, ItemCheckEventArgs e)
		{
			_model.ToggleInCurrentList();
			BindCurrentWSList(_model);
			_wsListPanel.Refresh();
		}

		private void WritingSystemListMouseDown(object sender, MouseEventArgs e)
		{
			// Show a menu when the right click is made over a writing system.
			var listBox = (CheckedListBox)sender;
			if (e.Button == MouseButtons.Right)
			{
				int index = listBox.IndexFromPoint(e.Location);
				if (index != ListBox.NoMatches)
				{
					if (index != _model.CurrentWritingSystemIndex)
					{
						// Select the item if it isn't currently selected before showing the menu.
						listBox.Select();
						listBox.SelectedIndex = index;
					}
					var disposeThese = new List<ToolStripMenuItem>();
					foreach (ToolStripMenuItem item in _addMenuStrip.Items)
					{
						disposeThese.Add(item);
					}
					_addMenuStrip.Items.Clear();
					foreach (var toolStripMenuItem in disposeThese)
					{
						toolStripMenuItem.Dispose();
					}

					foreach (var item in _model.GetRightClickMenuItems())
					{
						var menuItem = new ToolStripMenuItem(item.MenuText, null, item.ClickHandler);
						menuItem.Enabled = item.IsEnabled;
						_addMenuStrip.Items.Add(menuItem);
						_addMenuStrip.Show(listBox, e.Location);
					}
				}
			}
			else if (e.Button == MouseButtons.Left)
			{
				int index = listBox.IndexFromPoint(e.Location);
				if (index == -1)
				{

				}
			}
		}

		private void FormHelpClick(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(_helpTopicProvider, "UserHelpFile", "khtpProjectProperties_WritingSystem");
		}

		private void WritingListHelpClick(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(_helpTopicProvider, "UserHelpFile", "khtpProjectProperties_WritingSystem_List");
		}

		private void ShareWithSldrCheckboxCheckChanged(object sender, EventArgs e)
		{
			_model.IsSharingWithSldr = _shareWithSldrCheckbox.Checked;
		}

		private void LanguageNameTextboxOnTextChanged(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(_languageNameTextbox.Text.Trim()))
			{
				_model.LanguageName = _languageNameTextbox.Text;
				BindToModel(_model);
			}
		}

		private void OnCurrentItemUpdated(object sender, EventArgs e)
		{
			BindCurrentWSList(_model);
			// Binding the general tab is overkill, this method is called because it was changed
			// but we do want the code to refresh, so do that.
			m_FullCode.Text = _model.CurrentWsSetupModel.CurrentLanguageTag;
			_enableAdvanced.Visible = _model.ShowAdvancedScriptRegionVariantCheckBox;
		}
		#endregion

		#region List Item Model
		private sealed class WsListItem : Tuple<string, string>
		{
			public WsListItem(string display, string code) : base(display, code)
			{
			}

			public override string ToString()
			{
				return Item1;
			}

			public string Code => Item2;
		}
		#endregion


		/// <summary>
		/// Display a writing system dialog for the purpose of modifying a new project.
		/// </summary>
		public static bool ShowNewDialog(IWin32Window parentForm, WritingSystemManager wsManager, IWritingSystemContainer wsContainer, IHelpTopicProvider helpProvider, IApp app, FwWritingSystemSetupModel.ListType type, out IEnumerable<CoreWritingSystemDefinition> newWritingSystems)
		{
			newWritingSystems = new List<CoreWritingSystemDefinition>();
			var model = new FwWritingSystemSetupModel(wsContainer, type, wsManager);
			using (var dlg = new FwWritingSystemSetupDlg(model, helpProvider, app))
			{
				dlg.ShowDialog(parentForm);
				if (dlg.DialogResult == DialogResult.OK)
				{
					foreach (var item in model.WorkingList)
					{
						((List<CoreWritingSystemDefinition>)newWritingSystems).Add(item.WorkingWs);
					}

					return true;
				}
			}

			return false;
		}

		private void WsListPanelCellPaint(object sender, TableLayoutCellPaintEventArgs e)
		{
			if (e.Column == 0 && !_model.IsListValid)
			{
				e.Graphics.FillRectangle(new SolidBrush(Color.Red), e.CellBounds);
			}
		}

		private void RightToLeftCheckChanged(object sender, EventArgs e)
		{
			_model.CurrentWsSetupModel.CurrentRightToLeftScript = _rightToLeftCheckbox.Checked;
		}
	}
}
