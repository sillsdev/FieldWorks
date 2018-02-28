// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookEdit
{
	/// <summary>
	/// This is a slice for displaying and updating roled participants in
	/// Data Notebook records. It is actually a parent slice for multiple slices, one
	/// for each roled participants object. This slice displays the default roled
	/// participants, and the child slices display the participants for specific roles.
	/// </summary>
	internal sealed class RoledParticipantsSlice : CustomReferenceVectorSlice
	{
		public RoledParticipantsSlice()
			: base(new VectorReferenceLauncher())
		{
		}

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
			}

			base.Dispose(disposing);
		}

		private IRnGenericRec Record => (IRnGenericRec)Object;

		/// <summary />
		protected override void InitLauncher()
		{
			var defaultRoledPartic = Record.DefaultRoledParticipants;
			Func<ICmObject> defaultRoleCreator = null;
			if (defaultRoledPartic == null)
			{
				// Initialize the view with an action that can create one if the use clicks on it.
				defaultRoleCreator = () =>
					{
						// create a default roled participants object if it does not already exist
						NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
							() =>
								{
									defaultRoledPartic = Record.MakeDefaultRoledParticipant();
								});
						return defaultRoledPartic;
					};
			}

			// this slice displays the default roled participants
			var vrl = (VectorReferenceLauncher) Control;
			if (vrl.PropertyTable == null)
			{
				vrl.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			}
			vrl.Initialize(Cache, defaultRoledPartic, RnRoledParticTags.kflidParticipants, m_fieldName, PersistenceProvider,
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

		/// <summary />
		public override void GenerateChildren(XElement node, XElement caller, ICmObject obj, int indent, ref int insPos, ArrayList path, ObjSeqHashMap reuseMap, bool fUsePersistentExpansion)
		{
			foreach (var roledPartic in Record.ParticipantsOC)
			{
				if (roledPartic.RoleRA != null)
				{
					GenerateChildNode(roledPartic, node, caller, indent, ref insPos, path, reuseMap);
				}
			}
			Expansion = Record.ParticipantsOC.Count == 0 ? TreeItemState.ktisCollapsedEmpty : TreeItemState.ktisExpanded;
		}

		private void GenerateChildNode(IRnRoledPartic roledPartic, XElement node, XElement caller, int indent, ref int insPos, ArrayList path, ObjSeqHashMap reuseMap)
		{
			var sliceElem = new XElement("slice",
				new XAttribute("label", roledPartic.RoleRA.Name.BestAnalysisAlternative.Text),
				new XAttribute("field", "Participants"),
				new XAttribute("editor", "possVectorReference"),
				new XAttribute("menu", "mnuDataTree-Participants"));
			foreach (var childNode in node.Elements())
			{
				sliceElem.Add(XElement.Parse(childNode.GetOuterXml()));
			}
			node.ReplaceNodes(XElement.Parse(sliceElem.ToString()));
			CreateIndentedNodes(caller, roledPartic, indent, ref insPos, path, reuseMap, node);
			node.RemoveNodes();
		}

		/// <summary>
		/// Determine if the object really has data to be shown in the slice
		/// </summary>
		public static bool ShowSliceForVisibleIfData(XElement node, ICmObject obj)
		{
			// this slice does not have data if the only roled participants object
			// is the default roled participants and it does not contain any participants
			var rec = (IRnGenericRec) obj;
			if (rec.ParticipantsOC.Count > 1)
			{
				return true;
			}

			return rec.ParticipantsOC.Any(roledPartic => roledPartic.ParticipantsRC.Any());
		}

		private ContextMenuStrip m_contextMenuStrip;
		/// <summary />
		public override bool HandleMouseDown(Point p)
		{
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
			var existingRoles = Record.Roles;
			foreach (var role in Cache.LanguageProject.RolesOA.PossibilitiesOS)
			{
				// only display add menu options for roles that have not been added yet
				if (!existingRoles.Contains(role))
				{
					var label = string.Format(LanguageExplorerResources.ksAddParticipants, role.Name.BestAnalysisAlternative.Text);
					var item = new ToolStripMenuItem(label, null, AddParticipants) {Tag = role};
					contextMenuStrip.Items.Add(item);
				}
			}

			// display the standard visiblility and help menu items
			var tsdropdown = new ToolStripDropDownMenu();
			var itemAlways = new ToolStripMenuItem(LanguageExplorerResources.ksAlwaysVisible, null, ShowFieldAlwaysVisible);
			var itemIfData = new ToolStripMenuItem(LanguageExplorerResources.ksHiddenUnlessData, null, ShowFieldIfData);
			var itemHidden = new ToolStripMenuItem(LanguageExplorerResources.ksNormallyHidden, null, ShowFieldNormallyHidden);
			itemAlways.CheckOnClick = true;
			itemIfData.CheckOnClick = true;
			itemHidden.CheckOnClick = true;
			itemAlways.Checked = IsVisibilityItemChecked("always");
			itemIfData.Checked = IsVisibilityItemChecked("ifdata");
			itemHidden.Checked = IsVisibilityItemChecked("never");

			tsdropdown.Items.Add(itemAlways);
			tsdropdown.Items.Add(itemIfData);
			tsdropdown.Items.Add(itemHidden);
			var fieldVis = new ToolStripMenuItem(LanguageExplorerResources.ksFieldVisibility) { DropDown = tsdropdown };

			contextMenuStrip.Items.Add(new ToolStripSeparator());
			contextMenuStrip.Items.Add(fieldVis);
#if RANDYTODO
			Image imgHelp = ContainingDataTree.SmallImages.GetImage("Help");
			contextMenuStrip.Items.Add(new ToolStripMenuItem(LanguageExplorerResources.ksHelp, imgHelp, ShowHelpTopic));
#endif

			return contextMenuStrip;
		}

		private void AddParticipants(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem) sender;
			var role = (ICmPossibility) item.Tag;
			var roleName = role.Name.BestAnalysisAlternative.Text;
			var displayWs = "analysis vernacular";
			var node = ConfigurationNode?.Element("deParams");
			if (node != null)
			{
				displayWs = XmlUtils.GetOptionalAttributeValue(node, "ws", "analysis vernacular").ToLower();
			}
			var labels = ObjectLabel.CreateObjectLabels(Cache, Cache.LanguageProject.PeopleOA.PossibilitiesOS, DisplayNameProperty, displayWs);

			using (var chooser = new SimpleListChooser(PersistenceProvider, labels, m_fieldName,
				Cache, null, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				chooser.TextParamHvo = Cache.LanguageProject.PeopleOA.Hvo;
				chooser.SetHelpTopic(GetChooserHelpTopicID());
				if (ConfigurationNode != null)
				{
					chooser.InitializeExtras(ConfigurationNode, PropertyTable);
				}

				if (DialogResult.Cancel == chooser.ShowDialog())
				{
					return;
				}

				if (ConfigurationNode != null)
				{
					chooser.HandleAnyJump();
				}

				if (chooser.ChosenObjects == null)
				{
					return;
				}
				IRnRoledPartic roledPartic = null;
				UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerResources.ksUndoAddParticipants, roleName),
					string.Format(LanguageExplorerResources.ksRedoAddParticipants, roleName), role, () =>
					{
						roledPartic = Cache.ServiceLocator.GetInstance<IRnRoledParticFactory>().Create();
						Record.ParticipantsOC.Add(roledPartic);
						roledPartic.RoleRA = role;
						foreach (ICmPerson person in chooser.ChosenObjects)
						{
							roledPartic.ParticipantsRC.Add(person);
						}
					});
				ExpandNewNode(roledPartic);
			}
		}

		private void ShowFieldAlwaysVisible(object sender, EventArgs e)
		{
			SetFieldVisibility("always");
		}

		private void ShowFieldIfData(object sender, EventArgs e)
		{
			SetFieldVisibility("ifdata");
		}

		private void ShowFieldNormallyHidden(object sender, EventArgs e)
		{
			SetFieldVisibility("never");
		}

		private void ShowHelpTopic(object sender, EventArgs e)
		{
			var areaName = PropertyTable.GetValue<string>(AreaServices.AreaChoice);
			ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"),
				areaName == AreaServices.TextAndWordsAreaMachineName
					? "khtpField-notebookEdit-InterlinearEdit-RnGenericRec-Participants"
					: "khtpField-notebookEdit-CustomSlice-RnGenericRec-Participants");
		}

#if RANDYTODO
		public virtual bool OnDisplayDeleteParticipants(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = true;
			display.Visible = true;
			return true;
		}
#endif

		/// <summary />
		public bool OnDeleteParticipants(object args)
		{
			var slice = ContainingDataTree.CurrentSlice;
			var roledPartic = slice.Object as IRnRoledPartic;
			if (roledPartic != null)
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoDeleteParticipants, LanguageExplorerResources.ksRedoDeleteParticipants, roledPartic,
					() => Record.ParticipantsOC.Remove(roledPartic));
				Collapse();
				Expand();
			}
			return true;
		}

		/// <summary>
		/// Updates the display of a slice, if an hvo and tag it cares about has changed in some way.
		/// </summary>
		protected internal override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
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
			using (new DataTreeLayoutSuspensionHelper(PropertyTable.GetValue<IFwMainWnd>("window"), ContainingDataTree))
			{
				XElement caller = null;
				if (Key.Length > 1)
				{
					caller = Key[Key.Length - 2] as XElement;
				}
				var insPos = IndexInContainer + Record.ParticipantsOC.Count - 1;
				GenerateChildNode(roledPartic, ConfigurationNode, caller, Indent, ref insPos, new ArrayList(Key), new ObjSeqHashMap());
				Expansion = TreeItemState.ktisExpanded;
			}
		}

		/// <summary>
		/// Expand this node, which is at position iSlice in its parent.
		/// </summary>
		public override void Expand(int iSlice)
		{
			using (new DataTreeLayoutSuspensionHelper(PropertyTable.GetValue<IFwMainWnd>("window"), ContainingDataTree))
			{
				XElement caller = null;
				if (Key.Length > 1)
				{
					caller = Key[Key.Length - 2] as XElement;
				}
				var insPos = iSlice + 1;
				GenerateChildren(ConfigurationNode, caller, Object, Indent, ref insPos, new ArrayList(Key), new ObjSeqHashMap(), false);
				Expansion = TreeItemState.ktisExpanded;
			}
		}

		/// <summary />
		public override void AboutToDiscard()
		{
			if (Record != null && Record.IsValidObject)
			{
				var rgDel = Record.ParticipantsOC.Where(roledPartic => roledPartic.RoleRA != null && !roledPartic.ParticipantsRC.Any()).ToList();
				if (rgDel.Any())
				{
					// remove all empty roled participants when we leave this record
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					{
						foreach (var roledPartic in rgDel)
						{
							Record.ParticipantsOC.Remove(roledPartic);
						}
					});
				}
			}
			base.AboutToDiscard();
		}
	}
}