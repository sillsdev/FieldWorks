// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.ObjectModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class SandboxEditMonitorHelper : DisposableBase
	{
		internal SandboxEditMonitorHelper(SandboxEditMonitor editMonitor, bool fSuspendMonitor)
		{
			EditMonitor = editMonitor;
			if (!fSuspendMonitor)
			{
				return;
			}
			EditMonitor.StopMonitoring();
			SuspendedMonitor = true;
		}

		private SandboxEditMonitor EditMonitor { get; set; }
		private bool SuspendedMonitor { get; set; }

		protected override void DisposeManagedResources()
		{
			if (!SuspendedMonitor)
			{
				return;
			}
			// re-enable monitor if we had suspended it.
			EditMonitor.StartMonitoring();
			SuspendedMonitor = false;
		}

		protected override void DisposeUnmanagedResources()
		{
			EditMonitor = null;
		}
	}
}