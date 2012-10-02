//-------------------------------------------------------------------------------------------------
// <copyright file="SymbolCollection.cs" company="Microsoft">
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
// Hash table collection of symbols.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Diagnostics;

	/// <summary>
	/// Hash table collection of symbols.
	/// </summary>
	public class SymbolCollection : ICollection, IEnumerable
	{
		private Hashtable collection;

		/// <summary>
		/// Created a new SymbolCollection.
		/// </summary>
		public SymbolCollection()
		{
			this.collection = new Hashtable();
		}

		/// <summary>
		/// Gets the number of elements in the collection.
		/// </summary>
		/// <value>Number of elements in collection.</value>
		public int Count
		{
			get { return this.collection.Count; }
		}

		/// <summary>
		/// Gets the keys of the hash table.
		/// </summary>
		/// <value>Collection of keys.</value>
		public ICollection Keys
		{
			get { return this.collection.Keys; }
		}

		/// <summary>
		/// Gets if this collection has been synchronized.
		/// </summary>
		/// <value>true if collection has been synchronized.</value>
		public bool IsSynchronized
		{
			get { return this.collection.IsSynchronized; }
		}

		/// <summary>
		/// Gets the synchronization object for this collection.
		/// </summary>
		/// <value>Object for synchronization.</value>
		public object SyncRoot
		{
			get { return this; }
		}

		/// <summary>
		/// Gets a symbol by name from the collection.
		/// </summary>
		/// <param name="symbolName">Name of symbol to find.</param>
		/// <exception cref="DuplicateSymbolsException">If the symbol is duplicated a DuplicateSymbolsException is thrown.</exception>
		public Symbol this[string symbolName]
		{
			get
			{
				try
				{
					return (Symbol)this.collection[symbolName];
				}
				catch (InvalidCastException)
				{
					ArrayList symbols = this.collection[symbolName] as ArrayList;
					if (null == symbols)
					{
						throw;
					}
					else
					{
						throw new DuplicateSymbolsException(symbols);
					}
				}
			}
		}

		/// <summary>
		/// Adds a symbol to the collection.
		/// </summary>
		/// <param name="symbol">Symbol to add collection.</param>
		/// <remarks>Add symbol to hash by name.</remarks>
		public void Add(Symbol symbol)
		{
			if (null == symbol)
			{
				throw new ArgumentNullException("symbol");
			}

			this.collection.Add(symbol.Name, symbol);
		}


		/// <summary>
		/// Adds a symbol to the collection.
		/// </summary>
		/// <param name="symbol">Symbol to add collection.</param>
		/// <remarks>Add symbol to hash by name.</remarks>
		public void AddDuplicate(Symbol symbol)
		{
			if (null == symbol)
			{
				throw new ArgumentNullException("symbol");
			}

			ArrayList symbols;
			object o = this.collection[symbol.Name];
			if (null == o)
			{
				throw new ArgumentNullException("o");
			}
			else if (o is ArrayList)
			{
				symbols = (ArrayList)o;
			}
			else
			{
				Debug.Assert(o is Symbol);
				symbols = new ArrayList();
				symbols.Add((Symbol)o);
			}

			symbols.Add(symbol);
			this.collection[symbol.Name] = symbols;
		}

		/// <summary>
		/// Checks if collection contains object.
		/// </summary>
		/// <param name="item">Item to check in collection.</param>
		/// <returns>true if collection contains item.</returns>
		public bool Contains(object item)
		{
			return this.collection.Contains(item);
		}

		/// <summary>
		/// Copies collection to array.
		/// </summary>
		/// <param name="array">Array to copy collection into.</param>
		/// <param name="index">Index to start copying at.</param>
		public void CopyTo(System.Array array, int index)
		{
			this.collection.CopyTo(array, index);
		}

		/// <summary>
		/// Gets an enumerator for the collection.
		/// </summary>
		/// <returns>Enumerator for collection.</returns>
		public virtual IEnumerator GetEnumerator()
		{
			return this.collection.Values.GetEnumerator();
		}
	}
}
