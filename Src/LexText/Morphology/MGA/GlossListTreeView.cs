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
// File: GLossListTreeView.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	/// <summary>
	/// Summary description for GlossListTreeView.
	/// </summary>
	public class GlossListTreeView : TreeView, IFWDisposable
	{
		public enum ImageKind
		{
			closedFolder = 0,
			openFolder,
			complex,
			featureStructureType,
			userChoice,
			checkBox,
			checkedBox,
			radio,
			radioSelected
		}
		protected bool m_fTerminalsUseCheckBoxes;
		protected string m_sTopOfList = "eticGlossList";
		protected string m_sAfterSeparator;
		protected string m_sComplexNameSeparator;
		protected bool m_fComplexNameFirst;
		protected string m_sWritingSystemAbbrev;
		protected string m_sTermNodeXPath = "term[@ws='en']";
		protected string m_sAbbrevNodeXPath = "abbrev[@ws='en']";
		protected TreeNode m_lastSelectedTreeNode = null;
		protected FdoCache m_cache;

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

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
		public FdoCache Cache
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
				XmlDocument dom = CreateDomAndLoadXMLFile(sXmlFile);

				// SECTION 2. Initialize the GlossListTreeView control.
				XmlNode treeTop = InitializeGlossListTreeViewControl(dom, sDefaultAnalysisWritingSystem);

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
			XmlNode treeTop = dom.SelectSingleNode(m_sTopOfList);
			// set CheckBoxes value
			string sAttr = XmlUtils.GetAttributeValue(treeTop, "checkBoxes");
			CheckBoxes = XmlUtils.GetBooleanAttributeValue(sAttr);
			// set complex name separator value
			m_sAfterSeparator = XmlUtils.GetAttributeValue(treeTop, "afterSeparator");
			m_sComplexNameSeparator = XmlUtils.GetAttributeValue(treeTop, "complexNameSeparator");
			// set complex name first value
			sAttr = XmlUtils.GetAttributeValue(treeTop, "complexNameFirst");
			m_fComplexNameFirst = XmlUtils.GetBooleanAttributeValue(sAttr);
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
			XmlNode xn = dom.SelectSingleNode("//item/term[@ws='" + m_sWritingSystemAbbrev + "']");
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
			XmlNodeList nodes = dom.SelectNodes(m_sTopOfList + "/item");
			foreach (XmlNode node in nodes)
			{
				AddNode(node, null, dom);
			}
			string sExpandAll = XmlUtils.GetAttributeValue(treeTop, "expandAll");
			if (XmlUtils.GetBooleanAttributeValue(sExpandAll))
				ExpandAll();
			else
				CollapseAll();
		}

		#endregion
		#region private methods
		private void CommonInit()
		{
			AfterCollapse += new TreeViewEventHandler(OnAfterCollapse);
			AfterExpand += new TreeViewEventHandler(OnAfterExpand);
			MouseUp += new MouseEventHandler(OnMouseUp);
			KeyUp += new KeyEventHandler(OnKeyUp);

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
		void OnMouseUp(object obj, MouseEventArgs mea)
		{
			if (mea.Button == MouseButtons.Left)
			{
				TreeView tv = (TreeView) obj;
				TreeNode tn = tv.GetNodeAt(mea.X, mea.Y);
				if (tn != null)
				{
					Rectangle rec = tn.Bounds;
					rec.X += -18;       // include the image bitmap (16 pixels plus 2 pixels between the image and the text)
					rec.Width += 18;
					if (rec.Contains(mea.X, mea.Y))
					{
						HandleCheckBoxNodes(tv, tn);
//						int i = tn.ImageIndex;
					}
				}
			}
		}
		void OnKeyUp(object obj, KeyEventArgs kea)
		{
			TreeView tv = (TreeView) obj;
			TreeNode tn = tv.SelectedNode;
			if (kea.KeyCode == Keys.Space)
			{
				HandleCheckBoxNodes(tv, tn);
			}
		}
		bool IsTerminalNode(TreeNode tn)
		{
			return (tn.Nodes.Count == 0);
		}
		void UndoLastSelectedNode()
		{
			if (m_lastSelectedTreeNode != null)
			{
				if (IsTerminalNode(m_lastSelectedTreeNode))
				{
					m_lastSelectedTreeNode.Checked = false;
					if (m_fTerminalsUseCheckBoxes)
						m_lastSelectedTreeNode.ImageIndex = m_lastSelectedTreeNode.SelectedImageIndex = (int)ImageKind.checkBox;
					else
						m_lastSelectedTreeNode.ImageIndex = m_lastSelectedTreeNode.SelectedImageIndex = (int)ImageKind.radio;
				}
			}
		}
		protected virtual void HandleCheckBoxNodes(TreeView tv, TreeNode tn)
		{
			UndoLastSelectedNode();
			if (m_fTerminalsUseCheckBoxes)
			{
				if (IsTerminalNode(tn))
				{
					MasterInflectionFeature mif = tn.Tag as MasterInflectionFeature;
					if (tn.Checked)
					{
						if (mif == null || !mif.InDatabase)
						{
							tn.Checked = false;
							tn.ImageIndex = tn.SelectedImageIndex = (int) ImageKind.checkBox;
						}
					}
					else
					{
						tn.Checked = true;
						tn.ImageIndex = tn.SelectedImageIndex = (int)ImageKind.checkedBox;
						if (mif != null)
						{
							string sId = XmlUtils.GetOptionalAttributeValue(mif.Node, "id");
							if (m_cache.LanguageProject.MsFeatureSystemOA.GetSymbolicValue(sId) != null)
							{
								// we want to set all other sisters that are in the database
								TreeNode sibling = tn.Parent.FirstNode;
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
												sibling.ImageIndex =
													sibling.SelectedImageIndex = (int) ImageKind.checkedBox;
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
			}
			else
			{
				if (IsTerminalNode(tn))
				{
					tn.Checked = true;
					tn.ImageIndex = tn.SelectedImageIndex = (int)ImageKind.radioSelected;
					if (tn.Parent != null)
					{
						TreeNode sibling = tn.Parent.FirstNode;
						while (sibling != null)
						{
							if (IsTerminalNode(sibling) && sibling != tn)
							{
								sibling.Checked = false;
								sibling.ImageIndex = sibling.SelectedImageIndex = (int)ImageKind.radio;
							}
							sibling = sibling.NextNode;
						}
					}
					tv.Invalidate();
				}
				m_lastSelectedTreeNode = tn;
			}
		}

		void OnAfterCollapse(object obj, TreeViewEventArgs tvea)
		{
			TreeNode tn = tvea.Node;
			if (tn.ImageIndex == (int)ImageKind.openFolder)
				tn.ImageIndex = tn.SelectedImageIndex = (int)ImageKind.closedFolder;
		}
		void OnAfterExpand(object obj, TreeViewEventArgs tvea)
		{
			TreeNode tn = tvea.Node;
			if (tn.ImageIndex == (int)ImageKind.closedFolder)
				tn.ImageIndex = tn.SelectedImageIndex = (int)ImageKind.openFolder;
		}
		private void AddNode(XmlNode currentNode, TreeNode parentNode, XmlDocument dom )
		{
			string sStatus = XmlUtils.GetAttributeValue(currentNode, "status");
			if (sStatus == "hidden")
				return;
			if (sStatus == "proxy")
			{
				FleshOutProxy(currentNode, dom);
			}
			XmlNodeList nodes = currentNode.SelectNodes("item");
			TreeNode newNode = null;
			string sType = XmlUtils.GetAttributeValue(currentNode, "type");
			if (sType == "fsType")
			{ // skip an fsType to get to its contents
				newNode = parentNode;
			}
			else
			{
				string sTerm = GetTerm(currentNode);
				string sAbbrev = GetAbbrev(currentNode);
				StringBuilder sbNode = new StringBuilder();
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
					Nodes.Add(newNode);
				else
					parentNode.Nodes.Add(newNode);
			}
			foreach (XmlNode node in nodes)
			{
				AddNode(node, newNode, dom);
			}
		}

		protected virtual TreeNode CreateNewNode(XmlNode currentNode, string sType, StringBuilder sbNode, string sTerm)
		{
			TreeNode newNode;
			GlossListTreeView.ImageKind ik = GetImageKind(sType);
			newNode = new TreeNode(TsStringUtils.NormalizeToNFC(sbNode.ToString()), (int) ik, (int) ik);
			MasterInflectionFeature mif = new MasterInflectionFeature(currentNode, ik, sTerm);
			newNode.Tag = (MasterInflectionFeature) mif;
			return newNode;
		}

		private void FleshOutProxy(XmlNode currentNode, XmlDocument dom)
		{
			string sTarget = XmlUtils.GetAttributeValue(currentNode, "target");
			XmlNode xn = dom.SelectSingleNode("//item[@id='" + sTarget + "']");
			if (xn != null)
			{
				XmlAttribute idAttr = dom.CreateAttribute("id");
				idAttr.Value = sTarget;
				idAttr.Value = sTarget;
#if !__MonoCS__
				currentNode.Attributes.Append(idAttr);
#else
				// TODO-Linux: work around for Mono bug 508296 https://bugzilla.novell.com/show_bug.cgi?id=508296
				try
				{
					currentNode.Attributes.Append(idAttr);
				}
				catch { }
#endif
				XmlAttribute typeAttr = (XmlAttribute)xn.SelectSingleNode("@type");
				if (typeAttr != null)
					currentNode.Attributes.Append((XmlAttribute)typeAttr.Clone());
				// replace any abbrev, term or def items from target and add any citations in target
				string[] asNodes =  new string[3] {"abbrev", "term", "def"};
				for (int i = 0; i<3; i++)
				{
					XmlNode newTempNode = xn.SelectSingleNode(asNodes[i]);
					XmlNode oldNode = currentNode.SelectSingleNode(asNodes[i]);
					if (newTempNode != null)
					{
						if (oldNode != null)
							currentNode.ReplaceChild(newTempNode.Clone(), oldNode);
						else
							currentNode.AppendChild(newTempNode.Clone());
					}
				}
				XmlNodeList citationNodes = xn.SelectNodes("citation");
				foreach (XmlNode citation in citationNodes)
				{
					currentNode.AppendChild(citation.Clone());
				}
			}
		}

		private string GetTypeOfCrossReference(XmlNode currentNode, XmlDocument dom)
		{
			string sType;
			string sTarget = XmlUtils.GetAttributeValue(currentNode, "target");
			XmlNode xn = dom.SelectSingleNode("//item[@id='" + sTarget + "']");
			sType = XmlUtils.GetAttributeValue(xn, "type");
			return sType;
		}

		private string GetAbbrev(XmlNode currentNode)
		{
			XmlNode xn;
			xn = currentNode.SelectSingleNode(m_sAbbrevNodeXPath);
			string sAbbrev = "";
			if (xn != null)
				sAbbrev = xn.InnerText;
			return sAbbrev;
		}

		private string GetTerm(XmlNode currentNode)
		{
			XmlNode xn = currentNode.SelectSingleNode(m_sTermNodeXPath);
			string sTerm = "";
			if (xn != null)
				sTerm = xn.InnerText;
			return sTerm;
		}

		private ImageKind GetImageKind(string sType)
		{
			ImageKind ik;
			if (sType == "value")
			{
				if (TerminalsUseCheckBoxes)
					ik = GlossListTreeView.ImageKind.checkBox;
				else
					ik = GlossListTreeView.ImageKind.radio;
			}
			else
			{
				if (sType == "feature")
					ik = GlossListTreeView.ImageKind.userChoice;
				else if (sType == "fsType")
					ik = GlossListTreeView.ImageKind.featureStructureType;
				else if (sType == "complex")
					ik = GlossListTreeView.ImageKind.complex;
				else
					ik = GlossListTreeView.ImageKind.closedFolder;
			}
			return ik;
		}

		#endregion
	}
}
