// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// View constructor for InnerLabeledMultiStringView.
	/// </summary>
	internal class LabeledMultiStringVc: FwBaseVc
	{
		internal int m_flid;
		internal List<CoreWritingSystemDefinition> m_rgws; // writing systems to display
		ITsTextProps m_ttpLabel; // Props to use for ws name labels
		bool m_editable = true;
		int m_wsEn;
		internal int m_mDxmpLabelWidth;

		public LabeledMultiStringVc(int flid, List<CoreWritingSystemDefinition> rgws, int wsUser, bool editable, int wsEn)
		{
			Reuse(flid, rgws, editable);
			m_ttpLabel = WritingSystemServices.AbbreviationTextProperties;
			m_wsEn = wsEn == 0 ? wsUser : wsEn;
			// Here's the C++ code which does the same thing using styles.
			//				StrUni stuLangCodeStyle(L"Language Code");
			//				ITsPropsFactoryPtr qtpf;
			//				qtpf.CreateInstance(CLSID_TsPropsFactory);
			//				StrUni stu;
			//				ITsStringPtr qtss;
			//				ITsStrFactoryPtr qtsf;
			//				qtsf.CreateInstance(CLSID_TsStrFactory);
			//				// Get the properties of the "Language Code" style for the writing system
			//				// which corresponds to the user's environment.
			//				qtpf->MakeProps(stuLangCodeStyle.Bstr(), ???->UserWs(), 0, &qttp);
		}

		public virtual string TextStyle
		{
			get
			{

				return "Default Paragraph Characters";
			}
			set
			{
				/*m_textStyle = value;*/
			}
		}

		public void Reuse(int flid, List<CoreWritingSystemDefinition> rgws, bool editable)
		{
			m_flid = flid;
			m_rgws = rgws;
			m_editable = editable;
		}

		private ITsString NameOfWs(int i)
		{
			// Display in English if possible for now (August 2008).  See LT-8631 and LT-8574.
			var result = m_rgws[i].Abbreviation;

			if (string.IsNullOrEmpty(result))
			{
				result = "??";
			}

			return TsStringUtils.MakeString(result, m_wsEn);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			TriggerDisplay(vwenv);

			// We use a table to display
			// encodings in column one and the strings in column two.
			// The table uses 100% of the available width.
			VwLength vlTable;
			vlTable.nVal = 10000;
			vlTable.unit = VwUnit.kunPercent100;

			// The width of the writing system column is determined from the width of the
			// longest one which will be displayed.
			m_mDxmpLabelWidth = 0;
			for (var i = 0; i < m_rgws.Count; ++i)
			{
				int dxs;	// Width of displayed string.
				int dys;	// Height of displayed string (not used here).
				// Set qtss to a string representing the writing system.
				vwenv.get_StringWidth(NameOfWs(i), m_ttpLabel, out dxs, out dys);
				m_mDxmpLabelWidth = Math.Max(m_mDxmpLabelWidth, dxs);
			}
			VwLength vlColWs; // 5-pt space plus max label width.
			vlColWs.nVal = m_mDxmpLabelWidth + 5000;
			vlColWs.unit = VwUnit.kunPoint1000;

			// Enhance JohnT: possibly allow for right-to-left UI by reversing columns?

			// The Main column is relative and uses the rest of the space.
			VwLength vlColMain;
			vlColMain.nVal = 1;
			vlColMain.unit = VwUnit.kunRelative;

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
			var visibleWss = new HashSet<ILgWritingSystem>();
			// if we passed in a view and have WritingSystemsToDisplay
			// then we'll load that list in order to filter our larger m_rgws list.
			AddViewWritingSystems(visibleWss);
			for (var i = 0; i < m_rgws.Count; ++i)
			{
				if (SkipEmptyWritingSystem(visibleWss, i, hvo))
				{
					continue;
				}
				vwenv.OpenTableRow();

				// First cell has writing system abbreviation displayed using m_ttpLabel.
				vwenv.Props = m_ttpLabel;
				vwenv.OpenTableCell(1,1);
				vwenv.AddString(NameOfWs(i));
				vwenv.CloseTableCell();

				// Second cell has the string contents for the alternative.
				// DN version has some property setting, including trailing margin and
				// RTL.
				if (m_rgws[i].RightToLeftScript)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
					vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalTrailing);
				}
				if (!m_editable)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
				}
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, 2000);
				vwenv.OpenTableCell(1,1);
				var wsdef = m_rgws[i];
				if (wsdef != null && wsdef.IsVoice)
				{
					// We embed it in a conc paragraph to ensure it never takes more than a line.
					// It will typically be covered up by a sound control.
					// Also set foreground color to match the window, so nothing shows even if the sound doesn't overlap it perfectly.
					// (transparent does not seem to work as a foreground color)
					vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Window)));
					// Must not spell-check a conc para, leads to layout failures when the paragraph tries to cast the source to
					// a conc text source, if it is overridden by a spelling text source.
					vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);
					vwenv.OpenConcPara(0, 1, VwConcParaOpts.kcpoDefault, 0);
					vwenv.AddStringAltMember(m_flid, m_rgws[i].Handle, this);
					vwenv.CloseParagraph();
				}
				else
				{
					if (!string.IsNullOrEmpty(TextStyle))
					{
						vwenv.set_StringProperty((int) FwTextPropType.ktptNamedStyle, TextStyle);

					}
					vwenv.AddStringAltMember(m_flid, m_rgws[i].Handle, this);
				}
				vwenv.CloseTableCell();

				vwenv.CloseTableRow();
			}
			vwenv.CloseTableBody();

			vwenv.CloseTable();
		}

		/// <summary>
		/// Subclass with InnerLabeledMultiStringView tests for empty alternatives and returns true to skip them.
		/// </summary>
		internal virtual bool SkipEmptyWritingSystem(ISet<ILgWritingSystem> visibleWss, int i, int hvo)
		{
			return false;
		}

		/// <summary>
		/// Subclass with LabelledMultiStringView gets extra WSS to display from it.
		/// </summary>
		internal virtual void AddViewWritingSystems(ISet<ILgWritingSystem> visibleWss)
		{
		}

		/// <summary>
		/// Subclass with LabelledMultiStringView calls TriggerView
		/// </summary>
		internal virtual void TriggerDisplay(IVwEnv vwenv)
		{
		}
	}
}