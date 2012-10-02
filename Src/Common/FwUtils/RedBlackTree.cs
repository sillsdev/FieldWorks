using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary> Implements a red-black tree.
	/// Note that all "matching" is based on the CompareTo method.
	/// </summary>
	/// <author>  Mark Allen Weiss
	/// </author>
	public class RedBlackTree
	{
		/// <summary> Test if the tree is logically empty.
		/// </summary>
		/// <remarks>
		///
		/// RedBlackTree class
		///
		/// CONSTRUCTION: with a negative infinity sentinel
		///
		/// ******************PUBLIC OPERATIONS*********************
		/// void Insert( x )       --> Insert x
		/// void Remove( x )       --> Remove x (unimplemented)
		/// IComparable Find( x )   --> Return item that matches x
		/// IComparable FindMin( )  --> Return smallest item
		/// IComparable FindMax( )  --> Return largest item
		/// boolean IsEmpty( )     --> Return true if empty; else false
		/// void MakeEmpty( )      --> Remove all items
		/// void PrintTree( )      --> Print tree in sorted order
		/// </remarks>
		/// <returns> true if empty, false otherwise.</returns>
		virtual public bool Empty
		{
			get
			{
				return header.right == nullNode;
			}
		}
		/// <summary> Construct the tree.
		/// </summary>
		/// <param name="negInf">a value less than or equal to all others.
		/// </param>
		public RedBlackTree(IComparable negInf)
		{
			header = new RedBlackNode(negInf);
			header.left = header.right = nullNode;
		}

		/// <summary> Insert into the tree. Does nothing if item already present.
		/// </summary>
		/// <param name="item">the item to insert.
		/// </param>
		public virtual void  Insert(IComparable item)
		{
			current = parent = grand = header;
			nullNode.element = item;

			while (current.element.CompareTo(item) != 0)
			{
				great = grand; grand = parent; parent = current;
				current = item.CompareTo(current.element) < 0?current.left:current.right;

				// Check if two red children; fix if so
				if (current.left.color == RED && current.right.color == RED)
					handleReorient(item);
			}

			// Insertion fails if already present
			if (current != nullNode)
				return ;
			current = new RedBlackNode(item, nullNode, nullNode);

			// Attach to parent
			if (item.CompareTo(parent.element) < 0)
				parent.left = current;
			else
				parent.right = current;
			handleReorient(item);
		}

		/// <summary> Remove from the tree.
		/// Not implemented in this version.
		/// </summary>
		/// <param name="x">the item to remove.
		///
		/// </param>
		public virtual void Remove(IComparable x)
		{
			System.Console.Out.WriteLine("Remove is not implemented");
		}

		/// <summary> Find the smallest item  the tree.
		/// </summary>
		/// <returns> the smallest item or null if empty.
		///
		/// </returns>
		public virtual IComparable FindMin()
		{
			if (Empty)
				return null;

			RedBlackNode itr = header.right;

			while (itr.left != nullNode)
				itr = itr.left;

			return itr.element;
		}

		/// <summary> Find the largest item in the tree.
		/// </summary>
		/// <returns> the largest item or null if empty.
		///
		/// </returns>
		public virtual IComparable FindMax()
		{
			if (Empty)
				return null;

			RedBlackNode itr = header.right;

			while (itr.right != nullNode)
				itr = itr.right;

			return itr.element;
		}

		/// <summary> Find an item in the tree.
		/// </summary>
		/// <param name="x">the item to search for.
		/// </param>
		/// <returns> the matching item or null if not found.
		///
		/// </returns>
		public virtual IComparable Find(IComparable x)
		{
			nullNode.element = x;
			current = header.right;

			for (; ; )
			{
				if (x.CompareTo(current.element) < 0)
					current = current.left;
				else if (x.CompareTo(current.element) > 0)
					current = current.right;
				else if (current != nullNode)
					return current.element;
				else
					return null;
			}
		}

		/// <summary> Find an item in the tree.
		/// </summary>
		/// <param name="x">the item to search for.</param>
		/// <param name="comparer">The ICompararer used to compare when finding the given node.</param>
		/// <returns> the matching item or null if not found.</returns>
		public virtual IComparable Find(IComparable x, System.Collections.IComparer comparer)
		{
			// TODO: why is this line here?
			nullNode.element = x;
			current = header.right;

			for (; ; )
			{
				if (comparer.Compare(x,current.element) < 0)
					current = current.left;
				else if (comparer.Compare(x,current.element) > 0)
					current = current.right;
				else if (current != nullNode)
					return current.element;
				else
					return null;
			}
		}

		/// <summary> Make the tree logically empty.
		/// </summary>
		public virtual void MakeEmpty()
		{
			header.right = nullNode;
		}


		/// <summary> Print the tree contents in sorted order.
		/// </summary>
		public virtual void PrintTree()
		{
			if (Empty)
				System.Console.Out.WriteLine("Empty tree");
			else
				PrintTree(header.right);
		}

		/// <summary> Internal method to print a subtree in sorted order.
		/// </summary>
		/// <param name="t">the node that roots the tree.
		///
		/// </param>
		private void PrintTree(RedBlackNode t)
		{
			if (t != nullNode)
			{
				PrintTree(t.left);
				System.Console.Out.WriteLine(t.element);
				PrintTree(t.right);
			}
		}

		/// <summary> Internal routine that is called during an insertion
		/// if a node has two red children. Performs flip and rotations.
		/// </summary>
		/// <param name="item">the item being inserted.
		///
		/// </param>
		private void handleReorient(IComparable item)
		{
			// Do the color flip
			current.color = RED;
			current.left.color = BLACK;
			current.right.color = BLACK;

			if (parent.color == RED)
				// Have to rotate
			{
				grand.color = RED;
				if ((item.CompareTo(grand.element) < 0) != (item.CompareTo(parent.element) < 0))
					parent = rotate(item, grand);
				// Start dbl rotate
				current = rotate(item, great);
				current.color = BLACK;
			}
			header.right.color = BLACK; // Make root black
		}

		/// <summary> Internal routine that performs a single or double rotation.
		/// Because the result is attached to the parent, there are four cases.
		/// Called by handleReorient.
		/// </summary>
		/// <param name="item">the item in handleReorient.
		/// </param>
		/// <param name="parent">the parent of the root of the rotated subtree.
		/// </param>
		/// <returns> the root of the rotated subtree.</returns>
		private RedBlackNode rotate(IComparable item, RedBlackNode parent)
		{
			if (item.CompareTo(parent.element) < 0)
				return parent.left = item.CompareTo(parent.left.element) < 0?rotateWithLeftChild(parent.left):rotateWithRightChild(parent.left);
				// LR
			else
				return parent.right = item.CompareTo(parent.right.element) < 0?rotateWithLeftChild(parent.right):rotateWithRightChild(parent.right);
			// RR
		}

		/// <summary> Rotate binary tree node with left child.
		/// </summary>
		internal static RedBlackNode rotateWithLeftChild(RedBlackNode k2)
		{
			RedBlackNode k1 = k2.left;
			k2.left = k1.right;
			k1.right = k2;
			return k1;
		}

		/// <summary> Rotate binary tree node with right child.
		/// </summary>
		internal static RedBlackNode rotateWithRightChild(RedBlackNode k1)
		{
			RedBlackNode k2 = k1.right;
			k1.right = k2.left;
			k2.left = k1;
			return k2;
		}

		private RedBlackNode header;
		private static RedBlackNode nullNode;

		internal const int BLACK = 1; // Black must be 1
		internal const int RED = 0;

		// Used in insert routine and its helpers
		private static RedBlackNode current;
		private static RedBlackNode parent;
		private static RedBlackNode grand;
		private static RedBlackNode great;


//		// Test program
//		[STAThread]
//		public static void  Main(System.String[] args)
//		{
//			RedBlackTree t = new RedBlackTree(new MyInteger(System.Int32.MinValue));
//			int NUMS = 40000;
//			int GAP = 307;
//
//			System.Console.Out.WriteLine("Checking... (no more output means success)");
//
//			for (int i = GAP; i != 0; i = (i + GAP) % NUMS)
//				t.Insert(new MyInteger(i));
//
//			if (NUMS < 40)
//				t.PrintTree();
//			if (((MyInteger) (t.FindMin())).intValue() != 1 || ((MyInteger) (t.FindMax())).intValue() != NUMS - 1)
//				System.Console.Out.WriteLine("FindMin or FindMax error!");
//
//			for (int i = 1; i < NUMS; i++)
//				if (((MyInteger) (t.Find(new MyInteger(i)))).intValue() != i)
//					System.Console.Out.WriteLine("Find error1!");
//		}
		static RedBlackTree()
		{
			nullNode = new RedBlackNode(null);
			nullNode.left = nullNode.right = nullNode;
		}
	}

	/// <summary>
	/// 	Basic node stored in red-black trees
	///		Note that this class is not accessible outside
	///		of package DataStructures
	/// </summary>
	class RedBlackNode
	{
		// Constructors
		internal RedBlackNode(IComparable theElement):this(theElement, null, null)
		{
		}

		internal RedBlackNode(IComparable theElement, RedBlackNode lt, RedBlackNode rt)
		{
			element = theElement;
			left = lt;
			right = rt;
			color = RedBlackTree.BLACK;
		}

		// Friendly data; accessible by other package routines
		internal IComparable element; // The data in the node
		internal RedBlackNode left; // Left child
		internal RedBlackNode right; // Right child
		internal int color; // Color
	}

	sealed class Rotations
	{
		/// <summary> Rotate binary tree node with left child.
		/// For AVL trees, this is a single rotation for case 1.
		/// </summary>
		internal static RedBlackNode withLeftChild(RedBlackNode k2)
		{
			RedBlackNode k1 = k2.left;
			k2.left = k1.right;
			k1.right = k2;
			return k1;
		}

		/// <summary> Rotate binary tree node with right child.
		/// For AVL trees, this is a single rotation for case 4.
		/// </summary>
		internal static RedBlackNode withRightChild(RedBlackNode k1)
		{
			RedBlackNode k2 = k1.right;
			k1.right = k2.left;
			k2.left = k1;
			return k2;
		}

		/// <summary> Double rotate binary tree node: first left child
		/// with its right child; then node k3 with new left child.
		/// For AVL trees, this is a double rotation for case 2.
		/// </summary>
		internal static RedBlackNode doubleWithLeftChild(RedBlackNode k3)
		{
			k3.left = withRightChild(k3.left);
			return withLeftChild(k3);
		}

		/// <summary> Double rotate binary tree node: first right child
		/// with its left child; then node k1 with new right child.
		/// For AVL trees, this is a double rotation for case 3.
		/// </summary>
		internal static RedBlackNode doubleWithRightChild(RedBlackNode k1)
		{
			k1.right = withLeftChild(k1.right);
			return withRightChild(k1);
		}
	}
}