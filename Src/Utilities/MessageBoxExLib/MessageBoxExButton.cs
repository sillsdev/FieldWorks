//From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp

using System;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	/// Internal DataStructure used to represent a button
	/// </summary>
	public class MessageBoxExButton
	{
		private string _text = null;
		/// <summary>
		/// Gets or Sets the text of the button
		/// </summary>
		public string Text
		{
			get{ return _text; }
			set{ _text = value; }
		}

		private string _value = null;
		/// <summary>
		/// Gets or Sets the return value when this button is clicked
		/// </summary>
		public string Value
		{
			get{ return _value; }
			set{_value = value; }
		}

		private string _helpText = null;
		/// <summary>
		/// Gets or Sets the tooltip that is displayed for this button
		/// </summary>
		public string HelpText
		{
			get{ return _helpText; }
			set{ _helpText = value; }
		}

		private bool _isCancelButton = false;
		/// <summary>
		/// Gets or Sets wether this button is a cancel button. i.e. the button
		/// that will be assumed to have been clicked if the user closes the message box
		/// without pressing any button.
		/// </summary>
		public bool IsCancelButton
		{
			get{ return _isCancelButton; }
			set{ _isCancelButton = value; }
		}
	}
}
