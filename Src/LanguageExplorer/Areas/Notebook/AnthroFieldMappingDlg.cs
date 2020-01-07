// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary />
	public partial class AnthroFieldMappingDlg : Form
	{
		private LcmCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private IFwMetaDataCacheManaged m_mdc;
		private IVwStylesheet m_stylesheet;
		private RnSfMarker m_rsfm;
		private ListRefFieldOptions m_listOpt;
		private TextFieldOptions m_textOpt;
		private DateFieldOptions m_dateOpt;
		private StringFieldOptions m_stringOpt;
		private LinkFieldOptions m_linkOpt;
		private DiscardOptions m_discardOpt;
		private Dictionary<int, string> m_mapFlidName;
		private IPropertyTable m_propertyTable;
		private IPublisher m_publisher;
		private Point m_locSubCtrl = new Point(2, 20);
		private SfmFile m_sfmFile;
		private string m_sContentsGroupFmt;
		private string m_sContentsLabelFmt;

		/// <summary />
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
		}

		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app, RnSfMarker rsf, SfmFile sfmFile, Dictionary<int, string> mapFlidName, IVwStylesheet stylesheet,
			IPropertyTable propertyTable, IPublisher publisher)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_rsfm = rsf;
			m_sfmFile = sfmFile;
			m_stylesheet = stylesheet;
			m_mdc = cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_mapFlidName = mapFlidName;
			FillInFieldList();
			FillInContentsPane(rsf, sfmFile);
			SetSubControl();
		}

		private void FillInFieldList()
		{
			m_cbDestination.Items.Clear();
			m_cbDestination.Sorted = true;
			foreach (var flid in m_mapFlidName.Keys)
			{
				m_cbDestination.Items.Add(new DestinationField(flid, m_mapFlidName[flid]));
			}
			m_cbDestination.Sorted = false;
			m_cbDestination.Items.Insert(0, new DestinationField(0, LanguageExplorerControls.ksDoNotImport));
			var idx = m_cbDestination.Items.IndexOf(new DestinationField(m_rsfm.m_flid, m_rsfm.m_sName));
			if (idx > -1)
			{
				m_cbDestination.SelectedIndex = idx;
			}
		}

		private void FillInContentsPane(RnSfMarker rsf, SfmFile sfmFile)
		{
			m_groupContents.Text = string.Format(m_sContentsGroupFmt, rsf.m_sMkr);
			m_lvContents.Items.Clear();
			var setContents = new HashSet<string>();
			foreach (var field in m_sfmFile.Lines)
			{
				if (field.Marker == rsf.m_sMkr)
				{
					if (!setContents.Contains(field.Data))
					{
						setContents.Add(field.Data);
						var lvi = new ListViewItem($"\\{field.Marker} {(string.IsNullOrEmpty(field.Data) ? string.Empty : field.Data)}");
						m_lvContents.Items.Add(lvi);
					}
				}
			}
			m_lblContents.Text = string.Format(m_sContentsLabelFmt, rsf.m_sMkr, sfmFile.GetSFMCount(rsf.m_sMkr), m_lvContents.Items.Count);
		}

		private void SetSubControl()
		{
			m_groupOptions.Controls.Clear();
			if (m_rsfm.m_flid == 0)
			{
				m_groupOptions.Text = LanguageExplorerControls.ksDiscardedField;
				m_groupOptions.Controls.Add(m_discardOpt);
				m_discardOpt.Location = m_locSubCtrl;
				return;
			}
			var cpt = (CellarPropertyType)m_mdc.GetFieldType(m_rsfm.m_flid);
			int clidDst;
			switch (cpt)
			{
				case CellarPropertyType.ReferenceAtomic:
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					clidDst = m_mdc.GetDstClsId(m_rsfm.m_flid);
					switch (clidDst)
					{
						case RnGenericRecTags.kClassId:
							m_groupOptions.Text = NotebookResources.ksRnLinkFieldOptions;
							m_groupOptions.Controls.Add(m_linkOpt);
							m_linkOpt.Location = m_locSubCtrl;
							break;
						case CrossReferenceTags.kClassId:
						case ReminderTags.kClassId:
							throw new NotSupportedException(LanguageExplorerControls.ksUnimplementedField);
						default:
							var clidBase = clidDst;
							while (clidBase != 0 && clidBase != CmPossibilityTags.kClassId)
							{
								clidBase = m_mdc.GetBaseClsId(clidBase);
							}
							if (clidBase == CmPossibilityTags.kClassId)
							{
								m_groupOptions.Text = LanguageExplorerControls.ksListRefImportOptions;
								m_groupOptions.Controls.Add(m_listOpt);
								m_listOpt.Location = m_locSubCtrl;
								m_listOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_rsfm, cpt);
								break;
							}
							throw new ArgumentException(LanguageExplorerControls.ksInvalidField);
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
							m_groupOptions.Text = LanguageExplorerControls.ksTextImportOptions;
							m_groupOptions.Controls.Add(m_textOpt);
							m_textOpt.Location = m_locSubCtrl;
							m_textOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_stylesheet, m_rsfm);
							break;
						case RnRoledParticTags.kClassId:
							m_groupOptions.Text = LanguageExplorerControls.ksListRefImportOptions;
							m_groupOptions.Controls.Add(m_listOpt);
							m_listOpt.Location = m_locSubCtrl;
							m_listOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_rsfm, cpt);
							break;
						case RnGenericRecTags.kClassId:
							throw new NotSupportedException(LanguageExplorerControls.ksUnimplementedField);
						default:
							throw new ArgumentException(LanguageExplorerControls.ksInvalidField);
					}
					break;
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
					m_groupOptions.Text = LanguageExplorerControls.ksMultiStringImportOptions;
					m_groupOptions.Controls.Add(m_stringOpt);
					m_stringOpt.Location = m_locSubCtrl;
					m_stringOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_rsfm);
					break;
				case CellarPropertyType.String:
					m_groupOptions.Text = LanguageExplorerControls.ksStringImportOptions;
					m_groupOptions.Controls.Add(m_stringOpt);
					m_stringOpt.Location = m_locSubCtrl;
					m_stringOpt.Initialize(m_cache, m_helpTopicProvider, m_app, m_rsfm);
					break;
				case CellarPropertyType.GenDate:
					m_groupOptions.Text = LanguageExplorerControls.ksGenDateImportOptions;
					m_groupOptions.Controls.Add(m_dateOpt);
					m_dateOpt.Location = m_locSubCtrl;
					m_dateOpt.Initialize(m_cache, m_helpTopicProvider, m_rsfm, true);
					break;
				case CellarPropertyType.Time:
					m_groupOptions.Text = LanguageExplorerControls.ksDateTimeImportOptions;
					m_groupOptions.Controls.Add(m_dateOpt);
					m_dateOpt.Location = m_locSubCtrl;
					m_dateOpt.Initialize(m_cache, m_helpTopicProvider, m_rsfm, false);
					break;
				case CellarPropertyType.Unicode:
				case CellarPropertyType.Binary:
				case CellarPropertyType.Image:
				case CellarPropertyType.Boolean:
				case CellarPropertyType.Float:
				case CellarPropertyType.Guid:
				case CellarPropertyType.Integer:
				case CellarPropertyType.Numeric:
					throw new ArgumentException(LanguageExplorerControls.ksInvalidField);
			}
		}

		private void m_cbDestination_SelectedIndexChanged(object sender, EventArgs e)
		{
			var dest = m_cbDestination.SelectedItem as DestinationField;
			m_rsfm.m_flid = dest.Flid;
			m_rsfm.m_sName = dest.Name;
			SetSubControl();
		}

		private void m_btnAddCustom_Click(object sender, EventArgs e)
		{
			using (var dlg = new AddCustomFieldDlg(m_propertyTable, m_publisher, CustomFieldLocationType.Notebook))
			{
				if (dlg.ShowCustomFieldWarning(m_propertyTable.GetValue<Form>(FwUtils.window)))
				{
					dlg.ShowDialog();
				}
				// Now, clean up our map of possible field targets and reload the field combo list.
				var delFields = m_mapFlidName.Keys.Where(key => !m_mdc.FieldExists(key)).ToList();
				foreach (var flid in delFields)
				{
					m_mapFlidName.Remove(flid);
				}
				foreach (var flid in m_mdc.GetFields(RnGenericRecTags.kClassId, false, (int)CellarPropertyTypeFilter.All))
				{
					if (m_mapFlidName.ContainsKey(flid) || !m_mdc.IsCustom(flid))
					{
						continue;
					}
					var name = m_mdc.GetFieldName(flid);
					m_mapFlidName.Add(flid, name);
				}
				FillInFieldList();
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			var dest = m_cbDestination.SelectedItem as DestinationField;
			m_rsfm.m_flid = dest.Flid;
			m_rsfm.m_sName = dest.Name;
			Debug.Assert(m_groupOptions.Controls.Count == 1);
			var ctrl = m_groupOptions.Controls[0];
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
				m_rsfm.m_sName = LanguageExplorerControls.ksDoNotImport;
			}
			else
			{
				m_rsfm.m_flid = 0;
				m_rsfm.m_sName = LanguageExplorerControls.ksDoNotImport;
			}
			DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpDataNotebookImportContMapSet");
		}

		private void m_lvContents_SizeChanged(object sender, EventArgs e)
		{
			columnHeader1.Width = m_lvContents.ClientSize.Width;
		}

		/// <summary>
		/// This provides the results after the dialog has closed.
		/// </summary>
		internal RnSfMarker Results => new RnSfMarker
		{
			m_flid = m_rsfm.m_flid,
			m_nLevel = m_rsfm.m_nLevel,
			m_sMkr = m_rsfm.m_sMkr,
			m_sMkrOverThis = m_rsfm.m_sMkrOverThis,
			m_sName = m_rsfm.m_sName,
			m_dto = { m_rgsFmt = m_rsfm.m_dto.m_rgsFmt },
			m_sto =
			{
				m_wsId = m_rsfm.m_sto.m_wsId,
				m_ws = m_rsfm.m_sto.m_ws
			},
			m_txo =
			{
				m_fStartParaBlankLine = m_rsfm.m_txo.m_fStartParaBlankLine,
				m_fStartParaIndented = m_rsfm.m_txo.m_fStartParaIndented,
				m_fStartParaNewLine = m_rsfm.m_txo.m_fStartParaNewLine,
				m_fStartParaShortLine = m_rsfm.m_txo.m_fStartParaShortLine,
				m_cchShortLim = m_rsfm.m_txo.m_cchShortLim,
				m_sStyle = m_rsfm.m_txo.m_sStyle,
				m_wsId = m_rsfm.m_txo.m_wsId,
				m_ws = m_rsfm.m_txo.m_ws
			},
			m_tlo =
			{
				m_fIgnoreNewStuff = m_rsfm.m_tlo.m_fIgnoreNewStuff,
				m_sEmptyDefault = m_rsfm.m_tlo.m_sEmptyDefault,
				m_default = m_rsfm.m_tlo.m_default,
				m_fHaveBefore = m_rsfm.m_tlo.m_fHaveBefore,
				m_sBefore = m_rsfm.m_tlo.m_sBefore,
				m_rgsBefore = m_rsfm.m_tlo.m_rgsBefore,
				m_fHaveBetween = m_rsfm.m_tlo.m_fHaveBetween,
				m_sMarkStart = m_rsfm.m_tlo.m_sMarkStart,
				m_sMarkEnd = m_rsfm.m_tlo.m_sMarkEnd,
				m_rgsMarkStart = m_rsfm.m_tlo.m_rgsMarkStart,
				m_rgsMarkEnd = m_rsfm.m_tlo.m_rgsMarkEnd,
				m_fHaveMulti = m_rsfm.m_tlo.m_fHaveMulti,
				m_sDelimMulti = m_rsfm.m_tlo.m_sDelimMulti,
				m_rgsDelimMulti = m_rsfm.m_tlo.m_rgsDelimMulti,
				m_fHaveSub = m_rsfm.m_tlo.m_fHaveSub,
				m_sDelimSub = m_rsfm.m_tlo.m_sDelimSub,
				m_rgsDelimSub = m_rsfm.m_tlo.m_rgsDelimSub,
				m_pnt = m_rsfm.m_tlo.m_pnt,
				m_rgsMatch = m_rsfm.m_tlo.m_rgsMatch,
				m_rgsReplace = m_rsfm.m_tlo.m_rgsReplace,
				m_wsId = m_rsfm.m_tlo.m_wsId,
				m_ws = m_rsfm.m_tlo.m_ws
			}
		};

		private sealed class DestinationField
		{
			internal DestinationField(int flid, string name)
			{
				Flid = flid;
				Name = name;
			}

			internal int Flid { get; }

			internal string Name { get; }

			public override string ToString()
			{
				return Name;
			}

			public override bool Equals(object obj)
			{
				var that = obj as DestinationField;
				if (that == null)
				{
					return false;
				}
				return Flid == that.Flid && Name == that.Name;
			}

			public override int GetHashCode()
			{
				return Flid.GetHashCode() + Name.GetHashCode();
			}
		}
	}
}