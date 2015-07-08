// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class holds methods which are used by legacy view configuration settings as well as migration of these
	/// configuration settings to new formats.
	/// <note>Most of these methods were moved here from the XmlDocConfigureDlg class</note>
	/// </summary>
	static public class LegacyConfigurationUtils
	{
		internal static void BuildTreeFromLayoutAndParts(XmlNode configurationLayoutsNode, ILayoutConverter converter)
		{
			var layoutTypes = new List<XmlNode>();
			layoutTypes.AddRange(configurationLayoutsNode.ChildNodes.OfType<XmlNode>().Where(x => x.Name == "layoutType"));
			Debug.Assert(layoutTypes.Count > 0);
			var xnConfig = layoutTypes[0].SelectSingleNode("configure");
			Debug.Assert(xnConfig != null);
			var configClass = XmlUtils.GetManditoryAttributeValue(xnConfig, "class");
			foreach (var xn in converter.GetLayoutTypes())
			{
				var xnConfigure = xn.SelectSingleNode("configure");
				if (XmlUtils.GetManditoryAttributeValue(xnConfigure, "class") == configClass)
					layoutTypes.Add(xn);
			}
			foreach (var xnLayoutType in layoutTypes)
			{
				if (xnLayoutType is XmlComment || xnLayoutType.Name != "layoutType")
					continue;
				string sLabel = XmlUtils.GetAttributeValue(xnLayoutType, "label");
				if (sLabel == "$wsName") // if the label for the layout matches $wsName then this is a reversal index layout
				{
					string sLayout = XmlUtils.GetAttributeValue(xnLayoutType, "layout");
					Debug.Assert(sLayout.EndsWith("-$ws"));
					bool fReversalIndex = true;
					foreach (XmlNode config in xnLayoutType.ChildNodes)
					{
						if (config is XmlComment || config.Name != "configure")
							continue;
						string sClass = XmlUtils.GetAttributeValue(config, "class");
						if (sClass != "ReversalIndexEntry")
						{
							fReversalIndex = false;
							break;
						}
					}
					if (!fReversalIndex)
						continue;
					foreach(IReversalIndex ri in converter.Cache.LangProject.LexDbOA.CurrentReversalIndices)
					{
						IWritingSystem ws = converter.Cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem);
						string sWsTag = ws.Id;
						converter.ExpandWsTaggedNodes(sWsTag);	// just in case we have a new index.
						// Create a copy of the layoutType node for the specific writing system.
						XmlNode xnRealLayout = CreateWsSpecficLayoutType(xnLayoutType,
																						 ws.DisplayLabel, sLayout.Replace("$ws", sWsTag), sWsTag);
						List<XmlDocConfigureDlg.LayoutTreeNode> rgltnStyle = BuildLayoutTree(xnRealLayout, converter);
						converter.AddDictionaryTypeItem(xnRealLayout, rgltnStyle);
					}
				}
				else
				{
					List<XmlDocConfigureDlg.LayoutTreeNode> rgltnStyle = BuildLayoutTree(xnLayoutType, converter);
					converter.AddDictionaryTypeItem(xnLayoutType, rgltnStyle);
				}
			}
		}

		private static XmlNode CreateWsSpecficLayoutType(XmlNode xnLayoutType, string sWsLabel,
																		 string sWsLayout, string sWsTag)
		{
			XmlNode xnRealLayout = xnLayoutType.Clone();
			if (xnRealLayout.Attributes != null)
			{
				xnRealLayout.Attributes["label"].Value = sWsLabel;
				xnRealLayout.Attributes["layout"].Value = sWsLayout;
				foreach (XmlNode config in xnRealLayout.ChildNodes)
				{
					if (config is XmlComment || config.Name != "configure")
						continue;
					string sInternalLayout = XmlUtils.GetAttributeValue(config, "layout");
					Debug.Assert(sInternalLayout.EndsWith("-$ws"));
					if (config.Attributes != null)
						config.Attributes["layout"].Value = sInternalLayout.Replace("$ws", sWsTag);
				}
			}
			return xnRealLayout;
		}

		/// <summary>
		/// Configure LayoutType via its child configure nodes
		/// </summary>
		internal static List<XmlDocConfigureDlg.LayoutTreeNode> BuildLayoutTree(XmlNode xnLayoutType, ILayoutConverter converter)
		{
			var treeNodeList = new List<XmlDocConfigureDlg.LayoutTreeNode>();
			foreach (XmlNode config in xnLayoutType.ChildNodes)
			{   // expects a configure element
				if (config is XmlComment || config.Name != "configure")
					continue;
				var ltn = BuildMainLayout(config, converter);
				if (XmlUtils.GetOptionalBooleanAttributeValue(config, "hideConfig", false))
					treeNodeList.AddRange(Enumerable.Cast<XmlDocConfigureDlg.LayoutTreeNode>(ltn.Nodes));
				else
					treeNodeList.Add(ltn);
			}
			return treeNodeList;
		}

		/// <summary>
		/// Builds control tree nodes based on a configure element
		/// </summary>
		private static XmlDocConfigureDlg.LayoutTreeNode BuildMainLayout(XmlNode config, ILayoutConverter converter)
		{
			var mainLayoutNode = new XmlDocConfigureDlg.LayoutTreeNode(config, converter.StringTable, null);
			converter.SetOriginalIndexForNode(mainLayoutNode);
			string className = mainLayoutNode.ClassName;
			string layoutName = mainLayoutNode.LayoutName;
			XmlNode layout = converter.GetLayoutElement(className, layoutName);
			if (layout == null)
				throw new Exception("Cannot configure layout " + layoutName + " of class " + className + " because it does not exist");
			mainLayoutNode.ParentLayout = layout;	// not really the parent layout, but the parent of this node's children
			string sVisible = XmlUtils.GetAttributeValue(layout, "visibility");
			mainLayoutNode.Checked = sVisible != "never";
			AddChildNodes(layout, mainLayoutNode, mainLayoutNode.Nodes.Count, converter);
			mainLayoutNode.OriginalNumberOfSubnodes = mainLayoutNode.Nodes.Count;
			return mainLayoutNode;
		}

		internal static void AddChildNodes(XmlNode layout, XmlDocConfigureDlg.LayoutTreeNode ltnParent, int iStart, ILayoutConverter converter)
		{
			bool fMerging = iStart < ltnParent.Nodes.Count;
			int iNode = iStart;
			string className = XmlUtils.GetManditoryAttributeValue(layout, "class");
			List<XmlNode> nodes = PartGenerator.GetGeneratedChildren(layout, converter.Cache,
																						new[] { "ref", "label" });
			foreach (XmlNode node in nodes)
			{
				XmlNode subLayout;
				if (node.Name == "sublayout")
				{
					Debug.Assert(!fMerging);
					string subLayoutName = XmlUtils.GetOptionalAttributeValue(node, "name", null);
					if (subLayoutName == null)
					{
						subLayout = node; // a sublayout lacking a name contains the part refs directly.
					}
					else
					{
						subLayout = converter.GetLayoutElement(className, subLayoutName);
					}
					if (subLayout != null)
						AddChildNodes(subLayout, ltnParent, ltnParent.Nodes.Count, converter);
				}
				else if (node.Name == "part")
				{
					// Check whether this node has already been added to this parent.  Don't add
					// it if it's already there!
					XmlDocConfigureDlg.LayoutTreeNode ltnOld = FindMatchingNode(ltnParent, node);
					if (ltnOld != null)
						continue;
					string sRef = XmlUtils.GetManditoryAttributeValue(node, "ref");
					XmlNode part = converter.GetPartElement(className, sRef);
					if (part == null && sRef != "$child")
						continue;
					bool fHide = XmlUtils.GetOptionalBooleanAttributeValue(node, "hideConfig", false);
					XmlDocConfigureDlg.LayoutTreeNode ltn;
					var cOrig = 0;
					if (!fHide)
					{
						ltn = new XmlDocConfigureDlg.LayoutTreeNode(node, converter.StringTable, className)
							{
								OriginalIndex = ltnParent.Nodes.Count,
								ParentLayout = layout,
								HiddenNode = converter.LayoutLevels.HiddenPartRef,
								HiddenNodeLayout = converter.LayoutLevels.HiddenLayout
							};
						if (!String.IsNullOrEmpty(ltn.LexRelType))
							converter.BuildRelationTypeList(ltn);
						if (!String.IsNullOrEmpty(ltn.EntryType))
							converter.BuildEntryTypeList(ltn, ltnParent.LayoutName);
						//if (fMerging)
						//((LayoutTreeNode)ltnParent.Nodes[iNode]).MergedNodes.Add(ltn);
						//else
						ltnParent.Nodes.Add(ltn);
					}
					else
					{
						Debug.Assert(!fMerging);
						ltn = ltnParent;
						cOrig = ltn.Nodes.Count;
						if (className == "StTxtPara")
						{
							ltnParent.HiddenChildLayout = layout;
							ltnParent.HiddenChild = node;
						}
					}
					try
					{
						converter.LayoutLevels.Push(node, layout);
						var fOldAdding = ltn.AddingSubnodes;
						ltn.AddingSubnodes = true;
						if (part != null)
							ProcessChildNodes(part.ChildNodes, className, ltn, converter);
						ltn.OriginalNumberOfSubnodes = ltn.Nodes.Count;
						ltn.AddingSubnodes = fOldAdding;
						if (fHide)
						{
							var cNew = ltn.Nodes.Count - cOrig;
							var msg = String.Format("{0} nodes for a hidden PartRef ({1})!", cNew, node.OuterXml);
							converter.LogConversionError(msg);
							//Debug.Assert(cNew <= 1, msg);
							//if (cNew > 1)
							//    Debug.WriteLine(msg);
						}
					}
					finally
					{
						converter.LayoutLevels.Pop();
					}
					++iNode;
				}
			}
		}

		/// <summary>
		/// Walk the tree of child nodes, storing information for each &lt;obj&gt; or &lt;seq&gt;
		/// node.
		/// </summary>
		/// <param name="xmlNodeList"></param>
		/// <param name="className"></param>
		/// <param name="ltn"></param>
		private static void ProcessChildNodes(XmlNodeList xmlNodeList, string className, XmlDocConfigureDlg.LayoutTreeNode ltn, ILayoutConverter converter)
		{
			foreach (XmlNode xn in xmlNodeList)
			{
				if (xn is XmlComment)
					continue;
				if (xn.Name == "obj" || xn.Name == "seq" || xn.Name == "objlocal")
				{
					StoreChildNodeInfo(xn, className, ltn, converter);
				}
				else
				{
					ProcessChildNodes(xn.ChildNodes, className, ltn, converter);
				}
			}
		}

		private static void StoreChildNodeInfo(XmlNode xn, string className, XmlDocConfigureDlg.LayoutTreeNode ltn, ILayoutConverter converter)
		{
			string sField = XmlUtils.GetManditoryAttributeValue(xn, "field");
			XmlNode xnCaller = converter.LayoutLevels.PartRef;
			if (xnCaller == null)
				xnCaller = ltn.Configuration;
			bool hideConfig = xnCaller == null ? false : XmlUtils.GetOptionalBooleanAttributeValue(xnCaller, "hideConfig", false);
			// Insert any special configuration appropriate for this property...unless the caller is hidden, in which case,
			// we don't want to configure it at all.
			if (!ltn.IsTopLevel && !hideConfig)
			{
				if (sField == "Senses" && (ltn.ClassName == "LexEntry" || ltn.ClassName == "LexSense"))
				{
					ltn.ShowSenseConfig = true;
				}
				else if (sField == "ReferringSenses" && ltn.ClassName == "ReversalIndexEntry")
				{
					ltn.ShowSenseConfig = true;
				}
				if (sField == "MorphoSyntaxAnalysis" && ltn.ClassName == "LexSense")
				{
					ltn.ShowGramInfoConfig = true;
				}
				if (sField == "VisibleComplexFormBackRefs" || sField == "ComplexFormsNotSubentries")
				{
					//The existence of the attribute is important for this setting, not its value!
					var sShowAsIndentedPara = XmlUtils.GetAttributeValue(ltn.Configuration, "showasindentedpara");
					ltn.ShowComplexFormParaConfig = !String.IsNullOrEmpty(sShowAsIndentedPara);
				}
			}
			bool fRecurse = XmlUtils.GetOptionalBooleanAttributeValue(ltn.Configuration, "recurseConfig", true);
			if (!fRecurse)
			{
				// We don't want to recurse forever just because senses have subsenses, which
				// can have subsenses, which can ...
				// Or because entries have subentries (in root type layouts)...
				ltn.UseParentConfig = true;
				return;
			}
			var sLayout = XmlVc.GetLayoutName(xn, xnCaller);
			var clidDst = 0;
			string sClass = null;
			string sTargetClasses = null;
			try
			{
				// Failure should be fairly unusual, but, for example, part MoForm-Jt-FormEnvPub attempts to display
				// the property PhoneEnv inside an if that checks that the MoForm is one of the subclasses that has
				// the PhoneEnv property. MoForm itself does not.
				if (!((IFwMetaDataCacheManaged)converter.Cache.DomainDataByFlid.MetaDataCache).FieldExists(className, sField, true))
					return;
				var flid = converter.Cache.DomainDataByFlid.MetaDataCache.GetFieldId(className, sField, true);
				var type = (CellarPropertyType)converter.Cache.DomainDataByFlid.MetaDataCache.GetFieldType(flid);
				Debug.Assert(type >= CellarPropertyType.MinObj);
				if (type >= CellarPropertyType.MinObj)
				{
					var mdc = converter.Cache.MetaDataCacheAccessor;
					sTargetClasses = XmlUtils.GetOptionalAttributeValue(xn, "targetclasses");
					clidDst = mdc.GetDstClsId(flid);
					if (clidDst == 0)
						sClass = XmlUtils.GetOptionalAttributeValue(xn, "targetclass");
					else
						sClass = mdc.GetClassName(clidDst);
					if (clidDst == StParaTags.kClassId)
					{
						string sClassT = XmlUtils.GetOptionalAttributeValue(xn, "targetclass");
						if (!String.IsNullOrEmpty(sClassT))
							sClass = sClassT;
					}
				}
			}
			catch
			{
				return;
			}
			if (clidDst == MoFormTags.kClassId && !sLayout.StartsWith("publi"))
				return;	// ignore the layouts used by the LexEntry-Jt-Headword part.
			if (String.IsNullOrEmpty(sLayout) || String.IsNullOrEmpty(sClass))
				return;
			if (sTargetClasses == null)
				sTargetClasses = sClass;
			string[] rgsClasses = sTargetClasses.Split(new[] { ',', ' ' },
																	 StringSplitOptions.RemoveEmptyEntries);
			XmlNode subLayout = null;
			if(rgsClasses.Length > 0)
				subLayout = converter.GetLayoutElement(rgsClasses[0], sLayout);

			if (subLayout != null)
			{
				int iStart = ltn.Nodes.Count;
				int cNodes = subLayout.ChildNodes.Count;
				AddChildNodes(subLayout, ltn, iStart, converter);

				bool fRepeatedConfig = XmlUtils.GetOptionalBooleanAttributeValue(xn, "repeatedConfig", false);
				if (fRepeatedConfig)
					return;		// repeats an earlier part element (probably as a result of <if>s)
				for (int i = 1; i < rgsClasses.Length; i++)
				{
					XmlNode mergedLayout = converter.GetLayoutElement(rgsClasses[i], sLayout);
					if (mergedLayout != null && mergedLayout.ChildNodes.Count == cNodes)
					{
						AddChildNodes(mergedLayout, ltn, iStart, converter);
					}
				}
			}
			else
			{
				// The "layout" in a part node can actually refer directly to another part, so check
				// for that possibility.
				var subPart = converter.GetPartElement(rgsClasses[0], sLayout) ?? converter.GetPartElement(className, sLayout);
				if (subPart == null && !sLayout.EndsWith("-en"))
				{
					// Complain if we can't find either a layout or a part, and the name isn't tagged
					// for a writing system.  (We check only for English, being lazy.)
					var msg = String.Format("Missing jtview layout for class=\"{0}\" name=\"{1}\"",
													rgsClasses[0], sLayout);
					converter.LogConversionError(msg);
				}
			}
		}

		private static XmlDocConfigureDlg.LayoutTreeNode FindMatchingNode(XmlDocConfigureDlg.LayoutTreeNode ltn, XmlNode node)
		{
			if (ltn == null || node == null)
				return null;
			foreach (XmlDocConfigureDlg.LayoutTreeNode ltnSub in ltn.Nodes)
			{
				if (NodesMatch(ltnSub.Configuration, node))
					return ltnSub;
			}
			return FindMatchingNode(ltn.Parent as XmlDocConfigureDlg.LayoutTreeNode, node);
		}

		private static bool NodesMatch(XmlNode first, XmlNode second)
		{
			if (first.Name != second.Name)
				return false;
			if((first.Attributes == null) && (second.Attributes != null))
				return false;
			if(first.Attributes == null)
			{
				return ChildNodesMatch(first.ChildNodes, second.ChildNodes);
			}
			if(first.Attributes.Count != second.Attributes.Count)
			{
				return false;
			}

			var firstAttSet = new SortedList<string, string>();
			var secondAttSet = new SortedList<string, string>();
			for (int i = 0; i < first.Attributes.Count; ++i)
			{
				firstAttSet.Add(first.Attributes[i].Name, first.Attributes[i].Value);
				secondAttSet.Add(second.Attributes[i].Name, second.Attributes[i].Value);
			}
			using (var firstIter = firstAttSet.GetEnumerator())
			using (var secondIter = secondAttSet.GetEnumerator())
			{
				for(;firstIter.MoveNext() && secondIter.MoveNext();)
				{
					if(!firstIter.Current.Equals(secondIter.Current))
						return false;
				}
				return true;
			}
		}

		/// <summary>
		/// This method should sort the node lists and call NodesMatch with each pair.
		/// </summary>
		/// <param name="firstNodeList"></param>
		/// <param name="secondNodeList"></param>
		/// <returns></returns>
		private static bool ChildNodesMatch(XmlNodeList firstNodeList, XmlNodeList secondNodeList)
		{
			if (firstNodeList.Count != secondNodeList.Count)
				return false;
			var firstAtSet = new SortedList<string, XmlNode>();
			var secondAtSet = new SortedList<string, XmlNode>();
			for (int i = 0; i < firstNodeList.Count; ++i)
			{
				firstAtSet.Add(firstNodeList[i].Name, firstNodeList[i]);
				secondAtSet.Add(secondNodeList[i].Name, secondNodeList[i]);
			}
			using (var firstIter = firstAtSet.GetEnumerator())
			using (var secondIter = secondAtSet.GetEnumerator())
			{
				for (; firstIter.MoveNext() && secondIter.MoveNext(); )
				{
					if (!NodesMatch(firstIter.Current.Value, secondIter.Current.Value))
						return false;
				}
				return true;
			}
		}

		internal static XmlNode GetLayoutElement(Inventory layouts, string className, string layoutName)
		{
			return layouts.GetElement("layout", new[] { className, "jtview", layoutName, null });
		}

		internal static XmlNode GetPartElement(Inventory parts, string className, string sRef)
		{
			return parts.GetElement("part", new[] { className + "-Jt-" + sRef });
		}
	}
}