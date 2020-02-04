// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.SendReceive
{
	internal static class CommonBridgeServices
	{
		/// <summary>
		/// Extension for Chorus notes files.
		/// </summary>
		internal const string kChorusNotesExtension = ".ChorusNotes";

		internal const string FLExBridge = "FLExBridge";

		internal const string LiftBridge = "LiftBridge";

		internal const string NoBridgeUsedYet = "NoBridgeUsedYet";

		internal const string LastBridgeUsed = "LastBridgeUsed";

		// For send/receive involving LIFT projects, use the lift version "0.13_ldml3" so the version 3 ldml files will exist on a different chorus branch
		internal const string LiftModelVersion = "0.13_ldml3";

		internal static bool ShowMessageBeforeFirstSendReceive_IsUserReady(IHelpTopicProvider helpTopicProvider)
		{
			using (var firstTimeDlg = new FLExBridgeFirstSendReceiveInstructionsDlg(helpTopicProvider))
			{
				return DialogResult.OK == firstTimeDlg.ShowDialog();
			}
		}

		internal static string GetFullProjectFileName(LcmCache cache)
		{
			return Path.Combine(cache.ProjectId.ProjectFolder, cache.ProjectId.Name + LcmFileHelper.ksFwDataXmlFileExtension);
		}

		internal static void PrepareForSR(IPropertyTable propertyTable, IPublisher publisher, LcmCache cache, IBridge lastBridgeUsed)
		{
			//Make sure any last changes are saved. (Process focus lost for controls)
			Application.DoEvents();
			StopParser(publisher);
			//Give all forms the opportunity to save any uncommitted data
			//(important for analysis sandboxes)
			var activeForm = propertyTable.GetValue<Form>(FwUtils.window);
			activeForm?.ValidateChildren(ValidationConstraints.Enabled);
			//Commit all the data in the cache and save to disk
			ProjectLockingService.UnlockCurrentProject(cache);
			propertyTable.SetProperty(LastBridgeUsed, lastBridgeUsed.Name, true, settingsGroup: SettingsGroup.LocalSettings);
		}

		internal static void StopParser(IPublisher publisher)
		{
			publisher.Publish(LanguageExplorerConstants.StopParser, null);
		}

		/// <summary>Callback to refresh the Message Slice after OnView[Lift]Messages</summary>
		internal static void BroadcastMasterRefresh(IPublisher publisher)
		{
			publisher.Publish("MasterRefresh", null);
		}

		internal static void PublishHandleLocalHotlinkMessage(IPublisher publisher, object sender, FLExJumpEventArgs e)
		{
			if (!String.IsNullOrEmpty(e.JumpUrl))
			{
				publisher.Publish(FwUtils.HandleLocalHotlink, new LocalLinkArgs { Link = e.JumpUrl });
			}
		}

		internal static string SendReceiveUser => Environment.UserName;

		internal static void ReportDuplicateBridge()
		{
			ObtainProjectMethod.ReportDuplicateBridge();
		}

		internal static bool DetectMainConflicts(string path, IReadOnlyDictionary<string, long> savedState)
		{
			foreach (var file in Directory.GetFiles(path, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				// TODO: Test to see if one conflict tool can do both FLEx and LIFT conflicts.
				if (file.Contains(LcmFileHelper.OtherRepositories))
				{
					continue; // Skip them, since they are part of some other repository.
				}
				long oldLength;
				savedState.TryGetValue(file, out oldLength);
				if (new FileInfo(file).Length == oldLength)
				{
					continue; // no new notes in this file.
				}
				return true; // Review JohnT: do we need to look in the file to see if what was added is a conflict?
			}
			return false; // no conflicts added.
		}

		internal static void RefreshCacheWindowAndAll(IFieldWorksManager manager, string fullProjectFileName, string publisherMessage, bool conflictOccurred)
		{
			var appArgs = new FwAppArgs(fullProjectFileName);
			var newAppWindow = (IFwMainWnd)manager.ReopenProject(manager.Cache.ProjectId.Name, appArgs).ActiveMainWindow;
			if (newAppWindow.PropertyTable.GetValue(LanguageExplorerConstants.UseVernSpellingDictionary, true))
			{
				WfiWordformServices.ConformSpellingDictToWordforms(newAppWindow.Cache);
			}
			// Clear out any sort cache files (or whatever else might mess us up) and then refresh
			newAppWindow.ClearInvalidatedStoredData();
			newAppWindow.RefreshAllViews();
			if (conflictOccurred)
			{
				// Send a message for the reopened instance to display the message viewer (used to be conflict report).
				// Caller has been disposed by now.
				newAppWindow.Publisher.Publish(publisherMessage, null);
			}
		}

		internal static bool IsConfiguredForSR(string projectFolder)
		{
			return Directory.Exists(Path.Combine(projectFolder, ".hg"));
		}

		/// <summary>
		/// Returns true if there are any Chorus Notes to view in the main FW repo or in the Lift repo.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="checkForLiftNotes">
		/// When 'false', then don't consider any Lift notes files in considering those present.
		/// When 'true', then skip any Flex notes, and only consider the Lift notes.
		/// </param>
		/// <returns>'true' if there are any Chorus Notes files at the given level. Otherwise, it returns 'false'.</returns>
		internal static bool NotesFileIsPresent(LcmCache cache, bool checkForLiftNotes)
		{
			// Default to look for notes in the main FW repo.
			var folderToSearchIn = cache.ProjectId.ProjectFolder;
			var liftFolder = GetLiftRepositoryFolderFromFwProjectFolder(folderToSearchIn);
			if (checkForLiftNotes)
			{
				if (!Directory.Exists(liftFolder))
				{
					return false; // If the folder doesn't even exist, there can't be any lift notes.
				}

				// Switch to look for note files in the Lift repo.
				folderToSearchIn = liftFolder;
			}
			if (!Directory.Exists(Path.Combine(folderToSearchIn, ".hg")))
			{
				return false; // No repo, so there can be no notes files.
			}
			foreach (var notesPathname in Directory.GetFiles(folderToSearchIn, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				if (!NotesFileHasContent(notesPathname) || checkForLiftNotes)
				{
					continue; // Skip ones with no content.
				}
				if (checkForLiftNotes || !notesPathname.Contains(liftFolder)/* Skip any lift ones down in a nested repo. */)
				{
					return true;
				}
				// Must be a nested lift one to get here, so try another one.
			}
			return false;
		}

		private static bool NotesFileHasContent(string chorusNotesPathname)
		{
			var doc = XDocument.Load(chorusNotesPathname);
			return doc.Root.HasElements; // Files with no notes (e.g., "Lexicon.fwstub.ChorusNotes") are not interesting.
		}

		/// <summary />
		internal static string GetLiftRepositoryFolderFromFwProjectFolder(string projectFolder)
		{
			var otherDir = Path.Combine(projectFolder, LcmFileHelper.OtherRepositories);
			if (Directory.Exists(otherDir))
			{
				var extantOtherFolders = Directory.GetDirectories(otherDir);
				var extantLiftFolder = extantOtherFolders.FirstOrDefault(folder => folder.EndsWith("_LIFT"));
				if (extantLiftFolder != null)
				{
					return extantLiftFolder; // Reuse the old one, no matter what the new project dir name is.
				}
			}
			var flexProjName = Path.GetFileName(projectFolder);
			return Path.Combine(projectFolder, LcmFileHelper.OtherRepositories, flexProjName + '_' + FLExBridgeHelper.LIFT);
		}

		/// <summary>
		/// Convert FLEx ChorusNotes file referencing lex entries to LIFT notes by adjusting the "ref" attributes.
		/// </summary>
		/// <remarks>
		/// This method is internal, rather than static to let a test call it.
		/// </remarks>
		internal static void ConvertFlexNotesToLift(TextReader reader, TextWriter writer, string liftFileName)
		{
			// Typical input is something like
			// silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=bab7776e-531b-4ce1-997f-fa638c09e381&amp;tag=&amp;id=bab7776e-531b-4ce1-997f-fa638c09e381&amp;label=Entry &quot;pintu&quot;
			// produce: lift://John.lift?type=entry&amp;label=fox&amp;id=f3093b9b-ea2f-422b-86b6-0defaa4646fe
			ConvertRefAttrs(reader, writer, liftFileName, "lift://{0}?type=entry&amp;label={1}&amp;id={2}");
		}

		/// <summary>
		/// Convert LIFT ChorusNotes file to FLEx notes by adjusting the "ref" attributes.
		/// </summary>
		/// <remarks>
		/// This method is internal, rather than static to let a test call it.
		/// </remarks>
		internal static void ConvertLiftNotesToFlex(TextReader reader, TextWriter writer)
		{
			// produce: silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=bab7776e-531b-4ce1-997f-fa638c09e381&amp;tag=&amp;id=bab7776e-531b-4ce1-997f-fa638c09e381&amp;label=Entry &quot;pintu&quot;
			ConvertRefAttrs(reader, writer, String.Empty, "silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid={2}&amp;tag=&amp;id={2}&amp;label={1}");
		}

		private static void ConvertRefAttrs(TextReader reader, TextWriter writer, string liftFileName, string outputTemplate)
		{
			// Typical input is something like
			// silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=bab7776e-531b-4ce1-997f-fa638c09e381&amp;tag=&amp;id=bab7776e-531b-4ce1-997f-fa638c09e381&amp;label=Entry &quot;pintu&quot;
			// or: lift://John.lift?type=entry&amp;label=fox&amp;id=f3093b9b-ea2f-422b-86b6-0defaa4646fe
			// both contain id=...&amp; and label=...&amp. One may be at the end without following &amp;.
			// Note that the ? is essential to prevent the greedy match including multiple parameters.
			// A label may contain things like &quot; so we can't just search for [^&]*.
			var reOuter = new Regex("ref=\\\"([^\\\"]*)\"");
			var reLabel = new Regex("label=(.*?)(&amp;|$)");
			var reId = new Regex("id=(.*?)(&amp;|$)");
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				var matchLine = reOuter.Match(line);
				if (matchLine.Success)
				{
					var input = matchLine.Groups[1].Value;
					var matchLabel = reLabel.Match(input);
					var matchId = reId.Match(input);
					if (matchLabel.Success && matchId.Success)
					{
						var guid = matchId.Groups[1].Value;
						var label = matchLabel.Groups[1].Value;
						var output = String.Format(outputTemplate, liftFileName, label, guid);
						writer.WriteLine(line.Replace(input, output));
						continue;
					}
				}
				writer.WriteLine(line);
			}
		}
	}
}