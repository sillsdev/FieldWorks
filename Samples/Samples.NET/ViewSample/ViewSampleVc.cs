/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: ViewSampleVc.cs
/// Responsibility: John Thomson
/// Last reviewed:
///
/// <remarks>
/// Implementation of the view constructor
/// </remarks>
/// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Samples.ViewSample
{
	/// <summary>
	/// Implementation of the ViewConstructor
	/// </summary>
	public class ViewSampleVc: VwBaseVc
	{
		// Tag constants for the document structure.
		public const int ktagBookSections = 101; // a book has sections
		public const int ktagBookName = 102; // and a name.
		public const int ktagSectionParas = 202; // a section is mainly made up of paragraphs
		public const int ktagSectionRefs = 203; // but also has references
		public const int ktagSectionTitle = 204; // and a title
		public const int ktagParaContents = 307; // a paragraph has contents (strings in multiple languages)
		public const int ktagParaBundles = 308; // and also bundles, for interlinear display
		public const int ktagBundleBase = 409; // A Bundle has a base form
		public const int ktagBundleIdiom = 410; // and an idiomatic back translation
		public const int ktagBundleLing = 411; // and a more 'linguistic' back translation

		//Fragment identifiers
		public const int kfrBook = 1001;
		public const int kfrSection = 1002;
		public const int kfrDoublePara = 1003;
		public const int kfrBundle = 1004;

		// Member variables
		int m_wsSrc; // source writing system
		int m_wsDst; // destination ws.
		int m_colorEditable = (int)RGB(Color.FromKnownColor(KnownColor.Window));
		ITsString m_tssLeftParen;
		ITsString m_tssRightParen;


		public int SourceWs
		{
			get { return m_wsSrc; }
			set
			{
				m_wsSrc = value;
				ITsStrFactory tsf = (ITsStrFactory) new FwKernelLib.TsStrFactoryClass();
				m_tssLeftParen = tsf.MakeString(" (", m_wsSrc);
				m_tssRightParen = tsf.MakeString(").", m_wsSrc);
			}
		}

		public int DestWs
		{
			get { return m_wsDst; }
			set { m_wsDst = value; }
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
			default:
				break;
			case kfrBook:
				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptFontSize, (int)FwKernelLib.FwTextPropVar.ktpvMilliPoint, 24000);
				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptBold, (int)FwKernelLib.FwTextPropVar.ktpvEnum,
					(int)FwKernelLib.FwTextToggleVal.kttvOn);
				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptAlign, (int)FwKernelLib.FwTextPropVar.ktpvEnum,
					(int)FwKernelLib.FwTextAlign.ktalCenter);

				vwenv.AddStringProp(ktagBookName, this);
				vwenv.AddLazyVecItems(ktagBookSections, this, kfrSection);
				break;
			case kfrSection:
				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptItalic, (int)FwKernelLib.FwTextPropVar.ktpvEnum,
					(int)FwKernelLib.FwTextToggleVal.kttvOn);
				vwenv.OpenParagraph();
				vwenv.AddStringProp(ktagSectionTitle, this);
				vwenv.AddString(m_tssLeftParen);
				vwenv.AddStringProp(ktagSectionRefs, this);
				vwenv.AddString(m_tssRightParen);
				vwenv.CloseParagraph();
				vwenv.AddLazyVecItems(ktagSectionParas, this, kfrDoublePara);
				break;
			case kfrDoublePara:
				AddDoublePara(vwenv, hvo);
				// Now insert an interlinear version of the paragraph. This is basically editable.
				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptBackColor,
					(int)FwKernelLib.FwTextPropVar.ktpvDefault, m_colorEditable);
				vwenv.OpenParagraph();
				vwenv.AddObjVecItems(ktagParaBundles, this, kfrBundle);
				vwenv.CloseParagraph();
				break;
			case kfrBundle:
				// Put a little space after each bundle to separate them.
				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptMarginTrailing,
					(int)FwKernelLib.FwTextPropVar.ktpvMilliPoint, 5000);
				vwenv.OpenInnerPile();
				vwenv.AddStringProp(ktagBundleBase, this);

				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptBold,
					(int)FwKernelLib.FwTextPropVar.ktpvEnum,
					(int)FwKernelLib.FwTextToggleVal.kttvOn);
				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptFontSize,
					(int)FwKernelLib.FwTextPropVar.ktpvMilliPoint, 13000);
				vwenv.AddStringProp(ktagBundleIdiom, this);

				vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptFontSize,
					(int)FwKernelLib.FwTextPropVar.ktpvMilliPoint, 8000);
				vwenv.AddStringProp(ktagBundleLing, this);
				vwenv.CloseInnerPile();

				break;
			}
		}

		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			switch (frag)
			{
			default:
				return 1000;
			case kfrBook:
				return 1000;
			case kfrSection:
				return 300;
			}
		}

		static public uint RGB(Color c)
		{
			return RGB(c.R, c.G, c.B);
		}

		/// <summary>
		/// Make a standard Win32 color from three components.
		/// </summary>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		static public uint RGB(int r, int g, int b)
		{
			return ((uint)(((byte)(r)|((short)((byte)(g))<<8))|(((short)(byte)(b))<<16)));

		}

		internal void AddDoublePara(IVwEnv vwenv, int hvo)
		{
			// We use a table to display the source WS in column 1, and the dest ws in column 2.
			FwViews.VwLength vlTable; // we use this to specify that the table takes 100% of the width.
			vlTable.nVal = 10000;
			vlTable.unit = FwViews.VwUnit.kunPercent100;

			FwViews.VwLength vlColumn; // and this one to specify half the width for each column
			vlColumn.nVal = 5000;
			vlColumn.unit = FwViews.VwUnit.kunPercent100;

			// Enhance JohnT: possibly allow for right-to-left UI by reversing columns?

			vwenv.OpenTable(2, // Two columns.
				ref vlTable, // Table uses 100% of available width.
				0, // Border thickness.
				FwViews.VwAlignment.kvaLeft, // Default alignment.
				FwViews.VwFramePosition.kvfpVoid, // No border.
				FwViews.VwRule.kvrlNone, // No rules between cells.
				3000, // Three points of space between cells.
				3000); // Three points padding inside cells.
			// Specify column widths. The first argument is the number of columns,
			// not a column index.
			vwenv.MakeColumns(2, vlColumn);

			vwenv.OpenTableBody();
			vwenv.OpenTableRow();

			// Source cell, not editable
			vwenv.OpenTableCell(1,1);
			vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptEditable, (int)FwKernelLib.FwTextPropVar.ktpvEnum,
				(int)FwKernelLib.TptEditable.ktptNotEditable);
			vwenv.AddStringAltMember(ktagParaContents, m_wsSrc, this);
			vwenv.CloseTableCell();

			// Dest cell, editable, therefore with white background.
			vwenv.set_IntProperty((int)FwKernelLib.FwTextPropType.ktptBackColor, (int)FwKernelLib.FwTextPropVar.ktpvDefault,
				m_colorEditable);
			vwenv.OpenTableCell(1,1);
			vwenv.AddStringAltMember(ktagParaContents, m_wsDst, this);
			vwenv.CloseTableCell();

			vwenv.CloseTableRow();
			vwenv.CloseTableBody();
			vwenv.CloseTable();
		}
	}
}
