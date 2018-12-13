// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// ActiveViewHelper attempts to keep track of the active view (i.e. an IRootSite) of a form.
	/// </summary>
	public class ActiveViewHelper : IDisposable
	{
		private Control m_rootControl;
		private List<IRootSite> m_availableSites = new List<IRootSite>();
		private IRootSite m_activeSite;

		#region Constructor

		/// <summary>
		/// Creates an ActiveViewHelper for the specified form.
		/// </summary>
		/// <param name="rootControl">Control to create an ActiveViewHelper for</param>
		public ActiveViewHelper(Control rootControl)
		{
			Debug.Assert(rootControl != null);
			m_rootControl = rootControl;
			DeepAddControl(rootControl);
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

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
			// Therefore, you should call GC.SuppressFinalize to
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			m_activeSite = null;
			if (disposing)
			{
				// Dispose managed resources here.
				if (m_rootControl != null)
				{
					DeepRemoveControl(m_rootControl);
				}
				m_availableSites?.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rootControl = null;
			m_availableSites = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// This function attempts to determine whether a root site can really be seen
		/// in the sense that it can reasonably receive commands (such as from the style
		/// dialog). It doesn't check everything possible; for example, a control could
		/// be visible in all the ways checked here and still covered by another control
		/// or scrolled out of sight.
		/// </summary>
		private static bool IsReallyVisible(IRootSite site)
		{
			var control = site as Control;
			if (control == null || !control.Visible)
			{
				return false;
			}
			// Unfortunately the above can somehow still be true for a control that is
			// part of a disposed window. Check some more things to make sure.
			if (!control.IsHandleCreated)
			{
				return false;
			}
			var rootSite = site as IVwRootSite;
			if (rootSite?.RootBox == null)
			{
				return false;
			}
			if (control.IsDisposed)
			{
				return false;
			}
			// It may be visible along with all its parents, but if the chain doesn't
			// go up to a form we can't really see it.
			return control.TopLevelControl is Form;
		}

		#region Properties
		/// <summary>
		/// Gets the views that are in the application.
		/// </summary>
		public IRootSite[] Views => m_availableSites.ToArray();

		/// <summary>
		/// Gets the active view of the owner of this ActiveViewHelper, or null if there is
		/// none.
		/// </summary>
		public IRootSite ActiveView
		{
			get
			{
				foreach (var rootSite in m_availableSites.ToList())
				{
					// Get rid of any deadbeat controls.
					var control = (Control)rootSite;
					if (control.IsDisposed || !control.IsHandleCreated || !(control.TopLevelControl is Form))
					{
						m_availableSites.Remove(rootSite);
						if (m_activeSite == rootSite)
						{
							m_activeSite = null;
						}
					}
				}
				if (m_activeSite != null)
				{
					var control = m_activeSite as Control;
					if (control == null)
					{
						return m_activeSite;
					}
					return (IsReallyVisible(m_activeSite) ? m_activeSite : null);
				}
				foreach (var site in m_availableSites)
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
		/// <summary>
		/// Handler for when a control is added to a control. Runs <see cref="DeepAddControl"/>
		/// on <c>e.Control</c>.
		/// </summary>
		private void ControlWasAdded(object sender, ControlEventArgs e)
		{
			DeepAddControl(e.Control);
		}

		/// <summary />
		private void ControlWasRemoved(object sender, ControlEventArgs e)
		{
			DeepRemoveControl(e.Control);
		}

		/// <summary>
		/// Handler for when a view gains focus.  This sets the active view to the view that got
		/// focus.
		/// </summary>
		private void ViewGotFocus(object sender, EventArgs e)
		{
			var oldActiveView = ActiveView;
			m_activeSite = sender as IRootSite;
			if (ActiveView != oldActiveView)
			{
				ActiveViewChanged?.Invoke(this, new EventArgs());
			}
		}

		///<summary>
		/// Fired when the active view changes (at least as a result of a view getting focus)
		///</summary>
		public event EventHandler<EventArgs> ActiveViewChanged;

		#endregion

		#region Private methods
		/// <summary>
		/// Recursively add handlers for the ControlAdded and GotFocus events of the specified
		/// control and all its sub-controls.
		/// </summary>
		private void DeepAddControl(Control control)
		{
			// Before blindly adding the event handler, first remove it if there is already
			// one subscribed. (This ensures that stray event handlers don't exist when controls
			// are removed and re-added to a form.)
			control.ControlAdded -= ControlWasAdded;
			control.ControlRemoved -= ControlWasRemoved;
			control.ControlAdded += ControlWasAdded;
			control.ControlRemoved += ControlWasRemoved;

			if (control is IRootSite)
			{
				// Before blindly adding the event handler, first remove it if there is
				// already one subscribed
				control.GotFocus -= ViewGotFocus;
				control.GotFocus += ViewGotFocus;
				m_availableSites.Add((IRootSite)control);
			}

			foreach (Control con in control.Controls)
			{
				DeepAddControl(con);
			}
		}

		/// <summary>
		/// Recursively remove handlers for the ControlAdded and GotFocus events of the specified
		/// control and all its sub-controls.
		/// </summary>
		private void DeepRemoveControl(Control control)
		{
			control.ControlAdded -= ControlWasAdded;
			control.ControlRemoved -= ControlWasRemoved;

			if (control is IRootSite)
			{
				control.GotFocus -= ViewGotFocus;
				m_availableSites.Remove((IRootSite)control);
				if (m_activeSite == control)
				{
					m_activeSite = null;
				}
			}
			foreach (Control childControl in control.Controls)
			{
				DeepRemoveControl(childControl);
			}
		}
		#endregion
	}
}