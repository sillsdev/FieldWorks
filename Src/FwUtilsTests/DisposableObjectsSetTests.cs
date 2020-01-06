// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	[TestFixture]
	public class DisposableObjectsSetTests
	{
		private sealed class DummyDisposableObjectsSet<T> : DisposableObjectsSet<T> where T : class
		{
			internal int Count => m_ObjectsToDispose.Count;
		}

		#region Simple class that we can use for our tests
		private sealed class A : IDisposable
		{
			internal A(string name)
			{
				Name = name;
			}

			~A()
			{
				Dispose(false);
			}

			/// <inheritdoc />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");
				IsDisposed = true;
			}

			internal bool IsDisposed { get; private set; }

			internal string Name { private get; set; }

			public override bool Equals(object obj)
			{
				var other = obj as A;
				return other != null && Name == other.Name;
			}

			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}
		#endregion

		/// <summary />
		[Test]
		public void TwoDifferentObjectsWithSameNameGetBothDisposed()
		{
			using (var one = new A("name"))
			using (var two = new A("name"))
			{
				using (var sut = new DummyDisposableObjectsSet<A>())
				{
					sut.Add(one);
					sut.Add(two);

					Assert.AreEqual(2, sut.Count);
				}

				Assert.IsTrue(one.IsDisposed);
				Assert.IsTrue(two.IsDisposed);
			}
		}

		/// <summary />
		[Test]
		public void TwoDifferentObjectsWithDifferentNameGetBothDisposed()
		{
			using (var one = new A("one"))
			using (var two = new A("two"))
			{
				using (var sut = new DummyDisposableObjectsSet<A>())
				{
					sut.Add(one);
					sut.Add(two);

					Assert.AreEqual(2, sut.Count);
				}

				Assert.IsTrue(one.IsDisposed);
				Assert.IsTrue(two.IsDisposed);
			}
		}

		/// <summary />
		[Test]
		public void SameReferenceIsAddedOnlyOnce()
		{
			using (var one = new A("name"))
			{
				var two = one;
				using (var sut = new DummyDisposableObjectsSet<A>())
				{
					sut.Add(one);
					sut.Add(two);

					Assert.AreEqual(1, sut.Count);
				}

				Assert.IsTrue(one.IsDisposed);
			}
		}

		/// <summary />
		[Test]
		public void SameReferenceWithDifferentNameIsAddedOnlyOnce()
		{
			using (var one = new A("name"))
			{
				var two = one;
				using (var sut = new DummyDisposableObjectsSet<A>())
				{
					sut.Add(one);
					two.Name = "changed";
					sut.Add(two);

					Assert.AreEqual(1, sut.Count);
				}

				Assert.IsTrue(one.IsDisposed);
			}
		}
	}
}