using System;
using System.Collections;
using System.Collections.Generic;

namespace SIL.HermitCrab
{
	public enum Direction { RIGHT, LEFT };

	/// <summary>
	/// This is an abstract class that all linked list nodes must extend. Having to specify both the type
	/// of the class that extends this class and the type of the linked list that handles it is a little
	/// weird, but it allows us to have strongly-typed methods in the node class that can manipulate the
	/// owning linked list.
	/// </summary>
	/// <typeparam name="T">Item Type, must be the type of the class that extends this class.</typeparam>
	/// <typeparam name="O">Linked List Type, must be the type of the linked list that handles the <c>T</c> type.</typeparam>
	public abstract class HCLinkedListNode<T, O> : ICloneable where T : HCLinkedListNode<T, O> where O : HCLinkedList<T, O>
	{
		internal T m_next = null;
		internal T m_prev = null;
		internal O m_owner = null;

		protected int m_partition = -1;

		public HCLinkedListNode()
		{
		}

		public HCLinkedListNode(T node)
		{
			m_partition = node.m_partition;
		}

		/// <summary>
		/// Gets the linked list that owns this record.
		/// </summary>
		/// <value>The owning linked list.</value>
		public O Owner
		{
			get
			{
				return m_owner;
			}
		}

		/// <summary>
		/// Gets the next node in the owning linked list.
		/// </summary>
		/// <value>The next node.</value>
		public T Next
		{
			get
			{
				return m_next;
			}
		}

		/// <summary>
		/// Gets the previous node in the owning linked list.
		/// </summary>
		/// <value>The previous node.</value>
		public T Prev
		{
			get
			{
				return m_prev;
			}
		}

		/// <summary>
		/// Gets or sets the partition.
		/// </summary>
		/// <value>The partition.</value>
		public int Partition
		{
			get
			{
				return m_partition;
			}

			set
			{
				m_partition = value;
			}
		}

		/// <summary>
		/// Gets the next node in the owning linked list.
		/// </summary>
		/// <returns>The next node.</returns>
		public T GetNext()
		{
			if (m_owner == null)
				return null;

			return m_owner.GetNext(this as T);
		}

		/// <summary>
		/// Gets the next node in the owning linked list according to the
		/// specified direction.
		/// </summary>
		/// <param name="dir">The direction</param>
		/// <returns>The next node.</returns>
		public T GetNext(Direction dir)
		{
			if (m_owner == null)
				return null;

			return m_owner.GetNext(this as T, dir);
		}

		/// <summary>
		/// Gets the previous node in the owning linked list.
		/// </summary>
		/// <returns>The previous node.</returns>
		public T GetPrev()
		{
			if (m_owner == null)
				return null;

			return m_owner.GetPrev(this as T);
		}

		/// <summary>
		/// Gets the previous node in the owning linked list according to the
		/// specified direction.
		/// </summary>
		/// <param name="dir">The direction</param>
		/// <returns>The previous node.</returns>
		public T GetPrev(Direction dir)
		{
			if (m_owner == null)
				return null;

			return m_owner.GetPrev(this as T, dir);
		}

		/// <summary>
		/// Removes this node from the owning linked list.
		/// </summary>
		/// <returns><c>true</c> if the node is a member of a linked list, otherwise <c>false</c></returns>
		public bool Remove()
		{
			if (m_owner == null)
				return false;

			return m_owner.Remove(this as T);
		}

		/// <summary>
		/// Inserts the specified node to the right or left of this node.
		/// </summary>
		/// <param name="newRec">The new node.</param>
		/// <param name="dir">The direction to insert the node.</param>
		public void Insert(T newNode, Direction dir)
		{
			if (m_owner == null)
				return;

			m_owner.Insert(newNode, this as T, dir);
		}

		public abstract T Clone();

		object ICloneable.Clone()
		{
			return this.Clone();
		}
	}

	/// <summary>
	/// This is an abstract class that all linked list class extend. Having to specify both the type
	/// of the class that extends this class and the type of the linked list node that it handles is a
	/// little weird, but it allows us to have strongly-typed methods in the node class that can manipulate
	/// the owning linked list.
	/// </summary>
	/// <typeparam name="T">Item Type, must be the type of the class that the linked list handles.</typeparam>
	/// <typeparam name="O">Linked List Type, must be the type of the class that extends this class.</typeparam>
	public abstract class HCLinkedList<T, O> : ICollection<T> where T : HCLinkedListNode<T, O> where O : HCLinkedList<T, O>
	{
		T m_first = null;
		T m_last = null;
		int m_size = 0;

		public HCLinkedList()
		{
		}

		public HCLinkedList(IEnumerable<T> e)
		{
			AddMany(e);
		}

		public int Count
		{
			get
			{
				return m_size;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		public Object SyncRoot
		{
			get
			{
				return this;
			}
		}

		/// <summary>
		/// Adds the specified node to the end of this list.
		/// </summary>
		/// <param name="node">The node.</param>
		public virtual void Add(T node)
		{
			if (m_last != null)
				m_last.m_next = node;
			node.m_prev = m_last;
			node.m_next = null;
			node.m_owner = this as O;
			m_last = node;
			if (m_first == null)
				m_first = node;
			m_size++;
		}

		public void Clear()
		{
			m_first = null;
			m_last = null;
			m_size = 0;
		}

		public bool Contains(T node)
		{
			return node.m_owner == this;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			foreach (T node in this)
				array[arrayIndex++] = node;
		}

		/// <summary>
		/// Removes the specified node from this list.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns><c>true</c> if <c>node</c> is a member of this list, otherwise <c>false</c></returns>
		public virtual bool Remove(T node)
		{
			if (node.m_owner != this)
				return false;

			T prev = node.m_prev;
			if (prev == null)
				m_first = node.m_next;
			else
				prev.m_next = node.m_next;

			T next = node.m_next;
			if (next == null)
				m_last = node.m_prev;
			else
				next.m_prev = node.m_prev;

			m_size--;

			return true;
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (T node = First; node != null; node = node.Next)
				yield return node;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Gets the first node in this list.
		/// </summary>
		/// <value>The first node.</value>
		public T First
		{
			get
			{
				return m_first;
			}
		}

		/// <summary>
		/// Gets the last node in this list.
		/// </summary>
		/// <value>The last node.</value>
		public T Last
		{
			get
			{
				return m_last;
			}
		}

		/// <summary>
		/// Gets the first node in this list.
		/// </summary>
		/// <returns>The first node.</returns>
		public T GetFirst()
		{
			return m_first;
		}

		/// <summary>
		/// Gets the first node in this list according to the specified direction.
		/// </summary>
		/// <param name="dir">The direction.</param>
		/// <returns>The first node.</returns>
		public T GetFirst(Direction dir)
		{
			if (dir == Direction.RIGHT)
			{
				return m_first;
			}
			else
			{
				return m_last;
			}
		}

		/// <summary>
		/// Gets the last node in this list.
		/// </summary>
		/// <returns>The last node.</returns>
		public T GetLast()
		{
			return m_last;
		}

		/// <summary>
		/// Gets the last node in this list according to the specified direction.
		/// </summary>
		/// <param name="dir">The direction.</param>
		/// <returns>The last node.</returns>
		public T GetLast(Direction dir)
		{
			if (dir == Direction.RIGHT)
			{
				return m_last;
			}
			else
			{
				return m_first;
			}
		}

		/// <summary>
		/// Gets the node after the specified node.
		/// </summary>
		/// <param name="cur">The current node.</param>
		/// <returns>The next node.</returns>
		public T GetNext(T cur)
		{
			return GetNext(cur, Direction.RIGHT);
		}

		/// <summary>
		/// Gets the node after the specified node according to the specified direction.
		/// </summary>
		/// <param name="cur">The current node.</param>
		/// <param name="dir">The direction.</param>
		/// <returns>The next node.</returns>
		/// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
		public T GetNext(T cur, Direction dir)
		{
			if (cur.m_owner != this)
				throw new ArgumentException(string.Format(HCStrings.kstidLLNotMember, "cur"), "cur");

			if (dir == Direction.RIGHT)
			{
				return cur.m_next;
			}
			else
			{
				return cur.m_prev;
			}
		}

		/// <summary>
		/// Gets the node before the specified node.
		/// </summary>
		/// <param name="cur">The current node.</param>
		/// <returns>The previous node.</returns>
		public T GetPrev(T cur)
		{
			return GetPrev(cur, Direction.RIGHT);
		}

		/// <summary>
		/// Gets the node before the specified node according to the specified direction.
		/// </summary>
		/// <param name="cur">The current node.</param>
		/// <param name="dir">The direction.</param>
		/// <returns>The previous node.</returns>
		/// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
		public T GetPrev(T cur, Direction dir)
		{
			if (cur.m_owner != this)
				throw new ArgumentException(string.Format(HCStrings.kstidLLNotMember, "cur"), "cur");

			if (dir == Direction.RIGHT)
			{
				return cur.m_prev;
			}
			else
			{
				return cur.m_next;
			}
		}

		/// <summary>
		/// Inserts <c>newNode</c> to the left or right of <c>node</c>.
		/// </summary>
		/// <param name="newNode">The new node.</param>
		/// <param name="node">The current node.</param>
		/// <param name="dir">The direction to insert the new node.</param>
		/// <exception cref="System.ArgumentException">Thrown when the specified node is not owned by this linked list.</exception>
		public virtual void Insert(T newNode, T node, Direction dir)
		{
			if (node.m_owner != this)
				throw new ArgumentException(string.Format(HCStrings.kstidLLNotMember, "node"), "node");

			if (dir == Direction.RIGHT)
			{
				newNode.m_next = node.m_next;
				node.m_next = newNode;
				newNode.m_prev = node;
				if (newNode.m_next != null)
					newNode.m_next.m_prev = newNode;
			}
			else
			{
				newNode.m_prev = node.m_prev;
				node.m_prev = newNode;
				newNode.m_next = node;
				if (newNode.m_prev != null)
					newNode.m_prev.m_next = newNode;
			}

			if (newNode.m_next == null)
				m_last = newNode;

			if (newNode.m_prev == null)
				m_first = newNode;

			newNode.m_owner = this as O;
			m_size++;
		}

		/// <summary>
		/// Adds all of the nodes from the enumerable collection.
		/// </summary>
		/// <param name="e">The enumerable collection.</param>
		public void AddMany(IEnumerable<T> e)
		{
			foreach (T node in e)
				Add(node.Clone());
		}

		/// <summary>
		/// Adds all of the nodes from the enumerable collection and assigns them the specified partition.
		/// </summary>
		/// <param name="e">The enumerable collection.</param>
		/// <param name="partition">The partition.</param>
		public void AddPartition(IEnumerable<T> e, int partition)
		{
			foreach (T node in e)
			{
				T newNode = node.Clone();
				newNode.Partition = partition;
				Add(newNode);
			}
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			foreach (T node in this)
				hashCode ^= node.GetHashCode();
			return hashCode;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as HCLinkedList<T, O>);
		}

		public bool Equals(HCLinkedList<T, O> other)
		{
			if (other == null)
				return false;

			if (Count != other.Count)
				return false;

			IEnumerator<T> e1 = GetEnumerator();
			IEnumerator<T> e2 = other.GetEnumerator();
			while (e1.MoveNext() && e2.MoveNext())
			{
				if (!e1.Current.Equals(e2.Current))
					return false;
			}

			return true;
		}
	}
}
