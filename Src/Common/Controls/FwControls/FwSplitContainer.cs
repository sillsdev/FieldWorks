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
// File: FwSplitContainer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Enhances a .NET SplitContainer control with properties for initial and maximum pane
	/// width percentage and persistence.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwSplitContainer : SplitContainer, ISettings
	{
		#region Member variables
		private float m_desiredFirstPanePercentage;
		private float m_maxFirstPanePercentage;
		private bool m_fShownBefore;
		/// <summary>registry settings</summary>
		protected Persistence m_persistence;
		/// <summary>indicates which panel of the two panels in SplitContainer should be
		/// activated</summary>
		protected Panel m_panelToActivate;
		private RegistryKey m_settingsRegKey;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwSplitContainer"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwSplitContainer()
		{
			InitializeComponent();

			SplitterMoving += new SplitterCancelEventHandler(OnSplitterMoving);
			SplitterMoved += new SplitterEventHandler(OnSplitterMoved);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwSplitContainer"/> class.
		/// </summary>
		/// <param name="settingsRegKey">The settings registry key used to persist settings.</param>
		/// ------------------------------------------------------------------------------------
		public FwSplitContainer(RegistryKey settingsRegKey): this()
		{
			SettingsKey = settingsRegKey;
		}

		#endregion

		#region Disposed stuff
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

		// Dispose method is in FwSplitContainer.Designer.cs
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set the desired percentage number for the first (top or left) pane's size
		/// (height or width).
		/// When the splitter is moved or the wrapper resized, the pane minimums may be enforced
		/// meaning the actual displayed ratio may become different. But the desired ratio is
		/// maintained by this property, and restored when the window is resized
		/// back to a larger size.
		/// Setting this property does not trigger any events in the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float DesiredFirstPanePercentage
		{
			get
			{
				CheckDisposed();
				return m_desiredFirstPanePercentage;
			}
			set
			{
				CheckDisposed();
				m_desiredFirstPanePercentage = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the maximum percentage the first pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float MaxFirstPanePercentage
		{
			get
			{
				CheckDisposed();
				return m_maxFirstPanePercentage;
			}
			set
			{
				CheckDisposed();
				m_maxFirstPanePercentage = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the pane that should be activated when this FwSplitContainer is
		/// activated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Panel PanelToActivate
		{
			get
			{
				CheckDisposed();
				return m_panelToActivate;
			}
			set
			{
				CheckDisposed();
				m_panelToActivate = value;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the visible changed event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (!m_fShownBefore && Visible)
			{
				bool fHorizSplitter = Orientation == Orientation.Horizontal;
				if (m_desiredFirstPanePercentage > 0 && m_desiredFirstPanePercentage <= 1)
					SplitterDistance = (int)(m_desiredFirstPanePercentage *
						(fHorizSplitter ? Height : Width));
				if (m_maxFirstPanePercentage > 0)
					OnSplitterMoved(new SplitterEventArgs(0, 0, fHorizSplitter ? 0 : SplitterDistance,
						fHorizSplitter ? SplitterDistance : 0));

				m_fShownBefore = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user moves the splitter.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.SplitterCancelEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnSplitterMoving(object sender, SplitterCancelEventArgs e)
		{
			if (m_maxFirstPanePercentage <= 0 || m_maxFirstPanePercentage > 1)
				return;

			if (Orientation == Orientation.Horizontal)
			{
				if ((float)e.SplitY > (float)(Height * m_maxFirstPanePercentage) && Height > 0)
					e.SplitY = (int)(Height * m_maxFirstPanePercentage);
			}
			else
			{
				if ((float)e.SplitX > (float)(Width * m_maxFirstPanePercentage) && Width > 0)
					e.SplitX = (int)(Width * m_maxFirstPanePercentage);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called after the splitter moved.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.SplitterEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnSplitterMoved(object sender, SplitterEventArgs e)
		{
			if (m_maxFirstPanePercentage <= 0 || m_maxFirstPanePercentage > 1)
				return;

			if (Orientation == Orientation.Horizontal)
			{
				if ((float)e.SplitY > (float)(Height * m_maxFirstPanePercentage) && Height > 0)
					SplitterDistance = (int)(Height * m_maxFirstPanePercentage);
			}
			else
			{
				if ((float)e.SplitX > (float)(Width * m_maxFirstPanePercentage) && Width > 0)
					SplitterDistance = (int)(Width * m_maxFirstPanePercentage);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.GotFocus"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			if (PanelToActivate != null)
			{
				// We loop throught the controls on the panel. If we find one that is visible
				// and is either not docked or has Dock set to Fill that one gets focus.
				foreach (Control ctrl in PanelToActivate.Controls)
				{
					if (ctrl.Visible && (ctrl.Dock == DockStyle.None || ctrl.Dock == DockStyle.Fill))
					{
						ctrl.Focus();
						return;
					}
				}
				PanelToActivate.Focus();
			}
		}
		#endregion

		#region ISettings Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the window should keep its current Size and Location
		/// properities when it is displayed.
		/// Returns false if the window should override these with values persisted in the
		/// registry. By default, most implementations should return false.
		/// This is irrelevant for non-Forms; their implementation should return false also.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool KeepWindowSizePos
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement this method to save the persistent settings for your class. Normally, this
		/// should also result in recursively calling SaveSettingsNow for all children which
		/// also implement ISettings.
		/// </summary>
		/// <remarks>Note: A form's implementation of <see cref="M:SIL.FieldWorks.Common.Controls.ISettings.SaveSettingsNow"/> normally
		/// calls <see cref="M:SIL.FieldWorks.Common.Controls.Persistence.SaveSettingsNow(System.Windows.Forms.Control)"/> (after optionally saving any
		/// class-specific properties).</remarks>
		/// ------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();
			if (m_persistence != null)
			{
				m_persistence.SaveSettingsNow(this);
				// Since Panel1 and Panel2 don't implement ISettings, we have to call
				// SaveSettingsNow explicitly so that our grand-children get a chance to save
				// their information!
				m_persistence.SaveSettingsNow(Panel1);
				m_persistence.SaveSettingsNow(Panel2);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a key in the registry where <see cref="T:SIL.FieldWorks.Common.Controls.Persistence"/> should store settings.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public RegistryKey SettingsKey
		{
			get { return m_settingsRegKey; }
			set
			{
				if (m_persistence != null)
				{
					m_persistence.Dispose();
					m_persistence = null;
				}
				m_settingsRegKey = value;
				if (m_settingsRegKey != null)
				{
					m_persistence = new Persistence(components, this);
					m_persistence.EnableSaveWindowSettings = false;
					m_persistence.LoadSettings += new Persistence.Settings(OnLoadSettings);
					m_persistence.SaveSettings += new Persistence.Settings(OnSaveSettings);
				}
			}
		}

		#endregion

		#region Persistence
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to save settings.
		/// </summary>
		/// <param name="key">The key.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnSaveSettings(RegistryKey key)
		{
			CheckDisposed();
			if (m_desiredFirstPanePercentage > 0)
			{
				RegistryHelper.WriteFloatSetting(SettingsKey,
					Name + "FirstPanePercentage", SplitterDistance /
					(float)((Orientation == Orientation.Horizontal) ? Height : Width));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to load settings.
		/// </summary>
		/// <param name="key">The key.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void OnLoadSettings(RegistryKey key)
		{
			CheckDisposed();
			float desiredFirstPanePercentage = RegistryHelper.ReadFloatSetting(SettingsKey,
				Name + "FirstPanePercentage", m_desiredFirstPanePercentage);
			if (desiredFirstPanePercentage > 0 && desiredFirstPanePercentage <= 1)
			{
				DesiredFirstPanePercentage = desiredFirstPanePercentage;
				SplitterDistance = (int)(desiredFirstPanePercentage *
					((Orientation == Orientation.Horizontal) ? Height : Width));
			}
		}

		#endregion
	}
}
