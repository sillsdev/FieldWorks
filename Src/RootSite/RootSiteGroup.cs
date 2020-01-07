// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.RootSites
{
#if RANDYTODO
	// TODO: This class seems to not be used by anything but its tests. Consider removing it.
#endif
	/// <summary>
	/// This class acts as a master root site for one or more slaves. It is a wrapper
	/// UserControl which contains the scroll bar. Certain invalidate operations
	/// that are a result of scroll position changes need to affect all slaves.
	/// </summary>
	public class RootSiteGroup : Control, IRootSite, IHeightEstimator, IRootSiteGroup
	{
		private ActiveViewHelper m_activeViewHelper;
		private IRootSiteSlave m_scrollingController;
		private IParagraphCounter m_paraCounter;

		/// <summary />
		/// <param name="cache">The LCM Cache</param>
		/// <param name="viewTypeId">An identifier for a group of views that share the same height estimates</param>
		public RootSiteGroup(LcmCache cache = null, int viewTypeId = 0)
		{
			// NOTE: This ParagraphCounter is shared among multiple views (i.e. references to
			// the same counter will be used in each RootSiteGroup with the same cache and
			// viewTypeId)
			if (cache != null)
			{
				m_paraCounter = cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().GetParaCounter(viewTypeId);
			}
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
				Debug.Assert(m_scrollingController == null || Controls.Contains(m_scrollingController as Control));
				if (Slaves != null)
				{
					// We need to close all of the rootboxes because when controls are
					// destroyed they cause the other controls still on the parent to
					// resize. If the rootbox is sync'd with other views then the other
					// views will try to layout their rootboxes. This is BAD!!! :)
					foreach (RootSite site in Slaves)
					{
						site.CloseRootBox();
					}
					foreach (Control ctrl in Slaves)
					{
						if (!Controls.Contains(ctrl))
						{
							ctrl.Dispose();
						}
					}
				}
				Slaves?.Clear();
				m_activeViewHelper?.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			Slaves = null;
			if (Synchronizer != null)
			{
				Marshal.ReleaseComObject(Synchronizer);
				Synchronizer = null;
			}
			m_activeViewHelper = null;
			m_scrollingController = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Methods

		/// <summary />
		private void HandleVerticalScrollPositionChanged(object sender, int oldPos, int newPos)
		{
			foreach (RootSite slave in Slaves)
			{
				if (slave != sender)
				{
					slave.ScrollPosition = new Point(-slave.ScrollPosition.X, newPos);
				}
			}
		}

		/// <summary>
		/// This MUST be called by the MakeRoot method or something similar AFTER the
		/// root box is created but BEFORE the view is laid out. Even after it is called,
		/// MakeRoot must not do anything that would cause layout; that should not happen
		/// until all roots are synchronized.
		/// </summary>
		public virtual void Synchronize(IVwRootBox rootb)
		{
			Synchronizer.AddRoot(rootb);
		}

		/// <summary>
		/// Add another slave to the synchronization group.
		/// Note that it is usually also necessary to add it to the Controls collection.
		/// That isn't done here to give the client more control over when it is done.
		/// </summary>
		public void AddToSyncGroup(IRootSiteSlave rootsite)
		{
			if (rootsite == null)
			{
				return;
			}
			Slaves.Add(rootsite);
			rootsite.Group = this;
		}

		/// <summary>
		/// See RootSite.InvalidateForLazyFix for explanation.
		/// </summary>
		public void InvalidateForLazyFix()
		{
			foreach (RootSite rootsite in Slaves)
			{
				rootsite.Invalidate();
			}
		}
		#endregion

		#region Properties

		/// <summary>
		/// Gets all of the slaves in this group
		/// </summary>
		public List<IRootSiteSlave> Slaves { get; private set; } = new List<IRootSiteSlave>(3);

		/// <summary>
		/// Gets or sets the member of the rootsite group that controls scrolling (i.e. the one
		/// with the vertical scroll bar).
		/// </summary>
		public IRootSiteSlave ScrollingController
		{
			get
			{
				return m_scrollingController;
			}
			set
			{
				if (m_scrollingController != null)
				{
					m_scrollingController.VerticalScrollPositionChanged -= HandleVerticalScrollPositionChanged;
				}

				m_scrollingController = value;
				Debug.Assert(Slaves.Contains(m_scrollingController));

				if (m_scrollingController != null)
				{
					m_scrollingController.VerticalScrollPositionChanged += HandleVerticalScrollPositionChanged;
				}
			}
		}

		/// <summary>
		/// Controls whether size change suppression is in effect.
		/// </summary>
		public bool SizeChangedSuppression { get; set; }

		/// <summary>
		/// Gets and sets which slave rootsite is the active, or focused, one. Commands such as
		/// Find/Replace will pertain to the active rootsite.
		/// </summary>
		public IRootSite FocusedRootSite
		{
			get
			{
				if (m_activeViewHelper == null)
				{
					m_activeViewHelper = new ActiveViewHelper(this);
				}
				return m_activeViewHelper.ActiveView == this
					? null
					: m_activeViewHelper.ActiveView is IRootSiteGroup
						? ((IRootSiteGroup)m_activeViewHelper.ActiveView).FocusedRootSite
						: m_activeViewHelper.ActiveView;
			}
		}

		/// <summary>
		/// Get the object that synchronizes all the root boxes.
		/// </summary>
		public IVwSynchronizer Synchronizer { get; private set; } = VwSynchronizerClass.Create();
		#endregion

		#region Event related methods
		/// <summary>
		/// If possible, pass focus to your currently focused control.
		/// If you don't know one, try to pass it to your first slave.
		/// </summary>
		protected override void OnGotFocus(EventArgs e)
		{
			if (FocusedRootSite is Control)
			{
				(FocusedRootSite as Control).Focus();
			}
			else if (Slaves.Count > 0 && Slaves[0] is Control)
			{
				(Slaves[0] as Control).Focus();
			}
			else
			{
				Debug.Assert(false, "RootSiteGroup should not get focus with no slaves");
			}
		}
		#endregion

		#region Implementation of IRootSite
		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		public bool ScrollSelectionToLocation(IVwSelection sel, int dyPos)
		{
			throw new NotSupportedException("ScrollSelectionToLocation is not supported for RootSiteGroup");
		}

		/// <summary>
		/// Refreshes the display :)
		/// </summary>
		public virtual bool RefreshDisplay()
		{
			Debug.Assert(ScrollingController != null);
			// RefreshDisplay now happens through all sync'd views in the Views code.
			ScrollingController.RefreshDisplay();
			//Enhance: If all descendant controls have been refreshed return true here (perhaps return the above line)
			return false;
		}

		/// <summary />
		public virtual void CloseRootBox()
		{
			foreach (var slave in Slaves)
			{
				if (slave is IRootSite)
				{
					((IRootSite)slave).CloseRootBox();
				}
			}
		}

		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite. This will recurse through nested
		/// rootsite slaves until it finds a real IVwRootSite.
		/// </summary>
		public virtual IVwRootSite CastAsIVwRootSite()
		{
			var rootSite = FocusedRootSite;
			// If we didn't find the focused rootsite then find the first slave that is an
			// IRootSite.
			if (rootSite == null)
			{
				foreach (var slave in Slaves)
				{
					if (slave is IRootSite)
					{
						rootSite = (IRootSite)slave;
						if (rootSite is Control && ((Control)rootSite).FindForm() == Form.ActiveForm)
						{
							((Control)rootSite).Focus();
						}
						break;
					}
				}
			}
			return rootSite?.CastAsIVwRootSite();
		}

		/// <summary>
		/// Gets editing helper from focused root site, if there is one.
		/// </summary>
		public virtual EditingHelper EditingHelper => FocusedRootSite?.EditingHelper;

		/// <summary>
		/// A list of zero or more internal rootboxes.
		/// </summary>
		public virtual List<IVwRootBox> AllRootBoxes()
		{
			var rootboxes = new List<IVwRootBox>();
			foreach (var slave in Slaves)
			{
				if (slave is IRootSite)
				{
					var rs = (IRootSite)slave;
					rootboxes.AddRange(rs.AllRootBoxes());
				}
			}
			return rootboxes;
		}

		/// <summary>
		/// <c>false</c> to prevent OnPaint from happening, <c>true</c> to perform
		/// OnPaint. This is used to prevent redraws from happening while we do a RefreshDisplay.
		/// </summary>
		public bool AllowPainting
		{
			get
			{
				return true;
			}
			set
			{
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether or not to allow layout.
		/// </summary>
		public bool AllowLayout => true;

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
		public int GetAvailWidth(IVwRootBox prootb)
		{
			throw new NotSupportedException("The method or operation is not supported.");
		}

		#endregion

		#region Implementation of IHeightEstimator

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
		public int EstimateHeight(int hvo, int frag, int availableWidth)
		{
			// Find maximum height of all rootsite slaves in view
			var maxHeight = 0;
			int paraHeight;
			if (Slaves.Count > 0)
			{
				Debug.Assert(m_paraCounter != null, "Need to set ParagraphCounterManager.ParagraphCounterType before creating RootSiteGroup");
				foreach (RootSite slave in Slaves)
				{
					var slaveWidth = slave.GetAvailWidth(null);
					Debug.Assert(slaveWidth != 0 || !slave.Visible);
					slaveWidth = (slaveWidth == 0) ? 1 : slaveWidth;
					paraHeight = (RootSite.kdxBaselineRootsiteWidth * slave.AverageParaHeight) / slaveWidth;
					maxHeight = Math.Max(m_paraCounter.GetParagraphCount(hvo, frag) * paraHeight, maxHeight);
				}
			}
			else
			{
				Debug.Fail("Need to handle this!");
			}

			return maxHeight;
		}

		/// <summary>
		/// Gets the average paragraph height in points.
		/// </summary>
		public int AverageParaHeight
		{
			get { throw new NotSupportedException("The method or operation is not supported."); }
		}

		#endregion
	}
}