// Copyright (c) 2015-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Test the PubSubSystem class.
	/// </summary>
	[TestFixture]
	public class PubSubSystemTests
	{
		/// <summary>
		/// Set up for each test.
		/// </summary>
		[SetUp]
		public void TestSetup()
		{
			var _subscriber = new Subscriber();
			Singleton.Instance.TestingSubscriber = _subscriber;
			Singleton.Instance.TestingPublisher = new Publisher(_subscriber); ;
		}

		/// <summary>
		/// Tear down after each test.
		/// </summary>
		[TearDown]
		public void TestTeardown()
		{
			Singleton.Instance.TestingSubscriber = null;
			Singleton.Instance.TestingPublisher = null;
		}

		/// <summary>
		/// This tests the code path of: Single pub call does not throw on next single pub call.
		/// </summary>
		[Test]
		public void Ordinary_Rentry_Does_Not_Throw()
		{
			// Set up.
			var subscriber = new ReentrantSubscriber_SingleCall
			{
				One = true,
			};
			subscriber.DoSubscriptions();
			subscriber.ShouldDoReentrantPublish = true;
			SomeRandomMessageSubscriber.DoSubscriptions();

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.DoesNotThrow(() => TestPublisher.PublishMessageOne());
			subscriber.DoUnsubscriptions();
			SomeRandomMessageSubscriber.DoUnsubscriptions();
		}

		/// <summary>
		/// This tests the code path of: Single publisher handler then calls a multiple publisher.
		/// </summary>
		[Test]
		public void Single_Publisher_Handler_Calls_Multiple_Publisher_on_Rentry_Does_Not_Throw()
		{
			// Set up.
			var subscriber = new ReentrantSubscriber_Single_CallsMultiple
			{
				One = true,
			};
			subscriber.DoSubscriptions();
			subscriber.ShouldDoReentrantPublish = true;
			SomeRandomMessageSubscriber.DoSubscriptions();
			var niceGuyMultipleSubscriber = new NiceGuy_MultipleSubscriber();
			niceGuyMultipleSubscriber.DoSubscriptions();

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.DoesNotThrow(() => FwUtils.Publisher.Publish(new PublisherParameterObject("BadBoy", false)));
			subscriber.DoUnsubscriptions();
			SomeRandomMessageSubscriber.DoUnsubscriptions();
			niceGuyMultipleSubscriber.DoUnsubscriptions();
		}

		/// <summary>
		/// Ordinary multiple publish method call, but not re-entrant.
		/// </summary>
		[Test]
		public void Multiple_Publishing()
		{
			// Set up.
			var subscriber = new NiceGuy_MultipleSubscriber
			{
				One = true,
				Two = int.MinValue
			};
			subscriber.DoSubscriptions();

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);
			TestPublisher.PublishBothMessages();
			Assert.IsFalse(subscriber.One);
			Assert.AreEqual(int.MaxValue, subscriber.Two);
			subscriber.DoUnsubscriptions();

			subscriber.One = true;
			subscriber.Two = int.MinValue;
			TestPublisher.PublishBothMessages();
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);
		}

		/// <summary>
		/// Ordinary single call for MessageOneHandler of Subscriber.
		/// </summary>
		[Test]
		public void Test_Subscriber_MessageOneHandling()
		{
			// Set up.
			var subscriber = new SingleMessageSubscriber
			{
				One = true,
				Two = 1
			};
			subscriber.DoSubscriptions();

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(1, subscriber.Two);
			TestPublisher.PublishMessageOne();
			Assert.IsFalse(subscriber.One); // Did change.
			Assert.AreEqual(1, subscriber.Two); // Did not change.

			subscriber.One = true;
			Assert.IsTrue(subscriber.One);
			subscriber.DoUnsubscriptions();
			TestPublisher.PublishMessageOne();
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.AreEqual(1, subscriber.Two); // Did not change.
		}

		/// <summary>
		/// Ordinary single call for MessageTwoHandler of Subscriber.
		/// </summary>
		[Test]
		public void Test_Subscriber_MessageTwoHandling()
		{
			// Set up.
			var subscriber = new SingleMessageSubscriber
			{
				One = true,
				Two = 1
			};
			subscriber.DoSubscriptions();

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(1, subscriber.Two);

			TestPublisher.PublishMessageTwo();
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.AreEqual(2, subscriber.Two); // Did change.

			subscriber.Two = 1;
			Assert.AreEqual(1, subscriber.Two);
			subscriber.DoUnsubscriptions();
			TestPublisher.PublishMessageTwo();
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.AreEqual(1, subscriber.Two); // Did not change.
		}

		/// <summary>
		/// Ordinary single message call for MessageOneHandler, but on two subscribers.
		/// </summary>
		[Test]
		public void Test_Two_Subscribers_For_MessageOneHandling()
		{
			// Set up.
			var subscriber = new SingleMessageSubscriber
			{
				One = true
			};
			subscriber.DoSubscriptions();
			var subscriber2 = new DoubleMessageSubscriber
			{
				One = true
			};
			subscriber2.DoSubscriptions();

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.IsTrue(subscriber2.One);

			TestPublisher.PublishMessageOne();
			Assert.IsFalse(subscriber.One); // Did change.
			Assert.IsFalse(subscriber2.One); // Did change.

			subscriber.One = true;
			subscriber2.One = true;
			subscriber.DoUnsubscriptions();
			subscriber2.DoUnsubscriptions();
			TestPublisher.PublishMessageOne();
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.IsTrue(subscriber2.One); // Did not change.
		}

		[Test]
		[TestCase(null)]
		[TestCase("")]
		[TestCase(" ")]
		public void IllFormedPublisherParameterObjectThrows(string message)
		{
			Assert.Throws<ArgumentNullException>(() => { var dummy = new PublisherParameterObject(message); });
		}

		[Test]
		public void VerifyPublishMessageCalls()
		{
			// Test multiple call Publish override.
			Assert.Throws<ArgumentNullException>(() => FwUtils.Publisher.Publish((IList<PublisherParameterObject>)null));
			var publisherParameterObject = new List<PublisherParameterObject>();
			Assert.Throws<InvalidOperationException>(() => FwUtils.Publisher.Publish(publisherParameterObject));
			publisherParameterObject.Add(new PublisherParameterObject("MessageOne"));
			Assert.Throws<InvalidOperationException>(() => FwUtils.Publisher.Publish(publisherParameterObject));
			publisherParameterObject.Add(new PublisherParameterObject("MessageTwo"));
			Assert.DoesNotThrow(() => FwUtils.Publisher.Publish(publisherParameterObject));

			// Test single message Publish method override.
			Assert.Throws<ArgumentNullException>(() => FwUtils.Publisher.Publish((PublisherParameterObject)null));
			Assert.DoesNotThrow(() => FwUtils.Publisher.Publish(publisherParameterObject[0]));
		}

		/// <summary>
		/// Test publishing at EndOfAction.
		/// </summary>
		[Test]
		public void Test_PublishAtEndOfAction()
		{
			// Set up.
			var subscriber = new EndOfAction_MultipleSubscriber
			{
				One = true,
				Two = int.MinValue
			};
			subscriber.DoSubscriptions();

			Assert.IsNull(subscriber.First);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);

			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.RecordNavigation, false));
			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, int.MaxValue));

			// Confirm that nothing changed.
			Assert.IsNull(subscriber.First);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);

			// SUT - Process the EndOfActionManager IdleQueue.
			FwUtils.Publisher.EndOfActionManager.IdleEndOfAction(null);

			Assert.AreEqual(EventConstants.RecordNavigation, subscriber.First);
			Assert.IsFalse(subscriber.One);
			Assert.AreEqual(int.MaxValue, subscriber.Two);
			subscriber.DoUnsubscriptions();
		}

		/// <summary>
		/// Test publishing at EndOfAction. It does not matter what order the events are published,
		/// they still execute in the same order.
		/// </summary>
		[Test]
		public void Test_PublishAtEndOfAction_OrderDoesNotMatter()
		{
			// Set up.
			var subscriber = new EndOfAction_MultipleSubscriber
			{
				One = true,
				Two = int.MinValue
			};
			subscriber.DoSubscriptions();

			Assert.IsNull(subscriber.First);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);

			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, int.MaxValue));
			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.RecordNavigation, false));

			// Confirm that nothing changed.
			Assert.IsNull(subscriber.First);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);

			// SUT - Process the EndOfActionManager IdleQueue.
			FwUtils.Publisher.EndOfActionManager.IdleEndOfAction(null);

			Assert.AreEqual(EventConstants.RecordNavigation, subscriber.First);
			Assert.IsFalse(subscriber.One);
			Assert.AreEqual(int.MaxValue, subscriber.Two);
			subscriber.DoUnsubscriptions();
		}

		/// <summary>
		/// Test publishing at EndOfAction. Confirm that only the events that were published
		/// actually execute.
		/// </summary>
		[Test]
		public void Test_PublishAtEndOfAction_OnlyExecuteEventsThatArePublished()
		{
			// Set up.
			var subscriber = new EndOfAction_MultipleSubscriber
			{
				One = true,
				Two = int.MinValue
			};
			subscriber.DoSubscriptions();

			Assert.IsNull(subscriber.First);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);

			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, int.MaxValue));

			// Confirm that nothing changed.
			Assert.IsNull(subscriber.First);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);

			// SUT - Process the EndOfActionManager IdleQueue.
			FwUtils.Publisher.EndOfActionManager.IdleEndOfAction(null);

			Assert.AreEqual(EventConstants.SelectionChanged, subscriber.First);
			Assert.IsTrue(subscriber.One);	// Doesn't change.
			Assert.AreEqual(int.MaxValue, subscriber.Two);
			subscriber.DoUnsubscriptions();
		}

		private static class TestPublisher
		{
			internal static void PublishMessageOne()
			{
				FwUtils.Publisher.Publish(new PublisherParameterObject("MessageOne", false));
			}

			internal static void PublishMessageTwo()
			{
				FwUtils.Publisher.Publish(new PublisherParameterObject("MessageTwo", 2));
			}

			internal static void PublishBothMessages()
			{
				var messages = new List<PublisherParameterObject>
				{
					new PublisherParameterObject("MessageOne", false),
					new PublisherParameterObject("MessageTwo", int.MaxValue)
				};
				FwUtils.Publisher.Publish(messages);
			}
		}

		private static class SomeRandomMessageSubscriber
		{
			/// <summary>
			/// This is the subscribed message handler for "SomeRandomMessage" message.
			/// This is used in testing re-entrant calls.
			/// </summary>
			private static void SomeRandomMessageOneHandler(object newValue)
			{
			}

			internal static void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("SomeRandomMessage", SomeRandomMessageOneHandler);
			}

			internal static void DoUnsubscriptions()
			{
				FwUtils.Subscriber.Unsubscribe("SomeRandomMessage", SomeRandomMessageOneHandler);
			}
		}

		private sealed class SingleMessageSubscriber
		{
			internal bool One { get; set; }
			internal int Two { get; set; }

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void MessageOneHandler(object newValue)
			{
				One = (bool)newValue;
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageTwo" message.
			/// </summary>
			private void MessageTwoHandler(object newValue)
			{
				Two = (int)newValue;
			}

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("MessageOne", MessageOneHandler);
				FwUtils.Subscriber.Subscribe("MessageTwo", MessageTwoHandler);
			}

			internal void DoUnsubscriptions()
			{
				FwUtils.Subscriber.Unsubscribe("MessageOne", MessageOneHandler);
				FwUtils.Subscriber.Unsubscribe("MessageTwo", MessageTwoHandler);
			}
		}

		private sealed class DoubleMessageSubscriber
		{
			internal bool One { get; set; }

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void SecondMessageOneHandler(object newValue)
			{
				One = (bool)newValue;
			}

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("MessageOne", SecondMessageOneHandler);
			}

			internal void DoUnsubscriptions()
			{
				FwUtils.Subscriber.Unsubscribe("MessageOne", SecondMessageOneHandler);
			}
		}

		private sealed class ReentrantSubscriber_SingleCall
		{
			private bool _one;
			internal bool ShouldDoReentrantPublish { get; set; }

			internal bool One
			{
				get => _one;
				set
				{
					_one = value;
					if (ShouldDoReentrantPublish)
					{
						// Bad boy! Re-entrant test should fail on this.
						FwUtils.Publisher.Publish(new PublisherParameterObject("SomeRandomMessage", "Whatever"));
					}
				}
			}

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("MessageOne", ReentrantMessageOneHandler);
			}

			internal void DoUnsubscriptions()
			{
				FwUtils.Subscriber.Unsubscribe("MessageOne", ReentrantMessageOneHandler);
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void ReentrantMessageOneHandler(object newValue)
			{
				One = (bool)newValue; // NB: The bad part is in the setter, which fires off more Publish calls.
			}
		}

		private sealed class ReentrantSubscriber_Single_CallsMultiple
		{
			private bool _one;
			internal bool ShouldDoReentrantPublish { get; set; }

			internal bool One
			{
				get => _one;
				set
				{
					_one = value;
					if (ShouldDoReentrantPublish)
					{
						// Bad boy! Re-entrant test should fail on this.
						var messages = new List<PublisherParameterObject>
						{
							new PublisherParameterObject("MessageOne", false),
							new PublisherParameterObject("SomeRandomMessage", "Whatever")
						};
						FwUtils.Publisher.Publish(messages);
					}
				}
			}

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("BadBoy", ReentrantBadBoyHandler);
			}

			internal void DoUnsubscriptions()
			{
				FwUtils.Subscriber.Unsubscribe("BadBoy", ReentrantBadBoyHandler);
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void ReentrantBadBoyHandler(object newValue)
			{
				One = (bool)newValue; // NB: The bad part is in the setter, which fires off more Publish calls.
			}
		}

		private sealed class NiceGuy_MultipleSubscriber
		{
			internal bool One { get; set; }
			internal int Two { get; set; }

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("MessageOne", MessageOneHandler);
				FwUtils.Subscriber.Subscribe("MessageTwo", MessageTwoHandler);
			}

			internal void DoUnsubscriptions()
			{
				FwUtils.Subscriber.Unsubscribe("MessageOne", MessageOneHandler);
				FwUtils.Subscriber.Unsubscribe("MessageTwo", MessageTwoHandler);
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void MessageOneHandler(object newValue)
			{
				One = (bool)newValue;
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void MessageTwoHandler(object newValue)
			{
				Two = (int)newValue;
			}
		}

		private sealed class EndOfAction_MultipleSubscriber
		{
			internal string First { get; set; }
			internal bool One { get; set; }
			internal int Two { get; set; }

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe(EventConstants.RecordNavigation, RecordNavigationHandler);
				FwUtils.Subscriber.Subscribe(EventConstants.SelectionChanged, SelectionChangedHandler);
			}

			internal void DoUnsubscriptions()
			{
				FwUtils.Subscriber.Unsubscribe(EventConstants.RecordNavigation, RecordNavigationHandler);
				FwUtils.Subscriber.Unsubscribe(EventConstants.SelectionChanged, SelectionChangedHandler);
			}

			/// <summary>
			/// This is the subscribed message handler for RecordNavigation message.
			/// </summary>
			private void RecordNavigationHandler(object data)
			{
				if (First == null)
					First = EventConstants.RecordNavigation;
				One = (bool)data;
			}

			/// <summary>
			/// This is the subscribed message handler for SelectionChanged message.
			/// </summary>
			private void SelectionChangedHandler(object data)
			{
				if (First == null)
					First = EventConstants.SelectionChanged;
				Two = (int)data;
			}
		}

	}
}