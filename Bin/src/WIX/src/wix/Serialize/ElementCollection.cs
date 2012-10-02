//-------------------------------------------------------------------------------------------------
// <copyright file="ElementCollection.cs" company="Microsoft">
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
// Element collections used by generated strongly-typed schema objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Serialize
{
	using System;
	using System.Collections;
	using System.Globalization;

	/// <summary>
	/// Summary description for ElementChoiceCollection.
	/// </summary>
	public class ElementCollection : ICollection, IEnumerable
	{
		private CollectionType collectionType;
		private int minimum = 1;
		private int maximum = 1;
		private int totalContainedItems;
		private int containersUsed;
		private ArrayList items;

		public ElementCollection(CollectionType collectionType)
		{
			this.collectionType = collectionType;
			this.items = new ArrayList();
		}

		public ElementCollection(CollectionType collectionType, int minimum, int maximum) : this(collectionType)
		{
			this.minimum = minimum;
			this.maximum = maximum;
		}

		public CollectionType Type
		{
			get { return this.collectionType; }
		}

		public void AddElement(ISchemaElement element)
		{
			foreach (object obj in this.items)
			{
				bool containerUsed;

				CollectionItem collectionItem = obj as CollectionItem;
				if (collectionItem != null)
				{
					containerUsed = collectionItem.Elements.Count != 0;
					if (collectionItem.ElementType.IsAssignableFrom(element.GetType()))
					{
						collectionItem.AddElement(element);

						if (!containerUsed)
						{
							this.containersUsed++;
						}

						this.totalContainedItems++;
						return;
					}

					continue;
				}

				ElementCollection collection = obj as ElementCollection;
				if (collection != null)
				{
					containerUsed = collection.Count != 0;

					try
					{
						collection.AddElement(element);

						if (!containerUsed)
						{
							this.containersUsed++;
						}

						this.totalContainedItems++;
						return;
					}
					catch (ArgumentException)
					{
						// Eat the exception and keep looking. We'll throw our own if we can't find its home.
					}

					continue;
				}
			}

			throw new ArgumentException(String.Format(
				CultureInfo.InvariantCulture,
				"Element of type {0} is not valid for this collection.",
				element.GetType().Name));
		}

		public void RemoveElement(ISchemaElement element)
		{
			foreach (object obj in this.items)
			{
				CollectionItem collectionItem = obj as CollectionItem;
				if (collectionItem != null)
				{
					if (collectionItem.ElementType.IsAssignableFrom(element.GetType()))
					{
						if (collectionItem.Elements.Count == 0)
						{
							return;
						}

						collectionItem.RemoveElement(element);

						if (collectionItem.Elements.Count == 0)
						{
							this.containersUsed--;
						}

						this.totalContainedItems--;
						return;
					}

					continue;
				}

				ElementCollection collection = obj as ElementCollection;
				if (collection != null)
				{
					if (collection.Count == 0)
					{
						continue;
					}

					try
					{
						collection.RemoveElement(element);

						if (collection.Count == 0)
						{
							this.containersUsed--;
						}

						this.totalContainedItems--;
						return;
					}
					catch (ArgumentException)
					{
						// Eat the exception and keep looking. We'll throw our own if we can't find its home.
					}

					continue;
				}
			}

			throw new ArgumentException(String.Format(
				CultureInfo.InvariantCulture,
				"Element of type {0} is not valid for this collection.",
				element.GetType().Name));
		}

		internal void AddItem(CollectionItem collectionItem)
		{
			this.items.Add(collectionItem);
		}

		internal void AddCollection(ElementCollection collection)
		{
			this.items.Add(collection);
		}

		internal abstract class CollectionItem
		{
			private Type elementType;
			private ArrayList elements;

			public CollectionItem(Type elementType)
			{
				this.elementType = elementType;
				this.elements = new ArrayList();
			}

			public Type ElementType
			{
				get { return this.elementType; }
			}

			public void AddElement(ISchemaElement element)
			{
				if (!elementType.IsAssignableFrom(element.GetType()))
				{
					throw new ArgumentException(String.Format(
						CultureInfo.InvariantCulture,
						"Element must be a subclass of {0}, but was of type {1}.",
						elementType.Name,
						element.GetType().Name),
						"element");
				}

				this.elements.Add(element);
			}

			public void RemoveElement(ISchemaElement element)
			{
				if (!elementType.IsAssignableFrom(element.GetType()))
				{
					throw new ArgumentException(String.Format(
						CultureInfo.InvariantCulture,
						"Element must be a subclass of {0}, but was of type {1}.",
						elementType.Name,
						element.GetType().Name),
						"element");
				}

				this.elements.Remove(element);
			}

			internal ArrayList Elements
			{
				get { return this.elements; }
			}
		}

		internal class ChoiceItem : CollectionItem
		{
			public ChoiceItem(Type elementType) : base(elementType)
			{
			}
		}

		internal class SequenceItem : CollectionItem
		{
			private int minimum = 1;
			private int maximum = 1;

			public SequenceItem(Type elementType) : base(elementType)
			{
			}

			public SequenceItem(Type elementType, int minimum, int maximum) : base(elementType)
			{
				this.minimum = minimum;
				this.maximum = maximum;
			}
		}

		private class ElementCollectionEnumerator : IEnumerator
		{
			private ElementCollection collection;
			private Stack collectionStack;

			public ElementCollectionEnumerator(ElementCollection collection)
			{
				this.collection = collection;
			}

			public object Current
			{
				get
				{
					if (this.collectionStack != null && this.collectionStack.Count > 0)
					{
						CollectionTuple tuple = (CollectionTuple)this.collectionStack.Peek();
						object container = tuple.Collection.items[tuple.ContainerIndex];

						CollectionItem collectionItem = container as CollectionItem;
						if (collectionItem != null)
						{
							return collectionItem.Elements[tuple.ItemIndex];
						}

						throw new ApplicationException(String.Format(
							CultureInfo.InvariantCulture,
							"Element of type {0} found in enumerator. Must be ChoiceItem or SequenceItem.",
							container.GetType().Name));
					}

					return null;
				}
			}

			public void Reset()
			{
				if (this.collectionStack != null)
				{
					this.collectionStack.Clear();
					this.collectionStack = null;
				}
			}

			public bool MoveNext()
			{
				if (this.collectionStack == null)
				{
					if (this.collection.Count == 0)
					{
						return false;
					}

					this.collectionStack = new Stack();
					this.collectionStack.Push(new CollectionTuple(this.collection));
				}

				CollectionTuple tuple = (CollectionTuple)this.collectionStack.Peek();

				if (this.FindNext(tuple))
				{
					return true;
				}

				this.collectionStack.Pop();
				if (this.collectionStack.Count == 0)
				{
					return false;
				}

				return this.MoveNext();
			}

			private void PushCollection(ElementCollection collection)
			{
				if (collection.Count <= 0)
				{
					throw new ArgumentException(String.Format(
						CultureInfo.InvariantCulture,
						"Collection has {0} elements. Must have at least one.",
						collection.Count));
				}

				CollectionTuple tuple = new CollectionTuple(collection);
				this.collectionStack.Push(tuple);
				this.FindNext(tuple);
			}

			private bool FindNext(CollectionTuple tuple)
			{
				object container = tuple.Collection.items[tuple.ContainerIndex];

				CollectionItem collectionItem = container as CollectionItem;
				if (collectionItem != null)
				{
					if (tuple.ItemIndex + 1 < collectionItem.Elements.Count)
					{
						tuple.ItemIndex++;
						return true;
					}
				}

				ElementCollection elementCollection = container as ElementCollection;
				if (elementCollection != null && elementCollection.Count > 0 && tuple.ItemIndex == -1)
				{
					tuple.ItemIndex++;
					this.PushCollection(elementCollection);
					return true;
				}

				tuple.ItemIndex = 0;

				for (int i = tuple.ContainerIndex + 1; i < tuple.Collection.items.Count; ++i)
				{
					object nestedContainer = tuple.Collection.items[i];

					CollectionItem nestedCollectionItem = nestedContainer as CollectionItem;
					if (nestedCollectionItem != null)
					{
						if (nestedCollectionItem.Elements.Count > 0)
						{
							tuple.ContainerIndex = i;
							return true;
						}
					}

					ElementCollection nestedElementCollection = nestedContainer as ElementCollection;
					if (nestedElementCollection != null && nestedElementCollection.Count > 0)
					{
						tuple.ContainerIndex = i;
						this.PushCollection(nestedElementCollection);
						return true;
					}
				}

				return false;
			}

			private class CollectionTuple
			{
				private ElementCollection collection;
				private int containerIndex;
				private int itemIndex = -1;

				public CollectionTuple(ElementCollection collection)
				{
					this.collection = collection;
				}

				public ElementCollection Collection
				{
					get { return this.collection; }
				}

				public int ContainerIndex
				{
					get { return this.containerIndex; }
					set { this.containerIndex = value; }
				}

				public int ItemIndex
				{
					get { return this.itemIndex; }
					set { this.itemIndex = value; }
				}
			}
		}

		public int Count
		{
			get { return this.totalContainedItems; }
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { return this; }
		}

		public void CopyTo(Array array, int index)
		{
			int item = 0;
			foreach (ISchemaElement element in this)
			{
				array.SetValue(element, (long)(item + index));
				item++;
			}
		}

		public IEnumerator GetEnumerator()
		{
			return new ElementCollectionEnumerator(this);
		}

		public IEnumerable Filter(Type childType)
		{
			foreach (object container in this.items)
			{
				CollectionItem collectionItem = container as CollectionItem;
				if (collectionItem != null)
				{
					if (collectionItem.ElementType.IsAssignableFrom(childType))
					{
						return collectionItem.Elements;
					}

					continue;
				}

				ElementCollection elementCollection = container as ElementCollection;
				if (elementCollection != null)
				{
					IEnumerable nestedFilter = elementCollection.Filter(childType);
					if (nestedFilter != null)
					{
						return nestedFilter;
					}

					continue;
				}
			}

			return null;
		}

		/// <summary>
		/// Enum representing types of XML collections.
		/// </summary>
		public enum CollectionType
		{
			Choice,
			Sequence
		}
	}
}
