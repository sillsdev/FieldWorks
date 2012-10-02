using System;

namespace NMockSample.Order
{
	/// <summary>
	/// Process an order:
	/// - If the amount is greater than or equal to 1000, notify the user that
	/// their order has been dispatched.
	/// - If the order is also marked as urgent, notify the administrator to urgently dispatch.
	/// - If the order is less than 1000, be quiet. It's not worth our time.
	/// </summary>
	public class OrderProcessor
	{
		internal Notifier notifier = new Notifier();

		public virtual void Process(Order order)
		{
			if (order.Amount >= 1000)
			{
				notifier.NotifyUser(order.User, String.Format( "Order {0} has been dispatched", order.Number));
				if (order.Urgent)
				{
					notifier.NotifyAdmin(String.Format("Order {0} needs to be urgently dispatched to {1}", order.Number, order.User));
				}
			}
		}
	}

}
