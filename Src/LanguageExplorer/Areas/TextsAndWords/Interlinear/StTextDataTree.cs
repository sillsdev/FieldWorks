// Copyright (c) 2007-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal sealed class StTextDataTree : DataTree
	{
		private InfoPane m_infoPane;

		internal InfoPane InfoPane
		{
			set { m_infoPane = value; }
		}

		internal StTextDataTree(ISharedEventHandlers sharedEventHandlers, LcmCache cache)
			: base(sharedEventHandlers, false)
		{
			Cache = cache;
			InitializeBasic(cache, false);
			InitializeComponent();
		}

		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			// Set up Slice menu: "mnuTextInfo_Notebook"
			this.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuTextInfo_Notebook, Create_mnuTextInfo_Notebook);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuTextInfo_Notebook(Slice slice, ContextMenuName contextMenuId)
		{
			Require.That(contextMenuId == ContextMenuName.mnuTextInfo_Notebook, $"Expected argument value of '{ContextMenuName.mnuTextInfo_Notebook.ToString()}', but got '{contextMenuId.ToString()}' instead.");

			// Start: <menu id="mnuTextInfo_Notebook">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ContextMenuName.mnuTextInfo_Notebook.ToString()
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			// <item command="CmdJumpToNotebook"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, JumpToNotebook_Clicked, TextAndWordsResources.Show_Record_in_Notebook);

			// End: <menu id="mnuTextInfo_Notebook">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void JumpToNotebook_Clicked(object sender, EventArgs e)
		{
			/*
			<command id="CmdJumpToNotebook" label="Show Record in Notebook" message="JumpToTool">
				<parameters tool="notebookEdit" className="RnGenericRecord"/>
			</command>
			*/
			var currentObject = CurrentSlice.MyCmObject;
			if (currentObject is IText)
			{
				currentObject = ((IText)currentObject).AssociatedNotebookRecord;
			}
			LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(AreaServices.NotebookEditToolMachineName, currentObject.Guid));
		}

		protected override void SetDefaultCurrentSlice(bool suppressFocusChange)
		{
			base.SetDefaultCurrentSlice(suppressFocusChange);
			// currently we always want the focus in the first slice by default,
			// since the user cannot control the governing browse view with a cursor.
			if (!suppressFocusChange && CurrentSlice == null)
			{
				FocusFirstPossibleSlice();
			}
		}

		public override void ShowObject(ICmObject root, string layoutName, string layoutChoiceField, ICmObject descendant, bool suppressFocusChange)
		{
			if (m_infoPane != null && m_infoPane.CurrentRootHvo == 0)
			{
				return;
			}
			var showObj = root;
			ICmObject stText;
			if (root.ClassID == CmBaseAnnotationTags.kClassId)  // RecordList is tracking the annotation
			{
				// This pane, as well as knowing how to work with a record list of Texts, knows
				// how to work with one of CmBaseAnnotations, that is, a list of occurrences of
				// a word.
				var cba = (ICmBaseAnnotation)root;
				var cmoPara = cba.BeginObjectRA;
				stText = cmoPara.Owner;
				showObj = stText;
			}
			else
			{
				stText = root;
			}
			if (stText.OwningFlid == TextTags.kflidContents)
			{
				showObj = stText.Owner;
			}
			base.ShowObject(showObj, layoutName, layoutChoiceField, showObj, suppressFocusChange);
		}
	}
}