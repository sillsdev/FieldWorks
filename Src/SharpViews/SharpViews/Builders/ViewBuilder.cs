// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// Class ViewBuilder supports a fluent api for constructing views.
	/// </summary>
	public class ViewBuilder
	{
		private GroupBox m_destination; // where we put the boxes built.
		private Box m_insertAfter; // box to insert after in m_destination, if it is not a paragraph.
		private int m_insertRunAt; // position to insert new runs in m_destination, if it is a paragraph
		// Where we put hookups linking boxes built.
		internal GroupHookup CurrentHookup { get; set;}
		// currently derived from GroupBox, but allow for enhancement to set separately
		// in a new constructor, in case we want to build things not yet installed in the root.
		private RootBox m_rootBox;
		public ViewBuilder(GroupBox destination)
		{
			m_destination = destination;
			m_rootBox = destination.Root;
			if (m_rootBox.RootHookup == null && m_destination == m_rootBox)
			{
				m_rootBox.RootHookup = new RootHookup(m_rootBox);
			}
			// Enhance JohnT: if we really use the capability to make a builder whose box is not the root,
			// we may need a more subtle way to get the appropriate current hookup.
			CurrentHookup = m_rootBox.RootHookup;
		}

		/// <summary>
		/// The rootbox into which the content will go.
		/// </summary>
		protected RootBox Root { get { return m_rootBox; } }

		/// <summary>
		/// Show some expression, such as Paragraph.Containing(Display.Of(...).Using(...).Format(...), by letting the Flow
		/// fluent language item constructed by the Show() argument add itself to the builder.
		///
		/// How this works:
		/// Display.Of has various overloads; ones that display objects or object sequences return
		/// ItemBuilders, which implement various overloads of Using to indicate how to display items.
		/// ItemBuilder.Using, and overloads of Of() that don't require Using, return a Flow,
		/// which has methods for setting various format properties (bold, italic, margins, style).
		/// Paragraph.Containing and similar methods take a plain Flow and return it, but specify
		/// the box it should use. The final step is for the builder to Show() the flow, which calls
		/// a method on the flow itself to add the appropriate material to the builder (and thus the view).
		/// </summary>
		public ViewBuilder Show(Flow flow)
		{
			flow.Show(this);
			return this;
		}

		/// <summary>
		/// For the fluent language, this allows several flows to be shown by just listing them
		/// as parameters, such as builder.Show(Paragraph.Containing(...), Paragraph.Containing(...),...).
		/// </summary>
		public ViewBuilder Show(params Flow[] flows)
		{
			foreach (var flow in flows)
				flow.Show(this);
			return this;
		}

		private AssembledStyles m_nestedBoxStyles;
		/// <summary>
		/// Get the styles that should be used for making a box nested inside the current one.
		/// </summary>
		public AssembledStyles NestedBoxStyles
		{
			get
			{
				if (m_nestedBoxStyles != null)
					return m_nestedBoxStyles;
				return m_destination.Style.ResetNonInherited();
			}
		}

		/// <summary>
		/// Add a group box to the current box. Make it the current destination for the duration
		/// of calling AddContents, then restore the current destination.
		/// </summary>
		public ViewBuilder AddGroupBox(GroupBox box, Action<ViewBuilder> addContents)
		{
			var oldDest = m_destination;
			var oldNestedBoxStyles = m_nestedBoxStyles;
			if (box is RunBox)
			{
				// allow the new contents to add to the current box, but override the default nested styles.
				m_nestedBoxStyles = box.Style;
				addContents(this);
			}
			else
			{
				// Let nested styles be obtained as needed from the new destination box.
				// No need to compute them if they are not used.
				m_nestedBoxStyles = null;
				InsertBox(box);
				var oldInsertAfter = m_insertAfter;
				m_destination = box;
				// in case we aren't making hookups, insert at start of new destination
				m_insertAfter = null;
				m_insertRunAt = 0;
				addContents(this);
				m_insertAfter = oldInsertAfter;
			}
			m_destination = oldDest;
			m_nestedBoxStyles = oldNestedBoxStyles;
			return this;
		}

		/// <summary>
		/// This function adds a string to the group box, in the specified writing system
		/// </summary>
		public ViewBuilder AddString(Expression<Func<string>> fetchString, int ws)
		{
			var fetcher = fetchString.Compile();
			var run = new StringClientRun(fetcher(), NestedBoxStyles.WithWs(ws));
			return AddStringClientRun(run, fetchString, fetcher);
		}
		/// <summary>
		/// This function adds a string to the group box, in the specified writing system
		/// </summary>
		public ViewBuilder AddString(Expression<Func<string>> fetchString, int ws, string substitute, int substituteWs)
		{
			var fetcher = fetchString.Compile();
			var run = new SubstituteStringClientRun(fetcher(), NestedBoxStyles.WithWs(ws),
				substitute, NestedBoxStyles.WithWs(substituteWs));
			return AddStringClientRun(run, fetchString, fetcher);
		}

		/// <summary>
		/// The common logic of the two versions of AddString.
		/// </summary>
		private ViewBuilder AddStringClientRun(StringClientRun run, Expression<Func<string>> fetchString, Func<string> fetcher)
		{
			ParaBox para = InsertParaOrRun(run);

			// Try to hook an event for notification of changes to the property.
			var mexp = (fetchString.Body as MemberExpression);
			var argExp = Expression.Lambda<Func<object>>(mexp.Expression);
			Type type = mexp.Member.DeclaringType;
			string name = mexp.Member.Name;
			EventInfo einfo = type.GetEvent(name + "Changed");
			if (einfo != null)
			{

				var target = argExp.Compile().Invoke();
				var stringHookup = new StringHookup(target, fetcher,
					hookup => einfo.AddEventHandler(target, new EventHandler<EventArgs>(hookup.StringPropChanged)),
					hookup => einfo.RemoveEventHandler(target, new EventHandler<EventArgs>(hookup.StringPropChanged)),
					para);
				AddHookupToRun(run, stringHookup);
				var propInfo = type.GetProperty(name);
				if (propInfo.CanWrite)
					stringHookup.Writer = newVal => propInfo.SetValue(target, newVal, null);
			}
			return this;
		}

		/// <summary>
		/// This function adds a (literal) string to the group box, in the specified writing system
		/// (and returns the builder, in case we want to do more).
		/// </summary>
		public ViewBuilder AddString(string content, int ws)
		{
			var actualWs = ws;
			if (ws == 0)
			{
				var factory = Root.RendererFactory;
				if (factory != null)
					actualWs = factory.UserWs;
			}
			var run = new StringClientRun(content, NestedBoxStyles.WithWs(actualWs));
			ParaBox para = InsertParaOrRun(run);
			return this;
		}

		private ParaBox InsertParaOrRun(TextClientRun run)
		{
			var para = m_destination as ParaBox;
			if (para != null)
			{
				((ParaBox) m_destination).InsertRun(m_insertRunAt, run);
				m_insertRunAt++;
			}
			else
			{
				var runs = new List<IClientRun>();
				runs.Add(run);
				var source = new TextSource(runs);
				para = new ParaBox(m_destination.Style, source);
				InsertBox(para);
			}
			return para;
		}

		/// <summary>
		/// This function adds a TsString to the group box, in the specified writing system.
		/// Refactor JohnT: Similar enough to the other AddString to be annoying, but different enough
		/// to be hard to factor out the common stuff.
		/// </summary>
		public ViewBuilder AddString(Expression<Func<ITsString>> fetchString)
		{
			var fetcher = fetchString.Compile();
			var tss = GetNonNullTsString(fetcher);
			var run = new TssClientRun(tss, NestedBoxStyles);
			return AddTssClientRun(fetchString, fetcher, run);
		}

		/// <summary>
		/// This function adds a string to the group box, in the specified writing system
		/// </summary>
		public ViewBuilder AddString(Expression<Func<ITsString>> fetchString, string substitute, int substituteWs)
		{
			var fetcher = fetchString.Compile();
			var tss = GetNonNullTsString(fetcher);
			var run = new SubstituteTssClientRun(tss, NestedBoxStyles,
				substitute, NestedBoxStyles.WithWs(substituteWs));
			return AddTssClientRun(fetchString, fetcher, run);
		}

		private ITsString GetNonNullTsString(Func<ITsString> fetcher)
		{
			return fetcher() ?? TsStrFactoryClass.Create().EmptyString(Root.RendererFactory.UserWs);
		}

		private ViewBuilder AddTssClientRun(Expression<Func<ITsString>> fetchString, Func<ITsString> fetcher, TssClientRun run)
		{
			ParaBox para = InsertParaOrRun(run);

			// Try to hook an event for notification of changes to the property.
			var mexp = (fetchString.Body as MemberExpression);
			var argExp = Expression.Lambda<Func<object>>(mexp.Expression);
			Type type = mexp.Member.DeclaringType;
			string name = mexp.Member.Name;
			EventInfo einfo = type.GetEvent(name + "Changed");
			var target = argExp.Compile().Invoke();
			if (einfo == null)
			{
				MakeHookupForString(fetcher, run, name, target, para);
			}
			else
			{
				var stringHookup = new TssHookup(target, fetcher,
					hookup => einfo.AddEventHandler(target, new EventHandler<EventArgs>(hookup.TssPropChanged)),
					hookup => einfo.RemoveEventHandler(target, new EventHandler<EventArgs>(hookup.TssPropChanged)),
					para);
				AddHookupToRun(run, stringHookup);
				var propInfo = type.GetProperty(name);
				if (propInfo.CanWrite)
					stringHookup.Writer = newVal => propInfo.SetValue(target, newVal, null);
			}
			return this;
		}

		public ViewBuilder AddString(Func<IViewMultiString> fetcher, int ws)
		{
			var run = new MlsClientRun(fetcher(), NestedBoxStyles.WithWs(ws));
			return AddMlsClientRun(fetcher, run, ws);
		}

		/// <summary>
		/// This function adds a string to the group box, in the specified writing system
		/// </summary>
		public ViewBuilder AddString(Func<IViewMultiString> fetcher, int ws, string substitute, int substituteWs)
		{
			var run = new SubstituteMlsClientRun(fetcher(), NestedBoxStyles.WithWs(ws), substitute, NestedBoxStyles.WithWs(substituteWs));
			return AddMlsClientRun(fetcher, run, ws);
		}

		private ViewBuilder AddMlsClientRun(Func<IViewMultiString> fetcher, MlsClientRun run, int ws)
		{
			ParaBox para = InsertParaOrRun(run);

			var mls = fetcher();
			var stringHookup = new MlsHookup(null, fetcher(), ws,
				hookup => mls.StringChanged += hookup.MlsPropChanged,
				hookup => mls.StringChanged += hookup.MlsPropChanged,
				para);
			AddHookupToRun(run, stringHookup);

			// Try to hook an event for notification of changes to the property.
			//MemberExpression mexp = (fetchString.Body as MemberExpression);
			//var argExp = Expression.Lambda<Func<object>>(mexp.Expression);
			//Type type = mexp.Member.DeclaringType;
			//string name = mexp.Member.Name;
			//EventInfo einfo = type.GetEvent(name + "Changed");
			//var target = argExp.Compile().Invoke();
			//if (einfo == null)
			//{
			//    MakeHookupForString(fetcher, run, name, target, para);
			//}
			//else
			//{
			//    var stringHookup = new MlsHookup(target, fetcher(), ws,
			//        hookup => einfo.AddEventHandler(target, new EventHandler<MlsChangedEventArgs>(hookup.MlsPropChanged)),
			//        hookup => einfo.RemoveEventHandler(target, new EventHandler<MlsChangedEventArgs>(hookup.MlsPropChanged)), para);
			//    AddHookupToRun(run, stringHookup);
			//    var propInfo = type.GetProperty(name);
			//}
			return this;
		}

		/// <summary>
		/// Given a function that fetches strings, a run which represents the initial value of that string
		/// already inserted into a particular paragraph box, and that we have identified the fetcher as
		/// a property with the specified name of the specified target object, but we have not been able to find
		/// a ''Name'Changed' event on the target object, this stub provides a possible place for a subclass to
		/// use an alternative strategy for hooking something up to notify the view of changes to the property.
		/// </summary>
		protected virtual void MakeHookupForString(Func<ITsString> fetcher, TssClientRun run, string name, object target, ParaBox para)
		{
			// In this SharpViewsLight base class we have no alternate strategy. We won't get notifications of changes.
		}

		protected virtual void MakeHookupForString(Func<IViewMultiString> fetcher, MlsClientRun run, string name, object target, ParaBox para)
		{
			// In this SharpViewsLight base class we have no alternate strategy. We won't get notifications of changes.
		}

		internal void AddHookupToRun(TextClientRun run, LiteralStringParaHookup stringHookup)
		{
			run.Hookup = stringHookup;
			stringHookup.ClientRunIndex = stringHookup.ParaBox.Source.ClientRuns.IndexOf(run);
			if (CurrentHookup != null)
				CurrentHookup.InsertChildHookup(run.Hookup, CurrentHookup.Children.Count);
		}

		private void InsertBox(Box box)
		{
			if (m_destination is ParaBox)
			{
				((ParaBox)m_destination).InsertRun(m_insertRunAt, (IClientRun) box);
				m_insertRunAt++;
				// Enhance JohnT: should we do something special instead of the usual insertion into the CurrentHookup?
			}
			else
			{
				m_destination.InsertBox(box, m_insertAfter);
				m_insertAfter = box;
			}
			var currentItem = CurrentHookup as ItemHookup;
			if (currentItem != null)
				currentItem.AddBox(box);
		}

		internal void PushHookup(GroupHookup child, int insertAt)
		{
			m_insertAfter = null; // default insert at start
			m_insertRunAt = 0;
			if (CurrentHookup != null)
			{
				if (insertAt > 0)
				{
					var prevChild = CurrentHookup.Children[insertAt - 1] as IItemsHookup;
					if (prevChild != null)
					{
						if (prevChild.LastBox != null && prevChild.LastBox.Container == m_destination)
							m_insertAfter = prevChild.LastBox;
						var runHookup = prevChild.LastChild as LiteralStringParaHookup;
						if (runHookup != null && runHookup.ParaBox == m_destination)
							m_insertRunAt = runHookup.ClientRunIndex + 1;
					}
				}
				CurrentHookup.InsertChildHookup(child, insertAt);
			}
			CurrentHookup = child;
		}

		internal void PopHookup()
		{
			CurrentHookup = CurrentHookup.ParentHookup;
			m_insertAfter = null;
			m_insertRunAt = 0;
		}

		/// <summary>
		/// Set the object that will be used by the current hookup to handle paragraph-level operations.
		/// It must match, both in that the current hookup can do paragraph operations, and in the
		/// template argument.
		/// </summary>
		public ViewBuilder EditParagraphsUsing<T>(ISeqParagraphOperations<T> paragraphOps)
		{
			var seqHookup = CurrentHookup as IndependentSequenceHookup<T>;
			if (seqHookup == null)
				throw new InvalidOperationException("Can only specify paragraph ops for an independent sequence hookup of the correct type");
			seqHookup.ParagraphOperations = paragraphOps;
			var list = seqHookup.Fetcher() as IList<T>;
			var listSetter = paragraphOps as IParaOpsList<T>;
			if (list != null && listSetter != null)
				listSetter.List = list;
			return this;
		}

		/// <summary>
		/// This function adds to the group box a display of a sequence of objects of type T.
		/// The initial value, obtained and added immediately, is obtained by executing fetchTs.
		/// It is expected that fetchTs decomposes into a property of an object.
		/// For example, one might pass () => someObj.SomeProp. AddObjSeq decomposes this function,
		/// and looks for an event by the name SomePropChanged. If one is found, it hooks this event,
		/// and will replace what it put in the view with an appropriate new display whenever the event
		/// indicates that the property has changed.
		///
		/// The displayOneT method is used to add to the box, via the builder, a display of one item
		/// in the sequence. It is passed the object and the builder (this) and makes calls on the builder
		/// to insert whatever is appropriate.
		///
		/// The method returns 'this' to facilitate adding multiple properties.
		/// </summary>
		internal void AddObjSeq<T>(Expression<Func<IEnumerable<T>>> fetchItems, Action<ViewBuilder, T> displayOneItem)
		{
			Func<IEnumerable<T>> fetcher;
			Action<IReceivePropChanged> hookEventAction;
			Action<IReceivePropChanged> unhookEventAction;
			int tag;
			object target = InterpretFetchExpression(fetchItems, out fetcher, out hookEventAction, out unhookEventAction, out tag);
			AddObjSeq(target, fetcher, hookEventAction, unhookEventAction, displayOneItem, tag);
		}

		internal void AddObj<T>(Expression<Func<T>> fetchItem, Action<ViewBuilder, T> displayOneItem)
		{
			Func<T> fetcher;
			Action<IReceivePropChanged> hookEventAction;
			Action<IReceivePropChanged> unhookEventAction;
			int tag;
			object target = InterpretFetchExpression(fetchItem, out fetcher, out hookEventAction, out unhookEventAction, out tag);
			AddObj(target, fetcher, hookEventAction, unhookEventAction, displayOneItem, tag);
		}

		private object InterpretFetchExpression<T>(Expression<Func<IEnumerable<T>>> fetchItems, out Func<IEnumerable<T>> fetcher, out Action<IReceivePropChanged> hookEventAction, out Action<IReceivePropChanged> unhookEventAction, out int tag)
		{
			var mexp = (fetchItems.Body as MemberExpression);
			var argExp = Expression.Lambda<Func<object>>(mexp.Expression);

			fetcher = fetchItems.Compile();
			object target = argExp.Compile().Invoke();
			var name = mexp.Member.Name;
			Type type = mexp.Member.DeclaringType;
			var propChangeInfo = type.GetEvent(name + "Changed");
			tag = 0;
			if (propChangeInfo == null)
			{
				GetHookupEventActions(name, target, out hookEventAction, out unhookEventAction);
			}
			else
			{
				if (propChangeInfo.EventHandlerType == typeof(EventHandler<ObjectSequenceEventArgs>))
				{
					hookEventAction = hookup => propChangeInfo.AddEventHandler(target, new EventHandler<ObjectSequenceEventArgs>(hookup.PropChanged));
					unhookEventAction =
						hookup => propChangeInfo.RemoveEventHandler(target, new EventHandler<ObjectSequenceEventArgs>(hookup.PropChanged));
				}
				else
				{
					hookEventAction = hookup => propChangeInfo.AddEventHandler(target, new EventHandler<EventArgs>(hookup.PropChanged));
					unhookEventAction =
						hookup => propChangeInfo.RemoveEventHandler(target, new EventHandler<EventArgs>(hookup.PropChanged));
				}
			}
			return target;
		}

		private object InterpretFetchExpression<T>(Expression<Func<T>> fetchItem, out Func<T> fetcher, out Action<IReceivePropChanged> hookEventAction, out Action<IReceivePropChanged> unhookEventAction, out int tag)
		{
			var mexp = (fetchItem.Body as MemberExpression);
			var argExp = Expression.Lambda<Func<object>>(mexp.Expression);

			fetcher = fetchItem.Compile();
			object target = argExp.Compile().Invoke();
			var name = mexp.Member.Name;
			Type type = mexp.Member.DeclaringType;
			var propChangeInfo = type.GetEvent(name + "Changed");
			tag = 0;
			if (propChangeInfo == null)
			{
				GetHookupEventActions(name, target, out hookEventAction, out unhookEventAction);
			}
			else
			{
				hookEventAction = hookup => propChangeInfo.AddEventHandler(target, new EventHandler<EventArgs>(hookup.PropChanged));
				unhookEventAction = hookup => propChangeInfo.RemoveEventHandler(target, new EventHandler<EventArgs>(hookup.PropChanged));
			}
			return target;
		}

		/// <summary>
		/// Given that we are displaying property name of the target object, we would like to obtain actions that will connect and disconnect
		/// us from receiving notifications when that property changes. We have determined that there is no "'Name'Changed" event on the
		/// object. This hook method allows subclasses to provide an alternative strategy for setting up notifications.
		/// </summary>
		protected virtual void GetHookupEventActions(string name, object target, out Action<IReceivePropChanged> hookEventAction, out Action<IReceivePropChanged> unhookEventAction)
		{
			// Fallback if not overridden: we don't know how to get updates, but we can still display the list once.
			// Leave hookEvent and unhookEvent null.
			hookEventAction = null;
			unhookEventAction = null;
		}

		/// <summary>
		/// This function adds to the group box a display of a sequence of objects of type T.
		/// The initial value, obtained and added immediately, is obtained by executing fetchTs.
		/// It is expected that fetchTs decomposes into a property of an object.
		/// For example, one might pass () => someObj.SomeProp. AddObjSeq decomposes this function,
		/// and looks for an event by the name SomePropChanged. If one is found, it hooks this event,
		/// and will replace what it put in the view with an appropriate new display whenever the event
		/// indicates that the property has changed.
		///
		/// The displayOneT method is used to add to the box, via the builder, a display of one item
		/// in the sequence. It is passed the object and the builder (this) and makes calls on the builder
		/// to insert whatever is appropriate.
		///
		/// The method returns 'this' to facilitate adding multiple properties.
		/// </summary>
		internal void AddLazyObjSeq<T>(Expression<Func<IEnumerable<T>>> fetchItems, Action<ViewBuilder, T> displayOneItem) where T:class
		{
			Func<IEnumerable<T>> fetcher;
			Action<IReceivePropChanged> hookEventAction;
			Action<IReceivePropChanged> unhookEventAction;
			int tag;
			object target = InterpretFetchExpression(fetchItems, out fetcher, out hookEventAction, out unhookEventAction, out tag);
			AddLazyObjSeq(target, fetcher, hookEventAction, unhookEventAction, displayOneItem, tag);
		}

		/// <summary>
		/// Should be called only from other overloads. Sets up an independent sequence hookup linking together
		/// the display of the items produced by running fetcher, each displayed using displayOneT.
		/// The hook/unhookEventActions are passed to the hookup, where they are invoked with appropriate arguments
		/// and should set up and tear down the connection that allows the hookup to receive notifications when
		/// the sequence computed by fetcher is going to change.
		/// </summary>
		private ViewBuilder AddObjSeq<T>(object target, Func<IEnumerable<T>> fetcher, Action<IReceivePropChanged> hookEventAction,
			Action<IReceivePropChanged> unhookEventAction, Action<ViewBuilder, T> displayOneT, int tag)
		{
			var hookup = new IndependentSequenceHookup<T>(target, m_destination, fetcher, hookEventAction, unhookEventAction,
				displayOneT);
			CurrentHookup = hookup;
			hookup.Tag = tag;
			var currentTs = fetcher();
			// Todo: we want to build more hookup structure around this.
			foreach (var item in currentTs)
				hookup.BuildAnItemDisplay(this, item, hookup.Children.Count);
			return this;
		}

		private ViewBuilder AddObj<T>(object target, Func<T> fetcher, Action<IReceivePropChanged> hookEventAction,
			Action<IReceivePropChanged> unhookEventAction, Action<ViewBuilder, T> displayOneT, int tag)
		{
			var hookup = new ObjHookup<T>(target, m_destination, fetcher, hookEventAction, unhookEventAction,
				displayOneT);
			CurrentHookup = hookup;
			hookup.Tag = tag;
			var currentTs = fetcher();
			// Todo: we want to build more hookup structure around this.
			hookup.BuildAnItemDisplay(this, fetcher(), hookup.Children.Count);
			return this;
		}

		/// <summary>
		/// This produces effectively the same display and automatic updating as AddObjSeq, but instead of immediately
		/// building the display, it creates a LazyBox and LazyHookup which between them manage the process of
		/// building the item displays only when they are needed.
		/// </summary>
		private ViewBuilder AddLazyObjSeq<T>(object target, Func<IEnumerable<T>> fetcher, Action<IReceivePropChanged> hookEventAction,
			Action<IReceivePropChanged> unhookEventAction, Action<ViewBuilder, T> displayOneT, int tag) where T: class
		{
			var hookup = new LazyHookup<T>(target, m_destination, fetcher, hookEventAction, unhookEventAction,
				displayOneT);
			CurrentHookup = hookup;
			hookup.Tag = tag;
			var currentTs = fetcher();
			if (currentTs.FirstOrDefault()== null)
				return this; // no need to insert anything, currently no items.
			InsertBox(new LazyBox<T>(NestedBoxStyles, hookup, fetcher()));
			return this;
		}

		/// <summary>
		/// Add a Blockbox of the specified color and size to the view.
		/// </summary>
		public ViewBuilder AddBlock(System.Drawing.Color color, int mpWidth, int mpHeight)
		{
			InsertBox(new BlockBox(NestedBoxStyles, color, mpWidth, mpHeight));
			return this;
		}
	}
}
