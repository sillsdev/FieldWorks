// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TxlInfo.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public partial class TxlInfo : UserControl
	{
		private System.Threading.Timer m_timer;
		/// <summary>Used for locking the splash screen</summary>
		/// <remarks>Note: we can't use lock(this) (or equivalent) since .NET uses lock(this)
		/// e.g. in it's Dispose(bool) method which might result in dead locks!
		/// </remarks>
		internal object m_Synchronizer = new object();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TxlInfo"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TxlInfo()
		{
			InitializeComponent();

			if (ParentForm != null)
			{
				ParentForm.FormClosing += (sender, args) =>
				{
					if (m_timer != null)
						m_timer.Change(Timeout.Infinite, Timeout.Infinite);
				};
			}

			// Get copyright information from assembly info. By doing this we don't have
			// to update the splash screen each year.
			var assembly = Assembly.GetExecutingAssembly();
			object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
			if (attributes.Length > 0)
				m_lblCopyright.Text = ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
			m_lblCopyright.Text = string.Format(Properties.Resources.kstidCopyrightFmt, m_lblCopyright.Text.Replace("(C)", "©"));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (m_timer != null)
					m_timer.Dispose();
			}
			m_timer = null;
			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts the marquee scrolling.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void StartMarqueeScrolling()
		{
			if (!IsDisposed && IsHandleCreated)
				m_timer = new System.Threading.Timer(ScrollCreditsCallback, null, 0, 700);
		}

		#region Marquee scrolling related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Timer event to scroll the credits "marquee" style. Since this event can occur in a
		/// different thread from the one in which the form exists, we cannot make the change in
		/// this thread because it could generate a cross threading error. Calling the invoke
		/// method will invoke the method on the same thread in which the form was created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ScrollCreditsCallback(object state)
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
					Thread.CurrentThread.Name = "ScrollCreditsCallback";
#endif

					// In some rare cases the splash screen is already disposed and the
					// timer is still running. It happened to me (EberhardB) when I stopped
					// debugging while starting up, but it might happen at other times too
					// - so just be safe.
					if (m_timer == null || IsDisposed)
						return;

#if !__MonoCS__
					Invoke(new Action(ScrollCredits));
#else // Windows have to be on the main thread on mono.
					UpdateDisplay();
					Application.DoEvents(); // force a paint
#endif
				}
				catch (Exception e)
				{
					// just ignore any exceptions
					Debug.WriteLine("Got exception in ScrollCreditsCallback: " + e.Message);
				}
				finally
				{
					Monitor.Exit(m_Synchronizer);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Give a "scrolling marquee" effect with the credits
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ScrollCredits()
		{
			try
			{
				Control first = m_pnlCredits.Controls[0];
				m_pnlCredits.Controls.RemoveAt(0);
				m_pnlCredits.Controls.Add(first);
			}
			catch
			{
			}
		}
		#endregion

	}
}
