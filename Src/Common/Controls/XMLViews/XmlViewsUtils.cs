using System;
using System.Linq;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text; // StringBuilder
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Filters;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Controls
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
		static protected IWritingSystem s_qwsCurrent = null;
		/// <summary></summary>
		static protected int s_cwsMulti = 0;	// count of current ws alternatives.
		/// <summary></summary>
		static protected string s_sMultiSep = null;
		/// <summary></summary>
		static protected bool s_fMultiFirst = false;

		// static methods only, no sense making one.
		private XmlViewsUtils()
		{

		}

		/// <summary>
		/// looks up plural form alternative first for given flid, secondly for its destination class.
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="owningFlid"></param>
		/// <param name="titleStr">*{dstClass}* if couldn't find result.</param>
		/// <returns>true if we found an alternate form. false if titleStr is null or in *{key}* format.</returns>
		public static bool TryFindPluralFormFromFlid(IFwMetaDataCache mdc, int owningFlid, out string titleStr)
		{
			// first see if we can find an expanded name for the name of a flid.
			string flidName = mdc.GetFieldName(owningFlid);
			if (!String.IsNullOrEmpty(flidName))
			{
				if (TryFindString("AlternativeTitles", flidName, out titleStr))
					return true;
			}
			// secondly, see if we can find the plural form for the destination class.
			int dstClass = mdc.GetDstClsId(owningFlid);
			return TryFindPluralFormFromClassId(mdc, dstClass, out titleStr);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mdc"></param>
		/// <param name="clsId"></param>
		/// <param name="titleStr">*{dstClass}* if couldn't find result.</param>
		/// <returns>true if we found an alternate form. false if titleStr is null or in *{ClassName}* format.</returns>
		public static bool TryFindPluralFormFromClassId(IFwMetaDataCache mdc, int clsId, out string titleStr)
		{
			titleStr = null;
			if (clsId != 0)
			{
				string className = mdc.GetClassName(clsId);
				return TryFindString("AlternativeTitles", String.Format("{0}-Plural", className), out titleStr);
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="group"></param>
		/// <param name="key"></param>
		/// <param name="result"></param>
		/// <returns>true if we found a value associated with the given key. false if result is in *{key}* format.</returns>
		public static bool TryFindString(string group, string key, out string result)
		{
			result = StringTable.Table.GetString(key, group);
			return FoundStringTableString(key, result);
		}

		/// <summary>
		/// determine if string table query gave a significant result (i.e. not *{key}* format).
		/// </summary>
		/// <param name="key"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private static bool FoundStringTableString(string key, string result)
		{
			return !String.IsNullOrEmpty(result) &&
				!(result.StartsWith("*") && result == ("*" + key + "*"));
		}

		/// <summary>
		/// If any attributes of input (or its children) are of the form "$param=default",
		/// generate a complete copy of input in which "$param=default" is replaced with
		/// "default", and return it. Otherwise just return input.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static XmlNode CopyWithParamDefaults(XmlNode input)
		{
			if (!HasParam(input))
				return input;
			XmlNode result = input.Clone();

			ReplaceParamWithDefault replacer = new ReplaceParamWithDefault();
			XmlUtils.VisitAttributes(result, replacer);

			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the replacing param default.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <param name="paramId">The param id.</param>
		/// <param name="val">The val.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static XmlNode CopyReplacingParamDefault(XmlNode input, string paramId, string val)
		{
			XmlNode result = input.Clone();

			ReplaceParamDefault replacer = new ReplaceParamDefault(paramId, val);
			XmlUtils.VisitAttributes(result, replacer);

			return result;
		}

		/// <summary>
		/// Find the value of the first parameter-like attribute value "$param=default"
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static bool HasParam(XmlNode input)
		{
			TestForParameter tfp = new TestForParameter();
			XmlUtils.VisitAttributes(input, tfp);
			return tfp.HasAttribute;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the params.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string[] FindParams(XmlNode input)
		{
			AccumulateParameters ap = new AccumulateParameters();
			XmlUtils.VisitAttributes(input, ap);
			return ap.Parameters.ToArray();
		}

		/// <summary>
		/// Search the node for an element with an attribute that has a value that looks like "$ws=..." and return
		/// the "...".
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static string FindWsParam(XmlNode node)
		{
			string[] paramList = FindParams(node);
			foreach (string s in paramList)
			{
				// Enhance JohnT: may handle other parameters, and show something here for them.
				if (s.StartsWith(StringServices.WsParamLabel))
				{
					return s.Substring(StringServices.WsParamLabel.Length);
				}
			}
			return "";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the index of the node in nodes that has value attVal for attribute attName.
		/// Return -1 if not found.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="attName">Name of the att.</param>
		/// <param name="attVal">The att val.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int FindIndexOfAttrVal(List<XmlNode> nodes, string attName, string attVal)
		{
			int index = 0;
			foreach (XmlNode node in nodes)
			{
				string sAttr = XmlUtils.GetLocalizedAttributeValue(node, attName, null);
				if (sAttr == attVal)
					return index;
				index++;
			}
			return -1;
		}


		/// <summary>
		/// go through possible attributes to match, and return true when the ones that exist match
		/// the same attributes in another node.
		/// </summary>
		/// <param name="colSpec1"></param>
		/// <param name="colSpec2"></param>
		/// <param name="possibleAttributesToMatch">the attributes to try to match, in the order we try to match them.
		/// if this routine returns <c>true</c> then this the top queue element will be the attribute we matched on.</param>
		/// <returns></returns>
		public static bool TryMatchExistingAttributes(XmlNode colSpec1, XmlNode colSpec2, ref Queue<string> possibleAttributesToMatch)
		{
			if (colSpec1 == colSpec2)
				return true;
			if (possibleAttributesToMatch == null || possibleAttributesToMatch.Count == 0)
			{
				// we've compared all the possible attributes given, and haven't found a mismatch.
				return true;
			}
			string attribute = possibleAttributesToMatch.Peek();
			string attrVal1 = XmlUtils.GetOptionalAttributeValue(colSpec1, attribute);
			string attrVal2 = XmlUtils.GetOptionalAttributeValue(colSpec2, attribute);
			possibleAttributesToMatch.Dequeue();
			return attrVal1 == attrVal2 &&
				TryMatchExistingAttributes(colSpec1, colSpec2, ref possibleAttributesToMatch);
		}


		/// <summary>
		/// Return a string such that ICU alphabetic comparison of the strings
		/// will produce the same results as numberic comparison of the values.
		/// For positive integers the string is a recognizeable representation of
		/// the number (with extra leading zeros); for negative, it is not
		/// recognizeable but works.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		public static string AlphaCompNumberString(int val)
		{
			// We want to produce things like
			// 0000000
			// 0000001
			// 0000010
			// Then, negative numbers should sort with more negative ones smaller.
			// We do this by a similar approach, with leading minus (fortunately smaller
			// alphabetically than zero), followed by a similar trick applied to Int32.Max + val + 1
			// -0000001
			// -0000002
			// However, if we pad with zeros as above, "0" as a pattern matches everything!
			// Since that's a fairly likely pattern, we pad with "/" (less than 0, more than '-'.
			// Bizarre matches are still possible, but much less likely.
			if (val >= 0)
				return AlphaCompPosNumString(val);
			else
				// We add 1 here because otherwise we pass a negative value to the other method for Int32.MinValue
				return "-" + AlphaCompPosNumString(Int32.MaxValue + val + 1);
		}

		// Like AlphaCompNumberString, but for positive integers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Alphas the comp pos num string.
		/// </summary>
		/// <param name="val">The val.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string AlphaCompPosNumString(int val)
		{
			Debug.Assert(val >= 0);
			int maxDigits = Int32.MaxValue.ToString().Length;
			string sVal = val.ToString();
			if (sVal.Length == maxDigits)
				return sVal;
			else
				return new String('/', maxDigits - sVal.Length) + sVal;
		}

		/// <summary>
		/// Return a string such that ICU alphabetic comparison of the strings
		/// will produce the same results as DateTime.Compare of the values.
		/// </summary>
		/// <param name="dt"></param>
		/// <returns></returns>
		public static string DateTimeCompString(DateTime dt)
		{
			string format = "u";	// 2000-08-17 23:32:32Z
			return dt.ToString(format, System.Globalization.DateTimeFormatInfo.InvariantInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the index of the node in nodes that 'matches' the target node.
		/// Return -1 if not found.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="target">The target.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int FindIndexOfMatchingNode(IEnumerable<XmlNode> nodes, XmlNode target)
		{
			return XmlUtils.FindIndexOfMatchingNode(nodes, target);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the node with attr val.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="attName">Name of the att.</param>
		/// <param name="attVal">The att val.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static XmlNode FindNodeWithAttrVal(List<XmlNode> nodes, string attName, string attVal)
		{
			int index = FindIndexOfAttrVal(nodes, attName, attVal);
			if (index == -1)
				return null;
			return nodes[index];
		}

		/// <summary>
		/// Answer a list containing, for each node in selectNodes, the item in sourceNodes
		/// that has the same value for the attribute attName.
		/// </summary>
		/// <param name="sourceNodes"></param>
		/// <param name="selectNodes"></param>
		/// <param name="attName"></param>
		/// <returns></returns>
		public static List<XmlNode> CorrespondingItems(List<XmlNode> sourceNodes, List<XmlNode> selectNodes, string attName)
		{
			List<XmlNode> result = new List<XmlNode>(selectNodes.Count);
			foreach(XmlNode node in selectNodes)
			{
				string attVal = XmlUtils.GetManditoryAttributeValue(node, attName);
				foreach(XmlNode node1 in sourceNodes)
				{
					if (XmlUtils.GetManditoryAttributeValue(node1, attName) == attVal)
					{
						result.Add(node1);
						break;
					}
				}
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given one of the original list items, and the spec of the column we want to sort by,
		/// add to collector whatever ManyOnePathSortItems are appropriate.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="colSpec">The col spec.</param>
		/// <param name="collector">The collector.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="layouts">The layouts.</param>
		/// ------------------------------------------------------------------------------------
		public static void CollectBrowseItems(int hvo, XmlNode colSpec, ArrayList collector,
			IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts)
		{
			XmlNode topNode = XmlBrowseViewBaseVc.GetColumnNode(colSpec, hvo, sda, layouts);

			// Todo: handle various cases here, mostly drill-down to <seq> or <obj>
			CollectBrowseItems(hvo, topNode, collector, mdc, sda, layouts, null, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Main (recursive) part of CollectBrowseItems. Given that hvo is to be displayed using node,
		/// figure what objects to put in the list.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="node">The node.</param>
		/// <param name="collector">The collector.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="layouts">The layouts.</param>
		/// <param name="caller">The caller.</param>
		/// <param name="hvos">The hvos.</param>
		/// <param name="flids">The flids.</param>
		/// ------------------------------------------------------------------------------------
		static void CollectBrowseItems(int hvo, XmlNode node, ArrayList collector,
			IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts, XmlNode caller, int[] hvos, int[] flids)
		{
			switch(node.Name)
			{
			case "obj":
			{
				int clsid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
				int flid = mdc.GetFieldId2(clsid, XmlUtils.GetManditoryAttributeValue(node, "field"), true);
				int hvoDst = sda.get_ObjectProp(hvo, flid);
				if (hvoDst == 0)
				{
					// We want a row, even though it's blank for this column.
					collector.Add(new ManyOnePathSortItem(hvo, hvos, flids));
					return;
				}
				// At this point we have to mimic the process that XmlVc uses to come up with the
				// node that will be used to process the destination item.
				XmlNode dstNode = GetNodeForRelatedObject(hvoDst, caller, node, layouts, sda);
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
				int clsid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
				int flid = mdc.GetFieldId2(clsid, XmlUtils.GetManditoryAttributeValue(node, "field"), true);
				int chvo = sda.get_VecSize(hvo, flid);
				if (chvo == 0)
				{
					// We want a row, even though it's blank for this column.
					collector.Add(new ManyOnePathSortItem(hvo, hvos, flids));
					return;
				}
				for (int ihvo = 0; ihvo < chvo; ihvo++)
				{
					int hvoDst = sda.get_VecItem(hvo, flid, ihvo);
					// At this point we have to mimic the process that XmlVc uses to come up with the
					// node that will be used to process the destination item.
					XmlNode dstNode = GetNodeForRelatedObject(hvoDst, caller, node, layouts, sda);
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
				XmlNode mainChild = FindMainChild(node);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the main child.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static private XmlNode FindMainChild(XmlNode node)
		{
			XmlNode mainChild = null;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (child is XmlComment || child.Name == "properties")
					continue;
				if (mainChild != null)
				{
					// multiple main children, stop here.
					return null;
				}
				mainChild = child;
			}
			return mainChild;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The node argument is an "obj" or "seq" element, and caller (if not null) is a part
		/// element that invoked the node and may override its "layout" attribute.
		/// Apply the same logic used by XmlVc to determine the node that will be used
		/// to display the destination object hvoDst
		/// </summary>
		/// <param name="hvoDst">The hvo DST.</param>
		/// <param name="caller">The caller.</param>
		/// <param name="node">The node.</param>
		/// <param name="layouts">The layouts.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static XmlNode GetNodeForRelatedObject(int hvoDst, XmlNode caller, XmlNode node,
			LayoutCache layouts, ISilDataAccess sda)
		{
			if (XmlUtils.GetOptionalAttributeValue(node, "frag") != null)
				return null; // old approach not handled.
			// (frag="true" is also used to prevent splitting entry when sorting on gloss or
			// allomorph in Find Entries dialog display.  Part of fixing LT-10293.)
			string layoutName = XmlVc.GetLayoutName(node, caller);
			XmlNode layoutNode = XmlVc.GetNodeForPart(hvoDst, layoutName, true, sda, layouts);
			return XmlVc.GetDisplayNodeForChild(layoutNode, node, layouts);
		}

		static int[] AppendInt(int[] sofar, int add)
		{
			if (sofar == null)
				return new int[] {add};
			List<int> result = new List<int>(sofar);
			result.Add(add);
			return result.ToArray();
		}

		// Concatenate two arrays of strings. If either is empty return the other.
		// (If both are null returns null.)
		static private string[] Concatenate(string[] first, string[] second)
		{
			if (first == null || first.Length == 0)
				return second;
			if (second == null || second.Length == 0)
				return first;
			List<string> result = new List<string>(first);
			result.AddRange(second);
			return result.ToArray();
		}

		/// <summary>
		/// Return the concatenation of all the input strings as a single string.
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		static public string[] Assemble(string[] items)
		{
			if (items == null)
				return new string[0];
			if (items.Length <= 1)
				return items;
			var bldr = new StringBuilder();
			foreach(string s in items)
				bldr.Append(s);
			return new[] {bldr.ToString()};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an array of string values (keys) for the objects under the layout child nodes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static internal string[] ChildKeys(FdoCache fdoCache, ISilDataAccess sda, XmlNode layout, int hvo,
			LayoutCache layoutCache, XmlNode caller, int wsForce)
		{
			string[] result = null;
			foreach (XmlNode child in layout.ChildNodes)
			{
				if (child is XmlComment)
					continue;
				result = Concatenate(result, StringsFor(fdoCache, sda, child, hvo, layoutCache, caller, wsForce));
			}
			return result;
		}

		static private void AddSeparator(ref string item, int ichInsert, XmlNode layout)
		{
			string separator = XmlUtils.GetOptionalAttributeValue(layout, "sep");
			if (string.IsNullOrEmpty(separator))
				return;
			bool fCheckForEmptyItems = XmlUtils.GetOptionalBooleanAttributeValue(layout, "checkForEmptyItems", false);
			if (item == null || ichInsert < 0 || ichInsert > item.Length || fCheckForEmptyItems && item.Length == 0)
				return;
			item = item.Insert(ichInsert, separator);
		}


		static private string[] AssembleChildKeys(FdoCache fdoCache, ISilDataAccess sda, XmlNode layout, int hvo,
			LayoutCache layoutCache, XmlNode caller, int wsForce)
		{
			return Assemble(ChildKeys(fdoCache, sda, layout, hvo, layoutCache, caller, wsForce));
		}

		/// <summary>
		/// This is a simplified version of XmlVc.GetFlid.
		/// It does not look for a flid attr, nor try to cache the result.
		/// It looks for a "field" property, and optionally a "class" one, and uses them
		/// (or the class of hvo, if "class" is missing) to figure the flid.
		/// Virtual properties are assumed already created.
		/// </summary>
		/// <param name="sda">The sda.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		static int GetFlid(ISilDataAccess sda, XmlNode frag, int hvo)
		{
			string stClassName = XmlUtils.GetOptionalAttributeValue(frag,"class");
			string stFieldName = XmlUtils.GetManditoryAttributeValue(frag,"field");
			if (string.IsNullOrEmpty(stClassName))
			{
				int clid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
				return sda.MetaDataCache.GetFieldId2(clid, stFieldName, true);
			}
			return sda.MetaDataCache.GetFieldId(stClassName, stFieldName, true);
		}

		// Utility function to get length of an array variable which might be null (return 0 if so).
		static int GetArrayLength(string[] items)
		{
			if (items == null)
				return 0;
			return items.Length;
		}

		internal static string DisplayWsLabel(IWritingSystem ws, FdoCache cache)
		{
			if (ws == null)
				return "";

			string sLabel = ws.Abbreviation;
			if (sLabel == null)
				sLabel = ws.Id;
			if (sLabel == null)
				sLabel = XMLViewsStrings.ksUNK;
			return sLabel + " ";
		}

		static string AddMultipleAlternatives(FdoCache cache, ISilDataAccess sda, IEnumerable<int> wsIds, int hvo, int flid, XmlNode frag)
		{
			string sep = XmlUtils.GetOptionalAttributeValue(frag, "sep", null);
			bool fLabel = XmlUtils.GetOptionalBooleanAttributeValue(frag, "showLabels", false); // true to 'separate' using multistring labels.
			string result = "";
			bool fFirst = true;
			foreach (int ws in wsIds)
			{
				string val = sda.get_MultiStringAlt(hvo, flid, ws).Text;
				if (string.IsNullOrEmpty(val))
					continue; // doesn't even count as 'first'
				if (fLabel)
				{
					IWritingSystem wsObj = cache.ServiceLocator.WritingSystemManager.Get(ws);
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
		internal static string[] AddStringFromOtherObj(XmlNode frag, int hvoTarget, FdoCache cache, ISilDataAccess sda)
		{
			int flid = XmlVc.GetFlid(frag, hvoTarget, sda);
			ITsStrFactory tsf = cache.TsStrFactory;
			CellarPropertyType itype = (CellarPropertyType)sda.MetaDataCache.GetFieldType(flid);
			if (itype == CellarPropertyType.Unicode)
			{
				return new[] { sda.get_UnicodeProp(hvoTarget, flid) };
			}
			else if (itype == CellarPropertyType.String)
			{
				return new[] { sda.get_StringProp(hvoTarget, flid).Text };
			}
			else // multistring of some type
			{
				int wsid = 0;
				string sep = "";
				if (s_cwsMulti > 1)
				{
					string sLabelWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
					if (sLabelWs != null && sLabelWs == "current")
					{
						sep = DisplayMultiSep(frag)
							+ DisplayWsLabel(s_qwsCurrent, cache);
						wsid = s_qwsCurrent.Handle;
					}
				}
				if (wsid == 0)
					wsid = WritingSystemServices.GetWritingSystem(cache,
						frag, null, WritingSystemServices.kwsAnal).Handle;
				if (itype == CellarPropertyType.MultiUnicode)
				{
					return new[] { sep, sda.get_MultiStringAlt(hvoTarget, flid, wsid).Text };
				}
				else
				{
					return new[] { sep, sda.get_MultiStringAlt(hvoTarget, flid, wsid).Text };
				}
			}
		}

		internal static string DisplayMultiSep(XmlNode frag)
		{
			string sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
			if (sWs != null && sWs == "current")
			{
				if (!s_fMultiFirst && !string.IsNullOrEmpty(s_sMultiSep))
				{
					return s_sMultiSep;
				}
				else
				{
					s_fMultiFirst = false;
				}
			}
			return "";
		}

		/// <summary>
		/// Returns an array of string values (keys) for the objects under this layout node.
		/// </summary>
		/// <param name="fdoCache">The fdo cache.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="layout">The layout.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="layoutCache">The layout cache.</param>
		/// <param name="caller">where layout is a component of a 'part' element, caller
		/// is the 'part ref' that invoked it.</param>
		/// <param name="wsForce">if non-zero, "string" elements are forced to use that writing system for multistrings.</param>
		/// <returns></returns>
		static public string[] StringsFor(FdoCache fdoCache, ISilDataAccess sda, XmlNode layout, int hvo,
			LayoutCache layoutCache, XmlNode caller, int wsForce)
		{
			// Some nodes are known to be uninteresting.
			if (XmlVc.CanSkipNode(layout))
				return new string[0]; // don't know how to sort, treat as empty key.

			switch (layout.Name)
			{
				case "string":
				{
					int hvoTarget = hvo;
					XmlVc.GetActualTarget(layout, ref hvoTarget, sda);	// modify the hvo if needed
					if (hvo != hvoTarget)
					{
						return AddStringFromOtherObj(layout, hvoTarget, fdoCache, sda);
					}
					int flid = GetFlid(sda, layout, hvo);
					if (wsForce != 0)
					{
						// If we are forcing a writing system, and it's a multistring, get the forced alternative.
						int itype = sda.MetaDataCache.GetFieldType(flid);
						itype = itype & (int)CellarPropertyTypeFilter.VirtualMask;
						switch (itype)
						{
							case (int) CellarPropertyType.MultiUnicode:
							case (int) CellarPropertyType.MultiString:
								if (wsForce < 0)
								{
									int wsActual;
									var tss = WritingSystemServices.GetMagicStringAlt(fdoCache, sda, wsForce, hvo, flid, true, out wsActual);
									return new[] {tss == null ? "" : tss.Text };
								}
								return new[]
										   {sda.get_MultiStringAlt(hvo, flid, wsForce).Text};
						}
					}
					bool fFoundType;
					var strValue = fdoCache.GetText(hvo, flid, layout, out fFoundType);
					if (fFoundType)
						return new[] {strValue};

					throw new Exception("Bad property type (" + strValue + " for hvo " + hvo +
												" found for string property "
							+ flid + " in " + layout.OuterXml);
				}
				case "configureMlString":
				{
					int flid = GetFlid(sda, layout, hvo);
					// The Ws info specified in the part ref node
					HashSet<int> wsIds = WritingSystemServices.GetAllWritingSystems(fdoCache, caller, null, hvo, flid);
					if (wsIds.Count == 1)
					{
						var strValue = sda.get_MultiStringAlt(hvo, flid, wsIds.First()).Text;
						return new[] {strValue};
					}
					return new[] {AddMultipleAlternatives(fdoCache, sda, wsIds, hvo, flid, caller)};
				}
				case "multiling":
					return ProcessMultiLingualChildren(fdoCache, sda, layout, hvo, layoutCache, caller, wsForce);
				case "layout":
					// "layout" can occur when GetNodeToUseForColumn returns a phony 'layout'
					// formed by unifying a layout with child nodes. Assemble its children.
					// (arguably, we should treat that like div if current flow is a pile.
					// but we can't tell that and it rarely makes a difference.)
				case "para":
				case "span":
				{
					return AssembleChildKeys(fdoCache, sda, layout, hvo, layoutCache, caller, wsForce);
				}
				case "column":
					// top-level node for whole column; concatenate children as for "para"
					// if multipara is false, otherwise as for "div"
					if (XmlUtils.GetOptionalBooleanAttributeValue(layout, "multipara", false))
						return ChildKeys(fdoCache, sda, layout, hvo, layoutCache, caller, wsForce);
					else
						return AssembleChildKeys(fdoCache, sda, layout, hvo, layoutCache, caller, wsForce);

				case "part":
				{
					string partref = XmlUtils.GetOptionalAttributeValue(layout, "ref");
					if (partref == null)
						return ChildKeys(fdoCache, sda, layout, hvo, layoutCache, caller, wsForce); // an actual part, made up of its pieces
					XmlNode part = XmlVc.GetNodeForPart(hvo, partref, false, sda, layoutCache);
					// This is the critical place where we introduce a caller. The 'layout' is really a 'part ref' which is the
					// 'caller' for all embedded nodes in the called part.
					return StringsFor(fdoCache, sda, part, hvo, layoutCache, layout, wsForce);
				}
				case "div":
				case "innerpile":
				{
					// Concatenate keys for child nodes (as distinct strings)
					return ChildKeys(fdoCache, sda, layout, hvo, layoutCache, caller, wsForce);
				}
				case "obj":
				{
					// Follow the property, get the object, look up the layout to use,
					// invoke recursively.
					int flid = GetFlid(sda, layout, hvo);
					int hvoTarget = sda.get_ObjectProp(hvo, flid);
					if (hvoTarget == 0)
						break; // return empty key
					string targetLayoutName = XmlUtils.GetOptionalAttributeValue(layout, "layout"); // uses 'default' if missing.
					XmlNode layoutTarget = GetLayoutNodeForChild(sda, hvoTarget, flid, targetLayoutName, layout, layoutCache);
					if (layoutTarget == null)
						break;
					return ChildKeys(fdoCache, sda, layoutTarget, hvoTarget, layoutCache, caller, wsForce);
				}
				case "seq":
				{
					// Follow the property. For each object, look up the layout to use,
					// invoke recursively, concatenate
					int flid = GetFlid(sda, layout, hvo);
					int[] contents;
					int ctarget = sda.get_VecSize(hvo, flid);
					using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(ctarget))
					{
						int chvo;
						sda.VecProp(hvo, flid, ctarget, out chvo, arrayPtr);
						contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
					}

					string[] result = null;
					string targetLayoutName = XmlVc.GetLayoutName(layout, caller); // also allows for finding "param" attr in caller, if not null
					int i = 0;
					foreach (int hvoTarget in contents)
					{
						int prevResultLength = GetArrayLength(result);
						XmlNode layoutTarget = GetLayoutNodeForChild(sda, hvoTarget, flid, targetLayoutName, layout, layoutCache);
						if (layoutTarget == null)
							continue; // should not happen, but best recovery we can make
						result = Concatenate(result, ChildKeys(fdoCache, sda, layoutTarget, hvoTarget, layoutCache, caller, wsForce));
						// add a separator between the new childkey group and the previous childkey group
						if (i > 0 && prevResultLength != GetArrayLength(result) && prevResultLength > 0)
						{
							int ichIns = 0;
							if (result[prevResultLength - 1] != null)
								ichIns = result[prevResultLength - 1].Length;
							AddSeparator(ref result[prevResultLength - 1],  ichIns, layout);
						}
						++i;
					}

					return result;
				}
				case "choice":
				{
					foreach(XmlNode whereNode in layout.ChildNodes)
					{
						if (whereNode.Name != "where")
						{
							if (whereNode.Name == "otherwise")
								return StringsFor(fdoCache, sda, XmlUtils.GetFirstNonCommentChild(whereNode), hvo, layoutCache, caller, wsForce);
							continue; // ignore any other nodes,typically comments
						}
						// OK, it's a where node.
						if (XmlVc.ConditionPasses(whereNode, hvo, fdoCache, sda, caller))
							return StringsFor(fdoCache, sda, XmlUtils.GetFirstNonCommentChild(whereNode), hvo, layoutCache, caller, wsForce);
					}
					break; // if no condition passes and no otherwise, return null.
				}
				case "if":
				{
					if (XmlVc.ConditionPasses(layout, hvo, fdoCache, sda, caller))
						return StringsFor(fdoCache, sda, XmlUtils.GetFirstNonCommentChild(layout), hvo, layoutCache, caller, wsForce);
					break;
				}
				case "ifnot":
				{
					if (!XmlVc.ConditionPasses(layout, hvo, fdoCache, sda, caller))
						return StringsFor(fdoCache, sda, XmlUtils.GetFirstNonCommentChild(layout), hvo, layoutCache, caller, wsForce);
					break;
				}
				case "lit":
				{
					string literal = layout.InnerText;
					string sTranslate = XmlUtils.GetOptionalAttributeValue(layout, "translate", "");
					if (sTranslate.Trim().ToLower() != "do not translate")
						literal = StringTable.Table.LocalizeLiteralValue(literal);
					return new[] { literal };
				}
				case "int":
				{
					int flid = GetFlid(sda, layout, hvo);
					int val = sda.get_IntProp(hvo, flid);
					return new[] {AlphaCompNumberString(val)};
				}
				case "datetime":
				{
					int flid = GetFlid(sda, layout, hvo);
					CellarPropertyType itype = (CellarPropertyType)sda.MetaDataCache.GetFieldType(flid);
					if (itype == CellarPropertyType.Time)
					{
						DateTime dt = SilTime.GetTimeProperty(sda, hvo, flid);
						return new[] {DateTimeCompString(dt)};
					}
					else
					{
						string stFieldName = XmlUtils.GetManditoryAttributeValue(layout, "field");
						throw new Exception("Bad field type (" + stFieldName + " for hvo " + hvo + " found for " +
							layout.Name + "  property "	+ flid + " in " + layout.OuterXml);
					}
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a fragment's children against multiple writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static private string[] ProcessMultiLingualChildren(FdoCache fdoCache, ISilDataAccess sda, XmlNode frag, int hvo,
			LayoutCache layoutCache, XmlNode caller, int wsForce)
		{
			string sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
			if (sWs == null)
				return null;

			Debug.Assert(s_qwsCurrent == null);
			Debug.Assert(s_cwsMulti == 0);
			string[] result = null;
			try
			{
				HashSet<int> wsIds = WritingSystemServices.GetAllWritingSystems(fdoCache, frag, s_qwsCurrent, 0, 0);
				s_cwsMulti = wsIds.Count;
				if (s_cwsMulti > 1)
					s_sMultiSep = XmlUtils.GetOptionalAttributeValue(frag, "sep");
				s_fMultiFirst = true;
				foreach (int WSId in wsIds)
				{
					s_qwsCurrent = fdoCache.ServiceLocator.WritingSystemManager.Get(WSId);
					result = Concatenate(result, ChildKeys(fdoCache, sda, frag, hvo, layoutCache, caller, wsForce));
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

		static private XmlNode GetLayoutNodeForChild(ISilDataAccess sda, int hvoTarget, int flid, string targetLayoutName,
			XmlNode layout, LayoutCache layoutCache)
		{
			XmlNode layoutTarget = XmlVc.GetNodeForPart(hvoTarget, targetLayoutName, true, sda, layoutCache);
			if (layoutTarget == null)
				layoutTarget = layout; // no layout looked up, use whatever children caller has
			else if (layout.ChildNodes.Count != 0)
			{
				// got both a looked-up layout and child nodes overriding.
				if (layoutTarget.Name == "layout")
				{
					// thing we looked up is a layout, we will unify.
					layoutTarget = layoutCache.LayoutInventory.GetUnified(layoutTarget, layout);
				}
				else
				{
					// thing we looked up is a part, for now (see XmlVc.Display) we just replace
					// with supplied parts
					layoutTarget = layout;
				}
			}
			return layoutTarget;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We want to display the object bvi.KeyObject, or one of its pathobjects, in a
		/// column specified by colSpec.
		/// Determine the hvo and XmlNode that we should use as the root for the cell.
		/// By default, we display the first object in the path, using the base node
		/// derived from the colSpec.
		/// However, if the colSpec begins with a path compatible with bvi.PathFlid(0),
		/// we can use bvi.PathObject(1) and the appropriate derived node.
		/// If all flids match we can use bvi.KeyObject itself.
		/// If collectOuterStructParts is non-null, it accumulates containing parts
		/// that are structural, like para, span, div.
		/// </summary>
		/// <param name="bvi">The bvi.</param>
		/// <param name="colSpec">The col spec.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="layouts">The layouts.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="collectOuterStructParts">The collect outer struct parts.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static XmlNode GetNodeToUseForColumn(IManyOnePathSortItem bvi, XmlNode colSpec,
			IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts, out int hvo, List<XmlNode> collectOuterStructParts)
		{
			return GetDisplayCommandForColumn(bvi, colSpec, mdc, sda, layouts, out hvo, collectOuterStructParts).Node;
		}

		/// <summary>
		/// This returns a NodeDisplayCommand containing thd node for GetNodeToUseForColumn. However, it distinguishes whether to
		/// display the children of this node or the node itself by returning the appropriate kind of NodeDisplayCommand.
		/// </summary>
		/// <param name="bvi"></param>
		/// <param name="colSpec"></param>
		/// <param name="mdc"></param>
		/// <param name="sda"></param>
		/// <param name="layouts"></param>
		/// <param name="hvo"></param>
		/// <param name="collectOuterStructParts"></param>
		/// <returns></returns>
		public static NodeDisplayCommand GetDisplayCommandForColumn(IManyOnePathSortItem bvi, XmlNode colSpec,
			IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts, out int hvo, List<XmlNode> collectOuterStructParts)
		{
			XmlNode topNode = XmlBrowseViewBaseVc.GetColumnNode(colSpec, bvi.PathObject(0), sda, layouts);
			return GetDisplayCommandForColumn1(bvi, topNode, mdc, sda, layouts, 0, out hvo, collectOuterStructParts);
		}

		/// <summary>
		/// Recursive implementation method for GetDisplayCommandForColumn.
		/// </summary>
		/// <param name="bvi"></param>
		/// <param name="node"></param>
		/// <param name="mdc"></param>
		/// <param name="sda"></param>
		/// <param name="layouts"></param>
		/// <param name="depth"></param>
		/// <param name="hvo"></param>
		/// <param name="collectOuterStructParts"></param>
		/// <returns></returns>
		static NodeDisplayCommand GetDisplayCommandForColumn1(IManyOnePathSortItem bvi, XmlNode node,
			IFwMetaDataCache mdc, ISilDataAccess sda, LayoutCache layouts, int depth,
			out int hvo, List<XmlNode> collectOuterStructParts)
		{
			hvo = bvi.PathObject(depth); // default
			switch(node.Name)
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

				int clsid = sda.get_IntProp(bvi.PathObject(depth), CmObjectTags.kflidClass);
				int flid = mdc.GetFieldId2(clsid, XmlUtils.GetManditoryAttributeValue(node, "field"), true);
				if (flid != bvi.PathFlid(depth))
					return new NodeDisplayCommand(node); // different field, can't dig deeper.
				int hvoDst = bvi.PathObject(depth + 1);
				// If the path object has been deleted, fall back to displaying whatever the property currently holds.
				if (sda.get_IntProp(hvoDst, CmObjectTags.kflidClass) == 0)
					return new NodeDisplayCommand(node); // different field, can't dig deeper.
				// At this point we have to mimic the process that XmlVc uses to come up with the
				// node that will be used to process the destination item.
				XmlNode dstNode = GetNodeForRelatedObject(hvoDst, null, node, layouts, sda);
				return GetDisplayCommandForColumn1(bvi, dstNode, mdc, sda, layouts, depth + 1, out hvo, collectOuterStructParts);
			}
			case "para":
			case "span":
			case "div":
			case "concpara":
			case "innerpile":
			{
				XmlNode mainChild = FindMainChild(node);
				if (mainChild == null)
					return new NodeDisplayCommand(node); // can't usefully go further.
				if (collectOuterStructParts != null)
					collectOuterStructParts.Add(node);
				return GetDisplayCommandForColumn1(bvi, mainChild, mdc, sda, layouts, depth, out hvo, collectOuterStructParts);
			}
				// Review JohnT: In XmlVc, "part" is the one thing that calls ProcessChildren with non-null caller.
				// this should make some difference here, but I can't figure what yet, or come up with a test that fails.
				// We may need a "caller" argument to pass this down so it can be used in GetNodeForRelatedObject.
			case "part":
			{
				string layoutName = XmlUtils.GetOptionalAttributeValue(node, "ref");
				if (layoutName != null)
				{
					// It's actually a part ref, in a layout, not a part looked up by one!
					// Get the node it refers to, and make a command to process its children.
					XmlNode part = XmlVc.GetNodeForPart(hvo, layoutName, false, sda, layouts);
					if (part != null)
						return new NodeChildrenDisplayCommand(part); // display this object using the children of the part referenced.
					else
						return new NodeDisplayCommand(node); // no matching part, do default.
				}

				// These are almost the same, but are never added to collectOuterStructParts.
				// Also, expecially in the case of 'layout', they may result from unification, and be meaningless
				// except for their children; in any case, the children are all we want to process.
				// This is the main reason we return a command, not just a node: this case has to return the subclass.
				XmlNode mainChild = FindMainChild(node);
				if (mainChild == null)
					return new NodeChildrenDisplayCommand(node); // can't usefully go further.
				return GetDisplayCommandForColumn1(bvi, mainChild, mdc, sda, layouts, depth, out hvo, collectOuterStructParts);
			}
			case "column":
			case "layout":
			{

				// These are almost the same as para, span, etc, but are never added to collectOuterStructParts.
				// Also, expecially in the case of 'layout', they may result from unification, and be meaningless
				// except for their children; in any case, the children are all we want to process.
				// This is the main reason we return a command, not just a node: this case has to return the subclass.
				XmlNode mainChild = FindMainChild(node);
				if (mainChild == null)
					return new NodeChildrenDisplayCommand(node); // can't usefully go further.
				return GetDisplayCommandForColumn1(bvi, mainChild, mdc, sda, layouts, depth, out hvo, collectOuterStructParts);
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
		/// <param name="wsParam"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static int GetWsFromString(string wsParam, FdoCache cache)
		{
			if (wsParam == null)
				return 0;
			IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
			switch (wsParam)
			{
				case "analysis":
					return wsContainer.DefaultAnalysisWritingSystem.Handle;
				case "vernacular":
					return wsContainer.DefaultVernacularWritingSystem.Handle;
				case "pronunciation":
					return wsContainer.DefaultPronunciationWritingSystem.Handle;
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
		/// <param name="frag"></param>
		/// <returns></returns>
		public static bool GetWsRequiresObject(XmlNode frag)
		{
			var xa = frag.Attributes["ws"];
			if (xa == null)
				return false;
			var wsSpec = xa.Value;
			return GetWsRequiresObject(wsSpec);
		}

		/// <summary>
		/// Return true if the specified fragment requires an hvo (and possibly flid) for its interpretation.
		/// Currently this assumes just the "ws" attribute, since smartws is obsolete.
		/// </summary>
		/// <returns></returns>
		public static bool GetWsRequiresObject(string wsSpec)
		{
			wsSpec = StringServices.GetWsSpecWithoutPrefix(wsSpec);
			return wsSpec.StartsWith("best") || wsSpec.StartsWith("reversal") || wsSpec == "va" || wsSpec == "av";
		}

		private const string sUnspecComplexFormType = "a0000000-dd15-4a03-9032-b40faaa9a754";
		private const string sUnspecVariantType = "b0000000-c40e-433e-80b5-31da08771344";

		/// <summary>
		/// Returns a 'fake' Guid used to filter unspecified Complex Form types in
		/// XmlVc. Setup in configuration files by XmlDocConfigureDlg.
		/// </summary>
		/// <returns></returns>
		public static Guid GetGuidForUnspecifiedComplexFormType()
		{
			return new Guid(sUnspecComplexFormType);
		}

		/// <summary>
		/// Returns a 'fake' Guid used to filter unspecified Variant types in
		/// XmlVc. Setup in configuration files by XmlDocConfigureDlg.
		/// </summary>
		/// <returns></returns>
		public static Guid GetGuidForUnspecifiedVariantType()
		{
			return new Guid(sUnspecVariantType);
		}
	}

	/// <summary>
	/// This class tests whether there is a parameter and if so stops the processing.
	/// </summary>
	class TestForParameter : IAttributeVisitor
	{
		bool m_fFound = false;
		public TestForParameter()
		{
		}

		public virtual bool Visit(XmlAttribute xa)
		{
			m_fFound |= IsParameter(xa.Value);
			return m_fFound;
		}

		public bool HasAttribute
		{
			get { return m_fFound; }
		}

		/// <summary>
		/// This is the definition of a parameter-like value.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		internal static bool IsParameter(string input)
		{
			if (input.Length < 2)
				return false;
			if (input[0] != '$')
				return false;
			return (input.IndexOf('=') >= 0);
		}
	}

	/// <summary>
	/// Accumulate all parameters. Inherits from TestForParameter so it can inherit
	/// the function that defines one.
	/// </summary>
	class AccumulateParameters : TestForParameter
	{
		List<string> m_list = new List<string>();

		public override bool Visit(XmlAttribute xa)
		{
			if (IsParameter(xa.Value))
				m_list.Add(xa.Value);
			return false; // this one wants to accumulate them all
		}

		public List<string> Parameters
		{
			get { return m_list; }
		}
	}

	/// <summary>
	/// This one modifies the attribute, replacing the parameter with its default.
	/// </summary>
	class ReplaceParamWithDefault : TestForParameter
	{
		public override bool Visit(XmlAttribute xa)
		{
			if (!IsParameter(xa.Value))
				return false;
			xa.Value = xa.Value.Substring(xa.Value.IndexOf('=') + 1);
			return false;
		}

	}
	/// <summary>
	/// This one modifies the attribute, replacing the default value of the named parameter.
	/// </summary>
	class ReplaceParamDefault : IAttributeVisitor
	{
		string m_paramPrefix;
		string m_defVal;

		public ReplaceParamDefault(string paramName, string defVal)
		{
			m_paramPrefix = "$" + paramName + "=";
			m_defVal = defVal;
		}

		public bool Visit(XmlAttribute xa)
		{
			if (!xa.Value.StartsWith(m_paramPrefix))
				return false;
			xa.Value = m_paramPrefix + m_defVal;
			return true;
		}

	}
}
