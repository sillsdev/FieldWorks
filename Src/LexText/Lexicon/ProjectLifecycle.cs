// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks.LexText;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.Reporting;
using XCore;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// The project steps a command takes before and after another program edits the project
	/// file: committing pending edits, stopping the parser, and reopening the project.
	/// </summary>
	internal static class ProjectLifecycle
	{
		/// <summary>Returns the path of the project's .fwdata file.</summary>
		internal static string GetFullProjectFileName(LcmCache cache)
		{
			return Path.Combine(cache.ProjectId.ProjectFolder, cache.ProjectId.Name + LcmFileHelper.ksFwDataXmlFileExtension);
		}

		/// <summary>
		/// Saves the project and releases its lock. If that fails, the project is locked again
		/// before the failure is rethrown, since the lock may already have been released.
		/// </summary>
		internal static void SaveAndUnlockProject(LcmCache cache)
		{
			try
			{
				ProjectLockingService.UnlockCurrentProject(cache);
			}
			catch
			{
				try
				{
					// Locking a project this process already holds succeeds and keeps it locked.
					ProjectLockingService.LockCurrentProject(cache);
				}
				catch (Exception error)
				{
					Logger.WriteError("Relocking the project after a failed save failed", error);
				}
				throw;
			}
		}

		/// <summary>Commits the active window's pending editor changes.</summary>
		internal static void ValidatePendingEditorData(PropertyTable propertyTable)
		{
			var activeForm = propertyTable.GetValue<Form>("window");
			if (activeForm != null)
			{
				activeForm.ValidateChildren(ValidationConstraints.Enabled);
			}
		}

		/// <summary>
		/// Stops the parser, so it makes no background changes while another program uses the
		/// project.
		/// </summary>
		internal static void StopParser(PropertyTable propertyTable)
		{
			Publisher.Publish(new PublisherParameterObject(EventConstants.StopParser, null, propertyTable.GetWindow()));
		}

		/// <summary>
		/// Reopens the project from disk, refreshes the new main window, and returns it. The
		/// window that called this, and its listeners, are disposed by the reopen.
		/// </summary>
		internal static FwXWindow RefreshCacheWindowAndAll(LexTextApp app, string fullProjectFileName)
		{
			var manager = app.FwManager;
			var appArgs = new FwAppArgs(fullProjectFileName);
			var newAppWindow =
				(FwXWindow)manager.ReopenProject(manager.Cache.ProjectId.Name, appArgs).ActiveMainWindow;
			if (IsVernacularSpellingEnabled(newAppWindow.PropTable))
				WfiWordformServices.ConformSpellingDictToWordforms(newAppWindow.Cache);
			newAppWindow.RefreshDisplay();
			return newAppWindow;
		}

		// currently duplicated in MorphologyListener, to avoid an assembly dependency.
		private static bool IsVernacularSpellingEnabled(PropertyTable propertyTable)
		{
			return propertyTable.GetBoolProperty("UseVernSpellingDictionary", false);
		}
	}
}
