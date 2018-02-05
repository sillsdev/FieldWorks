// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using Gecko;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.MGA
{
	public class MGAHtmlHelpDialog : MGADialog
	{
		private GeckoWebBrowser m_browser;
		private XslCompiledTransform m_xslShowInfoTransform;
		private XmlDocument m_xmlShowInfoDoc;
		private readonly string m_sHelpHtm = Path.Combine(FwDirectoryFinder.CodeDirectory, string.Format("Language Explorer{0}MGA{0}Help.htm", Path.DirectorySeparatorChar));

		/// <summary>
		/// Constructor.
		/// </summary>
		public MGAHtmlHelpDialog(LcmCache cache, IHelpTopicProvider helpTopicProvider, string sMorphemeForm)
			: base(cache, helpTopicProvider, sMorphemeForm)
		{
			m_browser = new GeckoWebBrowser
						{
							Dock = DockStyle.Fill,
							Location = new Point(0, 0),
							TabIndex = 1,
							MinimumSize = new Size(20, 20),
							NoDefaultContextMenu = true
						};
			splitContainerHorizontal.Panel2.Controls.Add(m_browser);
		}

		protected override void SetupInitialState()
		{
			// init transform used in help panel
			m_xslShowInfoTransform = new XslCompiledTransform();
			var sXsltFile = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "MGA", "MGAShowInfo.xsl");
			m_xslShowInfoTransform.Load(sXsltFile);

			// init XmlDoc, too
			m_xmlShowInfoDoc = new XmlDocument();

			ShowInfoPane();
			buttonInfo.Visible = true;

			if (m_browser.Handle != IntPtr.Zero)
			{
				var uri = new Uri(m_sHelpHtm);
				m_browser.Navigate(uri.AbsoluteUri);
			}
		}

		protected override void DisplayHelpInfo(XmlNode node)
		{
			var tempfile = Path.Combine(Path.GetTempPath(), "temphelp.htm");
			using (var w = new StreamWriter(tempfile, false))
			using (var tw = new XmlTextWriter(w))
			{
				m_xmlShowInfoDoc.LoadXml(node.OuterXml); // N.B. LoadXml requires UTF-16 or UCS-2 encodings

				var args = new XsltArgumentList();
				args.AddParam("sHelpFile", "", m_sHelpHtm);
				m_xslShowInfoTransform.Transform(m_xmlShowInfoDoc, args, tw);
			}
			var uri = new Uri(tempfile);
			m_browser.Navigate(uri.AbsoluteUri);
		}

		#region Overrides of MGADialog

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_browser.Dispose();
			}

			m_browser = null;

			base.Dispose(disposing);
		}

		#endregion
	}
}
