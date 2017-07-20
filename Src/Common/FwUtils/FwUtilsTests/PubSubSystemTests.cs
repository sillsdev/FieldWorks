// Copyright (c) 2015-2017 SIL International
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
		private IPublisher _publisher;
		private ISubscriber _subscriber;

		private static class Publisher
		{
			internal static void PublishMessageOne(IPublisher pubSystem)
			{
				pubSystem.Publish("MessageOne", false);
			}

			internal static void PublishMessageTwo(IPublisher pubSystem)
			{
				pubSystem.Publish("MessageTwo", 2);
			}

			internal static void PublishBothMessages(IPublisher pubSystem)
			{
				var commands = new List<string>
				{
					"MessageOne",
					"MessageTwo"
				};
				var parms = new List<object>
				{
					false,
					int.MaxValue
				};
				pubSystem.Publish(commands, parms);
			}
		}

		private class Subscriber
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

			internal void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("MessageOne", MessageOneHandler);
				subscriber.Subscribe("MessageTwo", MessageTwoHandler);
			}

			internal void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("MessageOne", MessageOneHandler);
				subscriber.Unsubscribe("MessageTwo", MessageTwoHandler);
			}
		}

		private class Subscriber2
		{
			internal bool One { get; set; }

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void SecondMessageOneHandler(object newValue)
			{
				One = (bool)newValue;
			}

			internal void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("MessageOne", SecondMessageOneHandler);
			}

			internal void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("MessageOne", SecondMessageOneHandler);
			}
		}

		private class ReentrantSubscriber
		{
			internal IPublisher Publisher { get; set; }
			internal bool One { get; set; }

			internal void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("MessageOne", ReentrantMessageOneHandler);
			}

			internal void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("MessageOne", ReentrantMessageOneHandler);
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void ReentrantMessageOneHandler(object newValue)
			{
				One = (bool)newValue;
				Publisher.Publish("MessageOne", !One);
			}
		}

		private class ReentrantSubscriber2
		{
			internal IPublisher Publisher { get; set; }
			internal bool One { get; set; }
			internal int Two { get; set; }

			internal void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("MessageOne", ReentrantMessageOneHandler);
				subscriber.Subscribe("MessageTwo", OrdinaryMessageTwoHandler);
			}

			internal void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("MessageOne", ReentrantMessageOneHandler);
				subscriber.Unsubscribe("MessageTwo", OrdinaryMessageTwoHandler);
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void ReentrantMessageOneHandler(object newValue)
			{
				One = (bool)newValue;
				var commands = new List<string>
				{
					"MessageOne",
					"MessageTwo"
				};
				var parms = new List<object>
				{
					false,
					int.MaxValue
				};
				Publisher.Publish(commands, parms);
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void OrdinaryMessageTwoHandler(object newValue)
			{
				Two = (int)newValue;
			}
		}

		private class MultipleSubscriber
		{
			internal bool One { get; set; }
			internal int Two { get; set; }

			internal void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("MessageOne", MessageOneHandler);
				subscriber.Subscribe("MessageTwo", MessageTwoHandler);
			}

			internal void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("MessageOne", MessageOneHandler);
				subscriber.Unsubscribe("MessageTwo", MessageTwoHandler);
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

		/// <summary>
		/// Set up for each test.
		/// </summary>
		[SetUp]
		public void TestSetup()
		{
			PubSubSystemFactory.CreatePubSubSystem(out _publisher, out _subscriber);
		}

		/// <summary>
		/// Tear down after each test.
		/// </summary>
		[TearDown]
		public void TestTeardown()
		{
			_publisher = null;
			_subscriber = null;
		}

		/// <summary>
		/// Test "MessageOne" handler of ReentrantSubscriber.
		/// </summary>
		[Test]
		public void Ordinary_Rentry_Throws()
		{
			// Set up.
			var subscriber = new ReentrantSubscriber
			{
				One = true,
				Publisher = _publisher
			};
			subscriber.DoSubscriptions(_subscriber);

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.Throws<InvalidOperationException>(() => Publisher.PublishMessageOne(_publisher));
			subscriber.DoUnsubscriptions(_subscriber);
		}

		/// <summary>
		/// Test "MessageOne" handler of ReentrantSubscriber.
		/// </summary>
		[Test]
		public void Multiple_Rentry_Throws()
		{
			// Set up.
			var subscriber = new ReentrantSubscriber2
			{
				One = true,
				Two = int.MinValue,
				Publisher = _publisher
			};
			subscriber.DoSubscriptions(_subscriber);

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);
			Assert.Throws<InvalidOperationException>(() => Publisher.PublishBothMessages(_publisher));
			subscriber.DoUnsubscriptions(_subscriber);
		}

		/// <summary>
		/// Test "MessageOne" handler of ReentrantSubscriber.
		/// </summary>
		[Test]
		public void Multiple_Publishing()
		{
			// Set up.
			var subscriber = new MultipleSubscriber
			{
				One = true,
				Two = int.MinValue
			};
			subscriber.DoSubscriptions(_subscriber);

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);
			Publisher.PublishBothMessages(_publisher);
			Assert.IsFalse(subscriber.One);
			Assert.AreEqual(int.MaxValue, subscriber.Two);
			subscriber.DoUnsubscriptions(_subscriber);

			subscriber.One = true;
			subscriber.Two = int.MinValue;
			Publisher.PublishBothMessages(_publisher);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);
		}

		/// <summary>
		/// Test MessageOneHandler of Subscriber.
		/// </summary>
		[Test]
		public void Test_Subscriber_MessageOneHandling()
		{
			// Set up.
			var subscriber = new Subscriber
			{
				One = true,
				Two = 1
			};
			subscriber.DoSubscriptions(_subscriber);

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(1, subscriber.Two);
			Publisher.PublishMessageOne(_publisher);
			Assert.IsFalse(subscriber.One); // Did change.
			Assert.AreEqual(1, subscriber.Two); // Did not change.

			subscriber.One = true;
			Assert.IsTrue(subscriber.One);
			subscriber.DoUnsubscriptions(_subscriber);
			Publisher.PublishMessageOne(_publisher);
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.AreEqual(1, subscriber.Two); // Did not change.
		}

		/// <summary>
		/// Test MessageTwoHandler of Subscriber.
		/// </summary>
		[Test]
		public void Test_Subscriber_MessageTwoHandling()
		{
			// Set up.
			var subscriber = new Subscriber
			{
				One = true,
				Two = 1
			};
			subscriber.DoSubscriptions(_subscriber);

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(1, subscriber.Two);

			Publisher.PublishMessageTwo(_publisher);
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.AreEqual(2, subscriber.Two); // Did change.

			subscriber.Two = 1;
			Assert.AreEqual(1, subscriber.Two);
			subscriber.DoUnsubscriptions(_subscriber);
			Publisher.PublishMessageTwo(_publisher);
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.AreEqual(1, subscriber.Two); // Did not change.
		}

		/// <summary>
		/// Test MessageOneHandler on two subscribers.
		/// </summary>
		[Test]
		public void Test_Two_Subscribers_For_MessageOneHandling()
		{
			// Set up.
			var subscriber = new Subscriber
			{
				One = true
			};
			subscriber.DoSubscriptions(_subscriber);
			var subscriber2 = new Subscriber2
			{
				One = true
			};
			subscriber2.DoSubscriptions(_subscriber);

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.IsTrue(subscriber2.One);

			Publisher.PublishMessageOne(_publisher);
			Assert.IsFalse(subscriber.One); // Did change.
			Assert.IsFalse(subscriber2.One); // Did change.

			subscriber.One = true;
			subscriber2.One = true;
			subscriber.DoUnsubscriptions(_subscriber);
			subscriber2.DoUnsubscriptions(_subscriber);
			Publisher.PublishMessageOne(_publisher);
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.IsTrue(subscriber2.One); // Did not change.
		}
	}
}
