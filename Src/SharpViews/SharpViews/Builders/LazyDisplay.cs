// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// This class exists solely to implement the overloads of LazyDisplay.Of as part of the fluent language.
	/// It implements a subset of the operations supported by Display.Of
	/// </summary>
	public class LazyDisplay
	{
		/// <summary>
		/// This function adds to the view a display of a sequence of objects of type T.
		/// The initial value, obtained immediately, is obtained by executing fetchItems.
		/// It is expected that fetchItems decomposes into a property of an object.
		/// For example, one might pass () => someObj.SomeProp. Display.Of decomposes this function,
		/// and looks for an event by the name SomePropChanged. If one is found, it hooks this event,
		/// and will replace what it put in the view with an appropriate new display whenever the event
		/// indicates that the property has changed.
		///
		/// It returns a LazyItemBuilder, and one of the Using methods of the ItemBuilder must be
		/// called to specify how to display one item. The resulting Flow object, possibly
		/// after calling various modifiers, is normally passed to ViewBuilder.LazyShow().
		/// Optionally, a method of the LazyItemBuilder may be called to specify how to estimate the
		/// size of an item.
		///
		/// As is normal with lazy views, the individual items will not be built until needed.
		///
		/// For example: root.ViewBuilder.Show(LazyDisplay.Of(()=>someObj.SomeProp).Using(...)).
		/// </summary>
		public static ItemBuilder<T> Of<T>(Expression<Func<IEnumerable<T>>> fetchItems) where T : class
		{
			var flow = new LazyAddObjSeqFlow<T>();
			flow.FetchItems = fetchItems;
			// The flow needs to remember the item builder so as to get from it the details of
			// how to build items.
			// Enhance JohnT: the other possibility is that ItemBuilder.Using stores the information
			// in the flow...but it would have to cast the flow to a subclass to do it.
			flow.ItemBuilder = new ItemBuilder<T>(flow);
			return flow.ItemBuilder;
		}
	}
}
