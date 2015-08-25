// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExportTranslatedListsDlg.cs
// Responsibility: mcconnel
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils.FileDialog;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This dialog allows the user to select specific lists to export in specific writing
	/// systems.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ExportTranslatedListsDlg : Form
	{
		private IPropertyTable m_propertyTable;
		FdoCache m_cache;
		string m_titleFrag;
		string m_defaultExt;
		string m_filter;
		Dictionary<int, bool> m_excludedListFlids = new Dictionary<int, bool>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ExportTranslatedListsDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ExportTranslatedListsDlg()
		{
			InitializeComponent();
			m_btnExport.Enabled = false;

			// We don't want to deal with these lists, at least for now.
			m_excludedListFlids.Add(MoMorphDataTags.kflidProdRestrict, true);
			m_excludedListFlids.Add(ReversalIndexTags.kflidPartsOfSpeech, true);
			m_excludedListFlids.Add(PhPhonDataTags.kflidPhonRuleFeats, true);
			m_excludedListFlids.Add(LangProjectTags.kflidAffixCategories, true);
			m_excludedListFlids.Add(LangProjectTags.kflidAnnotationDefs, true);
			m_excludedListFlids.Add(LangProjectTags.kflidCheckLists, true);
			m_columnLists.Width = m_lvLists.Width - 25;
			m_columnWs.Width = m_lvWritingSystems.Width - 25;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the dialog with all needed information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize(IPropertyTable propertyTable, FdoCache cache, string titleFrag,
			string defaultExt, string filter)
		{
			m_propertyTable = propertyTable;
			m_cache = cache;
			m_titleFrag = titleFrag;
			m_defaultExt = defaultExt;
			m_filter = filter;

			FillInLists();
			FillInWritingSystems();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the selected output filename.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FileName
		{
			get { return m_tbFilepath.Text; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the list of selected writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> SelectedWritingSystems
		{
			get
			{
				List<int> list = new List<int>(m_lvWritingSystems.CheckedItems.Count);
				foreach (var item in m_lvWritingSystems.CheckedItems)
				{
					Debug.Assert(item is ListViewItem);
					ListViewItem lvi = item as ListViewItem;
					Debug.Assert(lvi.Tag is IWritingSystem);
					list.Add((lvi.Tag as IWritingSystem).Handle);
				}
				return list;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the list of selected lists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<ICmPossibilityList> SelectedLists
		{
			get
			{
				List<ICmPossibilityList> list = new List<ICmPossibilityList>(m_lvLists.CheckedItems.Count);
				foreach (var item in m_lvLists.CheckedItems)
				{
					Debug.Assert(item is ListViewItem);
					ListViewItem lvi = item as ListViewItem;
					Debug.Assert(lvi.Tag is ICmPossibilityList);
					list.Add(lvi.Tag as ICmPossibilityList);
				}
				return list;
			}
		}

		private void FillInLists()
		{
			ICmPossibilityListRepository repo = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			foreach (var list in repo.AllInstances())
			{
				if (list.Owner != null &&
					list.Owner != m_cache.LangProject.TranslatedScriptureOA &&
					!m_excludedListFlids.ContainsKey(list.OwningFlid))
				{
					ListViewItem lvi = new ListViewItem();
					lvi.Text = list.Name.UserDefaultWritingSystem.Text;
					if (String.IsNullOrEmpty(lvi.Text) || lvi.Text == list.Name.NotFoundTss.Text)
						lvi.Text = list.Name.BestAnalysisVernacularAlternative.Text;
					lvi.Tag = list;
					m_lvLists.Items.Add(lvi);
				}
			}
			m_lvLists.Sort();
		}

		private void FillInWritingSystems()
		{
			foreach (var xws in m_cache.LangProject.AnalysisWritingSystems)
			{
				if (xws.IcuLocale != "en")
					m_lvWritingSystems.Items.Add(CreateListViewItemForWs(xws));
			}
			foreach (var xws in m_cache.LangProject.VernacularWritingSystems)
			{
				if (xws.IcuLocale != "en")
					m_lvWritingSystems.Items.Add(CreateListViewItemForWs(xws));
			}
			m_lvWritingSystems.Sort();
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			m_columnLists.Width = m_lvLists.Width - 25;
			m_columnWs.Width = m_lvWritingSystems.Width - 25;
		}

		private ListViewItem CreateListViewItemForWs(IWritingSystem xws)
		{
			ListViewItem lvi = new ListViewItem();
			lvi.Text = xws.DisplayLabel;
			lvi.Tag = xws;
			lvi.Checked = xws.Handle == m_cache.DefaultAnalWs;
			return lvi;
		}

		private void m_btnBrowse_Click(object sender, EventArgs e)
		{
			using (var dlg = new SaveFileDialogAdapter())
			{
				dlg.AddExtension = true;
				dlg.DefaultExt = String.IsNullOrEmpty(m_defaultExt) ? ".xml" : m_defaultExt;
				dlg.Filter = String.IsNullOrEmpty(m_filter) ? "*.xml" : m_filter;
				dlg.Title = String.Format(xWorksStrings.ExportTo0,
					String.IsNullOrEmpty(m_titleFrag) ? "Translated List" : m_titleFrag);
				dlg.InitialDirectory = m_propertyTable.GetValue("ExportDir", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
				if (dlg.ShowDialog(this) != DialogResult.OK)
					return;
				m_tbFilepath.Text = dlg.FileName;
				EnableExportButton();
			}
		}

		private void m_btnExport_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void m_lvLists_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			EnableExportButton();
		}

		private void m_lvWritingSystems_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			EnableExportButton();
		}

		private void EnableExportButton()
		{
			if (String.IsNullOrEmpty(m_tbFilepath.Text) ||
				String.IsNullOrEmpty(m_tbFilepath.Text.Trim()))
			{
				m_btnExport.Enabled = false;
				return;
			}
			if (m_lvLists.CheckedItems.Count == 0)
			{
				m_btnExport.Enabled = false;
				return;
			}
			if (m_lvWritingSystems.CheckedItems.Count == 0)
			{
				m_btnExport.Enabled = false;
				return;
			}
			m_btnExport.Enabled = true;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "khtpExportTranslatedListsDlg");
		}

		private void m_btnSelectAll_Click(object sender, EventArgs e)
		{
			foreach (var obj in m_lvLists.Items)
			{
				ListViewItem lvi = obj as ListViewItem;
				lvi.Checked = true;
			}
		}

		private void m_btnClearAll_Click(object sender, EventArgs e)
		{
			foreach (var obj in m_lvLists.Items)
			{
				ListViewItem lvi = obj as ListViewItem;
				lvi.Checked = false;
			}
		}

		private void m_tbFilepath_TextChanged(object sender, EventArgs e)
		{
			EnableExportButton();
		}
	}
}