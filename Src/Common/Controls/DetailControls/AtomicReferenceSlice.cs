// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AtomicReferenceSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for AtomicReferenceSlice.
	/// </summary>
	public class AtomicReferenceSlice : ReferenceSlice, IVwNotifyChange
	{
		int m_dxLastWidth; // remember width when OnSizeChanged called.
		/// <summary>
		/// Use this to do the Add/RemoveNotifications.
		/// </summary>
		private ISilDataAccess m_sda;

		/// <summary>
		/// Initializes a new instance of the <see cref="AtomicReferenceSlice"/> class.
		/// Used by custom slices that extend this class.
		/// </summary>
		/// <param name="control"></param>
		protected AtomicReferenceSlice(Control control)
			: base(control)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AtomicReferenceSlice"/> class.
		/// Used by slices that extend this class.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="flid">The flid.</param>
		protected AtomicReferenceSlice(Control control, FdoCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
			m_sda = m_cache.MainCacheAccessor;
			m_sda.AddNotification(this);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AtomicReferenceSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public AtomicReferenceSlice(FdoCache cache, ICmObject obj, int flid)
			: this(new AtomicReferenceLauncher(), cache, obj, flid)
		{
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
				if (m_sda != null)
				{
					m_sda.RemoveNotification(this);
					m_sda = null;
				}
				var arl = Control as AtomicReferenceLauncher;
				if (arl != null)
				{
					arl.ChoicesMade -= RefreshTree;
					arl.ViewSizeChanged -= OnViewSizeChanged;
					var view = (AtomicReferenceView)arl.MainControl;
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

			var arl = (AtomicReferenceLauncher)Control;
			arl.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			arl.Initialize(m_cache, m_obj, m_flid, m_fieldName, m_persistenceProvider,
				DisplayNameProperty,
				BestWsName); // TODO: Get better default 'best ws'.
			arl.ConfigurationNode = ConfigurationNode;
			XmlNode deParams = ConfigurationNode.SelectSingleNode("deParams");
			if (XmlUtils.GetOptionalBooleanAttributeValue(
				deParams, "changeRequiresRefresh", false))
			{
				arl.ChoicesMade += RefreshTree;
			}

			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			// Now done in Slice.ctor
			//arl.Visible = false;
			arl.ViewSizeChanged += OnViewSizeChanged;
			var view = (AtomicReferenceView)arl.MainControl;
			view.ViewSizeChanged += OnViewSizeChanged;
		}

		protected void RefreshTree(object sender, EventArgs args)
		{
			CheckDisposed();
			ContainingDataTree.RefreshList(false);
		}

		public override void ShowSubControls()
		{
			CheckDisposed();
			base.ShowSubControls();
			Control.Visible = true;
		}

		/// <summary>
		/// Handle changes in the size of the underlying view.
		/// </summary>
		protected void OnViewSizeChanged(object sender, FwViewSizeEventArgs e)
		{
			// When height is more than one line (e.g., long definition without gloss),
			// this can get called initially before it has a parent.
			if (ContainingDataTree == null)
				return;
			// For now, just handle changes in the height.
			var arl = (AtomicReferenceLauncher)Control;
			var view = (AtomicReferenceView)arl.MainControl;
			int hMin = ContainingDataTree.GetMinFieldHeight();
			int h1 = view.RootBox.Height;
			Debug.Assert(e.Height == h1);
			int hOld = TreeNode.Height;
			int hNew = Math.Max(h1, hMin) + 3;
			if (hNew > hOld)
			{
				TreeNode.Height = hNew;
				arl.Height = hNew - 1;
				view.Height = hNew - 1;
				Height = hNew;
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (Width == m_dxLastWidth)
				return;
			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			var arl = (AtomicReferenceLauncher)Control;
			var view = (AtomicReferenceView)arl.MainControl;
			view.PerformLayout();
			int h1 = view.RootBox.Height;
			int hNew = Math.Max(h1, ContainingDataTree.GetMinFieldHeight()) + 3;
			if (hNew != Height)
			{
				Height = hNew;
			}
		}

		protected override void UpdateDisplayFromDatabase()
		{
			var arl = (AtomicReferenceLauncher)Control;
			arl.UpdateDisplayFromDatabase();
		}
		public override void RegisterWithContextHelper()
		{
			CheckDisposed();

			if (Control == null)
				return;

			string caption = XmlUtils.GetLocalizedAttributeValue(ConfigurationNode, "label", "");
			var launcher = (AtomicReferenceLauncher)Control;
			Publisher.Publish("RegisterHelpTargetWithId", new object[]{launcher.AtomicRefViewControl, caption, HelpId});
			Publisher.Publish("RegisterHelpTargetWithId", new object[]{launcher.PanelControl, caption, HelpId, "Button"});
		}

		#region IVwNotifyChange Members
		/// <summary>
		/// This PropChanged detects a needed UI update.  See LT-9002.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (m_flid == PartOfSpeechTags.kflidDefaultInflectionClass &&
				cvIns == 0 && cvDel > 0 &&
				(tag == PartOfSpeechTags.kflidInflectionClasses ||
				 tag == MoInflClassTags.kflidSubclasses) &&
				((IPartOfSpeech)m_obj).DefaultInflectionClassRA == null)
			{
				var arl = (AtomicReferenceLauncher)Control;
				arl.UpdateDisplayFromDatabase();
			}
		}

		#endregion
	}

	/// <summary>
	/// This class should be extended by any custom atomic reference slices.
	/// </summary>
	public abstract class CustomAtomicReferenceSlice : AtomicReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CustomAtomicReferenceSlice"/> class.
		/// </summary>
		/// <param name="control"></param>
		protected CustomAtomicReferenceSlice(Control control)
			: base(control)
		{
		}

		public override void FinishInit()
		{
			CheckDisposed();
			SetFieldFromConfig();
			base.FinishInit();
		}
	}
	public class AtomicReferenceDisabledSlice: AtomicReferenceSlice
	{
		public AtomicReferenceDisabledSlice(FdoCache cache, ICmObject obj, int flid)
			:base(cache, obj, flid)
		{
		}
		public override void FinishInit()
		{
			CheckDisposed();
			base.FinishInit();
			var arl = (AtomicReferenceLauncher)Control;
			var view = (AtomicReferenceView)arl.MainControl;
			view.FinishInit(ConfigurationNode);
		}
	}
}
