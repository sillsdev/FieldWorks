// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RootSiteGroup.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using Microsoft.Win32;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	#region Class RootSiteGroup
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds and manages a list of slave root sites. This class serves as a base class and is
	/// used as default group for views that should not scroll synchronized.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class RootSiteGroup: IRootSiteGroup, IRootSite, IFWDisposable
	{
		/// <summary>Holds RootSite objects</summary>
		protected List<IRootSiteSlave> m_slaves = new List<IRootSiteSlave>();
		/// <summary></summary>
		protected bool m_fDisposed;
		/// <summary></summary>
		protected IRootSiteSlave m_scrollingController;
		/// <summary></summary>
		protected SplitGrid m_Parent;
		/// <summary></summary>
		protected bool m_fAllowPainting = true;

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Should only be used for tests!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RootSiteGroup()
			: this(null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RootSiteGroup"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// ------------------------------------------------------------------------------------
		public RootSiteGroup(SplitGrid parent)
		{
			m_Parent = parent;
		}

		#endregion

		#region Disposed stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		/// <remarks>This property is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed
		{
			get { return m_fDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// RootSiteGroup is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~RootSiteGroup()
		{
			Dispose(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this before doing anything else.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				// Dispose managed resources here.
				if (m_slaves != null)
				{
					// We need to close all of the rootboxes because when controls are
					// destroyed they cause the other controls still on the parent to
					// resize. If the rootbox is sync'd with other views then the other
					// views will try to layout their rootboxes. This is BAD!!! :)
					foreach (IRootSite site in m_slaves)
						site.CloseRootBox();

					foreach (IDisposable ctrl in m_slaves)
						ctrl.Dispose();
				}
				if (m_slaves != null)
					m_slaves.Clear();
			}

			m_fDisposed = true;
		}
		#endregion

		#region IRootSiteGroup Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add another slave to the synchronization group.
		/// Note that it is usually also necessary to add it to the Controls collection.
		/// That isn't done here to give the client more control over when it is done.
		/// </summary>
		/// <param name="rootSiteSlave">The slave to add</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AddToSyncGroup(IRootSiteSlave rootSiteSlave)
		{
			CheckDisposed();

			if (rootSiteSlave == null)
				return;
			m_slaves.Add(rootSiteSlave);
			IRootSite rootSite = rootSiteSlave as IRootSite;
			if (rootSite != null)
				rootSite.AllowPainting = AllowPainting;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets which slave rootsite is the active, or focused, one. Commands such as
		/// Find/Replace will pertain to the active rootsite.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public IRootSite FocusedRootSite
		{
			get
			{
				CheckDisposed();
				if (m_Parent == null)
					return null;

				return m_Parent.FocusedRootSite;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See RootSite.InvalidateForLazyFix for explanation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InvalidateForLazyFix()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the member of the rootsite group that controls scrolling (i.e. the one
		/// with the vertical scroll bar).
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public virtual IRootSiteSlave ScrollingController
		{
			get
			{
				CheckDisposed();
				return m_scrollingController;
			}
			set
			{
				CheckDisposed();
				m_scrollingController = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Controls whether size change suppression is in effect.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool SizeChangedSuppression
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all of the slaves in this group
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public List<IRootSiteSlave> Slaves
		{
			get { return m_slaves; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This MUST be called by the MakeRoot method or something similar AFTER the
		/// root box is created but BEFORE the view is laid out. Even after it is called,
		/// MakeRoot must not do anything that would cause layout; that should not happen
		/// until all roots are synchronized.
		/// </summary>
		/// <param name="rootb">The root box.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Synchronize(IVwRootBox rootb)
		{
			// do nothing here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object that synchronizes all the root boxes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IVwSynchronizer Synchronizer
		{
			get
			{
				CheckDisposed();
				return null;
			}
		}

		#endregion

		#region IRootSite Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the complete list of rootboxes used within this IRootSite control.
		/// The resulting list may contain zero or more items.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<IVwRootBox> AllRootBoxes()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets sets whether or not to allow painting on the view
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool AllowPainting
		{
			get
			{
				CheckDisposed();
				if (ScrollingController != null && ScrollingController is IRootSite)
					return ((IRootSite)ScrollingController).AllowPainting;
				return m_fAllowPainting;
			}
			set
			{
				CheckDisposed();
				m_fAllowPainting = value;
				foreach (IRootSiteSlave slave in Slaves)
				{
					if (slave is IRootSite)
						((IRootSite)slave).AllowPainting = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwRootSite CastAsIVwRootSite()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows forcing the closing of root boxes. This is necessary when an instance of a
		/// SimpleRootSite is created but never shown so it's handle doesn't get created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseRootBox()
		{
			CheckDisposed();

			for (int i = 0; i < m_slaves.Count; i++)
			{
				if (m_slaves[i] is IRootSite)
					((IRootSite)m_slaves[i]).CloseRootBox();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Commits all outstanding changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Commit()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editing helper for this IRootsite.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public EditingHelper EditingHelper
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the width available for laying things out in the view.
		/// Return the layout width for the window, depending on whether or not there is a
		/// scroll bar. If there is no scroll bar, we pretend that there is, so we don't have
		/// to keep adjusting the width back and forth based on the toggling on and off of
		/// vertical and horizontal scroll bars and their interaction.
		/// The return result is in pixels.
		/// The only common reason to override this is to answer instead a very large integer,
		/// which has the effect of turning off line wrap, as everything apparently fits on
		/// a line.
		/// </summary>
		/// <param name="prootb">The root box</param>
		/// <returns>Width available for layout</returns>
		/// ------------------------------------------------------------------------------------
		public int GetAvailWidth(IVwRootBox prootb)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display :)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool RefreshDisplay()
		{
			CheckDisposed();

			if (ScrollingController != null)
				ScrollingController.RefreshDisplay();
			//Enhance: if all descendants of this control have had their RefreshDisplay called return true.
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		/// ------------------------------------------------------------------------------------
		public bool ScrollSelectionToLocation(IVwSelection sel, int dyPos)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		///
//		/// </summary>
//		/// <param name="sender"></param>
//		/// <param name="oldPos"></param>
//		/// <param name="newPos"></param>
//		/// ------------------------------------------------------------------------------------
//		private void HandleVerticalScrollPositionChanged(object sender, int oldPos, int newPos)
//		{
//			foreach (IRootSiteSlave slave in m_slaves)
//			{
//				if (slave != sender)
//					slave.ScrollPosition = new Point(-slave.ScrollPosition.X, newPos);
//			}
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool AllowLayout
		{
			set
			{
				CheckDisposed();
				foreach (IRootSiteSlave slave in Slaves)
				{
					if (slave is SimpleRootSite)
						((SimpleRootSite)slave).AllowLayout = value;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the views.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ShowViews()
		{
			CheckDisposed();
			foreach (IRootSiteSlave slave in Slaves)
			{
				if (slave is Control)
					((Control)slave).Show();
			}
		}
	}
	#endregion

	#region class SyncedRootSiteGroup
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds and manages a list of slave root sites that scroll synchronized.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class SyncedRootSiteGroup : RootSiteGroup, IHeightEstimator
	{
		private IVwSynchronizer m_sync = VwSynchronizerClass.Create();
		private IParagraphCounter m_paraCounter;

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Should only be used for tests!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SyncedRootSiteGroup()
			: this(null, null, 0)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="cache">The Fdo Cache</param>
		/// <param name="viewTypeId">An identifier for a group of views that share the same
		/// height estimates</param>
		/// ------------------------------------------------------------------------------------
		public SyncedRootSiteGroup(SplitGrid parent, FdoCache cache, int viewTypeId)
			: base(parent)
		{
			// NOTE: This ParagraphCounter is shared among multiple views (i.e. references to
			// the same counter will be used in each RootSiteGroup with the same cache and
			// viewTypeId)
			m_paraCounter = cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().GetParaCounter(viewTypeId);
		}
		#endregion

		#region Disposed stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose managed resources here.
			}

			base.Dispose(disposing);
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add another slave to the synchronization group.
		/// Note that it is usually also necessary to add it to the Controls collection.
		/// That isn't done here to give the client more control over when it is done.
		/// </summary>
		/// <param name="rootsite"></param>
		/// ------------------------------------------------------------------------------------
		public override void AddToSyncGroup(IRootSiteSlave rootsite)
		{
			base.AddToSyncGroup(rootsite);
			if (rootsite != null)
				rootsite.Group = this;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the member of the rootsite group that controls scrolling (i.e. the one
		/// with the vertical scroll bar).
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override IRootSiteSlave ScrollingController
		{
			get
			{
				CheckDisposed();
				return base.ScrollingController;
			}
			set
			{
				CheckDisposed();

				if (m_scrollingController != null)
				{
					m_scrollingController.VerticalScrollPositionChanged -=
						new ScrollPositionChanged(HandleVerticalScrollPositionChanged);
				}

				base.ScrollingController = value;
				Debug.Assert(m_slaves.Contains(m_scrollingController));

				if (m_scrollingController != null)
				{
					m_scrollingController.VerticalScrollPositionChanged +=
						new ScrollPositionChanged(HandleVerticalScrollPositionChanged);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This MUST be called by the MakeRoot method or something similar AFTER the
		/// root box is created but BEFORE the view is laid out. Even after it is called,
		/// MakeRoot must not do anything that would cause layout; that should not happen
		/// until all roots are synchronized.
		/// </summary>
		/// <param name="rootb">The root box.</param>
		/// ------------------------------------------------------------------------------------
		public override void Synchronize(IVwRootBox rootb)
		{
			CheckDisposed();
			Synchronizer.AddRoot(rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object that synchronizes all the root boxes.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override IVwSynchronizer Synchronizer
		{
			get
			{
				CheckDisposed();
				return m_sync;
			}
		}
		#endregion

		#region IHeightEstimator Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems. Note that the calling
		/// code does NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available width.
		/// </summary>
		/// <param name="hvo">Item whose height is to be estimated</param>
		/// <param name="frag">Basically indicates what kind of thing the HVO represents (or
		/// else we're in trouble)</param>
		/// <param name="availableWidth"></param>
		/// <returns>Height of an item in points</returns>
		/// ------------------------------------------------------------------------------------
		public int EstimateHeight(int hvo, int frag, int availableWidth)
		{
			CheckDisposed();

			// Find maximum height of all rootsite slaves in view
			int maxHeight = 0;
			int paraHeight;
			if (m_slaves.Count > 0)
			{
				Debug.Assert(m_paraCounter != null,
					"Need to set ParagraphCounterManager.ParagraphCounterType before creating RootSiteGroup");
				int slaveWidth;
				foreach (IRootSite slave in m_slaves)
				{
					slaveWidth = slave.GetAvailWidth(null);
					Debug.Assert(slaveWidth != 0 || !((Control)slave).Visible);
					slaveWidth = (slaveWidth == 0) ? 1 : slaveWidth;
					paraHeight = (RootSite.kdxBaselineRootsiteWidth *
						((IHeightEstimator)slave).AverageParaHeight) / slaveWidth;
					maxHeight = Math.Max(m_paraCounter.GetParagraphCount(hvo, frag) * paraHeight,
						maxHeight);
				}
			}
			else
			{
				Debug.Fail("Need to handle this!");
			}

			return maxHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the average paragraph height in points.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int AverageParaHeight
		{
			get { throw new NotImplementedException("The method or operation is not implemented."); }
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="oldPos"></param>
		/// <param name="newPos"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleVerticalScrollPositionChanged(object sender, int oldPos, int newPos)
		{
			foreach (IRootSiteSlave slave in m_slaves)
			{
				if (slave != sender)
					slave.ScrollPosition = new Point(-slave.ScrollPosition.X, newPos);
			}
		}
	}
	#endregion
}
