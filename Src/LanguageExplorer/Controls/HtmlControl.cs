// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Gecko;
using MsHtmHstInterop;

namespace LanguageExplorer.Controls
{
	/// <summary />
	public class HtmlControl : MainUserControl
	{
		private string m_url;
		private Container components = null;

		/// <summary>
		/// Allow owning class to check on the url
		/// </summary>
		public event HtmlControlEventHandler HCBeforeNavigate;

		/// <summary>
		/// Handle message "HCBeforeNavigate".
		/// </summary>
		protected virtual void OnHCBeforeNavigate(HtmlControlEventArgs e)
		{
			// Invokes the delegates.
			HCBeforeNavigate?.Invoke(this, e);
		}

		/// <summary>
		/// Get the browser.
		/// </summary>
		public GeckoWebBrowser Browser { get; private set; }

		/// <summary>
		/// Get/Set the URL for the control.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string URL
		{
			get
			{
				return m_url ?? "about:blank";
			}
			set
			{
				// Review (Hasso): is there a case in which we would *want* to set m_url to null specifically (not about:blank)?
				m_url = value ?? "about:blank";
				if (Browser.Handle == IntPtr.Zero)
				{
					return; // This should never happen.
				}
				Browser.Navigate(m_url);
			}
		}

		/// <summary>
		/// Get browser document object
		/// </summary>
		/// <remarks>
		/// Used for allowing a web page/JavaScript to interact with C# code.
		/// (See Src\LexText\ParserUI\ParseWordDlg.cs for an example)
		/// </remarks>
		public object Document => Browser.Document;

		/// <summary>
		/// The HTML text of the document currently loaded in the browser
		/// TODO: implement get for GeckoFX browser
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string DocumentText
		{
			get
			{
				// TODO pH 2013.09: GeckoFX implementation
				return @"<!DOCTYPE HTML><HTML/>";
			}
			set
			{
				Browser.LoadHtml(value, null);
			}
		}

		/// <summary />
		public HtmlControl()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			AccNameDefault = "HtmlControl";	// default accessibility name
			// no right-click context menu needed
			Browser.NoDefaultContextMenu = true;

		}

		/// <summary>
		/// Invoke "go back" in browser history
		/// </summary>
		public void Back()
		{
			try
			{
				Browser.GoBack();
			}
			catch
			{
				Browser.Refresh();
			}
		}

		/// <summary>
		/// Invoke "go forward" in browser history
		/// </summary>
		public void Forward()
		{
			try
			{
				Browser.GoForward();
			}
			catch
			{
				Browser.Refresh();
			}
		}

		/// <summary>
		/// note: this old version of before navigate is being used because of a bug in the.net framework.
		/// win this bug is fixed, we should look at switching to using BeforeNavigate2
		/// </summary>
		protected void OnBeforeNavigate(string url, int flags, string targetFrameName, ref object postData, string headers, ref bool wasHandled)
		{
			// Let the owning class know
			OnHCBeforeNavigate(new HtmlControlEventArgs(url));
		}

		/// <summary>
		/// Get key state.
		/// </summary>
		[DllImport("User32.dll")]
		public static extern short GetAsyncKeyState(int vKey);

		/// <summary>
		/// Allow a select set of keys be active when using the IDocHostUIHandler interface
		/// </summary>
		/// <param name="lpmsg">the message/key combination</param>
		public void AllowKeysForIDocHostUIHandler(tagMSG lpmsg)
		{
			const int WM_KEYDOWN = 0x0100;
			const int VK_CONTROL = 0x11;
			if (lpmsg.message == WM_KEYDOWN)
			{
				switch (lpmsg.wParam)
				{
					case (uint)Keys.Down:       // all of these are the same: allow them to have their normal function
					case (uint)Keys.Up:
					case (uint)Keys.Left:
					case (uint)Keys.Right:
					case (uint)Keys.PageDown:
					case (uint)Keys.PageUp:
						throw new COMException("", 1);  // returns HRESULT = S_FALSE
					case (uint) Keys.F:			// all of these are the same: allow these control key sequences
					case (uint) Keys.End:
					case (uint) Keys.Home:
						if (GetAsyncKeyState(VK_CONTROL) < 0)
						{
							throw new COMException("", 1); // returns HRESULT = S_FALSE
						}
						break;
					default:
						break; // do nothing
				}
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if ( disposing )
			{
				Browser?.Dispose();
				components?.Dispose();
			}
			m_url = null;
			Browser = null;

			base.Dispose( disposing );
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
//			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(HtmlControl));
			//this.m_browser = new AxSHDocVw.AxWebBrowser();
			this.Browser = new GeckoWebBrowser();
			//((System.ComponentModel.ISupportInitialize)(this.m_browser)).BeginInit();
			this.SuspendLayout();
			//
			// m_browser
			//
			this.Browser.Dock = System.Windows.Forms.DockStyle.Fill;
			//this.m_browser.Enabled = true;
			//this.m_browser.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("m_browser.OcxState")));
			this.Browser.Size = new System.Drawing.Size(320, 184);
			this.Browser.TabIndex = 1;
			//
			// HtmlControl
			//
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.Browser});
			this.Name = "HtmlControl";
			this.Size = new System.Drawing.Size(320, 184);
			//((System.ComponentModel.ISupportInitialize)(this.m_browser)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
