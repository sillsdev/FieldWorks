// Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
using System;
using NUnit.Framework;

namespace SIL.Utils
{
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
		private sealed class A: IDisposable
		{
			public A(string name)
			{
				Name = name;
			}

			public void Dispose()
			{
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
