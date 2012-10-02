namespace SIL.Utils
{
	/// <summary>
	/// An interface which objects (currently implmenentations of ISilDataAccess)
	/// may implement if they desire to to update something when the user refreshes the display.
	/// </summary>
	public interface IRefreshable
	{
		/// <summary>
		/// Update whatever needs it.
		/// </summary>
		void Refresh();
	}

	/// <summary>
	/// An interface which may also be implemented by implementors of IRefreshable, allowing
	/// a high-level call which has refreshed them to suspend further Refreshes which may be
	/// made by lower-level calls.
	/// </summary>
	public interface ISuspendRefresh
	{
		/// <summary>
		/// Ignore Refresh until the next Resume
		/// </summary>
		void SuspendRefresh();
		/// <summary>
		/// Stop ignoring Refreshes. Currently, this will cancel the effect of any number of
		/// SuspendRefresh calls. It does NOT perform a refresh, even if one was requested
		/// while suspended.
		/// </summary>
		void ResumeRefresh();
	}
}
