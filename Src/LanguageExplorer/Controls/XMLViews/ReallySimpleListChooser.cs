// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using LanguageExplorer.Areas;
using SIL.Code;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Xml;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary></summary>
	public class ReallySimpleListChooser : Form
	{
		/// <summary />
		protected Button btnOK;
		/// <summary />
		protected Button btnCancel;
		/// <summary />
		protected TreeView m_labelsTreeView;
		private bool m_fFlatList = false;
		private bool m_fSortLabels = true;
		private bool m_fSortLabelsSet = false;	// set true if explicitly assigned.
		private List<ICmObject> m_objs;
		private FlatListView m_flvLabels;
		private List<ObjectLabel> m_labels;
		/// <summary />
		protected ToolTip toolTip1;
		private IContainer components;
		/// <summary />
		protected IPersistenceProvider m_persistProvider;
		/// <summary />
		protected ImageList m_imageList;
		/// <summary />
		protected LinkLabel m_lblLink2;
		/// <summary />
		protected PictureBox m_picboxLink2;
		/// <summary />
		protected LinkLabel m_lblLink1;
		/// <summary />
		protected PictureBox m_picboxLink1;
		/// <summary />
		protected Label m_lblExplanation;

		/// <summary>
		/// True to prevent choosing more than one item.
		/// </summary>
		public bool Atomic { get; set; }

		/// <summary />
		protected NullObjectLabel m_nullLabel;
		/// <summary />
		protected int m_hvoObject;
		/// <summary />
		protected int m_flidObject;
		/// <summary />
		protected IPropertyTable m_propertyTable;
		/// <summary />
		protected IPublisher m_publisher;
		/// <summary />
		protected ISubscriber m_subscriber;
		/// <summary />
		protected string m_fieldName;
		private int m_cLinksShown;
		private object m_obj2;
		private FwLinkArgs m_linkJump;
		private ChooserCommand m_linkCmd;

		private Guid m_guidLink = Guid.Empty;
		private readonly HashSet<ICmObject> m_chosenObjs;
		private List<ICmObject> m_newChosenObjs;
		private bool m_fEnableCtrlCheck; // true to allow ctrl-click on check box to select all children.

		private Button buttonHelp;
		private HelpProvider m_helpProvider;
		private string m_helpTopic;
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
		/// <summary />
		protected Panel m_viewPanel;
		/// <summary />
		protected FlowLayoutPanel m_link2Panel;
		/// <summary />
		protected Panel m_viewExtrasPanel;
		private Label m_ctrlClickLabel;
		/// <summary />
		protected FlowLayoutPanel m_link1Panel;

		private ToolStrip m_helpBrowserStrip;
		private ToolStripButton m_backButton;
		private ToolStripButton m_forwardButton;
		/// <summary />
		protected FlowLayoutPanel m_checkBoxPanel;
		private CheckBox m_displayUsageCheckBox;

		private ToolStripButton m_printButton;

		/// <summary>
		/// Constructor for use with designer
		/// </summary>
		public ReallySimpleListChooser()
		{
			m_cLinksShown = 0;
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);
		}

		/// <summary>
		/// (Deprecated) constructor for use with changing or setting a value
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentObj"></param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		public ReallySimpleListChooser(IPersistenceProvider persistProvider, IHelpTopicProvider helpTopicProvider, IEnumerable<ObjectLabel> labels, ICmObject currentObj, string fieldName)
		{
			m_cLinksShown = 0;
			Init(null, helpTopicProvider, persistProvider, fieldName, labels, currentObj, XMLViewsStrings.ksEmpty, null);
		}

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
		public ReallySimpleListChooser(LcmCache cache, IHelpTopicProvider helpTopicProvider, IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName, string nullLabel, IVwStylesheet stylesheet)
		{
			m_cLinksShown = 0;
			Init(cache, helpTopicProvider, persistProvider, fieldName, labels, currentObj, nullLabel, stylesheet);
		}

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
		public ReallySimpleListChooser(LcmCache cache, IHelpTopicProvider helpTopicProvider, IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName, string nullLabel)
		{
			m_cLinksShown = 0;
			Init(cache, helpTopicProvider, persistProvider, fieldName, labels, currentObj, nullLabel, null);
		}

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
		public ReallySimpleListChooser(LcmCache cache, IHelpTopicProvider helpTopicProvider,
			IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels,
			ICmObject currentObj, string fieldName)
		{
			m_cLinksShown = 0;
			Init(cache, helpTopicProvider, persistProvider, fieldName, labels, currentObj, XMLViewsStrings.ksEmpty, null);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReallySimpleListChooser"/> class.
		/// </summary>
		private void Init(LcmCache cache, IHelpTopicProvider helpTopicProvider,
			IPersistenceProvider persistProvider, string fieldName,
			IEnumerable<ObjectLabel> labels, ICmObject currentObj, string nullLabel,
			IVwStylesheet stylesheet)
		{
			m_stylesheet = stylesheet;
			m_helpTopicProvider = helpTopicProvider;
			m_nullLabel = new NullObjectLabel(cache) {DisplayName = nullLabel};
			Cache = cache;
			m_persistProvider = persistProvider;
			m_fieldName = fieldName;
			m_fFlatList = IsListFlat(labels);
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);

			m_persistProvider?.RestoreWindowSettings("SimpleListChooser", this);

			// It's easier to localize a format string than code that pieces together a string.
			Text = fieldName == XMLViewsStrings.ksPublishIn || fieldName == XMLViewsStrings.ksShowAsHeadwordIn ? fieldName : string.Format(XMLViewsStrings.ksChooseX, fieldName);

			LoadTree(labels, currentObj, true);
		}

		private static bool IsListFlat(IEnumerable<ObjectLabel> labels)
		{
			if (!labels.Any())
			{
				return false;
			}

			foreach (var label in labels)
			{
				if (label.HaveSubItems)
				{
					return false;
				}
			}
			return true;
		}

		private void InitHelp()
		{
			// Only enable the Help button if we have a help topic for the fieldName
			if (buttonHelp.Enabled || m_helpProvider != null || m_helpTopicProvider == null)
			{
				return;
			}

			buttonHelp.Enabled = helpTopicIsValid(m_helpTopic);
			if (!buttonHelp.Enabled)
			{
				return;
			}

			if (m_helpProvider == null)
			{
				m_helpProvider = new HelpProvider();
			}
			m_helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		public ReallySimpleListChooser(IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels, string fieldName, IHelpTopicProvider helpTopicProvider)
			: this(persistProvider, labels, fieldName, null, helpTopicProvider)
		{
		}

		/// <summary>
		/// constructor for use with adding a new value (and stylesheet)
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="stylesheet">for getting right height for text</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		public ReallySimpleListChooser(IPersistenceProvider persistProvider, IEnumerable<ObjectLabel> labels, string fieldName, IVwStylesheet stylesheet,
			IHelpTopicProvider helpTopicProvider)
		{
			m_stylesheet = stylesheet;
			m_cLinksShown = 0;
			m_helpTopicProvider = helpTopicProvider;
			m_persistProvider = persistProvider;
			m_fieldName = fieldName;
			m_nullLabel = new NullObjectLabel();
			m_fFlatList = IsListFlat(labels);
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);

			m_persistProvider?.RestoreWindowSettings("SimpleListChooser", this);

			// It's easier to localize a format string than code that pieces together a string.
			Text = fieldName == XMLViewsStrings.ksPublishIn || fieldName == XMLViewsStrings.ksShowAsHeadwordIn ? fieldName : string.Format(XMLViewsStrings.ksChooseX, fieldName);

			LoadTree(labels, null, false);
		}

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
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, LcmCache cache,
			IEnumerable<ICmObject> chosenObjs, IHelpTopicProvider helpTopicProvider) :
			this(persistProvider, labels, fieldName, cache, chosenObjs, IsListSorted(labels), helpTopicProvider)
		{
		}

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
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			IEnumerable<ObjectLabel> labels, string fieldName, LcmCache cache,
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
		protected void FinishConstructor(IEnumerable<ObjectLabel> labels)
		{
			// Note: anything added here might need to be added to the LeafChooser constructor also.
			LoadTree(labels, null, false);
		}

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
		protected ReallySimpleListChooser(IPersistenceProvider persistProvider,
			string fieldName, LcmCache cache, IEnumerable<ICmObject> chosenObjs,
			IHelpTopicProvider helpTopicProvider)
		{
			Cache = cache;
			m_persistProvider = persistProvider;
			m_helpTopicProvider = helpTopicProvider;
			m_cLinksShown = 0;
			m_fieldName = fieldName;
			m_nullLabel = new NullObjectLabel();
			InitializeComponent();
			AccessibleNameCreator.AddNames(this);

			m_persistProvider?.RestoreWindowSettings("SimpleListChooser", this);

			// It's easier to localize a format string than code that pieces together a string.
			Text = fieldName == XMLViewsStrings.ksPublishIn || fieldName ==  XMLViewsStrings.ksShowAsHeadwordIn ? fieldName : string.Format(XMLViewsStrings.ksChooseX, fieldName);

			m_labelsTreeView.CheckBoxes = true;
			m_labelsTreeView.AfterCheck += m_labelsTreeView_AfterCheck;
			// We have to allow selections in order to allow keyboard support.  See LT-3068.
			m_labelsTreeView.BeforeCheck += m_labelsTreeView_BeforeCheck;
			m_chosenObjs = new HashSet<ICmObject>();
			if (chosenObjs != null)
			{
				m_chosenObjs.UnionWith(chosenObjs);
			}
		}

		/// <summary>
		/// Check whether the list should be sorted.  See LT-5149.
		/// </summary>
		private static bool IsListSorted(IEnumerable<ObjectLabel> labels)
		{
			if (!labels.Any())
			{
				return true;
			}
			var labelObj = labels.First().Object;
			var owner = labelObj.Owner;
			return !(owner is ICmPossibilityList) || (owner as ICmPossibilityList).IsSorted;
		}

		/// <summary />
		public void SetObjectAndFlid(int hvo, int flid)
		{
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
				var sText = value;
				Text = sText.IndexOf("{0}") >= 0 && (STextParam != null || TextParamHvo != 0)
					? string.Format(sText, TextParam)
					: sText;
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
				var sText = value;
				if (sText.IndexOf("{0}") >= 0 && (STextParam != null || TextParamHvo != 0))
				{
					m_lblExplanation.Text = string.Format(sText, TextParam);
				}
				else
				{
					m_lblExplanation.Text = sText;
				}
				m_lblExplanation.Visible = true;
			}
		}

		/// <summary>
		/// Set the text and picture for one of the two possible "link" items in the dialog.  If
		/// two links are used, set the lower one first.  The tree chooser shrinks as needed to
		/// show only the link(s) used.
		/// </summary>
		public void AddLink(string sText, LinkType type, object obj)
		{
			// Any links past two not only assert, but then quietly do nothing.
			Debug.Assert(m_cLinksShown < 2);
			// Don't show a link if it's back to this object's owner
			// Note LexEntry no longer has an owner. But m_hvoObject can be 0 (FWR-2886).
			var objt = (m_hvoObject != 0) ? Cache.ServiceLocator.GetObject(m_hvoObject) : null;
			var ownedHvo = (objt != null && objt.Owner != null) ? objt.Owner.Hvo : 0;
			if (TextParamHvo != 0 && m_hvoObject != 0 && TextParamHvo == ownedHvo)
			{
				return;
			}
			// A goto link is also inappropriate if it's back to the object we are editing itself.
			if (TextParamHvo == m_hvoObject && TextParamHvo != 0 && type == LinkType.kGotoLink)
			{
				return;
			}
			if (m_cLinksShown >= 2)
			{
				return;
			}
			if (sText.IndexOf("{0}") >= 0 && (STextParam != null || TextParamHvo != 0))
			{
				sText = string.Format(sText, TextParam);
			}
			++m_cLinksShown;
			if (m_cLinksShown == 1)
			{
				m_lblLink1.Text = sText;
				if (type != LinkType.kSimpleLink)
				{
					m_picboxLink1.Image = m_imageList.Images[(int)type];
				}
				Obj1 = obj;
				m_link1Panel.Visible = true;
			}
			else if (m_cLinksShown == 2)
			{
				m_lblLink2.Text = sText;
				if (type != LinkType.kSimpleLink)
				{
					m_picboxLink2.Image = m_imageList.Images[(int)type];
				}
				m_obj2 = obj;
				m_link2Panel.Visible = true;
			}
		}

		/// <summary>
		/// Show extra radio buttons for Add/Replace (and possibly Remove)
		/// </summary>
		public void ShowFuncButtons()
		{
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
			m_fEnableCtrlCheck = true;
			m_ctrlClickLabel.Visible = true;
			m_viewExtrasPanel.Visible = true;
		}

		/// <summary>
		/// Called after a check box is checked (or unchecked).
		/// </summary>
		private void m_labelsTreeView_AfterCheck(object sender, TreeViewEventArgs e)
		{
			var clickNode = (LabelNode)e.Node;
			if (!clickNode.Enabled)
			{
				return;
			}
			if (m_fEnableCtrlCheck && ModifierKeys == Keys.Control && !Atomic)
			{
				using (new WaitCursor())
				{
					if (e.Action != TreeViewAction.Unknown)
					{
						// The original check, not recursive.
						clickNode.AddChildren(true, new HashSet<ICmObject>()); // All have to exist to get checked/unchecked
						if (!clickNode.IsExpanded)
						{
							clickNode.Expand(); // open up at least one level to show effects.
						}
					}
					foreach (TreeNode node in clickNode.Nodes)
					{
						node.Checked = e.Node.Checked; // and recursively checks children.
					}
				}
			}
			if (ForbidNoItemChecked)
			{
				btnOK.Enabled = AnyItemChecked(m_labelsTreeView.Nodes);
			}
			if (Atomic && clickNode.Checked)
			{
				var checkedNodes = new HashSet<TreeNode>();
				foreach (TreeNode child in m_labelsTreeView.Nodes)
				{
					CollectCheckedNodes(child, checkedNodes);
				}
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
		private static void CollectCheckedNodes(TreeNode root, HashSet<TreeNode> checkedNodes)
		{
			if (root.Checked)
			{
				checkedNodes.Add(root);
			}

			foreach (TreeNode child in root.Nodes)
			{
				CollectCheckedNodes(child, checkedNodes);
			}
		}

		/// <summary>
		/// Set to prevent the user closing the dialog with nothing selected.
		/// Note that it is assumed that something is selected when the dialog opens.
		/// Setting this will not disable the OK button until the user changes something.
		/// </summary>
		public bool ForbidNoItemChecked { get; set; }

		private static bool AnyItemChecked(TreeNodeCollection nodes)
		{
			foreach (TreeNode child in nodes)
			{
				if (child.Checked || AnyItemChecked(child.Nodes))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// True if we should replace items.
		/// </summary>
		public bool ReplaceMode => m_ReplaceButton != null && m_ReplaceButton.Checked;

		/// <summary>
		/// Gets a value indicating whether [remove mode].
		/// </summary>
		public bool RemoveMode => m_RemoveButton != null && m_RemoveButton.Checked;

		internal ListMatchOptions ListMatchMode
		{
			get
			{
				if (m_AllButton != null && m_AllButton.Checked)
				{
					return ListMatchOptions.All;
				}

				if (m_NoneButton.Checked)
				{
					return ListMatchOptions.None;
				}

				if (m_ExactButton != null && m_ExactButton.Checked)
				{
					return ListMatchOptions.Exact;
				}
				return ListMatchOptions.Any;
			}
			set
			{
				switch (value)
				{
					case ListMatchOptions.All:
						m_AllButton.Checked = true;
						break;
					case ListMatchOptions.None:
						m_NoneButton.Checked = true;
						break;
					case ListMatchOptions.Exact:
						m_ExactButton.Checked = true;
						break;
					default:
						m_AnyButton.Checked = true;
						break;
				}
			}
		}

		/// <summary>
		/// Set the database id of the object which serves as a possible parameter to
		/// InstructionalText, Title, or a link label.
		/// </summary>
		public int TextParamHvo { get; set; }

		/// <summary>
		/// Get the name which serves as a parameter to InstructionalText, Title, or link
		/// labels.  If computing from an HVO, also save its CmObject GUID for possible later
		/// use in a FwLink object as a side-effect,
		/// </summary>
		public string TextParam
		{
			set
			{
				STextParam = value;
			}
			get
			{
				if (STextParam != null)
				{
					return STextParam;
				}
				if (TextParamHvo != 0)
				{
					var co = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(TextParamHvo);
					STextParam = co.ShortName;
					// We want this link Guid value only if label/text hint that it's needed.
					// (This requirement is subject to change without much notice!)
					m_guidLink = co.Guid;
					return STextParam;
				}
				return XMLViewsStrings.ksQuestionMarks;
			}
		}

		/// <summary>
		/// Initialize the behavior from an XML configuration node.
		/// </summary>
		public void InitializeExtras(XElement configNode, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			Guard.AgainstNull(propertyTable, nameof(propertyTable));
			Guard.AgainstNull(publisher, nameof(publisher));
			Guard.AgainstNull(subscriber, nameof(subscriber));

			Debug.Assert(Cache != null);
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_subscriber = subscriber;
			var ws = Cache.DefaultAnalWs;
			SetFontFromWritingSystem(ws);

			if (configNode == null)
			{
				return;
			}
			var node = configNode.Element("chooserInfo") ?? GenerateChooserInfoForCustomNode(configNode);
			if (node == null)
			{
				return;
			}
			var sTextParam = XmlUtils.GetOptionalAttributeValue(node, "textparam", "owner").ToLower();
			// The default case ("owner") is handled by the caller setting TextParamHvo.
			if (sTextParam == "vernws")
			{
				CoreWritingSystemDefinition co = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
				STextParam = co.DisplayLabel;
			}
			var sFlid = XmlUtils.GetOptionalAttributeValue(node, "flidTextParam");
			if (sFlid != null)
			{
				try
				{
					var flidTextParam = int.Parse(sFlid, CultureInfo.InvariantCulture);
					if (flidTextParam != 0)
					{
						var sda = Cache.DomainDataByFlid;
						TextParamHvo = sda.get_ObjectProp(m_hvoObject, flidTextParam);
					}
				}
				catch
				{
					// Ignore any badness here.
				}
			}

			var sTitle = XmlUtils.GetOptionalAttributeValue(node, "title");
			if (sTitle != null)
			{
				Title = sTitle;
			}
			var sText = XmlUtils.GetOptionalAttributeValue(node, "text");
			if (sText != null)
			{
				InstructionalText = sText;
			}
			var linkNodes = node.Elements("chooserLink").ToList();
			Debug.Assert(linkNodes != null && linkNodes.Count <= 2);
			for (var i = linkNodes.Count - 1; i >= 0 ; --i)
			{
				var sType = XmlUtils.GetOptionalAttributeValue(linkNodes[i], "type", "goto").ToLower();
				var sLabel = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(linkNodes[i], "label", null));
				switch (sType)
				{
					case "goto":
					{
						var sTool = XmlUtils.GetOptionalAttributeValue(linkNodes[i], "tool");
						if (sLabel != null && sTool != null)
						{
							AddLink(sLabel, LinkType.kGotoLink, new FwLinkArgs(sTool, m_guidLink));
						}
						break;
					}
					case "dialog":
					{
						var sDialog = XmlUtils.GetOptionalAttributeValue(linkNodes[i], "dialog");
						// TODO: make use of sDialog somehow to create a ChooserCommand object.
						// TODO: maybe even better, use a new SubDialog object that allows us
						// to call the specified dialog, then return to this dialog, adding
						// a newly created object to the list of chosen items (or making the
						// newly created object the chosen item).
						if (sLabel != null && sDialog != null)
						{
							AddLink(sLabel, LinkType.kDialogLink, null);
						}
						break;
					}
					case "simple":
					{
						var sTool = XmlUtils.GetOptionalAttributeValue(linkNodes[i], "tool");
						if (sLabel != null && sTool != null)
						{
							AddSimpleLink(sLabel, sTool, linkNodes[i]);
						}
						break;
					}
				}
			}
			var sGuiControl = XmlUtils.GetOptionalAttributeValue(node, "guicontrol");
			// Replace the tree view control with a browse view control if it's both desirable
			// and feasible.
			if (m_fFlatList && !string.IsNullOrEmpty(sGuiControl))
			{
				ReplaceTreeView(sGuiControl);
			}
			var useHelpBrowser = XmlUtils.GetOptionalBooleanAttributeValue(node, "helpBrowser", false);
			if (useHelpBrowser)
			{
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
		private XElement GenerateChooserInfoForCustomNode(XElement configNode)
		{
			var editor = XmlUtils.GetOptionalAttributeValue(configNode, "editor");
			if (configNode.Name != "slice" || editor != "autoCustom" || TextParamHvo == 0)
			{
				return null;
			}
			var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(TextParamHvo);
			var list = obj as ICmPossibilityList;
			if (list == null)
			{
				return null;
			}
			var listFlid = list.OwningFlid;
			string listField = null;
			string listOwnerClass = null;
			if (list.Owner != null)
			{
				listField = Cache.MetaDataCacheAccessor.GetFieldName(listFlid);
				listOwnerClass = Cache.MetaDataCacheAccessor.GetClassName(listFlid / 1000);
			}
			var itemClass = Cache.MetaDataCacheAccessor.GetClassName(list.ItemClsid);
			// We need to dynamically figure out a tool for this list.
			string sTool = null;
			XElement chooserNode = null;
			var windowConfig = m_propertyTable.GetValue<XElement>("WindowConfiguration");
			if (windowConfig != null)
			{
				// The easiest search is through various jump command parameters.
				foreach (var xnCommand in windowConfig.Elements("/window/commands/command"))
				{
					var xnParam = xnCommand.Element("parameters");
					if (xnParam != null)
					{
						if (XmlUtils.GetOptionalAttributeValue(xnParam, "className") == itemClass &&
							XmlUtils.GetOptionalAttributeValue(xnParam, "ownerClass") == listOwnerClass &&
							XmlUtils.GetOptionalAttributeValue(xnParam, "ownerField") == listField)
						{
							sTool = XmlUtils.GetOptionalAttributeValue(xnParam, "tool");
							if (!string.IsNullOrEmpty(sTool))
							{
								break;
							}
						}
					}
				}
				// Couldn't find anything in the commands, try the record lists and tools.
				if (string.IsNullOrEmpty(sTool))
				{
					sTool = ScanToolsAndLists(windowConfig, listOwnerClass, listField);
				}
			}

			if (string.IsNullOrEmpty(sTool))
			{
				return null;
			}
			sTool = XmlUtils.MakeSafeXmlAttribute(sTool);
			var bldr = new StringBuilder();
			bldr.AppendLine("<chooserInfo>");
			var label = list.Name.UserDefaultWritingSystem.Text;
			if (string.IsNullOrEmpty(label) || label == list.Name.NotFoundTss.Text)
			{
				label = list.Name.BestAnalysisVernacularAlternative.Text;
			}
			label = XmlUtils.MakeSafeXmlAttribute(label);
			bldr.AppendFormat("<chooserLink type=\"goto\" label=\"Edit the {0} list\" tool=\"{1}\"/>", label, sTool);
			bldr.AppendLine();
			bldr.AppendLine("</chooserInfo>");
			var doc = XDocument.Parse(bldr.ToString());
			chooserNode = doc.Root;
			return chooserNode;
		}

		private string ScanToolsAndLists(XElement windowConfig, string listOwnerClass, string listField)
		{
			foreach (var xnItem in windowConfig.Elements("/window/lists/list/item"))
			{
				foreach (var xnClerk in xnItem.Elements("parameters/clerks/clerk"))
				{
					var recordListId = XmlUtils.GetOptionalAttributeValue(xnClerk, "id");
					if (string.IsNullOrEmpty(recordListId))
					{
						continue;
					}
					var xnList = xnClerk.Element("recordList");
					if (xnList == null)
					{
						continue;
					}
					if (XmlUtils.GetOptionalAttributeValue(xnList, "owner") == listOwnerClass && XmlUtils.GetOptionalAttributeValue(xnList, "property") == listField)
					{
						foreach (var xnTool in xnItem.Elements("parameters/tools/tool"))
						{
							var sTool = XmlUtils.GetOptionalAttributeValue(xnTool, "value");
							if (string.IsNullOrEmpty(sTool))
							{
								continue;
							}
							var xnParam = xnTool.Element("control/parameters/control/parameters");
							if (xnParam == null)
							{
								continue;
							}
							var recordList = XmlUtils.GetOptionalAttributeValue(xnParam, "clerk");
							if (recordList == recordListId)
							{
								return sTool;
							}
						}
					}
				}
			}
			return null;
		}

		private void InitHelpBrowser()
		{
			var splitterDistance = m_splitContainer.Width;
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
				m_helpBrowserButton.Text = $"<<< {XMLViewsStrings.ksLess}";
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
			var helpTopic = GetHelpTopic(e.Url);
			if (helpTopic == null)
			{
				return;
			}
			helpTopic = helpTopic.ToLowerInvariant();
			// ENHANCE (DamienD): cache HelpId to object mappings in a dictionary, so we
			// don't have to search through all objects to find a match
			if (m_labelsTreeView != null)
			{
				var stack = new Stack<LabelNode>(m_labelsTreeView.Nodes.Cast<LabelNode>());
				while (stack.Count > 0)
				{
					var node = stack.Pop();
					var pos = node.Label.Object as ICmPossibility;
					var curHelpTopic = pos?.HelpId;
					if (curHelpTopic != null && curHelpTopic.ToLowerInvariant() == helpTopic)
					{
						m_labelsTreeView.SelectedNode = node;
						break;
					}
					node.AddChildren(true, m_chosenObjs);
					foreach (TreeNode childNode in node.Nodes)
					{
						var labelNode = childNode as LabelNode;
						if (labelNode != null)
						{
							stack.Push(labelNode);
						}
					}
				}
			}
			else
			{
				for (var i = 0; i < m_labels.Count; i++)
				{
					var pos = m_labels[i].Object as ICmPossibility;
					var curHelpTopic = pos?.HelpId;
					if (curHelpTopic != null && curHelpTopic.ToLowerInvariant() == helpTopic)
					{
						m_flvLabels.SelectedIndex = i;
						break;
					}
				}
			}
		}

		private void NavigateToSelectedTopic()
		{
			if (m_webBrowser == null)
			{
				return;
			}

			ObjectLabel selectedLabel = null;
			if (m_labelsTreeView != null)
			{
				if (m_labelsTreeView.SelectedNode != null)
				{
					selectedLabel = ((LabelNode)m_labelsTreeView.SelectedNode).Label;
				}
			}
			else
			{
				var idx = m_flvLabels.SelectedIndex;
				selectedLabel = m_labels[idx];
			}

			if (selectedLabel == null)
			{
				return;
			}
			var pos = selectedLabel.Object as ICmPossibility;
			if (pos != null)
			{
				var helpFile = pos.OwningList.HelpFile;
#if __MonoCS__
// Force Linux to use combined Ocm/OcmFrame files
					if (helpFile == "Ocm.chm")
						helpFile = "OcmFrame";
#endif
				var helpTopic = pos.HelpId;
				if (!string.IsNullOrEmpty(helpFile) && !string.IsNullOrEmpty(helpTopic))
				{
					var curHelpTopic = GetHelpTopic(m_webBrowser.Url);
					if (curHelpTopic != null && helpTopic.ToLowerInvariant() == curHelpTopic.ToLowerInvariant())
					{
						return;
					}
					if (!Path.IsPathRooted(helpFile))
					{
						// Helps are part of the installed code files.  See FWR-1002.
						var helpsPath = Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps");
						helpFile = Path.Combine(helpsPath, helpFile);
					}
#if __MonoCS__
// remove file extension, we need folder of the same name with the htm files
					helpFile = helpFile.Replace(".chm","");
					string url = string.Format("{0}/{1}.htm", helpFile, helpTopic.ToLowerInvariant());
#else
					var url = $"its:{helpFile}::/{helpTopic}.htm";
#endif
					m_webBrowser.Navigate(url);
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

		private static string GetHelpTopic(Uri url)
		{
			if (url == null)
			{
				return null;
			}
			var urlStr = url.ToString();
#if __MonoCS__
			int startIndex = urlStr.IndexOf("OcmFrame/");
			if (startIndex == -1)
				return null;
			startIndex += 9;
#else
			var startIndex = urlStr.IndexOf("::/");
			if (startIndex == -1)
			{
				return null;
			}
			startIndex += 3;
#endif
			var endIndex = urlStr.IndexOf(".htm", startIndex);
			return endIndex == -1 ? null : urlStr.Substring(startIndex, endIndex - startIndex);
		}

		private void GenerateDefaultPage(ITsString tssTitle, ITsString tssDesc)
		{
			var ws = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
			var userFont = ws.DefaultFontName;

			string title, titleFont;
			if (tssTitle != null)
			{
				title = tssTitle.Text;
				var wsHandle = TsStringUtils.GetWsAtOffset(tssTitle, 0);
				ws = Cache.ServiceLocator.WritingSystemManager.Get(wsHandle);
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
					var wsHandle = TsStringUtils.GetWsAtOffset(tssDesc, 0);
					ws = Cache.ServiceLocator.WritingSystemManager.Get(wsHandle);
					descFont = ws.DefaultFontName;
				}
			}
			else
			{
				desc = XMLViewsStrings.ksNoDesc;
				descFont = userFont;
			}

			var htmlElem = new XElement("html",
				new XElement("head", new XElement("title", title)),
				new XElement("body",
					new XElement("font", new XAttribute("face", titleFont), new XElement("h2", title)),
					new XElement("font", new XAttribute("face", userFont), new XElement("h3", XMLViewsStrings.ksShortDesc)),
					new XElement("font", new XAttribute("face", descFont), new XElement("p", desc))));
			if (MiscUtils.IsMono)
			{
				var tempfile = Path.Combine(FileUtils.GetTempFile("htm"));
			var xDocument = new XDocument(htmlElem);
			xDocument.Save(tempfile);
			m_webBrowser.Navigate(tempfile);
				if (FileUtils.FileExists(tempfile))
				{
					FileUtils.Delete(tempfile);
				}
			tempfile = null;
			}
#if !__MonoCS__
			else
			{
				m_webBrowser.DocumentText = htmlElem.ToString();
			}
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
			m_helpBrowserButton.Text = $"<<< {XMLViewsStrings.ksLess}";
		}

		private void CollapseHelpBrowser()
		{
			if (m_splitContainer.Panel2Collapsed)
			{
				return;
			}

			ClientSize = new Size(m_splitContainer.SplitterDistance, ClientSize.Height);
			m_splitContainer.Panel2Collapsed = true;
			m_splitContainer.IsSplitterFixed = true;
			m_helpBrowserButton.Text = $"{XMLViewsStrings.ksMore} >>>";
		}

		private void m_helpBrowserButton_Click(object sender, EventArgs e)
		{
			if (m_splitContainer.Panel2Collapsed)
			{
				ExpandHelpBrowser();
			}
			else
			{
				CollapseHelpBrowser();
			}
		}

		private void m_displayUsageCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (m_labelsTreeView == null)
			{
				return;
			}
			using (new WaitCursor(this))
			{
				m_labelsTreeView.BeginUpdate();
				var stack = new Stack<LabelNode>(m_labelsTreeView.Nodes.Cast<LabelNode>());
				while (stack.Count > 0)
				{
					var node = stack.Pop();
					node.DisplayUsage = m_displayUsageCheckBox.Checked;
					foreach (TreeNode childNode in node.Nodes)
					{
						var labelNode = childNode as LabelNode;
						if (labelNode != null)
						{
							stack.Push(labelNode);
						}
					}
				}
				m_labelsTreeView.EndUpdate();
			}
		}

		/// <summary>
		/// Access for outsiders who don't call InitializExtras.
		/// </summary>
		public void ReplaceTreeView(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, string sGuiControl)
		{
			if (!m_fFlatList)
			{
				return;
			}

			if (m_propertyTable == null)
			{
				m_propertyTable = propertyTable;
			}
			if (m_publisher == null)
			{
				m_publisher = publisher;
			}
			if (m_subscriber == null)
			{
				m_subscriber = subscriber;
			}
			ReplaceTreeView(sGuiControl);
		}

		/// <summary>
		/// This does the tricky work of replace the tree view control with a browse view
		/// control.
		/// </summary>
		private void ReplaceTreeView(string sGuiControl)
		{
			if (!m_fFlatList || string.IsNullOrEmpty(sGuiControl))
			{
				return;
			}
			var doc = XDocument.Parse(XMLViewsStrings.SimpleChooserParameters);
			var guiControlElement = doc.Root.Elements("guicontrol").First(cg => cg.Attribute("id").Value == sGuiControl);
			var configNode = guiControlElement?.Element("parameters");
			if (configNode == null)
			{
				return;
			}
			m_flvLabels = new FlatListView
			{
				Dock = DockStyle.Fill,
				TabStop = m_labelsTreeView.TabStop,
				TabIndex = m_labelsTreeView.TabIndex
			};
			m_flvLabels.SelectionChanged += m_flvLabels_SelectionChanged;
			IVwStylesheet stylesheet = FwUtils.StyleSheetFromPropertyTable(m_propertyTable);
			m_flvLabels.Initialize(Cache, stylesheet, m_propertyTable, m_publisher, m_subscriber, configNode, m_objs);
			if (m_chosenObjs != null)
			{
				m_flvLabels.SetCheckedItems(m_chosenObjs);
			}
			m_viewPanel.Controls.Remove(m_labelsTreeView);
			m_viewPanel.Controls.Add(m_flvLabels);
			m_labelsTreeView?.Dispose();
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
		public void SetFontForDialog(int[] wss, IVwStylesheet stylesheet, ILgWritingSystemFactory wsf)
		{
			m_stylesheet = stylesheet;
			var tmpFont = FontHeightAdjuster.GetFontForNormalStyle(wss[0], stylesheet, wsf);
			var font = tmpFont;
			try
			{
				for (var i = 1; i < wss.Length; i++)
				{
					using (var other = FontHeightAdjuster.GetFontForNormalStyle(wss[i], stylesheet, wsf))
					{
						// JohnT: this is a compromise. I don't think it is guaranteed that a font with the
						// same SizeInPoints will be the same height. But it should be about the same,
						// and until we implement a proper multilingual treeview replacement, I'm not sure we
						// can do much better than this.
						if (other.Height > font.Height)
						{
							if (font != tmpFont)
							{
								font.Dispose();
							}
							font = new Font(font.FontFamily, Math.Max(font.SizeInPoints, other.SizeInPoints));
						}
					}
				}

				m_labelsTreeView.Font = font;
			}
			finally
			{
				if (font != tmpFont)
				{
					tmpFont.Dispose();
				}
			}
		}

		private void SetFontFromWritingSystem(int ws)
		{
			var oldFont = m_labelsTreeView.Font;
			IVwStylesheet stylesheet = FwUtils.StyleSheetFromPropertyTable(m_propertyTable);
			var font = FontHeightAdjuster.GetFontForNormalStyle(ws, stylesheet, Cache.WritingSystemFactory);
			var maxPoints = font.SizeInPoints;
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				if (node.NodeFont != oldFont && node.NodeFont != null) // overridden because of vernacular text
				{
					node.ResetVernacularFont(Cache.WritingSystemFactory, Cache.DefaultUserWs, stylesheet);
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
				{
					node.NodeFont = font;
				}
			}
			oldFont.Dispose();
		}

		/// <summary>
		/// Initializes the raw.
		/// </summary>
		public void InitializeRaw(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber, string sTitle, string sText, string sGotoLabel, string sTool, string sWs)
		{
			Guard.AgainstNull(propertyTable, nameof(propertyTable));
			Guard.AgainstNull(publisher, nameof(publisher));
			Guard.AgainstNull(subscriber, nameof(subscriber));

			Debug.Assert(Cache != null);
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_subscriber = subscriber;
			if (sTitle != null)
			{
				Title = sTitle;
			}

			if (sText != null)
			{
				InstructionalText = sText;
			}
			if (sGotoLabel != null && sTool != null)
			{
				AddLink(sGotoLabel, LinkType.kGotoLink, new FwLinkArgs(sTool, m_guidLink));
			}
			var ws = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			SetFontFromWritingSystem(ws);
		}

		/// <summary />
		protected virtual void AddSimpleLink(string sLabel, string sTool, XElement node)
		{
			switch (sTool)
			{
				default:
					// TODO: Handle other cases as they arise.
					AddLink(sLabel, LinkType.kSimpleLink, null);
					break;
			}
		}

		/// <summary>
		/// Gets the hvo of highest POS.
		/// </summary>
		protected int GetHvoOfHighestPOS(int startHvo, out string sTopPOS)
		{
			var posHvo = 0;
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
		/// If the user clicked on a link label, publish a message to jump to that
		/// location in the program.
		/// </summary>
		public bool HandleAnyJump()
		{
			if (m_publisher != null && m_linkJump != null)
			{
				LinkHandler.PublishFollowLinkMessage(m_publisher, m_linkJump);
				return true;
			}
			return false;
		}

		/// <summary>
		/// If the user clicked on a link label, publish a message to jump to that
		/// location in the program.
		/// </summary>
		public bool HandleAnyJump(IPublisher publisher)
		{
			if (publisher != null && m_linkJump != null)
			{
				LinkHandler.PublishFollowLinkMessage(m_publisher, m_linkJump);
				return true;
			}
			return false;
		}

		private void SimpleListChooser_Activated(object sender, EventArgs e)
		{
			if (m_labelsTreeView != null)
			{
				m_labelsTreeView.Focus();
			}
			else
			{
				m_flvLabels.Focus();
			}
		}

		/// <summary>
		/// Overridden to defeat the standard .NET behavior of adjusting size by
		/// screen resolution. That is bad for a list chooser because we remember the size,
		/// and if we remember the enlarged size, it just keeps growing.
		/// If we defeat it, it may look a bit small the first time at high resolution,
		/// but at least it will stay the size the user sets.
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			var size = Size;
			base.OnLoad(e);
			if (Size != size)
			{
				Size = size;
			}
		}

		/// <summary>
		/// Loads the tree.
		/// </summary>
		protected void LoadTree(IEnumerable<ObjectLabel> labels, ICmObject currentObj, bool showCurrentSelection)
		{
			Debug.Assert(showCurrentSelection? (m_chosenObjs == null) : (currentObj == null), "If showEmptyOption is false, currentHvo should be zero, since it is meaningless");

			if (m_fFlatList)
			{
				m_labels = labels.ToList();
				m_objs = labels.Select(label => label.Object).ToList();
			}
			using (new WaitCursor())
			{
				m_labelsTreeView.BeginUpdate();
				m_labelsTreeView.Nodes.Clear();

				// if m_fSortLabels is true, we'll sort the labels alphabetically, using dumb English sort.
				// otherwise, we'll keep the labels in their given order.
				if (!m_fSortLabelsSet && Cache != null)
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
					if (Cache != null)
					{
						ownershipStack = GetOwnershipStack(currentObj);
					}

					if (m_nullLabel.DisplayName != null)
					{
						m_labelsTreeView.Nodes.Add(CreateLabelNode(m_nullLabel, m_displayUsageCheckBox.Checked));
					}
				}

				var rgLabelNodes = new ArrayList();
				var rgOwnershipStacks = new Dictionary<ICmObject, Stack<ICmObject>>();
				if (m_chosenObjs != null)
				{
					foreach (var obj in m_chosenObjs)
					{
						if (obj != null)
						{
							rgOwnershipStacks[obj] = GetOwnershipStack(obj);
						}
					}
				}
				//	m_labelsTreeView.Nodes.AddRange(labels.AsObjectArray);
				foreach (ObjectLabel label in labels)
				{
					if (!WantNodeForLabel(label))
					{
						continue;
					}
					// notice that we are only adding the top-level notes now.
					// others will be added when the user expands them.
					var x = CreateLabelNode(label, m_displayUsageCheckBox.Checked);
					m_labelsTreeView.Nodes.Add(x);
					if (m_chosenObjs != null)
					{
						x.Checked = m_chosenObjs.Contains(label.Object);
					}

					//notice that we don't actually use the "stack-ness" of the stack.
					//if we did, we would have to worry about skipping the higher level owners, like
					//language project.
					//but just treat it as an array, we can ignore those issues.
					if (Cache != null && showCurrentSelection && ownershipStack.Contains(label.Object))
					{
						nodeRepresentingCurrentChoice = x.AddChildrenAndLookForSelected(currentObj, ownershipStack, null);
					}
					if (Cache != null && m_chosenObjs != null)
					{
						foreach (var obj in m_chosenObjs)
						{
							if (obj == null)
							{
								continue;
							}
							var curOwnershipStack = rgOwnershipStacks[obj];
							if (curOwnershipStack.Contains(label.Object))
							{
								rgLabelNodes.Add(x.AddChildrenAndLookForSelected(obj, curOwnershipStack, m_chosenObjs));
							}
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

		/// <summary>
		/// Wants the node for label.
		/// </summary>
		public virtual bool WantNodeForLabel(ObjectLabel label)
		{
			return true; // by default want all nodes.
		}

		/// <summary>
		/// Creates the label node.
		/// </summary>
		protected virtual LabelNode CreateLabelNode(ObjectLabel nol, bool displayUsage)
		{
			return new LabelNode(nol, m_stylesheet, displayUsage);
		}

		/// <summary>
		/// Gets the ownership stack.
		/// </summary>
		protected Stack<ICmObject> GetOwnershipStack(ICmObject obj)
		{
			var stack = new Stack<ICmObject>();
			while (obj != null)
			{
				obj = obj.Owner;
				if (obj != null)
				{
					stack.Push(obj);
				}
			}
			return stack;
		}

#region we-might-not-need-this-stuff-anymore
		/// <summary>
		/// Finds the node at root level.
		/// </summary>
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
		protected LabelNode FindNodeFromObj(ICmObject obj)
		{
			// is it in the root level of choices?
			var n = FindNodeAtRootLevel(obj);
			if (n != null)
			{
				return n;
			}

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
		protected LabelNode FindNode(LabelNode searchNode, ICmObject obj)
		{
			//is it me?
			if (searchNode.Label.Object == obj)
			{
				return searchNode;
			}

			//no, so look in my descendants
			searchNode.AddChildren(true, m_chosenObjs);
			foreach (LabelNode node in searchNode.Nodes)
			{
				var n = FindNode(node, obj);
				if (n != null)
				{
					return n;
				}
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
		public ObjectLabel ChosenOne { get; protected set; }

		/// <summary>
		/// returns true if the selected object was generated by executing a link.
		/// </summary>
		public bool LinkExecuted { get; protected set; }

		/// <summary>
		/// use this to change the label for null to, for example, "&lt;not sure&gt;"
		/// </summary>
		public ObjectLabel NullLabel => m_nullLabel;

		/// <summary>
		/// Returns the list of hvos for the chosen items.
		/// </summary>
		public IEnumerable<ICmObject> ChosenObjects => m_newChosenObjs;

		/// <summary>
		/// Get or set the internal LcmCache value.
		/// </summary>
		public LcmCache Cache { get; set; }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if ( disposing )
			{
				components?.Dispose();
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
			var cmd = node?.Tag as ChooserCommand;
			if (cmd == null)
			{
				return;
			}

			if (cmd.ShouldCloseBeforeExecuting)
			{
				Visible = false;
			}
			ChosenOne = cmd.Execute();
		}

		private void OnOKClick(object sender, EventArgs e)
		{
			Persist();
			if (m_linkCmd != null)
			{
				Visible = false;
				ChosenOne = m_linkCmd.Execute();
				LinkExecuted = true;
			}
			else if (m_labelsTreeView?.SelectedNode?.Tag is ChooserCommand)
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
			if (m_persistProvider == null)
			{
				return;
			}
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

		private void SetChosen()
		{
			if (m_chosenObjs != null)
			{
				ChosenOne = null;
				if (m_labelsTreeView != null)
				{
					m_newChosenObjs = new List<ICmObject>();
					// Walk the tree of labels looking for Checked == true.  This allows us to
					// return an ordered list of hvos (sorted by list display order).
					for (var i = 0; i < m_labelsTreeView.Nodes.Count; ++i)
					{
						if (m_labelsTreeView.Nodes[i].Checked)
						{
							m_newChosenObjs.Add(((LabelNode)m_labelsTreeView.Nodes[i]).Label.Object);
						}
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
					ChosenOne = ((LabelNode) m_labelsTreeView.SelectedNode)?.Label;
				}
				else
				{
					ChosenOne = m_labels[m_flvLabels.SelectedIndex];
				}
			}
		}

		private void CheckChildrenForChosen(LabelNode node)
		{
			if (node?.Nodes == null)
			{
				return;
			}
			for (var i = 0; i < node.Nodes.Count; ++i)
			{
				var x = node.Nodes[i] as LabelNode;
				if (x != null)
				{
					if (x.Checked)
					{
						m_newChosenObjs.Add(x.Label.Object);
					}
					CheckChildrenForChosen(x);
				}
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Persist();
			ChosenOne = null;
		}

		private void m_labelsTreeView_DoubleClick(object sender, EventArgs e)
		{
			// When using checkboxes for multiple selections, ignore double clicks.
			if (!m_labelsTreeView.CheckBoxes)
			{
				btnOK.PerformClick();
			}
		}

		/// <summary>
		/// Handles the BeforeExpand event of the m_labelsTreeView control.
		/// </summary>
		protected void m_labelsTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			var node = (LabelNode)e.Node;
			using (new WaitCursor(this))
			{
				node.AddChildren(false, m_chosenObjs);
			}
		}

		/// <summary />
		public void AddChooserCommand(ChooserCommand cmd)
		{
			var node = new ChooserCommandNode(cmd);
			var defAnalWS = cmd.Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			var sFontName = defAnalWS.DefaultFontName;
			// TODO: need to get analysis font's size
			// and then set it to use underline:
			var font = new Font(sFontName, 10.0f, FontStyle.Italic);
			node.NodeFont = font;
			//node.ForeColor = Color.DarkGreen;
			m_labelsTreeView.Nodes.Insert(0, node);
		}

		private void m_lblLink1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (Obj1 == null)
			{
				return;
			}
			m_linkJump = Obj1 as FwLinkArgs;
			m_linkCmd = Obj1 as ChooserCommand;
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

		private void m_lblLink2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (m_obj2 == null)
			{
				return;
			}
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

		private void m_labelsTreeView_BeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			e.Cancel = !NodeIsEnabled(e.Node as LabelNode);
		}

		private bool NodeIsEnabled(LabelNode node)
		{
			return node == null || node.Enabled;
		}

		/// <summary />
		private sealed class ChooserCommandNode : TreeNode
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="ChooserCommandNode"/> class.
			/// </summary>
			public ChooserCommandNode(ChooserCommand cmd)
			{
				Tag = cmd;
				Text = cmd.Label;
			}

			/// <summary>
			/// Gets the command.
			/// </summary>
			public ChooserCommand Command => (ChooserCommand)Tag;
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
			return m_helpTopicProvider != null && !string.IsNullOrEmpty(helpStr) && m_helpTopicProvider.GetHelpString(helpStr) != null;
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			m_helpTopic = helpTopic;
			InitHelp();
		}

		/// <summary>
		/// Bring up a chooser for selecting a natural class, and insert it into the string
		/// representation stored in the rootbox.  This static method is used by
		/// LanguageExplorer.Controls.DetailControls.PhoneEnvReferenceSlice and
		/// LanguageExplorer.Areas.PhEnvStrRepresentationSlice.
		/// </summary>
		public static bool ChooseNaturalClass(IVwRootBox rootb, LcmCache cache, IPersistenceProvider persistenceProvider, IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			var labels = ObjectLabel.CreateObjectLabels(cache,
				cache.LanguageProject.PhonologicalDataOA.NaturalClassesOS, "",
				cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id);

			using (var chooser = new ReallySimpleListChooser(persistenceProvider, labels, "NaturalClass", propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
			{
				var sTitle = StringTable.Table.GetString("kstidChooseNaturalClass", "Linguistics/Morphology/NaturalClassChooser");
				var sDescription = StringTable.Table.GetString("kstidNaturalClassListing", "Linguistics/Morphology/NaturalClassChooser");
				var sJumpLabel = StringTable.Table.GetString("kstidGotoNaturalClassList", "Linguistics/Morphology/NaturalClassChooser");
				if (string.IsNullOrEmpty(sTitle) || sTitle == "kstidChooseNaturalClass")
				{
					sTitle = XMLViewsStrings.ksChooseNaturalClass;
				}

				if (string.IsNullOrEmpty(sDescription) || sDescription == "kstidNaturalClassListing")
				{
					sDescription = XMLViewsStrings.ksNaturalClassDesc;
				}

				if (string.IsNullOrEmpty(sJumpLabel) || sJumpLabel == "kstidGotoNaturalClassList")
				{
					sJumpLabel = XMLViewsStrings.ksEditNaturalClasses;
				}
				chooser.Cache = cache;
				chooser.SetObjectAndFlid(0, 0);
				chooser.SetHelpTopic("khtpChooseNaturalClass");
				chooser.InitializeRaw(propertyTable, publisher, subscriber, sTitle, sDescription, sJumpLabel, AreaServices.NaturalClassEditMachineName, "analysis vernacular");

				var res = chooser.ShowDialog();
				if (DialogResult.Cancel == res)
				{
					return true;
				}

				if (chooser.HandleAnyJump())
				{
					return true;
				}

				if (chooser.ChosenOne == null)
				{
					return true;
				}
				var pnc = (IPhNaturalClass) chooser.ChosenOne.Object;
				var tss = pnc.Abbreviation.BestAnalysisVernacularAlternative;
				var sName = tss.Text;
				var sIns = $"[{sName}]";
				var wsPending = cache.DefaultVernWs;
				var site = rootb.Site;
				IVwGraphics vg = null;
				if (site != null)
				{
					vg = site.get_ScreenGraphics(rootb);
				}
				rootb.OnTyping(vg, sIns, VwShiftStatus.kfssNone, ref wsPending);
			}
			return true;
		}

		/// <summary>
		/// Make the selection from the given object.
		/// </summary>
		public void MakeSelection(ICmObject obj)
		{
			m_labelsTreeView.SelectedNode = FindNodeFromObj(obj) ?? m_labelsTreeView.Nodes[0];

			if (m_labelsTreeView.SelectedNode == null)
			{
				return;
			}
			m_labelsTreeView.SelectedNode.EnsureVisible();
			//for some reason, doesn't actually select it, so do this:
			m_labelsTreeView.SelectedNode.ForeColor = Color.Blue;
		}

		/// <summary>
		/// Return the database id of the currently selected node.
		/// </summary>
		public ICmObject SelectedObject
		{
			get
			{
				var node = m_labelsTreeView.SelectedNode as LabelNode;
				var label = node?.Label;
				if (label != null)
				{
					return label.Object;
				}
#if __MonoCS__
				// On Mono, m_labelsTreeView.SelectedNode is somehow cleared between OnOKClick
				// and getting SelectedObject from the caller.  (See FWNX-853.)
				if (ChosenOne != null)
					return ChosenOne.Object;
#endif
				return null;
			}
		}

		public object Obj1 { get; set; }

		public string STextParam { get; set; }

		/// <summary>
		/// Hides the m_displayUsageCheckBox control.
		/// </summary>
		public void HideDisplayUsageCheckBox()
		{
			m_displayUsageCheckBox.Enabled = false;
			m_displayUsageCheckBox.Visible = false;
			if (m_displayUsageCheckBox.Checked)
			{
				m_displayUsageCheckBox.Checked = false;
			}
		}
	}
}