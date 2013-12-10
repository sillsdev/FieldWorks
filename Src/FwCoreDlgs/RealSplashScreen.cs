// --------------------------------------------------------------------------------------------
#region // Copyright (C) 2002-2004, SIL International. All Rights Reserved.
// <copyright from='2002' to='2004' company='SIL International'>
//		Copyright (C) 2002-2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwSplashScreen.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The real splash screen that the user sees. It gets created and handled by FwSplashScreen
	/// and runs in a separate thread.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class RealSplashScreen : Form, IProgress, IFWDisposable
	{
		#region Events
		event CancelEventHandler IProgress.Canceling
		{
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}
		#endregion

		#region Data members
		private delegate void UpdateOpacityDelegate();

		private Assembly m_productExecutableAssembly;
		private Label lblProductName;
		private Label lblCopyright;
		private Label lblMessage;
		private System.Threading.Timer m_timer;
		private Panel m_panel;
		private Label lblAppVersion;
		private Label lblFwVersion;
		private PictureBox m_picSilLogo;
		private EventWaitHandle m_waitHandle;

		private ProgressLine progressLine;
		private PictureBox marqueeGif;
		private bool m_fDisplaySILInfo;
		private Label m_lblSuiteName;

		/// <summary>Used for locking the splash screen</summary>
		/// <remarks>Note: we can't use lock(this) (or equivalent) since .NET uses lock(this)
		/// e.g. in it's Dispose(bool) method which might result in dead locks!
		/// </remarks>
		internal object m_Synchronizer = new object();
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor for FwSplashScreen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private RealSplashScreen()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			// Don't fade in on Linux to work-around for timer issue with UpdateOpacityCallback, fixing FWNX-959.
			if (MiscUtils.IsUnix)
				Opacity = 1;

			HandleCreated += SetPosition;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor for FwSplashScreen
		/// </summary>
		/// <param name="fDisplaySILInfo">if set to <c>false</c>, any SIL-identifying information
		/// will be hidden.</param>
		/// ------------------------------------------------------------------------------------
		public RealSplashScreen(bool fDisplaySILInfo) : this()
		{
			m_fDisplaySILInfo = fDisplaySILInfo;
			m_picSilLogo.Visible = fDisplaySILInfo;
			if (!fDisplaySILInfo)
				m_lblSuiteName.Text = m_lblSuiteName.Text.Replace(Application.CompanyName, string.Empty).Trim();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));

#if __MonoCS__
			// Note: mono can only create a winform on the main thread.
			// So this ensures the progress bar gets painted when modified.
			if (IsHandleCreated)
				Application.DoEvents();
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disposes of the resources (other than memory) used by the
		/// <see cref="T:System.Windows.Forms.Form"></see>.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false
		/// to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_timer != null)
					m_timer.Dispose();
			}
			m_timer = null;
			m_waitHandle = null;
			base.Dispose(disposing);
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RealSplashScreen));
			this.m_lblSuiteName = new System.Windows.Forms.Label();
			this.m_picSilLogo = new System.Windows.Forms.PictureBox();
			this.lblProductName = new System.Windows.Forms.Label();
			this.lblCopyright = new System.Windows.Forms.Label();
			this.lblMessage = new System.Windows.Forms.Label();
			this.lblAppVersion = new System.Windows.Forms.Label();
			this.m_panel = new System.Windows.Forms.Panel();
			this.marqueeGif = new System.Windows.Forms.PictureBox();
			this.progressLine = new SIL.FieldWorks.Common.Controls.ProgressLine();
			this.lblFwVersion = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_picSilLogo)).BeginInit();
			this.m_panel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.marqueeGif)).BeginInit();
			this.SuspendLayout();
			//
			// m_lblSuiteName
			//
			resources.ApplyResources(this.m_lblSuiteName, "m_lblSuiteName");
			this.m_lblSuiteName.BackColor = System.Drawing.Color.Transparent;
			this.m_lblSuiteName.Name = "m_lblSuiteName";
			//
			// m_picSilLogo
			//
			resources.ApplyResources(this.m_picSilLogo, "m_picSilLogo");
			this.m_picSilLogo.Name = "m_picSilLogo";
			this.m_picSilLogo.TabStop = false;
			//
			// lblProductName
			//
			resources.ApplyResources(this.lblProductName, "lblProductName");
			this.lblProductName.BackColor = System.Drawing.Color.Transparent;
			this.lblProductName.ForeColor = System.Drawing.Color.Black;
			this.lblProductName.Name = "lblProductName";
			this.lblProductName.UseMnemonic = false;
			//
			// lblCopyright
			//
			resources.ApplyResources(this.lblCopyright, "lblCopyright");
			this.lblCopyright.BackColor = System.Drawing.Color.Transparent;
			this.lblCopyright.ForeColor = System.Drawing.Color.Black;
			this.lblCopyright.Name = "lblCopyright";
			//
			// lblMessage
			//
			resources.ApplyResources(this.lblMessage, "lblMessage");
			this.lblMessage.BackColor = System.Drawing.Color.Transparent;
			this.lblMessage.ForeColor = System.Drawing.Color.Black;
			this.lblMessage.Name = "lblMessage";
			//
			// lblAppVersion
			//
			resources.ApplyResources(this.lblAppVersion, "lblAppVersion");
			this.lblAppVersion.BackColor = System.Drawing.Color.Transparent;
			this.lblAppVersion.Name = "lblAppVersion";
			this.lblAppVersion.UseMnemonic = false;
			//
			// m_panel
			//
			this.m_panel.BackColor = System.Drawing.Color.Transparent;
			this.m_panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel.Controls.Add(this.marqueeGif);
			this.m_panel.Controls.Add(this.progressLine);
			this.m_panel.Controls.Add(this.lblFwVersion);
			this.m_panel.Controls.Add(this.m_picSilLogo);
			this.m_panel.Controls.Add(this.m_lblSuiteName);
			this.m_panel.Controls.Add(this.lblAppVersion);
			this.m_panel.Controls.Add(this.lblMessage);
			this.m_panel.Controls.Add(this.lblCopyright);
			this.m_panel.Controls.Add(this.lblProductName);
			resources.ApplyResources(this.m_panel, "m_panel");
			this.m_panel.Name = "m_panel";
			//
			// marqueeGif
			//
			resources.ApplyResources(this.marqueeGif, "marqueeGif");
			this.marqueeGif.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.wait22trans;
			this.marqueeGif.Name = "marqueeGif";
			this.marqueeGif.TabStop = false;
			//
			// progressLine
			//
			this.progressLine.BackColor = System.Drawing.Color.White;
			this.progressLine.ForeColor = System.Drawing.SystemColors.Control;
			this.progressLine.ForeColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(90)))), ((int)(((byte)(152)))));
			this.progressLine.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			resources.ApplyResources(this.progressLine, "progressLine");
			this.progressLine.MaxValue = 1000;
			this.progressLine.Name = "progressLine";
			//
			// lblFwVersion
			//
			resources.ApplyResources(this.lblFwVersion, "lblFwVersion");
			this.lblFwVersion.BackColor = System.Drawing.Color.Transparent;
			this.lblFwVersion.Name = "lblFwVersion";
			this.lblFwVersion.UseMnemonic = false;
			//
			// RealSplashScreen
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.ControlBox = false;
			this.Controls.Add(this.m_panel);
			this.ForeColor = System.Drawing.Color.Black;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RealSplashScreen";
			this.Opacity = 0;
			((System.ComponentModel.ISupportInitialize)(this.m_picSilLogo)).EndInit();
			this.m_panel.ResumeLayout(false);
			this.m_panel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.marqueeGif)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Public Methods
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Activates (brings back to the top) the splash screen (assuming it is already visible
		/// and the application showing it is the active application).
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public void RealActivate()
		{
			CheckDisposed();

			base.BringToFront();
			Refresh();
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the splash screen
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public void RealClose()
		{
			CheckDisposed();

			if (m_timer != null)
				m_timer.Change(Timeout.Infinite, Timeout.Infinite);
			base.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the assembly of the product-specific EXE (e.g., TE.exe or FLEx.exe).
		/// .Net callers should set this.
		/// </summary>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		public void SetProductExecutableAssembly(Assembly value)
		{
			m_productExecutableAssembly = value;
			InitControlLabels();
		}
		#endregion

		#region Internal properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the splash screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal EventWaitHandle WaitHandle
		{
			set { m_waitHandle = value; }
		}
		#endregion

		#region Non-public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.VisibleChanged"></see> event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (Visible)
			{
				m_waitHandle.Set();
				m_timer = new System.Threading.Timer(UpdateOpacityCallback, null, 0, 50);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize text of controls prior to display
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void InitControlLabels()
		{
			try
			{
				// Set the Application label to the name of the app
				if (m_productExecutableAssembly != null)
				{
					VersionInfoProvider viProvider = new VersionInfoProvider(m_productExecutableAssembly, m_fDisplaySILInfo);
					lblProductName.Text = viProvider.ProductName;
					Text = lblProductName.Text;
					lblAppVersion.Text = viProvider.ApplicationVersion;
					lblFwVersion.Text = viProvider.MajorVersion;
					lblCopyright.Text = viProvider.CopyrightString + Environment.NewLine + viProvider.LicenseString;
				}
			}
			catch
			{
				// ignore errors
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tasks needing to be done when Window is being opened:
		///		Set window position.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetPosition(object obj, System.EventArgs e)
		{
			Left = (ScreenUtils.PrimaryScreen.WorkingArea.Width - Width) / 2;
			Top = (ScreenUtils.PrimaryScreen.WorkingArea.Height - Height) / 2;
		}
		#endregion

		#region Opacity related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Timer event to increase the opacity of the splash screen over time. Since this
		/// event occurs in a different thread from the one in which the form exists, we
		/// cannot set the form's opacity property in this thread because it will generate
		/// a cross threading error. Calling the invoke method will invoke the method on
		/// the same thread in which the form was created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateOpacityCallback(object state)
		{
			// This callback might get called multiple times before the Invoke is finished,
			// which causes some problems. We just ignore any callbacks we get while we are
			// processing one, so we are using TryEnter/Exit(m_Synchronizer) instead of
			// lock(m_Synchronizer).
			// We sync on "m_Synchronizer" so that we're using the same flag as the FwSplashScreen class.
			if (Monitor.TryEnter(m_Synchronizer))
			{
				try
				{

#if DEBUG && !__MonoCS__
					Thread.CurrentThread.Name = "UpdateOpacityCallback";
#endif

					if (m_timer == null)
						return;

					// In some rare cases the splash screen is already disposed and the
					// timer is still running. It happened to me (EberhardB) when I stopped
					// debugging while starting up, but it might happen at other times too
					// - so just be safe.
					if (!IsDisposed && IsHandleCreated)
#if !__MonoCS__
						this.Invoke(new UpdateOpacityDelegate(UpdateOpacity));
#else // Windows have to be on the main thread on mono.
					{
						UpdateOpacity();
						Application.DoEvents(); // force a paint
					}
#endif
				}
				catch (Exception e)
				{
					// just ignore any exceptions
					Debug.WriteLine("Got exception in UpdateOpacityCallback: " + e.Message);
				}
				finally
				{
					Monitor.Exit(m_Synchronizer);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateOpacity()
		{
			try
			{
				double currentOpacity = Opacity;
				if (currentOpacity < 1.0)
#if !__MonoCS__
					Opacity = currentOpacity + 0.05;
#else
					Opacity = currentOpacity + 0.025; // looks nicer on mono/linux
#endif
				else if (m_timer != null)
				{
					m_timer.Dispose();
					m_timer = null;
				}
			}
			catch
			{
			}
		}
		#endregion

		#region IProgress implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the message to display to indicate startup activity on the splash screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			get
			{
				CheckDisposed();
				return lblMessage.Text;
			}
			set
			{
				CheckDisposed();

				// In some rare cases, setting the text causes an exception which should just
				// be ignored.
				try
				{
					lblMessage.Text = value;
				}
				catch { }
			}
		}

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return this; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the form displaying the progress (used for message box owners, etc). If the progress
		/// is not associated with a visible Form, then this returns its owning form, if any.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Form Form
		{
			get { return this; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		public bool IsIndeterminate
		{
			get { return marqueeGif.Visible; }
			set { marqueeGif.Visible = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the opertation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowCancel
		{
			get { return false; }
			set { throw new NotImplementedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a Position
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Position
		{
			get
			{
				CheckDisposed();
				return progressLine.Value;
			}
			set
			{
				if (value < progressLine.MinValue)
					progressLine.Value = progressLine.MinValue;
				else if (value > progressLine.MaxValue)
					progressLine.Value = progressLine.MaxValue;
				else
					progressLine.Value = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the minimum
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Minimum
		{
			get
			{
				CheckDisposed();
				return progressLine.MinValue;
			}
			set
			{
				CheckDisposed();
				progressLine.MinValue = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the maximum
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Maximum
		{
			get
			{
				CheckDisposed();
				return progressLine.MaxValue;
			}
			set
			{
				CheckDisposed();
				progressLine.MaxValue = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member Step
		/// </summary>
		/// <param name="nStepAmt">nStepAmt</param>
		/// ------------------------------------------------------------------------------------
		public void Step(int nStepAmt)
		{
			CheckDisposed();

			//Debug.WriteLine(string.Format("Step {0}; value is {1}", nStepAmt, progressLine.Value));

			if (nStepAmt > 0)
				progressLine.Increment(nStepAmt);
			else
				progressLine.PerformStep();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the title of the progress display window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Title
		{
			get { return Text; }
			set { Text = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a StepSize
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int StepSize
		{
			get
			{
				CheckDisposed();
				return progressLine.Step;
			}
			set
			{
				CheckDisposed();
				progressLine.Step = value;
			}
		}

		#endregion
	}
}
