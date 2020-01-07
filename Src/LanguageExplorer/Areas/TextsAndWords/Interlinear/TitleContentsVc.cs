// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Vc for title contents pane. Makes a table.
	/// </summary>
	internal class TitleContentsVc : FwBaseVc
	{
		public const int kfragRoot = 6789;
		private ITsString m_tssTitle;
		private int m_vtagStTextTitle;
		private int m_dxLabWidth;
		private int m_dxWsLabWidth; // width of writing system labels.
		private ITsTextProps m_ttpBold;
		private ITsTextProps m_ttpDataCellProps;
		private CoreWritingSystemDefinition[] m_writingSystems;
		private ITsString[] m_WsLabels;
		private ITsTextProps m_ttpWsLabel;
		private int m_editBackColor = (int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Window));

		public TitleContentsVc(LcmCache cache)
		{
			var wsUser = cache.DefaultUserWs;
			m_tssTitle = TsStringUtils.MakeString(ITextStrings.ksTitle, wsUser);
			var tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			m_ttpBold = tpb.GetTextProps();
			tpb = TsStringUtils.MakePropsBldr();
			// Set some padding all around.
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, 3000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 3000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, 11000); // 10000 clips right border.
			m_ttpDataCellProps = tpb.GetTextProps();
			m_vtagStTextTitle = cache.MetaDataCacheAccessor.GetFieldId("StText", "Title", false);
			// Set up the array of writing systems we will display for title.
			SetupWritingSystemsForTitle(cache);
		}

		internal void SetupWritingSystemsForTitle(LcmCache cache)
		{
			m_ttpWsLabel = WritingSystemServices.AbbreviationTextProperties;
			m_writingSystems = new CoreWritingSystemDefinition[2];
			m_writingSystems[0] = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			m_writingSystems[1] = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			m_WsLabels = new ITsString[m_writingSystems.Length];
			for (var i = 0; i < m_writingSystems.Length; i++)
			{
				// For now (August 2008), try English abbreviation before UI writing system.
				// (See LT-8185.)
				m_WsLabels[i] = TsStringUtils.MakeString(m_writingSystems[i].Abbreviation, cache.DefaultUserWs);
				if (string.IsNullOrEmpty(m_WsLabels[i].Text))
				{
					m_WsLabels[i] = TsStringUtils.MakeString(m_writingSystems[i].Abbreviation, cache.DefaultUserWs);
				}
			}
		}

		/// <summary>
		/// Indicates whether to treat the RootSite object as a scripture.
		/// </summary>
		internal bool IsScripture { get; set; }

		/// <summary>
		/// Indicates whether we should allow editing in the TitleContents fields.
		/// </summary>
		internal bool Editable { get; set; } = true;

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// Ignore 0 hvo's. RootObject may have not been set. FWNX-613.
			if (hvo == 0)
			{
				return;
			}
			switch (frag)
			{
				case kfragRoot:
					if (m_dxLabWidth == 0)
					{
						int dmpx1, dmpy;    //, dmpx2;
						vwenv.get_StringWidth(m_tssTitle, m_ttpBold, out dmpx1, out dmpy);
						m_dxLabWidth = dmpx1 + 13000; // add 3 pt spacing to box, 10 to margin.
						m_dxWsLabWidth = 0;
						foreach (var tssLabel in m_WsLabels)
						{
							vwenv.get_StringWidth(tssLabel, m_ttpWsLabel, out dmpx1, out dmpy);
							m_dxWsLabWidth = Math.Max(m_dxWsLabWidth, dmpx1);
						}
						m_dxWsLabWidth += 18000; // 3 pts white space each side, 11 margin, 1 border, plus 1 for safety.
					}
					VwLength vlTable;
					vlTable.nVal = 10000;
					vlTable.unit = VwUnit.kunPercent100;

					VwLength vlColLabels; // 5-pt space plus max label width.
					vlColLabels.nVal = m_dxLabWidth;
					vlColLabels.unit = VwUnit.kunPoint1000;

					VwLength vlColWsLabels; // 5-pt space plus max ws label width.
					vlColWsLabels.nVal = m_dxWsLabWidth;
					vlColWsLabels.unit = VwUnit.kunPoint1000;

					// The Main column is relative and uses the rest of the space.
					VwLength vlColMain;
					vlColMain.nVal = 1;
					vlColMain.unit = VwUnit.kunRelative;

					// The Padding column allows for the the trailing margin and border.
					VwLength vlColPadding;
					vlColPadding.nVal = 10000;
					vlColPadding.unit = VwUnit.kunPoint1000;

					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop, (int)FwTextPropVar.ktpvMilliPoint, 5000);
					vwenv.OpenTable(4, // Four columns.
						vlTable, // Table uses 100% of available width.
						0, // Border thickness.
						VwAlignment.kvaLeft, // Default alignment.
						VwFramePosition.kvfpVoid, // No border.
						VwRule.kvrlNone, // No rules between cells.
						0, // No forced space between cells.
						0, // no padding inside cells.
						false);
					vwenv.MakeColumns(1, vlColLabels);
					vwenv.MakeColumns(1, vlColWsLabels);
					vwenv.MakeColumns(1, vlColMain);
					vwenv.MakeColumns(1, vlColPadding);
					vwenv.OpenTableBody();
					for (var i = 0; i < m_writingSystems.Length; i++)
					{
						vwenv.OpenTableRow();
						// First cell has 'Title' label in bold.
						vwenv.Props = m_ttpBold;
						vwenv.OpenTableCell(1, 1);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, 10000);
						if (i == 0) // only on the first row
						{
							// We want this fixed at 10 point, since it's considered a UI component, not data.
							// See LT-4816
							vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 10000);
							vwenv.AddString(m_tssTitle);
						}
						vwenv.CloseTableCell();
						// Second cell has ws labels.
						vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, m_editBackColor);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, 1000);
						if (i == 0)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
						}
						if (i == m_writingSystems.Length - 1)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
						}
						vwenv.OpenTableCell(1, 1);
						vwenv.Props = m_ttpDataCellProps;

						vwenv.Props = m_ttpWsLabel;
						vwenv.AddString(m_WsLabels[i]);
						vwenv.CloseTableCell();

						// Third cell has the Title property, in a box.
						vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, m_editBackColor);
						// Set the underlying directionality so that arrow keys work properly.
						var fRTL = m_writingSystems[i].RightToLeftScript;
						vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, fRTL ? -1 : 0);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, fRTL ? (int)FwTextAlign.ktalRight : (int)FwTextAlign.ktalLeft);
						if (i == 0)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 1000);
						}
						if (i == m_writingSystems.Length - 1)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
						}
						vwenv.OpenTableCell(1, 1);
						vwenv.OpenParagraph();
						vwenv.Props = m_ttpDataCellProps;
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, Editable ? (int)TptEditable.ktptIsEditable : (int)TptEditable.ktptNotEditable);
						vwenv.AddStringAltMember(IsScripture ? m_vtagStTextTitle : CmMajorObjectTags.kflidName, m_writingSystems[i].Handle, this);
						vwenv.CloseParagraph();
						vwenv.CloseTableCell();

						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, 10000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1000);
						vwenv.OpenTableCell(1, 1);
						vwenv.CloseTableCell();

						vwenv.CloseTableRow();
					}
					vwenv.CloseTableBody();
					vwenv.CloseTable();
					break;
				default:
					throw new Exception("Bad frag id in TitleContentsVc");
			}
		}
	}
}