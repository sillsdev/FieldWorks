// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LCMBrowser
{
	/// <summary />
	public class GenericInspectorObjectList : List<IInspectorObject>, IInspectorList
	{
		/// <summary />
		public event EventHandler BeginItemExpanding;
		/// <summary />
		public event EventHandler EndItemExpanding;
		private bool m_sortAscending = true;

		/// <summary>
		/// Initializes the list using the specified top level object.
		/// </summary>
		public virtual void Initialize(object topLevelObj)
		{
			Clear();
			TopLevelObject = topLevelObj;
			AddRange(GetInspectorObjects(topLevelObj, 0));
		}

		/// <summary />
		public object TopLevelObject { get; private set; }

		/// <summary>
		/// Toggles the object expansion.
		/// </summary>
		public virtual bool ToggleObjectExpansion(int index)
		{
			if (index < 0 || index >= Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			var currObj = this[index];
			if (currObj == null)
			{
				throw new NullReferenceException("currObj");
			}
			return currObj.HasChildren && (IsExpanded(index) ? CollapseObject(index) : ExpandObject(index));
		}

		/// <summary>
		/// Collapses the object at the specified index.
		/// </summary>
		public virtual bool CollapseObject(int index)
		{
			var io = this[index];
			var currLevel = io.Level;
			var count = 0;
			for (var i = index + 1; i < Count && this[i].Level > currLevel; i++, count++)
			{
			}
			if (count == 0)
			{
				return false;
			}
			RemoveRange(index + 1, count);
			return true;
		}

		/// <summary>
		/// Expands the specified object at the specified index.
		/// </summary>
		public virtual bool ExpandObject(int index)
		{
			var io = this[index];
			var list = GetInspectorObjects(io, io.Level + 1);
			if (list.Count == 0)
			{
				return false;
			}
			BeginItemExpanding?.Invoke(io, EventArgs.Empty);
			InsertRange(index + 1, list);
			EndItemExpanding?.Invoke(io, EventArgs.Empty);

			return true;
		}

		/// <summary>
		/// Gets a list of IInspectorObject objects representing all the properties for the
		/// specified object, which is assumed to be at the specified level.
		/// </summary>
		protected virtual List<IInspectorObject> GetInspectorObjects(object obj, int level)
		{
			var ioParent = obj as IInspectorObject;
			if (ioParent != null)
			{
				obj = ioParent.Object;
			}
			var list = new List<IInspectorObject>();
			// We used to check if the object implemented ICollection but that didn't
			// work for Linq results. This works for both, but we need to make sure the
			// object is not a string because we don't want to show strings as an array
			// of characters.
			var enumList = obj as IEnumerable;
			if (enumList != null && enumList.GetType() != typeof(string))
			{
				var i = 0;
				foreach (var item in enumList)
				{
					var io = CreateInspectorObject(item, obj, ioParent, level);
					io.DisplayName = $"[{i++}]";
					list.Add(io);
				}
				return list;
			}

			foreach (var pi in GetPropsForObj(obj))
			{
				try
				{
					var propObj = pi.GetValue(obj, null);
					list.Add(CreateInspectorObject(pi, propObj, obj, ioParent, level));
				}
				catch (Exception e)
				{
					list.Add(CreateExceptionInspectorObject(e, obj, pi.Name, level, ioParent));
				}
			}

			list.Sort(CompareInspectorObjectNames);
			return list;
		}

		/// <summary>
		/// Gets the properties for the specified object.
		/// </summary>
		protected virtual PropertyInfo[] GetPropsForObj(object obj)
		{
			return (obj == null ? new PropertyInfo[] { } : obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty));
		}

		/// <summary>
		/// Gets an inspector object for the specified property info., object and level.
		/// </summary>
		protected virtual IInspectorObject CreateInspectorObject(object obj, object owningObj, IInspectorObject ioParent, int level)
		{
			return CreateInspectorObject(null, obj, owningObj, ioParent, level);
		}

		/// <summary>
		/// Gets an inspector object for the specified property info., object and level.
		/// </summary>
		protected virtual IInspectorObject CreateInspectorObject(PropertyInfo pi, object obj, object owningObj, IInspectorObject ioParent, int level)
		{
			var fSetHasChildrenFromType = true;
			Type objType = null;
			var gio = new GenericInspectorObject
			{
				OwningList = this,
				Flid = 0,
				ParentInspectorObject = ioParent,
				Level = level,
				OwningObject = owningObj,
				DisplayValue = obj == null ? "null" : obj.ToString()
			};
			gio.Object = gio.OriginalObject = obj;

			if (obj != null)
			{
				objType = obj.GetType();
				gio.DisplayType = CleanupGenericListType(objType.ToString());

				// Check if the object is a collection. If so, we need to convert it
				// to an array and store that array as the object. It makes for easier
				// handling when the object is expanded. REVIEW: Are there more enumerable
				// types that we should not treat as an array?
				var showAsArray = obj.GetType() != typeof(string) && objType.GetInterface("IEnumerable") != null;
				if (showAsArray || obj.ToString().StartsWith("System.Linq.Enumerable+<CastIterator>"))
				{
					var collection = new List<object>(((IEnumerable)obj).Cast<object>());
					var array = collection.ToArray();
					gio.Object = array;
					gio.DisplayValue = FormatCountString(array.Length);
					gio.HasChildren = array.Length > 0;
					fSetHasChildrenFromType = false;
				}
			}

			if (pi != null)
			{
				gio.DisplayName = pi.Name;
				if (objType == null)
				{
					objType = pi.PropertyType;
					gio.DisplayType = CleanupGenericListType(objType.ToString());
				}
			}

			if (objType != null)
			{
				gio.DisplayType = CleanupGenericListType(gio.DisplayType.Replace('+', '.'));
				if (fSetHasChildrenFromType)
				{
					var flags = BindingFlags.Instance | BindingFlags.Public;
					gio.HasChildren = (obj != null && objType.GetProperties(flags).Length > 0 && !objType.IsPrimitive
									   && objType != typeof(string) && objType != typeof(Guid));
				}
			}

			return gio;
		}

		/// <summary>
		/// Creates an inspector object for an exception.
		/// </summary>
		protected IInspectorObject CreateExceptionInspectorObject(Exception e, object obj, string propName, int level, IInspectorObject ioParent)
		{
			var msg = e.Message;
			var trace = new StringBuilder("Exception:" + e.StackTrace);
			e = e.InnerException;
			while (e != null)
			{
				msg = e.Message;
				trace.Append(Environment.NewLine);
				trace.Append(e.StackTrace);
				e = e.InnerException;
			}

			var gio = new GenericInspectorObject
			{
				DisplayName = propName,
				DisplayValue = "Error: " + msg,
				DisplayType = trace.ToString(),
				Flid = 0,
				OwningList = this,
				Level = level,
				OwningObject = null,
				ParentInspectorObject = ioParent
			};
			gio.Object = gio.OriginalObject = obj;

			return gio;
		}

		/// <summary>
		/// Cleanups the text for types that are generic lists.
		/// </summary>
		protected virtual string CleanupGenericListType(string type)
		{
			if (string.IsNullOrWhiteSpace(type))
			{
				return string.Empty;
			}
			if (CleanDictionaryType("System.Collections.Generic.KeyValuePair`2", ref type)
				|| CleanDictionaryType("System.Collections.Generic.Dictionary`2", ref type))
			{
				return type;
			}

			var i = type.IndexOf("`1[");
			if (i < 0)
			{
				return type;
			}
			var collection = type.Substring(0, i);
			var collectionOf = type.Substring(i + 3);
			i = collection.LastIndexOf('.');
			if (i > 0)
			{
				collection = collection.Substring(i + 1);
			}
			i = collectionOf.LastIndexOf('.');
			if (i > 0)
			{
				collectionOf = collectionOf.Substring(i + 1);
			}
			type = collection + "<" + collectionOf;
			type = type.Replace(']', '>');

			if (type.StartsWith("Enumerable+<CastIterator>"))
			{
				i = type.LastIndexOf("<");
				if (i > 0)
				{
					type = "IEnumerable" + type.Substring(i);
				}
			}

			return type;
		}

		/// <summary>
		/// Cleans the type of the dictionary.
		/// </summary>
		private bool CleanDictionaryType(string dictType, ref string type)
		{
			if (!type.StartsWith(dictType))
			{
				return false;
			}
			type = type.Replace(dictType, string.Empty);
			type = type.Replace('[', '<');
			type = type.Replace(']', '>');
			type = type.Replace(",", ", ");
			type = "Dictionary" + type;
			return true;
		}

		/// <summary>
		/// Formats a string for display showing the specified count.
		/// </summary>
		protected string FormatCountString(int count)
		{
			return $"Count = {count}";
		}

		/// <summary>
		/// Determines whether the object at the specified index is expanded.
		/// </summary>
		public virtual bool IsExpanded(int index)
		{
			var currObj = this[index];
			var nextObj = (index + 1 < Count ? this[index + 1] : null);
			return (nextObj != null && nextObj.Level > currObj.Level);
		}

		/// <summary>
		/// Determines whether the object at the specified index is a terminus (i.e. has no
		/// following items at the same level).
		/// </summary>
		public virtual bool IsTerminus(int index)
		{
			if (index >= 0 && index < Count)
			{
				var level = this[index].Level;
				for (var i = index + 1; i < Count; i++)
				{
					if (this[i].Level == level)
					{
						return false;
					}
					if (this[i].Level < level)
					{
						break;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Determines whether or not the item specified by index has any following uncles at
		/// the specified level.
		/// </summary>
		public virtual bool HasFollowingUncleAtLevel(int index, int level)
		{
			if (index < 0 || index >= Count || this[index].Level == 0)
			{
				return false;
			}
			// Go forward until we find an item with a shallower level.
			for (var i = index + 1; i < Count; i++)
			{
				if (this[i].Level == level)
				{
					return true;
				}
				if (this[i].Level < level)
				{
					break;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets the first preceding item in the list before the one specified by index, that
		/// has a shallower level.
		/// </summary>
		public IInspectorObject GetParent(int index)
		{
			int indexParent;
			return GetParent(index, out indexParent);
		}

		/// <summary>
		/// Gets the first preceding item in the list before the one specified by index, that
		/// has a shallower level.
		/// </summary>
		public IInspectorObject GetParent(int index, out int indexParent)
		{
			indexParent = index;
			if (index >= 0 && index < Count && this[index].Level > 0)
			{
				var level = this[index].Level;
				for (indexParent = index - 1; indexParent >= 0; indexParent--)
				{
					var io = this[indexParent];
					if (io.Level < level)
					{
						return io;
					}
				}
			}

			indexParent = -1;
			return null;
		}

		/// <summary>
		/// Gets the index of the IInspectorObject whose OriginalObject is equal to the
		/// specified object.
		/// </summary>
		public int IndexOfOrig(object obj)
		{
			for (var i = 0; i < Count; i++)
			{
				if (this[i].OriginalObject == obj)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Gets the index of the IInspectorObject whose Object is equal to the specified
		/// object.
		/// </summary>
		public int IndexOf(object obj)
		{
			for (var i = 0; i < Count; i++)
			{
				if (this[i].Object == obj)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Compares the inspector object names.
		/// </summary>
		protected int CompareInspectorObjectNames(IInspectorObject x, IInspectorObject y)
		{
			if (x == null && y == null)
			{
				return 0;
			}
			if (string.IsNullOrEmpty(x.DisplayName) && string.IsNullOrEmpty(y.DisplayName))
			{
				return 0;
			}
			var lx = long.MaxValue;
			var ly = long.MaxValue;

			if (x.DisplayName.StartsWith("[") && x.DisplayName.EndsWith("]"))
			{
				var num = x.DisplayName.TrimStart('[');
				num = num.TrimEnd(']');
				long.TryParse(num, out lx);
			}
			if (y.DisplayName.StartsWith("[") && y.DisplayName.EndsWith("]"))
			{
				var num = y.DisplayName.TrimStart('[');
				num = num.TrimEnd(']');
				long.TryParse(num, out ly);
			}

			return lx < long.MaxValue || ly < long.MaxValue ? (int)(lx - ly)
				: m_sortAscending ? string.Compare(x.DisplayName, y.DisplayName, true)
					: string.Compare(y.DisplayName, x.DisplayName, true);
		}
	}
}