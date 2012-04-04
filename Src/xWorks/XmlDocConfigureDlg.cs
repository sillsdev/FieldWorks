// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlDocConfigureDlg.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// Uncomment the #define if you want to see the "Restore Defaults" and "Set All" buttons.
// (This affects only DEBUG builds.)
// </remarks>
//#define DEBUG_TEST
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.XWorks
{
	public partial class XmlDocConfigureDlg : Form, IFWDisposable
	{
		/// <summary>
		/// XmlDocConfigureDlg is used to configure parts of a jtview layout, for instance, as used
		/// for the Dictionary view in Flex.  It builds a tree view from the XML &lt;part&gt; nodes
		/// contained within &lt;layout&gt; nodes, following the path of layouts called by parts called
		/// by those "part ref" nodes.
		/// The first such document handled is a dictionary in the Language Explorer program.  There
		/// are a few traces of that in the code below, but most of the code should be rather generic.
		/// If any additional documents are configured with this tool, it may be worth try to generalize
		/// some of these fine points, possibly by adding more configuration control attributes like the
		/// hideConfig="true" attributes added to a couple of part refs just to make the node tree look
		/// nicer to the users.
		/// </summary>
		XmlNode m_configurationParameters;
		string m_defaultRootLayoutName;
		FdoCache m_cache;
		IFwMetaDataCache m_mdc;
		FwStyleSheet m_styleSheet;
		IMainWindowDelegateCallbacks m_callbacks;
		Mediator m_mediator;
		string m_sLayoutPropertyName = null;
		StringTable m_stringTbl;
		Inventory m_layouts;
		Inventory m_parts;
		LayoutTreeNode m_current;
		List<StyleComboItem> m_rgCharStyles;
		List<StyleComboItem> m_rgParaStyles;
		List<XmlNode> m_stackLayouts = new List<XmlNode>(8);
		int m_dxContextOffset = 0;
		private string m_helpTopicID; // should we store the helpID or the configObject
		bool m_fDeleteCustomFiles = false;

		/// <summary>
		/// This class provides a stack of nodes that represent a hidden level in displaying
		/// the configuration tree.
		/// </summary>
		protected class HiddenLevels
		{
			private List<XmlNode> m_stackParts = new List<XmlNode>();
			private List<XmlNode> m_stackLayouts = new List<XmlNode>();
			private List<XmlNode> m_stackCallers = new List<XmlNode>();

			/// <summary>
			/// Constructor.
			/// </summary>
			public HiddenLevels()
			{
			}

			/// <summary>
			/// Add a set of nodes that represent a hidden level.
			/// </summary>
			public void Push(XmlNode part, XmlNode layout, XmlNode caller)
			{
				m_stackParts.Add(part);
				m_stackLayouts.Add(layout);
				m_stackCallers.Add(caller);
			}

			/// <summary>
			/// Remove the most recent set of nodes that represent a hidden level.
			/// </summary>
			public void Pop()
			{
				if (m_stackParts.Count > 0)
					m_stackParts.RemoveAt(m_stackParts.Count - 1);
				if (m_stackLayouts.Count > 0)
					m_stackLayouts.RemoveAt(m_stackLayouts.Count - 1);
				if (m_stackCallers.Count > 0)
					m_stackCallers.RemoveAt(m_stackCallers.Count - 1);
			}

			/// <summary>
			/// Get the most recently hidden part node.
			/// </summary>
			public XmlNode Part
			{
				get
				{
					if (m_stackParts.Count > 0)
						return m_stackParts[m_stackParts.Count - 1];
					else
						return null;
				}
			}

			/// <summary>
			/// Get the most recently hidden layout node.
			/// </summary>
			public XmlNode Layout
			{
				get
				{
					if (m_stackLayouts.Count > 0)
						return m_stackLayouts[m_stackLayouts.Count - 1];
					else
						return null;
				}
			}

			/// <summary>
			/// Get the most recently hidden caller node.
			/// </summary>
			public XmlNode Caller
			{
				get
				{
					if (m_stackCallers.Count > 0)
						return m_stackCallers[m_stackCallers.Count - 1];
					else
						return null;
				}
			}
		}
		HiddenLevels m_hidden = new HiddenLevels();

		#region Constructor, Initialization, etc.

		public XmlDocConfigureDlg()
		{
			InitializeComponent();

			// Relocate the label and link for parent nodes.
			m_lblMoreDetail.Visible = false;
			m_tbMoreDetail.Visible = false;
			m_lnkConfigureNow.Visible = false;
			m_lblMoreDetail.Location = new Point(10, 65);
			m_tbMoreDetail.Location = new Point(10, 58);
			m_lnkConfigureNow.Location = new Point(10, 84);

			// Relocate the controls for Sense nodes.
			m_chkShowSingleGramInfoFirst.Location = new Point(10, 104);
			m_grpSenseNumber.Location = new Point(10, 108 + m_chkShowSingleGramInfoFirst.Height);
			FillNumberStyleComboList();
			FillNumberFontComboList();

			// "showasindentedpara"
			m_chkComplexFormsAsParagraphs.Location = m_chkShowSingleGramInfoFirst.Location;

			m_tvParts.AfterCheck += new TreeViewEventHandler(m_tvParts_AfterCheck);
			m_tbBefore.LostFocus += new EventHandler(m_tbBefore_LostFocus);
			m_tbBetween.LostFocus += new EventHandler(m_tbBetween_LostFocus);
			m_tbAfter.LostFocus += new EventHandler(m_tbAfter_LostFocus);
			m_tbBefore.GotFocus += new EventHandler(m_tbBefore_GotFocus);
			m_tbBetween.GotFocus += new EventHandler(m_tbBetween_GotFocus);
			m_tbAfter.GotFocus += new EventHandler(m_tbAfter_GotFocus);
			m_lvWritingSystems.ItemCheck += new ItemCheckEventHandler(m_lvWritingSystems_ItemCheck);

			// This functionality is too difficult to get working on a limited basis (affecting
			// only the current general layout), so it's being disabled here and moved elsewhere.
			// See LT-6984.
			m_btnRestoreDefaults.Visible = false;
			m_btnRestoreDefaults.Enabled = false;
#if DEBUG
#if DEBUG_TEST
			m_btnRestoreDefaults.Visible = true;	// but it can be useful when testing...
			m_btnRestoreDefaults.Enabled = true;
			m_btnSetAll.Visible = true;
			m_btnSetAll.Enabled = true;
			m_btnSetAll.Text = "DEBUG: Set All";
#endif
#endif
		}

		/// <summary>
		/// Fill the combobox list which gives the possibilities for numbering a recursive
		/// sequence.  The  first argument of the combo item is the displayed string.  The
		/// second element is the marker inserted into the string for displaying numbers as
		/// given.  The marker must be interpreted by code in
		/// XmlVc.DisplayVec(IVwEnv vwenv, int hvo, int flid, int frag) (XMLViews/XmlVc.cs).
		/// </summary>
		private void FillNumberStyleComboList()
		{
			m_cbNumberStyle.Items.Add(new NumberStyleComboItem(xWorksStrings.ksNone1, ""));
			m_cbNumberStyle.Items.Add(new NumberStyleComboItem("1  1.2  1.2.3", "%O"));
			m_cbNumberStyle.Items.Add(new NumberStyleComboItem("1  b  iii", "%z"));
		}

		/// <summary>
		/// Fill the combobox list which gives the possible fonts for displaying the numbers
		/// of a numbered recursive sequence.
		/// </summary>
		private void FillNumberFontComboList()
		{
			m_cbNumberFont.Items.Add(xWorksStrings.ksUnspecified);
			using (InstalledFontCollection installedFontCollection = new InstalledFontCollection())
			{
				FontFamily[] fontFamilies = installedFontCollection.Families;
				int count = fontFamilies.Length;
				for (int i = 0; i < count; ++i)
				{
					// The .NET framework is unforgiving of using a font that doesn't support the
					// "regular" style.  So we won't allow the user to even see them...
					if (fontFamilies[i].IsStyleAvailable(FontStyle.Regular))
					{
						string familyName = fontFamilies[i].Name;
						m_cbNumberFont.Items.Add(familyName);
						fontFamilies[i].Dispose();
					}
				}
			}
		}

		/// <summary>
		/// Initialize the dialog after creating it.
		/// </summary>
		/// <param name="configurationParameters"></param>
		/// <param name="cache"></param>
		/// <param name="styleSheet"></param>
		/// <param name="mainWindowDelegateCallbacks"></param>
		/// <param name="mediator"></param>
		public void SetConfigDlgInfo(XmlNode configurationParameters, FdoCache cache,
			FwStyleSheet styleSheet, IMainWindowDelegateCallbacks mainWindowDelegateCallbacks,
			Mediator mediator, string sLayoutPropertyName)
		{
			CheckDisposed();
			m_configurationParameters = configurationParameters;
			string labelKey = XmlUtils.GetAttributeValue(configurationParameters, "viewTypeLabelKey");
			if (!String.IsNullOrEmpty(labelKey))
			{
				string sLabel = xWorksStrings.ResourceManager.GetString(labelKey);
				if (!String.IsNullOrEmpty(sLabel))
					m_lblViewType.Text = sLabel;
			}
			m_cache = cache;
			m_mdc = m_cache.DomainDataByFlid.MetaDataCache;
			m_styleSheet = styleSheet;
			m_callbacks = mainWindowDelegateCallbacks;
			m_mediator = mediator;
			m_sLayoutPropertyName = sLayoutPropertyName;
			if (m_mediator != null && m_mediator.HasStringTable)
				m_stringTbl = m_mediator.StringTbl;
			m_layouts = Inventory.GetInventory("layouts", cache.ProjectId.Name);
			m_parts = Inventory.GetInventory("parts", cache.ProjectId.Name);
			string configObjectName = XmlUtils.GetLocalizedAttributeValue(m_mediator.StringTbl,
				configurationParameters, "configureObjectName", "");
			this.Text = String.Format(this.Text, configObjectName);
			m_defaultRootLayoutName = XmlUtils.GetAttributeValue(configurationParameters, "layout");
			string sLayoutType = null;
			if (m_mediator != null && m_mediator.PropertyTable != null)
			{
				object objType = m_mediator.PropertyTable.GetValue(m_sLayoutPropertyName);
				if (objType != null)
					sLayoutType = (string)objType;
			}
			if (String.IsNullOrEmpty(sLayoutType))
				sLayoutType = m_defaultRootLayoutName;
			CreateComboAndTreeItems(sLayoutType);

			// Restore the location and size from last time we called this dialog.
			if (m_mediator != null && m_mediator.PropertyTable != null)
			{
				object locWnd = m_mediator.PropertyTable.GetValue("XmlDocConfigureDlg_Location");
				object szWnd = m_mediator.PropertyTable.GetValue("XmlDocConfigureDlg_Size");
				if (locWnd != null && szWnd != null)
				{
					Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);
					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
			}

			// Make a help topic ID
			m_helpTopicID = generateChooserHelpTopicID(configObjectName);

			// Load the lists for the styles combo boxes.
			SetStylesLists();
		}

		private void CreateComboAndTreeItems(string sLayoutType)
		{
			XmlNode configureLayouts = XmlUtils.FindNode(m_configurationParameters, "configureLayouts");
			foreach (XmlNode xnLayoutType in configureLayouts.ChildNodes)
			{
				if (xnLayoutType is XmlComment || xnLayoutType.Name != "layoutType")
					continue;
				string sLabel = XmlUtils.GetAttributeValue(xnLayoutType, "label");
				if (sLabel == "$wsName")
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
					foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.CurrentReversalIndices)
					{
						IWritingSystem ws = m_cache.ServiceLocator.WritingSystemManager.Get(ri.WritingSystem);
						string sWsTag = ws.Id;
						m_layouts.ExpandWsTaggedNodes(sWsTag);	// just in case we have a new index.
						// Create a copy of the layoutType node for the specific writing system.
						XmlNode xnRealLayout = CreateWsSpecficLayoutType(xnLayoutType,
							ws.DisplayLabel, sLayout.Replace("$ws", sWsTag), sWsTag);
						List<LayoutTreeNode> rgltnStyle = BuildLayoutTree(xnRealLayout);
						m_cbDictType.Items.Add(new LayoutTypeComboItem(xnRealLayout, rgltnStyle));
					}
				}
				else
				{
					List<LayoutTreeNode> rgltnStyle = BuildLayoutTree(xnLayoutType);
					m_cbDictType.Items.Add(new LayoutTypeComboItem(xnLayoutType, rgltnStyle));
				}
			}
			int idx = -1;
			for (int i = 0; i < m_cbDictType.Items.Count; ++i)
			{
				if ((m_cbDictType.Items[i] as LayoutTypeComboItem).LayoutName == sLayoutType)
				{
					idx = i;
					break;
				}
			}
			if (idx < 0)
				idx = 0;
			m_cbDictType.SelectedIndex = idx;
		}

		private static XmlNode CreateWsSpecficLayoutType(XmlNode xnLayoutType, string sWsLabel,
			string sWsLayout, string sWsTag)
		{
			XmlNode xnRealLayout = xnLayoutType.Clone();
			xnRealLayout.Attributes["label"].Value = sWsLabel;
			xnRealLayout.Attributes["layout"].Value = sWsLayout;
			foreach (XmlNode config in xnRealLayout.ChildNodes)
			{
				if (config is XmlComment || config.Name != "configure")
					continue;
				string sInternalLayout = XmlUtils.GetAttributeValue(config, "layout");
				Debug.Assert(sInternalLayout.EndsWith("-$ws"));
				config.Attributes["layout"].Value = sInternalLayout.Replace("$ws", sWsTag);
			}
			return xnRealLayout;
		}

		private List<LayoutTreeNode> BuildLayoutTree(XmlNode xnLayoutType)
		{
			List<LayoutTreeNode> rgltn = new List<LayoutTreeNode>();
			foreach (XmlNode config in xnLayoutType.ChildNodes)
			{
				if (config is XmlComment || config.Name != "configure")
					continue;
				rgltn.Add(BuildMainLayout(config));
			}
			return rgltn;
		}


		private void SetStylesLists()
		{
			if (m_rgCharStyles == null)
				m_rgCharStyles = new List<StyleComboItem>();
			else
				m_rgCharStyles.Clear();
			if (m_rgParaStyles == null)
				m_rgParaStyles = new List<StyleComboItem>();
			else
				m_rgParaStyles.Clear();
			m_rgCharStyles.Add(new StyleComboItem(null));
			m_rgParaStyles.Add(new StyleComboItem(null));
			foreach (BaseStyleInfo sty in m_styleSheet.Styles)
			{
				if (sty.IsCharacterStyle)
					m_rgCharStyles.Add(new StyleComboItem(sty));
				else if (sty.IsParagraphStyle)
					m_rgParaStyles.Add(new StyleComboItem(sty));
			}
			m_rgCharStyles.Sort();
			m_rgParaStyles.Sort();
		}

		private LayoutTreeNode BuildMainLayout(XmlNode config)
		{
			LayoutTreeNode ltn = new LayoutTreeNode(config, m_stringTbl, null);
			ltn.OriginalIndex = m_tvParts.Nodes.Count;
			string className = ltn.ClassName;
			string layoutName = ltn.LayoutName;
			XmlNode layout = m_layouts.GetElement("layout", new[] { className, "jtview", layoutName, null });
			if (layout == null)
				throw new Exception("Cannot configure layout " + layoutName + " of class " + className + " because it does not exist");
			ltn.ParentLayout = layout;	// not really the parent layout, but the parent of this node's children
			string sVisible = XmlUtils.GetAttributeValue(layout, "visibility");
			ltn.Checked = sVisible != "never";
			AddChildNodes(layout, ltn, ltn.Nodes.Count);
			ltn.OriginalNumberOfSubnodes = ltn.Nodes.Count;
			return ltn;
		}

		private void AddChildNodes(XmlNode layout, LayoutTreeNode ltnParent, int iStart)
		{
			m_stackLayouts.Insert(0, layout);		// push this layout onto the stack.
			try
			{
				bool fMerging = iStart < ltnParent.Nodes.Count;
				int iNode = iStart;
				string className = XmlUtils.GetManditoryAttributeValue(layout, "class");
				List<XmlNode> nodes = PartGenerator.GetGeneratedChildren(layout, m_cache,
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
							subLayout = m_layouts.GetElement("layout",
								new[] { className, "jtview", subLayoutName, null });
						}
						if (subLayout != null)
							AddChildNodes(subLayout, ltnParent, ltnParent.Nodes.Count);
					}
					else if (node.Name == "part")
					{
						// Check whether this node has already been added to this parent.  Don't add
						// it if it's already there!
						LayoutTreeNode ltnOld = FindMatchingNode(ltnParent, node);
						if (ltnOld != null)
							continue;
						string sRef = XmlUtils.GetManditoryAttributeValue(node, "ref");
						XmlNode part = m_parts.GetElement("part",
							new string[] { className + "-Jt-" + sRef });
						if (part == null && sRef != "$child")
							continue;
						bool fHide = XmlUtils.GetOptionalBooleanAttributeValue(node, "hideConfig", false);
						LayoutTreeNode ltn;
						if (!fHide)
						{
							ltn = new LayoutTreeNode(node, m_stringTbl, className);
							ltn.OriginalIndex = ltnParent.Nodes.Count;
							ltn.CallingLayout = m_stackLayouts[0];
							ltn.ParentLayout = layout;
							ltn.HiddenNode = m_hidden.Part;
							ltn.HiddenParent = m_hidden.Layout;
							ltn.HiddenCallingLayout = m_hidden.Caller;
							if (fMerging)
								(ltnParent.Nodes[iNode] as LayoutTreeNode).MergedNodes.Add(ltn);
							else
								ltnParent.Nodes.Add(ltn);
						}
						else
						{
							Debug.Assert(!fMerging);
							ltn = ltnParent;
							m_hidden.Push(node, layout, m_stackLayouts[0]);
							if (className == "StTxtPara")
							{
								ltnParent.HiddenChildLayout = layout;
								ltnParent.HiddenChild = node;
							}
						}
						if (part != null)
							ProcessChildNodes(part.ChildNodes, className, ltn);
						ltn.OriginalNumberOfSubnodes = ltn.Nodes.Count;
						m_hidden.Pop();
						++iNode;
					}
				}
			}
			finally
			{
				m_stackLayouts.RemoveAt(0);	// pop this layout off the stack.
			}
		}

		private LayoutTreeNode FindMatchingNode(LayoutTreeNode ltn, XmlNode node)
		{
			if (ltn == null || node == null)
				return null;
			foreach (LayoutTreeNode ltnSub in ltn.Nodes)
			{
				if (ltnSub.Configuration == node)
					return ltnSub;
			}
			return FindMatchingNode(ltn.Parent as LayoutTreeNode, node);
		}

		/// <summary>
		/// Walk the tree of child nodes, storing information for each &lt;obj&gt; or &lt;seq&gt;
		/// node.
		/// </summary>
		/// <param name="xmlNodeList"></param>
		/// <param name="className"></param>
		/// <param name="ltn"></param>
		private void ProcessChildNodes(XmlNodeList xmlNodeList, string className, LayoutTreeNode ltn)
		{
			foreach (XmlNode xn in xmlNodeList)
			{
				if (xn is XmlComment)
					continue;
				if (xn.Name == "obj" || xn.Name == "seq" || xn.Name == "objlocal")
				{
					StoreChildNodeInfo(xn, className, ltn);
				}
				else
				{
					ProcessChildNodes(xn.ChildNodes, className, ltn);
				}
			}
		}

		private void StoreChildNodeInfo(XmlNode xn, string className, LayoutTreeNode ltn)
		{
			string sField = XmlUtils.GetManditoryAttributeValue(xn, "field");
			if (ltn.Level > 0)
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
				if (sField == "ComplexFormEntryBackRefs" && ltn.ClassName == "LexEntry")
				{
					string sShowAsIndentedPara = XmlUtils.GetAttributeValue(ltn.Configuration, "showasindentedpara");
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
			XmlNode xnCaller = m_hidden.Part;
			if (xnCaller == null)
				xnCaller = ltn.Configuration;
			string sLayout = XmlVc.GetLayoutName(xn, xnCaller);
			int flid = 0;
			int clidDst = 0;
			string sClass = null;
			string sTargetClasses = null;
			try
			{
				// Failure should be fairly unusual, but, for example, part MoForm-Jt-FormEnvPub attempts to display
				// the property PhoneEnv inside an if that checks that the MoForm is one of the subclasses that has
				// the PhoneEnv property. MoForm itself does not.
				if (!((FDO.Infrastructure.IFwMetaDataCacheManaged)m_cache.DomainDataByFlid.MetaDataCache).FieldExists(className, sField, true))
					return;
				flid = m_cache.DomainDataByFlid.MetaDataCache.GetFieldId(className, sField, true);
				CellarPropertyType type = (CellarPropertyType)m_cache.DomainDataByFlid.MetaDataCache.GetFieldType(flid);
				Debug.Assert(type >= CellarPropertyType.MinObj);
				if (type >= CellarPropertyType.MinObj)
				{
					sTargetClasses = XmlUtils.GetOptionalAttributeValue(xn, "targetclasses");
					clidDst = m_mdc.GetDstClsId(flid);
					if (clidDst == 0)
						sClass = XmlUtils.GetOptionalAttributeValue(xn, "targetclass");
					else
						sClass = m_mdc.GetClassName(clidDst);
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
			if (rgsClasses.Length > 0)
				subLayout = m_layouts.GetElement("layout", new[] { rgsClasses[0], "jtview", sLayout, null });

			if (subLayout != null)
			{
				int iStart = ltn.Nodes.Count;
				int cNodes = subLayout.ChildNodes.Count;
				AddChildNodes(subLayout, ltn, iStart);

				bool fRepeatedConfig = XmlUtils.GetOptionalBooleanAttributeValue(xn, "repeatedConfig", false);
				if (fRepeatedConfig)
					return;		// repeats an earlier part element (probably as a result of <if>s)
				for (int i = 1; i < rgsClasses.Length; i++)
				{
					XmlNode mergedLayout = m_layouts.GetElement("layout",
						new[] { rgsClasses[i], "jtview", sLayout, null });
					if (mergedLayout != null && mergedLayout.ChildNodes.Count == cNodes)
					{
						AddChildNodes(mergedLayout, ltn, iStart);
					}
				}
			}
		}

		#endregion // Constructor, Initialization, etc.

		/// <summary>
		/// Overridden to defeat the standard .NET behavior of adjusting size by
		/// screen resolution. That is bad for this dialog because we remember the size,
		/// and if we remember the enlarged size, it just keeps growing.
		/// If we defeat it, it may look a bit small the first time at high resolution,
		/// but at least it will stay the size the user sets.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			Size size = this.Size;
			base.OnLoad(e);
			if (this.Size != size)
				this.Size = size;
			if (m_cbDictType.Items.Count < 2)
			{
				// If there's only one choice, then hide the relevant controls, move all the other
				// controls up to the vacated space, and shrink the overall dialog box.
				RemoveDictTypeComboBox();
			}
		}

		/// <summary>
		/// When the combobox for "dictionary type" may offer only one choice, there's no
		/// reason to display it.  This means that section of the dialog box can go away
		/// entirely.  (See FWR-736.)
		/// </summary>
		private void RemoveDictTypeComboBox()
		{
			m_lblViewType.Visible = false;
			m_cbDictType.Visible = false;
			label2.Visible = false;
			groupBox1.Visible = false;
			int diff = m_tvParts.Location.Y - m_lblViewType.Location.Y;
			// Moving things and shrinking the dialog is a bit tricky, because shrinking
			// the dialog can move or resize controls based on how they are anchored.
			// Being anchored to the top and not to the bottom is the only safe setting
			// when changing the height of the entire dialog after repositioning all the
			// controls vertically.
			List<Control> anchoredBottom = new List<Control>();
			List<Control> anchoredTop = new List<Control>();
			foreach (Control ctl in this.Controls)
			{
				if (ctl.Location.Y > diff)
				{
					int height = ctl.Height;
					Point loc = ctl.Location;
					AnchorStyles anchor = ctl.Anchor;
					if ((ctl.Anchor & AnchorStyles.Top) == AnchorStyles.Top)
						anchoredTop.Add(ctl);
					if ((ctl.Anchor & AnchorStyles.Bottom) == AnchorStyles.Bottom)
					{
						anchoredBottom.Add(ctl);
						ctl.Anchor &= ~AnchorStyles.Bottom;
						ctl.Anchor |= AnchorStyles.Top;
					}
					ctl.Location = new Point(loc.X, loc.Y - diff);
					ctl.Height = height;
				}
			}
			if (this.MinimumSize.Height > diff)
				this.MinimumSize = new Size(this.MinimumSize.Width, this.MinimumSize.Height - diff);
			this.Height = this.Height - diff;
			foreach (Control ctl in anchoredBottom)
			{
				ctl.Anchor |= AnchorStyles.Bottom;
				ctl.Anchor &= ~AnchorStyles.Top;
			}
			foreach (Control ctl in anchoredTop)
				ctl.Anchor |= AnchorStyles.Top;
		}

		/// <summary>
		/// Save the location and size for next time.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty("XmlDocConfigureDlg_Location", Location, false);
				m_mediator.PropertyTable.SetPropertyPersistence("XmlDocConfigureDlg_Location", true);
				m_mediator.PropertyTable.SetProperty("XmlDocConfigureDlg_Size", Size, false);
				m_mediator.PropertyTable.SetPropertyPersistence("XmlDocConfigureDlg_Size", true);
			}
			base.OnClosing(e);
		}

		#region Dialog Event Handlers

		/// <summary>
		/// Users want to see visible spaces.  The only way to do this is to select everything,
		/// and show the selection when the focus leaves for elsewhere.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_tbBefore_LostFocus(object sender, EventArgs e)
		{
			m_tbBefore.SelectAll();
		}
		void m_tbBetween_LostFocus(object sender, EventArgs e)
		{
			m_tbBetween.SelectAll();
		}
		void m_tbAfter_LostFocus(object sender, EventArgs e)
		{
			m_tbAfter.SelectAll();
		}

		/// <summary>
		/// When the focus returns, it's probably more useful to select at the end rather
		/// than selecting everything.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_tbBefore_GotFocus(object sender, EventArgs e)
		{
			m_tbBefore.Select(m_tbBefore.Text.Length, 0);
		}
		void m_tbBetween_GotFocus(object sender, EventArgs e)
		{
			m_tbBetween.Select(m_tbBetween.Text.Length, 0);
		}
		void m_tbAfter_GotFocus(object sender, EventArgs e)
		{
			m_tbAfter.Select(m_tbAfter.Text.Length, 0);
		}

		private void m_cbDictType_SelectedIndexChanged(object sender, EventArgs e)
		{
			List<LayoutTreeNode> rgltn = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).TreeNodes;
			m_tvParts.Nodes.Clear();
			m_tvParts.Nodes.AddRange(rgltn.ToArray());
			if (m_tvParts.Nodes.Count > 0)
				m_tvParts.SelectedNode = m_tvParts.Nodes[0];
		}

		private void m_btnMoveUp_Click(object sender, EventArgs e)
		{
			LayoutTreeNode ltn = (LayoutTreeNode)m_current.Clone();
			int idx = m_current.Index;
			TreeNode tnParent = m_current.Parent;
			tnParent.Nodes.Remove(m_current);
			tnParent.Nodes.Insert(idx - 1, ltn);
			m_tvParts.SelectedNode = ltn;
		}

		private void m_btnMoveDown_Click(object sender, EventArgs e)
		{
			LayoutTreeNode ltn = (LayoutTreeNode)m_current.Clone();
			int idx = m_current.Index;
			TreeNode tnParent = m_current.Parent;
			tnParent.Nodes.Remove(m_current);
			tnParent.Nodes.Insert(idx + 1, ltn);
			m_tvParts.SelectedNode = ltn;
		}

		private void m_btnDuplicate_Click(object sender, EventArgs e)
		{
			Debug.Assert(m_current.Level > 0);
			StoreNodeData();	// Ensure duplicate has current data.
			LayoutTreeNode ltnDup = (LayoutTreeNode)m_current.Clone();
			ltnDup.IsDuplicate = true;
			// Generate a unique label to identify this as the n'th duplicate in the list.
			List<string> rgsLabels = new List<string>();
			string sBaseLabel = null;
			for (int i = 0; i < m_current.Parent.Nodes.Count; ++i)
			{
				LayoutTreeNode ltn = (LayoutTreeNode)m_current.Parent.Nodes[i];
				if (ltn.Configuration == m_current.Configuration &&
					ltn.LayoutName == m_current.LayoutName &&
					ltn.PartName == m_current.PartName)
				{
					rgsLabels.Add(ltn.Label);
					if (!ltn.IsDuplicate)
						sBaseLabel = ltn.Label;
				}
			}
			if (sBaseLabel == null)
				sBaseLabel = m_current.Label;
			int cDup = 1;
			string sLabel = String.Format("{0} ({1})", sBaseLabel, cDup);
			while (rgsLabels.Contains(sLabel))
			{
				++cDup;
				sLabel = String.Format("{0} ({1})", sBaseLabel, cDup);
			}
			ltnDup.Label = sLabel;		// sets Text as well.
			string sDup = ltnDup.DupString;
			if (String.IsNullOrEmpty(sDup))
				sDup = cDup.ToString();
			else
				sDup = String.Format("{0}-{1}", sDup, cDup);
			ltnDup.DupString = sDup;
			int idx = m_current.Index;
			m_current.Parent.Nodes.Insert(idx + 1, ltnDup);
			m_tvParts.SelectedNode = ltnDup;
		}

		private void m_btnRemove_Click(object sender, EventArgs e)
		{
			Debug.Assert(m_current.IsDuplicate);
			// REVIEW:  Should we have a user prompt?
			m_current.Parent.Nodes.Remove(m_current);
		}

		private void m_btnRestoreDefaults_Click(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				LayoutCache.InitializePartInventories(null, (IApp)m_mediator.PropertyTable.GetValue("App"),
					m_cache.ProjectId.ProjectFolder);
				Inventory layouts = Inventory.GetInventory("layouts", null);
				Inventory parts = Inventory.GetInventory("parts", null);
				m_layouts = layouts;
				m_parts = parts;
				// recreate the layout trees.
				m_cbDictType.Items.Clear();
				m_tvParts.Nodes.Clear();
				CreateComboAndTreeItems(m_defaultRootLayoutName);
				m_fDeleteCustomFiles = true;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			if (IsDirty())
			{
				m_mediator.PropertyTable.SetProperty(m_sLayoutPropertyName,
					((LayoutTypeComboItem)m_cbDictType.SelectedItem).LayoutName, true,
					PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_sLayoutPropertyName, true,
					PropertyTable.SettingsGroup.LocalSettings);
				SaveModifiedLayouts();
				DialogResult = DialogResult.OK;
			}
			else
			{
				DialogResult = DialogResult.Cancel;
			}
			this.Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, m_helpTopicID);
		}

		/// <summary>
		/// Fire up the Styles dialog, and if necessary, reload the related combobox.
		/// </summary>
		private void m_btnStyles_Click(object sender, EventArgs e)
		{
			StyleComboItem sci = (StyleComboItem)m_cbCharStyle.SelectedItem;
			string charStyleName = (sci != null && sci.Style != null) ? sci.Style.Name : "";
			string paraStyleName = m_styleSheet.GetDefaultBasedOnStyleName();
			bool fRightToLeft = false;
			IVwRootSite site = null;		// Do we need something better?  We don't have anything!
			int nMaxStyleLevel = m_callbacks != null ? m_callbacks.MaxStyleLevelToShow : 0;
			int hvoAppRoot = m_callbacks != null ? m_callbacks.HvoAppRootObject : 0;
			IApp app = ((IApp) m_mediator.PropertyTable.GetValue("App"));
			using (FwStylesDlg stylesDlg = new FwStylesDlg(
				site,
				m_cache,
				m_styleSheet,
				fRightToLeft,
				m_cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				m_styleSheet.GetDefaultBasedOnStyleName(),
				nMaxStyleLevel,
				app.MeasurementSystem,
				paraStyleName,
				charStyleName,
				hvoAppRoot,
				app,
				m_mediator.HelpTopicProvider))
			{
				stylesDlg.ShowTEStyleTypes = false;
				stylesDlg.CanSelectParagraphBackgroundColor = false;
				if (stylesDlg.ShowDialog(this) == DialogResult.OK &&
					((stylesDlg.ChangeType & StyleChangeType.DefChanged) > 0 ||
					(stylesDlg.ChangeType & StyleChangeType.Added) > 0))
				{
					app.Synchronize(SyncMsg.ksyncStyle);
				}
				// Reload the lists for the styles combo boxes, and redisplay the controls.
				SetStylesLists();
				DisplayStyleControls(m_current.AllParentsChecked);
				if (m_btnBeforeStyles.Visible)
					DisplayBeforeStyleControls(m_current.AllParentsChecked);
			}
		}

		/// <summary>
		/// Fire up the Styles dialog, and if necessary, reload the related combobox.
		/// </summary>
		private void m_btnBeforeStyles_Click(object sender, EventArgs e)
		{
			StyleComboItem sci = (StyleComboItem)m_cbCharStyle.SelectedItem;
			string charStyleName = (sci != null && sci.Style != null) ? sci.Style.Name : "";
			string paraStyleName = m_styleSheet.GetDefaultBasedOnStyleName();
			bool fRightToLeft = false;
			IVwRootSite site = null;		// Do we need something better?  We don't have anything!
			int nMaxStyleLevel = m_callbacks != null ? m_callbacks.MaxStyleLevelToShow : 0;
			int hvoAppRoot = m_callbacks != null ? m_callbacks.HvoAppRootObject : 0;
			IApp app = ((IApp)m_mediator.PropertyTable.GetValue("App"));
			using (FwStylesDlg stylesDlg = new FwStylesDlg(
				site,
				m_cache,
				m_styleSheet,
				fRightToLeft,
				m_cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				m_styleSheet.GetDefaultBasedOnStyleName(),
				nMaxStyleLevel,
				app.MeasurementSystem,
				paraStyleName,
				charStyleName,
				hvoAppRoot,
				app,
				m_mediator.HelpTopicProvider))
			{
				//stylesDlg.StylesRenamedOrDeleted +=
				//    new FwStylesDlg.StylesRenOrDelDelegate(OnStylesRenamedOrDeleted);
				stylesDlg.ShowTEStyleTypes = false;
				stylesDlg.CanSelectParagraphBackgroundColor = false;
				if (stylesDlg.ShowDialog(this) == DialogResult.OK &&
					((stylesDlg.ChangeType & StyleChangeType.DefChanged) > 0 ||
					(stylesDlg.ChangeType & StyleChangeType.Added) > 0))
				{
					app.Synchronize(SyncMsg.ksyncStyle);
				}
				// Reload the lists for the styles combo boxes, and redisplay the controls.
				SetStylesLists();
				if (m_btnStyles.Visible)
					DisplayStyleControls(m_current.AllParentsChecked);
				DisplayBeforeStyleControls(m_current.AllParentsChecked);
			}
		}

		/// <summary>
		/// Move to the first child node when this "Configure Now" link text is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_lnkConfigureNow_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Debug.Assert(m_current != null && m_current.Nodes.Count > 0);
			m_tvParts.SelectedNode = m_current.Nodes[0];
		}

		/// <summary>
		/// Don't allow the top-level nodes to be unchecked!  Also, tie the check boxes in
		/// the tree view to the "Display Data" checkbox if the user clicks on the tree view
		/// while that node is the current one displayed in detail.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_tvParts_AfterCheck(object sender, TreeViewEventArgs e)
		{
			// LT-10472 says that nothing is really required for dictionary configuration!
			//if (!e.Node.Checked)
			//{
			//    if (e.Node.Level == 0)
			//        e.Node.Checked = true;
			//    else if (((LayoutTreeNode)e.Node).IsRequired)
			//        e.Node.Checked = true;
			//}
			if (e.Node == m_current)
			{
				m_chkDisplayData.Checked = e.Node.Checked;
				// Re-enable/disable the controls in the detail pane.
				if (m_current.Level > 0)
				{
					if (m_current.Nodes.Count > 0)
						DisplayDetailsForAParentNode(m_current.AllParentsChecked);
					else if (m_current.UseParentConfig)
						DisplayDetailsForRecursiveNode(m_current.AllParentsChecked);
					else
						DisplayDetailsForLeafNode(m_current.AllParentsChecked);
					DisplayBeforeStyleControls(m_current.AllParentsChecked);
				}
			}
			else
			{
				if (m_current.IsDescendedFrom(e.Node))
				{
					DisplayCurrentNodeDetails();
				}
			}
		}

		/// <summary>
		/// We are messing with the checkmarks while processing a checkmark change, and don't
		/// want the cascading recursive processing to occur.  Hence this variable...
		/// </summary>
		bool m_fItemCheck = false;

		/// <summary>
		/// We need to enforce two rules:
		/// 1. At least one item must always be checked.
		/// 2. If a magic ("Default Analysis" etc) item is checked, then it is the only one that can be checked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_lvWritingSystems_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (m_fItemCheck)
				return;
			try
			{
				m_fItemCheck = true;

				int cChecks = 0;
				ListView.CheckedListViewItemCollection lvic = m_lvWritingSystems.CheckedItems;
				if (lvic == null || lvic.Count == 0)
				{
					// I don't think this can happen, but just in case...
					if (e.NewValue == CheckState.Unchecked)
						e.NewValue = CheckState.Checked;
					return;
				}
				ListViewItem lviEvent = m_lvWritingSystems.Items[e.Index];
				if (e.NewValue == CheckState.Checked)
				{
					cChecks = lvic.Count + 1;
					for (int i = lvic.Count - 1; i >= 0; --i)
					{
						if (lviEvent.Tag is ILgWritingSystem)
						{
							if (lvic[i].Tag is int)
							{
								lvic[i].Checked = false;	// Uncheck any magic ws items.
								--cChecks;
							}
						}
						else
						{
							if (lvic[i].Index != e.Index)
							{
								lvic[i].Checked = false;	// uncheck any other items.
								--cChecks;
							}
						}
					}
				}
				else
				{
					cChecks = lvic.Count - 1;
					// Can't uncheck the last remaining checked item! (rule 1 above)
					if (lvic.Count == 1 && lvic[0].Index == e.Index)
					{
						e.NewValue = CheckState.Checked;
						++cChecks;
					}
				}
				m_chkDisplayWsAbbrs.Enabled = cChecks > 1;
			}
			finally
			{
				m_fItemCheck = false;
			}
		}

		/// <summary>
		/// Tie the "Display Data" checkbox to the corresponding checkbox in the treeview.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_chkDisplayData_CheckedChanged(object sender, EventArgs e)
		{
			if (m_current != null)
				m_current.Checked = m_chkDisplayData.Checked;
		}

		private void m_btnMoveWsUp_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection sic
				= m_lvWritingSystems.SelectedIndices;
			int idx = GetIndexToMoveWsUp(sic);

			MoveWritingSystemsItem(idx, Math.Max(idx - 1, 0));
		}

		private void m_btnMoveWsDown_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection sic
				= m_lvWritingSystems.SelectedIndices;
			int idx = GetIndexToMoveWsDown(sic);

			MoveWritingSystemsItem(idx, Math.Min(idx + 1,
				m_lvWritingSystems.Items.Count - 1));
		}

		private void m_lvWritingSystems_SelectedIndexChanged(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection sic = m_lvWritingSystems.SelectedIndices;
			m_btnMoveWsUp.Enabled = GetIndexToMoveWsUp(sic) > 0;
			m_btnMoveWsDown.Enabled = GetIndexToMoveWsDown(sic) < m_lvWritingSystems.Items.Count - 1;
		}

		private int GetIndexToMoveWsUp(ListView.SelectedIndexCollection sic)
		{
			if (sic == null || sic.Count == 0)
				return 0;
			Debug.Assert(sic.Count == 1);
			int idx = sic[0];
			if (idx == 0)
				return 0;
			object tagPrev = m_lvWritingSystems.Items[idx - 1].Tag;
			return tagPrev is ILgWritingSystem ? idx : 0;
		}

		private int GetIndexToMoveWsDown(ListView.SelectedIndexCollection sic)
		{
			int idxMax = m_lvWritingSystems.Items.Count - 1;
			if (sic == null || sic.Count == 0)
				return idxMax;
			Debug.Assert(sic.Count == 1);
			int idx = sic[0];
			if (idx >= idxMax)
				return idxMax;
			object tagSel = m_lvWritingSystems.Items[idx].Tag;
			return tagSel is ILgWritingSystem ? idx : idxMax;
		}


		private void m_tvParts_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// Save the data for the old node before displaying the data for the new node.
			if (m_current != null && m_current.Level > 0)
				StoreNodeData();

			// Set up the dialog for editing the current node's layout.
			m_current = e.Node as LayoutTreeNode;
			DisplayCurrentNodeDetails();
		}

		private void DisplayCurrentNodeDetails()
		{
			bool fEnabled = true;
			string sHeading = m_current.Text;
			TreeNode tnParent = m_current.Parent;
			if (tnParent != null)
			{
				fEnabled = m_current.AllParentsChecked;
				sHeading = String.Format(xWorksStrings.ksHierarchyLabel, tnParent.Text, sHeading);
			}
			// Use the text box if the label is too wide. See LT-9281.
			m_lblPanel.Text = sHeading;
			m_tbPanelHeader.Text = sHeading;
			if (m_lblPanel.Location.X + m_lblPanel.PreferredWidth >
				panel1.ClientSize.Width - (panel1.Margin.Left + panel1.Margin.Right))
			{
				m_lblPanel.Enabled = false;
				m_lblPanel.Visible = false;
				if (tnParent != null)
					m_tbPanelHeader.Text = String.Format(xWorksStrings.ksHierarchyLabel2, tnParent.Text, m_current.Text);
				else
					m_tbPanelHeader.Text = sHeading;
				m_tbPanelHeader.Enabled = true;
				m_tbPanelHeader.Visible = true;
			}
			else
			{
				m_lblPanel.Enabled = true;
				m_lblPanel.Visible = true;
				m_tbPanelHeader.Enabled = false;
				m_tbPanelHeader.Visible = false;
			}
			if (m_current.Level == 0)
			{
				DisplayDetailsForTopLevelNode();
				m_btnMoveUp.Enabled = false;
				m_btnMoveDown.Enabled = false;
				m_btnDuplicate.Enabled = false;
				m_btnRemove.Enabled = false;
			}
			else
			{
				// The "Display Data" control is common to all non-toplevel nodes.
				m_chkDisplayData.Enabled = !m_current.IsRequired && fEnabled;
				m_chkDisplayData.Checked = m_current.Checked;
				if (m_current.Nodes.Count > 0)
				{
					DisplayDetailsForAParentNode(fEnabled);
				}
				else
				{
					if (m_current.UseParentConfig)
						DisplayDetailsForRecursiveNode(fEnabled);
					else
						DisplayDetailsForLeafNode(fEnabled);
				}
				DisplayControlsForAnyNode(fEnabled);
				m_btnMoveUp.Enabled = EnableMoveUpButton();
				m_btnMoveDown.Enabled = EnableMoveDownButton();
				m_btnDuplicate.Enabled = true;
				m_btnRemove.Enabled = m_current.IsDuplicate;
			}
		}

		#endregion // Dialog Event Handlers

		#region Misc internal functions

		private void MoveWritingSystemsItem(int idx, int newIndex)
		{
			// This Select() call should be here, not after the
			// ListViewItem.set_Selected(bool) call near the end of the method,
			// in order to prevent focus from switching to m_cbCharStyle
			m_lvWritingSystems.Select();

			if (idx == newIndex)
				return;

			Debug.Assert((newIndex >= 0)
				&& (newIndex < m_lvWritingSystems.Items.Count));

			ListViewItem ltiSel = m_lvWritingSystems.Items[idx];
			bool itemSelected = ltiSel.Selected;
			ListViewItem lti = (ListViewItem) ltiSel.Clone();

			m_lvWritingSystems.BeginUpdate();

			m_lvWritingSystems.Items.RemoveAt(idx);
			m_lvWritingSystems.Items.Insert(newIndex, lti);

			if (itemSelected)
				lti.Selected = true;

			m_lvWritingSystems.EndUpdate();
		}

		/// <summary>
		/// Generates a possible help topic id from an identifying string, but does NOT check it for validity!
		/// </summary>
		/// <returns></returns>
		private string generateChooserHelpTopicID(string fromStr)
		{
			string candidateID = "khtpConfig";

			// Should we capitalize the next letter?
			bool nextCapital = true;

			// Lets turn our field into a candidate help page!
			foreach (char ch in fromStr.ToCharArray())
			{
				if (Char.IsLetterOrDigit(ch)) // might we include numbers someday?
				{
					if (nextCapital)
						candidateID += Char.ToUpper(ch);
					else
						candidateID += ch;
					nextCapital = false;
				}
				else // unrecognized character... exclude it
					nextCapital = true; // next letter should be a capital
			}

			return candidateID;
		}

		/// <summary>
		/// The MoveUp button is enabled only when the current node is not the first child of
		/// its parent tree node, and the preceding node comes from the same actual layout.
		/// (Remember, the parts from a sublayout node appear as children of the the same tree
		/// node as any parts from the sublayout node appear as children of the the same tree
		/// node as any parts from the layout containing the sublayout.)
		/// </summary>
		/// <returns></returns>
		private bool EnableMoveUpButton()
		{
			int idx = m_current.Index;
			if (idx <= 0)
				return false;
			XmlNode xnParent = m_current.HiddenParent;
			if (xnParent == null)
				xnParent = m_current.ParentLayout;
			LayoutTreeNode ltnPrev = (LayoutTreeNode)m_current.Parent.Nodes[idx-1];
			XmlNode xnPrevParent = ltnPrev.HiddenParent;
			if (xnPrevParent == null)
				xnPrevParent = ltnPrev.ParentLayout;
			return xnParent == xnPrevParent;
		}

		/// <summary>
		/// The MoveDown button is enabled only when the current node is not the last child of
		/// its parent tree node, and the following node comes from the same actual layout.
		/// (Remember, the parts from a sublayout node appear as children of the the same tree
		/// node as any parts from the layout containing the sublayout.)
		/// </summary>
		/// <returns></returns>
		private bool EnableMoveDownButton()
		{
			int idx = m_current.Index;
			if (idx >= m_current.Parent.Nodes.Count - 1)
				return false;
			XmlNode xnParent = m_current.HiddenParent;
			if (xnParent == null)
				xnParent = m_current.ParentLayout;
			LayoutTreeNode ltnNext = (LayoutTreeNode)m_current.Parent.Nodes[idx+1];
			XmlNode xnNextParent = ltnNext.HiddenParent;
			if (xnNextParent == null)
				xnNextParent = ltnNext.ParentLayout;
			return xnParent == xnNextParent;
		}

		private void StoreNodeData()
		{
			m_current.ContentVisible = m_current.Checked;
			if (m_tbBefore.Visible && m_tbBefore.Enabled)
				m_current.Before = m_tbBefore.Text;
			if (m_tbBetween.Visible && m_tbBetween.Enabled)
				m_current.Between = m_tbBetween.Text;
			if (m_tbAfter.Visible && m_tbAfter.Enabled)
				m_current.After = m_tbAfter.Text;
			if (m_chkDisplayWsAbbrs.Visible)
				m_current.ShowWsLabels = m_chkDisplayWsAbbrs.Checked && m_chkDisplayWsAbbrs.Enabled;
			if (m_grpSenseNumber.Visible)
				StoreSenseNumberData();
			if (m_chkShowSingleGramInfoFirst.Visible)
				StoreGramInfoData();
			if (m_chkComplexFormsAsParagraphs.Visible)
				StoreComplexFormData();
			if (m_cbCharStyle.Visible && m_cbCharStyle.Enabled)
			{
				StyleComboItem sci = m_cbCharStyle.SelectedItem as StyleComboItem;
				if (sci != null && sci.Style != null)
					m_current.StyleName = sci.Style.Name;
				else
					m_current.StyleName = String.Empty;
			}
			if (m_cbBeforeStyle.Visible && m_cbBeforeStyle.Enabled)
			{
				var sci = m_cbBeforeStyle.SelectedItem as StyleComboItem;
				if (sci != null && sci.Style != null)
					m_current.BeforeStyleName = sci.Style.Name;
				else
					m_current.BeforeStyleName = String.Empty;
			}
			if (m_lvWritingSystems.Visible && m_lvWritingSystems.Enabled)
				m_current.WsLabel = GenerateWsLabelFromListView();
		}

		private string GenerateWsLabelFromListView()
		{
			var sbLabel = new StringBuilder();
			foreach (ListViewItem lvi in m_lvWritingSystems.CheckedItems)
			{
				string sWs = String.Empty;
				if (lvi.Tag is int)
				{
					int ws = (int)lvi.Tag;
					switch (ws)
					{
						case WritingSystemServices.kwsAnal: sWs = "analysis"; break;
						case WritingSystemServices.kwsVern: sWs = "vernacular"; break;
						case WritingSystemServices.kwsPronunciation: sWs = "pronunciation"; break;
						case WritingSystemServices.kwsReversalIndex: sWs = "reversal"; break;
					}
				}
				else if (lvi.Tag is IWritingSystem)
				{
					sWs = ((IWritingSystem) lvi.Tag).Id;
				}
				if (sbLabel.Length > 0)
					sbLabel.Append(",");
				sbLabel.Append(sWs);
			}
			return sbLabel.ToString();
		}

		private void StoreSenseNumberData()
		{
			if (m_cbNumberStyle.SelectedIndex == 0)
			{
				m_current.Number = "";
			}
			else
			{
				m_current.Number = m_tbBeforeNumber.Text +
					(m_cbNumberStyle.SelectedItem as NumberStyleComboItem).FormatString +
					m_tbAfterNumber.Text;
				m_current.NumStyle = GenerateNumStyleFromCheckBoxes();
				m_current.NumFont = m_cbNumberFont.SelectedItem.ToString();	// item is a string actually...
				if (m_current.NumFont == xWorksStrings.ksUnspecified)
					m_current.NumFont = String.Empty;
				m_current.NumberSingleSense = m_chkNumberSingleSense.Checked;
			}
			m_current.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
			// Set the information on the child grammatical info node as well.
			foreach (TreeNode n in m_current.Nodes)
			{
				LayoutTreeNode ltn = n as LayoutTreeNode;
				if (ltn != null && ltn.ShowGramInfoConfig)
				{
					ltn.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
					break;
				}
			}
		}

		private void StoreGramInfoData()
		{
			if (m_grpSenseNumber.Visible)
				return;
			m_current.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
			// Set the information on the parent sense node as well.
			LayoutTreeNode ltn = m_current.Parent as LayoutTreeNode;
			if (ltn != null && ltn.ShowSenseConfig)
				ltn.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
		}

		private void StoreComplexFormData()
		{
			m_current.ShowComplexFormPara = m_chkComplexFormsAsParagraphs.Checked;
		}

		private string GenerateNumStyleFromCheckBoxes()
		{
			StringBuilder sbNumStyle = new StringBuilder();
			switch (m_chkSenseBoldNumber.CheckState)
			{
				case CheckState.Checked:
					sbNumStyle.Append("bold");
					break;
				case CheckState.Unchecked:
					sbNumStyle.Append("-bold");
					break;
			}
			switch (m_chkSenseItalicNumber.CheckState)
			{
				case CheckState.Checked:
					if (sbNumStyle.Length > 0)
						sbNumStyle.Append(" ");
					sbNumStyle.Append("italic");
					break;
				case CheckState.Unchecked:
					if (sbNumStyle.Length > 0)
						sbNumStyle.Append(" ");
					sbNumStyle.Append("-italic");
					break;
			}
			return sbNumStyle.ToString();
		}

		private void DisplayDetailsForTopLevelNode()
		{
			// Set up the details for top-level nodes.
			m_chkDisplayData.Checked = m_current.Checked;	// now layout attribute dependant
			m_chkDisplayData.Enabled = true;

			m_lblBefore.Visible = false;
			m_lblBetween.Visible = false;
			m_lblAfter.Visible = false;
			m_lblContext.Visible = false;
			m_tbBefore.Visible = false;
			m_tbBetween.Visible = false;
			m_tbAfter.Visible = false;
			m_chkDisplayWsAbbrs.Visible = false;

			DisplayDetailsForAParentNode(true);	// A top-level node is also a parent node!
		}

		private void DisplayControlsForAnyNode(bool fEnabled)
		{
			if (m_current.ShowSenseConfig)
				DisplaySenseConfigControls(fEnabled);
			else
				HideSenseConfigControls();
			if (m_current.ShowGramInfoConfig)
				DisplayGramInfoConfigControls(fEnabled);
			else
				HideGramInfoConfigControls();
			if (m_current.ShowComplexFormParaConfig)
				DisplayComplexFormConfigControls(fEnabled);
			else
				HideComplexFormConfigControls();
			DisplayBeforeStyleControls(fEnabled);
		}

		private void DisplayDetailsForAParentNode(bool fEnabled)
		{
			// Set up the details for nodes with subitems.
			m_chkDisplayWsAbbrs.Visible = false;
			m_lblListView.Visible = false;
			m_lvWritingSystems.Visible = false;
			m_btnMoveWsUp.Visible = false;
			m_btnMoveWsDown.Visible = false;
			m_lblCharStyle.Visible = false;
			m_cbCharStyle.Visible = false;
			m_btnStyles.Visible = false;

			DisplayContextControls(fEnabled);

			string sMoreDetail = String.Format(xWorksStrings.ksCanBeConfiguredInMoreDetail,
				m_current.Text);
			m_lblMoreDetail.Text = sMoreDetail;
			if (m_lblMoreDetail.Location.X + m_lblMoreDetail.PreferredWidth >
				panel1.ClientSize.Width - (panel1.Margin.Left + panel1.Margin.Right))
			{
				m_lblMoreDetail.Visible = false;
				m_lblMoreDetail.Enabled = false;
				m_tbMoreDetail.Text = sMoreDetail;
				m_tbMoreDetail.Visible = true;
				m_tbMoreDetail.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_lblMoreDetail.Visible = true;
				m_lblMoreDetail.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbMoreDetail.Visible = false;
				m_tbMoreDetail.Enabled = false;
			}
			m_lnkConfigureNow.Visible = true;
			m_lnkConfigureNow.Enabled = m_chkDisplayData.Checked && fEnabled;
			if (m_chkDisplayData.Checked && fEnabled)
				m_current.Expand();
		}

		private void DisplayDetailsForRecursiveNode(bool fEnabled)
		{
			m_lblListView.Visible = false;
			m_lvWritingSystems.Visible = false;
			m_btnMoveWsUp.Visible = false;
			m_btnMoveWsDown.Visible = false;
			m_lblCharStyle.Visible = false;
			m_cbCharStyle.Visible = false;
			m_btnStyles.Visible = false;
			m_lnkConfigureNow.Visible = false;
			m_chkDisplayWsAbbrs.Visible = false;

			DisplayContextControls(fEnabled);

			string sMoreDetail = String.Format(xWorksStrings.ksUsesTheSameConfigurationAs,
				m_current.Text, m_current.Parent.Text);
			m_lblMoreDetail.Text = sMoreDetail;
			if (m_lblMoreDetail.Location.X + m_lblMoreDetail.PreferredWidth >
				panel1.ClientSize.Width - (panel1.Margin.Left + panel1.Margin.Right))
			{
				m_lblMoreDetail.Visible = false;
				m_lblMoreDetail.Enabled = false;
				m_tbMoreDetail.Text = sMoreDetail;
				m_tbMoreDetail.Visible = true;
				m_tbMoreDetail.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_lblMoreDetail.Visible = true;
				m_lblMoreDetail.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbMoreDetail.Visible = false;
				m_tbMoreDetail.Enabled = false;
			}
		}

		private void DisplaySenseConfigControls(bool fEnabled)
		{
			if (m_dxContextOffset == 0)
				m_dxContextOffset = panel1.Height - m_lblContext.Location.Y;
			int dxOffset = panel1.Height - m_lblContext.Location.Y;
			// Make room for the group box when the dialog is at its minimum size.
			// We don't show the check box for showing ws abbrs in this situation.
			if (dxOffset == m_dxContextOffset)
			{
				MoveControlVertically(m_lblContext, 30);
				MoveControlVertically(m_lblBefore, 30);
				MoveControlVertically(m_lblBetween, 30);
				MoveControlVertically(m_lblAfter, 30);
				MoveControlVertically(m_tbBefore, 30);
				MoveControlVertically(m_tbBetween, 30);
				MoveControlVertically(m_tbAfter, 30);
			}
			string sBefore, sMark, sAfter;
			m_current.SplitNumberFormat(out sBefore, out sMark, out sAfter);
			m_grpSenseNumber.Visible = true;
			// Don't show the "Show single gram info first" checkbox control for the
			// subsense display.
			m_chkShowSingleGramInfoFirst.Visible = !(m_current.Parent as LayoutTreeNode).ShowSenseConfig &&
				!m_defaultRootLayoutName.Contains("Reversal");
			if (m_current.Number == "")
				m_cbNumberStyle.SelectedIndex = 0;
			else if (sMark == "%O")
				m_cbNumberStyle.SelectedIndex = 1;
			else
				m_cbNumberStyle.SelectedIndex = 2;
			m_tbBeforeNumber.Text = sBefore;
			m_tbAfterNumber.Text = sAfter;
			SetNumStyleCheckStates();
			if (String.IsNullOrEmpty(m_current.NumFont))
			{
				m_cbNumberFont.SelectedIndex = 0;
				m_cbNumberFont.SelectedText = "";
			}
			else
			{
				for (int i = 0; i < m_cbNumberFont.Items.Count; ++i)
				{
					if (m_current.NumFont == m_cbNumberFont.Items[i].ToString())
					{
						m_cbNumberFont.SelectedIndex = i;
						break;
					}
				}
			}
			m_chkNumberSingleSense.Checked = m_current.NumberSingleSense;
			m_chkShowSingleGramInfoFirst.Checked = m_current.ShowSingleGramInfoFirst;
			EnableSenseConfigControls(fEnabled);
		}

		private void SetNumStyleCheckStates()
		{
			CheckState csBold = CheckState.Indeterminate;
			CheckState csItalic = CheckState.Indeterminate;
			string sStyle = m_current.NumStyle;
			if (!String.IsNullOrEmpty(sStyle))
			{
				sStyle = sStyle.ToLowerInvariant();
				if (sStyle.IndexOf("-bold") >= 0)
					csBold = CheckState.Unchecked;
				else if (sStyle.IndexOf("bold") >= 0)
					csBold = CheckState.Checked;
				if (sStyle.IndexOf("-italic") >= 0)
					csItalic = CheckState.Unchecked;
				else if (sStyle.IndexOf("italic") >= 0)
					csItalic = CheckState.Checked;
			}
			m_chkSenseBoldNumber.CheckState = csBold;
			m_chkSenseItalicNumber.CheckState = csItalic;
		}

		private void HideSenseConfigControls()
		{
			m_grpSenseNumber.Visible = false;
			m_chkShowSingleGramInfoFirst.Visible = false;
			// Restore the original position of the surrounding context controls if
			// necessary.
			if (m_dxContextOffset == 0)
				m_dxContextOffset = panel1.Height - m_lblContext.Location.Y;
			int dxOffset = panel1.Height - m_lblContext.Location.Y;
			if (dxOffset != m_dxContextOffset)
			{
				int diff = dxOffset - m_dxContextOffset;
				MoveControlVertically(m_lblContext, diff);
				MoveControlVertically(m_lblBefore, diff);
				MoveControlVertically(m_lblBetween, diff);
				MoveControlVertically(m_lblAfter, diff);
				MoveControlVertically(m_tbBefore, diff);
				MoveControlVertically(m_tbBetween, diff);
				MoveControlVertically(m_tbAfter, diff);
			}
		}

		private void DisplayGramInfoConfigControls(bool fEnabled)
		{
			m_chkShowSingleGramInfoFirst.Visible = true;
			m_chkShowSingleGramInfoFirst.Checked = m_current.ShowSingleGramInfoFirst;
			m_chkShowSingleGramInfoFirst.Enabled = fEnabled;
		}

		private void HideGramInfoConfigControls()
		{
			if (m_current.ShowSenseConfig)
				return;
			m_chkShowSingleGramInfoFirst.Visible = false;
		}

		private void DisplayComplexFormConfigControls(bool fEnabled)
		{
			m_chkComplexFormsAsParagraphs.Visible = true;
			m_chkComplexFormsAsParagraphs.Checked = m_current.ShowComplexFormPara;
			m_chkComplexFormsAsParagraphs.Enabled = fEnabled;
		}

		private void HideComplexFormConfigControls()
		{
			m_chkComplexFormsAsParagraphs.Visible = false;
		}

		private void MoveControlVertically(Control ctl, int dy)
		{
			ctl.Location = new Point(ctl.Location.X, ctl.Location.Y + dy); ;
		}

		private void EnableSenseConfigControls(bool fEnabled)
		{
			m_grpSenseNumber.Enabled = m_chkDisplayData.Checked && fEnabled;
			m_chkShowSingleGramInfoFirst.Enabled = m_chkDisplayData.Checked && fEnabled;
		}

		private void DisplayDetailsForLeafNode(bool fEnabled)
		{
			m_lblMoreDetail.Visible = false;
			m_lnkConfigureNow.Visible = false;

			DisplayContextControls(fEnabled);
			DisplayWritingSystemControls(fEnabled);
			DisplayStyleControls(fEnabled);
			if (m_current.ShowSenseConfig)
				DisplaySenseConfigControls(fEnabled);
			else
				HideSenseConfigControls();
			m_chkDisplayWsAbbrs.Visible = m_lvWritingSystems.Visible;
			if (m_chkDisplayWsAbbrs.Visible)
			{
				if (m_lvWritingSystems.CheckedIndices.Count > 1)
				{
					m_chkDisplayWsAbbrs.Checked = m_current.ShowWsLabels;
					m_chkDisplayWsAbbrs.Enabled = m_chkDisplayData.Checked && fEnabled;
				}
				else
				{
					m_chkDisplayWsAbbrs.Checked = false;
					m_chkDisplayWsAbbrs.Enabled = false;
				}
			}
		}

		private void DisplayWritingSystemControls(bool fEnabled)
		{
			// don't disable the display just because we don't have a WsLabel. If we have
			// a WsType, we should be able to construct a list and select a default. (LT-9862)
			if (string.IsNullOrEmpty(m_current.WsLabel) && string.IsNullOrEmpty(m_current.WsType))
			{
				m_lblListView.Visible = false;
				m_lvWritingSystems.Visible = false;
				m_btnMoveWsUp.Visible = false;
				m_btnMoveWsDown.Visible = false;
				return;
			}
			m_lblListView.Visible = true;
			m_lblListView.Enabled = m_chkDisplayData.Checked && fEnabled;
			InitializeWsListView();
			m_lvWritingSystems.Visible = true;
			m_lvWritingSystems.Enabled = m_chkDisplayData.Checked && fEnabled;
			m_btnMoveWsUp.Visible = true;
			m_btnMoveWsDown.Visible = true;
			if (m_lvWritingSystems.SelectedIndices.Count > 1)
			{
				m_btnMoveWsUp.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_btnMoveWsDown.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_btnMoveWsUp.Enabled = false;
				m_btnMoveWsDown.Enabled = false;
			}
		}

		private void InitializeWsListView()
		{
			m_lvWritingSystems.Items.Clear();
			ListViewItem lvi;
			int wsDefault = 0;
			int wsDefault2 = 0;
			switch (m_current.WsType)
			{
				case "analysis":
					lvi = new ListViewItem(xWorksStrings.ksDefaultAnalysis);
					wsDefault = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault;
					m_lvWritingSystems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_lvWritingSystems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
				case "vernacular":
					lvi = new ListViewItem(xWorksStrings.ksDefaultVernacular);
					wsDefault = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault;
					m_lvWritingSystems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
						m_lvWritingSystems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
				case "pronunciation":
					lvi = new ListViewItem(xWorksStrings.ksDefaultPronunciation);
					wsDefault = WritingSystemServices.kwsPronunciation;
					lvi.Tag = wsDefault;
					m_lvWritingSystems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems)
						m_lvWritingSystems.Items.Add(new ListViewItem(ws.DisplayLabel) { Tag = ws });
					break;
				case "reversal":
					lvi = new ListViewItem(xWorksStrings.ksCurrentReversal);
					wsDefault = WritingSystemServices.kwsReversalIndex;
					lvi.Tag = wsDefault;
					m_lvWritingSystems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_lvWritingSystems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
				case "analysis vernacular":
					lvi = new ListViewItem(xWorksStrings.ksDefaultAnalysis);
					wsDefault = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault;
					m_lvWritingSystems.Items.Add(lvi);
					lvi = new ListViewItem(xWorksStrings.ksDefaultVernacular);
					wsDefault2 = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault2;
					m_lvWritingSystems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_lvWritingSystems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
						m_lvWritingSystems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
				default:
					lvi = new ListViewItem(xWorksStrings.ksDefaultVernacular);
					wsDefault = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault;
					m_lvWritingSystems.Items.Add(lvi);
					lvi = new ListViewItem(xWorksStrings.ksDefaultAnalysis);
					wsDefault2 = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault2;
					m_lvWritingSystems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
						m_lvWritingSystems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_lvWritingSystems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
			}
			switch (m_current.WsLabel)
			{
				case "":
					// it used to be possible to get in a situation where WsLabel is (persisted) empty, e.g. if
					// the user hides/unselects writing systems in the Setup Writing Systems dialog
					// and the revisits this dialog tree node leaving no items selected.
					// In that case, we want to go ahead and select a reasonable default to
					// restore this node to a sensible state. (LT-9862)
					SelectDefaultWss(wsDefault, wsDefault2);
					break;
				case "analysis":
					SetDefaultWritingSystem(WritingSystemServices.kwsAnal);
					break;
				case "vernacular":
					SetDefaultWritingSystem(WritingSystemServices.kwsVern);
					break;
				case "pronunciation":
					SetDefaultWritingSystem(WritingSystemServices.kwsPronunciation);
					break;
				case "reversal":
					SetDefaultWritingSystem(WritingSystemServices.kwsReversalIndex);
					break;
				case "all reversal":
				case "all analysis":
				case "all vernacular":
					for (int i = 0; i < m_lvWritingSystems.Items.Count; ++i)
					{
						var lws = m_lvWritingSystems.Items[i].Tag as ILgWritingSystem;
						if (lws != null)
							m_lvWritingSystems.Items[i].Checked = true;
					}
					break;
				default:	// must be explicit ws tags.
					string[] rgws = m_current.WsLabel.Split(new[] { ',' });
					int indexTarget = 0;
					for (int i = 0; i < m_lvWritingSystems.Items.Count; ++i)
					{
						if (!(m_lvWritingSystems.Items[i].Tag is int))
						{
							indexTarget = i;
							break;
						}
					}
					for (int i = 0; i < rgws.Length; ++i)
					{
						string sLabel = rgws[i];
						bool fChecked = false;
						for (int iws = 0; iws < m_lvWritingSystems.Items.Count; ++iws)
						{
							var ws = m_lvWritingSystems.Items[iws].Tag as IWritingSystem;
							if (ws != null && ws.Id == sLabel)
							{
								m_lvWritingSystems.Items[iws].Checked = true;
								MoveWritingSystemsItem(iws, indexTarget++);
								fChecked = true;
								break;
							}
						}
						if (!fChecked)
						{
							// Add this to the list of writing systems, since the user must have
							// wanted it at some time.
							IWritingSystem ws;
							if (m_cache.ServiceLocator.WritingSystemManager.TryGet(sLabel, out ws))
								m_lvWritingSystems.Items.Insert(indexTarget++, new ListViewItem(ws.DisplayLabel) {Tag = ws, Checked = true});
						}
					}
					break;
			}
			// if for some reason nothing was selected, try to select a default.
			if (m_lvWritingSystems.CheckedItems.Count == 0)
				SelectDefaultWss(wsDefault, wsDefault2);
			if (m_lvWritingSystems.CheckedItems.Count == 0)
				SelectAllDefaultWritingSystems();
		}

		private void SelectDefaultWss(int wsDefault, int wsDefault2)
		{
			if (wsDefault != 0)
				SetDefaultWritingSystem(wsDefault);
			if (wsDefault2 != 0)
				SetDefaultWritingSystem(wsDefault2);
		}

		private void SetDefaultWritingSystem(int wsWanted)
		{
			for (int i = 0; i < m_lvWritingSystems.Items.Count; ++i)
			{
				if (m_lvWritingSystems.Items[i].Tag is int)
				{
					int ws = (int)m_lvWritingSystems.Items[i].Tag;
					if (ws == wsWanted)
					{
						m_lvWritingSystems.Items[i].Checked = true;
						break;
					}
				}
			}
		}

		private void SelectAllDefaultWritingSystems()
		{
			for (int i = 0; i < m_lvWritingSystems.Items.Count; ++i)
			{
				if (m_lvWritingSystems.Items[i].Tag is int)
					m_lvWritingSystems.Items[i].Checked = true;
			}
		}

		private void DisplayStyleControls(bool fEnabled)
		{
			if (m_current.AllowCharStyle)
			{
				m_lblCharStyle.Text = xWorksStrings.ksCharacterStyle;
				m_cbCharStyle.Items.Clear();
				m_cbCharStyle.Items.AddRange(m_rgCharStyles.ToArray());
				for (int i = 0; i < m_rgCharStyles.Count; ++i)
				{
					if (m_rgCharStyles[i].Style != null &&
						m_rgCharStyles[i].Style.Name == m_current.StyleName)
					{
						m_cbCharStyle.SelectedIndex = i;
						break;
					}
				}
				m_lblCharStyle.Visible = true;
				m_cbCharStyle.Visible = true;
				m_btnStyles.Visible = true;
				m_lblCharStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_cbCharStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_btnStyles.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else if (m_current.AllowParaStyle)
			{
				m_lblCharStyle.Text = xWorksStrings.ksParagraphStyle;
				m_cbCharStyle.Items.Clear();
				m_cbCharStyle.Items.AddRange(m_rgParaStyles.ToArray());
				for (int i = 0; i < m_rgParaStyles.Count; ++i)
				{
					if (m_rgParaStyles[i].Style != null &&
						m_rgParaStyles[i].Style.Name == m_current.StyleName)
					{
						m_cbCharStyle.SelectedIndex = i;
						break;
					}
				}
				m_lblCharStyle.Visible = true;
				m_cbCharStyle.Visible = true;
				m_btnStyles.Visible = true;
				m_lblCharStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_cbCharStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_btnStyles.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_lblCharStyle.Visible = false;
				m_cbCharStyle.Visible = false;
				m_btnStyles.Visible = false;
				return;
			}
		}

		private void DisplayBeforeStyleControls(bool fEnabled)
		{
			if (m_current.AllowBeforeStyle && !m_lvWritingSystems.Visible)
			{
				m_cbBeforeStyle.Items.Clear();
				bool fParaStyles = m_current.FlowType == "div";
				if (fParaStyles)
				{
					m_lblBeforeStyle.Text = xWorksStrings.ksParagraphStyleForBefore;
					m_cbBeforeStyle.Items.AddRange(m_rgParaStyles.ToArray());
					for (int i = 0; i < m_rgParaStyles.Count; ++i)
					{
						if (m_rgParaStyles[i].Style != null &&
							m_rgParaStyles[i].Style.Name == m_current.BeforeStyleName)
						{
							m_cbBeforeStyle.SelectedIndex = i;
							break;
						}
					}
				}
				else
				{
					m_lblBeforeStyle.Text = xWorksStrings.ksCharacterStyleForBefore;
					m_cbBeforeStyle.Items.AddRange(m_rgCharStyles.ToArray());
					for (int i = 0; i < m_rgCharStyles.Count; ++i)
					{
						if (m_rgCharStyles[i].Style != null &&
							m_rgCharStyles[i].Style.Name == m_current.BeforeStyleName)
						{
							m_cbBeforeStyle.SelectedIndex = i;
							break;
						}
					}
				}
				m_lblBeforeStyle.Visible = true;
				m_cbBeforeStyle.Visible = true;
				m_btnBeforeStyles.Visible = true;
				m_lblBeforeStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_cbBeforeStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_btnBeforeStyles.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_lblBeforeStyle.Visible = false;
				m_cbBeforeStyle.Visible = false;
				m_btnBeforeStyles.Visible = false;
				return;
			}
		}

		private void DisplayContextControls(bool fEnabled)
		{
			if (m_current.Before == null && m_current.After == null && m_current.Between == null)
			{
				m_lblContext.Visible = false;
				m_lblBefore.Visible = false;
				m_tbBefore.Visible = false;
				m_lblAfter.Visible = false;
				m_tbAfter.Visible = false;
				m_lblBetween.Visible = false;
				m_tbBetween.Visible = false;
				return;
			}

			m_lblContext.Visible = true;
			m_lblContext.Enabled = m_chkDisplayData.Checked && fEnabled;
			if (m_current.Before != null)
			{
				m_lblBefore.Text = xWorksStrings.ksBefore;
				m_tbBefore.Text = m_current.Before;
				m_lblBefore.Visible = true;
				m_tbBefore.Visible = true;
				m_lblBefore.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbBefore.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				//m_lblBefore.Visible = false;
				//m_tbBefore.Visible = false;
				m_tbBefore.Text = "";
				m_lblBefore.Visible = true;
				m_tbBefore.Visible = true;
				m_lblBefore.Enabled = false;
				m_tbBefore.Enabled = false;
			}

			if (m_current.After != null)
			{
				m_tbAfter.Text = m_current.After;
				m_lblAfter.Visible = true;
				m_tbAfter.Visible = true;
				m_lblAfter.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbAfter.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				//m_lblAfter.Visible = false;
				//m_tbAfter.Visible = false;
				m_tbAfter.Text = "";
				m_lblAfter.Visible = true;
				m_tbAfter.Visible = true;
				m_lblAfter.Enabled = false;
				m_tbAfter.Enabled = false;
			}

			if (m_current.Between != null)
			{
				m_tbBetween.Text = m_current.Between;
				m_lblBetween.Visible = true;
				m_tbBetween.Visible = true;
				m_lblBetween.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbBetween.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else //if (m_tbBefore.Visible && m_tbAfter.Visible)
			{
				m_tbBetween.Text = "";
				m_lblBetween.Visible = true;
				m_tbBetween.Visible = true;
				m_lblBetween.Enabled = false;
				m_tbBetween.Enabled = false;
			}
			//else
			//{
			//    m_lblBetween.Visible = false;
			//    m_tbBetween.Visible = false;
			//}
		}

		/// <summary>
		/// Return true if any changes have been made to the layout configuration.
		/// </summary>
		/// <returns></returns>
		private bool IsDirty()
		{
			StoreNodeData();
			if (m_fDeleteCustomFiles)
				return true;
			string sOldRootLayout = m_mediator.PropertyTable.GetStringProperty(m_sLayoutPropertyName, null);
			string sRootLayout = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).LayoutName;
			if (sOldRootLayout != sRootLayout)
				return true;
			for (int ici = 0; ici < m_cbDictType.Items.Count; ++ici)
			{
				LayoutTypeComboItem ltci = (LayoutTypeComboItem)m_cbDictType.Items[ici];
				for (int itn = 0; itn < ltci.TreeNodes.Count; ++itn)
				{
					LayoutTreeNode ltn = ltci.TreeNodes[itn];
					if (ltn.IsDirty())
						return true;
				}
			}
			return false;
		}

		private void SaveModifiedLayouts()
		{
			List<XmlNode> rgxnLayouts = new List<XmlNode>();
			for (int ici = 0; ici < m_cbDictType.Items.Count; ++ici)
			{
				LayoutTypeComboItem ltci = (LayoutTypeComboItem)m_cbDictType.Items[ici];
				for (int itn = 0; itn < ltci.TreeNodes.Count; ++itn)
				{
					ltci.TreeNodes[itn].MakeSenseNumberSchemeConsistent();
					ltci.TreeNodes[itn].GetModifiedLayouts(rgxnLayouts);
				}
			}
			if (m_fDeleteCustomFiles)
			{
				Debug.Assert(m_layouts.DatabaseName == null);
				m_layouts.DeleteUserOverrides(m_cache.ProjectId.Name);
				m_parts.DeleteUserOverrides(m_cache.ProjectId.Name);
				Inventory.SetInventory("layouts", m_cache.ProjectId.Name, m_layouts);
				Inventory.SetInventory("parts", m_cache.ProjectId.Name, m_parts);
				Inventory.RemoveInventory("layouts", null);
				Inventory.RemoveInventory("parts", null);
			}
			for (int i = 0; i < rgxnLayouts.Count; ++i)
				m_layouts.PersistOverrideElement(rgxnLayouts[i]);
		}


		#endregion // Misc internal functions

		#region IFWDisposable Members

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion // IFWDisposable Members

		#region LayoutTreeNode class

		public class LayoutTreeNode : TreeNode
		{
			// These are basic values that we need to know for every node.
			XmlNode m_xnConfig;
			string m_sLayoutName;
			string m_sPartName;
			string m_sClassName;
			string m_sLabel;
			string m_sVisibility;
			bool m_fContentVisible = false;
			bool m_fUseParentConfig = false;
			bool m_fShowSenseConfig = false;
			bool m_fShowGramInfoConfig = false;
			bool m_fShowComplexFormParaConfig = false;
			string m_sParam = null;
			string m_sFlowType = null;

			// These values depend on the particular node, and affect what is displayed in the
			// details pane.  If a string value is null, then the corresponding control (and
			// label) is not shown.
			string m_sBefore = null;
			string m_sAfter = null;
			string m_sSep = null;
			string m_sWsLabel = null;
			string m_sWsType = null;
			string m_sStyleName = null;
			string m_sBeforeStyleName = null;
			bool m_fAllowBeforeStyle = false;
			bool m_fAllowCharStyle = false;
			bool m_fAllowParaStyle = false;
			string m_sNumber = null;
			string m_sNumStyle = null;
			bool m_fNumSingle = false;
			bool m_fSingleGramInfoFirst = false;
			bool m_fShowComplexFormPara = false;
			string m_sNumFont = null;
			bool m_fShowWsLabels = false;

			// These are used to trace creating, deleting, and moving nodes.
			bool m_fDuplicate = false;
			string m_sDup = null;
			int m_cSubnodes = -1;
			int m_idxOrig = -1;

			XmlNode m_xnCallingLayout;
			XmlNode m_xnParentLayout = null;

			XmlNode m_xnHiddenNode = null;
			XmlNode m_xnHiddenParentLayout = null;
			XmlNode m_xnHiddenCallingLayout = null;

			/// <summary>
			/// This node is a hidden child that provides/receives the style name.
			/// </summary>
			XmlNode m_xnHiddenChild = null;
			XmlNode m_xnHiddenChildLayout = null;
			bool m_fStyleFromHiddenChild = false;
			bool m_fHiddenChildDirty = false;

			List<LayoutTreeNode> m_rgltnMerged = new List<LayoutTreeNode>();

			public LayoutTreeNode(XmlNode config, StringTable stringTbl, string classParent)
			{
				m_xnConfig = config;
				m_sLabel = XmlUtils.GetLocalizedAttributeValue(stringTbl, config, "label", null);
				if (config.Name == "configure")
				{
					m_sClassName = XmlUtils.GetManditoryAttributeValue(config, "class");
					m_sLayoutName = XmlUtils.GetManditoryAttributeValue(config, "layout");
					m_sPartName = String.Empty;
					m_sVisibility = "required";
				}
				else if (config.Name == "part")
				{
					m_sClassName = classParent;
					string sRef = XmlUtils.GetManditoryAttributeValue(config, "ref");
					if (m_sLabel == null && stringTbl != null)
						m_sLabel = stringTbl.LocalizeAttributeValue(sRef);
					if (config.ParentNode != null && config.ParentNode.Name == "layout")
						m_sLayoutName = XmlUtils.GetManditoryAttributeValue(config.ParentNode, "name");
					else
						m_sLayoutName = String.Empty;
					m_sPartName = String.Format("{0}-Jt-{1}", classParent, sRef);
					m_sVisibility = XmlUtils.GetOptionalAttributeValue(config, "visibility", "always");
					m_fContentVisible = m_sVisibility.ToLowerInvariant() != "never";
					m_sParam = XmlUtils.GetOptionalAttributeValue(config, "param");

					m_sWsLabel = XmlUtils.GetOptionalAttributeValue(config, "ws");
					if (m_sWsLabel != null)
					{
						if (m_sWsLabel.StartsWith(StringUtils.WsParamLabel))
							m_sWsLabel = m_sWsLabel.Substring(StringUtils.WsParamLabel.Length);
					}
					m_sWsType = XmlUtils.GetOptionalAttributeValue(config, "wsType");
					if (m_sWsLabel != null && String.IsNullOrEmpty(m_sWsType))
					{
						// Try to calculate a WS type from the WS label.
						int ichVern = m_sWsLabel.ToLowerInvariant().IndexOf("vern");
						int ichAnal = m_sWsLabel.ToLowerInvariant().IndexOf("anal");
						int ichPronun = m_sWsLabel.ToLowerInvariant().IndexOf("pronun");
						int ichRevers = m_sWsLabel.ToLowerInvariant().IndexOf("revers");
						if (ichVern >= 0 && ichAnal >= 0 && ichVern > ichAnal)
							m_sWsType = "analysis vernacular";
						else if (ichVern >= 0 && ichAnal >= 0 && ichAnal > ichVern)
							m_sWsType = "vernacular analysis";
						else if (ichVern >= 0)
							m_sWsType = "vernacular";
						else if (ichAnal >= 0)
							m_sWsType = "analysis";
						else if (ichPronun >= 0)
							m_sWsType = "pronunciation";
						else if (ichRevers >= 0)
							m_sWsType = "reversal";
						else
						{
							Debug.Fail(String.Format("This layout node ({0}) does not specify @wsType "
								+ "and we couldn't compute something reasonable from @ws='{1}' "
								+ "so we're setting @wsType to 'vernacular analysis'",
								config.Attributes["ref"].Value, config.Attributes["ws"].Value));
							m_sWsType = "vernacular analysis";	// who knows???
					}
						// store the wsType attribute on the node, so that if 'ws' changes to something
						// specific, we still know what type of wss to provide options for in the m_lvWritingSystems.
						XmlAttribute xa = config.OwnerDocument.CreateAttribute("wsType");
						xa.Value = m_sWsType;
						config.Attributes.Append(xa);
					}
					if (m_sWsType != null)
					{
						if (m_sWsType.StartsWith(StringUtils.WsParamLabel))
							m_sWsType = m_sWsType.Substring(StringUtils.WsParamLabel.Length);
						if (m_sWsLabel == null)
							m_sWsLabel = "";
					}
					string sSep = null;
					// By default, if we have a ws type or ws label we should be able to show multiple wss,
					// and thus need a separator between them.
					if (!String.IsNullOrEmpty(m_sWsLabel) || !String.IsNullOrEmpty(m_sWsType))
						sSep = " ";
					m_sBeforeStyleName = XmlUtils.GetOptionalAttributeValue(config, "beforeStyle");
					m_fAllowBeforeStyle = !String.IsNullOrEmpty(m_sBeforeStyleName);
					m_sBefore = XmlUtils.GetOptionalAttributeValue(config, "before", "");
					m_sSep = XmlUtils.GetOptionalAttributeValue(config, "sep", sSep);
					m_sAfter = XmlUtils.GetOptionalAttributeValue(config, "after", " ");

					m_sStyleName = XmlUtils.GetOptionalAttributeValue(config, "style");
					m_sFlowType = XmlUtils.GetOptionalAttributeValue(config, "flowType", "span");
					if (m_sFlowType == "span")
					{
						m_fAllowCharStyle = true;
						if (m_sBefore == null)
							m_sBefore = "";
						if (m_sAfter == null)
							m_sAfter = "";
					}
					// Special handling for div flow elements, which can contain a sequence of paragraphs.
					else if (m_sFlowType == "div")
					{
						m_fAllowParaStyle = m_sClassName == "StText";
						if (m_fAllowParaStyle)
						{
							// We'll be getting the style name from a child layout.
							Debug.Assert(String.IsNullOrEmpty(m_sStyleName));
						}
						m_sSep = null;
						m_sAfter = null;
					}
					else if (m_sFlowType == "para")
					{
						m_fAllowParaStyle = !String.IsNullOrEmpty(m_sStyleName);
					}
					m_sNumber = XmlUtils.GetOptionalAttributeValue(config, "number");
					m_sNumStyle = XmlUtils.GetOptionalAttributeValue(config, "numstyle");
					m_fNumSingle = XmlUtils.GetOptionalBooleanAttributeValue(config, "numsingle", false);
					m_sNumFont = XmlUtils.GetOptionalAttributeValue(config, "numfont");
					m_fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(config, "singlegraminfofirst", false);
					m_fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(config, "showasindentedpara", false);
					m_fShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(config, "showLabels", false);
					m_sDup = XmlUtils.GetOptionalAttributeValue(config, "dup");
					m_fDuplicate = !String.IsNullOrEmpty(m_sDup);
				}
				this.Checked = m_sVisibility.ToLowerInvariant() != "never";
				this.Text = m_sLabel;
				this.Name = String.Format("{0}/{1}/{2}", m_sClassName, m_sLayoutName, m_sPartName);
			}

			public LayoutTreeNode()
			{
			}

#if __MonoCS__ // work around mono bug https://bugzilla.novell.com/show_bug.cgi?id=613708
			// Create a LayoutTreeNode from a TreeNode constructor.
			protected LayoutTreeNode(TreeNode copyNode) : base(copyNode.Text, copyNode.ImageIndex, copyNode.SelectedImageIndex)
			{
				if (copyNode.Nodes != null) {
					foreach (TreeNode child in copyNode.Nodes)
						Nodes.Add ((TreeNode)child.Clone ());
				}
				this.Tag = copyNode.Tag;
				this.Checked = copyNode.Checked;
				this.BackColor = copyNode.BackColor;
				this.ForeColor = copyNode.ForeColor;
				this.NodeFont = copyNode.NodeFont;
			}
#endif


			public override object Clone()
			{
#if !__MonoCS__
				LayoutTreeNode ltn = (LayoutTreeNode)base.Clone();
#else // work around mono bug https://bugzilla.novell.com/show_bug.cgi?id=613708
				LayoutTreeNode ltn = new LayoutTreeNode(this);
#endif
				ltn.m_xnConfig = m_xnConfig;
				ltn.m_sLayoutName = m_sLayoutName;
				ltn.m_sPartName = m_sPartName;
				ltn.m_sClassName = m_sClassName;
				ltn.m_sLabel = m_sLabel;
				ltn.m_sVisibility = m_sVisibility;
				ltn.m_fContentVisible = m_fContentVisible;
				ltn.m_fUseParentConfig = m_fUseParentConfig;
				ltn.m_fShowSenseConfig = m_fShowSenseConfig;
				ltn.m_sParam = m_sParam;
				ltn.m_sFlowType = m_sFlowType;
				ltn.m_sBeforeStyleName = m_sBeforeStyleName;
				ltn.m_fAllowBeforeStyle = m_fAllowBeforeStyle;
				ltn.m_sBefore = m_sBefore;
				ltn.m_sAfter = m_sAfter;
				ltn.m_sSep = m_sSep;
				ltn.m_sWsLabel = m_sWsLabel;
				ltn.m_sWsType = m_sWsType;
				ltn.m_sStyleName = m_sStyleName;
				ltn.m_fAllowCharStyle = m_fAllowCharStyle;
				ltn.m_sNumber = m_sNumber;
				ltn.m_sNumStyle = m_sNumStyle;
				ltn.m_fNumSingle = m_fNumSingle;
				ltn.m_sNumFont = m_sNumFont;
				ltn.m_fSingleGramInfoFirst = m_fSingleGramInfoFirst;
				ltn.m_fShowComplexFormPara = m_fShowComplexFormPara;
				ltn.m_fShowWsLabels = m_fShowWsLabels;
				ltn.m_fDuplicate = m_fDuplicate;
				ltn.m_sDup = m_sDup;
				ltn.m_cSubnodes = m_cSubnodes;
				ltn.m_idxOrig = m_idxOrig;
				ltn.m_xnCallingLayout = m_xnCallingLayout;
				ltn.m_xnParentLayout = m_xnParentLayout;
				ltn.m_xnHiddenNode = m_xnHiddenNode;
				ltn.m_xnHiddenParentLayout = m_xnHiddenParentLayout;
				ltn.m_xnHiddenCallingLayout = m_xnHiddenCallingLayout;
				ltn.m_xnHiddenChild = m_xnHiddenChild;
				ltn.m_xnHiddenChildLayout = m_xnHiddenChildLayout;
				ltn.m_fStyleFromHiddenChild = m_fStyleFromHiddenChild;

				return ltn;
			}

			public bool AllParentsChecked
			{
				get
				{
					TreeNode tn = Parent;
					while (tn != null)
					{
						if (!tn.Checked)
							return false;
						tn = tn.Parent;
					}
					return true;
				}
			}

			public bool IsDescendedFrom(TreeNode tnPossibleAncestor)
			{
				TreeNode tn = Parent;
				while (tn != null)
				{
					if (tn == tnPossibleAncestor)
						return true;
					tn = tn.Parent;
				}
				return false;
			}

			public XmlNode Configuration
			{
				get { return m_xnConfig; }
			}

			public string LayoutName
			{
				get { return m_sLayoutName; }
			}

			public string PartName
			{
				get { return m_sPartName; }
			}

			public string ClassName
			{
				get { return m_sClassName; }
			}

			public string FlowType
			{
				get { return m_sFlowType; }
			}

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

			public bool ShowGramInfoConfig
			{
				get { return m_fShowGramInfoConfig; }
				set { m_fShowGramInfoConfig = value; }
			}

			public bool ShowComplexFormParaConfig
			{
				get { return m_fShowComplexFormParaConfig; }
				set { m_fShowComplexFormParaConfig = value; }
			}

			public bool ContentVisible
			{
				get { return m_fContentVisible; }
				set { m_fContentVisible = value; }
			}

			public string BeforeStyleName
			{
				get { return m_sBeforeStyleName; }
				set { m_sBeforeStyleName = value; }
			}

			public string Before
			{
				get { return m_sBefore; }
				set { m_sBefore = value; }
			}

			public string After
			{
				get { return m_sAfter; }
				set { m_sAfter = value; }
			}

			public string Between
			{
				get { return m_sSep; }
				set { m_sSep = value; }
			}

			public string WsLabel
			{
				get { return m_sWsLabel; }
				set { m_sWsLabel = value; }
			}

			public string WsType
			{
				get { return m_sWsType; }
				set { m_sWsType = value; }
			}

			public string StyleName
			{
				get { return m_sStyleName; }
				set { m_sStyleName = value; }
			}

			public bool AllowCharStyle
			{
				get { return m_fAllowCharStyle; }
			}

			public bool AllowBeforeStyle
			{
				get { return m_fAllowBeforeStyle; }
			}

			public bool AllowParaStyle
			{
				get { return m_fAllowParaStyle; }
			}

			public string Number
			{
				get { return m_sNumber; }
				set { m_sNumber = value; }
			}

			public string NumStyle
			{
				get { return m_sNumStyle; }
				set { m_sNumStyle = value; }
			}

			public bool NumberSingleSense
			{
				get { return m_fNumSingle; }
				set { m_fNumSingle = value; }
			}

			public string NumFont
			{
				get { return m_sNumFont; }
				set { m_sNumFont = value; }
			}

			public bool ShowSingleGramInfoFirst
			{
				get { return m_fSingleGramInfoFirst; }
				set { m_fSingleGramInfoFirst = value; }
			}

			public bool ShowComplexFormPara
			{
				get { return m_fShowComplexFormPara; }
				set { m_fShowComplexFormPara = value; }
			}

			public bool ShowWsLabels
			{
				get { return m_fShowWsLabels; }
				set { m_fShowWsLabels = value; }
			}

			public string Param
			{
				get { return m_sParam; }
				set { m_sParam = value; }
			}

			public bool IsDuplicate
			{
				get { return m_fDuplicate; }
				set { m_fDuplicate = value; }
			}

			public string DupString
			{
				get { return m_sDup; }
				set
				{
					m_sDup = value;
					if (Nodes.Count > 0)
					{
						string sDupChild = String.Format("{0}.0", value);
						for (int i = 0; i < Nodes.Count; ++i)
							((LayoutTreeNode)Nodes[i]).DupString = sDupChild;
					}
				}
			}

			internal int OriginalIndex
			{
				set
				{
					Debug.Assert(m_idxOrig == -1 || m_idxOrig == value);
					if (m_idxOrig == -1)
						m_idxOrig = value;
				}
			}

			internal int OriginalNumberOfSubnodes
			{
				get
				{
					return m_cSubnodes == -1 ? 0 : m_cSubnodes;
				}
				set
				{
					Debug.Assert(value >= m_cSubnodes);
					if (m_cSubnodes == -1 || Level == 0)
						m_cSubnodes = value;
				}
			}

			internal bool IsDirty()
			{
				if (IsNodeDirty())
					return true;
				for (int i = 0; i < Nodes.Count; ++i)
				{
					LayoutTreeNode ltn = (LayoutTreeNode)Nodes[i];
					if (ltn.IsDirty())
						return true;
				}
				return false;
			}

			internal bool IsNodeDirty()
			{
				if (Index != m_idxOrig)
					return true;
				if (Nodes.Count != OriginalNumberOfSubnodes)
					return true;
				if (Level > 0)
				{
					// Now, compare our member variables to the content of m_xnConfig.
					if (m_xnConfig.Name == "part")
					{
						bool fContentVisible = m_sVisibility != "never";
						m_fContentVisible = this.Checked;	// in case (un)checked in treeview, but node never selected.
						if (fContentVisible != m_fContentVisible)
							return true;
						string sBeforeStyleName = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "beforeStyle");
						if (StringsDiffer(sBeforeStyleName, m_sBeforeStyleName))
							return true;
						string sBefore = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "before");
						if (StringsDiffer(sBefore, m_sBefore))
							return true;
						string sAfter = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "after");
						if (StringsDiffer(sAfter, m_sAfter))
							return true;
						string sSep = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "sep");
						if (StringsDiffer(sSep, m_sSep))
							return true;
						string sWsLabel = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "ws");
						if (sWsLabel != null)
						{
							if (sWsLabel.StartsWith(StringUtils.WsParamLabel))
								sWsLabel = sWsLabel.Substring(StringUtils.WsParamLabel.Length);
						}
						if (StringsDiffer(sWsLabel, m_sWsLabel))
							return true;
						if (m_fStyleFromHiddenChild)
						{
							string sStyleName = XmlUtils.GetOptionalAttributeValue(m_xnHiddenChild, "style");
							m_fHiddenChildDirty = StringsDiffer(sStyleName, m_sStyleName);
							if (m_fHiddenChildDirty)
								return true;
						}
						else
						{
							string sStyleName = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "style");
							if (StringsDiffer(sStyleName, m_sStyleName))
								return true;
						}
						string sNumber = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "number");
						if (StringsDiffer(sNumber, m_sNumber))
							return true;
						string sNumStyle = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numstyle");
						if (StringsDiffer(sNumStyle, m_sNumStyle))
							return true;
						bool fNumSingle = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "numsingle", false);
						if (fNumSingle != m_fNumSingle)
							return true;
						string sNumFont = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numfont");
						if (StringsDiffer(sNumFont, m_sNumFont))
							return true;
						bool fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "singlegraminfofirst", false);
						if (fSingleGramInfoFirst != m_fSingleGramInfoFirst)
							return true;
						bool fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showasindentedpara", false);
						if (fShowComplexFormPara != m_fShowComplexFormPara)
							return true;
						bool fShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showLabels", false);
						if (fShowWsLabels != m_fShowWsLabels)
							return true;
						string sDuplicate = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "dup");
						if (StringsDiffer(sDuplicate, m_sDup))
							return true;
					}
				}
				else
				{
					return Level == 0 && OverallLayoutVisibilityChanged();
				}
				return false;
			}

			private bool OverallLayoutVisibilityChanged()
			{
				Debug.Assert(Level == 0);
				string sVisible = XmlUtils.GetAttributeValue(m_xnParentLayout, "visibility");
				bool fOldVisible = sVisible != "never";
				return this.Checked != fOldVisible;
			}

			private bool StringsDiffer(string s1, string s2)
			{
				return (s1 != s2 && !(String.IsNullOrEmpty(s1) && String.IsNullOrEmpty(s2)));
			}

			internal bool IsRequired
			{
				// LT-10472 says that nothing is really required.
				//get { return m_sVisibility != null && m_sVisibility.ToLowerInvariant() == "required"; }
				get { return false; }
			}

			internal XmlNode CallingLayout
			{
				get { return m_xnCallingLayout; }
				set { m_xnCallingLayout = value; ; }
			}

			internal XmlNode ParentLayout
			{
				get { return m_xnParentLayout; }
				set { m_xnParentLayout = value; }
			}

			internal XmlNode HiddenNode
			{
				get { return m_xnHiddenNode; }
				set { m_xnHiddenNode = value; }
			}

			internal XmlNode HiddenParent
			{
				get { return m_xnHiddenParentLayout; }
				set { m_xnHiddenParentLayout = value; }
			}

			internal XmlNode HiddenCallingLayout
			{
				get { return m_xnHiddenCallingLayout; }
				set { m_xnHiddenCallingLayout = value; }
			}

			internal XmlNode HiddenChildLayout
			{
				get { return m_xnHiddenChildLayout; }
				set { m_xnHiddenChildLayout = value; }
			}

			internal XmlNode HiddenChild
			{
				get { return m_xnHiddenChild; }
				set
				{
					m_xnHiddenChild = value;
					if (m_sClassName == "StText" && m_fAllowParaStyle && String.IsNullOrEmpty(m_sStyleName))
					{
						m_fStyleFromHiddenChild = true;
						m_sStyleName = XmlUtils.GetOptionalAttributeValue(value, "style");
					}
				}
			}

			internal bool HiddenChildDirty
			{
				get { return m_fHiddenChildDirty; }
			}

			internal bool GetModifiedLayouts(List<XmlNode> rgxn)
			{
				List<XmlNode> rgxnDirtyLayouts = new List<XmlNode>();
				for (int i = 0; i < Nodes.Count; ++i)
				{
					LayoutTreeNode ltn = (LayoutTreeNode)Nodes[i];
					if (ltn.GetModifiedLayouts(rgxn))
					{
						XmlNode xn = ltn.ParentLayout;
						if (xn != null && !rgxnDirtyLayouts.Contains(xn))
							rgxnDirtyLayouts.Add(xn);
						xn = ltn.HiddenChildLayout;
						if (xn != null && ltn.HiddenChildDirty && !rgxnDirtyLayouts.Contains(xn))
							rgxnDirtyLayouts.Add(xn);
						foreach (LayoutTreeNode ltnMerged in ltn.MergedNodes)
						{
							xn = ltnMerged.ParentLayout;
							if (xn != null && !rgxnDirtyLayouts.Contains(xn))
								rgxnDirtyLayouts.Add(xn);
						}
					}
				}
				if (Level == 0 && OverallLayoutVisibilityChanged())
				{
					if (!rgxnDirtyLayouts.Contains(m_xnParentLayout))
						rgxnDirtyLayouts.Add(m_xnParentLayout);
				}
				foreach (XmlNode xnDirtyLayout in rgxnDirtyLayouts)
				{
					// Create a new layout node with all its parts in order.  This is needed
					// to handle arbitrary reordering and possible addition or deletion of
					// duplicate nodes.  This is complicated by the presence (or rather absence)
					// of "hidden" nodes, and by "merged" nodes.
					XmlNode xnLayout = xnDirtyLayout.Clone();
					if (xnDirtyLayout == m_xnParentLayout && Level == 0 && OverallLayoutVisibilityChanged())
						UpdateAttribute(xnLayout, "visibility", this.Checked ? "always" : "never");
					XmlAttribute[] rgxa = new XmlAttribute[xnLayout.Attributes.Count];
					xnLayout.Attributes.CopyTo(rgxa, 0);
					List<XmlNode> rgxnGen = new List<XmlNode>();
					List<int> rgixn = new List<int>();
					for (int i = 0; i < xnLayout.ChildNodes.Count; ++i)
					{
						XmlNode xn = xnLayout.ChildNodes[i];
						if (xn.Name != "part")
						{
							rgxnGen.Add(xn);
							rgixn.Add(i);
						}
					}
					xnLayout.RemoveAll();
					for (int i = 0; i < rgxa.Length; ++i)
						xnLayout.Attributes.SetNamedItem(rgxa[i]);
					for (int i = 0; i < Nodes.Count; ++i)
					{
						LayoutTreeNode ltn = (LayoutTreeNode)Nodes[i];
						if (ltn.ParentLayout == xnDirtyLayout)
						{
							xnLayout.AppendChild(ltn.Configuration.CloneNode(true));
						}
						else if (ltn.HiddenParent == xnDirtyLayout)
						{
							xnLayout.AppendChild(ltn.HiddenNode.CloneNode(true));
						}
						else if (ltn.HiddenChildLayout == xnDirtyLayout)
						{
							xnLayout.AppendChild(ltn.HiddenChild.CloneNode(true));
						}
						else
						{
							for (int itn = 0; itn < ltn.MergedNodes.Count; itn++)
							{
								LayoutTreeNode ltnMerged = ltn.MergedNodes[itn];
								if (ltnMerged.ParentLayout == xnDirtyLayout)
								{
									xnLayout.AppendChild(ltnMerged.Configuration.CloneNode(true));
									break;
								}
							}
						}
					}
					XmlNode xnRef;
					for (int i = 0; i < rgxnGen.Count; ++i)
					{
						if (rgixn[i] <= xnLayout.ChildNodes.Count / 2)
						{
							xnRef = xnLayout.ChildNodes[rgixn[i]];
							xnLayout.InsertBefore(rgxnGen[i], xnRef);
						}
						else
						{
							if (rgixn[i] < xnLayout.ChildNodes.Count)
								xnRef = xnLayout.ChildNodes[rgixn[i]];
							else
								xnRef = xnLayout.LastChild;
							xnLayout.InsertAfter(rgxnGen[i], xnRef);
						}
					}
					if (!rgxn.Contains(xnLayout))
						rgxn.Add(xnLayout);
				}
				if (Level == 0)
					return UpdateLayoutVisibilityIfChanged();
				bool fDirty = Level > 0 && IsNodeDirty();
				if (fDirty)
					StoreUpdatedValuesInConfiguration();
				return fDirty;
			}

			private bool UpdateLayoutVisibilityIfChanged()
			{
				if (Level == 0 && OverallLayoutVisibilityChanged())
				{
					UpdateAttribute(m_xnParentLayout, "visibility", this.Checked ? "always" : "never");
					return true;
				}
				else
				{
					return false;
				}
			}

			private void UpdateAttributeIfDirty(XmlNode xn, string sName, string sValue)
			{
				string sOldValue = XmlUtils.GetOptionalAttributeValue(xn, sName);
				if (StringsDiffer(sValue, sOldValue))
					UpdateAttribute(xn, sName, sValue);
			}

			private void UpdateAttribute(XmlNode xn, string sName, string sValue)
			{
				if (sValue == null)
				{
					// probably can't happen...
					xn.Attributes.RemoveNamedItem(sName);
				}
				else
				{
					XmlAttribute xa = xn.OwnerDocument.CreateAttribute(sName);
					xa.Value = sValue;
					xn.Attributes.SetNamedItem(xa);
				}
			}

			private void StoreUpdatedValuesInConfiguration()
			{
				if (m_xnConfig.Name != "part")
					return;
				string sDuplicate = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "dup");
				if (StringsDiffer(sDuplicate, m_sDup))
				{
					// Copy Part Node
					m_xnConfig = m_xnConfig.CloneNode(true);
					UpdateAttribute(m_xnConfig, "label", m_sLabel);
					UpdateAttribute(m_xnConfig, "dup", m_sDup);
					if (m_xnHiddenNode != null)
					{
						string sNewName = String.Format("{0}_{1}",
							XmlUtils.GetManditoryAttributeValue(m_xnParentLayout, "name"), m_sDup);
						m_xnHiddenNode = m_xnHiddenNode.CloneNode(true);
						UpdateAttribute(m_xnHiddenNode, "dup", m_sDup);
						UpdateAttribute(m_xnHiddenNode, "param", sNewName);
						m_xnParentLayout = m_xnParentLayout.CloneNode(true);
						UpdateAttribute(m_xnParentLayout, "name", sNewName);
					}
					foreach (LayoutTreeNode ltn in m_rgltnMerged)
					{
						ltn.m_xnConfig = ltn.m_xnConfig.CloneNode(true);
						UpdateAttribute(ltn.m_xnConfig, "label", m_sLabel);
						UpdateAttribute(ltn.m_xnConfig, "dup", m_sDup);
					}
				}
				CopyPartAttributes(m_xnConfig);
				foreach (LayoutTreeNode ltn in m_rgltnMerged)
				{
					CopyPartAttributes(ltn.m_xnConfig);
				}
			}

			/// <summary>
			/// xn is a part ref element containing the currently saved version of the part that
			/// this LayoutTreeNode represents. Copy any changed information from yourself to xn.
			/// </summary>
			/// <param name="xn"></param>
			/// <returns></returns>
			private XmlNode CopyPartAttributes(XmlNode xn)
			{
				string sVisibility = XmlUtils.GetOptionalAttributeValue(xn, "visibility");
				bool fContentVisible = sVisibility != "never";
				m_fContentVisible = this.Checked;	// in case (un)checked in treeview, but node never selected.
				if (fContentVisible != m_fContentVisible)
					UpdateAttribute(xn, "visibility", m_fContentVisible ? "ifdata" : "never");
				UpdateAttributeIfDirty(xn, "beforeStyle", m_sBeforeStyleName);
				UpdateAttributeIfDirty(xn, "before", m_sBefore);
				UpdateAttributeIfDirty(xn, "after", m_sAfter);
				UpdateAttributeIfDirty(xn, "sep", m_sSep);
				string sWsLabel = XmlUtils.GetOptionalAttributeValue(xn, "ws");
				if (sWsLabel != null)
				{
					if (sWsLabel.StartsWith(StringUtils.WsParamLabel))
						sWsLabel = sWsLabel.Substring(StringUtils.WsParamLabel.Length);
				}
				if (StringsDiffer(sWsLabel, m_sWsLabel))
					UpdateAttribute(xn, "ws", m_sWsLabel);
				if (m_fStyleFromHiddenChild)
				{
					UpdateAttributeIfDirty(m_xnHiddenChild, "style", m_sStyleName);
				}
				else
				{
					UpdateAttributeIfDirty(xn, "style", m_sStyleName);
				}
				UpdateAttributeIfDirty(xn, "number", m_sNumber);
				UpdateAttributeIfDirty(xn, "numstyle", m_sNumStyle);
				bool fNumSingle = XmlUtils.GetOptionalBooleanAttributeValue(xn, "numsingle", false);
				if (fNumSingle != m_fNumSingle)
					UpdateAttribute(xn, "numsingle", m_fNumSingle.ToString());
				UpdateAttributeIfDirty(xn, "numfont", m_sNumFont);
				bool fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "singlegraminfofirst", false);
				if (fSingleGramInfoFirst != m_fSingleGramInfoFirst)
				{
					UpdateAttribute(xn, "singlegraminfofirst", m_fSingleGramInfoFirst.ToString());
					LayoutTreeNode ltnOther = null;
					if (this.ShowSenseConfig)
					{
						foreach (TreeNode n in this.Nodes)
						{
							LayoutTreeNode ltn = n as LayoutTreeNode;
							if (ltn != null && ltn.ShowGramInfoConfig)
							{
								ltnOther = ltn;
								break;
							}
						}
					}
					else if (this.ShowGramInfoConfig)
					{
						LayoutTreeNode ltn = this.Parent as LayoutTreeNode;
						if (ltn != null && ltn.ShowSenseConfig)
							ltnOther = ltn;
					}
					if (ltnOther != null)
						UpdateAttribute(ltnOther.m_xnConfig, "singlegraminfofirst", m_fSingleGramInfoFirst.ToString());
				}
				bool fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showasindentedpara", false);
				if (fShowComplexFormPara != m_fShowComplexFormPara)
					UpdateAttribute(xn, "showasindentedpara", m_fShowComplexFormPara.ToString());
				bool fShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(xn, "showLabels", false);
				if (fShowWsLabels != m_fShowWsLabels)
					UpdateAttribute(xn, "showLabels", m_fShowWsLabels.ToString());
				return xn;
			}

			/// <summary>
			/// If this node shows sense config information, make sure any changes are consistent with
			/// any child that also shows sense config. In particular if the numbering scheme (1.2.3 vs 1 b iii)
			/// has changed, change in all places.
			/// So far, we never have more than one child that has this property, so we don't try to handle
			/// inconsistent children.
			/// </summary>
			internal void MakeSenseNumberSchemeConsistent()
			{
				foreach (TreeNode tn in Nodes)
				{
					LayoutTreeNode ltn = tn as LayoutTreeNode;
					if (ltn != null)
					{
						ltn.MakeSenseNumberSchemeConsistent(); // recurse first, in case it has children needing to be fixed.
						if (!ShowSenseConfig || !ltn.ShowSenseConfig)
							continue;

						string sBefore, sMark, sAfter;
						SplitNumberFormat(out sBefore, out sMark, out sAfter);

						string sBeforeChild, sMarkChild, sAfterChild;
						ltn.SplitNumberFormat(out sBeforeChild, out sMarkChild, out sAfterChild);

						if (sMark == sMarkChild)
							continue; // nothing to reconcile.

						string sOld = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "number");
						string sBeforeOld, sMarkOld, sAfterOld;
						ltn.SplitNumberFormat(sOld, out sBeforeOld, out sMarkOld, out sAfterOld);

						string sChildOld = XmlUtils.GetOptionalAttributeValue(ltn.m_xnConfig, "number");
						string sBeforeChildOld, sMarkChildOld, sAfterChildOld;
						ltn.SplitNumberFormat(sChildOld, out sBeforeChildOld, out sMarkChildOld, out sAfterChildOld);

						if (sMark != sMarkOld)
						{
							// parent changed; make child consistent
							ltn.Number = sBeforeChild + sMark + sAfterChild;
						}
						else if (sMarkChild != sMarkChildOld)
						{
							// child changed.
							Number = sBefore + sMarkChild + sAfter;
						}
					}
				}

			}
			internal void SplitNumberFormat(out string sBefore, out string sMark, out string sAfter)
			{
				SplitNumberFormat(Number, out sBefore, out sMark, out sAfter);
			}

			internal void SplitNumberFormat(string sNumber, out string sBefore, out string sMark, out string sAfter)
			{
				sBefore = "";
				sMark = "%O";
				sAfter = ") ";
				if (!String.IsNullOrEmpty(sNumber))
				{
					int ich = sNumber.IndexOf('%');
					if (ich < 0)
						ich = sNumber.Length;
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

			public List<LayoutTreeNode> MergedNodes
			{
				get { return m_rgltnMerged; }
			}
		}
		#endregion // LayoutTreeNode class

		#region LayoutTypeComboItem class

		public class LayoutTypeComboItem
		{
			private string m_sLabel;
			private string m_sLayout;
			private List<LayoutTreeNode> m_rgltn;

			public LayoutTypeComboItem(XmlNode xnLayoutType, List<LayoutTreeNode> rgltn)
			{
				m_sLabel = XmlUtils.GetManditoryAttributeValue(xnLayoutType, "label");
				m_sLayout = XmlUtils.GetManditoryAttributeValue(xnLayoutType, "layout");
				m_rgltn = rgltn;
			}

			public string Label
			{
				get { return m_sLabel; }
			}

			public string LayoutName
			{
				get { return m_sLayout; }
			}

			public List<LayoutTreeNode> TreeNodes
			{
				get { return m_rgltn; }
			}

			public override string ToString()
			{
				return m_sLabel;
			}
		}
		#endregion

		#region StyleComboItem class

		public class StyleComboItem : IComparable
		{
			private BaseStyleInfo m_style;

			public StyleComboItem(BaseStyleInfo sty)
			{
				m_style = sty;
			}

			public override string ToString()
			{
				if (m_style == null)
					return xWorksStrings.ksNone;
				else
					return m_style.Name;
			}

			public BaseStyleInfo Style
			{
				get { return m_style; }
			}

			#region IComparable Members

			public int CompareTo(object obj)
			{
				StyleComboItem that = obj as StyleComboItem;
				if (this == that)
					return 0;
				else if (that == null)
					return 1;
				else if (this.Style == that.Style)
					return 0;
				else if (that.Style == null)
					return 1;
				else if (this.Style == null)
					return -1;
				else
					return this.Style.Name.CompareTo(that.Style.Name);
			}

			#endregion
		}

		#endregion // StyleComboItem class

		#region NumberStyleComboItem class

		public class NumberStyleComboItem
		{
			private string m_sLabel;
			private string m_sFormat;

			public NumberStyleComboItem(string sLabel, string sFormat)
			{
				m_sLabel = sLabel;
				m_sFormat = sFormat;
			}

			public override string ToString()
			{
				return m_sLabel;
			}

			public string FormatString
			{
				get { return m_sFormat; }
			}

			public string Label
			{
				get { return m_sLabel; }
			}
		}

		#endregion // NumberStyleComboItem class

		private void XmlDocConfigureDlg_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (DialogResult != DialogResult.OK)
			{
				if (m_fDeleteCustomFiles)
				{
					Inventory.RemoveInventory("layouts", null);
					Inventory.RemoveInventory("parts", null);
				}
			}
		}

		private void m_btnSetAll_Click(object sender, EventArgs e)
		{
			if (m_tvParts == null || m_tvParts.Nodes == null || m_tvParts.Nodes.Count == 0)
				return;
			foreach (TreeNode node in m_tvParts.Nodes)
				CheckNodeAndChildren(node);
		}

		private static void CheckNodeAndChildren(TreeNode node)
		{
			node.Checked = true;
			foreach (TreeNode tn in node.Nodes)
				CheckNodeAndChildren(tn);
		}

		private void m_cbNumberStyle_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_cbNumberStyle.SelectedIndex == 0)
			{
				m_chkNumberSingleSense.Enabled = false;
				m_tbAfterNumber.Enabled = false;
				m_tbBeforeNumber.Enabled = false;
				m_chkSenseBoldNumber.Enabled = false;
				m_chkSenseItalicNumber.Enabled = false;
				m_cbNumberFont.Enabled = false;
			}
			else
			{
				m_chkNumberSingleSense.Enabled = true;
				m_tbAfterNumber.Enabled = true;
				m_tbBeforeNumber.Enabled = true;
				m_chkSenseBoldNumber.Enabled = true;
				m_chkSenseItalicNumber.Enabled = true;
				m_cbNumberFont.Enabled = true;
			}
		}
	}
}
