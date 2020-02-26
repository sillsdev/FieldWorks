// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.TestUtilities;
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
		/// <summary />
		private FlexComponentParameters _flexComponentParameters;

		/// <summary>
		/// Set up for each test.
		/// </summary>
		[SetUp]
		public void TestSetup()
		{
			_flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
		}

		/// <summary>
		/// Tear down after each test.
		/// </summary>
		[TearDown]
		public void TestTeardown()
		{
			TestSetupServices.DisposeTrash(_flexComponentParameters);
			_flexComponentParameters = null;
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
				Publisher = _flexComponentParameters.Publisher
			};
			subscriber.DoSubscriptions(_flexComponentParameters.Subscriber);
			subscriber.ShouldDoReentrantPublish = true;
			SomeRandomMessageSubscriber.DoSubscriptions(_flexComponentParameters.Subscriber);

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.DoesNotThrow(() => Publisher.PublishMessageOne(_flexComponentParameters.Publisher));
			subscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);
			SomeRandomMessageSubscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);
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
				Publisher = _flexComponentParameters.Publisher
			};
			subscriber.DoSubscriptions(_flexComponentParameters.Subscriber);
			subscriber.ShouldDoReentrantPublish = true;
			SomeRandomMessageSubscriber.DoSubscriptions(_flexComponentParameters.Subscriber);
			var niceGuyMultipleSubscriber = new NiceGuy_MultipleSubscriber();
			niceGuyMultipleSubscriber.DoSubscriptions(_flexComponentParameters.Subscriber);

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.DoesNotThrow(() => _flexComponentParameters.Publisher.Publish(new PublisherParameterObject("BadBoy", false)));
			subscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);
			SomeRandomMessageSubscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);
			niceGuyMultipleSubscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);
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
			subscriber.DoSubscriptions(_flexComponentParameters.Subscriber);

			// Run test.
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(int.MinValue, subscriber.Two);
			Publisher.PublishBothMessages(_flexComponentParameters.Publisher);
			Assert.IsFalse(subscriber.One);
			Assert.AreEqual(int.MaxValue, subscriber.Two);
			subscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);

			subscriber.One = true;
			subscriber.Two = int.MinValue;
			Publisher.PublishBothMessages(_flexComponentParameters.Publisher);
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
			subscriber.DoSubscriptions(_flexComponentParameters.Subscriber);

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(1, subscriber.Two);
			Publisher.PublishMessageOne(_flexComponentParameters.Publisher);
			Assert.IsFalse(subscriber.One); // Did change.
			Assert.AreEqual(1, subscriber.Two); // Did not change.

			subscriber.One = true;
			Assert.IsTrue(subscriber.One);
			subscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);
			Publisher.PublishMessageOne(_flexComponentParameters.Publisher);
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
			subscriber.DoSubscriptions(_flexComponentParameters.Subscriber);

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.AreEqual(1, subscriber.Two);

			Publisher.PublishMessageTwo(_flexComponentParameters.Publisher);
			Assert.IsTrue(subscriber.One); // Did not change.
			Assert.AreEqual(2, subscriber.Two); // Did change.

			subscriber.Two = 1;
			Assert.AreEqual(1, subscriber.Two);
			subscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);
			Publisher.PublishMessageTwo(_flexComponentParameters.Publisher);
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
			subscriber.DoSubscriptions(_flexComponentParameters.Subscriber);
			var subscriber2 = new DoubleMessageSubscriber
			{
				One = true
			};
			subscriber2.DoSubscriptions(_flexComponentParameters.Subscriber);

			// Run tests
			Assert.IsTrue(subscriber.One);
			Assert.IsTrue(subscriber2.One);

			Publisher.PublishMessageOne(_flexComponentParameters.Publisher);
			Assert.IsFalse(subscriber.One); // Did change.
			Assert.IsFalse(subscriber2.One); // Did change.

			subscriber.One = true;
			subscriber2.One = true;
			subscriber.DoUnsubscriptions(_flexComponentParameters.Subscriber);
			subscriber2.DoUnsubscriptions(_flexComponentParameters.Subscriber);
			Publisher.PublishMessageOne(_flexComponentParameters.Publisher);
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
			Assert.Throws<ArgumentNullException>(() => _flexComponentParameters.Publisher.Publish((IList<PublisherParameterObject>)null));
			var publisherParameterObject = new List<PublisherParameterObject>();
			Assert.Throws<InvalidOperationException>(() => _flexComponentParameters.Publisher.Publish(publisherParameterObject));
			publisherParameterObject.Add(new PublisherParameterObject("MessageOne"));
			Assert.Throws<InvalidOperationException>(() => _flexComponentParameters.Publisher.Publish(publisherParameterObject));
			publisherParameterObject.Add(new PublisherParameterObject("MessageTwo"));
			Assert.DoesNotThrow(() => _flexComponentParameters.Publisher.Publish(publisherParameterObject));

			// Test single message Publish method override.
			Assert.Throws<ArgumentNullException>(() => _flexComponentParameters.Publisher.Publish((PublisherParameterObject)null));
			Assert.DoesNotThrow(() => _flexComponentParameters.Publisher.Publish(publisherParameterObject[0]));
		}

		private static class Publisher
		{
			internal static void PublishMessageOne(IPublisher pubSystem)
			{
				pubSystem.Publish(new PublisherParameterObject("MessageOne", false));
			}

			internal static void PublishMessageTwo(IPublisher pubSystem)
			{
				pubSystem.Publish(new PublisherParameterObject("MessageTwo", 2));
			}

			internal static void PublishBothMessages(IPublisher pubSystem)
			{
				var messages = new List<PublisherParameterObject>
				{
					new PublisherParameterObject("MessageOne", false),
					new PublisherParameterObject("MessageTwo", int.MaxValue)
				};
				pubSystem.Publish(messages);
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

			internal static void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("SomeRandomMessage", SomeRandomMessageOneHandler);
			}

			internal static void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("SomeRandomMessage", SomeRandomMessageOneHandler);
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

			internal void DoSubscriptions(ISubscriber subscriber)
			{
				subscriber.Subscribe("MessageOne", SecondMessageOneHandler);
			}

			internal void DoUnsubscriptions(ISubscriber subscriber)
			{
				subscriber.Unsubscribe("MessageOne", SecondMessageOneHandler);
			}
		}

		private sealed class ReentrantSubscriber_SingleCall
		{
			private bool _one;
			internal IPublisher Publisher { get; set; }
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
						Publisher.Publish(new PublisherParameterObject("SomeRandomMessage", "Whatever"));
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

		private sealed class ReentrantSubscriber_Single_CallsMultiple
		{
			private bool _one;
			internal IPublisher Publisher { get; set; }
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
						Publisher.Publish(messages);
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

		private sealed class NiceGuy_MultipleSubscriber
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
	}
}