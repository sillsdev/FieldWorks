//From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp

namespace SIL.CoreImpl.MessageBoxEx
{
	/// <summary>
	/// Internal DataStructure used to represent a button
	/// </summary>
	public class MessageBoxExButton
	{
		/// <summary>
		/// Gets or Sets the text of the button
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Gets or Sets the return value when this button is clicked
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Gets or Sets the tooltip that is displayed for this button
		/// </summary>
		public string HelpText { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public MessageBoxExButton()
		{
			Text = null;
			Value = null;
			HelpText = null;
			IsCancelButton = false;
		}

		/// <summary>
		/// Gets or Sets wether this button is a cancel button. i.e. the button
		/// that will be assumed to have been clicked if the user closes the message box
		/// without pressing any button.
		/// </summary>
		public bool IsCancelButton { get; set; }
	}
}
