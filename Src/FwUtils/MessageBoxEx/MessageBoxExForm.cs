// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
// From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils.MessageBoxEx
{
	/// <summary>
	/// An advanced MessageBox that supports customizations like Font, Icon,
	/// Buttons and Saved Responses
	/// </summary>
	internal sealed class MessageBoxExForm : Form
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
		#endregion

		#region Fields

		private IContainer components;
		private CheckBox chbSaveResponse;
		private ImageList imageListIcons;
		private ToolTip buttonToolTip;
		private MessageBoxExButton _cancelButton;
		private Button _defaultButtonControl;
		private int _maxLayoutWidth;
		private int _maxWidth;
		private int _maxHeight;
		private bool _allowCancel = true;

		/// <summary>
		/// Used to determine the alert sound to play
		/// </summary>
		private MessageBoxIcon _standardIcon = MessageBoxIcon.None;
		private Icon _iconImage;
		private Timer timerTimeout;
		private Panel panelIcon;
		private RichTextBox rtbMessage;

		/// <summary>
		/// Maps MessageBoxEx buttons to Button controls
		/// </summary>
		private Dictionary<MessageBoxExButton, Button> _buttonControlsTable = new Dictionary<MessageBoxExButton, Button>();
		#endregion

		#region Properties
		/// <summary>
		/// Set the message box message.
		/// </summary>
		public string Message
		{
			set
			{
				rtbMessage.Text = value;
			}
		}

		/// <summary>
		/// Set the message box caption.
		/// </summary>
		public string Caption
		{
			set
			{
				Text = value;
			}
		}

		/// <summary>
		/// Get the message box buttons.
		/// </summary>
		public List<MessageBoxExButton> Buttons { get; } = new List<MessageBoxExButton>();

		/// <summary>
		/// Get/Set if message box allows a save response.
		/// </summary>
		public bool AllowSaveResponse { get; set; }

		/// <summary>
		/// Get value of save response check box.
		/// </summary>
		public bool SaveResponse => chbSaveResponse.Checked;

		/// <summary>
		/// Set the save response text.
		/// </summary>
		public string SaveResponseText
		{
			set
			{
				chbSaveResponse.Text = value;
			}
		}

		/// <summary>
		/// Set the standard icon.
		/// </summary>
		public MessageBoxIcon StandardIcon
		{
			set
			{
				SetStandardIcon(value);
			}
		}

		/// <summary>
		/// Set a custom icon.
		/// </summary>
		public Icon CustomIcon
		{
			set
			{
				_standardIcon = MessageBoxIcon.None;
				_iconImage = value;
			}
		}

		/// <summary>
		/// Set the custom cancel button.
		/// </summary>
		public MessageBoxExButton CustomCancelButton
		{
			set
			{
				_cancelButton = value;
			}
		}

		/// <summary>
		/// Get the result.
		/// </summary>
		public string Result { get; private set; }

		/// <summary>
		/// Set/Set value to play an alert sound.
		/// </summary>
		public bool PlayAlertSound { get; set; } = true;

		/// <summary>
		/// Set/Set a timeout value.
		/// </summary>
		public int Timeout { get; set; } = 0;

		/// <summary>
		/// Get/Set a timeout result.
		/// </summary>
		public TimeoutResult TimeoutResult { get; set; } = TimeoutResult.Default;

		/// <summary>
		/// Gets the longest button text
		/// </summary>
		private string LongestButtonText
		{
			get
			{
				var maxLen = 0;
				string maxStr = null;
				foreach (var button in Buttons)
				{
					if (button.Text != null && button.Text.Length > maxLen)
					{
						maxLen = button.Text.Length;
						maxStr = button.Text;
					}
				}

				return maxStr;
			}
		}

		/// <summary>
		/// Calculates the button size based on the text of the longest
		/// button text
		/// </summary>
		private Size ButtonSize
		{
			get
			{
				var longestButtonText = LongestButtonText;
				if (longestButtonText == null)
				{
					//TODO:Handle this case
				}
				var buttonTextSize = MeasureString(longestButtonText, _maxLayoutWidth);
				var buttonSize = new Size(buttonTextSize.Width + BUTTON_LEFT_PADDING + BUTTON_RIGHT_PADDING, buttonTextSize.Height + BUTTON_TOP_PADDING + BUTTON_BOTTOM_PADDING);

				if (buttonSize.Width < MIN_BUTTON_WIDTH)
				{
					buttonSize.Width = MIN_BUTTON_WIDTH;
				}
				if (buttonSize.Height < MIN_BUTTON_HEIGHT)
				{
					buttonSize.Height = MIN_BUTTON_HEIGHT;
				}

				return buttonSize;
			}
		}

		/// <summary>
		/// Returns the width that will be occupied by all buttons including
		/// the inter-button padding
		/// </summary>
		private int WidthOfAllButtons => ButtonSize.Width * Buttons.Count + BUTTON_PADDING * (Buttons.Count - 1);

		/// <summary>
		/// Gets the width of the caption
		/// </summary>
		private Size CaptionSize
		{
			get
			{
				using (var captionFont = Win32.GetCaptionFont() ?? new Font("Tahoma", 11) /* some error occured while determining system font */)
				{
					var availableWidth = _maxWidth - SystemInformation.CaptionButtonSize.Width - SystemInformation.Border3DSize.Width * 2;
					var captionSize = MeasureString(this.Text, availableWidth, captionFont);
					captionSize.Width += SystemInformation.CaptionButtonSize.Width + SystemInformation.Border3DSize.Width * 2;
					return captionSize;
				}
			}
		}
		#endregion

		#region Ctor/Dtor

		/// <summary />
		public MessageBoxExForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			_maxWidth = (int)(SystemInformation.WorkingArea.Width * 0.60);
			_maxHeight = (int)(SystemInformation.WorkingArea.Height * 0.90);

#if DEBUG
			MouseMove += MessageBoxExForm_MouseMove;
#endif
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
#if DEBUG
				MouseMove -= MessageBoxExForm_MouseMove;
#endif
				foreach (var button in _buttonControlsTable.Values)
				{
#if DEBUG
					button.MouseHover -= buttonCtrl_MouseHover;
#endif
					button.Dispose();
				}
			}
			components = null;
			_buttonControlsTable = null;

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
			this.ResumeLayout(false);

		}
		#endregion

		#region Overrides

		/// <inheritdoc />
		protected override void OnLoad(EventArgs e)
		{
			//Reset result
			Result = null;

			Size = new Size(_maxWidth, _maxHeight);

			//This is the rectangle in which all items will be laid out
			_maxLayoutWidth = ClientSize.Width - LEFT_PADDING - RIGHT_PADDING;

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

			base.OnLoad(e);
		}

		/// <inheritdoc />
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if ((int)keyData == (int)(Keys.Alt | Keys.F4) && !_allowCancel)
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		/// <inheritdoc />
		protected override void OnClosing(CancelEventArgs e)
		{
			if (Result == null)
			{
				if (_allowCancel)
				{
					Result = _cancelButton.Value;
				}
				else
				{
					e.Cancel = true;
					return;
				}
			}

			timerTimeout?.Stop();

			base.OnClosing(e);
		}

		/// <inheritdoc />
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (_iconImage != null)
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
			using (var g = this.CreateGraphics())
			{
				var strRectSizeF = g.MeasureString(str, font, maxWidth);

				return new Size((int)Math.Ceiling(strRectSizeF.Width), (int)Math.Ceiling(strRectSizeF.Height));
			}
		}

		/// <summary>
		/// Measures a string using the Graphics object for this form and the
		/// font of this form
		/// </summary>
		private Size MeasureString(string str, int maxWidth)
		{
			return MeasureString(str, maxWidth, Font);
		}

		/// <summary>
		/// Sets the size and visibility of the Message
		/// </summary>
		private void SetMessageSizeAndVisibility()
		{
			if (rtbMessage.Text == null || rtbMessage.Text.Trim().Length == 0)
			{
				rtbMessage.Size = Size.Empty;
				rtbMessage.Visible = false;
			}
			else
			{
				int maxWidth = _maxLayoutWidth;
				if (panelIcon.Size.Width != 0)
				{
					maxWidth = maxWidth - (panelIcon.Size.Width + ICON_MESSAGE_PADDING);
				}

				var messageRectSize = MeasureString(rtbMessage.Text, maxWidth);
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
			if (_iconImage == null)
			{
				panelIcon.Visible = false;
				panelIcon.Size = Size.Empty;
			}
			else
			{
				panelIcon.Size = new Size(32, 32);
				panelIcon.Visible = true;
			}
		}

		/// <summary>
		/// Sets the size and visibility of the save response checkbox
		/// </summary>
		private void SetCheckboxSizeAndVisibility()
		{
			if (!AllowSaveResponse)
			{
				chbSaveResponse.Visible = false;
				chbSaveResponse.Size = Size.Empty;
			}
			else
			{
				var saveResponseTextSize = MeasureString(chbSaveResponse.Text, _maxLayoutWidth);
				saveResponseTextSize.Width += CHECKBOX_WIDTH;
				chbSaveResponse.Size = saveResponseTextSize;
				chbSaveResponse.Visible = true;
			}
		}

		/// <summary>
		/// Set the icon
		/// </summary>
		private void SetStandardIcon(MessageBoxIcon icon)
		{
			_standardIcon = icon;

			switch (icon)
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
				case MessageBoxIcon.Question:
					_iconImage = SystemIcons.Question;
					break;
				case MessageBoxIcon.None:
					_iconImage = null;
					break;
			}
		}

		private void AddOkButtonIfNoButtonsPresent()
		{
			if (Buttons.Count == 0)
			{
				var okButton = new MessageBoxExButton
				{
					Text = MessageBoxExButtons.OK.ToString(),
					Value = MessageBoxExButtons.OK.ToString()
				};

				Buttons.Add(okButton);
			}
		}


		/// <summary>
		/// Centers the form on the screen
		/// </summary>
		private void CenterForm()
		{
			var x = (SystemInformation.WorkingArea.Width - Width) / 2;
			var y = (SystemInformation.WorkingArea.Height - Height) / 2;
			Location = new Point(x, y);
		}

		/// <summary>
		/// Sets the optimum size for the form based on the controls that
		/// need to be displayed
		/// </summary>
		private void SetOptimumSize()
		{
			var ncWidth = Width - ClientSize.Width;
			var ncHeight = Height - ClientSize.Height;
			var iconAndMessageRowWidth = rtbMessage.Width + ICON_MESSAGE_PADDING + panelIcon.Width;
			var saveResponseRowWidth = chbSaveResponse.Width + panelIcon.Width / 2;
			var buttonsRowWidth = WidthOfAllButtons;
			var captionWidth = CaptionSize.Width;
			var maxItemWidth = Math.Max(saveResponseRowWidth, Math.Max(iconAndMessageRowWidth, buttonsRowWidth));
			var requiredWidth = LEFT_PADDING + maxItemWidth + RIGHT_PADDING + ncWidth;
			//Since Caption width is not client width, we do the check here
			if (requiredWidth < captionWidth)
			{
				requiredWidth = captionWidth;
			}
			var requiredHeight = TOP_PADDING + Math.Max(rtbMessage.Height, panelIcon.Height) + ITEM_PADDING + chbSaveResponse.Height + ITEM_PADDING + ButtonSize.Height + BOTTOM_PADDING + ncHeight;

			//Fix the bug where if the message text is huge then the buttons are overwritten.
			//In case the required height is more than the max height then adjust that in the
			//message height
			if (requiredHeight > _maxHeight)
			{
				rtbMessage.Height -= requiredHeight - _maxHeight;
			}
			var height = Math.Min(requiredHeight, _maxHeight);
			var width = Math.Min(requiredWidth, _maxWidth);
			Size = new Size(width, height);
		}

		/// <summary>
		/// Layout all the controls
		/// </summary>
		private void LayoutControls()
		{
			panelIcon.Location = new Point(LEFT_PADDING, TOP_PADDING);
			rtbMessage.Location = new Point(LEFT_PADDING + panelIcon.Width + ICON_MESSAGE_PADDING * (panelIcon.Width == 0 ? 0 : 1), TOP_PADDING);

			chbSaveResponse.Location = new Point(LEFT_PADDING + panelIcon.Width / 2, TOP_PADDING + Math.Max(panelIcon.Height, rtbMessage.Height) + ITEM_PADDING);

			var buttonSize = ButtonSize;
			var allButtonsWidth = WidthOfAllButtons;

			var firstButtonX = ((ClientSize.Width - allButtonsWidth) / 2);
			var firstButtonY = ClientSize.Height - BOTTOM_PADDING - buttonSize.Height;
			var nextButtonLocation = new Point(firstButtonX, firstButtonY);

			var foundDefaultButton = false;
			foreach (var button in Buttons)
			{
				var buttonCtrl = GetButton(button, buttonSize, nextButtonLocation);
				if (!foundDefaultButton)
				{
					_defaultButtonControl = buttonCtrl;
					foundDefaultButton = true;
				}

				nextButtonLocation.X += buttonSize.Width + BUTTON_PADDING;
			}

			ShowInTaskbar = true;//hatton added
		}

		/// <summary>
		/// Gets the button control for the specified MessageBoxExButton, if the
		/// control has not been created this method creates the control
		/// </summary>
		private Button GetButton(MessageBoxExButton button, Size size, Point location)
		{
			Button buttonCtrl;
			if (_buttonControlsTable.ContainsKey(button))
			{
				buttonCtrl = _buttonControlsTable[button];
				buttonCtrl.Size = size;
				buttonCtrl.Location = location;
			}
			else
			{
				buttonCtrl = CreateButton(button, size, location);
				_buttonControlsTable[button] = buttonCtrl;
				Controls.Add(buttonCtrl);
			}

			return buttonCtrl;
		}

		/// <summary>
		/// Creates a button control based on info from MessageBoxExButton
		/// </summary>
		private Button CreateButton(MessageBoxExButton button, Size size, Point location)
		{
			var buttonCtrl = new Button
			{
				Size = size,
				Text = button.Text,
				TextAlign = ContentAlignment.MiddleCenter,
				FlatStyle = FlatStyle.System,
				Location = location,
				Tag = button.Value,
				Name = Text // for nunitforms support
			};
			if (button.HelpText != null && button.HelpText.Trim().Length != 0)
			{
				buttonToolTip.SetToolTip(buttonCtrl, button.HelpText);
			}
			buttonCtrl.Click += OnButtonClicked;
#if DEBUG
			buttonCtrl.MouseHover += buttonCtrl_MouseHover;
#endif
			return buttonCtrl;
		}

		private void DisableCloseIfMultipleButtonsAndNoCancelButton()
		{
			if (Buttons.Count > 1)
			{
				if (_cancelButton != null)
				{
					return;
				}
				//See if standard cancel button is present
				foreach (var button in Buttons)
				{
					if (button.Text == MessageBoxExButtons.Cancel.ToString() && button.Value == MessageBoxExButtons.Cancel.ToString())
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
			else if (Buttons.Count == 1)
			{
				_cancelButton = Buttons[0];
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
			if (PlayAlertSound)
			{
				if (_standardIcon != MessageBoxIcon.None)
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
			_defaultButtonControl?.Select();
		}

		private void StartTimerIfTimeoutGreaterThanZero()
		{
			if (Timeout > 0)
			{
				if (timerTimeout == null)
				{
					timerTimeout = new Timer(components);
					timerTimeout.Tick += timerTimeout_Tick;
				}

				if (!timerTimeout.Enabled)
				{
					timerTimeout.Interval = Timeout;
					timerTimeout.Start();
				}
			}
		}

		private void SetResultAndClose(string result)
		{
			Result = result;
			DialogResult = DialogResult.OK;
		}

		#endregion

		#region Event Handlers
		private void OnButtonClicked(object sender, EventArgs e)
		{
			var btn = sender as Button;
			if (btn?.Tag == null)
			{
				return;
			}
			var result = btn.Tag as string;
			SetResultAndClose(result);
		}

		private void timerTimeout_Tick(object sender, EventArgs e)
		{
			timerTimeout.Stop();

			switch (TimeoutResult)
			{
				case TimeoutResult.Default:
					_defaultButtonControl.PerformClick();
					break;

				case TimeoutResult.Cancel:
					if (_cancelButton != null)
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

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem,
		uint uEnable);

		private const int SC_CLOSE = 0xF060;
		private const int MF_BYCOMMAND = 0x0;
		private const int MF_GRAYED = 0x1;

		private static void DisableCloseButton(Form form)
		{
			try
			{
				EnableMenuItem(GetSystemMenu(form.Handle, false), SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
			}
			catch (Exception /*ex*/)
			{
				//System.Console.WriteLine(ex.Message);
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "MessageBeep")]
		private static extern bool MessageBeepWindows(uint type);

		private static void MessageBeep(uint type)
		{
			if (Platform.IsWindows)
			{
				MessageBeepWindows(type);
			}
		}
		#endregion

#if DEBUG
		private void MessageBoxExForm_MouseMove(object sender, MouseEventArgs e)
		{
			Console.WriteLine("Move" + DateTime.Now.Millisecond);
		}

		private void buttonCtrl_MouseHover(object sender, EventArgs e)
		{
			Console.WriteLine("Move b" + DateTime.Now.Millisecond);
		}
#endif
	}
}