// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyBasicViewVc.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// The class that displays the draft view.
	/// </summary>
	public class SimpleViewVc : VwBaseVc
	{
		/// <summary>How to display the text</summary>
		[Flags]
		public enum DisplayType
		{
			/// <summary>Display lazy boxes</summary>
			kLazy = 1,
			/// <summary>Display non-lazy boxes</summary>
			kNormal = 2,
			/// <summary>Display paragraphs with top-margin</summary>
			kWithTopMargin = 4,
			/// <summary>Display lazy and non-lazy boxes (this isn't really "all", because it
			/// doesn't include outer object details or literal sring labels)</summary>
			kAll = 7,
			/// <summary>Display outer object details (for testing GetOuterObject)</summary>
			kOuterObjDetails = 8,
			/// <summary>View adds a read-only label literal string as a label before
			/// each paragraph</summary>
			kLiteralStringLabels = 16,
			/// <summary>View adds each paragraph an additional time (only applies when kNormal flag is set)</summary>
			kDuplicateParagraphs = 32,
			/// <summary>Display a mapped paragraph</summary>
			kMappedPara = 64,
			/// <summary>In addition to displaying the normal StTexts as requested in the
			/// constructor, also display the Title.</summary>
			kTitle = 128,
			/// <summary>Display a Footnote by displaying its "FootnoteMarker" in a paragraph
			/// by itself, followed by its sequence of paragraphs.</summary>
			kFootnoteDetailsSeparateParas = 256,
			/// <summary>Display a Footnote by displaying its "FootnoteMarker" followed by the
			/// contents of its first paragraph (similar to the way footnotes are displayed in
			/// real life.</summary>
			kFootnoteDetailsSinglePara = 512,
			/// <summary>When displaying normal (non-tagged) paragraphs, apply the properties</summary>
			kUseParaProperties = 1024,
			/// <summary>Strangely enough, the "normal" behavior of this VC is to display paragraph
			/// contents three times. use this flag to suppress this.</summary>
			kOnlyDisplayContentsOnce = 2048,
		}

		private readonly DisplayType m_displayType;
		private readonly int m_flid;
		private int m_counter = 1;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DummyBasicViewVc class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleViewVc(): this(DisplayType.kAll, kflidTestDummy)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DummyBasicViewVc class
		/// </summary>
		/// <param name="display"></param>
		/// <param name="flid">Flid in which the root object contains a sequence of StTexts
		/// </param>
		/// ------------------------------------------------------------------------------------
		public SimpleViewVc(DisplayType display, int flid)
		{
			m_displayType = display;
			m_flid = flid;
		}

		/// <summary></summary>
		public const int kflidTestDummy = 999;
		/// <summary></summary>
		public const int kMarginTop = 60000;
		/// <summary></summary>
		public const int kdzmpInch = 72000;
		/// <summary></summary>
		public const int kEstimatedParaHeight = 30;

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			if ((m_displayType & DisplayType.kLiteralStringLabels) != 0)
			{
				ITsStrFactory factory = TsStrFactoryClass.Create();
				vwenv.AddString(factory.MakeString("Label" + m_counter++, m_wsDefault));
			}
			switch(frag)
			{
				case 1: // the root; display the subitems, first using non-lazy view, then lazy one.
					if ((m_displayType & DisplayType.kFootnoteDetailsSeparateParas) == DisplayType.kFootnoteDetailsSeparateParas)
						vwenv.AddObjVecItems(m_flid, this, 10);
					if ((m_displayType & DisplayType.kFootnoteDetailsSinglePara) == DisplayType.kFootnoteDetailsSinglePara)
						vwenv.AddObjVecItems(m_flid, this, 11);
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
					if ((m_displayType & DisplayType.kTitle) == DisplayType.kTitle)
						vwenv.AddObjProp(SimpleRootsiteTestsBase.kflidDocTitle, this, 3);
					if (m_displayType == DisplayType.kOuterObjDetails)
						vwenv.AddObjVecItems(m_flid, this, 6);
					break;
				case 2: // An StText, display paragraphs lazily
					if ((m_displayType & DisplayType.kWithTopMargin) == DisplayType.kWithTopMargin)
						vwenv.AddLazyVecItems(SimpleRootsiteTestsBase.kflidTextParas, this, 4);
					vwenv.AddLazyVecItems(SimpleRootsiteTestsBase.kflidTextParas, this, 5);
					break;
				case 3: // An StText, display paragraphs not lazily.
					if ((m_displayType & DisplayType.kWithTopMargin) == DisplayType.kWithTopMargin)
						vwenv.AddObjVecItems(SimpleRootsiteTestsBase.kflidTextParas, this, 4);
					vwenv.AddObjVecItems(SimpleRootsiteTestsBase.kflidTextParas, this, 5);
					if ((m_displayType & DisplayType.kDuplicateParagraphs) != 0)
						vwenv.AddObjVecItems(SimpleRootsiteTestsBase.kflidTextParas, this, 5);
					break;
				case 4: // StTxtPara, display contents with top margin
					OpenParaIfNeeded(vwenv, hvo);
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop,
						(int)FwTextPropVar.ktpvMilliPoint, kMarginTop);
					AddParagraphContents(vwenv);
					break;
				case 5: // StTxtPara, display contents without top margin
					OpenParaIfNeeded(vwenv, hvo);
					AddParagraphContents(vwenv);
					break;
				case 6: // StTxtPara, display details of our outer object
					int hvoOuter, tag, ihvo;
					vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
					ITsString tss = TsStringHelper.MakeTSS("Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo,
						m_wsDefault);
					vwenv.AddString(tss);
					break;
				case SimpleRootsiteTestsBase.kflidDocDivisions:
					vwenv.AddObjVecItems(SimpleRootsiteTestsBase.kflidDocDivisions, this,
						SimpleRootsiteTestsBase.kflidSectionStuff);
					break;
				case SimpleRootsiteTestsBase.kflidSectionStuff:
					if ((m_displayType & DisplayType.kNormal) == DisplayType.kNormal)
						vwenv.AddObjProp(frag, this, 3);
					if ((m_displayType & DisplayType.kLazy) == DisplayType.kLazy)
						vwenv.AddObjProp(frag, this, 2);
					break;
				case 7: // ScrBook
					vwenv.OpenDiv();
					vwenv.AddObjVecItems(SimpleRootsiteTestsBase.kflidDocFootnotes, this, 8);
					vwenv.CloseDiv();
					break;
				case 8: // StFootnote
					vwenv.AddObjVecItems(SimpleRootsiteTestsBase.kflidTextParas, this,
						9);
					break;
				case 9: // StTxtPara
					vwenv.AddStringProp(SimpleRootsiteTestsBase.kflidParaContents, null);
					break;
				case 10:
					// Display a Footnote by displaying its "FootnoteMarker" in a paragraph
					// by itself, followed by the sequence of paragraphs.
					vwenv.AddStringProp(SimpleRootsiteTestsBase.kflidFootnoteMarker, null);
					vwenv.AddObjVecItems(SimpleRootsiteTestsBase.kflidTextParas, this,
						9);
					break;
				case 11:
					// Display a Footnote by displaying its "FootnoteMarker" followed by the
					// contents of its first paragraph (similar to the way footnotes are displayed in
					// real life.
					vwenv.AddObjVecItems(SimpleRootsiteTestsBase.kflidTextParas, this, 12);
					break;
				case 12: // Footnote paragraph with marker
					vwenv.OpenMappedTaggedPara();
					// The footnote marker is not editable.
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptNotEditable);
					vwenv.AddStringProp(SimpleRootsiteTestsBase.kflidFootnoteMarker, null);

					// add a read-only space after the footnote marker
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptNotEditable);
					ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
					strBldr.Append(" ");
					vwenv.AddString(strBldr.GetString());
					vwenv.AddStringProp(SimpleRootsiteTestsBase.kflidParaContents, null);
					vwenv.CloseParagraph();
					break;
				default:
					throw new ApplicationException("Unexpected frag in DummyBasicViewVc");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the current paragraph's contents (either once or thrice, depending on display
		/// flags) and then close the paragraph if necessary.
		/// </summary>
		/// <param name="vwenv">The view environment</param>
		/// ------------------------------------------------------------------------------------
		private void AddParagraphContents(IVwEnv vwenv)
		{
			vwenv.AddStringProp(SimpleRootsiteTestsBase.kflidParaContents, null);
			if ((m_displayType & DisplayType.kOnlyDisplayContentsOnce) != DisplayType.kOnlyDisplayContentsOnce)
			{
				vwenv.AddStringProp(SimpleRootsiteTestsBase.kflidParaContents, null);
				vwenv.AddStringProp(SimpleRootsiteTestsBase.kflidParaContents, null);
			}
			if ((m_displayType & DisplayType.kMappedPara) == DisplayType.kMappedPara)
				vwenv.CloseParagraph();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Conditionally open a mapped paragraph (depending on the requested display type).
		/// Also, if requested, apply the paragraph properties of the given paragraph
		/// </summary>
		/// <param name="vwenv">The view environment</param>
		/// <param name="hvo">HVO of the paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void OpenParaIfNeeded(IVwEnv vwenv, int hvo)
		{
			if ((m_displayType & DisplayType.kMappedPara) == DisplayType.kMappedPara)
			{
				if ((m_displayType & DisplayType.kUseParaProperties) == DisplayType.kUseParaProperties)
					vwenv.Props = (ITsTextProps) vwenv.DataAccess.get_UnknownProp(hvo, SimpleRootsiteTestsBase.kflidParaProperties);
				vwenv.OpenMappedPara();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems. Note that the calling
		/// code does NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available width.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>Height of an item</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
//			Debug.WriteLine(string.Format("Estimateheight for hvo: {0}, frag:{1}", hvo, frag));
			return kEstimatedParaHeight;  // just give any arbitrary number
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID. This dummy version just returns something similar to what
		/// TE would normally put in for an alpha footnote.
		/// </summary>
		/// <param name="bstrGuid"></param>
		/// <returns>non-breaking space</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString GetStrForGuid(string bstrGuid)
		{
			TsStrFactory strFactory = TsStrFactoryClass.Create();
			return strFactory.MakeString("\uFEFFa", m_wsDefault);
		}
		#endregion
	}
}
