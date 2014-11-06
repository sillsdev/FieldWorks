using System;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// This class captures all of the configurable settings for FDO.
	/// </summary>
	public sealed class FdoSettings
	{
		private bool m_frozen;
		private bool m_disableDataMigration;
		private int m_sharedXMLBackendCommitLogSize;

		/// <summary>
		/// Initializes a new instance of the <see cref="FdoSettings"/> class.
		/// </summary>
		public FdoSettings()
		{
			m_sharedXMLBackendCommitLogSize = 100000 * SharedXMLBackendProvider.PageSize;
		}

		internal void Freeze()
		{
			m_frozen = true;
		}

		private void CheckFrozen()
		{
			if (m_frozen)
				throw new InvalidOperationException("The FDO settings cannot be changed.");
		}

		/// <summary>
		/// Gets or sets a value indicating whether data migration is disabled.
		/// </summary>
		public bool DisableDataMigration
		{
			get { return m_disableDataMigration; }
			set
			{
				CheckFrozen();
				m_disableDataMigration = value;
			}
		}

		/// <summary>
		/// Gets or sets the size of the shared XML backend commit log.
		/// </summary>
		public int SharedXMLBackendCommitLogSize
		{
			get { return m_sharedXMLBackendCommitLogSize; }
			set
			{
				CheckFrozen();
				m_sharedXMLBackendCommitLogSize = value;
			}
		}
	}
}
