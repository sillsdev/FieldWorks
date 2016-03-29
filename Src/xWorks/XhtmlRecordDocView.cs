// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Gecko;
using XCore;
using SIL.Utils;
using SIL.Windows.Forms.HtmlBrowser;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// XhtmlRecordDocView implements a RecordView (view showing one object at a time from a sequence)
	/// in which the single object is displayed using generated XHTML in a (Gecko) browser.
	/// </summary>
	public class XhtmlRecordDocView : RecordView
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "element does NOT need to be disposed locally!")]
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
				XhtmlDocView.HandleDomRightClick(browser, e, element, m_propertyTable, m_mediator, m_configObjectName);
			}
		}

		protected override void ShowRecord()
		{
			if (!m_fullyInitialized)
				return;
			base.ShowRecord();
			var cmo = Clerk.CurrentObject;
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
	}
}
