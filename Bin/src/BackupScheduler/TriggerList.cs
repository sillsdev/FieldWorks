/*
 *
 * This file was taken from http://www.codeproject.com/KB/cs/tsnewlib.aspx
 * ("A New Task Scheduler Class Library for .NET" by Dennis Austin)
 * This file is licensed under The Code Project Open License (CPOL):
 * http://www.codeproject.com/info/cpol10.aspx
 *
 */

using System;
using System.Collections;
using TaskSchedulerInterop;

namespace TaskScheduler {

	/// <summary>
	/// TriggerList is a collection of Triggers.  Every Task has a TriggerList that is
	/// created and destroyed automatically along with the Task.  There are no public constructors.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A TriggerList can be empty, and indeed a newly created Task has an empty list.
	/// It's not clear how the system handles a task with no triggers, however.</para>
	/// <para>
	/// TriggerList implements IList and behaves like other indexable collections with one limitation:
	/// You can'task insert a Trigger at a position.  <c>Insert()</c> throws NotImplementedException.  This
	/// restriction is based on the underlying API. </para>
	/// </remarks>
	public class TriggerList : IList, IDisposable {
		// Internal COM interface to access task that this list is associated with.
		private ITask iTask;
		// Trigger objects store in an ArrayList
		private ArrayList oTriggers;

		/// <summary>
		/// Internal constructor creates TriggerList using an ITask interface to initialize.
		/// </summary>
		/// <param name="iTask">Instance of an ITask.</param>
		internal TriggerList(ITask iTask) {
			this.iTask = iTask;
			ushort cnt = 0;
			iTask.GetTriggerCount(out cnt);
			oTriggers = new ArrayList(cnt+5); //Allow for five additional entries without growing base array
			for (int i=0; i<cnt; i++) {
				ITaskTrigger iTaskTrigger;
				iTask.GetTrigger((ushort)i, out iTaskTrigger);
				oTriggers.Add(Trigger.CreateTrigger(iTaskTrigger));
			}
		}

		/// <summary>
		/// Enumerator for TriggerList; implements IEnumerator interface.
		/// </summary>
		private class Enumerator : IEnumerator {
			private TriggerList outer;
			private int currentIndex;

			/// <summary>
			/// Internal constructor - Only accessible through <see cref="IEnumerable.GetEnumerator()"/>.
			/// </summary>
			/// <param name="outer">Instance of a TriggerList.</param>
			internal Enumerator(TriggerList outer) {
				this.outer = outer;
				Reset();
			}

			/// <summary>
			/// Moves to the next trigger. See <see cref="IEnumerator.MoveNext()"/> for more information.
			/// </summary>
			/// <returns>False if there is no next trigger.</returns>
			public bool MoveNext() {
				return ++currentIndex < outer.oTriggers.Count;
			}

			/// <summary>
			/// Reset trigger enumeration. See <see cref="IEnumerator.Reset()"/> for more information.
			/// </summary>
			public void Reset() {
				currentIndex = -1;
			}

			/// <summary>
			/// Retrieves the current trigger.  See <see cref="IEnumerator.Current"/> for more information.
			/// </summary>
			public object Current {
				get { return outer.oTriggers[currentIndex]; }
			}
		}

		#region Implementation of IList
		/// <summary>
		/// Removes the trigger at a specified index.
		/// </summary>
		/// <param name="index">Index of trigger to remove.</param>
		/// <exception cref="ArgumentOutOfRangeException">Index out of range.</exception>
		public void RemoveAt(int index) {
			if (index >= Count)
				throw new ArgumentOutOfRangeException("index", index, "Failed to remove Trigger. Index out of range.");
			((Trigger)oTriggers[index]).Unbind(); //releases resources in the trigger
			oTriggers.RemoveAt(index); //Remove the Trigger object from the array representing the list
			iTask.DeleteTrigger((ushort)index); //Remove the trigger from the Task Scheduler
		}

		/// <summary>
		/// Not implemented; throws NotImplementedException.
		/// If implemented, would insert a trigger at a specified index.
		/// </summary>
		/// <param name="index">Index to insert trigger.</param>
		/// <param name="value">Value of trigger to insert.</param>
		void IList.Insert(int index, object value) {
			throw new NotImplementedException("TriggerList does not support Insert().");
		}

		/// <summary>
		/// Removes the trigger from the collection.  If the trigger is not in
		/// the collection, nothing happens.  (No exception.)
		/// </summary>
		/// <param name="trigger">Trigger to remove.</param>
		public void Remove(Trigger trigger) {
			int i = IndexOf(trigger);
			if (i != -1)
				RemoveAt(i);
		}

		/// <summary>
		/// IList.Remove implementation.
		/// </summary>
		void IList.Remove(object value) {
			Remove(value as Trigger);
		}

		/// <summary>
		/// Test to see if trigger is part of the collection.
		/// </summary>
		/// <param name="trigger">Trigger to find.</param>
		/// <returns>true if trigger found in collection.</returns>
		public bool Contains(Trigger trigger) {
			return (IndexOf(trigger) != -1);
		}

		/// <summary>
		/// IList.Contains implementation.
		/// </summary>
		bool IList.Contains(object value) {
			return Contains(value as Trigger);
		}

		/// <summary>
		/// Remove all triggers from collection.
		/// </summary>
		public void Clear() {
			for (int i = Count-1; i >= 0; i--)  {
				RemoveAt(i);
			}
		}

		/// <summary>
		/// Returns the index of the supplied Trigger.
		/// </summary>
		/// <param name="trigger">Trigger to find.</param>
		/// <returns>Zero based index in collection, -1 if not a member.</returns>
		public int IndexOf(Trigger trigger) {
			for (int i = 0; i < Count; i++) {
				if (this[i].Equals(trigger))
					return i;
			}
			return -1;
		}

		/// <summary>
		/// IList.IndexOf implementation.
		/// </summary>
		int IList.IndexOf(object value) {
			return IndexOf(value as Trigger);
		}

		/// <summary>
		/// Add the supplied Trigger to the collection.  The Trigger to be added must be unbound,
		/// i.e. it must not be a current member of a TriggerList--this or any other.
		/// </summary>
		/// <param name="trigger">Trigger to add.</param>
		/// <returns>Index of added trigger.</returns>
		/// <exception cref="ArgumentException">Trigger being added is already bound.</exception>
		public int Add(Trigger trigger) {
			// if trigger is already bound a list throw an exception
			if (trigger.Bound)
				throw new ArgumentException("A Trigger cannot be added if it is already in a list.");
			// Add a trigger to the task for this TaskList
			ITaskTrigger iTrigger;
			ushort index;
			iTask.CreateTrigger(out index, out iTrigger);
			// Add the Trigger to the TaskList
			trigger.Bind(iTrigger);
			int index2 = oTriggers.Add(trigger);
			// Verify index is the same in task and in list
			if (index2 != (int)index)
				throw new ApplicationException("Assertion Failure");
			return (int)index;
		}

		/// <summary>
		/// IList.Add implementation.
		/// </summary>
		int IList.Add(object value) {
			return Add(value as Trigger);
		}

		/// <summary>
		/// Gets read-only state of collection. Always false for TriggerLists.
		/// </summary>
		public bool IsReadOnly {
			get { return false; }
		}

		/// <summary>
		/// Access the Trigger at a specified index.  Assigning to a TriggerList element requires
		/// the value to unbound.  The previous list element becomes unbound and lost,
		/// while the newly assigned Trigger becomes bound in its place.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Collection index out of range.</exception>
		public Trigger this[int index] {
			get {
				if (index >= Count)
					throw new ArgumentOutOfRangeException("index", index, "TriggerList collection");
				return (Trigger)oTriggers[index];
			}
			set {
				if (index >= Count)
					throw new ArgumentOutOfRangeException("index", index, "TriggerList collection");
				Trigger previous = (Trigger)oTriggers[index];
				value.Bind(previous);
				oTriggers[index] = value;
			}
		}

		/// <summary>
		/// IList.this[int] implementation.
		/// </summary>
		object IList.this[int index] {
			get { return this[index]; }
			set { this[index] = (value as Trigger); }
		}

		/// <summary>
		/// Returns whether collection is a fixed size. Always returns false for TriggerLists.
		/// </summary>
		public bool IsFixedSize {
			get { return false; }
		}
		#endregion

		#region Implementation of ICollection
		/// <summary>
		/// Gets the number of Triggers in the collection.
		/// </summary>
		public int Count {
			get {
				return oTriggers.Count;
			}
		}

		/// <summary>
		/// Copies all the Triggers in the collection to an array, beginning at the given index.
		/// The Triggers assigned to the array are cloned from the originals, implying they are
		/// unbound copies.  (Can'task tell if cloning is the intended semantics for this ICollection method,
		/// but it seems a good choice for TriggerLists.)
		/// </summary>
		/// <param name="array">Array to copy triggers into.</param>
		/// <param name="index">Index at which to start copying.</param>
		public void CopyTo(System.Array array, int index) {
			if (oTriggers.Count > array.Length - index) {
				throw new ArgumentException("Array has insufficient space to copy the collection.");
			}
			for (int i = 0; i<oTriggers.Count; i++) {
				array.SetValue( ((Trigger)oTriggers[i]).Clone(), index + i );
			}
		}

		/// <summary>
		/// Returns synchronizable state. Always false since the Task Scheduler is not
		/// thread safe.
		/// </summary>
		public bool IsSynchronized {
			get { return false; }
		}

		/// <summary>
		/// Gets the root object for synchronization. Always null since TriggerLists aren'task synchronized.
		/// </summary>
		public object SyncRoot {
			get { return null; }
		}
		#endregion

		#region Implementation of IEnumerable
		/// <summary>
		/// Gets a TriggerList enumerator.
		/// </summary>
		/// <returns>Enumerator for TriggerList.</returns>
		public System.Collections.IEnumerator GetEnumerator() {
			return new Enumerator(this);
		}
		#endregion

		#region Implementation of IDisposable
		/// <summary>
		/// Unbinds and Disposes all the Triggers in the collection, releasing the com interfaces they hold.
		/// Destroys the internal private pointer to the ITask com interface, but does not
		/// specifically release the interface because it is also in the containing task.
		/// </summary>
		public void Dispose() {
			foreach (object o in oTriggers) {
				((Trigger)o).Unbind();
			}
			oTriggers = null;
			iTask = null;
		}
		#endregion
	}

}