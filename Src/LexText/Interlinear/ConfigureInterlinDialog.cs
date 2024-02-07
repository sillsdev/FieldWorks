// Copyright (c) 2015-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls; // for XmlViews stuff, especially borrowed form ColumnConfigureDialog
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using DesktopAnalytics;
using Gecko;
using Gecko.DOM;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using Directory = System.IO.Directory;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for ConfigureInterlinDialog.
	/// </summary>
	public partial class ConfigureInterlinDialog : Form
	{

		private const string s_helpTopic = "khtpConfigureInterlinearLines";
		private HelpProvider helpProvider;

		private Dictionary<ColumnConfigureDialog.WsComboContent, ComboBox.ObjectCollection> m_cachedComboContentForColumns;

		private LcmCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;

		private const string PersistProviderID = "ConfigureInterlinearLines";
		private PersistenceProvider m_persistProvider;

		InterlinLineChoices m_choices;
		private ComboBox wsCombo;
		private List<WsComboItem> m_columns;

		public ConfigureInterlinDialog(Mediator mediator, PropertyTable propertyTable, LcmCache cache, IHelpTopicProvider helpTopicProvider,
			InterlinLineChoices choices)
		{
			InitializeComponent();
			wsCombo = new ComboBox();
			AccessibleName = GetType().Name;

			m_helpTopicProvider = helpTopicProvider;
			helpProvider = new FlexHelpProvider();
			helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);

			m_persistProvider = new PersistenceProvider(mediator, propertyTable, PersistProviderID);
			m_persistProvider.RestoreWindowSettings(PersistProviderID, this);

			m_cachedComboContentForColumns = new Dictionary<ColumnConfigureDialog.WsComboContent, ComboBox.ObjectCollection>();
			m_columns = new List<WsComboItem>();

			m_cache = cache;
			m_choices = choices;

			InitColumnDictionary();
			InitColumns();

			if (!(mainBrowser.NativeBrowser is GeckoWebBrowser))
				return;

			var browser = (GeckoWebBrowser) mainBrowser.NativeBrowser;
			var htmlPath = SaveHtmlToTemp();
			mainBrowser.Url = new Uri(htmlPath);
			browser.DomContentChanged += Browser_DomContentChanged;
			Analytics.Track("ConfigureInterlinear", new Dictionary<string, string> {
			{
					"interlinearMode", Enum.GetName(typeof(InterlinLineChoices.InterlinMode), choices.Mode)
			}});
		}

		/// <summary>
		/// Saves the generated content in the temp directory, to a unique but discoverable and somewhat stable location.
		/// </summary>
		/// <returns>The path to the HTML file</returns>
		private string SaveHtmlToTemp()
		{
			var preferredPath = GetPreferredSavePath();
			var htmlPath = Path.ChangeExtension(preferredPath, "html");
			SavePublishedHtmlAndCss(htmlPath);
			return htmlPath;
		}

		/// <summary>
		/// Gets the preferred save path based on the project.
		/// </summary>
		/// <returns>The temp directory save path</returns>
		private string GetPreferredSavePath()
		{
			var basePath = Path.Combine(Path.GetTempPath(), "ConfigureInterlinear", m_cache.ProjectId.Name);
			FileUtils.EnsureDirectoryExists(basePath);

			return Path.Combine(basePath, "ConfigureInterlinear");
		}

		/// <summary>
		/// Writes and saves the content to the HTML file in the temporary directory.
		/// </summary>
		/// <param name="htmlPath">The save path to the HTML file that will be written</param>
		private void SavePublishedHtmlAndCss(string htmlPath)
		{
			if (htmlPath == null)
				throw new ArgumentNullException();

			using (var fileStream = new StreamWriter(htmlPath, false, Encoding.UTF8))
			{
				SavePublishedHtmlAndCss(fileStream);

			}
		}

		internal void SavePublishedHtmlAndCss(StreamWriter fileStream)
		{
			// Make the HTML write nicely
			var htmlWriterSettings = new XmlWriterSettings()
			{
				Indent = true,
				IndentChars = "    "
			};

			var cssPath = FwDirectoryFinder.TemplateDirectory + Path.DirectorySeparatorChar +
				"ConfigureInterlinear" + Path.DirectorySeparatorChar + "ConfigureInterlinear.css";
			using (var htmlWriter = XmlWriter.Create(fileStream, htmlWriterSettings))
			{
				GenerateOpeningHtml(htmlWriter, cssPath);
				GenerateHtmlTable(htmlWriter, m_choices.Mode);
				GenerateClosingHtml(htmlWriter);
				htmlWriter.Flush();
			}
		}

		/// <summary>
		/// Adds the basic HTML5 doctype, required JavaScript files, and starts the body.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="cssPath">The path of the CSS file</param>
		private void GenerateOpeningHtml(XmlWriter htmlWriter, string cssPath)
		{
			htmlWriter.WriteRaw("\n<!doctype html>\n");
			htmlWriter.WriteStartElement("html");
			htmlWriter.WriteStartElement("head");
			CreateLinkElementForStylesheet(htmlWriter, cssPath);
			htmlWriter.WriteFullEndElement(); // </head>
			htmlWriter.WriteStartElement("body");
			htmlWriter.WriteStartElement("div");

			//Warning for having dechecked all the checkboxes
			htmlWriter.WriteAttributeString("class", "center");
			htmlWriter.WriteStartElement("span");
			htmlWriter.WriteAttributeString("id", "warning");
			htmlWriter.WriteRaw("You must have at least one option selected before continuing.");
			htmlWriter.WriteFullEndElement(); // </span>
			htmlWriter.WriteEndElement(); // </div>


			htmlWriter.WriteStartElement("div");
			htmlWriter.WriteAttributeString("id", "container");
		}

		/// <summary>
		/// Creates an HTML5 link element for CSS stylesheets.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="cssPath">The path of the CSS file to include</param>
		private void CreateLinkElementForStylesheet(XmlWriter htmlWriter, string cssPath)
		{
			if (string.IsNullOrEmpty(cssPath))
				return;

			htmlWriter.WriteStartElement("link");
			htmlWriter.WriteAttributeString("rel", "stylesheet");
			htmlWriter.WriteAttributeString("type", "text/css");
			htmlWriter.WriteAttributeString("href", new System.Uri(cssPath).AbsoluteUri);
			htmlWriter.WriteEndElement(); // />
		}

		/// <summary>
		/// Creates an HTML5 script element with the given path of the script.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="scriptPath">The path of the JavaScript file</param>
		private void CreateScriptElement(XmlWriter htmlWriter, string scriptPath)
		{
			if (string.IsNullOrEmpty(scriptPath))
				return;

			htmlWriter.WriteStartElement("script");
			htmlWriter.WriteAttributeString("type", "text/javascript");
			htmlWriter.WriteAttributeString("src", new System.Uri(scriptPath).AbsoluteUri);
			htmlWriter.WriteFullEndElement(); // </script>
		}

		/// <summary>
		/// Creates the main table to contain the choices and checkboxes.
		/// If isTextChart is true, the table is written accordingly.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="mode">The InterlinMode of the choices</param>
		private void GenerateHtmlTable(XmlWriter htmlWriter, InterlinLineChoices.InterlinMode mode)
		{
			var rowChoicesList = InitRowChoices(m_choices);

			htmlWriter.WriteStartElement("div");
			var wsColumnWidths = string.Join(" ", m_columns.Select(c => "4em"));
			htmlWriter.WriteAttributeString("style", $"display: grid; grid-template-columns: 2em 2em 8em {wsColumnWidths};");
			htmlWriter.WriteAttributeString("id", "parent-grid");
			htmlWriter.WriteStartElement("div");
			htmlWriter.WriteAttributeString("class", "hamburger header");
			htmlWriter.WriteAttributeString("id", "header-hamburger-1");
			htmlWriter.WriteFullEndElement(); // </tr>
			htmlWriter.WriteStartElement("div");
			htmlWriter.WriteAttributeString("class", "hamburger header");
			htmlWriter.WriteAttributeString("id", "header-hamburger-2");
			htmlWriter.WriteFullEndElement(); // </tr>
			htmlWriter.WriteStartElement("div");
			htmlWriter.WriteAttributeString("class", "rowNames header");
			htmlWriter.WriteAttributeString("id", "header-row-names");
			htmlWriter.WriteFullEndElement(); // </tr>
			GenerateWsTableColumns(htmlWriter, "header");

			foreach (var rowChoice in rowChoicesList)
			{
				rowChoice.GenerateRow(htmlWriter, m_columns, m_cache, m_choices);
			}

			htmlWriter.WriteFullEndElement(); // </div>
		}

		/// <summary>
		/// This class encapsulates the data needed to render one or more rows in the Configure Interlinear Dialog
		/// </summary>
		internal sealed class InterlinearTableGroup
		{

			private InterlinearTableRow FirstRow;

			/// <summary>
			/// Morpheme level and segment level rows are grouped into a section that stick together
			/// </summary>
			private List<InterlinearTableRow> RemainingRows = new List<InterlinearTableRow>();

			public InterlinearTableGroup(InterlinearTableRow row)
			{
				FirstRow = row;
			}

			public bool IsSection(InterlinLineChoices choices) => IsMorphemeRow(FirstRow, choices) || IsSegmentRow(FirstRow, choices);

			/// <summary>
			/// A row is a segment row if the InterlinLineSpec shows that it is not a word level line
			/// </summary>
			private bool IsSegmentRow(InterlinearTableRow row, InterlinLineChoices choices)
			{
				if (row.HasSpecs)
				{
					return !row.FirstSpec.WordLevel;
				}
				return !choices.CreateSpec(row.Flid, 0).WordLevel;
			}

			/// <summary>
			/// A line results in a morpheme row if the specification indicates it
			/// </summary>
			private bool IsMorphemeRow(InterlinearTableRow row, InterlinLineChoices choices)
			{
				if (row.HasSpecs)
				{
					return row.FirstSpec.MorphemeLevel;
				}
				return choices.CreateSpec(row.Flid, 0).MorphemeLevel;
			}

			public void FillSection(Queue<InterlinearTableRow> remainingChoiceRows, InterlinLineChoices choices)
			{
				if (IsMorphemeRow(FirstRow, choices))
				{
					while (remainingChoiceRows.Any() &&
						IsMorphemeRow(remainingChoiceRows.Peek(), choices))
					{
						RemainingRows.Add(remainingChoiceRows.Dequeue());
					}
				}
				else if (IsSegmentRow(FirstRow, choices))
				{
					while (remainingChoiceRows.Any() &&
						IsSegmentRow(remainingChoiceRows.Peek(), choices))
					{
						RemainingRows.Add(remainingChoiceRows.Dequeue());
					}
				}
			}

			public void GenerateRow(XmlWriter htmlWriter,
				List<WsComboItem> columns,
				LcmCache cache,
				InterlinLineChoices choices)
			{
				htmlWriter.WriteStartElement("div");
				var wsColumnWidths = string.Join(" ", columns.Select(c => "4em"));
				htmlWriter.WriteAttributeString("style", $"grid-column-start: 1; grid-column-end: span {columns.Count + 3}; display: grid; grid-template-columns: 2em 2em 8em {wsColumnWidths};");
				htmlWriter.WriteAttributeString("class", IsMorphemeRow(FirstRow, choices)
					? "morpheme-container"
					: IsSegmentRow(FirstRow, choices) ? "segment-container" : "word-container");
				htmlWriter.WriteAttributeString("id", $"row-grid-{FirstRow.Flid}");
				htmlWriter.WriteStartElement("span");
				if (!IsSegmentRow(FirstRow, choices))
				{
					htmlWriter.WriteAttributeString("class", "row-handle");
					htmlWriter.WriteAttributeString("id", $"row-grab-{FirstRow.Flid}");
					htmlWriter.WriteRaw("&#8801;");
				}
				htmlWriter.WriteFullEndElement(); // </span>
				if (IsSection(choices))
				{
					var allRows = new List<InterlinearTableRow>();
					allRows.Add(FirstRow);
					allRows.AddRange(RemainingRows);
					foreach (var otherRow in allRows)
					{
						htmlWriter.WriteStartElement("div");
						htmlWriter.WriteAttributeString("class", IsMorphemeRow(FirstRow, choices) ? "morpheme-row" : "segment-row");
						htmlWriter.WriteAttributeString("style", $"grid-column-start: 2; grid-column-end: span {columns.Count + 2}; display: grid; grid-template-columns: 2em 8em {wsColumnWidths};");
						htmlWriter.WriteAttributeString("id", $"internal-row-grid-{otherRow.Flid}");
						htmlWriter.WriteStartElement("span");
						htmlWriter.WriteAttributeString("class", "internal-row-handle");
						htmlWriter.WriteAttributeString("id", $"internal-row-grab-{FirstRow.Flid}");
						htmlWriter.WriteRaw("&#8801;");
						htmlWriter.WriteFullEndElement();
						GenerateRowCells(htmlWriter, otherRow, columns, cache, choices);
						htmlWriter.WriteFullEndElement();
					}
				}
				else
				{
					htmlWriter.WriteStartElement("div");
					htmlWriter.WriteFullEndElement(); // </div>
					GenerateRowCells(htmlWriter, FirstRow, columns, cache, choices);
				}
				htmlWriter.WriteFullEndElement(); // </div>
			}

			/// <summary>
			/// Generates a Table Row which can be reordered to anywhere else in the table
			/// </summary>
			private void GenerateRowCells(XmlWriter htmlWriter, InterlinearTableRow rowData, List<WsComboItem> columns, LcmCache cache, InterlinLineChoices choices)
			{
				htmlWriter.WriteStartElement("div");
				htmlWriter.WriteAttributeString("class", "line-choice");
				htmlWriter.WriteAttributeString("id", $"{rowData.Flid}");
				htmlWriter.WriteRaw(rowData.Label);
				htmlWriter.WriteFullEndElement(); // row label div
				GenerateCheckboxes(htmlWriter, rowData, columns, cache, choices);
			}


			/// <summary>
			/// Determines if the row's ComboContent is contained in one of the cached keys of comboContentCache.
			/// If it is, then loop through each column to determine if a checkbox is needed. Otherwise, write the td with no checkbox.
			/// </summary>
			private void GenerateCheckboxes(XmlWriter htmlWriter,
				InterlinearTableRow row,
				List<WsComboItem> columns,
				LcmCache cache,
				InterlinLineChoices choices)
			{
					foreach (var column in columns)
					{
						var interlinSpec = row.HasSpecs
							? row.FirstSpec
							: choices.CreateSpec(row.Flid, 0);
						var wsItems = new List<WsComboItem>();
						ColumnConfigureDialog.AddWritingSystemsToCombo(cache, wsItems, interlinSpec.ComboContent);

						if (RowNeedsCheckbox(cache, interlinSpec, wsItems, column))
						{
							var id = row.Flid + "%" + column.WritingSystem;
							GenerateCheckbox(htmlWriter, choices, id, column.WritingSystemType);
						}
						else
						{
							GenerateCheckbox(htmlWriter, choices, "", column.WritingSystemType, true);
						}
					}
			}

			private static bool RowNeedsCheckbox(LcmCache lcmCache, InterlinLineSpec interlinSpec,
				List<WsComboItem> wsItems, WsComboItem column)
			{
				if (lcmCache.MetaDataCacheAccessor is IFwMetaDataCacheManaged mdc)
				{
					if (mdc.FieldExists(interlinSpec.Flid) && mdc.IsCustom(interlinSpec.Flid))
					{
						string workingWs = column.WritingSystemType;
						if (workingWs == "both")
						{
							int customWS = mdc.GetFieldWs(interlinSpec.Flid);
							if (customWS == WritingSystemServices.kwsVern)
								workingWs = "vernacular";
							else
								workingWs = "analysis";
						}

						return
							((workingWs == "analysis" && column.Id == lcmCache.LangProject.DefaultAnalysisWritingSystem.Id) ||
							(workingWs == "vernacular" && column.Id == lcmCache.LangProject.DefaultVernacularWritingSystem.Id)) &&
							wsItems.Exists(wsItem => wsItem.Id == column.Id);
					}
					return wsItems.Exists(wsItem => wsItem.Id == column.Id);
				}

				throw new ApplicationException("A metadata cache is expected to exist here to check for custom fields.");
			}

			/// <summary>
			/// Creates a new div with a checkbox if isEmptyCell is false. If isEmptyCell is true, then it creates
			/// a div with no checkbox. If a className is given, then it adds the class to the div to show
			/// a separation/distinction between the writing systems. id is typically the flid and ws
			/// together. If blank, no id is written.
			/// </summary>
			/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
			/// <param name="choices"></param>
			/// <param name="id">The id for the checkbox to be written</param>
			/// <param name="className">The name of the class to attach to the div</param>
			/// <param name="isEmptyCell">If true, a div is written with no checkbox</param>
			private static void GenerateCheckbox(XmlWriter htmlWriter, InterlinLineChoices choices, string id, string className, bool isEmptyCell = false)
			{
				if (htmlWriter == null)
					return;
				htmlWriter.WriteStartElement("div");
				htmlWriter.WriteAttributeString("class", $"grid-cell {className}");
				htmlWriter.WriteAttributeString("id", $"div{id}");

				if (!isEmptyCell)
				{
					var flid = int.Parse(id.Split('%')[0]);
					var ws = int.Parse(id.Split('%')[1]);

					htmlWriter.WriteStartElement("input");
					htmlWriter.WriteAttributeString("type", "checkbox");
					htmlWriter.WriteAttributeString("id", id);
					htmlWriter.WriteAttributeString("class", "checkBox");
					htmlWriter.WriteAttributeString("name", id.Split('%')[0] + "[]");
					if (choices.IndexInEnabled(flid, ws, true) != -1) // If the option is enabled in choices, check it
						htmlWriter.WriteAttributeString("checked", "checked");
					htmlWriter.WriteEndElement(); // /> <--- End input element
				}
				htmlWriter.WriteFullEndElement(); // </td>
			}
		}

		/// <summary>
		/// Gets the Initial Row Choices for the HTML to generate
		/// </summary>
		internal static IEnumerable<InterlinearTableGroup> InitRowChoices(InterlinLineChoices choices)
		{
			var rowChoices = new List<InterlinearTableGroup>();
			var remainingChoiceRows = new Queue<InterlinearTableRow>();
			var lineOptions = choices.ConfigurationLineOptions;
			foreach (var lineOption in lineOptions)
			{
				remainingChoiceRows.Enqueue(new InterlinearTableRow(new Tuple<LineOption, InterlinLineSpec[]>(lineOption,
					choices.EnabledLineSpecs.Where(spec => spec.Flid == lineOption.Flid).ToArray())));
			}
			do
			{
				var row = new InterlinearTableGroup(remainingChoiceRows.Dequeue());
				if (row.IsSection(choices))
				{
					row.FillSection(remainingChoiceRows, choices);
				}
				rowChoices.Add(row);
			} while (remainingChoiceRows.Any());

			return rowChoices;
		}

		/// <summary>
		/// Creates the writing system columns for the table
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		private void GenerateWsTableColumns(XmlWriter htmlWriter, string extraClasses = "")
		{
			foreach (var column in m_columns)
			{
				htmlWriter.WriteStartElement("div");
				htmlWriter.WriteAttributeString("class", $"grid-cell {extraClasses}");
				htmlWriter.WriteRaw(column.Abbreviation);
				htmlWriter.WriteFullEndElement();
			}
		}

		/// <summary>
		/// Includes the last JavaScript file needed for the jQuery UI sortable and then closes the last HTML elements.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		private void GenerateClosingHtml(XmlWriter htmlWriter)
		{
			var javascriptFilePaths = GetFilePaths();
			// If we're working with a text chart, we need to enforce the UI constraints for checkboxes.
			if (m_choices.Mode == InterlinLineChoices.InterlinMode.Chart)
				CreateScriptElement(htmlWriter, GetPathFromFile(javascriptFilePaths, "scriptForChart.js")); // scriptForChart.js
			CreateScriptElement(htmlWriter, GetPathFromFile(javascriptFilePaths, "dragula.min.js"));
			CreateScriptElement(htmlWriter, GetPathFromFile(javascriptFilePaths, "configureInterlinearLines.js"));
			htmlWriter.WriteFullEndElement(); // </body>
			htmlWriter.WriteFullEndElement(); // </html>
		}

		/// <summary>
		/// Navigates to the path where all of the required JavaScript files are stored and then returns the paths in a string array.
		/// </summary>
		/// <returns>A string array with the paths of each JavaScript file needed</returns>
		private string[] GetFilePaths()
		{
			var pathToFiles = FwDirectoryFinder.TemplateDirectory + Path.DirectorySeparatorChar + "ConfigureInterlinear" + Path.DirectorySeparatorChar;
			return Directory.GetFiles(pathToFiles);
		}

		private static string GetPathFromFile(string[] filePaths, string fileName)
		{
			for (int i = 0; i < filePaths.Length; i++)
			{
				if (fileName.Equals(Path.GetFileName(filePaths[i])))
				{
					return filePaths[i];
				}
			}
			return String.Empty;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		public InterlinLineChoices Choices
		{
			get
			{
				CheckDisposed();

				return m_choices;
			}
		}

		public void InitColumnDictionary()
		{
			// Cache the baseline and best analysis no matter what we see in the specs
			WsComboItems(ColumnConfigureDialog.WsComboContent.kwccVernacularInParagraph);
			WsComboItems(ColumnConfigureDialog.WsComboContent.kwccBestAnalysis);
			foreach (var spec in m_choices.EnabledLineSpecs)
			{
				WsComboItems(spec.ComboContent);
			}
		}

		private void InitColumns()
		{
			m_columns.Clear();

			if (m_choices.Mode != InterlinLineChoices.InterlinMode.Chart)
			{
				var vernacularInParaWss =
					m_cachedComboContentForColumns[
						ColumnConfigureDialog.WsComboContent.kwccVernacularInParagraph];
				var bestAnalysisWss =
					m_cachedComboContentForColumns[
						ColumnConfigureDialog.WsComboContent.kwccBestAnalysis];
				var vernacularAndAnalysisWss = new List<WsComboItem>();

				// We need Baseline and the remaining vernacular writing systems
				foreach (WsComboItem ws in vernacularInParaWss)
				{
					ws.WritingSystem = GetWsFromId(ws.Id);
					ws.WritingSystemType = "vernacular";
					if (bestAnalysisWss.Contains(ws))
					{
						ws.WritingSystemType = "both";
						vernacularAndAnalysisWss.Add(ws);
						continue;
					}
					m_columns.Add(ws);
				}

				// Ensure that the ones that are in both display in the middle
				foreach (var ws in vernacularAndAnalysisWss)
					m_columns.Add(ws);

				// Next, we need the analysis writing systems
				foreach (WsComboItem ws in bestAnalysisWss)
				{
					// If the writing system was already added, there's no need to add it again.
					// In the case that it already contains the writing system, that means it is both
					// a vernacular and analysis writing system. We only need one column for it.
					// It will display checkboxes for every option.
					if (vernacularInParaWss.Contains(ws))
						continue;
					ws.WritingSystem = GetWsFromId(ws.Id);
					ws.WritingSystemType = "analysis";
					m_columns.Add(ws);
				}
			}
			else
			{
				// Otherwise, we will use the vernacular writing systems followed by the analysis ones
				var vernacularWss = m_cache.LangProject.CurrentVernacularWritingSystems;
				var analysisWss = m_cache.LangProject.CurrentAnalysisWritingSystems;
				var vernacularAndAnalysisWss = new List<WsComboItem>();

				foreach (var ws in vernacularWss)
				{
					var item =
						new WsComboItem(ws.DisplayLabel, ws.Id) { WritingSystem = ws.Handle, WritingSystemType = "vernacular" };
					if (analysisWss.Contains(ws))
					{
						item.WritingSystemType = "both";
						vernacularAndAnalysisWss.Add(item);
						continue;
					}
					m_columns.Add(item);
				}

				foreach (var ws in vernacularAndAnalysisWss)
					m_columns.Add(ws);

				foreach (var ws in analysisWss)
				{
					if (vernacularWss.Contains(ws))
						continue;

					var item = new WsComboItem(ws.DisplayLabel, ws.Id)
					{
						WritingSystem = ws.Handle,
						WritingSystemType = "analysis"
					};
					m_columns.Add(item);
				}
			}
		}

		/// <summary>
		/// This is used to create an object collection with the appropriate writing system choices to be used in wsCombo. The reason it is cached is because
		/// list generation will require looping through each kind of combo box several times.
		/// </summary>
		/// <param name="comboContent"></param>
		/// <returns></returns>
		private ComboBox.ObjectCollection WsComboItems(ColumnConfigureDialog.WsComboContent comboContent)
		{
			return WsComboItemsInternal(m_cache, wsCombo, m_cachedComboContentForColumns, comboContent);
		}

		/// <summary>
		/// This is used to create an object collection with the appropriate writing system choices to be used in wsCombo.  The reason it is cached is because
		/// list generation will require looping through each kind of combo box several times.
		///
		/// This version is visible to InterlinDocRootSiteBase for its context menu.
		/// </summary>
		/// <param name="cachedBoxes"></param>
		/// <param name="comboContent"></param>
		/// <param name="cache"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		internal static ComboBox.ObjectCollection WsComboItemsInternal(LcmCache cache, ComboBox owner,
			Dictionary<ColumnConfigureDialog.WsComboContent, ComboBox.ObjectCollection> cachedBoxes,
			ColumnConfigureDialog.WsComboContent comboContent)
		{
			ComboBox.ObjectCollection objectCollection;
			if (!cachedBoxes.ContainsKey(comboContent))
			{
				objectCollection = new ComboBox.ObjectCollection(owner);

				// The final argument here restricts writing systems that will be added to the combo box to
				// only be "real" writing systems.  So, English will be added, but not "Default Analysis".
				// This functionality should eventually go away.  See LT-4740.
				// JohnT: it now partially has, two lines support 'best analysis'.
				ColumnConfigureDialog.AddWritingSystemsToCombo(cache, objectCollection, comboContent,
					comboContent != ColumnConfigureDialog.WsComboContent.kwccBestAnalysis);
				cachedBoxes[comboContent] = objectCollection;
			}
			else
			{
				objectCollection = cachedBoxes[comboContent];
			}

			return objectCollection;
		}

		private int GetWsFromId(string id)
		{
			// special case, the only few we support so far (and only for a few fields).
			if (id == "best analysis")
				return WritingSystemServices.kwsFirstAnal;//LangProject.kwsFirstAnal;
			else if (id == "vern in para")
				return WritingSystemServices.kwsVernInParagraph;
			Debug.Assert(!XmlViewsUtils.GetWsRequiresObject(id), "Writing system is magic.  These should never be used in the Interlinear area.");

			int ws = -50;
			try
			{
				if (!XmlViewsUtils.GetWsRequiresObject(id))
				{
					// Try to convert the ws parameter into an int.  Sometimes the parameter
					// cannot be interpreted without an object, such as when the ws is a magic
					// string that will change the actual ws depending on the contents of the
					// object.  In these cases, we give -50 as a known constant to check for.
					// This can possibly throw an exception, so we'll enclose it in a try block.
					ws = WritingSystemServices.InterpretWsLabel(m_cache, id, null, 0, 0, null);
				}
			}
			catch
			{
				Debug.Assert(ws != -50, "InterpretWsLabel was not able to interpret the Ws Label.  The most likely cause for this is that a magic ws was passed in.");
			}
			return ws;
		}

		#region Event Handlers

		/// <summary>
		/// When the number of checked checkboxes is not greater than 0, the OK button is disabled
		/// because at least one option is required.
		/// </summary>
		private void Browser_DomContentChanged(object sender, EventArgs e)
		{
			using (var context = new AutoJSContext(((GeckoWebBrowser)mainBrowser.NativeBrowser).Window))
			{
				context.EvaluateScript("anyCheckboxSelected()", out var anyChecked);
				okButton.Enabled = Convert.ToBoolean(anyChecked);
			}
		}

		private void HelpButton_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		/// <summary>
		/// When the okButton is clicked, this gets the JavaScript order of the rows to reorder the line choices. Additionally,
		/// this gets the checkboxes on the page to enable/disable them in the actual m_choices.
		/// </summary>
		private void OkButton_Click(object sender, EventArgs e)
		{
			m_persistProvider.PersistWindowSettings(PersistProviderID, this);

			var checkBoxes =
				(mainBrowser.NativeBrowser as GeckoWebBrowser)?.Document.GetElementsByClassName("checkBox");

			if (checkBoxes == null)
				return;

			List<int> orderedFlids;
			using (var context = new AutoJSContext(((GeckoWebBrowser)mainBrowser.NativeBrowser).Window.DomWindow))
			{
				string rows;
				context.EvaluateScript(@"getRows()", out rows);
				orderedFlids = Array.ConvertAll(rows.Split(','), int.Parse).ToList();
			}

			// Get all of the specs with the new enabled (checked) value.
			// (The Flid order is correct but the order of the writing systems are not.)
			List<InterlinLineSpec> newLineSpecsUnordered = new List<InterlinLineSpec>();
			foreach (var checkBox in checkBoxes)
			{
				var element = (GeckoInputElement)checkBox;
				var elementId = element.GetAttribute("id");
				var flidAndWs = elementId.Split('%');
				var flid = int.Parse(flidAndWs[0]);
				var ws = int.Parse(flidAndWs[1]);
				newLineSpecsUnordered.Add(m_choices.CreateSpec(flid, ws, element.Checked));
			}

			OrderAllSpecs(m_choices, orderedFlids, newLineSpecsUnordered);
		}

		internal static void OrderAllSpecs(InterlinLineChoices choices, List<int> orderedFlids, List<InterlinLineSpec> newLineSpecsUnordered)
		{
			// Preserve the existing order of the writing system for each Flid.
			// (This list might not contain all of the specs.)
			ReadOnlyCollection<InterlinLineSpec> oldLineSpecsOrder = choices.AllLineSpecs;
			choices.ReinitializeEmptyAllLineSpecs();

			// Update m_choices with the new Flid (row) order and new enabled (checked) values while
			// preserving the old writing system order for each Flid.
			foreach (int flid in orderedFlids)
			{
				InterlinLineSpec newSpec = null;
				// Preserve the order of the writing systems from the old list.
				foreach (InterlinLineSpec oldSpec in oldLineSpecsOrder)
				{
					if (oldSpec.Flid == flid)
					{
						newSpec = newLineSpecsUnordered.Find(spec => ((spec.Flid == flid) && (spec.WritingSystem == oldSpec.WritingSystem)));
						if (newSpec != null)
						{
							choices.Append(newSpec);
							newLineSpecsUnordered.Remove(newSpec);
						}
					}
				}

				// Add any remaing specs for this flid in the default order.
				do
				{
					newSpec = newLineSpecsUnordered.Find(spec => spec.Flid == flid);
					if (newSpec != null)
					{
						choices.Append(newSpec);
						newLineSpecsUnordered.Remove(newSpec);
					}

				} while (newSpec != null);
			}

			Debug.Assert(newLineSpecsUnordered.Count == 0);
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			m_persistProvider.PersistWindowSettings(PersistProviderID, this);
		}
		#endregion

	}

	/// <summary>
	/// A row on a table will represent a single LineOption and the status of its available writing systems.
	/// Each writing system represents a separate InterlinLineSpec
	/// </summary>
	internal sealed class InterlinearTableRow
	{
		private Tuple<LineOption, InterlinLineSpec[]> _lineInfo;

		public InterlinearTableRow(Tuple<LineOption, InterlinLineSpec[]> lineInfo)
		{
			_lineInfo = lineInfo;
		}

		public int Flid => _lineInfo.Item1.Flid;
		public string Label => _lineInfo.Item1.Label;
		public ColumnConfigureDialog.WsComboContent ComboContent { get; set; }
		public bool HasSpecs => _lineInfo.Item2.Length > 0;
		public InterlinLineSpec FirstSpec => _lineInfo.Item2?[0];
	}
}
