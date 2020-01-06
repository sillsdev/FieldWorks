// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary was empty, but a quick review of the code causes some serious concern about the use
	/// of statics in this class.  Especially scary is the Graphics object being static and passed
	/// in during the construction of objects, as well as overwriting the previous value for all
	/// calls.  IOW - Each LineDrawing object that is created will overwrite all existing
	/// LineDrawing objects that could have been created with different values.
	/// (That surely wasn't intended .. how could it have been?)  This doesn't seem to be
	/// crashing, but it needs a refactor.  To much else going on to do it now. Dec 06 - DanH.
	/// </summary>
	[ToolboxBitmap(typeof(FwButton), "resources.BorderDrawing.bmp")]
	public sealed class LineDrawing : Component
	{
		/// <summary />
		private static Graphics g_graphics;
		/// <summary />
		private static LineTypes g_LineType = LineTypes.Etched;
		/// <summary />
		private static Pen g_penSolidLine = new Pen(SystemColors.ControlText, 1);
		/// <summary />
		private static Pen g_penLightLine = new Pen(SystemColors.ControlLightLight, 1);
		/// <summary />
		private static Pen g_penDarkLine = new Pen(SystemColors.ControlDark, 1);
		/// <summary />
		private static Point g_StartLocation = new Point(0, 0);
		/// <summary />
		private static Point g_EndLocation = new Point(0, 0);
		/// <summary />
		private static int g_dypThickness = 1;

		#region Constructors

		/// <summary />
		public LineDrawing()
		{
			g_graphics = null;
		}

		/// <summary />
		public LineDrawing(Graphics g)
		{
			g_graphics = g;
		}

		/// <summary />
		public LineDrawing(Graphics g, LineTypes lineType)
		{
			g_graphics = g;
			g_LineType = lineType;
		}

		/// <summary />
		public LineDrawing(Graphics g, int x, int y, int dxpLength)
		{
			Draw(g, x, y, dxpLength);
		}

		/// <summary />
		public LineDrawing(Graphics g, int x, int y, int dxpLength, LineTypes lineType)
		{
			Draw(g, x, y, dxpLength, lineType);
		}

		/// <summary />
		public LineDrawing(Graphics g, int x1, int y1, int x2, int y2)
		{
			Draw(g, x1, y1, x2, y2);
		}

		/// <summary />
		public LineDrawing(Graphics g, int x1, int y1, int x2, int y2, LineTypes lineType)
		{
			Draw(g, x1, y1, x2, y2, lineType);
		}

		/// <summary />
		public LineDrawing(IContainer container)
		{
			container.Add(this);
		}
		#endregion

		/// <summary />
		public bool IsDisposed { get; private set; }

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");

			base.Dispose(disposing);
			IsDisposed = true;
		}

		#region Properties

		/// <summary>
		/// Set the graphics object.
		/// </summary>
		[Browsable(false)]
		public Graphics Graphics
		{
			get
			{
				return g_graphics;
			}
			set
			{
				g_graphics = value;
			}
		}

		/// <summary />
		[Description("Determines the type of line (e.g. etched, solid, etc.)")]
		public LineTypes LineType
		{
			get
			{
				return g_LineType;
			}
			set
			{
				g_LineType = value;
			}
		}

		/// <summary />
		[Description("Determines the thickness of lines of type Solid.")]
		public int SolidLineThickness
		{
			get
			{
				return g_dypThickness;
			}
			set
			{
				if (g_dypThickness != value)
				{
					g_dypThickness = value;
					g_penSolidLine = new Pen(g_penSolidLine.Color, value);
				}
			}
		}

		/// <summary />
		[Description("Determines the color used to draw lines of type Solid.")]
		public Color SolidLineColor
		{
			get
			{
				return g_penSolidLine.Color;
			}
			set
			{
				g_penSolidLine = new Pen(value, this.SolidLineThickness);
			}
		}

		/// <summary>
		/// Gets or Sets a line's beginning point.
		/// </summary>
		[Description("Determines the start location for the line.")]
		public Point StartLocation
		{
			get
			{
				return g_StartLocation;
			}
			set
			{
				g_StartLocation = value;
			}
		}

		/// <summary>
		/// Gets or Sets a line's end point.
		/// </summary>
		[Description("Determines the end location for the line.")]
		public Point EndLocation
		{
			get
			{
				return g_EndLocation;
			}
			set
			{
				g_EndLocation = value;
			}
		}

		#endregion

		#region ShouldSerialize methods

		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		private bool ShouldSerializeLineType()
		{
			return g_LineType != LineTypes.Etched;
		}

		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		private bool ShouldSerializeSolidLineColor()
		{
			return g_LineType == LineTypes.Solid && g_penSolidLine.Color != SystemColors.ControlText;
		}

		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		private bool ShouldSerializeSolidLineThickness()
		{
			return g_LineType == LineTypes.Solid && g_dypThickness != 1;
		}

		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		private bool ShouldSerializeStartLocation()
		{
			return g_StartLocation.X != 0 || g_StartLocation.Y != 0;
		}

		/// <summary>
		/// Returns true if value is different from Default value
		/// </summary>
		private bool ShouldSerializeEndLocation()
		{
			return g_EndLocation.X != 0 || g_EndLocation.Y != 0;
		}

		#endregion

		/// <summary />
		private static void Draw(Graphics g, int x, int y, int dxpLength)
		{
			g_graphics = g;
			g_StartLocation = new Point(x, y);
			g_EndLocation = new Point(x + dxpLength, y);
			Draw();
		}

		/// <summary />
		public static void Draw(Graphics g, int x, int y, int dxpLength, LineTypes lineType)
		{
			g_LineType = lineType;
			Draw(g, x, y, dxpLength);
		}

		/// <summary />
		public static void Draw(Graphics g, int x1, int y1, int x2, int y2)
		{
			g_graphics = g;
			g_StartLocation = new Point(x1, y1);
			g_EndLocation = new Point(x2, y2);
			Draw();
		}

		/// <summary />
		internal void Draw(int x1, int y1, int x2, int y2, Color solidLineColor)
		{
			g_StartLocation = new Point(x1, y1);
			g_EndLocation = new Point(x2, y2);
			g_penSolidLine.Color = solidLineColor;
			g_LineType = LineTypes.Solid;
			Draw();
		}

		/// <summary />
		private static void Draw(Graphics g, int x1, int y1, int x2, int y2, LineTypes lineType)
		{
			g_LineType = lineType;
			Draw(g, x1, y1, x2, y2);
		}

		/// <summary />
		private static void Draw()
		{
			// Can't draw without a graphics object.
			if (g_graphics == null)
			{
				throw (new ArgumentNullException());
			}

			Point tmpEnd = new Point(g_EndLocation.X, g_EndLocation.Y + 1);
			switch (g_LineType)
			{
				case LineTypes.Etched:
				case LineTypes.Raised:
					g_graphics.DrawLine((g_LineType == LineTypes.Etched ? g_penDarkLine : g_penLightLine), g_StartLocation, g_EndLocation);
					var tmpStart = new Point(g_StartLocation.X, g_StartLocation.Y + 1);

					g_graphics.DrawLine((g_LineType == LineTypes.Etched ? g_penLightLine : g_penDarkLine), tmpStart, tmpEnd);
					break;

				case LineTypes.Solid:
					g_graphics.DrawLine(g_penSolidLine, g_StartLocation, g_EndLocation);
					break;
			}
		}

		/// <summary>
		/// Draws an etched, horizontal line above controls (usually buttons) on a dialog box.
		/// The etched line will be drawn the same distance above the specified control as the
		/// control is from the bottom of the dialog.
		/// </summary>
		/// <param name="g">Graphics object in which to draw</param>
		/// <param name="rcDlgClient">The client rectangle of the dialog box.</param>
		/// <param name="y">The vertical position where the etched line is to be drawn.</param>
		public static void DrawDialogControlSeparator(Graphics g, Rectangle rcDlgClient, int y)
		{
			Draw(g, 8, y, rcDlgClient.Width - 17, LineTypes.Etched);
		}
	}
}