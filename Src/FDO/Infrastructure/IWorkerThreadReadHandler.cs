
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
