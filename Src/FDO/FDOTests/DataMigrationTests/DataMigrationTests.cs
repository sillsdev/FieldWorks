// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigrationTests.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
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
