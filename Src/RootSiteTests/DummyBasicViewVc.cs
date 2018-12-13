// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using FieldWorks.TestUtilities;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// The class that displays the draft view.
	/// </summary>
	public class DummyBasicViewVc : FwBaseVc
	{
		private readonly DisplayType m_displayType;
		private readonly int m_flid;
		private int m_counter = 1;

		/// <summary />
		public DummyBasicViewVc() : this(DisplayType.kAll, kflidTestDummy)
		{
		}

		/// <summary />
		public DummyBasicViewVc(DisplayType display, int flid)
		{
			m_displayType = display;
			m_flid = flid;
		}

		/// <summary />
		public const int kflidTestDummy = 999;
		/// <summary />
		public const int kMarginTop = 60000;
		/// <summary />
		public const int kdzmpInch = 72000;
		/// <summary />
		public const int kEstimatedParaHeight = 30;

		#region Overridden methods
		/// <summary />
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			if ((m_displayType & DisplayType.kLiteralStringLabels) != 0)
			{
				vwenv.AddString(TsStringUtils.MakeString("Label" + m_counter++, m_wsDefault));
			}
			switch (frag)
			{
				case 1: // the root; display the subitems, first using non-lazy view, then lazy one.
					if ((m_displayType & DisplayType.kFootnoteDetailsSeparateParas) == DisplayType.kFootnoteDetailsSeparateParas)
					{
						vwenv.AddObjVecItems(m_flid, this, 10);
					}
					if ((m_displayType & DisplayType.kFootnoteDetailsSeparateParas) == DisplayType.kFootnoteDetailsSeparateParas)
					{
						vwenv.AddObjVecItems(m_flid, this, 11);
					}
					else
					{
						if ((m_displayType & DisplayType.kNormal) == DisplayType.kNormal)
						{
							vwenv.AddObjVecItems(m_flid, this, 3);
						}
						if ((m_displayType & DisplayType.kLazy) == DisplayType.kLazy)
						{
							vwenv.AddObjVecItems(m_flid, this, 2);
						}
					}
					if ((m_displayType & DisplayType.kBookTitle) == DisplayType.kBookTitle)
					{
						vwenv.AddObjProp(ScrBookTags.kflidTitle, this, 3);
					}
					if (m_displayType == DisplayType.kOuterObjDetails)
					{
						vwenv.AddObjVecItems(m_flid, this, 6);
					}
					break;
				case 2: // An StText, display paragraphs lazily
					if ((m_displayType & DisplayType.kWithTopMargin) == DisplayType.kWithTopMargin)
					{
						vwenv.AddLazyVecItems(StTextTags.kflidParagraphs, this, 4);
					}
					vwenv.AddLazyVecItems(StTextTags.kflidParagraphs, this, 5);
					break;
				case 3: // An StText, display paragraphs not lazily.
					if ((m_displayType & DisplayType.kWithTopMargin) == DisplayType.kWithTopMargin)
					{
						vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, 4);
					}
					vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, 5);
					if ((m_displayType & DisplayType.kDuplicateParagraphs) != 0)
					{
						vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, 5);
					}
					break;
				case 4: // StTxtPara, display contents with top margin
					if ((m_displayType & DisplayType.kMappedPara) == DisplayType.kMappedPara)
					{
						vwenv.OpenMappedPara();
					}
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop, (int)FwTextPropVar.ktpvMilliPoint, kMarginTop);
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					if ((m_displayType & DisplayType.kMappedPara) == DisplayType.kMappedPara)
					{
						vwenv.CloseParagraph();
					}
					break;
				case 5: // StTxtPara, display contents without top margin
					if ((m_displayType & DisplayType.kMappedPara) == DisplayType.kMappedPara)
					{
						vwenv.OpenMappedPara();
					}
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					if ((m_displayType & DisplayType.kMappedPara) == DisplayType.kMappedPara)
					{
						vwenv.CloseParagraph();
					}
					break;
				case 6: // StTxtPara, display details of our outer object
					int hvoOuter, tag, ihvo;
					vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
					var tss = TsStringUtils.MakeString("Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo, m_wsDefault);
					vwenv.AddString(tss);
					break;
				case ScrBookTags.kflidSections:
					vwenv.AddObjVecItems(ScrBookTags.kflidSections, this, ScrSectionTags.kflidContent);
					break;
				case ScrSectionTags.kflidHeading:
				case ScrSectionTags.kflidContent:
					if ((m_displayType & DisplayType.kNormal) == DisplayType.kNormal)
					{
						vwenv.AddObjProp(frag, this, 3);
					}
					if ((m_displayType & DisplayType.kLazy) == DisplayType.kLazy)
					{
						vwenv.AddObjProp(frag, this, 2);
					}
					break;
				case 7: // ScrBook
					vwenv.OpenDiv();
					vwenv.AddObjVecItems(ScrBookTags.kflidFootnotes, this, 8);
					vwenv.CloseDiv();
					break;
				case 8: // StFootnote
					vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, 9);
					break;
				case 9: // StTxtPara
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					break;
				case 10:
					// Display a Footnote by displaying its "FootnoteMarker" in a paragraph
					// by itself, followed by the sequence of paragraphs.
					vwenv.AddStringProp(StFootnoteTags.kflidFootnoteMarker, null);
					vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, 9);
					break;
				case 11:
					// Display a Footnote by displaying its "FootnoteMarker" followed by the
					// contents of its first paragraph (similar to the way footnotes are displayed in
					// real life.
					vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this, 12);
					break;
				case 12: // Footnote paragraph with marker
					vwenv.OpenMappedTaggedPara();
					// The footnote marker is not editable.
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);

					// add a read-only space after the footnote marker
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
					vwenv.AddString(TsStringUtils.MakeString(" ", DefaultWs));
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					vwenv.CloseParagraph();
					break;
				default:
					throw new ApplicationException("Unexpected frag in DummyBasicViewVc");
			}
		}

		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems. Note that the calling
		/// code does NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available width.
		/// </summary>
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			return kEstimatedParaHeight;  // just give any arbitrary number
		}

		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID. This dummy version just returns something similar to what
		/// TE would normally put in for an alpha footnote.
		/// </summary>
		public override ITsString GetStrForGuid(string bstrGuid)
		{
			return TsStringUtils.MakeString("\uFEFFa", m_wsDefault);
		}
		#endregion
	}
}