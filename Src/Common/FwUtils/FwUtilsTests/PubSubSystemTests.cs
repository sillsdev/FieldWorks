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
			Assert.That(subscriber.One, Is.True);
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
			Assert.That(subscriber.One, Is.True);
			Assert.DoesNotThrow(() => FwUtils.Publisher.Publish(new PublisherParameterObject("BadBoy", false, null)));
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
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(int.MinValue));
			TestPublisher.PublishBothMessages();
			Assert.That(subscriber.One, Is.False);
			Assert.That(subscriber.Two, Is.EqualTo(int.MaxValue));
			subscriber.DoUnsubscriptions();

			subscriber.One = true;
			subscriber.Two = int.MinValue;
			TestPublisher.PublishBothMessages();
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(int.MinValue));
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
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(1));
			TestPublisher.PublishMessageOne();
			Assert.That(subscriber.One, Is.False); // Did change.
			Assert.That(subscriber.Two, Is.EqualTo(1)); // Did not change.

			subscriber.One = true;
			Assert.That(subscriber.One, Is.True);
			subscriber.DoUnsubscriptions();
			TestPublisher.PublishMessageOne();
			Assert.That(subscriber.One, Is.True); // Did not change.
			Assert.That(subscriber.Two, Is.EqualTo(1)); // Did not change.
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
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(1));

			TestPublisher.PublishMessageTwo();
			Assert.That(subscriber.One, Is.True); // Did not change.
			Assert.That(subscriber.Two, Is.EqualTo(2)); // Did change.

			subscriber.Two = 1;
			Assert.That(subscriber.Two, Is.EqualTo(1));
			subscriber.DoUnsubscriptions();
			TestPublisher.PublishMessageTwo();
			Assert.That(subscriber.One, Is.True); // Did not change.
			Assert.That(subscriber.Two, Is.EqualTo(1)); // Did not change.
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
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber2.One, Is.True);

			TestPublisher.PublishMessageOne();
			Assert.That(subscriber.One, Is.False); // Did change.
			Assert.That(subscriber2.One, Is.False); // Did change.

			subscriber.One = true;
			subscriber2.One = true;
			subscriber.DoUnsubscriptions();
			subscriber2.DoUnsubscriptions();
			TestPublisher.PublishMessageOne();
			Assert.That(subscriber.One, Is.True); // Did not change.
			Assert.That(subscriber2.One, Is.True); // Did not change.
		}

		/// <summary>
		/// A scoped publish goes to same-scope and unscoped subscribers, but not to
		/// subscribers with a different scope.
		/// </summary>
		[Test]
		public void Scoped_Publish_Delivers_Only_To_Matching_Scope_And_Unscoped_Subscribers()
		{
			// Set up.
			var scopeA = new TestPubSubScope();
			var subscriberA = new ScopeAwareSubscriber(scopeA) { One = true };
			var subscriberB = new ScopeAwareSubscriber(new TestPubSubScope()) { One = true };
			var unscopedSubscriber = new ScopeAwareSubscriber(null) { One = true };
			subscriberA.DoSubscriptions();
			subscriberB.DoSubscriptions();
			unscopedSubscriber.DoSubscriptions();

			// Run test.
			FwUtils.Publisher.Publish(new PublisherParameterObject("MessageOne", false, scopeA));
			Assert.That(subscriberA.One, Is.False); // Same scope: delivered.
			Assert.That(subscriberB.One, Is.True); // Different scope: not delivered.
			Assert.That(unscopedSubscriber.One, Is.False); // Unscoped subscriber: delivered.

			subscriberA.DoUnsubscriptions();
			subscriberB.DoUnsubscriptions();
			unscopedSubscriber.DoUnsubscriptions();
		}

		/// <summary>
		/// An unscoped publish is process-wide: every subscriber receives it, scoped or not.
		/// </summary>
		[Test]
		public void Unscoped_Publish_Delivers_To_Scoped_And_Unscoped_Subscribers()
		{
			// Set up.
			var scopedSubscriber = new ScopeAwareSubscriber(new TestPubSubScope()) { One = true };
			var unscopedSubscriber = new ScopeAwareSubscriber(null) { One = true };
			scopedSubscriber.DoSubscriptions();
			unscopedSubscriber.DoSubscriptions();

			// Run test.
			FwUtils.Publisher.Publish(new PublisherParameterObject("MessageOne", false, null));
			Assert.That(scopedSubscriber.One, Is.False); // Delivered.
			Assert.That(unscopedSubscriber.One, Is.False); // Delivered.

			scopedSubscriber.DoUnsubscriptions();
			unscopedSubscriber.DoUnsubscriptions();
		}

		/// <summary>
		/// The scope must survive the EndOfActionManager round trip through the idle queue.
		/// </summary>
		[Test]
		public void Scope_Is_Preserved_Through_PublishAtEndOfAction()
		{
			// Set up.
			var scopeA = new TestPubSubScope();
			var subscriberA = new ScopeAwareSubscriber(scopeA) { Two = int.MinValue };
			var subscriberB = new ScopeAwareSubscriber(new TestPubSubScope()) { Two = int.MinValue };
			subscriberA.DoSubscriptions();
			subscriberB.DoSubscriptions();

			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, int.MaxValue, scopeA));

			// Nothing is delivered until the idle queue drains.
			Assert.That(subscriberA.Two, Is.EqualTo(int.MinValue));
			Assert.That(subscriberB.Two, Is.EqualTo(int.MinValue));

			// SUT - Process the EndOfActionManager IdleQueue.
			FwUtils.Publisher.EndOfActionManager.IdleEndOfAction(null);

			Assert.That(subscriberA.Two, Is.EqualTo(int.MaxValue)); // Same scope: delivered.
			Assert.That(subscriberB.Two, Is.EqualTo(int.MinValue)); // Different scope: not delivered.

			subscriberA.DoUnsubscriptions();
			subscriberB.DoUnsubscriptions();
		}

		/// <summary>
		/// EndOfAction coalescing is per (message, scope): the same message queued from two
		/// scopes is delivered to both, while a re-publish from the same scope overwrites.
		/// </summary>
		[Test]
		public void PublishAtEndOfAction_Coalesces_Per_Scope()
		{
			// Set up.
			var scopeA = new TestPubSubScope();
			var scopeB = new TestPubSubScope();
			var subscriberA = new ScopeAwareSubscriber(scopeA) { Two = int.MinValue };
			var subscriberB = new ScopeAwareSubscriber(scopeB) { Two = int.MinValue };
			subscriberA.DoSubscriptions();
			subscriberB.DoSubscriptions();

			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, 1, scopeA));
			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, 2, scopeB));
			// Same scope again: coalesces with (overwrites) the first scopeA publish.
			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, 3, scopeA));

			// SUT - Process the EndOfActionManager IdleQueue.
			FwUtils.Publisher.EndOfActionManager.IdleEndOfAction(null);

			Assert.That(subscriberA.Two, Is.EqualTo(3)); // Latest scopeA publish won.
			Assert.That(subscriberB.Two, Is.EqualTo(2)); // scopeB publish survived independently.

			subscriberA.DoUnsubscriptions();
			subscriberB.DoUnsubscriptions();
		}

		/// <summary>
		/// Prefix subscriptions honor the same scope rule as specific subscriptions:
		/// scoped publishes skip other-scope prefix subscribers and reach same-scope and
		/// unscoped ones; non-matching prefixes are never delivered.
		/// </summary>
		[Test]
		public void Prefix_Subscriptions_Honor_Scope()
		{
			// Set up.
			var scopeA = new TestPubSubScope();
			var subscriberA = new PrefixScopeAwareSubscriber(scopeA);
			var subscriberB = new PrefixScopeAwareSubscriber(new TestPubSubScope());
			var unscopedSubscriber = new PrefixScopeAwareSubscriber(null);
			subscriberA.DoSubscriptions();
			subscriberB.DoSubscriptions();
			unscopedSubscriber.DoSubscriptions();

			// Run test.
			FwUtils.Publisher.Publish(new PublisherParameterObject("PrefixedMessageOne", 7, scopeA));
			Assert.That(subscriberA.LastMessage, Is.EqualTo("PrefixedMessageOne")); // Same scope: delivered.
			Assert.That(subscriberB.LastMessage, Is.Null); // Different scope: not delivered.
			Assert.That(unscopedSubscriber.LastMessage, Is.EqualTo("PrefixedMessageOne")); // Unscoped subscriber: delivered.

			subscriberA.LastMessage = null;
			unscopedSubscriber.LastMessage = null;
			FwUtils.Publisher.Publish(new PublisherParameterObject("UnrelatedMessage", 7, scopeA));
			Assert.That(subscriberA.LastMessage, Is.Null); // Prefix does not match: not delivered.
			Assert.That(unscopedSubscriber.LastMessage, Is.Null);

			subscriberA.DoUnsubscriptions();
			subscriberB.DoUnsubscriptions();
			unscopedSubscriber.DoUnsubscriptions();
		}

		[Test]
		[TestCase(null)]
		[TestCase("")]
		[TestCase(" ")]
		public void IllFormedPublisherParameterObjectThrows(string message)
		{
			Assert.Throws<ArgumentNullException>(() => { var dummy = new PublisherParameterObject(message, null, null); });
		}

		[Test]
		public void VerifyPublishMessageCalls()
		{
			// Test multiple call Publish override.
			Assert.Throws<ArgumentNullException>(() => FwUtils.Publisher.Publish((IList<PublisherParameterObject>)null));
			var publisherParameterObject = new List<PublisherParameterObject>();
			Assert.Throws<InvalidOperationException>(() => FwUtils.Publisher.Publish(publisherParameterObject));
			publisherParameterObject.Add(new PublisherParameterObject("MessageOne", null, null));
			Assert.Throws<InvalidOperationException>(() => FwUtils.Publisher.Publish(publisherParameterObject));
			publisherParameterObject.Add(new PublisherParameterObject("MessageTwo", null, null));
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

			Assert.That(subscriber.First, Is.Null);
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(int.MinValue));

			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.RecordNavigation, false, null));
			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, int.MaxValue, null));

			// Confirm that nothing changed.
			Assert.That(subscriber.First, Is.Null);
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(int.MinValue));

			// SUT - Process the EndOfActionManager IdleQueue.
			FwUtils.Publisher.EndOfActionManager.IdleEndOfAction(null);

			Assert.That(subscriber.First, Is.EqualTo(EventConstants.RecordNavigation));
			Assert.That(subscriber.One, Is.False);
			Assert.That(subscriber.Two, Is.EqualTo(int.MaxValue));
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

			Assert.That(subscriber.First, Is.Null);
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(int.MinValue));

			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, int.MaxValue, null));
			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.RecordNavigation, false, null));

			// Confirm that nothing changed.
			Assert.That(subscriber.First, Is.Null);
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(int.MinValue));

			// SUT - Process the EndOfActionManager IdleQueue.
			FwUtils.Publisher.EndOfActionManager.IdleEndOfAction(null);

			Assert.That(subscriber.First, Is.EqualTo(EventConstants.RecordNavigation));
			Assert.That(subscriber.One, Is.False);
			Assert.That(subscriber.Two, Is.EqualTo(int.MaxValue));
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

			Assert.That(subscriber.First, Is.Null);
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(int.MinValue));

			FwUtils.Publisher.PublishAtEndOfAction(new PublisherParameterObject(EventConstants.SelectionChanged, int.MaxValue, null));

			// Confirm that nothing changed.
			Assert.That(subscriber.First, Is.Null);
			Assert.That(subscriber.One, Is.True);
			Assert.That(subscriber.Two, Is.EqualTo(int.MinValue));

			// SUT - Process the EndOfActionManager IdleQueue.
			FwUtils.Publisher.EndOfActionManager.IdleEndOfAction(null);

			Assert.That(subscriber.First, Is.EqualTo(EventConstants.SelectionChanged));
			Assert.That(subscriber.One, Is.True);	// Doesn't change.
			Assert.That(subscriber.Two, Is.EqualTo(int.MaxValue));
			subscriber.DoUnsubscriptions();
		}

		private static class TestPublisher
		{
			internal static void PublishMessageOne()
			{
				FwUtils.Publisher.Publish(new PublisherParameterObject("MessageOne", false, null));
			}

			internal static void PublishMessageTwo()
			{
				FwUtils.Publisher.Publish(new PublisherParameterObject("MessageTwo", 2, null));
			}

			internal static void PublishBothMessages()
			{
				var messages = new List<PublisherParameterObject>
				{
					new PublisherParameterObject("MessageOne", false, null),
					new PublisherParameterObject("MessageTwo", int.MaxValue, null)
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
				FwUtils.Subscriber.Subscribe("SomeRandomMessage", SomeRandomMessageOneHandler, null);
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
				FwUtils.Subscriber.Subscribe("MessageOne", MessageOneHandler, null);
				FwUtils.Subscriber.Subscribe("MessageTwo", MessageTwoHandler, null);
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
				FwUtils.Subscriber.Subscribe("MessageOne", SecondMessageOneHandler, null);
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
						FwUtils.Publisher.Publish(new PublisherParameterObject("SomeRandomMessage", "Whatever", null));
					}
				}
			}

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("MessageOne", ReentrantMessageOneHandler, null);
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
							new PublisherParameterObject("MessageOne", false, null),
							new PublisherParameterObject("SomeRandomMessage", "Whatever", null)
						};
						FwUtils.Publisher.Publish(messages);
					}
				}
			}

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("BadBoy", ReentrantBadBoyHandler, null);
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
				FwUtils.Subscriber.Subscribe("MessageOne", MessageOneHandler, null);
				FwUtils.Subscriber.Subscribe("MessageTwo", MessageTwoHandler, null);
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

		private sealed class TestPubSubScope : IPubSubScope
		{
		}

		private sealed class PrefixScopeAwareSubscriber
		{
			private readonly IPubSubScope _scope;

			internal PrefixScopeAwareSubscriber(IPubSubScope scope)
			{
				_scope = scope;
			}

			internal string LastMessage { get; set; }

			/// <summary>
			/// This is the subscribed prefix handler for messages starting with "PrefixedMessage".
			/// </summary>
			private void PrefixedMessageHandler(string message, object newValue)
			{
				LastMessage = message;
			}

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.PrefixSubscribe("PrefixedMessage", PrefixedMessageHandler, _scope);
			}

			internal void DoUnsubscriptions()
			{
				FwUtils.Subscriber.PrefixUnsubscribe("PrefixedMessage", PrefixedMessageHandler);
			}
		}

		private sealed class ScopeAwareSubscriber
		{
			private readonly IPubSubScope _scope;

			internal ScopeAwareSubscriber(IPubSubScope scope)
			{
				_scope = scope;
			}

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
			/// This is the subscribed message handler for the SelectionChanged message.
			/// </summary>
			private void SelectionChangedHandler(object newValue)
			{
				Two = (int)newValue;
			}

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe("MessageOne", MessageOneHandler, _scope);
				FwUtils.Subscriber.Subscribe(EventConstants.SelectionChanged, SelectionChangedHandler, _scope);
			}

			internal void DoUnsubscriptions()
			{
				FwUtils.Subscriber.Unsubscribe("MessageOne", MessageOneHandler);
				FwUtils.Subscriber.Unsubscribe(EventConstants.SelectionChanged, SelectionChangedHandler);
			}
		}

		private sealed class EndOfAction_MultipleSubscriber
		{
			internal string First { get; set; }
			internal bool One { get; set; }
			internal int Two { get; set; }

			internal void DoSubscriptions()
			{
				FwUtils.Subscriber.Subscribe(EventConstants.RecordNavigation, RecordNavigationHandler, null);
				FwUtils.Subscriber.Subscribe(EventConstants.SelectionChanged, SelectionChangedHandler, null);
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