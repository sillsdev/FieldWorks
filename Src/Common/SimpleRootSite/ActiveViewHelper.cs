// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ActiveViewHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ActiveViewHelper attemps to keep track of the active view (i.e. an IRootSite) of a form.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ActiveViewHelper : IFWDisposable
	{
		#region Member variables
		private Control m_rootControl;
		private List<IRootSite> m_availableSites = new List<IRootSite>();
		private IRootSite m_activeSite;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an ActiveViewHelper for the specified form.
		/// </summary>
		/// <param name="rootControl">Control to create an ActiveViewHelper for</param>
		/// ------------------------------------------------------------------------------------
		public ActiveViewHelper(Control rootControl)
		{
			Debug.Assert(rootControl != null);
			m_rootControl = rootControl;
			DeepAddControl(rootControl);
		}
		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ActiveViewHelper()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + "******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			m_activeSite = null;
			if (disposing)
			{
				// Dispose managed resources here.
				if (m_rootControl != null)
					DeepRemoveControl(m_rootControl);
				if (m_availableSites != null)
					m_availableSites.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rootControl = null;
			m_availableSites = null;

			m_isDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}


		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function attempts to determine whether a root site can really be seen
		/// in the sense that it can reasonably receive commands (such as from the style
		/// dialog). It doesn't check everything possible; for example, a control could
		/// be visible in all the ways checked here and still covered by another control
		/// or scrolled out of sight.
		/// </summary>
		/// <param name="site">The rootsite</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		bool IsReallyVisible(IRootSite site)
		{
			Control control = site as Control;
			if (control == null || !control.Visible)
				return false;
			// Unfortunately the above can somehow still be true for a control that is
			// part of a disposed window. Check some more things to make sure.
			if (!control.IsHandleCreated)
				return false;
			var rootSite = site as IVwRootSite;
			if (rootSite == null || rootSite.RootBox == null)
				return false;
			// Don't do this! It produces a stack overflow because CastAsIVwRootBox
			// uses ActiveView to try to get a RootSite.
			//if (site.CastAsIVwRootSite().RootBox == null)
			//	return false;
			if (control.IsDisposed)
				return false;
			// It may be visible along with all its parents, but if the chain doesn't
			// go up to a form we can't really see it.
			return control.TopLevelControl is Form;
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the views that are in the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite[] Views
		{
			get { CheckDisposed(); return m_availableSites.ToArray(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active view of the owner of this ActiveViewHelper, or null if there is
		/// none.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite ActiveView
		{
			get
			{
				CheckDisposed();
				if (m_activeSite != null)
				{
					Control control = m_activeSite as Control;
					if (control == null)
						return m_activeSite;
					return (IsReallyVisible(m_activeSite) ? m_activeSite : null);
				}
				foreach (IRootSite site in m_availableSites)
				{
					if (IsReallyVisible(site))
					{
						m_activeSite = site;
						return site;
					}
				}
				return null;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handler for when a control is added to a control. Runs <see cref="DeepAddControl"/>
		/// on <c>e.Control</c>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ControlWasAdded(object sender, ControlEventArgs e)
		{
			DeepAddControl(e.Control);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ControlWasRemoved(object sender, ControlEventArgs e)
		{
			DeepRemoveControl(e.Control);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handler for when a view gains focus.  This sets the active view to the view that got
		/// focus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ViewGotFocus(object sender, EventArgs e)
		{
			var oldActiveView = ActiveView;
			m_activeSite = sender as IRootSite;
			if (ActiveView != oldActiveView && ActiveViewChanged != null)
				ActiveViewChanged(this, new EventArgs());
		}

		///<summary>
		/// Fired when the active view changes (at least as a result of a view getting focus)
		///</summary>
		public event EventHandler<EventArgs> ActiveViewChanged;

		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified view is visible.
		/// </summary>
		/// <param name="view"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool IsViewVisible(IRootSite view)
		{
			CheckDisposed();
			Control ctrl = view as Control;

			if (ctrl != null)
				return ctrl.Visible && ctrl.FindForm() != null;

			return false;
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively add handlers for the ControlAdded and GotFocus events of the specified
		/// control and all its sub-controls.
		/// </summary>
		/// <param name="control"></param>
		/// ------------------------------------------------------------------------------------
		private void DeepAddControl(Control control)
		{
			// Before blindly adding the event handler, first remove it if there is already
			// one subscribed. (This ensures that stray event handlers don't exist when controls
			// are removed and re-added to a form.)
			control.ControlAdded -= new ControlEventHandler(ControlWasAdded);
			control.ControlRemoved -= new ControlEventHandler(ControlWasRemoved);
			control.ControlAdded += new ControlEventHandler(ControlWasAdded);
			control.ControlRemoved += new ControlEventHandler(ControlWasRemoved);

			if (control is IRootSite)
			{
				// Before blindly adding the event handler, first remove it if there is
				// already one subscribed
				control.GotFocus -= new EventHandler(ViewGotFocus);
				control.GotFocus += new EventHandler(ViewGotFocus);
				m_availableSites.Add((IRootSite)control);
			}

			foreach (Control con in control.Controls)
				DeepAddControl(con);

			if (control is IControl)
			{
				foreach (Control con in ((IControl)control).FocusableControls)
					DeepAddControl(con);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively remove handlers for the ControlAdded and GotFocus events of the specified
		/// control and all its sub-controls.
		/// </summary>
		/// <param name="control"></param>
		/// ------------------------------------------------------------------------------------
		private void DeepRemoveControl(Control control)
		{
			control.ControlAdded -= new ControlEventHandler(ControlWasAdded);
			control.ControlRemoved -= new ControlEventHandler(ControlWasRemoved);

			if (control is IRootSite)
			{
				control.GotFocus -= new EventHandler(ViewGotFocus);
				m_availableSites.Remove((IRootSite)control);
				if (m_activeSite == control)
					m_activeSite = null;
			}

			foreach (Control childControl in control.Controls)
				DeepRemoveControl(childControl);

			if (control is IControl)
			{
#if __MonoCS__ // TODO-Linux FWNX-534: work around for mono bug: https://bugzilla.novell.com/show_bug.cgi?id=656701
				if (!control.IsDisposed)
#endif
				foreach (Control focusableControl in ((IControl)control).FocusableControls)
					DeepRemoveControl(focusableControl);
			}
		}
		#endregion
	}
}
