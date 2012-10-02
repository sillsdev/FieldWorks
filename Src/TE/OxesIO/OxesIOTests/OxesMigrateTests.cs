using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using OxesIO;

namespace OxesIO.Tests
{
	/// <summary>
	/// These methods test the methods in the OxesIO.Migrator class.
	/// </summary>
	[TestFixture]
	public class OxesMigrateTests
	{
		/// <summary>
		/// Check that a current file doesn't claim to need migration.
		/// </summary>
		[Test]
		public void IsMigrationNeeded_Latest_ReturnsFalse()
		{
			string path = null;
			try
			{
				path = TempOxesFiles.MinimalValidFile(null);
				Assert.IsFalse(Migrator.IsMigrationNeeded(path));
			}
			finally
			{
				if (!String.IsNullOrEmpty(path))
					File.Delete(path);
			}
		}

		/// <summary>
		/// Check that migrating a current file throws an error.
		/// </summary>
		[Test, ExpectedException(typeof(ArgumentException))]
		public void MigrateToLatestVersion_HasCurrentVersion_Throws()
		{
			string path = null;
			try
			{
				path = TempOxesFiles.MinimalValidFile(null);
				Migrator.MigrateToLatestVersion(path);
			}
			finally
			{
				if (!String.IsNullOrEmpty(path))
					File.Delete(path);
			}
		}

		/// <summary>
		/// Check that migrating an old file creates a new file with a different pathname,
		/// and that the migrated file validates properly.
		/// </summary>
		[Test]
		public void MigrateToLatestVersion_IsOldVersion_ReturnsDifferentPath()
		{
			string path = null;
			string pathMigrated = null;
			try
			{
				path = TempOxesFiles.MinimalVersion107File(null);
				pathMigrated = Migrator.MigrateToLatestVersion(path);
				Assert.AreNotEqual(path, pathMigrated);
				string errors = Validator.GetAnyValidationErrors(pathMigrated);
				Assert.IsNull(errors);
			}
			finally
			{
				if (!String.IsNullOrEmpty(path))
					File.Delete(path);
				if (!String.IsNullOrEmpty(pathMigrated) && pathMigrated != path)
					File.Delete(pathMigrated);
			}
		}

		/// <summary>
		/// Check that migrating a file with an invalid version number throws.
		/// </summary>
		[Test, ExpectedException(typeof(ApplicationException))]
		public void MigrateToLatestVersion_BadVersion_Throws()
		{
			string path = null;
			try
			{
				path = TempOxesFiles.MinimalVersion099File(null);
				Migrator.MigrateToLatestVersion(path);
			}
			finally
			{
				if (!String.IsNullOrEmpty(path))
					File.Delete(path);
			}
		}
	}
}
