using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Utils
{
	/// <summary>
	/// Represents a collection of key/value pairs that are sorted on the key.
	/// Retrievals, insertions, and removals are executed in O(log n) time, because this dictionary
	/// is implemented as a binary search tree, specifically a red-black tree. The main advantage
	/// of this dictionary over other dictionary implementations is that this class provides
	/// the ability to query for ranges of key/value pairs within specified bounds.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class TreeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
		#region Enumerations

		private enum NodeColor
		{
			Red = 0,
			Black = 1
		}
		private enum Direction
		{
			Left = 0,
			Right = 1
		}

		#endregion

		#region Data Members

		private RedBlackNode m_rootNode;
		private int m_nodeCount;
		private readonly IComparer<TKey> m_comparer;

		#endregion

		#region Properties

		/// <summary>
		/// Returns true if the tree is empty, false otherwise.
		/// </summary>
		public bool IsEmpty
		{
			get { return (m_rootNode == null); }
		}

		/// <summary>
		/// Returns the minimum key/value pair in the tree, but throws an exception if the tree is empty.
		/// </summary>
		public KeyValuePair<TKey, TValue> Minimum
		{
			get
			{
				if (IsEmpty)
					throw new InvalidOperationException("Cannot determine minimum of an empty tree");

				// You can get the min value by traversing left from the root until you can't any more.
				var node = m_rootNode;
				while (node.LeftNode != null)
					node = node.LeftNode;

				return node.Pair;
			}
		}

		/// <summary>
		/// Returns the maximum key/value pair in the tree, but throws an exception if the tree is empty.
		/// </summary>
		public KeyValuePair<TKey, TValue> Maximum
		{
			get
			{
				if (IsEmpty)
					throw new InvalidOperationException("Cannot determine maximum of an empty tree");

				// You can get the max value by traversing right from the root until you can't any more.
				var node = m_rootNode;
				while (node.RightNode != null)
					node = node.RightNode;

				return node.Pair;
			}
		}
		#endregion

		#region Constructor(s)

		/// <summary>
		/// Initializes a new instance of the <see cref="TreeDictionary{TKey,TValue}"/> class.
		/// </summary>
		public TreeDictionary()
		{
			m_comparer = Comparer<TKey>.Default;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TreeDictionary{TKey,TValue}"/> class.
		/// </summary>
		/// <param name="comparer">The comparer.</param>
		public TreeDictionary(IComparer<TKey> comparer)
		{
			m_comparer = comparer;
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Gets all of the key/value pairs between the lower and upper bounds, inclusive.
		/// </summary>
		/// <param name="lower">The lower bound.</param>
		/// <param name="upper">The upper bound.</param>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<TKey, TValue>> GetRange(TKey lower, TKey upper)
		{
			if (lower == null)
				throw new ArgumentNullException("lower");
			if (upper == null)
				throw new ArgumentNullException("upper");

			if (IsEmpty)
				yield break;

			// first locate the lower bound
			Stack<RedBlackNode> stack = GetLowerBoundNodes(lower);

			while (stack.Count != 0)
			{
				RedBlackNode node = stack.Pop();

				if (m_comparer.Compare(upper, node.Pair.Key) < 0)
					yield break;

				yield return node.Pair;

				// only the right node needs to be traversed, the left node has already been checked
				if (node.RightNode != null)
				{
					foreach (KeyValuePair<TKey, TValue> pair in InOrderTraversal(node.RightNode, n => m_comparer.Compare(upper, n.Pair.Key) < 0))
						yield return pair;
				}
			}
		}

		/// <summary>
		/// Gets all of the key/value pairs above the lower bound, inclusive.
		/// </summary>
		/// <param name="lower">The lower bound.</param>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<TKey, TValue>> GetRangeAbove(TKey lower)
		{
			if (lower == null)
				throw new ArgumentNullException("lower");

			if (IsEmpty)
				yield break;

			// first locate the lower bound
			Stack<RedBlackNode> stack = GetLowerBoundNodes(lower);

			while (stack.Count != 0)
			{
				RedBlackNode node = stack.Pop();

				yield return node.Pair;

				// only the right node needs to be traversed, the left node has already been checked
				if (node.RightNode != null)
				{
					foreach (KeyValuePair<TKey, TValue> pair in InOrderTraversal(node.RightNode, null))
						yield return pair;
				}
			}
		}

		/// <summary>
		/// Gets all of the key/value pairs below the upper bound, inclusive.
		/// </summary>
		/// <param name="upper">The upper bound.</param>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<TKey, TValue>> GetRangeBelow(TKey upper)
		{
			if (upper == null)
				throw new ArgumentNullException("upper");

			if (IsEmpty)
				yield break;

			foreach (KeyValuePair<TKey, TValue> pair in InOrderTraversal(m_rootNode, n => m_comparer.Compare(upper, n.Pair.Key) < 0))
				yield return pair;
		}

		#region Implementation of ICollection<KeyValuePair<TKey,TValue>>

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			AddPair(item);
		}

		/// <summary>
		/// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		public void Clear()
		{
			m_rootNode = null;
			m_nodeCount = 0;
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			RedBlackNode node;
			if (TryGetNode(item.Key, out node))
				return EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(item, node.Pair);
			return false;
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
		///                     -or-
		///						<paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
		///                     -or-
		///                     The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
		/// </exception>
		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException("arrayIndex");
			if (arrayIndex >= array.Length)
				throw new ArgumentException("arrayIndex");
			if (array.Rank > 1 || m_nodeCount > array.Length - arrayIndex)
				throw new ArgumentException("array");

			foreach (KeyValuePair<TKey, TValue> pair in this)
				array[arrayIndex++] = pair;
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			RedBlackNode node;
			if (TryGetNode(item.Key, out node))
			{
				if (EqualityComparer<KeyValuePair<TKey, TValue>>.Default.Equals(item, node.Pair))
				{
					HardDelete(node);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		public int Count
		{
			get
			{
				return m_nodeCount;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
		/// </returns>
		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
		{
			get { return false; }
		}

		#endregion

		#region Implementation of IDictionary<TKey,TValue>

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the key; otherwise, false.
		/// </returns>
		/// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
		public bool ContainsKey(TKey key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			RedBlackNode node;
			return TryGetNode(key, out node);
		}

		/// <summary>
		/// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
		/// </summary>
		/// <param name="key">The object to use as the key of the element to add.</param>
		/// <param name="value">The object to use as the value of the element to add.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
		/// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</exception>
		public void Add(TKey key, TValue value)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			if (!AddPair(new KeyValuePair<TKey, TValue>(key, value)))
				throw new ArgumentException("key");
		}

		/// <summary>
		/// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
		/// </summary>
		/// <returns>
		/// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
		/// </returns>
		/// <param name="key">The key of the element to remove.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
		public bool Remove(TKey key)
		{
			if (key == null)
				throw new ArgumentNullException("key");
			RedBlackNode node;
			if (TryGetNode(key, out node))
			{
				HardDelete(node);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
		/// <returns>
		/// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
		/// </returns>
		/// <param name="key">The key whose value to get.</param>
		/// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
		public bool TryGetValue(TKey key, out TValue value)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			RedBlackNode node;
			if (TryGetNode(key, out node))
			{
				value = node.Pair.Value;
				return true;
			}
			value = default(TValue);
			return false;
		}

		/// <summary>
		/// Gets or sets the element with the specified key.
		/// </summary>
		/// <returns>
		/// The element with the specified key.
		/// </returns>
		/// <param name="key">The key of the element to get or set.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="key"/> is null.</exception>
		/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
		public TValue this[TKey key]
		{
			get
			{
				if (key == null)
					throw new ArgumentNullException("key");
				TValue value;
				if (TryGetValue(key, out value))
					return value;
				throw new KeyNotFoundException();
			}

			set
			{
				if (key == null)
					throw new ArgumentNullException("key");
				RedBlackNode node;
				if (TryGetNode(key, out node))
				{
					node.Pair = new KeyValuePair<TKey, TValue>(key, value);
					return;
				}
				Add(key, value);
			}
		}

		/// <summary>
		/// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
		/// </returns>
		public ICollection<TKey> Keys
		{
			get
			{
				return (from pair in this
						select pair.Key).ToArray();
			}
		}

		/// <summary>
		/// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
		/// </returns>
		public ICollection<TValue> Values
		{
			get
			{
				return (from pair in this
						select pair.Value).ToArray();
			}
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey, TValue>> Members

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			foreach (KeyValuePair<TKey, TValue> pair in InOrderTraversal(m_rootNode, null))
				yield return pair;
		}

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#endregion

		#region Misc methods

		private bool AddPair(KeyValuePair<TKey, TValue> pair)
		{
			if (m_rootNode == null)
			{
				// In this case we are inserting the root node, the first of the tree.
				var node = new RedBlackNode(pair, m_comparer) { ParentNode = null, Color = NodeColor.Black };
				m_rootNode = node;
				m_nodeCount++;
				return true;
			}

			// The root already exists, so traverse the tree to figure out where to put the node.
			return InsertNode(pair, m_rootNode);
		}

		private bool TryGetNode(TKey key, out RedBlackNode node)
		{
			node = null;

			if (IsEmpty)
				return false;

			var current = m_rootNode;
			while (current != null)
			{
				int compare = m_comparer.Compare(key, current.Pair.Key);
				if (compare < 0)
				{
					current = current.LeftNode;
				}
				else if (compare > 0)
				{
					current = current.RightNode;
				}
				else
				{
					node = current;
					return true;
				}
			}
			return false;
		}

		private Stack<RedBlackNode> GetLowerBoundNodes(TKey lower)
		{
			var stack = new Stack<RedBlackNode>();
			RedBlackNode current = m_rootNode;
			while (current != null)
			{
				int compare = m_comparer.Compare(lower, current.Pair.Key);
				if (compare < 0)
				{
					stack.Push(current);
					current = current.LeftNode;
				}
				else if (compare > 0)
				{
					current = current.RightNode;
				}
				else
				{
					stack.Push(current);
					break;
				}
			}
			return stack;
		}

		#endregion Misc methods

		#region Tree Insertion / Deletion

		private bool InsertNode(KeyValuePair<TKey, TValue> pair, RedBlackNode current)
		{
			int compare = m_comparer.Compare(pair.Key, current.Pair.Key);
			if (compare < 0)
			{
				if (current.LeftNode == null)
				{
					var node = new RedBlackNode(pair, m_comparer)
					{
						Color = NodeColor.Red,
						ParentNode = current,
					};
					current.LeftNode = node;
					m_nodeCount++;
				}
				else
				{
					return InsertNode(pair, current.LeftNode);
				}
			}
			else if (compare > 0)
			{
				if (current.RightNode == null)
				{
					var node = new RedBlackNode(pair, m_comparer)
					{
						Color = NodeColor.Red,
						ParentNode = current,
					};
					current.RightNode = node;
					m_nodeCount++;
				}
				else
				{
					return InsertNode(pair, current.RightNode);
				}
			}
			else
			{
				return false;
			}

			// Make sure we didn't violate the rules of a red/black tree.
			CheckNode(current);

			// Automatically make sure the root node is black. (this is valid in a red/black tree)
			m_rootNode.Color = NodeColor.Black;
			return true;
		}

		private void CheckNode(RedBlackNode current)
		{
			if (current == null)
				return;

			if (current.Color != NodeColor.Red) return;
			var uncleNode = GetSiblingNode(current);
			if (uncleNode != null && uncleNode.Color == NodeColor.Red)
			{
				// Switch colors and then check grandparent.
				uncleNode.Color = NodeColor.Black;
				current.Color = NodeColor.Black;
				current.ParentNode.Color = NodeColor.Red;

				// We don't have to check the root node, I'm just going to turn it black.
				if (current.ParentNode.ParentNode != null && m_comparer.Compare(current.ParentNode.ParentNode.Pair.Key, m_rootNode.Pair.Key) != 0)
				{
					var node = current.ParentNode.ParentNode;
					CheckNode(node);
				}
			}
			else
			{
				var redChild = (current.LeftNode != null && current.LeftNode.Color == NodeColor.Red) ? Direction.Left : Direction.Right;

				// Need to rotate, figure out the node and direction for the rotation.
				// There are 4 scenarios here, left child of right parent, left child of left parent, right child of right parent, right child of left parent
				if (redChild == Direction.Left)
				{
					if (current.ParentDirection == Direction.Right)
					{
						RotateLeftChildRightParent(current);
					}
					else
					{
						RotateLeftChildLeftParent(current);
					}
				}
				else
				{
					// Only do this if the right child is red, otherwise no rotation is needed.
					if (current.RightNode != null)
					{
						if (current.RightNode.Color == NodeColor.Red)
						{
							if (current.ParentDirection == Direction.Right)
							{
								RotateRightChildRightParent(current);
							}
							else
							{
								RotateRightChildLeftParent(current);
							}
						}
					}
				}
			}
		}

		private RedBlackNode GetSiblingNode(RedBlackNode current)
		{
			if (current == null || current.ParentNode == null)
				return null;

			if (current.ParentNode.LeftNode != null && m_comparer.Compare(current.ParentNode.LeftNode.Pair.Key, current.Pair.Key) == 0)
				return current.ParentNode.RightNode;

			return current.ParentNode.LeftNode;
		}

		private void HardDelete(RedBlackNode current)
		{
			// Make sure we remember to adjust the nodecount.
			m_nodeCount--;

			if (current.LeftNode != null && current.RightNode != null)
			{
				// Find the successor node, swap the value up the tree, and delete the successor.
				var successor = FindSuccessor(current);
				current.Pair = successor.Pair;
				PerformHardDelete(successor);
			}
			else
				PerformHardDelete(current);
		}

		private void PerformHardDelete(RedBlackNode current)
		{
			// The node has either no children or 1 child.
			if (current.LeftNode == null && current.RightNode == null)
			{
				var sibling = GetSiblingNode(current);

				// In this case we are deleting a leaf node, just get rid of it.
				if (current.ParentDirection == Direction.Left)
					current.ParentNode.RightNode = null;
				else
					current.ParentNode.LeftNode = null;

				current.ParentNode = null;

				// The node is out of the tree, make sure the tree is still valid.
				// The tree only needs fixed if the node we deleted was black.
				// If we deleted a red leaf node then nothing needs adjusted.
				if (current.Color == NodeColor.Black)
				{
					// If the sibling has no children (it has to be black in this case)
					if (sibling != null)
					{
						if (sibling.LeftNode == null && sibling.RightNode == null)
						{
							// Turn the sibling node red to compensate for the black node being deleted,
							// and make sure the parent is black.
							sibling.Color = NodeColor.Red;
							sibling.ParentNode.Color = NodeColor.Black;
						}
						else if (sibling.LeftNode == null && sibling.RightNode != null)
						{
							// We need to figure out which rotation case fixes the problem.
							if (m_comparer.Compare(sibling.Pair.Key, current.Pair.Key) > 0)
							{
								// There will need to be a rotation to fix this situation, and
								// nodes will have to be re-colored.
								sibling.RightNode.Color = NodeColor.Black;
								sibling.Color = sibling.ParentNode.Color;
								sibling.ParentNode.Color = NodeColor.Black;
								RotateRightChildLeftParent(sibling);
							}
							else
							{
								// There will be 2 rotations done here, after the first
								// it becomes the exact same case as above.
								RotateRightChildRightParent(sibling);
								sibling.Color = NodeColor.Black;
								sibling.ParentNode.Color = sibling.ParentNode.ParentNode.Color;
								sibling.ParentNode.ParentNode.Color = NodeColor.Black;
								RotateLeftChildRightParent(sibling.ParentNode);
							}
						}
						else if (sibling.LeftNode != null && sibling.RightNode == null)
						{
							// We need to figure out which rotation case fixes the problem.
							if (m_comparer.Compare(sibling.Pair.Key, current.Pair.Key) > 0)
							{
								// There will be 2 rotations done here, after the first
								// it becomes the exact same case as above.
								RotateLeftChildLeftParent(sibling);
								sibling.Color = NodeColor.Black;
								sibling.ParentNode.Color = sibling.ParentNode.ParentNode.Color;
								sibling.ParentNode.ParentNode.Color = NodeColor.Black;
								RotateRightChildLeftParent(sibling.ParentNode);
							}
							else
							{
								// There will need to be a rotation to fix this situation, and
								// nodes will have to be re-colored.
								sibling.LeftNode.Color = NodeColor.Black;
								sibling.Color = sibling.ParentNode.Color;
								sibling.ParentNode.Color = NodeColor.Black;
								RotateLeftChildRightParent(sibling);
							}
						}
						else if (sibling.RightNode != null && sibling.LeftNode != null)
						{
							if (sibling.Color == NodeColor.Black)
							{
								if (m_comparer.Compare(sibling.Pair.Key, current.Pair.Key) > 0)
								{
									// In this case, the sibling has 2 children.
									// There will need to be a rotation to fix this situation, and
									// nodes will have to be re-colored.
									sibling.RightNode.Color = NodeColor.Black;
									sibling.Color = sibling.ParentNode.Color;
									sibling.ParentNode.Color = NodeColor.Black;
									RotateRightChildLeftParent(sibling);
								}
								else
								{
									// In this case, the sibling has 2 children.
									// There will need to be a rotation to fix this situation, and
									// nodes will have to be re-colored.
									sibling.LeftNode.Color = NodeColor.Black;
									sibling.Color = sibling.ParentNode.Color;
									sibling.ParentNode.Color = NodeColor.Black;
									RotateLeftChildRightParent(sibling);
								}
							}
							else
							{
								// This is the case where the sibling of the deleted node is red with 2 black children.
								// First, swap the sibling color with the parent color.
								sibling.ParentNode.Color = NodeColor.Red;
								sibling.Color = NodeColor.Black;

								if (m_comparer.Compare(sibling.Pair.Key, current.Pair.Key) > 0)
								{
									var newSib = sibling.LeftNode;
									RotateRightChildLeftParent(sibling);

									if (newSib.Color == NodeColor.Black)
									{
										// In this case, we need to swap colors to preserve the black node count.
										if ((newSib.LeftNode == null || (newSib.LeftNode != null && newSib.LeftNode.Color == NodeColor.Black))
										 && (newSib.RightNode == null || (newSib.RightNode != null && newSib.RightNode.Color == NodeColor.Black)))
										{
											newSib.Color = newSib.ParentNode.Color;
											newSib.ParentNode.Color = NodeColor.Black;

											// Perform additional re-coloring and rotation to fix violations.
											if (newSib.RightNode != null)
												newSib.RightNode.Color = NodeColor.Black;

											// Possible case for rotation.
											if (newSib.Color == NodeColor.Red && (newSib.LeftNode != null || newSib.RightNode != null))
												RotateRightChildLeftParent(newSib);
										}
										else if (newSib.RightNode != null && newSib.RightNode.Color == NodeColor.Red)
										{
											// This handles if the new sibling has a red right child.
											// Perform additional re-coloring and rotation to fix violations.
											newSib.RightNode.Color = NodeColor.Black;

											newSib.Color = newSib.ParentNode.Color;
											newSib.ParentNode.Color = NodeColor.Black;
											RotateRightChildLeftParent(newSib);
										}
										else if (newSib.LeftNode != null && newSib.LeftNode.Color == NodeColor.Red)
										{
											// The new sibling has just a red left child.
											// Perform additional re-coloring and rotatin to fix violations.
											RotateLeftChildLeftParent(newSib);
											newSib.Color = NodeColor.Black;
											newSib.ParentNode.Color = newSib.ParentNode.ParentNode.Color;
											newSib.ParentNode.ParentNode.Color = NodeColor.Black;

											if (newSib.ParentNode.Color == NodeColor.Red)
												RotateRightChildLeftParent(newSib.ParentNode);
										}
									}
								}
								else
								{
									var newSib = sibling.RightNode;
									RotateLeftChildRightParent(sibling);

									if (newSib.Color == NodeColor.Black)
									{
										// In this case, we need to swap colors to preserve the black node count.
										if ((newSib.LeftNode == null || (newSib.LeftNode != null && newSib.LeftNode.Color == NodeColor.Black))
										 && (newSib.RightNode == null || (newSib.RightNode != null && newSib.RightNode.Color == NodeColor.Black)))
										{
											newSib.Color = newSib.ParentNode.Color;
											newSib.ParentNode.Color = NodeColor.Black;

											// Perform additional re-coloring and rotation to fix violations.
											if (newSib.LeftNode != null)
												newSib.LeftNode.Color = NodeColor.Black;

											// Possible case for rotation.
											if (newSib.Color == NodeColor.Red && (newSib.LeftNode != null || newSib.RightNode != null))
												RotateLeftChildRightParent(newSib);
										}
										else if (newSib.LeftNode != null && newSib.LeftNode.Color == NodeColor.Red)
										{
											// Perform additional re-coloring and rotation to fix violations.
											newSib.LeftNode.Color = NodeColor.Black;

											newSib.Color = newSib.ParentNode.Color;
											newSib.ParentNode.Color = NodeColor.Black;
											RotateLeftChildRightParent(newSib);
										}
										else if (newSib.RightNode != null && newSib.RightNode.Color == NodeColor.Red)
										{
											// The new sibling has just a red left child.
											// Perform additional re-coloring and rotatin to fix violations.
											RotateRightChildRightParent(newSib);
											newSib.Color = NodeColor.Black;
											newSib.ParentNode.Color = newSib.ParentNode.ParentNode.Color;
											newSib.ParentNode.ParentNode.Color = NodeColor.Black;

											if (newSib.ParentNode.Color == NodeColor.Red)
												RotateLeftChildRightParent(newSib.ParentNode);
										}
									}
								}
							}
						}
					}
				}
			}

			if (current.LeftNode != null)
			{
				// In this case, the node we are deleting has a single child, so we know it's a
				// black node.  A red node can't have a single child because it wouldn't be a
				// valid tree.
				current.LeftNode.ParentNode = current.ParentNode;

				// This shouldn't happen since we checked for the root node above
				// but resharper warnings annoy me :)
				if (current.ParentNode != null)
				{
					if (current.ParentDirection == Direction.Left)
						current.ParentNode.RightNode = current.LeftNode;
					else
						current.ParentNode.LeftNode = current.LeftNode;
				}
			}

			if (current.RightNode != null)
			{
				// In this case, the node we are deleting has a single child, so we know it's a
				// black node.  A red node can't have a single child because it wouldn't be a
				// valid tree.
				current.RightNode.ParentNode = current.ParentNode;

				// This shouldn't happen since we checked for the root node above
				// but resharper warnings annoy me :)
				if (current.ParentNode != null)
				{
					if (current.ParentDirection == Direction.Left)
						current.ParentNode.RightNode = current.RightNode;
					else
						current.ParentNode.LeftNode = current.RightNode;
				}
			}
		}

		private static RedBlackNode FindSuccessor(RedBlackNode node)
		{
			// The successor to a node is the node closest in value to it that is larger.
			if (node.RightNode == null)
				return null;

			var cur = node.RightNode;
			while (cur.LeftNode != null)
				cur = cur.LeftNode;

			return cur;
		}

		private static void FixChildColors(RedBlackNode current)
		{
			// If a node is red, both children must be black.....switch if necessary.
			if (current.Color == NodeColor.Red)
			{
				if (current.LeftNode != null && current.LeftNode.Color == NodeColor.Black
				   && current.RightNode != null && current.RightNode.Color == NodeColor.Red)
				{
					current.LeftNode.Color = NodeColor.Red;
					current.Color = NodeColor.Black;
				}
				else if (current.RightNode != null && current.RightNode.Color == NodeColor.Black
				   && current.LeftNode != null && current.LeftNode.Color == NodeColor.Red)
				{
					current.RightNode.Color = NodeColor.Red;
					current.Color = NodeColor.Black;
				}
			}
		}
		#endregion

		#region Tree Rotation

		private void RotateRightChildRightParent(RedBlackNode current)
		{
			// Don't rotate on the root.
			if (current.IsRoot)
				return;

			var tmpNode = current.RightNode.LeftNode;
			current.RightNode.ParentNode = current.ParentNode;
			current.ParentNode.LeftNode = current.RightNode;
			current.ParentNode = current.RightNode;
			current.RightNode.LeftNode = current;

			if (tmpNode != null)
			{
				current.RightNode = tmpNode;
				tmpNode.ParentNode = current;
			}
			else
			{
				current.RightNode = tmpNode;
			}

			// The new node to check is the parent node.
			var newCurrent = current.ParentNode;
			CheckNode(newCurrent);
		}

		private void RotateLeftChildLeftParent(RedBlackNode current)
		{
			// Don't rotate on the root.
			if (current.IsRoot)
				return;

			var tmpNode = current.LeftNode.RightNode;
			current.LeftNode.ParentNode = current.ParentNode;
			current.ParentNode.RightNode = current.LeftNode;
			current.ParentNode = current.LeftNode;
			current.LeftNode.RightNode = current;

			if (tmpNode != null)
			{
				current.LeftNode = tmpNode;
				tmpNode.ParentNode = current;
			}
			else
			{
				current.LeftNode = tmpNode;
			}

			// The new node to check is the parent node.
			var newCurrent = current.ParentNode;
			CheckNode(newCurrent);
		}

		private void RotateLeftChildRightParent(RedBlackNode current)
		{
			// Don't rotate on the root.
			if (current.IsRoot)
				return;

			if (current.RightNode != null)
			{
				current.ParentNode.LeftNode = current.RightNode;
				current.RightNode.ParentNode = current.ParentNode;
			}
			else
			{
				current.ParentNode.LeftNode = current.RightNode;
			}

			var tmpNode = current.ParentNode.ParentNode;

			current.RightNode = current.ParentNode;
			current.ParentNode.ParentNode = current;

			if (tmpNode == null)
			{
				m_rootNode = current;
				current.ParentNode = null;
			}
			else
			{
				current.ParentNode = tmpNode;

				// Make sure we have the pointer from the parent.
				if (m_comparer.Compare(tmpNode.Pair.Key, current.Pair.Key) > 0)
				{
					tmpNode.LeftNode = current;
				}
				else
				{
					tmpNode.RightNode = current;
				}
			}

			FixChildColors(current);

			// The new node to check is the parent node.
			var newCurrent = current.ParentNode;
			CheckNode(newCurrent);
		}

		private void RotateRightChildLeftParent(RedBlackNode current)
		{
			// Don't rotate on the root.
			if (current.IsRoot)
				return;

			if (current.LeftNode != null)
			{
				current.ParentNode.RightNode = current.LeftNode;
				current.LeftNode.ParentNode = current.ParentNode;
			}
			else
			{
				current.ParentNode.RightNode = current.LeftNode;
			}

			var tmpNode = current.ParentNode.ParentNode;
			current.LeftNode = current.ParentNode;
			current.ParentNode.ParentNode = current;

			if (tmpNode == null)
			{
				m_rootNode = current;
				current.ParentNode = null;
			}
			else
			{
				current.ParentNode = tmpNode;

				// Make sure we have the pointer from the parent.
				if (m_comparer.Compare(tmpNode.Pair.Key, current.Pair.Key) > 0)
				{
					tmpNode.LeftNode = current;
				}
				else
				{
					tmpNode.RightNode = current;
				}
			}

			FixChildColors(current);

			// The new node to check is the parent node.
			var newCurrent = current.ParentNode;
			CheckNode(newCurrent);
		}

		#endregion

		#region Tree Traversal Methods

		private static IEnumerable<KeyValuePair<TKey, TValue>> InOrderTraversal(RedBlackNode node, Func<RedBlackNode, bool> breakPredicate)
		{
			if (node.LeftNode != null)
			{
				foreach (KeyValuePair<TKey, TValue> pair in InOrderTraversal(node.LeftNode, breakPredicate))
					yield return pair;
			}

			if (breakPredicate != null && breakPredicate(node))
				yield break;

			yield return node.Pair;

			if (node.RightNode != null)
			{
				foreach (KeyValuePair<TKey, TValue> pair in InOrderTraversal(node.RightNode, breakPredicate))
					yield return pair;
			}
		}

		#endregion

		#region Nested Node Class

		private class RedBlackNode
		{
			private readonly IComparer<TKey> m_comparer;

			#region Properties

			public NodeColor Color { get; set; }

			public Direction ParentDirection
			{
				get
				{
					if (ParentNode == null || m_comparer.Compare(Pair.Key, ParentNode.Pair.Key) > 0)
						return Direction.Left;

					return Direction.Right;
				}
			}

			public KeyValuePair<TKey, TValue> Pair { get; set; }

			public RedBlackNode ParentNode { get; set; }

			public RedBlackNode LeftNode { get; set; }

			public RedBlackNode RightNode { get; set; }

			public Boolean IsRoot
			{
				get { return (ParentNode == null); }
			}
			#endregion

			#region Constructor(s)

			public RedBlackNode(KeyValuePair<TKey, TValue> pair, IComparer<TKey> comparer)
			{
				Pair = pair;
				Color = NodeColor.Red;
				m_comparer = comparer;
			}

			#endregion

			#region Overrides

			public override string ToString()
			{
				return Pair.ToString();
			}

			#endregion
		}
		#endregion
	}
}
