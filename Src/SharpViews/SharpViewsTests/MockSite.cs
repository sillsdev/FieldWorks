using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	class MockSite : ISharpViewSite
	{
		public PaintTransform m_transform;
		public IVwGraphics m_vwGraphics;
		public MockGraphicsHolder GraphicsHolder;
		public List<Rectangle> RectsInvalidated = new List<Rectangle>();
		public List<Rectangle> RectsInvalidatedInRoot = new List<Rectangle>();

		#region Implementation of ISharpViewSite

		/// <summary>
		/// Returns the information needed for figuring out where to draw (or invalidate) things.
		/// </summary>
		public IGraphicsHolder DrawingInfo
		{
			get
			{
				GraphicsHolder = new MockGraphicsHolder();
				GraphicsHolder.VwGraphics = m_vwGraphics;
				GraphicsHolder.Transform = m_transform;
				return GraphicsHolder;
			}
		}

		public void InvalidateInRoot(Rectangle rect)
		{
			RectsInvalidatedInRoot.Add(rect);
		}

		/// <summary>
		/// Invalidate (mark as needing to be painted) the specified rectangle.
		/// </summary>
		public void Invalidate(Rectangle rect)
		{
			RectsInvalidated.Add(rect);
		}

		public List<Action> PerformAfterNotificationsActions = new List<Action>();
		public void PerformAfterNotifications(Action task)
		{
			PerformAfterNotificationsActions.Add(task);
		}

		public class DoDragDropArgs
		{
			public object Data;
			public DragDropEffects AllowedEffects;
		}
		public DoDragDropArgs LastDoDragDropArgs { get; set; }
		public DragDropEffects NextDoDragDropResult { get; set; }

		public DragDropEffects DoDragDrop(object data, DragDropEffects allowedEffects)
		{
			LastDoDragDropArgs = new DoDragDropArgs() {Data = data, AllowedEffects = allowedEffects};
			return NextDoDragDropResult;
		}

		public void DoPendingAfterNotificationTasks()
		{
			foreach (var task in PerformAfterNotificationsActions)
				task();
			PerformAfterNotificationsActions.Clear();
		}

		#endregion
	}

	class MockGraphicsHolder : IGraphicsHolder
	{
		public bool WasDisposed { get; private set; }
		public void Dispose()
		{
			WasDisposed = true;
			GC.SuppressFinalize(this);
		}

		public IVwGraphics VwGraphics { get; set;}

		public PaintTransform Transform { get; set;}
	}
}
