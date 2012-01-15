using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of IDataMigrationManager
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class FdoDataMigrationManager : IDataMigrationManager
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A reusable migration instance for all migrations that just bump the version up
		/// a notch.
		///
		/// This instance can be added to the 'm_individualMigrations' dictionary as
		/// many times as it is needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private readonly IDataMigration m_bumpNumberOnlyMigration = new DoNothingDataMigration();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starting with the first key of 7000001 and continuing in numberical order (with not gaps),
		/// add instances of IDataMigration as values, which will perform the migration for
		/// the step at 'key'.
		///
		/// Eventually, data sets will have used the lower numbered steps, and only need
		/// to use the higher numbered steps.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private readonly Dictionary<int, IDataMigration> m_individualMigrations = new Dictionary<int, IDataMigration>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal FdoDataMigrationManager()
		{
			// Starting with the first key of 7000001 and continuing in numerical order (with no gaps),
			// add instances of IDataMigration as values, which will perform the migration for
			// the step at 'key'.
			//
			// Eventually, data sets will have already used the lower numbered steps, and only need
			// to use the higher numbered steps.
			m_individualMigrations.Add(7000001, new DataMigration7000001());
			m_individualMigrations.Add(7000002, new DataMigration7000002());
			m_individualMigrations.Add(7000003, new DataMigration7000003());
			m_individualMigrations.Add(7000004, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000005, new DataMigration7000005());
			m_individualMigrations.Add(7000006, new DataMigration7000006());
			m_individualMigrations.Add(7000007, new DataMigration7000007());
			m_individualMigrations.Add(7000008, new DataMigration7000008());
			m_individualMigrations.Add(7000009, new DataMigration7000009());
			m_individualMigrations.Add(7000010, new DataMigration7000010());
			m_individualMigrations.Add(7000011, new DataMigration7000011());
			m_individualMigrations.Add(7000012, new DataMigration7000012());
			m_individualMigrations.Add(7000013, new DataMigration7000013());
			m_individualMigrations.Add(7000014, new DataMigration7000014());
			m_individualMigrations.Add(7000015, new DataMigration7000015());
			m_individualMigrations.Add(7000016, new DataMigration7000016());
			m_individualMigrations.Add(7000017, new DataMigration7000017());
			m_individualMigrations.Add(7000018, new DataMigration7000018());
			m_individualMigrations.Add(7000019, new DataMigration7000019());
			m_individualMigrations.Add(7000020, new DataMigration7000020());
			m_individualMigrations.Add(7000021, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000022, new DataMigration7000022());
			m_individualMigrations.Add(7000023, new DataMigration7000023());
			m_individualMigrations.Add(7000024, new DataMigration7000024());
			m_individualMigrations.Add(7000025, new DataMigration7000025());
			m_individualMigrations.Add(7000026, new DataMigration7000026());
			m_individualMigrations.Add(7000027, new DataMigration7000027());
			m_individualMigrations.Add(7000028, new DataMigration7000028());
			m_individualMigrations.Add(7000029, new DataMigration7000029());
			m_individualMigrations.Add(7000030, new DataMigration7000030());
			m_individualMigrations.Add(7000031, new DataMigration7000031());
			m_individualMigrations.Add(7000032, new DataMigration7000032());
			m_individualMigrations.Add(7000033, new DataMigration7000033());
			m_individualMigrations.Add(7000034, new DataMigration7000034());
			m_individualMigrations.Add(7000035, new DataMigration7000035());
			m_individualMigrations.Add(7000036, new DataMigration7000036());
			m_individualMigrations.Add(7000037, new DataMigration7000037());
			m_individualMigrations.Add(7000038, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000039, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000040, new DataMigration7000040());
			m_individualMigrations.Add(7000041, new DataMigration7000041());
			m_individualMigrations.Add(7000042, new DataMigration7000042());
			m_individualMigrations.Add(7000043, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000044, new DataMigration7000044());
			m_individualMigrations.Add(7000045, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000046, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000047, new DataMigration7000047());
			m_individualMigrations.Add(7000048, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000049, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000050, m_bumpNumberOnlyMigration);
			m_individualMigrations.Add(7000051, new DataMigration7000051());
			//m_individualMigrations.Add(7000008, m_bumpNumberOnlyMigration);
			//m_individualMigrations.Add(..., new WhateverDataMigration());
			//m_individualMigrations.Add(n, new SomethingElseDataMigration());
		}

		#region Implementation of IDataMigrationManager

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the data migration on the DTOs in the given repository.
		///
		/// The resulting xml in the DTOs must be compatible with the xml of the given
		/// <paramref name="updateToVersion"/> number.
		///
		/// The implementation of this interface will process as many migrations, in sequence,
		/// as are needed to move from whatever version is in the repository to bring it up to
		/// the specified version number in <paramref name="updateToVersion"/>.
		/// </summary>
		/// <param name="domainObjectDtoRepository">Repository of CmObject DTOs.</param>
		/// <param name="updateToVersion">
		/// Model version number the updated repository should have after the migration
		/// </param>
		/// <param name="progressDlg">The dialog to display migration progress</param>
		/// <remarks>
		/// An implementation of this interface is free to define its own migration definitions.
		/// These definitions need not be compatible with any other implementation of this interface.
		/// That is, there is no interface defined for declaring and consuming data migration instructions.
		/// </remarks>
		/// <exception cref="DataMigrationException">
		/// Thrown if the migration cannot be done successfully for the following reasons:
		/// <list type="disc">
		/// <item>
		/// <paramref name="domainObjectDtoRepository"/> is null.
		/// </item>
		/// <item>
		/// Don't know how to upgrade to version: <paramref name="updateToVersion"/>.
		/// </item>
		/// <item>
		/// Version number in <paramref name="domainObjectDtoRepository"/>
		/// is newer than <paramref name="updateToVersion"/>.
		/// </item>
		/// </list>
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository,
									 int updateToVersion, IThreadedProgress progressDlg)
		{
			if (domainObjectDtoRepository == null) throw new DataMigrationException("'domainObjectDtoRepository' is null.");

			// Current version of the source.
			int currentDataStoreModelVersion = domainObjectDtoRepository.CurrentModelVersion;
			if (currentDataStoreModelVersion == updateToVersion) return; // That migration was easy enough. :-)

			if (currentDataStoreModelVersion > updateToVersion)
				throw new DataMigrationException(
					String.Format("Cannot perform backward migration from the newer '{0}' data version to the older code version '{1}'.",
								  currentDataStoreModelVersion, updateToVersion));

			progressDlg.Title = Strings.ksDataMigrationCaption;
			progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
			progressDlg.AllowCancel = false;
			progressDlg.Minimum = 0;
			progressDlg.Maximum = (updateToVersion - currentDataStoreModelVersion) + 1;
			progressDlg.RunTask(true, MigrateTask, domainObjectDtoRepository, updateToVersion);
		}

		private object MigrateTask(IThreadedProgress progressDlg, object[] parameters)
		{
			var domainObjectDtoRepository = (IDomainObjectDTORepository) parameters[0];
			var updateToVersion = (int) parameters[1];
			int currentDataStoreModelVersion = domainObjectDtoRepository.CurrentModelVersion;
			while (currentDataStoreModelVersion < updateToVersion)
			{
				var currentKey = currentDataStoreModelVersion + 1;
				progressDlg.Message = string.Format(Strings.ksDataMigrationStatusMessage, currentKey);
				m_individualMigrations[currentKey].PerformMigration(domainObjectDtoRepository);
				if (currentKey != domainObjectDtoRepository.CurrentModelVersion)
					throw new InvalidOperationException(
						string.Format("Migrator for {0} did not function correctly and returned an invalid version number.", currentKey));
				currentDataStoreModelVersion = domainObjectDtoRepository.CurrentModelVersion;
				progressDlg.Step(1);
			}

			// Have DTO repos 'merge' newbies, dirtballs, and goners into proper hashset.
			// This needs to be done for cases of multiple step migrations,
			// as one step can add a new DTO, while another can modify it.
			// That would put it in the newbies and the dirtball hashsets,
			// which causes the DM client code to crash.
			progressDlg.Message = Strings.ksDataMigrationFinishingMessage;
			domainObjectDtoRepository.EnsureItemsInOnlyOneSet();
			progressDlg.Step(1);
			return null;
		}

		/// <summary>
		/// An optional check for real data migration needs can be performed before calling
		/// PerformMigration by calling this method. If it returns <c>true</c>,
		/// then real data changes will be required to get from
		/// <paramref name="startingVersionNumber"/> to <paramref name="endingVersionNumber"/>.
		/// </summary>
		/// <param name="startingVersionNumber">The starting version number of the data.</param>
		/// <param name="endingVersionNumber">
		/// The desired ending number of the migration.
		/// NB: This should never be higher than the migration manaer can support,
		/// but it may be lower than highest supported migration number.
		/// </param>
		/// <returns><c>true</c> if the migration requires real data migration, otherwise
		/// <c>false</c>, in which case the caller may opt to just update its own
		/// version number to <paramref name="endingVersionNumber"/>.</returns>
		/// <exception cref="DataMigrationException">
		/// Thrown on any of the following conditions:
		///
		/// 1. <paramref name="startingVersionNumber"/> is less than 7000000,
		/// 2. <paramref name="endingVersionNumber"/> is greater than the highest
		///		currently suported migration version number, or
		/// 3. <paramref name="startingVersionNumber"/> is greater than
		///		<paramref name="endingVersionNumber"/>.
		/// </exception>
		public bool NeedsRealMigration(int startingVersionNumber, int endingVersionNumber)
		{
			if (startingVersionNumber > endingVersionNumber)
				throw new DataMigrationException("Cannot do the check with the given version numbers.",
												 new ArgumentOutOfRangeException("startingVersionNumber", "Starting version is higher than ending version."));
			if (startingVersionNumber < 7000000)
				throw new DataMigrationException("Cannot do the check with the given version numbers.",
												 new ArgumentOutOfRangeException("startingVersionNumber", "Starting version is lower than required minimum."));
			if (!m_individualMigrations.ContainsKey(endingVersionNumber))
				throw new DataMigrationException("Cannot do the check with the given version numbers.",
												 new ArgumentOutOfRangeException("endingVersionNumber", "Cannot migrate to the ending version."));

			while (startingVersionNumber < endingVersionNumber)
			{
				if (!(m_individualMigrations[++startingVersionNumber] is DoNothingDataMigration))
					return true;
			}

			return false;
		}

		#endregion
	}
}