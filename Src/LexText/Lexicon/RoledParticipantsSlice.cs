using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// This is a slice for displaying and updating roled participants in
	/// Data Notebook records. It is actually a parent slice for multiple slices, one
	/// for each roled participants object. This slice displays the default roled
	/// participants, and the child slices display the participants for specific roles.
	/// </summary>
	public class RoledParticipantsSlice : CustomReferenceVectorSlice
	{
		public RoledParticipantsSlice()
			: base(new VectorReferenceLauncher())
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (Mediator != null)
					Mediator.RemoveColleague(this);
			}

			base.Dispose(disposing);
		}

		private IRnGenericRec Record
		{
			get
			{
				return (IRnGenericRec) m_obj;
			}
		}

		public override void FinishInit()
		{
			CheckDisposed();

			base.FinishInit();
			Mediator.AddColleague(this);
		}

		protected override void InitLauncher()
		{
			CheckDisposed();

			IRnRoledPartic defaultRoledPartic = Record.DefaultRoledParticipants;
			Func<ICmObject> defaultRoleCreator = null;
			if (defaultRoledPartic == null)
			{
				// Initialize the view with an action that can create one if the use clicks on it.
				defaultRoleCreator = () =>
					{
						// create a default roled participants object if it does not already exist
						NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor,
							() =>
								{
									defaultRoledPartic = Record.MakeDefaultRoledParticipant();
								});
						return defaultRoledPartic;
					};
			}

			// this slice displays the default roled participants
			var vrl = (VectorReferenceLauncher) Control;
			vrl.Initialize(m_cache, defaultRoledPartic, RnRoledParticTags.kflidParticipants, m_fieldName, m_persistenceProvider, Mediator,
				DisplayNameProperty,
				BestWsName); // TODO: Get better default 'best ws'.
			vrl.ObjectCreator = defaultRoleCreator;
			vrl.ConfigurationNode = ConfigurationNode;
			vrl.ViewSizeChanged += OnViewSizeChanged;
			var view = (VectorReferenceView) vrl.MainControl;
			view.ViewSizeChanged += OnViewSizeChanged;
			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			vrl.Visible = false;
		}

		public override void GenerateChildren(XmlNode node, XmlNode caller, ICmObject obj, int indent, ref int insPos,
			ArrayList path, ObjSeqHashMap reuseMap, bool fUsePersistentExpansion)
		{
			CheckDisposed();

			foreach (IRnRoledPartic roledPartic in Record.ParticipantsOC)
			{
				if (roledPartic.RoleRA != null)
					GenerateChildNode(roledPartic, node, caller, indent, ref insPos, path, reuseMap);
			}
			Expansion = Record.ParticipantsOC.Count == 0 ? DataTree.TreeItemState.ktisCollapsedEmpty : DataTree.TreeItemState.ktisExpanded;
		}

		private void GenerateChildNode(IRnRoledPartic roledPartic, XmlNode node, XmlNode caller, int indent,
			ref int insPos, ArrayList path, ObjSeqHashMap reuseMap)
		{
			var sliceElem = new XElement("slice",
				new XAttribute("label", roledPartic.RoleRA.Name.BestAnalysisAlternative.Text),
				new XAttribute("field", "Participants"),
				new XAttribute("editor", "possVectorReference"),
				new XAttribute("menu", "mnuDataTree-Participants"));
			foreach (XmlNode childNode in node.ChildNodes)
			{
				if (childNode.NodeType != XmlNodeType.Comment)
					sliceElem.Add(XElement.Parse(childNode.OuterXml));
			}
			node.InnerXml = sliceElem.ToString();
			CreateIndentedNodes(caller, roledPartic, indent, ref insPos, path, reuseMap, node);
			node.InnerXml = "";
		}

		/// <summary>
		/// Determine if the object really has data to be shown in the slice
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="obj">object to check</param>
		/// <returns>true if the slice contains data, otherwise false</returns>
		public static bool ShowSliceForVisibleIfData(XmlNode node, ICmObject obj)
		{
			// this slice does not have data if the only roled participants object
			// is the default roled participants and it does not contain any participants
			var rec = (IRnGenericRec) obj;
			if (rec.ParticipantsOC.Count > 1)
				return true;
			foreach (IRnRoledPartic roledPartic in rec.ParticipantsOC)
			{
				if (roledPartic.ParticipantsRC.Count > 0)
					return true;
			}
			return false;
		}

		private ContextMenuStrip m_contextMenuStrip;
		public override bool HandleMouseDown(Point p)
		{
			CheckDisposed();
			DisposeContextMenu(this, new EventArgs());
			m_contextMenuStrip = CreateContextMenu();
			m_contextMenuStrip.Closed += contextMenuStrip_Closed; // dispose when no longer needed (but not sooner! needed after this returns)
			m_contextMenuStrip.Show(TreeNode, p);
			return true;
		}

		void contextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			// It's apparently still needed by the menu handling code in .NET.
			// So we can't dispose it yet.
			// But we want to eventually (Eberhard says if it has a Dispose we MUST call it to make Mono happy)
			Application.Idle += DisposeContextMenu;
		}

		void DisposeContextMenu(object sender, EventArgs e)
		{
			Application.Idle -= DisposeContextMenu;
			if (m_contextMenuStrip != null && !m_contextMenuStrip.IsDisposed)
			{
				m_contextMenuStrip.Dispose();
				m_contextMenuStrip = null;
			}
		}
		private ContextMenuStrip CreateContextMenu()
		{
			var contextMenuStrip = new ContextMenuStrip();

			IEnumerable<ICmPossibility> existingRoles = Record.Roles;
			foreach (ICmPossibility role in m_cache.LanguageProject.RolesOA.PossibilitiesOS)
			{
				// only display add menu options for roles that have not been added yet
				if (!existingRoles.Contains(role))
				{
					string label = string.Format(LexEdStrings.ksAddParticipants, role.Name.BestAnalysisAlternative.Text);
					var item = new ToolStripMenuItem(label, null, AddParticipants) {Tag = role};
					contextMenuStrip.Items.Add(item);
				}
			}

			// display the standard visiblility and help menu items
			var tsdropdown = new ToolStripDropDownMenu();
			var itemAlways = new ToolStripMenuItem(LexEdStrings.ksAlwaysVisible, null, ShowFieldAlwaysVisible);
			var itemIfData = new ToolStripMenuItem(LexEdStrings.ksHiddenUnlessData, null, ShowFieldIfData);
			var itemHidden = new ToolStripMenuItem(LexEdStrings.ksNormallyHidden, null, ShowFieldNormallyHidden);
			itemAlways.CheckOnClick = true;
			itemIfData.CheckOnClick = true;
			itemHidden.CheckOnClick = true;
			itemAlways.Checked = IsVisibilityItemChecked("always");
			itemIfData.Checked = IsVisibilityItemChecked("ifdata");
			itemHidden.Checked = IsVisibilityItemChecked("never");

			tsdropdown.Items.Add(itemAlways);
			tsdropdown.Items.Add(itemIfData);
			tsdropdown.Items.Add(itemHidden);
			var fieldVis = new ToolStripMenuItem(LexEdStrings.ksFieldVisibility) {DropDown = tsdropdown};

			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(fieldVis);
			Image imgHelp = ContainingDataTree.SmallImages.GetImage("Help");
			contextMenuStrip.Items.Add(new ToolStripMenuItem(LexEdStrings.ksHelp, imgHelp, ShowHelpTopic));

			return contextMenuStrip;
		}

		private void AddParticipants(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem) sender;
			var role = (ICmPossibility) item.Tag;
			string roleName = role.Name.BestAnalysisAlternative.Text;
			string displayWs = "analysis vernacular";
			if (m_configurationNode != null)
			{
				XmlNode node = m_configurationNode.SelectSingleNode("deParams");
				if (node != null)
					displayWs = XmlUtils.GetAttributeValue(node, "ws", "analysis vernacular").ToLower();
			}
			IEnumerable<ObjectLabel> labels = ObjectLabel.CreateObjectLabels(m_cache, m_cache.LanguageProject.PeopleOA.PossibilitiesOS,
				DisplayNameProperty, displayWs);

			using (var chooser = new SimpleListChooser(m_persistenceProvider, labels, m_fieldName,
				m_cache, null, m_mediator.HelpTopicProvider))
			{
				chooser.TextParamHvo = m_cache.LanguageProject.PeopleOA.Hvo;
				chooser.SetHelpTopic(GetChooserHelpTopicID());
				if (m_configurationNode != null)
					chooser.InitializeExtras(m_configurationNode, Mediator);

				DialogResult res = chooser.ShowDialog();
				if (DialogResult.Cancel == res)
					return;

				if (m_configurationNode != null)
					chooser.HandleAnyJump();

				if (chooser.ChosenObjects != null)
				{
					IRnRoledPartic roledPartic = null;
					UndoableUnitOfWorkHelper.Do(string.Format(LexEdStrings.ksUndoAddParticipants, roleName),
						string.Format(LexEdStrings.ksRedoAddParticipants, roleName), role, () =>
					{
						roledPartic = m_cache.ServiceLocator.GetInstance<IRnRoledParticFactory>().Create();
						Record.ParticipantsOC.Add(roledPartic);
						roledPartic.RoleRA = role;
						foreach (ICmPerson person in chooser.ChosenObjects)
							roledPartic.ParticipantsRC.Add(person);
					});
					ExpandNewNode(roledPartic);
				}
			}
		}

		private void ShowFieldAlwaysVisible(object sender, EventArgs e)
		{
			CheckDisposed();
			SetFieldVisibility("always");
		}

		private void ShowFieldIfData(object sender, EventArgs e)
		{
			CheckDisposed();
			SetFieldVisibility("ifdata");
		}

		private void ShowFieldNormallyHidden(object sender, EventArgs e)
		{
			CheckDisposed();
			SetFieldVisibility("never");
		}

		private void ShowHelpTopic(object sender, EventArgs e)
		{
			CheckDisposed();
			string areaName = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
			if (areaName == "textsWords")
				ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, "khtpField-notebookEdit-InterlinearEdit-RnGenericRec-Participants");
			else
				ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, "khtpField-notebookEdit-CustomSlice-RnGenericRec-Participants");
		}

		public virtual bool OnDisplayDeleteParticipants(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = true;
			display.Visible = true;
			return true;
		}

		public bool OnDeleteParticipants(object args)
		{
			CheckDisposed();

			Slice slice = ContainingDataTree.CurrentSlice;
			var roledPartic = slice.Object as IRnRoledPartic;
			if (roledPartic != null)
			{
				UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoDeleteParticipants, LexEdStrings.ksRedoDeleteParticipants, roledPartic,
					() => Record.ParticipantsOC.Remove(roledPartic));
				Collapse();
				Expand();
			}
			return true;
		}

		/// <summary>
		/// Updates the display of a slice, if an hvo and tag it cares about has changed in some way.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>true, if it the slice updated its display</returns>
		protected override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			CheckDisposed();

			// Can't check hvo since it may have been deleted by an undo operation already.
			if (Record.Hvo == hvo && tag == RnGenericRecTags.kflidParticipants)
			{
				// if this flickers too annoyingly, we can probably optimize by extracting relevant lines from Collapse.
				Collapse();
				Expand();
				return true;
			}

			return false;
		}

		private void ExpandNewNode(IRnRoledPartic roledPartic)
		{
			try
			{
				ContainingDataTree.DeepSuspendLayout();
				XmlNode caller = null;
				if (Key.Length > 1)
					caller = Key[Key.Length - 2] as XmlNode;
				int insPos = IndexInContainer + Record.ParticipantsOC.Count - 1;
				GenerateChildNode(roledPartic, m_configurationNode, caller, Indent, ref insPos, new ArrayList(Key), new ObjSeqHashMap());
				Expansion = DataTree.TreeItemState.ktisExpanded;
			}
			finally
			{
				ContainingDataTree.DeepResumeLayout();
			}

		}

		/// <summary>
		/// Expand this node, which is at position iSlice in its parent.
		/// </summary>
		/// <param name="iSlice"></param>
		public override void Expand(int iSlice)
		{
			CheckDisposed();
			try
			{
				ContainingDataTree.DeepSuspendLayout();
				XmlNode caller = null;
				if (Key.Length > 1)
					caller = Key[Key.Length - 2] as XmlNode;
				int insPos = iSlice + 1;
				GenerateChildren(m_configurationNode, caller, m_obj, Indent, ref insPos, new ArrayList(Key), new ObjSeqHashMap(), false);
				Expansion = DataTree.TreeItemState.ktisExpanded;
			}
			finally
			{
				ContainingDataTree.DeepResumeLayout();
			}
		}

		public override void AboutToDiscard()
		{
			CheckDisposed();
			if (Record != null && Record.IsValidObject)
			{
				List<IRnRoledPartic> rgDel = new List<IRnRoledPartic>();
				foreach (IRnRoledPartic roledPartic in Record.ParticipantsOC)
				{
					if (roledPartic.RoleRA != null && roledPartic.ParticipantsRC.Count == 0)
						rgDel.Add(roledPartic);
				}
				if (rgDel.Count > 0)
				{
					// remove all empty roled participants when we leave this record
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					{
						foreach (IRnRoledPartic roledPartic in rgDel)
							Record.ParticipantsOC.Remove(roledPartic);
					});
				}
			}
			base.AboutToDiscard();
		}
	}
}
