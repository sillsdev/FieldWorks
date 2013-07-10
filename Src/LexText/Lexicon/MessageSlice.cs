using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Chorus;
using Chorus.UI.Notes.Bar;
using Palaso.Progress;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// This slice supports showing the Chorus notes messages for a particular entry.
	/// </summary>
	public class MessageSlice : Slice
	{
		private ChorusSystem m_chorusSystem;
		NotesBarView m_notesBar;

		/// <summary>
		/// The user that we want MessageSlice (and FLExBridge) to consider to be the current user,
		/// for the purposes of identifying the source of Send/Receive changes and Notes.
		/// Enhance JohnT: We would like to get the current user name stored in the Mercurial INI file
		/// (see HgRepository.SetUserNameInIni/GetUserNameFromIni). But until we merge FlexBridge,
		/// FLEx does not have access to code that knows about Mercurial.
		/// </summary>
		public static string SendReceiveUser
		{
			get { return FLExBridgeListener.SendReceiveUser; }
		}

		public override void FinishInit()
		{
#if __MonoCS__
			try
			{
#endif
			m_chorusSystem = new ChorusSystem(Cache.ProjectId.ProjectFolder);
			m_chorusSystem.InitWithoutHg(SendReceiveUser);
			// This is a required object for CreateNotesBar. It specifies delegates for getting the information
			// the bar requires about the current object.
			var notesToRecordMapping = new NotesToRecordMapping()
				{
					FunctionToGetCurrentUrlForNewNotes = GetCurrentUrlForNewNotes,
					FunctionToGoFromObjectToItsId = GetIdForObject
				};
			var dataFilePath = GetDataFilePath(Cache);
			m_notesBar = m_chorusSystem.WinForms.CreateNotesBar(dataFilePath, notesToRecordMapping, new NullProgress());
			m_notesBar.SetTargetObject(m_obj);
			this.Control = m_notesBar;
#if __MonoCS__
			}
			catch (Exception ex)
			{
				// This does not work yet on Linux, but shouldn't keep the rest of
				// the lexicon edit tool from working!  (See FWNX-960.)
				Console.WriteLine("Initializing Chorus UI element failed: {0}", ex.Message);
			}
#endif
		}

		// The notes bar expects to store notes about a particular file. Our notes are currently about the lexicon,
		// which is just part of the .fwdata file. We don't want to use that, because it can be renamed, and
		// using that name as the thing to annotate would just mean one more file to rename with the project
		// and in the process confuse Mercurial. So we have a dummy file to annotate. We don't S/R this file,
		// since it has no unpredictable content; we just create it any time this slice wants it, if it does
		// not already exist. The content does not matter; it is just a hint of the file purpose in case
		// someone finds it in a browser.
		private static string GetDataFilePath(FdoCache cache)
		{
			var dataFilePath = Path.Combine(cache.ProjectId.ProjectFolder, FLExBridgeListener.FakeLexiconFileName);
			if (!File.Exists(dataFilePath))
			{
				using (var writer = new StreamWriter(dataFilePath, false, Encoding.UTF8))
				{
					writer.WriteLine("This is a stub file to provide an attachment point for " +
						FLExBridgeListener.FlexLexiconNotesFileName);
				}
			}
			return dataFilePath;
		}

		private string GetCurrentUrlForNewNotes(object dataItemInFocus, string escapedId)
		{
			// In this URI, the stuff up to tag=& is the part that allows FLEx to switch to the right object from the notes browser.
			// the ID is the thing that allows the NotesBar to select the right notes to display for a particular entry.
			// the Label is the title under which the note is shown in the Note creation window and notes browser.
			return string.Format("silfw://localhost/link?app=flex&database=current&server=&tool=default&guid={0}&tag=&id={0}&label={1}", m_obj.Guid, m_obj.ShortName);
		}

		private static string GetIdForObject(object targetOfNote)
		{
			return ((ICmObject)targetOfNote).Guid.ToString().ToLowerInvariant();
		}

		// Used in ShowSliceForVisibleData. It will never display the control to which it passes this,
		// so it will never need the URL for a new annotation.
		private static string DummyGetCurrentUrlForNewNotes(object dataItemInFocus, string escapedId)
		{
			return "";
		}

		/// <summary>
		/// Determine if the object really has data to be shown in the slice. This method is called by reflection
		/// from DataTree.AddSimpleNode, to determine whether to create the slice when visibility is "ifdata".
		/// </summary>
		/// <param name="obj">object to check; should be an ILexEntry</param>
		/// <returns>true if there are chorus notes for this object; false otherwise</returns>
		public static bool ShowSliceForVisibleIfData(XmlNode node, ICmObject obj)
		{
			using (var chorusSystem = new ChorusSystem(obj.Cache.ProjectId.ProjectFolder))
			{
				chorusSystem.InitWithoutHg(SendReceiveUser);
				// This is a required object for CreateNotesBar. It specifies delegates for getting the information
				// the bar requires about the current object. For this model the FunctionToGetCurrentUrlForNewNotes will not be used.
				var notesToRecordMapping = new NotesToRecordMapping()
				{
					FunctionToGetCurrentUrlForNewNotes = DummyGetCurrentUrlForNewNotes,
					FunctionToGoFromObjectToItsId = GetIdForObject
				};
				var dataFilePath = GetDataFilePath(obj.Cache);
				var notesmodel = chorusSystem.WinForms.CreateNotesBarModel(dataFilePath, notesToRecordMapping, new NullProgress());
				notesmodel.SetTargetObject(obj);
				return notesmodel.GetAnnotationsToShow().Any();
			}
		}
	}
}
