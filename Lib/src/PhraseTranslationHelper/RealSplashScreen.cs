// --------------------------------------------------------------------------------------------
#region // Copyright © 2012, SIL International. All Rights Reserved.
// <copyright from='2012' company='SIL International'>
//		Copyright © 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RealSplashScreen.cs
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows.Forms;
using SIL.Utils;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The real splash screen that the user sees. It gets created and handled by FwSplashScreen
	/// and runs in a separate thread.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class RealSplashScreen : Form
	{
		private readonly Screen m_displayToUse;

		#region Data members
		private EventWaitHandle m_waitHandle;
		private System.Threading.Timer m_timer;
		private SILUBS.PhraseTranslationHelper.TxlInfo m_txlInfo;
		private Label lblMessage;

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
		internal RealSplashScreen(Screen displayToUse)
		{
			m_displayToUse = displayToUse;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			Opacity = 0;
			HandleCreated += SetPosition;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		private void CheckDisposed()
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="marqueeGif gets added to Controls collection and disposed there")]
		private void InitializeComponent()
		{
			System.Windows.Forms.PictureBox marqueeGif;
			this.lblMessage = new System.Windows.Forms.Label();
			marqueeGif = new System.Windows.Forms.PictureBox();
			this.m_txlInfo = new SILUBS.PhraseTranslationHelper.TxlInfo();
			((System.ComponentModel.ISupportInitialize)(marqueeGif)).BeginInit();
			this.SuspendLayout();
			//
			// m_txlInfo
			//
			m_txlInfo.Location = new System.Drawing.Point(0, 0);
			m_txlInfo.Name = "m_txlInfo";
			m_txlInfo.Size = new System.Drawing.Size(623, 367);
			m_txlInfo.TabIndex = 0;
			m_txlInfo.TabStop = false;
			//
			// marqueeGif
			//
			marqueeGif.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources.wait22trans;
			marqueeGif.Location = new System.Drawing.Point(343, 368);
			marqueeGif.Name = "marqueeGif";
			marqueeGif.Size = new System.Drawing.Size(22, 22);
			marqueeGif.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			marqueeGif.TabIndex = 1;
			marqueeGif.TabStop = false;
			//
			// lblMessage
			//
			this.lblMessage.BackColor = System.Drawing.Color.Transparent;
			this.lblMessage.ForeColor = System.Drawing.Color.Black;
			this.lblMessage.Location = new System.Drawing.Point(12, 368);
			this.lblMessage.Name = "lblMessage";
			this.lblMessage.Size = new System.Drawing.Size(325, 23);
			this.lblMessage.TabIndex = 2;
			//
			// RealSplashScreen
			//
			this.BackColor = System.Drawing.Color.White;
			this.ClientSize = new System.Drawing.Size(629, 397);
			this.ControlBox = false;
			this.Controls.Add(m_txlInfo);
			this.Controls.Add(marqueeGif);
			this.Controls.Add(this.lblMessage);
			this.ForeColor = System.Drawing.Color.Black;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RealSplashScreen";
			this.Opacity = 0;
			this.ShowIcon = false;
			((System.ComponentModel.ISupportInitialize)(marqueeGif)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

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

			BringToFront();
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
			Close();
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
				m_timer = new System.Threading.Timer(UpdateDisplayCallback, null, 0, 50);
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
			Left = m_displayToUse.WorkingArea.X + (m_displayToUse.WorkingArea.Width - Width) / 2;
			Top = m_displayToUse.WorkingArea.Y + (m_displayToUse.WorkingArea.Height - Height) / 2;
		}
		#endregion

		#region Dynamic display related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Timer event to increase the opacity and make other necessary visual changes in the
		/// splash screen over time. Since this event occurs in a different thread from the one
		/// in which the form exists, we cannot set the form's opacity property in this thread
		/// because it will generate a cross threading error. Calling the invoke method will
		/// invoke the method on the same thread in which the form was created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateDisplayCallback(object state)
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
					Thread.CurrentThread.Name = "UpdateDisplayCallback";
#endif

					if (m_timer == null)
						return;

					// In some rare cases the splash screen is already disposed and the
					// timer is still running. It happened to me (EberhardB) when I stopped
					// debugging while starting up, but it might happen at other times too
					// - so just be safe.
					if (!IsDisposed && IsHandleCreated)
#if !__MonoCS__
						Invoke(new Action(UpdateDisplay));
#else // Windows have to be on the main thread on mono.
					{
						UpdateDisplay();
						Application.DoEvents(); // force a paint
					}
#endif
				}
				catch (Exception e)
				{
					// just ignore any exceptions
					Debug.WriteLine("Got exception in UpdateDisplayCallback: " + e.Message);
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
		private void UpdateDisplay()
		{
			try
			{
				double currentOpacity = Opacity;
				if (currentOpacity < 1.0)
				{
#if !__MonoCS__
					Opacity = currentOpacity + 0.05;
#else
					Opacity = currentOpacity + 0.025; // looks nicer on mono/linux
#endif
					currentOpacity = Opacity;
					if (currentOpacity == 1.0)
					{
						m_timer.Dispose();
						m_timer = null;
						m_txlInfo.StartMarqueeScrolling();
					}
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
		#endregion
	}
}
