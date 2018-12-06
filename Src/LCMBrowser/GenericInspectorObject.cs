// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LCMBrowser
{
	/// <summary />
	public class GenericInspectorObject : IInspectorObject
	{
		/// <summary>
		/// There are certain cases (e.g. when OriginalObject is a generic collection) in which
		/// the object that's displayed is really a reconstituted version of OriginalObject.
		/// This property stores the reconstituted object.
		/// </summary>
		public virtual object Object { get; set; }

		/// <summary />
		public virtual object OriginalObject { get; set; }

		/// <summary />
		public virtual int Level { get; set; }

		/// <summary />
		public virtual IInspectorList OwningList { get; set; }

		/// <summary />
		public virtual IInspectorObject ParentInspectorObject { get; set; }

		/// <summary />
		public virtual int Flid { get; set; }

		/// <summary />
		public virtual string DisplayName { get; set; }

		/// <summary />
		public virtual string DisplayValue { get; set; }

		/// <summary />
		public virtual string DisplayType { get; set; }

		/// <summary />
		public virtual bool HasChildren { get; set; }

		/// <summary />
		public object OwningObject { get; set; }

		/// <summary>
		/// Gets a value based on the sum of the hash codes for the OriginalObject, Level,
		/// DisplayName, DisplayValue and DisplayType properties. This key should be the same
		/// for two IInspectorObjects having the same values for those properties.
		/// </summary>
		public int Key
		{
			get
			{
				var key = Level.GetHashCode();
				if (DisplayName != null)
				{
					key += DisplayName.GetHashCode();
				}
				if (DisplayType != null)
				{
					key += DisplayType.GetHashCode();
				}
				if (DisplayValue != null)
				{
					key += DisplayValue.GetHashCode();
				}
				for (var ioParent = ParentInspectorObject; ioParent != null; ioParent = ioParent.ParentInspectorObject)
				{
					key += ioParent.Key;
				}

				return key;
			}
		}
	}
}