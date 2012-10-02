using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;

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
		/// This function adds to the view a display of a single object of type T.
		/// The initial value, obtained and added immediately, is obtained by executing fetchItem.
		/// It is expected that fetchItem decomposes into a property of an object.
		/// For example, one might pass () => someObj.SomeProp. Display.Of decomposes this function,
		/// and looks for an event by the name SomePropChanged. If one is found, it hooks this event,
		/// and will replace what it put in the view with an appropriate new display whenever the event
		/// indicates that the property has changed.
		///
		/// It returns an ItemBuilder, and one of the Using methods of the ItemBuilder must be
		/// called next to specify how to display the item. The resulting Flow object, possibly
		/// after calling various modifiers, is normally passed to ViewBuilder.Show().
		///
		/// For example: root.ViewBuilder.Show(Display.Of(()=>someObj.SomeProp).Using(...)).
		/// </summary>
		/// <remarks>I wish this could just be another overload of 'Of', but the problem is,
		/// if we do that then this overload rather than the Expression(Func(IEnumerable(T))) one
		/// is selected when we pass a func that returns any kind of collection except
		/// IEnumerable itself. The type inference system guesses wrong!</remarks>
		public static ItemBuilder<T> OfObj<T>(Expression<Func<T>> fetchItem) where T : class
		{
			var flow = new AddObjFlow<T>();
			flow.FetchItem = fetchItem;
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

		public static Flow Of(string content, int ws)
		{
			return new LiteralFlow(content, ws);
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
		/// Returns a flow which causes the specified alternative of the multistring obtained by
		/// executing the fetchString() function.
		/// </summary>
		/// <remarks>Enhance JohnT: we don't really need this overload to take a Func, it could just
		/// take an IViewMultiString, since we can read the value of the alternative from it
		/// whenever we want to update the screen. However, all similar overloads take a Func,
		/// so it's nice to make this consistent. Also, if we don't implement the StringChanged
		/// event in the FieldWorks MultiAccessor, we will need to change this to an Expression
		/// and take it apart to get the CmObject and hook up to get PropChanged calls about it,
		/// so it seems safer to leave it taking a functor for now.</remarks>
		public static MlsExpressionFlow Of(Func<IViewMultiString> fetchString, int ws)
		{
			return new MlsExpressionFlow(fetchString, ws);
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
