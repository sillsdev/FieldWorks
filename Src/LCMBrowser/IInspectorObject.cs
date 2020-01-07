// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LCMBrowser
{
	/// <summary />
	public interface IInspectorObject
	{
		/// <summary />
		int Level { get; set; }

		/// <summary />
		bool HasChildren { get; set; }

		/// <summary>Used by custom fields</summary>
		int Flid { get; set; }

		/// <summary />
		object OwningObject { get; set; }

		/// <summary />
		IInspectorObject ParentInspectorObject { get; set; }

		/// <summary />
		IInspectorList OwningList { get; set; }

		/// <summary />
		object OriginalObject { get; set; }

		/// <summary>
		/// There are certain cases (e.g. when OriginalObject is a generic collection) in which
		/// the object that's displayed is really a reconstituted version of OriginalObject.
		/// This property stores the reconstituted object.
		/// </summary>
		object Object { get; set; }

		/// <summary />
		string DisplayName { get; set; }

		/// <summary />
		string DisplayValue { get; set; }

		/// <summary />
		string DisplayType { get; set; }

		/// <summary>
		/// Gets a value based on the sum of the hash codes for the OriginalObject, Level,
		/// DisplayName, DisplayValue and DisplayType properties. This key should be the same
		/// for two IInspectorObjects having the same values for those properties.
		/// </summary>
		int Key { get; }
	}
}