// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AnthroFieldMappingDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using XCore;
using System.Reflection;

namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	/// ----------------------------------------------------------------------------------------
	/// <summary></summary>
	/// ----------------------------------------------------------------------------------------
	public partial class AnthroFieldMappingDlg : Form
	{
		FdoCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		IFwMetaDataCacheManaged m_mdc;
		IVwStylesheet m_stylesheet;
		NotebookImportWiz.RnSfMarker m_rsfm;
		ListRefFieldOptions m_listOpt;
		TextFieldOptions m_textOpt;
		DateFieldOptions m_dateOpt;
		StringFieldOptions m_stringOpt;
		LinkFieldOptions m_linkOpt;
		DiscardOptions m_discardOpt;
		Mediator m_mediator;
		Dictionary<int, string> m_mapFlidName;

		Point m_locSubCtrl = new Point(2, 20);
		Sfm2Xml.SfmFile m_sfmFile;

		string m_sContentsGroupFmt;
		string m_sContentsLabelFmt;
		//string m_sOptionsGroupFmt;

		internal class DestinationField
		{
			private int m_flid;
			private string m_name;

			internal DestinationField(int flid, string name)
			{
				m_flid = flid;
				m_name = name;
			}

			internal int Flid
			{
				get { return m_flid; }
			}

			internal string Name
			{
				get { return m_name; }
			}

			public override string ToString()
			{
				return m_name;
			}

			public override bool Equals(object obj)
			{
				DestinationField that = obj as DestinationField;
				if (that == null)
					return false;
				else
					return this.m_flid == that.m_flid && this.m_name == that.m_name;
			}

			public override int GetHashCode()
			{
				return this.m_flid.GetHashCode() + this.m_name.GetHashCode();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary></summary>
		/// ------------------------------------------------------------------------------------
		public AnthroFieldMappingDlg()
		{
			InitializeComponent();
			m_listOpt = new ListRefFieldOptions();
			m_textOpt = new TextFieldOptions();
			m_dateOpt = new DateFieldOptions();
			m_stringOpt = new StringFieldOptions();
			m_linkOpt = new LinkFieldOptions();
			m_discardOpt = new DiscardOptions();
			m_sContentsGroupFmt = m_groupContents.Text;
			m_sContentsLabelFmt = m_lblContents.Text;
			//m_sOptionsGroupFmt = m_groupOptions.Text;
		}

		public void Initialize(FdoCache cache, IHelpTopicProvider helpTopicProvider, IApp app,
			NotebookImportWiz.RnSfMarker rsf, Sfm2Xml.SfmFile sfmFile,
			Dictionary<int, string> mapFlidName, IVwStylesheet stylesheet,
			Mediator mediator)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_rsfm = rsf;
			m_sfmFile = sfmFile;
			m_stylesheet = stylesheet;
			m_mdc = cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			m_mediator = mediator;
			m_mapFlidName = mapFlidName;

			FillInFieldList();
			FillInContentsPane(rsf, sfmFile);
			SetSubControl();
		}

		private void FillInFieldList()
		{
			m_cbDestination.Items.Clear();
			m_cbDestination.Sorted = true;
			foreach (int flid in m_mapFlidName.Keys)
				m_cbDestination.Items.Add(new DestinationField(flid, m_mapFlidName[flid]));
			m_cbDestination.Sorted = false;
			m_cbDestination.Items.Insert(0, new DestinationField(0, LexTextControls.ksDoNotImport));
			int idx = m_cbDestination.Items.IndexOf(new DestinationField(m_rsfm.m_flid, m_rsfm.m_sName));
			if (idx > -1)
				m_cbDestination.SelectedIndex = idx;
		}

		private void FillInContentsPane(NotebookImportWiz.RnSfMarker rsf, Sfm2Xml.SfmFile sfmFile)
		{
			m_groupContents.Text = String.Format(m_sContentsGroupFmt, rsf.m_sMkr);
			m_lvContents.Items.Clear();
			Set<string> setContents = new Set<string>();
			foreach (Sfm2Xml.SfmField field in m_sfmFile.Lines)
			{
				if (field.Marker == rsf.m_sMkr)
				{
					if (!setContents.Contains(field.Data))
					{
						setContents.Add(field.Data);
						ListViewItem lvi = new ListViewItem(String.Format("\\{0} {1}", field.Marker,
							String.IsNullOrEmpty(field.Data) ? String.Empty : field.Data));
						m_lvContents.Items.Add(lvi);
					}
				}
			}
			m_lblContents.Text = String.Format(m_sContentsLabelFmt, rsf.m_sMkr,
				sfmFile.GetSFMCount(rsf.m_sMkr), m_lvContents.Items.Count);
		}

		private void SetSubControl()
		{
			m_groupOptions.Controls.Clear();
			if (m_rsfm.m_flid == 0)
			{
				m_groupOptions.Text = LexTextControls.ksDiscardedField;
				m_groupOptions.Controls.Add(m_discardOpt);
				m_discardOpt.Location = m_locSubCtrl;
				return;
			}
			CellarPropertyType cpt = (CellarPropertyType)m_mdc.GetFieldType(m_rsfm.m_flid);
			int clidDst = -1;
			switch (cpt)
			{
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					clidDst = m_mdc.GetDstClsId(m_rsfm.m_flid);
					switch (clidDst)
					{
						case RnGenericRecTags.kClassId:
							m_groupOptions.Text = LexTextControls.ksRnLinkFieldOptions;
							m_groupOptions.Controls.Add(m_linkOpt);
							m_linkOpt.Location = m_locSubCtrl;
							break;
						case CrossReferenceTags.kClassId:
						case ReminderTags.kClassId:
							throw new NotImplementedException(LexTextControls.ksUnimplementedField);
						default:
							int clidBase = clidDst;
							while (clidBase != 0 && clidBase != CmPossibilityTags.kClassId)
								clidBase = m_mdc.GetBaseClsId(clidBase);
							if (clidBase == CmPossibilityTags.kClassId)
							{
								m_groupOptions.Text = LexTextControls.ksListRefImportOptions;
								m_groupOptions.Controls.Add(m_listOpt);
								m_listOpt.Location = m_locSubCtrl;
								m_listOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_stylesheet, m_rsfm, cpt);
								break;
							}
							throw new ArgumentException(LexTextControls.ksInvalidField);
					}
					break;
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.OwningSequence:
					clidDst = m_mdc.GetDstClsId(m_rsfm.m_flid);
					switch (clidDst)
					{
						case StTextTags.kClassId:
							Debug.Assert(cpt == CellarPropertyType.OwningAtomic);
							m_groupOptions.Text = LexTextControls.ksTextImportOptions;
							m_groupOptions.Controls.Add(m_textOpt);
							m_textOpt.Location = m_locSubCtrl;
							m_textOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_stylesheet, m_rsfm);
							break;
						case RnRoledParticTags.kClassId:
							m_groupOptions.Text = LexTextControls.ksListRefImportOptions;
							m_groupOptions.Controls.Add(m_listOpt);
							m_listOpt.Location = m_locSubCtrl;
							m_listOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_stylesheet, m_rsfm, cpt);
							break;
						case RnGenericRecTags.kClassId:
							throw new NotImplementedException(LexTextControls.ksUnimplementedField);
						default:
							throw new ArgumentException(LexTextControls.ksInvalidField);
					}
					break;
				case CellarPropertyType.MultiBigString:
				case CellarPropertyType.MultiBigUnicode:
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
					m_groupOptions.Text = LexTextControls.ksMultiStringImportOptions;
					m_groupOptions.Controls.Add(m_stringOpt);
					m_stringOpt.Location = m_locSubCtrl;
					m_stringOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_stylesheet, m_rsfm);
					break;
				case CellarPropertyType.String:
				case CellarPropertyType.BigString:
					m_groupOptions.Text = LexTextControls.ksStringImportOptions;
					m_groupOptions.Controls.Add(m_stringOpt);
					m_stringOpt.Location = m_locSubCtrl;
					m_stringOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_stylesheet, m_rsfm);
					break;
				case CellarPropertyType.GenDate:
					m_groupOptions.Text = LexTextControls.ksGenDateImportOptions;
					m_groupOptions.Controls.Add(m_dateOpt);
					m_dateOpt.Location = m_locSubCtrl;
					m_dateOpt.Initialize(m_cache, m_helpTopicProvider, m_rsfm, true);
					break;
				case CellarPropertyType.Time:
					m_groupOptions.Text = LexTextControls.ksDateTimeImportOptions;
					m_groupOptions.Controls.Add(m_dateOpt);
					m_dateOpt.Location = m_locSubCtrl;
					m_dateOpt.Initialize(m_cache, m_helpTopicProvider, m_rsfm, false);
					break;
				case CellarPropertyType.Unicode:
				case CellarPropertyType.BigUnicode:
				case CellarPropertyType.Binary:
				case CellarPropertyType.Image:
				case CellarPropertyType.Boolean:
				case CellarPropertyType.Float:
				case CellarPropertyType.Guid:
				case CellarPropertyType.Integer:
				case CellarPropertyType.Numeric:
					throw new ArgumentException(LexTextControls.ksInvalidField);
			}
		}

		private void m_cbDestination_SelectedIndexChanged(object sender, EventArgs e)
		{
			DestinationField dest = m_cbDestination.SelectedItem as DestinationField;
			m_rsfm.m_flid = dest.Flid;
			m_rsfm.m_sName = dest.Name;
			SetSubControl();
		}

		private void m_btnAddCustom_Click(object sender, EventArgs e)
		{
			// What we'd like to do is the following bit of code, but we can't due to
			// circular dependencies that would be introduced.  We could possibly move
			// the dialog to another assembly/dll, but that would require reworking a
			// fair number of strings that have been converted to resources.
			//using (var dlg = new AddCustomFieldDlg(m_mediator, AddCustomFieldDlg.LocationType.Notebook))
			//    dlg.ShowDialog();
			System.Type typeFound;
			MethodInfo mi = XmlUtils.GetStaticMethod("xWorks.dll",
				"SIL.FieldWorks.XWorks.AddCustomFieldDlg",
				"ShowNotebookCustomFieldDlg",
				"AnthroFieldMappingDlg.m_btnAddCustom_Click()", out typeFound);
			if (mi != null)
			{
				var parameters = new object[1];
				parameters[0] = m_mediator;
				mi.Invoke(typeFound,
					System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
					System.Reflection.BindingFlags.NonPublic, null, parameters, null);
				// Now, clean up our map of possible field targets and reload the field combo list.
				List<int> delFields = new List<int>();
				foreach (int key in m_mapFlidName.Keys)
				{
					if (!m_mdc.FieldExists(key))
						delFields.Add(key);
				}
				foreach (int flid in delFields)
					m_mapFlidName.Remove(flid);
				foreach (int flid in m_mdc.GetFields(RnGenericRecTags.kClassId, false, (int)CellarPropertyTypeFilter.All))
				{
					if (m_mapFlidName.ContainsKey(flid))
						continue;
					if (m_mdc.IsCustom(flid))
					{
						string name = m_mdc.GetFieldName(flid);
						m_mapFlidName.Add(flid, name);
					}
				}
				FillInFieldList();
			}
			else
			{
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DestinationField dest =  m_cbDestination.SelectedItem as DestinationField;
			m_rsfm.m_flid = dest.Flid;
			m_rsfm.m_sName = dest.Name;
			Debug.Assert(m_groupOptions.Controls.Count == 1);
			Control ctrl = m_groupOptions.Controls[0];
			if (ctrl == m_listOpt)
			{
				m_rsfm.m_tlo.m_sEmptyDefault = m_listOpt.DefaultValue;
				m_rsfm.m_tlo.m_fHaveMulti = m_listOpt.HaveMultiple;
				m_rsfm.m_tlo.m_sDelimMulti = m_listOpt.DelimForMultiple;
				m_rsfm.m_tlo.m_fHaveSub = m_listOpt.HaveHierarchy;
				m_rsfm.m_tlo.m_sDelimSub = m_listOpt.DelimForHierarchy;
				m_rsfm.m_tlo.m_fHaveBetween = m_listOpt.HaveBetweenMarkers;
				m_rsfm.m_tlo.m_sMarkStart = m_listOpt.LeadingBetweenMarkers;
				m_rsfm.m_tlo.m_sMarkEnd = m_listOpt.TrailingBetweenMarkers;
				m_rsfm.m_tlo.m_fHaveBefore = m_listOpt.HaveCommentMarker;
				m_rsfm.m_tlo.m_sBefore = m_listOpt.CommentMarkers;
				m_rsfm.m_tlo.m_fIgnoreNewStuff = m_listOpt.DiscardNewStuff;
				m_rsfm.m_tlo.m_rgsMatch = m_listOpt.Matches;
				m_rsfm.m_tlo.m_rgsReplace = m_listOpt.Replacements;
				m_rsfm.m_tlo.m_wsId = m_textOpt.WritingSystem;
			}
			else if (ctrl == m_textOpt)
			{
				m_rsfm.m_txo.m_fStartParaNewLine = m_textOpt.ParaForEachLine;
				m_rsfm.m_txo.m_fStartParaBlankLine = m_textOpt.ParaAfterBlankLine;
				m_rsfm.m_txo.m_fStartParaIndented = m_textOpt.ParaWhenIndented;
				m_rsfm.m_txo.m_fStartParaShortLine = m_textOpt.ParaAfterShortLine;
				m_rsfm.m_txo.m_cchShortLim = m_textOpt.ShortLineLimit;
				m_rsfm.m_txo.m_sStyle = m_textOpt.Style;
				m_rsfm.m_txo.m_wsId = m_textOpt.WritingSystem;
			}
			else if (ctrl == m_stringOpt)
			{
				m_rsfm.m_sto.m_wsId = m_stringOpt.WritingSystem;
			}
			else if (ctrl == m_dateOpt)
			{
				m_rsfm.m_dto.m_rgsFmt = m_dateOpt.Formats;
			}
			else if (ctrl == m_linkOpt)
			{
				// No options to set, but preserve the flid and name.
			}
			else if (ctrl == m_discardOpt)
			{
				m_rsfm.m_flid = 0;
				m_rsfm.m_sName = LexTextControls.ksDoNotImport;
			}
			else
			{
				m_rsfm.m_flid = 0;
				m_rsfm.m_sName = LexTextControls.ksDoNotImport;
			}
			DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			SIL.FieldWorks.Common.FwUtils.ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportContMapSet");
		}

		private void m_lvContents_SizeChanged(object sender, EventArgs e)
		{
			columnHeader1.Width = m_lvContents.ClientSize.Width;
		}

		/// <summary>
		/// This provides the results after the dialog has closed.
		/// </summary>
		public NotebookImportWiz.RnSfMarker Results
		{
			get
			{
				NotebookImportWiz.RnSfMarker rsf = new NotebookImportWiz.RnSfMarker();
				rsf.m_flid = m_rsfm.m_flid;
				rsf.m_nLevel = m_rsfm.m_nLevel;
				rsf.m_sMkr = m_rsfm.m_sMkr;
				rsf.m_sMkrOverThis = m_rsfm.m_sMkrOverThis;
				rsf.m_sName = m_rsfm.m_sName;

				rsf.m_dto.m_rgsFmt = m_rsfm.m_dto.m_rgsFmt;

				rsf.m_sto.m_wsId = m_rsfm.m_sto.m_wsId;
				rsf.m_sto.m_ws = m_rsfm.m_sto.m_ws;

				rsf.m_txo.m_fStartParaBlankLine = m_rsfm.m_txo.m_fStartParaBlankLine;
				rsf.m_txo.m_fStartParaIndented = m_rsfm.m_txo.m_fStartParaIndented;
				rsf.m_txo.m_fStartParaNewLine = m_rsfm.m_txo.m_fStartParaNewLine;
				rsf.m_txo.m_fStartParaShortLine = m_rsfm.m_txo.m_fStartParaShortLine;
				rsf.m_txo.m_cchShortLim = m_rsfm.m_txo.m_cchShortLim;
				rsf.m_txo.m_sStyle = m_rsfm.m_txo.m_sStyle;
				rsf.m_txo.m_wsId = m_rsfm.m_txo.m_wsId;
				rsf.m_txo.m_ws = m_rsfm.m_txo.m_ws;

				rsf.m_tlo.m_fIgnoreNewStuff = m_rsfm.m_tlo.m_fIgnoreNewStuff;
				rsf.m_tlo.m_sEmptyDefault = m_rsfm.m_tlo.m_sEmptyDefault;
				rsf.m_tlo.m_default = m_rsfm.m_tlo.m_default;
				rsf.m_tlo.m_fHaveBefore = m_rsfm.m_tlo.m_fHaveBefore;
				rsf.m_tlo.m_sBefore = m_rsfm.m_tlo.m_sBefore;
				rsf.m_tlo.m_rgsBefore = m_rsfm.m_tlo.m_rgsBefore;
				rsf.m_tlo.m_fHaveBetween = m_rsfm.m_tlo.m_fHaveBetween;
				rsf.m_tlo.m_sMarkStart = m_rsfm.m_tlo.m_sMarkStart;
				rsf.m_tlo.m_sMarkEnd = m_rsfm.m_tlo.m_sMarkEnd;
				rsf.m_tlo.m_rgsMarkStart = m_rsfm.m_tlo.m_rgsMarkStart;
				rsf.m_tlo.m_rgsMarkEnd = m_rsfm.m_tlo.m_rgsMarkEnd;
				rsf.m_tlo.m_fHaveMulti = m_rsfm.m_tlo.m_fHaveMulti;
				rsf.m_tlo.m_sDelimMulti = m_rsfm.m_tlo.m_sDelimMulti;
				rsf.m_tlo.m_rgsDelimMulti = m_rsfm.m_tlo.m_rgsDelimMulti;
				rsf.m_tlo.m_fHaveSub = m_rsfm.m_tlo.m_fHaveSub;
				rsf.m_tlo.m_sDelimSub = m_rsfm.m_tlo.m_sDelimSub;
				rsf.m_tlo.m_rgsDelimSub = m_rsfm.m_tlo.m_rgsDelimSub;
				rsf.m_tlo.m_pnt = m_rsfm.m_tlo.m_pnt;
				rsf.m_tlo.m_rgsMatch = m_rsfm.m_tlo.m_rgsMatch;
				rsf.m_tlo.m_rgsReplace = m_rsfm.m_tlo.m_rgsReplace;
				rsf.m_tlo.m_wsId = m_rsfm.m_tlo.m_wsId;
				rsf.m_tlo.m_ws = m_rsfm.m_tlo.m_ws;

				return rsf;
			}
		}
	}
}