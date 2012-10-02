using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// This class handles some callbacks which a view may need to make during layout and similar operations,
	/// including expanding a lazy box. It stands as a proxy for the root site, and forwards all the messages
	/// to it when disposed. During its lifetime, the root box cannot be painted; an attempt to do so will add
	/// to the rectangles to be invalidated when the LCB is disposed.
	/// </summary>
	class LayoutCallbacks : IDisposable
	{
		private RootBox m_root;
		public LayoutCallbacks(RootBox root)
		{
			m_root = root;
			m_root.SuspendPaint(this);
		}

		~LayoutCallbacks()
		{
			throw new InvalidOperationException("LayoutCallbacks must be disposed; destructor should never be called");
		}
		public virtual void Dispose()
		{
			m_root.ResumePaint();
			foreach (var rect in RectsInvalidated)
				m_root.Site.Invalidate(rect);
			foreach (var rect in RectsInvalidatedInRoot)
				m_root.Site.InvalidateInRoot(rect);
			foreach (var args in ExpandArgs)
				m_root.RaiseLazyExpanded(args);
			GC.SuppressFinalize(this);
		}
		public List<Rectangle> RectsInvalidated = new List<Rectangle>();
		/// <summary>
		/// On Dispose, call invalidate on the root's site.
		/// </summary>
		public virtual void Invalidate(Rectangle rect)
		{
			RectsInvalidated.Add(rect);
		}
		public List<Rectangle> RectsInvalidatedInRoot = new List<Rectangle>();
		/// <summary>
		/// On Dispose, call invalidateInRoot on the root's site.
		/// </summary>
		public virtual void InvalidateInRoot(Rectangle rect)
		{
			RectsInvalidatedInRoot.Add(rect);
		}

		private List<RootBox.LazyExpandedEventArgs> ExpandArgs = new List<RootBox.LazyExpandedEventArgs>();
		/// <summary>
		/// On Dispose, raise the SizeChanged event on the root box.
		/// </summary>
		internal void RaiseLazyExpanded(int top, int bottom, int delta)
		{
			ExpandArgs.Add(new RootBox.LazyExpandedEventArgs() { EstimatedTop = top, EstimatedBottom = bottom, DeltaHeight = delta });
		}
	}

	/// <summary>
	/// Although implemented as a base class, this is conceptually a special case of LayoutCallbacks,
	/// when we are expanding a lazy box and don't want to do invalidates.
	/// </summary>
	class NoInvalidateLayoutCallbacks : LayoutCallbacks
	{
		private RootBox m_root;
		public NoInvalidateLayoutCallbacks(RootBox root) : base(root)
		{
		}

		~NoInvalidateLayoutCallbacks()
		{
			throw new InvalidOperationException("NoInvalidateLayoutCallbacks must be disposed; destructor should never be called");
		}

		/// <summary>
		/// Override to suppress Invalidates
		/// </summary>
		public override void Invalidate(Rectangle rect)
		{
		}
		/// <summary>
		/// Override to suppress Invalidates
		/// </summary>
		public override void InvalidateInRoot(Rectangle rect)
		{
		}

	}
}
