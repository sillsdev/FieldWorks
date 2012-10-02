using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews
{
	public interface IGraphicsHolder : IDisposable
	{
		IVwGraphics VwGraphics { get; }
		PaintTransform Transform { get; }
	}

	class GraphicsHolder : GraphicsManager, IGraphicsHolder
	{
		internal delegate PaintTransform TransformMaker(IVwGraphics vg);
		public GraphicsHolder(Control c, TransformMaker maker)
			: base(c)
		{
			Transform = maker(VwGraphics);
		}

		public PaintTransform Transform { get; private set; }
	}

	/// <summary>
	/// A class that manages the lifetime of an IVwGraphics. Intended usage is
	/// using(var gm = new GraphicsManager(this, g))
	/// {
	///		// do something with gm.VwGraphics
	/// }
	/// </summary>
	internal class GraphicsManager : IDisposable
	{
		private Graphics m_graphics;
		private bool m_shouldDisposeGraphics;
		private IVwGraphicsWin32 m_vwGraphics;
		private float m_dpiX;
		private float m_dpiY;

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~GraphicsManager()
		{
			Dispose(false);
		}

		void Dispose(bool properly)
		{
			// Can't do anything sensible if called from destructor; nothing to do if somehow called twice.
			if (!properly || m_graphics == null)
				return;
			IntPtr hdc = m_vwGraphics.GetDeviceContext();
			m_vwGraphics.ReleaseDC();
			if (hdc != IntPtr.Zero)
				m_graphics.ReleaseHdc(hdc);
			m_vwGraphics = null;
			if (m_shouldDisposeGraphics)
				m_graphics.Dispose();
			m_graphics = null;
		}

		#endregion

		/// <summary>
		/// This version will create (and dispose of properly when this is disposed) a Graphics using c.CreateGraphics().
		/// </summary>
		/// <param name="c"></param>
		public GraphicsManager(Control c) : this(c, null)
		{
		}

		/// <summary>
		/// Make one with the specified control and Graphics (typically passed in to a Paint method). The Graphics will not be disposed.
		/// </summary>
		public GraphicsManager(Control c, Graphics g)
		{
			m_graphics = g;
			if (g == null)
			{
				m_graphics = c.CreateGraphics();
				m_shouldDisposeGraphics = true;
			}
			m_vwGraphics = VwGraphicsWin32Class.Create();
			double zoom = 1.0; // eventually an argument, probably
			m_dpiX = m_graphics.DpiX;
			m_dpiY = m_graphics.DpiY;
			int dpix = (int)(m_dpiX * zoom);
			int dpiy = (int)(m_dpiY * zoom);
			IntPtr hdc = m_graphics.GetHdc();
			m_vwGraphics.Initialize(hdc);
			m_vwGraphics.XUnitsPerInch = dpix;
			m_vwGraphics.YUnitsPerInch = dpiy;
			m_vwGraphics.BackColor = (int)FwTextColor.kclrTransparent;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the VwGraphics object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwGraphics VwGraphics
		{
			get { return m_vwGraphics; }
		}

		/// <summary>
		/// The DpiX of the wrapped Graphics
		/// </summary>
		public float DpiX { get { return m_dpiX; } }
		/// <summary>
		/// Tehe DpiY ofr the wrapped Graphics.
		/// </summary>
		public float DpiY { get { return m_dpiY; } }
	}
}
