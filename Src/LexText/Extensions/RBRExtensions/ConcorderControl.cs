using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.XWorks;
using XCore;

namespace RBRExtensions
{
	internal partial class ConcorderControl : UserControl, IxCoreColleague
	{
		private Mediator m_mediator;
		ICmObject m_selectedFinder;

		internal ICmObject SelectedFinder
		{
			set
			{
				m_selectedFinder = value;
				UsedByFiller ubf = m_cbUsedBy.SelectedItem as UsedByFiller;
				if (ubf != null)
					ubf.LoadList(m_splitContainer.Panel2, value);
			}
		}

		internal ConcorderControl()
		{
			InitializeComponent();

			// SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize.
			//m_splitContainer.Panel1MinSize = 178;
			//m_splitContainer.Panel2MinSize = 178;
		}

		#region IxCoreColleague implementation

		/// <summary>
		///
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			mediator.AddColleague(this);
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			List<IxCoreColleague> colleagues = new List<IxCoreColleague>();
			colleagues.Add(this);
			// Add current FindComboFiller & UsedByFiller.
			// Those
			return colleagues.ToArray();
		}

		#endregion IxCoreColleague implementation

		/// <summary>
		/// This method assumes all of the Find and UsedBy items are included in the fcfList.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="fcfList"></param>
		/// <param name="startingItem"></param>
		internal void SetupDlg(Mediator mediator, List<FindComboFillerBase> fcfList, FindComboFillerBase startingItem)
		{
			if (mediator == null)
				throw new ArgumentException("No Mediator.");
			if (fcfList == null)
				throw new ArgumentException("No items found.");
			if (fcfList.Count < 1)
				throw new ArgumentException("There has to be at least one item.");
			foreach (FindComboFillerBase fcf in fcfList)
			{
				if (fcf.List_UBF.Count == 0)
					throw new ArgumentException("No sub-items found.");
			}
			if (startingItem != null && !fcfList.Contains(startingItem))
				throw new ArgumentException("'startingItem' is not in the 'fcfList' list.");

			m_mediator = mediator;
			m_cbFind.BeginUpdate();
			m_cbFind.Items.Clear();
			m_cbFind.Items.AddRange(fcfList.ToArray());
			m_cbFind.EndUpdate();
			m_cbFind.SelectedItem = startingItem;
			m_mediator.BroadcastPendingItems();
		}

		#region Event Handlers

		/// <summary>
		/// This is the primary combo box.
		/// When an item is selected, then the secondary combo box can be
		/// filled with approriate items.
		/// With the selected itme here, we can also populate the
		/// left side control (the one below the combo box).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_cbFind_SelectedIndexChanged(object sender, EventArgs e)
		{
			Cursor oldState = FindForm().Cursor;
			m_selectedFinder = null;
			FindForm().Cursor = Cursors.WaitCursor;
			FindComboFillerBase fcf = m_cbFind.SelectedItem as FindComboFillerBase;
			m_cbUsedBy.BeginUpdate();
			m_cbUsedBy.Items.Clear();
			m_cbUsedBy.Items.AddRange(fcf.List_UBF.ToArray());
			m_cbUsedBy.EndUpdate();
			fcf.LoadList(m_mediator, m_splitContainer.Panel1);
			if (m_cbUsedBy.Items.Count > 0)
				m_cbUsedBy.SelectedIndex = 0;
			else
				m_cbUsedBy.SelectedItem = null;

			FindForm().Cursor = oldState;
		}

		/// <summary>
		/// This is the secondary combo box. When an item is selected,
		/// we can determine what to show in the control below
		/// the secondary combo box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_cbUsedBy_SelectedIndexChanged(object sender, EventArgs e)
		{
			Cursor oldState = FindForm().Cursor;
			FindForm().Cursor = Cursors.WaitCursor;
			UsedByFiller ubf = m_cbUsedBy.SelectedItem as UsedByFiller;
			ubf.LoadList(m_splitContainer.Panel2, m_selectedFinder);
			FindForm().Cursor = oldState;
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			FindForm().Close();
		}

		#endregion Event Handlers

		#region internal classes

		internal abstract class FindComboFillerBase : IxCoreColleague
		{
			private string m_label;
			private List<UsedByFiller> m_ubfList = new List<UsedByFiller>();
			protected XmlNode m_configurationNode;
			protected Control m_currentControl;
			protected Mediator m_mediator;

			/// <summary>
			/// Internal getter used to fill the 'Find' combo in the ConcorderControl class.
			/// </summary>
			internal List<UsedByFiller> List_UBF
			{
				get { return m_ubfList; }
			}

			protected ConcorderControl MainControl
			{
				get { return m_currentControl.Parent.Parent.Parent as ConcorderControl; }
			}

			internal FindComboFillerBase()
			{
			}

			internal void LoadList(Mediator mediator, SplitterPanel parent)
			{
				parent.FindForm().UseWaitCursor = true;
				parent.SuspendLayout();
				RemovePreviousControl();

				LoadListInternal(mediator, parent);

				parent.ResumeLayout();
				parent.FindForm().UseWaitCursor = false;
			}

			protected virtual void LoadListInternal(Mediator mediator, SplitterPanel parent)
			{
			}

			protected virtual void RemovePreviousControl()
			{
				m_mediator.RemoveColleague(this);
				if (m_currentControl != null)
				{
					m_currentControl.SuspendLayout();
					m_currentControl.Parent.Controls.Remove(m_currentControl);
					m_currentControl.Dispose();
					m_currentControl = null;
				}
			}

			#region IxCoreColleague implementation

			/// <summary>
			///
			/// </summary>
			/// <param name="mediator"></param>
			/// <param name="configurationParameters"></param>
			public virtual void Init(Mediator mediator, XmlNode configurationParameters)
			{
				m_mediator = mediator;
				m_configurationNode = configurationParameters;
				m_label = m_configurationNode.Attributes["label"].Value;

				// Add a UsedByFiller for each control node in the targetcontrols element.
				foreach (XmlNode targetControlNode in configurationParameters.SelectNodes("targetcontrols/control"))
				{
					UsedByFiller ubf = new UsedByFiller();
					m_ubfList.Add(ubf);
					ubf.Init(mediator, targetControlNode);
				}
			}

			/// <summary>
			/// return an array of all of the objects which should
			/// 1) be queried when looking for someone to deliver a message to
			/// 2) be potential recipients of a broadcast
			/// </summary>
			/// <returns></returns>
			public virtual IxCoreColleague[] GetMessageTargets()
			{
				List<IxCoreColleague> colleagues = new List<IxCoreColleague>();
				colleagues.Add(this);
				return colleagues.ToArray();
			}

			#endregion IxCoreColleague implementation

			public override string ToString()
			{
				return m_label;
			}
		}

		internal class FindComboFiller : FindComboFillerBase
		{
			internal FindComboFiller()
			{
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="argument"></param>
			/// <returns>
			/// true; we fully handled it.
			/// </returns>
			public bool OnRecordNavigation(object argument)
			{
				RecordNavigationInfo rni = argument as RecordNavigationInfo;
				RecordClerk clerk = rni.Clerk;
				string clerkId = clerk.Id;
				string myClerkId = m_configurationNode.SelectSingleNode("parameters").Attributes["clerk"].Value;
				if (clerk.Id == myClerkId)
				{
					MainControl.SelectedFinder = rni.Clerk.CurrentObject;

					return true;
				}
				else
					return false;
			}

			protected override void LoadListInternal(Mediator mediator, SplitterPanel parent)
			{
				// Add the new browse view, if available in the config node.
				if (m_configurationNode.HasChildNodes)
				{
					RecordBrowseView browseView = new RecordBrowseView();
					browseView.SuspendLayout();
					browseView.Dock = DockStyle.Fill;
					m_currentControl = browseView;
					parent.Controls.Add(browseView);
					browseView.Init(mediator, m_configurationNode.SelectSingleNode("parameters"));
					mediator.RemoveColleague(browseView);
					browseView.BringToFront();
					browseView.ResumeLayout();
					m_mediator.AddColleague(this);
				}
				base.LoadListInternal(mediator, parent);
			}
		}

		internal class FindPossibilityComboFiller : FindComboFillerBase
		{
			private ICmPossibilityList m_possibilityList;

			internal FindPossibilityComboFiller(ICmPossibilityList list)
				: base()
			{
				Debug.Assert(list != null);

				m_possibilityList = list;
			}

			protected override void LoadListInternal(Mediator mediator, SplitterPanel parent)
			{
				parent.FindForm().UseWaitCursor = true;
				parent.SuspendLayout();
				RemovePreviousControl();

				TreeView tv = new TreeView();
				tv.SuspendLayout();
				tv.Dock = DockStyle.Fill;
				AddPossibilities(tv.Nodes, m_possibilityList.PossibilitiesOS);
				tv.AfterSelect += new TreeViewEventHandler(tv_AfterSelect);
				m_currentControl = tv;
				tv.HideSelection = false;
				parent.Controls.Add(tv);
				tv.BringToFront();
				tv.ResumeLayout();
				parent.ResumeLayout();
				parent.FindForm().UseWaitCursor = false;

				base.LoadListInternal(mediator, parent);

				tv.SelectedNode = tv.Nodes[0];
			}

			protected override void RemovePreviousControl()
			{
				if (m_currentControl != null)
				{
					TreeView tv = m_currentControl as TreeView;
					tv.AfterSelect -= new TreeViewEventHandler(tv_AfterSelect);
				}
				base.RemovePreviousControl();
			}

			void tv_AfterSelect(object sender, TreeViewEventArgs e)
			{
				MainControl.SelectedFinder = e.Node.Tag as ICmObject;
			}

			private void AddPossibilities(TreeNodeCollection tnc, FdoOwningSequence<ICmPossibility> possibilities)
			{
				foreach (ICmPossibility poss in possibilities)
				{
					TreeNode tn = new TreeNode(poss.Name.BestAnalysisVernacularAlternative.Text);
					tn.Tag = poss;
					tnc.Add(tn);
					AddPossibilities(tn.Nodes, poss.SubPossibilitiesOS);
				}
			}
		}

		internal class UsedByFiller : IxCoreColleague
		{
			private Control m_currentControl;
			private Mediator m_mediator;
			private XmlNode m_configurationNode;
			private string m_label;
			private Orientation m_orientation = Orientation.Vertical;

			/// <summary>
			/// Internal getter used to fill the 'Find' combo in the ConcorderControl class.
			/// </summary>
			internal Orientation Orientation
			{
				get { return m_orientation; }
			}

			internal void LoadList(SplitterPanel parent, ICmObject mainObject)
			{
				parent.FindForm().UseWaitCursor = true;
				parent.SuspendLayout();
				m_mediator.RemoveColleague(this);
				if (m_currentControl != null)
				{
					m_currentControl.SuspendLayout();
					m_currentControl.Parent.Controls.Remove(m_currentControl);
					m_currentControl.Dispose();
					m_currentControl = null;
				}

				// Add the new browse view, if available in the config node.
				if (m_configurationNode.HasChildNodes)
				{
					XmlNode parms = m_configurationNode.SelectSingleNode("parameters");
					if (mainObject != null)
					{
						RecordClerk clerk = (RecordClerk)m_mediator.PropertyTable.GetValue("RecordClerk-" + parms.Attributes["clerk"].Value);
						if (clerk == null)
							clerk = RecordClerkFactory.CreateClerk(m_mediator, parms);
						clerk.OwningObject = mainObject;
					}
					RecordBrowseView browseView = new RecordBrowseView();
					browseView.SuspendLayout();
					browseView.Dock = DockStyle.Fill;
					m_currentControl = browseView;
					parent.Controls.Add(browseView);
					browseView.Init(m_mediator, parms);
					m_mediator.RemoveColleague(browseView);
					browseView.BringToFront();
					browseView.ResumeLayout();
					m_mediator.AddColleague(this);
				}

				parent.ResumeLayout();
				parent.FindForm().UseWaitCursor = false;
			}

			internal UsedByFiller()
			{
			}

			public override string ToString()
			{
				return m_label;
			}

			#region IxCoreColleague implementation

			/// <summary>
			///
			/// </summary>
			/// <param name="mediator"></param>
			/// <param name="configurationParameters"></param>
			public void Init(Mediator mediator, XmlNode configurationParameters)
			{
				m_mediator = mediator;
				m_configurationNode = configurationParameters;
				m_label = m_configurationNode.Attributes["id"].Value;
				m_orientation = Orientation.Vertical;
			}

			/// <summary>
			/// return an array of all of the objects which should
			/// 1) be queried when looking for someone to deliver a message to
			/// 2) be potential recipients of a broadcast
			/// </summary>
			/// <returns></returns>
			public IxCoreColleague[] GetMessageTargets()
			{
				List<IxCoreColleague> colleagues = new List<IxCoreColleague>();
				colleagues.Add(this);
				return colleagues.ToArray();
			}

			#endregion IxCoreColleague implementation
		}

		#endregion internal classes
	}
}
