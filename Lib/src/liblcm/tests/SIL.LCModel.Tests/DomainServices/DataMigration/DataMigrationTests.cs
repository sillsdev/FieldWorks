// Copyright (c) 2009-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;

namespace SIL.LCModel.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test fixture for data migrations.
	///
	/// Each migration step should have at least one test in another file which is a
	/// partial class of DataMigrationTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DataMigrationTestsBase
	{
		internal IDataMigrationManager m_dataMigrationManager;
		private string m_temporaryPathname;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the migration manager.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public virtual void TestSetup()
		{
			m_dataMigrationManager = new LcmDataMigrationManager();
			m_temporaryPathname = Path.GetTempFileName();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get rid of migration manager.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public virtual void TestTearDown()
		{
			m_dataMigrationManager = null;
			File.Delete(m_temporaryPathname);
		}
	}
}
