// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.COMInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Discourse export dialog implements a dialog for exporting the discourse chart.
	/// Considerable refactoring is in order to share more code with InterlinearExportDialog,
	/// or move common code down to ExportDialog. This has been postponed in the interests
	/// of being able to release FW 5.2.1 without requiring changes to DLLs other than Discourse.
	/// </summary>
	public class DiscourseExportDialog : ExportDialog
	{
		private readonly List<XmlNode> m_ddNodes = new List<XmlNode>(8); // Saves XML nodes used to configure items.
		readonly int m_hvoRoot;
		readonly IVwViewConstructor m_vc;
		readonly int m_wsLineNumber;

		public DiscourseExportDialog(int hvoRoot, IVwViewConstructor vc, int wsLineNumber)
		{
			m_hvoRoot = hvoRoot;
			m_vc = vc;
			m_wsLineNumber = wsLineNumber;
		}

		#region Overrides of ExportDialog

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public override void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			base.InitializeFlexComponent(propertyTable, publisher, subscriber);

			m_helpTopic = "khtpExportDiscourse";
			columnHeader1.Text = LanguageExplorerResources.ksFormat;
			columnHeader2.Text = LanguageExplorerResources.ksExtension;
			Text = LanguageExplorerResources.ksExportDiscourse;
		}

		#endregion

		protected override string ConfigurationFilePath
		{
			get { return String.Format("Language Explorer{0}Export Templates{0}Discourse",
				Path.DirectorySeparatorChar); }
		}

		// Items in this version are never disabled.
		protected override bool ItemDisabled(string tag)
		{
			return false;
		}

		/// <summary>
		/// Override to do nothing since not configuring an FXT export process.
		/// </summary>
		/// <param name="document"></param>
		/// <param name="item"></param>
		/// <param name="ddNode"></param>
		protected override void ConfigureItem(XmlDocument document, ListViewItem item, XmlNode ddNode)
		{
			m_ddNodes.Add(ddNode);
			columnHeader1.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
			//columnHeader2.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		}

		// Export the data according to specifications.
		// Prime candidate for refactoring, almost identical to base class method once we reinstate OO, as we
		// will want to. Main diffs are using a different class of exporter and a different directory path.
		protected override void DoExport(string outPath)
		{
			var fxtPath = (string)m_exportList.SelectedItems[0].Tag;
			var ddNode = m_ddNodes[NodeIndex(fxtPath)];
			var mode = XmlUtils.GetOptionalAttributeValue(ddNode, "mode", "xml");
			using (new WaitCursor(this))
			{
				try
				{
					DiscourseExporter exporter;
					ExportPhase1(out exporter, outPath);
					var rootDir = FwDirectoryFinder.CodeDirectory;
					var transform = XmlUtils.GetOptionalAttributeValue(ddNode, "transform", "");
					var sTransformPath = Path.Combine(rootDir,
						String.Format("Language Explorer{0}Export Templates{0}Discourse",
						Path.DirectorySeparatorChar));
					switch (mode)
					{
						case "doNothing":
							break;
						case "applySingleTransform":
							string sTransform = Path.Combine(sTransformPath, transform);
							exporter.PostProcess(sTransform, outPath, 1);
							break;
					}
				}
				catch (Exception e)
				{
					MessageBox.Show(this, String.Format(LanguageExplorerResources.ksExportErrorMsg, e.Message));
				}
			}
			Close();
		}

		protected int NodeIndex(string tag)
		{
			var file = tag.Substring(tag.LastIndexOf('\\') + 1);
			for (var i = 0; i < m_ddNodes.Count; i++)
			{
				var fileN = m_ddNodes[i].BaseURI.Substring(m_ddNodes[i].BaseURI.LastIndexOf('/') + 1);
				if (fileN == file)
					return i;
			}
			return 0;
		}

		internal bool ExportPhase1(out DiscourseExporter exporter, string fileName)
		{
			CheckDisposed();

			using (var writer = new XmlTextWriter(fileName, System.Text.Encoding.UTF8))
			{
				writer.WriteStartDocument();
				writer.WriteStartElement("document");
				exporter = new DiscourseExporter(m_cache, writer, m_hvoRoot, m_vc, m_wsLineNumber);
				exporter.ExportDisplay();
				writer.WriteEndElement();
				writer.WriteEndDocument();
				writer.Close();
			}
			return true;
		}
	}
}
