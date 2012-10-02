using NUnit.Framework;
using NMock;

namespace NMockSample.Order
{

	/// <summary>
	/// This test demonstrates how to test a class interacts with other domain specific
	/// classes properly.
	///
	/// The Order class is coupled to the database. The Notifier is coupled to the SMTP mail
	/// system. This fixture is testing the OrderProcessor which directly interacts with
	/// Order and Notifier.
	///
	/// The test substitutes in mock implementations of Order and Notifier so OrderProcessor
	/// can be tested in isolation.
	/// </summary>
	[TestFixture]
	public class OrderProcessorTest
	{

		private IMock order;
		private IMock notifier;
		private OrderProcessor orderProcessor;

		[SetUp]
		public void SetUp()
		{
			// setup mock Order and populate with default return values.
			order = new DynamicMock(typeof(Order));
			order.SetupResult("Amount", 1002.0);
			order.SetupResult("Urgent", false);
			order.SetupResult("Number", 123);
			order.SetupResult("User", "joe");

			// create mock Notifier
			notifier = new DynamicMock(typeof(Notifier));

			// create real OrderProcessor to be tested
			orderProcessor = new OrderProcessor();

			// switch the OrderProcessor to use the mock Notifier
			orderProcessor.notifier = (Notifier)notifier.MockInstance;
		}

		[Test]
		public void NotifyUser()
		{
			// setup
			notifier.Expect("NotifyUser", "joe", "Order 123 has been dispatched");

			// execute
			orderProcessor.Process((Order)order.MockInstance);

			// verify
			notifier.Verify();
		}

		[Test]
		public void NotifyAnotherUserAnotherNumber()
		{
			// setup
			order.SetupResult("Number", 456);
			order.SetupResult("User", "chris");
			notifier.Expect("NotifyUser", "chris", "Order 456 has been dispatched");

			// execute
			orderProcessor.Process((Order)order.MockInstance);

			// verify
			notifier.Verify();
		}

		[Test]
		public void DontNotifyUser()
		{
			// setup
			order.SetupResult("Amount", 999.0);

			// execute
			orderProcessor.Process((Order)order.MockInstance);

			// verify
			notifier.Verify();
		}

		[Test]
		public void NotifyUserAndAdmin()
		{
			// setuo
			order.SetupResult("Urgent", true);
			notifier.Expect("NotifyUser", "joe", "Order 123 has been dispatched");
			notifier.Expect("NotifyAdmin", "Order 123 needs to be urgently dispatched to joe");

			// execute
			orderProcessor.Process((Order)order.MockInstance);

			// verify
			notifier.Verify();
		}

		[Test]
		public void DontNotifyEvenThoughUrgent()
		{
			// setup
			order.SetupResult("Amount", 999.0);
			order.SetupResult("Urgent", true);

			// execute
			orderProcessor.Process((Order)order.MockInstance);

			// verify
			notifier.Verify();
		}
	}
}
