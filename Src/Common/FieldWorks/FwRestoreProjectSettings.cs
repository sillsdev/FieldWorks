// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwRestoreProjectSettings.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using System;
using System.IO;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Settings used to restore a FieldWorks project from a backup.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class FwRestoreProjectSettings
	{
		#region Member variables
		private readonly string m_fwAppCommandLineAbbrev;
		private readonly RestoreProjectSettings m_settings;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwRestoreProjectSettings"/> class.
		/// </summary>
		/// <param name="fwAppCommandLineAbbrev">The FW application command line abbrev (TE or
		/// FLEx), case-insensitive.</param>
		/// <param name="settings">The restore settings (as saved by the dialog).</param>
		/// ------------------------------------------------------------------------------------
		internal FwRestoreProjectSettings(string fwAppCommandLineAbbrev, RestoreProjectSettings settings)
		{
			m_fwAppCommandLineAbbrev = fwAppCommandLineAbbrev;
			m_settings = settings;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FW application command line abbrev (TE or FLEx), case-insensitive.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string FwAppCommandLineAbbrev
		{
			get { return m_fwAppCommandLineAbbrev; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the restore settings (as saved by the dialog).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal RestoreProjectSettings Settings
		{
			get { return m_settings; }
		}
		#endregion
	}
}
