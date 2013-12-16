// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MGAHtmlHelpDialog.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
#if __MonoCS__
using Gecko;
#endif
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	public class MGAHtmlHelpDialog : MGADialog
	{
#if __MonoCS__
		private GeckoWebBrowser m_browser;
#else
		private readonly WebBrowser m_webBrowserInfo;
#endif
		private XslCompiledTransform m_xslShowInfoTransform;
		private XmlDocument m_xmlShowInfoDoc;
		private readonly string m_sHelpHtm = Path.Combine(DirectoryFinder.FWCodeDirectory, String.Format("Language Explorer{0}MGA{0}Help.htm", Path.DirectorySeparatorChar));

		/// <summary>
		/// Constructor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "Offending code is compiled only on Windows")]
		public MGAHtmlHelpDialog(FdoCache cache, Mediator mediator, string sMorphemeForm) : base(cache, mediator, sMorphemeForm)
		{
#if __MonoCS__
			m_browser = new GeckoWebBrowser
						{
							Dock = DockStyle.Fill,
							Location = new Point(0, 0),
							TabIndex = 1,
							MinimumSize = new Size(20, 20),
							NoDefaultContextMenu = true
						};
			splitContainerHorizontal.Panel2.Controls.Add(m_browser);
#else
			m_webBrowserInfo = new WebBrowser
								{
									Dock = DockStyle.Fill,
									Location = new Point(0, 0),
									TabIndex = 1,
									IsWebBrowserContextMenuEnabled = false,
									MinimumSize = new Size(20, 20),
									Name = "webBrowserInfo",
									WebBrowserShortcutsEnabled = false
								};

			splitContainerHorizontal.Panel2.Controls.Add(m_webBrowserInfo);
#endif
		}

		protected override void SetupInitialState()
		{
			// init transform used in help panel
			m_xslShowInfoTransform = new XslCompiledTransform();
			string sXsltFile = Path.Combine(DirectoryFinder.FWCodeDirectory, String.Format("Language Explorer{0}MGA{0}MGAShowInfo.xsl", Path.DirectorySeparatorChar));
			m_xslShowInfoTransform.Load(sXsltFile);

			// init XmlDoc, too
			m_xmlShowInfoDoc = new XmlDocument();

			ShowInfoPane();
			buttonInfo.Visible = true;

#if __MonoCS__
			if (m_browser.Handle != IntPtr.Zero)
				m_browser.Navigate(m_sHelpHtm);
#else
			m_webBrowserInfo.Navigate(m_sHelpHtm);
#endif
		}

		protected override void DisplayHelpInfo(XmlNode node)
		{
#if __MonoCS__
			var tempfile = Path.Combine(Path.GetTempPath(), "temphelp.htm");
			using (var w = new StreamWriter(tempfile, false))
#else
			using (var w = new StringWriter())
#endif
			using (var tw = new XmlTextWriter(w))
			{
				m_xmlShowInfoDoc.LoadXml(node.OuterXml); // N.B. LoadXml requires UTF-16 or UCS-2 encodings

				var args = new XsltArgumentList();
				args.AddParam("sHelpFile", "", m_sHelpHtm);
				m_xslShowInfoTransform.Transform(m_xmlShowInfoDoc, args, tw);
#if !__MonoCS__
				m_webBrowserInfo.DocumentText = w.GetStringBuilder().ToString();
#endif
			}
#if __MonoCS__
			m_browser.Navigate(tempfile);
#endif
		}

	}
}
