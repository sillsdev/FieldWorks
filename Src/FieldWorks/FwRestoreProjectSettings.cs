// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.DomainServices.BackupRestore;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Settings used to restore a FieldWorks project from a backup.
	/// </summary>
	[Serializable]
	public class FwRestoreProjectSettings
	{
		/// <summary />
		/// <param name="settings">The restore settings (as saved by the dialog).</param>
		internal FwRestoreProjectSettings(RestoreProjectSettings settings)
		{
			Settings = settings;
		}

		/// <summary>
		/// Gets the restore settings (as saved by the dialog).
		/// </summary>
		internal RestoreProjectSettings Settings { get; }
	}
}
