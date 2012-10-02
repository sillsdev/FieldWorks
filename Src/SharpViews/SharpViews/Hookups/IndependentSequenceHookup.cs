using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// An IndepenentSequenceHookup is built when the displays of individual items in the sequence
	/// are independent of one another: for example, the last item in the sequence is not displayed
	/// differently from any other just because of its position. Also, there is no additional material:
	/// the display of the entire sequence is a concatenation of the displays of the items.
	/// </summary>
	public class IndependentSequenceHookup<T> : SequenceHookup<T>, IHaveParagagraphOperations
	{
		Action<ViewBuilder, T> DisplayOneT { get; set; }
		private IParagraphOperations<T> m_paragraphOps;
		internal IParagraphOperations<T> ParagraphOperations
		{
			get { return m_paragraphOps; }

			set
			{
				m_paragraphOps = value;
				m_paragraphOps.Hookup = this;
			}
		}
		public IndependentSequenceHookup(object target, GroupBox containingBox, Func<IEnumerable<T>> fetcher,
			Action<IReceivePropChanged> hookEvent, Action<IReceivePropChanged> unhookEvent,
			Action<ViewBuilder, T> displayOneT)
			: base(target, containingBox, fetcher, hookEvent, unhookEvent)
		{
			DisplayOneT = displayOneT;
		}

		public override void PropChanged(object sender, EventArgs args)
		{
			var builder = ContainingBox.Builder;
			builder.CurrentHookup = this;
			var newTs = Fetcher().ToList();
			var currentTs = (from hookup in Children select (T) hookup.Target).ToList();
			int firstDiff = 0;
			int lim = Math.Min(newTs.Count, currentTs.Count);
			while (firstDiff < lim && newTs[firstDiff].Equals(currentTs[firstDiff]))
				firstDiff++;
			int limNew = newTs.Count;
			int limCurrent = currentTs.Count;
			while (limNew > 0 && limCurrent > 0 && newTs[limNew - 1].Equals(currentTs[limCurrent - 1]))
			{
				limNew--;
				limCurrent--;
			}
			// Items 0 to firstDiff are the same.
			// Items limCurrent to end of current equal items limNew to end of new.
			// Items firstDiff to limCurrent need to be deleted.
			if (limCurrent > firstDiff)
			{
				if (ContainingBox is ParaBox)
				{
					// remove runs. Enhance: allow for possibly empty items. Allow boxes nested in para.
					var firstDelChild = (LiteralStringParaHookup)((ItemHookup) Children[firstDiff]).Children.First();
					var lastDelChild = (LiteralStringParaHookup)((ItemHookup)Children[limCurrent - 1]).Children.Last();
					((ParaBox) ContainingBox).RemoveRuns(firstDelChild.ClientRunIndex,
						lastDelChild.ClientRunIndex - firstDelChild.ClientRunIndex + 1);
				}
				else
				{
					// remove child boxes. Enhance: allow for possibly empty items.
					var firstGoner = ((ItemHookup)Children[firstDiff]).FirstBox;
					var lastGoner = ((ItemHookup)Children[limCurrent - 1]).LastBox;
					ContainingBox.RemoveBoxes(firstGoner, lastGoner);
				}
				for (int i = firstDiff; i < limCurrent; i++)
				{
					var disposeChild = Children[i] as IDisposable;
					if (disposeChild != null)
						disposeChild.Dispose();
				}
				Children.RemoveRange(firstDiff, limCurrent - firstDiff);
			}
			// Items firstDiff to limNew are new and must be inserted.
			for (int i = firstDiff; i < limNew; i++)
				BuildAnItemDisplay(builder, newTs[i], i);
			using (var gh = ContainingBox.Root.Site.DrawingInfo)
			{
				ContainingBox.RelayoutWithParents(gh);
			}
		}

		internal void BuildAnItemDisplay(ViewBuilder builder, T item, int insertAt)
		{
			builder.PushHookup(new ItemHookup(item, ContainingBox), insertAt);
			DisplayOneT(builder, item);
			builder.PopHookup();
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
