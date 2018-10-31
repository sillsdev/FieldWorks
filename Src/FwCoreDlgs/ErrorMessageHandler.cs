// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

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
		/// Contains a Set of ErrorMessage objects that the user currently needs to address.
		/// </summary>
		private Dictionary<TextBox, HashSet<ErrorMessage>> errorTable = new Dictionary<TextBox, HashSet<ErrorMessage>>();
		/// <summary>
		/// A Dictionary you need to define when you create a new instance of this class. It should contain
		/// the TextBox as a key and the Label association as a value.
		/// </summary>
		private Dictionary<TextBox, Label> labelAssociation;
		/// <summary>
		/// The resource manager used to access language dependent strings.
		/// </summary>
		private static System.Resources.ResourceManager m_res = new System.Resources.ResourceManager("SIL.FieldWorks.FwCoreDlgs.FWCoreDlgsErrors", System.Reflection.Assembly.GetExecutingAssembly());
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
			HashSet<ErrorMessage> listOfMessages;
			if (errorTable.TryGetValue(textBox, out listOfMessages))
			{
				// An entry for this message box exists
				listOfMessages.Add(message);
			}
			else
			{
				// Not found, so initialize it.
				listOfMessages = new HashSet<ErrorMessage> { message };
				errorTable.Add(textBox, listOfMessages);
			}
			DisplayErrorTable();
		}

		/// <summary>
		/// Adds multiple new messages to a table and displays it
		/// </summary>
		/// <param name="textBox">The text box where the error appears</param>
		/// <param name="messages">The set of messages you wish to add</param>
		public void AddMessage(TextBox textBox, ISet<ErrorMessage> messages)
		{
			foreach (var message in messages)
			{
				AddMessage(textBox, message);
			}
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
			foreach (var label in labelAssociation.Values)
			{
				// Clear the labels and retrieves the color value
				label.ForeColor = m_errorColor;
				label.Text = "";
			}

			foreach (var kvp in errorTable)
			{
				var currentLabel = labelAssociation[kvp.Key];
				var newLabelText = new StringBuilder(currentLabel.Text);
				foreach (var errorMessage in kvp.Value)
				{
					//Display the error message
					var emts = errorMessage.ToString();
					var message = m_res.GetString($"kstid{emts.Substring(emts.LastIndexOf('.') + 1)}");
					newLabelText.AppendFormat("{0}{1}{2}", m_bullet, message, Environment.NewLine);
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
			if (!textBox.Enabled)
			{
				return;
			}
			// Don't make a new label if we've already made one.
			if (!starLabels.ContainsKey(textBox))
			{
				var newLabel = new Label
				{
					Left = textBox.Left - 16,
					Top = textBox.Top,
					Text = "\u2717",
					ForeColor = m_errorColor,
					Size = new Size(16, 16),
					Name = "star",
					Font = new Font(textBox.Font.FontFamily, (float)14.0, FontStyle.Regular, GraphicsUnit.Pixel)
				};
				textBox.Parent.Controls.Add(newLabel);
				// Force the parent to re-draw so that the star will appear
				textBox.Parent.Refresh();
				// Add the star to a dictionary so that we can delete it
				starLabels.Add(textBox, newLabel);
			}
			//If there are no errors
			if (starLabels.Count == 0)
			{
				m_enabledControl.Enabled = true;
			}
			else
			{
				m_enabledControl.Enabled = false;
			}
		}

		/// <summary>
		/// Removes the star from the given text box.
		/// </summary>
		/// <param name="textBox">The text box that may or may not have a star to remove.</param>
		public void RemoveStar(TextBox textBox)
		{
			Label starLabel;
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
			{
				m_enabledControl.Enabled = true;
			}
			else
			{
				m_enabledControl.Enabled = false;
			}
		}

		#endregion

		#endregion
	}
}