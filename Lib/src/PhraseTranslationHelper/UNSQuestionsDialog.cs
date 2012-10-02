// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UNSQuestionsDialog.cs
// Responsibility: Tom Bogle
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SILUBS.PhraseTranslationHelper
{
	#region UNSQuestionsDialog class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// UNSQuestionsDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UNSQuestionsDialog : Form
	{
		#region Member Data
		private readonly string m_projectName;
		private readonly string m_vernIcuLocale;
		private readonly Action m_selectVernacularKeyboard;
		private readonly Action m_helpDelegate;
		private readonly PhraseTranslationHelper m_helper;
		private readonly string m_translationsFile;
		private readonly string m_unsDataFolder;
		private readonly string m_defaultLcfFolder;
		private readonly string m_appName;
		private readonly IDictionary<string, string> m_sectionHeadText;
		private readonly int m_gridRowHeight;
		private DateTime m_lastSaveTime;

		private DataGridView dataGridUns;
		private DataGridViewTextBoxColumn m_colReference;
		private DataGridViewTextBoxColumn m_colEnglish;
		private DataGridViewTextBoxColumn m_colTranslation;
		private DataGridViewCheckBoxColumn m_colUserTranslated;
		private DataGridViewTextBoxColumn m_colDebugInfo;
		private MenuStrip m_mainMenu;
		private ToolStripMenuItem fileToolStripMenuItem;
		private ToolStripMenuItem saveToolStripMenuItem;
		private ToolStripMenuItem reloadToolStripMenuItem;
		private ToolStripMenuItem closeToolStripMenuItem;
		private ToolStripMenuItem filterToolStripMenuItem;
		private ToolStripMenuItem mnuKtFilter;
		private ToolStripMenuItem mnuShowAllPhrases;
		private ToolStripMenuItem mnuShowPhrasesWithKtRenderings;
		private ToolStripMenuItem mnuShowPhrasesWithMissingKtRenderings;
		private ToolStripMenuItem viewToolStripMenuItem;
		private ToolStrip toolStrip1;
		private ToolStripLabel toolStripLabel1;
		private ToolStripTextBox txtFilterByPart;
		private ToolStripMenuItem mnuMatchExact;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripMenuItem mnuGenerateTemplate;
		private ToolStripSeparator toolStripSeparator3;
		private ToolStripMenuItem mnuViewToolbar;
		private ToolStripMenuItem mnuAutoSave;
		private ToolStripButton btnSave;
		#endregion

		#region Delegates
		public Func<IEnumerable<int>> GetAvailableBooks { private get; set; }
		public event EventHandler UpdateCustomMenu;
		#endregion

		#region Properties
		public ComprehensionCheckingSettings Settings
		{
			get { return new ComprehensionCheckingSettings(this); }
		}

		internal PhraseTranslationHelper.KeyTermFilterType CheckedKeyTermFilterType
		{
			get
			{
				return (PhraseTranslationHelper.KeyTermFilterType)mnuKtFilter.DropDownItems.Cast<ToolStripMenuItem>().First(menu => menu.Checked).Tag;
			}
			private set
			{
				mnuKtFilter.DropDownItems.Cast<ToolStripMenuItem>().Where(
					menu => (PhraseTranslationHelper.KeyTermFilterType)menu.Tag == value).First().Checked = true;
				ApplyFilter(null, new EventArgs());
			}
		}

		protected bool SaveNeeded
		{
			get { return btnSave.Enabled; }
			set
			{
				if (mnuAutoSave.Checked && DateTime.Now > m_lastSaveTime.AddSeconds(10))
					Save(null, new EventArgs());
				else
					saveToolStripMenuItem.Enabled = btnSave.Enabled = value;
			}
		}

		protected IEnumerable<int> AvailableBookIds
		{
			get
			{
				if (GetAvailableBooks != null)
				{
					foreach (int i in GetAvailableBooks())
						yield return i;
				}
				else
				{
					for (int i = 1; i <= BCVRef.LastBook; i++)
						yield return i;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating matching on parts matches whole part.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool MatchWholeParts
		{
			get { return mnuMatchExact.Checked; }
			set { mnuMatchExact.Checked = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether toolbar is displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool ShowToolbar
		{
			get { return mnuViewToolbar.Checked; }
			set { mnuViewToolbar.Checked = value; }
		}

		internal GenerateTemplateSettings GenTemplateSettings { get; private set; }
		#endregion

		#region class UnsTranslation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Little class to support XML serialization
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlType("Translation")]
		public class UnsTranslation
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the reference.
			/// </summary>
			/// --------------------------------------------------------------------------------
			[XmlAttribute("ref")]
			public string Reference { get; set; }
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the original phrase.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string OriginalPhrase { get; set; }
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the translation.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string Translation { get; set; }
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="UnsTranslation"/> class, needed
			/// for XML serialization.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public UnsTranslation()
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="UnsTranslation"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public UnsTranslation(TranslatablePhrase tp)
			{
				Reference = tp.Reference;
				OriginalPhrase = tp.OriginalPhrase;
				Translation = tp.Translation;
			}
		}
		#endregion

		#region class AnswersAndComments
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the questions from the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class AnswersAndComments
		{
			private List<string> m_answers;
			private List<string> m_comments;

			internal void AddAnswer(string answer)
			{
				if (string.IsNullOrEmpty(answer))
					return;
				if (m_answers == null)
					m_answers = new List<string>(1);
				m_answers.Add(answer);
			}

			internal void AddComment(string comment)
			{
				if (string.IsNullOrEmpty(comment))
					return;
				if (m_comments == null)
					m_comments = new List<string>(1);
				m_comments.Add(comment);
			}

			internal bool HasAnswer
			{
				get { return m_answers != null;}
			}

			internal bool HasComment
			{
				get { return m_comments != null; }
			}

			internal IEnumerable<string> Answers
			{
				get
				{
					if (m_answers != null)
						return m_answers;
					return new List<string>();
				}
			}

			internal IEnumerable<string> Comments
			{
				get
				{
					if (m_comments != null)
						return m_comments;
					return new List<string>();
				}
			}
		}
		#endregion

		#region class QuestionProvider
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the questions from the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class QuestionProvider : IEnumerable<TranslatablePhrase>
		{
			private readonly string m_sFilename;
			private readonly Dictionary<string, string> m_sectionHeadText = new Dictionary<string, string>();
			private bool finishedParsingFile = false;
			private static readonly string s_kSectionHead = @"\rf";
			private static readonly string s_kRefMarker = @"\tqref";
			private static readonly string s_kQuestionMarker = @"\bttq";
			private static readonly string s_kAnswerMarker = @"\tqe";
			private static readonly string s_kCommentMarker = @"\an";
			private static readonly List<string> s_categories = new List<string>();

			static QuestionProvider()
			{
				s_categories.Add(@"\oh");
				s_categories.Add(@"\dh");
			}

			internal QuestionProvider(string filename)
			{
				m_sFilename = filename;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Parses the given reference (that could be a verse bridge) and returns a BBBCCCVVV
			/// integer representing the start and end references.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void Parse(string sReference, out int startRef, out int endRef)
			{
				BCVRef bcvStartRef = 0;
				BCVRef bcvEndRef = 0;
				BCVRef.ParseRefRange(sReference, ref bcvStartRef, ref bcvEndRef);
				startRef = bcvStartRef;
				endRef = bcvEndRef;
			}

			internal IDictionary<string, string> SectionHeads
			{
				get
				{
					if (!finishedParsingFile)
						throw new InvalidOperationException("Cannot access SectionHeads until all TranslatablePhrases have been retrieved");
					return m_sectionHeadText; }
			}

			#region IEnumerable<string> Members
			public IEnumerator<TranslatablePhrase> GetEnumerator()
			{
				// Initialize the ID textbox.
				TextReader reader = null;
				int currCat = -1;
				string currRef = null;
				string currQuestion = null;
				AnswersAndComments currAnswersAndComments = new AnswersAndComments();
				int startRef = 0, endRef = 0, seq = 0;
				List<int> categoriesAdded = new List<int>(s_categories.Count);
				int kSectHeadMarkerLen = s_kSectionHead.Length;
				int kRefMarkerLen = s_kRefMarker.Length;
				int kQMarkerLen = s_kQuestionMarker.Length;
				int kAMarkerLen = s_kAnswerMarker.Length;
				int kCommentMarkerLen = s_kCommentMarker.Length;
				string sectionHeadText = null;
				try
				{
					reader = new StreamReader(m_sFilename, Encoding.UTF8);

					string sLine;
					while ((sLine = reader.ReadLine()) != null)
					{
						if (sLine.StartsWith(s_kQuestionMarker))
						{
							if (currQuestion != null)
							{
								yield return new TranslatablePhrase(currQuestion, currCat, currRef, startRef, endRef, seq++, currAnswersAndComments);
								currAnswersAndComments = new AnswersAndComments();
							}
							currQuestion = sLine.Substring(kQMarkerLen).Trim();
						}
						else if (sLine.StartsWith(s_kAnswerMarker))
						{
							currAnswersAndComments.AddAnswer(sLine.Substring(kAMarkerLen).Trim());
						}
						else if (sLine.StartsWith(s_kCommentMarker))
						{
							currAnswersAndComments.AddComment(sLine.Substring(kCommentMarkerLen).Trim());
						}
						else
						{
							if (currQuestion != null)
							{
								yield return new TranslatablePhrase(currQuestion, currCat, currRef, startRef, endRef, seq++, currAnswersAndComments);
								currQuestion = null;
								currAnswersAndComments = new AnswersAndComments();
							}

							if (sLine.StartsWith(s_kRefMarker))
							{
								currRef = sLine.Substring(kRefMarkerLen).Trim();
								if (sectionHeadText != null)
								{
									m_sectionHeadText[currRef] = sectionHeadText;
									sectionHeadText = null;
								}
								Parse(currRef, out startRef, out endRef);
								seq = 0;
							}
							else if (sLine.StartsWith(s_kSectionHead))
							{
								sectionHeadText = sLine.Substring(kSectHeadMarkerLen).Trim();
							}
							else
							{
								for (int i = 0; i < s_categories.Count; i++)
								{
									string category = s_categories[i];
									if (sLine.StartsWith(category))
									{
										if (i == 0)
											startRef = endRef = 0;
										seq = 0;
										currCat = i;
										if (!categoriesAdded.Contains(i))
										{
											yield return new TranslatablePhrase(sLine.Substring(category.Length).Trim(),
												-1, string.Empty, 0, 0, i);
											categoriesAdded.Add(i);
										}
										break;
									}
								}
							}
						}
					}
					if (currQuestion != null)
						yield return new TranslatablePhrase(currQuestion, currCat, currRef, startRef, endRef, seq, currAnswersAndComments);
				}
				finally
				{
					if (reader != null)
						reader.Close();
					finishedParsingFile = true;
				}
			}
			#endregion

			#region IEnumerable Members
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
			#endregion
		}
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UNSQuestionsDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UNSQuestionsDialog(string projectName, IEnumerable<IKeyTerm> keyTerms,
			Font vernFont, string VernIcuLocale, bool fVernIsRtoL, string sDefaultLcfFolder,
			ComprehensionCheckingSettings settings, string appName,
			IList<KeyValuePair<string, Func<int, int, string, bool>>> refFilters,
			Action selectVernacularKeyboard, Action helpDelegate)
		{
			m_projectName = projectName;
			m_vernIcuLocale = VernIcuLocale;
			m_selectVernacularKeyboard = selectVernacularKeyboard;
			m_helpDelegate = helpDelegate;
			m_defaultLcfFolder = sDefaultLcfFolder;
			m_appName = appName;

			InitializeComponent();

			mnuShowAllPhrases.Tag = PhraseTranslationHelper.KeyTermFilterType.All;
			mnuShowPhrasesWithKtRenderings.Tag = PhraseTranslationHelper.KeyTermFilterType.WithRenderings;
			mnuShowPhrasesWithMissingKtRenderings.Tag = PhraseTranslationHelper.KeyTermFilterType.WithoutRenderings;

			Location = settings.Location;
			WindowState = settings.DefaultWindowState;
			if (MinimumSize.Height <= settings.DialogSize.Height &&
				MinimumSize.Width <= settings.DialogSize.Width)
			{
				Size = settings.DialogSize;
			}
			MatchWholeParts = settings.MatchWholeParts;
			ShowToolbar = settings.ShowToolbar;
			GenTemplateSettings = settings.GenTemplateSettings;

			if (refFilters != null)
			{
				int index;
				for (index = 0; index < refFilters.Count; index++)
				{
					KeyValuePair<string, Func<int, int, string, bool>> filter = refFilters[index];
					ToolStripMenuItem menuItem = new ToolStripMenuItem(filter.Key, null, ApplyFilter);
					menuItem.Tag = filter.Value;
					filterToolStripMenuItem.DropDownItems.Insert(index, menuItem);
				}
				if (index > 0)
				{
					filterToolStripMenuItem.DropDownItems.Insert(index, new ToolStripSeparator());
					filterToolStripMenuItem.DropDownOpening += DetermineAvailabilityOfCustomFilterMenuItems;
				}
			}

			if (!File.Exists(settings.QuestionsFile))
			{
				MessageBox.Show(Properties.Resources.kstidFileNotFound + settings.QuestionsFile, Text);
				return;
			}

			m_unsDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"UNS Questions");
			m_translationsFile = Path.Combine(m_unsDataFolder, string.Format("Translations of Checking Questions - {0}.xml", projectName));

			HelpButton = (m_helpDelegate != null);

			Exception e;
			KeyTermRules rules = XmlSerializationHelper.DeserializeFromFile<KeyTermRules>(Path.Combine(m_unsDataFolder, "keyTermRules.xml"), out e);
			if (e != null)
				MessageBox.Show(e.ToString(), Text);

			QuestionProvider qp = new QuestionProvider(settings.QuestionsFile);
			m_helper = new PhraseTranslationHelper(qp, keyTerms, rules);
			m_sectionHeadText = qp.SectionHeads;
			if (File.Exists(m_translationsFile))
			{
				List<UnsTranslation> translations = XmlSerializationHelper.DeserializeFromFile<List<UnsTranslation>>(m_translationsFile, out e);
				if (e != null)
				{
					MessageBox.Show(e.ToString());
				}
				else
				{
					foreach (UnsTranslation unsTranslation in translations)
					{
						TranslatablePhrase phrase = m_helper.GetPhrase(unsTranslation.Reference, unsTranslation.OriginalPhrase);
						if (phrase != null) // unlikely, but an happen if master list is modified
							phrase.Translation = unsTranslation.Translation;
					}
				}
			}
			m_helper.ProcessAllTranslations();
			m_helper.TranslationsChanged += m_helper_TranslationsChanged;

			DataGridViewCellStyle translationCellStyle = new DataGridViewCellStyle();
			translationCellStyle.Font = vernFont;
			m_colTranslation.DefaultCellStyle = translationCellStyle;
			if (fVernIsRtoL)
				m_colTranslation.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

			m_gridRowHeight = Math.Max(vernFont.Height, dataGridUns.Font.Height) + 2;

			dataGridUns.RowCount = m_helper.Phrases.Count();
			Margin = new Padding(Margin.Left, toolStrip1.Height, Margin.Right, Margin.Bottom);

			// Now apply settings that have filtering side-effects
			CheckedKeyTermFilterType = settings.KeyTermFilterType;
		}

		void DetermineAvailabilityOfCustomFilterMenuItems(object sender, EventArgs e)
		{
			if (UpdateCustomMenu == null)
				return;
			foreach (ToolStripItem menu in filterToolStripMenuItem.DropDownItems)
			{
				if (menu == mnuKtFilter || menu is ToolStripSeparator)
					break;
				UpdateCustomMenu(menu, e);
			}
		}
		#endregion

		#region InitializeComponent
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forms designer method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.ToolStripMenuItem mnuViewDebugInfo;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UNSQuestionsDialog));
			this.dataGridUns = new System.Windows.Forms.DataGridView();
			this.m_colReference = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_colEnglish = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_colTranslation = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_colUserTranslated = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.m_colDebugInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_mainMenu = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuAutoSave = new System.Windows.Forms.ToolStripMenuItem();
			this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuGenerateTemplate = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.filterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuKtFilter = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuShowAllPhrases = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuShowPhrasesWithKtRenderings = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuShowPhrasesWithMissingKtRenderings = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuMatchExact = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuViewToolbar = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.btnSave = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.txtFilterByPart = new System.Windows.Forms.ToolStripTextBox();
			mnuViewDebugInfo = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.dataGridUns)).BeginInit();
			this.m_mainMenu.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			//
			// mnuViewDebugInfo
			//
			mnuViewDebugInfo.Checked = true;
			mnuViewDebugInfo.CheckOnClick = true;
			mnuViewDebugInfo.CheckState = System.Windows.Forms.CheckState.Checked;
			mnuViewDebugInfo.Name = "mnuViewDebugInfo";
			resources.ApplyResources(mnuViewDebugInfo, "mnuViewDebugInfo");
			mnuViewDebugInfo.CheckedChanged += new System.EventHandler(this.m_chkDebugInfo_CheckedChanged);
			//
			// dataGridUns
			//
			this.dataGridUns.AllowUserToAddRows = false;
			this.dataGridUns.AllowUserToDeleteRows = false;
			this.dataGridUns.AllowUserToResizeRows = false;
			this.dataGridUns.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.dataGridUns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridUns.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.m_colReference,
			this.m_colEnglish,
			this.m_colTranslation,
			this.m_colUserTranslated,
			this.m_colDebugInfo});
			resources.ApplyResources(this.dataGridUns, "dataGridUns");
			this.dataGridUns.Name = "dataGridUns";
			this.dataGridUns.RowHeadersVisible = false;
			this.dataGridUns.VirtualMode = true;
			this.dataGridUns.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_CellDoubleClick);
			this.dataGridUns.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridUns_ColumnHeaderMouseClick);
			this.dataGridUns.RowHeightInfoNeeded += new System.Windows.Forms.DataGridViewRowHeightInfoNeededEventHandler(this.dataGridUns_RowHeightInfoNeeded);
			this.dataGridUns.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dataGridUns_CellValueNeeded);
			this.dataGridUns.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dataGridUns_CellValuePushed);
			this.dataGridUns.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_CellEnter);
			//
			// m_colReference
			//
			this.m_colReference.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.m_colReference, "m_colReference");
			this.m_colReference.Name = "m_colReference";
			this.m_colReference.ReadOnly = true;
			this.m_colReference.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.m_colReference.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_colEnglish
			//
			this.m_colEnglish.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			resources.ApplyResources(this.m_colEnglish, "m_colEnglish");
			this.m_colEnglish.Name = "m_colEnglish";
			this.m_colEnglish.ReadOnly = true;
			//
			// m_colTranslation
			//
			this.m_colTranslation.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			resources.ApplyResources(this.m_colTranslation, "m_colTranslation");
			this.m_colTranslation.Name = "m_colTranslation";
			//
			// m_colUserTranslated
			//
			this.m_colUserTranslated.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
			resources.ApplyResources(this.m_colUserTranslated, "m_colUserTranslated");
			this.m_colUserTranslated.Name = "m_colUserTranslated";
			this.m_colUserTranslated.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			//
			// m_colDebugInfo
			//
			this.m_colDebugInfo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			resources.ApplyResources(this.m_colDebugInfo, "m_colDebugInfo");
			this.m_colDebugInfo.Name = "m_colDebugInfo";
			this.m_colDebugInfo.ReadOnly = true;
			this.m_colDebugInfo.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_mainMenu
			//
			this.m_mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.fileToolStripMenuItem,
			this.filterToolStripMenuItem,
			this.viewToolStripMenuItem});
			resources.ApplyResources(this.m_mainMenu, "m_mainMenu");
			this.m_mainMenu.Name = "m_mainMenu";
			//
			// fileToolStripMenuItem
			//
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.saveToolStripMenuItem,
			this.mnuAutoSave,
			this.reloadToolStripMenuItem,
			this.toolStripSeparator2,
			this.mnuGenerateTemplate,
			this.toolStripSeparator3,
			this.closeToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
			//
			// saveToolStripMenuItem
			//
			resources.ApplyResources(this.saveToolStripMenuItem, "saveToolStripMenuItem");
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.Save);
			//
			// mnuAutoSave
			//
			this.mnuAutoSave.Checked = true;
			this.mnuAutoSave.CheckOnClick = true;
			this.mnuAutoSave.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuAutoSave.Name = "mnuAutoSave";
			resources.ApplyResources(this.mnuAutoSave, "mnuAutoSave");
			this.mnuAutoSave.CheckedChanged += new System.EventHandler(this.mnuAutoSave_CheckedChanged);
			//
			// reloadToolStripMenuItem
			//
			this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
			resources.ApplyResources(this.reloadToolStripMenuItem, "reloadToolStripMenuItem");
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
			//
			// mnuGenerateTemplate
			//
			this.mnuGenerateTemplate.Name = "mnuGenerateTemplate";
			resources.ApplyResources(this.mnuGenerateTemplate, "mnuGenerateTemplate");
			this.mnuGenerateTemplate.Click += new System.EventHandler(this.mnuGenerateTemplate_Click);
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
			//
			// closeToolStripMenuItem
			//
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			resources.ApplyResources(this.closeToolStripMenuItem, "closeToolStripMenuItem");
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
			//
			// filterToolStripMenuItem
			//
			this.filterToolStripMenuItem.CheckOnClick = true;
			this.filterToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuKtFilter,
			this.mnuMatchExact});
			this.filterToolStripMenuItem.Name = "filterToolStripMenuItem";
			resources.ApplyResources(this.filterToolStripMenuItem, "filterToolStripMenuItem");
			//
			// mnuKtFilter
			//
			this.mnuKtFilter.CheckOnClick = true;
			this.mnuKtFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuShowAllPhrases,
			this.mnuShowPhrasesWithKtRenderings,
			this.mnuShowPhrasesWithMissingKtRenderings});
			this.mnuKtFilter.Name = "mnuKtFilter";
			resources.ApplyResources(this.mnuKtFilter, "mnuKtFilter");
			//
			// mnuShowAllPhrases
			//
			this.mnuShowAllPhrases.Checked = true;
			this.mnuShowAllPhrases.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuShowAllPhrases.Name = "mnuShowAllPhrases";
			resources.ApplyResources(this.mnuShowAllPhrases, "mnuShowAllPhrases");
			this.mnuShowAllPhrases.CheckedChanged += new System.EventHandler(this.OnKeyTermsFilterChecked);
			this.mnuShowAllPhrases.Click += new System.EventHandler(this.OnKeyTermsFilterChange);
			//
			// mnuShowPhrasesWithKtRenderings
			//
			this.mnuShowPhrasesWithKtRenderings.CheckOnClick = true;
			this.mnuShowPhrasesWithKtRenderings.Name = "mnuShowPhrasesWithKtRenderings";
			resources.ApplyResources(this.mnuShowPhrasesWithKtRenderings, "mnuShowPhrasesWithKtRenderings");
			this.mnuShowPhrasesWithKtRenderings.CheckedChanged += new System.EventHandler(this.OnKeyTermsFilterChecked);
			this.mnuShowPhrasesWithKtRenderings.Click += new System.EventHandler(this.OnKeyTermsFilterChange);
			//
			// mnuShowPhrasesWithMissingKtRenderings
			//
			this.mnuShowPhrasesWithMissingKtRenderings.CheckOnClick = true;
			this.mnuShowPhrasesWithMissingKtRenderings.Name = "mnuShowPhrasesWithMissingKtRenderings";
			resources.ApplyResources(this.mnuShowPhrasesWithMissingKtRenderings, "mnuShowPhrasesWithMissingKtRenderings");
			this.mnuShowPhrasesWithMissingKtRenderings.CheckedChanged += new System.EventHandler(this.OnKeyTermsFilterChecked);
			this.mnuShowPhrasesWithMissingKtRenderings.Click += new System.EventHandler(this.OnKeyTermsFilterChange);
			//
			// mnuMatchExact
			//
			this.mnuMatchExact.CheckOnClick = true;
			this.mnuMatchExact.Name = "mnuMatchExact";
			resources.ApplyResources(this.mnuMatchExact, "mnuMatchExact");
			this.mnuMatchExact.CheckedChanged += new System.EventHandler(this.ApplyFilter);
			//
			// viewToolStripMenuItem
			//
			this.viewToolStripMenuItem.Checked = true;
			this.viewToolStripMenuItem.CheckOnClick = true;
			this.viewToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			mnuViewDebugInfo,
			this.mnuViewToolbar});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			resources.ApplyResources(this.viewToolStripMenuItem, "viewToolStripMenuItem");
			this.viewToolStripMenuItem.CheckedChanged += new System.EventHandler(this.mnuViewToolbar_CheckedChanged);
			//
			// mnuViewToolbar
			//
			this.mnuViewToolbar.Checked = true;
			this.mnuViewToolbar.CheckOnClick = true;
			this.mnuViewToolbar.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuViewToolbar.Name = "mnuViewToolbar";
			resources.ApplyResources(this.mnuViewToolbar, "mnuViewToolbar");
			this.mnuViewToolbar.CheckStateChanged += new System.EventHandler(this.mnuViewToolbar_CheckedChanged);
			//
			// toolStrip1
			//
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.btnSave,
			this.toolStripSeparator1,
			this.toolStripLabel1,
			this.txtFilterByPart});
			resources.ApplyResources(this.toolStrip1, "toolStrip1");
			this.toolStrip1.Name = "toolStrip1";
			//
			// btnSave
			//
			this.btnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.btnSave, "btnSave");
			this.btnSave.Name = "btnSave";
			this.btnSave.Click += new System.EventHandler(this.Save);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
			//
			// toolStripLabel1
			//
			this.toolStripLabel1.Name = "toolStripLabel1";
			resources.ApplyResources(this.toolStripLabel1, "toolStripLabel1");
			//
			// txtFilterByPart
			//
			this.txtFilterByPart.AcceptsReturn = true;
			this.txtFilterByPart.Name = "txtFilterByPart";
			resources.ApplyResources(this.txtFilterByPart, "txtFilterByPart");
			this.txtFilterByPart.TextChanged += new System.EventHandler(this.ApplyFilter);
			//
			// UNSQuestionsDialog
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.dataGridUns);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.m_mainMenu);
			this.HelpButton = true;
			this.MainMenuStrip = this.m_mainMenu;
			this.Name = "UNSQuestionsDialog";
			this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.UNSQuestionsDialog_HelpButtonClicked);
			((System.ComponentModel.ISupportInitialize)(this.dataGridUns)).EndInit();
			this.m_mainMenu.ResumeLayout(false);
			this.m_mainMenu.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Closing"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (SaveNeeded)
			{
				switch (MessageBox.Show(this, "You have made changes. Do you wish to save before closing?",
					"Save changes?", MessageBoxButtons.YesNoCancel))
				{
					case DialogResult.Yes:
						Save(null, null);
						break;
					case DialogResult.Cancel:
						e.Cancel = true;
						break;
				}
			}

			base.OnClosing(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the data grid when the translations change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_helper_TranslationsChanged()
		{
			dataGridUns.Refresh();
		}

		private void dataGridUns_CellEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == 2 && m_selectVernacularKeyboard != null)
				m_selectVernacularKeyboard();
		}

		private void m_chkDebugInfo_CheckedChanged(object sender, EventArgs e)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			if (!item.Checked)
				dataGridUns.Columns.Remove(m_colDebugInfo);
			else
				dataGridUns.Columns.Add(m_colDebugInfo);
		}

		private void dataGridUns_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			switch (e.ColumnIndex)
			{
				case 0: e.Value = m_helper[e.RowIndex].Reference; break;
				case 1: e.Value = m_helper[e.RowIndex].OriginalPhrase; break;
				case 2: e.Value = m_helper[e.RowIndex].Translation; break;
				case 3: e.Value = m_helper[e.RowIndex].HasUserTranslation; break;
				case 4: e.Value = m_helper[e.RowIndex].Parts; break;
			}
		}

		private void dataGridUns_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
		{
			switch (e.ColumnIndex)
			{
				case 2: m_helper[e.RowIndex].Translation = (string)e.Value; SaveNeeded = true;  break;
				case 3: m_helper[e.RowIndex].HasUserTranslation = (bool)e.Value; SaveNeeded = true; break;
			}
		}

		private void dataGridUns_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			int iClickedCol = e.ColumnIndex;
			// We want to sort it ascending unless it already was ascending.
			bool sortAscending = (dataGridUns.Columns[iClickedCol].HeaderCell.SortGlyphDirection != SortOrder.Ascending);
			if (!sortAscending)
			{
				dataGridUns.Columns[iClickedCol].HeaderCell.SortGlyphDirection = SortOrder.Descending;
			}
			else
			{
				for (int i = 0; i < dataGridUns.Columns.Count; i++)
				{
					dataGridUns.Columns[i].HeaderCell.SortGlyphDirection = (i == iClickedCol) ?
						SortOrder.Ascending : SortOrder.None;
				}
			}
			SortByColumn(iClickedCol, sortAscending);
			dataGridUns.Refresh();
		}

		private void SortByColumn(int iClickedCol, bool sortAscending)
		{
			switch (iClickedCol)
			{
				case 0: m_helper.Sort(PhraseTranslationHelper.SortBy.Reference, sortAscending); break;
				case 1: m_helper.Sort(PhraseTranslationHelper.SortBy.OriginalPhrase, sortAscending); break;
				case 2: m_helper.Sort(PhraseTranslationHelper.SortBy.Translation, sortAscending); break;
				case 3: m_helper.Sort(PhraseTranslationHelper.SortBy.Status, sortAscending); break;
				case 4: m_helper.Sort(PhraseTranslationHelper.SortBy.Default, sortAscending); break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the filter.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ApplyFilter(object sender, EventArgs e)
		{
			Func<int, int, string, bool> refFilter = (sender is ToolStripMenuItem) ?
				((ToolStripMenuItem)sender).Tag as Func<int, int, string, bool>: null;
			dataGridUns.RowCount = 0;
			m_helper.Filter(txtFilterByPart.Text, MatchWholeParts, CheckedKeyTermFilterType, refFilter);
			dataGridUns.RowCount = m_helper.Phrases.Count();
		}

		private void dataGridUns_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == 4)
			{
				StringBuilder sbldr = new StringBuilder("Key Terms:\n");
				foreach (KeyTermMatch keyTermMatch in m_helper[e.RowIndex].GetParts().OfType<KeyTermMatch>())
				{
					foreach (string sEnglishTerm in keyTermMatch.AllTerms.Select(term => term.Term))
					{
						sbldr.Append(sEnglishTerm);
						sbldr.Append(Environment.NewLine);
					}
				}
				MessageBox.Show(sbldr.ToString(), "More Key Term Debug Info");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the RowHeightInfoNeeded event of the dataGridView1 control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewRowHeightInfoNeededEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void dataGridUns_RowHeightInfoNeeded(object sender, DataGridViewRowHeightInfoNeededEventArgs e)
		{
			e.Height = m_gridRowHeight;
			e.MinimumHeight = m_gridRowHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when one of the Key Term filtering sub-menus is clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnKeyTermsFilterChange(object sender, EventArgs e)
		{
			if (sender == mnuShowAllPhrases && mnuShowAllPhrases.Checked)
				return;

			if (!((ToolStripMenuItem)sender).Checked)
				mnuShowAllPhrases.Checked = true;
			ApplyFilter(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when one of the Key Term filtering sub-menus is checked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnKeyTermsFilterChecked(object sender, EventArgs e)
		{
			ToolStripMenuItem clickedMenu = (ToolStripMenuItem)sender;
			if (clickedMenu.Checked)
			{
				foreach (ToolStripMenuItem menu in mnuKtFilter.DropDownItems)
				{
					if (menu != clickedMenu)
						menu.Checked = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the HelpButtonClicked event of the UNSQuestionsDialog control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void UNSQuestionsDialog_HelpButtonClicked(object sender, System.ComponentModel.CancelEventArgs e)
		{
			m_helpDelegate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the UNS Translation data.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void Save(object sender, EventArgs e)
		{
			if (!Directory.Exists(m_unsDataFolder))
				Directory.CreateDirectory(m_unsDataFolder);
			XmlSerializationHelper.SerializeToFile(m_translationsFile,
				(from translatablePhrase in m_helper.UnfilteredPhrases
				 where translatablePhrase.HasUserTranslation
				 select new UnsTranslation(translatablePhrase)).ToList());
			m_lastSaveTime = DateTime.Now;
			SaveNeeded = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the closeToolStripMenuItem control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the mnuGenerateTemplate control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void mnuGenerateTemplate_Click(object sender, EventArgs e)
		{
			using (GenerateTemplateDlg dlg = new GenerateTemplateDlg(m_projectName,
				GenTemplateSettings, AvailableBookIds))
			{
				dlg.m_lblFolder.Text = m_defaultLcfFolder;
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					GenTemplateSettings = dlg.Settings;

					Func<int, bool> InRange;
					if (dlg.m_rdoWholeBook.Checked)
					{
						int bookNum = BCVRef.BookToNumber((string)dlg.m_cboBooks.SelectedItem);
						InRange = (bcv) =>
						{
							return BCVRef.GetBookFromBcv(bcv) == bookNum;
						};
					}
					else
					{
						throw new NotImplementedException();
					}

					List<TranslatablePhrase> allPhrasesInRange = m_helper.UnfilteredPhrases.Where(tp => tp.Category > -1 && InRange(tp.StartRef)).ToList();
					if (dlg.m_rdoDisplayWarning.Checked)
					{
						int untranslatedQuestions = allPhrasesInRange.Count(p => !p.HasUserTranslation);
						if (untranslatedQuestions > 0 &&
							MessageBox.Show(string.Format(Properties.Resources.kstidUntranslatedQuestionsWarning, untranslatedQuestions),
							m_appName, MessageBoxButtons.YesNo) == DialogResult.No)
						{
							return;
						}
					}
					using (StreamWriter sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
					{
						sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
						sw.WriteLine("<html>");
						sw.WriteLine("<head>");
						sw.WriteLine("<meta content=\"text/html; charset=UTF-8\" http-equiv=\"content-type\"/>");
						sw.WriteLine("<title>" + dlg.m_txtTitle.Text.Normalize(NormalizationForm.FormC) + "</title>");
						if (!dlg.m_rdoEmbedStyleInfo.Checked)
						{
							sw.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href= \"" + dlg.CssFile + "\"/>");
							if (dlg.WriteCssFile)
							{
								if (dlg.m_chkOverwriteCss.Checked)
								{
									using (StreamWriter css = new StreamWriter(dlg.FullCssPath))
									{
										WriteCssStyleInfo(css, dlg.m_lblQuestionGroupHeadingsColor.ForeColor,
											dlg.m_lblEnglishQuestionColor.ForeColor, dlg.m_lblEnglishAnswerTextColor.ForeColor,
											dlg.m_lblCommentTextColor.ForeColor, (int)dlg.m_numBlankLines.Value,
											dlg.m_chkNumberQuestions.Checked);
									}
								}
							}
						}

						sw.WriteLine("<style type=\"text/css\">");
						// This CSS directive always gets written directly to the template file because it's
						// important to get right and it's unlikely that someone will want to do a global override.
						sw.WriteLine(":lang(" + m_vernIcuLocale + ") {font-family:serif," +
							m_colTranslation.DefaultCellStyle.Font.FontFamily.Name + ",Arial Unicode MS;}");
						if (dlg.m_rdoEmbedStyleInfo.Checked)
						{
							WriteCssStyleInfo(sw, dlg.m_lblQuestionGroupHeadingsColor.ForeColor,
								dlg.m_lblEnglishQuestionColor.ForeColor, dlg.m_lblEnglishAnswerTextColor.ForeColor,
								dlg.m_lblCommentTextColor.ForeColor, (int)dlg.m_numBlankLines.Value,
								dlg.m_chkNumberQuestions.Checked);
						}
						sw.WriteLine("</style>");
						sw.WriteLine("</head>");
						sw.WriteLine("<body lang=\"" + m_vernIcuLocale + "\">");
						sw.WriteLine("<h1 lang=\"en\">" + dlg.m_txtTitle.Text.Normalize(NormalizationForm.FormC) + "</h1>");
						int prevCategory = -1;
						string prevSectionRef = null;
						string prevQuestionRef = null;
						string pendingSectionHead = null;

						foreach (TranslatablePhrase phrase in allPhrasesInRange)
						{
							if (phrase.Category == 0 && prevSectionRef != phrase.Reference)
							{
								if (!m_sectionHeadText.TryGetValue(phrase.Reference, out pendingSectionHead))
									pendingSectionHead = phrase.Reference;
								prevCategory = -1;
							}
							prevSectionRef = phrase.Reference;

							if (!phrase.HasUserTranslation && !dlg.m_rdoUseOriginal.Checked)
								continue; // skip this question

							if (pendingSectionHead != null)
							{
								sw.WriteLine("<h2 lang=\"en\">" + pendingSectionHead.Normalize(NormalizationForm.FormC) + "</h2>");
								pendingSectionHead = null;
							}

							if (phrase.Category != prevCategory)
							{
								sw.WriteLine("<h3>" + phrase.CategoryName.Normalize(NormalizationForm.FormC) + "</h3>");
								prevCategory = phrase.Category;
							}

							if (prevQuestionRef != phrase.Reference)
							{
								if (phrase.Category > 0 || dlg.m_chkPassageBeforeOverview.Checked)
								{
									sw.WriteLine("<p class=\"scripture\">");
									sw.WriteLine(@"\ref " + BCVRef.MakeReferenceString(phrase.StartRef, phrase.EndRef, ".", "-"));
									sw.WriteLine("</p>");
								}
								prevQuestionRef = phrase.Reference;
							}

							sw.WriteLine("<p class=\"question\">" +
								(phrase.HasUserTranslation ? phrase.Translation : phrase.OriginalPhrase).Normalize(NormalizationForm.FormC) + "</p>");

							sw.WriteLine("<div class=\"extras\" lang=\"en\">");
							if (dlg.m_chkEnglishQuestions.Checked && phrase.HasUserTranslation)
								sw.WriteLine("<p class=\"questionbt\">" + phrase.OriginalPhrase.Normalize(NormalizationForm.FormC) + "</p>");
							AnswersAndComments answersAndComments = (AnswersAndComments)phrase.AdditionalInfo[0];
							if (dlg.m_chkEnglishAnswers.Checked && answersAndComments.HasAnswer)
							{
								foreach (string answer in answersAndComments.Answers)
									sw.WriteLine("<p class=\"answer\">" + answer.Normalize(NormalizationForm.FormC) + "</p>");
							}
							if (dlg.m_chkIncludeComments.Checked && answersAndComments.HasComment)
							{
								foreach (string comment in answersAndComments.Comments)
									sw.WriteLine("<p class=\"comment\">" + comment.Normalize(NormalizationForm.FormC) + "</p>");
							}
							sw.WriteLine("</div>");
						}

						sw.WriteLine("</body>");
					}
					MessageBox.Show(Properties.Resources.kstidTemplateGenerationComplete);
				}
			}
		}

		private void WriteCssStyleInfo(StreamWriter sw, Color questionGroupHeadingsClr,
			Color englishQuestionClr, Color englishAnswerClr, Color commentClr, int cBlankLines, bool fNumberQuestions)
		{
			if (fNumberQuestions)
			{
				sw.WriteLine("body {font-size:100%; counter-reset:qnum;}");
				sw.WriteLine(".question {counter-increment:qnum;}");
				sw.WriteLine("p.question:before {content:counter(qnum) \". \";}");
			}
			else
				sw.WriteLine("body {font-size:100%;}");
			sw.WriteLine("h1 {font-size:2.0em;");
			sw.WriteLine("  text-align:center}");
			sw.WriteLine("h2 {font-size:1.7em;");
			sw.WriteLine("  color:white;");
			sw.WriteLine("  background-color:black;}");
			sw.WriteLine("h3 {font-size:1.3em;");
			sw.WriteLine("  color:blue;}");
			sw.WriteLine("p {font-size:1.0em;}");
			sw.WriteLine("h1:lang(en) {font-family:sans-serif;}");
			sw.WriteLine("h2:lang(en) {font-family:serif;}");
			sw.WriteLine("p:lang(en) {font-family:serif;");
			sw.WriteLine("font-size:0.85em;}");
			sw.WriteLine("h3 {color:" + questionGroupHeadingsClr.Name + ";}");
			sw.WriteLine(".questionbt {color:" + englishQuestionClr.Name + ";}");
			sw.WriteLine(".answer {color:" + englishAnswerClr.Name + ";}");
			sw.WriteLine(".comment {color:" + commentClr.Name + ";}");
			sw.WriteLine(".extras {margin-bottom:" + cBlankLines + "em;}");
		}

		private void mnuViewToolbar_CheckedChanged(object sender, EventArgs e)
		{
			toolStrip1.Visible = mnuViewToolbar.Checked;
			if (toolStrip1.Visible)
				m_mainMenu.SendToBack(); // this makes the toolbar appear below the menu
		}
		#endregion

		private void mnuAutoSave_CheckedChanged(object sender, EventArgs e)
		{
			if (mnuAutoSave.Checked && SaveNeeded)
				Save(sender, e);
		}
	}
	#endregion
}