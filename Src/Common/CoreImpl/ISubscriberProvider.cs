namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface that returns an ISubscriber implementation.
	/// </summary>
	public interface ISubscriberProvider
	{
		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		ISubscriber Subscriber { get; }
	}
}