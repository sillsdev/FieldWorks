#if !NET_4_0
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System
{
	/// <summary>
	/// Provides static methods for creating tuple objects. The tuple classes are intended to mimic
	/// the interface of the tuple classes in .NET 4. When we start using .NET 4, these classes can be
	/// removed.
	/// </summary>
	public static class Tuple
	{
		/// <summary>
		/// Creates a new 2-tuple, or pair.
		/// </summary>
		/// <typeparam name="T1">The type of the first item.</typeparam>
		/// <typeparam name="T2">The type of the second item.</typeparam>
		/// <param name="item1">The first item.</param>
		/// <param name="item2">The second item.</param>
		/// <returns>A 2-tuple.</returns>
		public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
		{
			return new Tuple<T1, T2>(item1, item2);
		}

		/// <summary>
		/// Creates a new 3-tuple, or triple.
		/// </summary>
		/// <typeparam name="T1">The type of the first item.</typeparam>
		/// <typeparam name="T2">The type of the second item.</typeparam>
		/// <typeparam name="T3">The type of the third item.</typeparam>
		/// <param name="item1">The first item.</param>
		/// <param name="item2">The second item.</param>
		/// <param name="item3">The third item.</param>
		/// <returns>A 3-tuple.</returns>
		public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
		{
			return new Tuple<T1, T2, T3>(item1, item2, item3);
		}
	}

	/// <summary>
	/// Represents a 2-tuple, or pair.
	/// </summary>
	/// <typeparam name="T1">The type of the first item.</typeparam>
	/// <typeparam name="T2">The type of the second item.</typeparam>
	[DebuggerDisplay("({Item1}, {Item2})")]
	public class Tuple<T1, T2> : IComparable<Tuple<T1, T2>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Tuple&lt;T1, T2&gt;"/> class.
		/// </summary>
		/// <param name="item1">The first item.</param>
		/// <param name="item2">The second item.</param>
		public Tuple(T1 item1, T2 item2)
		{
			Item1 = item1;
			Item2 = item2;
		}

		/// <summary>
		/// Gets the first item.
		/// </summary>
		/// <value>The first item.</value>
		public T1 Item1
		{
			get; private set;
		}

		/// <summary>
		/// Gets the second item.
		/// </summary>
		/// <value>The second item.</value>
		public T2 Item2
		{
			get; private set;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		public override bool Equals(object obj)
		{
			if (obj == null)
				throw new NullReferenceException();

			if (!(obj is Tuple<T1, T2>))
				return false;

			var key = (Tuple<T1, T2>)obj;
			return EqualityComparer<T1>.Default.Equals(Item1, key.Item1)
				   && EqualityComparer<T2>.Default.Equals(Item2, key.Item2);
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			return (Item1 == null ? 0 : Item1.GetHashCode()) ^ (Item2 == null ? 0 : Item2.GetHashCode());
		}

		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public int CompareTo(Tuple<T1, T2> other)
		{
			if(EqualityComparer<T1>.Default.Equals(Item1, other.Item1)) //first part of the pair is equal
			{
				if(Item2 is IComparable) //use the comparer if the second part of our pair is valid for Comparer
					return Comparer<T2>.Default.Compare(Item2, other.Item2);

				if(other.Item2 != null) //if the other item 2 is not null, and this Item2 is not comparable, hash and subtract
				{
					//There is a very small chance of hash collision causing this to go wrong, but this method will
					//go away when we upgrade to the next version of C#, and I don't feel like writing the code required to do a
					//better job.
					return Item2.GetHashCode() - other.Item2.GetHashCode();
				}
			}
			else if(Item1 is IComparable) //if the first part of the pair differs, and it is comparable, compare it
				return Comparer<T1>.Default.Compare(Item1, other.Item1);
			if(other.Item1 != null) //the first part of this tuple is not comparable, hash and subtract.
			{
				return Item1.GetHashCode() - other.Item1.GetHashCode();
			}
			return 1; //Some part of other is null, so we must be better, we'll say that means greater
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0}, {1})", Item1, Item2);
		}
	}

	/// <summary>
	/// Represents a 3-tuple, or triple.
	/// </summary>
	/// <typeparam name="T1">The type of the first item.</typeparam>
	/// <typeparam name="T2">The type of the second item.</typeparam>
	/// <typeparam name="T3">The type of the third item.</typeparam>
	[DebuggerDisplay("({Item1}, {Item2}, {Item3})")]
	public class Tuple<T1, T2, T3>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Tuple&lt;T1, T2, T3&gt;"/> class.
		/// </summary>
		/// <param name="item1">The first item.</param>
		/// <param name="item2">The second item.</param>
		/// <param name="item3">The third item.</param>
		public Tuple(T1 item1, T2 item2, T3 item3)
		{
			Item1 = item1;
			Item2 = item2;
			Item3 = item3;
		}

		/// <summary>
		/// Gets the first item.
		/// </summary>
		/// <value>The first item.</value>
		public T1 Item1
		{
			get; private set;
		}

		/// <summary>
		/// Gets the second item.
		/// </summary>
		/// <value>The second item.</value>
		public T2 Item2
		{
			get; private set;
		}

		/// <summary>
		/// Gets the third item.
		/// </summary>
		/// <value>The third item.</value>
		public T3 Item3
		{
			get; private set;
		}

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		public override bool Equals(object obj)
		{
			if (obj == null)
				throw new NullReferenceException();

			if (!(obj is Tuple<T1, T2, T3>))
				return false;

			var tuple = (Tuple<T1, T2, T3>)obj;
			return EqualityComparer<T1>.Default.Equals(Item1, tuple.Item1)
				   && EqualityComparer<T2>.Default.Equals(Item2, tuple.Item2)
				   && EqualityComparer<T3>.Default.Equals(Item3, tuple.Item3);
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode()
		{
			return (Item1 == null ? 0 : Item1.GetHashCode()) ^ (Item2 == null ? 0 : Item2.GetHashCode())
				^ (Item3 == null ? 0 : Item3.GetHashCode());
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return String.Format("({0}, {1}, {2})", Item1, Item2, Item3);
		}
	}
}
#endif