// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlVcDisplayVec.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The old XmlVc.DisplayVec() method had gotten to 370 lines of code. This class is a method
	/// object whose primary purpose is to allow refactoring of this huge method.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_cache is a reference")]
	public class XmlVcDisplayVec
	{
		#region Member Variables

		private readonly XmlVc m_viewConstructor;
		private readonly IVwEnv m_vwEnv;
		private readonly int m_hvo;
		private readonly int m_flid;
		private readonly int m_frag;
		private readonly FdoCache m_cache;
		private readonly ISilDataAccess m_sda;
		private readonly ICmObjectRepository m_objRepo;
		private readonly StringTable m_stringTable;
		/// <summary>
		/// The number part ref that is either current when we call OutputItemNumber, or that
		/// was current when we set tssDelayedNumber.
		/// </summary>
		private XmlNode m_numberPartRef;
		int m_hvoDelayedNumber;

		#endregion

		/// <summary>
		/// The method object's constructor.
		/// </summary>
		/// <param name="vc">The view constructor</param>
		/// <param name="vwenv">The view environment</param>
		/// <param name="hvo">A handle on the root object</param>
		/// <param name="flid">The field ID</param>
		/// <param name="frag">A code identifying the current part of the display</param>
		public XmlVcDisplayVec(XmlVc vc, IVwEnv vwenv, int hvo, int flid, int frag)
		{
			m_viewConstructor = vc;
			m_vwEnv = vwenv;
			m_hvo = hvo;
			m_flid = flid;
			m_frag = frag;
			m_cache = m_viewConstructor.Cache;
			m_sda = m_viewConstructor.DataAccess;
			if (vwenv.DataAccess != null)
				m_sda = vwenv.DataAccess;
			m_objRepo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			m_stringTable = m_viewConstructor.StringTbl;
		}

		private LayoutCache Layouts
		{
			get { return m_viewConstructor.m_layouts; }
		}

		internal bool DelayedNumberExists
		{
			get
			{
				return m_viewConstructor.DelayedNumberExists;
			}
		}

		const string strEng = "en";
		const int kflidSenseMsa = LexSenseTags.kflidMorphoSyntaxAnalysis;

		/// <summary>
		/// The main entry point to do the work of the original method.
		/// </summary>
		internal void Display(ref ITsString tssDelayedNumber)
		{
			MainCallerDisplayCommand dispInfo;
			if (!m_viewConstructor.CanGetMainCallerDisplayCommand(m_frag, out dispInfo))
			{
				// Shouldn't be possible, but just in case...
				Debug.Assert(true, "No MainCallerDisplayCommand!");
				return;
			}

			XmlNode listDelimitNode; // has the list seps attrs like 'sep'
			XmlNode specialAttrsNode;  // has the more exotic ones like 'excludeHvo'
			listDelimitNode = specialAttrsNode = dispInfo.MainNode;
			// 'inheritSeps' attr means to use the 'caller' (the part ref node)
			// to get the separator information.
			if (XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "inheritSeps", false))
				listDelimitNode = dispInfo.Caller;
			//
			// 1. get number of items in vector
			//
			var chvo = m_sda.get_VecSize(m_hvo, m_flid);
			if (chvo == 0)
			{
				// We may want to do something special for empty vectors.  See LT-9687.
				ProcessEmptyVector(m_vwEnv, m_hvo, m_flid);
				return;
			}
			//
			// 2. for each item in the vector,
			//		a) print leading number if desired.
			//		b) call AddObj
			//		c) print separator if desired and needed
			//
			int[] rghvo = GetVector(m_sda, m_hvo, m_flid);
			Debug.Assert(chvo == rghvo.Length);
			//
			// Define some special boolean flags.
			//
			// Note that we deliberately don't use the listDelimitNode here.
			// These three props are not currently configurable, and they belong on the 'seq' element,
			// not the part ref.
			var fCheckForEmptyItems = XmlUtils.GetOptionalBooleanAttributeValue(specialAttrsNode,
				"checkForEmptyItems", false);
			string exclude = XmlUtils.GetOptionalAttributeValue(specialAttrsNode, "excludeHvo", null);
			var fFirstOnly = XmlUtils.GetOptionalBooleanAttributeValue(specialAttrsNode, "firstOnly", false);

			XmlAttribute xaNum;
			var fNumber = SetNumberFlagIncludingSingleOption(listDelimitNode, chvo, out xaNum);

			ApplySortingIfSpecified(rghvo, specialAttrsNode);

			// Determine if sequence should be filtered by a stored list of Guids.
			// Note that if we filter, we replace rghvo with the filtered list.
			if (m_viewConstructor.ShouldFilterByGuid)
			{  // order by vector item type guids
				// Don't reorder LexEntry VisibleComplexFormBackRefs vector if the user overrode it manually.
				var obj = m_cache.ServiceLocator.GetObject(m_hvo);
				if (obj is ILexEntry)
				{
					var lexEntry = obj as ILexEntry;
					if (m_flid == m_cache.MetaDataCacheAccessor.GetFieldId("LexEntry", "VisibleComplexFormBackRefs", false))
					{
						if (!VirtualOrderingServices.HasVirtualOrdering(lexEntry, "VisibleComplexFormBackRefs"))
							chvo = ApplyFilterToSequence(ref rghvo);
					}
					else
					{
						chvo = ApplyFilterToSequence(ref rghvo);
					}
				}
				else
				{
					chvo = ApplyFilterToSequence(ref rghvo);
				}
			}

			// Check whether the user wants the grammatical information to appear only once, preceding any
			// sense numbers, if there is only one set of grammatical information, and all senses refer to
			// it.  See LT-9663.
			var childFrag = m_frag;
			var fSingleGramInfoFirst = false;
			if (m_flid == LexEntryTags.kflidSenses)
				fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "singlegraminfofirst", false);

			// This groups senses by placing graminfo before the number, and omitting it if the same as the
			// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
			// the future.  (See LT-9663.)
			//bool fGramInfoBeforeNumber = XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "graminfobeforenumber", false);

			// Setup text properties for any numbering
			int wsEng = m_cache.WritingSystemFactory.GetWsFromStr(strEng);
			ITsTextProps ttpNum = null;
			var fDelayNumber = false;
			if (fNumber)
			{
				ttpNum = SetNumberTextProperties(wsEng, listDelimitNode);
				fDelayNumber = XmlUtils.GetOptionalBooleanAttributeValue(specialAttrsNode, "numdelay", false);
			}

			// A vector may be conditionally configured to display its objects as separate paragraphs
			// in dictionary (document) configuration.  See LT-9667.
			var fShowAsParagraphsInInnerPile = XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode,
				"showasindentedpara", false);
			// We have (this is probably bad) two ways to do this. The better one is setting the flowType to divInPara.
			// When we do this for a vector, and configure properties that require us to insert numbering and so forth,
			// we currently force a paragraph for each item. It's too hard otherwise to get the numbers etc. into the paragraph.
			// This means that the X_AsPara layout which the configure dialog causes to be invoked when setting up sense-as-para
			// has to be configured NOT to make a paragraph. It also means we can't readily configure a view that has more than one
			// paragraph, except when doing another layer of inserting paragraphs into what is usually a single one. I don't like
			// this much but it does what we need for now and making it better looks very messy.
			var fShowAsParagraphsInDivInPara = XmlUtils.GetOptionalAttributeValue(listDelimitNode, "flowType", null) == "divInPara";
			ITsString tssBefore = null;
			string sParaStyle = null;
			if (fShowAsParagraphsInInnerPile && chvo > 0)
			{
				sParaStyle = XmlUtils.GetOptionalAttributeValue(listDelimitNode, "style");
				tssBefore = SetBeforeString(specialAttrsNode, listDelimitNode);
				// We need a line break here to force the inner pile of paragraphs to begin at
				// the margin, rather than somewhere in the middle of the line.
				m_vwEnv.AddString(m_cache.TsStrFactory.MakeString(StringUtils.kChHardLB.ToString(),
					m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle));
				m_vwEnv.OpenInnerPile();
			}
			else if (fShowAsParagraphsInDivInPara)
			{
				sParaStyle = XmlUtils.GetOptionalAttributeValue(listDelimitNode, "parastyle", "");
			}

			// Setup and run actual internal vector loop
			var tsf = m_cache.TsStrFactory;
			var xattrSeparator = listDelimitNode.Attributes["sep"];
			var fFirst = true; // May actually mean first non-empty.
			for (var ihvo = 0; ihvo < chvo; ++ihvo)
			{
				if (IsExcluded(exclude, ihvo, rghvo))
					continue;

				if (fCheckForEmptyItems && IsItemEmpty(rghvo[ihvo], childFrag))
					continue;

				if (fShowAsParagraphsInInnerPile || fShowAsParagraphsInDivInPara)
					SetupParagraph(sParaStyle, (fFirst ? tssBefore : null), listDelimitNode);

				// This needs to happen AFTER we wet up the paragraph that we want it to be part of
				// and any 'before' stuff that goes ahead of the whole sequence, but BEFORE we add any other stuff to the para.
				if (fFirst && fNumber && fSingleGramInfoFirst)
				{
					var fAllMsaSame = SetAllMsaSameFlag(chvo, rghvo);
					if (fAllMsaSame)
						DisplayFirstChildPOS(rghvo[0], childFrag);

					// Exactly if we put out the grammatical info at the start, we need to NOT put it out
					// as part of each item. Note that we must not set this flag before we put out the one-and-only
					// gram info, or that will be suppressed too!
					m_viewConstructor.ShouldIgnoreGramInfo = fAllMsaSame;
				}

				if (!fShowAsParagraphsInInnerPile && !fShowAsParagraphsInDivInPara)
					AddSeparatorIfNeeded(fFirst, xattrSeparator, listDelimitNode, wsEng);

				// add the numbering if needed.
				if (fNumber)
				{
					var sTag = CalculateAndFormatSenseLabel(rghvo, ihvo, xaNum);

					ITsStrBldr tsb = tsf.GetBldr();
					tsb.Replace(0, 0, sTag, ttpNum);
					ITsString tss = tsb.GetString();
					m_numberPartRef = listDelimitNode;
					AddNumberingNowOrDelayed(fDelayNumber, tss, out tssDelayedNumber);
				}

				// add the object.
				Debug.Assert(ihvo < rghvo.Length);
				m_vwEnv.AddObj(rghvo[ihvo], m_viewConstructor, childFrag);

				// Close Paragraph if displaying paragraphs
				if (fShowAsParagraphsInInnerPile || fShowAsParagraphsInDivInPara)
					m_vwEnv.CloseParagraph();

				fFirst = false;
				if (fFirstOnly)
					break;
			} // end of sequence 'for' loop

			// Close Inner Pile if displaying paragraphs
			if (fShowAsParagraphsInInnerPile && chvo > 0)
				m_vwEnv.CloseInnerPile();

			// Reset the flag for ignoring grammatical information after the first if it was set
			// earlier in this method.
			if (fSingleGramInfoFirst)
				m_viewConstructor.ShouldIgnoreGramInfo = false;

			// Reset the flag for delaying displaying a number.
			if (m_viewConstructor.DelayNumFlag && m_hvo == m_hvoDelayedNumber)
			{
				m_viewConstructor.DelayNumFlag = false;
				tssDelayedNumber = null;
			}

			// end of Display method
		}

		private bool SetAllMsaSameFlag(int chvo, int[] rghvo)
		{
			int hvoMsa = m_sda.get_ObjectProp(rghvo[0], kflidSenseMsa);
			var fAllMsaSame = SubsenseMsasMatch(rghvo[0], hvoMsa);
			for (var i = 1; fAllMsaSame && i < chvo; ++i)
			{
				int hvoMsa2 = m_sda.get_ObjectProp(rghvo[i], kflidSenseMsa);
				fAllMsaSame = hvoMsa == hvoMsa2 && SubsenseMsasMatch(rghvo[i], hvoMsa);
			}
			return fAllMsaSame;
		}

		// Display the first child (item in the vector) in a special mode which suppresses everything except the child
		// marked singlegraminfofirst, to show the POS.
		private void DisplayFirstChildPOS(int firstChildHvo, int childFrag)
		{
			var dispCommand = (MainCallerDisplayCommand)m_viewConstructor.m_idToDisplayCommand[childFrag];
			string layoutName;
			var parent = dispCommand.GetNodeForChild(out layoutName, childFrag, m_viewConstructor, firstChildHvo);
			foreach(XmlNode gramInfoPartRef in parent.ChildNodes)
			{
				if (XmlUtils.GetOptionalBooleanAttributeValue(gramInfoPartRef, "singlegraminfofirst", false))
				{
					// It really is the gram info part ref we want.
					//m_viewConstructor.ProcessPartRef(gramInfoPartRef, firstChildHvo, m_vwEnv); no! the sense is not on the stack.
					var sVisibility = XmlUtils.GetOptionalAttributeValue(gramInfoPartRef, "visibility", "always");
					if (sVisibility == "never")
						return; // user has configured gram info first, but turned off gram info.
					string morphLayoutName = XmlUtils.GetManditoryAttributeValue(gramInfoPartRef, "ref");
					var part = m_viewConstructor.GetNodeForPart(firstChildHvo, morphLayoutName, false);
					if (part == null)
						throw new ArgumentException("Attempt to display gram info of first child, but part for " + morphLayoutName + " does not exist");
					var objNode = XmlUtils.GetFirstNonCommentChild(part);
					if (objNode == null || objNode.Name != "obj")
						throw new ArgumentException("Attempt to display gram info of first child, but part for " + morphLayoutName + " does not hav a single <obj> child");
					int flid = XmlVc.GetFlid(objNode, firstChildHvo, m_viewConstructor.DataAccess);
					int hvoTarget = m_viewConstructor.DataAccess.get_ObjectProp(firstChildHvo, flid);
					if (hvoTarget == 0)
						return; // first sense has no category.
					int fragId = m_viewConstructor.GetSubFragId(objNode, gramInfoPartRef);
					if (m_vwEnv is ConfiguredExport)
						(m_vwEnv as ConfiguredExport).BeginCssClassIfNeeded(gramInfoPartRef);
					m_vwEnv.AddObj(hvoTarget, m_viewConstructor, fragId);
					if (m_vwEnv is ConfiguredExport)
						(m_vwEnv as ConfiguredExport).EndCssClassIfNeeded(gramInfoPartRef);
					return;
				}
			}
			throw new ArgumentException("Attempt to display gram info of first child, but template has no singlegraminfofirst");
		}

		private ITsString SetBeforeString(XmlNode specialAttrsNode, XmlNode listDelimitNode)
		{
			ITsString tssBefore = null;
			string sBefore = XmlUtils.GetLocalizedAttributeValue(m_stringTable, listDelimitNode, "before", null);
			if (!String.IsNullOrEmpty(sBefore) || DelayedNumberExists)
			{
				if (sBefore == null)
					sBefore = String.Empty;
				tssBefore = m_cache.TsStrFactory.MakeString(sBefore, m_cache.WritingSystemFactory.UserWs);
				tssBefore = ApplyStyleToBeforeString(listDelimitNode, tssBefore);
				tssBefore = ApplyDelayedNumber(specialAttrsNode, tssBefore);
			}
			return tssBefore;
		}

		private static ITsString ApplyStyleToBeforeString(XmlNode listDelimitNode, ITsString tssBefore)
		{
			var sStyle = XmlUtils.GetAttributeValue(listDelimitNode, "beforeStyle");
			if (!String.IsNullOrEmpty(sStyle))
			{
				var bldr = tssBefore.GetBldr();
				bldr.SetStrPropValue(0, bldr.Length, (int) FwTextPropType.ktptNamedStyle, sStyle);
				tssBefore = bldr.GetString();
			}
			return tssBefore;
		}

		private ITsString ApplyDelayedNumber(XmlNode specialAttrsNode, ITsString tssBefore)
		{
			var tssNumber = m_viewConstructor.GetDelayedNumber(
				specialAttrsNode, m_vwEnv is TestCollectorEnv);
			if (tssNumber != null)
			{
				var tsb = tssBefore.GetBldr();
				tsb.Replace(0, 0, tssNumber.Text, null);
				tssBefore = tsb.GetString();
			}
			return tssBefore;
		}

		private ITsTextProps SetNumberTextProperties(int wsEng, XmlNode listDelimitNode)
		{
			ITsTextProps ttpNum;
			ITsPropsBldr tpb = TsPropsFactoryClass.Create().GetPropsBldr();
			// TODO: find more appropriate writing system?
			tpb.SetIntPropValues((int) FwTextPropType.ktptWs, 0, wsEng);
			string style = XmlUtils.GetOptionalAttributeValue(listDelimitNode, "numstyle", null);
			ApplyStyleToTsPropertyBuilder(tpb, style);
			string font = XmlUtils.GetOptionalAttributeValue(listDelimitNode, "numfont", null);
			if (!String.IsNullOrEmpty(font))
				tpb.SetStrPropValue((int) FwTextPropType.ktptFontFamily, font);
			m_viewConstructor.MarkSource(tpb, listDelimitNode);
			ttpNum = tpb.GetTextProps();
			return ttpNum;
		}

		private static bool SetNumberFlagIncludingSingleOption(XmlNode listDelimitNode, int chvo, out XmlAttribute xaNum)
		{
			Debug.Assert(listDelimitNode != null, "Node can not be null!");
			xaNum = listDelimitNode.Attributes["number"];
			var flag = xaNum != null && !String.IsNullOrEmpty(xaNum.Value);
			if (flag && chvo == 1)
				flag = XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "numsingle", false);
			return flag;
		}

		private int ApplyFilterToSequence(ref int[] rghvo)
		{
			rghvo = m_viewConstructor.FilterAndSortListByComplexFormType(rghvo, m_hvo);
			var chvo = rghvo.Length;
			if (chvo == 0)
			{
				// We may want to do something special for empty vectors.  See LT-9687.
				ProcessEmptyVector(m_vwEnv, m_hvo, m_flid);
			}
			return chvo;
		}

		private void AddSeparatorIfNeeded(bool fFirst, XmlNode xaSep, XmlNode listDelimitNode, int ws)
		{
			if (fFirst || xaSep == null)
				return;

			// add the separator.
			var sSep = !string.IsNullOrEmpty(xaSep.Value) ? xaSep.Value : " ";
			m_viewConstructor.AddMarkedString(m_vwEnv, listDelimitNode, sSep, ws);
		}

		private void SetupParagraph(string sParaStyle, ITsString tssBefore, XmlNode listDelimitNode)
		{
			if (!String.IsNullOrEmpty(sParaStyle))
				m_vwEnv.set_StringProperty((int) FwTextPropType.ktptNamedStyle, sParaStyle);
			m_vwEnv.OpenParagraph();
			if (tssBefore != null)
				m_viewConstructor.AddMarkedString(m_vwEnv, listDelimitNode, tssBefore);
		}

		private bool IsExcluded(string exclude, int ihvo, int[] rghvo)
		{
			if (String.IsNullOrEmpty(exclude))
				return false;
			if (exclude == "this" && rghvo[ihvo] == m_hvo)
				return true;
			if (exclude == "parent")
			{
				int hvoParent, tagDummy, ihvoDummy;
				m_vwEnv.GetOuterObject(m_vwEnv.EmbeddingLevel - 1, out hvoParent,
					out tagDummy, out ihvoDummy);
				if (rghvo[ihvo] == hvoParent)
					return true;
			}
			return false;
		}

		private void AddNumberingNowOrDelayed(bool fDelayNumber, ITsString tss, out ITsString tssDelayedNumber)
		{
			if (fDelayNumber)
			{
				m_viewConstructor.DelayNumFlag = true;

				ITsIncStrBldr tisb = tss.GetIncBldr();
				tisb.Append("  "); // add some padding for separation
				tssDelayedNumber = tisb.GetString();
				m_hvoDelayedNumber = m_hvo;
			}
			else
			{
				m_viewConstructor.DelayNumFlag = false;

				OutputItemNumber(m_vwEnv, tss);
				tssDelayedNumber = null;
			}
			// This groups senses by placing graminfo before the number, and omitting it if the same as the
			// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
			// the future.  (See LT-9663.)
			//if (fGramInfoBeforeNumber)
			//{
			//    tssDelayedNumber = tss;
			//    if (fFirst)
			//        m_hvoGroupedValue = 0;
			//}
			//else if (!m_fDelayNumber)
			//{
			//    m_vwEnv.AddString(tss);
			//    tssDelayedNumber = null;
			//}
		}

		private void ApplySortingIfSpecified(int[] rghvo, XmlNode specialAttrsNode)
		{
			string sort = XmlUtils.GetOptionalAttributeValue(specialAttrsNode, "sort", null);
			if (sort == null)
				return;
			// sort the items in this collection, based on the SortKey property
			bool ascending = sort.ToLowerInvariant() == "ascending";
			var hvos = new List<int>(rghvo);
			using (var comparer = new CmObjectComparer(m_cache))
				hvos.Sort(comparer);
			if (!ascending)
				hvos.Reverse();
			hvos.CopyTo(rghvo);
		}

		private static void ApplyStyleToTsPropertyBuilder(ITsPropsBldr tpb, string style)
		{
			if (String.IsNullOrEmpty(style))
				return;
			style = style.ToLowerInvariant();
			// N.B.: Was tempted to refactor with IndexOf("-"), but realized
			// that style could be "italic -bold" or "-italic bold" or "-italic -bold".
			if (style.IndexOf("-bold") >= 0)
			{
				tpb.SetIntPropValues((int) FwTextPropType.ktptBold,
									 (int) FwTextPropVar.ktpvEnum,
									 (int) FwTextToggleVal.kttvOff);
			}
			else if (style.IndexOf("bold") >= 0)
			{
				tpb.SetIntPropValues((int) FwTextPropType.ktptBold,
									 (int) FwTextPropVar.ktpvEnum,
									 (int) FwTextToggleVal.kttvForceOn);
			}
			if (style.IndexOf("-italic") >= 0)
			{
				tpb.SetIntPropValues((int) FwTextPropType.ktptItalic,
									 (int) FwTextPropVar.ktpvEnum,
									 (int) FwTextToggleVal.kttvOff);
			}
			else if (style.IndexOf("italic") >= 0)
			{
				tpb.SetIntPropValues((int) FwTextPropType.ktptItalic,
									 (int) FwTextPropVar.ktpvEnum,
									 (int) FwTextToggleVal.kttvForceOn);
			}
		}

		/// <summary>
		/// Takes a coded number string and interprets it.
		/// Only the first code in the string is interpreted.
		/// The emedded codes are:
		/// %d for an integer
		/// %A, %a for upper and lower alpha,
		/// %I, %i for upper and lower roman numerals,
		/// %O for outline number (a real number)
		/// %z is short for %d.%a.%i; the lowest level is incremented or set.
		/// </summary>
		/// <param name="rghvo">hvo array of senses</param>
		/// <param name="ihvo">A sequence number or index (into rghvo).</param>
		/// <param name="xaNum">The sequence number format to apply</param>
		/// <returns>The format filled in with the sequence number</returns>
		private string CalculateAndFormatSenseLabel(int[] rghvo, int ihvo, XmlAttribute xaNum)
		{
			var sNum = "";
			var sTag = xaNum.Value;
			int ich;
			for (ich = 0; ich < sTag.Length; ++ich)
			{
				if (sTag[ich] != '%' || sTag.Length <= ich + 1)
					continue;
				++ich;
				switch (sTag[ich])
				{
					case 'd':
						sNum = string.Format("{0}", ihvo + 1);
						break;
					case 'A':
						sNum = AlphaOutline.NumToAlphaOutline(ihvo + 1);
						break;
					case 'a':
						sNum = AlphaOutline.NumToAlphaOutline(ihvo + 1).ToLower();
						break;
					case 'I':
						sNum = RomanNumerals.IntToRoman(ihvo + 1);
						break;
					case 'i':
						sNum = RomanNumerals.IntToRoman(ihvo + 1).ToLower();
						break;
					case 'O':
						if (m_cache.MetaDataCacheAccessor.get_IsVirtual(m_flid))
							sNum = String.Format("{0}", ihvo + 1);
						else
							sNum = m_cache.GetOutlineNumber(m_objRepo.GetObject(rghvo[ihvo]), false, true);
						break;
					case 'z':
						sNum = GetOutlineStyle2biv(rghvo, ihvo);
						break;
					// MDL: can only get to this case for "%%" - does it mean anything?
					case '%':
						sTag.Remove(ich, 1); // removes the 2nd %
						--ich;
						break;
				}
				break; // ich is the position of the code character after the last %
			}
			// sNum is the ordinate that will replace the 2-letter code.
			if (sNum.Length != 0)
				sTag = (sTag.Remove(ich - 1, 2)).Insert(ich - 1, sNum);
			return sTag;
		}

		private string GetOutlineStyle2biv(int[] rghvo, int ihvo)
		{
			// Top-level: Arabic numeral.
			// Second-level: lowercase letter.
			// Third-level (and lower): lowercase Roman numeral.
			string sNum; // return result string
			var sOutline = m_cache.GetOutlineNumber(m_objRepo.GetObject(rghvo[ihvo]), false, true);
			var cchPeriods = 0;
			var ichPeriod = sOutline.IndexOf('.');
			while (ichPeriod >= 0)
			{
				++cchPeriods;
				ichPeriod = sOutline.IndexOf('.', ichPeriod + 1);
			}
			switch (cchPeriods)
			{
				case 0:
					sNum = string.Format("{0}", ihvo + 1);
					break;
				case 1:
					sNum = AlphaOutline.NumToAlphaOutline(ihvo + 1).ToLower();
					break;
				default:
					sNum = RomanNumerals.IntToRoman(ihvo + 1).ToLower();
					break;
			}
			return sNum;
		}

		/// <summary>
		/// Check whether all the subsenses (if any) use the given MSA.
		/// </summary>
		private bool SubsenseMsasMatch(int hvoSense, int hvoMsa)
		{
			int[] rghvoSubsense = GetVector(m_sda, hvoSense, LexSenseTags.kflidSenses);
			for (var i = 0; i < rghvoSubsense.Length; ++i)
			{
				int hvoMsa2 = m_sda.get_ObjectProp(rghvoSubsense[i],
					LexSenseTags.kflidMorphoSyntaxAnalysis);
				if (hvoMsa != hvoMsa2 || !SubsenseMsasMatch(rghvoSubsense[i], hvoMsa))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Get the items from a vector property.
		/// </summary>
		private static int[] GetVector(ISilDataAccess sda, int hvo, int tag)
		{
			var chvo = sda.get_VecSize(hvo, tag);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
			{
				sda.VecProp(hvo, tag, chvo, out chvo, arrayPtr);
				return MarshalEx.NativeToArray<int>(arrayPtr, chvo);
			}
		}

		private bool IsItemEmpty(int hvo, int fragId)
		{
			// If it's not the kind of vector we know how to deal with, safest to assume the item
			// is not empty.
			MainCallerDisplayCommand command;
			if (!m_viewConstructor.CanGetMainCallerDisplayCommand(fragId, out command))
				return false;

			string layoutName;
			XmlNode node = command.GetNodeForChild(out layoutName, fragId, m_viewConstructor, hvo);
			var keys = XmlViewsUtils.ChildKeys(m_cache, m_sda, node, hvo, Layouts,
				command.Caller, m_stringTable, m_viewConstructor.WsForce);
			return AreAllKeysEmpty(keys);
		}

		/// <summary>
		/// Return true if every key in the array is empty or null. This includes the case of zero keys.
		/// </summary>
		/// <param name="keys"></param>
		/// <returns></returns>
		private static bool AreAllKeysEmpty(IEnumerable<string> keys)
		{
			return keys == null || keys.All(String.IsNullOrEmpty);
		}

		/// <summary>
		/// Processes the empty vector.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="flid">The flid.</param>
		private void ProcessEmptyVector(IVwEnv vwenv, int hvo, int flid)
		{
			// If we're collecting displayed items, and we could have a list of items but don't,
			// add an hvo of 0 to the collection for use in filtering on missing information.
			// See LT-9687.
			if (vwenv is XmlBrowseViewBaseVc.ItemsCollectorEnv)
			{
				// The complexities of LexEntryRef objects makes the following special case code
				// necessary to achieve satisfactory results for LT-9687.
				if (flid == LexEntryRefTags.kflidComplexEntryTypes)
				{
					int type = m_sda.get_IntProp(hvo, LexEntryRefTags.kflidRefType);
					if (type != LexEntryRefTags.krtComplexForm)
						return;
				}
				else if (flid == LexEntryRefTags.kflidVariantEntryTypes)
				{
					int type = m_sda.get_IntProp(hvo, LexEntryRefTags.kflidRefType);
					if (type != LexEntryRefTags.krtVariant)
						return;
				}
				if ((vwenv as XmlBrowseViewBaseVc.ItemsCollectorEnv).HvosCollectedInCell.Count == 0)
					(vwenv as XmlBrowseViewBaseVc.ItemsCollectorEnv).HvosCollectedInCell.Add(0);
			}
		}

		private void OutputItemNumber(IVwEnv vwenv, ITsString tssNumber)
		{
			if (vwenv is ConfiguredExport)
				(vwenv as ConfiguredExport).OutputItemNumber(tssNumber, m_numberPartRef);
			else
				vwenv.AddString(tssNumber);
		}
	}
}
