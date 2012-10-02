// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: FwDrawing.cs
// Responsibility: DavidO
// Last reviewed:
//
// Implementation of FwDrawing
//
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.ComponentModel;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Drawing
{
	//*******************************************************************************************
	/// <summary>
	///
	/// </summary>
	//*******************************************************************************************
	public enum BorderTypes
	{
		/// <summary>A single border where all four sides are the same color.</summary>
		Single,
		/// <summary>A single border gives a 3D raised appearance.</summary>
		SingleRaised,
		/// <summary>A single border gives a 3D sunken appearance.</summary>
		SingleSunken,
		/// <summary>A raised border that gives a more prominant 3D raised look
		/// than the SingleRaised type.</summary>
		DoubleRaised,
		/// <summary>A sunken border that gives a deeper 3D look than the
		/// SingleSunken type.</summary>
		DoubleSunken
	};

	//*******************************************************************************************
	/// <summary>
	///
	/// </summary>
	//*******************************************************************************************
	public enum LineTypes
	{
		/// <summary></summary>
		Etched,
		/// <summary></summary>
		Raised,
		/// <summary></summary>
		Solid
	};

	// We have to specify FwButton for our bitmap, because we are in a different namespace.
	// See http://www.syncfusion.com/faq/winforms/search/607.asp
	/// <summary>
	///
	/// </summary>
	[ToolboxBitmap(typeof(SIL.FieldWorks.Common.Controls.FwButton), "resources.BorderDrawing.bmp")]
	public class BorderDrawing : Component, IFWDisposable
	{
		//*******************************************************************************************
		//*******************************************************************************************
		/// <summary></summary>
		protected Graphics m_graphics;
		/// <summary></summary>
		protected Color m_clrSingleBorder = SystemColors.ControlDark;
		/// <summary></summary>
		protected Rectangle m_rect;
		/// <summary></summary>
		protected BorderTypes m_brdrType = BorderTypes.Single;
		/// <summary></summary>
		protected Pen m_penLightestEdge = new Pen(SystemColors.ControlLightLight, 1);
		/// <summary></summary>
		protected Pen m_penLightEdge = new Pen(SystemColors.ControlLight, 1);
		/// <summary></summary>
		protected Pen m_penDarkestEdge = new Pen(SystemColors.ControlDarkDark, 1);
		/// <summary></summary>
		protected Pen m_penDarkEdge = new Pen(SystemColors.ControlDark, 1);

		#region Constructors
		//*******************************************************************************************
		/// <summary>
		///  Constructor 1
		/// </summary>
		//*******************************************************************************************
		public BorderDrawing()
		{
			m_graphics = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  Constructor 2
		/// </summary>
		/// <param name="g"></param>
		/// ------------------------------------------------------------------------------------
		public BorderDrawing(Graphics g)
		{
			m_graphics = g;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  Constructor 3
		/// </summary>
		/// <param name="g"></param>
		/// <param name="rect"></param>
		/// ------------------------------------------------------------------------------------
		public BorderDrawing(Graphics g, System.Drawing.Rectangle rect)
		{
			m_graphics = g;
			m_rect = rect;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  Constructor 4
		/// </summary>
		/// <param name="g"></param>
		/// <param name="rect"></param>
		/// <param name="brdrType"></param>
		/// ------------------------------------------------------------------------------------
		public BorderDrawing(Graphics g, System.Drawing.Rectangle rect, BorderTypes brdrType)
		{
			m_graphics = g;
			m_rect = rect;
			m_brdrType = brdrType;
			Draw();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///  Constructor 5
		/// </summary>
		/// <param name="g"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="nWidth"></param>
		/// <param name="nHeight"></param>
		/// <param name="brdrType"></param>
		/// ------------------------------------------------------------------------------------
		public BorderDrawing(Graphics g, int x, int y, int nWidth, int nHeight, BorderTypes brdrType)
		{
			m_graphics = g;
			m_rect = new System.Drawing.Rectangle(x, y, nWidth, nHeight);
			m_brdrType = brdrType;
			Draw();
		}

		//*******************************************************************************************
		/// <summary>
		///  Constructor 6
		/// </summary>
		/// <param name="container"></param>
		//*******************************************************************************************
		public BorderDrawing(System.ComponentModel.IContainer container)
		{
			// Required for Windows.Forms Class Composition Designer support
			container.Add(this);
		}
		#endregion

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		///
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		private bool m_isDisposed = false;

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
			}

			base.Dispose(disposing);

			m_graphics = null;
			m_penLightestEdge = null;
			m_penLightEdge = null;
			m_penDarkestEdge = null;
			m_penDarkEdge = null;

			m_isDisposed = true;
		}

		//*******************************************************************************************
		/// <summary>
		///  This is an alternate way to set the edge colors.
		/// </summary>
		/// <param name="clrLightest"></param>
		/// <param name="clrLight"></param>
		/// <param name="clrDark"></param>
		/// <param name="clrDarkest"></param>
		//*******************************************************************************************
		public void EdgeColors(Color clrLightest, Color clrLight, Color clrDark, Color clrDarkest)
		{
			CheckDisposed();

			BorderLightestColor = clrLightest;
			BorderLightColor = clrLight;
			BorderDarkColor = clrDark;
			BorderDarkestColor = clrDarkest;
		}

		//*******************************************************************************************
		/// <summary>
		/// This is an alternate way to set the edge colors to be used if border type is single
		/// sunken or raised.
		/// </summary>
		/// <param name="clrLight"></param>
		/// <param name="clrDark"></param>
		//*******************************************************************************************
		public void EdgeColors(Color clrLight, Color clrDark)
		{
			CheckDisposed();

			BorderLightestColor = clrLight;
			BorderDarkColor = clrDark;
		}

		#region Properties
		//*******************************************************************************************
		/// <summary>
		/// Gets or sets the graphics object.
		/// </summary>
		//*******************************************************************************************
		[Browsable(false)]
		public Graphics Graphics
		{
			get
			{
				CheckDisposed();

				return m_graphics;
			}
			set
			{
				CheckDisposed();

				m_graphics = value;
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Gets or sets the color used to draw border style Single.
		/// </summary>
		//*******************************************************************************************
		[Description("Determines the color used to draw border style Single.")]
		public Color SingleBorderColor
		{
			get
			{
				CheckDisposed();

				return m_clrSingleBorder;
			}
			set
			{
				CheckDisposed();

				m_clrSingleBorder = value;
			}
		}

		//*******************************************************************************************
		/// <summary>
		///  Allow setting individual edge colors for raised and sunken borders.
		/// </summary>
		//*******************************************************************************************
		public Color BorderLightestColor
		{
			get
			{
				CheckDisposed();

				return m_penLightestEdge.Color;
			}
			set
			{
				CheckDisposed();

				m_penLightestEdge.Color = value;
			}
		}

		/// <summary>
		///  Allow setting individual edge colors for raised and sunken borders.
		/// </summary>
		public Color BorderLightColor
		{
			get
			{
				CheckDisposed();

				return m_penLightEdge.Color;
			}
			set
			{
				CheckDisposed();

				m_penLightEdge.Color = value;
			}
		}

		/// <summary>
		///  Allow setting individual edge colors for raised and sunken borders.
		/// </summary>
		public Color BorderDarkColor
		{
			get
			{
				CheckDisposed();

				return m_penDarkEdge.Color;
			}
			set
			{
				CheckDisposed();

				m_penDarkEdge.Color = value;
			}
		}

		/// <summary>
		///  Allow setting individual edge colors for raised and sunken borders.
		/// </summary>
		public Color BorderDarkestColor
		{
			get
			{
				CheckDisposed();

				return m_penDarkestEdge.Color;
			}
			set
			{
				CheckDisposed();

				m_penDarkestEdge.Color = value;
			}
		}
		#endregion

		#region ShouldSerialize methods
		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeBorderDarkColor()
		{
			return BorderDarkColor != SystemColors.ControlDark;
		}

		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeBorderDarkestColor()
		{
			return BorderDarkestColor != SystemColors.ControlDarkDark;
		}
		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeBorderLightestColor()
		{
			return BorderLightestColor != SystemColors.ControlLightLight;
		}
		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeBorderLightColor()
		{
			return BorderLightColor != SystemColors.ControlLight;
		}

		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeSingleBorderColor()
		{
			return SingleBorderColor != SystemColors.ControlDark;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="g"></param>
		/// <param name="rect"></param>
		/// <param name="brdrType"></param>
		/// ------------------------------------------------------------------------------------
		public void Draw(Graphics g, System.Drawing.Rectangle rect, BorderTypes brdrType)
		{
			CheckDisposed();

			m_graphics = g;
			m_rect = rect;
			m_brdrType = brdrType;
			Draw();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Draw()
		{
			CheckDisposed();

			//***************************************************************************************
			// Can't draw without a graphics object.
			//***************************************************************************************
			if (m_graphics == null)
			{
				throw (new ArgumentNullException());
			}

			m_rect.Width--;
			m_rect.Height--;

			//***************************************************************************************
			// Single border
			//***************************************************************************************
			if (m_brdrType == BorderTypes.Single)
			{
				using (Pen penDarkEdge = new Pen(m_clrSingleBorder, 1))
				{
					m_graphics.DrawRectangle(penDarkEdge, m_rect);
					return;
				}
			}

			//***************************************************************************************
			// Single border, raised or sunken
			//***************************************************************************************
			if (m_brdrType == BorderTypes.SingleRaised || m_brdrType == BorderTypes.SingleSunken)
			{
				//***********************************************************************************
				// First, draw the dark border all around the rectangle.
				//***********************************************************************************
				m_graphics.DrawRectangle(m_penDarkEdge, m_rect);

				//***********************************************************************************
				// Then draw the two light edges where they should appear.
				//***********************************************************************************
				if (m_brdrType == BorderTypes.SingleRaised)
				{
					// Note: left and top border line are one pixel shorter!
					m_graphics.DrawLine(m_penLightestEdge, m_rect.Left, m_rect.Top, m_rect.Left, m_rect.Bottom-1);
					m_graphics.DrawLine(m_penLightestEdge, m_rect.Left, m_rect.Top, m_rect.Right-1, m_rect.Top);
				}
				else
				{
					m_graphics.DrawLine(m_penLightestEdge, m_rect.Right, m_rect.Top, m_rect.Right, m_rect.Bottom);
					m_graphics.DrawLine(m_penLightestEdge, m_rect.Left, m_rect.Bottom, m_rect.Right, m_rect.Bottom);
				}
			}

				//***************************************************************************************
				// Double border raised or sunken
				//***************************************************************************************
			else
			{
				//***********************************************************************************
				// Draw the dark and darkest border all around the rectangle. One inside the other.
				//***********************************************************************************
				m_graphics.DrawRectangle(m_penDarkestEdge, m_rect);

				m_graphics.DrawRectangle(m_penDarkEdge, m_rect.X + 1, m_rect.Y + 1,
					m_rect.Width - 2, m_rect.Height - 2);

				//***********************************************************************************
				//***********************************************************************************
				if (m_brdrType == BorderTypes.DoubleRaised)
				{
					// Note: left and top border line are one pixel shorter!
					m_graphics.DrawLine(m_penLightEdge, m_rect.Left, m_rect.Top, m_rect.Left, m_rect.Bottom-1);
					m_graphics.DrawLine(m_penLightEdge, m_rect.Left, m_rect.Top, m_rect.Right-1, m_rect.Top);
					m_graphics.DrawLine(m_penLightestEdge, m_rect.Left+1, m_rect.Top+1, m_rect.Left+1, m_rect.Bottom-2);
					m_graphics.DrawLine(m_penLightestEdge, m_rect.Left+1, m_rect.Top+1, m_rect.Right-2, m_rect.Top+1);
				}
				else
				{
					// DoubleSunken
					m_graphics.DrawLine(m_penLightEdge, m_rect.Right, m_rect.Top, m_rect.Right, m_rect.Bottom);
					m_graphics.DrawLine(m_penLightEdge, m_rect.Left, m_rect.Bottom, m_rect.Right, m_rect.Bottom);
					m_graphics.DrawLine(m_penLightestEdge, m_rect.Right-1, m_rect.Top+1, m_rect.Right-1, m_rect.Bottom-1);
					m_graphics.DrawLine(m_penLightestEdge, m_rect.Left+1, m_rect.Bottom-1, m_rect.Right-1, m_rect.Bottom-1);
				}
			}
		} // Draw
	} // Class BorderDrawing

	/// <summary>
	/// Summary was empty, but a quick review of the code causes some serious concern about the use
	/// of statics in this class.  Especially scary is the Graphics object being static and passed
	/// in during the construction of objects, as well as overwriting the previous value for all
	/// calls.  IOW - Each LineDrawing object that is created will overwrite all existing
	/// LineDrawing objects that could have been created with different values.
	/// (That surely wasn't intended .. how could it have been?)  This doesn't seem to be
	/// crashing, but it needs a refactor.  To much else going on to do it now. Dec 06 - DanH.
	/// </summary>
	[ToolboxBitmap(typeof(SIL.FieldWorks.Common.Controls.FwButton), "resources.BorderDrawing.bmp")]
	public class LineDrawing : Component, IFWDisposable
	{
		//*******************************************************************************************
		//*******************************************************************************************
		/// <summary></summary>
		static protected Graphics g_graphics;
		/// <summary></summary>
		static protected LineTypes g_LineType = LineTypes.Etched;
		/// <summary></summary>
		static protected Pen g_penSolidLine = new Pen(SystemColors.ControlText, 1);
		/// <summary></summary>
		static protected Pen g_penLightLine = new Pen(SystemColors.ControlLightLight, 1);
		/// <summary></summary>
		static protected Pen g_penDarkLine = new Pen(SystemColors.ControlDark, 1);
		/// <summary></summary>
		static protected Point g_StartLocation = new Point(0,0);
		/// <summary></summary>
		static protected Point g_EndLocation = new Point(0,0);
		/// <summary></summary>
		static protected int g_dypThickness = 1;

		#region Constructors
		//*******************************************************************************************
		/// <summary>
		/// Constructor 1
		/// </summary>
		//*******************************************************************************************
		public LineDrawing()
		{
			g_graphics = null;
		}

		//*******************************************************************************************
		/// <summary>
		/// Constructor 2
		/// </summary>
		//*******************************************************************************************
		public LineDrawing(Graphics g)
		{
			g_graphics = g;
		}

		//*******************************************************************************************
		/// <summary>
		/// Constructor 3
		/// </summary>
		//*******************************************************************************************
		public LineDrawing(Graphics g, LineTypes lineType)
		{
			g_graphics = g;
			g_LineType = lineType;
		}

		//*******************************************************************************************
		/// <summary>
		/// Constructor 4
		/// </summary>
		//*******************************************************************************************
		public LineDrawing(Graphics g, int x, int y, int dxpLength)
		{
			Draw(g, x, y, dxpLength);
		}

		//*******************************************************************************************
		/// <summary>
		/// Constructor 4
		/// </summary>
		//*******************************************************************************************
		public LineDrawing(Graphics g, int x, int y, int dxpLength, LineTypes lineType)
		{
			Draw(g, x, y, dxpLength, lineType);
		}

		//*******************************************************************************************
		/// <summary>
		/// Constructor 5
		/// </summary>
		//*******************************************************************************************
		public LineDrawing(Graphics g, int x1, int y1, int x2, int y2)
		{
			Draw(g, x1, y1, x2, y2);
		}

		//*******************************************************************************************
		/// <summary>
		/// Constructor 6
		/// </summary>
		//*******************************************************************************************
		public LineDrawing(Graphics g, int x1, int y1, int x2, int y2, LineTypes lineType)
		{
			Draw(g, x1, y1, x2, y2, lineType);
		}

		//*******************************************************************************************
		/// <summary>
		/// Constructor 7
		/// Required for Windows.Forms Class Composition Designer support
		/// </summary>
		//*******************************************************************************************
		public LineDrawing(System.ComponentModel.IContainer container)
		{
			container.Add(this);
		}
		#endregion

		/// <summary>
		///
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		private bool m_isDisposed = false;

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
			}

			base.Dispose(disposing);
			m_isDisposed = true;
		}

		#region Properties
		//*******************************************************************************************
		/// <summary>
		/// Set the graphics object.
		/// </summary>
		//*******************************************************************************************
		[Browsable(false)]
		public Graphics Graphics
		{
			get
			{
				CheckDisposed();

				return g_graphics;
			}
			set
			{
				CheckDisposed();

				g_graphics = value;
			}
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		[Description("Determines the type of line (e.g. etched, solid, etc.)")]
		public LineTypes LineType
		{
			get
			{
				CheckDisposed();

				return g_LineType;
			}
			set
			{
				CheckDisposed();

				g_LineType = value;
			}
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		[Description("Determines the thickness of lines of type Solid.")]
		public int SolidLineThickness
		{
			get
			{
				CheckDisposed();

				return g_dypThickness;
			}
			set
			{
				CheckDisposed();

				if (g_dypThickness != value)
				{
					g_dypThickness = value;
					g_penSolidLine = new Pen(g_penSolidLine.Color, value);
				}
			}
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		[Description("Determines the color used to draw lines of type Solid.")]
		public Color SolidLineColor
		{
			get
			{
				CheckDisposed();

				return g_penSolidLine.Color;
			}
			set
			{
				CheckDisposed();

				g_penSolidLine = new Pen(value, this.SolidLineThickness);
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Gets or Sets etched and raised line's light color.
		/// </summary>
		//*******************************************************************************************
		[Description("Determines the light color for lines of type Etched or Raised.")]
		public Color LightColor
		{
			get
			{
				CheckDisposed();

				return g_penLightLine.Color;
			}
			set
			{
				CheckDisposed();

				g_penLightLine = new Pen(value, 1);
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Gets or Sets etched and raised line's dark color.
		/// </summary>
		//*******************************************************************************************
		[Description("Determines the dark color for lines of type Etched or Raised.")]
		public Color DarkColor
		{
			get
			{
				CheckDisposed();

				return g_penDarkLine.Color;
			}
			set
			{
				CheckDisposed();

				g_penDarkLine = new Pen(value, 1);
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Gets or Sets a line's beginning point.
		/// </summary>
		//*******************************************************************************************
		[Description("Determines the start location for the line.")]
		public Point StartLocation
		{
			get
			{
				CheckDisposed();

				return g_StartLocation;
			}
			set
			{
				CheckDisposed();

				g_StartLocation = value;
			}
		}

		//*******************************************************************************************
		/// <summary>
		/// Gets or Sets a line's end point.
		/// </summary>
		//*******************************************************************************************
		[Description("Determines the end location for the line.")]
		public Point EndLocation
		{
			get
			{
				CheckDisposed();

				return g_EndLocation;
			}
			set
			{
				CheckDisposed();

				g_EndLocation = value;
			}
		}

		#endregion

		#region ShouldSerialize methods
		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeLineType()
		{
			return (g_LineType != LineTypes.Etched);
		}

		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeDarkColor()
		{
			return (g_LineType != LineTypes.Solid &&
					g_penDarkLine.Color != SystemColors.ControlDark);
		}

		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeLightColor()
		{
			return (g_LineType != LineTypes.Solid &&
				g_penLightLine.Color != SystemColors.ControlLight);
		}

		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeSolidLineColor()
		{
			return (g_LineType == LineTypes.Solid &&
					g_penSolidLine.Color != SystemColors.ControlText);
		}

		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeSolidLineThickness()
		{
			return (g_LineType == LineTypes.Solid && g_dypThickness != 1);
		}

		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeStartLocation()
		{
			return (g_StartLocation.X != 0 || g_StartLocation.Y != 0);
		}

		//*******************************************************************************************
		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		//*******************************************************************************************
		protected bool ShouldSerializeEndLocation()
		{
			return (g_EndLocation.X != 0 || g_EndLocation.Y != 0);
		}

		#endregion

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		public void Draw(int x, int y, int dxpLength)
		{
			CheckDisposed();

			g_StartLocation = new Point(x, y);
			g_EndLocation = new Point(x + dxpLength, y);
			Draw();
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		static public void Draw(Graphics g, int x, int y, int dxpLength)
		{
			g_graphics = g;
			g_StartLocation = new Point(x, y);
			g_EndLocation = new Point(x + dxpLength, y);
			Draw();
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		static public void Draw(Graphics g, int x, int y, int dxpLength, LineTypes lineType)
		{
			g_LineType = lineType;
			Draw(g, x, y, dxpLength);
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		static public void Draw(int x, int y, int dxpLength, LineTypes lineType)
		{
			g_LineType = lineType;
			Draw(g_graphics, x, y, dxpLength);
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		public void Draw(int x1, int y1, int x2, int y2)
		{
			CheckDisposed();

			g_StartLocation = new Point(x1, y1);
			g_EndLocation = new Point(x2, y2);
			Draw();
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		static public void Draw(Graphics g, int x1, int y1, int x2, int y2)
		{
			g_graphics = g;
			g_StartLocation = new Point(x1, y1);
			g_EndLocation = new Point(x2, y2);
			Draw();
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		public void Draw(int x1, int y1, int x2, int y2, Color solidLineColor)
		{
			CheckDisposed();

			g_StartLocation = new Point(x1, y1);
			g_EndLocation = new Point(x2, y2);
			g_penSolidLine.Color = solidLineColor;
			g_LineType = LineTypes.Solid;
			Draw();
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		static public void Draw(Graphics g, int x1, int y1, int x2, int y2, Color solidLineColor)
		{
			g_graphics = g;
			g_StartLocation = new Point(x1, y1);
			g_EndLocation = new Point(x2, y2);
			g_penSolidLine.Color = solidLineColor;
			g_LineType = LineTypes.Solid;
			Draw();
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		public void Draw(int x1, int y1, int x2, int y2, LineTypes lineType)
		{
			CheckDisposed();

			g_LineType = lineType;
			Draw(x1, y1, x2, y2);
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		static public void Draw(Graphics g, int x1, int y1, int x2, int y2, LineTypes lineType)
		{
			g_LineType = lineType;
			Draw(g, x1, y1, x2, y2);
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		//*******************************************************************************************
		static public void Draw()
		{
			//***************************************************************************************
			// Can't draw without a graphics object.
			//***************************************************************************************
			if (g_graphics == null)
			{
				throw(new ArgumentNullException());
			}

			switch (g_LineType)
			{
				case LineTypes.Etched:
				case LineTypes.Raised:
					g_graphics.DrawLine((g_LineType == LineTypes.Etched ?
						g_penDarkLine : g_penLightLine),
						g_StartLocation, g_EndLocation);

					Point tmpStart = new Point(g_StartLocation.X, g_StartLocation.Y + 1);
					Point tmpEnd = new Point(g_EndLocation.X, g_EndLocation.Y + 1);

					g_graphics.DrawLine((g_LineType == LineTypes.Etched ?
						g_penLightLine : g_penDarkLine), tmpStart, tmpEnd);
					break;

				case LineTypes.Solid:
					g_graphics.DrawLine(g_penSolidLine, g_StartLocation, g_EndLocation);
					break;
			}
		} // Draw

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws an etched, horizontal line above controls (usually buttons) on a dialog box.
		/// The etched line will be drawn the same distance above the specified control as the
		/// control is from the bottom of the dialog.
		/// </summary>
		/// <param name="g">Graphics object in which to draw</param>
		/// <param name="rcDlgClient">The client rectangle of the dialog box.</param>
		/// <param name="rcControl">The rectangle of a control on the dialog to use as a
		/// reference point for drawing the etched, separator line.</param>
		/// ------------------------------------------------------------------------------------
		static public void DrawDialogControlSeparator(Graphics g, Rectangle rcDlgClient,
			Rectangle rcControl)
		{
			Draw(g, 8, rcControl.Top - (rcDlgClient.Height - rcControl.Bottom),
				rcDlgClient.Width - 17,	LineTypes.Etched);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws an etched, horizontal line above controls (usually buttons) on a dialog box.
		/// The etched line will be drawn the same distance above the specified control as the
		/// control is from the bottom of the dialog.
		/// </summary>
		/// <param name="g">Graphics object in which to draw</param>
		/// <param name="rcDlgClient">The client rectangle of the dialog box.</param>
		/// <param name="y">The vertical position where the etched line is to be drawn.</param>
		/// ------------------------------------------------------------------------------------
		static public void DrawDialogControlSeparator(Graphics g, Rectangle rcDlgClient, int y)
		{
			Draw(g, 8, y, rcDlgClient.Width - 17, LineTypes.Etched);
		}

	} // Class LineDrawing


	//*******************************************************************************************
	/// <summary>
	/// Converts content alignment to various other types of alignment.
	/// </summary>
	//*******************************************************************************************
	public class ContentAlignmentHelper
	{
		//******************************************************************************
		/// <summary>
		/// Determine horizontal alignment for text based on a content alignment value.
		/// </summary>
		/// <param name="align"></param>
		/// <returns></returns>
		//******************************************************************************
		public static StringAlignment ConAlignToHorizStrAlign(ContentAlignment align)
		{
			switch(align)
			{
				case ContentAlignment.BottomRight:
				case ContentAlignment.MiddleRight:
				case ContentAlignment.TopRight:
					return StringAlignment.Far;

				case ContentAlignment.TopLeft:
				case ContentAlignment.MiddleLeft:
				case ContentAlignment.BottomLeft:
					return StringAlignment.Near;

				default:
					return StringAlignment.Center;
			}
		}

		//******************************************************************************
		/// <summary>
		/// Determine vertical alignment for text based on a content alignment value.
		/// </summary>
		/// <param name="align"></param>
		/// <returns></returns>
		//******************************************************************************
		public static StringAlignment ConAlignToVertStrAlign(ContentAlignment align)
		{
			switch(align)
			{
				case ContentAlignment.TopLeft:
				case ContentAlignment.TopCenter:
				case ContentAlignment.TopRight:
					return StringAlignment.Near;

				case ContentAlignment.BottomLeft:
				case ContentAlignment.BottomCenter:
				case ContentAlignment.BottomRight:
					return StringAlignment.Far;

				default:
					return StringAlignment.Center;
			}
		}

		//*******************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="align"></param>
		/// <param name="img"></param>
		/// <param name="rc"></param>
		/// <returns></returns>
		//*******************************************************************************************
		public static Point ConAlignToImgPosition(ContentAlignment align, Image img, Rectangle rc)
		{
			return ConAlignToImgPosition(align, img, rc, 0);
		}

		//*******************************************************************************************
		/// <summary>
		/// This function determines where in a rectangle an image should be drawn given a
		/// content alignment specification.
		/// </summary>
		/// <param name="align">The content alignment type where the image should be drawn.</param>
		/// <param name="img">The image object to be drawn.</param>
		/// <param name="rc">The rectangle in which the image is to be drawn.</param>
		/// <param name="nMargin">The number of pixels of margin between the image and whatever
		/// edge of the rectangle it may be near. When either the X and Y components of the point
		/// are calculated to center the image, this parameter is ignored for that calculation.
		/// For example, if the content alignment is TopCenter, the calculation for X will
		/// ignore the margin but the calculation for Y will include adjusting for it.</param>
		/// <returns>A point type specifying where, relative to the rectangle, the image should
		/// be drawn. </returns>
		//*******************************************************************************************
		public static Point ConAlignToImgPosition(ContentAlignment align, Image img, Rectangle rc, int nMargin)
		{
			Point pt = new Point(rc.Left + nMargin, rc.Top + nMargin);

			//******************************************************************************
			// Determine the horizontal location for the image.
			//******************************************************************************
			if (align == ContentAlignment.BottomCenter || align == ContentAlignment.MiddleCenter ||
				align == ContentAlignment.TopCenter)
				pt.X = rc.Left + (rc.Width - img.Width) / 2;
			else if (align == ContentAlignment.BottomRight || align == ContentAlignment.MiddleRight ||
				align == ContentAlignment.TopRight)
				pt.X = rc.Left + (rc.Width - img.Width) - nMargin;

			//******************************************************************************
			// Determine the vertical location for the image.
			//******************************************************************************
			if (align == ContentAlignment.MiddleLeft || align == ContentAlignment.MiddleCenter ||
				align == ContentAlignment.MiddleRight)
				pt.Y = rc.Top + (rc.Height - img.Height) / 2;
			else if (align == ContentAlignment.BottomLeft || align == ContentAlignment.BottomCenter ||
				align == ContentAlignment.BottomRight)
				pt.Y = rc.Top + (rc.Height - img.Height) - nMargin;

			return pt;
		}

	} // Class ContentAlignmentConverter
} // Drawing Namespace
