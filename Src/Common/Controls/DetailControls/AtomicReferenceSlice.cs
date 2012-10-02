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
// File: AtomicReferenceSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using XCore;
using SIL.FieldWorks.Common.COMInterfaces;


namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for AtomicReferenceSlice.
	/// </summary>
	public class AtomicReferenceSlice : ReferenceSlice, IVwNotifyChange
	{
		int m_dxLastWidth = 0; // remember width when OnSizeChanged called.
		/// <summary>
		/// Use this to do the Add/RemoveNotifications.
		/// </summary>
		private ISilDataAccess m_sda;
		/// <summary>
		/// Default Constructor.
		/// </summary>
		public AtomicReferenceSlice() : base()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AtomicReferenceSlice"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public AtomicReferenceSlice(FdoCache cache, ICmObject obj, int flid,
			XmlNode configurationNode, IPersistenceProvider persistenceProvider,
			Mediator mediator, StringTable stringTbl)
			: base(cache, obj, flid, configurationNode, persistenceProvider, mediator, stringTbl)
		{
			m_sda = m_cache.MainCacheAccessor;
			m_sda.AddNotification(this);
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
				AtomicReferenceLauncher arl = Control as AtomicReferenceLauncher;
				if (arl != null)
				{
					arl.ChoicesMade -= new EventHandler(this.RefreshTree);
					arl.ViewSizeChanged -= new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
					AtomicReferenceView view = (AtomicReferenceView)arl.MainControl;
					view.ViewSizeChanged -= new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		///
		/// </summary>
		/// <param name="persistenceProvider"></param>
		/// <param name="stringTbl"></param>
		protected override void SetupControls(IPersistenceProvider persistenceProvider,
			Mediator mediator, StringTable stringTbl)
		{
			AtomicReferenceLauncher arl = new AtomicReferenceLauncher();
			arl.Initialize(m_cache, m_obj, m_flid, m_fieldName, persistenceProvider, mediator,
				DisplayNameProperty,
				BestWsName); // TODO: Get better default 'best ws'.
			arl.ConfigurationNode = ConfigurationNode;
			XmlNode deParams = ConfigurationNode.SelectSingleNode("deParams");
			if (XmlUtils.GetOptionalBooleanAttributeValue(
				deParams, "changeRequiresRefresh", false))
			{
				arl.ChoicesMade += new EventHandler(this.RefreshTree);
			}


			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			arl.Visible = false;
			this.Control = arl;
			arl.ViewSizeChanged += new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
			AtomicReferenceView view = (AtomicReferenceView)arl.MainControl;
			view.ViewSizeChanged += new FwViewSizeChangedEventHandler(this.OnViewSizeChanged);
		}

		protected void RefreshTree(object sender, EventArgs args)
		{
			CheckDisposed();
			ContainingDataTree.RefreshList(false);
		}

		public override void ShowSubControls()
		{
			CheckDisposed();
			base.ShowSubControls ();
			this.Control.Visible = true;
		}

		/// <summary>
		/// Handle changes in the size of the underlying view.
		/// </summary>
		protected void OnViewSizeChanged(object sender, FwViewSizeEventArgs e)
		{
			// For now, just handle changes in the height.
			AtomicReferenceLauncher arl = (AtomicReferenceLauncher)this.Control;
			AtomicReferenceView view = (AtomicReferenceView)arl.MainControl;
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
				this.Height = hNew;
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			if (this.Width == m_dxLastWidth)
				return;
			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			AtomicReferenceLauncher arl = (AtomicReferenceLauncher)this.Control;
			AtomicReferenceView view = (AtomicReferenceView)arl.MainControl;
			view.PerformLayout();
			int h1 = view.RootBox.Height;
			int hNew = Math.Max(h1, ContainingDataTree.GetMinFieldHeight()) + 3;
			if (hNew != this.Height)
			{
				this.Height = hNew;
			}
		}

		protected override void UpdateDisplayFromDatabase()
		{
			AtomicReferenceLauncher arl = (AtomicReferenceLauncher)this.Control;
			arl.UpdateDisplayFromDatabase();
		}
		public override void RegisterWithContextHelper()
		{
			CheckDisposed();
			if (this.Control != null)
			{
				Mediator mediator = this.Mediator;
				StringTable tbl = null;
				if (mediator.HasStringTable)
					tbl = mediator.StringTbl;
				string caption = XmlUtils.GetLocalizedAttributeValue(tbl, ConfigurationNode, "label", "");

				AtomicReferenceLauncher launcher = (AtomicReferenceLauncher)this.Control;

				//NB: which is 0 and which is 1 is sensitive to the front-back order of these widgets in the launcher
				mediator.SendMessage("RegisterHelpTargetWithId",
					new object[]{launcher.Controls[1], caption, HelpId}, false);
				mediator.SendMessage("RegisterHelpTargetWithId",
					new object[]{launcher.Controls[0], caption, HelpId, "Button"}, false);
			}
		}

		#region IVwNotifyChange Members
		/// <summary>
		/// This PropChanged detects a needed UI update.  See LT-9002.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (m_flid == (int)PartOfSpeech.PartOfSpeechTags.kflidDefaultInflectionClass &&
				cvIns == 0 && cvDel > 0 &&
				(tag == (int)PartOfSpeech.PartOfSpeechTags.kflidInflectionClasses ||
				 tag == (int)MoInflClass.MoInflClassTags.kflidSubclasses) &&
				(m_obj as PartOfSpeech).DefaultInflectionClassRAHvo == 0)
			{
				AtomicReferenceLauncher arl = (AtomicReferenceLauncher)this.Control;
				arl.UpdateDisplayFromDatabase();
			}
		}

		#endregion
	}
}
