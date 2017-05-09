// Copyright (c) 2011-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// SingletonsContainer tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SingletonsContainerTests // can't derive from BaseTest because of dependencies
	{
		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>Release the SingletonContainer.</summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Teardown()
		{
			SingletonsContainer.Release();
		}

		#endregion

		private class DummyDisposable : IDisposable
		{
			~DummyDisposable()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			}
		}

		private sealed class MyDisposable : IDisposable
		{
			public bool DisposeCalled { get; private set; }

			#region IDisposable Members
			/// <summary>Finalizer</summary>
			~MyDisposable()
			{
				Dispose(false);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or
			/// resetting unmanaged resources.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			#endregion

			private void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
				if (fDisposing)
					DisposeCalled = true;
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that singletons get properly disposed
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SingletonProperlyDisposed()
		{
			using (var singleton = new MyDisposable())
			{
				SingletonsContainer.Add(singleton.GetType().FullName, singleton);

				Assert.IsFalse(singleton.DisposeCalled);

				// Simulate application exit
				SingletonsContainer.Release();
				Assert.IsTrue(singleton.DisposeCalled);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that singletons get properly disposed. This tests the Add method that
		/// automatically calculates the key from the type of the object.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SingletonProperlyDisposedAutoKey()
		{
			using (var singleton = new MyDisposable())
			{
				SingletonsContainer.Add(singleton);

				Assert.IsFalse(singleton.DisposeCalled);

				// Simulate application exit
				SingletonsContainer.Release();
				Assert.IsTrue(singleton.DisposeCalled);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing a singleton from the container. Removing the singleton should not
		/// call dispose on it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		///
		[Test]
		public void RemoveSingleton()
		{
			using (var singleton = new MyDisposable())
			{
				SingletonsContainer.Add("foo", singleton);
				Assert.IsTrue(SingletonsContainer.Remove("foo"));
				Assert.IsFalse(singleton.DisposeCalled);
				Assert.IsNull(SingletonsContainer.Item("foo"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing a non-existing singleton from the container. Sh
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveNonExistingSingleton()
		{
			using (var singleton = new MyDisposable())
			{
				SingletonsContainer.Add("foo", singleton);
				Assert.IsFalse(SingletonsContainer.Remove("bar"));
				Assert.AreSame(singleton, SingletonsContainer.Item("foo"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a singleton.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RetrieveSingleton()
		{
			using (var singleton = new MyDisposable())
			{
				SingletonsContainer.Add(singleton);
				Assert.AreSame(singleton, SingletonsContainer.Item(typeof(MyDisposable).FullName));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a non-existing singleton.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RetrieveNonExistingSingleton()
		{
			using (var singleton = new MyDisposable())
			{
				SingletonsContainer.Add(singleton);
				Assert.IsNull(SingletonsContainer.Item("bla"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a singleton that didn't exist before. We expect it to get created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNonExisting()
		{
			var singleton = SingletonsContainer.Get<MyDisposable>();
			Assert.IsNotNull(singleton);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a singleton that was created before. We expect it to return the
		/// previously created one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetExisting()
		{
			using (var existingSingleton = new MyDisposable())
			{
				SingletonsContainer.Add(existingSingleton);
				var singleton = SingletonsContainer.Get<MyDisposable>();
				Assert.AreSame(existingSingleton, singleton);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a singleton that didn't exist before. We expect it to get created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNonExistingWithKey()
		{
			var singleton = SingletonsContainer.Get<MyDisposable>("foo");
			Assert.IsNotNull(singleton);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a singleton that was created before. We expect it to return the
		/// previously created one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetExistingWithKey()
		{
			using (var existingSingleton = new MyDisposable())
			{
				SingletonsContainer.Add("foo", existingSingleton);
				var singleton = SingletonsContainer.Get<MyDisposable>("foo");
				Assert.AreSame(existingSingleton, singleton);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a singleton by key that was created before but has the wrong type.
		/// We expect to get an exception.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetExistingWrongKey()
		{
			using (var existingSingleton = new DummyDisposable())
			{
				SingletonsContainer.Add("foo", existingSingleton);
				Assert.Throws(typeof(InvalidCastException),
					() => SingletonsContainer.Get<MyDisposable>("foo"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving singletons when we store more than one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetExistingReturnsCorrectOne()
		{
			using (var singleton1 = SingletonsContainer.Get<MyDisposable>())
			{
				using (var singleton2 = SingletonsContainer.Get<MyDisposable>("foo"))
				{
					Assert.AreNotSame(singleton1, singleton2);
					Assert.AreSame(singleton1, SingletonsContainer.Get<MyDisposable>());
					Assert.AreSame(singleton2, SingletonsContainer.Get<MyDisposable>("foo"));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Tests that adding a singleton with a key that already exists throws an
		/// exception.</summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddingTwiceThrowsException()
		{
			using (var singleton = new MyDisposable())
			{
				SingletonsContainer.Add(singleton);
				Assert.Throws(typeof(ArgumentException), () => SingletonsContainer.Add(singleton));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a singleton that didn't exist before. We expect it to get created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNonExistingWithCreateFunc()
		{
			var singleton = SingletonsContainer.Get("foo", () => new MyDisposable());
			Assert.IsNotNull(singleton);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a singleton that was created before. We expect it to return the
		/// previously created one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetExistingWithCreateFunc()
		{
			using (var existingSingleton = new MyDisposable())
			{
				SingletonsContainer.Add("foo", existingSingleton);
				var singleton = SingletonsContainer.Get("foo", () => new MyDisposable());
				Assert.AreSame(existingSingleton, singleton);
			}
		}

		/// <summary>
		/// Tests checking the existance of a singleton. In this case the singleton wasn't
		/// created before.
		/// </summary>
		[Test]
		public void ContainsNonExisting()
		{
			Assert.IsFalse(SingletonsContainer.Contains<MyDisposable>());
		}

		/// <summary>
		/// Tests checking the existance of a singleton. In this case the singleton was
		/// created before.
		/// </summary>
		[Test]
		public void ContainsExisting()
		{
			using (SingletonsContainer.Get<MyDisposable>())
			{
				Assert.IsTrue(SingletonsContainer.Contains<MyDisposable>());
			}
		}

		/// <summary>
		/// Tests checking the existance of a singleton with the specified key. In this case the
		/// singleton wasn't created before.
		/// </summary>
		[Test]
		public void ContainsNonExistingWithKey()
		{
			Assert.IsFalse(SingletonsContainer.Contains<MyDisposable>("foo"));
		}

		/// <summary>
		/// Tests checking the existance of a singleton with the specified key. In this case the
		/// singleton was created before.
		/// </summary>
		[Test]
		public void ContainsExistingWithKey()
		{
			using (SingletonsContainer.Get<MyDisposable>("foo"))
			{
				Assert.IsTrue(SingletonsContainer.Contains<MyDisposable>("foo"));
			}
		}

		/// <summary>
		/// Tests checking the existance of a singleton with the specified key. In this case a
		/// singleton with a different key was created before.
		/// </summary>
		[Test]
		public void ContainsWithDifferentKey()
		{
			using (SingletonsContainer.Get<MyDisposable>("foo"))
			{
				Assert.IsFalse(SingletonsContainer.Contains<MyDisposable>("bar"));
			}
		}

		/// <summary>
		/// Tests checking the existance of a singleton with the specified key. In this case a
		/// singleton with a different type was created before.
		/// </summary>
		[Test]
		public void ContainsWithDifferentType()
		{
			using (SingletonsContainer.Get<MyDisposable>("foo"))
			{
				Assert.IsFalse(SingletonsContainer.Contains<DummyDisposable>("foo"));
			}
		}
	}
}
