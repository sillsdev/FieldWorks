// Copyright (c) 2016-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using Gecko;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.FDO;
using XCore;
using SIL.Utils;
using SIL.Windows.Forms.HtmlBrowser;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// XhtmlRecordDocView implements a RecordView (view showing one object at a time from a sequence)
	/// in which the single object is displayed using generated XHTML in a (Gecko) browser.
	/// </summary>
	public class XhtmlRecordDocView : RecordView, IVwNotifyChange
	{
		private XWebBrowser m_mainView;
		internal string m_configObjectName;

		public override void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			InitBase(mediator, propertyTable, configurationParameters);
			m_mainView = new XWebBrowser(XWebBrowser.BrowserType.GeckoFx);
			m_mainView.Dock = DockStyle.Fill;
			m_mainView.Location = new Point(0, 0);
			m_mainView.IsWebBrowserContextMenuEnabled = false;
			Controls.Add(m_mainView);
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser != null)
				browser.DomClick += OnDomClick;
			m_fullyInitialized = true;
			// Add ourselves as a listener for changes to the item we are displaying
			Clerk.VirtualListPublisher.AddNotification(this);
		}

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();
			var backColorName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters,
				"backColor", "Window");
			BackColor = Color.FromName(backColorName);
			m_configObjectName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "configureObjectName", null);
		}

		/// <summary>
		/// Handle a mouse click in the web browser displaying the xhtml.
		/// </summary>
		private void OnDomClick(object sender, DomMouseEventArgs e)
		{
			XhtmlDocView.CloseContextMenuIfOpen();
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser == null)
				return;
			var element = browser.DomDocument.ElementFromPoint(e.ClientX, e.ClientY);
			if (element == null || element.TagName == "html")
				return;
			if (e.Button == GeckoMouseButton.Left)
			{
				XhtmlDocView.HandleDomLeftClick(Clerk, e, element);
			}
			else if (e.Button == GeckoMouseButton.Right)
			{
				XhtmlDocView.HandleDomRightClick(browser, e, element, m_propertyTable, m_mediator);
			}
		}

		/// <summary>
		/// Enable the 'File Print...' menu option for the LexEdit dictionary preview
		/// </summary>
		public bool OnDisplayPrint(object parameter, UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = true;
			return true;
		}

		/// <summary>
		/// Handle the 'File Print...' menu item click (defined in the Lexicon areaConfiguration.xml)
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnPrint(object commandObject)
		{
			XhtmlDocView.PrintPage(m_mainView);
			return true;
		}

		protected override void ShowRecord()
		{
			if (!m_fullyInitialized || IsDisposed || m_mainView.IsDisposed || !Visible)
				return;
			base.ShowRecord();
			var cmo = Clerk.CurrentObject;
			// Don't steal focus
			Enabled = false;
			m_mainView.DocumentCompleted += EnableRecordDocView;
			if (cmo != null && cmo.Hvo > 0)
			{
				var configurationFile = DictionaryConfigurationListener.GetCurrentConfiguration(m_propertyTable);
				if (String.IsNullOrEmpty(configurationFile))
				{
					m_mainView.DocumentText = String.Format("<html><body><p>{0}</p></body></html>",
						xWorksStrings.ksNoConfiguration);
					return;
				}
				var configuration = new DictionaryConfigurationModel(configurationFile, Cache);
				var xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new [] { cmo.Hvo }, null, configuration, m_propertyTable);
				m_mainView.Url = new Uri(xhtmlPath);
				m_mainView.Refresh(WebBrowserRefreshOption.Completely);
			}
			else
			{
				m_mainView.DocumentText = "<html><body></body></html>";
			}
		}

		private void EnableRecordDocView(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			Enabled = true;
			m_mainView.DocumentCompleted -= EnableRecordDocView;
		}

		/// <summary>
		/// If the item we are showing changes update the view.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (Clerk == null || m_mainView == null || m_mediator == null || hvo != Clerk.CurrentObjectHvo)
				return;

			var gb = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (gb != null && gb.Document != null)
			{
				gb.Document.Body.SetAttribute("style", "background-color:#DEDEDE");
			}
			if (!m_mediator.IdleQueue.Contains(ShowRecordOnIdle))
			{
				m_mediator.IdleQueue.Add(IdleQueuePriority.High, ShowRecordOnIdle);
			}
		}

		private bool ShowRecordOnIdle(object arg)
		{
			if (IsDisposed)
				return true; // no longer necessary to refresh the view
			var ui = Cache.ServiceLocator.GetInstance<IFdoUI>();
			if (ui != null && DateTime.Now - ui.LastActivityTime < TimeSpan.FromMilliseconds(400))
				return false; // Don't interrupt a user who is busy typing. Wait for a pause to refresh the view.
			ShowRecord();
			return true;
		}
	}
}
