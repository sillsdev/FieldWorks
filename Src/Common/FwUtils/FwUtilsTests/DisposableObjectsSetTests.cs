// Copyright (c) 2011-2017 SIL International
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
		private class DummyDisposableObjectsSet<T> : DisposableObjectsSet<T> where T : class
		{
			public int Count
			{
				get { return m_ObjectsToDispose.Count; }
			}
		}

		#region Simple class that we can use for our tests
		private sealed class A : IDisposable
		{
			public A(string name)
			{
				Name = name;
			}

			~A()
			{
				Dispose(false);
			}

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

			public bool IsDisposed { get; private set; }

			public string Name { get; set; }

			public override bool Equals(object obj)
			{
				var other = obj as A;
				if (other == null)
					return false;
				return Name == other.Name;
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
			{
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
		}

		/// <summary />
		[Test]
		public void TwoDifferentObjectsWithDifferentNameGetBothDisposed()
		{
			using (var one = new A("one"))
			{
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
