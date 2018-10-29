// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Status Bar Panel that Displays Progress
	/// </summary>
	public sealed class StatusBarProgressPanel : StatusBarPanel, IProgressDisplayer
	{
		#region Member Variables

		private TraceSwitch traceSwitch = new TraceSwitch("Common_Controls", "Used for diagnostic output", "Off");
		private bool _drawEventRegistered;
		private Brush _progressBrush;
		private Brush _textBrush;
		private Font _textFont;
		private Rectangle m_bounds;
		/// <summary />
		private ProgressState m_stateProvider;
		/// <summary>
		/// for double buffering
		/// </summary>
		private Bitmap offScreenBmp;
		/// <summary>
		/// for double buffering
		/// </summary>
		private Graphics offScreenDC;
		/// <summary />
		private int m_drawPosition;
		private Timer timer1;
		private IContainer components;
		#endregion

		#region Construction / Destruction

		/// <inheritdoc />
		public StatusBarProgressPanel(StatusBar bar)
		{
			Init();
			bar.DrawItem += OnDrawItem;
		}

		//for designer use only
		internal StatusBarProgressPanel()
		{
			Init();
		}

		/// <summary />
		private void Init()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			AnimationStyle = ProgressDisplayStyle.Infinite;
			StepSize = 10;
			StartPoint = 0;
			EndPoint = 100;
			ShowText = true;
			_textFont = new Font(MiscUtils.StandardSansSerif, 8);
			_textBrush = SystemBrushes.ControlText;
			_progressBrush = SystemBrushes.Highlight;
			AnimationTick = TimeSpan.FromSeconds(0.5);
			AnimationStyle = ProgressDisplayStyle.LeftToRight;
			AnimationTick = System.TimeSpan.Parse("00:00:00.5000000");
			EndPoint = 100;
			ShowText = true;
			StartPoint = 0;
			StepSize = 10;
			Style = StatusBarPanelStyle.OwnerDraw;
			TextFont = new Font(MiscUtils.StandardSansSerif, 8F);
			const int TIMER_INTERVAL = 1000; //1000 milliseconds
			timer1.Interval = TIMER_INTERVAL;
			timer1.Start();
		}

		/// <summary>
		///
		/// </summary>
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
			}

			timer1.Dispose();
			timer1 = null;

			traceSwitch = null;
			_progressBrush = null;
			_textBrush = null;
			_textFont = null;

			base.Dispose(disposing);
			IsDisposed = true;
		}

		#endregion

		/// <summary />
		/// <remarks>Getting things set up to actually draw in any custom status panel is surprisingly
		/// difficult. The first problem is, until some magical point, we cannot even figure out who are parent is.
		/// Apart from that, we cannot even find out what our boundary rectangle is. we can find out
		/// the boundary rectangle only when our parent tells us to draw.
		/// kept the nature of this progress bar is that it actually wants to draw on its own agenda, not on the parent's.
		/// Thus, it is up to some other code to somehow get this event to fire so that we can figure out our boundary.
		/// </remarks>
		public void OnDrawItem(object sender, StatusBarDrawItemEventArgs sbdevent)
		{
			// It has proved difficult to dispose of all the panels of a status bar without something
			// at the system level trying to draw one that has already been disposed. The simplest
			// solution is just to ignore attempts to draw disposed ones.
			if (IsDisposed)
			{
				return;
			}
			if (sbdevent.Panel == this)
			{
				m_bounds = sbdevent.Bounds;
				// if we are using visual styles, the progress bar will sometimes overlap the border, so we reduce
				// the size of the progress bar a little
				if (Application.RenderWithVisualStyles)
				{
					m_bounds.Width = m_bounds.Width - SystemInformation.Border3DSize.Width;
				}
			}
		}

		/// <inheritdoc />
		public void SetStateProvider(ProgressState state)
		{
			m_stateProvider = state;
			Reset();
		}

		/// <inheritdoc />
		public void ClearStateProvider()
		{
			m_stateProvider = null;
			Reset();
		}

		/// <summary />
		public void Tick(object stateInfo)
		{
			if (m_stateProvider == null)
			{
				return;
			}
			RefreshSafely();
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
			//
			// timer1
			//
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			((System.ComponentModel.ISupportInitialize)(this)).EndInit();

		}

		#endregion

		#region Properties

		/// <summary>
		/// The method used when drawing the progress bar
		/// </summary>
		[Category("Animation")]
		private ProgressDisplayStyle AnimationStyle { get; set; }

		/// <summary>
		/// Timespan between infinite progress animation changes
		/// </summary>
		[Category("Animation")]
		public TimeSpan AnimationTick { get; set; }

		/// <summary>
		/// Amount to move on each progress step
		/// </summary>
		[Category("Measurement")]
		public long StepSize { get; set; }

		/// <summary>
		/// Start point of progress
		/// </summary>
		[Category("Measurement")]
		public long StartPoint { get; set; }

		/// <summary>
		/// Point of progress completion
		/// </summary>
		[Category("Measurement")]
		public long EndPoint { get; set; }

		/// <summary>
		/// Current Position of the Progress Indicator
		/// </summary>
		public int ProgressPosition
		{
			get
			{
				if (m_stateProvider == null)
				{
					return 0;
				}
				var x = m_stateProvider.PercentDone; // ProgressPosition;
				return x > 100 ? 100 : x;
			}
		}

		/// <summary>
		/// Font style of the Text when it is drawn
		/// </summary>
		[Category("Style")]
		public Font TextFont
		{
			get
			{
				return _textFont;
			}
			set
			{
				_textFont?.Dispose();
				_textFont = value;
			}
		}

		/// <summary>
		/// Optionally Display Text value of the Indicator
		/// </summary>
		[Category("Style")]
		public bool ShowText { get; set; }

		#endregion

		private void DrawPanel()
		{
			if (m_bounds.Width == 0)
			{
				//TOO SLOW BY FAR!				System.Diagnostics.Debug.Write(".");
				return; //not ready yet
			}

			using (var graphics = Parent.CreateGraphics())
			{
				var eventBounds = m_bounds;
				if (offScreenBmp == null)
				{
					offScreenBmp = new Bitmap(eventBounds.Width, eventBounds.Height);
					offScreenDC = Graphics.FromImage(offScreenBmp);
				}

				var fullBounds = eventBounds;
				fullBounds.X = 0;
				fullBounds.Y = 0;
				offScreenDC.FillRectangle(SystemBrushes.Control, fullBounds);

				//allow it to 'catch up' smoothly
				var pos = ProgressPosition;
				m_drawPosition = pos;
				if (m_drawPosition != StartPoint)
				{
					if ((m_drawPosition <= EndPoint) || AnimationStyle == ProgressDisplayStyle.Infinite)
					{
						var bounds = eventBounds;
						var percent = m_drawPosition / (EndPoint - (float)StartPoint);

						switch (AnimationStyle)
						{

							case ProgressDisplayStyle.LeftToRight:
								{
									bounds.Width = (int)(percent * eventBounds.Width);
									break;
								}
							case ProgressDisplayStyle.RightToLeft:
								{
									bounds.Width = (int)(percent * eventBounds.Width);
									bounds.X += eventBounds.Width - bounds.Width;
									break;
								}
							case ProgressDisplayStyle.BottomToTop:
								{
									bounds.Height = (int)(percent * eventBounds.Height);
									bounds.Y += eventBounds.Height - bounds.Height;
									break;
								}
							case ProgressDisplayStyle.TopToBottom:
								{
									bounds.Height = (int)(percent * eventBounds.Height);
									break;
								}
							case ProgressDisplayStyle.Infinite:
								{
									bounds.Height = (int)(percent * eventBounds.Height);
									bounds.Y += (eventBounds.Height - bounds.Height) / 2;
									bounds.Width = (int)(percent * eventBounds.Width);
									bounds.X += (eventBounds.Width - bounds.Width) / 2;
									break;
								}
						}

						// draw the progress bar
						bounds.X = 0;
						bounds.Y = 0;
						offScreenDC.FillRectangle(_progressBrush, bounds);
						if (ShowText)
						{
							// draw the text on top of the progress bar
							offScreenDC.DrawString(m_stateProvider.Status, _textFont, _textBrush, new PointF(0.0F, 0.0F));
						}
					}
				}

				graphics.DrawImage(offScreenBmp, eventBounds.X, eventBounds.Y);
			}
		}

		#region Refresh

		/// <inheritdoc />
		public void Refresh()
		{
			RefreshSafely();
		}

		#endregion

		#region Reset

		/// <summary>
		/// Reinitializes the progress bar
		/// </summary>
		public void Reset()
		{
			m_drawPosition = 0;
			RefreshSafely();
		}

		private void RefreshSafely()
		{

			if (IsDisposed)
			{
				return;
			}
			DrawPanel();
		}

		#endregion

		private void timer1_Tick(object sender, System.EventArgs e)
		{
			if (Parent == null)
			{
				return;
			}
			// don't invoke if parent is yet to be created
			if (!Parent.IsHandleCreated)
			{
				return;
			}
			if (!_drawEventRegistered)
			{
				Debug.WriteLineIf(traceSwitch.TraceInfo, "reg", traceSwitch.DisplayName);
				_drawEventRegistered = true;
				Parent.Invoke(new RefreshDelegate(Refresh));
			}

			RefreshSafely();
		}

		private delegate void RefreshDelegate();

		/// <summary>
		/// Statusbar Progress Display Styles
		/// </summary>
		private enum ProgressDisplayStyle
		{
			/// <summary>
			/// A continually moving animation
			/// </summary>
			Infinite,
			/// <summary>
			/// A progress bar that fills from left to right
			/// </summary>
			LeftToRight,
			/// <summary>
			/// A progress bar that fills from right to left
			/// </summary>
			RightToLeft,
			/// <summary>
			/// A progress bar that fills from bottom to top
			/// </summary>
			BottomToTop,
			/// <summary>
			/// A progress bar that fills from top to bottom
			/// </summary>
			TopToBottom
		}
	}
}