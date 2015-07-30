namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface that returns an IPublisher implementation.
	/// </summary>
	public interface IPublisherProvider
	{
		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		IPublisher Publisher { get; }
	}
}