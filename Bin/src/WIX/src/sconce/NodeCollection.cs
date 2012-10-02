//-------------------------------------------------------------------------------------------------
// <copyright file="NodeCollection.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// A strongly-typed collection class for Node objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.Globalization;

	public class NodeCollection : SortedCollection
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(NodeCollection);

		private Hashtable idTable = new Hashtable(); // keyed by HierarchyId, contains Node.
#if USE_NET20_FRAMEWORK
		private Hashtable pathTable = new Hashtable(StringComparer.InvariantCultureIgnoreCase); // keyed by absolute path, contains Node.
#else
		private Hashtable pathTable = new Hashtable(CaseInsensitiveHashCodeProvider.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant); // keyed by absolute path, contains Node.
#endif
		private FolderNode parent;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeCollection"/> class.
		/// </summary>
		/// <param name="parent">The collection's parent folder node.</param>
		public NodeCollection(FolderNode parent) : this(parent, new FolderFirstComparer())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeCollection"/> class.
		/// </summary>
		/// <param name="parent">The collection's parent folder node.</param>
		/// <param name="comparer">A comparer to use for the collection.</param>
		protected NodeCollection(FolderNode parent, IComparer comparer) : base(comparer)
		{
			Tracer.VerifyNonNullArgument(parent, "parent");
			this.parent = parent;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the collection's parent folder node.
		/// </summary>
		public FolderNode Parent
		{
			get { return this.parent; }
		}
		#endregion

		#region Indexers
		//==========================================================================================
		// Indexers
		//==========================================================================================

		/// <summary>
		/// Gets the <see cref="Node"/> at the specified index.
		/// </summary>
		public Node this[int index]
		{
			get { return (Node)this.InnerList[index]; }
		}

		/// <summary>
		/// Gets the <see cref="Node"/> with the specified hierarchy identifier.
		/// </summary>
		public Node this[uint hierarchyId]
		{
			get { return this[this.IndexOf(hierarchyId)]; }
		}

		/// <summary>
		/// Gets the <see cref="Node"/> with the specified absolute path.
		/// </summary>
		public Node this[string absolutePath]
		{
			get { return this[this.IndexOf(absolutePath)]; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Adds a node to the sorted collection.
		/// </summary>
		/// <param name="node">The <see cref="Node"/> to add.</param>
		/// <returns>The index of the newly added node.</returns>
		public int Add(Node node)
		{
			return this.InnerList.Add(node);
		}

		/// <summary>
		/// Creates a deep copy of this collection.
		/// </summary>
		/// <returns>A deep copy of this collection.</returns>
		public override object Clone()
		{
			NodeCollection clone = new NodeCollection(this.Parent, this.Comparer);
			this.CloneInto(clone);
			return clone;
		}

		/// <summary>
		/// Returns a value indicating whether the specified node is contained in this collection.
		/// </summary>
		/// <param name="node">The node to check.</param>
		/// <returns>true if the node is contained within this collection; otherwise, false.</returns>
		public bool Contains(Node node)
		{
			return this.InnerList.Contains(node);
		}

		/// <summary>
		/// Returns a value indicating whether the specified hierarchy identifier is contained in this collection.
		/// </summary>
		/// <param name="hierarchyId">The hierarchy identifier to check.</param>
		/// <returns>true if the node with the specified hierarchy identifier is contained within this collection; otherwise, false.</returns>
		public bool Contains(uint hierarchyId)
		{
			return this.idTable.ContainsKey(hierarchyId);
		}

		/// <summary>
		/// Returns a value indicating whether the specified hierarchy identifier is contained in this collection.
		/// </summary>
		/// <param name="absolutePath">The absolute path of the node to find.</param>
		/// <returns>true if the node with the specified absolute path is contained within this collection; otherwise, false.</returns>
		public bool Contains(string absolutePath)
		{
			return this.pathTable.ContainsKey(absolutePath);
		}

		/// <summary>
		/// Returns the index of the specified node in the collection.
		/// </summary>
		/// <param name="node">The node to find.</param>
		/// <returns>The index of the specified node in the collection or -1 if the node is not found.</returns>
		/// <remarks>
		/// Do not cache the results of this call as the index of the item will likely change with
		/// subsequent <see cref="Add"/> or <see cref="Remove"/> operations.
		/// </remarks>
		public int IndexOf(Node node)
		{
			return this.InnerList.IndexOf(node);
		}

		/// <summary>
		/// Returns the index of the node with the specified hierarchy identifier in the collection.
		/// </summary>
		/// <param name="hierarchyId">The hierarchy identifier of the node to find.</param>
		/// <returns>The index of the node with the specified hierarchy identifier in the collection
		/// or -1 if the node is not found.</returns>
		/// <remarks>
		/// Do not cache the results of this call as the index of the item will likely change with
		/// subsequent <see cref="Add"/> or <see cref="Remove"/> operations.
		/// </remarks>
		public int IndexOf(uint hierarchyId)
		{
			return this.IndexOf((Node)this.idTable[hierarchyId]);
		}

		/// <summary>
		/// Returns the index of the node with the specified hierarchy identifier in the collection.
		/// </summary>
		/// <param name="absolutePath">The absolute path of the node to find.</param>
		/// <returns>The index of the node with the specified absolute path in the collection
		/// or -1 if the node is not found.</returns>
		/// <remarks>
		/// Do not cache the results of this call as the index of the item will likely change with
		/// subsequent <see cref="Add"/> or <see cref="Remove"/> operations.
		/// </remarks>
		public int IndexOf(string absolutePath)
		{
			return this.IndexOf((Node)this.pathTable[absolutePath]);
		}

		/// <summary>
		/// Removes the child from the hierarchy and any of its children if it is a folder node.
		/// </summary>
		/// <param name="node">The <see cref="Node"/> to remove.</param>
		public void Remove(Node node)
		{
			this.InnerList.Remove(node);
		}

		/// <summary>
		/// Called right after the node is added to the collection.
		/// </summary>
		/// <param name="index">The index of the item added to the collection.</param>
		/// <param name="value">The <see cref="Node"/> just added to the collection.</param>
		protected override void OnAddComplete(int index, object value)
		{
			Node node = (Node)value;

			// Set the node's parent to us.
			node.Parent = this.Parent;

			// Add the item to our lookup tables.
			Tracer.Assert(!this.idTable.ContainsKey(node.HierarchyId), "Something's wrong with our comparer.");
			this.idTable[node.HierarchyId] = node;
			Tracer.Assert(!this.pathTable.ContainsKey(node.AbsolutePath), "This node has already been added.");
			this.pathTable[node.AbsolutePath] = node;

			// This is useful information to trace, so we'll use the Hierarchy category and the Information level,
			// which will allow this to be traced by default.
			Tracer.WriteLine(classType, "OnAddComplete", Tracer.Level.Information, "Added '{0}' to the hierarchy.", node.ToString());
		}

		/// <summary>
		/// Called right after the value is removed from the collection.
		/// </summary>
		/// <param name="index">The index of the item removed from the collection.</param>
		/// <param name="value">The value just removed from the collection.</param>
		protected override void OnRemoveComplete(int index, object value)
		{
			Node node = (Node)value;

			// First remove the children of the node.
			FolderNode folderNode = node as FolderNode;
			if (folderNode != null)
			{
				folderNode.Children.Clear();
			}

			// Remove the node from our lookup tables.
			this.idTable.Remove(node.HierarchyId);
			this.pathTable.Remove(node.AbsolutePath);

			// Set the node's parent to null.
			node.Parent = null;

			// This is useful information to trace, so we'll use the Hierarchy category and the Information level,
			// which will allow this to be traced by default.
			Tracer.WriteLineInformation(classType, "OnRemoveComplete", "Removed '{0}' from the hierarchy.", node);
		}

		/// <summary>
		/// Validates the type to make sure it adheres to the strongly typed collection.
		/// Null values are not accepted by default.
		/// </summary>
		/// <param name="value">The value to verify.</param>
		protected override void ValidateType(object value)
		{
			// Check for null values.
			base.ValidateType(value);

			// Make sure the value is a Node.
			if (!(value is Node))
			{
				throw new ArgumentException("value must be of type Node.", "value");
			}
		}
		#endregion
	}
}