// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Win32;
using SIL.AlloGenModel;
using SIL.AlloGenService;
using SIL.AllomorphGenerator;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.VarGenService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XCore;

namespace SIL.VariantGenerator
{
	public partial class VariantGenForm : AlloGenFormBase
	{
		Button btnVariantTypes;
		ListBox lBoxVariantTypes;
		Label lbVariantTypes;
		CheckBox cbShowMinorEntry;
		Button btnPublishEntryIn;
		ListBox lBoxPublishEntryIn;
		Label lbPublishEntryIn;
		VariantCreator variantCreator;

		protected new const string OperationsFilePrompt =
			"Variant Generator Operations File (*.vgf)|*.vgf|" + "All Files (*.*)|*.*";
		const string RegKey = "Software\\SIL\\VariantGenerator";

		public VariantGenForm(LcmCache cache, PropertyTable propTable, Mediator mediator)
		{
			Cache = cache;
			PropTable = propTable;
			Mediator = mediator;
			FLExCustomFieldsObtainer obtainer = new FLExCustomFieldsObtainer(cache);
			customFields = obtainer.CustomFields;
			VarGenInitForm();
		}

		public VariantGenForm()
		{
			VarGenInitForm();
		}

		protected void VarGenInitForm()
		{
			base.InitializeComponent();
			if (plActions != null)
			{
				RemoveEnvironmentsAndStemName();
				AdjustHeightOfReplaceOpsBox();
				InitializeVariantTypesControls();
				InitializeShowInMinorEntryControls();
				InitializePublishEntryInControls();
				SetBackColor();
				this.Text = "Variant Generator";
			}
			// Create an instance of a ListView column sorter and assign it
			// to the ListView control.
			lvwColumnSorter = new ListViewColumnSorter();
			lvPreview.ListViewItemSorter = lvwColumnSorter;
			lvwEditReplaceOpsColumnSorter = new ListViewColumnSorter();
			lvEditReplaceOps.ListViewItemSorter = lvwEditReplaceOpsColumnSorter;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(
				this.OnFormClosing
			);
			RememberFormState(RegKey);
			Provider = new XmlBackEndProvider();
			Migrator = new DatabaseMigrator();
			LoadMigrateGetOperations();
			FillOperationsListBox();
			FillApplyToComboBox();
			SetupFontAndStyleInfo();
			SetUpOperationsCheckedListBox();
			SetUpPreviewCheckedListBox();
			FillApplyOperationsListView();
			SetUpEditReplaceOpsListView();
			FillReplaceOpsListView();
			BuildReplaceContextMenu();
			BuildEditReplaceOpContextMenu();
			BuildOperationsCheckBoxContextMenu();
			BuildPreviewCheckBoxContextMenu();
			lBoxMorphTypes.ClearSelected();
			lBoxEnvironments.ClearSelected();
			RememberTabSelection();
			MarkAsChanged(false);
			variantCreator = new VariantCreator(Cache, WritingSystems);
		}

		protected void AdjustHeightOfReplaceOpsBox()
		{
			this.lBoxReplaceOps.Height -= 20;
		}

		protected void InitializeVariantTypesControls()
		{
			btnVariantTypes = new Button();
			lBoxVariantTypes = new ListBox();
			lbVariantTypes = new Label();
			plActions.Controls.Add(btnVariantTypes);
			plActions.Controls.Add(lBoxVariantTypes);
			plActions.Controls.Add(lbVariantTypes);
			//
			// lbVariantTypes
			//
			lbVariantTypes.AutoSize = true;
			lbVariantTypes.Location = new Point(83, 83); // was 119
			lbVariantTypes.Margin = new Padding(2, 0, 2, 0);
			lbVariantTypes.Name = "lblbVariantTypes";
			lbVariantTypes.Size = new Size(71, 13);
			lbVariantTypes.TabIndex = 3;
			lbVariantTypes.Text = "Variant Types";
			//
			// lBoxVariantTypes
			//
			lBoxVariantTypes.Enabled = false;
			lBoxVariantTypes.FormattingEnabled = true;
			lBoxVariantTypes.Location = new Point(177, 83);
			lBoxVariantTypes.Margin = new Padding(2, 2, 2, 2);
			lBoxVariantTypes.Name = "lBoxVariantTypes";
			lBoxVariantTypes.Size = new Size(207, 62);
			lBoxVariantTypes.TabIndex = 4;
			lBoxVariantTypes.BringToFront();
			//
			// btnVariantTypes
			//
			btnVariantTypes.Location = new Point(399, 83);
			btnVariantTypes.Margin = new Padding(2, 2, 2, 2);
			btnVariantTypes.Name = "btnVariantTypes";
			btnVariantTypes.Size = new Size(33, 20);
			btnVariantTypes.TabIndex = 5;
			btnVariantTypes.Text = "Ed&it";
			btnVariantTypes.UseVisualStyleBackColor = true;
			btnVariantTypes.BringToFront();
			btnVariantTypes.Click += new System.EventHandler(this.btnVariantTypes_Click);
		}

		private void InitializeShowInMinorEntryControls()
		{
			cbShowMinorEntry = new CheckBox();
			plActions.Controls.Add(cbShowMinorEntry);
			//
			// cbShowInMinorEntry
			//
			cbShowMinorEntry.AutoSize = true;
			cbShowMinorEntry.Location = new Point(81, 148);
			cbShowMinorEntry.Name = "cbShowInMinorEntry";
			cbShowMinorEntry.Margin = new Padding(2, 0, 2, 0);
			cbShowMinorEntry.Size = new System.Drawing.Size(60, 13);
			cbShowMinorEntry.TabIndex = 6;
			cbShowMinorEntry.Text = "Show minor entry";
			cbShowMinorEntry.Checked = true;
			cbShowMinorEntry.Click += new EventHandler(this.cbShowMinorEntry_Click);
		}

		private void InitializePublishEntryInControls()
		{
			btnPublishEntryIn = new Button();
			lBoxPublishEntryIn = new ListBox();
			lbPublishEntryIn = new Label();
			plActions.Controls.Add(btnPublishEntryIn);
			plActions.Controls.Add(lBoxPublishEntryIn);
			plActions.Controls.Add(lbPublishEntryIn);
			//
			// lbPublishEntryIn
			//
			lbPublishEntryIn.AutoSize = true;
			lbPublishEntryIn.Location = new Point(83, 190);
			lbPublishEntryIn.Margin = new Padding(2, 0, 2, 0);
			lbPublishEntryIn.Name = "lblbPublishEntryIn";
			lbPublishEntryIn.Size = new Size(71, 13);
			lbPublishEntryIn.TabIndex = 7;
			lbPublishEntryIn.Text = "Publish Entry In";
			//
			// lBoxPublishEntryIn
			//
			lBoxPublishEntryIn.Enabled = false;
			lBoxPublishEntryIn.FormattingEnabled = true;
			lBoxPublishEntryIn.Location = new Point(177, 190);
			lBoxPublishEntryIn.Margin = new Padding(2, 2, 2, 2);
			lBoxPublishEntryIn.Name = "lBoxPublishEntryIn";
			lBoxPublishEntryIn.Size = new Size(207, 62);
			lBoxPublishEntryIn.TabIndex = 8;
			lBoxPublishEntryIn.BringToFront();
			//
			// btnPublishEntryIn
			//
			btnPublishEntryIn.Location = new Point(399, 190);
			btnPublishEntryIn.Margin = new Padding(2, 2, 2, 2);
			btnPublishEntryIn.Name = "btnPublishEntryIn";
			btnPublishEntryIn.Size = new Size(33, 20);
			btnPublishEntryIn.TabIndex = 9;
			btnPublishEntryIn.Text = "Ed&it";
			btnPublishEntryIn.UseVisualStyleBackColor = true;
			btnPublishEntryIn.BringToFront();
			btnPublishEntryIn.Click += new System.EventHandler(this.btnPublishEntryIn_Click);
		}

		private void RemoveEnvironmentsAndStemName()
		{
			plActions.Controls.Remove(lbEnvironments);
			plActions.Controls.Remove(lBoxEnvironments);
			plActions.Controls.Remove(btnEnvironments);
			plActions.Controls.Remove(lbStemName);
			plActions.Controls.Remove(tbStemName);
			plActions.Controls.Remove(btnStemName);
		}

		private void SetBackColor()
		{
			Color tabBackColor = Color.Linen;
			tabEditOps.BackColor = tabBackColor;
			tabRunOps.BackColor = tabBackColor;
			tabEditReplaceOps.BackColor = tabBackColor;
			plPattern.BackColor = tabBackColor;
			plActions.BackColor = tabBackColor;
		}

		protected void btnVariantTypes_Click(object sender, EventArgs e)
		{
			if (Cache != null)
			{
				VariantTypesChooser chooser = new VariantTypesChooser(Cache);
				chooser.setSelected(ActionOp.VariantTypes);
				chooser.FillVariantTypesListBox();
				chooser.ShowDialog();
				if (chooser.DialogResult == DialogResult.OK)
				{
					ActionOp.VariantTypes.Clear();
					ActionOp.VariantTypes.AddRange(chooser.SelectedVariantTypes);
					RefreshVariantTypesListBox();
					MarkAsChanged(true);
				}
			}
		}

		protected void cbShowMinorEntry_Click(object sender, EventArgs e)
		{
			if (Operation != null)
			{
				Operation.Action.ShowMinorEntry = cbShowMinorEntry.Checked;
			}
		}

		protected void btnPublishEntryIn_Click(object sender, EventArgs e)
		{
			if (Cache != null)
			{
				PublishEntryInChooser chooser = new PublishEntryInChooser(Cache);
				chooser.setSelected(ActionOp.PublishEntryInItems);
				chooser.FillPublishEntryInItemsListBox();
				chooser.ShowDialog();
				if (chooser.DialogResult == DialogResult.OK)
				{
					ActionOp.PublishEntryInItems.Clear();
					ActionOp.PublishEntryInItems.AddRange(chooser.SelectedPublishEntryInItems);
					RefreshPublishEntryInListBox();
					MarkAsChanged(true);
				}
			}
		}

		protected void OnFormClosing(object sender, EventArgs e)
		{
			SaveAnyChanges();
			SaveRegistryInfo(RegKey);
		}

		protected void RefreshVariantTypesListBox()
		{
			lBoxVariantTypes.Items.Clear();
			foreach (AlloGenModel.VariantType item in ActionOp.VariantTypes)
			{
				lBoxVariantTypes.Items.Add(item);
			}
		}

		protected void RefreshPublishEntryInListBox()
		{
			lBoxPublishEntryIn.Items.Clear();
			foreach (AlloGenModel.PublishEntryInItem item in ActionOp.PublishEntryInItems)
			{
				lBoxPublishEntryIn.Items.Add(item);
			}
		}

		protected override bool CheckForInvalidActionComponents()
		{
			return CheckForInvalidVariantTypes() && CheckForInvalidPublishEntryItems();
		}

		protected override string CreateUndoRedoPrompt(Operation op)
		{
			return " Variant Generation for '" + op.Name;
		}

		protected override void ApplyOperationToEntry(
			Operation op,
			ILexEntry entry,
			List<string> forms
		)
		{
			ILexEntryRef variantEntryRef = variantCreator.CreateVariant(entry, forms);
			variantCreator.SetShowMinorEntry(variantEntryRef, op.Action.ShowMinorEntry);
			if (op.Action.VariantTypes.Count > 0)
			{
				variantCreator.AddVariantTypes(variantEntryRef, op.Action.VariantTypes);
			}
			if (op.Action.PublishEntryInItems.Count > 0)
			{
				variantCreator.AddPublishEntryInItems(op.Action.PublishEntryInItems);
			}
		}

		protected bool CheckForInvalidVariantTypes()
		{
			bool allIsGood = true;
			foreach (ListViewItem lvItem in lvOperations.CheckedItems)
			{
				Operation op = (Operation)lvItem.Tag;
				if (op.Action.VariantTypes.Count > 0)
				{
					foreach (AlloGenModel.VariantType varType in op.Action.VariantTypes)
					{
						var variantType =
							Cache.ServiceLocator.ObjectRepository.GetObjectOrIdWithHvoFromGuid(
								new Guid(varType.Guid)
							);
						if (variantType == null)
						{
							ReportMissingFLExItem("The variant type '", varType.Name, op.Name);
							allIsGood = false;
						}
					}
				}
			}
			return allIsGood;
		}

		protected bool CheckForInvalidPublishEntryItems()
		{
			bool allIsGood = true;
			foreach (ListViewItem lvItem in lvOperations.CheckedItems)
			{
				Operation op = (Operation)lvItem.Tag;
				if (op.Action.PublishEntryInItems.Count > 0)
				{
					foreach (
						AlloGenModel.PublishEntryInItem pubItem in op.Action.PublishEntryInItems
					)
					{
						var publication =
							Cache.ServiceLocator.ObjectRepository.GetObjectOrIdWithHvoFromGuid(
								new Guid(pubItem.Guid)
							);
						if (publication == null)
						{
							ReportMissingFLExItem("The publication '", pubItem.Name, op.Name);
							allIsGood = false;
						}
					}
				}
			}
			return allIsGood;
		}

		protected override void GetMatchingEntries(
			PatternMatcher patMatcher,
			out IList<ILexEntry> matchingEntries,
			out IList<ILexEntry> matchingEntriesWithItems
		)
		{
			matchingEntries = patMatcher
				.MatchPattern(patMatcher.NonVariantMainEntries, Operation.Pattern)
				.ToList();
			// following gets all main entries that already have a variant that matches the replace op
			matchingEntriesWithItems = patMatcher
				.MatchEntriesWithVariantsPerPattern(Operation, Pattern)
				.ToList();
			foreach (ILexEntry entry in matchingEntriesWithItems)
			{
				if (matchingEntries.Contains(entry))
				{
					// it is already there, so remove it
					matchingEntries.Remove(entry);
				}
			}
		}

		protected override void lBoxOperations_SelectedIndexChanged(object sender, EventArgs e)
		{
			base.lBoxOperations_SelectedIndexChanged(sender, e);
			if (Operation != null)
			{
				RefreshVariantTypesListBox();
				cbShowMinorEntry.Checked = Operation.Action.ShowMinorEntry;
				RefreshPublishEntryInListBox();
			}
		}

		protected override string GetOperationsFilePrompt()
		{
			return OperationsFilePrompt;
		}

		protected override Form BuildCreateNewOpenCancelDialog()
		{
			var dlg = new CreateNewOpenCancelDialog();
			dlg.Text = "Variant Generator";
			return dlg;
		}

		protected override string GetUserDocPath()
		{
			string helpsPath = Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps");
			//String basedir = GetAppBaseDir();
			return Path.Combine(helpsPath, "Language Explorer", "Utilities", "VarGenUserDocumentation.pdf");
		}

		protected override Uri GetBaseUri()
		{
			return new Uri(Assembly.GetExecutingAssembly().CodeBase);
		}
	}
}
