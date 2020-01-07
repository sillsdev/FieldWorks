// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Gecko;
using Gecko.DOM;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.Windows.Forms.HtmlBrowser;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	internal class ConfigureInterlinDialog : Form
	{
		private Label label1;
		private Button helpButton;
		private Button cancelButton;
		private Button okButton;
		private const string s_helpTopic = "khtpConfigureInterlinearLines";
		private HelpProvider helpProvider;
		private Dictionary<WsComboContent, ComboBox.ObjectCollection> m_cachedComboContentForColumns;
		private IContainer components;
		private bool m_fUpdatingWsCombo = false; // true during UpdateWsCombo
		private LcmCache m_cache;
		private IHelpTopicProvider m_helpTopicProvider;
		private ComboBox wsCombo;
		private XWebBrowser mainBrowser;
		private FlowLayoutPanel mainLayoutPanel;
		private FlowLayoutPanel buttonLayoutPanel;
		private List<WsComboItem> m_columns;

		public ConfigureInterlinDialog(LcmCache cache, IHelpTopicProvider helpTopicProvider, InterlinLineChoices choices)
		{
			InitializeComponent();
			wsCombo = new ComboBox();
			AccessibleName = GetType().Name;

			m_helpTopicProvider = helpTopicProvider;
			helpProvider = new HelpProvider
			{
				HelpNamespace = m_helpTopicProvider.HelpFile
			};
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);

			m_cachedComboContentForColumns = new Dictionary<WsComboContent, ComboBox.ObjectCollection>();
			m_columns = new List<WsComboItem>();

			m_cache = cache;
			Choices = choices;

			InitColumnDictionary();
			InitColumns();

			if (!(mainBrowser.NativeBrowser is GeckoWebBrowser))
			{
				return;
			}
			var browser = (GeckoWebBrowser)mainBrowser.NativeBrowser;
			var htmlPath = SaveHtmlToTemp();
			mainBrowser.Url = new Uri(htmlPath);
			browser.DomContentChanged += Browser_DomContentChanged;
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigureInterlinDialog));
			this.label1 = new System.Windows.Forms.Label();
			this.helpButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.mainBrowser = new SIL.Windows.Forms.HtmlBrowser.XWebBrowser();
			this.mainLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.buttonLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.mainLayoutPanel.SuspendLayout();
			this.buttonLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// helpButton
			//
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.Click += new System.EventHandler(this.HelpButton_Click);
			//
			// cancelButton
			//
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Name = "cancelButton";
			//
			// okButton
			//
			resources.ApplyResources(this.okButton, "okButton");
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Name = "okButton";
			this.okButton.Click += OkButton_Click;
			//
			// mainBrowser
			//
			resources.ApplyResources(this.mainBrowser, "mainBrowser");
			this.mainBrowser.IsWebBrowserContextMenuEnabled = false;
			this.mainBrowser.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			this.mainBrowser.Margin = new Padding(10, 10, 10, 10);
			this.mainBrowser.Height = 300;
			this.mainBrowser.Name = "mainBrowser";
			//
			// mainLayoutPanel
			//
			this.mainLayoutPanel.Controls.Add(this.label1);
			this.mainLayoutPanel.Controls.Add(this.mainBrowser);
			this.mainLayoutPanel.Controls.Add(this.buttonLayoutPanel);
			resources.ApplyResources(this.mainLayoutPanel, "mainLayoutPanel");
			this.mainLayoutPanel.Name = "mainLayoutPanel";
			//
			// buttonLayoutPanel
			//
			this.buttonLayoutPanel.Controls.Add(this.helpButton);
			this.buttonLayoutPanel.Controls.Add(this.cancelButton);
			this.buttonLayoutPanel.Controls.Add(this.okButton);
			resources.ApplyResources(this.buttonLayoutPanel, "buttonLayoutPanel");
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
			//
			// ConfigureInterlinDialog
			//
			this.AcceptButton = this.okButton;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.mainLayoutPanel);
			this.Name = "ConfigureInterlinDialog";
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.mainLayoutPanel.ResumeLayout(false);
			this.buttonLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		public InterlinLineChoices Choices { get; }

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
			{
				throw new ArgumentNullException();
			}
			// Make the HTML write nicely
			var htmlWriterSettings = new XmlWriterSettings()
			{
				Indent = true,
				IndentChars = "    "
			};
			var cssPath = Path.Combine(FwDirectoryFinder.TemplateDirectory, "ConfigureInterlinear", "ConfigureInterlinear.css");
			using (var htmlWriter = XmlWriter.Create(htmlPath, htmlWriterSettings))
			{
				GenerateOpeningHtml(htmlWriter, cssPath);
				GenerateHtmlTable(htmlWriter, Choices.Mode);
				GenerateClosingHtml(htmlWriter);
				htmlWriter.Flush();
			}
		}

		/// <summary>
		/// Adds the basic HTML5 doctype, required JavaScript files, and starts the body.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="cssPath">The path of the CSS file</param>
		private static void GenerateOpeningHtml(XmlWriter htmlWriter, string cssPath)
		{
			var javascriptFilePaths = GetFilePaths();
			htmlWriter.WriteRaw("\n<!doctype html>\n");
			htmlWriter.WriteStartElement("html");
			htmlWriter.WriteStartElement("head");
			CreateLinkElementForStylesheet(htmlWriter, cssPath);
			CreateScriptElement(htmlWriter, GetPathFromFile(javascriptFilePaths, "jquery.js")); // jQuery
			CreateScriptElement(htmlWriter, GetPathFromFile(javascriptFilePaths, "jqueryui.js")); // jQuery UI
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
		private static void CreateLinkElementForStylesheet(XmlWriter htmlWriter, string cssPath)
		{
			if (string.IsNullOrEmpty(cssPath))
			{
				return;
			}
			htmlWriter.WriteStartElement("link");
			htmlWriter.WriteAttributeString("rel", "stylesheet");
			htmlWriter.WriteAttributeString("type", "text/css");
			htmlWriter.WriteAttributeString("href", "file:\\" + cssPath);
			htmlWriter.WriteEndElement(); // />
		}

		/// <summary>
		/// Creates an HTML5 script element with the given path of the script.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="scriptPath">The path of the JavaScript file</param>
		private static void CreateScriptElement(XmlWriter htmlWriter, string scriptPath)
		{
			if (string.IsNullOrEmpty(scriptPath))
			{
				return;
			}
			htmlWriter.WriteStartElement("script");
			htmlWriter.WriteAttributeString("type", "text/javascript");
			htmlWriter.WriteAttributeString("src", "file:\\" + scriptPath);
			htmlWriter.WriteFullEndElement(); // </script>
		}

		/// <summary>
		/// Creates the main table to contain the choices and checkboxes.
		/// If isTextChart is true, the table is written accordingly.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="mode">The InterlinMode of the choices</param>
		private void GenerateHtmlTable(XmlWriter htmlWriter, InterlinMode mode)
		{
			var rowChoicesList = InitRowChoices();
			htmlWriter.WriteStartElement("table");
			htmlWriter.WriteStartElement("thead");
			// The first th is for the outer .grab elements
			// The second th is for the inner .grab elements
			// The third th is a blank one for the row lables (Word, Word Gloss, etc.)
			htmlWriter.WriteStartElement("tr");
			htmlWriter.WriteStartElement("th");
			htmlWriter.WriteFullEndElement(); // </th>
			htmlWriter.WriteStartElement("th");
			htmlWriter.WriteFullEndElement(); // </th>
			htmlWriter.WriteStartElement("th");
			htmlWriter.WriteAttributeString("class", "mainTh");
			htmlWriter.WriteFullEndElement(); // </th>
			GenerateWsTableColumns(htmlWriter, "mainTh");
			htmlWriter.WriteFullEndElement(); // </tr>
			htmlWriter.WriteFullEndElement(); // </thead>
			htmlWriter.WriteStartElement("tbody");
			if (mode != InterlinMode.Chart)
			{
				// The UI needs to enforce the way morpheme-level options are ordered, so morpheme-level options are linked
				// together as one unit. The Literal Translation, Free Translation, and Note options are in their own table
				// because they must remain on the bottom of the options list.
				foreach (var currentRow in rowChoicesList)
				{
					if (currentRow.Count > 1 && (currentRow.First.Value.Item2.MorphemeLevel && currentRow.First.Value.Item2.WordLevel)) // Morpheme-level option
					{
						GenerateTableRowWithClusterTable(htmlWriter, currentRow, "clusterTable");
					}
					else if (currentRow.Count == 1)
					{
						GenerateNormalTableRow(htmlWriter, currentRow.First.Value, true);
					}
				}
				// </tbody>
				htmlWriter.WriteFullEndElement();
				// </table>
				htmlWriter.WriteFullEndElement();
				// The Literal Translation, Free Translation, and Note options are grouped in a separate table.
				GenerateNonWordLevelLineOptions(htmlWriter, "specialTable", "clusterTable", rowChoicesList.Last.Value, 3);
			}
			else
			{
				foreach (var currentRow in rowChoicesList)
				{
					if (currentRow.First.Value.Item2.MorphemeLevel) // The morpheme-level options are in a separate table
					{
						GenerateMorphemeLevelTable(htmlWriter, currentRow);
					}
					else
					{
						GenerateTableRowWithClusterTable(htmlWriter, currentRow, "clusterTable hasDashedLine");
					}
				}
				htmlWriter.WriteFullEndElement(); // </tbody>
				htmlWriter.WriteFullEndElement(); // </table>
			}
			htmlWriter.WriteFullEndElement(); // </div>
		}

		/// <summary>
		/// Generates sub-table with Morpheme Level Line Choices
		/// </summary>
		private void GenerateMorphemeLevelTable(XmlWriter htmlWriter, LinkedList<Tuple<LineOption, InterlinLineSpec>> tableRowData)
		{
			htmlWriter.WriteStartElement("tr");
			htmlWriter.WriteStartElement("td");
			htmlWriter.WriteAttributeString("class", "grab specialGrab");
			htmlWriter.WriteRaw("&#8801;");
			htmlWriter.WriteFullEndElement(); // </td>
			htmlWriter.WriteStartElement("td");
			htmlWriter.WriteAttributeString("class", "specialTD");
			htmlWriter.WriteAttributeString("colspan", (m_columns.Count + 2).ToString());
			htmlWriter.WriteStartElement("table");
			htmlWriter.WriteAttributeString("class", "clusterTable");
			htmlWriter.WriteStartElement("thead");
			htmlWriter.WriteStartElement("tr");
			htmlWriter.WriteStartElement("th");
			htmlWriter.WriteFullEndElement(); // </th>
			htmlWriter.WriteStartElement("th");
			htmlWriter.WriteAttributeString("class", "mainTh");
			htmlWriter.WriteFullEndElement(); // </th>
			GenerateWsTableColumns(htmlWriter, "mainTh");
			htmlWriter.WriteFullEndElement(); // </tr>
			htmlWriter.WriteFullEndElement(); // </thead>
			htmlWriter.WriteStartElement("tbody");
			var morphemesOrLexGlossWasDrawn = false;
			foreach (var row in tableRowData)
			{
				if ((row.Item1.Flid == InterlinLineChoices.kflidMorphemes || row.Item1.Flid == InterlinLineChoices.kflidLexGloss) && !morphemesOrLexGlossWasDrawn)
				{
					htmlWriter.WriteStartElement("tr");
					htmlWriter.WriteStartElement("td");
					htmlWriter.WriteAttributeString("class", "specialTD");
					htmlWriter.WriteAttributeString("colspan", (m_columns.Count + 2).ToString());
					htmlWriter.WriteStartElement("table");
					htmlWriter.WriteAttributeString("class", "clusterTable hasDashedLine");
					htmlWriter.WriteAttributeString("id", "specialClusterTable");
					htmlWriter.WriteStartElement("thead");
					htmlWriter.WriteStartElement("tr");
					htmlWriter.WriteStartElement("th");
					htmlWriter.WriteFullEndElement(); // </th>
					htmlWriter.WriteStartElement("th");
					htmlWriter.WriteAttributeString("class", "mainTh");
					htmlWriter.WriteFullEndElement(); // </th>
					GenerateWsTableColumns(htmlWriter, "mainTh");
					htmlWriter.WriteFullEndElement(); // </tr>
					htmlWriter.WriteFullEndElement(); // </thead>
					htmlWriter.WriteStartElement("tbody");
					GenerateNormalTableRow(htmlWriter, row, false);
					morphemesOrLexGlossWasDrawn = true;
				}
				else if ((row.Item1.Flid == InterlinLineChoices.kflidMorphemes || row.Item1.Flid == InterlinLineChoices.kflidLexGloss) && morphemesOrLexGlossWasDrawn)
				{
					GenerateNormalTableRow(htmlWriter, row, false);
					htmlWriter.WriteFullEndElement(); // </tbody>
					htmlWriter.WriteFullEndElement(); // </table>
				}
				else
				{
					GenerateNormalTableRow(htmlWriter, row, false);
				}
			}
			htmlWriter.WriteFullEndElement(); // </tr>
			htmlWriter.WriteFullEndElement(); // </tbody>
			htmlWriter.WriteFullEndElement(); // </table>
			htmlWriter.WriteFullEndElement(); // </td>
			htmlWriter.WriteFullEndElement(); // </tr>
		}

		private void GenerateTableRowWithClusterTable(XmlWriter htmlWriter, LinkedList<Tuple<LineOption, InterlinLineSpec>> rowData, string tableClass)
		{
			htmlWriter.WriteStartElement("tr");
			htmlWriter.WriteStartElement("td");
			htmlWriter.WriteAttributeString("class", "grab specialGrab");
			htmlWriter.WriteRaw("&#8801;");
			htmlWriter.WriteFullEndElement(); // </td>
			htmlWriter.WriteStartElement("td");
			htmlWriter.WriteAttributeString("class", "specialTD");
			htmlWriter.WriteAttributeString("colspan", (m_columns.Count + 2).ToString());
			GenerateClusterTable(htmlWriter, rowData, "", tableClass);
			htmlWriter.WriteFullEndElement(); // </td>
			htmlWriter.WriteFullEndElement(); // </tr>
		}

		/// <summary>
		/// Gets the Initial Row Choices for the HTML to generate
		/// </summary>
		private LinkedList<LinkedList<Tuple<LineOption, InterlinLineSpec>>> InitRowChoices()
		{
			var rowChoicesList = new LinkedList<LinkedList<Tuple<LineOption, InterlinLineSpec>>>();
			if (Choices.Mode != InterlinMode.Chart)
			{
				var choiceIndex = 0;
				while (choiceIndex < Choices.AllLineOptions.Count)
				{
					var currentRowChoiceList = new LinkedList<Tuple<LineOption, InterlinLineSpec>>();
					var rowChoice = new LinkedListNode<Tuple<LineOption, InterlinLineSpec>>(new Tuple<LineOption, InterlinLineSpec>(Choices.AllLineOptions[choiceIndex], Choices.AllLineSpecs[choiceIndex]));
					currentRowChoiceList.AddLast(rowChoice);
					if (Choices.AllLineSpecs[choiceIndex].MorphemeLevel && Choices.AllLineSpecs[choiceIndex].WordLevel || !Choices.AllLineSpecs[choiceIndex].WordLevel)
					{
						var incremented = choiceIndex + 1;
						if (incremented < Choices.AllLineSpecs.Count)
						{
							var isNextChoiceValid = Choices.AllLineSpecs[incremented].MorphemeLevel && Choices.AllLineSpecs[incremented].WordLevel
													|| !Choices.AllLineSpecs[incremented].WordLevel;
							while (isNextChoiceValid)
							{
								currentRowChoiceList.AddLast(new Tuple<LineOption, InterlinLineSpec>(Choices.AllLineOptions[incremented], Choices.AllLineSpecs[incremented++]));
								if (incremented >= Choices.AllLineOptions.Count || !Choices.AllLineSpecs[incremented].MorphemeLevel && Choices.AllLineSpecs[incremented].WordLevel)
								{
									isNextChoiceValid = false;
								}
							}
							choiceIndex = incremented;
						}
					}
					else
					{
						choiceIndex++;
					}
					rowChoicesList.AddLast(currentRowChoiceList);
				}
			}
			else
			{
				var choiceIndex = 0;
				while (choiceIndex < Choices.AllLineOptions.Count)
				{
					var currentRowChoiceList = new LinkedList<Tuple<LineOption, InterlinLineSpec>>();
					var rowChoice = new LinkedListNode<Tuple<LineOption, InterlinLineSpec>>(new Tuple<LineOption, InterlinLineSpec>(Choices.AllLineOptions[choiceIndex], Choices.AllLineSpecs[choiceIndex]));
					currentRowChoiceList.AddLast(rowChoice);
					if (Choices.AllLineSpecs[choiceIndex].WordLevel) // Word and Morpheme-level stuff
					{
						var incremented = choiceIndex + 1;
						if (incremented < Choices.AllLineSpecs.Count)
						{
							var initialOptionIsWordLevel = Choices.AllLineSpecs[choiceIndex].WordLevel && !Choices.AllLineSpecs[choiceIndex].MorphemeLevel;
							var isNextChoiceValid = Choices.AllLineSpecs[incremented].WordLevel;
							while (isNextChoiceValid)
							{
								currentRowChoiceList.AddLast(new Tuple<LineOption, InterlinLineSpec>(Choices.AllLineOptions[incremented], Choices.AllLineSpecs[incremented++]));
								if (incremented >= Choices.AllLineSpecs.Count || initialOptionIsWordLevel && Choices.AllLineSpecs[incremented].MorphemeLevel || !Choices.AllLineSpecs[incremented].WordLevel)
								{
									isNextChoiceValid = false;
								}
							}
							choiceIndex = incremented;
						}
					}
					else
					{
						choiceIndex++;
					}
					rowChoicesList.AddLast(currentRowChoiceList);
				}
			}
			return rowChoicesList;
		}

		/// <summary>
		/// Creates the writing system columns for the table
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="className">The name of the class to add to the table header</param>
		private void GenerateWsTableColumns(XmlWriter htmlWriter, string className)
		{
			foreach (var column in m_columns)
			{
				htmlWriter.WriteStartElement("th");
				htmlWriter.WriteAttributeString("class", className);
				htmlWriter.WriteRaw(column.ToString());
				htmlWriter.WriteFullEndElement(); // </th>
			}
		}

		/// <summary>
		/// Generates a Table Row which can be reordered to anywhere else in the table
		/// </summary>
		private void GenerateNormalTableRow(XmlWriter htmlWriter, Tuple<LineOption, InterlinLineSpec> rowDataTuple, bool needsBlankTdBefore)
		{
			htmlWriter.WriteStartElement("tr");
			htmlWriter.WriteAttributeString("id", rowDataTuple.Item1.Flid.ToString());
			htmlWriter.WriteAttributeString("class", "row");
			if (needsBlankTdBefore)
			{
				htmlWriter.WriteStartElement("td");
				htmlWriter.WriteAttributeString("class", "noBorder");
				htmlWriter.WriteFullEndElement(); // </td>
			}
			htmlWriter.WriteStartElement("td");
			htmlWriter.WriteAttributeString("class", "grab");
			htmlWriter.WriteRaw("&#8801;"); // hamburger-looking character
			htmlWriter.WriteFullEndElement(); // </td>
			htmlWriter.WriteStartElement("td");
			htmlWriter.WriteAttributeString("class", "lineChoice");
			htmlWriter.WriteRaw(rowDataTuple.Item1.Label);
			htmlWriter.WriteFullEndElement(); // </td>
			GenerateCheckboxes(htmlWriter, rowDataTuple.Item2);
			htmlWriter.WriteFullEndElement(); // </tr>
		}

		/// <summary>
		/// Generates a sub-table row which can be re-ordered as a whole like a normal table row
		/// but the internal elements can only be re-ordered among themselves
		/// </summary>
		private void GenerateClusterTable(XmlWriter htmlWriter, LinkedList<Tuple<LineOption, InterlinLineSpec>> rowData, string tableId, string tableClass = "clusterTable")
		{
			htmlWriter.WriteStartElement("table");
			htmlWriter.WriteAttributeString("class", tableClass);
			htmlWriter.WriteAttributeString("id", tableId);
			htmlWriter.WriteStartElement("thead");
			htmlWriter.WriteStartElement("tr");
			htmlWriter.WriteStartElement("th");
			htmlWriter.WriteFullEndElement(); // </th>
			htmlWriter.WriteStartElement("th");
			if (Choices.Mode == InterlinMode.Chart)
			{
				htmlWriter.WriteAttributeString("class", "mainTh");
			}
			htmlWriter.WriteFullEndElement(); // </th>
			GenerateWsTableColumns(htmlWriter, Choices.Mode == InterlinMode.Chart ? "mainTh" : "");
			htmlWriter.WriteFullEndElement(); // </tr>
			htmlWriter.WriteFullEndElement(); // </thead>
			htmlWriter.WriteStartElement("tbody");
			foreach (var dataTuple in rowData)
			{
				GenerateNormalTableRow(htmlWriter, dataTuple, false);
			}
			htmlWriter.WriteFullEndElement(); // </tbody>
			htmlWriter.WriteFullEndElement(); // </table>
		}

		/// <summary>
		/// Generates Literal Translation, Free Translation, and Notes in a separate table
		/// </summary>
		private void GenerateNonWordLevelLineOptions(XmlWriter htmlWriter, string tableId, string tableClass, LinkedList<Tuple<LineOption, InterlinLineSpec>> rowData, int numOfBlankColumns = 0)
		{
			htmlWriter.WriteStartElement("table");
			htmlWriter.WriteAttributeString("id", tableId);
			htmlWriter.WriteAttributeString("class", tableClass);
			htmlWriter.WriteStartElement("thead");
			htmlWriter.WriteStartElement("tr");
			for (var i = 0; i < numOfBlankColumns; i++)
			{
				htmlWriter.WriteStartElement("th");
				htmlWriter.WriteFullEndElement(); // </th>
			}
			GenerateWsTableColumns(htmlWriter, "");
			htmlWriter.WriteFullEndElement(); // </tr>
			htmlWriter.WriteFullEndElement(); // </thead>
			htmlWriter.WriteStartElement("tbody");
			foreach (var row in rowData)
			{
				GenerateNormalTableRow(htmlWriter, new Tuple<LineOption, InterlinLineSpec>(row.Item1, row.Item2), true);
			}
			htmlWriter.WriteFullEndElement(); // </tbody>
			htmlWriter.WriteFullEndElement(); // </table>
		}

		/// <summary>
		/// Determines if the spec's ComboContent is contained in one of the cached keys of m_cachedComboContentForColumns.
		/// If it is, then loop through each column to determine if a checkbox is needed. Otherwise, write the td with no checkbox.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="spec">The InterlinLineSpec that contains the ComboContent</param>
		private void GenerateCheckboxes(XmlWriter htmlWriter, InterlinLineSpec spec)
		{
			ComboBox.ObjectCollection objectCollection = null;
			if (m_cachedComboContentForColumns.ContainsKey(spec.ComboContent))
			{
				objectCollection = m_cachedComboContentForColumns[spec.ComboContent];
			}
			if (objectCollection != null)
			{
				foreach (var column in m_columns)
				{
					if (objectCollection.Contains(column))
					{
						var id = spec.Flid + "%" + column.WritingSystem;
						GenerateCheckbox(htmlWriter, id, column.WritingSystemType);
					}
					else
					{
						GenerateCheckbox(htmlWriter, "", column.WritingSystemType, true);
					}
				}
			}
			else
			{
				foreach (var column in m_columns)
				{
					GenerateCheckbox(htmlWriter, "", column.WritingSystemType, true);
				}
			}
		}

		/// <summary>
		/// Creates a new td with a checkbox if isEmptyTd is false. If isEmptyTd is true, then it creates
		/// a td with no checkbox. If a className is given, then it adds the class to the td to show
		/// a separation/distinction between the writing systems. id is typically the flid and ws
		/// together. If blank, no id is written.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		/// <param name="id">The id for the checkbox to be written</param>
		/// <param name="isEmptyTd">If true, a td is written with no checkbox</param>
		/// <param name="className">The name of the class to attach to the td</param>
		private void GenerateCheckbox(XmlWriter htmlWriter, string id, string className, bool isEmptyTd = false)
		{
			if (htmlWriter == null)
			{
				return;
			}
			htmlWriter.WriteStartElement("td");
			htmlWriter.WriteAttributeString("class", className);
			if (!isEmptyTd)
			{
				htmlWriter.WriteStartElement("input");
				htmlWriter.WriteAttributeString("type", "checkbox");
				htmlWriter.WriteAttributeString("id", id);
				htmlWriter.WriteAttributeString("class", "checkBox");
				htmlWriter.WriteAttributeString("name", id.Split('%')[0] + "[]");
				if (Choices.IndexOf(int.Parse(id.Split('%')[0]), int.Parse(id.Split('%')[1]), true) != -1) // If the option is in m_choices, check it
				{
					htmlWriter.WriteAttributeString("checked", "checked");
				}
				htmlWriter.WriteEndElement(); // /> <--- End input element
			}
			htmlWriter.WriteFullEndElement(); // </td>
		}

		/// <summary>
		/// Includes the last JavaScript file needed for the jQuery UI sortable and then closes the last HTML elements.
		/// </summary>
		/// <param name="htmlWriter">The XmlWriter to write the HTML</param>
		private void GenerateClosingHtml(XmlWriter htmlWriter)
		{
			var javascriptFilePaths = GetFilePaths();
			// If we're working with a text chart, we need to enforce the UI constraints for checkboxes.
			if (Choices.Mode == InterlinMode.Chart)
			{
				CreateScriptElement(htmlWriter, GetPathFromFile(javascriptFilePaths, "scriptForChart.js")); // scriptForChart.js
			}
			CreateScriptElement(htmlWriter, GetPathFromFile(javascriptFilePaths, "script.js")); // script.js
			htmlWriter.WriteFullEndElement(); // </body>
			htmlWriter.WriteFullEndElement(); // </html>
		}

		/// <summary>
		/// Navigates to the path where all of the required JavaScript files are stored and then returns the paths in a string array.
		/// </summary>
		/// <returns>A string array with the paths of each JavaScript file needed</returns>
		private static string[] GetFilePaths()
		{
			return Directory.GetFiles(Path.Combine(FwDirectoryFinder.TemplateDirectory, "ConfigureInterlinear"));
		}

		private static string GetPathFromFile(string[] filePaths, string fileName)
		{
			foreach (var path in filePaths)
			{
				if (fileName.Equals(Path.GetFileName(path)))
				{
					return path;
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Finds the line choices in m_choices with the given flids and then sets the new order.
		/// </summary>
		/// <param name="lineChoicesFlids">The array containing the flids of the line choices</param>
		private void ReorderLineChoices(int[] lineChoicesFlids)
		{
			Choices.AllLineOptions = lineChoicesFlids.Select(choice => Choices.AllLineOptions.Find(x => x.Flid == choice)).Where(optionToMove => optionToMove != null).ToList();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		public void InitColumnDictionary()
		{
			foreach (var spec in Choices.AllLineSpecs)
			{
				WsComboItems(spec.ComboContent);
			}
		}

		private void InitColumns()
		{
			m_columns.Clear();

			if (Choices.Mode != InterlinMode.Chart)
			{
				var vernacularInParaWss = m_cachedComboContentForColumns[WsComboContent.kwccVernacularInParagraph];
				var bestAnalysisWss = m_cachedComboContentForColumns[WsComboContent.kwccBestAnalysis];
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
				{
					m_columns.Add(ws);
				}
				// Next, we need the analysis writing systems
				foreach (WsComboItem ws in bestAnalysisWss)
				{
					// If the writing system was already added, there's no need to add it again.
					// In the case that it already contains the writing system, that means it is both
					// a vernacular and analysis writing system. We only need one column for it.
					// It will display checkboxes for every option.
					if (vernacularInParaWss.Contains(ws))
					{
						continue;
					}
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
					var item = new WsComboItem(ws.DisplayLabel, ws.Id) { WritingSystem = ws.Handle, WritingSystemType = "vernacular" };
					if (analysisWss.Contains(ws))
					{
						item.WritingSystemType = "both";
						vernacularAndAnalysisWss.Add(item);
						continue;
					}
					m_columns.Add(item);
				}
				foreach (var ws in vernacularAndAnalysisWss)
				{
					m_columns.Add(ws);
				}
				foreach (var ws in analysisWss)
				{
					if (vernacularWss.Contains(ws))
					{
						continue;
					}
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
		private void WsComboItems(WsComboContent comboContent)
		{
			WsComboItemsInternal(m_cache, wsCombo, m_cachedComboContentForColumns, comboContent);
		}

		/// <summary>
		/// This is used to create an object collection with the appropriate writing system choices to be used in wsCombo.  The reason it is cached is because
		/// list generation will require looping through each kind of combo box several times.
		///
		/// This version is visible to InterlinDocRootSiteBase for its context menu.
		/// </summary>
		internal static ComboBox.ObjectCollection WsComboItemsInternal(LcmCache cache, ComboBox owner, Dictionary<WsComboContent, ComboBox.ObjectCollection> cachedBoxes, WsComboContent comboContent)
		{
			ComboBox.ObjectCollection objectCollection;
			if (!cachedBoxes.ContainsKey(comboContent))
			{
				objectCollection = new ComboBox.ObjectCollection(owner);
				// The final argument here restricts writing systems that will be added to the combo box to
				// only be "real" writing systems.  So, English will be added, but not "Default Analysis".
				// This functionality should eventually go away.  See LT-4740.
				// JohnT: it now partially has, two lines support 'best analysis'.
				ColumnConfigureDialog.AddWritingSystemsToCombo(cache, objectCollection, comboContent, comboContent != WsComboContent.kwccBestAnalysis);
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
			switch (id)
			{
				// special case, the only few we support so far (and only for a few fields).
				case "best analysis":
					return WritingSystemServices.kwsFirstAnal;//LangProject.kwsFirstAnal;
				case "vern in para":
					return WritingSystemServices.kwsVernInParagraph;
			}
			Debug.Assert(!XmlViewsUtils.GetWsRequiresObject(id), "Writing system is magic.  These should never be used in the Interlinear area.");
			var ws = -50;
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
				string checkedBoxes;
				context.EvaluateScript("getNumOfCheckedBoxes()", out checkedBoxes);
				var numOfCheckedBoxes = Convert.ToInt32(checkedBoxes);
				okButton.Enabled = numOfCheckedBoxes > 0;
			}
		}

		private void HelpButton_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		/// <summary>
		/// When the okButton is clicked, this gets the JavaScript order of the rows to reorder the line choices. Additionally,
		/// this gets the checkboxes on the page to add/remove them from the actual m_choices.
		/// </summary>
		private void OkButton_Click(object sender, EventArgs e)
		{
			var checkBoxes = (mainBrowser.NativeBrowser as GeckoWebBrowser)?.Document.GetElementsByClassName("checkBox");
			if (checkBoxes == null)
			{
				return;
			}
			using (var context = new AutoJSContext(((GeckoWebBrowser)mainBrowser.NativeBrowser).Window.DomWindow))
			{
				string rows;
				context.EvaluateScript(@"getRows()", out rows);
				ReorderLineChoices(Array.ConvertAll(rows.Split(','), int.Parse));
			}
			Choices.m_specs.Clear();
			foreach (var checkBox in checkBoxes)
			{
				var element = (GeckoInputElement)checkBox;
				var elementId = element.GetAttribute("id");
				var flidAndWs = elementId.Split('%');
				var flid = int.Parse(flidAndWs[0]);
				var ws = int.Parse(flidAndWs[1]);

				if (element.Checked)
				{
					Choices.m_specs.Add(Choices.CreateSpec(flid, ws));
				}
			}
		}
		#endregion
	}
}