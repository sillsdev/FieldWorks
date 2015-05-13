// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PhoneEnvReferenceSlice.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for PhoneEnvReferenceSlice.
	/// </summary>
	public class PhoneEnvReferenceSlice : ReferenceSlice
	{
		private int m_dxLastWidth; // width last time OnSizeChanged was called.

		public PhoneEnvReferenceSlice(FdoCache cache, ICmObject obj, int flid)
			: base(new PhoneEnvReferenceLauncher(), cache, obj, flid)
		{
			Debug.Assert(obj is IMoAffixAllomorph || obj is IMoStemAllomorph);
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				var rl = Control as PhoneEnvReferenceLauncher;
				if (rl != null)
				{
					rl.ViewSizeChanged -= OnViewSizeChanged;
					var view = (PhoneEnvReferenceView)rl.MainControl;
					view.ViewSizeChanged -= OnViewSizeChanged;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// This method is called to handle Undo/Redo operations on this slice.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		protected internal override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			if (tag == Flid)
			{
				PhoneEnvReferenceLauncher rl = Control as PhoneEnvReferenceLauncher;
				if (rl != null)
				{
					PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
					view.ResynchListToDatabaseAndRedisplay();
					return true;
				}
			}
			return base.UpdateDisplayIfNeeded(hvo, tag);
		}

		public override void FinishInit()
		{
			base.FinishInit();

			var rl = (PhoneEnvReferenceLauncher)Control;
			rl.Initialize(m_cache, m_obj, m_flid, m_fieldName, m_persistenceProvider, Mediator, m_propertyTable, null, null);
			rl.ConfigurationNode = ConfigurationNode;
			rl.ViewSizeChanged += OnViewSizeChanged;
			var view = (PhoneEnvReferenceView)rl.MainControl;
			view.ViewSizeChanged += OnViewSizeChanged;
			view.LayoutSizeChanged += view_LayoutSizeChanged;
		}

		// JohnT: this is the proper way to detect changes in height that come from editing within the view.
		// Probably the private ViewSizeChanged event isn't really needed but I'm leaving it for now just in case.
		void view_LayoutSizeChanged(object sender, EventArgs e)
		{
			PhoneEnvReferenceLauncher rl = Control as PhoneEnvReferenceLauncher;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			this.OnViewSizeChanged(this, new FwViewSizeEventArgs(view.RootBox.Height, view.RootBox.Width));
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			if (this.Width == m_dxLastWidth)
				return;
			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			ReferenceLauncher rl = (ReferenceLauncher)this.Control;
			RootSite rs = (RootSite)rl.MainControl;
			rs.PerformLayout();
			if (rs.RootBox != null)
			{
				// Allow it to be the height it wants + fluff to get rid of scroll bar.
				// Adjust our own height to suit.
				// Note that this may produce a recursive call!
				this.Height = rs.RootBox.Height + 8;
			}
		}

		public override void RegisterWithContextHelper()
		{
			CheckDisposed();

			string caption = XmlUtils.GetLocalizedAttributeValue(ConfigurationNode, "label", "");

			PhoneEnvReferenceLauncher launcher = (PhoneEnvReferenceLauncher)this.Control;
			Mediator.SendMessage("RegisterHelpTargetWithId",
				new object[]{launcher.Controls[1], caption, HelpId}, false);
			Mediator.SendMessage("RegisterHelpTargetWithId",
				new object[]{launcher.Controls[0], caption, HelpId, "Button"}, false);
		}

		/// <summary>
		/// Handle changes in the size of the underlying view.
		/// </summary>
		protected void OnViewSizeChanged(object sender, FwViewSizeEventArgs e)
		{
			// For now, just handle changes in the height.
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;

			if (ContainingDataTree == null)
				return; // called too soon, from initial layout before connected.
			int hMin = ContainingDataTree.GetMinFieldHeight();
			int h1 = view.RootBox.Height;
			Debug.Assert(e.Height == h1);
			int hOld = TreeNode == null ? 0 : TreeNode.Height;
			int hNew = Math.Max(h1, hMin) + 3;
			if (hNew != hOld)
			{
				if (TreeNode != null)
					TreeNode.Height = hNew;
				Height = hNew - 1;
			}
		}

		public override XCore.IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();
			// Normally a slice should only handle messages if both it and its data tree
			// are visible. Override this method if there is some reason to handle messages
			// while not visible. Note however that currently (31 Aug 2005) RecordEditView
			// hides the data tree but does not remove slices when no record is current.
			// Thus, a slice that is not visible might belong to a display of a deleted
			// or unavailable object, so be very careful what you enable!
			if (this.Visible && ContainingDataTree.Visible)
			{
				PhoneEnvReferenceLauncher rl = Control as PhoneEnvReferenceLauncher;
				//PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
				return new XCore.IxCoreColleague[] {rl.MainControl as IxCoreColleague, this};
			}
			else
				return new XCore.IxCoreColleague[0];
		}

		/// <summary>
		/// This action is needed whenever we leave the slice, not just when we move to another
		/// slice but also when we move directly to another tool.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLeave(EventArgs e)
		{
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			view.ConnectToRealCache();
			if (view.RootBox != null)
				view.RootBox.DestroySelection();
			base.OnLeave(e);
		}

		#region Special menu item methods
		/// <summary>
		/// This menu item is turned off if an underscore already exists in the environment
		/// string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayShowEnvironmentError(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			display.Enabled = view.CanShowEnvironmentError();
			return true;
		}

		public bool OnShowEnvironmentError(object args)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			view.ShowEnvironmentError();
			return true;
		}

		/// <summary>
		/// This menu item is turned off if a slash already exists in the environment string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayInsertSlash(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			display.Enabled = view.CanInsertSlash();
			return true;
		}

		public bool OnInsertSlash(object args)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			view.RootBox.OnChar((int)'/');
			return true;
		}

		/// <summary>
		/// This menu item is turned off if an underscore already exists in the environment
		/// string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayInsertEnvironmentBar(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			display.Enabled = view.CanInsertEnvBar();
			return true;
		}

		public bool OnInsertEnvironmentBar(object args)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			view.RootBox.OnChar((int)'_');
			return true;
		}

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayInsertNaturalClass(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			display.Enabled = view.CanInsertItem();
			return true;
		}

		public bool OnInsertNaturalClass(object args)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			return SimpleListChooser.ChooseNaturalClass(view.RootBox, m_cache,
				m_persistenceProvider, Mediator, m_propertyTable);
		}

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayInsertOptionalItem(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			display.Enabled = view.CanInsertItem();
			return true;
		}

		public bool OnInsertOptionalItem(object args)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			IVwRootBox rootb = view.RootBox;
			InsertOptionalItem(rootb);
			return true;
		}

		/// <summary>
		/// Insert "()" into the rootbox at the current selection, then back up the selection
		/// to be between the parentheses.
		/// </summary>
		/// <param name="rootb"></param>
		public static void InsertOptionalItem(IVwRootBox rootb)
		{
			rootb.OnChar('(');
			rootb.OnChar(')');
			// Adjust the selection to be between the parentheses.
			var vwsel = rootb.Selection;
			var cvsli = vwsel.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			cvsli--;
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttp;
			var rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
												   out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
												   out ws, out fAssocPrev, out ihvoEnd, out ttp);
			Debug.Assert(ichAnchor == ichEnd);
			Debug.Assert(ichAnchor > 0);
			--ichEnd;
			--ichAnchor;
			rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, tagTextProp, cpropPrevious,
									ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, ttp, true);
		}

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayInsertHashMark(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			display.Enabled = view.CanInsertHashMark();
			return true;
		}

		public bool OnInsertHashMark(object args)
		{
			CheckDisposed();
			PhoneEnvReferenceLauncher rl = (PhoneEnvReferenceLauncher)this.Control;
			PhoneEnvReferenceView view = (PhoneEnvReferenceView)rl.MainControl;
			view.RootBox.OnChar((int)'#');
			return true;
		}
		#endregion
	}
}
