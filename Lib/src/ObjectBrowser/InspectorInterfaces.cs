using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.ObjectBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IInspectorList
	{
		/// <summary></summary>
		event EventHandler BeginItemExpanding;
		/// <summary></summary>
		event EventHandler EndItemExpanding;

		/// <summary></summary>
		void Initialize(object obj);

		/// <summary></summary>
		int Count { get; }

		/// <summary></summary>
		object TopLevelObject { get; }

		/// <summary></summary>
		IInspectorObject this[int index] { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the object expansion.
		/// </summary>
		/// <param name="index">The index.</param>
		/// ------------------------------------------------------------------------------------
		bool ToggleObjectExpansion(int index);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the object at the specified index is expanded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IsExpanded(int index);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collapses the object at the specified index.
		/// </summary>
		/// <param name="index">The index of the object in the list.</param>
		/// ------------------------------------------------------------------------------------
		bool CollapseObject(int index);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expands the specified object at the specified index.
		/// </summary>
		/// <param name="index">The index of the object to expand.</param>
		/// ------------------------------------------------------------------------------------
		bool ExpandObject(int index);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the object at the specified index is a terminus.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns>
		/// 	<c>true</c> if the specified index is terminus; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		bool IsTerminus(int index);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the item specified by index has any following uncles at
		/// the specified level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool HasFollowingUncleAtLevel(int index, int level);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first preceding item in the list before the one specified by index, that
		/// has a shallower level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IInspectorObject GetParent(int index);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first preceding item in the list before the one specified by index, that
		/// has a shallower level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IInspectorObject GetParent(int index, out int indexParent);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IInspectorObject
	{
		/// <summary></summary>
		int Level { get; set; }

		/// <summary></summary>
		bool HasChildren { get; set; }

		/// <summary>Used by custom fields</summary>
		int Flid { get; set; }

		/// <summary></summary>
		object OwningObject { get; set; }

		/// <summary></summary>
		IInspectorObject ParentInspectorObject { get; set; }

		/// <summary></summary>
		IInspectorList OwningList { get; set; }

		/// <summary></summary>
		object OriginalObject { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// There are certain cases (e.g. when OriginalObject is a generic collection) in which
		/// the object that's displayed is really a reconstituted version of OriginalObject.
		/// This property stores the reconstituted object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		object Object { get; set; }

		/// <summary></summary>
		string DisplayName { get; set; }

		/// <summary></summary>
		string DisplayValue { get; set; }

		/// <summary></summary>
		string DisplayType { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value based on the sum of the hash codes for the OriginalObject, Level,
		/// DisplayName, DisplayValue and DisplayType properties. This key should be the same
		/// for two IInspectorObjects having the same values for those properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int Key { get; }
	}
}
