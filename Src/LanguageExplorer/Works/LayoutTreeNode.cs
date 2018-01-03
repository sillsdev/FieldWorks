// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Works
{
	internal class LayoutTreeNode : TreeNode
	{
		// These are basic values that we need to know for every node.
		// **Important Note**: In most cases, adding a member variable here means you need to add it to
		// CopyValuesTo() also, so that a duplicate node will end up with the right values.
		XElement m_xnConfig;
		string m_sLayoutName;
		string m_sPartName;
		string m_sClassName;
		string m_sLabel;
		string m_sVisibility;
		bool m_fUseParentConfig;
		bool m_fShowSenseConfig;

		string m_sFlowType;
		// These values depend on the particular node, and affect what is displayed in the
		// details pane.  If a string value is null, then the corresponding control (and
		// label) is not shown.
		string m_sWsLabel;

		bool m_fShowComplexFormPara;

		// These are used to trace creating, deleting, and moving nodes.
		string m_sDup;
		int m_cSubnodes = -1;
		int m_idxOrig = -1;
		// **NB**: If you're planning to add a member variable here, see the Important Note above.
		XElement m_xnCallingLayout;

		/// <summary>
		/// This node is a hidden child that provides/receives the style name.
		/// </summary>
		XElement m_xnHiddenChild;

		bool m_fStyleFromHiddenChild;
		readonly List<LayoutTreeNode> m_rgltnMerged = new List<LayoutTreeNode>();

		internal LayoutTreeNode(XElement config, ILayoutConverter converter, string classParent)
		{
			m_xnConfig = config;
			m_sLabel = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(config, "label", null));
			if (config.Name == "configure")
			{
				m_sClassName = XmlUtils.GetMandatoryAttributeValue(config, "class");
				m_sLayoutName = XmlUtils.GetMandatoryAttributeValue(config, "layout");
				m_sPartName = String.Empty;
				m_sVisibility = "required";
			}
			else if (config.Name == "part")
			{
				m_sClassName = classParent;
				var sRef = XmlUtils.GetMandatoryAttributeValue(config, "ref");
				if (m_sLabel == null && converter.UseStringTable)
				{
					m_sLabel = StringTable.Table.LocalizeAttributeValue(sRef);
				}
				if (config.Parent != null && config.Parent.Name.LocalName == "layout")
				{
					m_sLayoutName = XmlUtils.GetMandatoryAttributeValue(config.Parent, "name");
				}
				else
				{
					m_sLayoutName = string.Empty;
				}
				m_sPartName = $"{classParent}-Jt-{sRef}";
				m_sVisibility = XmlUtils.GetOptionalAttributeValue(config, "visibility", "always");
				ContentVisible = m_sVisibility.ToLowerInvariant() != "never";
				Param = XmlUtils.GetOptionalAttributeValue(config, "param");

				m_sWsLabel = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(config, "ws"));
				WsType = XmlUtils.GetOptionalAttributeValue(config, "wsType");
				if (m_sWsLabel != null && String.IsNullOrEmpty(WsType))
				{
					// Try to calculate a WS type from the WS label.
					var ichVern = m_sWsLabel.ToLowerInvariant().IndexOf("vern");
					var ichAnal = m_sWsLabel.ToLowerInvariant().IndexOf("anal");
					var ichPronun = m_sWsLabel.ToLowerInvariant().IndexOf("pronun");
					var ichRevers = m_sWsLabel.ToLowerInvariant().IndexOf("revers");
					if (ichVern >= 0 && ichAnal >= 0 && ichVern > ichAnal)
					{
						WsType = "analysis vernacular";
					}
					else if (ichVern >= 0 && ichAnal >= 0 && ichAnal > ichVern)
					{
						WsType = "vernacular analysis";
					}
					else if (ichVern >= 0)
					{
						WsType = "vernacular";
					}
					else if (ichAnal >= 0)
					{
						WsType = "analysis";
					}
					else if (ichPronun >= 0)
					{
						WsType = "pronunciation";
					}
					else if (ichRevers >= 0)
					{
						WsType = "reversal";
					}
					else
					{
						WsType = "vernacular analysis";  // who knows???
						var refValue = string.Empty;
						var wsValue = string.Empty;
						if (config.HasAttributes)
						{
							refValue = config.Attribute("ref").Value;
							wsValue = config.Attribute("ws").Value;
						}
						converter.LogConversionError($"This layout node ({refValue}) does not specify @wsType and we couldn't compute something reasonable from @ws='{wsValue}' so we're setting @wsType to 'vernacular analysis'");
					}
					// store the wsType attribute on the node, so that if 'ws' changes to something
					// specific, we still know what type of wss to provide options for in the m_lvWritingSystems.
					var xa = new XAttribute("wsType", WsType);
					config.Add(xa);
				}
				WsType = StringServices.GetWsSpecWithoutPrefix(WsType);
				if (WsType != null && m_sWsLabel == null)
				{
					m_sWsLabel = string.Empty;
				}
				string sSep = null;
				// By default, if we have a ws type or ws label we should be able to show multiple wss,
				// and thus need a separator between them.
				if (!string.IsNullOrEmpty(m_sWsLabel) || !String.IsNullOrEmpty(WsType))
				{
					sSep = " ";
				}
				BeforeStyleName = XmlUtils.GetOptionalAttributeValue(config, "beforeStyle");
				AllowBeforeStyle = !String.IsNullOrEmpty(BeforeStyleName);
				Before = XmlUtils.GetOptionalAttributeValue(config, "before", "");
				Between = XmlUtils.GetOptionalAttributeValue(config, "sep", sSep);
				After = XmlUtils.GetOptionalAttributeValue(config, "after", " ");

				StyleName = XmlUtils.GetOptionalAttributeValue(config, "style");
				m_sFlowType = XmlUtils.GetOptionalAttributeValue(config, "flowType", "span");
				if (m_sFlowType == "span")
				{
					AllowCharStyle = !XmlUtils.GetOptionalBooleanAttributeValue(config, "disallowCharStyle", false);
					if (Before == null)
					{
						Before = string.Empty;
					}
					if (After == null)
					{
						After = string.Empty;
					}
				}
				// Special handling for div flow elements, which can contain a sequence of paragraphs.
				else if (m_sFlowType == "div")
				{
					AllowParaStyle = m_sClassName == "StText";
					AllowDivParaStyle = false;
					if (AllowParaStyle)
					{
						// We'll be getting the style name from a child layout.
						Debug.Assert(string.IsNullOrEmpty(StyleName));
					}
					else
					{
						StyleName = XmlUtils.GetOptionalAttributeValue(config, "parastyle");
						AllowDivParaStyle = !string.IsNullOrEmpty(StyleName);
					}
					Between = null;
					After = null;
					// This is subtly differerent: in a div, we will hide or disable the before control (value null) unless the XML
					// explicitly calls for it by including the attribute. Above we provided an empty string default,
					// which enables the empty control.
					Before = XmlUtils.GetOptionalAttributeValue(config, "before");
				}
				else if (m_sFlowType == "para")
				{
					AllowParaStyle = !string.IsNullOrEmpty(StyleName);
				}
				else if (m_sFlowType == "divInPara")
				{
					Before = After = Between = null; // suppress the whole separators group since each item is a para
					StyleName = XmlUtils.GetOptionalAttributeValue(config, "parastyle");
					AllowDivParaStyle = !string.IsNullOrEmpty(StyleName);
				}
				SenseParaStyle = XmlUtils.GetOptionalAttributeValue(config, "parastyle");
				Number = XmlUtils.GetOptionalAttributeValue(config, "number");
				NumStyle = XmlUtils.GetOptionalAttributeValue(config, "numstyle");
				NumberSingleSense = XmlUtils.GetOptionalBooleanAttributeValue(config, "numsingle", false);
				NumFont = XmlUtils.GetOptionalAttributeValue(config, "numfont");
				ShowSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(config, "singlegraminfofirst", false);
				ShowSenseAsPara = m_sFlowType == "divInPara" && Param != null && Param.EndsWith("_AsPara");

				PreventNullStyle = XmlUtils.GetOptionalBooleanAttributeValue(config, "preventnullstyle", false);
				m_fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(config, "showasindentedpara", false);
				if (m_fShowComplexFormPara)
				{
					AllowParaStyle = true;
				}
				ShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(config, "showLabels", false);
				m_sDup = XmlUtils.GetOptionalAttributeValue(config, "dup");
				IsDuplicate = !string.IsNullOrEmpty(m_sDup);

				LexRelType = XmlUtils.GetOptionalAttributeValue(config, "lexreltype");
				if (!string.IsNullOrEmpty(LexRelType))
				{
					var sRelTypes = XmlUtils.GetOptionalAttributeValue(config, "reltypeseq");
					RelTypeList = LexReferenceInfo.CreateListFromStorageString(sRelTypes);
				}
				EntryType = XmlUtils.GetOptionalAttributeValue(config, "entrytype");
				if (!string.IsNullOrEmpty(EntryType))
				{
					var sTypeseq = XmlUtils.GetOptionalAttributeValue(config, "entrytypeseq");
					EntryTypeList = ItemTypeInfo.CreateListFromStorageString(sTypeseq);
				}
			}
			Checked = m_sVisibility.ToLowerInvariant() != "never";
			Text = m_sLabel;
			Name = $"{m_sClassName}/{m_sLayoutName}/{m_sPartName}";
		}

		public LayoutTreeNode()
		{
		}

		/// <summary>
		/// Copies the tree node and the entire subtree rooted at this tree node.
		/// This is a partial copy that copies references to objects rather than
		/// cloning the objects themselves (other than the tree nodes of the subtree).
		/// This is good for moving a tree node up and down, but not so good for
		/// duplicating the tree node.  In the latter case, at least the configuration
		/// XML nodes must be cloned, and the layouts those point to, and new names
		/// generated for the duplicates.  Otherwise, editing the subnodes of the
		/// duplicated node ends up editing the configuration of the subnodes of the
		/// original node.
		/// </summary>
		public override object Clone()
		{
			var ltn = (LayoutTreeNode)base.Clone();
			CopyValuesTo(ltn);
			return ltn;
		}

		private void CopyValuesTo(LayoutTreeNode ltn)
		{
			// Review: might be difficult to keep this up-to-date! How do we know we've got
			// everything in here that needs to be here?! --gjm
			ltn.m_cSubnodes = m_cSubnodes;
			ltn.AllowBeforeStyle = AllowBeforeStyle;
			ltn.AllowCharStyle = AllowCharStyle;
			ltn.AllowDivParaStyle = AllowDivParaStyle;
			ltn.AllowParaStyle = AllowParaStyle;
			ltn.ContentVisible = ContentVisible;
			ltn.IsDuplicate = IsDuplicate;
			ltn.NumberSingleSense = NumberSingleSense;
			ltn.ShowSenseAsPara = ShowSenseAsPara;
			ltn.m_fShowComplexFormPara = m_fShowComplexFormPara;
			ltn.ShowComplexFormParaConfig = ShowComplexFormParaConfig;
			ltn.ShowGramInfoConfig = ShowGramInfoConfig;
			ltn.m_fShowSenseConfig = m_fShowSenseConfig;
			ltn.ShowWsLabels = ShowWsLabels;
			ltn.ShowSingleGramInfoFirst = ShowSingleGramInfoFirst;
			ltn.m_fStyleFromHiddenChild = m_fStyleFromHiddenChild;
			ltn.m_fUseParentConfig = m_fUseParentConfig;
			ltn.m_idxOrig = m_idxOrig;
			ltn.After = After;
			ltn.Before = Before;
			ltn.BeforeStyleName = BeforeStyleName;
			ltn.m_sClassName = m_sClassName;
			ltn.m_sDup = m_sDup;
			ltn.m_sFlowType = m_sFlowType;
			ltn.m_sLabel = m_sLabel;
			ltn.m_sLayoutName = m_sLayoutName;
			ltn.Number = Number;
			ltn.NumFont = NumFont;
			ltn.NumStyle = NumStyle;
			ltn.Param = Param;
			ltn.m_sPartName = m_sPartName;
			ltn.SenseParaStyle = SenseParaStyle;
			ltn.Between = Between;
			ltn.StyleName = StyleName;
			ltn.m_sVisibility = m_sVisibility;
			ltn.m_sWsLabel = m_sWsLabel;
			ltn.WsType = WsType;
			ltn.m_xnCallingLayout = m_xnCallingLayout;
			ltn.m_xnConfig = m_xnConfig;
			ltn.m_xnHiddenChild = m_xnHiddenChild;
			ltn.HiddenChildLayout = HiddenChildLayout;
			ltn.HiddenNode = HiddenNode;
			ltn.HiddenNodeLayout = HiddenNodeLayout;
			ltn.ParentLayout = ParentLayout;
			ltn.LexRelType = LexRelType;
			ltn.RelTypeList = RelTypeList;
			ltn.EntryType = EntryType;
			ltn.EntryTypeList = EntryTypeList;
		}

		internal LayoutTreeNode CreateCopy()
		{
			var ltn = new LayoutTreeNode();
			CopyValuesTo(ltn);
			ltn.m_xnConfig = m_xnConfig.Clone();
			ltn.IsDuplicate = true;
			ltn.m_cSubnodes = 0;
			if (ltn.RelTypeList != null && ltn.RelTypeList.Count > 0)
			{
				ltn.RelTypeList = LexReferenceInfo.CreateListFromStorageString(ltn.LexRelTypeSequence);
			}
			if (ltn.EntryTypeList != null && ltn.EntryTypeList.Count > 0)
			{
				ltn.EntryTypeList = ItemTypeInfo.CreateListFromStorageString(ltn.EntryTypeSequence);
			}
			return ltn;
		}

		public bool AllParentsChecked
		{
			get
			{
				var tn = Parent;
				while (tn != null)
				{
					if (!tn.Checked)
					{
						return false;
					}
					tn = tn.Parent;
				}
				return true;
			}
		}

		public bool IsDescendedFrom(TreeNode tnPossibleAncestor)
		{
			var tn = Parent;
			while (tn != null)
			{
				if (tn == tnPossibleAncestor)
				{
					return true;
				}
				tn = tn.Parent;
			}
			return false;
		}

		public bool IsTopLevel => m_xnConfig.Name == "configure" && Level == 0;

		public XElement Configuration => m_xnConfig;

		public string LayoutName => m_sLayoutName;

		public virtual string PartName => m_sPartName;

		public string ClassName
		{
			get { return m_sClassName; }
			internal set { m_sClassName = value; }
		}

		public string FlowType => m_sFlowType;

		public string Label
		{
			get { return m_sLabel; }
			set
			{
				m_sLabel = value;
				Text = m_sLabel;
			}
		}

		public bool UseParentConfig
		{
			get { return m_fUseParentConfig; }
			set { m_fUseParentConfig = value; }
		}

		public bool ShowSenseConfig
		{
			get { return m_fShowSenseConfig; }
			set { m_fShowSenseConfig = value; }
		}

		public string LexRelType { get; internal set; }
		public List<LexReferenceInfo> RelTypeList { get; internal set; }

		public string LexRelTypeSequence
		{
			get
			{
				if (RelTypeList == null)
				{
					return null;
				}
				var bldr = new StringBuilder();
				for (var i = 0; i < RelTypeList.Count; ++i)
				{
					if (i > 0)
					{
						bldr.Append(",");
					}
					bldr.Append(RelTypeList[i].StorageString);
				}
				return bldr.ToString();
			}
		}

		public string EntryType { get; internal set; }
		public List<ItemTypeInfo> EntryTypeList { get; internal set; }

		public string EntryTypeSequence
		{
			get
			{
				if (EntryTypeList == null)
				{
					return null;
				}
				var bldr = new StringBuilder();
				for (var i = 0; i < EntryTypeList.Count; ++i)
				{
					if (i > 0)
					{
						bldr.Append(",");
					}
					bldr.Append(EntryTypeList[i].StorageString);
				}
				return bldr.ToString();
			}
		}

		public bool ShowGramInfoConfig { get; set; }

		public bool ShowComplexFormParaConfig { get; set; }

		public bool ShowSenseAsPara { get; set; }

		public string SenseParaStyle { get; set; }

		public bool ContentVisible { get; set; }

		public string BeforeStyleName { get; set; }

		public string Before { get; set; }

		public string After { get; set; }

		public string Between { get; set; }

		public string WsLabel
		{
			get { return m_sWsLabel; }
			set
			{
				var temp = value;
				if (!NewValueIsCompatibleWithMagic(m_sWsLabel, temp))
				{
					m_sWsLabel = temp;
				}
			}
		}

		private static bool NewValueIsCompatibleWithMagic(string possibleMagicLabel, string newValue)
		{
			if (string.IsNullOrEmpty(possibleMagicLabel) || String.IsNullOrEmpty(newValue))
				return false;

			var magicId = WritingSystemServices.GetMagicWsIdFromName(possibleMagicLabel);
			if (magicId == 0)
			{
				return false;
			}

			var newId = WritingSystemServices.GetMagicWsIdFromName(newValue);
			if (newId == 0)
			{
				return false;
			}

			return newId == WritingSystemServices.SmartMagicWsToSimpleMagicWs(magicId);
		}

		public string WsType { get; set; }

		public string StyleName { get; set; }

		/// <summary>
		/// True if (none) should be suppressed from the style list.
		/// Currently this is obsolete. There is code to implement it for main paragraph styles;
		/// but we do not create 'none' as an option for m_rgParaStyles at all (see commented-out code linked to LT-10950).
		/// It has never been implemented for character or 'before' styles.
		/// </summary>
		public bool PreventNullStyle { get; }

		public bool AllowCharStyle { get; private set; }

		public bool AllowBeforeStyle { get; private set; }

		public bool AllowParaStyle { get; private set; }

		public bool AllowDivParaStyle { get; private set; }

		public string Number { get; set; }

		public string NumStyle { get; set; }

		public bool NumberSingleSense { get; set; }

		public string NumFont { get; set; }

		public bool ShowSingleGramInfoFirst { get; set; }

		public bool ShowComplexFormPara
		{
			get { return m_fShowComplexFormPara; }
			set { m_fShowComplexFormPara = value; AllowParaStyle = value; }
		}

		public bool ShowWsLabels { get; set; }

		public string Param { get; set; }

		public bool IsDuplicate { get; set; }

		public string DupString
		{
			get { return m_sDup; }
			set
			{
				m_sDup = value;
				if (Nodes.Count > 0)
				{
					var sDupChild = $"{value}.0";
					for (var i = 0; i < Nodes.Count; ++i)
					{
						((LayoutTreeNode)Nodes[i]).DupString = sDupChild;
					}
				}
			}
		}

		internal int OriginalIndex
		{
			set
			{
				if (m_idxOrig == -1)
				{
					m_idxOrig = value;
				}
			}
		}

		/// <summary>
		/// Flag that we're still adding subnodes to this node.
		/// </summary>
		internal bool AddingSubnodes { get; set; }

		internal int OriginalNumberOfSubnodes
		{
			get
			{
				return m_cSubnodes == -1 ? 0 : m_cSubnodes;
			}
			set
			{
				Debug.Assert(value >= m_cSubnodes);
				if (m_cSubnodes == -1 || IsTopLevel || AddingSubnodes)
				{
					m_cSubnodes = value;
				}
				else
				{
					Debug.WriteLine("OriginalNumberOfSubnodes did not update!");
				}
			}
		}

		internal bool IsDirty()
		{
			if (IsNodeDirty() || HasMoved)
			{
				return true;
			}
			for (var i = 0; i < Nodes.Count; ++i)
			{
				var ltn = (LayoutTreeNode)Nodes[i];
				if (ltn.IsDirty())
				{
					return true;
				}
			}
			return false;
		}

		internal bool HasMoved => Index != m_idxOrig;

		internal bool IsNodeDirty()
		{
			if (Nodes.Count != OriginalNumberOfSubnodes)
			{
				return true;
			}
			if (!IsTopLevel)
			{
				// Now, compare our member variables to the content of m_xnConfig.
				if (m_xnConfig.Name == "part")
				{
					var fContentVisible = m_sVisibility != "never";
					ContentVisible = Checked; // in case (un)checked in treeview, but node never selected.
					if (fContentVisible != ContentVisible)
					{
						return true;
					}
					var sBeforeStyleName = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "beforeStyle");
					if (StringsDiffer(sBeforeStyleName, BeforeStyleName))
					{
						return true;
					}
					var sBefore = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "before");
					if (StringsDiffer(sBefore, Before))
					{
						return true;
					}
					var sAfter = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "after");
					if (StringsDiffer(sAfter, After))
					{
						if (sAfter == null)
						{
							if (StringsDiffer(" ", After))
								return true;
						}
						else
						{
							return true;
						}
					}
					var sSep = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "sep");
					if (StringsDiffer(sSep, Between))
					{
						if (sSep == null)
						{
							string sSepDefault = null;
							if (!string.IsNullOrEmpty(m_sWsLabel) || !string.IsNullOrEmpty(WsType))
							{
								sSepDefault = " ";
							}
							if (StringsDiffer(sSepDefault, Between))
							{
								return true;
							}
						}
						else
						{
							return true;
						}
					}
					var sWsLabel = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(m_xnConfig, "ws"));
					if (StringsDiffer(sWsLabel, m_sWsLabel))
					{
						return true;
					}
					if (m_fStyleFromHiddenChild)
					{
						var sStyleName = XmlUtils.GetOptionalAttributeValue(m_xnHiddenChild, "style");
						HiddenChildDirty = StringsDiffer(sStyleName, StyleName);
						if (HiddenChildDirty)
						{
							return true;
						}
					}
					else
					{
						var sStyleName = XmlUtils.GetOptionalAttributeValue(m_xnConfig, AllowDivParaStyle ? "parastyle" : "style");
						if (StringsDiffer(sStyleName, StyleName))
						{
							return true;
						}
					}
					var sNumber = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "number");
					if (StringsDiffer(sNumber, Number))
					{
						return true;
					}
					var sNumStyle = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numstyle");
					if (StringsDiffer(sNumStyle, NumStyle))
					{
						return true;
					}
					var fNumSingle = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "numsingle", false);
					if (fNumSingle != NumberSingleSense)
					{
						return true;
					}
					var sNumFont = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numfont");
					if (StringsDiffer(sNumFont, NumFont))
					{
						return true;
					}
					var fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "singlegraminfofirst", false);
					if (fSingleGramInfoFirst != ShowSingleGramInfoFirst)
					{
						return true;
					}
					var fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showasindentedpara", false);
					if (fShowComplexFormPara != m_fShowComplexFormPara)
					{
						return true;
					}
					var fShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showLabels", false);
					if (fShowWsLabels != ShowWsLabels)
					{
						return true;
					}
					var sDuplicate = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "dup");
					if (StringsDiffer(sDuplicate, m_sDup))
					{
						return true;
					}
					if (ShowSenseConfig)
					{
						var fSenseIsPara = Param != null && Param.EndsWith("_AsPara");
						if (fSenseIsPara != ShowSenseAsPara)
						{
							return true;
						}
						var sSenseParaStyle = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "parastyle");
						if (sSenseParaStyle != SenseParaStyle)
						{
							return true;
						}
					}
					if (!String.IsNullOrEmpty(LexRelType))
					{
						var sRefTypeSequence = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "reltypeseq");
						if (sRefTypeSequence != LexRelTypeSequence)
							return true;
					}
					if (!string.IsNullOrEmpty(EntryType))
					{
						var sEntryTypeSeq = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "entrytypeseq");
						if (sEntryTypeSeq != EntryTypeSequence)
						{
							return true;
						}
					}
				}
			}
			else
			{
				return IsTopLevel && OverallLayoutVisibilityChanged();
			}
			return false;
		}

		private bool OverallLayoutVisibilityChanged()
		{
			Debug.Assert(Level == 0);
			var sVisible = XmlUtils.GetOptionalAttributeValue(ParentLayout, "visibility");
			var fOldVisible = sVisible != "never";
			return Checked != fOldVisible;
		}

		private static bool StringsDiffer(string s1, string s2)
		{
			return (s1 != s2 && !(string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)));
		}

		// LT-10472 says that nothing is really required.
		//get { return m_sVisibility != null && m_sVisibility.ToLowerInvariant() == "required"; }
		internal bool IsRequired => false;

		internal XElement ParentLayout { get; set; }

		internal XElement HiddenNode { get; set; }

		internal XElement HiddenNodeLayout { get; set; }

		internal XElement HiddenChildLayout { get; set; }

		internal XElement HiddenChild
		{
			get { return m_xnHiddenChild; }
			set
			{
				m_xnHiddenChild = value;
				if (m_sClassName == "StText" && AllowParaStyle && String.IsNullOrEmpty(StyleName))
				{
					m_fStyleFromHiddenChild = true;
					StyleName = XmlUtils.GetOptionalAttributeValue(value, "style");
				}
			}
		}

		internal bool HiddenChildDirty { get; private set; }

		internal bool GetModifiedLayouts(List<XElement> elements, List<LayoutTreeNode> topNodes)
		{
			var dirtyLayouts = new List<XElement>();
			for (var i = 0; i < Nodes.Count; ++i)
			{
				var ltn = (LayoutTreeNode)Nodes[i];
				if (ltn.GetModifiedLayouts(elements, topNodes))
				{
					var xn = ltn.ParentLayout;
					if (xn != null && !dirtyLayouts.Contains(xn))
					{
						dirtyLayouts.Add(xn);
					}
					xn = ltn.HiddenChildLayout;
					if (xn != null && ltn.HiddenChildDirty && !dirtyLayouts.Contains(xn))
					{
						dirtyLayouts.Add(xn);
					}
					xn = ltn.HiddenNodeLayout;
					if (xn != null && ltn.HasMoved && !dirtyLayouts.Contains(xn))
					{
						dirtyLayouts.Add(xn);
					}
					foreach (var ltnMerged in ltn.MergedNodes)
					{
						xn = ltnMerged.ParentLayout;
						if (xn != null && !dirtyLayouts.Contains(xn))
						{
							dirtyLayouts.Add(xn);
						}
					}
				}
			}
			var fDirty = IsDirty();
			if (Level == 0 && !dirtyLayouts.Contains(ParentLayout))
			{
				if (OverallLayoutVisibilityChanged())
				{
					dirtyLayouts.Add(ParentLayout);
				}
				else if (!IsTopLevel && fDirty)
				{
					dirtyLayouts.Add(ParentLayout);
				}
			}
			foreach (var xnDirtyLayout in dirtyLayouts)
			{
				// Create a new layout node with all its parts in order.  This is needed
				// to handle arbitrary reordering and possible addition or deletion of
				// duplicate nodes.  This is complicated by the presence (or rather absence)
				// of "hidden" nodes, and by "merged" nodes.
				var xnLayout = xnDirtyLayout.Clone();
				var layoutList = xnLayout.Elements().ToList();
				if (xnDirtyLayout == ParentLayout && IsTopLevel && OverallLayoutVisibilityChanged())
				{
					UpdateAttribute(xnLayout, "visibility", Checked ? "always" : "never");
				}
				if (xnLayout.HasAttributes)
				{
					var rgxa = xnLayout.Attributes().ToArray();
					var rgxnGen = new List<XElement>();
					var rgixn = new List<int>();
					for (var i = 0; i < layoutList.Count; ++i)
					{
						var xn = layoutList[i];
						if (xn.Name.LocalName != "part")
						{
							rgxnGen.Add(xn);
							rgixn.Add(i);
						}
					}
					xnLayout.RemoveAll();
					for (var i = 0; i < rgxa.Length; ++i)
					{
						var srcAttr = rgxa[i];
						var attr = xnLayout.Attribute(srcAttr.Name);
						if (attr == null)
						{
							xnLayout.Add(new XAttribute(srcAttr.Name, srcAttr.Value));
						}
						else
						{
							attr.SetValue(srcAttr.Value);
						}
					}
					if (Level == 0 && !IsTopLevel && xnDirtyLayout == ParentLayout)
					{
						foreach (var ltn in topNodes)
						{
							if (ltn.IsTopLevel || ltn.ParentLayout != xnDirtyLayout)
								continue;
							if (fDirty && ltn == this)
								ltn.StoreUpdatedValuesInConfiguration();
							xnLayout.Add(ltn.Configuration.Clone());
						}
					}
					else
					{
						for (var i = 0; i < Nodes.Count; ++i)
						{
							var ltn = (LayoutTreeNode)Nodes[i];
							if (ltn.ParentLayout == xnDirtyLayout)
							{
								xnLayout.Add(ltn.Configuration.Clone());
							}
							else if (ltn.HiddenNodeLayout == xnDirtyLayout)
							{
								var xpathString = "/" + ltn.HiddenNode.Name + "[" +
								                  BuildXPathFromAttributes(ltn.HiddenNode.Attributes()) + "]";
								if (xnLayout.XPathSelectElement(xpathString) == null)
								{
									xnLayout.Add(ltn.HiddenNode.Clone());
								}
							}
							else if (ltn.HiddenChildLayout == xnDirtyLayout)
							{
								var xpathString = "/" + ltn.HiddenNode.Name + "[" + BuildXPathFromAttributes(ltn.HiddenChild.Attributes()) + "]";
								if (xnLayout.XPathSelectElement(xpathString) == null)
								{
									xnLayout.Add(ltn.HiddenChild.Clone());
								}
							}
							else
							{
								for (var itn = 0; itn < ltn.MergedNodes.Count; itn++)
								{
									var ltnMerged = ltn.MergedNodes[itn];
									if (ltnMerged.ParentLayout == xnDirtyLayout)
									{
										xnLayout.Add(ltnMerged.Configuration.Clone());
										break;
									}
								}
							}
						}
					}
					layoutList = xnLayout.Elements().ToList();
					for (var i = 0; i < rgxnGen.Count; ++i)
					{
						XElement xnRef;
						if (rgixn[i] <= layoutList.Count / 2)
						{
							xnRef = layoutList[rgixn[i]];
							xnRef.AddBeforeSelf(rgxnGen[i]);
						}
						else
						{
							xnRef = rgixn[i] < layoutList.Count ? layoutList[rgixn[i]] : xnLayout.Elements().Last();
							xnRef.AddAfterSelf(rgxnGen[i]);
						}
					}
				}
				if (!elements.Contains(xnLayout))
				{
					elements.Add(xnLayout);
				}
			}
			if (IsTopLevel)
			{
				return UpdateLayoutVisibilityIfChanged();
			}
			if (Level > 0 && fDirty)
				StoreUpdatedValuesInConfiguration();
			return fDirty || HasMoved || IsNew;
		}

		private static string BuildXPathFromAttributes(IEnumerable<XAttribute> attributes)
		{
			if (attributes == null)
			{
				return string.Empty;
			}
			string xpath = null;
			foreach (var attr in attributes)
			{
				if (string.IsNullOrEmpty(xpath))
				{
					xpath = "@" + attr.Name + "='" + attr.Value + "'";
				}
				else
				{
					xpath += " and @" + attr.Name + "='" + attr.Value + "'";
				}
			}
			return xpath;
		}

		internal bool IsNew { get; set; }

		private bool UpdateLayoutVisibilityIfChanged()
		{
			if (IsTopLevel && OverallLayoutVisibilityChanged())
			{
				UpdateAttribute(ParentLayout, "visibility", Checked ? "always" : "never");
				return true;
			}
			return false;
		}

		private static void UpdateAttributeIfDirty(XElement xn, string sName, string sValue)
		{
			var sOldValue = XmlUtils.GetOptionalAttributeValue(xn, sName);
			if (StringsDiffer(sValue, sOldValue))
			{
				UpdateAttribute(xn, sName, sValue);
			}
		}

		private static void UpdateAttribute(XElement xn, string sName, string sValue)
		{
			if (!xn.HasAttributes)
			{
				return;
			}
			Debug.Assert(sName != null);
			if (sValue == null)
			{
				// probably can't happen...
				xn.Attributes(sName).Remove();
				//In LayoutTreeNode(XElement, StringTable, string) if the flowtype is "div" we can remove "after" and "sep" attributes, so don't assert on them.
				Debug.Assert(sName == "flowType" || xn.Attribute("flowType") != null && xn.Attribute("flowType").Value == "div");       // only values we intentionally delete.
			}
			else
			{
				xn.Add(new XAttribute(sName, sValue));
			}
		}

		private void StoreUpdatedValuesInConfiguration()
		{
			if (m_xnConfig.Name != "part")
			{
				return;
			}
			var sDuplicate = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "dup");
			if (StringsDiffer(sDuplicate, m_sDup))
			{
				// Copy Part Node
				m_xnConfig = m_xnConfig.Clone();
				UpdateAttribute(m_xnConfig, "label", m_sLabel);
				UpdateAttribute(m_xnConfig, "dup", m_sDup);
				if (HiddenNode != null)
				{
					var sNewName = $"{XmlUtils.GetMandatoryAttributeValue(ParentLayout, "name")}_{m_sDup}";
					var sNewParam = $"{XmlUtils.GetMandatoryAttributeValue(HiddenNode, "param")}_{m_sDup}";
					HiddenNode = HiddenNode.Clone();
					UpdateAttribute(HiddenNode, "dup", m_sDup);
					UpdateAttribute(HiddenNode, "param", sNewParam);
					ParentLayout = ParentLayout.Clone();
					UpdateAttribute(ParentLayout, "name", sNewName);
				}
				foreach (var ltn in m_rgltnMerged)
				{
					ltn.m_xnConfig = ltn.m_xnConfig.Clone();
					UpdateAttribute(ltn.m_xnConfig, "label", m_sLabel);
					UpdateAttribute(ltn.m_xnConfig, "dup", m_sDup);
				}
			}
			CopyPartAttributes(m_xnConfig);
			foreach (var ltn in m_rgltnMerged)
			{
				CopyPartAttributes(ltn.m_xnConfig);
			}
		}

		/// <summary>
		/// xn is a part ref element containing the currently saved version of the part that
		/// this LayoutTreeNode represents. Copy any changed information from yourself to xn.
		/// </summary>
		private void CopyPartAttributes(XElement xn)
		{
			var sVisibility = XmlUtils.GetOptionalAttributeValue(xn, "visibility");
			var fContentVisible = sVisibility != "never";
			ContentVisible = Checked;    // in case (un)checked in treeview, but node never selected.
			if (fContentVisible != ContentVisible)
			{
				UpdateAttribute(xn, "visibility", ContentVisible ? "ifdata" : "never");
			}
			UpdateAttributeIfDirty(xn, "beforeStyle", BeforeStyleName);
			UpdateAttributeIfDirty(xn, "before", Before);
			UpdateAttributeIfDirty(xn, "after", After);
			UpdateAttributeIfDirty(xn, "sep", Between);
			var sWsLabel = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(xn, "ws"));
			if (StringsDiffer(sWsLabel, m_sWsLabel))
			{
				UpdateAttribute(xn, "ws", m_sWsLabel);
			}
			if (m_fStyleFromHiddenChild)
			{
				UpdateAttributeIfDirty(m_xnHiddenChild, "style", StyleName);
			}
			else
			{
				UpdateAttributeIfDirty(xn, AllowDivParaStyle ? "parastyle" : "style", StyleName);
			}
			UpdateAttributeIfDirty(xn, "number", Number);
			UpdateAttributeIfDirty(xn, "numstyle", NumStyle);
			var fNumSingle = XmlUtils.GetOptionalBooleanAttributeValue(xn, "numsingle", false);
			if (fNumSingle != NumberSingleSense)
			{
				UpdateAttribute(xn, "numsingle", NumberSingleSense.ToString());
			}
			UpdateAttributeIfDirty(xn, "numfont", NumFont);
			var fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "singlegraminfofirst", false);
			if (fSingleGramInfoFirst != ShowSingleGramInfoFirst)
			{
				UpdateAttribute(xn, "singlegraminfofirst", ShowSingleGramInfoFirst.ToString());
				LayoutTreeNode ltnOther = null;
				if (ShowSenseConfig)
				{
					foreach (TreeNode n in Nodes)
					{
						var ltn = n as LayoutTreeNode;
						if (ltn != null && ltn.ShowGramInfoConfig)
						{
							ltnOther = ltn;
							break;
						}
					}
				}
				else if (ShowGramInfoConfig)
				{
					var ltn = Parent as LayoutTreeNode;
					if (ltn != null && ltn.ShowSenseConfig)
						ltnOther = ltn;
				}
				if (ltnOther != null)
				{
					UpdateAttribute(ltnOther.m_xnConfig, "singlegraminfofirst", ShowSingleGramInfoFirst.ToString());
				}
			}
			if (ShowSenseConfig)
			{
				var fSenseIsPara = Param != null && Param.EndsWith("_AsPara");
				var sSenseParaStyle = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "parastyle");
				if (fSenseIsPara != ShowSenseAsPara || sSenseParaStyle != SenseParaStyle)
				{
					UpdateSenseConfig(xn);
				}
			}
			var fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showasindentedpara", false);
			if (fShowComplexFormPara != m_fShowComplexFormPara)
			{
				UpdateAttribute(xn, "showasindentedpara", m_fShowComplexFormPara.ToString());
			}
			var fShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(xn, "showLabels", false);
			if (fShowWsLabels != ShowWsLabels)
			{
				UpdateAttribute(xn, "showLabels", ShowWsLabels.ToString());
			}
			if (!string.IsNullOrEmpty(LexRelType))
			{
				var sOrigRefTypeSeq = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "reltypeseq");
				var sNewRefTypeSeq = LexRelTypeSequence;
				if (sOrigRefTypeSeq != sNewRefTypeSeq)
				{
					UpdateAttribute(xn, "reltypeseq", sNewRefTypeSeq);
				}
			}
			if (!string.IsNullOrEmpty(EntryType))
			{
				var sOrigEntryTypeSeq = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "entrytypeseq");
				var sNewEntryTypeSeq = EntryTypeSequence;
				if (sOrigEntryTypeSeq != sNewEntryTypeSeq)
				{
					UpdateAttribute(xn, "entrytypeseq", sNewEntryTypeSeq);
				}
			}
		}

		private void UpdateSenseConfig(XElement xn)
		{
			if (ShowSenseAsPara)
			{
				var sParam = Param;
				if (!Param.EndsWith("_AsPara"))
				{
					sParam = Param + "_AsPara";
				}
				UpdateAttribute(xn, "param", sParam);
				UpdateAttribute(xn, "flowType", "divInPara");
				UpdateAttribute(xn, "parastyle", SenseParaStyle);
				UpdateAttribute(xn, "before", "");
				UpdateAttribute(xn, "sep", "");
				UpdateAttribute(xn, "after", "");
			}
			else
			{
				var sParam = Param;
				if (Param.EndsWith("_AsPara"))
				{
					sParam = Param.Substring(0, Param.Length - 7);
				}
				UpdateAttribute(xn, "param", sParam);
				UpdateAttribute(xn, "flowType", null);
				UpdateAttribute(xn, "parastyle", SenseParaStyle);
			}
		}

		/// <summary>
		/// If this node shows sense config information, make sure any changes are consistent with
		/// any child that also shows sense config. In particular if the numbering scheme (1.2.3 vs 1 b iii)
		/// has changed, change in all places.
		/// So far, we never have more than one child that has this property, so we don't try to handle
		/// inconsistent children.
		/// </summary>
		/// <remarks>
		/// pH 2013.09 LT-14749: Before I addressed this report, this code was intentionally not synchronising
		/// punctuation before and after the numerals.  Per a discussion with Steve McConnel, before the dialog
		/// to set these settings, users could set them by editing [project]/ConfigurationSettings/LexEntry.fwlayout
		/// or LexSense.fwlayout.  So my fix may break formatting that users had set up, iff they change settings
		/// through the dialog (Tools > Configure > Dictionary... > Sense).  However, the dialog does not provide the
		/// same granularity as editing the .fwlayout files, and therefore, if users are using the dialog, they
		/// probably expect to update formatting of sense numbers at all levels.
		/// </remarks>
		internal void MakeSenseNumberFormatConsistent()
		{
			foreach (TreeNode tn in Nodes)
			{
				var ltn = tn as LayoutTreeNode;
				if (ltn != null)
				{
					ltn.MakeSenseNumberFormatConsistent(); // recurse first, in case it has children needing to be fixed.
					if (!ShowSenseConfig || !ltn.ShowSenseConfig)
					{
						continue;
					}

					// Update numerals and punctuation
					var sNumber = Number;
					var sNumberChild = ltn.Number;
					if (sNumber != sNumberChild)
					{
						var sNumberOld = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "number");
						var sNumberChildOld = XmlUtils.GetOptionalAttributeValue(ltn.m_xnConfig, "number");
						if (sNumber != sNumberOld)
						{
							// parent changed; make child consistent
							ltn.Number = sNumber;
						}
						else if (sNumberChild != sNumberChildOld)
						{
							// child changed; make parent consistent
							Number = sNumberChild;
						}
					}

					// Update style
					var sStyle = NumStyle;
					var sStyleChild = ltn.NumStyle;
					if (sStyle != sStyleChild)
					{
						var sStyleOld = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numstyle");
						var sStyleChildOld = XmlUtils.GetOptionalAttributeValue(ltn.m_xnConfig, "numstyle");
						if (sStyle != sStyleOld)
						{
							ltn.NumStyle = sStyle;
						}
						else if (sStyleChild != sStyleChildOld)
						{
							NumStyle = sStyleChild;
						}
					}

					// Update font
					var sFont = NumFont;
					var sFontChild = ltn.NumFont;
					if (sFont != sFontChild)
					{
						var sFontOld = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numfont");
						var sFontChildOld = XmlUtils.GetOptionalAttributeValue(ltn.m_xnConfig, "numfont");
						if (sFont != sFontOld)
						{
							ltn.NumFont = sFont;
						}
						else if (sFontChild != sFontChildOld)
						{
							NumFont = sFontChild;
						}
					}

					// Update whether a single sense is numbered
					var bNumSingle = NumberSingleSense;
					var bNumSingleChild = ltn.NumberSingleSense;
					if (bNumSingle != bNumSingleChild)
					{
						var bNumSingleOld = XmlUtils.GetBooleanAttributeValue(m_xnConfig, "numsingle");
						var bNumSingleChildOld = XmlUtils.GetBooleanAttributeValue(ltn.m_xnConfig, "numsingle");
						if (bNumSingle != bNumSingleOld)
						{
							ltn.NumberSingleSense = bNumSingle;
						}
						else if (bNumSingleChild != bNumSingleChildOld)
						{
							NumberSingleSense = bNumSingleChild;
						}
					}
				}
				// TODO: before, sep, after, param, parastyle, flowType, others? (these can wait until the refactor)
			}

		}
		internal void SplitNumberFormat(out string sBefore, out string sMark, out string sAfter)
		{
			SplitNumberFormat(Number, out sBefore, out sMark, out sAfter);
		}

		internal void SplitNumberFormat(string sNumber, out string sBefore, out string sMark, out string sAfter)
		{
			sBefore = string.Empty;
			sMark = "%O";
			sAfter = ") ";
			if (!string.IsNullOrEmpty(sNumber))
			{
				var ich = sNumber.IndexOf('%');
				if (ich < 0)
				{
					ich = sNumber.Length;
				}
				sBefore = sNumber.Substring(0, ich);
				if (ich < sNumber.Length)
				{
					if (ich == sNumber.Length - 1)
					{
						sMark = "%O";
						ich += 1;
					}
					else
					{
						sMark = sNumber.Substring(ich, 2);
						ich += 2;
					}
					sAfter = sNumber.Substring(ich);
				}
			}
		}

		public List<LayoutTreeNode> MergedNodes => m_rgltnMerged;
	}
}