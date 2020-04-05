// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary />
	internal sealed class PhoneEnvReferenceSlice : ReferenceSlice, IPhEnvSliceCommon
	{
		private int m_dxLastWidth; // width last time OnSizeChanged was called.
		private PhoneEnvReferenceView MainControlOfMyControl => (PhoneEnvReferenceView)MyLauncher.MainControl;
		private PhoneEnvReferenceLauncher MyLauncher => (PhoneEnvReferenceLauncher)Control;

		/// <summary>
		/// Insert "()" into the rootbox at the current selection, then back up the selection
		/// to be between the parentheses.
		/// </summary>
		internal static void InsertOptionalItem(IVwRootBox rootb)
		{
			rootb.OnChar('(');
			rootb.OnChar(')');
			// Adjust the selection to be between the parentheses.
			var vwsel = rootb.Selection;
			var cvsli = vwsel.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			cvsli--;
			var rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli, out var ihvoRoot, out var tagTextProp, out var cpropPrevious, out var ichAnchor, out var ichEnd, out var ws, out var fAssocPrev, out var ihvoEnd, out var ttp);
			Debug.Assert(ichAnchor == ichEnd);
			Debug.Assert(ichAnchor > 0);
			--ichEnd;
			--ichAnchor;
			rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, tagTextProp, cpropPrevious, ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, ttp, true);
		}

		internal PhoneEnvReferenceSlice(LcmCache cache, ICmObject obj, int flid)
			: base(new PhoneEnvReferenceLauncher(), cache, obj, flid)
		{
			Debug.Assert(obj is IMoAffixAllomorph || obj is IMoStemAllomorph);
		}

		public override void FinishInit()
		{
			base.FinishInit();
			var rl = MyLauncher;
			// Don't even 'think' of calling "rl.InitializeFlexComponent" at this point.
			// I (RBR) have done it, and long since repented.
			rl.Initialize(Cache, MyCmObject, m_flid, m_fieldName, PersistenceProvider, null, null);
			rl.ConfigurationNode = ConfigurationNode;
			rl.ViewSizeChanged += OnViewSizeChanged;
			var view = MainControlOfMyControl;
			view.ViewSizeChanged += OnViewSizeChanged;
			view.LayoutSizeChanged += view_LayoutSizeChanged;
		}

		/// <summary>
		/// Set the Editable property on the launcher, which is created before installation, and
		/// then finish installing this slice.
		/// </summary>
		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);
			// The launcher's view wants the RightClickPopupMenuFactory to get one of two context menus.
			MyLauncher.SetRightClickPopupMenuFactory(MyDataTreeSliceContextMenuParameterObject.RightClickPopupMenuFactory);
		}

		// JohnT: this is the proper way to detect changes in height that come from editing within the view.
		// Probably the private ViewSizeChanged event isn't really needed but I'm leaving it for now just in case.
		private void view_LayoutSizeChanged(object sender, EventArgs e)
		{
			var view = MainControlOfMyControl;
			OnViewSizeChanged(this, new FwViewSizeEventArgs(view.RootBox.Height, view.RootBox.Width));
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (Width == m_dxLastWidth)
			{
				return;
			}
			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			var rs = MainControlOfMyControl;
			rs.PerformLayout();
			if (rs.RootBox != null)
			{
				// Allow it to be the height it wants + fluff to get rid of scroll bar.
				// Adjust our own height to suit.
				// Note that this may produce a recursive call!
				Height = rs.RootBox.Height + 8;
			}
		}

		/// <summary>
		/// Handle changes in the size of the underlying view.
		/// </summary>
		private void OnViewSizeChanged(object sender, FwViewSizeEventArgs e)
		{
			// For now, just handle changes in the height.
			var view = MainControlOfMyControl;
			if (ContainingDataTree == null)
			{
				return; // called too soon, from initial layout before connected.
			}
			var hMin = ContainingDataTree.GetMinFieldHeight();
			var h1 = view.RootBox.Height;
			Debug.Assert(e.Height == h1);
			var hOld = TreeNode?.Height ?? 0;
			var hNew = Math.Max(h1, hMin) + 3;
			if (hNew == hOld)
			{
				return;
			}
			if (TreeNode != null)
			{
				TreeNode.Height = hNew;
			}
			Height = hNew - 1;
		}

		/// <summary>
		/// This action is needed whenever we leave the slice, not just when we move to another
		/// slice but also when we move directly to another tool.
		/// </summary>
		protected override void OnLeave(EventArgs e)
		{
			var view = MainControlOfMyControl;
			view.ConnectToRealCache();
			view.RootBox?.DestroySelection();
			base.OnLeave(e);
		}

		/// <summary>
		/// This method is called to handle Undo/Redo operations on this slice.
		/// </summary>
		public override bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			if (tag != Flid)
			{
				return base.UpdateDisplayIfNeeded(hvo, tag);
			}
			MainControlOfMyControl.ResynchListToDatabaseAndRedisplay();
			return true;
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
				MyLauncher.ViewSizeChanged -= OnViewSizeChanged;
				MainControlOfMyControl.ViewSizeChanged -= OnViewSizeChanged;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region IPhEnvSliceCommon implementation

		/// <inheritdoc />
		public bool CanShowEnvironmentError => MainControlOfMyControl.CanShowEnvironmentError();

		/// <inheritdoc />
		public void ShowEnvironmentError()
		{
			MainControlOfMyControl.ShowEnvironmentError();
		}

		/// <inheritdoc />
		public bool CanInsertHashMark => MainControlOfMyControl.CanInsertHashMark;

		/// <inheritdoc />
		public void InsertHashMark()
		{
			MainControlOfMyControl.RootBox.OnChar('#');
		}

		/// <inheritdoc />
		public bool CanInsertOptionalItem => MainControlOfMyControl.CanInsertItem;

		/// <inheritdoc />
		public void InsertOptionalItem()
		{
			InsertOptionalItem(MainControlOfMyControl.RootBox);
		}

		/// <inheritdoc />
		public bool CanInsertNaturalClass => MainControlOfMyControl.CanInsertItem;

		/// <inheritdoc />
		public void InsertNaturalClass()
		{
			ReallySimpleListChooser.ChooseNaturalClass(MainControlOfMyControl.RootBox, Cache, PersistenceProvider, PropertyTable, Publisher, Subscriber);
		}

		/// <inheritdoc />
		public bool CanInsertEnvironmentBar => MainControlOfMyControl.CanInsertEnvBar;

		/// <inheritdoc />
		public void InsertEnvironmentBar()
		{
			MainControlOfMyControl.RootBox.OnChar('_');
		}

		/// <inheritdoc />
		public bool CanInsertSlash => MainControlOfMyControl.CanInsertSlash;

		/// <inheritdoc />
		public void InsertSlash()
		{
			MainControlOfMyControl.RootBox.OnChar('/');
		}
		#endregion
	}
}