// Copyright (c) 2014-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)


namespace SIL.LCModel
{
	/// <summary>
	/// Exception thrown when we try to open a project which requires data migration but data migration has been forbidden by the
	/// application
	/// </summary>
	public class LcmDataMigrationForbiddenException : LcmInitializationException
	{
		/// <summary />
		public LcmDataMigrationForbiddenException()
			: base("This project requires a data migration, but migrations have been forbidden.")
		{
		}
	}
}