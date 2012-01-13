using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Controls; // for XmlViews stuff, especially borrowed form ColumnConfigureDialog
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using System.Diagnostics;
using SIL.CoreImpl;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for ConfigureInterlinDialog.
	/// </summary>
	public class ConfigureInterlinDialog : Form, IFWDisposable
	{
		private Label label1;
		private Label label2;
		private ListBox optionsList;
		private Label label3;
		private FwOverrideComboBox wsCombo;
		private Button moveDownButton;
		private Button moveUpButton;
		private Button removeButton;
		private Button addButton;
		private Button helpButton;
		private Button cancelButton;
		private ListView currentList;
		private ColumnHeader InfoColumn;
		private Label label4;
		private Button okButton;
		private ColumnHeader LineColumn;

		private const string s_helpTopic = "khtpConfigureInterlinearLines";
		private HelpProvider helpProvider;

		private Dictionary<ColumnConfigureDialog.WsComboContent, ComboBox.ObjectCollection> m_cachedComboBoxes;
		private IContainer components;

		bool m_fUpdatingWsCombo = false; // true during UpdateWsCombo
		private FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private ImageList imageList1;

		InterlinLineChoices m_choices;


		public ConfigureInterlinDialog(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			InterlinLineChoices choices)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			m_helpTopicProvider = helpTopicProvider;
			helpProvider = new HelpProvider();
			helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);

			m_cachedComboBoxes = new Dictionary<ColumnConfigureDialog.WsComboContent, ComboBox.ObjectCollection>();

			m_cache = cache;
			m_choices = choices;

			InitPossibilitiesList();

			// Owner draw requires drawing the column header as well as the list items.  See LT-7007.
			currentList.DrawColumnHeader += currentList_DrawColumnHeader;
			InitCurrentList(0); // also inits WsCombo.

			currentList.SelectedIndexChanged += currentList_SelectedIndexChanged;
			optionsList.SelectedIndexChanged += optionsList_SelectedIndexChanged;
			EnableControls();
		}

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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigureInterlinDialog));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.optionsList = new System.Windows.Forms.ListBox();
			this.label3 = new System.Windows.Forms.Label();
			this.wsCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.moveDownButton = new System.Windows.Forms.Button();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.moveUpButton = new System.Windows.Forms.Button();
			this.removeButton = new System.Windows.Forms.Button();
			this.addButton = new System.Windows.Forms.Button();
			this.helpButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.currentList = new System.Windows.Forms.ListView();
			this.LineColumn = new System.Windows.Forms.ColumnHeader();
			this.InfoColumn = new System.Windows.Forms.ColumnHeader();
			this.label4 = new System.Windows.Forms.Label();
			this.okButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// optionsList
			//
			resources.ApplyResources(this.optionsList, "optionsList");
			this.optionsList.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.optionsList.Name = "optionsList";
			this.optionsList.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.optionsList_DrawItem);
			this.optionsList.DoubleClick += new System.EventHandler(this.addButton_Click);
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// wsCombo
			//
			this.wsCombo.AllowSpaceInEditBox = false;
			resources.ApplyResources(this.wsCombo, "wsCombo");
			this.wsCombo.Name = "wsCombo";
			this.wsCombo.SelectedIndexChanged += new System.EventHandler(this.wsCombo_SelectedIndexChanged);
			//
			// moveDownButton
			//
			resources.ApplyResources(this.moveDownButton, "moveDownButton");
			this.moveDownButton.ImageList = this.imageList1;
			this.moveDownButton.Name = "moveDownButton";
			this.moveDownButton.Click += new System.EventHandler(this.moveDownButton_Click);
			//
			// imageList1
			//
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList1.Images.SetKeyName(0, "LargeUpArrow.bmp");
			this.imageList1.Images.SetKeyName(1, "LargeDownArrow.bmp");
			//
			// moveUpButton
			//
			resources.ApplyResources(this.moveUpButton, "moveUpButton");
			this.moveUpButton.ImageList = this.imageList1;
			this.moveUpButton.Name = "moveUpButton";
			this.moveUpButton.Click += new System.EventHandler(this.moveUpButton_Click);
			//
			// removeButton
			//
			resources.ApplyResources(this.removeButton, "removeButton");
			this.removeButton.Name = "removeButton";
			this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
			//
			// addButton
			//
			resources.ApplyResources(this.addButton, "addButton");
			this.addButton.Name = "addButton";
			this.addButton.Click += new System.EventHandler(this.addButton_Click);
			//
			// helpButton
			//
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.Click += new System.EventHandler(this.helpButton_Click);
			//
			// cancelButton
			//
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Name = "cancelButton";
			//
			// currentList
			//
			resources.ApplyResources(this.currentList, "currentList");
			this.currentList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.LineColumn,
			this.InfoColumn});
			this.currentList.FullRowSelect = true;
			this.currentList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.currentList.HideSelection = false;
			this.currentList.MultiSelect = false;
			this.currentList.Name = "currentList";
			this.currentList.OwnerDraw = true;
			this.currentList.ShowItemToolTips = true;
			this.currentList.UseCompatibleStateImageBehavior = false;
			this.currentList.View = System.Windows.Forms.View.Details;
			this.currentList.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.currentList_DrawItem);
			this.currentList.DoubleClick += new System.EventHandler(this.removeButton_Click);
			//
			// LineColumn
			//
			resources.ApplyResources(this.LineColumn, "LineColumn");
			//
			// InfoColumn
			//
			resources.ApplyResources(this.InfoColumn, "InfoColumn");
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// okButton
			//
			resources.ApplyResources(this.okButton, "okButton");
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Name = "okButton";
			//
			// ConfigureInterlinDialog
			//
			this.AcceptButton = this.okButton;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.label3);
			this.Controls.Add(this.wsCombo);
			this.Controls.Add(this.moveDownButton);
			this.Controls.Add(this.moveUpButton);
			this.Controls.Add(this.removeButton);
			this.Controls.Add(this.addButton);
			this.Controls.Add(this.helpButton);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.currentList);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.optionsList);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "ConfigureInterlinDialog";
			this.ResumeLayout(false);

		}
		#endregion

		public InterlinLineChoices Choices
		{
			get
			{
				CheckDisposed();

				return m_choices;
			}
		}

		// Initialize the list of possible lines.
		void InitPossibilitiesList()
		{
			optionsList.Items.AddRange(m_choices.LineOptions());
		}

		// Init the list and select specified item
		void InitCurrentList(InterlinLineSpec spec)
		{
			InitCurrentList(m_choices.IndexOf(spec));
		}

		/// <summary>
		/// (re)initialize the current list to correspond to the items in m_choices.
		/// This will destroy the selection, and then try to select the indicated line.
		/// </summary>
		void InitCurrentList(int index)
		{
			currentList.SuspendLayout();
			currentList.Items.Clear();
			foreach (InterlinLineSpec ls in m_choices)
			{
				string[] cols = new string[2];

				cols[0] = TsStringUtils.NormalizeToNFC(m_choices.LabelFor(ls.Flid));

				string wsName = "";
				// This tries to find a matching ws from the combo box that would be displayed for this item
				// The reason we use the combo box is because that will give us names like "Default Analysis" instead of
				// the actual analysis ws name.
				foreach (WsComboItem item in WsComboItems(ls.ComboContent))
				{
					if (getWsFromId(item.Id) == ls.WritingSystem)
					{
						wsName = item.ToString();
						break;
					}
				}
				// Last ditch effort
				if (wsName == "")
				{
					ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
					int wsui = m_cache.DefaultUserWs;
					IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ls.WritingSystem);
					if (wsObj != null)
						wsName = wsObj.DisplayLabel;
				}
				cols[1] = TsStringUtils.NormalizeToNFC(wsName);
				cols[1] = cols[1].Substring(0, Math.Min(cols[1].Length, 42));

				var item1WithToolTip = new ListViewItem(cols);
				item1WithToolTip.ToolTipText = TsStringUtils.NormalizeToNFC(wsName);

				currentList.Items.Add(item1WithToolTip);
			}

			if (index > currentList.Items.Count && index > 0)
				index--; // for when we delete the last item.
			if (index >= 0 && index < currentList.Items.Count) // range check mainly for passing 0 on empty
				currentList.Items[index].Selected = true;

			currentList.ResumeLayout();
		}

		/// <summary>
		/// This is used to create an object collection with the appropriate writing system choices to be used in wsCombo.  The reason it is cached is because
		/// list generation will require looping through each kind of combo box several times.
		/// </summary>
		/// <param name="comboContent"></param>
		/// <returns></returns>
		private ComboBox.ObjectCollection WsComboItems(ColumnConfigureDialog.WsComboContent comboContent)
		{
			return WsComboItemsInternal(m_cache, wsCombo, m_cachedComboBoxes, comboContent);
		}

		/// <summary>
		/// This is used to create an object collection with the appropriate writing system choices to be used in wsCombo.  The reason it is cached is because
		/// list generation will require looping through each kind of combo box several times.
		///
		/// This version is visible to InterlinDocRootSiteBase for its context menu.
		/// </summary>
		/// <param name="cachedBoxes"></param>
		/// <param name="comboContent"></param>
		/// <param name="cache"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		internal static ComboBox.ObjectCollection WsComboItemsInternal(FdoCache cache, ComboBox owner,
			Dictionary<ColumnConfigureDialog.WsComboContent, ComboBox.ObjectCollection> cachedBoxes,
			ColumnConfigureDialog.WsComboContent comboContent)
		{
			ComboBox.ObjectCollection objectCollection;
			if (!cachedBoxes.ContainsKey(comboContent))
			{
				objectCollection = new ComboBox.ObjectCollection(owner);

				// The final argument here restricts writing systems that will be added to the combo box to
				// only be "real" writing systems.  So, English will be added, but not "Default Analysis".
				// This functionality should eventually go away.  See LT-4740.
				// JohnT: it now partially has, two lines support 'best analysis'.
				ColumnConfigureDialog.AddWritingSystemsToCombo(cache, objectCollection, comboContent,
					comboContent != ColumnConfigureDialog.WsComboContent.kwccBestAnalysis);
				cachedBoxes[comboContent] = objectCollection;
			}
			else
			{
				objectCollection = cachedBoxes[comboContent];
			}

			return objectCollection;
		}

		int CurrentListIndex
		{
			get
			{
				if (currentList.SelectedIndices.Count == 0)
					return -1;
				return currentList.SelectedIndices[0];
			}
		}

		/// <summary>
		/// Set all controls to appropriate states for the current list selections.
		/// </summary>
		void EnableControls()
		{
			addButton.Enabled = optionsList.SelectedIndex >= 0;
			// We could use OkToRemove here, but we'd rather be able to display the message
			// if there is some reason not to.
			int listIndex = CurrentListIndex;
			removeButton.Enabled = listIndex >= 0; //Enhance: && m_choices.OkToRemove(listIndex);
			moveDownButton.Enabled = listIndex >= 0 && m_choices.OkToMoveDown(listIndex);
			moveUpButton.Enabled = listIndex >= 0 && m_choices.OkToMoveUp(listIndex);
			UpdateWsComboValue();
			wsCombo.Enabled = listIndex >= 0 && m_choices.OkToChangeWritingSystem(listIndex);
		}

		void UpdateWsComboValue()
		{
			try
			{
				m_fUpdatingWsCombo = true;
				int index = CurrentListIndex;
				if (index < 0 || index >= m_choices.Count)
				{
					wsCombo.SelectedIndex = -1;
					wsCombo.Enabled = false;
					return;
				}
				InterlinLineSpec spec = m_choices[index];

				ComboBox.ObjectCollection comboObjects = WsComboItems(spec.ComboContent);
				object[] choices = new object[comboObjects.Count];
				comboObjects.CopyTo(choices, 0);
				wsCombo.Items.Clear();
				wsCombo.Items.AddRange(choices);

				int ws = spec.WritingSystem;
				wsCombo.Enabled = true;
				// JohnT: note that, because 'Default analysis' and 'Default Vernacular'
				// come first, the corresponding actual writing systems will never be
				// chosen by this algorithm.
				foreach (WsComboItem item in wsCombo.Items)
				{
					if (getWsFromId(item.Id) == ws)
					{
						wsCombo.SelectedItem = item;
						break;
					}
				}
			}
			finally
			{
				m_fUpdatingWsCombo = false;
			}
		}

		private int getWsFromId(string id)
		{
			// special case, the only few we support so far (and only for a few fields).
			if (id == "best analysis")
				return WritingSystemServices.kwsFirstAnal;//LangProject.kwsFirstAnal;
			else if (id == "vern in para")
				return WritingSystemServices.kwsVernInParagraph;
			Debug.Assert(!XmlViewsUtils.GetWsRequiresObject(id), "Writing system is magic.  These should never be used in the Interlinear area.");

			int ws = -50;
			try
			{
				if (!XmlViewsUtils.GetWsRequiresObject(id))
				{
					// Try to convert the ws parameter into an int.  Sometimes the parameter
					// cannot be interpreted without an object, such as when the ws is a magic
					// string that will change the actual ws depending on the contents of the
					// object.  In these cases, we give -50 as a known constant to check for.
					// This can possibly throw an exception, so we'll enclose it in a try block.
					ws = WritingSystemServices.InterpretWsLabel(m_cache, id, null, 0, 0, null);
				}
			}
			catch
			{
				Debug.Assert(ws != -50, "InterpretWsLabel was not able to interpret the Ws Label.  The most likely cause for this is that a magic ws was passed in.");
			}
			return ws;
		}

		private void wsCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_fUpdatingWsCombo)
				return;
			if (!(wsCombo.SelectedItem is WsComboItem))
				return;
			int listIndex = CurrentListIndex;
			if (listIndex < 0)
				return;

			InterlinLineSpec spec = m_choices[listIndex];
			spec.WritingSystem = getWsFromId(((WsComboItem)wsCombo.SelectedItem).Id);

			InitCurrentList(listIndex);
		}

		private void addButton_Click(object sender, System.EventArgs e)
		{
			if (optionsList.SelectedItem == null)
				return;
			int flid = (optionsList.SelectedItem as LineOption).Flid;
			int index = m_choices.Add(flid);
			InitCurrentList(index);

		}

		private void removeButton_Click(object sender, System.EventArgs e)
		{
			int index =  CurrentListIndex;
			if (index < 0)
				return;
			string message;
			InterlinLineSpec spec = m_choices[index];
			if (!m_choices.OkToRemove(spec, out message))
			{
				MessageBox.Show(this, message, ITextStrings.ksCannotHideField);
				return;
			}
			if (message != null && MessageBox.Show(this, message, ITextStrings.ksWarning, MessageBoxButtons.OKCancel)
				== DialogResult.Cancel)
				return;
			m_choices.Remove(spec);
			InitCurrentList(index);
		}

		private void moveUpButton_Click(object sender, System.EventArgs e)
		{
			if (CurrentListIndex < 0 || !m_choices.OkToMoveUp(CurrentListIndex))
				return;
			InterlinLineSpec spec = m_choices[CurrentListIndex];
			m_choices.MoveUp(CurrentListIndex);
			InitCurrentList(spec);
		}

		private void moveDownButton_Click(object sender, System.EventArgs e)
		{
			if (CurrentListIndex < 0 || !m_choices.OkToMoveDown(CurrentListIndex))
				return;
			InterlinLineSpec spec = m_choices[CurrentListIndex];
			m_choices.MoveDown(CurrentListIndex);
			InitCurrentList(spec);
		}

		private void helpButton_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		private void currentList_SelectedIndexChanged(object sender, EventArgs e)
		{
			EnableControls();
		}

		private void optionsList_SelectedIndexChanged(object sender, EventArgs e)
		{
			EnableControls();
		}

		private void optionsList_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
		{
			LineOption option = optionsList.Items[e.Index] as LineOption;
			InterlinLineSpec spec = m_choices.CreateSpec(option.Flid, 0);
			DrawItem(e, spec);
		}

		private void DrawItem(System.Windows.Forms.DrawItemEventArgs e, InterlinLineSpec spec)
		{
			bool selected = ((e.State & DrawItemState.Selected) != 0);
			Brush textBrush = GetBrush(spec, selected);
			try
			{
				Font drawFont = e.Font;
				e.DrawBackground();
				e.Graphics.DrawString(optionsList.Items[e.Index].ToString(), drawFont, textBrush, e.Bounds);
			}
			finally
			{
				if (!selected)
					textBrush.Dispose();
			}
		}

		private void currentList_DrawItem(object sender, DrawListViewItemEventArgs e)
		{
			InterlinLineSpec spec = m_choices[e.ItemIndex] as InterlinLineSpec;
			DrawItem(e, spec);
		}

		/// <summary>
		/// Owner draw requires drawing the column header as well as the list items.  See LT-7007.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void currentList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
		{
			e.DrawBackground();
			e.DrawText();
		}

		private Brush GetBrush(InterlinLineSpec spec, bool selected)
		{
			Brush textBrush = SystemBrushes.ControlText;
			if (selected)
			{
				textBrush = SystemBrushes.HighlightText;
			}
			else
			{
				textBrush = new SolidBrush(m_choices.LabelColorFor(spec));
			}
			return textBrush;
		}

		private void DrawItem(System.Windows.Forms.DrawListViewItemEventArgs e, InterlinLineSpec spec)
		{
			Brush backBrush = SystemBrushes.ControlLightLight;
			if (e.Item.Selected)
				backBrush = SystemBrushes.Highlight;
			e.Graphics.FillRectangle(backBrush, e.Bounds);
			if (e.Item.Focused)
				ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds);
			SolidBrush textBrush = GetBrush(spec, e.Item.Selected) as SolidBrush;
			try
			{
				Font drawFont = e.Item.Font;
				ListViewItem item = e.Item as ListViewItem;
				// Draw the line label.
				e.Graphics.DrawString(item.Text, drawFont, textBrush, e.Bounds);
				// Now draw the WritingSystem info.
				e.Graphics.DrawString(item.SubItems[1].Text, item.SubItems[1].Font,
									  e.Item.Selected ? textBrush : SystemBrushes.ControlText, item.SubItems[1].Bounds);
			}
			finally
			{
				if (!e.Item.Selected)
					textBrush.Dispose();
			}
		}
	}
}
