// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class StringSliceVc : FwBaseVc
	{
		private IPublisher m_publisher;
		int m_flid;
		private bool m_fMultilingual;
		int m_wsEnOrDefaultUserWs;

		public StringSliceVc()
		{
		}

		/// <summary>
		/// Create one that is NOT multilingual.
		/// </summary>
		public StringSliceVc(int flid, LcmCache cache, IPublisher publisher)
		{
			m_flid = flid;
			// ReSharper disable DoNotCallOverridableMethodsInConstructor
			Cache = cache;
			// ReSharper restore DoNotCallOverridableMethodsInConstructor
			m_publisher = publisher;
			m_wsEnOrDefaultUserWs = cache.WritingSystemFactory.GetWsFromStr("en");
			if (m_wsEnOrDefaultUserWs == 0)
			{
				m_wsEnOrDefaultUserWs = cache.DefaultUserWs;
			}
		}

		/// <summary>
		/// Create one that IS multilingual.
		/// </summary>
		public StringSliceVc(int flid, int ws, LcmCache cache, IPublisher publisher)
			: this(flid, cache, publisher)
		{
			m_wsDefault = ws;
			m_fMultilingual = true;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			if (m_fMultilingual)
			{
				SetParaRtlIfNeeded(vwenv, m_wsDefault);
				vwenv.AddStringAltMember(m_flid, m_wsDefault, this);
			}
			else
			{
				// Set the underlying paragraph to RTL if the first writing system in the
				// string is RTL.
				if (m_cache != null)
				{
					var tss = m_cache.DomainDataByFlid.get_StringProp(hvo, m_flid);
					var ttp = tss.get_Properties(0);
					int dummy;
					var ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
					if (ws == 0)
					{
						ws = m_wsDefault;
					}

					if (ws == 0)
					{
						ws = m_cache.DefaultAnalWs;
					}
					if (ws != 0)
					{
						SetParaRtlIfNeeded(vwenv, ws);
						if (ShowWsLabel)
						{
							DisplayWithWritingSystemLabel(vwenv, ws);
							return;
						}
					}
				}
				vwenv.AddStringProp(m_flid, this);
			}
		}

		private void DisplayWithWritingSystemLabel(IVwEnv vwenv, int ws)
		{
			var tssLabel = NameOfWs(ws);
			// We use a table to display
			// encodings in column one and the strings in column two.
			// The table uses 100% of the available width.
			VwLength vlTable;
			vlTable.nVal = 10000;
			vlTable.unit = VwUnit.kunPercent100;

			int dxs;    // Width of displayed string.
			int dys;    // Height of displayed string (not used here).
			vwenv.get_StringWidth(tssLabel, null, out dxs, out dys);
			VwLength vlColWs; // 5-pt space plus max label width.
			vlColWs.nVal = dxs + 5000;
			vlColWs.unit = VwUnit.kunPoint1000;

			// The Main column is relative and uses the rest of the space.
			VwLength vlColMain;
			vlColMain.nVal = 1;
			vlColMain.unit = VwUnit.kunRelative;

			// Enhance JohnT: possibly allow for right-to-left UI by reversing columns?
			vwenv.OpenTable(2, // Two columns.
				vlTable, // Table uses 100% of available width.
				0, // Border thickness.
				VwAlignment.kvaLeft, // Default alignment.
				VwFramePosition.kvfpVoid, // No border.
				VwRule.kvrlNone, // No rules between cells.
				0, // No forced space between cells.
				0, // No padding inside cells.
				false);
			// Specify column widths. The first argument is the number of columns,
			// not a column index. The writing system column only occurs at all if its
			// width is non-zero.
			vwenv.MakeColumns(1, vlColWs);
			vwenv.MakeColumns(1, vlColMain);

			vwenv.OpenTableBody();
			vwenv.OpenTableRow();

			// First cell has writing system abbreviation displayed using m_ttpLabel.
			//vwenv.Props = m_ttpLabel;
			vwenv.OpenTableCell(1, 1);
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			vwenv.AddString(tssLabel);
			vwenv.CloseTableCell();

			// Second cell has the string contents for the alternative.
			// DN version has some property setting, including trailing margin and RTL.
			if (m_fRtlScript)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalTrailing);
			}

			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, 2000);
			vwenv.OpenTableCell(1, 1);
			vwenv.AddStringProp(m_flid, this);
			vwenv.CloseTableCell();
			vwenv.CloseTableRow();
			vwenv.CloseTableBody();
			vwenv.CloseTable();
		}

		private ITsString NameOfWs(int ws)
		{
			MostRecentlyDisplayedWritingSystemHandle = ws;
			var sWs = m_cache.WritingSystemFactory.GetStrFromWs(ws);
			CoreWritingSystemDefinition wsys;
			WritingSystemServices.FindOrCreateWritingSystem(m_cache, FwDirectoryFinder.TemplateDirectory, sWs, false, false, out wsys);
			var result = wsys.Abbreviation;
			if (string.IsNullOrEmpty(result))
			{
				result = "??";
			}
			var tsb = TsStringUtils.MakeStrBldr();
			tsb.Replace(0, 0, result, WritingSystemServices.AbbreviationTextProperties);
			tsb.SetIntPropValues(0, tsb.Length, (int)FwTextPropType.ktptWs, 0, m_wsEnOrDefaultUserWs);
			return tsb.GetString();
		}

		bool m_fRtlScript;

		private void SetParaRtlIfNeeded(IVwEnv vwenv, int ws)
		{
			if (m_cache == null)
			{
				return;
			}
			var wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
			if (wsObj != null && wsObj.RightToLeftScript)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalTrailing);
				m_fRtlScript = true;
			}
			else
			{
				m_fRtlScript = false;
			}
		}

		/// <summary>
		/// Get/set flag whether to display writing system label even for monolingual string.
		/// </summary>
		public bool ShowWsLabel { get; set; }

		/// <summary>
		/// Get the ws for the most recently displayed writing system label.
		/// </summary>
		internal int MostRecentlyDisplayedWritingSystemHandle { get; private set; }

		/// <summary>
		/// We may have a link embedded here.
		/// </summary>
		public override void DoHotLinkAction(string strData, ISilDataAccess sda)
		{
			if (strData.Length > 0 && strData[0] == (int)FwObjDataTypes.kodtExternalPathName)
			{
				var url = strData.Substring(1); // may also be just a file name, launches default app.
				try
				{
					if (url.StartsWith(FwLinkArgs.kFwUrlPrefix))
					{
						LinkHandler.PublishFollowLinkMessage(m_publisher, new FwLinkArgs(url));
						return;
					}
				}
				catch
				{
					// REVIEW: Why are we catching all errors?
					// JohnT: one reason might be that the above will fail if the link is to another project.
					// Review: would we be better to use the default? That is now smart about
					// local links, albeit by a rather more awkward route because of dependency problems.
				}
			}
			base.DoHotLinkAction(strData, sda);
		}
	}
}