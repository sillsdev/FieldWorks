using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// Class that gathers all the info needed by a box Layout operation that typically needs to be passed down (sometimes with
	/// minor alterations) to child boxes.
	/// </summary>
	public class LayoutInfo : LayoutTransform
	{
		/// <summary>
		/// This class represents the information needed to lay out part of the display.
		/// </summary>
		/// <param name="dx"></param>
		/// <param name="dy"></param>
		/// <param name="dpiX"></param>
		/// <param name="dpiY"></param>
		/// <param name="maxWidth">The maximum width available for layout (in units indicated by dpiX).</param>
		/// <param name="graphics"></param>
		/// <param name="rf"></param>
		public LayoutInfo(int dx, int dy, int dpiX, int dpiY, int maxWidth, IVwGraphics graphics, IRendererFactory rf) : base(dx, dy, dpiX, dpiY)
		{
			MaxWidth = maxWidth;
			VwGraphics = graphics;
			RendererFactory = rf;
		}

		public LayoutInfo(LayoutTransform source, int maxWidth, IVwGraphics graphics, IRendererFactory rf)
			: this(source.XOffset, source.YOffset, source.DpiX, source.DpiY, maxWidth, graphics, rf)
		{
		}

		/// <summary>
		/// The maximum width this box can occupy (in pixels, of size indicated by DpiX)
		/// </summary>
		public int MaxWidth { get; private set; }

		public IVwGraphics VwGraphics { get; private set; }

		internal IRendererFactory RendererFactory { get; private set; }

		/// <summary>
		/// Get a rendering engine for the specified ws and the current VwGraphics.
		/// </summary>
		public IRenderEngine GetRenderer(int ws)
		{
			return RendererFactory.GetRenderer(ws, VwGraphics);
		}

		///// <summary>
		///// Answer an otherwise identical LayoutTransform with the specified maximum layout width.
		///// </summary>
		///// <param name="maxWidth"></param>
		///// <returns></returns>
		//public LayoutInfo WithMaxWidth(int maxWidth)
		//{
		//    LayoutInfo result = (LayoutInfo)MemberwiseClone();
		//    result.MaxWidth = maxWidth;
		//    return result;
		//}

		/// <summary>
		/// Answer an otherwise identical LayoutTransform with the specified maximum layout width,
		/// adjusted appropriately for a child box of a box with Left = dx, Top = dy.
		/// Should be consistent with LayoutTransform.OffsetBy.
		/// </summary>
		public LayoutInfo WithMaxWidthOffsetBy(int maxWidth, int dx, int dy)
		{
			LayoutInfo result = (LayoutInfo)MemberwiseClone();
			result.MaxWidth = maxWidth;
			result.InitializeOnlyOffsetBy(dx, dy);
			return result;
		}
	}
}
