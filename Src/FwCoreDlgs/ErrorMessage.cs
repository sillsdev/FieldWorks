using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Handles error messages and how to present them to the user via GUI
	/// </summary>
	public class ErrorMessageHandler
	{
		#region Constructors

		/// <summary>
		/// Constructs a new ErrorMessageHandler (should only have one instance per dialog)
		/// </summary>
		/// <param name="labelAssociation">Dictionary that contains your
		/// textbox as a key and the associated error label</param>
		/// <param name="enabledControl">Contains the control that we want to enable/disable
		/// based on whether we have errors present.</param>
		public ErrorMessageHandler(Dictionary<TextBox, Label> labelAssociation, Control enabledControl)
		{
			this.labelAssociation = labelAssociation;
			m_enabledControl = enabledControl;
		}

		#endregion

		#region Member Variables

		/// <summary>
		/// Contains a list of all the possible error messages our dialog box can have
		/// </summary>
		public enum ErrorMessage
		{
			/// <summary>No error</summary>
			none,
			/// <summary> Codepoint is too short in length (3&lt;length&lt;7) </summary>
			shortCodepoint,
			/// <summary> A name must be provided </summary>
			emptyName,
			/// <summary>Numeric digits may not be negative</summary>
			numericDashDigit,
			/// <summary>Numeric digits may not be fractions</summary>
			numericSlashDigit,
			/// <summary>Numeric digits may not contain the '.'</summary>
			numericDotDigit,
			/// <summary>The fraction is malformed</summary>
			numericMalformedFraction,
			/// <summary>The codepoint is outside the PUA range</summary>
			outsidePua,
			/// <summary>The codepoint is within the Surrogate range</summary>
			inSurrogateRange,
			/// <summary>Zero is not a valid code point</summary>
			zeroCodepoint,
			/// <summary>Codepoint length must be 4-6 digits</summary>
			longCodepoint,
			/// <summary>If there is a decomposition type, you need a decomposition</summary>
			mustEnterDecomp
		}

		/// <summary>
		/// Contains a Set of ErrorMessage objects that the user currently needs to address.
		/// </summary>
		private Dictionary<TextBox, Set<ErrorMessage>> errorTable = new Dictionary<TextBox, Set<ErrorMessage>>();
		/// <summary>
		/// A Dictionary you need to define when you create a new instance of this class. It should contain
		/// the TextBox as a key and the Label association as a value.
		/// </summary>
		private Dictionary<TextBox, Label> labelAssociation = null;
		/// <summary>
		/// The resource manager used to access language dependant strings.
		/// </summary>

		//Retrieves the error message values from our resource file
		private static System.Resources.ResourceManager m_res =
			new System.Resources.ResourceManager("SIL.FieldWorks.FwCoreDlgs.FWCoreDlgsErrors",
			System.Reflection.Assembly.GetExecutingAssembly());
		//Sets color of error messages
		private static Color m_errorColor = Color.Red;
		private static string m_bullet = "\u25BA";
		//Contains the control to be enabled/disabled when errors are present
		private Control m_enabledControl;

		#endregion


		#region Methods

		/// <summary>
		/// Adds a single new message to a table and displays it
		/// </summary>
		/// <param name="textBox">The text box where the error appears</param>
		/// <param name="message">The error message you wish to add</param>
		public void AddMessage(TextBox textBox, ErrorMessage message)
		{
			Set<ErrorMessage> listOfMessages = null;
			if (errorTable.TryGetValue(textBox, out listOfMessages))
			{
				// An entry for this message box exists
				listOfMessages.Add(message);
			}
			else
			{
				// Not found, so initialize it.
				listOfMessages = new Set<ErrorMessage>();
				listOfMessages.Add(message);
				errorTable.Add(textBox, listOfMessages);
			}
			DisplayErrorTable();
		}

		/// <summary>
		/// Adds multiple new messages to a table and displays it
		/// </summary>
		/// <param name="textBox">The text box where the error appears</param>
		/// <param name="messages">The set of messages you wish to add</param>
		public void AddMessage(TextBox textBox, Set<ErrorMessage> messages)
		{
			foreach(ErrorMessage message in messages)
				AddMessage(textBox, message);
		}

		/// <summary>
		/// Removes message from table of error messages
		/// </summary>
		/// <param name="textBox">The text box associated with the errors you wish to remove</param>
		public void RemoveMessage(TextBox textBox)
		{
			errorTable.Remove(textBox);
			DisplayErrorTable();
		}

		/// <summary>
		/// Displays all the errors that currently the user hasn't fixed,
		/// using the labels specified in the labelAssociation Dictionary.
		/// </summary>
		private void DisplayErrorTable()
		{
			foreach(Label label in labelAssociation.Values)
			{
				// Clear the labels and retrieves the color value
				label.ForeColor = m_errorColor;
				label.Text = "";
			}

			foreach(KeyValuePair<TextBox, Set<ErrorMessage>> kvp in errorTable)
			{
				Label currentLabel = labelAssociation[kvp.Key];
				StringBuilder newLabelText = new StringBuilder(currentLabel.Text);
				foreach(ErrorMessage errorMessage in kvp.Value)
				{
					//Display the error message
					string emts = errorMessage.ToString();
					string message = m_res.GetString(
						String.Format("kstid{0}", emts.Substring(emts.LastIndexOf('.') + 1)));
					newLabelText.AppendFormat("{0}{1}{2}",
						m_bullet,
						message,
						Environment.NewLine);
				}
				currentLabel.Text = newLabelText.ToString();
			}
		}


		#region star handling

		private Dictionary<TextBox, Label> starLabels = new Dictionary<TextBox, Label>();

		/// <summary>
		/// Adds a star to the given TextBox.
		/// </summary>
		/// <param name="textBox">The text box to place the star by</param>
		public void AddStar(TextBox textBox)
		{
			// Don't add stars to disabled boxes
			if(!textBox.Enabled)
				return;
			// Don't make a new label if we've already made one.
			if(!starLabels.ContainsKey(textBox))
			{
				Label newLabel = new Label();
				// Set the new star next to the text box
				newLabel.Left = textBox.Left - 16;
				newLabel.Top = textBox.Top;
				// U+2738 is a big '*'
				newLabel.Text = "\u2717";
				// Set it to the error color
				newLabel.ForeColor = m_errorColor;
				// Set the label two 2x2 grid point (16x16 pixels)
				newLabel.Size = new Size(16,16);
				newLabel.Name = "star";
				// Set the font to be a little bigger.
				newLabel.Font  = new
					Font(textBox.Font.FontFamily,(float)14.0,FontStyle.Regular,GraphicsUnit.Pixel);
				textBox.Parent.Controls.Add(newLabel);
				// Force the parent to re-draw so that the star will appear
				textBox.Parent.Refresh();
				// Add the star to a dictionary so that we can delete it
				starLabels.Add(textBox, newLabel);
			}
			//If there are no errors
			if(starLabels.Count == 0)
				m_enabledControl.Enabled = true;
			else
				m_enabledControl.Enabled = false;
		}

		/// <summary>
		/// Removes the star from the given text box.
		/// </summary>
		/// <param name="textBox">The text box that may or may not have a star to remove.</param>
		public void RemoveStar(TextBox textBox)
		{
			Label starLabel = null;
			if (starLabels.TryGetValue(textBox, out starLabel))
			{
				// Remove the label
				textBox.Parent.Controls.Remove(starLabel);
				// Remove it from the dictionary, so we know to add it later
				starLabels.Remove(textBox);
				// Refresh so it disappear
				textBox.Parent.Refresh();
			}
			//If there are no errors
			if (starLabels.Count == 0)
				m_enabledControl.Enabled = true;
			else
				m_enabledControl.Enabled = false;
		}

		#endregion

		#endregion
	}
}
