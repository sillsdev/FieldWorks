// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// This interface provides methods for notifying FDO of read operations on worker threads.
	/// This allows for proper synchronization of access to FDO. It is not necessary for the main
	/// thread to start read tasks.
	/// </summary>
	public interface IWorkerThreadReadHandler
	{
		/// <summary>
		/// Begins a read task on a worker thread.
		/// </summary>
		void BeginReadTask();

		/// <summary>
		/// Ends a read task on a worker thread.
		/// </summary>
		void EndReadTask();
	}
}
