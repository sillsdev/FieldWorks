// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StatusBarProgressPanel.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
//	I (JH) worked on it 10-20 minutes at a time in between watching kids at home, as a side project, so
//	it's a bit choppy. I hesitate to check it in, but if I don't it will get lost and I think we will want it
//	when we start running LexText on something less than developer machines.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Status Bar Panel that Displays Progress
	/// </summary>
	public class StatusBarProgressPanel : StatusBarPanel, IFWDisposable, IProgressDisplayer
	{
		//static public StatusBarProgressPanel s_StatusBarProgressPanel;
		#region Member Variables

		//private RefreshDelegate _refreshDelegate;
		private TraceSwitch traceSwitch = new TraceSwitch("Common_Controls", "Used for diagnostic output", "Off");
		private bool _drawEventRegistered=false;
		private ProgressDisplayStyle _animationStyle;
		private long _stepSize;
		private long _startPoint;
		private long _endPoint;
		private Brush _progressBrush;
		private Brush _textBrush;
		private Font _textFont;
		private bool _showText;
		private Rectangle m_bounds;
		private TimeSpan _animationTick;
		/// <summary>
		///
		/// </summary>
		protected ProgressState m_stateProvider;
		/// <summary>
		/// for double buffering
		/// </summary>
		private Bitmap offScreenBmp = null;
		/// <summary>
		/// for double buffering
		/// </summary>
		private Graphics offScreenDC = null;
		/// <summary>
		///
		/// </summary>
		protected int m_drawPosition;

		private System.Windows.Forms.Timer timer1;
		private System.ComponentModel.IContainer components;
		#endregion

		#region Construction / Destruction

		/// <summary>
		/// Creates a new StatusBarProgressPanel
		/// </summary>
		public StatusBarProgressPanel(StatusBar bar)
		{
			Init();
			bar.DrawItem +=
				new StatusBarDrawItemEventHandler(this.OnDrawItem);
		}
		//for designer use only
		internal StatusBarProgressPanel()
		{
			Init();
		}

		/// <summary>
		///
		/// </summary>
		protected void Init()
		{
//			s_StatusBarProgressPanel = this;
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();


			_animationStyle = ProgressDisplayStyle.Infinite;

			//			ProgressPosition = 0;
			_stepSize = 10;
			_startPoint = 0;
			_endPoint = 100;

			_showText = true;
			_textFont = new Font("Arial", 8);
			_textBrush = SystemBrushes.ControlText;

			_progressBrush = SystemBrushes.Highlight;

			//			_increasing = true;

			_animationTick = TimeSpan.FromSeconds(0.5);
			//			InitializeAnimationThread();

			//			_refreshDelegate = new RefreshDelegate( this.Refresh );

			//------
			this.AnimationStyle = ProgressDisplayStyle.LeftToRight;
			this.AnimationTick = System.TimeSpan.Parse("00:00:00.5000000");
			this.EndPoint = ((long)(100));
			//	this.ProgressPosition = ((long)(0));
			this.ShowText = true;
			this.StartPoint = ((long)(0));
			this.StepSize = ((long)(10));
			this.Style = System.Windows.Forms.StatusBarPanelStyle.OwnerDraw;
			this.TextFont = new System.Drawing.Font("Arial", 8F);

			const int TIMER_INTERVAL = 1000; //1000 milliseconds
			timer1.Interval = TIMER_INTERVAL;
			timer1.Start();


			// Create a timer that signals the delegate to invoke
			// CheckStatus after one second, and every 1/4 second
			// thereafter.
			//			Console.WriteLine("{0} Creating timer.\n",
			//				DateTime.Now.ToString("h:mm:ss.fff"));
			////			m_outOfThreadTimer = new System.Threading.Timer(new TimerCallback(this.Tick),
			//				null, 100, 100);
		}

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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}

			timer1.Dispose();
			timer1 = null;

			traceSwitch = null;
			_progressBrush = null;
			_textBrush = null;
			_textFont = null;

			base.Dispose( disposing );
			m_isDisposed = true;
		}

		#endregion

		/// <summary>
		///
		/// </summary>
		/// <remarks>Getting things set up to actually draw in any custom status panel is surprisingly
		/// difficult. The first problem is, until some magical point, we cannot even figure out who are parent is.
		/// Apart from that, we cannot even find out what our boundary rectangle is. we can find out
		/// the boundary rectangle only when our parent tells us to draw.
		/// kept the nature of this progress bar is that it actually wants to draw on its own agenda, not on the parent's.
		/// Thus, it is up to some other code to somehow get this event to fire so that we can figure out our boundary.
		/// </remarks>
		/// <param name="sender"></param>
		/// <param name="sbdevent"></param>
		public void OnDrawItem(object sender, StatusBarDrawItemEventArgs sbdevent)
		{
			// It has proved difficult to dispose of all the panels of a status bar without something
			// at the system level trying to draw one that has already been disposed. The simplest
			// solution is just to ignore attempts to draw disposed ones.
			if (IsDisposed)
				return;
			if (sbdevent.Panel == this)
			{
				m_bounds = sbdevent.Bounds;
				// if we are using visual styles, the progress bar will sometimes overlap the border, so we reduce
				// the size of the progress bar a little
				if (Application.RenderWithVisualStyles)
					m_bounds.Width = m_bounds.Width - SystemInformation.Border3DSize.Width;
				//				System.Diagnostics.Debug.WriteLine("");
				//				System.Diagnostics.Debug.WriteLine("SetBounds");

			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="state"></param>
		public void SetStateProvider(ProgressState state)
		{
			CheckDisposed();

			m_stateProvider = state;
			this.Reset();
		}

		/// <summary>
		///
		/// </summary>
		public void ClearStateProvider()
		{
			CheckDisposed();

			m_stateProvider = null;
			this.Reset();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="stateInfo"></param>
		public void Tick(Object stateInfo)
		{
			CheckDisposed();

			if (m_stateProvider == null)
				return;
			RefreshSafely();
			//this.Step();
			//			if(this.ProgressPosition >= this.EndPoint)
			//				this.Reset();

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
		public ProgressDisplayStyle AnimationStyle
		{
			get
			{
				CheckDisposed();

				return _animationStyle;
			}
			set
			{
				CheckDisposed();

				_animationStyle = value;
			}
		}

		/// <summary>
		/// Timespan between infinate progress animation changes
		/// </summary>
		[Category("Animation")]
		public TimeSpan AnimationTick
		{
			get
			{
				CheckDisposed();

				return _animationTick;
			}
			set
			{
				CheckDisposed();

				_animationTick = value;
			}
		}

		/// <summary>
		/// Ammount to move on each progress step
		/// </summary>
		[Category("Measurement")]
		public long StepSize
		{
			get
			{
				CheckDisposed();

				return _stepSize;
			}
			set
			{
				CheckDisposed();

				_stepSize = value;
			}
		}

		/// <summary>
		/// Start point of progress
		/// </summary>
		[Category("Measurement")]
		public long StartPoint
		{
			get
			{
				CheckDisposed();

				return _startPoint;
			}
			set
			{
				CheckDisposed();

				_startPoint = value;
			}
		}

		/// <summary>
		/// Point of progress completion
		/// </summary>
		[Category("Measurement")]
		public long EndPoint
		{
			get
			{
				CheckDisposed();

				return _endPoint;
			}
			set
			{
				CheckDisposed();

				_endPoint = value;
			}
		}

		/// <summary>
		/// Current Position of the Progress Indicator
		/// </summary>
		public int ProgressPosition
		{
			get
			{
				CheckDisposed();

				if(m_stateProvider ==null)
					return 0;
				int x= m_stateProvider.PercentDone;// ProgressPosition;
				if(x>100)
					return 100;
				else
					return x;
			}
		}

		/// <summary>
		/// Brush style of the progress indicator
		/// </summary>
		[Category("Style")]
		public Brush ProgressDrawStyle
		{
			get
			{
				CheckDisposed();

				return _progressBrush;
			}
			set
			{
				CheckDisposed();

				if (_progressBrush != null)
					_progressBrush.Dispose();
				_progressBrush = value;
			}
		}

		/// <summary>
		/// Brush style of the Text when it is drawn
		/// </summary>
		[Category("Style")]
		public Brush TextDrawStyle
		{
			get
			{
				CheckDisposed();

				return _textBrush;
			}
			set
			{
				CheckDisposed();

				if (_textBrush != null)
					_textBrush.Dispose();
				_textBrush = value;
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
				CheckDisposed();

				return _textFont;
			}
			set
			{
				CheckDisposed();

				if (_textFont != null)
					_textFont.Dispose();
				_textFont = value;
			}
		}

		/// <summary>
		/// Optionally Display Text value of the Indicator
		/// </summary>
		[Category("Style")]
		public bool ShowText
		{
			get
			{
				CheckDisposed();

				return _showText;
			}
			set
			{
				CheckDisposed();

				_showText = value;
			}
		}

		#endregion

		#region Step

		//		/// <summary>
		//		/// Promotes the progress bar by one step
		//		/// </summary>
		//		public void Step()
		//		{
		//			if ( ! _drawEventRegistered )
		//			{
		//				this.Parent.DrawItem += new StatusBarDrawItemEventHandler(OnDrawItem);
		//				_drawEventRegistered = true;
		//			}
		//
		//			if ( this.IsAnimated )
		//			{
		//				if ( _increasing )
		//				{
		//					ProgressPosition += _stepSize;
		//
		//					if (ProgressPosition >= _endPoint)
		//					{
		//						_increasing = false;
		//					}
		//				}
		//				else
		//				{
		//					ProgressPosition -= _stepSize;
		//
		//					if (ProgressPosition <= _startPoint)
		//					{
		//						_increasing = true;
		//					}
		//				}
		//			}
		//			else if (ProgressPosition < _endPoint)
		//			{
		//				ProgressPosition += _stepSize;
		//			}
		//
		//			this.Parent.Invoke( _refreshDelegate );
		//		}

		#endregion

		private void DrawPanel()
		{
			if (m_bounds.Width == 0)
			{
//TOO SLOW BY FAR!				System.Diagnostics.Debug.Write(".");
				return; //not ready yet
			}

			using (Graphics graphics = this.Parent.CreateGraphics())
			{
				Rectangle eventBounds = m_bounds;

				if (offScreenBmp == null)
				{
					offScreenBmp = new Bitmap(eventBounds.Width,
						eventBounds.Height);
					offScreenDC = Graphics.FromImage(offScreenBmp);
				}

				Rectangle fullBounds = eventBounds;
				fullBounds.X = 0;
				fullBounds.Y = 0;
				offScreenDC.FillRectangle(SystemBrushes.Control, fullBounds);

				//allow it to 'catch up' smoothly
				int pos = ProgressPosition;

				//			if(m_drawPosition > pos)
				m_drawPosition = pos;

				//			if(m_drawPosition < pos)
				//				++m_drawPosition;

				if (m_drawPosition != _startPoint)
				{
					if ((m_drawPosition <= _endPoint) ||
						(this.AnimationStyle == ProgressDisplayStyle.Infinite))
					{
						Rectangle bounds = eventBounds;
						float percent = ((float) m_drawPosition/
										 ((float) _endPoint - (float) _startPoint));

						switch (this.AnimationStyle)
						{

							case ProgressDisplayStyle.LeftToRight:
								{
									bounds.Width = (int) (percent*(float) eventBounds.Width);
									break;
								}
							case ProgressDisplayStyle.RightToLeft:
								{
									bounds.Width = (int) (percent*(float) eventBounds.Width);
									bounds.X += eventBounds.Width - bounds.Width;
									break;
								}
							case ProgressDisplayStyle.BottomToTop:
								{
									bounds.Height = (int) (percent*(float) eventBounds.Height);
									bounds.Y += eventBounds.Height - bounds.Height;
									break;
								}
							case ProgressDisplayStyle.TopToBottom:
								{
									bounds.Height = (int) (percent*(float) eventBounds.Height);
									break;
								}
							case ProgressDisplayStyle.Infinite:
								{
									bounds.Height = (int) (percent*(float) eventBounds.Height);
									bounds.Y += (eventBounds.Height - bounds.Height)/2;
									bounds.Width = (int) (percent*(float) eventBounds.Width);
									bounds.X += (eventBounds.Width - bounds.Width)/2;
									break;
								}
						}

						// draw the progress bar
						bounds.X = 0;
						bounds.Y = 0;
						offScreenDC.FillRectangle(_progressBrush, bounds);
						if (this.ShowText)
						{
							// draw the text on top of the progress bar
							//						offScreenDC.DrawString((percent * 100).ToString(),
							offScreenDC.DrawString(m_stateProvider.Status,
								_textFont, _textBrush, new PointF(0.0F, 0.0F));
						}
					}
				}

				graphics.DrawImage(offScreenBmp, eventBounds.X, eventBounds.Y);
			}
		}

		#region Refresh

		/// <summary>
		/// Refreshes the progress bar
		/// </summary>
		public void Refresh()
		{
			CheckDisposed();

			//this.Parent.Refresh();
			RefreshSafely();
		}

		#endregion

		#region Reset

		/// <summary>
		/// Reinitializes the progress bar
		/// </summary>
		public void Reset()
		{
			CheckDisposed();

			m_drawPosition = 0;
//			StopAnimation();
			//ProgressPosition = _startPoint;

			RefreshSafely();
		}

		private void RefreshSafely()
		{

			if (IsDisposed)
				return;
//			if ( ! _drawEventRegistered )
//			{
//				this.Parent.DrawItem += new StatusBarDrawItemEventHandler(OnDrawItem);
//				_drawEventRegistered = true;
//			}
//
			DrawPanel();
			//this.Parent.Invoke( _refreshDelegate );
		}

		#endregion

		#region Animation

		// <summary>
		// Spawn the progress animation thread
		// </summary>
//		public void StartAnimation()
//		{
//			StopAnimation();
//
//			//			ProgressPosition = 0;
//
//			//			InitializeAnimationThread();
//
//			_animationThread.Start();
//		}
//
//		/// <summary>
//		/// Stop the progress animation thread
//		/// </summary>
//		public void StopAnimation()
//		{
//			if ( _animationThread.IsAlive )
//			{
//				_animationThread.Abort();
//			}
//		}

		/// <summary>
		/// ThreadStart Delegate Handler for infinate progress animation
		/// </summary>
		//		private void AnimationThreadStartCallback()
		//		{
		//			while ( true )
		//			{
		//				this.Step();
		//				Thread.Sleep( _animationTick );
		//			}
		//		}

		//		private void InitializeAnimationThread()
		//		{
		//			_animationThread = new Thread(new ThreadStart( this.AnimationThreadStartCallback ));
		//			_animationThread.IsBackground = true;
		//			_animationThread.Name = "Progress Bar Animation Thread";
		//		}

		#endregion


		private void timer1_Tick(object sender, System.EventArgs e)
		{
			if(this.Parent == null)
				return;

			// don't invoke if parent is yet to be created
			if(!this.Parent.IsHandleCreated)
				return;

//			if(m_stateProvider== null || this.Parent ==null)
//				return;

			if ( ! _drawEventRegistered )
			{
				System.Diagnostics.Debug.WriteLineIf(traceSwitch.TraceInfo, "reg", traceSwitch.DisplayName);
//				this.Parent.DrawItem += new StatusBarDrawItemEventHandler(OnDrawItem);
//				this.Parent.Resize += new EventHandler(Parent_Resize);
				_drawEventRegistered = true;
				this.Parent.Invoke(  new RefreshDelegate( this.Refresh ) );
			}


			RefreshSafely();
		}

		#region Delegates

		private delegate void RefreshDelegate();

		#endregion

// The following method is never used and produces a warning (error) when compiled with Mono.
//		private void Parent_Resize(object sender, EventArgs e)
//		{
//		//	if ( e.Panel == this )
//			{
//				System.Diagnostics.Debug.WriteLineIf(traceSwitch.TraceInfo, "", traceSwitch.DisplayName);
//				System.Diagnostics.Debug.WriteLineIf(traceSwitch.TraceInfo, "xx", traceSwitch.DisplayName);
//			}
//		}
	}

	#region ProgressDisplayStyle

	/// <summary>
	/// Statusbar Progress Display Styles
	/// </summary>
	public enum ProgressDisplayStyle
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

	#endregion


}
