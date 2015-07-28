// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SidebarLibrary.Win32;
using SidebarLibrary.General;
using System.Diagnostics.CodeAnalysis;

namespace SidebarLibrary.WinControls
{

	// I put the delegate and the event handler
	// outside the class so that the user does not have
	// to prefix the progressbar class name to use this properties or delegate
	// Putting them inside the class just make them hard to use

	// Enumeration of property change events
	public enum ProgressBarProperty
	{
		BackgroundColor,
		ForegroundColor,
		BorderColor,
		Border3D,
		EnableBorder3D,
		Value,
		Step,
		Minimun,
		Maximun,
		Smooth,
		ShowProgressText,
		BackgroundBitmap,
		ForegroundBitmap,
		ProgressTextHiglightColor,
		ProgressTextColor,
		GradientStartColor,
		GradientMiddleColor,
		GradientEndColor,

	}

	// Declare the property change event signature
	public delegate void ProgressBarPropertyChangedHandler(ProgressBarEx pogressBar, ProgressBarProperty prop);

	/// <summary>
	/// Summary description for FlatProgressBar.
	/// </summary>
	public class ProgressBarEx : System.Windows.Forms.Control
	{

		// We need to know how we are going to draw the progress bar
		// this won't come from the user setting a flag but how the
		// progress bar is constructed
		private enum ProgressBarType { Standard, Bitmap, Gradient }

		// Public events
		public event ProgressBarPropertyChangedHandler PropertyChanged;

		Color backgroundColor;
		Color foregroundColor;
		Color borderColor;
		int _value = 0;
		int step = 1;
		int min = 0;
		int max = 100;
		bool smooth = false;
		Border3DStyle border3D = Border3DStyle.Flat;
		bool enableBorder3D = false;
		bool showProgressText = false;
		Color progressTextHiglightColor = Color.Empty;
		Color progressTextColor = Color.Empty;
		ProgressBarType barType = ProgressBarType.Standard;
		Bitmap foregroundBitmap = null;
		Bitmap backgroundBitmap = null;
		Color gradientStartColor = Color.Empty;
		Color gradientMiddleColor = Color.Empty;
		Color gradientEndColor = Color.Empty;


		// Default contructor to draw a "Standard" progress Bar
		public ProgressBarEx()
		{
			InitializeProgressControl(ProgressBarType.Standard, ColorUtil.VSNetControlColor,
				ColorUtil.VSNetBorderColor, SystemColors.Highlight, null, null, Color.Empty, Color.Empty, Color.Empty);
		}

		public ProgressBarEx(Bitmap foregroundBitmap, Bitmap backgroundBitmap)
		{
			InitializeProgressControl(ProgressBarType.Bitmap, ColorUtil.VSNetControlColor,
				ColorUtil.VSNetBorderColor, ColorUtil.VSNetBorderColor,
				foregroundBitmap, backgroundBitmap, Color.Empty, Color.Empty, Color.Empty);
		}

		public ProgressBarEx(Bitmap foregroundBitmap)
		{
			InitializeProgressControl(ProgressBarType.Bitmap, ColorUtil.VSNetControlColor,
				ColorUtil.VSNetBorderColor,ColorUtil.VSNetBorderColor,
				foregroundBitmap, null, Color.Empty, Color.Empty, Color.Empty);
		}

		public ProgressBarEx(Color gradientStartColor, Color gradientEndColor)
		{
			InitializeProgressControl(ProgressBarType.Gradient, ColorUtil.VSNetControlColor,
				ColorUtil.VSNetBorderColor, ColorUtil.VSNetBorderColor,
				foregroundBitmap, null, gradientStartColor, Color.Empty, gradientEndColor);
		}

		public ProgressBarEx(Color gradientStartColor, Color gradientMiddleColor, Color gradientEndColor)
		{
			InitializeProgressControl(ProgressBarType.Gradient, ColorUtil.VSNetControlColor,
				ColorUtil.VSNetBorderColor, ColorUtil.VSNetBorderColor,
				foregroundBitmap, null, gradientStartColor, gradientMiddleColor, gradientEndColor);
		}

		void InitializeProgressControl(ProgressBarType barType, Color backgroundColor,
			Color foregroundColor, Color borderColor, Bitmap foregroundBitmap, Bitmap backgroundBitmap, Color gradientStartColor,
			Color gradientMiddleColor, Color gradientEndColor)
		{
			// Setup Double buffering
			SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.DoubleBuffer, true);

			this.barType = barType;
			this.backgroundColor = backgroundColor;
			this.foregroundColor = foregroundColor;
			this.borderColor = borderColor;
			this.foregroundBitmap = foregroundBitmap;
			this.backgroundBitmap = backgroundBitmap;
			this.gradientStartColor = gradientStartColor;
			this.gradientMiddleColor = gradientMiddleColor;
			this.gradientEndColor = gradientEndColor;
		}

		public Color BackgroundColor
		{
			set
			{
				if ( backgroundColor != value )
				{
					backgroundColor = value;
					FirePropertyChange(ProgressBarProperty.BackgroundColor);
				}
			}
			get { return backgroundColor; }
		}

		public Color ForegroundColor
		{
			set
			{
				if ( foregroundColor != value )
				{
					foregroundColor = value;
					FirePropertyChange(ProgressBarProperty.ForegroundColor);
				}
			}
			get { return foregroundColor; }
		}

		public Color BorderColor
		{
			set
			{
				if ( borderColor != value )
				{
					borderColor = value;
					FirePropertyChange(ProgressBarProperty.BorderColor);
				}
			}
			get { return borderColor; }
		}

		public int Value
		{
			set
			{
				if ( _value != value )
				{
					if ( !(value <= max && value >= min) )
					{
						// Throw exception to indicate out of range condition
						string message = "ProgressBarEx Value: " + value.ToString()
							+ " is out of range. It needs to be between " +
							min.ToString() + " and " + max.ToString();
						ArgumentOutOfRangeException outRangeException = new ArgumentOutOfRangeException("Value", message);
						throw(outRangeException);
					}
					_value = value;
					FirePropertyChange(ProgressBarProperty.Value);
				}
			}
			get { return _value; }
		}

		public new Size Size
		{
			set
			{
				// Make sure width and height dimensions are always
				// an even number so that we can do round math
				//  when we draw the progress bar segments
				Size newSize = value;
				if ( newSize.Width % 2 != 0) newSize.Width++;
				if ( newSize.Height % 2 != 0) newSize.Height++;
				base.Size = newSize;
			}
			get { return base.Size; }
		}

		public int Step
		{
			set
			{
				if ( step != value )
				{
					step = value;
					FirePropertyChange(ProgressBarProperty.Step);
				}
			}
			get { return step; }
		}

		public int Minimum
		{
			set
			{
				if ( min != value )
				{
					if ( value >= max )
					{
						// Throw exception to indicate out of range condition
						string message = "ProgressBarEx Minimum Value: "
							+ value.ToString() + " is out of range. It needs to be less than " +
							"Maximun value: " + max.ToString();
						ArgumentOutOfRangeException outRangeException = new ArgumentOutOfRangeException("Value", message);
						throw(outRangeException);
					}
					min = value;
					FirePropertyChange(ProgressBarProperty.Minimun);
				}
			}
			get { return min; }
		}

		public int Maximum
		{
			set
			{
				if ( max != value )
				{
					if ( value <= min )
					{
						// Throw exception to indicate out of range condition
						string message = "ProgressBarEx Maximum Value: " + value.ToString()
							+ " is out of range. It needs to be greater than " +
							"Minimum value: " + min.ToString();
						ArgumentOutOfRangeException outRangeException = new ArgumentOutOfRangeException("Value", message);
						throw(outRangeException);
					}
					max = value;
					FirePropertyChange(ProgressBarProperty.Maximun);
				}
			}
			get { return max; }
		}

		public bool Smooth
		{
			set
			{
				if ( smooth != value )
				{
					smooth = value;
					FirePropertyChange(ProgressBarProperty.Smooth);
				}
			}
			get { return smooth; }
		}

		public Border3DStyle Border3D
		{
			set
			{
				if ( border3D != value )
				{
					border3D = value;
					FirePropertyChange(ProgressBarProperty.Border3D);
				}
			}
			get { return border3D; }
		}

		public bool EnableBorder3D
		{
			set
			{
				if ( enableBorder3D != value )
				{
					enableBorder3D = value;
					FirePropertyChange(ProgressBarProperty.Border3D);
				}
			}
			get { return enableBorder3D; }
		}

		public bool ShowProgressText
		{
			set
			{
				if ( showProgressText != value )
				{
					showProgressText = value;
					FirePropertyChange(ProgressBarProperty.ShowProgressText);
				}
			}
			get { return showProgressText; }
		}

		public Color ProgressTextHiglightColor
		{
			set
			{
				if ( progressTextHiglightColor != value )
				{
					progressTextHiglightColor = value;
					FirePropertyChange(ProgressBarProperty.ProgressTextHiglightColor);
				}
			}
			get { return progressTextHiglightColor; }
		}

		public Color ProgressTextColor
		{
			set
			{
				if ( progressTextColor != value )
				{
					progressTextColor = value;
					FirePropertyChange(ProgressBarProperty.ProgressTextColor);
				}
			}
			get { return progressTextColor; }
		}


		public Bitmap ForegroundBitmap
		{
			set
			{
				if ( foregroundBitmap != value )
				{
					foregroundBitmap = value;
					FirePropertyChange(ProgressBarProperty.ForegroundBitmap);
				}
			}
			get { return foregroundBitmap; }
		}

		public Bitmap BackgroundBitmap
		{
			set
			{
				if ( backgroundBitmap != value )
				{
					backgroundBitmap = value;
					FirePropertyChange(ProgressBarProperty.BackgroundBitmap);
				}
			}
			get { return backgroundBitmap; }
		}

		public Color GradientStartColor
		{
			set
			{
				if ( gradientStartColor != value )
				{
					gradientStartColor = value;
					FirePropertyChange(ProgressBarProperty.GradientStartColor);
				}
			}
			get { return gradientStartColor; }
		}

		public Color GradientMiddleColor
		{
			set
			{
				if ( gradientMiddleColor != value )
				{
					gradientMiddleColor = value;
					FirePropertyChange(ProgressBarProperty.GradientMiddleColor);
				}
			}
			get { return gradientMiddleColor; }
		}

		public Color GradientEndColor
		{
			set
			{
				if ( gradientEndColor != value )
				{
					gradientEndColor = value;
					FirePropertyChange(ProgressBarProperty.GradientEndColor);
				}
			}
			get { return gradientEndColor; }
		}

		public void PerformStep()
		{
			if ( _value < max )
				_value += step;
			if ( _value > max )
				_value = max;
			FirePropertyChange(ProgressBarProperty.Step);
		}

		void FirePropertyChange(ProgressBarProperty property)
		{
			// Fire event if we need to
			if (PropertyChanged != null)
				PropertyChanged(this, property);
			// Force a repaint of the control
			Invalidate();
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "g is a reference")]
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Get window area
			Win32.RECT rc = new Win32.RECT();
			WindowsAPI.GetWindowRect(Handle, ref rc);

			// Convert to a client size rectangle
			Rectangle rect = new Rectangle(0, 0, rc.right - rc.left, rc.bottom - rc.top);

			Graphics g = e.Graphics;
			DrawBackground(g, rect);
			DrawBorder(g, rect);
			DrawForeground(g, rect);

		}

		void DrawBorder(Graphics g, Rectangle windowRect)
		{
			if ( enableBorder3D == false )
			{
				g.DrawRectangle(new Pen(borderColor), windowRect.Left, windowRect.Top,
					windowRect.Width-1, windowRect.Height-1);
			}
			else
			{
				ControlPaint.DrawBorder3D(g, windowRect, border3D);
			}
		}

		void DrawBackground(Graphics g, Rectangle windowRect)
		{
			if ( barType == ProgressBarType.Standard )
			{
				DrawStandardBackground(g, windowRect);
			}
			else if ( barType == ProgressBarType.Bitmap )
			{
				DrawBitmapBackground(g, windowRect);
			}
			else if ( barType == ProgressBarType.Gradient )
			{
				DrawGradientBackground(g, windowRect);
			}
		}

		void DrawStandardBackground(Graphics g, Rectangle windowRect)
		{
			windowRect.Inflate(-1, -1);
			g.FillRectangle(new SolidBrush(backgroundColor), windowRect);
		}

		void DrawBitmapBackground(Graphics g, Rectangle windowRect)
		{
			if (  backgroundBitmap != null )
			{
				// If we strech the bitmap most likely than not the bitmap
				// won't look good. I will draw the background bitmap just
				// by sampling a portion of the bitmap equal to the segment width
				// -- if we were drawing segments --- and draw this over and over
				// without leaving gaps
				int segmentWidth = (windowRect.Height-4)*3/4;
				segmentWidth -= 2;
				Rectangle drawingRect = new Rectangle(windowRect.Left+1, windowRect.Top+1, segmentWidth, windowRect.Height-2);
				for ( int i = 0; i < windowRect.Width-2; i += segmentWidth)
				{
					g.DrawImage(backgroundBitmap, drawingRect.Left + i, drawingRect.Top,
						segmentWidth, windowRect.Height);
					// If last segment does not fit, just draw a portion of it
					if ( i + segmentWidth > windowRect.Width-2 )
						g.DrawImage(backgroundBitmap, drawingRect.Left + i + segmentWidth, drawingRect.Top,
							windowRect.Width-2 - (drawingRect.Left + i + segmentWidth), windowRect.Height);
				}
			}
			else
			{
				windowRect.Inflate(-1, -1);
				g.FillRectangle(new SolidBrush(backgroundColor), windowRect);
			}
		}


		void DrawGradientBackground(Graphics g, Rectangle windowRect)
		{
			// Same as the standard background
			windowRect.Inflate(-1, -1);
			g.FillRectangle(new SolidBrush(backgroundColor), windowRect);
		}

		void DrawForeground(Graphics g, Rectangle windowRect)
		{
			if ( barType == ProgressBarType.Standard )
			{
				DrawStandardForeground(g, windowRect);
			}
			else if ( barType == ProgressBarType.Bitmap )
			{
				DrawBitmapForeground(g, windowRect);
			}
			else if ( barType == ProgressBarType.Gradient )
			{
				DrawGradientForeground(g, windowRect);
			}
		}

		void DrawStandardForeground(Graphics g, Rectangle windowRect)
		{
			if ( smooth )
				DrawStandardForegroundSmooth(g, windowRect);
			else
				DrawStandardForegroundSegmented(g, windowRect);

		}

		void DrawBitmapForeground(Graphics g, Rectangle windowRect)
		{

			// We should have a valid foreground bitmap if the type of
			// the progress bar is bitmap
			Debug.Assert(foregroundBitmap != null);

			// If we strech the bitmap most likely than not the bitmap
			// won't look good. I will draw the foreground bitmap just
			// by sampling a portion of the bitmap equal to the segment width
			// -- if we were drawing segments --- and draw this over and over
			// without leaving gaps
			int segmentWidth = (windowRect.Height-4)*3/4;
			segmentWidth -= 2;

			Rectangle segmentRect = new Rectangle(2,
				windowRect.Top + 2, segmentWidth, windowRect.Height-4);

			int progressWidth = (GetScaledValue() - 2);
			if ( progressWidth < 0 ) progressWidth = 0;
			int gap = 2;
			if ( smooth ) gap = 0;

			for ( int i = 0; i < progressWidth; i += segmentRect.Width+gap )
			{
				if ( i+segmentRect.Width+gap > progressWidth && (i+segmentRect.Width+gap > windowRect.Width-2-gap) )
				{
					// if we are about to leave because next segment does not fit
					// draw the portion that fits
					int partialWidth = progressWidth-i-2;
					Rectangle drawingRect = new Rectangle(segmentRect.Left+i,
						segmentRect.Top, partialWidth, segmentRect.Height);
					g.DrawImage(foregroundBitmap, drawingRect, 0, 0, drawingRect.Width, drawingRect.Height, GraphicsUnit.Pixel);
					break;
				}
				Rectangle completeSegment = new Rectangle(segmentRect.Left+i, segmentRect.Top, segmentRect.Width, segmentRect.Height);
				g.DrawImage(foregroundBitmap, completeSegment, 0, 0, completeSegment.Width, completeSegment.Height, GraphicsUnit.Pixel);
			}
		}


		void DrawGradientForeground(Graphics g, Rectangle windowRect)
		{
			// Three color gradient?
			bool useMiddleColor = false;
			if ( gradientMiddleColor != Color.Empty )
				useMiddleColor = true;

			if ( useMiddleColor )
				DrawThreeColorsGradient(g, windowRect);
			else
				DrawTwoColorsGradient(g, windowRect);
		}

		void DrawTwoColorsGradient(Graphics g, Rectangle windowRect)
		{
			// Calculate color distance
			int redStep = Math.Max(gradientEndColor.R, gradientStartColor.R)
				- Math.Min(gradientEndColor.R, gradientStartColor.R);
			int greenStep = Math.Max(gradientEndColor.G, gradientStartColor.G)
				- Math.Min(gradientEndColor.G, gradientStartColor.G);
			int blueStep = Math.Max(gradientEndColor.B, gradientStartColor.B)
				- Math.Min(gradientEndColor.B, gradientStartColor.B);

			// Do we need to increase or decrease
			int redDirection;
			if ( gradientEndColor.R > gradientStartColor.R )
				redDirection = 1;
			else
				redDirection = -1;

			int greenDirection;
			if (  gradientEndColor.G >  gradientStartColor.G )
				greenDirection = 1;
			else
				greenDirection = -1;

			int blueDirection;
			if ( gradientEndColor.B > gradientStartColor.B )
				blueDirection = 1;
			else
				blueDirection = -1;

			// The progress control won't allow its height to be anything other than
			// and even number since the width of the segment needs to be a perfect 3/4
			// of the control (height - 4) -- Four pixels are padding --
			int segmentWidth = (windowRect.Height-4)*3/4;
			segmentWidth -= 2;

			// how many segements we need to draw
			int gap = 2;
			if ( smooth ) gap = 0;
			int numOfSegments = (windowRect.Width - 4)/(segmentWidth + gap);

			// calculate the actual RGB steps for every segment
			redStep /= numOfSegments;
			greenStep /= numOfSegments;
			blueStep /= numOfSegments;

			Rectangle segmentRect = new Rectangle(2,
				windowRect.Top + 2, segmentWidth, windowRect.Height-4);

			int progressWidth = (GetScaledValue() - 2);
			if ( progressWidth < 0 ) progressWidth = 0;
			int counter = 0;
			for ( int i = 0; i < progressWidth; i += segmentRect.Width+gap )
			{
				Color currentColor = Color.FromArgb(gradientStartColor.R+(redStep*counter*redDirection),
					gradientStartColor.G+(greenStep*counter*greenDirection), gradientStartColor.B+(blueStep*counter*blueDirection));
				if ( i+segmentRect.Width+gap > progressWidth && (i+segmentRect.Width+gap > windowRect.Width-2-gap) )
				{
					// if we are about to leave because next segment does not fit
					// draw the portion that fits
					int partialWidth = progressWidth-i-2;
					Rectangle drawingRect = new Rectangle(segmentRect.Left+i,
						segmentRect.Top, partialWidth, segmentRect.Height);
					g.FillRectangle(new SolidBrush(currentColor), drawingRect);
					break;
				}
				Rectangle completeSegment = new Rectangle(segmentRect.Left+i, segmentRect.Top, segmentRect.Width, segmentRect.Height);
				g.FillRectangle(new SolidBrush(currentColor), completeSegment);
				counter++;
			}

		}

		void DrawThreeColorsGradient(Graphics g, Rectangle windowRect)
		{
			// Calculate color distance for the first half
			int redStepFirst = Math.Max(gradientStartColor.R, gradientMiddleColor.R)
				- Math.Min(gradientStartColor.R, gradientMiddleColor.R);
			int greenStepFirst = Math.Max(gradientStartColor.G, gradientMiddleColor.G)
				- Math.Min(gradientStartColor.G, gradientMiddleColor.G);
			int blueStepFirst = Math.Max(gradientStartColor.B, gradientMiddleColor.B)
				- Math.Min(gradientStartColor.B, gradientMiddleColor.B);

			// Calculate color distance for the second half
			int redStepSecond = Math.Max(gradientEndColor.R, gradientMiddleColor.R)
				- Math.Min(gradientEndColor.R, gradientMiddleColor.R);
			int greenStepSecond = Math.Max(gradientEndColor.G, gradientMiddleColor.G)
				- Math.Min(gradientEndColor.G, gradientMiddleColor.G);
			int blueStepSecond = Math.Max(gradientEndColor.B, gradientMiddleColor.B)
				- Math.Min(gradientEndColor.B, gradientMiddleColor.B);

			// Do we need to increase or decrease for the first half
			int redDirectionFirst;
			if ( gradientStartColor.R < gradientMiddleColor.R )
				redDirectionFirst = 1;
			else
				redDirectionFirst = -1;

			int greenDirectionFirst;
			if (  gradientStartColor.G <  gradientMiddleColor.G )
				greenDirectionFirst = 1;
			else
				greenDirectionFirst = -1;

			int blueDirectionFirst;
			if ( gradientStartColor.B < gradientMiddleColor.B )
				blueDirectionFirst = 1;
			else
				blueDirectionFirst = -1;

			// Do we need to increase or decrease for the second half
			int redDirectionSecond;
			if ( gradientMiddleColor.R < gradientEndColor.R )
				redDirectionSecond = 1;
			else
				redDirectionSecond = -1;

			int greenDirectionSecond;
			if (  gradientMiddleColor.G <  gradientEndColor.G )
				greenDirectionSecond = 1;
			else
				greenDirectionSecond = -1;

			int blueDirectionSecond;
			if ( gradientMiddleColor.B < gradientEndColor.B )
				blueDirectionSecond = 1;
			else
				blueDirectionSecond = -1;

			// The progress control won't allow its height to be anything other than
			// and even number since the width of the segment needs to be a perfect 3/4
			// of the control (height - 4) -- Four pixels are padding --
			int segmentWidth = (windowRect.Height-4)*3/4;
			segmentWidth -= 2;

			// how many segements we need to draw
			int gap = 2;
			if ( smooth ) gap = 0;
			int numOfSegments = (windowRect.Width - 4)/(segmentWidth + gap);

			// calculate the actual RGB step for every segment
			redStepFirst /= (numOfSegments/2);
			greenStepFirst /= (numOfSegments/2);
			blueStepFirst /= (numOfSegments/2);
			redStepSecond /= (numOfSegments/2);
			greenStepSecond /= (numOfSegments/2);
			blueStepSecond /= (numOfSegments/2);

			Rectangle segmentRect = new Rectangle(2,
				windowRect.Top + 2, segmentWidth, windowRect.Height-4);

			int progressWidth = (GetScaledValue() - 2);
			if ( progressWidth < 0 ) progressWidth = 0;
			int counter = 0;
			bool counterReset = true;
			for ( int i = 0; i < progressWidth; i += segmentRect.Width+gap )
			{
				Color currentColor = Color.Empty;
				if ( i < (windowRect.Width-4)/2 )
				{
					currentColor = Color.FromArgb(gradientStartColor.R+(redStepFirst*counter*redDirectionFirst),
						gradientStartColor.G+(greenStepFirst*counter*greenDirectionFirst),
						gradientStartColor.B+(blueStepFirst*counter*blueDirectionFirst));
				}
				else
				{
					if ( counterReset )
					{
						counterReset = false;
						counter = 0;
					}
					currentColor = Color.FromArgb(gradientMiddleColor.R+(redStepSecond*counter*redDirectionSecond),
						gradientMiddleColor.G+(greenStepSecond*counter*greenDirectionSecond),
						gradientMiddleColor.B+(blueStepSecond*counter*blueDirectionSecond));
				}

				if ( i+segmentRect.Width+gap > progressWidth && (i+segmentRect.Width+gap > windowRect.Width-2-gap) )
				{
					// if we are about to leave because next segment does not fit
					// draw the portion that fits
					int partialWidth = progressWidth-i-2;
					Rectangle drawingRect = new Rectangle(segmentRect.Left+i,
						segmentRect.Top, partialWidth, segmentRect.Height);
					g.FillRectangle(new SolidBrush(currentColor), drawingRect);
					break;
				}
				Rectangle completeSegment = new Rectangle(segmentRect.Left+i, segmentRect.Top, segmentRect.Width, segmentRect.Height);
				g.FillRectangle(new SolidBrush(currentColor), completeSegment);
				counter++;
			}
		}

		void DrawStandardForegroundSegmented(Graphics g, Rectangle windowRect)
		{
			// The progress control won't allow its height to be anything other than
			// and even number since the width of the segment needs to be a perfect 3/4
			// of the control (height - 4) -- Four pixels are padding --
			int segmentWidth = (windowRect.Height-4)*3/4;
			segmentWidth -= 2;

			Rectangle segmentRect = new Rectangle(2,
				windowRect.Top + 2, segmentWidth, windowRect.Height-4);

			int progressWidth = (GetScaledValue() - 2);
			if ( progressWidth < 0 ) progressWidth = 0;
			for ( int i = 0; i < progressWidth; i += segmentRect.Width+2 )
			{
				if ( i+segmentRect.Width+2 > progressWidth && (i+segmentRect.Width+2 > windowRect.Width-4) )
				{
					// if we are about to leave because next segment does not fit
					// draw the portion that fits
					int partialWidth = progressWidth-i-2;
					g.FillRectangle(new SolidBrush(foregroundColor),
						segmentRect.Left+i, segmentRect.Top, partialWidth, segmentRect.Height);
					break;
				}
				g.FillRectangle(new SolidBrush(foregroundColor), segmentRect.Left+i, segmentRect.Top,
					segmentRect.Width, segmentRect.Height);
			}
		}

		void DrawStandardForegroundSmooth(Graphics g, Rectangle windowRect)
		{
			int progressWidth = (GetScaledValue() - 4);
			g.FillRectangle(new SolidBrush(foregroundColor), windowRect.Left + 2, windowRect.Top+2,
				progressWidth, windowRect.Height-4);
			if ( ShowProgressText)
			{
				int percent = GetScaledValue()*100/windowRect.Width;
				string percentageValue = percent.ToString() + " " + "%";
				Size size = TextUtil.GetTextSize(g, percentageValue, Font);

				// Draw first part of the text in hightlight color in case it needs to be
				Rectangle clipRect = new Rectangle(windowRect.Left + 2, windowRect.Top+2,
					progressWidth, windowRect.Height-4);
				Point pos = new Point((windowRect.Width - size.Width)/2, (windowRect.Height - size.Height)/2);
				g.Clip = new Region(clipRect);
				Color textColor = progressTextHiglightColor;
				if ( textColor == Color.Empty )
					textColor = SystemColors.HighlightText;
				g.DrawString(percentageValue, Font, new SolidBrush(textColor), pos);

				// Draw rest in control text color if it needs to be
				clipRect = new Rectangle(progressWidth+2, windowRect.Top+2,
					windowRect.Width, windowRect.Height-4);
				g.Clip = new Region(clipRect);
				textColor = progressTextColor;
				if ( textColor == Color.Empty )
					textColor = SystemColors.ControlText;
				g.DrawString(percentageValue, Font, new SolidBrush(textColor), pos);
			}
		}

		int GetScaledValue()
		{
			int scaledValue = _value;
			Size currentSize = Size;
			scaledValue = (_value-min)*currentSize.Width/(max - min);
			return scaledValue;
		}

	}
}
