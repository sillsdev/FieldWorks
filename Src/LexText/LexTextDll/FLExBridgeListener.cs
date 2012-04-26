using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FixData;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexText
{
	class FLExBridgeListener : IxCoreColleague, IFWDisposable
	{
		private Mediator _mediator;
		private FdoCache Cache { get; set; }
		#region IxCoreColleague Members

		public IxCoreColleague[] GetMessageTargets()
		{
			return new[] {this};
		}

		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

			_mediator = mediator;
			mediator.AddColleague(this);
			Cache = (FdoCache)_mediator.PropertyTable.GetValue("cache");
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Low; }
		}

		public bool ShouldNotCall
		{
			get { return false; }
		}

		#endregion

		public bool OnDisplayShowConflictReport(object parameters, ref UIItemDisplayProperties display)
		{
			var fullName = FLExBridgeHelper.FullFieldWorksBridgePath();
			display.Enabled = FileUtils.FileExists(fullName) && NotesFileIsPresent(Cache);
			display.Visible = display.Enabled;
			return true;
		}

		/// <summary>
		/// Returns true if there is any Chorus Notes to view.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static bool NotesFileIsPresent(FdoCache cache)
		{
			return cache.ProjectId.ProjectFolder != null;
		}

		/// <summary>
		/// The method/delegate that gets invoked when View->Conflict Report is clicked
		/// via the OnShowConflictReport message
		/// </summary>
		/// <param name="commandObject">Includes the XML command element of the OnShowConflictReport message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate.</returns>
		public bool OnShowConflictReport(object commandObject)
		{
			string url;
			string projectPath;
			FLExBridgeHelper.LaunchFieldworksBridge(Path.Combine(Cache.ProjectId.ProjectFolder, Cache.ProjectId.Name + ".fwdata"),
								   Environment.UserName,
								   FLExBridgeHelper.ConflictViewer, out projectPath, out url);
			if (!string.IsNullOrEmpty(url))
			{
				// TODO: Jump to url
				var args = new Common.RootSites.LocalLinkArgs {Link = url};
				if (_mediator != null)
				{
					_mediator.SendMessage("HandleLocalHotlink", args);
					if (args.LinkHandledLocally)
						return true;
				}
			}
			return true;
		}

		public bool OnDisplayFLExBridge(object parameters, ref UIItemDisplayProperties display)
		{
			var fullName = FLExBridgeHelper.FullFieldWorksBridgePath();
			display.Enabled = FileUtils.FileExists(fullName);
			display.Visible = display.Enabled;

			return true; // We dealt with it.
		}

		/// <summary>
		/// The method/delegate that gets invoked when File->Send/Receive Project is clicked
		/// via the OnFLExBridge message
		/// </summary>
		/// <param name="commandObject">Includes the XML command element of the OnFLExBridge message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate.</returns>
		public bool OnFLExBridge(object commandObject)
		{
			if (IsDb4oProject)
			{
				var dlg = new Db4oSendReceiveDialog();
				if (dlg.ShowDialog() == DialogResult.Abort)
				{
					// User clicked on link
					_mediator.SendMessage("FileProjectSharingLocation", null);
				}
				return true;
			}
			//Unlock project
			string url;
			ProjectLockingService.UnlockCurrentProject(Cache);
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var savedState = PrepareToDetectConflicts(projectFolder);
			string projectPath;
			var dataChanged = FLExBridgeHelper.LaunchFieldworksBridge(Path.Combine(projectFolder, Cache.ProjectId.Name + ".fwdata"),
																	  Environment.UserName,
																	  FLExBridgeHelper.SendReceive,
																	  out projectPath, out url);
			if(dataChanged)
			{
				var fixer = new FwDataFixer(Cache.ProjectId.Path, new StatusBarProgressHandler(null, null), logger);
				fixer.FixErrorsAndSave();
				bool conflictOccurred = DetectConflicts(projectFolder, savedState);
				var app = (LexTextApp)_mediator.PropertyTable.GetValue("App");
				var manager = app.FwManager;
				var appArgs = new FwAppArgs(app.ApplicationName, Cache.ProjectId.Name, "", "", Guid.Empty);
				var newApp = manager.ReopenProject(Cache.ProjectId.Name, appArgs);
				if (conflictOccurred)
				{
					((FwXWindow) newApp.ActiveMainWindow).Mediator.SendMessage("ShowConflictReport", null);
					//OnShowConflictReport(null); no good, we've been disposed.
				}
			}
			else //Re-lock project if we aren't trying to close the app
			{
				ProjectLockingService.LockCurrentProject(Cache);
			}
			return true;
		}

		protected virtual bool IsDb4oProject
		{
			get { return Cache.ProjectId.Type == FDOBackendProviderType.kDb4oClientServer; }
		}

		#region Implementation of IDisposable

		private bool DetectConflicts(string path, Dictionary<string, long> savedState)
		{
			foreach (var file in Directory.GetFiles(path, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				long oldLength;
				savedState.TryGetValue(file, out oldLength);
				if (new FileInfo(file).Length == oldLength)
					continue; // no new notes in this file.
				return true; // Review JohnT: do we need to look in the file to see if what was added is a conflict?
			}
			return false; // no conflicts added.
		}

		private Dictionary<string, long> PrepareToDetectConflicts(string path)
		{
			var result = new Dictionary<string, long>();
			foreach (var file in Directory.GetFiles(path, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				result[file] = new FileInfo(file).Length;
			}
			return result;
		}		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			_mediator.RemoveColleague(this);
			IsDisposed = true;
		}

		#endregion

		#region Implementation of IFWDisposable

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		///              true.  This is the case where a method or property in an object is being
		///              used but the object itself is no longer valid.
		///              This method should be added to all public properties and methods of this
		///              object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("FLExBridgeListener already disposed.");
		}

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		public bool IsDisposed { get; set; }

		#endregion

		private static void logger(string guid, string date, string description)
		{
			Console.WriteLine("Error reported, but not dealt with {0} {1} {2}", guid, date, description);
		}
	}
}
