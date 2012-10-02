using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Settings that specify how to back up a project. Passed to the backup project presenter to
	/// initialize it and the dialog, and also passed to the backup service.
	/// </summary>
	public class BackupProjectSettings
	{
		///<summary>
		/// Constructor
		///</summary>
		public BackupProjectSettings()
		{
			DestinationFolder = DirectoryFinder.DefaultBackupDirectory;
		}

		/// <summary>
		/// User's description of a particular back-up instance
		/// </summary>
		public String Comment { get; set; }

		///<summary>
		/// Whether or not user wants field visibilities, columns, dictionary layout, interlinear etc
		/// settings in the backup.
		///</summary>
		public bool ConfigurationSettings { get; set; }

		///<summary>
		/// Whether or not user wants pictures and sound files in the backup.
		///</summary>
		public bool MediaFiles { get; set; }

		///<summary>
		/// Whether or not user wants fonts in the backup.
		///</summary>
		public bool Fonts { get; set; }

		///<summary>
		/// Whether or not user wants keyboards in the backup.
		///</summary>
		public bool Keyboards { get; set; }

		///<summary>
		/// Folder into which the backup file will be written.
		///</summary>
		public String DestinationFolder { get; set; }
	}
}
