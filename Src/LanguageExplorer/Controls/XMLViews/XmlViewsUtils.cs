// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using LanguageExplorer.Filters;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Utility functions for XmlViews.
	/// Some of these may eventually migrate to a more general Utils if relevant.
	/// </summary>
	public class XmlViewsUtils
	{
		/// <summary>
		/// Current writing system id being used in multilingual fragment.
		/// Some methods that refer to this variable are static, so it must be static also.
		/// REVIEW (EberhardB/TimS): this probably won't work with different databases that have
		/// different default ws!
		/// </summary>
		protected static CoreWritingSystemDefinition s_qwsCurrent;
		/// <summary />
		protected static int s_cwsMulti;    // count of current ws alternatives.
		/// <summary />
		protected static string s_sMultiSep;
		/// <summary />
		protected static bool s_fMultiFirst;

		// static methods only, no sense making one.
		private XmlViewsUtils()
		{
		}

		/// <summary>
		/// looks up plural form alternative first for given flid, secondly for its destination class.
		/// </summary>
		/// <returns>true if we found an alternate form. false if titleStr is null or in *{key}* format.</returns>
		public static bool TryFindPluralFormFromFlid(IFwMetaDataCache mdc, int owningFlid, out string titleStr)
		{
			// first see if we can find an expanded name for the name of a flid.
			var flidName = mdc.GetFieldName(owningFlid);
			if (!string.IsNullOrEmpty(flidName))
			{
				if (TryFindString("AlternativeTitles", flidName, out titleStr))
				{
					return true;
				}
			}
			// secondly, see if we can find the plural form for the destination class.
			var dstClass = mdc.GetDstClsId(owningFlid);
			return TryFindPluralFormFromClassId(mdc, dstClass, out titleStr);
		}

		/// <summary />
		/// <returns>true if we found an alternate form. false if titleStr is null or in *{ClassName}* format.</returns>
		public static bool TryFindPluralFormFromClassId(IFwMetaDataCache mdc, int clsId, out string titleStr)
		{
			titleStr = null;
			return clsId != 0 && TryFindString("AlternativeTitles", $"{mdc.GetClassName(clsId)}-Plural", out titleStr);
		}

		/// <summary />
		public static bool TryFindString(string group, string key, out string result)
		{
			result = StringTable.Table.GetString(key, @group);
			return FoundStringTableString(key, result);
		}

		/// <summary>
		/// determine if string table query gave a significant result (i.e. not *{key}* format).
		/// </summary>
		private static bool FoundStringTableString(string key, string result)
		{
			return !string.IsNullOrEmpty(result) && !(result.StartsWith("*") && result == ("*" + key + "*"));
		}

		/// <summary>
		/// If any attributes of input (or its children) are of the form "$param=default",
		/// generate a complete copy of input in which "$param=default" is replaced with
		/// "default", and return it. Otherwise just return input.
		/// </summary>
		public static XElement CopyWithParamDefaults(XElement input)
		{
			if (!HasParam(input))
			{
				return input;
			}
			var result = input.Clone();

			var replacer = new ReplaceParamWithDefault();
			XmlUtils.VisitAttributes(result, replacer);

			return result;
		}

		/// <summary>
		/// Copies the replacing param default.
		/// </summary>
		public static XElement CopyReplacingParamDefault(XElement input, string paramId, string val)
		{
			var result = input.Clone();

			var replacer = new ReplaceParamDefault(paramId, val);
			XmlUtils.VisitAttributes(result, replacer);

			return result;
		}

		/// <summary>
		/// Find the value of the first parameter-like attribute value "$param=default"
		/// </summary>
		public static bool HasParam(XElement input)
		{
			var tfp = new TestForParameter();
			XmlUtils.VisitAttributes(input, tfp);
			return tfp.HasAttribute;
		}

		/// <summary>
		/// Finds the params.
		/// </summary>
		public static string[] FindParams(XElement input)
		{
			var ap = new AccumulateParameters();
			XmlUtils.VisitAttributes(input, ap);
			return ap.Parameters.ToArray();
		}

		/// <summary>
		/// Search the node for an element with an attribute that has a value that looks like "$ws=..." and return
		/// the "...".
		/// </summary>
		public static string FindWsParam(XElement node)
		{
			var paramList = FindParams(node);
			foreach (var s in paramList)
			{
				// Enhance JohnT: may handle other parameters, and show something here for them.
				if (s.StartsWith(StringServices.WsParamLabel))
				{
					return s.Substring(StringServices.WsParamLabel.Length);
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Find the index of the node in nodes that has value attVal for attribute attName.
		/// Return -1 if not found.
		/// </summary>
		public static int FindIndexOfAttrVal(List<XElement> nodes, string attName, string attVal)
		{
			var index = 0;
			foreach (var node in nodes)
			{
				var sAttr = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(node, attName, null));
				if (sAttr == attVal)
				{
					return index;
				}
				index++;
			}
			return -1;
		}


		/// <summary>
		/// Go through possible attributes to match, and return true when the ones that exist match
		/// the same attributes in another node.
		/// </summary>
		/// <param name="colSpec1"></param>
		/// <param name="colSpec2"></param>
		/// <param name="possibleAttributesToMatch">the attributes to try to match, in the order we try to match them.
		/// if this routine returns <c>true</c> then this the top queue element will be the attribute we matched on.</param>
		/// <returns></returns>
		public static bool TryMatchExistingAttributes(XElement colSpec1, XElement colSpec2, ref Queue<string> possibleAttributesToMatch)
		{
			if (colSpec1 == colSpec2)
			{
				return true;
			}
			if (possibleAttributesToMatch == null || possibleAttributesToMatch.Count == 0)
			{
				// we've compared all the possible attributes given, and haven't found a mismatch.
				return true;
			}
			var attribute = possibleAttributesToMatch.Peek();
			var attrVal1 = XmlUtils.GetOptionalAttributeValue(colSpec1, attribute);
			var attrVal2 = XmlUtils.GetOptionalAttributeValue(colSpec2, attribute);
			possibleAttributesToMatch.Dequeue();
			return attrVal1 == attrVal2 && TryMatchExistingAttributes(colSpec1, colSpec2, ref possibleAttributesToMatch);
		}

		/// <summary>
		/// Return a string such that ICU alphabetic comparison of the strings
		/// will produce the same results as numberic comparison of the values.
		/// For positive integers the string is a recognizeable representation of
		/// the number (with extra leading zeros); for negative, it is not
		/// recognizeable but works.
		/// </summary>
		public static string AlphaCompNumberString(int val)
		{
			// We want to produce things like
			// 0000000
			// 0000001
			// 0000010
			// Then, negative numbers should sort with more negative ones smaller.
			// We do this by a similar approach, with leading minus (fortunately smaller
			// alphabetically than zero), followed by a similar trick applied to int.Max + val + 1
			// -0000001
			// -0000002
			// However, if we pad with zeros as above, "0" as a pattern matches everything!
			// Since that's a fairly likely pattern, we pad with "/" (less than 0, more than '-'.
			// Bizarre matches are still possible, but much less likely.
			if (val >= 0)
			{
				return AlphaCompPosNumString(val);
			}
			// We add 1 here because otherwise we pass a negative value to the other method for int.MinValue
			return "-" + AlphaCompPosNumString(int.MaxValue + val + 1);
		}

		/// <summary>
		/// Alphas the comp pos num string.
		/// </summary>
		/// <remarks>
		/// Like AlphaCompNumberString, but for positive integers
		/// </remarks>
		public static string AlphaCompPosNumString(int val)
		{
			Debug.Assert(val >= 0);
			var maxDigits = int.MaxValue.ToString().Length;
			var sVal = val.ToString();
			if (sVal.Length == maxDigits)
			{
				return sVal;
			}
			return new string('/', maxDigits - sVal.Length) + sVal;
		}

		/// <summary>
		/// Return a string such that ICU alphabetic comparison of the strings
		/// will produce the same results as DateTime.Compare of the values.
		/// </summary>
		public static string DateTimeCompString(DateTime dt)
		{
			// "u" is: 2000-08-17 23:32:32Z
			return dt.ToString("u", DateTimeFormatInfo.InvariantInfo);
		}

		/// <summary>
		/// Find the index of the node in nodes that 'matches' the target node.
		/// Return -1 if not found.
		/// </summary>
		public static int FindIndexOfMatchingNode(IEnumerable<XElement> nodes, XElement target)
		{
			return XmlUtils.FindIndexOfMatchingNode(nodes, target);
		}

		/// <summary>
		/// Finds the node with attr val.
		/// </summary>
		public static XElement FindNodeWithAttrVal(List<XElement> nodes, string attName, string attVal)
		{
			var index = FindIndexOfAttrVal(nodes, attName, attVal);
			return index == -1 ? null : nodes[index];
		}

		/// <summary>
		/// Answer a list containing, for each node in selectNodes, the item in sourceNodes
		/// that has the same value for the attribute attName.
		/// </summary>
		public static List<XElement> CorrespondingItems(List<XElement> sourceNodes, List<XElement> selectNodes, string attName)
		{
			var result = new List<XElement>(selectNodes.Count);
			foreach(var node in selectNodes)
			{
				var attVal = XmlUtils.GetMandatoryAttributeValue(node, attName);
				foreach(var node1 in sourceNodes)
				{
					if (XmlUtils.GetMandatoryAttributeValue(node1, attName) == attVal)
					{
						result.Add(node1);
						break;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Given one of the original list items, and the spec of the column we want to sort by,
		/// add to collector whatever ManyOnePathSortItems are appropriate.
		/// </summary>
		public static void CollectBrowseItems(int hvo, XElement colSpec, ArrayList collector, IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts)
		{
			var topNode = XmlBrowseViewBaseVc.GetColumnNode(colSpec, hvo, sda, layouts);

			// Todo: handle various cases here, mostly drill-down to <seq> or <obj>
			CollectBrowseItems(hvo, topNode, collector, mdc, sda, layouts, null, null, null);
		}

		/// <summary>
		/// Main (recursive) part of CollectBrowseItems. Given that hvo is to be displayed using node,
		/// figure what objects to put in the list.
		/// </summary>
		private static void CollectBrowseItems(int hvo, XElement node, ArrayList collector, IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts, XElement caller, int[] hvos, int[] flids)
		{
			switch (node.Name.LocalName)
			{
				case "obj":
					{
						var clsid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
						var flid = mdc.GetFieldId2(clsid, XmlUtils.GetMandatoryAttributeValue(node, "field"), true);
						var hvoDst = sda.get_ObjectProp(hvo, flid);
						if (hvoDst == 0)
						{
							// We want a row, even though it's blank for this column.
							collector.Add(new ManyOnePathSortItem(hvo, hvos, flids));
							return;
						}
						// At this point we have to mimic the process that XmlVc uses to come up with the
						// node that will be used to process the destination item.
						var dstNode = GetNodeForRelatedObject(hvoDst, caller, node, layouts, sda);
						if (dstNode == null)
						{
							// maybe an old-style "frag" element? Anyway, we can't do anything smart,
							// so just insert the original object.
							collector.Add(new ManyOnePathSortItem(hvo, hvos, flids));
							return;
						}
						CollectBrowseItems(hvoDst, dstNode, collector, mdc, sda, layouts, null, AppendInt(hvos, hvo), AppendInt(flids, flid));
					}
					break;
				case "seq":
					{
						// very like "obj" except for the loop. How could we capture this?
						var clsid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
						var flid = mdc.GetFieldId2(clsid, XmlUtils.GetMandatoryAttributeValue(node, "field"), true);
						var chvo = sda.get_VecSize(hvo, flid);
						if (chvo == 0)
						{
							// We want a row, even though it's blank for this column.
							collector.Add(new ManyOnePathSortItem(hvo, hvos, flids));
							return;
						}
						for (var ihvo = 0; ihvo < chvo; ihvo++)
						{
							var hvoDst = sda.get_VecItem(hvo, flid, ihvo);
							// At this point we have to mimic the process that XmlVc uses to come up with the
							// node that will be used to process the destination item.
							var dstNode = GetNodeForRelatedObject(hvoDst, caller, node, layouts, sda);
							if (dstNode == null)
							{
								if (ihvo == 0)
								{
									// maybe an old-style "frag" element? Anyway, we can't do anything smart,
									// so just insert the original object.
									collector.Add(new ManyOnePathSortItem(hvo, hvos, flids));
									return;
								}
								// if this happens and it's not the first object, we have a funny mixture of modes.
								// As a fall-back, skip this object.
								continue;
							}
							CollectBrowseItems(hvoDst, dstNode, collector, mdc, sda, layouts, null, AppendInt(hvos, hvo), AppendInt(flids, flid));
						}
					}
					break;
				case "span":
				case "para":
				case "div":
				case "concpara":
				case "innerpile":
				case "column":
				// Review JohnT: In XmlVc, "part" is the one thing that calls ProcessChildren with non-null caller.
				// this should make some difference here, but I can't figure what yet, or come up with a test that fails.
				case "part":
				case "layout":
					// These are grouping nodes. In general this terminates things. However, if there is only
					// one thing embedded apart from comments and properties, we can proceed.
					var mainChild = FindMainChild(node);
					if (mainChild == null)
					{
						// no single non-trivial child, keep our current object
						collector.Add(new ManyOnePathSortItem(hvo, hvos, flids));
						return;
					}
					// Recurse with same object, but process the 'main child'.
					CollectBrowseItems(hvo, mainChild, collector, mdc, sda, layouts, caller, hvos, flids);
					break;

				default:
					collector.Add(new ManyOnePathSortItem(hvo, hvos, flids));
					break;
			}
		}

		/// <summary>
		/// Finds the main child.
		/// </summary>
		private static XElement FindMainChild(XElement node)
		{
			XElement mainChild = null;
			foreach (var child in node.Elements())
			{
				if (child.Name == "properties")
				{
					continue;
				}
				if (mainChild != null)
				{
					// multiple main children, stop here.
					return null;
				}
				mainChild = child;
			}
			return mainChild;
		}

		/// <summary>
		/// The node argument is an "obj" or "seq" element, and caller (if not null) is a part
		/// element that invoked the node and may override its "layout" attribute.
		/// Apply the same logic used by XmlVc to determine the node that will be used
		/// to display the destination object hvoDst
		/// </summary>
		private static XElement GetNodeForRelatedObject(int hvoDst, XElement caller, XElement node, LayoutCache layouts, ISilDataAccess sda)
		{
			if (XmlUtils.GetOptionalAttributeValue(node, "frag") != null)
			{
				return null; // old approach not handled.
			}
			// (frag="true" is also used to prevent splitting entry when sorting on gloss or
			// allomorph in Find Entries dialog display.  Part of fixing LT-10293.)
			var layoutName = XmlVc.GetLayoutName(node, caller);
			var layoutNode = XmlVc.GetNodeForPart(hvoDst, layoutName, true, sda, layouts);
			return XmlVc.GetDisplayNodeForChild(layoutNode, node, layouts);
		}

		private static int[] AppendInt(int[] sofar, int add)
		{
			if (sofar == null)
			{
				return new[] {add};
			}
			var result = new List<int>(sofar)
			{
				add
			};
			return result.ToArray();
		}

		// Concatenate two arrays of strings. If either is empty return the other.
		// (If both are null returns null.)
		private static string[] Concatenate(string[] first, string[] second)
		{
			if (first == null || first.Length == 0)
			{
				return second;
			}
			if (second == null || second.Length == 0)
			{
				return first;
			}
			var result = new List<string>(first);
			result.AddRange(second);
			return result.ToArray();
		}

		/// <summary>
		/// Return the concatenation of all the input strings as a single string.
		/// </summary>
		public static string[] Assemble(string[] items)
		{
			if (items == null)
			{
				return new string[0];
			}
			if (items.Length <= 1)
			{
				return items;
			}
			var bldr = new StringBuilder();
			foreach (var s in items)
			{
				bldr.Append(s);
			}
			return new[] {bldr.ToString()};
		}

		/// <summary>
		/// Returns an array of string values (keys) for the objects under the layout child nodes.
		/// </summary>
		internal static string[] ChildKeys(LcmCache lcmCache, ISilDataAccess sda, XElement layout, int hvo, LayoutCache layoutCache, XElement caller, int wsForce)
		{
			return layout.Elements().Aggregate<XElement, string[]>(null, (current, child) => Concatenate(current, StringsFor(lcmCache, sda, child, hvo, layoutCache, caller, wsForce)));
		}

		private static void AddSeparator(ref string item, int ichInsert, XElement layout)
		{
			var separator = XmlUtils.GetOptionalAttributeValue(layout, "sep");
			if (string.IsNullOrEmpty(separator))
			{
				return;
			}
			var fCheckForEmptyItems = XmlUtils.GetOptionalBooleanAttributeValue(layout, "checkForEmptyItems", false);
			if (item == null || ichInsert < 0 || ichInsert > item.Length || fCheckForEmptyItems && item.Length == 0)
			{
				return;
			}
			item = item.Insert(ichInsert, separator);
		}


		private static string[] AssembleChildKeys(LcmCache lcmCache, ISilDataAccess sda, XElement layout, int hvo, LayoutCache layoutCache, XElement caller, int wsForce)
		{
			return Assemble(ChildKeys(lcmCache, sda, layout, hvo, layoutCache, caller, wsForce));
		}

		/// <summary>
		/// This is a simplified version of XmlVc.GetFlid.
		/// It does not look for a flid attr, nor try to cache the result.
		/// It looks for a "field" property, and optionally a "class" one, and uses them
		/// (or the class of hvo, if "class" is missing) to figure the flid.
		/// Virtual properties are assumed already created.
		/// </summary>
		private static int GetFlid(ISilDataAccess sda, XElement frag, int hvo)
		{
			var stClassName = XmlUtils.GetOptionalAttributeValue(frag,"class");
			var stFieldName = XmlUtils.GetMandatoryAttributeValue(frag,"field");
			if (string.IsNullOrEmpty(stClassName))
			{
				var clid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
				return sda.MetaDataCache.GetFieldId2(clid, stFieldName, true);
			}
			return sda.MetaDataCache.GetFieldId(stClassName, stFieldName, true);
		}

		// Utility function to get length of an array variable which might be null (return 0 if so).
		static int GetArrayLength(string[] items)
		{
			return items?.Length ?? 0;
		}

		internal static string DisplayWsLabel(CoreWritingSystemDefinition ws, LcmCache cache)
		{
			if (ws == null)
			{
				return string.Empty;
			}
			return (ws.Abbreviation ?? ws.Id) ?? XMLViewsStrings.ksUNK + " ";
		}

		private static string AddMultipleAlternatives(LcmCache cache, ISilDataAccess sda, IEnumerable<int> wsIds, int hvo, int flid, XElement frag)
		{
			var sep = XmlUtils.GetOptionalAttributeValue(frag, "sep", null);
			var fLabel = XmlUtils.GetOptionalBooleanAttributeValue(frag, "showLabels", false); // true to 'separate' using multistring labels.
			var result = string.Empty;
			var fFirst = true;
			foreach (var ws in wsIds)
			{
				var val = sda.get_MultiStringAlt(hvo, flid, ws).Text;
				if (string.IsNullOrEmpty(val))
				{
					continue; // doesn't even count as 'first'
				}
				if (fLabel)
				{
					var wsObj = cache.ServiceLocator.WritingSystemManager.Get(ws);
					result += DisplayWsLabel(wsObj, cache);
				}
				if (fFirst)
				{
					fFirst = false;
				}
				else if (sep != null)
				{
					result = result + sep;
				}
				result += val;
			}
			return result;
		}

		internal static string[] AddStringFromOtherObj(XElement frag, int hvoTarget, LcmCache cache, ISilDataAccess sda)
		{
			var flid = XmlVc.GetFlid(frag, hvoTarget, sda);
			var itype = (CellarPropertyType)sda.MetaDataCache.GetFieldType(flid);
			switch (itype)
			{
				case CellarPropertyType.Unicode:
					return new[] { sda.get_UnicodeProp(hvoTarget, flid) };
				case CellarPropertyType.String:
					return new[] { sda.get_StringProp(hvoTarget, flid).Text };
				default:
					var wsid = 0;
					var sep = string.Empty;
					if (s_cwsMulti > 1)
					{
						var sLabelWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
						if (sLabelWs != null && sLabelWs == "current")
						{
							sep = DisplayMultiSep(frag) + DisplayWsLabel(s_qwsCurrent, cache);
							wsid = s_qwsCurrent.Handle;
						}
					}
					if (wsid == 0)
					{
						wsid = WritingSystemServices.GetWritingSystem(cache, FwUtils.ConvertElement(frag), null, WritingSystemServices.kwsAnal).Handle;
					}
					return new[] { sep, sda.get_MultiStringAlt(hvoTarget, flid, wsid).Text };
			}
		}

		internal static string DisplayMultiSep(XElement frag)
		{
			var sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
			if (sWs == null || sWs != "current")
			{
				return string.Empty;
			}
			if (!s_fMultiFirst && !string.IsNullOrEmpty(s_sMultiSep))
			{
				return s_sMultiSep;
			}
			s_fMultiFirst = false;
			return string.Empty;
		}

		/// <summary>
		/// Returns an array of string values (keys) for the objects under this layout node.
		/// </summary>
		public static string[] StringsFor(LcmCache lcmCache, ISilDataAccess sda, XElement layout, int hvo, LayoutCache layoutCache, XElement caller, int wsForce)
		{
			// Some nodes are known to be uninteresting.
			if (XmlVc.CanSkipNode(layout))
			{
				return new string[0]; // don't know how to sort, treat as empty key.
			}

			switch (layout.Name.LocalName)
			{
				case "string":
				{
					var hvoTarget = hvo;
					XmlVc.GetActualTarget(layout, ref hvoTarget, sda);	// modify the hvo if needed
					if (hvo != hvoTarget)
					{
						return AddStringFromOtherObj(layout, hvoTarget, lcmCache, sda);
					}
					var flid = GetFlid(sda, layout, hvo);
					if (wsForce != 0)
					{
						// If we are forcing a writing system, and it's a multistring, get the forced alternative.
						var itype = sda.MetaDataCache.GetFieldType(flid);
						itype = itype & (int)CellarPropertyTypeFilter.VirtualMask;
						switch (itype)
						{
							case (int) CellarPropertyType.MultiUnicode:
							case (int) CellarPropertyType.MultiString:
								if (wsForce < 0)
								{
									int wsActual;
									var tss = WritingSystemServices.GetMagicStringAlt(lcmCache, sda, wsForce, hvo, flid, true, out wsActual);
									return new[] {tss == null ? "" : tss.Text };
								}
								return new[] {sda.get_MultiStringAlt(hvo, flid, wsForce).Text};
						}
					}
					bool fFoundType;
					var strValue = lcmCache.GetText(hvo, flid, FwUtils.ConvertElement(layout), out fFoundType);
					if (fFoundType)
					{
						return new[] {strValue};
					}

					throw new Exception($"Bad property type ({strValue} for hvo {hvo} found for string property {flid} in {layout}");
				}
				case "configureMlString":
				{
					var flid = GetFlid(sda, layout, hvo);
					// The Ws info specified in the part ref node
					var wsIds = WritingSystemServices.GetAllWritingSystems(lcmCache, FwUtils.ConvertElement(caller), null, hvo, flid);
					if (wsIds.Count == 1)
					{
						var strValue = sda.get_MultiStringAlt(hvo, flid, wsIds.First()).Text;
						return new[] {strValue};
					}
					return new[] {AddMultipleAlternatives(lcmCache, sda, wsIds, hvo, flid, caller)};
				}
				case "multiling":
					return ProcessMultiLingualChildren(lcmCache, sda, layout, hvo, layoutCache, caller, wsForce);
				case "layout":
					// "layout" can occur when GetNodeToUseForColumn returns a phony 'layout'
					// formed by unifying a layout with child nodes. Assemble its children.
					// (arguably, we should treat that like div if current flow is a pile.
					// but we can't tell that and it rarely makes a difference.)
				case "para":
				case "span":
				{
					return AssembleChildKeys(lcmCache, sda, layout, hvo, layoutCache, caller, wsForce);
				}
				case "column":
					// top-level node for whole column; concatenate children as for "para"
					// if multipara is false, otherwise as for "div"
					if (XmlUtils.GetOptionalBooleanAttributeValue(layout, "multipara", false))
					{
						return ChildKeys(lcmCache, sda, layout, hvo, layoutCache, caller, wsForce);
					}
					return AssembleChildKeys(lcmCache, sda, layout, hvo, layoutCache, caller, wsForce);

				case "part":
				{
					var partref = XmlUtils.GetOptionalAttributeValue(layout, "ref");
					if (partref == null)
					{
						return ChildKeys(lcmCache, sda, layout, hvo, layoutCache, caller, wsForce); // an actual part, made up of its pieces
					}
					var part = XmlVc.GetNodeForPart(hvo, partref, false, sda, layoutCache);
					// This is the critical place where we introduce a caller. The 'layout' is really a 'part ref' which is the
					// 'caller' for all embedded nodes in the called part.
					return StringsFor(lcmCache, sda, part, hvo, layoutCache, layout, wsForce);
				}
				case "div":
				case "innerpile":
				{
					// Concatenate keys for child nodes (as distinct strings)
					return ChildKeys(lcmCache, sda, layout, hvo, layoutCache, caller, wsForce);
				}
				case "obj":
				{
					// Follow the property, get the object, look up the layout to use,
					// invoke recursively.
					var flid = GetFlid(sda, layout, hvo);
					var hvoTarget = sda.get_ObjectProp(hvo, flid);
					if (hvoTarget == 0)
					{
						break; // return empty key
					}
					var targetLayoutName = XmlUtils.GetOptionalAttributeValue(layout, "layout"); // uses 'default' if missing.
					var layoutTarget = GetLayoutNodeForChild(sda, hvoTarget, flid, targetLayoutName, layout, layoutCache);
					if (layoutTarget == null)
					{
						break;
					}
					return ChildKeys(lcmCache, sda, layoutTarget, hvoTarget, layoutCache, caller, wsForce);
				}
				case "seq":
				{
					// Follow the property. For each object, look up the layout to use,
					// invoke recursively, concatenate
					var flid = GetFlid(sda, layout, hvo);
					int[] contents;
					var ctarget = sda.get_VecSize(hvo, flid);
					using (var arrayPtr = MarshalEx.ArrayToNative<int>(ctarget))
					{
						int chvo;
						sda.VecProp(hvo, flid, ctarget, out chvo, arrayPtr);
						contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
					}

					string[] result = null;
					var targetLayoutName = XmlVc.GetLayoutName(layout, caller); // also allows for finding "param" attr in caller, if not null
					var i = 0;
					foreach (var hvoTarget in contents)
					{
						var prevResultLength = GetArrayLength(result);
						var layoutTarget = GetLayoutNodeForChild(sda, hvoTarget, flid, targetLayoutName, layout, layoutCache);
						if (layoutTarget == null)
						{
							continue; // should not happen, but best recovery we can make
						}
						result = Concatenate(result, ChildKeys(lcmCache, sda, layoutTarget, hvoTarget, layoutCache, caller, wsForce));
						// add a separator between the new childkey group and the previous childkey group
						if (i > 0 && prevResultLength != GetArrayLength(result) && prevResultLength > 0)
						{
							var ichIns = 0;
							if (result[prevResultLength - 1] != null)
							{
								ichIns = result[prevResultLength - 1].Length;
							}
							AddSeparator(ref result[prevResultLength - 1],  ichIns, layout);
						}
						++i;
					}

					return result;
				}
				case "choice":
				{
					foreach(var whereNode in layout.Elements())
					{
						if (whereNode.Name != "where")
						{
							if (whereNode.Name == "otherwise")
							{
								return StringsFor(lcmCache, sda, XmlUtils.GetFirstNonCommentChild(whereNode), hvo, layoutCache, caller, wsForce);
							}
							continue; // ignore any other nodes,typically comments
						}
						// OK, it's a where node.
						if (XmlVc.ConditionPasses(whereNode, hvo, lcmCache, sda, caller))
						{
							return StringsFor(lcmCache, sda, XmlUtils.GetFirstNonCommentChild(whereNode), hvo, layoutCache, caller, wsForce);
						}
					}
					break; // if no condition passes and no otherwise, return null.
				}
				case "if":
				{
					if (XmlVc.ConditionPasses(layout, hvo, lcmCache, sda, caller))
						return StringsFor(lcmCache, sda, XmlUtils.GetFirstNonCommentChild(layout), hvo, layoutCache, caller, wsForce);
					break;
				}
				case "ifnot":
				{
					if (!XmlVc.ConditionPasses(layout, hvo, lcmCache, sda, caller))
					{
						return StringsFor(lcmCache, sda, XmlUtils.GetFirstNonCommentChild(layout), hvo, layoutCache, caller, wsForce);
					}
					break;
				}
				case "lit":
				{
					var literal = string.Concat(layout.Elements());
					var sTranslate = XmlUtils.GetOptionalAttributeValue(layout, "translate", "");
					if (sTranslate.Trim().ToLower() != "do not translate")
					{
						literal = StringTable.Table.LocalizeLiteralValue(literal);
					}
					return new[] { literal };
				}
				case "int":
				{
					var flid = GetFlid(sda, layout, hvo);
					var val = sda.get_IntProp(hvo, flid);
					return new[] {AlphaCompNumberString(val)};
				}
				case "datetime":
				{
					var flid = GetFlid(sda, layout, hvo);
					var itype = (CellarPropertyType)sda.MetaDataCache.GetFieldType(flid);
					if (itype == CellarPropertyType.Time)
					{
						var dt = SilTime.GetTimeProperty(sda, hvo, flid);
						return new[] {DateTimeCompString(dt)};
					}
					var stFieldName = XmlUtils.GetMandatoryAttributeValue(layout, "field");
					throw new Exception($"Bad field type ({stFieldName} for hvo {hvo} found for {layout.Name} property {flid} in {layout}");
				}
				case "picture":
					// Treat a picture as a non-empty string for purposes of deciding whether something is empty.
					// This string seems as good as anything for other purposes.
					return new[] {"a picture"};
				default: // unknown or comment node, adds nothing
					Debug.Assert(false, "unrecognized XML node.");
					break;
			}
			return new string[0]; // don't know how to sort, treat as empty key.
		}

		/// <summary>
		/// Process a fragment's children against multiple writing systems.
		/// </summary>
		private static string[] ProcessMultiLingualChildren(LcmCache lcmCache, ISilDataAccess sda, XElement frag, int hvo, LayoutCache layoutCache, XElement caller, int wsForce)
		{
			var sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
			if (sWs == null)
			{
				return null;
			}

			Debug.Assert(s_qwsCurrent == null);
			Debug.Assert(s_cwsMulti == 0);
			string[] result = null;
			try
			{
				var wsIds = WritingSystemServices.GetAllWritingSystems(lcmCache, FwUtils.ConvertElement(frag), s_qwsCurrent, 0, 0);
				s_cwsMulti = wsIds.Count;
				if (s_cwsMulti > 1)
				{
					s_sMultiSep = XmlUtils.GetOptionalAttributeValue(frag, "sep");
				}
				s_fMultiFirst = true;
				foreach (var wsId in wsIds)
				{
					s_qwsCurrent = lcmCache.ServiceLocator.WritingSystemManager.Get(wsId);
					result = Concatenate(result, ChildKeys(lcmCache, sda, frag, hvo, layoutCache, caller, wsForce));
				}
			}
			finally
			{
				// Make sure these are reset, no matter what.
				s_qwsCurrent = null;
				s_cwsMulti = 0;
				s_sMultiSep = null;
				s_fMultiFirst = false;
			}
			return result;
		}

		private static XElement GetLayoutNodeForChild(ISilDataAccess sda, int hvoTarget, int flid, string targetLayoutName, XElement layout, LayoutCache layoutCache)
		{
			var layoutTarget = XmlVc.GetNodeForPart(hvoTarget, targetLayoutName, true, sda, layoutCache);
			if (layoutTarget == null)
			{
				layoutTarget = layout; // no layout looked up, use whatever children caller has
			}
			else if (layout.Elements().Any())
			{
				// got both a looked-up layout and child nodes overriding.
				layoutTarget = layoutTarget.Name == "layout" ? layoutCache.LayoutInventory.GetUnified(layoutTarget, layout) : layout;
			}
			return layoutTarget;
		}

		/// <summary>
		/// We want to display the object bvi.KeyObject, or one of its pathobjects, in a
		/// column specified by colSpec.
		/// Determine the hvo and XElement that we should use as the root for the cell.
		/// By default, we display the first object in the path, using the base node
		/// derived from the colSpec.
		/// However, if the colSpec begins with a path compatible with bvi.PathFlid(0),
		/// we can use bvi.PathObject(1) and the appropriate derived node.
		/// If all flids match we can use bvi.KeyObject itself.
		/// If collectOuterStructParts is non-null, it accumulates containing parts
		/// that are structural, like para, span, div.
		/// </summary>
		public static XElement GetNodeToUseForColumn(IManyOnePathSortItem bvi, XElement colSpec, IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts, out int hvo, List<XElement> collectOuterStructParts)
		{
			return GetDisplayCommandForColumn(bvi, colSpec, mdc, sda, layouts, out hvo, collectOuterStructParts).Node;
		}

		/// <summary>
		/// This returns a NodeDisplayCommand containing thd node for GetNodeToUseForColumn. However, it distinguishes whether to
		/// display the children of this node or the node itself by returning the appropriate kind of NodeDisplayCommand.
		/// </summary>
		public static NodeDisplayCommand GetDisplayCommandForColumn(IManyOnePathSortItem bvi, XElement colSpec, IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts, out int hvo, List<XElement> collectOuterStructParts)
		{
			var topNode = XmlBrowseViewBaseVc.GetColumnNode(colSpec, bvi.PathObject(0), sda, layouts);
			return GetDisplayCommandForColumn1(bvi, topNode, mdc, sda, layouts, 0, out hvo, collectOuterStructParts);
		}

		/// <summary>
		/// Recursive implementation method for GetDisplayCommandForColumn.
		/// </summary>
		static NodeDisplayCommand GetDisplayCommandForColumn1(IManyOnePathSortItem bvi, XElement node,
			IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts, int depth,
			out int hvo, List<XElement> collectOuterStructParts)
		{
			hvo = bvi.PathObject(depth); // default
			switch(node.Name.LocalName)
			{
				case "obj":
				case "seq":
				{
					// These two cases are the same here, because if the field matches, the object
					// that determines the next step comes from the bvi, not from one or many items
					// in the property.

					if (bvi.PathLength == depth)
					{
						// No more path, we display the final object using the node we've deduced is
						// appropriate for it.
						// (We could put this test outside the switch. But then we don't dig into
						// layout, para, span, etc elements at the end of the chain. It's more
						// consistent if we always dig as deep as we can.
						hvo = bvi.KeyObject;
						return new NodeDisplayCommand(node);
					}

					var clsid = sda.get_IntProp(bvi.PathObject(depth), CmObjectTags.kflidClass);
					var flid = mdc.GetFieldId2(clsid, XmlUtils.GetMandatoryAttributeValue(node, "field"), true);
					if (flid != bvi.PathFlid(depth))
					{
						return new NodeDisplayCommand(node); // different field, can't dig deeper.
					}
					var hvoDst = bvi.PathObject(depth + 1);
					// If the path object has been deleted, fall back to displaying whatever the property currently holds.
					if (sda.get_IntProp(hvoDst, CmObjectTags.kflidClass) == 0)
					{
						return new NodeDisplayCommand(node); // different field, can't dig deeper.
					}
					// At this point we have to mimic the process that XmlVc uses to come up with the
					// node that will be used to process the destination item.
					var dstNode = GetNodeForRelatedObject(hvoDst, null, node, layouts, sda);
					return GetDisplayCommandForColumn1(bvi, dstNode, mdc, sda, layouts, depth + 1, out hvo, collectOuterStructParts);
				}
				case "para":
				case "span":
				case "div":
				case "concpara":
				case "innerpile":
				{
					var mainChild = FindMainChild(node);
					if (mainChild == null)
					{
						return new NodeDisplayCommand(node); // can't usefully go further.
					}
					collectOuterStructParts?.Add(node);
					return GetDisplayCommandForColumn1(bvi, mainChild, mdc, sda, layouts, depth, out hvo, collectOuterStructParts);
				}
					// Review JohnT: In XmlVc, "part" is the one thing that calls ProcessChildren with non-null caller.
					// this should make some difference here, but I can't figure what yet, or come up with a test that fails.
					// We may need a "caller" argument to pass this down so it can be used in GetNodeForRelatedObject.
				case "part":
				{
					var layoutName = XmlUtils.GetOptionalAttributeValue(node, "ref");
					if (layoutName != null)
					{
						// It's actually a part ref, in a layout, not a part looked up by one!
						// Get the node it refers to, and make a command to process its children.
						var part = XmlVc.GetNodeForPart(hvo, layoutName, false, sda, layouts);
						return part != null ? new NodeChildrenDisplayCommand(part) : new NodeDisplayCommand(node);
					}

					// These are almost the same, but are never added to collectOuterStructParts.
					// Also, expecially in the case of 'layout', they may result from unification, and be meaningless
					// except for their children; in any case, the children are all we want to process.
					// This is the main reason we return a command, not just a node: this case has to return the subclass.
					var mainChild = FindMainChild(node);
					return mainChild == null ? new NodeChildrenDisplayCommand(node) : GetDisplayCommandForColumn1(bvi, mainChild, mdc, sda, layouts, depth, out hvo, collectOuterStructParts);
				}
				case "column":
				case "layout":
				{

					// These are almost the same as para, span, etc, but are never added to collectOuterStructParts.
					// Also, expecially in the case of 'layout', they may result from unification, and be meaningless
					// except for their children; in any case, the children are all we want to process.
					// This is the main reason we return a command, not just a node: this case has to return the subclass.
					var mainChild = FindMainChild(node);
					return mainChild == null ? new NodeChildrenDisplayCommand(node) : GetDisplayCommandForColumn1(bvi, mainChild, mdc, sda, layouts, depth, out hvo, collectOuterStructParts);
				}
				default:
					// If we can't find anything clever to do, we display the object at the
					// current level using the current node.
					return new NodeDisplayCommand(node);
			}
		}

		/// <summary>
		/// Convert the string found for a writing system to the appropriate integer code (hvo).
		/// </summary>
		public static int GetWsFromString(string wsParam, LcmCache cache)
		{
			if (wsParam == null)
			{
				return 0;
			}
			var wsContainer = cache.ServiceLocator.WritingSystems;
			switch (wsParam)
			{
				case "analysis":
					return wsContainer.DefaultAnalysisWritingSystem.Handle;
				case "vernacular":
					return wsContainer.DefaultVernacularWritingSystem.Handle;
				case "pronunciation":
					return wsContainer.DefaultPronunciationWritingSystem.Handle;
				case "reversal":
				{
					if (WritingSystemServices.CurrentReversalWsId > 0)
					{
						return WritingSystemServices.CurrentReversalWsId;
					}
					int wsmagic;
					return WritingSystemServices.InterpretWsLabel(cache, wsParam, wsContainer.DefaultAnalysisWritingSystem, 0, 0, null, out wsmagic);
				}
				case "":
					return wsContainer.DefaultAnalysisWritingSystem.Handle;		// Most likely value.
				default:
					return cache.ServiceLocator.WritingSystemManager.GetWsFromStr(wsParam);
			}
		}

		/// <summary>
		/// Return true if the specified fragment requires an hvo (and possibly flid) for its
		/// interpretation. Currently this assumes just the "ws" attribute,
		/// since smartws is obsolete.
		/// </summary>
		public static bool GetWsRequiresObject(XElement frag)
		{
			var xa = frag.Attribute("ws");
			if (xa == null)
			{
				return false;
			}
			var wsSpec = xa.Value;
			return GetWsRequiresObject(wsSpec);
		}

		/// <summary>
		/// Return true if the specified fragment requires an hvo (and possibly flid) for its interpretation.
		/// Currently this assumes just the "ws" attribute, since smartws is obsolete.
		/// </summary>
		public static bool GetWsRequiresObject(string wsSpec)
		{
			wsSpec = StringServices.GetWsSpecWithoutPrefix(wsSpec);
			return wsSpec.StartsWith("best") || wsSpec.StartsWith("reversal") || wsSpec == "va" || wsSpec == "av";
		}

		private const string sUnspecComplexFormType = "a0000000-dd15-4a03-9032-b40faaa9a754";
		private const string sUnspecVariantType = "b0000000-c40e-433e-80b5-31da08771344";
		private const string sUnspecExtendedNoteType = "c0000000-dd15-4a03-9032-b40faaa9a754";

		/// <summary>
		/// Returns a 'fake' Guid used to filter unspecified Complex Form types in
		/// XmlVc. Setup in configuration files by XmlDocConfigureDlg.
		/// </summary>
		public static Guid GetGuidForUnspecifiedComplexFormType()
		{
			return new Guid(sUnspecComplexFormType);
		}

		/// <summary>
		/// Returns a 'fake' Guid used to filter unspecified Variant types in
		/// XmlVc. Setup in configuration files by XmlDocConfigureDlg.
		/// </summary>
		public static Guid GetGuidForUnspecifiedVariantType()
		{
			return new Guid(sUnspecVariantType);
		}

		/// <summary>
		/// Returns a 'fake' Guid used to filter unspecified Extended Note types in
		/// XmlVc. Setup in configuration files by XmlDocConfigureDlg.
		/// </summary>
		public static Guid GetGuidForUnspecifiedExtendedNoteType()
		{
			return new Guid(sUnspecExtendedNoteType);
		}

		/// <summary>
		/// Get a Time property value coverted to a DateTime value.
		/// </summary>
		public static DateTime GetTimeProperty(ISilDataAccess sda, int hvo, int flid)
		{
			try
			{
				var silTime = sda.get_TimeProp(hvo, flid);
				return SilTime.ConvertFromSilTime(silTime);
			}
			catch
			{
				return DateTime.MinValue;
			}
		}

		/// <summary>
		/// Set a Time property to a given DateTime value.
		/// </summary>
		public static void SetTimeProperty(ISilDataAccess sda, int hvo, int flid, DateTime dt)
		{
			var silTime = SilTime.ConvertToSilTime(dt);
			sda.SetTime(hvo, flid, silTime);
		}

		/// <summary>
		/// Utility function to find a methodInfo for the named method.
		/// It is a static method of the class specified in the EditRowClass of the EditRowAssembly.
		/// </summary>
		public static MethodInfo GetStaticMethod(XElement node, string sAssemblyAttr, string sClassAttr, string sMethodName, out Type typeFound)
		{
			var sAssemblyName = XmlUtils.GetOptionalAttributeValue(node, sAssemblyAttr);
			var sClassName = XmlUtils.GetOptionalAttributeValue(node, sClassAttr);
			return GetStaticMethod(sAssemblyName, sClassName, sMethodName, "node " + node.GetOuterXml(), out typeFound);
		}

		/// <summary>
		/// Utility function to find a methodInfo for the named method.
		/// It is a static method of the class specified in the EditRowClass of the EditRowAssembly.
		/// </summary>
		public static MethodInfo GetStaticMethod(string sAssemblyName, string sClassName, string sMethodName, string sContext, out Type typeFound)
		{
			typeFound = null;
			Assembly assemblyFound;
			try
			{
				var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(MiscUtils.IsUnix ? 5 : 6);
				assemblyFound = Assembly.LoadFrom(Path.Combine(baseDir, sAssemblyName));
			}
			catch (Exception error)
			{
				throw new RuntimeConfigurationException(MakeGetStaticMethodErrorMessage("DLL at " + sAssemblyName, sContext), error);
			}
			Debug.Assert(assemblyFound != null);
			try
			{
				typeFound = assemblyFound.GetType(sClassName);
			}
			catch (Exception error)
			{
				throw new RuntimeConfigurationException(MakeGetStaticMethodErrorMessage("class called " + sClassName, sContext), error);
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			Debug.Assert(typeFound != null);
			MethodInfo mi;
			try
			{
				mi = typeFound.GetMethod(sMethodName);
			}
			catch (Exception error)
			{
				throw new RuntimeConfigurationException(MakeGetStaticMethodErrorMessage($"method called {sMethodName} of class {sClassName} in assembly {sAssemblyName}", sContext), error);
			}
			return mi;
		}

		private static string MakeGetStaticMethodErrorMessage(string sMainMsg, string sContext)
		{
			return $"GetStaticMethod() could not find the {sMainMsg} while processing {sContext}";
		}
	}
}