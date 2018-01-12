// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Core.Text;
using SIL.Xml;

namespace LanguageExplorer.MGA
{
	/// <summary>
	/// Summary description for GlossListTreeView.
	/// </summary>
	internal class GlossListTreeView : TreeView
	{
		protected bool m_fTerminalsUseCheckBoxes;
		protected string m_sTopOfList = "eticGlossList";
		protected string m_sAfterSeparator;
		protected string m_sComplexNameSeparator;
		protected bool m_fComplexNameFirst;
		protected string m_sWritingSystemAbbrev;
		protected string m_sTermNodeXPath = "term[@ws='en']";
		protected string m_sAbbrevNodeXPath = "abbrev[@ws='en']";
		protected TreeNode m_lastSelectedTreeNode = null;
		protected LcmCache m_cache;

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				ImageList?.Dispose();
			}
			ImageList = null;
			m_cache = null;

			base.Dispose(disposing);
		}

		#region Properties
		/// <summary>
		/// Gets default after separator character for glossing.
		/// </summary>
		public string AfterSeparator
		{
			get
			{
				CheckDisposed();

				return m_sAfterSeparator;
			}
		}
		/// <summary>
		/// Sets FDO cache
		/// </summary>
		public LcmCache Cache
		{
			set { m_cache = value;}
		}
		/// <summary>
		/// Gets flag whether the name of the complex item comes first or not.
		/// </summary>
		public bool ComplexNameFirst
		{
			get
			{
				CheckDisposed();

				return m_fComplexNameFirst;
			}
		}
		/// <summary>
		/// Gets default separator character to occur after a complex name in glossing.
		/// </summary>
		public string ComplexNameSeparator
		{
			get
			{
				CheckDisposed();

				return m_sComplexNameSeparator;
			}
		}
		/// <summary>
		/// Gets/sets tree view to show checks boxes (true) or radio buttons (false) for terminal nodes.
		/// </summary>
		public bool TerminalsUseCheckBoxes
		{
			get
			{
				CheckDisposed();

				return m_fTerminalsUseCheckBoxes;
			}
			set
			{
				CheckDisposed();

				m_fTerminalsUseCheckBoxes = value;
			}
		}
		/// <summary>
		/// Gets default writing system abbreviation.
		/// </summary>
		public string WritingSystemAbbrev
		{
			get
			{
				CheckDisposed();

				return m_sWritingSystemAbbrev;
			}
		}
		#endregion
		#region construction
		public GlossListTreeView()
		{
			CommonInit();
		}
		public GlossListTreeView(bool bUseCheckBoxesInTerminals)
		{
			CommonInit();
			m_fTerminalsUseCheckBoxes = bUseCheckBoxesInTerminals;
		}
		#endregion
		#region public methods
		public void LoadGlossListTreeFromXml(string sXmlFile, string sDefaultAnalysisWritingSystem)
		{
			CheckDisposed();

			try
			{
				// SECTION 1. Create a DOM Document and load the XML data into it.
				var dom = CreateDomAndLoadXMLFile(sXmlFile);

				// SECTION 2. Initialize the GlossListTreeView control.
				var treeTop = InitializeGlossListTreeViewControl(dom, sDefaultAnalysisWritingSystem);

				// SECTION 3. Populate the TreeView with the DOM nodes.
				PopulateTreeView(dom, treeTop);
			}
			catch(XmlException xex)
			{
				MessageBox.Show(xex.Message);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private XmlDocument CreateDomAndLoadXMLFile(string sXmlFile)
		{
			XmlDocument dom = new XmlDocument();
			dom.Load(sXmlFile);
			return dom;
		}
		private XmlNode InitializeGlossListTreeViewControl(XmlDocument dom, string sDefaultAnalysisWritingSystem)
		{
			// make sure we're starting fresh
			Nodes.Clear();
			// get top node
			var treeTop = dom.SelectSingleNode(m_sTopOfList);
			// set CheckBoxes value
			//string sAttr = XmlUtils.GetAttributeValue(treeTop, "checkBoxes");
			CheckBoxes = XmlUtils.GetBooleanAttributeValue(treeTop, "checkBoxes");
			// set complex name separator value
			m_sAfterSeparator = XmlUtils.GetOptionalAttributeValue(treeTop, "afterSeparator");
			m_sComplexNameSeparator = XmlUtils.GetOptionalAttributeValue(treeTop, "complexNameSeparator");
			// set complex name first value
			m_fComplexNameFirst = XmlUtils.GetBooleanAttributeValue(treeTop, "complexNameFirst");
			// set writing system abbreviation value
			SetWritingSystemAbbrev(sDefaultAnalysisWritingSystem, dom);
			return treeTop;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the writing system abbrev.
		/// </summary>
		/// <param name="sDefaultAnalysisWritingSystem">The s default analysis writing system.</param>
		/// <param name="dom">The DOM.</param>
		/// ------------------------------------------------------------------------------------
		private void SetWritingSystemAbbrev(string sDefaultAnalysisWritingSystem, XmlDocument dom)
		{
			// assume the default is OK
			m_sWritingSystemAbbrev = sDefaultAnalysisWritingSystem;
			var xn = dom.SelectSingleNode("//item/term[@ws='" + m_sWritingSystemAbbrev + "']");
			if (xn == null)
			{	// default not found in the file; use English (and hope for the best)
				m_sWritingSystemAbbrev = "en";
				xn = dom.SelectSingleNode("//item/term[@ws='" + m_sWritingSystemAbbrev + "']");
			}
			if (xn == null)
			{
				// The default analysis WS and english WS failed to be found, therefore,
				// try the cache's fallback locale. REVIEW: Should we check this one before
				// checking english?
				m_sWritingSystemAbbrev = WritingSystemServices.FallbackUserWsId;
			}

			m_sTermNodeXPath = "term[@ws='" + m_sWritingSystemAbbrev + "']";
			m_sAbbrevNodeXPath = "abbrev[@ws='" + m_sWritingSystemAbbrev + "']";
		}

		private void PopulateTreeView(XmlDocument dom, XmlNode treeTop)
		{
			var nodes = dom.SelectNodes(m_sTopOfList + "/item");
			foreach (XmlNode node in nodes)
			{
				AddNode(node, null, dom);
			}
			var sExpandAll = XmlUtils.GetOptionalAttributeValue(treeTop, "expandAll");
			if (XmlUtils.GetBooleanAttributeValue(sExpandAll))
			{
				ExpandAll();
			}
			else
			{
				CollapseAll();
			}
		}

		#endregion
		#region private methods
		private void CommonInit()
		{
			AfterCollapse += OnAfterCollapse;
			AfterExpand += OnAfterExpand;
			MouseUp += OnMouseUp;
			KeyUp += OnKeyUp;

#if Orig
			Sorted = true;
#endif

			// Get images for tree.
			ImageList = new ImageList();
			ImageList.Images.Add(new Bitmap(GetType(), "CLSDFOLD.BMP"));      // 0
			ImageList.Images.Add(new Bitmap(GetType(), "OPENFOLD.BMP"));      // 1
			ImageList.Images.Add(new Bitmap(GetType(), "Complex.bmp"));       // 2
			ImageList.Images.Add(new Bitmap(GetType(), "FSType.bmp"));        // 3
			ImageList.Images.Add(new Bitmap(GetType(), "UserChoice.bmp"));    // 4
			ImageList.Images.Add(new Bitmap(GetType(), "CheckBox.bmp"));      // 5
			ImageList.Images.Add(new Bitmap(GetType(), "CheckedBox.bmp"));    // 6
			ImageList.Images.Add(new Bitmap(GetType(), "Radio.bmp"));         // 7
			ImageList.Images.Add(new Bitmap(GetType(), "RadioSelected.bmp")); // 8
		}

		private void OnMouseUp(object obj, MouseEventArgs mea)
		{
			if (mea.Button != MouseButtons.Left)
			{
				return;
			}
			var tv = (TreeView) obj;
			var tn = tv.GetNodeAt(mea.X, mea.Y);
			if (tn == null)
			{
				return;
			}
			var rec = tn.Bounds;
			rec.X += -18;       // include the image bitmap (16 pixels plus 2 pixels between the image and the text)
			rec.Width += 18;
			if (rec.Contains(mea.X, mea.Y))
			{
				HandleCheckBoxNodes(tv, tn);
			}
		}

		private void OnKeyUp(object obj, KeyEventArgs kea)
		{
			var tv = (TreeView)obj;
			var tn = tv.SelectedNode;
			if (kea.KeyCode == Keys.Space)
			{
				HandleCheckBoxNodes(tv, tn);
			}
		}

		private static bool IsTerminalNode(TreeNode tn)
		{
			return (tn.Nodes.Count == 0);
		}

		private void UndoLastSelectedNode()
		{
			if (m_lastSelectedTreeNode == null || !IsTerminalNode(m_lastSelectedTreeNode))
			{
				return;
			}
			m_lastSelectedTreeNode.Checked = false;
			if (m_fTerminalsUseCheckBoxes)
			{
				m_lastSelectedTreeNode.ImageIndex = m_lastSelectedTreeNode.SelectedImageIndex = (int) MGAImageKind.checkBox;
			}
			else
			{
				m_lastSelectedTreeNode.ImageIndex = m_lastSelectedTreeNode.SelectedImageIndex = (int)MGAImageKind.radio;
			}
		}
		protected virtual void HandleCheckBoxNodes(TreeView tv, TreeNode tn)
		{
			UndoLastSelectedNode();
			if (m_fTerminalsUseCheckBoxes)
			{
				if (!IsTerminalNode(tn))
				{
					return;
				}
				var mif = tn.Tag as MasterInflectionFeature;
				if (tn.Checked)
				{
					if (mif == null || !mif.InDatabase)
					{
						tn.Checked = false;
						tn.ImageIndex = tn.SelectedImageIndex = (int) MGAImageKind.checkBox;
					}
				}
				else
				{
					tn.Checked = true;
					tn.ImageIndex = tn.SelectedImageIndex = (int)MGAImageKind.checkedBox;
					if (mif != null)
					{
						var sId = XmlUtils.GetOptionalAttributeValue(mif.Node, "id");
						if (m_cache.LanguageProject.MsFeatureSystemOA.GetSymbolicValue(sId) != null)
						{
							// we want to set all other sisters that are in the database
							var sibling = tn.Parent.FirstNode;
							while (sibling != null)
							{
								if (IsTerminalNode(sibling) && sibling != tn)
								{
									mif = sibling.Tag as MasterInflectionFeature;
									if (mif != null)
									{
										sId = XmlUtils.GetOptionalAttributeValue(mif.Node, "id");
										if (m_cache.LanguageProject.MsFeatureSystemOA.GetSymbolicValue(sId) != null)
										{
											sibling.Checked = true;
											sibling.ImageIndex = sibling.SelectedImageIndex = (int) MGAImageKind.checkedBox;
										}
									}
								}
								sibling = sibling.NextNode;
							}
						}
					}
				}
				tv.Invalidate();
			}
			else
			{
				if (IsTerminalNode(tn))
				{
					tn.Checked = true;
					tn.ImageIndex = tn.SelectedImageIndex = (int)MGAImageKind.radioSelected;
					if (tn.Parent != null)
					{
						var sibling = tn.Parent.FirstNode;
						while (sibling != null)
						{
							if (IsTerminalNode(sibling) && sibling != tn)
							{
								sibling.Checked = false;
								sibling.ImageIndex = sibling.SelectedImageIndex = (int)MGAImageKind.radio;
							}
							sibling = sibling.NextNode;
						}
					}
					tv.Invalidate();
				}
				m_lastSelectedTreeNode = tn;
			}
		}

		private static void OnAfterCollapse(object obj, TreeViewEventArgs tvea)
		{
			var tn = tvea.Node;
			if (tn.ImageIndex == (int) MGAImageKind.openFolder)
			{
				tn.ImageIndex = tn.SelectedImageIndex = (int)MGAImageKind.closedFolder;
			}
		}

		private static void OnAfterExpand(object obj, TreeViewEventArgs tvea)
		{
			var tn = tvea.Node;
			if (tn.ImageIndex == (int) MGAImageKind.closedFolder)
			{
				tn.ImageIndex = tn.SelectedImageIndex = (int)MGAImageKind.openFolder;
			}
		}

		private void AddNode(XmlNode currentNode, TreeNode parentNode, XmlDocument dom)
		{
			var sStatus = XmlUtils.GetOptionalAttributeValue(currentNode, "status");
			if (sStatus == "hidden")
			{
				return;
			}
			if (sStatus == "proxy")
			{
				FleshOutProxy(currentNode, dom);
			}
			var nodes = currentNode.SelectNodes("item");
			TreeNode newNode = null;
			var sType = XmlUtils.GetOptionalAttributeValue(currentNode, "type");
			if (sType == "fsType")
			{
				// skip an fsType to get to its contents
				newNode = parentNode;
			}
			else
			{
				var sTerm = GetTerm(currentNode);
				var sAbbrev = GetAbbrev(currentNode);
				var sbNode = new StringBuilder();
				sbNode.Append(sTerm);
				if (sType != "group")
				{
					sbNode.Append(": ");
					sbNode.Append(sAbbrev);
				}
				if (sType == "xref")
				{
					sType = GetTypeOfCrossReference(currentNode, dom);
				}
				newNode = CreateNewNode(currentNode, sType, sbNode, sTerm);
				if (parentNode == null)
				{
					Nodes.Add(newNode);
				}
				else
				{
					parentNode.Nodes.Add(newNode);
				}
			}
			foreach (XmlNode node in nodes)
			{
				AddNode(node, newNode, dom);
			}
		}

		protected virtual TreeNode CreateNewNode(XmlNode currentNode, string sType, StringBuilder sbNode, string sTerm)
		{
			var imageKind = GetImageKind(sType);
			var newNode = new TreeNode(TsStringUtils.NormalizeToNFC(sbNode.ToString()), (int) imageKind, (int) imageKind);
			var mif = new MasterInflectionFeature(currentNode, imageKind, sTerm);
			newNode.Tag = (MasterInflectionFeature) mif;
			return newNode;
		}

		private void FleshOutProxy(XmlNode currentNode, XmlDocument dom)
		{
			var sTarget = XmlUtils.GetOptionalAttributeValue(currentNode, "target");
			var xn = dom.SelectSingleNode("//item[@id='" + sTarget + "']");
			if (xn == null)
			{
				return;
			}
			var idAttr = dom.CreateAttribute("id");
			idAttr.Value = sTarget;
			idAttr.Value = sTarget;
			currentNode.Attributes.Append(idAttr);
			var guidAttr = (XmlAttribute) xn.SelectSingleNode("@guid");
			Debug.Assert(guidAttr != null, "guid is a required attribute for items with ids");
			currentNode.Attributes.Append((XmlAttribute)guidAttr.Clone());
			var typeAttr = (XmlAttribute)xn.SelectSingleNode("@type");
			if (typeAttr != null)
			{
				currentNode.Attributes.Append((XmlAttribute)typeAttr.Clone());
			}
			// replace any abbrev, term or def items from target and add any citations in target
			var asNodes =  new string[3] {"abbrev", "term", "def"};
			for (var i = 0; i<3; i++)
			{
				var newTempNode = xn.SelectSingleNode(asNodes[i]);
				var oldNode = currentNode.SelectSingleNode(asNodes[i]);
				if (newTempNode == null)
				{
					continue;
				}
				if (oldNode != null)
				{
					currentNode.ReplaceChild(newTempNode.Clone(), oldNode);
				}
				else
				{
					currentNode.AppendChild(newTempNode.Clone());
				}
			}
			var citationNodes = xn.SelectNodes("citation");
			foreach (XmlNode citation in citationNodes)
			{
				currentNode.AppendChild(citation.Clone());
			}
		}

		private static string GetTypeOfCrossReference(XmlNode currentNode, XmlDocument dom)
		{
			var sTarget = XmlUtils.GetOptionalAttributeValue(currentNode, "target");
			var xn = dom.SelectSingleNode("//item[@id='" + sTarget + "']");
			var sType = XmlUtils.GetOptionalAttributeValue(xn, "type");
			return sType;
		}

		private string GetAbbrev(XmlNode currentNode)
		{
			var xn = currentNode.SelectSingleNode(m_sAbbrevNodeXPath);
			var sAbbrev = string.Empty;
			if (xn != null)
			{
				sAbbrev = xn.InnerText;
			}
			return sAbbrev;
		}

		private string GetTerm(XmlNode currentNode)
		{
			var xn = currentNode.SelectSingleNode(m_sTermNodeXPath);
			var sTerm = string.Empty;
			if (xn != null)
			{
				sTerm = xn.InnerText;
			}
			return sTerm;
		}

		private MGAImageKind GetImageKind(string sType)
		{
			MGAImageKind ik;
			switch (sType)
			{
				case "value":
					ik = TerminalsUseCheckBoxes ? MGAImageKind.checkBox : MGAImageKind.radio;
					break;
				case "feature":
					ik = MGAImageKind.userChoice;
					break;
				case "fsType":
					ik = MGAImageKind.featureStructureType;
					break;
				case "complex":
					ik = MGAImageKind.complex;
					break;
				default:
					ik = MGAImageKind.closedFolder;
					break;
			}
			return ik;
		}

		#endregion
	}
}
