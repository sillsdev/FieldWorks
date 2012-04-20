//From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.Utils;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	/// An advanced MessageBox that supports customizations like Font, Icon,
	/// Buttons and Saved Responses
	/// </summary>
	internal class MessageBoxExForm : Form, IFWDisposable
	{
		#region Constants
		private const int LEFT_PADDING = 12;
		private const int RIGHT_PADDING = 12;
		private const int TOP_PADDING = 12;
		private const int BOTTOM_PADDING = 12;

		private const int BUTTON_LEFT_PADDING = 4;
		private const int BUTTON_RIGHT_PADDING = 4;
		private const int BUTTON_TOP_PADDING = 4;
		private const int BUTTON_BOTTOM_PADDING = 4;

		private const int MIN_BUTTON_HEIGHT = 23;
		private const int MIN_BUTTON_WIDTH = 74;

		private const int ITEM_PADDING = 10;
		private const int ICON_MESSAGE_PADDING = 15;

		private const int BUTTON_PADDING = 5;

		private const int CHECKBOX_WIDTH = 20;

		private const int IMAGE_INDEX_EXCLAMATION = 0;
		private const int IMAGE_INDEX_QUESTION = 1;
		private const int IMAGE_INDEX_STOP = 2;
		private const int IMAGE_INDEX_INFORMATION = 3;
		#endregion

		#region Fields

		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.CheckBox chbSaveResponse;
		private System.Windows.Forms.ImageList imageListIcons;
		private System.Windows.Forms.ToolTip buttonToolTip;

		private List<MessageBoxExButton> _buttons = new List<MessageBoxExButton>();
		private bool _allowSaveResponse;
		private bool _playAlert = true;
		private MessageBoxExButton _cancelButton = null;
		private Button _defaultButtonControl = null;

		private int _maxLayoutWidth;
		//private int _maxLayoutHeight;

		private int _maxWidth;
		private int _maxHeight;

		private bool _allowCancel = true;
		private string _result = null;

		/// <summary>
		/// Used to determine the alert sound to play
		/// </summary>
		private MessageBoxIcon _standardIcon = MessageBoxIcon.None;
		private Icon _iconImage = null;

		private Timer timerTimeout = null;
		private int _timeout = 0;
		private TimeoutResult _timeoutResult = TimeoutResult.Default;
		private System.Windows.Forms.Panel panelIcon;
		private System.Windows.Forms.RichTextBox rtbMessage;

		/// <summary>
		/// Maps MessageBoxEx buttons to Button controls
		/// </summary>
		private Dictionary<MessageBoxExButton, Button> _buttonControlsTable = new Dictionary<MessageBoxExButton, Button>();
		#endregion

		#region Properties
		public string Message
		{
			set
			{
				CheckDisposed();
				rtbMessage.Text = value;
			}
		}

		public string Caption
		{
			set
			{
				CheckDisposed();
				this.Text = value;
			}
		}

		public Font CustomFont
		{
			set
			{
				CheckDisposed();
				this.Font = value;
			}
		}

		public List<MessageBoxExButton> Buttons
		{
			get
			{
				CheckDisposed();
				return _buttons;
			}
		}

		public bool AllowSaveResponse
		{
			get
			{
				CheckDisposed();
				return _allowSaveResponse;
			}
			set
			{
				CheckDisposed();
				_allowSaveResponse = value;
			}
		}

		public bool SaveResponse
		{
			get
			{
				CheckDisposed();
				return chbSaveResponse.Checked;
			}
		}

		public string SaveResponseText
		{
			set
			{
				CheckDisposed();
				chbSaveResponse.Text = value;
			}
		}

		public MessageBoxIcon StandardIcon
		{
			set
			{
				CheckDisposed();
				SetStandardIcon(value);
			}
		}

		public Icon CustomIcon
		{
			set
			{
				CheckDisposed();
				_standardIcon = MessageBoxIcon.None;
				_iconImage = value;
			}
		}

		public MessageBoxExButton CustomCancelButton
		{
			set
			{
				CheckDisposed();
				_cancelButton = value;
			}
		}

		public string Result
		{
			get
			{
				CheckDisposed();
				return _result;
			}
		}

		public bool PlayAlertSound
		{
			get
			{
				CheckDisposed();
				return _playAlert;
			}
			set
			{
				CheckDisposed();
				_playAlert = value;
			}
		}

		public int Timeout
		{
			get
			{
				CheckDisposed();
				return _timeout;
			}
			set
			{
				CheckDisposed();
				_timeout = value;
			}
		}

		public TimeoutResult TimeoutResult
		{
			get
			{
				CheckDisposed();
				return _timeoutResult;
			}
			set
			{
				CheckDisposed();
				_timeoutResult = value;
			}
		}
		#endregion

		#region Ctor/Dtor
		public MessageBoxExForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			_maxWidth = (int)(SystemInformation.WorkingArea.Width * 0.60);
			_maxHeight = (int)(SystemInformation.WorkingArea.Height * 0.90);
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				foreach (var button in _buttonControlsTable.Values)
					button.Dispose();
			}
			components = null;
			base.Dispose(disposing);
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MessageBoxExForm));
			this.panelIcon = new System.Windows.Forms.Panel();
			this.chbSaveResponse = new System.Windows.Forms.CheckBox();
			this.imageListIcons = new System.Windows.Forms.ImageList(this.components);
			this.buttonToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.rtbMessage = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			//
			// panelIcon
			//
			this.panelIcon.BackColor = System.Drawing.Color.Transparent;
			this.panelIcon.Location = new System.Drawing.Point(8, 8);
			this.panelIcon.Name = "panelIcon";
			this.panelIcon.Size = new System.Drawing.Size(32, 32);
			this.panelIcon.TabIndex = 3;
			this.panelIcon.Visible = false;
			//
			// chbSaveResponse
			//
			this.chbSaveResponse.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.chbSaveResponse.Location = new System.Drawing.Point(56, 56);
			this.chbSaveResponse.Name = "chbSaveResponse";
			this.chbSaveResponse.Size = new System.Drawing.Size(104, 16);
			this.chbSaveResponse.TabIndex = 0;
			//
			// imageListIcons
			//
			this.imageListIcons.ColorDepth = System.Windows.Forms.ColorDepth.Depth16Bit;
			this.imageListIcons.ImageSize = new System.Drawing.Size(32, 32);
			this.imageListIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListIcons.ImageStream")));
			this.imageListIcons.TransparentColor = System.Drawing.Color.Transparent;
			//
			// rtbMessage
			//
			this.rtbMessage.BackColor = System.Drawing.SystemColors.Control;
			this.rtbMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.rtbMessage.Location = new System.Drawing.Point(200, 8);
			this.rtbMessage.Name = "rtbMessage";
			this.rtbMessage.ReadOnly = true;
			this.rtbMessage.Size = new System.Drawing.Size(100, 48);
			this.rtbMessage.TabIndex = 4;
			this.rtbMessage.Text = "";
			this.rtbMessage.Visible = false;
			this.rtbMessage.DetectUrls = false;
			//
			// MessageBoxExForm
			//
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(322, 224);
			this.Controls.Add(this.rtbMessage);
			this.Controls.Add(this.chbSaveResponse);
			this.Controls.Add(this.panelIcon);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MessageBoxExForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MessageBoxExForm_MouseMove);
			this.ResumeLayout(false);

		}
		#endregion

		#region Overrides
		/// <summary>
		/// This will get called everytime we call ShowDialog on the form
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			//Reset result
			_result = null;

			this.Size = new Size(_maxWidth, _maxHeight);

			//This is the rectangle in which all items will be layed out
			_maxLayoutWidth =  this.ClientSize.Width - LEFT_PADDING - RIGHT_PADDING;
			//_maxLayoutHeight = this.ClientSize.Height - TOP_PADDING - BOTTOM_PADDING;

			AddOkButtonIfNoButtonsPresent();
			DisableCloseIfMultipleButtonsAndNoCancelButton();

			SetIconSizeAndVisibility();
			SetMessageSizeAndVisibility();
			SetCheckboxSizeAndVisibility();

			SetOptimumSize();

			LayoutControls();

			CenterForm();

			PlayAlert();

			SelectDefaultButton();

			StartTimerIfTimeoutGreaterThanZero();

			base.OnLoad (e);
		}


		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if((int)keyData == (int)(Keys.Alt | Keys.F4) && !_allowCancel)
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}


		protected override void OnClosing(CancelEventArgs e)
		{
			if(_result == null)
			{
				if(_allowCancel)
				{
					_result = _cancelButton.Value;
				}
				else
				{
					e.Cancel = true;
					return;
				}
			}

			if(timerTimeout != null)
			{
				timerTimeout.Stop();
			}

			base.OnClosing (e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint (e);

			if(_iconImage != null)
			{
				e.Graphics.DrawIcon(_iconImage, new Rectangle(panelIcon.Location, new Size(32, 32)));
			}
		}

		#endregion

		#region Methods
		/// <summary>
		/// Measures a string using the Graphics object for this form with
		/// the specified font
		/// </summary>
		/// <param name="str">The string to measure</param>
		/// <param name="maxWidth">The maximum width available to display the string</param>
		/// <param name="font">The font with which to measure the string</param>
		/// <returns></returns>
		private Size MeasureString(string str, int maxWidth, Font font)
		{
			using (Graphics g = this.CreateGraphics())
			{
				SizeF strRectSizeF = g.MeasureString(str, font, maxWidth);

				return new Size((int)Math.Ceiling(strRectSizeF.Width), (int)Math.Ceiling(strRectSizeF.Height));
			}
		}

		/// <summary>
		/// Measures a string using the Graphics object for this form and the
		/// font of this form
		/// </summary>
		/// <param name="str"></param>
		/// <param name="maxWidth"></param>
		/// <returns></returns>
		private Size MeasureString(string str, int maxWidth)
		{
			return MeasureString(str, maxWidth, this.Font);
		}

		/// <summary>
		/// Gets the longest button text
		/// </summary>
		/// <returns></returns>
		private string GetLongestButtonText()
		{
			int maxLen = 0;
			string maxStr = null;
			foreach(MessageBoxExButton button in _buttons)
			{
				if(button.Text != null && button.Text.Length > maxLen)
				{
					maxLen = button.Text.Length;
					maxStr = button.Text;
				}
			}

			return maxStr;
		}

		/// <summary>
		/// Sets the size and visibility of the Message
		/// </summary>
		private void SetMessageSizeAndVisibility()
		{
			if(rtbMessage.Text == null || rtbMessage.Text.Trim().Length == 0)
			{
				rtbMessage.Size = Size.Empty;
				rtbMessage.Visible = false;
			}
			else
			{
				int maxWidth = _maxLayoutWidth;
				if(panelIcon.Size.Width != 0)
				{
					maxWidth = maxWidth - (panelIcon.Size.Width + ICON_MESSAGE_PADDING);
				}

				Size messageRectSize = MeasureString(rtbMessage.Text, maxWidth);
				rtbMessage.Size = messageRectSize;
				rtbMessage.Height = Math.Max(panelIcon.Height, rtbMessage.Height);

				rtbMessage.Visible = true;
			}
		}

		/// <summary>
		/// Sets the size and visibility of the Icon
		/// </summary>
		private void SetIconSizeAndVisibility()
		{
			if(_iconImage == null)
			{
				panelIcon.Visible = false;
				panelIcon.Size = Size.Empty;
			}
			else
			{
				panelIcon.Size = new Size(32,32);
				panelIcon.Visible = true;
			}
		}

		/// <summary>
		/// Sets the size and visibility of the save response checkbox
		/// </summary>
		private void SetCheckboxSizeAndVisibility()
		{
			if(!AllowSaveResponse)
			{
				chbSaveResponse.Visible = false;
				chbSaveResponse.Size = Size.Empty;
			}
			else
			{
				Size saveResponseTextSize = MeasureString(chbSaveResponse.Text, _maxLayoutWidth);
				saveResponseTextSize.Width += CHECKBOX_WIDTH;
				chbSaveResponse.Size = saveResponseTextSize;
				chbSaveResponse.Visible= true;
			}
		}

		/// <summary>
		/// Calculates the button size based on the text of the longest
		/// button text
		/// </summary>
		/// <returns></returns>
		private Size GetButtonSize()
		{
			string longestButtonText = GetLongestButtonText();
			if(longestButtonText == null)
			{
				//TODO:Handle this case
			}

			Size buttonTextSize  = MeasureString(longestButtonText, _maxLayoutWidth);
			Size buttonSize = new Size(buttonTextSize.Width + BUTTON_LEFT_PADDING + BUTTON_RIGHT_PADDING,
				buttonTextSize.Height + BUTTON_TOP_PADDING + BUTTON_BOTTOM_PADDING);

			if(buttonSize.Width < MIN_BUTTON_WIDTH)
				buttonSize.Width = MIN_BUTTON_WIDTH;
			if(buttonSize.Height < MIN_BUTTON_HEIGHT)
				buttonSize.Height = MIN_BUTTON_HEIGHT;

			return buttonSize;
		}

		/// <summary>
		/// Set the icon
		/// </summary>
		/// <param name="icon"></param>
		private void SetStandardIcon(MessageBoxIcon icon)
		{
			_standardIcon = icon;

			switch(icon)
			{
				case MessageBoxIcon.Asterisk:
					_iconImage = SystemIcons.Asterisk;
					break;
				case MessageBoxIcon.Error:
					_iconImage = SystemIcons.Error;
					break;
				case MessageBoxIcon.Exclamation:
					_iconImage = SystemIcons.Exclamation;
					break;
//				case MessageBoxIcon.Hand:
//					_iconImage = SystemIcons.Hand;
//					break;
//				case MessageBoxIcon.Information:
//					_iconImage = SystemIcons.Information;
//					break;
				case MessageBoxIcon.Question:
					_iconImage = SystemIcons.Question;
					break;
//				case MessageBoxIcon.Stop:
//					_iconImage = SystemIcons.Stop;
//					break;
//				case MessageBoxIcon.Warning:
//					_iconImage = SystemIcons.Warning;
//					break;

				case MessageBoxIcon.None:
					_iconImage = null;
					break;
			}
		}

		private void AddOkButtonIfNoButtonsPresent()
		{
			if(_buttons.Count == 0)
			{
				MessageBoxExButton okButton = new MessageBoxExButton();
				okButton.Text = MessageBoxExButtons.OK.ToString();
				okButton.Value = MessageBoxExButtons.OK.ToString();

				_buttons.Add(okButton);
			}
		}


		/// <summary>
		/// Centers the form on the screen
		/// </summary>
		private void CenterForm()
		{
			int x = (SystemInformation.WorkingArea.Width - this.Width ) / 2;
			int y = (SystemInformation.WorkingArea.Height - this.Height ) / 2;

			this.Location = new Point(x,y);
		}

		/// <summary>
		/// Sets the optimum size for the form based on the controls that
		/// need to be displayed
		/// </summary>
		private void SetOptimumSize()
		{
			int ncWidth = this.Width - this.ClientSize.Width;
			int ncHeight = this.Height - this.ClientSize.Height;

			int iconAndMessageRowWidth = rtbMessage.Width + ICON_MESSAGE_PADDING + panelIcon.Width;
			int saveResponseRowWidth = chbSaveResponse.Width + (int)(panelIcon.Width / 2);
			int buttonsRowWidth = GetWidthOfAllButtons();
			int captionWidth = GetCaptionSize().Width;

			int maxItemWidth = Math.Max(saveResponseRowWidth, Math.Max(iconAndMessageRowWidth, buttonsRowWidth));

			int requiredWidth = LEFT_PADDING + maxItemWidth + RIGHT_PADDING + ncWidth;
			//Since Caption width is not client width, we do the check here
			if(requiredWidth < captionWidth)
				requiredWidth = captionWidth;

			int requiredHeight = TOP_PADDING + Math.Max(rtbMessage.Height,panelIcon.Height) + ITEM_PADDING + chbSaveResponse.Height + ITEM_PADDING + GetButtonSize().Height + BOTTOM_PADDING + ncHeight;

			//Fix the bug where if the message text is huge then the buttons are overwritten.
			//Incase the required height is more than the max height then adjust that in the
			//message height
			if(requiredHeight > _maxHeight)
			{
				rtbMessage.Height -= requiredHeight - _maxHeight;
			}

			int height = Math.Min(requiredHeight, _maxHeight);
			int width = Math.Min(requiredWidth, _maxWidth);
			this.Size = new Size(width, height);
		}

		/// <summary>
		/// Returns the width that will be occupied by all buttons including
		/// the inter-button padding
		/// </summary>
		private int GetWidthOfAllButtons()
		{
			Size buttonSize = GetButtonSize();
			int allButtonsWidth = buttonSize.Width*_buttons.Count + BUTTON_PADDING*(_buttons.Count-1);

			return allButtonsWidth;
		}

		/// <summary>
		/// Gets the width of the caption
		/// </summary>
		private Size GetCaptionSize()
		{
			using (Font captionFont = Win32.GetCaptionFont()
				//some error occured while determining system font
				?? new Font("Tahoma",11))
			{
				int availableWidth = _maxWidth - SystemInformation.CaptionButtonSize.Width - SystemInformation.Border3DSize.Width * 2 ;
				Size captionSize = MeasureString(this.Text, availableWidth, captionFont);

				captionSize.Width += SystemInformation.CaptionButtonSize.Width + SystemInformation.Border3DSize.Width * 2;
				return captionSize;
			}
		}

		/// <summary>
		/// Layout all the controls
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="buttonCtrl is added to _buttonControlsTable and disposed in Dispose()")]
		private void LayoutControls()
		{
			panelIcon.Location = new Point(LEFT_PADDING, TOP_PADDING);
			rtbMessage.Location = new Point(LEFT_PADDING + panelIcon.Width + ICON_MESSAGE_PADDING * (panelIcon.Width == 0 ? 0 : 1) , TOP_PADDING);

			chbSaveResponse.Location = new Point(LEFT_PADDING + (int)(panelIcon.Width / 2),
				TOP_PADDING + Math.Max(panelIcon.Height, rtbMessage.Height) + ITEM_PADDING);

			Size buttonSize = GetButtonSize();
			int allButtonsWidth = GetWidthOfAllButtons();

			int firstButtonX = ((int)(this.ClientSize.Width - allButtonsWidth) / 2);
			int firstButtonY = this.ClientSize.Height - BOTTOM_PADDING - buttonSize.Height;
			Point nextButtonLocation = new Point(firstButtonX,firstButtonY);

			bool foundDefaultButton = false;
			foreach(MessageBoxExButton button in _buttons)
			{
				Button buttonCtrl = GetButton(button, buttonSize, nextButtonLocation);

				if(!foundDefaultButton)
				{
					_defaultButtonControl = buttonCtrl;
					foundDefaultButton = true;
				}

				nextButtonLocation.X += buttonSize.Width + BUTTON_PADDING;
			}

			this.ShowInTaskbar = true;//hatton added
		}

		/// <summary>
		/// Gets the button control for the specified MessageBoxExButton, if the
		/// control has not been created this method creates the control
		/// </summary>
		/// <param name="button"></param>
		/// <param name="size"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		private Button GetButton(MessageBoxExButton button, Size size, Point location)
		{
			Button buttonCtrl = null;
			if(_buttonControlsTable.ContainsKey(button))
			{
				buttonCtrl  = _buttonControlsTable[button];
				buttonCtrl.Size = size;
				buttonCtrl.Location = location;
			}
			else
			{
				buttonCtrl = CreateButton(button, size, location);
				_buttonControlsTable[button] = buttonCtrl;
				this.Controls.Add(buttonCtrl);
			}

			return buttonCtrl;
		}

		/// <summary>
		/// Creates a button control based on info from MessageBoxExButton
		/// </summary>
		/// <param name="button"></param>
		/// <param name="size"></param>
		/// <param name="location"></param>
		/// <returns></returns>
		private Button CreateButton(MessageBoxExButton button, Size size, Point location)
		{
			Button buttonCtrl = new Button();
			buttonCtrl.Size = size;
			buttonCtrl.Text = button.Text;
			buttonCtrl.TextAlign = ContentAlignment.MiddleCenter;
			buttonCtrl.FlatStyle = FlatStyle.System;
			if(button.HelpText != null && button.HelpText.Trim().Length != 0)
			{
				buttonToolTip.SetToolTip(buttonCtrl, button.HelpText);
			}
			buttonCtrl.Location = location;
			buttonCtrl.Click += new EventHandler(OnButtonClicked);
			buttonCtrl.MouseHover += new EventHandler(buttonCtrl_MouseHover);
			buttonCtrl.Tag = button.Value;
			buttonCtrl.Name=buttonCtrl.Text;//for nunitforms support
			return buttonCtrl;
		}

		private void DisableCloseIfMultipleButtonsAndNoCancelButton()
		{
			if(_buttons.Count > 1)
			{
				if(_cancelButton != null)
					return;

				//See if standard cancel button is present
				foreach(MessageBoxExButton button in _buttons)
				{
					if(button.Text == MessageBoxExButtons.Cancel.ToString() && button.Value == MessageBoxExButtons.Cancel.ToString())
					{
						_cancelButton = button;
						return;
					}
				}

				//Standard cancel button is not present, Disable
				//close button
				DisableCloseButton(this);
				_allowCancel = false;

			}
			else if(_buttons.Count == 1)
			{
				_cancelButton = _buttons[0];
			}
			else
			{
				//This condition should never get called
				_allowCancel = false;
			}
		}

		/// <summary>
		/// Plays the alert sound based on the icon set for the message box
		/// </summary>
		private void PlayAlert()
		{
			if(_playAlert)
			{
				if(_standardIcon != MessageBoxIcon.None)
				{
					MessageBeep((uint)_standardIcon);
				}
				else
				{
					MessageBeep(0 /*MB_OK*/);
				}
			}
		}

		private void SelectDefaultButton()
		{
			if(_defaultButtonControl != null)
			{
				_defaultButtonControl.Select();
			}
		}

		private void StartTimerIfTimeoutGreaterThanZero()
		{
			if(_timeout > 0)
			{
				if(timerTimeout == null)
				{
					timerTimeout = new Timer(this.components);
					timerTimeout.Tick += new EventHandler(timerTimeout_Tick);
				}

				if(!timerTimeout.Enabled)
				{
					timerTimeout.Interval = _timeout;
					timerTimeout.Start();
				}
			}
		}

		private void SetResultAndClose(string result)
		{
			_result = result;
			this.DialogResult = DialogResult.OK;
		}

		#endregion

		#region Event Handlers
		private void OnButtonClicked(object sender, EventArgs e)
		{
			Button btn = sender as Button;
			if(btn == null || btn.Tag == null)
				return;

			string result = btn.Tag as string;
			SetResultAndClose(result);
		}

		private void timerTimeout_Tick(object sender, EventArgs e)
		{
			timerTimeout.Stop();

			switch(_timeoutResult)
			{
				case TimeoutResult.Default:
					_defaultButtonControl.PerformClick();
					break;

				case TimeoutResult.Cancel:
					if(_cancelButton != null)
					{
						SetResultAndClose(_cancelButton.Value);
					}
					else
					{
						_defaultButtonControl.PerformClick();
					}
					break;

				case TimeoutResult.Timeout:
					SetResultAndClose(MessageBoxExResult.Timeout);
					break;
			}
		}
		#endregion

		#region P/Invoke - SystemParametersInfo, GetSystemMenu, EnableMenuItem, MessageBeep
		private const int LF_FACESIZE = 32;

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem,
		uint uEnable);

		private const int SC_CLOSE  = 0xF060;
		private const int MF_BYCOMMAND  = 0x0;
		private const int MF_GRAYED  = 0x1;
		private const int MF_ENABLED  = 0x0;

		private void DisableCloseButton(Form form)
		{
			try
			{
				EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
			}
			catch(Exception /*ex*/)
			{
				//System.Console.WriteLine(ex.Message);
			}
		}

#if !__MonoCS__
		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern bool MessageBeep(uint type);
#else
		private static bool MessageBeep(uint type)
		{
			Console.WriteLine("Warning using unimplemented method MessageBeep");
			// TODO-Linux: make a beep somehow using managed code
			return true;
		}
#endif
		#endregion

		private void MessageBoxExForm_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			System.Console.WriteLine("Move" + DateTime.Now.Millisecond);
		}

		private void buttonCtrl_MouseHover(object sender, EventArgs e)
		{
			System.Console.WriteLine("Move b" + DateTime.Now.Millisecond);
		}
	}
}
