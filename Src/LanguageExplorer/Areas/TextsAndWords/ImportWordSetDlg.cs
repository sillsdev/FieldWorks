// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Impls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.FileDialog;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary />
	internal sealed class ImportWordSetDlg : Form
	{
		private LcmCache Cache { get; set; }
		private IHelpTopicProvider HelpTopicProvider { get; set; }
		private IRecordList MyRecordList { get; set; }
		private ParserMenuManager ParserMenuManager { get; set; }

		#region Data members
		private string[] _paths;
		private Label label1;
		private Button btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private Label label2;
		private Button btnChooseFiles;
		private TextBox tbFileNames;
		private TextBox tbName;
		private Button buttonHelp;
		private Button btnImport;
		private const string s_helpTopic = "khtpImportWordSet";
		private HelpProvider helpProvider;

		#endregion Data members

		private ImportWordSetDlg()
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		public ImportWordSetDlg(LcmCache cache, IHelpTopicProvider helpTopicProvider, IRecordList recordList, ParserMenuManager parserMenuManager)
			: this()
		{
			Cache = cache;
			HelpTopicProvider = helpTopicProvider;
			MyRecordList = recordList;
			ParserMenuManager = parserMenuManager;
			helpProvider = new HelpProvider
			{
				HelpNamespace = HelpTopicProvider.HelpFile
			};
			helpProvider.SetHelpKeyword(this, HelpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		/// <inheritdoc />
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
				components?.Dispose();
			}
			Cache = null;
			HelpTopicProvider = null;
			MyRecordList = null;
			ParserMenuManager = null;

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportWordSetDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.btnImport = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.tbName = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnChooseFiles = new System.Windows.Forms.Button();
			this.tbFileNames = new System.Windows.Forms.TextBox();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// btnImport
			//
			resources.ApplyResources(this.btnImport, "btnImport");
			this.btnImport.Name = "btnImport";
			this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			//
			// tbName
			//
			resources.ApplyResources(this.tbName, "tbName");
			this.tbName.Name = "tbName";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// btnChooseFiles
			//
			resources.ApplyResources(this.btnChooseFiles, "btnChooseFiles");
			this.btnChooseFiles.Name = "btnChooseFiles";
			this.btnChooseFiles.Click += new System.EventHandler(this.btnChooseFiles_Click);
			//
			// tbFileNames
			//
			resources.ApplyResources(this.tbFileNames, "tbFileNames");
			this.tbFileNames.Name = "tbFileNames";
			this.tbFileNames.ReadOnly = true;
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// ImportWordSetDlg
			//
			this.AcceptButton = this.btnImport;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.tbFileNames);
			this.Controls.Add(this.tbName);
			this.Controls.Add(this.btnChooseFiles);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnImport);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ImportWordSetDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void btnImport_Click(object sender, EventArgs e)
		{
			if (_paths == null)
			{
				MessageBox.Show(ParserUIStrings.ksNoFilesToImport, ParserUIStrings.ksNoFiles, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			ParserMenuManager.DisconnectFromParser();
			CreateWordsetFromFiles(_paths);
			MyRecordList.ReloadFilterProvider();
			DialogResult = DialogResult.OK;
		}

		private void btnChooseFiles_Click(object sender, System.EventArgs e)
		{
			using (IOpenFileDialog dlg = new OpenFileDialogAdapter())
			{
				dlg.Multiselect = true;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.Text);
				if (dlg.ShowDialog(this) != DialogResult.OK)
				{
					return;
				}
				tbFileNames.Lines = dlg.FileNames;
				_paths = dlg.FileNames;
			}
		}
		/// <summary>
		/// Parse the given lists of files and create a wordset from them.
		/// </summary>
		/// <param name="paths"></param>
		/// <remarks>This is marked internal so that unit tests can call it</remarks>
		internal void CreateWordsetFromFiles(string[] paths)
		{
			using (var dlg = new ProgressDialogWorkingOn())
			{
				Cache.DomainDataByFlid.BeginUndoTask("Import Word Set", "Import Word Set");
				var sWordSetName = GetWordSetName(paths);
				var wordSet = Cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
				Cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wordSet);
				wordSet.Name.SetAnalysisDefaultWritingSystem(sWordSetName);
				wordSet.Description.SetAnalysisDefaultWritingSystem(GetWordSetDescription(paths));
				dlg.Owner = FindForm();
				dlg.Icon = dlg.Owner.Icon;
				dlg.Minimum = 0;
				dlg.Maximum = paths.Length;
				dlg.Text = string.Format(ParserUIStrings.ksLoadingFilesForWordSetX, sWordSetName);
				dlg.Show();
				dlg.BringToFront();
				var importer = new WordImporter(Cache);
				foreach (var path in paths)
				{
					UpdateProgress(path, dlg);
					importer.PopulateWordset(path, wordSet);
				}
				Cache.DomainDataByFlid.EndUndoTask();
				dlg.Close();
			}
		}

		private void UpdateProgress(string sMessage, ProgressDialogWorkingOn dlg)
		{
			dlg.WorkingOnText = sMessage;
			dlg.PerformStep();
			dlg.Refresh();
		}

		private string GetWordSetName(string[] paths)
		{
			var sWordSetName = tbName.Text;
			if (sWordSetName.Length == 0)
			{
				sWordSetName = System.IO.Path.GetFileName(paths[0]); // use first file name if user doesn't give one
			}
			return sWordSetName;
		}

		private static string GetWordSetDescription(IReadOnlyList<string> paths)
		{
			// REVIEW: SHOULD THE LIST BUILDING BE LOCALIZED SOMEHOW?
			var sb = new StringBuilder();
			for (var i = 0; i < paths.Count; i++)
			{
				if (i > 0)
				{
					sb.Append(", ");
				}
				sb.Append(paths[i]);
			}
			return string.Format(paths.Count > 1 ? ParserUIStrings.ksImportedFromFilesX : ParserUIStrings.ksImportedFromFileX, sb);
		}

		protected override void OnActivated(EventArgs ea)
		{
			base.OnActivated(ea);
			tbName.Focus();
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(HelpTopicProvider, s_helpTopic);
		}

		/// <summary>
		/// a class for parsing text files and populating WfiWordSets with them.
		/// </summary>
		private sealed class WordImporter
		{
			private readonly LcmCache m_cache;
			private readonly CoreWritingSystemDefinition m_ws;

			internal WordImporter(LcmCache cache)
			{
				m_cache = cache;
				m_ws = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			}

			internal void PopulateWordset(string path, IWfiWordSet wordSet)
			{
				// Note: The ref collection class ensures duplicates are not added to CasesRC.
				foreach (var wordform in GetWordformsInFile(path))
				{
					wordSet.CasesRC.Add(wordform);
				}
			}

			// NB: This will currently return hvos
			private IEnumerable<IWfiWordform> GetWordformsInFile(string path)
			{
				// Note: The GetUniqueWords uses the key for faster processing,
				// even though we don;t use it here.
				var wordforms = new Dictionary<string, IWfiWordform>();
				using (TextReader reader = File.OpenText(path))
				{
					// do timing tests; try doing readtoend() to one string and then parsing that; see if more efficient
					// While there is a line to be read in the file
					// REVIEW: can this be broken by a very long line?
					// RR answer: Yes it can.
					// According to the ReadLine docs an ArgumentOutOfRangeException
					// exception will be thrown if the line length exceeds the MaxValue constant property of an Int32,
					// which is 2,147,483,647. I doubt you will run into this exception any time soon. :-)
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						GetUniqueWords(wordforms, line);
					}
				}
				return wordforms.Values;
			}

			/// <summary>
			/// Collect up a set of unique WfiWordforms.
			/// </summary>
			/// <param name="wordforms">Table of unique wordforms.</param>
			/// <param name="buffer"></param>
			private void GetUniqueWords(Dictionary<string, IWfiWordform> wordforms, string buffer)
			{
				var start = -1; // -1 means we're still looking for a word to start.
				var length = 0;
				var totalLengh = buffer.Length;
				for (var i = 0; i < totalLengh; i++)
				{
					var isWordforming = m_ws.get_IsWordForming(buffer[i]);
					if (isWordforming)
					{
						length++;
						if (start < 0) //first character in this word?
						{
							start = i;
						}
					}
					if ((start <= -1) || (isWordforming && i != totalLengh - 1))
					{
						continue;
					}
					var word = buffer.Substring(start, length);
					if (!wordforms.ContainsKey(word))
					{
						wordforms.Add(word, WfiWordformServices.FindOrCreateWordform(m_cache, TsStringUtils.MakeString(word, m_ws.Handle)));
					}
					length = 0;
					start = -1;
				}
			}
		}
	}
}