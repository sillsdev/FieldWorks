// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MGAHtmlHelpDialog.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	public class MGAHtmlHelpDialog : MGADialog
	{
		private readonly WebBrowser m_webBrowserInfo;
		private XslCompiledTransform m_xslShowInfoTransform;
		private XmlDocument m_xmlShowInfoDoc;
		private readonly string m_sHelpHtm = Path.Combine(DirectoryFinder.FWCodeDirectory, String.Format("Language Explorer{0}MGA{0}Help.htm", Path.DirectorySeparatorChar));

		public MGAHtmlHelpDialog(FdoCache cache, Mediator mediator, string sMorphemeForm) : base(cache, mediator, sMorphemeForm)
		{
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
		}

		protected override void SetupInitialState()
		{
			// init transform used in help panel
			m_xslShowInfoTransform = new XslCompiledTransform();
			string sXsltFile = Path.Combine(DirectoryFinder.FWCodeDirectory, String.Format("Language Explorer{0}MGA{0}MGAShowInfo.xsl", Path.DirectorySeparatorChar));
			m_xslShowInfoTransform.Load(sXsltFile);

			// init XmlDoc, too
			m_xmlShowInfoDoc = new XmlDocument();

			splitContainerHorizontal.Panel2Collapsed = false;
			buttonInfo.Visible = true;

			m_webBrowserInfo.Navigate(m_sHelpHtm);
		}

		protected override void DisplayHelpInfo(XmlNode node)
		{
			using (var w = new StringWriter())
			using (var tw = new XmlTextWriter(w))
			{
				m_xmlShowInfoDoc.LoadXml(node.OuterXml); // N.B. LoadXml requires UTF-16 or UCS-2 encodings

				var args = new XsltArgumentList();
				args.AddParam("sHelpFile", "", m_sHelpHtm);
				m_xslShowInfoTransform.Transform(m_xmlShowInfoDoc, args, tw);
				m_webBrowserInfo.DocumentText = w.GetStringBuilder().ToString();
			}
		}

	}
}
