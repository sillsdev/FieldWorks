// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Xsl;
using System.IO;
using System.Xml;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for InterlinearExportDialog.
	/// </summary>
	public class InterlinearExportDialog : ExportDialog
	{
		private List<XmlNode> m_ddNodes = new List<XmlNode>(8); // Saves XML nodes used to configure items.
		ICmObject m_objRoot;
		InterlinVc m_vc;
		FDO.IText m_text;
		List<ICmObject> m_objs = new List<ICmObject>();
		private event EventHandler OnLaunchFilterScrScriptureSectionsDialog;
		private IBookImporter m_bookImporter;

		public InterlinearExportDialog(ICmObject objRoot, InterlinVc vc, IBookImporter bookImporter)
		{
			m_objRoot = objRoot;
			m_vc = vc;
			m_bookImporter = bookImporter;
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

			m_helpTopic = "khtpExportInterlinear";
			columnHeader1.Text = ITextStrings.ksFormat;
			columnHeader2.Text = ITextStrings.ksExtension;
			Text = ITextStrings.ksExportInterlinear;
			OnLaunchFilterScrScriptureSectionsDialog += LaunchFilterTextsDialog;
		}

		#endregion

		protected override string ConfigurationFilePath
		{
			get { return String.Format("Language Explorer{0}Export Templates{0}Interlinear",
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
		protected override void ConfigureItem(XmlDocument document, ListViewItem item, XmlNode ddNode)
		{
			m_ddNodes.Add(ddNode);
			columnHeader1.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
		}

		/// <summary>
		/// If the selected text is from Scripture, pop up a selection dialog to allow the user to
		/// export more than one section (maybe even more than one book!) to a single file.
		/// </summary>
		/// <returns>true iff export can proceed to choosing an output file</returns>
		protected override bool PrepareForExport()
		{
			m_objs.Clear();
			//if (m_objRoot.OwningFlid != TextTags.kflidContents) // MDL LT-11483
			if (OnLaunchFilterScrScriptureSectionsDialog != null)
				OnLaunchFilterScrScriptureSectionsDialog(this, EventArgs.Empty);
			return m_objs.Count > 0;
		}

		/// <summary>
		/// Launch the appropriate dialog, depending on IsOkToDisplayScriptureIfPresent (currently always true).
		/// Note that this means even the SE edition of FW requires ScrControls.dll. This is the price of making
		/// even the SE edition able to work with Paratext, which we want to do because it was not obvious to
		/// users that they needed the BTE edition if using Paratext rather than TE.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Gendarme is just too dumb to understand the try...finally pattern to ensure disposal of dlg")]
		private void LaunchFilterTextsDialog(object sender, EventArgs args)
		{
			IFilterTextsDialog<IStText> dlg = null;
			try
			{
				var interestingTextsList = InterestingTextsDecorator.GetInterestingTextList(PropertyTable, m_cache.ServiceLocator);
				var textsToChooseFrom = new List<IStText>(interestingTextsList.InterestingTexts);
				var isOkToDisplayScripture = m_cache.ServiceLocator.GetInstance<IScrBookRepository>().AllInstances().Any();
				if (!isOkToDisplayScripture)
				{   // Mustn't show any Scripture, so remove scripture from the list
					textsToChooseFrom = textsToChooseFrom.Where(text => !ScriptureServices.ScriptureIsResponsibleFor(text)).ToList();
				}
				var interestingTexts = textsToChooseFrom.ToArray();
				if (isOkToDisplayScripture)
				{
					dlg = new FilterTextsDialogTE(m_cache, interestingTexts, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), m_bookImporter);
				}
				else
				{
					dlg = new FilterTextsDialog(m_cache, interestingTexts, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
				}
				// LT-12181: Was 'PruneToSelectedTexts(text) and most others were deleted.
				// We want 'PruneToInterestingTextsAndSelect(interestingTexts, selectedText)'
				dlg.PruneToInterestingTextsAndSelect(interestingTexts, (IStText)m_objRoot);
				// LT-12140 Dialog name shouldn't change from Choose Texts
				//dlg.Text = ITextStrings.ksExportInterlinearizedTexts;
				dlg.TreeViewLabel = ITextStrings.ksSelectSectionsExported;
				if (dlg.ShowDialog(this) == DialogResult.OK)
					m_objs.AddRange(dlg.GetListOfIncludedTexts());
			}
			finally
			{
				if (dlg != null)
					((IDisposable)dlg).Dispose();
			}
		}

		/// <summary>
		/// Export the data according to specifications.
		/// </summary>
		/// <param name="outPath"></param>
		protected override void DoExport(string outPath)
		{
			if (m_objs.Count == 0)
				m_objs.Add(m_objRoot);

			string fxtPath = (string)m_exportList.SelectedItems[0].Tag;
			XmlNode ddNode = m_ddNodes[NodeIndex(fxtPath)];
			string mode = XmlUtils.GetOptionalAttributeValue(ddNode, "mode", "xml");
			InterlinearExporter exporter;
			using (new WaitCursor(this))
			{
				try
				{
					ExportPhase1(mode, out exporter, outPath);
					string rootDir = FwDirectoryFinder.CodeDirectory;
					string transform = XmlUtils.GetOptionalAttributeValue(ddNode, "transform", "");
					string sTransformPath = Path.Combine(rootDir,
							String.Format("Language Explorer{0}Export Templates{0}Interlinear",
							Path.DirectorySeparatorChar));
					switch (mode)
					{
						default:
							// no further processing needed.
							break;
						case "doNothing":
						case "xml":
						case "elan":
							// no further processing needed.
							break;
						case "applySingleTransform":
							string sTransform = Path.Combine(sTransformPath, transform);
							exporter.PostProcess(sTransform, outPath, 1);
							break;
						case "openOffice":
							string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
							// Paranoia...probably GetRandomFileName will never make the name of an existing folder, but make sure of it.
							while (Directory.Exists(tempDir))
								tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
							Directory.CreateDirectory(tempDir);
							try
							{
								XslCompiledTransform xsl = new XslCompiledTransform();
								XmlNode implementation = XmlUtils.GetFirstNonCommentChild(ddNode);
								string styleFileTransform = "xml2OOStyles.xsl";
								if (implementation != null)
									styleFileTransform = XmlUtils.GetOptionalAttributeValue(implementation,
																							"styleTransform",
																							styleFileTransform);
								xsl.Load(Path.Combine(sTransformPath, styleFileTransform));
								xsl.Transform(outPath, Path.Combine(tempDir, "styles.xml"));

								// Now generate the content. Do this after using outPath as the source above, because it renames the file.
								string contentFileTransform = "xml2OO.xsl";
								if (implementation != null)
									contentFileTransform = XmlUtils.GetOptionalAttributeValue(implementation,
																							  "contentTransform",
																							  contentFileTransform);
#pragma warning disable 219
								XslCompiledTransform xsl2 = new XslCompiledTransform();
#pragma warning restore 219
								xsl.Load(Path.Combine(sTransformPath, contentFileTransform));
								xsl.Transform(outPath, Path.Combine(tempDir, "content.xml"));
								string mimetypePath = Path.Combine(tempDir, "mimetype");
								File.Copy(Path.Combine(sTransformPath, "mimetype"),
										  mimetypePath);
								File.SetAttributes(mimetypePath, File.GetAttributes(mimetypePath) & ~FileAttributes.ReadOnly);
								string manifestDir = Path.Combine(tempDir, "META-INF");
								string manifestPath = Path.Combine(manifestDir, "manifest.xml");
								Directory.CreateDirectory(manifestDir);
								File.Copy(Path.Combine(sTransformPath, "manifest.xml"),
										  manifestPath);
								File.SetAttributes(manifestPath, File.GetAttributes(manifestPath) & ~FileAttributes.ReadOnly);
								FastZip zf = new FastZip();
								zf.CreateZip(outPath, tempDir, true, string.Empty);
							}
							finally
							{
								Directory.Delete(tempDir, true);
							}
							break;
					}
				}
				catch (Exception e)
				{
					MessageBox.Show(this, String.Format(ITextStrings.ksExportErrorMsg, e.Message));
				}
			}
			this.Close();
		}

		protected int NodeIndex(string pathname)
		{
			string file = Path.GetFileName(pathname);
			for (int i = 0; i < m_ddNodes.Count; i++)
			{
				string fileN = m_ddNodes[i].BaseURI.Substring(m_ddNodes[i].BaseURI.LastIndexOf('/') + 1);
				if (fileN == file)
					return i;
			}
			return 0;
		}

		internal bool ExportPhase1(string mode, out InterlinearExporter exporter, string fileName)
		{
			CheckDisposed();
			exporter = null;
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Encoding = System.Text.Encoding.UTF8;
			settings.Indent = true;
			using (XmlWriter writer = XmlTextWriter.Create(fileName, settings))
			{
				exporter = InterlinearExporter.Create(mode, m_cache, writer, m_objs[0], m_vc.LineChoices, m_vc);
				exporter.WriteBeginDocument();
				exporter.ExportDisplay();
				for (int i = 1; i < m_objs.Count; ++i)
				{
					exporter.SetRootObject(m_objs[i]);
					exporter.ExportDisplay();
				}
				exporter.WriteEndDocument();
				writer.Close();
				return true;
			}
		}
	}
}
