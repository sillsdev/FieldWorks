// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// Parameter class used for specifying how to load a backend system.
	/// </summary>
	public sealed class BackendStartupParameter
	{
		private readonly bool m_useMemoryWsManager;
		private readonly BackendBulkLoadDomain m_bulkLoadDomain;
		private readonly IProjectIdentifier m_projectId;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new instance.
		/// </summary>
		/// <param name="useMemoryWsManager">if set to <c>true</c> a memory-based writing system
		/// manager will be used.</param>
		/// <param name="bulkLoadDomain">The domain to bulk load.</param>
		/// <param name="projectId">the project identification</param>
		/// ------------------------------------------------------------------------------------
		public BackendStartupParameter(bool useMemoryWsManager, BackendBulkLoadDomain bulkLoadDomain,
			IProjectIdentifier projectId)
		{
			m_useMemoryWsManager = useMemoryWsManager;
			m_bulkLoadDomain = bulkLoadDomain;
			m_projectId = projectId;
		}

		/// <summary>
		/// Gets a value indicating whether to use the memory-based writing system manager.
		/// </summary>
		public bool UseMemoryWsManager
		{
			get { return m_useMemoryWsManager; }
		}

		///<summary>
		/// Get the bulk load domain.
		///</summary>
		public BackendBulkLoadDomain BulkLoadDomain
		{
			get { return m_bulkLoadDomain; }
		}

		///<summary>
		/// Get the project identification
		///</summary>
		public IProjectIdentifier ProjectId
		{
			get { return m_projectId; }
		}
	}
}