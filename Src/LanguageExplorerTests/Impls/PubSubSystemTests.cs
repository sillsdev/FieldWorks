// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorerTests.Impls
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

		private class SomeRandomMessageSubscriber
		{
			/// <summary>
			/// This is the subscribed message handler for "SomeRandomMessage" message.
			/// This is used in testing re-entrant calls.
			/// </summary>
			private void SomeRandomMessageOneHandler(object newValue)
			{
			}

			internal void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("SomeRandomMessage", SomeRandomMessageOneHandler);
			}

			internal void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("SomeRandomMessage", SomeRandomMessageOneHandler);
			}
		}

		private class SingleMessageSubscriber
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

		private class DoubleMessageSubscriber
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

		private class ReentrantSubscriber_SingleCall
		{
			private bool _one;
			internal IPublisher Publisher { get; set; }
			internal bool ShouldDoReentrantPublish { get; set; }

			internal bool One
			{
				get { return _one; }
				set
				{
					_one = value;
					if (ShouldDoReentrantPublish)
					{
						// Bad boy! Re-entrant test should fail on this.
						Publisher.Publish("SomeRandomMessage", "Whatever");
					}
				}
			}

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
				One = (bool)newValue; // NB: The bad part is in the setter, which fires off more Publish calls.
			}
		}

		private class ReentrantSubscriber_Single_CallsMultiple
		{
			private bool _one;
			internal IPublisher Publisher { get; set; }
			internal bool ShouldDoReentrantPublish { get; set; }

			internal bool One
			{
				get { return _one; }
				set
				{
					_one = value;
					if (ShouldDoReentrantPublish)
					{
						// Bad boy! Re-entrant test should fail on this.
						var commands = new List<string>
						{
							"MessageOne",
							"SomeRandomMessage"
						};
						var parms = new List<object>
						{
							false,
							"Whatever"
						};
						Publisher.Publish(commands, parms);
					}
				}
			}

			internal void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("BadBoy", ReentrantBadBoyHandler);
			}

			internal void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("BadBoy", ReentrantBadBoyHandler);
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void ReentrantBadBoyHandler(object newValue)
			{
				One = (bool)newValue; // NB: The bad part is in the setter, which fires off more Publish calls.
			}
		}

		private class ReentrantSubscriber_MultipleCalls
		{
			private bool _one;
			internal IPublisher Publisher { get; set; }

			internal bool One
			{
				get { return _one; }
				set
				{
					_one = value;
					if (ShouldDoReentrantPublish)
					{
						// Bad boy! Re-entrant test should fail on this.
						var commands = new List<string>
						{
							"MessageOne",
							"SomeRandomMessage"
						};
						var parms = new List<object>
						{
							false,
							"Whatever"
						};
						Publisher.Publish(commands, parms);
					}
				}
			}

			internal int Two { get; set; }
			internal bool ShouldDoReentrantPublish { get; set; }

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
				One = (bool)newValue; // NB: The bad part is in the setter, which fires off more Publish calls.
			}

			/// <summary>
			/// This is the subscribed message handler for "MessageOne" message.
			/// </summary>
			private void OrdinaryMessageTwoHandler(object newValue)
			{
				Two = (int)newValue;
			}
		}

		private class NiceGuy_MultipleSubscriber
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
			IPropertyTable propertyTable;
			TestSetupServices.SetupTestTriumvirate(out propertyTable, out _publisher, out _subscriber);
			propertyTable.Dispose(); // We don't really want it.
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
		/// This tests the code path of: Single pub call throws on next single pub call.
		/// </summary>
		[Test]
		public void Ordinary_Rentry_Throws()
		{
			// Set up.
			var subscriber = new ReentrantSubscriber_SingleCall
			{
				One = true,
				Publisher = _publisher
			};
			subscriber.DoSubscriptions(_subscriber);
			subscriber.ShouldDoReentrantPublish = true;
			var someRandomSubscriber = new SomeRandomMessageSubscriber();
			someRandomSubscriber.DoSubscriptions(_subscriber);

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.Throws<ApplicationException>(() => Publisher.PublishMessageOne(_publisher));
			subscriber.DoUnsubscriptions(_subscriber);
			someRandomSubscriber.DoUnsubscriptions(_subscriber);
		}

		/// <summary>
		/// This tests the code path of: Single publisher handler then calls a multiple publisher.
		/// </summary>
		[Test]
		public void Single_Publisher_Handler_Calls_Multiple_Publisher_on_Rentry_Which_Throws()
		{
			// Set up.
			var subscriber = new ReentrantSubscriber_Single_CallsMultiple
			{
				One = true,
				Publisher = _publisher
			};
			subscriber.DoSubscriptions(_subscriber);
			subscriber.ShouldDoReentrantPublish = true;
			var someRandomSubscriber = new SomeRandomMessageSubscriber();
			someRandomSubscriber.DoSubscriptions(_subscriber);
			var niceGuyMultipleSubscriber = new NiceGuy_MultipleSubscriber();
			niceGuyMultipleSubscriber.DoSubscriptions(_subscriber);

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.Throws<ApplicationException>(() => _publisher.Publish("BadBoy", false));
			subscriber.DoUnsubscriptions(_subscriber);
			someRandomSubscriber.DoUnsubscriptions(_subscriber);
			niceGuyMultipleSubscriber.DoUnsubscriptions(_subscriber);
		}

		/// <summary>
		/// This tests the code path of: Multi pub call throws on next multi pub call.
		/// </summary>
		[Test]
		public void Multiple_Rentry_Throws()
		{
			// Set up.
			var subscriber = new ReentrantSubscriber_MultipleCalls
			{
				One = true,
				Two = int.MinValue,
				Publisher = _publisher
			};
			subscriber.DoSubscriptions(_subscriber);
			subscriber.ShouldDoReentrantPublish = true;

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);
			Assert.Throws<ApplicationException>(() => Publisher.PublishBothMessages(_publisher));
			subscriber.DoUnsubscriptions(_subscriber);
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
			subscriber.DoSubscriptions(_subscriber);
			var subscriber2 = new DoubleMessageSubscriber
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
