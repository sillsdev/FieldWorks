// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlVcDisplayVec.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
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
			var xattrSeparator = listDelimitNode.Attributes["sep"];
			var fFirst = true; // May actually mean first non-empty.
			WrapParagraphDisplayCommand tempCommand = null;
			int tempId = 0;
			if (fShowAsParagraphsInInnerPile || fShowAsParagraphsInDivInPara)
			{
				// We make a temporary command object, whose purpose is to wrap the paragraphs we want to create
				// here for each item and any embellishments we add INSIDE the display of each individual item.
				// This ensures that if we break those paragraphs (e.g., for subentries of a sense), all the
				// paragraphs for the outer object are still correctly nested inside the item.
				// This is especially important for XML export, where if the paragraph and object elements are
				// not correctly nested, we don't even have valid XML.
				// The command object's lifetime is only as long as this XmlVcDisplayVec,
				// because it references this XmlVcDisplayVec object and even updates some of its member variables.
				// I'm not sure it really has to do that, but I was trying to make a minimal change to the code
				// that had to be wrapped in the command object so it could be invoked by the AddObj call.
				// It is possible we could make a more permanent and reusable command object, but it would
				// require extensive analysis of at least how the member variables it modifies are used to make
				// sure it is safe. At this point I'm going for the safest change I can. This whole area of the
				// system is likely to be rewritten sometime.
				tempCommand = new WrapParagraphDisplayCommand(childFrag, this, sParaStyle, tssBefore,
					listDelimitNode, fNumber, fDelayNumber, xaNum, ttpNum);
				tempId = m_viewConstructor.GetId(tempCommand);
				tempCommand.DelayedNumber = tssDelayedNumber;
			}
			for (var ihvo = 0; ihvo < chvo; ++ihvo)
			{
				if (IsExcluded(exclude, ihvo, rghvo))
					continue;

				if (fCheckForEmptyItems && IsItemEmpty(rghvo[ihvo], childFrag))
					continue;

				Debug.Assert(ihvo < rghvo.Length);
				if (fShowAsParagraphsInInnerPile || fShowAsParagraphsInDivInPara)
				{
					// Passing tempId causes it to use the tempCommand we made above, which does
					// much the same as the code in the else branch, except for wrapping the content
					// of the object in a paragraph (and not inserting separators). In general we want to keep
					// them the same, which is why the common code is wrapped in the AddItemEmbellishments
					// method. The critical thing is that the paragraph must be part of the object,
					// even though it is caused by the listDelimitNode attributes rather than by those
					// of the XML that directly controls the display of the object.
					m_vwEnv.AddObj(rghvo[ihvo], m_viewConstructor, tempId);
				}
				else
				{
					// not part of add embellishments because never used with either showAsParagraphs option
					AddSeparatorIfNeeded(fFirst, xattrSeparator, listDelimitNode, wsEng);
					AddItemEmbellishments(listDelimitNode, fNumber, rghvo[ihvo], ihvo, xaNum, ttpNum, fDelayNumber, ref tssDelayedNumber);
					m_vwEnv.AddObj(rghvo[ihvo], m_viewConstructor, childFrag);
					fFirst = false;
				}

				if (fFirstOnly)
					break;
			} // end of sequence 'for' loop
			if (tempCommand != null)
			{
				tssDelayedNumber = tempCommand.DelayedNumber; // recover the end result of how it was modified.
				m_viewConstructor.RemoveCommand(tempCommand, tempId);
			}

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

		private void AddItemEmbellishments(XmlNode listDelimitNode, bool fNumber, int hvo, int ihvo, XmlAttribute xaNum, ITsTextProps ttpNum, bool fDelayNumber, ref ITsString tssDelayedNumber)
		{
			// add the numbering if needed.
			if (fNumber)
			{
				var sTag = CalculateAndFormatSenseLabel(hvo, ihvo, xaNum.Value);

				ITsStrBldr tsb = m_cache.TsStrFactory.GetBldr();
				tsb.Replace(0, 0, sTag, ttpNum);
				ITsString tss = tsb.GetString();
				m_numberPartRef = listDelimitNode;
				AddNumberingNowOrDelayed(fDelayNumber, tss, out tssDelayedNumber);
			}
		}

		private ITsString SetBeforeString(XmlNode specialAttrsNode, XmlNode listDelimitNode)
		{
			ITsString tssBefore = null;
			string sBefore = XmlUtils.GetLocalizedAttributeValue(listDelimitNode, "before", null);
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

		internal static bool SetNumberFlagIncludingSingleOption(XmlNode listDelimitNode, int chvo, out XmlAttribute xaNum)
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
		/// <param name="hvo">the sense itself</param>
		/// <param name="ihvo">A sequence number or index (of hvo in its containing sequence).</param>
		/// <param name="sTag">The sequence number format to apply</param>
		/// <returns>The format filled in with the sequence number</returns>
		internal string CalculateAndFormatSenseLabel(int hvo, int ihvo, string sTag)
		{
			var sNum = "";
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
						{
							var item = m_objRepo.GetObject(hvo);
							if (item is ILexSense)
							{
								// Need to use a virtual property which can be overridden by DictionaryPublicationDecorator
								// So the numbering excludes any hidden senses.
								var senseOutlineFlid = item.Cache.MetaDataCacheAccessor.GetFieldId2(LexSenseTags.kClassId, "LexSenseOutline", false);
								sNum = m_sda.get_StringProp(item.Hvo, senseOutlineFlid).Text;
							}
							else
							{
								// Not sure this can ever happen (since the method name indicates is it supposed to make
								// labels for senses), but it seemed safest to keep the old generic behavior for any other cases.
								sNum = m_cache.GetOutlineNumber(item, false, true);
							}
						}
						break;
					case 'z':
						sNum = GetOutlineStyle2biv(hvo, ihvo);
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

		private string GetOutlineStyle2biv(int hvo, int ihvo)
		{
			// Top-level: Arabic numeral.
			// Second-level: lowercase letter.
			// Third-level (and lower): lowercase Roman numeral.
			string sNum; // return result string
			var sOutline = m_cache.GetOutlineNumber(m_objRepo.GetObject(hvo), false, true);
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
				command.Caller, m_viewConstructor.WsForce);
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

		/// <summary>
		/// This class exists to invoke a display of the same object using a frag ID specified in its constructor, but wrapped in
		/// an open and close paragraph. We need the Open/Close para to be INSIDE the main open/close object, so we need another
		/// level of fragId. We also need a bunch of other data which the main XmlVcDisplayVec.Display method figures out
		/// for other things we might want to do inside the paragraph. Since this is a temporary object that just exists
		/// for the lifetime of the one loop, we just pass it all in.
		/// Note that, unlike most DisplayCommand subclasses, we deliberately do NOT override Equals. In case anything goes
		/// wrong with removing one of these from the dictionary, it still won't get reused by any other instance of XmlVcDisplayVec.
		/// </summary>
		internal class WrapParagraphDisplayCommand : DisplayCommand
		{
			/// <summary>
			/// Make one.
			/// </summary>
			/// <param name="wrappedFragId"></param>
			/// <param name="creator"></param>
			/// <param name="paraStyle"></param>
			/// <param name="before"></param>
			/// <param name="listDelimitNode"></param>
			/// <param name="number"></param>
			/// <param name="delayNumber"></param>
			/// <param name="xaNum"></param>
			/// <param name="ttpNum"></param>
			public WrapParagraphDisplayCommand(int wrappedFragId, XmlVcDisplayVec creator, string paraStyle, ITsString before, XmlNode listDelimitNode,
				bool number, bool delayNumber, XmlAttribute xaNum, ITsTextProps ttpNum)
			{
				WrappedFragId = wrappedFragId;
				m_creator = creator;
				m_paraStyle = paraStyle;
				m_tssBefore = before;
				m_listDelimitNode = listDelimitNode;
				m_numberItems = number;
				m_delayNumber = delayNumber;
				m_xaNum = xaNum;
				m_ttpNum = ttpNum;
			}
			private readonly int WrappedFragId;
			private readonly XmlVcDisplayVec m_creator;
			private string m_paraStyle;
			private ITsString m_tssBefore;
			private XmlNode m_listDelimitNode;
			private bool m_numberItems;
			private bool m_delayNumber;
			public ITsString DelayedNumber;
			private XmlAttribute m_xaNum;
			private ITsTextProps m_ttpNum;

			internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
			{
				int level = vwenv.EmbeddingLevel;
				int hvoDum, tag, ihvo;
				vwenv.GetOuterObject(level - 2, out hvoDum, out tag, out ihvo);
				m_creator.SetupParagraph(m_paraStyle, m_tssBefore, m_listDelimitNode);
				m_creator.AddItemEmbellishments(m_listDelimitNode, m_numberItems, hvo, ihvo, m_xaNum, m_ttpNum, m_delayNumber, ref DelayedNumber);

				// This is the display of the object we wanted to wrap the paragraph etc around.
				// We want to produce an effect rather like
				// vwenv.AddObj(hvo, m_creator.m_viewConstructor, WrappedFragId);
				// except we do NOT want to open/close an object in the VC, since we are already inside the addition of this object.
				m_creator.m_viewConstructor.Display(vwenv, hvo, WrappedFragId);

				// Close Paragraph if displaying paragraphs
				vwenv.CloseParagraph();
			}
		}
	}
}
