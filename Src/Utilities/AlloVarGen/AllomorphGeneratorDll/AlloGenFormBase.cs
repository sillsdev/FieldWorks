// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Win32;
using SIL.AlloGenModel;
using SIL.AlloGenService;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XCore;
using static SIL.AlloGenService.FLExCustomFieldsObtainer;

namespace SIL.AllomorphGenerator
{
	public abstract partial class AlloGenFormBase : Form
	{
		public LcmCache Cache { get; set; }
		public Mediator Mediator { get; set; }
		public PropertyTable PropTable { get; set; }
		protected AllomorphCreator alloCreator;

		protected RegistryKey regkey;
		protected const string m_strLastDatabase = "LastDatabase";
		protected const string m_strLastOperationsFile = "LastOperationsFile";
		protected const string m_strLastOperation = "Lastoperation";
		protected const string m_strLastApplyOperation = "LastApplyOperation";
		protected const string m_strLastEditReplaceOps = "LastEditReplaceOps";
		protected const string m_strLastTab = "LastTab";
		protected const string m_strLocationX = "LocationX";
		protected const string m_strLocationY = "LocationY";
		protected const string m_strSizeHeight = "SizeHeight";
		protected const string m_strSizeWidth = "SizeWidth";
		protected const string m_strWindowState = "WindowState";

		protected ContextMenuStrip helpContextMenu;
		protected const string UserDocumentation = "User Documentation";
		protected const string About = "About";

		protected const string OperationsFilePrompt =
			"Allomorph Generator Operations File (*.agf)|*.agf|" + "All Files (*.*)|*.*";

		public Rectangle RectNormal { get; set; }

		public string LastDatabase { get; set; }
		public string LastOperationsFile { get; set; }
		public int LastOperation { get; set; }
		public int LastApplyOperation { get; set; }
		public int LastEditReplaceOps { get; set; }
		public int LastTab { get; set; }
		public int RetrievedLastOperation { get; set; }
		public int RetrievedLastApplyOperation { get; set; }
		public int RetrievedLastEditReplaceOps { get; set; }

		protected XmlBackEndProvider Provider { get; set; }
		protected DatabaseMigrator Migrator { get; set; }
		protected String OperationsFile { get; set; }
		protected AllomorphGenerators AlloGens { get; set; }
		protected List<Operation> Operations { get; set; }
		protected Operation Operation { get; set; }
		protected Operation LastOperationShown { get; set; } = null;
		protected List<Replace> ReplaceOps { get; set; }
		protected List<string> ReplaceOpRefs { get; set; }
		protected List<WritingSystem> WritingSystems { get; set; } = new List<WritingSystem>();
		protected AlloGenModel.Action ActionOp { get; set; }
		protected StemName StemName { get; set; }
		protected Pattern Pattern { get; set; }
		protected Category Category { get; set; }
		protected bool ChangesMade { get; set; } = false;
		protected Font fontForDefaultCitationForm;
		protected FontInfo fontInfoForDefaultCitationForm;
		protected Color colorForDefaultCitationForm;
		protected Dictionary<Operation, List<ILexEntry>> dictNonChosen =
			new Dictionary<Operation, List<ILexEntry>>();
		protected Dictionary<Operation, bool> dictOperationActiveState =
			new Dictionary<Operation, bool>();

		protected ListBox currentListBox;
		protected ContextMenuStrip editContextMenu;
		protected ContextMenuStrip editReplaceOpsContextMenu;
		protected const string formTitle = "Allomorph Generator";
		protected const string cmAdd = "Add";
		protected const string cmEdit = "Edit";
		protected const string cmInsertBefore = "Insert new before";
		protected const string cmInsertExistingBefore = "Insert existing before";
		protected const string cmInsertAfter = "Insert new after";
		protected const string cmInsertExistingAfter = "Insert existing after";
		protected const string cmMoveUp = "Move up";
		protected const string cmMoveDown = "Move down";
		protected const string cmDelete = "Delete";
		protected const string cmDuplicate = "Duplicate";

		protected ContextMenuStrip operationsCheckBoxContextMenu;
		protected ContextMenuStrip previewCheckBoxContextMenu;
		protected const string cmSelectAll = "Select All";
		protected const string cmClearAll = "Clear All";
		protected const string cmToggle = "Toggle";
		protected ListViewColumnSorter lvwColumnSorter;
		protected ListViewColumnSorter lvwEditReplaceOpsColumnSorter;
		protected List<FDWrapper> customFields = new List<FDWrapper>();
		protected string applyToField = "";

		protected void FillApplyToComboBox()
		{
			ApplyTo cit = new ApplyTo("Citation Form", LexEntryTags.kflidCitationForm);
			ApplyTo lex = new ApplyTo("Lexeme Form", LexEntryTags.kflidLexemeForm);
			ApplyTo ety = new ApplyTo("Etymology Form", LexEntryTags.kflidEtymology);
			cbApplyTo.Items.Add(cit);
			cbApplyTo.Items.Add(lex);
			cbApplyTo.Items.Add(ety);
			foreach (FDWrapper fdw in customFields)
			{
				ApplyTo cf = new ApplyTo(fdw.Fd.Name, fdw.Fd.Id);
				cbApplyTo.Items.Add(cf);
			}
			if (AlloGens.ApplyTo > -1)
			{
				int index = AlloGens.ApplyTo;
				if (index >= cbApplyTo.Items.Count)
					index = 0;
				cbApplyTo.SelectedIndex = index;
			}
			else
			{
				cbApplyTo.SelectedIndex = 0;
			}
		}

		protected void SetUpOperationsCheckedListBox()
		{
			lvOperations.SmallImageList = ilPreview;
			lvOperations.Columns.Add("", "", 25, HorizontalAlignment.Left, 0);
			lvOperations.Columns.Add("Operations", -2, HorizontalAlignment.Left);
		}

		protected void SetUpPreviewCheckedListBox()
		{
			lvPreview.SmallImageList = ilPreview;
			lvPreview.Columns.Clear();
			lvPreview.Columns.Add("", "", 25, HorizontalAlignment.Left, 0);
			lvPreview.Columns.Add(applyToField, -2, HorizontalAlignment.Left);
			foreach (WritingSystem ws in WritingSystems)
			{
				lvPreview.Columns.Add(ws.Name + "      ", -2, HorizontalAlignment.Left);
			}
		}

		protected void SetUpEditReplaceOpsListView()
		{
			lvEditReplaceOps.Columns.Add("Name", -2, HorizontalAlignment.Left);
			lvEditReplaceOps.Columns.Add("From", -2, HorizontalAlignment.Left);
			lvEditReplaceOps.Columns.Add("To", -2, HorizontalAlignment.Left);
			lvEditReplaceOps.Columns.Add("Mode", -2, HorizontalAlignment.Left);
			foreach (WritingSystem ws in WritingSystems)
			{
				lvEditReplaceOps.Columns.Add(ws.Name, -2, HorizontalAlignment.Left);
			}
			lvEditReplaceOps.Columns.Add("Description", -2, HorizontalAlignment.Left);
		}

		protected void SetupFontAndStyleInfo()
		{
			if (Cache != null)
			{
				var styles = Cache.LangProject.StylesOC.ToDictionary(style => style.Name);
				IStStyle normal = Cache.LangProject.StylesOC.FirstOrDefault(
					style => style.Name == "Normal"
				);
				if (normal != null)
				{
					SIL.FieldWorks.FwCoreDlgControls.StyleInfo styleInfo =
						new SIL.FieldWorks.FwCoreDlgControls.StyleInfo(normal);
					IList<CoreWritingSystemDefinition> vernWses = Cache
						.LangProject
						.CurrentVernacularWritingSystems;
					WritingSystems.Clear();
					foreach (CoreWritingSystemDefinition def in vernWses)
					{
						float fontSize = Math.Max(def.DefaultFontSize, 10);
						WritingSystem ws = new WritingSystem();
						ws.Name = def.Abbreviation;
						ws.Handle = def.Handle;
						ws.Font = new Font(def.DefaultFontName, fontSize);
						ws.FontInfo = styleInfo.FontInfoForWs(def.Handle);
						if (ws.FontInfo.FontColor.ValueIsSet)
							ws.Color = ws.FontInfo.FontColor.Value;
						SetFontAndStyleInfoForDefaultCitationForm(ws);
						WritingSystems.Add(ws);
					}
				}
			}
		}

		protected void SetFontAndStyleInfoForDefaultCitationForm(WritingSystem ws)
		{
			if (ws.Handle == Cache.DefaultVernWs)
			{
				fontForDefaultCitationForm = ws.Font;
				fontInfoForDefaultCitationForm = ws.FontInfo;
				colorForDefaultCitationForm = ws.Color;
			}
		}

		protected void RememberTabSelection()
		{
			if (LastTab < 0 || LastTab > tabControl.TabCount)
				LastTab = 0;
			tabControl.SelectedIndex = LastTab;
		}

		protected void BuildReplaceContextMenu()
		{
			editContextMenu = new ContextMenuStrip();
			editContextMenu.Name = "ReplaceOps";
			ToolStripMenuItem editItem = new ToolStripMenuItem(cmEdit);
			editItem.Click += new EventHandler(EditContextMenuReplace_Click);
			editItem.Name = cmEdit;
			ToolStripMenuItem insertBefore = new ToolStripMenuItem(cmInsertBefore);
			insertBefore.Click += new EventHandler(InsertBeforeContextMenu_Click);
			insertBefore.Name = cmInsertBefore;
			ToolStripMenuItem insertExistingBefore = new ToolStripMenuItem(cmInsertExistingBefore);
			insertExistingBefore.Click += new EventHandler(InsertExistingBeforeContextMenu_Click);
			insertExistingBefore.Name = cmInsertExistingBefore;
			ToolStripMenuItem insertAfter = new ToolStripMenuItem(cmInsertAfter);
			insertAfter.Click += new EventHandler(InsertAfterContextMenu_Click);
			insertAfter.Name = cmInsertAfter;
			ToolStripMenuItem insertExistingAfter = new ToolStripMenuItem(cmInsertExistingAfter);
			insertExistingAfter.Click += new EventHandler(InsertExistingAfterContextMenu_Click);
			insertExistingAfter.Name = cmInsertExistingAfter;
			ToolStripMenuItem moveUp = new ToolStripMenuItem(cmMoveUp);
			moveUp.Click += new EventHandler(MoveUpContextMenu_Click);
			moveUp.Name = cmMoveUp;
			ToolStripMenuItem moveDown = new ToolStripMenuItem(cmMoveDown);
			moveDown.Click += new EventHandler(MoveDownContextMenu_Click);
			moveDown.Name = cmMoveDown;
			ToolStripMenuItem deleteItem = new ToolStripMenuItem(cmDelete);
			deleteItem.Click += new EventHandler(DeleteContextMenu_Click);
			deleteItem.Name = cmDelete;
			ToolStripMenuItem duplicateItem = new ToolStripMenuItem(cmDuplicate);
			duplicateItem.Click += new EventHandler(DuplicateContextMenu_Click);
			duplicateItem.Name = cmDuplicate;
			editContextMenu.Items.Add(editItem);
			editContextMenu.Items.Add("-");
			editContextMenu.Items.Add(duplicateItem);
			editContextMenu.Items.Add(insertBefore);
			editContextMenu.Items.Add(insertExistingBefore);
			editContextMenu.Items.Add(insertAfter);
			editContextMenu.Items.Add(insertExistingAfter);
			editContextMenu.Items.Add("-");
			editContextMenu.Items.Add(moveUp);
			editContextMenu.Items.Add(moveDown);
			editContextMenu.Items.Add("-");
			editContextMenu.Items.Add(deleteItem);
		}

		protected void BuildOperationsCheckBoxContextMenu()
		{
			operationsCheckBoxContextMenu = new ContextMenuStrip();
			ToolStripMenuItem selectAll = new ToolStripMenuItem(cmSelectAll);
			selectAll.Click += new EventHandler(OperationsSelectAll_Click);
			selectAll.Name = cmSelectAll;
			ToolStripMenuItem clearAll = new ToolStripMenuItem(cmClearAll);
			clearAll.Click += new EventHandler(OperationsClearAll_Click);
			clearAll.Name = cmClearAll;
			ToolStripMenuItem toggle = new ToolStripMenuItem(cmToggle);
			toggle.Click += new EventHandler(OperationsToggle_Click);
			toggle.Name = cmToggle;
			operationsCheckBoxContextMenu.Items.Add(selectAll);
			operationsCheckBoxContextMenu.Items.Add(clearAll);
			operationsCheckBoxContextMenu.Items.Add(toggle);
		}

		protected void OperationsClearAll_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem lvItem in lvOperations.Items)
			{
				lvItem.Checked = false;
			}
		}

		protected void OperationsSelectAll_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem lvItem in lvOperations.Items)
			{
				lvItem.Checked = true;
			}
		}

		protected void OperationsToggle_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem lvItem in lvOperations.Items)
			{
				lvItem.Checked = !lvItem.Checked;
			}
		}

		protected void BuildPreviewCheckBoxContextMenu()
		{
			previewCheckBoxContextMenu = new ContextMenuStrip();
			ToolStripMenuItem selectAll = new ToolStripMenuItem(cmSelectAll + " / Ctrl-L");
			selectAll.Click += new EventHandler(PreviewSelectAll_Click);
			selectAll.Name = cmSelectAll;
			ToolStripMenuItem clearAll = new ToolStripMenuItem(cmClearAll + " / Ctrl-R");
			clearAll.Click += new EventHandler(PreviewClearAll_Click);
			clearAll.Name = cmClearAll;
			ToolStripMenuItem toggle = new ToolStripMenuItem(cmToggle + " / Ctrl-T");
			toggle.Click += new EventHandler(PreviewToggle_Click);
			toggle.Name = cmToggle;
			previewCheckBoxContextMenu.Items.Add(selectAll);
			previewCheckBoxContextMenu.Items.Add(clearAll);
			previewCheckBoxContextMenu.Items.Add(toggle);
			this.KeyUp += new KeyEventHandler(AlloGenFormBase_KeyUp);
		}

		protected void PreviewClearAll_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem lvItem in lvPreview.Items)
			{
				lvItem.Checked = false;
			}
		}

		protected void PreviewSelectAll_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem lvItem in lvPreview.Items)
			{
				lvItem.Checked = true;
			}
		}

		protected void PreviewToggle_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem lvItem in lvPreview.Items)
			{
				lvItem.Checked = !lvItem.Checked;
			}
		}

		protected void AlloGenFormBase_KeyUp(object sender, KeyEventArgs e)
		{
			if (tabControl.SelectedTab == tabRunOps)
			{
				if (e.Control && e.KeyCode == Keys.R)
				{
					PreviewClearAll_Click(sender, e);
				}
				else if (e.Control && e.KeyCode == Keys.L)
				{
					PreviewSelectAll_Click(sender, e);
				}
				else if (e.Control && e.KeyCode == Keys.T)
				{
					PreviewToggle_Click(sender, e);
				}
			}
		}

		protected void lBoxReplaceOps_MouseUp(object sender, MouseEventArgs e)
		{
			HandleContextMenu(sender, e);
		}

		protected void lBoxOperations_MouseUp(object sender, MouseEventArgs e)
		{
			HandleContextMenu(sender, e);
		}

		protected void HandleContextMenu(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				ListBox lBoxSender = (ListBox)sender;
				currentListBox = lBoxSender;
				int indexAtMouse = lBoxSender.IndexFromPoint(e.X, e.Y);
				if (indexAtMouse > -1)
				{
					AdjustContextMenuContent(lBoxSender, indexAtMouse);
					lBoxSender.SelectedIndex = indexAtMouse;
					Point ptClickedAt = e.Location;
					ptClickedAt = lBoxSender.PointToScreen(ptClickedAt);
					editContextMenu.Show(ptClickedAt);
				}
			}
		}

		protected void lvEditReplaceOps_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				ListView lvSender = (ListView)sender;
				ListViewItem item = lvSender.GetItemAt(e.X, e.Y);
				if (item != null)
				{
					//AdjustContextMenuContent(lBoxSender, indexAtMouse);
					//lBoxSender.SelectedIndex = indexAtMouse;
					Point ptClickedAt = e.Location;
					ptClickedAt = lvSender.PointToScreen(ptClickedAt);
					editReplaceOpsContextMenu.Show(ptClickedAt);
				}
			}
		}

		protected void BuildEditReplaceOpContextMenu()
		{
			editReplaceOpsContextMenu = new ContextMenuStrip();
			editReplaceOpsContextMenu.Name = "EditReplaceOps";
			ToolStripMenuItem editItem = new ToolStripMenuItem(cmEdit);
			editItem.Click += new EventHandler(EditReplaceOpsContextMenuEdit_Click);
			editItem.Name = cmEdit;
			ToolStripMenuItem add = new ToolStripMenuItem(cmAdd);
			add.Click += new EventHandler(EditReplaceOpsContextMenuAdd_Click);
			add.Name = cmAdd;
			ToolStripMenuItem deleteItem = new ToolStripMenuItem(cmDelete);
			deleteItem.Click += new EventHandler(EditReplaceOpsContextMenuDelete_Click);
			deleteItem.Name = cmDelete;
			ToolStripMenuItem duplicateItem = new ToolStripMenuItem(cmDuplicate);
			duplicateItem.Click += new EventHandler(EditReplaceOpsContextMenuDuplicate_Click);
			duplicateItem.Name = cmDuplicate;
			editReplaceOpsContextMenu.Items.Add(editItem);
			editReplaceOpsContextMenu.Items.Add("-");
			editReplaceOpsContextMenu.Items.Add(duplicateItem);
			editReplaceOpsContextMenu.Items.Add(add);
			editReplaceOpsContextMenu.Items.Add("-");
			editReplaceOpsContextMenu.Items.Add(deleteItem);
		}

		protected void AdjustContextMenuContent(ListBox lBoxSender, int indexAtMouse)
		{
			int indexLast = lBoxSender.Items.Count - 1;
			if (lBoxSender.Name == "lBoxOperations")
			{
				// Do not show Edit and its separator
				editContextMenu.Items[0].Visible = false;
				editContextMenu.Items[1].Visible = false;
				// Do not show "Insert existing before" or "Insert existing after"
				editContextMenu.Items[4].Visible = false;
				editContextMenu.Items[6].Visible = false;
			}
			else
			{
				editContextMenu.Items[0].Visible = true;
				editContextMenu.Items[1].Visible = true;
				editContextMenu.Items[4].Visible = true;
				editContextMenu.Items[6].Visible = true;
			}
			if (indexAtMouse == 0)
				// move up does not work
				editContextMenu.Items[8].Enabled = false;
			else
				editContextMenu.Items[8].Enabled = true;
			if (indexAtMouse == 0 && indexLast == 0)
				// delete does not work
				editContextMenu.Items[11].Enabled = false;
			else
				editContextMenu.Items[11].Enabled = true;
			if (indexAtMouse == indexLast)
				// move down does not work
				editContextMenu.Items[9].Enabled = false;
			else
				editContextMenu.Items[9].Enabled = true;
		}

		protected void EditContextMenuReplace_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmEdit)
			{
				InvokeEditReplaceOpForm();
			}
		}

		protected void EditReplaceOpsContextMenuReplace_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmEdit)
			{
				InvokeEditReplaceOpFormMasterList();
			}
		}

		protected void EditReplaceOpsContextMenuEdit_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmEdit)
			{
				InvokeEditReplaceOpFormMasterList();
			}
		}

		protected void EditReplaceOpsContextMenuDelete_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmDelete)
			{
				btnDeleteReplaceOp_Click(sender, e);
			}
		}

		protected void EditReplaceOpsContextMenuAdd_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmAdd)
			{
				btnAddNewReplaceOp_Click(sender, e);
			}
		}

		protected void EditReplaceOpsContextMenuDuplicate_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmDuplicate)
			{
				ListViewItem item = lvEditReplaceOps.SelectedItems[0];
				Replace thisReplace = (Replace)item.Tag;
				Replace replace = thisReplace.Duplicate();
				AddNewReplaceOpToMasterList(replace);
				InvokeEditReplaceOpFormMasterList();
			}
		}

		protected void InvokeEditReplaceOpForm()
		{
			using (var dialog = new EditReplaceOpForm())
			{
				Replace replace = (Replace)lBoxReplaceOps.SelectedItem;
				dialog.Initialize(replace, WritingSystems, Cache);
				dialog.ShowDialog();
				if (dialog.DialogResult == DialogResult.OK)
				{
					int index = lBoxReplaceOps.SelectedIndex;
					replace = dialog.ReplaceOp;
					AlloGens.AddReplaceOp(replace);
					lBoxReplaceOps.Items[index] = replace;
					MarkAsChanged(true);
				}
			}
		}

		protected void lBoxReplaceOps_DoubleClick(object sender, EventArgs e)
		{
			InvokeEditReplaceOpForm();
		}

		protected bool CanDoReplaceOpsMasterListOption()
		{
			bool doable = true;
			if (lvEditReplaceOps.SelectedItems.Count == 0)
			{
				MessageBox.Show("Please select a replace operation first.");
				doable = false;
			}
			return doable;
		}

		protected void InsertBeforeContextMenu_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmInsertBefore)
			{
				DoContextMenuInsert(currentListBox.SelectedIndex);
			}
		}

		protected void InsertAfterContextMenu_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmInsertAfter)
			{
				DoContextMenuInsert(currentListBox.SelectedIndex + 1);
			}
		}

		protected void DoContextMenuInsert(int index)
		{
			if (currentListBox.Name == "lBoxReplaceOps")
			{
				Replace replace = CreateNewReplace();
				AlloGens.AddReplaceOp(replace);
				ReplaceOpRefs.Insert(index, replace.Guid);
				currentListBox.Items.Insert(index, replace);
				currentListBox.SelectedIndex = index;
				InvokeEditReplaceOpForm();
			}
			else
			{
				Operation op = AlloGens.CreateNewOperation();
				// remove the new op added by CreateNewOperation() and insert it at the right place
				Operations.Remove(op);
				Operations.Insert(index, op);
				currentListBox.Items.Insert(index, op);
			}
			currentListBox.SetSelected(index, true);
			MarkAsChanged(true);
		}

		protected void InsertExistingAfterContextMenu_Click(object sender, EventArgs e)
		{
			int selectedIndex = lBoxReplaceOps.SelectedIndex + 1;
			InsertExistingReplaceOps(selectedIndex);
		}

		protected void InsertExistingBeforeContextMenu_Click(object sender, EventArgs e)
		{
			int selectedIndex = lBoxReplaceOps.SelectedIndex;
			InsertExistingReplaceOps(selectedIndex);
		}

		protected void InsertExistingReplaceOps(int selectedIndex)
		{
			ReplaceOperationsChooser chooser = new ReplaceOperationsChooser(AlloGens);
			chooser.FillReplaceOpsListBox();
			chooser.ShowDialog();
			if (chooser.DialogResult == DialogResult.OK)
			{
				int i = selectedIndex;
				foreach (Replace replace in chooser.SelectedReplaceOps)
				{
					if (i < lBoxReplaceOps.Items.Count)
					{
						lBoxReplaceOps.Items.Insert(i, replace);
						ReplaceOpRefs.Insert(i, replace.Guid);
					}
					else
					{
						lBoxReplaceOps.Items.Add(replace);
						ReplaceOpRefs.Add(replace.Guid);
					}
					i++;
				}
				RefreshReplaceListBox();
				MarkAsChanged(true);
			}
		}

		protected void MoveUpContextMenu_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmMoveUp)
			{
				int index = currentListBox.SelectedIndex;
				DoContextMenuMove(index, index - 1);
			}
		}

		protected void MoveDownContextMenu_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmMoveDown)
			{
				int index = currentListBox.SelectedIndex;
				DoContextMenuMove(index, index + 1);
			}
		}

		protected void DoContextMenuMove(int index, int otherIndex)
		{
			Object selectedItem = currentListBox.SelectedItem;
			Object otherItem = currentListBox.Items[otherIndex];
			if (currentListBox.Name == "lBoxReplaceOps")
			{
				ReplaceOpRefs[index] = ((Replace)otherItem).Guid;
				ReplaceOpRefs[otherIndex] = ((Replace)selectedItem).Guid;
			}
			else
			{
				Operations[index] = (Operation)otherItem;
				Operations[otherIndex] = (Operation)selectedItem;
			}
			currentListBox.Items[index] = otherItem;
			currentListBox.Items[otherIndex] = selectedItem;
			currentListBox.SelectedIndex = otherIndex;
			MarkAsChanged(true);
		}

		protected void DeleteContextMenu_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmDelete)
			{
				int index = currentListBox.SelectedIndex;
				if (currentListBox.Name == "lBoxReplaceOps")
				{
					ReplaceOpRefs.RemoveAt(index);
					Replace replace = (Replace)currentListBox.Items[index];
					StringBuilder sb = BuildDeleteReplaceOpRefMessage(replace);
					CheckOnRemovingSelectedReplaceOpFromMasterList(sb.ToString(), replace);
				}
				else
				{
					Operation op = Operations.ElementAt(index);
					AlloGens.DeleteEmptyReplaceOperationsFromAnOperation(op);
					Operations.RemoveAt(index);
				}
				currentListBox.Items.RemoveAt(index);
				int newIndex =
					index < currentListBox.Items.Count ? index : currentListBox.Items.Count - 1;
				if (newIndex > -1)
					currentListBox.SelectedIndex = newIndex;
			}
			MarkAsChanged(true);
		}

		protected void CheckOnRemovingSelectedReplaceOpFromMasterList(
			string prompt,
			Replace replace
		)
		{
			DialogResult result = MessageBox.Show(
				prompt,
				"Delete Replace Op",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2
			);
			if (result == DialogResult.Yes)
			{
				AlloGens.DeleteReplaceOp(replace);
				MarkAsChanged(true);
				FillReplaceOpsListView();
			}
		}

		protected void DuplicateContextMenu_Click(object sender, EventArgs e)
		{
			ToolStripItem menuItem = (ToolStripItem)sender;
			if (menuItem.Name == cmDuplicate)
			{
				int index = currentListBox.SelectedIndex + 1;
				if (currentListBox.Name == "lBoxReplaceOps")
				{
					Replace thisReplace = lBoxReplaceOps.SelectedItem as Replace;
					Replace replace = thisReplace.Duplicate();
					AlloGens.AddReplaceOp(replace);
					ReplaceOpRefs.Insert(index, replace.Guid);
					currentListBox.Items.Insert(index, replace);
				}
				else
				{
					Operation op = Operation.Duplicate();
					Operations.Insert(index, op);
					currentListBox.Items.Insert(index, op);
				}
			}
			MarkAsChanged(true);
		}

		virtual protected void RememberFormState(string sRegKey)
		{
			regkey = Registry.CurrentUser.OpenSubKey(sRegKey);
			if (regkey != null)
			{
				Cursor.Current = Cursors.WaitCursor;
				Application.DoEvents();
				RetrieveRegistryInfo();
				regkey.Close();
				DesktopBounds = RectNormal;
				WindowState = WindowState;
				StartPosition = FormStartPosition.Manual;
				if (!String.IsNullOrEmpty(LastOperationsFile))
					tbFile.Text = LastOperationsFile;
				Cursor.Current = Cursors.Default;
			}
		}

		protected void RetrieveRegistryInfo()
		{
			// Window location
			int iX = (int)regkey.GetValue(m_strLocationX, 100);
			int iY = (int)regkey.GetValue(m_strLocationY, 100);
			int iWidth = (int)regkey.GetValue(m_strSizeWidth, 863); // 1228);
			int iHeight = (int)regkey.GetValue(m_strSizeHeight, 670); // 947);
			RectNormal = new Rectangle(iX, iY, iWidth, iHeight);
			// Set form properties
			WindowState = (FormWindowState)regkey.GetValue(m_strWindowState, 0);

			LastDatabase = (string)regkey.GetValue(m_strLastDatabase);
			OperationsFile = LastOperationsFile = (string)regkey.GetValue(m_strLastOperationsFile);
			RetrievedLastOperation = LastOperation = (int)regkey.GetValue(m_strLastOperation, 0);
			RetrievedLastApplyOperation = LastApplyOperation = (int)
				regkey.GetValue(m_strLastApplyOperation, 0);
			RetrievedLastEditReplaceOps = LastEditReplaceOps = (int)
				regkey.GetValue(m_strLastEditReplaceOps, 0);
			LastTab = (int)regkey.GetValue(m_strLastTab, 0);
		}

		public void SaveRegistryInfo(string sRegKey)
		{
			regkey = Registry.CurrentUser.OpenSubKey(sRegKey, true);
			if (regkey == null)
			{
				regkey = Registry.CurrentUser.CreateSubKey(sRegKey);
			}

			if (LastDatabase != null)
				regkey.SetValue(m_strLastDatabase, LastDatabase);
			if (LastOperationsFile != null)
				regkey.SetValue(m_strLastOperationsFile, LastOperationsFile);
			regkey.SetValue(m_strLastOperation, LastOperation);
			regkey.SetValue(m_strLastApplyOperation, LastApplyOperation);
			regkey.SetValue(m_strLastEditReplaceOps, LastEditReplaceOps);
			regkey.SetValue(m_strLastTab, LastTab);
			// Window position and location
			regkey.SetValue(m_strWindowState, (int)WindowState);
			regkey.SetValue(m_strLocationX, RectNormal.X);
			regkey.SetValue(m_strLocationY, RectNormal.Y);
			regkey.SetValue(m_strSizeWidth, RectNormal.Width);
			regkey.SetValue(m_strSizeHeight, RectNormal.Height);
			regkey.Close();
		}

		protected virtual string GetOperationsFilePrompt()
		{
			return OperationsFilePrompt;
		}

		protected void btnBrowse_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = GetOperationsFilePrompt();
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				OperationsFile = dlg.FileName;
				LastOperationsFile = OperationsFile;
				tbFile.Text = OperationsFile;
				LoadMigrateGetOperations();
				if (Operations.Count > 0)
					Operation = Operations[0];
				LastOperation = 0;
				LastApplyOperation = 0;
				FillOperationsListBox();
				if (lvOperations.Visible)
				{
					FillApplyOperationsListView();
				}
				if (lvEditReplaceOps.Visible)
				{
					FillReplaceOpsListView();
				}
			}
			else if (String.IsNullOrEmpty(OperationsFile))
			{
				// probably first time run and user chose to cancel opening a file; quit
				this.Dispose();
			}
		}

		protected virtual Form BuildCreateNewOpenCancelDialog()
		{
			return new CreateNewOpenCancelDialog();
		}

		protected void LoadMigrateGetOperations()
		{
			if (!File.Exists(OperationsFile))
			{
				if (String.IsNullOrEmpty(OperationsFile))
				{
					// probably first time it is run
					var dlg = BuildCreateNewOpenCancelDialog();
					var result = dlg.ShowDialog();
					if (result == DialogResult.OK)
					{
						// create new operations file
						ChangesMade = false;
						SetupFontAndStyleInfo();
						btnNewFile_Click(this, new EventArgs());
						if (!String.IsNullOrEmpty(OperationsFile))
						{
							// Need to save it since it exists
							DoSave();
						}
					}
					else if (result == DialogResult.Yes)
					{
						// Open existing operations file
						btnBrowse_Click(this, new EventArgs());
					}
					else
					{
						// Assume it was canceled, so quit
						this.Dispose();
					}
				}
				else
				{
					MessageBox.Show(
						"Operations file not found!",
						"Load error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error
					);
				}
				return;
			}
#if Marks
			Provider.LoadDataFromFile(OperationsFile);
			AlloGens = Provider.AlloGens;
			if (AlloGens != null)
			{
				AlloGens = Migrator.Migrate(AlloGens, OperationsFile);
				Operations = AlloGens.Operations;
				WritingSystems = AlloGens.WritingSystems;
			}
#else
			string newFile = Migrator.Migrate(OperationsFile);
			Provider.LoadDataFromFile(newFile);
			AlloGens = Provider.AlloGens;
			if (AlloGens != null)
			{
				Operations = AlloGens.Operations;
				WritingSystems = AlloGens.WritingSystems;
			}
#endif
		}

		protected void SaveAnyChanges()
		{
			if (ChangesMade)
			{
				DialogResult res = MessageBox.Show(
					"Changes have been made.  Do you want to save them?",
					"Changes made",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question
				);
				if (res == DialogResult.Yes)
				{
					DoSave();
				}
			}
		}

		protected void DoSave()
		{
			Provider.AlloGens = AlloGens;
			Provider.SaveDataToFile(OperationsFile);
		}

		protected override void OnMove(EventArgs ea)
		{
			base.OnMove(ea);

			if (WindowState == FormWindowState.Normal)
				RectNormal = DesktopBounds;
		}

		protected override void OnResize(EventArgs ea)
		{
			base.OnResize(ea);

			if (WindowState == FormWindowState.Normal)
				RectNormal = DesktopBounds;
		}

		public void FillOperationsListBox()
		{
			lBoxOperations.Items.Clear();
			foreach (Operation op in Operations)
			{
				lBoxOperations.Items.Add(op);
			}
			if (Operations.Count > 0)
			{
				// select last used operation, if any
				if (LastOperation < 0 || LastOperation >= Operations.Count)
					LastOperation = 0;
				lBoxOperations.SetSelected(LastOperation, true);
				Operation = AlloGens.Operations[LastOperation];
				Pattern = Operation.Pattern;
			}
		}

		public void FillApplyOperationsListView()
		{
			lvOperations.Items.Clear();
			foreach (Operation op in Operations)
			{
				ListViewItem lvItem = new ListViewItem("");
				lvItem.UseItemStyleForSubItems = false;
				lvItem.Tag = op;
				lvItem.SubItems.Add(op.Name);
				if (dictOperationActiveState.ContainsKey(op))
				{
					bool value = false;
					dictOperationActiveState.TryGetValue(op, out value);
					lvItem.Checked = value;
				}
				else
				{
					lvItem.Checked = op.Active;
				}
				lvOperations.Items.Add(lvItem);
			}
			if (Operations.Count > 0)
			{
				// select last used operation, if any
				if (LastApplyOperation < 0 || LastApplyOperation >= Operations.Count)
					LastApplyOperation = 0;
				lvOperations.Items[LastApplyOperation].Selected = true;
				lvOperations.Select();
				Operation = AlloGens.Operations[LastApplyOperation];
				Pattern = Operation.Pattern;
			}
		}

		public void FillReplaceOpsListView()
		{
			lvEditReplaceOps.Items.Clear();
			foreach (Replace replace in AlloGens.ReplaceOperations)
			{
				ListViewItem lvItem = new ListViewItem(replace.Name);
				lvItem.UseItemStyleForSubItems = false;
				lvItem.Tag = replace;
				lvItem.SubItems[0].ForeColor = Color.Blue;
				lvItem.SubItems.Add(replace.From);
				lvItem.SubItems[1].ForeColor = Color.DarkGreen;
				lvItem.SubItems.Add(replace.To);
				lvItem.SubItems[2].ForeColor = Color.Navy;
				string sMode = replace.Mode ? " RegEx " : " Normal ";
				lvItem.SubItems.Add(sMode);
				foreach (WritingSystem ws in WritingSystems)
				{
					string sDialect = "";
					if (replace.WritingSystemRefs.Contains(ws.Name))
					{
						sDialect = " X ";
					}
					lvItem.SubItems.Add(sDialect);
				}
				lvItem.SubItems.Add(replace.Description);
				int descriptionIndex = lvItem.SubItems.Count - 1;
				lvItem.SubItems[descriptionIndex].ForeColor = Color.Purple;
				lvEditReplaceOps.Items.Add(lvItem);
			}
			if (AlloGens.ReplaceOperations.Count > 0)
			{
				// select last used operation, if any
				if (
					LastEditReplaceOps < 0 || LastEditReplaceOps >= AlloGens.ReplaceOperations.Count
				)
					LastEditReplaceOps = 0;
				lvEditReplaceOps.Items[LastEditReplaceOps].Selected = true;
				lvEditReplaceOps.Select();
			}
		}

		protected string GetDialectIndicator(bool dialect)
		{
			string indicator = dialect ? " X " : "";
			return indicator;
		}

		protected virtual void lBoxOperations_SelectedIndexChanged(object sender, EventArgs e)
		{
			Operation = lBoxOperations.SelectedItem as Operation;
			if (Operation != null)
			{
				LastOperation = lBoxOperations.SelectedIndex;
				tbName.Text = Operation.Name;
				tbDescription.Text = Operation.Description;
				Pattern = Operation.Pattern;
				tbMatch.Text = Pattern.Matcher.Pattern;
				RefreshMorphTypesListBox();
				if (Pattern.MorphTypes.Count > 0)
				{
					var selectedMorphType = Pattern.MorphTypes[0];
				}
				Category = Pattern.Category;
				tbCategory.Text = Category.Name;
				ActionOp = Operation.Action;
				RefreshEnvironmentsListBox();
				ReplaceOpRefs = ActionOp.ReplaceOpRefs;
				RefreshReplaceListBox();
				if (ActionOp.ReplaceOpRefs.Count == 0)
				{
					// need at least one replace action
					Replace replace = CreateNewReplace();
					AlloGens.AddReplaceOp(replace);
					lBoxReplaceOps.Items.Add(replace);
				}
				StemName = ActionOp.StemName;
				tbStemName.Text = StemName.Name;
				int index = lBoxOperations.SelectedIndex + 1;
				lbCountOps.Text = index.ToString() + " / " + AlloGens.Operations.Count.ToString();
			}
		}

		protected void RefreshReplaceListBox()
		{
			lBoxReplaceOps.Items.Clear();
			foreach (string guid in ReplaceOpRefs)
			{
				Replace replace = AlloGens.FindReplaceOp(guid);
				if (replace != null)
				{
					lBoxReplaceOps.Items.Add(replace);
				}
			}
			if (ReplaceOpRefs.Count > 0)
				lBoxReplaceOps.SetSelected(0, true);
		}

		protected void RefreshMorphTypesListBox()
		{
			lBoxMorphTypes.Items.Clear();
			foreach (MorphType item in Pattern.MorphTypes)
			{
				lBoxMorphTypes.Items.Add(item);
			}
		}

		protected void RefreshEnvironmentsListBox()
		{
			lBoxEnvironments.Items.Clear();
			foreach (AlloGenModel.Environment item in ActionOp.Environments)
			{
				lBoxEnvironments.Items.Add(item);
			}
		}

		protected void btnCategory_Click(object sender, EventArgs e)
		{
			if (Cache != null)
			{
				var allPoses =
					Cache.LanguageProject.PartsOfSpeechOA.ReallyReallyAllPossibilities.OrderBy(
						pos => pos.Name.BestAnalysisAlternative.Text
					);

				CategoryChooser chooser = new CategoryChooser();
				foreach (ICmPossibility pos in allPoses)
				{
					Category cat = new Category();
					cat.Name = pos.Name.BestAnalysisAlternative.Text;
					cat.Guid = pos.Guid.ToString();
					chooser.Categories.Add(cat);
				}
				chooser.FillCategoriesListBox();
				Category = Pattern.Category;
				if (Category.Name != null)
				{
					var catFound = chooser.Categories.FirstOrDefault(
						cat => cat.Name == Category.Name
					);
					int index = chooser.Categories.IndexOf(catFound);
					if (index > -1)
						chooser.SelectCategory(index);
					else
						chooser.SelectCategory(chooser.Categories.Count);
				}
				chooser.ShowDialog();
				if (chooser.DialogResult == DialogResult.OK)
				{
					Category cat = chooser.SelectedCategory;
					if (cat == chooser.NoneChosen)
					{
						Category.Name = "";
						Category.Guid = "";
						StemName = ActionOp.StemName;
						ClearStemNameValues();
						// if there's no category, there's no stem name
						tbStemName.Text = StemName.Name;
					}
					else
					{
						if (Category.Guid != cat.Guid)
						{
							ClearStemNameValues();
							tbStemName.Text = "";
						}
						Category.Name = cat.Name;
						Category.Guid = new Guid(cat.Guid).ToString();
					}
					tbCategory.Text = Category.Name;
					MarkAsChanged(true);
				}
			}
		}

		protected void ClearStemNameValues()
		{
			StemName.Name = "";
			StemName.Guid = "";
		}

		protected void btnStemName_Click(object sender, EventArgs e)
		{
			IPartOfSpeech pos = GetPartOfSpeechToUse(Pattern.Category.Guid);
			if (pos == null)
			{
				MessageBox.Show(
					"The category '"
						+ Pattern.Category.Name
						+ "' was not found in the FLEx database"
				);
				return;
			}
			StemNameChooser chooser = new StemNameChooser();
			foreach (
				IMoStemName msn in pos.AllStemNames.OrderBy(
					sn => sn.Name.BestAnalysisAlternative.Text
				)
			)
			{
				StemName stemName = new StemName();
				stemName.Name = msn.Name.BestAnalysisAlternative.Text;
				stemName.Guid = msn.Guid.ToString();
				chooser.StemNames.Add(stemName);
			}
			chooser.FillStemNamesListBox();
			StemName = ActionOp.StemName;
			if (StemName.Name != null)
			{
				var snFound = chooser.StemNames.FirstOrDefault(sn => sn.Name == StemName.Name);
				int index = chooser.StemNames.IndexOf(snFound);
				if (index > -1)
					chooser.SelectStemName(index);
				else
					chooser.SelectStemName(chooser.StemNames.Count);
			}
			chooser.ShowDialog();
			if (chooser.DialogResult == DialogResult.OK)
			{
				StemName sn = chooser.SelectedStemName;
				if (sn == chooser.NoneChosen)
				{
					ClearStemNameValues();
				}
				else
				{
					StemName.Name = sn.Name;
					StemName.Guid = new Guid(sn.Guid).ToString();
				}
				tbStemName.Text = StemName.Name;
				MarkAsChanged(true);
			}
		}

		protected IPartOfSpeech GetPartOfSpeechToUse(string poaGuid)
		{
			IPartOfSpeech pos = (IPartOfSpeech)
				Cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities.FirstOrDefault(
					p => p.Guid.ToString() == poaGuid
				);
			return pos;
		}

		protected void btnEnvironments_Click(object sender, EventArgs e)
		{
			if (Cache != null)
			{
				EnvironmentsChooser chooser = new EnvironmentsChooser(Cache);
				chooser.setSelected(ActionOp.Environments);
				chooser.FillEnvironmentsListBox();
				chooser.ShowDialog();
				if (chooser.DialogResult == DialogResult.OK)
				{
					ActionOp.Environments.Clear();
					ActionOp.Environments.AddRange(chooser.SelectedEnvironments);
					RefreshEnvironmentsListBox();
					MarkAsChanged(true);
				}
			}
		}

		protected void btnMorphTypes_Click(object sender, EventArgs e)
		{
			if (Cache != null)
			{
				MorphTypesChooser chooser = new MorphTypesChooser(Cache);
				chooser.setSelected(Pattern.MorphTypes);
				chooser.FillMorphTypesListBox();
				chooser.ShowDialog();
				if (chooser.DialogResult == DialogResult.OK)
				{
					Pattern.MorphTypes.Clear();
					Pattern.MorphTypes.AddRange(chooser.SelectedMorphTypes);
					RefreshMorphTypesListBox();
					MarkAsChanged(true);
				}
			}
		}

		protected void tbName_TextChanged(object sender, EventArgs e)
		{
			TextBox tb = sender as TextBox;
			if (tb != null)
			{
				Operation.Name = tb.Text;
				MarkAsChanged(true);
				int selectedOp = lBoxOperations.SelectedIndex;
				if (selectedOp > -1)
				{
					lBoxOperations.Items.Insert(selectedOp, Operation);
					lBoxOperations.Items.RemoveAt(selectedOp + 1);
					lBoxOperations.SelectedIndex = selectedOp;
				}
			}
		}

		protected void btnSaveChanges_Click(object sender, EventArgs e)
		{
			this.Cursor = Cursors.WaitCursor;
			SetOperationActiveStatus();
			Provider.AlloGens = AlloGens;
			Provider.SaveDataToFile(OperationsFile);
			MarkAsChanged(false);
			this.Cursor = Cursors.Arrow;
		}

		protected void btnSaveChanges2_Click(object sender, EventArgs e)
		{
			btnSaveChanges_Click(sender, e);
		}

		protected void btnSaveChanges3_Click(object sender, EventArgs e)
		{
			btnSaveChanges_Click(sender, e);
		}

		protected void SetOperationActiveStatus()
		{
			for (int i = 0; i < dictOperationActiveState.Count; i++)
			{
				KeyValuePair<Operation, bool> keyValuePair = dictOperationActiveState.ElementAt(i);
				int index = Operations.IndexOf(keyValuePair.Key);
				if (index > -1)
				{
					Operations[index].Active = keyValuePair.Value;
				}
			}
			AlloGens.Operations = Operations;
		}

		protected void tbDescription_TextChanged(object sender, EventArgs e)
		{
			TextBox tb = sender as TextBox;
			if (tb != null)
			{
				Operation.Description = tb.Text;
				MarkAsChanged(true);
			}
		}

		protected void MarkAsChanged(bool value)
		{
			ChangesMade = value;
			ShowChangeStatusOnForm();
		}

		protected void ShowChangeStatusOnForm()
		{
			if (AlloGenForm.ActiveForm != null)
			{
				AlloGenForm.ActiveForm.Text = formTitle;
				if (ChangesMade)
					AlloGenForm.ActiveForm.Text += "*";
			}
		}

		protected void btnNewFile_Click(object sender, EventArgs e)
		{
			SaveAnyChanges();
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.Filter = GetOperationsFilePrompt();
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				OperationsFile = dlg.FileName;
				LastOperationsFile = OperationsFile;
				tbFile.Text = OperationsFile;
				AlloGens = new AllomorphGenerators();
				AlloGens.WritingSystems = WritingSystems;
				Operation = AlloGens.CreateNewOperation();
				Pattern = Operation.Pattern;
				Operations = AlloGens.Operations;
				FillOperationsListBox();
				if (lvOperations.Visible)
				{
					FillApplyOperationsListView();
				}
				if (lvEditReplaceOps.Visible)
				{
					FillReplaceOpsListView();
				}
				// Need to save it since it exists
				DoSave();
			}
			else if (String.IsNullOrEmpty(OperationsFile))
			{
				// probably first time run and user chose to cancel creating the needed file; quit
				this.Dispose();
			}
		}

		protected void btnHelp_Click(object sender, EventArgs e)
		{
			string pathToUse = GetUserDocPath();
			Process.Start(pathToUse);
		}

		protected abstract string GetUserDocPath();
		protected abstract Uri GetBaseUri();

		protected string GetAppBaseDir()
		{
			string basedir;
			string rootdir;
			int indexOfBinInPath;
			DetermineIndexOfBinInExecutablesPath(out rootdir, out indexOfBinInPath);
			if (indexOfBinInPath >= 0)
				basedir = rootdir.Substring(0, indexOfBinInPath);
			else
				basedir = rootdir;
			return basedir;
		}

		protected void DetermineIndexOfBinInExecutablesPath(
			out string rootdir,
			out int indexOfBinInPath
		)
		{
			Uri uriBase = GetBaseUri();
			rootdir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
			indexOfBinInPath = rootdir.LastIndexOf("bin");
		}

		protected void btnMatch_Click(object sender, EventArgs e)
		{
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropTable);

			using (
				SimpleMatchDlgAlloGen dlg = new SimpleMatchDlgAlloGen(
					Cache.WritingSystemFactory,
					PropTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"),
					Cache.DefaultVernWs,
					stylesheet,
					Cache
				)
			)
			{
				Matcher agMatcher = Pattern.Matcher;
				dlg.SetDlgValues(agMatcher, stylesheet);
				if (dlg.ShowDialog() != DialogResult.OK || dlg.Pattern.Length == 0)
					return;
				agMatcher = dlg.GetMatcher();
				Pattern.Matcher = agMatcher;
				tbMatch.Text = agMatcher.Pattern;
				MarkAsChanged(true);
			}
		}

		protected virtual void tabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.Cursor = Cursors.WaitCursor;
			TabPage page = (sender as TabControl).SelectedTab;
			if (page != null)
			{
				LastTab = tabControl.SelectedIndex;
				if (LastTab == 0)
					FillOperationsListBox();
				else if (LastTab == 1)
					FillApplyOperationsListView();
				else
					FillReplaceOpsListView();
			}
			this.Cursor = Cursors.Arrow;
		}

		protected virtual bool CheckForInvalidActionComponents()
		{
			return CheckForInvalidEnvironmentsAndStemNames();
		}

		protected virtual string CreateUndoRedoPrompt(Operation op)
		{
			return " Allomorph Generation for '" + op.Name;
		}

		protected void btnApplyOperations_Click(object sender, EventArgs e)
		{
			RememberNonChosenEntries(Operation);
			if (lvOperations.CheckedItems.Count == 0)
			{
				MessageBox.Show("No operations are selected, so there's nothing to do");
				return;
			}
			if (!CheckForInvalidActionComponents())
			{
				return;
			}
			this.Cursor = Cursors.WaitCursor;
			List<Replace> replaceOpsToUse = new List<Replace>();
			foreach (ListViewItem lvItem in lvOperations.CheckedItems)
			{
				Operation op = (Operation)lvItem.Tag;
				List<ILexEntry> nonChosenEntries = new List<ILexEntry>();
				if (dictNonChosen.ContainsKey(op))
				{
					nonChosenEntries = dictNonChosen[op];
				}
				PatternMatcher patMatcher = new PatternMatcher(Cache, AlloGens);
				patMatcher.ApplyTo = cbApplyTo.SelectedItem as ApplyTo;
				IList<ILexEntry> matchingEntries,
					matchingEntriesWithAllos;
				GetMatchingEntries(patMatcher, out matchingEntries, out matchingEntriesWithAllos);
				if (matchingEntries == null || matchingEntries.Count() == 0)
				{
					continue;
				}
				GetRepaceOpsToUse(replaceOpsToUse, op);
				Replacer replacer = new Replacer(replaceOpsToUse);
				string undoRedoPrompt = CreateUndoRedoPrompt(op);
				UndoableUnitOfWorkHelper.Do(
					"Undo" + undoRedoPrompt,
					"Redo" + undoRedoPrompt,
					Cache.ActionHandlerAccessor,
					() =>
					{
						foreach (ILexEntry entry in matchingEntries)
						{
							if (nonChosenEntries.Contains(entry))
							{
								continue;
							}
							string formToUse = patMatcher.GetToMatch(entry).Text;
							List<string> forms = new List<string>();
							foreach (WritingSystem ws in WritingSystems)
							{
								forms.Add(GetPreviewForm(replacer, formToUse, ws));
							}
							ApplyOperationToEntry(op, entry, forms);
						}
					}
				);
			}
			ShowPreview();
			this.Cursor = Cursors.Arrow;
		}

		protected virtual void ApplyOperationToEntry(
			Operation op,
			ILexEntry entry,
			List<string> forms
		)
		{
			IMoStemAllomorph form = alloCreator.CreateAllomorph(entry, forms);
			if (op.Action.StemName.Guid.Length > 0)
			{
				alloCreator.AddStemName(form, op.Action.StemName.Guid);
			}
			if (op.Action.Environments.Count > 0)
			{
				alloCreator.AddEnvironments(form, op.Action.Environments);
			}
		}

		protected void GetRepaceOpsToUse(List<Replace> replaceOpsToUse, Operation op)
		{
			replaceOpsToUse.Clear();
			foreach (string guid in op.Action.ReplaceOpRefs)
			{
				Replace replace = AlloGens.FindReplaceOp(guid);
				if (replace != null)
				{
					replaceOpsToUse.Add(replace);
				}
			}
		}

		protected bool CheckForInvalidEnvironmentsAndStemNames()
		{
			bool allIsGood = true;
			foreach (ListViewItem lvItem in lvOperations.CheckedItems)
			{
				Operation op = (Operation)lvItem.Tag;
				string stemNameGuid = op.Action.StemName.Guid;
				if (stemNameGuid.Length > 0)
				{
					var stemName =
						Cache.ServiceLocator.ObjectRepository.GetObjectOrIdWithHvoFromGuid(
							new Guid(stemNameGuid)
						);
					if (stemName == null)
					{
						ReportMissingFLExItem("The stem name '", op.Action.StemName.Name, op.Name);
						allIsGood = false;
					}
				}
				if (op.Action.Environments.Count > 0)
				{
					foreach (AlloGenModel.Environment env in op.Action.Environments)
					{
						var phEnv =
							Cache.ServiceLocator.ObjectRepository.GetObjectOrIdWithHvoFromGuid(
								new Guid(env.Guid)
							);
						if (phEnv == null)
						{
							ReportMissingFLExItem("The environment '", env.Name, op.Name);
							allIsGood = false;
						}
					}
				}
			}
			return allIsGood;
		}

		protected void ReportMissingFLExItem(
			string missingItem,
			string itemName,
			string operationName
		)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(missingItem);
			sb.Append(itemName);
			sb.Append("' is no longer found.  Please fix it in operation '");
			sb.Append(operationName);
			sb.Append("'.");
			MessageBox.Show(sb.ToString());
		}

		protected void lvOperations_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			ListViewItem itemChecked = e.Item;
			if (itemChecked == null)
			{
				return;
			}
			Operation op = itemChecked.Tag as Operation;
			if (op != null)
			{
				if (dictOperationActiveState.ContainsKey(op))
				{
					dictOperationActiveState.Remove(op);
				}
				dictOperationActiveState.Add(op, itemChecked.Checked);
			}
		}

		protected void lvOperations_SelectedIndexChanged(object sender, EventArgs e)
		{
			ShowPreview();
		}

		protected void ShowPreview()
		{
			if (LastOperationShown != null)
			{
				RememberNonChosenEntries(LastOperationShown);
			}
			string sCount = "0";
			if (lvOperations.SelectedItems.Count == 0)
			{
				return;
			}
			this.Cursor = Cursors.WaitCursor;
			List<Replace> replaceOpsToUse = new List<Replace>();
			ListViewItem lvItem = lvOperations.SelectedItems[0];
			LastApplyOperation = lvOperations.Items.IndexOf(lvItem);
			Operation = lvItem.Tag as Operation;
			Pattern = Operation.Pattern;
			if (Operation != null)
			{
				PatternMatcher patMatcher = new PatternMatcher(Cache, AlloGens);
				patMatcher.ApplyTo = cbApplyTo.SelectedItem as ApplyTo;
				IList<ILexEntry> matchingEntries,
					matchingEntriesWithItemsAlready;
				GetMatchingEntries(
					patMatcher,
					out matchingEntries,
					out matchingEntriesWithItemsAlready
				);
				if (matchingEntries == null)
				{
					string errMsg = string.Format(
						FwCoreDlgs.kstidErrorInRegEx,
						patMatcher.ErrorMessage
					);
					MessageBox.Show(
						this,
						errMsg,
						FwCoreDlgs.kstidErrorInRegExHeader,
						MessageBoxButtons.OK,
						MessageBoxIcon.Error
					);
					return;
				}
				List<ILexEntry> nonChosenEntries = new List<ILexEntry>();
				if (dictNonChosen.ContainsKey(Operation))
				{
					nonChosenEntries = dictNonChosen[Operation];
				}
				GetRepaceOpsToUse(replaceOpsToUse, Operation);
				Replacer replacer = new Replacer(replaceOpsToUse);
				lvPreview.Items.Clear();
				foreach (ILexEntry entry in matchingEntries)
				{
					lvItem = new ListViewItem("");
					lvItem.Tag = entry;
					lvItem.UseItemStyleForSubItems = false;
					string formToUse = patMatcher.GetToMatch(entry).Text;
					lvItem.SubItems.Add(formToUse);
					if (matchingEntriesWithItemsAlready.Contains(entry))
					{
						lvItem.SubItems[1].BackColor = Color.Yellow;
					}
					int i = 2;
					foreach (WritingSystem ws in WritingSystems)
					{
						string previewForm = GetPreviewForm(replacer, formToUse, ws);
						lvItem.SubItems.Add(previewForm);
						lvItem.SubItems[i].Font = ws.Font;
						lvItem.SubItems[i].ForeColor = ws.Color;
						i++;
					}
					lvPreview.Items.Add(lvItem);
					lvItem.Checked = !nonChosenEntries.Contains(entry);
				}
				sCount = matchingEntries.Count().ToString();
				lvPreview.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
				if (lvPreview.Columns.Count > 0)
				{
					lvPreview.Columns[0].Width = 25;
				}
			}
			lbCountRunOps.Text = sCount;
			LastOperationShown = Operation;
			this.Cursor = Cursors.Arrow;
		}

		protected virtual void GetMatchingEntries(
			PatternMatcher patMatcher,
			out IList<ILexEntry> matchingEntries,
			out IList<ILexEntry> matchingEntriesWithItemsAlready
		)
		{
			matchingEntries = patMatcher
				.MatchPattern(patMatcher.EntriesWithNoAllomorphs, Operation.Pattern)
				.ToList();
			matchingEntriesWithItemsAlready = patMatcher
				.MatchEntriesWithAllosPerPattern(Operation, Pattern)
				.ToList();
			foreach (ILexEntry entry in matchingEntriesWithItemsAlready)
			{
				matchingEntries.Add(entry);
			}
		}

		protected void RememberNonChosenEntries(Operation op)
		{
			List<ILexEntry> uncheckedEntries = new List<ILexEntry>();
			foreach (ListViewItem item in lvPreview.Items)
			{
				if (!item.Checked)
				{
					uncheckedEntries.Add((ILexEntry)item.Tag);
				}
			}
			if (dictNonChosen.ContainsKey(op))
			{
				dictNonChosen.Remove(op);
			}
			dictNonChosen.Add(op, uncheckedEntries);
		}

		protected string GetPreviewForm(Replacer replacer, string formToUse, WritingSystem ws)
		{
			string previewForm = replacer.ApplyReplaceOpToOneWS(formToUse, ws.Name);
			return previewForm;
		}

		protected void lvOperations_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			// Only show the context menu
			if (e.Column == 0)
			{
				ListView lvSender = (ListView)sender;
				Point ptLowerLeft = new Point(0, 10);
				ptLowerLeft = lvSender.PointToScreen(ptLowerLeft);
				operationsCheckBoxContextMenu.Show(ptLowerLeft);
				return;
			}
		}

		protected void lvPreview_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			// Do not sort the checkboxes column; instead show context menu
			if (e.Column == 0)
			{
				ListView lvSender = (ListView)sender;
				Point ptLowerLeft = new Point(0, 10);
				ptLowerLeft = lvSender.PointToScreen(ptLowerLeft);
				previewCheckBoxContextMenu.Show(ptLowerLeft);
				return;
			}
			// Following code taken from
			// https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/sort-listview-by-column
			// on 2023.01.04
			// Determine if clicked column is already the column that is being sorted.
			if (e.Column == lvwColumnSorter.SortColumn)
			{
				// Reverse the current sort direction for this column.
				if (lvwColumnSorter.Order == SortOrder.Ascending)
				{
					lvwColumnSorter.Order = SortOrder.Descending;
				}
				else
				{
					lvwColumnSorter.Order = SortOrder.Ascending;
				}
			}
			else
			{
				// Set the column number that is to be sorted; default to ascending.
				lvwColumnSorter.SortColumn = e.Column;
				lvwColumnSorter.Order = SortOrder.Ascending;
			}

			// Perform the sort with these new sort options.
			this.lvPreview.Sort();
		}

		protected void lvEditReplaceOps_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			// Following code taken from
			// https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/sort-listview-by-column
			// on 2023.01.04
			// Determine if clicked column is already the column that is being sorted.
			if (e.Column == lvwEditReplaceOpsColumnSorter.SortColumn)
			{
				// Reverse the current sort direction for this column.
				if (lvwEditReplaceOpsColumnSorter.Order == SortOrder.Ascending)
				{
					lvwEditReplaceOpsColumnSorter.Order = SortOrder.Descending;
				}
				else
				{
					lvwEditReplaceOpsColumnSorter.Order = SortOrder.Ascending;
				}
			}
			else
			{
				// Set the column number that is to be sorted; default to ascending.
				lvwEditReplaceOpsColumnSorter.SortColumn = e.Column;
				lvwEditReplaceOpsColumnSorter.Order = SortOrder.Ascending;
			}

			// Perform the sort with these new sort options.
			lvEditReplaceOps.Sort();
		}

		protected void btnEditReplaceOp_Click(object sender, EventArgs e)
		{
			InvokeEditReplaceOpFormMasterList();
		}

		protected void btnAddNewReplaceOp_Click(object sender, EventArgs e)
		{
			using (var dialog = new EditReplaceOpForm())
			{
				Replace replace = CreateNewReplace();
				dialog.Initialize(replace, WritingSystems, Cache);
				dialog.ShowDialog();
				if (dialog.DialogResult == DialogResult.OK)
				{
					replace = dialog.ReplaceOp;
					AddNewReplaceOpToMasterList(replace);
				}
			}
		}

		protected Replace CreateNewReplace()
		{
			Replace replace = new Replace();
			foreach (WritingSystem ws in WritingSystems)
			{
				replace.WritingSystemRefs.Add(ws.Name);
			}
			return replace;
		}

		protected void AddNewReplaceOpToMasterList(Replace replace)
		{
			AlloGens.AddReplaceOp(replace);
			ListViewItem item = new ListViewItem();
			item.Tag = replace;
			lvEditReplaceOps.Items.Add(item);
			int index = Math.Max(0, lvEditReplaceOps.Items.Count - 1);
			lvEditReplaceOps.Items[index].Selected = true;
			lvEditReplaceOps.Select();
			MarkAsChanged(true);
			FillReplaceOpsListView();
		}

		protected void btnDeleteReplaceOp_Click(object sender, EventArgs e)
		{
			if (!CanDoReplaceOpsMasterListOption())
			{
				return;
			}
			ListViewItem lvItem = lvEditReplaceOps.SelectedItems[0];
			Replace replace = (Replace)lvItem.Tag;
			StringBuilder sb = BuildDeleteReplaceOpMessage(replace);
			CheckOnRemovingSelectedReplaceOpFromMasterList(sb.ToString(), replace);
		}

		protected StringBuilder BuildDeleteReplaceOpMessage(Replace replace)
		{
			List<Operation> operationsContainingReplaceOp = AlloGens.FindOperationsUsedByReplaceOp(
				replace
			);
			StringBuilder sb = new StringBuilder();
			sb.Append("Replace operation '");
			sb.Append(replace.ToString());
			sb.Append("' will be deleted.\n");
			if (operationsContainingReplaceOp.Count > 0)
			{
				sb.Append("It is used in the following operations:\n\n");
				foreach (Operation op in operationsContainingReplaceOp)
				{
					sb.Append(op.Name);
					sb.Append("\n");
				}
				sb.Append("\n");
			}
			else
			{
				sb.Append("It is not used in any operations.\n\n");
			}
			sb.Append("Are you sure you want to delete it?");
			return sb;
		}

		protected StringBuilder BuildDeleteReplaceOpRefMessage(Replace replace)
		{
			List<Operation> operationsContainingReplaceOp = AlloGens.FindOperationsUsedByReplaceOp(
				replace
			);
			StringBuilder sb = new StringBuilder();
			sb.Append("Replace operation '");
			sb.Append(replace.ToString());
			sb.Append("' will be removed from this operation.\n");
			sb.Append("You can also delete it from the master list.\n");
			if (operationsContainingReplaceOp.Count > 0)
			{
				sb.Append("It is used in the following operations:\n\n");
				foreach (Operation op in operationsContainingReplaceOp)
				{
					sb.Append(op.Name);
					sb.Append("\n");
				}
				sb.Append("\n");
			}
			else
			{
				sb.Append("It is not used in any operations.\n\n");
			}
			sb.Append("Do you want to delete it from the master list?");
			return sb;
		}

		protected void lvEditReplaceOps_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lvEditReplaceOps.SelectedItems.Count > 0)
			{
				ListViewItem lvItem = lvEditReplaceOps.SelectedItems[0];
				LastEditReplaceOps = lvEditReplaceOps.Items.IndexOf(lvItem);
				lbCountReplaceOps.Text =
					(LastEditReplaceOps + 1).ToString()
					+ " / "
					+ lvEditReplaceOps.Items.Count.ToString();
			}
		}

		protected void InvokeEditReplaceOpFormMasterList()
		{
			if (!CanDoReplaceOpsMasterListOption())
			{
				return;
			}
			ListViewItem item = lvEditReplaceOps.SelectedItems[0];
			;
			Replace replace = (Replace)item.Tag;
			using (var dialog = new EditReplaceOpForm())
			{
				dialog.Initialize(replace, WritingSystems, Cache);
				dialog.ShowDialog();
				if (dialog.DialogResult == DialogResult.OK)
				{
					int index = lvEditReplaceOps.SelectedIndices[0];
					replace = dialog.ReplaceOp;
					AlloGens.AddReplaceOp(replace);
					lvEditReplaceOps.Items[index].Tag = replace;
					MarkAsChanged(true);
					FillReplaceOpsListView();
				}
			}
		}

		protected void lvEditReplaceOps_DoubleClick(object sender, EventArgs e)
		{
			InvokeEditReplaceOpFormMasterList();
		}

		protected void cbApplyTo_SelectedIndexChanged(object sender, EventArgs e)
		{
			ComboBox cb = (ComboBox)sender;
			AlloGens.ApplyTo = cb.SelectedIndex;
			applyToField = ((ApplyTo)cb.SelectedItem).Name;
			SetUpPreviewCheckedListBox();
		}
	}
}
