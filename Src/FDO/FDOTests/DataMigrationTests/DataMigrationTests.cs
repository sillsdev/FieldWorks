// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigrationTests.cs
// Responsibility: FW Team

using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test fixture for data migrations.
	///
	/// Each migration step should have at least one test in another file which is a
	/// partial class of DataMigrationTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DataMigrationTestsBase : BaseTest
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
			m_dataMigrationManager = new FdoDataMigrationManager();
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
