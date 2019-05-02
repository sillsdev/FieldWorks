// Copyright (c) 2016-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using Gecko;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.DictionaryConfiguration;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Windows.Forms.HtmlBrowser;
using SIL.Xml;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// XhtmlRecordDocView implements a RecordView (view showing one object at a time from a sequence)
	/// in which the single object is displayed using generated XHTML in a (Gecko) browser.
	/// </summary>
	internal class XhtmlRecordDocView : RecordView, IVwNotifyChange
	{
		private XWebBrowser m_mainView;
		internal string m_configObjectName;
		private UiWidgetController _uiWidgetController;

		public XhtmlRecordDocView(XElement configurationParameters, LcmCache cache, IRecordList recordList, UiWidgetController uiWidgetController)
			: base(configurationParameters, cache, recordList)
		{
			_uiWidgetController = uiWidgetController;
			// Add handler stuff.
			var userController = new UserControlUiWidgetParameterObject(this);
			userController.MenuItemsForUserControl[MainMenu.File].Add(Command.CmdPrint, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(PrintMenu_Click, () => CanShowPrintMenu));
			_uiWidgetController.AddHandlers(userController);
		}

		#region Overrides of RecordView
		/// <summary />
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
				_uiWidgetController.RemoveUserControlHandlers(this);
			}

			base.Dispose(disposing);

			_uiWidgetController = null;
		}
		#endregion

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			InitBase();
			m_mainView = new XWebBrowser(XWebBrowser.BrowserType.GeckoFx)
			{
				Dock = DockStyle.Fill,
				Location = new Point(0, 0),
				IsWebBrowserContextMenuEnabled = false
			};
			Controls.Add(m_mainView);
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser != null)
			{
				browser.DomClick += OnDomClick;
			}
			m_fullyInitialized = true;
			// Add ourselves as a listener for changes to the item we are displaying
			MyRecordList.VirtualListPublisher.AddNotification(this);
		}

		internal void ReallyShowRecordNow()
		{
			ShowRecord();
		}

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();
			var backColorName = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "backColor", "Window");
			BackColor = Color.FromName(backColorName);
			m_configObjectName = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "configureObjectName", null);
		}

		/// <summary>
		/// Handle a mouse click in the web browser displaying the xhtml.
		/// </summary>
		private void OnDomClick(object sender, DomMouseEventArgs e)
		{
			XhtmlDocView.CloseContextMenuIfOpen();
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			var element = browser?.DomDocument.ElementFromPoint(e.ClientX, e.ClientY);
			if (element == null || element.TagName == "html")
			{
				return;
			}
			switch (e.Button)
			{
				case GeckoMouseButton.Left:
					XhtmlDocView.HandleDomLeftClick(MyRecordList, Cache.ServiceLocator.ObjectRepository, e, element);
					break;
				case GeckoMouseButton.Right:
					XhtmlDocView.HandleDomRightClick(browser, e, element, new FlexComponentParameters(PropertyTable, Publisher, Subscriber), m_configObjectName, Cache, MyRecordList);
					break;
			}
		}

		private static Tuple<bool, bool> CanShowPrintMenu => new Tuple<bool, bool>(true, true);

		/// <summary>
		/// Handle the 'File Print...' menu item click
		/// </summary>
		private void PrintMenu_Click(object sender, EventArgs e)
		{
			if (!ContainsFocus)
			{
				return;
			}
			XhtmlDocView.PrintPage(m_mainView);
		}

		protected override void ShowRecord()
		{
			if (!m_fullyInitialized)
			{
				return;
			}
			base.ShowRecord();
			var cmo = MyRecordList.CurrentObject;
			// Don't steal focus
			Enabled = false;
			m_mainView.DocumentCompleted += EnableRecordDocView;
			if (cmo != null && cmo.Hvo > 0)
			{
				var configurationFile = DictionaryConfigurationServices.GetCurrentConfiguration(PropertyTable);
				if (string.IsNullOrEmpty(configurationFile))
				{
					m_mainView.DocumentText = $"<html><body><p>{LexiconResources.ksNoConfiguration}</p></body></html>";
					return;
				}
				var configuration = new DictionaryConfigurationModel(configurationFile, Cache);
				var xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { cmo.Hvo }, null, configuration, PropertyTable, Cache, MyRecordList);
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
#if RANDYTODO
			if (MyRecordList == null || m_mainView == null || m_mediator == null || hvo != MyRecordList.CurrentObjectHvo)
			{
				return;
			}
			var gb = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (gb != null && gb.Document != null)
			{
				gb.Document.Body.SetAttribute("style", "background-color:#DEDEDE");
			}
			if (!m_mediator.IdleQueue.Contains(ShowRecordOnIdle))
			{
				m_mediator.IdleQueue.Add(IdleQueuePriority.High, ShowRecordOnIdle);
			}
#endif
		}

		private bool ShowRecordOnIdle(object arg)
		{
			if (IsDisposed)
			{
				return true; // no longer necessary to refresh the view
			}
			var ui = Cache.ServiceLocator.GetInstance<ILcmUI>();
			if (ui != null && DateTime.Now - ui.LastActivityTime < TimeSpan.FromMilliseconds(400))
			{
				return false; // Don't interrupt a user who is busy typing. Wait for a pause to refresh the view.
			}
			ShowRecord();
			return true;
		}
	}
}