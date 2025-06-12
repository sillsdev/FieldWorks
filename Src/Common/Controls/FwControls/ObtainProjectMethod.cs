// Copyright (c) 2015-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using DesktopAnalytics;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Method class that handles the 'obtain' FLEx Bridge option, which gets some repository (currently Lift or FW).
	/// The class will create a FLEx project from a lift repo if that repo is selected.
	/// </summary>
	public static class ObtainProjectMethod
	{
		/// <summary>
		/// Get a new FW project from some Mercurial repository.
		/// The repo may be a lift or full FW repo, but it can be from any source source, as long as the code can create an FW project from it.
		/// </summary>
		/// <returns>Null if the operation was cancelled or otherwise did not work. The full pathname of an fwdata file, if it did work.</returns>
		public static string ObtainProjectFromAnySource(Form parent, IHelpTopicProvider helpTopicProvider, out ObtainedProjectType obtainedProjectType)
		{
			var success = FLExBridgeHelper.LaunchFieldworksBridge(FwDirectoryFinder.ProjectsDirectory, null, FLExBridgeHelper.Obtain, null,
				LcmCache.ModelVersion, FLExBridgeHelper.LiftVersion, null, null, out _, out var projectFileFullPath);
			if (!success)
			{
				ReportDuplicateBridge();
				obtainedProjectType = ObtainedProjectType.None;
				return null;
			}
			if (string.IsNullOrWhiteSpace(projectFileFullPath))
			{
				obtainedProjectType = ObtainedProjectType.None;
				return null; // user canceled.
			}
			obtainedProjectType = ObtainedProjectType.FieldWorks;

			if (projectFileFullPath.EndsWith("lift"))
			{
				projectFileFullPath = CreateProjectFromLift(parent, helpTopicProvider, projectFileFullPath);
				obtainedProjectType = ObtainedProjectType.Lift;
			}

			Analytics.Track("CreateFromSRRepo", new Dictionary<string, string>
			{
				{ "type", obtainedProjectType.ToString() },
				{ "modelVersion", LcmCache.ModelVersion.ToString() },
				{ "liftVersion", FLExBridgeHelper.LiftVersion }
			});
			EnsureLinkedFoldersExist(projectFileFullPath);

			return projectFileFullPath;
		}

		/// <summary>
		/// Get a new FW project from the specified Mercurial repository.
		/// </summary>
		/// <returns>Null if the operation was cancelled or otherwise did not work. The full pathname of an fwdata file, if it did work.</returns>
		public static string ObtainProject(Uri repoUri, string repoName, string userName, string passWord, string repoIdentifier, out ObtainedProjectType obtainedProjectType)
		{
			var result = FLExBridgeHelper.LaunchFieldworksBridge(FwDirectoryFinder.ProjectsDirectory, userName, FLExBridgeHelper.Obtain, null,
				LcmCache.ModelVersion, FLExBridgeHelper.LiftVersion, null, null, out _, out var projectFileFullPath, repoUri, repoName, passWord, repoIdentifier);

			//Assume project type
			obtainedProjectType = result ? ObtainedProjectType.FieldWorks : ObtainedProjectType.None;

			return projectFileFullPath;
		}

		private static void EnsureLinkedFoldersExist(string fwdataFileFullPathname)
		{
			var projectFolder = Path.GetDirectoryName(fwdataFileFullPathname);

			// LinkedFiles: main folder in project folder.
			var linkedFilesDir = Path.Combine(projectFolder, "LinkedFiles");
			if (!Directory.Exists(linkedFilesDir))
				Directory.CreateDirectory(linkedFilesDir);

			var subfolders = new HashSet<string>
			{
				"AudioVisual",
				"Others",
				"Pictures"
			};

			foreach (var subfolderPath in subfolders
				.Select(subfolder => Path.Combine(linkedFilesDir, subfolder))
				.Where(subfolderPath => !Directory.Exists(subfolderPath)))
			{
				Directory.CreateDirectory(subfolderPath);
			}
		}

		/// <summary>
		/// Create a new Fieldworks project and import a lift file into it. Return the .fwdata path.
		/// </summary>
		private static string CreateProjectFromLift(Form parent, IHelpTopicProvider helpTopicProvider, string liftPath)
		{
			string projectPath;
			LcmCache cache;

			// Default to the enhanced OCM file list
			var anthroListFile = Path.Combine(FwDirectoryFinder.TemplateDirectory, FwDirectoryFinder.ksOCMFrameFilename);

			using (var progressDlg = new ProgressDialogWithTask(parent))
			{
				progressDlg.Title = FwControls.ksCreatingLiftProject;
				var cacheReceiver = new LcmCache[1]; // a clumsy way of handling an out parameter, consistent with RunTask
				projectPath = (string)progressDlg.RunTask(true, CreateProjectTask,
					new object[] { liftPath, parent, anthroListFile, cacheReceiver });
				cache = cacheReceiver[0];
			}

			CallImportObtainedLexicon(cache, liftPath, parent);

			ProjectLockingService.UnlockCurrentProject(cache); // finish all saves and completely write the file so we can proceed to open it
			cache.Dispose();

			return projectPath;
		}

		#region Reflective Methods And Supporting Constants

		internal const string ImportLexiconDll = @"LexEdDll.dll";
		internal const string ImportLexiconClass = @"SIL.FieldWorks.XWorks.LexEd.FLExBridgeListener";
		internal const string ImportLexiconMethod = @"ImportObtainedLexicon";

		internal static void CallImportObtainedLexicon(LcmCache cache, string liftPath, Form parent)
		{
			// this is a horrible way to invoke this, but the current project organization does not allow us to reference
			// the LexEdDll project, nor is there any straightforward way to move the code we need into some project we can
			// reference, or any obviously suitable project to move it to without creating other References loops.
			// nasty reflections call seems less technical debt than creating an otherwise unnecessary project.
			// (It puts up its own progress dialog.)
			ReflectionHelper.CallStaticMethod(ImportLexiconDll, ImportLexiconClass,
				ImportLexiconMethod, cache, liftPath, parent);
		}

		#endregion

		/// <summary>
		/// Method with signature required by ProgressDialogWithTask.RunTask to create the project (and a cache for it)
		/// as a background task while showing the dialog.
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="parameters">A specific list is required...see the first few lines of the method.</param>
		/// <returns></returns>
		private static object CreateProjectTask(IThreadedProgress progress, object[] parameters)
		{
			// Get required parameters. Ideally these would just be the signature of the method, but RunTask requires object[].
			var liftPathname = (string) parameters[0];
			var synchronizeInvoke = (ISynchronizeInvoke) parameters[1];
			var anthroFile = (string) parameters[2];
			var cacheReceiver = (LcmCache[]) parameters[3];

			CoreWritingSystemDefinition wsVern, wsAnalysis;
			RetrieveDefaultWritingSystemsFromLift(liftPathname, out wsVern, out wsAnalysis);

			string projectPath = LcmCache.CreateNewLangProj(progress,
				Directory.GetParent(Path.GetDirectoryName(liftPathname)).Parent.Name, // Get the new Flex project name from the Lift pathname.
				FwDirectoryFinder.LcmDirectories, synchronizeInvoke, wsAnalysis, wsVern, null, null, null, anthroFile);

			// This is a temporary cache, just to do the import, and AFAIK we have no access to the current
			// user WS. So create it as "English". Put it in the array to return to the caller.
			cacheReceiver[0] = LcmCache.CreateCacheFromLocalProjectFile(projectPath, "en", new SilentLcmUI(synchronizeInvoke),
				FwDirectoryFinder.LcmDirectories, new LcmSettings(), progress);
			return projectPath;
		}

		private static void RetrieveDefaultWritingSystemsFromLift(string liftPath, out CoreWritingSystemDefinition wsVern,
			out CoreWritingSystemDefinition wsAnalysis)
		{
			PerformLdmlMigrationInClonedLiftRepo(liftPath);
			using (var liftReader = new StreamReader(liftPath, Encoding.UTF8))
			{
				string vernWsId, analysisWsId;
				using (var reader = XmlReader.Create(liftReader))
					RetrieveDefaultWritingSystemIdsFromLift(reader, out vernWsId, out analysisWsId);
				var wsManager = new WritingSystemManager(SingletonsContainer.Get<CoreGlobalWritingSystemRepository>());
				wsManager.GetOrSet(vernWsId, out wsVern);
				wsManager.GetOrSet(analysisWsId, out wsAnalysis);
			}
		}

		/// <summary>
		/// Figure out the best default vernacular and analysis writing systems to use for the input, presumed to represent a LIFT file.
		/// We get the vernacular WS from the first form element nested in a lexical-unit with a lang attribute,
		/// and the analysis WS form the first form element nested in a definition or gloss with a lang attribute.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="vernWs"></param>
		/// <param name="analysisWs"></param>
		/// <returns></returns>
		internal static void RetrieveDefaultWritingSystemIdsFromLift(XmlReader reader, out string vernWs, out string analysisWs)
		{
			vernWs = analysisWs = null;
			bool inLexicalUnit = false;
			bool inDefnOrGloss = false;
			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case XmlNodeType.Element:
						switch (reader.Name)
						{
							case "lexical-unit":
								inLexicalUnit = true;
								break;
							case "definition":
							case "gloss":
								inDefnOrGloss = true;
								break;
							case "form":
								if (inLexicalUnit && string.IsNullOrWhiteSpace(vernWs))
									vernWs = reader.GetAttribute("lang"); // pathologically may leave it null, if so keep trying.
								if (inDefnOrGloss && string.IsNullOrWhiteSpace(analysisWs))
									analysisWs = reader.GetAttribute("lang"); // pathologically may leave it null, if so keep trying.
								if (!string.IsNullOrWhiteSpace(vernWs) && !string.IsNullOrWhiteSpace(analysisWs))
									return; // got all we need, skip rest of file.
								break;
						}
						break;
					case XmlNodeType.EndElement:
						switch (reader.Name)
						{
							case "lexical-unit":
								inLexicalUnit = false;
								break;
							case "definition":
							case "gloss":
								inDefnOrGloss = false;
								break;
						}
						break;
				}
			}
			if (string.IsNullOrWhiteSpace(vernWs))
				vernWs = "fr"; // Arbitrary default (consistent with default creation of new project) if we don't find an entry
			if (string.IsNullOrWhiteSpace(analysisWs))
				analysisWs = "en"; // Arbitrary default if we don't find a sense
		}

		/// <summary>
		/// Migrate LDML files to the latest version directly in the cloned lift repository
		/// </summary>
		private static void PerformLdmlMigrationInClonedLiftRepo(string liftPath)
		{
			string ldmlFolder = Path.Combine(Path.GetDirectoryName(liftPath), "WritingSystems");
			var ldmlMigrator = new WritingSystems.Migration.LdmlInFolderWritingSystemRepositoryMigrator(ldmlFolder, null);
			ldmlMigrator.Migrate();
		}

		/// <summary>
		/// Reports to the user that a copy of FLExBridge is already running.
		/// NB. Also used by LexTextDll.FLExBridgeListener.
		/// </summary>
		public static void ReportDuplicateBridge()
		{
			MessageBox.Show(FwControls.kBridgeAlreadyRunning, FwControls.kFlexBridge,
				MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
	}

	/// <summary>
	/// Enumeration of possible sources of a new FW project.
	/// </summary>
	public enum ObtainedProjectType
	{
		/// <summary>Default value for no source</summary>
		None,
		/// <summary>Lift repository was the source</summary>
		Lift,
		/// <summary>FW repository was the source</summary>
		FieldWorks
	}
}
