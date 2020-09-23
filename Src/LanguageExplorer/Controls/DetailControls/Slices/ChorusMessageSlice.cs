// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Chorus;
using Chorus.UI.Notes.Bar;
using LanguageExplorer.SendReceive;
using SIL.LCModel;
using SIL.Progress;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary>
	/// This slice supports showing the Chorus notes messages for a particular entry.
	/// </summary>
	internal sealed class ChorusMessageSlice : Slice
	{
		private ChorusSystem _chorusSystem;
		private NotesBarView _notesBarView;

		/// <summary>
		/// The user that we want MessageSlice (and FLExBridge) to consider to be the current user,
		/// for the purposes of identifying the source of Send/Receive changes and Notes.
		/// Enhance JohnT: We would like to get the current user name stored in the Mercurial INI file
		/// (see HgRepository.SetUserNameInIni/GetUserNameFromIni). But until we merge FlexBridge,
		/// FLEx does not have access to code that knows about Mercurial.
		/// </summary>
		internal static string SendReceiveUser => CommonBridgeServices.SendReceiveUser;

		/// <summary />
		public override void FinishInit()
		{
			_chorusSystem = new ChorusSystem(Cache.ProjectId.ProjectFolder);
			_chorusSystem.InitWithoutHg(SendReceiveUser);
			// This is a required object for CreateNotesBar. It specifies delegates for getting the information
			// the bar requires about the current object.
			var notesToRecordMapping = new NotesToRecordMapping
			{
				FunctionToGetCurrentUrlForNewNotes = GetCurrentUrlForNewNotes,
				FunctionToGoFromObjectToItsId = GetIdForObject,
				FunctionToGoFromObjectToAdditionalIds = GetAdditionalIdsForObject
			};
			var dataFilePath = GetDataFilePath(Cache);
			var additionalPaths = GetAdditionalLexiconFilePaths(Cache);
			const string idAttrForOtherFiles = "guid"; // .lexdb chorus notes files identify FLEx object with a url attr of "guid".
			_notesBarView = _chorusSystem.WinForms.CreateNotesBar(dataFilePath, additionalPaths, idAttrForOtherFiles, notesToRecordMapping, new NullProgress());
			_notesBarView.SetTargetObject(MyCmObject);
			// Set the writing systems for the NoteDetailDialog.  (See FWNX-1239.)
			var vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			var labelWs = new ChorusWritingSystem(vernWs.LanguageName, vernWs.Id, vernWs.DefaultFontName, 12);
			_notesBarView.LabelWritingSystem = labelWs;
			var analWs = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			var msgWs = new ChorusWritingSystem(analWs.LanguageName, analWs.Id, analWs.DefaultFontName, 12);
			_notesBarView.MessageWritingSystem = msgWs;
			Control = _notesBarView;
		}

		// The notes bar expects to store notes about a particular file. Our notes are currently about the lexicon,
		// which is just part of the .fwdata file. We don't want to use that, because it can be renamed, and
		// using that name as the thing to annotate would just mean one more file to rename with the project
		// and in the process confuse Mercurial. So we have a dummy file to annotate. We don't S/R this file,
		// since it has no unpredictable content; we just create it any time this slice wants it, if it does
		// not already exist. The content does not matter; it is just a hint of the file purpose in case
		// someone finds it in a browser.
		private static string GetDataFilePath(LcmCache cache)
		{
			var dataFilePath = Path.Combine(cache.ProjectId.ProjectFolder, SendReceiveMenuManager.FakeLexiconFileName);
			if (!File.Exists(dataFilePath))
			{
				using (var writer = new StreamWriter(dataFilePath, false, Encoding.UTF8))
				{
					writer.WriteLine("This is a stub file to provide an attachment point for " + SendReceiveMenuManager.FlexLexiconNotesFileName);
				}
			}
			return dataFilePath;
		}

		/// <summary>
		/// Gets additional files that have annotation files containing notes about lexicon conflicts.
		/// Note that we return the files (e.g., Lexicon_04.lexdb) that contain the lexical data,
		/// not the files (e.g., Lexicon_04.lexdb.ChorusNotes) that contain the notes.
		/// We do check for the existence of the ChorusNotes files (though I think the NotesBar would
		/// handle their absence itself) just as a performance optimization.
		/// </summary>
		private static IEnumerable<string> GetAdditionalLexiconFilePaths(LcmCache cache)
		{
			var results = new List<string>();
			var lexiconFolder = Path.Combine(cache.ProjectId.ProjectFolder, "Linguistics", "Lexicon");
			if (!Directory.Exists(lexiconFolder))
			{
				return results;
			}
			foreach (var path in Directory.EnumerateFiles(lexiconFolder, "*.lexdb"))
			{
				var notesFile = path + CommonBridgeServices.kChorusNotesExtension;
				if (File.Exists(notesFile))
				{
					results.Add(path);
				}
			}
			return results;
		}

		private string GetCurrentUrlForNewNotes(object dataItemInFocus, string escapedId)
		{
			// In this URI, the stuff up to tag=& is the part that allows FLEx to switch to the right object from the notes browser.
			// the ID is the thing that allows the NotesBar to select the right notes to display for a particular entry.
			// the Label is the title under which the note is shown in the Note creation window and notes browser.
			return $"silfw://localhost/link?app=flex&database=current&server=&tool=default&guid={MyCmObject.Guid}&tag=&id={MyCmObject.Guid}&label={MyCmObject.ShortName}";
		}

		private static string GetIdForObject(object targetOfNote)
		{
			return ((ICmObject)targetOfNote).Guid.ToString().ToLowerInvariant();
		}

		private static IEnumerable<string> GetAdditionalIdsForObject(object targetOfNote)
		{
			return ((ICmObject)targetOfNote).AllOwnedObjects.Select(t => t.Guid.ToString().ToLowerInvariant());
		}

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
				_chorusSystem?.Dispose();
				// _notesBarView is stored in Control, which is disposed by base.Dispose.
			}

			_chorusSystem = null;
			_notesBarView = null;

			base.Dispose(disposing);
		}
	}
}
