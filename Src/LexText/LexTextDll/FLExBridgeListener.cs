using System;
using System.IO;
using SIL.FieldWorks.Common.Framework;
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

		public bool OnShowConflictReport(object commandObject)
		{
			FLExBridgeHelper.LaunchFieldworksBridge(Path.Combine(Cache.ProjectId.ProjectFolder, Cache.ProjectId.Name + ".fwdata"),
								   Environment.UserName,
								   FLExBridgeHelper.ConflictViewer);
			return true;
		}

		public bool OnDisplayFLExBridge(object parameters, ref UIItemDisplayProperties display)
		{
			var fullName = FLExBridgeHelper.FullFieldWorksBridgePath();
			display.Enabled = FileUtils.FileExists(fullName);
			display.Visible = display.Enabled;

			return true; // We dealt with it.
		}

		public bool OnFLExBridge(object commandObject)
		{
			//Unlock project
			ProjectLockingService.UnlockCurrentProject(Cache);
			var dataChanged = FLExBridgeHelper.LaunchFieldworksBridge(Path.Combine(Cache.ProjectId.ProjectFolder, Cache.ProjectId.Name + ".fwdata"),
																	  Environment.UserName,
																	  FLExBridgeHelper.SendReceive);
			if(dataChanged)
			{
				var fixer = new FwDataFixer(Cache.ProjectId.Path, new StatusBarProgressHandler(null, null), logger);
				fixer.FixErrorsAndSave();
				var app = (LexTextApp)_mediator.PropertyTable.GetValue("App");
				var manager = app.FwManager;
				var appArgs = new FwAppArgs(app.ApplicationName, Cache.ProjectId.Name, "", "", Guid.Empty);
				manager.ReopenProject(Cache.ProjectId.Name, appArgs);
			}
			else //Re-lock project if we aren't trying to close the app
			{
				ProjectLockingService.LockCurrentProject(Cache);
			}
			return true;
		}
		#region Implementation of IDisposable

		/// <summary>
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