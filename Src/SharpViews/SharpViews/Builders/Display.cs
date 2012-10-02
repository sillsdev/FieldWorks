using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// This class exists solely to implement the overloads of Display.Of as part of the fluent language.
	/// </summary>
	public class Display
	{
		/// <summary>
		/// This function adds to the view a display of a sequence of objects of type T.
		/// The initial value, obtained and added immediately, is obtained by executing fetchItems.
		/// It is expected that fetchItems decomposes into a property of an object.
		/// For example, one might pass () => someObj.SomeProp. Display.Of decomposes this function,
		/// and looks for an event by the name SomePropChanged. If one is found, it hooks this event,
		/// and will replace what it put in the view with an appropriate new display whenever the event
		/// indicates that the property has changed.
		///
		/// It returns an ItemBuilder, and one of the Using methods of the ItemBuilder must be
		/// called next to specify how to display one item. The resulting Flow object, possibly
		/// after calling various modifiers, is normally passed to ViewBuilder.Show().
		///
		/// For example: root.ViewBuilder.Show(Display.Of(()=>someObj.SomeProp).Using(...)).
		/// </summary>
		public static ItemBuilder<T> Of<T>(Expression<Func<IEnumerable<T>>> fetchItems) where T : class
		{
			var flow = new AddObjSeqFlow<T>();
			flow.FetchItems = fetchItems;
			// The flow needs to remember the item builder so as to get from it the details of
			// how to build items.
			// Enhance JohnT: the other possibility is that ItemBuilder.Using stores the information
			// in the flow...but it would have to cast the flow to a subclass to do it.
			flow.ItemBuilder = new ItemBuilder<T>(flow);
			return flow.ItemBuilder;
		}

		/// <summary>
		/// Returns a Flow which causes the specified literal string to be added to the view.
		/// Additional formatting may be applied to the flow, as in
		/// builder.Show(Paragraph.Containing(Display.Of("a literal").Italic)));
		/// </summary>
		public static Flow Of(string content)
		{
			return new LiteralFlow(content);
		}

		/// <summary>
		/// Returns a flow which causes the specified string to be shown, as in ViewBuilder.AddString.
		/// The string is treated as being in the specified writing system.
		/// </summary>
		public static StringExpressionFlow Of(Expression<Func<string>> fetchString, int ws)
		{
			return new StringExpressionFlow(fetchString, ws);
		}

		public static TssExpressionFlow Of(Expression<Func<ITsString>> fetchString)
		{
			return new TssExpressionFlow(fetchString);
		}

		/// <summary>
		/// Returns a flow which causes the specified string to be shown, as in ViewBuilder.AddString.
		/// The string is treated as being in the default user writing system.
		/// </summary>
		public static Flow Of(Expression<Func<string>> fetchString)
		{
			return new StringExpressionFlow(fetchString, 0);
		}

		public static Flow Block(Color color, int mpWidth, int mpHeight)
		{
			return new BlockFlow(color, mpWidth, mpHeight);
		}
	}
}
