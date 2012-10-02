//From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	/// An extended MessageBox with lot of customizing capabilities.
	/// </summary>
	public class MessageBoxEx
	{
		#region Fields
		private MessageBoxExForm _msgBox = new MessageBoxExForm();

		private bool _useSavedResponse = true;
		private string _name = null;
		#endregion

		#region Properties
		internal string Name
		{
			get{ return _name; }
			set{ _name = value; }
		}

		/// <summary>
		/// Sets the caption of the message box
		/// </summary>
		public string Caption
		{
			set{_msgBox.Caption = value;}
		}

		/// <summary>
		/// Sets the text of the message box
		/// </summary>
		public string Text
		{
			set{_msgBox.Message = value;}
		}

		/// <summary>
		/// Sets the icon to show in the message box
		/// </summary>
		public Icon CustomIcon
		{
			set{_msgBox.CustomIcon = value;}
		}

		/// <summary>
		/// Sets the icon to show in the message box
		/// </summary>
		public MessageBoxExIcon Icon
		{
			set{ _msgBox.StandardIcon = (MessageBoxIcon)Enum.Parse(typeof(MessageBoxIcon), value.ToString());}
		}

		/// <summary>
		/// Sets the font for the text of the message box
		/// </summary>
		public Font Font
		{
			set{_msgBox.Font = value;}
		}

		/// <summary>
		/// Sets or Gets the ability of the  user to save his/her response
		/// </summary>
		public bool AllowSaveResponse
		{
			get{ return _msgBox.AllowSaveResponse; }
			set{ _msgBox.AllowSaveResponse = value; }
		}

		/// <summary>
		/// Sets the text to show to the user when saving his/her response
		/// </summary>
		public string SaveResponseText
		{
			set{_msgBox.SaveResponseText = value; }
		}

		/// <summary>
		/// Sets or Gets wether the saved response if available should be used
		/// </summary>
		public bool UseSavedResponse
		{
			get{ return _useSavedResponse; }
			set{ _useSavedResponse = value; }
		}

		/// <summary>
		/// Sets or Gets wether an alert sound is played while showing the message box.
		/// The sound played depends on the the Icon selected for the message box
		/// </summary>
		public bool PlayAlsertSound
		{
			get{ return _msgBox.PlayAlertSound; }
			set{ _msgBox.PlayAlertSound = value; }
		}

		/// <summary>
		/// Sets or Gets the time in milliseconds for which the message box is displayed.
		/// </summary>
		public int Timeout
		{
			get{ return _msgBox.Timeout; }
			set{ _msgBox.Timeout = value; }
		}

		/// <summary>
		/// Controls the result that will be returned when the message box times out.
		/// </summary>
		public TimeoutResult TimeoutResult
		{
			get{ return _msgBox.TimeoutResult; }
			set{ _msgBox.TimeoutResult = value; }
		}
		#endregion

		#region Methods
		/// <summary>
		/// Shows the message box
		/// </summary>
		/// <returns></returns>
		public string Show()
		{
			return Show(null);
		}

		/// <summary>
		/// Shows the messsage box with the specified owner
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public string Show(IWin32Window owner)
		{
			if(_useSavedResponse && this.Name != null)
			{
				string savedResponse = MessageBoxExManager.GetSavedResponse(this);
				if( savedResponse != null)
					return savedResponse;
			}

			if(owner == null)
			{
				_msgBox.Name = this._name;//needed for nunitforms support
				_msgBox.ShowDialog();
			}
			else
			{
				_msgBox.ShowDialog(owner);
			}

			if(this.Name != null)
			{
				if(_msgBox.AllowSaveResponse && _msgBox.SaveResponse)
					MessageBoxExManager.SetSavedResponse(this, _msgBox.Result);
				else
					MessageBoxExManager.ResetSavedResponse(this.Name);
			}
			else
			{
				Dispose();
			}

			return _msgBox.Result;
		}

		/// <summary>
		/// Add a custom button to the message box
		/// </summary>
		/// <param name="button">The button to add</param>
		public void AddButton(MessageBoxExButton button)
		{
			if(button == null)
				throw new ArgumentNullException("button","A null button cannot be added");

			_msgBox.Buttons.Add(button);

			if(button.IsCancelButton)
			{
				_msgBox.CustomCancelButton = button;
			}
		}

		/// <summary>
		/// Add a custom button to the message box
		/// </summary>
		/// <param name="text">The text of the button</param>
		/// <param name="val">The return value in case this button is clicked</param>
		public void AddButton(string text, string val)
		{
			if(text == null)
				throw new ArgumentNullException("text","Text of a button cannot be null");

			if(val == null)
				throw new ArgumentNullException("val","Value of a button cannot be null");

			MessageBoxExButton button = new MessageBoxExButton();
			button.Text = text;
			button.Value = val;

			AddButton(button);
		}

		/// <summary>
		/// Add a standard button to the message box
		/// </summary>
		/// <param name="buttons">The standard button to add</param>
		public void AddButton(MessageBoxExButtons button)
		{
			string buttonText = MessageBoxExManager.GetLocalizedString(button.ToString());
			if(buttonText == null)
			{
				buttonText = button.ToString();
			}

			string buttonVal = button.ToString();

			MessageBoxExButton btn = new MessageBoxExButton();
			btn.Text = buttonText;
			btn.Value = buttonVal;

			if(button == MessageBoxExButtons.Cancel)
			{
				btn.IsCancelButton = true;
			}

			AddButton(btn);
		}

		/// <summary>
		/// Add standard buttons to the message box.
		/// </summary>
		/// <param name="buttons">The standard buttons to add</param>
		public void AddButtons(MessageBoxButtons buttons)
		{
			switch(buttons)
			{
				case MessageBoxButtons.OK:
					AddButton(MessageBoxExButtons.OK);
					break;

				case MessageBoxButtons.AbortRetryIgnore:
					AddButton(MessageBoxExButtons.Abort);
					AddButton(MessageBoxExButtons.Retry);
					AddButton(MessageBoxExButtons.Ignore);
					break;

				case MessageBoxButtons.OKCancel:
					AddButton(MessageBoxExButtons.OK);
					AddButton(MessageBoxExButtons.Cancel);
					break;

				case MessageBoxButtons.RetryCancel:
					AddButton(MessageBoxExButtons.Retry);
					AddButton(MessageBoxExButtons.Cancel);
					break;

				case MessageBoxButtons.YesNo:
					AddButton(MessageBoxExButtons.Yes);
					AddButton(MessageBoxExButtons.No);
					break;

				case MessageBoxButtons.YesNoCancel:
					AddButton(MessageBoxExButtons.Yes);
					AddButton(MessageBoxExButtons.No);
					AddButton(MessageBoxExButtons.Cancel);
					break;
			}
		}
		#endregion

		#region Ctor
		/// <summary>
		/// Ctor is internal because this can only be created by MBManager
		/// </summary>
		internal MessageBoxEx()
		{
		}

		/// <summary>
		/// Called by the manager when it is disposed
		/// </summary>
		internal void Dispose()
		{
			if(_msgBox != null)
			{
				_msgBox.Dispose();
			}
		}
		#endregion
	}
}
