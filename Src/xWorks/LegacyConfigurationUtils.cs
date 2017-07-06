// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel.Core.Cellar;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class holds methods which are used by legacy view configuration settings as well as migration of these
	/// configuration settings to new formats.
	/// <note>Most of these methods were moved here from the XmlDocConfigureDlg class</note>
	/// </summary>
	public static class LegacyConfigurationUtils
	{
		internal static void BuildTreeFromLayoutAndParts(XElement configurationLayoutsNode, ILayoutConverter converter)
		{
			var layoutTypes = new List<XElement>();
			layoutTypes.AddRange(configurationLayoutsNode.Elements().Where(x => x.Name.LocalName == "layoutType"));
			Debug.Assert(layoutTypes.Count > 0);
			var xnConfig = layoutTypes[0].Element("configure");
			Debug.Assert(xnConfig != null);
			var configClass = XmlUtils.GetManditoryAttributeValue(xnConfig, "class");
			foreach (var xn in converter.GetLayoutTypes())
			{
				var xnConfigure = xn.Element("configure");
				if (XmlUtils.GetManditoryAttributeValue(xnConfigure, "class") == configClass)
				{
					layoutTypes.Add(xn);
				}
			}
			foreach (var xnLayoutType in layoutTypes)
			{
				if (xnLayoutType.Name.LocalName != "layoutType")
					continue;
				var sLabel = XmlUtils.GetOptionalAttributeValue(xnLayoutType, "label");
				if (sLabel == "$wsName") // if the label for the layout matches $wsName then this is a reversal index layout
				{
					string sLayout = XmlUtils.GetOptionalAttributeValue(xnLayoutType, "layout");
					Debug.Assert(sLayout.EndsWith("-$ws"));
					bool fReversalIndex = true;
					foreach (var config in xnLayoutType.Elements())
					{
						if (config.Name.LocalName != "configure")
							continue;
						var sClass = XmlUtils.GetOptionalAttributeValue(config, "class");
						if (sClass != "ReversalIndexEntry")
						{
							fReversalIndex = false;
							break;
						}
					}
					if (!fReversalIndex)
						continue;
					foreach(var ri in converter.Cache.LangProject.LexDbOA.CurrentReversalIndices)
					{
						var ws = converter.Cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem);
						var sWsTag = ws.Id;
						converter.ExpandWsTaggedNodes(sWsTag);	// just in case we have a new index.
						// Create a copy of the layoutType node for the specific writing system.
						var xnRealLayout = CreateWsSpecficLayoutType(xnLayoutType, ws.DisplayLabel, sLayout.Replace("$ws", sWsTag), sWsTag);
						var rgltnStyle = BuildLayoutTree(xnRealLayout, converter);
						converter.AddDictionaryTypeItem(xnRealLayout, rgltnStyle);
					}
				}
				else
				{
					var rgltnStyle = BuildLayoutTree(xnLayoutType, converter);
					if (rgltnStyle.Count > 0)
						converter.AddDictionaryTypeItem(xnLayoutType, rgltnStyle);
				}
			}
		}

		private static XElement CreateWsSpecficLayoutType(XElement xnLayoutType, string sWsLabel,
																		 string sWsLayout, string sWsTag)
		{
			var xnRealLayout = xnLayoutType.Clone();
			if (xnRealLayout.HasAttributes)
			{
				xnRealLayout.Attribute("label").Value = sWsLabel;
				xnRealLayout.Attribute("layout").Value = sWsLayout;
				foreach (var config in xnRealLayout.Elements())
				{
					if (config.Name.LocalName != "configure")
						continue;
					var sInternalLayout = XmlUtils.GetOptionalAttributeValue(config, "layout");
					Debug.Assert(sInternalLayout.EndsWith("-$ws"));
					if (config.HasAttributes)
						config.Attribute("layout").Value = sInternalLayout.Replace("$ws", sWsTag);
				}
			}
			return xnRealLayout;
		}

		/// <summary>
		/// Configure LayoutType via its child configure nodes
		/// </summary>
		internal static List<XmlDocConfigureDlg.LayoutTreeNode> BuildLayoutTree(XElement xnLayoutType, ILayoutConverter converter)
		{
			var treeNodeList = new List<XmlDocConfigureDlg.LayoutTreeNode>();
			foreach (var config in xnLayoutType.Elements())
			{
				// expects a configure element
				if (config.Name.LocalName != "configure")
					continue;
				var ltn = BuildMainLayout(config, converter);
				if (ltn != null)
				{
					if (XmlUtils.GetOptionalBooleanAttributeValue(config, "hideConfig", false))
						treeNodeList.AddRange(Enumerable.Cast<XmlDocConfigureDlg.LayoutTreeNode>(ltn.Nodes));
					else
						treeNodeList.Add(ltn);
				}
			}
			return treeNodeList;
		}

		/// <summary>
		/// Builds control tree nodes based on a configure element
		/// </summary>
		private static XmlDocConfigureDlg.LayoutTreeNode BuildMainLayout(XElement config, ILayoutConverter converter)
		{
			var mainLayoutNode = new XmlDocConfigureDlg.LayoutTreeNode(config, converter, null);
			converter.SetOriginalIndexForNode(mainLayoutNode);
			var className = mainLayoutNode.ClassName;
			var layoutName = mainLayoutNode.LayoutName;
			var layout = converter.GetLayoutElement(className, layoutName);
			if (layout == null)
			{
				var msg = String.Format("Cannot configure layout {0} of class {1} because it does not exist",layoutName, className);
				converter.LogConversionError(msg);
				return null;
			}
			mainLayoutNode.ParentLayout = layout;	// not really the parent layout, but the parent of this node's children
			string sVisible = XmlUtils.GetOptionalAttributeValue(layout, "visibility");
			mainLayoutNode.Checked = sVisible != "never";
			AddChildNodes(layout, mainLayoutNode, mainLayoutNode.Nodes.Count, converter);
			mainLayoutNode.OriginalNumberOfSubnodes = mainLayoutNode.Nodes.Count;
			return mainLayoutNode;
		}

		internal static void AddChildNodes(XElement layout, XmlDocConfigureDlg.LayoutTreeNode ltnParent, int iStart, ILayoutConverter converter)
		{
			bool fMerging = iStart < ltnParent.Nodes.Count;
			string className = XmlUtils.GetManditoryAttributeValue(layout, "class");
			var nodes = PartGenerator.GetGeneratedChildren(layout, converter.Cache,
																						new[] { "ref", "label" });
			foreach (var node in nodes)
			{
				if (node.Name.LocalName == "sublayout")
				{
					Debug.Assert(!fMerging);
					string subLayoutName = XmlUtils.GetOptionalAttributeValue(node, "name", null);
					XElement subLayout;
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
					var ltnOld = FindMatchingNode(ltnParent, node);
					if (ltnOld != null)
						continue;
					var sRef = XmlUtils.GetManditoryAttributeValue(node, "ref");
					var part = converter.GetPartElement(className, sRef);
					if (part == null && sRef != "$child")
						continue;
					var fHide = XmlUtils.GetOptionalBooleanAttributeValue(node, "hideConfig", false);
					XmlDocConfigureDlg.LayoutTreeNode ltn;
					var cOrig = 0;
					if (!fHide)
					{
						ltn = new XmlDocConfigureDlg.LayoutTreeNode(node, converter, className)
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
							ProcessChildNodes(part.Elements(), className, ltn, converter);
						ltn.OriginalNumberOfSubnodes = ltn.Nodes.Count;
						ltn.AddingSubnodes = fOldAdding;
						if (fHide)
						{
							var cNew = ltn.Nodes.Count - cOrig;
							if(cNew > 1)
							{
								var msg = String.Format("{0} nodes for a hidden PartRef ({1})!", cNew, node.GetOuterXml());
								converter.LogConversionError(msg);
							}
						}
					}
					finally
					{
						converter.LayoutLevels.Pop();
					}
				}
			}
		}

		/// <summary>
		/// Walk the tree of child nodes, storing information for each &lt;obj&gt; or &lt;seq&gt;
		/// node.
		/// </summary>
		private static void ProcessChildNodes(IEnumerable<XElement> xmlNodeList, string className, XmlDocConfigureDlg.LayoutTreeNode ltn, ILayoutConverter converter)
		{
			foreach (var xn in xmlNodeList)
			{
				if (xn.Name.LocalName == "obj" || xn.Name.LocalName == "seq" || xn.Name.LocalName == "objlocal")
				{
					StoreChildNodeInfo(xn, className, ltn, converter);
				}
				else
				{
					ProcessChildNodes(xn.Elements(), className, ltn, converter);
				}
			}
		}

		private static void StoreChildNodeInfo(XElement xn, string className, XmlDocConfigureDlg.LayoutTreeNode ltn, ILayoutConverter converter)
		{
			var sField = XmlUtils.GetManditoryAttributeValue(xn, "field");
			var xnCaller = converter.LayoutLevels.PartRef;
			if (xnCaller == null)
				xnCaller = ltn.Configuration;
			var hideConfig = xnCaller != null && XmlUtils.GetOptionalBooleanAttributeValue(xnCaller, "hideConfig", false);
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
					var sShowAsIndentedPara = XmlUtils.GetOptionalAttributeValue(ltn.Configuration, "showasindentedpara");
					ltn.ShowComplexFormParaConfig = !String.IsNullOrEmpty(sShowAsIndentedPara);
				}
			}
			var fRecurse = XmlUtils.GetOptionalBooleanAttributeValue(ltn.Configuration, "recurseConfig", true);
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
						var sClassT = XmlUtils.GetOptionalAttributeValue(xn, "targetclass");
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
			XElement subLayout = null;
			if (rgsClasses.Length > 0)
				subLayout = converter.GetLayoutElement(rgsClasses[0], sLayout);

			if (subLayout != null)
			{
				var iStart = ltn.Nodes.Count;
				var cNodes = subLayout.Elements().Count();
				AddChildNodes(subLayout, ltn, iStart, converter);

				var fRepeatedConfig = XmlUtils.GetOptionalBooleanAttributeValue(xn, "repeatedConfig", false);
				if (fRepeatedConfig)
					return;		// repeats an earlier part element (probably as a result of <if>s)
				for (var i = 1; i < rgsClasses.Length; i++)
				{
					var mergedLayout = converter.GetLayoutElement(rgsClasses[i], sLayout);
					if (mergedLayout != null && mergedLayout.Elements().Count() == cNodes)
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

		private static XmlDocConfigureDlg.LayoutTreeNode FindMatchingNode(XmlDocConfigureDlg.LayoutTreeNode ltn, XElement node)
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

		private static bool NodesMatch(XElement first, XElement second)
		{
			if (first.Name != second.Name)
				return false;
			if((!first.HasAttributes) && (second.HasAttributes))
				return false;
			if(!first.HasAttributes)
			{
				return ChildNodesMatch(first.Elements().ToList(), second.Elements().ToList());
			}
			if(first.Attributes().Count() != second.Attributes().Count())
			{
				return false;
			}

			var firstAttSet = new SortedList<string, string>();
			var secondAttSet = new SortedList<string, string>();
			var firstAttributes = first.Attributes().ToList();
			var secondAttributes = second.Attributes().ToList();
			for (var i = 0; i < firstAttributes.Count; ++i)
			{
				firstAttSet.Add(firstAttributes[i].Name.LocalName, firstAttributes[i].Value);
				secondAttSet.Add(secondAttributes[i].Name.LocalName, secondAttributes[i].Value);
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
		private static bool ChildNodesMatch(IList<XElement> firstNodeList, IList<XElement> secondNodeList)
		{
			if (firstNodeList.Count != secondNodeList.Count)
				return false;
			var firstAtSet = new SortedList<string, XElement>();
			var secondAtSet = new SortedList<string, XElement>();
			for (var i = 0; i < firstNodeList.Count; ++i)
			{
				firstAtSet.Add(firstNodeList[i].Name.LocalName, firstNodeList[i]);
				secondAtSet.Add(secondNodeList[i].Name.LocalName, secondNodeList[i]);
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

		internal static XElement GetLayoutElement(Inventory layouts, string className, string layoutName)
		{
			return layouts.GetElement("layout", new[] { className, "jtview", layoutName, null });
		}

		internal static XElement GetPartElement(Inventory parts, string className, string sRef)
		{
			return parts.GetElement("part", new[] { className + "-Jt-" + sRef });
		}
	}
}