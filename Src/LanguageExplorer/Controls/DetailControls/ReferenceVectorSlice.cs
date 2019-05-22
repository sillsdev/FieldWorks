// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lexicon;
using SIL.Code;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Class that can edit a field that is a sequence of object references, typically by launching a chooser.
	/// Also a base class for more specialized editors.
	/// Control should be a (subclass of) VectorReferenceLauncher.
	/// </summary>
	internal class ReferenceVectorSlice : ReferenceSlice
	{
		private int m_dxLastWidth; // remember width when OnSizeChanged called.

		/// <summary />
		protected ReferenceVectorSlice(Control control)
			: base(control)
		{
		}

		/// <summary />
		protected ReferenceVectorSlice(Control control, LcmCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
		}

		/// <summary />
		public ReferenceVectorSlice(LcmCache cache, ICmObject obj, int flid)
			: this(new VectorReferenceLauncher(), cache, obj, flid)
		{
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				var vrl = Control as VectorReferenceLauncher;
				if (vrl != null)
				{
					vrl.ViewSizeChanged -= OnViewSizeChanged;
					var view = (VectorReferenceView)vrl.MainControl;
					view.ViewSizeChanged -= OnViewSizeChanged;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override void FinishInit()
		{
			base.FinishInit();
			InitLauncher();
		}

		protected virtual void InitLauncher()
		{
			var vrl = (VectorReferenceLauncher)Control;
			vrl.Initialize(Cache, MyCmObject, m_flid, m_fieldName, PersistenceProvider, DisplayNameProperty, BestWsName); // TODO: Get better default 'best ws'.
			vrl.ConfigurationNode = ConfigurationNode;
			vrl.ViewSizeChanged += OnViewSizeChanged;
			var view = (VectorReferenceView)vrl.MainControl;
			view.ViewSizeChanged += OnViewSizeChanged;
			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			vrl.Visible = false;
		}

		internal Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuReorderVector(ContextMenuName contextMenuId)
		{
			Require.That(contextMenuId == ContextMenuName.mnuReorderVector, $"Expected argument value of '{ContextMenuName.mnuReorderVector.ToString()}', but got '{contextMenuId.ToString()}' instead.");

			// Start: <menu id="mnuReorderVector">
			// This menu and its commands are shared
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ContextMenuName.mnuReorderVector.ToString()
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

			ToolStripMenuItem menu;
			bool visible;
			var enabled = CanDisplayMoveTargetDownInSequence(out visible);
			if (visible)
			{
				// <command id="CmdMoveTargetToPreviousInSequence" label="Move Left" message="MoveTargetDownInSequence"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveReferencedTargetDownInSequence_Clicked, AreaResources.Move_Left);
				menu.Enabled = enabled;
			}
			enabled = CanDisplayMoveTargetUpInSequence(out visible);
			if (visible)
			{
				// <command id="CmdMoveTargetToNextInSequence" label="Move Right" message="MoveTargetUpInSequence"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveReferencedTargetUpInSequence_Clicked, AreaResources.Move_Right);
				menu.Enabled = enabled;
			}
			if (CanAlphabetize)
			{
				// <command id="CmdAlphabeticalOrder" label="Alphabetical Order" message="AlphabeticalOrder"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Referenced_AlphabeticalOrder_Clicked, LexiconResources.Alphabetical_Order);
			}
			// End: <menu id="mnuReorderVector">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void MoveReferencedTargetDownInSequence_Clicked(object sender, EventArgs e)
		{
			MyView.MoveItem(false);
		}

		private void MoveReferencedTargetUpInSequence_Clicked(object sender, EventArgs e)
		{
			MyView.MoveItem(true);
		}

		private void Referenced_AlphabeticalOrder_Clicked(object sender, EventArgs e)
		{
			MyView.RemoveOrdering();
		}

		public void MoveTargetDownInSequence()
		{
			MyView.MoveItem(false);
		}

		public bool CanDisplayMoveTargetDownInSequence(out bool visible)
		{
			return MyView.CanMoveItem(false, out visible);
		}

		public void MoveTargetUpInSequence()
		{
			MyView.MoveItem(true);
		}

		public bool CanDisplayMoveTargetUpInSequence(out bool visible)
		{
			return MyView.CanMoveItem(true, out visible);
		}

		public void Alphabetize()
		{
			MyView.RemoveOrdering();
		}

		public bool CanAlphabetize => MyView.RootPropertySupportsVirtualOrdering();

		private VectorReferenceView MyView => (VectorReferenceView)((VectorReferenceLauncher)Control).MainControl;

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (Width == m_dxLastWidth)
			{
				return;
			}
			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			var view = (VectorReferenceView)((VectorReferenceLauncher)Control).MainControl;
			view.PerformLayout();
			var h1 = view.RootBox.Height;
			var hNew = Math.Max(h1, ContainingDataTree.GetMinFieldHeight()) + 3;
			if (hNew != Height)
			{
				Height = hNew;
			}
		}

		public override void ShowSubControls()
		{
			base.ShowSubControls();
			Control.Visible = true;
		}

		/// <summary>
		/// Handle changes in the size of the underlying view.
		/// </summary>
		protected void OnViewSizeChanged(object sender, FwViewSizeEventArgs e)
		{
			// For now, just handle changes in the height.
			var vrl = (VectorReferenceLauncher)Control;
			var view = (VectorReferenceView)vrl.MainControl;
			var hMin = ContainingDataTree.GetMinFieldHeight();
			var h1 = view.RootBox.Height;
			Debug.Assert(e.Height == h1);
			var hOld = TreeNode.Height;
			var hNew = Math.Max(h1, hMin) + 3;
			if (hNew != hOld)
			{
				// JohnT: why all these -1's?
				Height = hNew - 1;
				// JohnT: don't know why we need this, vrl is the slice's control and is supposed to
				// be docked to fill the slice. But if we don't do it, there are cases where
				// narrowing the window makes the slice higher but not the embedded control.
				// The tree node is also supposed to be docked, but again, if we don't do this
				// then the tree node doesn't fill the height of the window, and clicks at the
				// bottom of it may not work.
				TreeNode.Height = hNew - 1;
				vrl.Height = hNew - 1;
				// The view height is docked to the launcher's.
			}
			if (Height != hNew - 1)
			{
				Height = hNew - 1;
			}
		}

		public override void RegisterWithContextHelper()
		{
			if (Control == null)
			{
				return;
			}
			if (Publisher != null)
			{
#if RANDYTODO
// TODO: Skip it for now, and figure out what to do with those context menus
					string caption = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "label", ""));
					var vrl = (VectorReferenceLauncher)Control;
					Publisher.Publish("RegisterHelpTargetWithId", new object[]{vrl.Controls[1], caption, HelpId});
					Publisher.Publish("RegisterHelpTargetWithId", new object[]{vrl.Controls[0], caption, HelpId, "Button"});
#endif
			}
		}
	}
}