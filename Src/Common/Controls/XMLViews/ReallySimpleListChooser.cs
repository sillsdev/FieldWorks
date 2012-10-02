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
// File: ReallySimpleListChooser.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Text;
using System.Globalization;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FdoUi;			// for FwLink (in FdoUiLowLevel assembly)
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for ReallySimpleListChooser.
	/// </summary>
	public class ReallySimpleListChooser : Form, IFWDisposable
	{
		/// <summary></summary>
		protected ObjectLabel m_chosenLabel;
		/// <summary></summary>
		protected System.Windows.Forms.Button btnOK;
		/// <summary></summary>
		protected System.Windows.Forms.Button btnCancel;
		/// <summary></summary>
		protected System.Windows.Forms.TreeView m_labelsTreeView;
		private bool m_fFlatList = false;
		private bool m_fSortLabels = true;
		private bool m_fSortLabelsSet = false;	// set true if explicitly assigned.
		private List<int> m_rghvo;
		private FlatListView m_flvLabels;
		private ObjectLabelCollection m_labels;
		/// <summary></summary>
		protected System.Windows.Forms.ToolTip toolTip1;
		/// <summary></summary>
		protected IContainer components;
		/// <summary></summary>
		protected IPersistenceProvider m_persistProvider;
		/// <summary></summary>
		protected System.Windows.Forms.ImageList m_imageList;
		/// <summary></summary>
		protected System.Windows.Forms.LinkLabel m_lblLink2;
		/// <summary></summary>
		protected System.Windows.Forms.PictureBox m_picboxLink2;
		/// <summary></summary>
		protected System.Windows.Forms.LinkLabel m_lblLink1;
		/// <summary></summary>
		protected System.Windows.Forms.PictureBox m_picboxLink1;
		/// <summary></summary>
		protected System.Windows.Forms.Label m_lblExplanation;
		/// <summary></summary>
		protected FdoCache m_cache;

		/// <summary></summary>
		protected bool m_fLinkExecuted = false;

		/// <summary></summary>
		protected ObjectLabel m_nullLabel =new NullObjectLabel();
		/// <summary></summary>
		protected int m_hvoObject;
		/// <summary></summary>
		protected int m_flidObject;

		/// <summary></summary>
		protected XCore.Mediator m_mediator = null;
		/// <summary></summary>
		protected string m_fieldName = null;
		private Point m_locTreeViewOrig;
		private int m_nInstTextHeight = 0;
		private int m_cLinksShown = 0;
		private int m_nLink1Height = 0;
		private int m_nLink2Height = 0;
		private object m_obj1 = null;
		private object m_obj2 = null;
		private FwLink m_linkJump = null;
		private ChooserCommand m_linkCmd = null;
		private string m_sTextParam = null;
		/// <summary></summary>
		protected int m_hvoTextParam = 0;
		private Guid m_guidLink = Guid.Empty;
		private List<int> m_rghvoChosen = null;
		private List<int> m_rghvoNewChosen = null;
		private bool m_fEnableCtrlCheck; // true to allow ctrl-click on check box to select all children.
		private bool m_fForbidNoItemChecked; // true to disable OK when nothing is checked.

		private System.Windows.Forms.Button buttonHelp;
		private System.Windows.Forms.HelpProvider helpProvider;
		private String m_helpTopic = null;

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
		}

		/// <summary>
		/// (Deprecated) constructor for use with changing or setting a value
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="currentHvo">use zero if empty</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, int currentHvo, string fieldName)
		{
			Init(null, persistProvider, fieldName, labels, currentHvo, XMLViewsStrings.ksEmpty, null);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentHvo">use zero if empty</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="nullLabel">The null label.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, int currentHvo, string fieldName, string nullLabel, IVwStylesheet stylesheet)
		{
			Init(cache, persistProvider, fieldName, labels, currentHvo, nullLabel, stylesheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// deprecated constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels">The labels.</param>
		/// <param name="currentHvo">use zero if empty</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited</param>
		/// <param name="nullLabel">The null label.</param>
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, int currentHvo, string fieldName, string nullLabel)
		{
			Init(cache, persistProvider, fieldName, labels, currentHvo,nullLabel, null);
		}
		/// <summary>
		/// constructor for use with changing or setting a value
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="currentHvo">use zero if empty</param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public ReallySimpleListChooser(FdoCache cache, IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, int currentHvo, string fieldName)
		{
			Init(cache, persistProvider, fieldName, labels, currentHvo,XMLViewsStrings.ksEmpty, null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReallySimpleListChooser"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void Init(FdoCache cache, IPersistenceProvider persistProvider,
			string fieldName, ObjectLabelCollection labels, int currentHvo,string nullLabel, IVwStylesheet stylesheet)
		{
			m_stylesheet = stylesheet;
			m_nullLabel.DisplayName = nullLabel;
			m_nullLabel.Cache = cache;
			m_cache = cache;
			m_persistProvider = persistProvider;
			m_fieldName = fieldName;
			if (labels.Count > 0 && labels.IsFlatList())
				m_fFlatList = true;
			InitializeComponent();

			if (m_persistProvider!= null)
				m_persistProvider.RestoreWindowSettings("SimpleListChooser", this);

			SetForDefaultExtras();

			// It's easier to localize a format string than code that pieces together a string.
			this.Text = String.Format(XMLViewsStrings.ksChooseX, fieldName);

			LoadTree(labels, currentHvo, true);

			InitHelp();
		}

		private void InitHelp()
		{
			// Only enable the Help button if we have a help topic for the fieldName
			if (!buttonHelp.Enabled && helpProvider == null)
			{
				buttonHelp.Enabled = (selectHelpID() == null ? false : true);
				if (buttonHelp.Enabled)
				{
					this.helpProvider = new System.Windows.Forms.HelpProvider();
					this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
					this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(selectHelpID(), 0));
					this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
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
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName)
			: this(persistProvider, labels, fieldName, null)
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
		/// ------------------------------------------------------------------------------------
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName, IVwStylesheet stylesheet)
		{
			m_stylesheet = stylesheet;
			m_persistProvider = persistProvider;
			m_fieldName = fieldName;
			if (labels.Count > 0 && labels.IsFlatList())
				m_fFlatList = true;
			InitializeComponent();

			if (m_persistProvider != null)
				m_persistProvider.RestoreWindowSettings("SimpleListChooser", this);

			SetForDefaultExtras();

			// It's easier to localize a format string than code that pieces together a string.
			this.Text = String.Format(XMLViewsStrings.ksChooseX, fieldName);

			LoadTree(labels, 0, false);

			InitHelp();
		}

		/// <summary>
		/// constructor for use with changing or setting multiple values.
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		/// <param name="cache"></param>
		/// <param name="rghvoChosen">use null or int[0] if empty</param>
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName, FdoCache cache, int[] rghvoChosen) :
			this(persistProvider, labels, fieldName, cache, rghvoChosen, IsListSorted(labels, cache))
		{
		}

		/// <summary>
		/// constructor for use with changing or setting multiple values.
		/// </summary>
		/// <param name="persistProvider">optional, if you want to preserve the size and
		/// location</param>
		/// <param name="labels"></param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		/// <param name="cache"></param>
		/// <param name="rghvoChosen">use null or int[0] if empty</param>
		/// <param name="fSortLabels">if true, sort the labels alphabetically. if false, keep the order of given labels.</param>
		public ReallySimpleListChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName, FdoCache cache, int[] rghvoChosen, bool fSortLabels)
			: this(persistProvider, fieldName, cache, rghvoChosen)
		{
			if (labels.Count > 0 && labels.IsFlatList())
				m_fFlatList = true;
			m_fSortLabels = fSortLabels;
			m_fSortLabelsSet = true;
			FinishConstructor(labels);
		}

		/// <summary>
		/// Tail end of typical constructor, isolated for calling after subclass constructor
		/// has done some of its own initialization.
		/// </summary>
		/// <param name="labels"></param>
		protected void FinishConstructor(ObjectLabelCollection labels)
		{
			// Note: anything added here might need to be added to the LeafChooser constructor also.
			LoadTree(labels, 0, false);
			InitHelp();
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
		/// <param name="rghvoChosen">use null or int[0] if empty</param>
		/// ------------------------------------------------------------------------------------
		protected ReallySimpleListChooser(IPersistenceProvider persistProvider,
			string fieldName, FdoCache cache, int[] rghvoChosen)
		{
			m_cache = cache;
			m_persistProvider = persistProvider;
			m_fieldName = fieldName;
			InitializeComponent();

			if (m_persistProvider!= null)
				m_persistProvider.RestoreWindowSettings("SimpleListChooser", this);

			SetForDefaultExtras();

			// It's easier to localize a format string than code that pieces together a string.
			this.Text = String.Format(XMLViewsStrings.ksChooseX, fieldName);

			m_labelsTreeView.CheckBoxes = true;
			m_labelsTreeView.AfterCheck += new TreeViewEventHandler(m_labelsTreeView_AfterCheck);
			// We have to allow selections in order to allow keyboard support.  See LT-3068.
			//m_labelsTreeView.BeforeSelect += new TreeViewCancelEventHandler(m_labelsTreeView_BeforeSelect);
			if (rghvoChosen != null)
			{
				m_rghvoChosen = new List<int>(rghvoChosen.Length);
				for (int i = 0; i < rghvoChosen.Length; ++i)
					m_rghvoChosen.Add(rghvoChosen[i]);
				m_rghvoChosen.Sort();
			}
		}

		/// <summary>
		/// Grow the tree chooser view to cover all the optional fields of this dialog.  If
		/// necessary, it will be shrunk to show any fields that are actually used.
		/// </summary>
		private void SetForDefaultExtras()
		{
			m_lblExplanation.Visible = false;
			m_locTreeViewOrig = m_labelsTreeView.Location;
			m_labelsTreeView.Location = m_lblExplanation.Location;
			m_nInstTextHeight = m_locTreeViewOrig.Y - m_lblExplanation.Location.Y;
			m_labelsTreeView.Height += m_nInstTextHeight;
			m_lblLink1.Visible = false;
			m_picboxLink1.Visible = false;
			m_lblLink2.Visible = false;
			m_picboxLink2.Visible = false;
			m_nLink1Height = (m_lblLink1.Location.Y + m_lblLink1.Height) -
				(m_lblLink2.Location.Y + m_lblLink2.Height);
			m_nLink2Height = (m_lblLink2.Location.Y + m_lblLink2.Height) -
				(m_labelsTreeView.Location.Y + m_labelsTreeView.Height);
			m_labelsTreeView.Height += m_nLink1Height + m_nLink2Height;
		}

		/// <summary>
		/// Check whether the list should be sorted.  See LT-5149.
		/// </summary>
		/// <param name="labels"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static bool IsListSorted(ObjectLabelCollection labels, FdoCache cache)
		{
			if (labels.Count > 0 && cache != null)
			{
				int hvoList = cache.GetOwnerOfObject(labels[0].Hvo);
				ICmObject co = CmObject.CreateFromDBObject(cache, hvoList);
				if (co is ICmPossibilityList)
					return (co as ICmPossibilityList).IsSorted;
			}
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		public void SetObjectAndFlid(int hvo, int flid)
		{
			CheckDisposed();

			m_hvoObject = hvo;
			m_flidObject = flid;
			InitHelp();
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
					this.Text = String.Format(sText, TextParam);
				else
					this.Text = sText;
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

				if (!m_lblExplanation.Visible)
				{
					m_labelsTreeView.Location = m_locTreeViewOrig;
					m_labelsTreeView.Height -= m_nInstTextHeight;
					m_lblExplanation.Visible = true;
				}
				string sText = value;
				if (sText.IndexOf("{0}") >= 0 && (m_sTextParam != null || m_hvoTextParam != 0))
					m_lblExplanation.Text = String.Format(sText, TextParam);
				else
					m_lblExplanation.Text = sText;
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
			if (m_hvoTextParam != 0 && m_hvoObject != 0 && m_hvoTextParam == m_cache.GetOwnerOfObject(m_hvoObject))
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
				m_labelsTreeView.Height -= m_nLink1Height;
				m_lblLink1.Text = sText;
				if (type != LinkType.kSimpleLink)
					m_picboxLink1.Image = m_imageList.Images[(int)type];
				m_obj1 = obj;
				m_lblLink1.Visible = true;
				m_picboxLink1.Visible = true;
			}
			else if (m_cLinksShown == 2)
			{
				m_labelsTreeView.Height -= m_nLink2Height;
				m_lblLink2.Text = sText;
				if (type != LinkType.kSimpleLink)
					m_picboxLink2.Image = m_imageList.Images[(int)type];
				m_obj2 = obj;
				m_lblLink2.Visible = true;
				m_picboxLink2.Visible = true;
			}
		}

		/// <summary>
		/// Show extra radio buttons for Add/Replace (and possibly Remove)
		/// </summary>
		public void ShowFuncButtons()
		{
			CheckDisposed();

			m_AddButton = new RadioButton();
			m_AddButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			m_ReplaceButton = new RadioButton();
			m_ReplaceButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			m_AddButton.Text = XMLViewsStrings.ksAddToExisting;
			m_ReplaceButton.Text = XMLViewsStrings.ksReplaceExisting;
			m_AddButton.Location = m_picboxLink2.Location;
			m_ReplaceButton.Location = m_picboxLink1.Location;
			m_AddButton.Width = m_labelsTreeView.Width / 2;
			m_ReplaceButton.Width = m_labelsTreeView.Width;
			m_AddButton.Checked = true;
			m_labelsTreeView.Height = m_AddButton.Top - 10 - m_labelsTreeView.Top;

			m_RemoveButton = new RadioButton();
			m_RemoveButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			m_RemoveButton.Text = XMLViewsStrings.ksRemoveExisting;
			m_RemoveButton.Width = m_AddButton.Width;
			m_RemoveButton.Location = new Point(m_AddButton.Right, m_AddButton.Top);
			m_RemoveButton.Height = 30;
			this.Controls.AddRange(new Control[] { m_AddButton, m_ReplaceButton, m_RemoveButton });
		}

		/// <summary>
		/// Show extra radio buttons for matching All, Any, or None.
		/// </summary>
		internal void ShowAnyAllNoneButtons(ListMatchOptions mode, bool fAtomic)
		{
			CheckDisposed();
			m_helpTopic = "khtpChoose-AnyAllNoneItems";
			InitHelp();
			m_AnyButton = new RadioButton();
			m_AnyButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			m_NoneButton = new RadioButton();
			m_NoneButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			m_AnyButton.Text = XMLViewsStrings.ksAnyChecked;
			m_NoneButton.Text = XMLViewsStrings.ksNoChecked;
			m_AnyButton.Location = m_picboxLink2.Location;
			m_NoneButton.Location = m_picboxLink1.Location;
			m_AnyButton.Width = m_labelsTreeView.Width / 2;
			m_NoneButton.Width = m_AnyButton.Width;
			m_AnyButton.Checked = true;
			m_labelsTreeView.Height = m_AnyButton.Top - 10 - m_labelsTreeView.Top;

			if (fAtomic)
				this.Controls.AddRange(new Control[] { m_AnyButton, m_NoneButton });
			else
			{
				m_AllButton = new RadioButton();
				m_AllButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
				m_AllButton.Text = XMLViewsStrings.ksAllChecked;
				m_AllButton.Width = m_AnyButton.Width;
				m_AllButton.Location = new Point(m_AnyButton.Right, m_AnyButton.Top);

				m_ExactButton = new RadioButton();
				m_ExactButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
				m_ExactButton.Text = XMLViewsStrings.ksExactlyChecked;
				m_ExactButton.Width = m_AnyButton.Width;
				m_ExactButton.Location = new Point(m_NoneButton.Right, m_NoneButton.Top);

				this.Controls.AddRange(new Control[] { m_AnyButton, m_NoneButton, m_AllButton, m_ExactButton });
			}

			ListMatchMode = mode;
		}

		/// <summary>
		/// Enable using Ctrl-Click to toggle subitems along with parent.
		/// </summary>
		internal void EnableCtrlClick()
		{
			CheckDisposed();
			m_fEnableCtrlCheck = true;

			Label ctrlLabel = new Label();
			ctrlLabel.Text = XMLViewsStrings.ksCtrlClickForSubItems;
			ctrlLabel.Height = ctrlLabel.PreferredHeight;
			m_labelsTreeView.Height -= ctrlLabel.Height;
			ctrlLabel.Location = new Point(m_labelsTreeView.Left, m_labelsTreeView.Bottom + 4);
			ctrlLabel.Width = m_labelsTreeView.Width;
			// Didn't work out...not legible.
			//ctrlLabel.Font = new Font(ctrlLabel.Font, FontStyle.Italic);
			ctrlLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
			this.Controls.Add(ctrlLabel);
		}

		/// <summary>
		/// Called after a check box is checked (or unchecked).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_labelsTreeView_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (m_fEnableCtrlCheck && Control.ModifierKeys == Keys.Control)
			{
				LabelNode rootNode = (LabelNode)e.Node;
				Cursor.Current = Cursors.WaitCursor;
				try
				{
					if (e.Action != TreeViewAction.Unknown)
					{
						// The original check, not recursive.
						rootNode.AddChildren(true, new List<int>()); // All have to exist to get checked/unchecked
						if (!rootNode.IsExpanded)
							rootNode.Expand(); // open up at least one level to show effects.
					}
					foreach (TreeNode node in rootNode.Nodes)
						node.Checked = e.Node.Checked; // and recursively checks children.
				}
				finally
				{
					Cursor.Current = Cursors.Default;
				}
			}
			if (m_fForbidNoItemChecked)
			{
				btnOK.Enabled = AnyItemChecked(m_labelsTreeView.Nodes);
			}
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

		private bool AnyItemChecked(TreeNodeCollection nodes)
		{
			foreach (TreeNode child in nodes)
				if (child.Checked || AnyItemChecked(child.Nodes))
					return true;
			return false;
		}

		private void FullyExpand(TreeNode treeNode)
		{
			if (!treeNode.IsExpanded)
				treeNode.Expand();
			foreach (TreeNode node in treeNode.Nodes)
				FullyExpand(node);
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
					ICmObject co = CmObject.CreateFromDBObject(m_cache, m_hvoTextParam);
					m_sTextParam = co.ShortName;
					// We want this link Guid value only if label/text hint that it's needed.
					// (This requirement is subject to change without much notice!)
					m_guidLink = m_cache.GetGuidFromId(m_hvoTextParam);
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
		/// <param name="mediator"></param>
		public void InitializeExtras(XmlNode configNode, XCore.Mediator mediator)
		{
			CheckDisposed();

			Debug.Assert(m_cache != null);
			m_mediator = mediator;
			int ws = m_cache.DefaultAnalWs;
			SetFontFromWritingSystem(ws, mediator);
			InitHelp(); // Give it another go now that we have a mediator
			string sGuiControl = null;
			if (configNode == null)
				return;
			XmlNode node = configNode.SelectSingleNode("chooserInfo");
			if (node != null)
			{
				string sTextParam =
					XmlUtils.GetAttributeValue(node, "textparam", "owner").ToLower();
				if (sTextParam != null)
				{
					// The default case ("owner") is handled by the caller setting TextParamHvo.
					if (sTextParam == "vernws")
					{
						ICmObject co = CmObject.CreateFromDBObject(m_cache,
							m_cache.DefaultVernWs);
						m_sTextParam = co.ShortName;
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
							ISilDataAccess sda = m_cache.MainCacheAccessor;
							m_hvoTextParam = sda.get_ObjectProp(m_hvoObject, flidTextParam);
						}
					}
					catch
					{
						// Ignore any badness here.
					}
				}

				StringTable tbl = null;
				if (m_mediator != null && m_mediator.HasStringTable)
					tbl = m_mediator.StringTbl;
				string sTitle = XmlUtils.GetAttributeValue(node, "title");
				if (sTitle != null)
					Title = sTitle;
				string sText = XmlUtils.GetAttributeValue(node, "text");
				if (sText != null)
					InstructionalText = sText;
				XmlNodeList linkNodes = node.SelectNodes("chooserLink");
				Debug.Assert(linkNodes.Count <= 2);
				for (int i = linkNodes.Count - 1; i >= 0 ; --i)
				{
					string sType = XmlUtils.GetAttributeValue(linkNodes[i], "type", "goto").ToLower();
					string sLabel = XmlUtils.GetLocalizedAttributeValue(tbl, linkNodes[i], "label", null);
					switch (sType)
					{
					case "goto":
					{
						string sTool = XmlUtils.GetAttributeValue(linkNodes[i], "tool");
						if (sLabel != null && sTool != null)
						{
							AddLink(sLabel, LinkType.kGotoLink, FwLink.Create(sTool,
								m_guidLink, m_cache.ServerName, m_cache.DatabaseName));
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
				sGuiControl = XmlUtils.GetOptionalAttributeValue(node, "guicontrol");
			}
			// Set the font for the tree view control based on the desired writing system.
			node = configNode.SelectSingleNode("deParams");
			//int ws = m_cache.DefaultAnalWs;
			//SetFontFromWritingSystem(ws, mediator);
			//InitHelp(); // Give it another go now that we have a mediator
			// Replace the tree view control with a browse view control if it's both desirable
			// and feasible.
			if (m_fFlatList)
				ReplaceTreeView(sGuiControl);
		}

		/// <summary>
		/// Access for outsiders who don't call InitializExtras.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="sGuiControl"></param>
		public void ReplaceTreeView(XCore.Mediator mediator, string sGuiControl)
		{
			if (m_fFlatList)
			{
				if (m_mediator == null)
					m_mediator = mediator;
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
			XmlNode xnWindow = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
			if (xnWindow == null)
				return;
			string sXPath = String.Format("controls/parameters/guicontrol[@id=\"{0}\"]/parameters", sGuiControl);
			XmlNode configNode = xnWindow.SelectSingleNode(sXPath);
			if (configNode == null)
				return;
			m_flvLabels = new FlatListView();
			IVwStylesheet stylesheet = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			m_flvLabels.Initialize(m_cache, stylesheet, m_mediator, configNode, m_rghvo);
			m_flvLabels.SetCheckedItems(m_rghvoChosen);
			m_flvLabels.Location = m_labelsTreeView.Location;
			m_flvLabels.Size = m_labelsTreeView.Size;
			m_flvLabels.TabStop = m_labelsTreeView.TabStop;
			m_flvLabels.TabIndex = m_labelsTreeView.TabIndex;
			m_flvLabels.Anchor = m_labelsTreeView.Anchor;
			this.Controls.Remove(m_labelsTreeView);
			this.Controls.Add(m_flvLabels);
			m_labelsTreeView = null;
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
			Font font = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.GetFontForNormalStyle(
				wss[0], stylesheet, wsf);
			for (int i = 1; i < wss.Length; i++)
			{
				Font other = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.GetFontForNormalStyle(
				wss[i], stylesheet, wsf);
				// JohnT: this is a compromise. I don't think it is guaranteed that a font with the
				// same SizeInPoints will be the same height. But it should be about the same,
				// and until we implement a proper multilingual treeview replacement, I'm not sure we
				// can do much better than this.
				if (other.Height > font.Height)
					font = new Font(font.FontFamily, Math.Max(font.SizeInPoints, other.SizeInPoints));
			}

			m_labelsTreeView.Font = font;
		}

		private void SetFontFromWritingSystem(int ws, XCore.Mediator mediator)
		{
			Font oldFont = m_labelsTreeView.Font;
			IVwStylesheet stylesheet = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.StyleSheetFromMediator(mediator);
			Font font = SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.GetFontForNormalStyle(
				ws, stylesheet, m_cache.LanguageWritingSystemFactoryAccessor);
			float maxPoints = font.SizeInPoints;
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				if (node.NodeFont != oldFont && node.NodeFont != null) // overridden because of vernacular text
				{
					node.ResetVernacularFont(m_cache.LanguageWritingSystemFactoryAccessor, m_cache.DefaultVernWs, stylesheet);
					maxPoints = Math.Max(maxPoints, node.NodeFont.SizeInPoints);
				}
			}
			if (maxPoints > font.SizeInPoints)
			{
				font = new Font(font.FontFamily, maxPoints);
			}
			m_labelsTreeView.Font = font;
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				if (node.NodeFont == oldFont) // not overridden because of vernacular text
					node.NodeFont = font;
			}
			//IWritingSystem lgws =
			//    m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws);
			//if (lgws != null)
			//{
			//    string sFont = lgws.DefaultSansSerif;
			//    if (sFont != null)
			//    {
			//        System.Drawing.Font font =
			//            new System.Drawing.Font(sFont, m_labelsTreeView.Font.SizeInPoints);
			//        m_labelsTreeView.Font = font;
			//    }
			//}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the raw.
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="sTitle">The s title.</param>
		/// <param name="sText">The s text.</param>
		/// <param name="sGotoLabel">The s goto label.</param>
		/// <param name="sTool">The s tool.</param>
		/// <param name="sWs">The s ws.</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeRaw(XCore.Mediator mediator, string sTitle, string sText,
			string sGotoLabel, string sTool, string sWs)
		{
			CheckDisposed();

			Debug.Assert(m_cache != null);
			m_mediator = mediator;
			if (sTitle != null)
				Title = sTitle;
			if (sText != null)
				InstructionalText = sText;
			if (sGotoLabel != null && sTool != null)
			{
				AddLink(sGotoLabel, LinkType.kGotoLink,
					FwLink.Create(sTool, m_guidLink, m_cache.ServerName, m_cache.DatabaseName));
			}
			int ws = m_cache.DefaultAnalWs;
			// Now that we're overriding the font for nodes that contain vernacular text,
			// it's best to let all the others default to the analysis language.
			//if (sWs != null)
			//{
			//    switch (sWs)
			//    {
			//    case "vernacular":
			//    case "all vernacular":
			//    case "vernacular analysis":
			//        ws = m_cache.DefaultVernWs;
			//        break;
			//    case "analysis":
			//    case "all analysis":
			//    case "analysis vernacular":
			//        ws = m_cache.DefaultAnalWs;
			//        break;
			//    }
			//}
			SetFontFromWritingSystem(ws, mediator);

			InitHelp(); // Give it another go now that we have a mediator
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
			ICmObject obj = CmObject.CreateFromDBObject(Cache, startHvo);
			while (obj.ClassID == FDO.Ling.PartOfSpeech.kclsidPartOfSpeech)
			{
				posHvo = obj.Hvo;
				sTopPOS = obj.ShortName;
				obj = CmObject.CreateFromDBObject(Cache, obj.OwnerHVO);
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

			if (m_mediator != null && m_linkJump != null)
			{
				m_mediator.PostMessage("FollowLink", m_linkJump);
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
		public bool HandleAnyJump(XCore.Mediator mediator)
		{
			CheckDisposed();

			if (mediator != null && m_linkJump != null)
			{
				mediator.PostMessage("FollowLink", m_linkJump);
				return true;
			}
			else
			{
				return false;
			}
		}

		private void SimpleListChooser_Activated(object sender, System.EventArgs e)
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
			Size size = this.Size;
			base.OnLoad (e);
			if (this.Size != size)
				this.Size = size;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the tree.
		/// </summary>
		/// <param name="labels">The labels.</param>
		/// <param name="currentHvo">The current hvo.</param>
		/// <param name="showCurrentSelection">if set to <c>true</c> [show current selection].</param>
		/// ------------------------------------------------------------------------------------
		protected void LoadTree(ObjectLabelCollection labels, int currentHvo,
			bool showCurrentSelection)
		{
			Debug.Assert(showCurrentSelection? (m_rghvoChosen==null) : (currentHvo==0),
				"If showEmptyOption is false, currentHvo should be zero, since it is meaningless");

			if (m_fFlatList)
			{
				m_labels = labels;
				m_rghvo = new List<int>(labels.Count);
				for (int i = 0; i < labels.Count; ++i)
					m_rghvo.Add(labels[i].Hvo);
			}
			Cursor.Current = Cursors.WaitCursor;
			m_labelsTreeView.BeginUpdate();
			m_labelsTreeView.Nodes.Clear();

			// if m_fSortLabels is true, we'll sort the labels alphabetically, using dumb English sort.
			// otherwise, we'll keep the labels in their given order.
			if (!m_fSortLabelsSet && m_cache != null)
			{
				m_fSortLabels = IsListSorted(labels, m_cache);
				m_fSortLabelsSet = true;
			}
			m_labelsTreeView.Sorted = m_fSortLabels;
			Stack ownershipStack = null;
			LabelNode nodeRepresentingCurrentChoice = null;
			//add <empty> row
			if (showCurrentSelection)
			{
				if (m_cache != null)
					ownershipStack  = GetOwnershipStack(currentHvo);

				if (m_nullLabel.DisplayName != null)
					m_labelsTreeView.Nodes.Add(CreateLabelNode(m_nullLabel));
			}

			ArrayList rgLabelNodes = new ArrayList();
			ArrayList rgOwnershipStacks = new ArrayList();
			if (m_rghvoChosen != null)
			{
				for (int i = 0; i < m_rghvoChosen.Count; ++i)
					rgOwnershipStacks.Add(GetOwnershipStack((int)m_rghvoChosen[i]));
			}
			//	m_labelsTreeView.Nodes.AddRange(labels.AsObjectArray);
			foreach (ObjectLabel label in labels)
			{
				if (!WantNodeForLabel(label))
					continue;
				// notice that we are only adding the top-level notes now.
				// others will be added when the user expands them.
				LabelNode x = CreateLabelNode(label);
				m_labelsTreeView.Nodes.Add(x);
				if (m_rghvoChosen != null)
					x.Checked = (m_rghvoChosen.BinarySearch(label.Hvo) >= 0);

				//notice that we don't actually use the "stack-ness" of the stack.
				//if we did, we would have to worry about skipping the higher level owners, like
				//language project.
				//but just treat it as an array, we can ignore those issues.
				if (m_cache != null &&
					showCurrentSelection &&
					ownershipStack.Contains(label.Hvo))
				{
					nodeRepresentingCurrentChoice = x.AddChildrenAndLookForSelected(currentHvo,
						ownershipStack, null);
				}
				if (m_cache != null &&
					m_rghvoChosen != null)
				{
					for (int i = 0; i < m_rghvoChosen.Count; ++i)
					{
						if (((Stack)rgOwnershipStacks[i]).Contains(label.Hvo))
						{
							rgLabelNodes.Add(x.AddChildrenAndLookForSelected(
								(int)m_rghvoChosen[i], (Stack)rgOwnershipStacks[i],
								m_rghvoChosen));
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
				if (nodeRepresentingCurrentChoice != null)
				{
					m_labelsTreeView.SelectedNode = nodeRepresentingCurrentChoice;
				}
				else
				{
					m_labelsTreeView.SelectedNode = FindNodeFromHvo(currentHvo);
				}
				if (m_labelsTreeView.SelectedNode != null)
				{
					m_labelsTreeView.SelectedNode.EnsureVisible();
					//for some reason, doesn't actually select it, so do this:
					m_labelsTreeView.SelectedNode.ForeColor = System.Drawing.Color.Blue;
				}
			}
			else if (m_rghvoChosen != null)
			{
				// Don't show a selection initially
				m_labelsTreeView.SelectedNode = null;
			}

			//important that we not do this sooner!
			m_labelsTreeView.BeforeExpand +=
				new System.Windows.Forms.TreeViewCancelEventHandler(
				m_labelsTreeView_BeforeExpand);
			Cursor.Current = Cursors.Default;
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the label node.
		/// </summary>
		/// <param name="nol">The nol.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual LabelNode CreateLabelNode(ObjectLabel nol)
		{
			return new LabelNode(nol, m_stylesheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ownership stack.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected Stack GetOwnershipStack(int hvo)
		{
			Stack stack = new Stack();
			while (hvo > 0) //!m_cache.ClassIsOwnerless(hvo))
			{
				hvo = m_cache.GetOwnerOfObject(hvo);
				if (hvo > 0)
					stack.Push(hvo);
			}
			return stack;
		}

		#region we-might-not-need-this-stuff-anymore
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the node at root level.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected LabelNode FindNodeAtRootLevel(int hvo)
		{
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				if (node.Label.Hvo == hvo)
				{
					return node;
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the node from hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected LabelNode FindNodeFromHvo(int hvo)
		{
			// is it in the root level of choices?
			LabelNode n = FindNodeAtRootLevel(hvo);
			if (n != null)
				return n;

			// enhance: this is the simplest thing that would possibly work, but it is slow!
			// see the #if'd-out code for the beginnings of a smarter algorithm which would only
			// expand what needed to be expanded.
			// No, so go looking deeper (and slower!)
			foreach (LabelNode node in m_labelsTreeView.Nodes)
			{
				n = FindNode(node, hvo);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the node.
		/// </summary>
		/// <param name="searchNode">The search node.</param>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected LabelNode FindNode(LabelNode searchNode, int hvo)
		{
			//is it me?
			if (searchNode.Label.Hvo == hvo)
				return searchNode;

			//no, so look in my descendants
			searchNode.AddChildren(true, m_rghvoChosen);
			foreach (LabelNode node in searchNode.Nodes)
			{
				LabelNode n = FindNode(node, hvo);
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
		public List<int> ChosenHvos
		{
			get
			{
				CheckDisposed();
				return m_rghvoNewChosen;
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
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink1)).BeginInit();
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
			// ReallySimpleListChooser
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.m_lblExplanation);
			this.Controls.Add(this.m_picboxLink1);
			this.Controls.Add(this.m_lblLink1);
			this.Controls.Add(this.m_picboxLink2);
			this.Controls.Add(this.m_lblLink2);
			this.Controls.Add(this.m_labelsTreeView);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.buttonHelp);
			this.Cursor = System.Windows.Forms.Cursors.Default;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ReallySimpleListChooser";
			this.ShowInTaskbar = false;
			this.Activated += new System.EventHandler(this.SimpleListChooser_Activated);
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		private void HandleCommmandChoice(ChooserCommandNode node)
		{
			if (node != null)
			{
				ChooserCommand cmd = node.Tag as ChooserCommand;
				if (cmd != null)
				{
					if (cmd.ShouldCloseBeforeExecuting)
						this.Visible = false;
					m_chosenLabel = cmd.Execute();
				}
			}
		}

		private void OnOKClick(object sender, System.EventArgs e)
		{
			Persist();
			if (m_linkCmd != null)
			{
				this.Visible = false;
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
			if (m_persistProvider!= null)
				m_persistProvider.PersistWindowSettings("SimpleListChooser",this);
		}

		private void SetChosen()
		{
			if (m_rghvoChosen != null)
			{
				m_chosenLabel = null;
				if (m_labelsTreeView != null)
				{
					m_rghvoNewChosen = new List<int>();
					// Walk the tree of labels looking for Checked == true.  This allows us to
					// return an ordered list of hvos (sorted by list display order).
					for (int i = 0; i < m_labelsTreeView.Nodes.Count; ++i)
					{
						if (m_labelsTreeView.Nodes[i].Checked)
							m_rghvoNewChosen.Add(((LabelNode)m_labelsTreeView.Nodes[i]).Label.Hvo);
						CheckChildrenForChosen(((LabelNode)m_labelsTreeView.Nodes[i]));
					}
				}
				else
				{
					m_rghvoNewChosen = m_flvLabels.GetCheckedItems();
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
				LabelNode x = node.Nodes[i] as LabelNode;
				if (x != null)
				{
					if (x.Checked)
						m_rghvoNewChosen.Add(x.Label.Hvo);
					CheckChildrenForChosen(x);
				}
			}
		}

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			Persist();
			m_chosenLabel = null;
		}

		private void m_labelsTreeView_DoubleClick(object sender, System.EventArgs e)
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
		protected void m_labelsTreeView_BeforeExpand(object sender,
			System.Windows.Forms.TreeViewCancelEventArgs e)
		{
			LabelNode node = (LabelNode)e.Node;
			Cursor.Current = Cursors.WaitCursor;
			node.AddChildren(false, m_rghvoChosen);
			Cursor.Current = Cursors.Default;
		}

		/// <summary>
		/// </summary>
		/// <param name="cmd"></param>
		public void AddChooserCommand(ChooserCommand cmd)
		{
			CheckDisposed();

			ChooserCommandNode node = new ChooserCommandNode(cmd);
			string sFontName = cmd.Cache.LangProject.DefaultAnalysisWritingSystemFont;

			// TODO: need to get analysis font's size
			// and then set it to use underline:
			Font font = new Font(sFontName, 10.0f, FontStyle.Italic);
			node.NodeFont = font;
			//node.ForeColor = Color.DarkGreen;
			m_labelsTreeView.Nodes.Insert(0, node);
		}

		private void m_lblLink1_LinkClicked(object sender,
			System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			if (m_obj1 != null)
			{
				m_linkJump = m_obj1 as FwLink;
				m_linkCmd = m_obj1 as ChooserCommand;
				if (m_linkJump != null)
				{
					btnCancel.PerformClick();
					// No result as such, but we'll perform a jump.
					this.DialogResult = DialogResult.Ignore;
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

		private void m_lblLink2_LinkClicked(object sender,
			System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			if (m_obj2 != null)
			{
				m_linkJump = m_obj2 as FwLink;
				m_linkCmd = m_obj2 as ChooserCommand;
				if (m_linkJump != null)
				{
					btnCancel.PerformClick();
					// No result as such, but we'll perform a jump.
					this.DialogResult = DialogResult.Ignore;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected class LabelNode : TreeNode
		{
			/// <summary></summary>
			protected IVwStylesheet m_stylesheet;
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:LabelNode"/> class.
			/// </summary>
			/// <param name="label">The label.</param>
			/// <param name="stylesheet">The stylesheet.</param>
			/// --------------------------------------------------------------------------------
			public LabelNode(ObjectLabel label, IVwStylesheet stylesheet) : base()
			{
				Tag = label;
				m_stylesheet = stylesheet;
				ITsString tssDisplay = label.AsTss;
				int wsVern;
				if (HasVernacularText(tssDisplay,
					label.Cache.LangProject.CurVernWssRS.HvoArray,
					out wsVern))
				{
					NodeFont = GetVernacularFont(label.Cache.LanguageWritingSystemFactoryAccessor, wsVern, stylesheet);
				}
				Text = tssDisplay.Text;
				if (label.GetHaveSubItems())
					// this is a hack to make the node expandable before we have filled in any
					// actual children
					Nodes.Add(new TreeNode("should not see this"));
			}

			private bool HasVernacularText(ITsString tss, int[] vernWses, out int wsVern)
			{
				wsVern = 0;
				int crun = tss.RunCount;
				for (int irun = 0; irun < crun; irun++)
				{
					ITsTextProps ttp = tss.get_Properties(irun);
					int nvar;
					int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
					foreach (int vernWS in vernWses)
					{
						if (ws == vernWS)
						{
							wsVern = ws;
							return true;
						}
					}
				}
				return false;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Resets the vernacular font.
			/// </summary>
			/// <param name="wsf">The WSF.</param>
			/// <param name="wsVern">The ws vern.</param>
			/// <param name="stylesheet">The stylesheet.</param>
			/// --------------------------------------------------------------------------------
			public void ResetVernacularFont(ILgWritingSystemFactory wsf, int wsVern, IVwStylesheet stylesheet)
			{
				NodeFont = GetVernacularFont(wsf, wsVern, stylesheet);
			}

			private Font GetVernacularFont(ILgWritingSystemFactory wsf, int wsVern, IVwStylesheet stylesheet)
			{
				if (stylesheet == null)
				{
					IWritingSystem wsEngine = wsf.get_EngineOrNull(wsVern);
					string fontName = wsEngine.DefaultSansSerif;
					return new Font(fontName, (float)10.0);
				}
				else
				{
					return SIL.FieldWorks.Common.Widgets.FontHeightAdjuster.GetFontForNormalStyle(wsVern, stylesheet, wsf);
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the label.
			/// </summary>
			/// <value>The label.</value>
			/// --------------------------------------------------------------------------------
			public ObjectLabel Label
			{
				get
				{
					return (ObjectLabel)this.Tag;
				}
			}
			/// <summary>
			///
			/// </summary>
			public void AddChildren(bool recursively, List<int> rghvoChosen)
			{
				// JohnT: if we redo this every time we open, we discard the old nodes AND THEIR VALUES.
				// Thus, collapsing and reopening a tree clears its members! But we do need to check
				// that we have a label node, we put a dummy one in to show that it can be expanded.
				if (this.Nodes.Count > 0 && this.Nodes[0] is LabelNode)
				{
					// This already has its nodes, but what about its children if recursive?
					if (!recursively)
						return;
					foreach (LabelNode node in this.Nodes)
						node.AddChildren(true, rghvoChosen);
					return;
				}
				this.Nodes.Clear(); // get rid of the dummy.

				this.AddSecondaryNodes(this, Nodes, rghvoChosen);
				foreach (ObjectLabel label in ((ObjectLabel)this.Tag).SubItems)
				{
					if (!WantNodeForLabel(label))
						continue;
					LabelNode node = Create(label, m_stylesheet);
					if (rghvoChosen != null)
						node.Checked = (rghvoChosen.BinarySearch(label.Hvo) >= 0);
					this.Nodes.Add(node);
					AddSecondaryNodes(node, node.Nodes, rghvoChosen);
					if (recursively)
					{
						node.AddChildren(true, rghvoChosen);
					}
				}
			}
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Wants the node for label.
			/// </summary>
			/// <param name="label">The label.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public virtual bool WantNodeForLabel(ObjectLabel label)
			{
				return true; // by default want all nodes.
			}
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Adds the secondary nodes.
			/// </summary>
			/// <param name="node">The node.</param>
			/// <param name="nodes">The nodes.</param>
			/// <param name="rghvoChosen">The rghvo chosen.</param>
			/// --------------------------------------------------------------------------------
			public virtual void AddSecondaryNodes(LabelNode node, TreeNodeCollection nodes, List<int> rghvoChosen)
			{
				// default is to do nothing
			}
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Creates the specified nol.
			/// </summary>
			/// <param name="nol">The nol.</param>
			/// <param name="stylesheet">The stylesheet.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			protected virtual LabelNode Create(ObjectLabel nol, IVwStylesheet stylesheet)
			{
				return new LabelNode(nol, stylesheet);
			}

			/// <summary>
			///
			/// </summary>
			public virtual LabelNode AddChildrenAndLookForSelected (int hvoToSelect,
				Stack ownershipStack, List<int> rghvoChosen)
			{
				LabelNode nodeRepresentingCurrentChoice = null;
				// JohnT: if this.Nodes[0] is not a LabelNode, it is a dummy node we added so that
				// its parent LOOKS like something we can expand. That is the usual case for a node
				// we can expand. Therefore finding one of those, or finding more or less than one
				// node, is evidence that we haven't previously computed the real children of this,
				// and should do so.
				bool fExpanded = this.Nodes.Count != 1 || (this.Nodes[0] as LabelNode) != null;
				if (!fExpanded)
				{
					this.Nodes.Clear();
					nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(this,
						Nodes, nodeRepresentingCurrentChoice, hvoToSelect, ownershipStack, rghvoChosen);
					foreach (ObjectLabel label in ((ObjectLabel)this.Tag).SubItems)
					{
						if (!WantNodeForLabel(label))
							continue;
						LabelNode node = Create(label, m_stylesheet);
						if (rghvoChosen != null)
							node.Checked = (rghvoChosen.BinarySearch(label.Hvo) >= 0);
						this.Nodes.Add(node);
						nodeRepresentingCurrentChoice = CheckForSelection(label, hvoToSelect,
							node, nodeRepresentingCurrentChoice, ownershipStack);
						nodeRepresentingCurrentChoice = AddSecondaryNodesAndLookForSelected(
							node, node.Nodes, nodeRepresentingCurrentChoice, hvoToSelect,
							ownershipStack, rghvoChosen);
					}
				}
				else
				{
					// Even if we don't have to create children for this, we need to search the
					// children for matches, and perhaps expand some of them.
					foreach (LabelNode node in this.Nodes)
					{
						nodeRepresentingCurrentChoice = CheckForSelection(node.Label,
							hvoToSelect, node, nodeRepresentingCurrentChoice, ownershipStack);
					}
				}
				if (nodeRepresentingCurrentChoice == null)
				{
					foreach (LabelNode node in this.Nodes)
					{
						if (ownershipStack.Contains(node.Label.Hvo))
						{
							nodeRepresentingCurrentChoice =	node.AddChildrenAndLookForSelected(
								hvoToSelect, ownershipStack, rghvoChosen);
							return nodeRepresentingCurrentChoice;
						}
					}
				}
				else
				{
					this.Expand();
					nodeRepresentingCurrentChoice.EnsureVisible();
				}
				return nodeRepresentingCurrentChoice;
			}
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Add secondary nodes to tree at nodes (and check any that occur in rghvoChosen),
			/// and return the one whose hvo is hvoToSelect, or nodeRepresentingCurrentChoice
			/// if none match.
			/// </summary>
			/// <param name="node">node to be added</param>
			/// <param name="nodes">where to add it</param>
			/// <param name="nodeRepresentingCurrentChoice">The node representing current choice.</param>
			/// <param name="hvoToSelect">The hvo to select.</param>
			/// <param name="ownershipStack">The ownership stack.</param>
			/// <param name="rghvoChosen">The rghvo chosen.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public virtual LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node,
				TreeNodeCollection nodes, LabelNode nodeRepresentingCurrentChoice,
				int hvoToSelect, Stack ownershipStack, List<int> rghvoChosen)
			{
				// default is to do nothing
				return nodeRepresentingCurrentChoice;
			}
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Checks for selection.
			/// </summary>
			/// <param name="label">The label.</param>
			/// <param name="hvoToSelect">The hvo to select.</param>
			/// <param name="node">The node.</param>
			/// <param name="nodeRepresentingCurrentChoice">The node representing current choice.</param>
			/// <param name="ownershipStack">The ownership stack.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			protected virtual LabelNode CheckForSelection(ObjectLabel label, int hvoToSelect,
				LabelNode node, LabelNode nodeRepresentingCurrentChoice, Stack ownershipStack)
			{
				if (label.Hvo == hvoToSelect)		//make it look selected
				{
					nodeRepresentingCurrentChoice = node;
				}
				return nodeRepresentingCurrentChoice;
			}

			//was ignored
			//			/// <summary>
			//			/// Returns a String that represents the current Object.
			//			/// </summary>
			//			/// <returns>A String that represents the current Object.</returns>
			//			public override string ToString()
			//			{
			//				CheckDisposed();
			//
			//				return ((ObjectLabel)this.Tag).ShortName;
			//			}
			//
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
			/// Initializes a new instance of the <see cref="T:ChooserCommandNode"/> class.
			/// </summary>
			/// <param name="cmd">The CMD.</param>
			/// --------------------------------------------------------------------------------
			public ChooserCommandNode(ChooserCommand cmd) : base()
			{
				this.Tag = cmd;
				this.Text = cmd.Label;
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
					return (ChooserCommand)this.Tag;
				}
			}
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "UserHelpFile", selectHelpID());
		}

		/// <summary>
		/// Is m_helpTopic a valid help topic?
		/// </summary>
		private bool helpTopicIsValid(string helpStr)
		{
			return (!String.IsNullOrEmpty(helpStr)) && (FwApp.App.GetHelpString(helpStr, 0) != null);
		}

		/// <summary>
		/// Generates a possible help topic id from the field name, but does NOT check it for validity!
		/// </summary>
		/// <returns></returns>
		private string generateChooserHelpTopicID(string fromStr)
		{
			StringBuilder candidateID = new StringBuilder("khtpChoose-");

			// Should we capitalize the next letter?
			bool nextCapital = true;

			// Lets turn our field into a candidate help page!
			foreach (char ch in fromStr.ToCharArray())
			{
				if (Char.IsLetterOrDigit(ch)) // might we include numbers someday?
				{
					if (nextCapital)
						candidateID.Append(Char.ToUpper(ch));
					else
						candidateID.Append(ch);
					nextCapital = false;
				}
				else // unrecognized character... exclude it
					nextCapital = true; // next letter should be a capital
			}
			return candidateID.ToString();
		}

		private string selectHelpID()
		{
			if (helpTopicIsValid(m_helpTopic)) // we already have a valid help topic, use that!
				return m_helpTopic;

			// Here we should name the odd cases where the help topic id is not able to be
			// generated from the field name. In most cases, we should just use the generated
			// name as the key and put that in the resx table --CameronB
			switch (m_fieldName)
			{
			case "NaturalClass":
				if (m_mediator != null && m_mediator.PropertyTable.GetStringProperty("currentContentControl", null) == "EnvironmentEdit")
					return "khtpChoose-NaturalClassGrammar";
				else if (m_mediator != null && m_mediator.PropertyTable.GetStringProperty("currentContentControl", null) == "lexiconEdit")
					return "khtpChoose-NaturalClassLexicon";
				else
					return null;
			case "Slots":
				return "khtpChoose-SlotsField"; // many "slots" help tags exist
			case "DataTreeWritingSystems":
				return "khtpChoose-DataTreeWritingSystems"; //This is for the specific help when the user launches Configure WS's from a slice.
			default:
				// Generate a candidate ID and see if that works
				string candidateID = m_fieldName;

				// Some field names have a "From" or a "To" at the beginning if
				// we are deriving them. Since we want to show the same help doc
				// regardless, lets remove the from or to...
				if (candidateID.StartsWith("from ", StringComparison.InvariantCultureIgnoreCase))
					candidateID = candidateID.Substring(5);
				else if (candidateID.StartsWith("to ", StringComparison.InvariantCultureIgnoreCase))
					candidateID = candidateID.Substring(3);

				// now lets make a "real" candidate ID
				candidateID = generateChooserHelpTopicID(candidateID);
				if (helpTopicIsValid(candidateID))
					return candidateID;

				// if nothing else fits...
				Debug.WriteLine("ATTENTION: Broken help link detected for: " + m_fieldName);
				Debug.WriteLine("   Showing a generic help doc for this chooser");//, NOT " + candidateID);
				return "khtpChoose-CmPossibility";
			}
		}

		/// <summary>
		/// Sets the help topic ID for the window.  This is used in both the Help button and when the user hits F1
		/// </summary>
		public void SetHelpTopic(string helpTopic)
		{
			CheckDisposed();

			m_helpTopic = helpTopic;
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
		/// <param name="mediator"></param>
		/// <returns></returns>
		public static bool ChooseNaturalClass(IVwRootBox rootb, FdoCache cache,
			IPersistenceProvider persistenceProvider, XCore.Mediator mediator)
		{
			List<int> candidates = null;
			int hvoPhonData = cache.LangProject.PhonologicalDataOA.Hvo;
			int flidNatClasses = (int)PhPhonData.PhPhonDataTags.kflidNaturalClasses;
			int[] targetHvos = cache.GetVectorProperty(hvoPhonData, flidNatClasses, false);
			if (targetHvos.Length > 0)
				candidates = new List<int>(targetHvos);
			else
				candidates = new List<int>(0);
			ObjectLabelCollection labels = new ObjectLabelCollection(cache, candidates,
				"",
				cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(cache.DefaultAnalWs));

			using (ReallySimpleListChooser chooser = new ReallySimpleListChooser(persistenceProvider,
				labels, "NaturalClass"))
			{
				string sTitle = null;
				string sDescription = null;
				string sJumpLabel = null;
				if (mediator != null && mediator.HasStringTable)
				{
					sTitle = mediator.StringTbl.GetString("kstidChooseNaturalClass",
						"Linguistics/Morphology/NaturalClassChooser");
					sDescription = mediator.StringTbl.GetString("kstidNaturalClassListing",
						"Linguistics/Morphology/NaturalClassChooser");
					sJumpLabel = mediator.StringTbl.GetString("kstidGotoNaturalClassList",
						"Linguistics/Morphology/NaturalClassChooser");
				}
				if (sTitle == null || sTitle.Length == 0 || sTitle == "kstidChooseNaturalClass")
					sTitle = XMLViewsStrings.ksChooseNaturalClass;
				if (sDescription == null || sDescription.Length == 0 || sDescription == "kstidNaturalClassListing")
					sDescription = XMLViewsStrings.ksNaturalClassDesc;
				if (sJumpLabel == null || sJumpLabel.Length == 0 || sJumpLabel == "kstidGotoNaturalClassList")
					sJumpLabel = XMLViewsStrings.ksEditNaturalClasses;
				chooser.Cache = cache;
				chooser.SetObjectAndFlid(0, 0);
				chooser.InitializeRaw(mediator, sTitle, sDescription, sJumpLabel,
					"naturalClassedit", "analysis vernacular");

				System.Windows.Forms.DialogResult res = chooser.ShowDialog();
				if (System.Windows.Forms.DialogResult.Cancel == res)
					return true;
				if (chooser.HandleAnyJump())
					return true;
				if (chooser.ChosenOne != null)
				{
					int hvo = chooser.ChosenOne.Hvo;
					IPhNaturalClass pnc = PhNaturalClass.CreateFromDBObject(cache, hvo);
					ITsString tss = pnc.Abbreviation.BestAnalysisVernacularAlternative;
					string sName = tss.Text;
					string sIns = String.Format("[{0}]", sName);
					int wsPending = cache.DefaultVernWs;
					IVwRootSite site = rootb.Site;
					IVwGraphics vg = null;
					if (site != null)
						vg = site.get_ScreenGraphics(rootb);
					rootb.OnTyping(vg, sIns, 0, 0, '[', ref wsPending);
				}
			}
			return true;
		}

		/// <summary>
		/// Make the selection from the given database id.
		/// </summary>
		/// <param name="hvo"></param>
		public void MakeSelection(int hvo)
		{
			CheckDisposed();

			m_labelsTreeView.SelectedNode = FindNodeFromHvo(hvo);
			if (m_labelsTreeView.SelectedNode == null)
				m_labelsTreeView.SelectedNode = m_labelsTreeView.Nodes[0];

			if (m_labelsTreeView.SelectedNode != null)
			{
				m_labelsTreeView.SelectedNode.EnsureVisible();
				//for some reason, doesn't actually select it, so do this:
				m_labelsTreeView.SelectedNode.ForeColor = System.Drawing.Color.Blue;
			}
		}

		/// <summary>
		/// Return the database id of the currently selected node.
		/// </summary>
		public int SelectedHvo
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
							return label.Hvo;
					}
				}
				return 0;
			}
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
		int m_leafFlid;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LeafChooser"/> class.
		/// </summary>
		/// <param name="persistProvider">The persist provider.</param>
		/// <param name="labels">The labels.</param>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="rghvoChosen">The rghvo chosen.</param>
		/// <param name="leafFlid">The leaf flid.</param>
		/// ------------------------------------------------------------------------------------
		public LeafChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName, FdoCache cache, int[] rghvoChosen, int leafFlid)
			: base (persistProvider, fieldName, cache, rghvoChosen)
		{
			m_leafFlid = leafFlid;

			// Normally done by the base class constructor, but requires m_leafFlid to be set, so
			// we made a special constructor to finesse things.
			FinishConstructor(labels);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the label node.
		/// </summary>
		/// <param name="nol">The nol.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override LabelNode CreateLabelNode(ObjectLabel nol)
		{
			return new LeafLabelNode(nol, m_stylesheet, m_leafFlid);
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
			return label.Cache.MainCacheAccessor.get_VecSize(label.Hvo, m_leafFlid) > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected class LeafLabelNode : ReallySimpleListChooser.LabelNode
		{
			int m_leafFlid;
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:LeafLabelNode"/> class.
			/// </summary>
			/// <param name="label">The label.</param>
			/// <param name="stylesheet">The stylesheet.</param>
			/// <param name="leafFlid">The leaf flid.</param>
			/// --------------------------------------------------------------------------------
			public LeafLabelNode(ObjectLabel label, IVwStylesheet stylesheet, int leafFlid)
				: base(label, stylesheet)
			{
				m_leafFlid = leafFlid;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Adds the secondary nodes.
			/// </summary>
			/// <param name="node">The node.</param>
			/// <param name="nodes">The nodes.</param>
			/// <param name="rghvoChosen">The rghvo chosen.</param>
			/// --------------------------------------------------------------------------------
			public override void AddSecondaryNodes(LabelNode node, TreeNodeCollection nodes, List<int> rghvoChosen)
			{
				AddSecondaryNodesAndLookForSelected(node, nodes, null, 0, null, rghvoChosen);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Add secondary nodes to tree at nodes (and check any that occur in rghvoChosen),
			/// and return the one whose hvo is hvoToSelect, or nodeRepresentingCurrentChoice
			/// if none match.
			/// </summary>
			/// <param name="node">node to be added</param>
			/// <param name="nodes">where to add it</param>
			/// <param name="nodeRepresentingCurrentChoice">The node representing current choice.</param>
			/// <param name="hvoToSelect">The hvo to select.</param>
			/// <param name="ownershipStack">The ownership stack.</param>
			/// <param name="rghvoChosen">The rghvo chosen.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public override LabelNode AddSecondaryNodesAndLookForSelected(LabelNode node, TreeNodeCollection nodes,
				LabelNode nodeRepresentingCurrentChoice, int hvoToSelect, Stack ownershipStack, List<int> rghvoChosen)
			{
				LabelNode result = nodeRepresentingCurrentChoice; // result unless we match hvoToSelect
				ObjectLabel label = (ObjectLabel)this.Tag;
				ISilDataAccess sda = label.Cache.MainCacheAccessor;
				int chvo = sda.get_VecSize(label.Hvo, m_leafFlid);
				List<int> secItems = new List<int>(chvo);
				for (int i = 0; i < chvo; i++)
					secItems.Add(sda.get_VecItem(label.Hvo, m_leafFlid, i));
				ObjectLabelCollection secLabels = new ObjectLabelCollection(label.Cache,
					new List<int>(secItems),
					"ShortNameTSS", "analysis vernacular"); // Enhance JohnT: may want to make these configurable one day...
				foreach (ObjectLabel secLabel in secLabels)
				{
					// Perversely, we do NOT want a LeafLabelNode for the leaves, because their HVOS are the leaf type,
					// and therefore objects that do NOT possess the leaf property!
					LabelNode secNode = new LabelNode(secLabel, m_stylesheet);
					if (rghvoChosen != null)
						secNode.Checked = (rghvoChosen.BinarySearch(secLabel.Hvo) >= 0);
					node.Nodes.Add(secNode);
					if (secLabel.Hvo == hvoToSelect)
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
				return label.Cache.MainCacheAccessor.get_VecSize(label.Hvo, m_leafFlid) > 0;
			}
		}
	}

}
