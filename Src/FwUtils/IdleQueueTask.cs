// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class represents a task that will be executed when
	/// the application is idle.
	/// </summary>
	public struct IdleQueueTask
	{
		/// <summary />
		public IdleQueueTask(IdleQueuePriority priority, Func<object, bool> del, object parameter)
		{
			Priority = priority;
			Delegate = del;
			Parameter = parameter;
		}

		/// <summary />
		public IdleQueueTask(Func<object, bool> del)
			: this(IdleQueuePriority.Medium, del, null)
		{
		}

		/// <summary>
		/// Gets the priority.
		/// </summary>
		public IdleQueuePriority Priority { get; }

		/// <summary>
		/// Gets the delegate.
		/// </summary>
		public Func<object, bool> Delegate { get; }

		/// <summary>
		/// Gets the parameter.
		/// </summary>
		public object Parameter { get; }

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <param name="obj">Another object to compare to.</param>
		/// <returns>
		/// true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			return obj is IdleQueueTask && Equals((IdleQueueTask)obj);
		}

		/// <summary>
		/// Determines if this task equals the specified task.
		/// </summary>
		public bool Equals(IdleQueueTask other)
		{
			return Delegate == other.Delegate;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this instance.
		/// </returns>
		public override int GetHashCode()
		{
			return Delegate.GetHashCode();
		}
	}
}