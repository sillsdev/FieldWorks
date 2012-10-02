using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	public class ObjHookup<T> : GroupHookup, IReceivePropChanged
	{
		/// <summary>
		/// The function that returns the current object to display.
		/// </summary>
		internal Func<T> Fetcher { get; private set; }
		Action<ViewBuilder, T> DisplayOneT { get; set; }
		private IObjParagraphOperations<T> m_paragraphOps;
		private Action<IReceivePropChanged> RemoveHook { get; set; }
		internal IObjParagraphOperations<T> ParagraphOperations
		{
			get { return m_paragraphOps; }

			set
			{
				m_paragraphOps = value;
				m_paragraphOps.Hookup = this;
			}
		}
		public ObjHookup(object target, GroupBox containingBox, Func<T> fetcher,
			Action<IReceivePropChanged> hookEvent, Action<IReceivePropChanged> unhookEvent,
			Action<ViewBuilder, T> displayOneT)
			: base(target, containingBox)
		{
			Fetcher = fetcher;
			if (hookEvent != null)
				hookEvent(this);
			RemoveHook = unhookEvent;
			DisplayOneT = displayOneT;
		}

		/// <summary>
		/// Sent when the contents of the property we are monitoring changes.
		/// </summary>
		public override void PropChanged(object sender, EventArgs args)
		{
			var builder = ContainingBox.Builder;
			builder.CurrentHookup = this;
			var newT = Fetcher();
			var currentT = (T)Children[0].Target;
			if (ContainingBox is ParaBox)
			{
				// remove runs. Enhance: allow for possibly empty items. Allow boxes nested in para.
				var firstDelChild = (LiteralStringParaHookup)((ItemHookup)Children[0]).Children.First();
				((ParaBox)ContainingBox).RemoveRuns(firstDelChild.ClientRunIndex, 1);
			}
			else
			{
				// remove child boxes. Enhance: allow for possibly empty items.
				var firstGoner = ((ItemHookup)Children[0]).FirstBox;
				var lastGoner = ((ItemHookup)Children[0]).LastBox;
				if(firstGoner != null || lastGoner != null)
					ContainingBox.RemoveBoxes(firstGoner, lastGoner);
			}
			var disposeChild = Children[0] as IDisposable;
			if (disposeChild != null)
				disposeChild.Dispose();
			Children.RemoveRange(0, 1);
			// Items firstDiff to limNew are new and must be inserted.
			BuildAnItemDisplay(builder, newT, 0);
			using (var gh = ContainingBox.Root.Site.DrawingInfo)
			{
				ContainingBox.RelayoutWithParents(gh);
			}
		}

		internal void BuildAnItemDisplay(ViewBuilder builder, T item, int insertAt)
		{
			builder.PushHookup(new ItemHookup(item, ContainingBox), insertAt);
			if (item != null)
			{
				DisplayOneT(builder, item);
			}
			builder.PopHookup();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (RemoveHook != null)
					RemoveHook(this);
			}
		}

		/// <summary>
		/// Fulfils an interface requirement by providing non-template access to the paragraph operations object.
		/// This allows a selection to obtain it without knowing the object class of the hookup.
		/// </summary>
		/// <returns></returns>
		public IParagraphOperations GetParagraphOperations()
		{
			return ParagraphOperations;
		}
	}
}
