// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;

namespace SIL.ObjectBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class GenericInspectorObjectList : List<IInspectorObject>, IInspectorList
	{
		/// <summary></summary>
		public event EventHandler BeginItemExpanding;
		/// <summary></summary>
		public event EventHandler EndItemExpanding;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum SortField
		{
			/// <summary></summary>
			Name,
			/// <summary></summary>
			Value,
			/// <summary></summary>
			Type
		}

		private object m_topLevelObj;
		private bool m_sortAscending = true;
		//private SortField m_sortField = SortField.Name;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the list using the specified top level object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Initialize(object topLevelObj)
		{
			Clear();
			m_topLevelObj = topLevelObj;
			AddRange(GetInspectorObjects(topLevelObj, 0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public object TopLevelObject
		{
			get { return m_topLevelObj; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the object expansion.
		/// </summary>
		/// <param name="index">The index.</param>
		/// ------------------------------------------------------------------------------------
		public virtual bool ToggleObjectExpansion(int index)
		{
			if (index < 0 || index >= Count)
				throw new ArgumentOutOfRangeException("index");

			IInspectorObject currObj = this[index];
			if (currObj == null)
				throw new NullReferenceException("currObj");

			if (!currObj.HasChildren)
				return false;

			if (IsExpanded(index))
				return CollapseObject(index);

			return ExpandObject(index);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collapses the object at the specified index.
		/// </summary>
		/// <param name="index">The index of the object in the list.</param>
		/// ------------------------------------------------------------------------------------
		public virtual bool CollapseObject(int index)
		{
			IInspectorObject io = this[index];
			int currLevel = io.Level;
			int count = 0;
			for (int i = index + 1; i < Count && this[i].Level > currLevel; i++, count++);
			if (count == 0)
				return false;

			RemoveRange(index + 1, count);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the specified object at the specified index.
		/// </summary>
		/// <param name="index">The index of the object to expand.</param>
		/// ------------------------------------------------------------------------------------
		public virtual bool ExpandObject(int index)
		{
			IInspectorObject io = this[index];

			List<IInspectorObject> list = GetInspectorObjects(io, io.Level + 1);
			if (list.Count == 0)
				return false;

			if (BeginItemExpanding != null)
				BeginItemExpanding(io, EventArgs.Empty);

			InsertRange(index + 1, list);

			if (EndItemExpanding != null)
				EndItemExpanding(io, EventArgs.Empty);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of IInspectorObject objects representing all the properties for the
		/// specified object, which is assumed to be at the specified level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual List<IInspectorObject> GetInspectorObjects(object obj, int level)
		{
			IInspectorObject ioParent = obj as IInspectorObject;
			if (ioParent != null)
				obj = ioParent.Object;

			List<IInspectorObject> list = new List<IInspectorObject>();

			// We used to check if the object implemented ICollection but that didn't
			// work for Linq results. This works for both, but we need to make sure the
			// object is not a string because we don't want to show strings as an array
			// of characters.
			IEnumerable enumList = obj as IEnumerable;
			if (enumList != null && enumList.GetType() != typeof(string))
			{
				int i = 0;
				foreach (object item in enumList)
				{
					IInspectorObject io = CreateInspectorObject(item, obj, ioParent, level);
					io.DisplayName = string.Format("[{0}]", i++);
					list.Add(io);
				}

				return list;
			}

			//ICollection collection = obj as ICollection;
			//if (collection != null)
			//{
			//    int i = 0;
			//    foreach (object item in collection)
			//    {
			//        IInspectorObject io = CreateInspectorObject(item, obj, ioParent, level);
			//        io.DisplayName = string.Format("[{0}]", i++);
			//        list.Add(io);
			//    }

			//    return list;
			//}

			PropertyInfo[] props = GetPropsForObj(obj);
			foreach (PropertyInfo pi in props)
			{
				try
				{
					object propObj = pi.GetValue(obj, null);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties for the specified object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual PropertyInfo[] GetPropsForObj(object obj)
		{
			BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty;
			return (obj == null ? new PropertyInfo[] {} : obj.GetType().GetProperties(flags));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an inspector object for the specified property info., object and level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual IInspectorObject CreateInspectorObject(object obj, object owningObj,
			IInspectorObject ioParent, int level)
		{
			return CreateInspectorObject(null, obj, owningObj, ioParent, level);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an inspector object for the specified property info., object and level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual IInspectorObject CreateInspectorObject(PropertyInfo pi,
			object obj, object owningObj, IInspectorObject ioParent, int level)
		{
			bool fSetHasChildrenFromType = true;
			Type objType = null;

			GenericInspectorObject gio = new GenericInspectorObject();
			gio.Object = gio.OriginalObject = obj;
			gio.OwningList = this;
			gio.Flid = 0;
			gio.ParentInspectorObject = ioParent;
			gio.Level = level;
			gio.OwningObject = owningObj;
			gio.DisplayValue = (obj == null ? "null" : obj.ToString());

			if (obj != null)
			{
				objType = obj.GetType();
				gio.DisplayType = CleanupGenericListType(objType.ToString());

				// Check if the object is a collection. If so, we need to convert it
				// to an array and store that array as the object. It makes for easier
				// handling when the object is expanded. REVIEW: Are there more enumerable
				// types that we should not treat as an array?
				bool showAsArray = (obj.GetType() != typeof(string) &&
					objType.GetInterface("IEnumerable") != null);

				if (showAsArray || obj.ToString() != null &&
					obj.ToString().StartsWith("System.Linq.Enumerable+<CastIterator>"))
				{
					var collection = new List<object>(((IEnumerable)obj).Cast<object>());
					object[] array = collection.ToArray();
					gio.Object = array;
					gio.DisplayValue = FormatCountString(array.Length);
					gio.HasChildren = (array.Length > 0);
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
					BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
					gio.HasChildren = (obj != null && objType.GetProperties(flags).Length > 0 &&
						!objType.IsPrimitive && objType != typeof(string) && objType != typeof(Guid));
				}
			}

			return gio;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an inspector object for an exception.
		/// </summary>
		/// <param name="e">The exception</param>
		/// <param name="obj">The object.</param>
		/// <param name="level">The indentation level.</param>
		/// ------------------------------------------------------------------------------------
		protected IInspectorObject CreateExceptionInspectorObject(Exception e,
			object obj, string propName, int level, IInspectorObject ioParent)
		{
			string msg = e.Message;
			StringBuilder trace = new StringBuilder("Exception:" + e.StackTrace);
			e = e.InnerException;

			while (e != null)
			{
				msg = e.Message;
				trace.Append(Environment.NewLine);
				trace.Append(e.StackTrace);
				e = e.InnerException;
			}

			GenericInspectorObject gio = new GenericInspectorObject();
			gio.DisplayName = propName;
			gio.DisplayValue = "Error: " + msg;
			gio.DisplayType = trace.ToString();
			gio.Flid = 0;
			gio.Object = gio.OriginalObject = obj;
			gio.OwningList = this;
			gio.Level = level;
			gio.OwningObject = null;
			gio.ParentInspectorObject = ioParent;

			return gio;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleanups the text for types that are generic lists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string CleanupGenericListType(string type)
		{
			if (type == null)
				return string.Empty;

			if (CleanDictionaryType("System.Collections.Generic.KeyValuePair`2", ref type) ||
				CleanDictionaryType("System.Collections.Generic.Dictionary`2", ref type))
			{
				return type;
			}

			int i = type.IndexOf("`1[");
			if (i < 0)
				return type;

			string collection = type.Substring(0, i);
			string collectionOf = type.Substring(i + 3);

			i = collection.LastIndexOf('.');
			if (i > 0)
				collection = collection.Substring(i + 1);

			i = collectionOf.LastIndexOf('.');
			if (i > 0)
				collectionOf = collectionOf.Substring(i + 1);

			type = collection + "<" + collectionOf;
			type = type.Replace(']', '>');

			if (type.StartsWith("Enumerable+<CastIterator>"))
			{
				i = type.LastIndexOf("<");
				if (i > 0)
					type = "IEnumerable" + type.Substring(i);
			}

			return type;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans the type of the dictionary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool CleanDictionaryType(string dictType, ref string type)
		{
			if (!type.StartsWith(dictType))
				return false;

			type = type.Replace(dictType, string.Empty);
			type = type.Replace('[', '<');
			type = type.Replace(']', '>');
			type = type.Replace(",", ", ");
			type = "Dictionary" + type;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Formats a string for display showing the specified count.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected string FormatCountString(int count)
		{
			return string.Format("Count = {0}", count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the object at the specified index is expanded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsExpanded(int index)
		{
			IInspectorObject currObj = this[index];
			IInspectorObject nextObj = (index + 1 < Count ? this[index + 1] : null);
			return (nextObj != null && nextObj.Level > currObj.Level);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the object at the specified index is a terminus (i.e. has no
		/// following items at the same level).
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>
		/// 	<c>true</c> if the specified index is terminus; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsTerminus(int index)
		{
			if (index >= 0 && index < Count)
			{
				int level = this[index].Level;
				for (int i = index + 1; i < Count; i++)
				{
					if (this[i].Level == level)
						return false;

					if (this[i].Level < level)
						break;
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the item specified by index has any following uncles at
		/// the specified level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool HasFollowingUncleAtLevel(int index, int level)
		{
			if (index < 0 || index >= Count || this[index].Level == 0)
				return false;

			// Go forward until we find an item with a shallower level.
			for (int i = index + 1; i < Count; i++)
			{
				if (this[i].Level == level)
					return true;

				if (this[i].Level < level)
					break;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first preceding item in the list before the one specified by index, that
		/// has a shallower level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IInspectorObject GetParent(int index)
		{
			int indexParent;
			return GetParent(index, out indexParent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first preceding item in the list before the one specified by index, that
		/// has a shallower level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IInspectorObject GetParent(int index, out int indexParent)
		{
			indexParent = index;

			if (index >= 0 && index < Count && this[index].Level > 0)
			{
				int level = this[index].Level;
				for (indexParent = index - 1; indexParent >= 0; indexParent--)
				{
					IInspectorObject io = this[indexParent];
					if (io.Level < level)
						return io;
				}
			}

			indexParent = -1;
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the IInspectorObject whose OriginalObject is equal to the
		/// specified object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IndexOfOrig(object obj)
		{
			for (int i = 0; i < Count; i++)
			{
				if (this[i].OriginalObject == obj)
					return i;
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the IInspectorObject whose Object is equal to the specified
		/// object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IndexOf(object obj)
		{
			for (int i = 0; i < Count; i++)
			{
				if (this[i].Object == obj)
					return i;
			}

			return -1;
		}

		#region List sort comparer methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the inspector object names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int CompareInspectorObjectNames(IInspectorObject x, IInspectorObject y)
		{
			if (x == null && y == null)
				return 0;

			if (string.IsNullOrEmpty(x.DisplayName) && string.IsNullOrEmpty(y.DisplayName))
				return 0;

			long lx = long.MaxValue;
			long ly = long.MaxValue;

			if (x.DisplayName.StartsWith("[") && x.DisplayName.EndsWith("]"))
			{
				string num = x.DisplayName.TrimStart('[');
				num = num.TrimEnd(']');
				long.TryParse(num, out lx);
			}

			if (y.DisplayName.StartsWith("[") && y.DisplayName.EndsWith("]"))
			{
				string num = y.DisplayName.TrimStart('[');
				num = num.TrimEnd(']');
				long.TryParse(num, out ly);
			}

			if (lx < long.MaxValue || ly < long.MaxValue)
				return (int)(lx - ly);

			return (m_sortAscending ?
				string.Compare(x.DisplayName, y.DisplayName, true) :
				string.Compare(y.DisplayName, x.DisplayName, true));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the inspector object values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int CompareInspectorObjectValues(IInspectorObject x, IInspectorObject y)
		{
			if (x == null && y == null)
				return 0;

			if (string.IsNullOrEmpty(x.DisplayValue) && string.IsNullOrEmpty(y.DisplayValue))
				return 0;

			return (m_sortAscending ?
				string.Compare(x.DisplayValue, y.DisplayValue, true) :
				string.Compare(y.DisplayValue, x.DisplayValue, true));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the inspector object types.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int CompareInspectorObjectTypes(IInspectorObject x, IInspectorObject y)
		{
			if (x == null && y == null)
				return 0;

			if (string.IsNullOrEmpty(x.DisplayType) && string.IsNullOrEmpty(y.DisplayType))
				return 0;

			return (m_sortAscending ?
				string.Compare(x.DisplayType, y.DisplayType, true) :
				string.Compare(y.DisplayType, x.DisplayType, true));
		}

		#endregion
	}

	#region GenericInspectorObject class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class GenericInspectorObject : IInspectorObject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// There are certain cases (e.g. when OriginalObject is a generic collection) in which
		/// the object that's displayed is really a reconstituted version of OriginalObject.
		/// This property stores the reconstituted object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual object Object { get; set; }

		/// <summary></summary>
		public virtual object OriginalObject { get; set; }

		/// <summary></summary>
		public virtual int Level { get; set; }

		/// <summary></summary>
		public virtual IInspectorList OwningList { get; set; }

		/// <summary></summary>
		public virtual IInspectorObject ParentInspectorObject { get; set; }

		/// <summary></summary>
		public virtual int Flid { get; set; }

		/// <summary></summary>
		public virtual string DisplayName { get; set; }

		/// <summary></summary>
		public virtual string DisplayValue { get; set; }

		/// <summary></summary>
		public virtual string DisplayType { get; set; }

		/// <summary></summary>
		public virtual bool HasChildren { get; set; }

		/// <summary></summary>
		public object OwningObject { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value based on the sum of the hash codes for the OriginalObject, Level,
		/// DisplayName, DisplayValue and DisplayType properties. This key should be the same
		/// for two IInspectorObjects having the same values for those properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Key
		{
			get
			{
				int key = Level.GetHashCode();

				if (DisplayName != null)
					key += DisplayName.GetHashCode();

				if (DisplayType != null)
					key += DisplayType.GetHashCode();

				if (DisplayValue != null)
					key += DisplayValue.GetHashCode();

				for (IInspectorObject ioParent = ParentInspectorObject; ioParent != null;
					ioParent = ioParent.ParentInspectorObject)
				{
					key += ioParent.Key;
				}

				return 	key;
			}
		}
	}

	#endregion
}
