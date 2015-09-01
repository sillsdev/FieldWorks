// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ReallySimpleListChooser.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary></summary>
	public class ReallySimpleListChooser : Form, IFWDisposable
	{
		/// <summary></summary>
		protected ObjectLabel m_chosenLabel;
		/// <summary></summary>
		protected Button btnOK;
		/// <summary></summary>
		protected Button btnCancel;
		/// <summary></summary>
		protected TreeView m_labelsTreeView;
		private bool m_fFlatList = false;
		private bool m_fSortLabels = true;
		private bool m_fSortLabelsSet = false;	// set true if explicitly assigned.
		private List<ICmObject> m_objs;
		private FlatListView m_flvLabels;
		private List<ObjectLabel> m_labels;
		/// <summary></summary>
		protected ToolTip toolTip1;
		private IContainer components;
		/// <summary></summary>
		protected IPersistenceProvider m_persistProvider;
		/// <summary></summary>
		protected ImageList m_imageList;
		/// <summary></summary>
		protected LinkLabel m_lblLink2;
		/// <summary></summary>
		protected PictureBox m_picboxLink2;
		/// <summary></summary>
		protected LinkLabel m_lblLink1;
		/// <summary></summary>
		protected PictureBox m_picboxLink1;
		/// <summary></summary>
		protected Label m_lblExplanation;
		/// <summary></summary>
		protected FdoCache m_cache;

		/// <summary>
		/// True to prevent choosing more than one item.
		/// </summary>
		public bool Atomic { get; set; }

		/// <summary></summary>
		protected bool m_fLinkExecuted = false;

		/// <summary></summary>
		protected NullObjectLabel m_nullLabel;
		/// <summary></summary>
		protected int m_hvoObject;
		/// <summary></summary>
		protected int m_flidObject;
		/// <summary></summary>
		protected IPropertyTable m_propertyTable;
		/// <summary></summary>
		protected IPublisher m_publisher;
		/// <summary></summary>
		protected string m_fieldName = null;
		private int m_cLinksShown = 0;
		private object m_obj1 = null;
		private object m_obj2 = null;
		private FwLinkArgs m_linkJump = null;
		private ChooserCommand m_linkCmd = null;
		private string m_sTextParam = null;
		/// <summary></summary>
		protected int m_hvoTextParam = 0;
		private Guid m_guidLink = Guid.Empty;
		private readonly HashSet<ICmObject> m_chosenObjs = null;
		private List<ICmObject> m_newChosenObjs = null;
		private bool m_fEnableCtrlCheck; // true to allow ctrl-click on check box to select all children.
		private bool m_fForbidNoItemChecked; // true to disable OK when nothing is checked.

		private Button buttonHelp;
		private HelpProvider m_helpProvider;
		private String m_helpTopic = null;
		private IHelpTopicProvider m_helpTopicProvider;

		private RadioButton m_AddButton;
		private RadioButton m_ReplaceButton;
		private RadioButton m_RemoveButton;

		// Another group of three used in filtering.
		private RadioButton m_AnyButton;
		private RadioButton m_AllButton;
		private RadioButton m_NoneButton;
		private RadioButton m_ExactButton;

		/// <summary></summary>
		protected IVwStylesheet m_stylesheet;

#if __MonoCS__
		private Gecko.GeckoWebBrowser m_webBrowser;
#else
		private WebBrowser m_webBrowser;
#endif
		private Panel m_mainPanel;
		private Button m_helpBrowserButton;
		private SplitContainer m_splitContainer;
		private FlowLayoutPanel m_buttonPanel;
		/// <summary></summary>
		protected Panel m_viewPanel;
		/// <summary></summary>
		protected FlowLayoutPanel m_link2Panel;
		/// <summary></summary>
		protected Panel m_viewExtrasPanel;
		private Label m_ctrlClickLabel;
		/// <summary></summary>
		protected FlowLayoutPanel m_link1Panel;

		private ToolStrip m_helpBrowserStrip;
		private ToolStripButton m_backButton;
		private ToolStripButton m_forwardButton;
		/// <summary></summary>
		protected FlowLayoutPanel m_checkBoxPanel;
		private CheckBox m_displayUsageCheckBox;

		private ToolStripButton m_printButton;

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
		/// Each enum value must correspond to an index into m_imageList (except for kSimpleLink
		/// which indicates that no icon is shown).
		/// </summary>
		public enum LinkType
		{
			/// <summary></summary>
			kSimpleLink = -1,
			/// <summary></summary>
			kGotoLink = 0,
			/// <summary></summary>
			kDialogLink = 1
		};

		/// <summary>
		/// Constructor for use with designer
		/// </summary>
		public ReallySimpleListChooser()
		{
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (Deprecated) constructor for use with changing or setting a value
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj">The current object.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(IPersistenceProvider persistProvider, IHelpTopicProvider helpTopicProvider,
			IEnumerable<ObjectLabel> labels, ICmObject currentObj, string fieldName)
		{
			Init(null, helpTopicProvider, persistProvider, fieldName, labels, currentObj, XMLViewsStrings.ksEmpty, null);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj">The current obj.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="nullLabel">The null label.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName, string nullLabel, IVwStylesheet stylesheet)
		{
			Init(cache, helpTopicProvider, persistProvider, fieldName, labels, currentObj, nullLabel, stylesheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// deprecated constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj">The current obj.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="nullLabel">The null label.</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName, string nullLabel)
		{
			Init(cache, helpTopicProvider, persistProvider, fieldName, labels, currentObj, nullLabel, null);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj">The current object.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName)
		{
			Init(cache, helpTopicProvider, persistProvider, fieldName, labels, currentObj, XMLViewsStrings.ksEmpty, null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReallySimpleListChooser"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void Init(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			IPersistenceProvider persistProvider, string fieldName,
			IEnumerable<ObjectLabel> labels, ICmObject currentObj, string nullLabel,
			IVwStylesheet stylesheet)
		{
			m_stylesheet = stylesheet;
			m_helpTopicProvider = helpTopicProvider;
			m_nullLabel = new NullObjectLabel(cache) {DisplayName = nullLabel};
			m_cache = cache;
			m_persistProvider = persistProvider;
			m_fieldName = fieldName;
			m_fFlatList = IsListFlat(labels);
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);

			if (m_persistProvider!= null)
				m_persistProvider.RestoreWindowSettings("SimpleListChooser", this);

			// It's easier to localize a format string than code that pieces together a string.
			Text = (fieldName == XMLViewsStrings.ksPublishIn) || (fieldName == XMLViewsStrings.ksShowAsHeadwordIn) ? fieldName : String.Format(XMLViewsStrings.ksChooseX, fieldName);

			LoadTree(labels, currentObj, true);
		}

		private static bool IsListFlat(IEnumerable<ObjectLabel> labels)
		{
			if (labels.Count() == 0)
				return false;

			foreach (ObjectLabel label in labels)
			{
				if (label.HaveSubItems)
					return false;
			}
			return true;
		}

		private void InitHelp()
		{
			// Only enable the Help button if we have a help topic for the fieldName
			if (!buttonHelp.Enabled && m_helpProvider == null && m_helpTopicProvider != null)
			{
				buttonHelp.Enabled = (helpTopicIsValid(m_helpTopic) ? true : false);
				if (buttonHelp.Enabled)
				{
					if (m_helpProvider == null)
						this.m_helpProvider = new HelpProvider();
					this.m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
					this.m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
					this.m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, IHelpTopicProvider helpTopicProvider)
			: this(persistProvider, labels, fieldName, null, helpTopicProvider)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with adding a new value (and stylesheet)
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="stylesheet">for getting right height for text</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, IVwStylesheet stylesheet,
			IHelpTopicProvider helpTopicProvider)
		{
			m_stylesheet = stylesheet;
			m_helpTopicProvider = helpTopicProvider;
			m_persistProvider = persistProvider;
			m_fieldName = fieldName;
			m_nullLabel = new NullObjectLabel();
			m_fFlatList = IsListFlat(labels);
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);

			if (m_persistProvider != null)
				m_persistProvider.RestoreWindowSettings("SimpleListChooser", this);

			// It's easier to localize a format string than code that pieces together a string.
			Text = (fieldName == XMLViewsStrings.ksPublishIn) || (fieldName == XMLViewsStrings.ksShowAsHeadwordIn) ? fieldName : String.Format(XMLViewsStrings.ksChooseX, fieldName);

			LoadTree(labels, null, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with changing or setting multiple values.
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="cache">The cache.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, FdoCache cache,
			IEnumerable<ICmObject> chosenObjs, IHelpTopicProvider helpTopicProvider) :
			this(persistProvider, labels, fieldName, cache, chosenObjs, IsListSorted(labels), helpTopicProvider)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with changing or setting multiple values.
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="cache">The cache.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		/// <param name="fSortLabels">if true, sort the labels alphabetically. if false, keep the order of given labels.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, FdoCache cache,
			IEnumerable<ICmObject> chosenObjs, bool fSortLabels, IHelpTopicProvider helpTopicProvider)
			: this(persistProvider, fieldName, cache, chosenObjs, helpTopicProvider)
		{
			m_fFlatList = IsListFlat(labels);
			m_fSortLabels = fSortLabels;
			m_fSortLabelsSet = true;
			FinishConstructor(labels);
		}

		/// <summary>
		/// Tail end of typical constructor, isolated for calling after subclass constructor
		/// has done some of its own initialization.
		/// </summary>
		/// <param name="labels"></param>
		protected void FinishConstructor(IEnumerable<ObjectLabel> labels)
		{
			// Note: anything added here might need to be added to the LeafChooser constructor also.
			LoadTree(labels, null, false);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor intended only for use by subclasses which need to initialize something
		/// before calling LoadTree (e.g., LeafChooser).
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="cache">The cache.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		protected ReallySimpleListChooser(IPersistenceProvider persistProvider,
			string fieldName, FdoCache cache, IEnumerable<ICmObject> chosenObjs,
			IHelpTopicProvider helpTopicProvider)
		{
			m_cache = cache;
			m_persistProvider = persistProvider;
			m_helpTopicProvider = helpTopicProvider;
			m_fieldName = fieldName;
			m_nullLabel = new NullObjectLabel();
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);

			if (m_persistProvider!= null)
				m_persistProvider.RestoreWindowSettings("SimpleListChooser", this);

			// It's easier to localize a format string than code that pieces together a string.
			Text = (fieldName == XMLViewsStrings.ksPublishIn) || (fieldName ==  XMLViewsStrings.ksShowAsHeadwordIn) ? fieldName : String.Format(XMLViewsStrings.ksChooseX, fieldName);

			m_labelsTreeView.CheckBoxes = true;
			m_labelsTreeView.AfterCheck += m_labelsTreeView_AfterCheck;
			// We have to allow selections in order to allow keyboard support.  See LT-3068.
			m_labelsTreeView.BeforeCheck += new TreeViewCancelEventHandler(m_labelsTreeView_BeforeCheck);
			m_chosenObjs = new HashSet<ICmObject>();
			if (chosenObjs != null)
				m_chosenObjs.UnionWith(chosenObjs);
		}

		/// <summary>
		/// Check whether the list should be sorted.  See LT-5149.
		/// </summary>
		/// <param name="labels">The labels.</param>
		/// <returns>
		/// </returns>
		private static bool IsListSorted(IEnumerable<ObjectLabel> labels)
		{
			if (labels.Count() > 0)
			{
				var labelObj = labels.First().Object;
				var owner = labelObj.Owner;
				if (owner is ICmPossibilityList)
					return (owner as ICmPossibilityList).IsSorted;
			}
			return true;
		}

		/// <summary></summary>
		public void SetObjectAndFlid(int hvo, int flid)
		{
			CheckDisposed();

			m_hvoObject = hvo;
			m_flidObject = flid;
		}

		/// <summary>
		/// Set the title (.Text) for the entire dialog.
		/// </summary>
		public string Title
		{
			set
			{
				CheckDisposed();

				string sText = value;
				if (sText.IndexOf("{0}") >= 0 && (m_sTextParam != null || m_hvoTextParam != 0))
					Text = String.Format(sText, TextParam);
				else
					Text = sText;
			}
		}

		/// <summary>
		/// Set a text for the "instructional text" area of the dialog.  Shrink the tree chooser
		/// if necessary.
		/// </summary>
		public string InstructionalText
		{
			set
			{
				CheckDisposed();

				string sText = value;
				if (sText.IndexOf("{0}") >= 0 && (m_sTextParam != null || m_hvoTextParam != 0))
					m_lblExplanation.Text = string.Format(sText, TextParam);
				else
					m_lblExplanation.Text = sText;
				m_lblExplanation.Visible = true;
			}
		}

		/// <summary>
		/// Set the text and picture for one of the two possible "link" items in the dialog.  If
		/// two links are used, set the lower one first.  The tree chooser shrinks as needed to
		/// show only the link(s) used.
		/// </summary>
		/// <param name="sText"></param>
		/// <param name="type">LinkType.kGotoLink for Goto picture, LinkType.kDialogType for
		/// Entry picture</param>
		/// <param name="obj">a FwLink for kGotoLink, a ChooserCommand for kDialogLink</param>
		public void AddLink(string sText, LinkType type, object obj)
		{
			CheckDisposed();

			// Any links past two not only assert, but then quietly do nothing.
			Debug.Assert(m_cLinksShown < 2);
			// Don't show a link if it's back to this object's owner
			// Note LexEntry no longer has an owner. But m_hvoObject can be 0 (FWR-2886).
			var objt = (m_hvoObject != 0) ? m_cache.ServiceLocator.GetObject(m_hvoObject) : null;
			var ownedHvo = (objt != null && objt.Owner != null) ? objt.Owner.Hvo : 0;
			if (m_hvoTextParam != 0 && m_hvoObject != 0 && m_hvoTextParam == ownedHvo)
				return;
			// A goto link is also inappropriate if it's back to the object we are editing itself.
			if (m_hvoTextParam == m_hvoObject && m_hvoTextParam != 0  && type == LinkType.kGotoLink)
				return;

			if (m_cLinksShown >= 2)
				return;
			if (sText.IndexOf("{0}") >= 0 && (m_sTextParam != null || m_hvoTextParam != 0))
				sText = String.Format(sText, TextParam);
			++m_cLinksShown;
			if (m_cLinksShown == 1)
			{
				m_lblLink1.Text = sText;
				if (type != LinkType.kSimpleLink)
					m_picboxLink1.Image = m_imageList.Images[(int)type];
				m_obj1 = obj;
				m_link1Panel.Visible = true;
			}
			else if (m_cLinksShown == 2)
			{
				m_lblLink2.Text = sText;
				if (type != LinkType.kSimpleLink)
					m_picboxLink2.Image = m_imageList.Images[(int)type];
				m_obj2 = obj;
				m_link2Panel.Visible = true;
			}
		}

		/// <summary>
		/// Show extra radio buttons for Add/Replace (and possibly Remove)
		/// </summary>
		public void ShowFuncButtons()
		{
			CheckDisposed();

			// reuse the m_link2Panel to display the buttons
			// ENHANCE (DamienD): Add a new panel in the designer that contain these buttons, and just make it visible here
			m_link2Panel.SuspendLayout();

			m_link2Panel.Controls.Clear();

			m_AddButton = new RadioButton {Text = XMLViewsStrings.ksAddToExisting, Checked = true};
			m_AddButton.Width = ((m_link2Panel.Width - m_link2Panel.Padding.Horizontal) / 2) - m_AddButton.Margin.Horizontal - 1;
			m_link2Panel.Controls.Add(m_AddButton);

			m_RemoveButton = new RadioButton { Text = XMLViewsStrings.ksRemoveExisting, Width = m_AddButton.Width, Height = 30 };
			m_link2Panel.Controls.Add(m_RemoveButton);

			m_ReplaceButton = new RadioButton {Text = XMLViewsStrings.ksReplaceExisting, Width = m_labelsTreeView.Width};
			m_link2Panel.Controls.Add(m_ReplaceButton);

			m_link2Panel.Visible = true;

			m_link2Panel.ResumeLayout();
		}

		/// <summary>
		/// Show extra radio buttons for matching All, Any, or None.
		/// </summary>
		internal void ShowAnyAllNoneButtons(ListMatchOptions mode, bool fAtomic)
		{
			CheckDisposed();
			SetHelpTopic("khtpChoose-AnyAllNoneItems");

			// reuse the m_link2Panel to display the buttons
			// ENHANCE (DamienD): Add a new panel in the designer that contain these buttons, and just make it visible here
			m_link2Panel.SuspendLayout();

			m_link2Panel.Controls.Clear();

			m_AnyButton = new RadioButton {Text = XMLViewsStrings.ksAnyChecked, Checked = true};
			m_AnyButton.Width = ((m_link2Panel.Width - m_link2Panel.Padding.Horizontal) / 2) - m_AnyButton.Margin.Horizontal - 1;
			m_link2Panel.Controls.Add(m_AnyButton);

			if (!fAtomic)
			{
				m_AllButton = new RadioButton {Text = XMLViewsStrings.ksAllChecked, Width = m_AnyButton.Width};
				m_link2Panel.Controls.Add(m_AllButton);
			}

			m_NoneButton = new RadioButton {Text = XMLViewsStrings.ksNoChecked, Width = m_AnyButton.Width};
			m_link2Panel.Controls.Add(m_NoneButton);

			if (!fAtomic)
			{
				m_ExactButton = new RadioButton {Text = XMLViewsStrings.ksExactlyChecked, Width = m_AnyButton.Width};
				m_link2Panel.Controls.Add(m_ExactButton);
			}

			if (fAtomic)
				m_link2Panel.FlowDirection = FlowDirection.TopDown;
			m_link2Panel.Visible = true;

			m_link2Panel.ResumeLayout();

			ListMatchMode = mode;
		}

		/// <summary>
		/// Enable using Ctrl-Click to toggle subitems along with parent.
		/// </summary>
		internal void EnableCtrlClick()
		{
			CheckDisposed();
			m_fEnableCtrlCheck = true;
			m_ctrlClickLabel.Visible = true;
			m_viewExtrasPanel.Visible = true;
		}

		/// <summary>
		/// Called after a check box is checked (or unchecked).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_labelsTreeView_AfterCheck(object sender, TreeViewEventArgs e)
		{
			var clickNode = (LabelNode)e.Node;
			if (!clickNode.Enabled)
				return;
			if (m_fEnableCtrlCheck && ModifierKeys == Keys.Control && !Atomic)
			{
				using (new WaitCursor())
				{
					if (e.Action != TreeViewAction.Unknown)
					{
						// The original check, not recursive.
						clickNode.AddChildren(true, new HashSet<ICmObject>()); // All have to exist to get checked/unchecked
						if (!clickNode.IsExpanded)
							clickNode.Expand(); // open up at least one level to show effects.
					}
					foreach (TreeNode node in clickNode.Nodes)
						node.Checked = e.Node.Checked; // and recursively checks children.
				}
			}
			if (m_fForbidNoItemChecked)
			{
				btnOK.Enabled = AnyItemChecked(m_labelsTreeView.Nodes);
			}
			if (Atomic && clickNode.Checked)
			{
				var checkedNodes = new HashSet<TreeNode>();
				foreach (TreeNode child in m_labelsTreeView.Nodes)
					CollectCheckedNodes(child, checkedNodes);
				checkedNodes.Remove(clickNode);
				foreach (var node in checkedNodes)
				{
					// will produce a recursive call, but it won't do much because the changing node
					// is NOT checked.
					node.Checked = false;
				}
			}
		}

		// Uncheck every node in the tree except possibly current.
		void CollectCheckedNodes(TreeNode root, HashSet<TreeNode> checkedNodes)
		{
			if (root.Checked)
				checkedNodes.Add(root);
			foreach (TreeNode child in root.Nodes)
				CollectCheckedNodes(child, checkedNodes);
		}

		/// <summary>
		/// Set to prevent the user closing the dialog with nothing selected.
		/// Note that it is assumed that something is selected when the dialog opens.
		/// Setting this will not disable the OK button until the user changes something.
		/// </summary>
		public bool ForbidNoItemChecked
		{
			get { return m_fForbidNoItemChecked; }
			set { m_fForbidNoItemChecked = value; }
		}

		private static bool AnyItemChecked(TreeNodeCollection nodes)
		{
			foreach (TreeNode child in nodes)
				if (child.Checked || AnyItemChecked(child.Nodes))
					return true;
			return false;
		}

		/// <summary>
		/// True if we should replace items.
		/// </summary>
		public bool ReplaceMode
		{
			get
			{
				CheckDisposed();
				return m_ReplaceButton != null && m_ReplaceButton.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether [remove mode].
		/// </summary>
		/// <value><c>true</c> if [remove mode]; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool RemoveMode
		{
			get
			{
				CheckDisposed();
				return m_RemoveButton != null && m_RemoveButton.Checked;
			}
		}

		internal ListMatchOptions ListMatchMode
		{
			get
			{
				CheckDisposed();

				if (m_AllButton != null && m_AllButton.Checked)
					return ListMatchOptions.All;
				else if (m_NoneButton.Checked)
					return ListMatchOptions.None;
				else if (m_ExactButton != null &&  m_ExactButton.Checked)
					return ListMatchOptions.Exact;
				else
					return ListMatchOptions.Any;
			}
			set
			{
				if (value == ListMatchOptions.All)
					m_AllButton.Checked = true;
				else if (value == ListMatchOptions.None)
					m_NoneButton.Checked = true;
				else if (value == ListMatchOptions.Exact)
					m_ExactButton.Checked = true;
				else
					m_AnyButton.Checked = true;
			}
		}

		/// <summary>
		/// Set the database id of the object which serves as a possible parameter to
		/// InstructionalText, Title, or a link label.
		/// </summary>
		public int TextParamHvo
		{
			get
			{
				CheckDisposed();
				return m_hvoTextParam;
			}
			set
			{
				CheckDisposed();
				m_hvoTextParam = value;
			}
		}

		/// <summary>
		/// Get the name which serves as a parameter to InstructionalText, Title, or link
		/// labels.  If computing from an HVO, also save its CmObject GUID for possible later
		/// use in a FwLink object as a side-effect,
		/// </summary>
		public string TextParam
		{
			set
			{
				CheckDisposed();
				m_sTextParam = value;
			}
			get
			{
				CheckDisposed();

				if (m_sTextParam != null)
				{
					return m_sTextParam;
				}
				else if (m_hvoTextParam != 0)
				{
					var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoTextParam);
					m_sTextParam = co.ShortName;
					// We want this link Guid value only if label/text hint that it's needed.
					// (This requirement is subject to change without much notice!)
					m_guidLink = co.Guid;
					return m_sTextParam;
				}
				else
				{
					return XMLViewsStrings.ksQuestionMarks;
				}
			}
		}

		/// <summary>
		/// Initialize the behavior from an XML configuration node.
		/// </summary>
		/// <param name="configNode"></param>
		/// <param name="propertyTable"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void InitializeExtras(XmlNode configNode, IPropertyTable propertyTable)
		{
			CheckDisposed();

			Debug.Assert(m_cache != null);
			m_propertyTable = propertyTable;
			int ws = m_cache.DefaultAnalWs;
			SetFontFromWritingSystem(ws);

			if (configNode == null)
				return;
			XmlNode node = configNode.SelectSingleNode("chooserInfo");
			if (node == null)
				node = GenerateChooserInfoForCustomNode(configNode);
			if (node != null)
			{
				string sTextParam =
					XmlUtils.GetAttributeValue(node, "textparam", "owner").ToLower();
				if (sTextParam != null)
				{
					// The default case ("owner") is handled by the caller setting TextParamHvo.
					if (sTextParam == "vernws")
					{
						IWritingSystem co = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
						m_sTextParam = co.DisplayLabel;
					}
				}
				string sFlid = XmlUtils.GetAttributeValue(node, "flidTextParam");
				if (sFlid != null)
				{
					try
					{
						int flidTextParam = Int32.Parse(sFlid, CultureInfo.InvariantCulture);
						if (flidTextParam != 0)
						{
							ISilDataAccess sda = m_cache.DomainDataByFlid;
							m_hvoTextParam = sda.get_ObjectProp(m_hvoObject, flidTextParam);
						}
					}
					catch
					{
						// Ignore any badness here.
					}
				}

				string sTitle = XmlUtils.GetAttributeValue(node, "title");
				if (sTitle != null)
					Title = sTitle;
				string sText = XmlUtils.GetAttributeValue(node, "text");
				if (sText != null)
					InstructionalText = sText;
				XmlNodeList linkNodes = node.SelectNodes("chooserLink");
				Debug.Assert(linkNodes != null && linkNodes.Count <= 2);
				for (int i = linkNodes.Count - 1; i >= 0 ; --i)
				{
					string sType = XmlUtils.GetAttributeValue(linkNodes[i], "type", "goto").ToLower();
					string sLabel = XmlUtils.GetLocalizedAttributeValue(linkNodes[i], "label", null);
					switch (sType)
					{
					case "goto":
					{
						string sTool = XmlUtils.GetAttributeValue(linkNodes[i], "tool");
						if (sLabel != null && sTool != null)
						{
							AddLink(sLabel, LinkType.kGotoLink, new FwLinkArgs(sTool, m_guidLink));
						}
						break;
					}
					case "dialog":
					{
						string sDialog = XmlUtils.GetAttributeValue(linkNodes[i], "dialog");
						// TODO: make use of sDialog somehow to create a ChooserCommand object.
						// TODO: maybe even better, use a new SubDialog object that allows us
						// to call the specified dialog, then return to this dialog, adding
						// a newly created object to the list of chosen items (or making the
						// newly created object the chosen item).
						if (sLabel != null && sDialog != null)
							AddLink(sLabel, LinkType.kDialogLink, null);
						break;
					}
					case "simple":
					{
						string sTool = XmlUtils.GetAttributeValue(linkNodes[i], "tool");
						if (sLabel != null && sTool != null)
						{
							AddSimpleLink(sLabel, sTool, linkNodes[i]);
						}
						break;
					}
					}
				}
				string sGuiControl = XmlUtils.GetOptionalAttributeValue(node, "guicontrol");
				// Replace the tree view control with a browse view control if it's both desirable
				// and feasible.
				if (m_fFlatList && !string.IsNullOrEmpty(sGuiControl))
					ReplaceTreeView(sGuiControl);

				bool useHelpBrowser = XmlUtils.GetOptionalBooleanAttributeValue(node, "helpBrowser", false);
				if (useHelpBrowser)
					InitHelpBrowser();
			}
		}

		/// <summary>
		/// A custom list reference field doesn't have the nice &lt;chooserInfo&gt; node
		/// provided for the slice, so we have to generate one (if possible).  See FWR-1187.
		/// </summary>
		/// <returns>A &lt;chooserInfo&gt; node, or null</returns>
		/// <remarks>
		/// This requires too intimate a knowledge of the layout of merged configuration files
		/// for my liking, but it's the only way I could think of to make this adaptable to
		/// ongoing growth of the system.
		/// </remarks>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private XmlNode GenerateChooserInfoForCustomNode(XmlNode configNode)
		{
			string editor = XmlUtils.GetAttributeValue(configNode, "editor");
			if (configNode.Name != "slice" || editor != "autoCustom" || m_hvoTextParam == 0)
				return null;
			ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoTextParam);
			ICmPossibilityList list = obj as ICmPossibilityList;
			if (list == null)
				return null;
			int listFlid = list.OwningFlid;
			string listField = null;
			string listOwnerClass = null;
			if (list.Owner != null)
			{
				listField = m_cache.MetaDataCacheAccessor.GetFieldName(listFlid);
				listOwnerClass = m_cache.MetaDataCacheAccessor.GetClassName(listFlid / 1000);
			}
			string itemClass = m_cache.MetaDataCacheAccessor.GetClassName(list.ItemClsid);
			// We need to dynamically figure out a tool for this list.
			string sTool = null;
			XmlNode chooserNode = null;
			XmlNode windowConfig = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			if (windowConfig != null)
			{
				// The easiest search is through various jump command parameters.
				foreach (XmlNode xnCommand in windowConfig.SelectNodes("/window/commands/command"))
				{
					XmlNode xnParam = xnCommand.SelectSingleNode("parameters");
					if (xnParam != null)
					{
						if (XmlUtils.GetAttributeValue(xnParam, "className") == itemClass &&
							XmlUtils.GetAttributeValue(xnParam, "ownerClass") == listOwnerClass &&
							XmlUtils.GetAttributeValue(xnParam, "ownerField") == listField)
						{
							sTool = XmlUtils.GetAttributeValue(xnParam, "tool");
							if (!String.IsNullOrEmpty(sTool))
								break;
						}
					}
				}
				// Couldn't find anything in the commands, try the clerks and tools.
				if (String.IsNullOrEmpty(sTool))
					sTool = ScanToolsAndClerks(windowConfig, listOwnerClass, listField);
			}
			if (!String.IsNullOrEmpty(sTool))
			{
				StringBuilder bldr = new StringBuilder();
				bldr.AppendLine("<chooserInfo>");
				string label = list.Name.UserDefaultWritingSystem.Text;
				if (String.IsNullOrEmpty(label) || label == list.Name.NotFoundTss.Text)
					label = list.Name.BestAnalysisVernacularAlternative.Text;
				bldr.AppendFormat("<chooserLink type=\"goto\" label=\"Edit the {0} list\" tool=\"{1}\"/>",
					label, sTool);
				bldr.AppendLine();
				bldr.AppendLine("</chooserInfo>");
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(bldr.ToString());
				chooserNode = doc.FirstChild;
			}
			return chooserNode;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private string ScanToolsAndClerks(XmlNode windowConfig, string listOwnerClass, string listField)
		{
			foreach (XmlNode xnItem in windowConfig.SelectNodes("/window/lists/list/item"))
			{
				foreach (XmlNode xnClerk in xnItem.SelectNodes("parameters/clerks/clerk"))
				{
					string sClerkId = XmlUtils.GetAttributeValue(xnClerk, "id");
					if (String.IsNullOrEmpty(sClerkId))
						continue;
					XmlNode xnList = xnClerk.SelectSingleNode("recordList");
					if (xnList == null)
						continue;
					if (XmlUtils.GetAttributeValue(xnList, "owner") == listOwnerClass &&
						XmlUtils.GetAttributeValue(xnList, "property") == listField)
					{
						foreach (XmlNode xnTool in xnItem.SelectNodes("parameters/tools/tool"))
						{
							string sTool = XmlUtils.GetAttributeValue(xnTool, "value");
							if (String.IsNullOrEmpty(sTool))
								continue;
							XmlNode xnParam = xnTool.SelectSingleNode("control/parameters/control/parameters");
							if (xnParam == null)
								continue;
							string sClerk = XmlUtils.GetAttributeValue(xnParam, "clerk");
							if (sClerk == sClerkId)
								return sTool;
						}
					}
				}
			}
			return null;
		}

		private void InitHelpBrowser()
		{
			int splitterDistance = m_splitContainer.Width;
			if (m_persistProvider != null)
			{
				m_persistProvider.RestoreWindowSettings("SimpleListChooser-HelpBrowser", this);
				splitterDistance = m_propertyTable.GetValue("SimpleListChooser-HelpBrowserSplitterDistance", m_splitContainer.Width);
			}

			// only create the web browser if we needed, because this control is pretty resource intensive
#if __MonoCS__
			m_webBrowser = new Gecko.GeckoWebBrowser
			{
				Dock = DockStyle.Fill,
				TabIndex = 1,
				MinimumSize = new Size(20, 20),
				NoDefaultContextMenu = true
			};
#else
			m_webBrowser = new WebBrowser
			{
				Dock = DockStyle.Fill,
				IsWebBrowserContextMenuEnabled = false,
				WebBrowserShortcutsEnabled = false,
				AllowWebBrowserDrop = false
			};
#endif
			m_helpBrowserButton.Visible = true;
			m_viewExtrasPanel.Visible = true;
#if !__MonoCS__
			m_webBrowser.Navigated += m_webBrowser_Navigated;
#endif
			m_webBrowser.CanGoBackChanged += m_webBrowser_CanGoBackChanged;
			m_webBrowser.CanGoForwardChanged += m_webBrowser_CanGoForwardChanged;
			m_splitContainer.Panel2.Controls.Add(m_webBrowser);

			m_backButton = new ToolStripButton(null, m_imageList.Images[2], m_backButton_Click) {Enabled = false};
			m_forwardButton = new ToolStripButton(null, m_imageList.Images[3], m_forwardButton_Click) {Enabled = false};
#if __MonoCS__
			m_helpBrowserStrip = new ToolStrip(m_backButton, m_forwardButton) { Dock = DockStyle.Top };
#else
			m_printButton = new ToolStripButton(null, m_imageList.Images[4], m_printButton_Click);
			m_helpBrowserStrip = new ToolStrip(m_backButton, m_forwardButton, m_printButton) { Dock = DockStyle.Top };
#endif
			m_splitContainer.Panel2.Controls.Add(m_helpBrowserStrip);

			if (splitterDistance < m_splitContainer.Width)
			{
				// the help browser was expanded when last saved, so display it expanded with the saved splitter distance
				m_splitContainer.IsSplitterFixed = false;
				m_splitContainer.SplitterDistance = splitterDistance;
				m_splitContainer.Panel2Collapsed = false;
				m_helpBrowserButton.Text = string.Format("<<< {0}", XMLViewsStrings.ksLess);
			}
			else
			{
				m_splitContainer.SplitterDistance = splitterDistance;
			}

			// navigate the the current selected object now, just in case the selection events are never fired
			NavigateToSelectedTopic();
		}

		private void m_webBrowser_CanGoForwardChanged(object sender, EventArgs e)
		{
			m_forwardButton.Enabled = m_webBrowser.CanGoForward;
		}

		private void m_webBrowser_CanGoBackChanged(object sender, EventArgs e)
		{
			m_backButton.Enabled = m_webBrowser.CanGoBack;
		}

		private void m_webBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
		{
			string helpTopic = GetHelpTopic(e.Url);
			if (helpTopic == null)
				return;
			helpTopic = helpTopic.ToLowerInvariant();
			// ENHANCE (DamienD): cache HelpId to object mappings in a dictionary, so we
			// don't have to search through all objects to find a match
			if (m_labelsTreeView != null)
			{
				var stack = new Stack<LabelNode>(m_labelsTreeView.Nodes.Cast<LabelNode>());
				while (stack.Count > 0)
				{
					LabelNode node = stack.Pop();
					var pos = node.Label.Object as ICmPossibility;
					if (pos != null)
					{
						string curHelpTopic = pos.HelpId;
						if (curHelpTopic != null && curHelpTopic.ToLowerInvariant() == helpTopic)
						{
							m_labelsTreeView.SelectedNode = node;
							break;
						}
					}
					node.AddChildren(true, m_chosenObjs);
					foreach (TreeNode childNode in node.Nodes)
					{
						var labelNode = childNode as LabelNode;
						if (labelNode != null)
							stack.Push(labelNode);
					}
				}
			}
			else
			{
				for (int i = 0; i < m_labels.Count; i++)
				{
					var pos = m_labels[i].Object as ICmPossibility;
					if (pos != null)
					{
						string curHelpTopic = pos.HelpId;
						if (curHelpTopic != null && curHelpTopic.ToLowerInvariant() == helpTopic)
						{
							m_flvLabels.SelectedIndex = i;
							break;
						}
					}
				}
			}
		}

		private void NavigateToSelectedTopic()
		{
			if (m_webBrowser == null)
				return;

			ObjectLabel selectedLabel = null;
			if (m_labelsTreeView != null)
			{
				if (m_labelsTreeView.SelectedNode != null)
					selectedLabel = ((LabelNode)m_labelsTreeView.SelectedNode).Label;
			}
			else
			{
				int idx = m_flvLabels.SelectedIndex;
				selectedLabel = m_labels[idx];
			}

			if (selectedLabel != null)
			{
				var pos = selectedLabel.Object as ICmPossibility;
				if (pos != null)
				{
					string helpFile = pos.OwningList.HelpFile;
#if __MonoCS__
					// Force Linux to use combined Ocm/OcmFrame files
					if (helpFile == "Ocm.chm")
						helpFile = "OcmFrame";
#endif
					string helpTopic = pos.HelpId;
					if (!string.IsNullOrEmpty(helpFile) && !string.IsNullOrEmpty(helpTopic))
					{
						string curHelpTopic = GetHelpTopic(m_webBrowser.Url);
						if (curHelpTopic == null || helpTopic.ToLowerInvariant() != curHelpTopic.ToLowerInvariant())
						{
							if (!Path.IsPathRooted(helpFile))
							{
								// Helps are part of the installed code files.  See FWR-1002.
								string helpsPath = Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps");
								helpFile = Path.Combine(helpsPath, helpFile);
							}
#if __MonoCS__
							// remove file extension, we need folder of the same name with the htm files
							helpFile = helpFile.Replace(".chm","");
							string url = string.Format("{0}/{1}.htm", helpFile, helpTopic.ToLowerInvariant());
#else
							string url = string.Format("its:{0}::/{1}.htm", helpFile, helpTopic);
#endif
							m_webBrowser.Navigate(url);
						}
					}
					else
					{
						GenerateDefaultPage(pos.ShortNameTSS, pos.Description.BestAnalysisAlternative);
					}
				}
				else
				{
					GenerateDefaultPage(selectedLabel.Object.ShortNameTSS, null);
				}
			}
		}

		private static string GetHelpTopic(Uri url)
		{
			if (url == null)
				return null;
			string urlStr = url.ToString();
#if __MonoCS__
			int startIndex = urlStr.IndexOf("OcmFrame/");
			if (startIndex == -1)
				return null;
			startIndex += 9;
#else
			int startIndex = urlStr.IndexOf("::/");
			if (startIndex == -1)
				return null;
			startIndex += 3;
#endif
			int endIndex = urlStr.IndexOf(".htm", startIndex);
			if (endIndex == -1)
				return null;
			return urlStr.Substring(startIndex, endIndex - startIndex);
		}

		private void GenerateDefaultPage(ITsString tssTitle, ITsString tssDesc)
		{
			IWritingSystem ws = m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
			string userFont = ws.DefaultFontName;

			string title, titleFont;
			if (tssTitle != null)
			{
				title = tssTitle.Text;
				int wsHandle = TsStringUtils.GetWsAtOffset(tssTitle, 0);
				ws = m_cache.ServiceLocator.WritingSystemManager.Get(wsHandle);
				titleFont = ws.DefaultFontName;
			}
			else
			{
				title = XMLViewsStrings.ksTitle;
				titleFont = userFont;
			}

			string desc, descFont;
			if (tssDesc != null)
			{
				desc = tssDesc.Text;
				if (desc == "***")
				{
					desc = XMLViewsStrings.ksNoDesc;
					descFont = userFont;
				}
				else
				{
					int wsHandle = TsStringUtils.GetWsAtOffset(tssDesc, 0);
					ws = m_cache.ServiceLocator.WritingSystemManager.Get(wsHandle);
					descFont = ws.DefaultFontName;
				}
			}
			else
			{
				desc = XMLViewsStrings.ksNoDesc;
				descFont = userFont;
			}

#if __MonoCS__
			var tempfile = Path.Combine(FileUtils.GetTempFile("htm"));
			using (var w = new StreamWriter(tempfile, false))
			using (var tw = new XmlTextWriter(w))
			{
#endif
			var htmlElem = new XElement("html",
				new XElement("head",
					new XElement("title", title)),
				new XElement("body",
					new XElement("font",
						new XAttribute("face", titleFont),
						new XElement("h2", title)),
					new XElement("font",
						new XAttribute("face", userFont),
						new XElement("h3", XMLViewsStrings.ksShortDesc)),
					new XElement("font",
						new XAttribute("face", descFont),
						new XElement("p", desc))));
#if __MonoCS__
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(htmlElem.ToString());
			xmlDocument.WriteTo(tw);
			}
			m_webBrowser.Navigate(tempfile);
			if (FileUtils.FileExists(tempfile))
				FileUtils.Delete(tempfile);
			tempfile = null;
#else
			m_webBrowser.DocumentText = htmlElem.ToString();
#endif
		}

		private void m_flvLabels_SelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			NavigateToSelectedTopic();
		}

		private void m_labelsTreeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			NavigateToSelectedTopic();
		}

		private void m_backButton_Click(object sender, EventArgs e)
		{
			m_webBrowser.GoBack();
		}

		private void m_forwardButton_Click(object sender, EventArgs e)
		{
			m_webBrowser.GoForward();
		}

#if !__MonoCS__
		private void m_printButton_Click(object sender, EventArgs e)
		{
			m_webBrowser.ShowPrintDialog();
		}
#endif

		private void ExpandHelpBrowser()
		{
			if (!m_splitContainer.Panel2Collapsed)
				return;

			m_splitContainer.IsSplitterFixed = false;
			m_splitContainer.SplitterDistance = m_splitContainer.Width;
			Width = Width + 400;
			m_splitContainer.Panel2Collapsed = false;
			m_helpBrowserButton.Text = string.Format("<<< {0}", XMLViewsStrings.ksLess);
		}

		private void CollapseHelpBrowser()
		{
			if (m_splitContainer.Panel2Collapsed)
				return;

			ClientSize = new Size(m_splitContainer.SplitterDistance, ClientSize.Height);
			m_splitContainer.Panel2Collapsed = true;
			m_splitContainer.IsSplitterFixed = true;
			m_helpBrowserButton.Text = string.Format("{0} >>>", XMLViewsStrings.ksMore);
		}

		private void m_helpBrowserButton_Click(object sender, EventArgs e)
		{
			if (m_splitContainer.Panel2Collapsed)
				ExpandHelpBrowser();
			else
				CollapseHelpBrowser();
		}

		private void m_displayUsageCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (m_labelsTreeView != null)
			{
				using (new WaitCursor(this))
				{
					m_labelsTreeView.BeginUpdate();
					var stack = new Stack<LabelNode>(m_labelsTreeView.Nodes.Cast<LabelNode>());
					while (stack.Count > 0)
					{
						LabelNode node = stack.Pop();
						node.DisplayUsage = m_displayUsageCheckBox.Checked;
						foreach (TreeNode childNode in node.Nodes)
						{
							var labelNode = childNode as LabelNode;
							if (labelNode != null)
								stack.Push(labelNode);
						}
					}
					m_labelsTreeView.EndUpdate();
				}
			}
		}

		/// <summary>
		/// Access for outsiders who don't call InitializExtras.
		/// </summary>
		/// <param name="propertyTable"></param>
		/// <param name="sGuiControl"></param>
		public void ReplaceTreeView(IPropertyTable propertyTable, string sGuiControl)
		{
			if (m_fFlatList)
			{
				if (m_propertyTable == null)
					m_propertyTable = propertyTable;
				ReplaceTreeView(sGuiControl);
			}
		}
		/// <summary>
		/// This does the tricky work of replace the tree view control with a browse view
		/// control.
		/// </summary>
		/// <param name="sGuiControl"></param>
		private void ReplaceTreeView(string sGuiControl)
		{
			if (!m_fFlatList || String.IsNullOrEmpty(sGuiControl))
				return;
			var xnWindow = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			if (xnWindow == null)
				return;
			string sXPath = string.Format("controls/parameters/guicontrol[@id=\"{0}\"]/parameters", sGuiControl);
			XmlNode configNode = xnWindow.SelectSingleNode(sXPath);
			if (configNode == null)
				return;
			m_flvLabels = new FlatListView
			{
				Dock = DockStyle.Fill,
				TabStop = m_labelsTreeView.TabStop,
				TabIndex = m_labelsTreeView.TabIndex
			};
			m_flvLabels.SelectionChanged += m_flvLabels_SelectionChanged;
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			m_flvLabels.Initialize(m_cache, stylesheet, m_propertyTable, configNode, m_objs);
			if (m_chosenObjs != null)
				m_flvLabels.SetCheckedItems(m_chosenObjs);
			m_viewPanel.Controls.Remove(m_labelsTreeView);
			m_viewPanel.Controls.Add(m_flvLabels);
			if (m_labelsTreeView != null)
				m_labelsTreeView.Dispose();
			m_labelsTreeView = null;
			m_checkBoxPanel.Visible = false;
		}

		/// <summary>
		/// Set the overall font for the dialog. This will be the default normal font for the first
		/// writing system in wss, except that if other wss require a larger one, we use a larger size,
		/// since otherwise the silly treeview cuts them off.
		/// This also sets a stylesheet which will be used to determine a size and family for
		/// vernacular text.
		/// </summary>
		/// <param name="wss"></param>
		/// <param name="stylesheet"></param>
		/// <param name="wsf"></param>
		public void SetFontForDialog(int[] wss, IVwStylesheet stylesheet, ILgWritingSystemFactory wsf)
		{
			CheckDisposed();

			m_stylesheet = stylesheet;
			Font tmpFont = FontHeightAdjuster.GetFontForNormalStyle(wss[0], stylesheet, wsf);
			Font font = tmpFont;
			try
			{
				for (int i = 1; i < wss.Length; i++)
				{
					using (Font other = FontHeightAdjuster.GetFontForNormalStyle(wss[i], stylesheet, wsf))
					{
						// JohnT: this is a compromise. I don't think it is guaranteed that a font with the
						// same SizeInPoints will be the same height. But it should be about the same,
						// and until we implement a proper multilingual treeview replacement, I'm not sure we
						// can do much better than this.
						if (other.Height > font.Height)
						{
							if (font != tmpFont)
								font.Dispose();
							font = new Font(font.FontFamily, Math.Max(font.SizeInPoints, other.SizeInPoints));
						}
					}
				}

				m_labelsTreeView.Font = font;
			}
			finally
			{
				if (font != tmpFont)
					tmpFont.Dispose();
			}
		}

		private void SetFontFromWritingSystem(int ws)
		{
			Font oldFont = m_labelsTreeView.Font;
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			Font font = FontHeightAdjuster.GetFontForNormalStyle(
				ws, stylesheet, m_cache.WritingSystemFactory);
			float maxPoints = font.SizeInPoints;
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				if (node.NodeFont != oldFont && node.NodeFont != null) // overridden because of vernacular text
				{
					node.ResetVernacularFont(
						m_cache.WritingSystemFactory,
						m_cache.DefaultVernWs,
						stylesheet);
					maxPoints = Math.Max(maxPoints, node.NodeFont.SizeInPoints);
				}
			}
			if (maxPoints > font.SizeInPoints)
			{
				var family = font.FontFamily;
				font.Dispose();
				font = new Font(family, maxPoints);
			}
			m_labelsTreeView.Font = font;
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				if (node.NodeFont == oldFont) // not overridden because of vernacular text
					node.NodeFont = font;
			}
			oldFont.Dispose();
			//IWritingSystem lgws =
			//	m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws);
			//if (lgws != null)
			//{
			//	string sFont = lgws.DefaultSansSerif;
			//	if (sFont != null)
			//	{
			//		System.Drawing.Font font =
			//			new System.Drawing.Font(sFont, m_labelsTreeView.Font.SizeInPoints);
			//		m_labelsTreeView.Font = font;
			//	}
			//}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the raw.
		/// </summary>
		/// <param name="propertyTable"></param>
		/// <param name="publisher"></param>
		/// <param name="sTitle">The s title.</param>
		/// <param name="sText">The s text.</param>
		/// <param name="sGotoLabel">The s goto label.</param>
		/// <param name="sTool">The s tool.</param>
		/// <param name="sWs">The s ws.</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeRaw(IPropertyTable propertyTable, IPublisher publisher, string sTitle, string sText,
			string sGotoLabel, string sTool, string sWs)
		{
			CheckDisposed();

			Debug.Assert(m_cache != null);
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			if (sTitle != null)
				Title = sTitle;
			if (sText != null)
				InstructionalText = sText;
			if (sGotoLabel != null && sTool != null)
			{
				AddLink(sGotoLabel, LinkType.kGotoLink, new FwLinkArgs(sTool, m_guidLink));
			}
			int ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			// Now that we're overriding the font for nodes that contain vernacular text,
			// it's best to let all the others default to the analysis language.
			//if (sWs != null)
			//{
			//	switch (sWs)
			//	{
			//	case "vernacular":
			//	case "all vernacular":
			//	case "vernacular analysis":
			//		ws = m_cache.DefaultVernWs;
			//		break;
			//	case "analysis":
			//	case "all analysis":
			//	case "analysis vernacular":
			//		ws = m_cache.DefaultAnalWs;
			//		break;
			//	}
			//}
			SetFontFromWritingSystem(ws);
		}

		/// <summary>
		///
		/// </summary>
		protected virtual void AddSimpleLink(string sLabel, string sTool, XmlNode node)
		{
			switch (sTool)
			{
			default:
				// TODO: Handle other cases as they arise.
				AddLink(sLabel, LinkType.kSimpleLink, null);
				break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvo of highest POS.
		/// </summary>
		/// <param name="startHvo">The start hvo.</param>
		/// <param name="sTopPOS">The s top POS.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int GetHvoOfHighestPOS(int startHvo, out string sTopPOS)
		{
			int posHvo = 0;
			sTopPOS = XMLViewsStrings.ksQuestionMarks;
			var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(startHvo);
			while (obj.ClassID == PartOfSpeechTags.kClassId)
			{
				posHvo = obj.Hvo;
				sTopPOS = obj.ShortName;
				obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(obj.Owner.Hvo);
			}
			return posHvo;
		}

		/// <summary>
		/// If the user clicked on a link label, post a message via the mediator to jump to that
		/// location in the program.
		/// </summary>
		/// <returns><c>true</c> if a jump taken, <c>false</c> otherwise</returns>
		public bool HandleAnyJump()
		{
			CheckDisposed();

			if (m_publisher != null && m_linkJump != null)
			{
				var commands = new List<string>
											{
												"AboutToFollowLink",
												"FollowLink"
											};
				var parms = new List<object>
											{
												null,
												m_linkJump
											};
				m_publisher.Publish(commands, parms);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// If the user clicked on a link label, post a message via the mediator to jump to that
		/// location in the program.
		/// </summary>
		/// <returns><c>true</c> if a jump taken, <c>false</c> otherwise</returns>
		public bool HandleAnyJump(IPublisher publisher)
		{
			CheckDisposed();

			if (publisher != null && m_linkJump != null)
			{
				var commands = new List<string>
											{
												"AboutToFollowLink",
												"FollowLink"
											};
				var parms = new List<object>
											{
												null,
												m_linkJump
											};
				m_publisher.Publish(commands, parms);
				return true;
			}
			else
			{
				return false;
			}
		}

		private void SimpleListChooser_Activated(object sender, EventArgs e)
		{
			if (m_labelsTreeView != null)
				m_labelsTreeView.Focus();
			else
				m_flvLabels.Focus();
		}

		/// <summary>
		/// Overridden to defeat the standard .NET behavior of adjusting size by
		/// screen resolution. That is bad for a list chooser because we remember the size,
		/// and if we remember the enlarged size, it just keeps growing.
		/// If we defeat it, it may look a bit small the first time at high resolution,
		/// but at least it will stay the size the user sets.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			Size size = Size;
			base.OnLoad(e);
			if (Size != size)
				Size = size;
		}

		/// <summary>
		/// Loads the tree.
		/// </summary>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj">The current object.</param>
		/// <param name="showCurrentSelection"></param>
		protected void LoadTree(IEnumerable<ObjectLabel> labels, ICmObject currentObj,
			bool showCurrentSelection)
		{
			Debug.Assert(showCurrentSelection? (m_chosenObjs == null) : (currentObj == null),
				"If showEmptyOption is false, currentHvo should be zero, since it is meaningless");

			if (m_fFlatList)
			{
				m_labels = labels.ToList();
				m_objs = (from label in labels
						  select label.Object).ToList();
			}
			using (new WaitCursor())
			{
				m_labelsTreeView.BeginUpdate();
				m_labelsTreeView.Nodes.Clear();

				// if m_fSortLabels is true, we'll sort the labels alphabetically, using dumb English sort.
				// otherwise, we'll keep the labels in their given order.
				if (!m_fSortLabelsSet && m_cache != null)
				{
					m_fSortLabels = IsListSorted(labels);
					m_fSortLabelsSet = true;
				}
				m_labelsTreeView.Sorted = m_fSortLabels;
				Stack<ICmObject> ownershipStack = null;
				LabelNode nodeRepresentingCurrentChoice = null;
				//add <empty> row
				if (showCurrentSelection)
				{
					if (m_cache != null)
						ownershipStack = GetOwnershipStack(currentObj);

					if (m_nullLabel.DisplayName != null)
						m_labelsTreeView.Nodes.Add(CreateLabelNode(m_nullLabel, m_displayUsageCheckBox.Checked));
				}

				var rgLabelNodes = new ArrayList();
				var rgOwnershipStacks = new Dictionary<ICmObject, Stack<ICmObject>>();
				if (m_chosenObjs != null)
				{
					foreach (ICmObject obj in m_chosenObjs)
					{
						if (obj != null)
							rgOwnershipStacks[obj] = GetOwnershipStack(obj);
					}
				}
				//	m_labelsTreeView.Nodes.AddRange(labels.AsObjectArray);
				foreach (ObjectLabel label in labels)
				{
					if (!WantNodeForLabel(label))
						continue;
					// notice that we are only adding the top-level notes now.
					// others will be added when the user expands them.
					LabelNode x = CreateLabelNode(label, m_displayUsageCheckBox.Checked);
					m_labelsTreeView.Nodes.Add(x);
					if (m_chosenObjs != null)
						x.Checked = m_chosenObjs.Contains(label.Object);

					//notice that we don't actually use the "stack-ness" of the stack.
					//if we did, we would have to worry about skipping the higher level owners, like
					//language project.
					//but just treat it as an array, we can ignore those issues.
					if (m_cache != null &&
						showCurrentSelection &&
						ownershipStack.Contains(label.Object))
					{
						nodeRepresentingCurrentChoice = x.AddChildrenAndLookForSelected(currentObj,
							ownershipStack, null);
					}
					if (m_cache != null &&
						m_chosenObjs != null)
					{
						foreach (ICmObject obj in m_chosenObjs)
						{
							if (obj == null)
								continue;
							var curOwnershipStack = rgOwnershipStacks[obj];
							if (curOwnershipStack.Contains(label.Object))
								rgLabelNodes.Add(x.AddChildrenAndLookForSelected(obj, curOwnershipStack, m_chosenObjs));
						}
					}
				}
				m_labelsTreeView.EndUpdate();

				// if for some reason we could not find it is smart way, go do it the painful way of
				// walking the entire tree, creating objects until we find it.
				// I'm not clear if we ever need this...the primary cover them would fail if the
				// labels were constructed in some way other than from an ownership hierarchy.
				if (showCurrentSelection)
				{
					m_labelsTreeView.SelectedNode = nodeRepresentingCurrentChoice ?? FindNodeFromObj(currentObj);
					if (m_labelsTreeView.SelectedNode != null)
					{
						m_labelsTreeView.SelectedNode.EnsureVisible();
						//for some reason, doesn't actually select it, so do this:
						m_labelsTreeView.SelectedNode.ForeColor = Color.Blue;
					}
				}
				else if (m_chosenObjs != null)
				{
					// Don't show a selection initially
					m_labelsTreeView.SelectedNode = null;
				}

				//important that we not do this sooner!
				m_labelsTreeView.BeforeExpand += m_labelsTreeView_BeforeExpand;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Wants the node for label.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool WantNodeForLabel(ObjectLabel label)
		{
			CheckDisposed();

			return true; // by default want all nodes.
		}
		/// <summary>
		/// Creates the label node.
		/// </summary>
		/// <param name="nol">The nol.</param>
		/// <param name="displayUsage">if set to <c>true</c> [display usage].</param>
		/// <returns></returns>
		protected virtual LabelNode CreateLabelNode(ObjectLabel nol, bool displayUsage)
		{
			return new LabelNode(nol, m_stylesheet, displayUsage);
		}

		/// <summary>
		/// Gets the ownership stack.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		protected Stack<ICmObject> GetOwnershipStack(ICmObject obj)
		{
			var stack = new Stack<ICmObject>();
			while (obj != null) //!m_cache.ClassIsOwnerless(hvo))
			{
				obj = obj.Owner;
				if (obj != null)
					stack.Push(obj);
			}
			return stack;
		}

		#region we-might-not-need-this-stuff-anymore
		/// <summary>
		/// Finds the node at root level.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		protected LabelNode FindNodeAtRootLevel(ICmObject obj)
		{
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				if (node.Label.Object == obj)
				{
					return node;
				}
			}
			return null;
		}

		/// <summary>
		/// Finds the node from the object.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		protected LabelNode FindNodeFromObj(ICmObject obj)
		{
			// is it in the root level of choices?
			LabelNode n = FindNodeAtRootLevel(obj);
			if (n != null)
				return n;

			// enhance: this is the simplest thing that would possibly work, but it is slow!
			// see the #if'd-out code for the beginnings of a smarter algorithm which would only
			// expand what needed to be expanded.
			// No, so go looking deeper (and slower!)
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				n = FindNode(node, obj);
				if (n != null)
				{
					return n;
				}
			}

			// JohnT: it can fail, for example, if we have obsolete data in TestLangProj that
			// uses an item not in the list. See LT-1973.
			//Debug.Fail("object not found in the tree");
			return null;
		}

		/// <summary>
		/// Finds the node.
		/// </summary>
		/// <param name="searchNode">The search node.</param>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		protected LabelNode FindNode(LabelNode searchNode, ICmObject obj)
		{
			//is it me?
			if (searchNode.Label.Object == obj)
				return searchNode;

			//no, so look in my descendants
			searchNode.AddChildren(true, m_chosenObjs);
			foreach (LabelNode node in searchNode.Nodes)
			{
				LabelNode n = FindNode(node, obj);
				if (n!=null)
					return n;
			}
			return null;
		}
		#endregion

		/// <summary>
		/// returns the object that was selected, or null and cancelled.
		/// </summary>
		/// <remarks>
		/// will return a NullObjectLabel if the user chooses "empty".
		/// will return null if the user chose nothing (which will happen when showEmptyOption
		/// is false).
		/// </remarks>
		public ObjectLabel ChosenOne
		{
			get
			{
				CheckDisposed();
				return m_chosenLabel;
			}
		}

		/// <summary>
		/// returns true if the selected object was generated by executing a link.
		/// </summary>
		public bool LinkExecuted
		{
			get
			{
				CheckDisposed();
				return m_fLinkExecuted;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// use this to change the label for null to, for example, "&lt;not sure&gt;"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ObjectLabel NullLabel
		{
			get
			{
				CheckDisposed();
				return m_nullLabel;
			}
		}

		/// <summary>
		/// Returns the list of hvos for the chosen items.
		/// </summary>
		public IEnumerable<ICmObject> ChosenObjects
		{
			get
			{
				CheckDisposed();
				return m_newChosenObjs;
			}
		}

		/// <summary>
		/// Get or set the internal FdoCache value.
		/// </summary>
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
			set
			{
				CheckDisposed();
				m_cache = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if ( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReallySimpleListChooser));
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.m_labelsTreeView = new System.Windows.Forms.TreeView();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.m_lblLink2 = new System.Windows.Forms.LinkLabel();
			this.m_picboxLink2 = new System.Windows.Forms.PictureBox();
			this.m_imageList = new System.Windows.Forms.ImageList(this.components);
			this.m_lblLink1 = new System.Windows.Forms.LinkLabel();
			this.m_picboxLink1 = new System.Windows.Forms.PictureBox();
			this.m_lblExplanation = new System.Windows.Forms.Label();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.m_mainPanel = new System.Windows.Forms.Panel();
			this.m_viewPanel = new System.Windows.Forms.Panel();
			this.m_viewExtrasPanel = new System.Windows.Forms.Panel();
			this.m_ctrlClickLabel = new System.Windows.Forms.Label();
			this.m_helpBrowserButton = new System.Windows.Forms.Button();
			this.m_checkBoxPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.m_displayUsageCheckBox = new System.Windows.Forms.CheckBox();
			this.m_link2Panel = new System.Windows.Forms.FlowLayoutPanel();
			this.m_link1Panel = new System.Windows.Forms.FlowLayoutPanel();
			this.m_buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.m_splitContainer = new System.Windows.Forms.SplitContainer();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink1)).BeginInit();
			this.m_mainPanel.SuspendLayout();
			this.m_viewPanel.SuspendLayout();
			this.m_viewExtrasPanel.SuspendLayout();
			this.m_checkBoxPanel.SuspendLayout();
			this.m_link2Panel.SuspendLayout();
			this.m_link1Panel.SuspendLayout();
			this.m_buttonPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_splitContainer)).BeginInit();
			this.m_splitContainer.Panel1.SuspendLayout();
			this.m_splitContainer.SuspendLayout();
			this.SuspendLayout();
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			this.btnOK.Click += new System.EventHandler(this.OnOKClick);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// m_labelsTreeView
			//
			resources.ApplyResources(this.m_labelsTreeView, "m_labelsTreeView");
			this.m_labelsTreeView.FullRowSelect = true;
			this.m_labelsTreeView.HideSelection = false;
			this.m_labelsTreeView.Name = "m_labelsTreeView";
			this.m_labelsTreeView.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
			((System.Windows.Forms.TreeNode)(resources.GetObject("m_labelsTreeView.Nodes"))),
			((System.Windows.Forms.TreeNode)(resources.GetObject("m_labelsTreeView.Nodes1")))});
			this.m_labelsTreeView.ShowLines = false;
			this.m_labelsTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_labelsTreeView_AfterSelect);
			this.m_labelsTreeView.DoubleClick += new System.EventHandler(this.m_labelsTreeView_DoubleClick);
			//
			// m_lblLink2
			//
			resources.ApplyResources(this.m_lblLink2, "m_lblLink2");
			this.m_lblLink2.Name = "m_lblLink2";
			this.m_lblLink2.TabStop = true;
			this.m_lblLink2.VisitedLinkColor = System.Drawing.Color.Blue;
			this.m_lblLink2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_lblLink2_LinkClicked);
			//
			// m_picboxLink2
			//
			resources.ApplyResources(this.m_picboxLink2, "m_picboxLink2");
			this.m_picboxLink2.BackColor = System.Drawing.SystemColors.Control;
			this.m_picboxLink2.Name = "m_picboxLink2";
			this.m_picboxLink2.TabStop = false;
			//
			// m_imageList
			//
			this.m_imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList.ImageStream")));
			this.m_imageList.TransparentColor = System.Drawing.Color.Magenta;
			this.m_imageList.Images.SetKeyName(0, "");
			this.m_imageList.Images.SetKeyName(1, "Create Entry.ico");
			this.m_imageList.Images.SetKeyName(2, "HistoryBack.bmp");
			this.m_imageList.Images.SetKeyName(3, "HistoryForward.bmp");
			this.m_imageList.Images.SetKeyName(4, "FWPrint.bmp");
			//
			// m_lblLink1
			//
			resources.ApplyResources(this.m_lblLink1, "m_lblLink1");
			this.m_lblLink1.Name = "m_lblLink1";
			this.m_lblLink1.TabStop = true;
			this.m_lblLink1.VisitedLinkColor = System.Drawing.Color.Blue;
			this.m_lblLink1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_lblLink1_LinkClicked);
			//
			// m_picboxLink1
			//
			resources.ApplyResources(this.m_picboxLink1, "m_picboxLink1");
			this.m_picboxLink1.BackColor = System.Drawing.SystemColors.Control;
			this.m_picboxLink1.Name = "m_picboxLink1";
			this.m_picboxLink1.TabStop = false;
			//
			// m_lblExplanation
			//
			resources.ApplyResources(this.m_lblExplanation, "m_lblExplanation");
			this.m_lblExplanation.Name = "m_lblExplanation";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// m_mainPanel
			//
			this.m_mainPanel.Controls.Add(this.m_viewPanel);
			this.m_mainPanel.Controls.Add(this.m_viewExtrasPanel);
			this.m_mainPanel.Controls.Add(this.m_checkBoxPanel);
			this.m_mainPanel.Controls.Add(this.m_link2Panel);
			this.m_mainPanel.Controls.Add(this.m_lblExplanation);
			this.m_mainPanel.Controls.Add(this.m_link1Panel);
			this.m_mainPanel.Controls.Add(this.m_buttonPanel);
			resources.ApplyResources(this.m_mainPanel, "m_mainPanel");
			this.m_mainPanel.Name = "m_mainPanel";
			//
			// m_viewPanel
			//
			this.m_viewPanel.Controls.Add(this.m_labelsTreeView);
			resources.ApplyResources(this.m_viewPanel, "m_viewPanel");
			this.m_viewPanel.Name = "m_viewPanel";
			//
			// m_viewExtrasPanel
			//
			this.m_viewExtrasPanel.Controls.Add(this.m_ctrlClickLabel);
			this.m_viewExtrasPanel.Controls.Add(this.m_helpBrowserButton);
			resources.ApplyResources(this.m_viewExtrasPanel, "m_viewExtrasPanel");
			this.m_viewExtrasPanel.Name = "m_viewExtrasPanel";
			//
			// m_ctrlClickLabel
			//
			resources.ApplyResources(this.m_ctrlClickLabel, "m_ctrlClickLabel");
			this.m_ctrlClickLabel.Name = "m_ctrlClickLabel";
			//
			// m_helpBrowserButton
			//
			resources.ApplyResources(this.m_helpBrowserButton, "m_helpBrowserButton");
			this.m_helpBrowserButton.Name = "m_helpBrowserButton";
			this.m_helpBrowserButton.UseVisualStyleBackColor = true;
			this.m_helpBrowserButton.Click += new System.EventHandler(this.m_helpBrowserButton_Click);
			//
			// m_checkBoxPanel
			//
			this.m_checkBoxPanel.Controls.Add(this.m_displayUsageCheckBox);
			resources.ApplyResources(this.m_checkBoxPanel, "m_checkBoxPanel");
			this.m_checkBoxPanel.Name = "m_checkBoxPanel";
			//
			// m_displayUsageCheckBox
			//
			resources.ApplyResources(this.m_displayUsageCheckBox, "m_displayUsageCheckBox");
			this.m_displayUsageCheckBox.Name = "m_displayUsageCheckBox";
			this.m_displayUsageCheckBox.UseVisualStyleBackColor = true;
			this.m_displayUsageCheckBox.CheckedChanged += new System.EventHandler(this.m_displayUsageCheckBox_CheckedChanged);
			//
			// m_link2Panel
			//
			resources.ApplyResources(this.m_link2Panel, "m_link2Panel");
			this.m_link2Panel.Controls.Add(this.m_picboxLink2);
			this.m_link2Panel.Controls.Add(this.m_lblLink2);
			this.m_link2Panel.Name = "m_link2Panel";
			//
			// m_link1Panel
			//
			resources.ApplyResources(this.m_link1Panel, "m_link1Panel");
			this.m_link1Panel.Controls.Add(this.m_picboxLink1);
			this.m_link1Panel.Controls.Add(this.m_lblLink1);
			this.m_link1Panel.Name = "m_link1Panel";
			//
			// m_buttonPanel
			//
			this.m_buttonPanel.Controls.Add(this.buttonHelp);
			this.m_buttonPanel.Controls.Add(this.btnCancel);
			this.m_buttonPanel.Controls.Add(this.btnOK);
			resources.ApplyResources(this.m_buttonPanel, "m_buttonPanel");
			this.m_buttonPanel.Name = "m_buttonPanel";
			//
			// m_splitContainer
			//
			resources.ApplyResources(this.m_splitContainer, "m_splitContainer");
			this.m_splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.m_splitContainer.Name = "m_splitContainer";
			//
			// m_splitContainer.Panel1
			//
			this.m_splitContainer.Panel1.Controls.Add(this.m_mainPanel);
			this.m_splitContainer.Panel2Collapsed = true;
			//
			// ReallySimpleListChooser
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.m_splitContainer);
			this.Cursor = System.Windows.Forms.Cursors.Default;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ReallySimpleListChooser";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Activated += new System.EventHandler(this.SimpleListChooser_Activated);
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink1)).EndInit();
			this.m_mainPanel.ResumeLayout(false);
			this.m_mainPanel.PerformLayout();
			this.m_viewPanel.ResumeLayout(false);
			this.m_viewExtrasPanel.ResumeLayout(false);
			this.m_viewExtrasPanel.PerformLayout();
			this.m_checkBoxPanel.ResumeLayout(false);
			this.m_checkBoxPanel.PerformLayout();
			this.m_link2Panel.ResumeLayout(false);
			this.m_link2Panel.PerformLayout();
			this.m_link1Panel.ResumeLayout(false);
			this.m_link1Panel.PerformLayout();
			this.m_buttonPanel.ResumeLayout(false);
			this.m_splitContainer.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_splitContainer)).EndInit();
			this.m_splitContainer.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void HandleCommmandChoice(ChooserCommandNode node)
		{
			if (node != null)
			{
				var cmd = node.Tag as ChooserCommand;
				if (cmd != null)
				{
					if (cmd.ShouldCloseBeforeExecuting)
						Visible = false;
					m_chosenLabel = cmd.Execute();
				}
			}
		}

		private void OnOKClick(object sender, EventArgs e)
		{
			Persist();
			if (m_linkCmd != null)
			{
				Visible = false;
				m_chosenLabel = m_linkCmd.Execute();
				m_fLinkExecuted = true;
			}
			else if (m_labelsTreeView != null &&
				m_labelsTreeView.SelectedNode != null &&
				m_labelsTreeView.SelectedNode.Tag != null &&
				m_labelsTreeView.SelectedNode.Tag is ChooserCommand)
			{
				HandleCommmandChoice(m_labelsTreeView.SelectedNode as ChooserCommandNode);
			}
			// TODO: Do something similar for a selected item in a FlatListView.
			else
			{
				SetChosen();
			}
		}

		private void Persist()
		{
			if (m_persistProvider != null)
			{
				if (m_webBrowser != null)
				{
					m_propertyTable.SetProperty("SimpleListChooser-HelpBrowserSplitterDistance", m_splitContainer.SplitterDistance, true, true);
					m_persistProvider.PersistWindowSettings("SimpleListChooser-HelpBrowser", this);
				}
				else
				{
					m_persistProvider.PersistWindowSettings("SimpleListChooser", this);
				}
			}
		}

		private void SetChosen()
		{
			if (m_chosenObjs != null)
			{
				m_chosenLabel = null;
				if (m_labelsTreeView != null)
				{
					m_newChosenObjs = new List<ICmObject>();
					// Walk the tree of labels looking for Checked == true.  This allows us to
					// return an ordered list of hvos (sorted by list display order).
					for (int i = 0; i < m_labelsTreeView.Nodes.Count; ++i)
					{
						if (m_labelsTreeView.Nodes[i].Checked)
							m_newChosenObjs.Add(((LabelNode)m_labelsTreeView.Nodes[i]).Label.Object);
						CheckChildrenForChosen(((LabelNode)m_labelsTreeView.Nodes[i]));
					}
				}
				else
				{
					m_newChosenObjs = m_flvLabels.GetCheckedItems().ToList();
				}
			}
			else
			{
				if (m_labelsTreeView != null)
				{
					if (m_labelsTreeView.SelectedNode == null)
						m_chosenLabel = null;
					else
						m_chosenLabel = ((LabelNode)m_labelsTreeView.SelectedNode).Label;
				}
				else
				{
					int idx = m_flvLabels.SelectedIndex;
					m_chosenLabel = m_labels[idx];
				}
			}
		}

		private void CheckChildrenForChosen(LabelNode node)
		{
			if (node == null || node.Nodes == null)
				return;
			for (int i = 0; i < node.Nodes.Count; ++i)
			{
				var x = node.Nodes[i] as LabelNode;
				if (x != null)
				{
					if (x.Checked)
						m_newChosenObjs.Add(x.Label.Object);
					CheckChildrenForChosen(x);
				}
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Persist();
			m_chosenLabel = null;
		}

		private void m_labelsTreeView_DoubleClick(object sender, EventArgs e)
		{
			// When using checkboxes for multiple selections, ignore double clicks.
			if (!m_labelsTreeView.CheckBoxes)
				btnOK.PerformClick();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the BeforeExpand event of the m_labelsTreeView control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.TreeViewCancelEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void m_labelsTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			var node = (LabelNode)e.Node;
			using (new WaitCursor(this))
			{
				node.AddChildren(false, m_chosenObjs);
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="cmd"></param>
		public void AddChooserCommand(ChooserCommand cmd)
		{
			CheckDisposed();

			var node = new ChooserCommandNode(cmd);
			IWritingSystem defAnalWS = cmd.Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			string sFontName = defAnalWS.DefaultFontName;

			// TODO: need to get analysis font's size
			// and then set it to use underline:
			var font = new Font(sFontName, 10.0f, FontStyle.Italic);
			node.NodeFont = font;
			//node.ForeColor = Color.DarkGreen;
			m_labelsTreeView.Nodes.Insert(0, node);
		}

		private void m_lblLink1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (m_obj1 != null)
			{
				m_linkJump = m_obj1 as FwLinkArgs;
				m_linkCmd = m_obj1 as ChooserCommand;
				if (m_linkJump != null)
				{
					btnCancel.PerformClick();
					// No result as such, but we'll perform a jump.
					DialogResult = DialogResult.Ignore;
				}
				else if (m_linkCmd != null)
				{
					btnOK.PerformClick();
				}
				else
				{
					Debug.Assert(m_linkJump != null || m_linkCmd != null);
				}
			}
		}

		private void m_lblLink2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (m_obj2 != null)
			{
				m_linkJump = m_obj2 as FwLinkArgs;
				m_linkCmd = m_obj2 as ChooserCommand;
				if (m_linkJump != null)
				{
					btnCancel.PerformClick();
					// No result as such, but we'll perform a jump.
					DialogResult = DialogResult.Ignore;
				}
				else if (m_linkCmd != null)
				{
					btnOK.PerformClick();
				}
				else
				{
					Debug.Assert(m_linkJump != null && m_linkCmd != null);
				}
			}

		}

		// We have to allow selections in order to allow keyboard support.  See LT-3068.
		// This event handler prevents selections when checkboxes are in use since the checks
		// show the possibly mulitple active selections.
		//private void m_labelsTreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		//{
		//	if (m_labelsTreeView.CheckBoxes)
		//		e.Cancel = true;
		//}

		private void m_labelsTreeView_BeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			e.Cancel = !NodeIsEnabled(e.Node as LabelNode);
		}

		private bool NodeIsEnabled(LabelNode node)
		{
			if (node != null)
				return node.Enabled;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected class ChooserCommandNode : TreeNode
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="ChooserCommandNode"/> class.
			/// </summary>
			/// <param name="cmd">The CMD.</param>
			/// --------------------------------------------------------------------------------
			public ChooserCommandNode(ChooserCommand cmd)
			{
				Tag = cmd;
				Text = cmd.Label;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the command.
			/// </summary>
			/// <value>The command.</value>
			/// --------------------------------------------------------------------------------
			public ChooserCommand Command
			{
				get
				{
					return (ChooserCommand) Tag;
				}
			}
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", m_helpTopic);
		}

		/// <summary>
		/// Is m_helpTopic a valid help topic?
		/// </summary>
		private bool helpTopicIsValid(string helpStr)
		{
			return (m_helpTopicProvider != null && !String.IsNullOrEmpty(helpStr))
				&& (m_helpTopicProvider.GetHelpString(helpStr) != null);
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			CheckDisposed();

			m_helpTopic = helpTopic;
			InitHelp();

		}

		/// <summary>
		/// Bring up a chooser for selecting a natural class, and insert it into the string
		/// representation stored in the rootbox.  This static method is used by
		/// SIL.FieldWorks.Common.Framework.DetailControls.PhoneEnvReferenceSlice and
		/// SIL.FieldWorks.XWorks.MorphologyEditor.PhEnvStrRepresentationSlice.
		/// </summary>
		/// <param name="rootb"></param>
		/// <param name="cache"></param>
		/// <param name="persistenceProvider"></param>
		/// <param name="propertyTable"></param>
		/// <param name="publisher"></param>
		/// <returns></returns>
		public static bool ChooseNaturalClass(IVwRootBox rootb, FdoCache cache,
			IPersistenceProvider persistenceProvider, IPropertyTable propertyTable, IPublisher publisher)
		{
			IEnumerable<ObjectLabel> labels = ObjectLabel.CreateObjectLabels(cache,
				cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS.Cast<ICmObject>(), "",
				cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id);

			using (var chooser = new ReallySimpleListChooser(persistenceProvider,
				labels, "NaturalClass", propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				string sTitle = null;
				string sDescription = null;
				string sJumpLabel = null;
				sTitle = StringTable.Table.GetString("kstidChooseNaturalClass",
					"Linguistics/Morphology/NaturalClassChooser");
				sDescription = StringTable.Table.GetString("kstidNaturalClassListing",
					"Linguistics/Morphology/NaturalClassChooser");
				sJumpLabel = StringTable.Table.GetString("kstidGotoNaturalClassList",
					"Linguistics/Morphology/NaturalClassChooser");
				if (string.IsNullOrEmpty(sTitle) || sTitle == "kstidChooseNaturalClass")
					sTitle = XMLViewsStrings.ksChooseNaturalClass;
				if (string.IsNullOrEmpty(sDescription) || sDescription == "kstidNaturalClassListing")
					sDescription = XMLViewsStrings.ksNaturalClassDesc;
				if (string.IsNullOrEmpty(sJumpLabel) || sJumpLabel == "kstidGotoNaturalClassList")
					sJumpLabel = XMLViewsStrings.ksEditNaturalClasses;
				chooser.Cache = cache;
				chooser.SetObjectAndFlid(0, 0);
				chooser.SetHelpTopic("khtpChooseNaturalClass");
				chooser.InitializeRaw(propertyTable, publisher, sTitle, sDescription, sJumpLabel,
					"naturalClassEdit", "analysis vernacular");

				DialogResult res = chooser.ShowDialog();
				if (DialogResult.Cancel == res)
					return true;
				if (chooser.HandleAnyJump())
					return true;
				if (chooser.ChosenOne != null)
				{
					var pnc = (IPhNaturalClass) chooser.ChosenOne.Object;
					ITsString tss = pnc.Abbreviation.BestAnalysisVernacularAlternative;
					string sName = tss.Text;
					string sIns = String.Format("[{0}]", sName);
					int wsPending = cache.DefaultVernWs;
					IVwRootSite site = rootb.Site;
					IVwGraphics vg = null;
					if (site != null)
						vg = site.get_ScreenGraphics(rootb);
					rootb.OnTyping(vg, sIns, VwShiftStatus.kfssNone, ref wsPending);
				}
			}
			return true;
		}

		/// <summary>
		/// Make the selection from the given object.
		/// </summary>
		/// <param name="obj">The obj.</param>
		public void MakeSelection(ICmObject obj)
		{
			CheckDisposed();

			m_labelsTreeView.SelectedNode = FindNodeFromObj(obj);
			if (m_labelsTreeView.SelectedNode == null)
				m_labelsTreeView.SelectedNode = m_labelsTreeView.Nodes[0];

			if (m_labelsTreeView.SelectedNode != null)
			{
				m_labelsTreeView.SelectedNode.EnsureVisible();
				//for some reason, doesn't actually select it, so do this:
				m_labelsTreeView.SelectedNode.ForeColor = Color.Blue;
			}
		}

		/// <summary>
		/// Return the database id of the currently selected node.
		/// </summary>
		public ICmObject SelectedObject
		{
			get
			{
				CheckDisposed();

				if (m_labelsTreeView.SelectedNode != null)
				{
					LabelNode node = m_labelsTreeView.SelectedNode as LabelNode;
					if (node != null)
					{
						ObjectLabel label = node.Label;
						if (label != null)
							return label.Object;
					}
				}
#if __MonoCS__
				// On Mono, m_labelsTreeView.SelectedNode is somehow cleared between OnOKClick
				// and getting SelectedObject from the caller.  (See FWNX-853.)
				if (m_chosenLabel != null)
					return m_chosenLabel.Object;
#endif
				return null;
			}
		}

		/// <summary>
		/// Hides the m_displayUsageCheckBox control.
		/// </summary>
		public void HideDisplayUsageCheckBox()
		{
			m_displayUsageCheckBox.Enabled = false;
			m_displayUsageCheckBox.Visible = false;
			if (m_displayUsageCheckBox.Checked)
				m_displayUsageCheckBox.Checked = false;
		}
	}

	/// <summary>
	/// A LeafChooser means we are trying to choose items at the leaves of a hierarchy.
	/// The prototypical case is choosing Inflection classes. So there is a tree of CmPossibilities,
	/// (actually PartOfSpeechs) any of which may have leaves in the InflectionClasses property.
	/// We want to display only the possibilities that have inflection classes (either themselves
	/// or some descendant), plus the inflection classes themselves.
	/// </summary>
	public class LeafChooser : ReallySimpleListChooser
	{
		private readonly int m_leafFlid;
		/// <summary>
		/// Initializes a new instance of the <see cref="LeafChooser"/> class.
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="chosenObjs">The chosen objects.</param>
		/// <param name="leafFlid">The leaf flid.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public LeafChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, FdoCache cache,
			IEnumerable<ICmObject> chosenObjs, int leafFlid, IHelpTopicProvider helpTopicProvider)
			: base (persistProvider, fieldName, cache, chosenObjs, helpTopicProvider)
		{
			m_leafFlid = leafFlid;

			// Normally done by the base class constructor, but requires m_leafFlid to be set, so
			// we made a special constructor to finesse things.
			FinishConstructor(labels);
		}

		/// <summary>
		/// Creates the label node.
		/// </summary>
		/// <param name="nol">The nol.</param>
		/// <param name="displayUsage"><c>true</c> if usage statistics will be displayed; otherwise, <c>false</c>.</param>
		/// <returns></returns>
		protected override LabelNode CreateLabelNode(ObjectLabel nol, bool displayUsage)
		{
			return new LeafLabelNode(nol, m_stylesheet, displayUsage, m_leafFlid);
		}
		/// <summary>
		/// In this class we want only those nodes that have interesting leaves somewhere.
		/// Unfortunately this method is duplicated on LeafLabelNode. I can't see a clean way to
		/// avoid this.
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		public override bool WantNodeForLabel(ObjectLabel label)
		{
			CheckDisposed();

			if (!base.WantNodeForLabel(label)) // currently does nothing, but just in case...
				return false;
			if (HasLeaves(label))
				return true;
			foreach (ObjectLabel labelSub in label.SubItems)
				if (WantNodeForLabel(labelSub))
					return true;
			return false;
		}
		private bool HasLeaves(ObjectLabel label)
		{
			return label.Cache.DomainDataByFlid.get_VecSize(label.Object.Hvo, m_leafFlid) > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected class LeafLabelNode : LabelNode
		{
			private readonly int m_leafFlid;

			/// <summary>
			/// Initializes a new instance of the <see cref="LeafLabelNode"/> class.
			/// </summary>
			/// <param name="label">The label.</param>
			/// <param name="stylesheet">The stylesheet.</param>
			/// <param name="displayUsage"><c>true</c> if usage statistics will be displayed; otherwise, <c>false</c>.</param>
			/// <param name="leafFlid">The leaf flid.</param>
			public LeafLabelNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage, int leafFlid)
				: base(label, stylesheet, displayUsage)
			{
				m_leafFlid = leafFlid;
			}

			/// <summary>
			/// Adds the secondary nodes.
			/// </summary>
			/// <param name="node">The node.</param>
			/// <param name="nodes">The nodes.</param>
			/// <param name="chosenObjs">The chosen objects.</param>
			public override void AddSecondaryNodes(LabelNode node, TreeNodeCollection nodes, IEnumerable<ICmObject> chosenObjs)
			{
				AddSecondaryNodesAndLookForSelected(node, nodes, null, null, null, chosenObjs);
			}

			/// <summary>
			/// Add secondary nodes to tree at nodes (and check any that occur in rghvoChosen),
			/// and return the one whose hvo is hvoToSelect, or nodeRepresentingCurrentChoice
			/// if none match.
			/// </summary>
			/// <param name="node">node to be added</param>
			/// <param name="nodes">where to add it</param>
			/// <param name="nodeRepresentingCurrentChoice">The node representing current choice.</param>
			/// <param name="objToSelect">The obj to select.</param>
			/// <param name="ownershipStack">The ownership stack.</param>
			/// <param name="chosenObjs">The chosen objects.</param>
			/// <returns></returns>
			public override LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node, TreeNodeCollection nodes,
				LabelNode nodeRepresentingCurrentChoice, ICmObject objToSelect, Stack<ICmObject> ownershipStack, IEnumerable<ICmObject> chosenObjs)
			{
				LabelNode result = nodeRepresentingCurrentChoice; // result unless we match hvoToSelect
				var label = (ObjectLabel) Tag;
				var sda = (ISilDataAccessManaged) label.Cache.DomainDataByFlid;
				var objs = from hvo in sda.VecProp(label.Object.Hvo, m_leafFlid)
						   select label.Cache.ServiceLocator.GetObject(hvo);
				var secLabels = ObjectLabel.CreateObjectLabels(label.Cache, objs,
					"ShortNameTSS", "analysis vernacular"); // Enhance JohnT: may want to make these configurable one day...
				foreach (ObjectLabel secLabel in secLabels)
				{
					// Perversely, we do NOT want a LeafLabelNode for the leaves, because their HVOS are the leaf type,
					// and therefore objects that do NOT possess the leaf property!
					var secNode = new LabelNode(secLabel, m_stylesheet, true);
					if (chosenObjs != null)
						secNode.Checked = chosenObjs.Contains(secLabel.Object);
					node.Nodes.Add(secNode);
					if (secLabel.Object == objToSelect)
						result = secNode;
				}
				return result;
			}

			/// <summary>
			/// In this class we want only those nodes that have interesting leaves somewhere.
			/// Unfortunately this method is duplicated on LeafChooser. I can't see a clean way to
			/// avoid this.
			/// </summary>
			/// <param name="label"></param>
			/// <returns></returns>
			public override bool WantNodeForLabel(ObjectLabel label)
			{
				if (!base.WantNodeForLabel(label)) // currently does nothing, but just in case...
					return false;
				if (HasLeaves(label))
					return true;
				foreach (ObjectLabel labelSub in label.SubItems)
					if (WantNodeForLabel(labelSub))
						return true;
				return false;
			}

			private bool HasLeaves(ObjectLabel label)
			{
				return label.Cache.DomainDataByFlid.get_VecSize(label.Object.Hvo, m_leafFlid) > 0;
			}
		}
	}

}
