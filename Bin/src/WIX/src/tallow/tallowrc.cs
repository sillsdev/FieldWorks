//-------------------------------------------------------------------------------------------------
// <copyright file="tallowrc.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Utilities for tallow to use while processing resource (.rc) files.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.IO;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Utilities for tallow to use while processing resource (.rc) files.
	/// </summary>
	public class TallowRCProcessing
	{
		/// <summary>
		/// Types of controls.
		/// </summary>
		private enum ControlType
		{
			/// <summary>Billboard</summary>
			Billboard,
			/// <summary>Bitmap</summary>
			Bitmap,
			/// <summary>CheckBox</summary>
			CheckBox,
			/// <summary>ComboBox</summary>
			ComboBox,
			/// <summary>DirectoryCombo</summary>
			DirectoryCombo,
			/// <summary>DirectoryList</summary>
			DirectoryList,
			/// <summary>Edit</summary>
			Edit,
			/// <summary>GroupBox</summary>
			GroupBox,
			/// <summary>Icon</summary>
			Icon,
			/// <summary>Line</summary>
			Line,
			/// <summary>ListBox</summary>
			ListBox,
			/// <summary>ListView</summary>
			ListView,
			/// <summary>MaskedEdit</summary>
			MaskedEdit,
			/// <summary>PathEdit</summary>
			PathEdit,
			/// <summary>ProgressBar</summary>
			ProgressBar,
			/// <summary>PushButton</summary>
			PushButton,
			/// <summary>RadioButtonGroup</summary>
			RadioButtonGroup,
			/// <summary>ScrollableText</summary>
			ScrollableText,
			/// <summary>SelectionTree</summary>
			SelectionTree,
			/// <summary>Text</summary>
			Text,
			/// <summary>VolumeCostList</summary>
			VolumeCostList,
			/// <summary>VolumeSelectCombo</summary>
			VolumeSelectCombo,
			/// <summary>Initialize</summary>
			Initialize
		}

		/// <summary>
		/// Types of errors.
		/// </summary>
		private enum ErrorCode
		{
			/// <summary>None</summary>
			None,
			/// <summary>NoParams</summary>
			NoParams,
			/// <summary>UnknownParam</summary>
			UnknownParam,
			/// <summary>InvalidOption</summary>
			InvalidOption,
			/// <summary>AdditionalArg</summary>
			AdditionalArg,
			/// <summary>NoFile</summary>
			NoFile,
			/// <summary>CantOpenFile</summary>
			CantOpenFile,
			/// <summary>ReadLine</summary>
			ReadLine
		}

		/// <summary>
		/// Processes a .rc file to the writer.
		/// </summary>
		/// <param name="writer">Writer to output to.</param>
		/// <param name="path">Path to resource file.</param>
		public static void ProcessResourceFile(XmlWriter writer, string path)
		{
			FileInfo file = new FileInfo(path);
			if (!file.Exists)
			{
				throw new WixFileNotFoundException(path, "File");
			}

			StreamReader reader = new StreamReader(path);
			ErrorCode error = ReadRCFile(reader, writer);

			if (ErrorCode.ReadLine == error)
			{
				Console.WriteLine("Error reading from input file.");
			}

			reader.Close();
		}

		/// <summary>
		/// Reads a .rc file.
		/// </summary>
		/// <param name="reader">StreamReader for the input.</param>
		/// <param name="writer">XmlWriter for the output.</param>
		/// <returns>Error or ErrorCode.None.</returns>
		private static ErrorCode ReadRCFile(StreamReader reader, XmlWriter writer)
		{
			string curLine;
			ErrorCode error = ErrorCode.None;

			while (null != (curLine = ReadLineFromFile(reader)) && ErrorCode.None == error)
			{
				string curToken;

				GetTokenFromLine(curLine, out curToken, 2);
				if (0 == String.Compare(curToken, "DIALOG") && GetNumberOfTokensInLine(curLine) == 7)
				{
					string dlgId;
					string tempStr;
					int dlgX = 0;
					int dlgY = 0;
					int dlgWidth = 0;
					int dlgHeight = 0;

					// Found a dialog

					// Get the dialog id
					GetTokenFromLine(curLine, out dlgId, 1);

					// Get the X location
					GetTokenFromLine(curLine, out tempStr, 4);
					dlgX = System.Convert.ToInt32(tempStr);

					// Get the Y location
					GetTokenFromLine(curLine, out tempStr, 5);
					dlgY = System.Convert.ToInt32(tempStr);

					// Get the Width
					GetTokenFromLine(curLine, out tempStr, 6);
					dlgWidth = System.Convert.ToInt32(tempStr);

					// Get the Height
					GetTokenFromLine(curLine, out tempStr, 7);
					dlgHeight = System.Convert.ToInt32(tempStr);

					// Initialize the dialog
					Dialog dlg = new Dialog(dlgId, dlgX, dlgY, dlgWidth, dlgHeight);
					error = ReadDialogFromRCFile(reader, writer, dlg);
				}
				else if (0 == String.Compare(curToken, "DIALOGEX") && GetNumberOfTokensInLine(curLine) == 6)
				{
					string dlgId;
					string tempStr;
					int dlgX = 0;
					int dlgY = 0;
					int dlgWidth = 0;
					int dlgHeight = 0;

					// Found a dialog

					// Get the dialog id
					GetTokenFromLine(curLine, out dlgId, 1);

					// Get the X location
					GetTokenFromLine(curLine, out tempStr, 3);
					dlgX = System.Convert.ToInt32(tempStr);

					// Get the Y location
					GetTokenFromLine(curLine, out tempStr, 4);
					dlgY = System.Convert.ToInt32(tempStr);

					// Get the Width
					GetTokenFromLine(curLine, out tempStr, 5);
					dlgWidth = System.Convert.ToInt32(tempStr);

					// Get the Height
					GetTokenFromLine(curLine, out tempStr, 6);
					dlgHeight = System.Convert.ToInt32(tempStr);

					// Initialize the dialog
					Dialog dlg = new Dialog(dlgId, dlgX, dlgY, dlgWidth, dlgHeight);
					error = ReadDialogFromRCFile(reader, writer, dlg);
				}
			}

			return error;
		}

		/// <summary>
		/// Reads a dialog from a .rc file.
		/// </summary>
		/// <param name="reader">StreamReader to read dialog from.</param>
		/// <param name="writer">XmlWriter to write output to.</param>
		/// <param name="dialog">Dialog being read.</param>
		/// <returns>Error or ErrorCode.None.</returns>
		private static ErrorCode ReadDialogFromRCFile(StreamReader reader, XmlWriter writer, Dialog dialog)
		{
			string curLine;
			string curToken;
			string dlgCaption;

			// Skip the next line (STYLE)
			if (null == (curLine = ReadLineFromFile(reader)))
			{
				return ErrorCode.ReadLine;
			}

			/*
			if (null == (curLine = ReadLineFromFile(reader)))
			{
				return ErrorCode.ReadLine;
			}
			*/

			// Look for the "CAPTION" line
			if (null == (curLine = ReadLineFromFile(reader)))
			{
				return ErrorCode.ReadLine;
			}

			GetTokenFromLine(curLine, out curToken, 1);
			if (0 == String.Compare(curToken, "CAPTION"))
			{
				// Found the dialog's caption

				// Get the dialog caption
				GetTokenFromLine(curLine, out dlgCaption, 2);

				int firstQuote = dlgCaption.IndexOf('\"');
				int lastQuote = dlgCaption.LastIndexOf('\"');
				if (-1 != firstQuote && -1 != lastQuote)
				{
					dialog.Title = dlgCaption.Substring(firstQuote, (lastQuote - firstQuote) - 1);
				}
			}

			// Write the entire dialog if it was properly formatted in the RC file
			if (null != dialog)
			{
				dialog.GenerateOutput(writer);
			}

			// Skip the next line (FONT)
			if (null == (curLine = ReadLineFromFile(reader)))
			{
				return ErrorCode.ReadLine;
			}

			// Look for the "BEGIN" line
			if (null == (curLine = ReadLineFromFile(reader)))
			{
				return ErrorCode.ReadLine;
			}

			GetTokenFromLine(curLine, out curToken, 1);
			if (0 == String.Compare(curToken, "BEGIN"))
			{
				bool foundDialogEnd = false;
				while (!foundDialogEnd)
				{
					// Look for the "END" line
					if (null == (curLine = ReadLineFromFile(reader)))
					{
						return ErrorCode.ReadLine;
					}

					GetTokenFromLine(curLine, out curToken, 1);
					if (0 == String.Compare(curToken, "END"))
					{
						foundDialogEnd = true;
					}
					else
					{
						Control newControl = new Control(null, ControlType.Initialize, 0, 0, 0, 0);

						// Determine the type of control we have
						GetTokenFromLine(curLine, out curToken, 1);
						if (0 == String.Compare(curToken, "DEFPUSHBUTTON"))
						{
							SetupPushButton(newControl, curLine, true);
						}
						else if (0 == String.Compare(curToken, "PUSHBUTTON"))
						{
							SetupPushButton(newControl, curLine, false);
						}
						else if (0 == String.Compare(curToken, "EDITTEXT"))
						{
							SetupEditBox(newControl, curLine);
						}
						else if (0 == String.Compare(curToken, "LTEXT"))
						{
							SetupStaticText(newControl, curLine);
						}
						else if (0 == String.Compare(curToken, "ICON"))
						{
							SetupIcon(newControl, curLine);
						}
						else if (0 == String.Compare(curToken, "COMBOBOX"))
						{
							SetupComboBox(newControl, curLine);
						}
						else if (0 == String.Compare(curToken, "LISTBOX"))
						{
							SetupListBox(newControl, curLine);
						}
						else if (0 == String.Compare(curToken, "CONTROL"))
						{
							if (ErrorCode.None != SetupControl(reader, newControl, curLine))
							{
								// An error occured. Never speak of this again.
								newControl = null;
							}
						}
						else
						{
							// If we got here we have not found a valid control so we must
							// not output anything.
							newControl = null;
						}

						if (null != newControl)
						{
							newControl.GenerateOutput(writer);
						}
					}
				}
			}

			return ErrorCode.None;
		}

		/// <summary>
		/// Sets up a different kind of control.
		/// </summary>
		/// <param name="reader">Reader from which to read the control info.</param>
		/// <param name="control">Control to be processed.</param>
		/// <param name="line">Line containing the control.</param>
		/// <returns>Error code or ErrorCode.None.</returns>
		private static ErrorCode SetupControl(StreamReader reader, Control control, string line)
		{
			string tempStr;
			string attributes;
			ControlType controlType = ControlType.ProgressBar;
			int numTokens = GetNumberOfTokensInLine(line);

			GetTokenFromLine(line, out tempStr, 3);
			control.Id = tempStr;

			GetTokenFromLine(line, out tempStr, 4);
			switch (tempStr)
			{
				case "Static":
					controlType = ControlType.Text;
					break;
				case "":
					break;
			}

			GetTokenFromLine(line, out tempStr, 5);
			attributes = tempStr;

			/*
			if (!bReadSkippedAttrib) // Skip the attributes
			{
				// Test for a radiobutton or checkbox
				if (strstr(tempStr, "BS_AUTORADIOBUTTON") != NULL)
					controlType = kWiXControl_Radio;
				else if (strstr(tempStr, "BS_AUTOCHECKBOX") != NULL)
					controlType = kWiXControl_Check;

				// The attributes span multiple lines.  We have more work to do here...
				char ch = tempStr[strlen(tempStr)-1];
				while (ch == ' ' || ch == '|')
				{
					if (!ReadLine(in, line))
						return kWiXError_ReadLine;
					numTokens = GetNumberOfTokensInLine(line);
					GetTokenFromLine(line, tempStr, 1);
					ch = tempStr[strlen(tempStr)-1];
					nextToken = 1;

					// Test for a radiobutton or checkbox
					if (strstr(tempStr, "BS_AUTORADIOBUTTON") != NULL)
						controlType = kWiXControl_Radio;
					else if (strstr(tempStr, "BS_AUTOCHECKBOX") != NULL)
						controlType = kWiXControl_Check;
				}
				bReadSkippedAttrib = TRUE;
			}
			*/

			GetTokenFromLine(line, out tempStr, 6);
			control.X = Convert.ToInt32(tempStr);

			GetTokenFromLine(line, out tempStr, 7);
			control.Y = Convert.ToInt32(tempStr);

			GetTokenFromLine(line, out tempStr, 8);
			control.Width = Convert.ToInt32(tempStr);

			GetTokenFromLine(line, out tempStr, 9);
			control.Height = Convert.ToInt32(tempStr);

			control.type = controlType;
			return ErrorCode.None;
		}

		/// <summary>
		/// Sets up a list box.
		/// </summary>
		/// <param name="control">The control being processed.</param>
		/// <param name="line">Line containing the control.</param>
		private static void SetupListBox(Control control, string line)
		{
			string tempStr;
			string id;
			int x = 0;
			int y = 0;
			int width = 0;
			int height = 0;

			// Get the control's id
			GetTokenFromLine(line, out id, 2);

			// Get the X location
			GetTokenFromLine(line, out tempStr, 3);
			x = System.Convert.ToInt32(tempStr);

			// Get the Y location
			GetTokenFromLine(line, out tempStr, 4);
			y = System.Convert.ToInt32(tempStr);

			// Get the Width
			GetTokenFromLine(line, out tempStr, 5);
			width = System.Convert.ToInt32(tempStr);

			// Get the Height
			GetTokenFromLine(line, out tempStr, 6);
			height = System.Convert.ToInt32(tempStr);

			control.Id = id;
			control.type = ControlType.ListBox;
			control.X = x;
			control.Y = y;
			control.Width = width;
			control.Height = height;
		}

		/// <summary>
		/// Sets up a combo box.
		/// </summary>
		/// <param name="control">The control being processed.</param>
		/// <param name="line">Line containing the control.</param>
		private static void SetupComboBox(Control control, string line)
		{
			string tempStr;
			string id;
			int x = 0;
			int y = 0;
			int width = 0;
			int height = 0;

			// Get the control's id
			GetTokenFromLine(line, out id, 2);

			// Get the X location
			GetTokenFromLine(line, out tempStr, 3);
			x = System.Convert.ToInt32(tempStr);

			// Get the Y location
			GetTokenFromLine(line, out tempStr, 4);
			y = System.Convert.ToInt32(tempStr);

			// Get the Width
			GetTokenFromLine(line, out tempStr, 5);
			width = System.Convert.ToInt32(tempStr);

			// Get the Height
			GetTokenFromLine(line, out tempStr, 6);
			height = System.Convert.ToInt32(tempStr);

			control.Id = id;
			control.type = ControlType.ComboBox;
			control.X = x;
			control.Y = y;
			control.Width = width;
			control.Height = height;
		}

		/// <summary>
		/// Sets up an icon.
		/// </summary>
		/// <param name="control">The control being processed.</param>
		/// <param name="line">Line containing the control.</param>
		private static void SetupIcon(Control control, string line)
		{
			string tempStr;
			string id;
			int x = 0;
			int y = 0;
			int width = 0;
			int height = 0;

			// Get the control's id
			GetTokenFromLine(line, out id, 3);

			// Get the X location
			GetTokenFromLine(line, out tempStr, 4);
			x = System.Convert.ToInt32(tempStr);

			// Get the Y location
			GetTokenFromLine(line, out tempStr, 5);
			y = System.Convert.ToInt32(tempStr);

			// Get the Width
			GetTokenFromLine(line, out tempStr, 6);
			width = System.Convert.ToInt32(tempStr);

			// Get the Height
			GetTokenFromLine(line, out tempStr, 7);
			height = System.Convert.ToInt32(tempStr);

			control.Id = id;
			control.type = ControlType.Icon;
			control.X = x;
			control.Y = y;
			control.Width = width;
			control.Height = height;
		}

		/// <summary>
		/// Sets up a static text control.
		/// </summary>
		/// <param name="control">The control being processed.</param>
		/// <param name="line">Line containing the control.</param>
		private static void SetupStaticText(Control control, string line)
		{
			string curToken;
			string tempStr;
			int x = 0;
			int y = 0;
			int width = 0;
			int height = 0;
			string id;

			// Get the control's id
			GetTokenFromLine(line, out id, 3);

			// Get the X location
			GetTokenFromLine(line, out tempStr, 4);
			x = System.Convert.ToInt32(tempStr);

			// Get the Y location
			GetTokenFromLine(line, out tempStr, 5);
			y = System.Convert.ToInt32(tempStr);

			// Get the Width
			GetTokenFromLine(line, out tempStr, 6);
			width = System.Convert.ToInt32(tempStr);

			// Get the Height
			GetTokenFromLine(line, out tempStr, 7);
			height = System.Convert.ToInt32(tempStr);

			control.Id = id;
			control.type = ControlType.Text;
			control.X = x;
			control.Y = y;
			control.Width = width;
			control.Height = height;

			GetTokenFromLine(line, out curToken, 2);
			control.Text = curToken;
		}

		/// <summary>
		/// Sets up an edit box.
		/// </summary>
		/// <param name="control">The control being created.</param>
		/// <param name="line">The line containing the control.</param>
		private static void SetupEditBox(Control control, string line)
		{
			string tempStr;
			int x = 0;
			int y = 0;
			int width = 0;
			int height = 0;
			string id;

			// Get the control's id
			GetTokenFromLine(line, out id, 2);

			// Get the X location
			GetTokenFromLine(line, out tempStr, 3);
			x = System.Convert.ToInt32(tempStr);

			// Get the Y location
			GetTokenFromLine(line, out tempStr, 4);
			y = System.Convert.ToInt32(tempStr);

			// Get the Width
			GetTokenFromLine(line, out tempStr, 5);
			width = System.Convert.ToInt32(tempStr);

			// Get the Height
			GetTokenFromLine(line, out tempStr, 6);
			height = System.Convert.ToInt32(tempStr);

			control.Id = id;
			control.type = ControlType.Edit;
			control.X = x;
			control.Y = y;
			control.Width = width;
			control.Height = height;
		}

		/// <summary>
		/// Creates a push button control.
		/// </summary>
		/// <param name="control">Control to fill in.</param>
		/// <param name="line">Line containing info about control.</param>
		/// <param name="defButton">Whether this button is default.</param>
		private static void SetupPushButton(Control control, string line, bool defButton)
		{
			string curToken;
			string tempStr;
			int x = 0;
			int y = 0;
			int width = 0;
			int height = 0;
			string id = null;

			// Get the control's id
			GetTokenFromLine(line, out id, 3);

			// Get the X location
			GetTokenFromLine(line, out tempStr, 4);
			x = System.Convert.ToInt32(tempStr);

			// Get the Y location
			GetTokenFromLine(line, out tempStr, 5);
			y = System.Convert.ToInt32(tempStr);

			// Get the Width
			GetTokenFromLine(line, out tempStr, 6);
			width = System.Convert.ToInt32(tempStr);

			// Get the Height
			GetTokenFromLine(line, out tempStr, 7);
			height = System.Convert.ToInt32(tempStr);

			control.Id = id;
			control.type = ControlType.PushButton;
			control.X = x;
			control.Y = y;
			control.Width = width;
			control.Height = height;

			GetTokenFromLine(line, out curToken, 2);
			control.Text = curToken;

			control.Default = defButton;
		}

		/// <summary>
		/// Reads a full statement from an RC file. If the line ends with a '|' or ',',
		/// it reads the next line, and so on.
		/// </summary>
		/// <param name="reader">Reader from which to read.</param>
		/// <returns>String representing line.</returns>
		private static string ReadLineFromFile(StreamReader reader)
		{
			StringBuilder builder = new StringBuilder();
			string line;

			do
			{
				line = reader.ReadLine();

				if (null == line && 0 == builder.Length)
				{
					return null;
				}
				else if (null == line)
				{
					break;
				}

				line = line.Trim();
				builder.Append(line);
			} while (line.EndsWith("|") || line.EndsWith(","));

			return builder.ToString();
		}

		/// <summary>
		/// Skips past the initial whitespace in a line.
		/// </summary>
		/// <param name="line">The line in question.</param>
		/// <param name="startLoc">Location from which to start skipping.</param>
		/// <returns>The index into the string after the whitespace.</returns>
		private static int SkipInitialWhitespaceInLine(string line, int startLoc)
		{
			int strLoc = startLoc;
			char ch;
			if (strLoc < line.Length)
			{
				ch = line[strLoc];
			}
			else
			{
				ch = '\0';
			}
			while ((ch == ' ' || ch == '\t' || ch == '\n') && strLoc < line.Length)
			{
				if (++strLoc < line.Length)
				{
					ch = line[strLoc];
				}
				else
				{
					ch = '\0';
				}
			}
			return strLoc;
		}

		/// <summary>
		/// Skips past whitespace (and commas) in a line.
		/// </summary>
		/// <param name="line">The linein question.</param>
		/// <param name="startLoc">Location from which to start skipping.</param>
		/// <returns>Index into the string after the whitespace.</returns>
		private static int SkipAllWhitespaceInLine(string line, int startLoc)
		{
			int strLoc = startLoc;
			char ch;
			if (strLoc < line.Length)
			{
				ch = line[strLoc];
			}
			else
			{
				ch = '\0';
			}
			while ((ch == ' ' || ch == '\t' || ch == ',' || ch == '\n') && strLoc < line.Length)
			{
				if (++strLoc < line.Length)
				{
					ch = line[strLoc];
				}
				else
				{
					ch = '\0';
				}
			}
			return strLoc;
		}

		/// <summary>
		/// Gets a token from a string.
		/// </summary>
		/// <param name="line">Line from which to pull the token.</param>
		/// <param name="token">Our param for the token.</param>
		/// <param name="startLoc">Location from which to start reading token.</param>
		/// <returns>Index into string just past token.</returns>
		private static int GetTokenString(string line, out string token, int startLoc)
		{
			token = null;
			StringBuilder tokenBuilder = new StringBuilder();
			char ch;
			if (startLoc < line.Length)
			{
				ch = line[startLoc];
			}
			else
			{
				ch = '\0';
			}

			while (ch != ' ' && ch != '\t' && ch != ',' && ch != '\n' && startLoc < line.Length)
			{
				if (ch == '\"')
				{
					if (++startLoc < line.Length)
					{
						ch = line[startLoc];
					}
					else
					{
						ch = '\0';
					}
					while (!(ch == '\"' || ch == '\n') && startLoc < line.Length)
					{
						tokenBuilder.Append(ch);
						if (++startLoc == line.Length)
						{
							ch = '\0';
						}
						else
						{
							ch = line[startLoc];
						}
					}

					if (++startLoc == line.Length)
					{
						ch = '\0';
					}
					else
					{
						ch = line[startLoc];
					}
				}
				else
				{
					tokenBuilder.Append(ch);
					if (++startLoc == line.Length)
					{
						ch = '\0';
					}
					else
					{
						ch = line[startLoc];
					}
				}

				// Special case for Attributes separated by " | "
				if (ch == ' ' && line[startLoc+1] == '|')
				{
					tokenBuilder.Append(ch);
					tokenBuilder.Append(line[++startLoc]);
					if (++startLoc == line.Length)
					{
						ch = '\0';
					}
					else
					{
						ch = line[startLoc];
					}
					if (ch == ' ')    // We only eat this ' ' if its present
					{
						tokenBuilder.Append(ch);
						if (++startLoc == line.Length)
						{
							ch = '\0';
						}
						else
						{
							ch = line[startLoc];
						}
					}
				}
			}

			token = tokenBuilder.ToString();
			return startLoc;
		}

		/// <summary>
		/// Gets a particular token from the line.
		/// </summary>
		/// <param name="line">Line from which to get the token.</param>
		/// <param name="token">Out param for the token.</param>
		/// <param name="tokenNum">Index of token in string.</param>
		private static void GetTokenFromLine(string line, out string token, int tokenNum)
		{
			int curToken = 1, strLoc = 0;
			token = null;

			// Skip all whitespace
			strLoc = SkipInitialWhitespaceInLine(line, 0);

			char ch;
			if (strLoc < line.Length)
			{
				ch = line[strLoc];
			}
			else
			{
				ch = '\0';
			}
			while (curToken != tokenNum)
			{
				string tempStr;

				// Skip this token
				strLoc = GetTokenString(line, out tempStr, strLoc);

				// Skip all whitespace
				strLoc = SkipAllWhitespaceInLine(line, strLoc);
				if (strLoc < line.Length)
				{
					ch = line[strLoc];
				}
				else
				{
					ch = '\0';
				}

				++curToken;
			}

			// Now copy the token into our string
			GetTokenString(line, out token, strLoc);
		}

		/// <summary>
		/// Gets the number of tokens in a line.
		/// </summary>
		/// <param name="line">The line in question.</param>
		/// <returns>The number of tokens in the line.</returns>
		private static int GetNumberOfTokensInLine(string line)
		{
			int numTokens = 0, strLoc = 0;

			// Skip all whitespace
			strLoc = SkipInitialWhitespaceInLine(line, 0);

			while (strLoc < line.Length)
			{
				string tempStr;

				// Skip this token
				strLoc = GetTokenString(line, out tempStr, strLoc);

				// Skip all whitespace
				strLoc = SkipAllWhitespaceInLine(line, strLoc);

				++numTokens;
			}
			return numTokens;
		}

		/// <summary>
		/// Converts a boolean to a yes or no string.
		/// </summary>
		/// <param name="input">Bool to convert.</param>
		/// <returns>Yes if input is true, no otherwise.</returns>
		private static string BoolToYesNo(bool input)
		{
			return input ? "yes" : "no";
		}

		/// <summary>
		/// Private class representing a control.
		/// </summary>
		private class Control
		{
			public ControlType type;
			public string Id;
			public bool Bitmap;
			public bool Cancel;
//            public bool CDROM;
			public string CheckBoxValue;
			public bool ComboList;
			public bool Default;
			public bool Disabled;
			public bool First;
//            public bool Fixed;
			public bool FixedSize;
//            public bool Floppy;
			public bool FormatSize;
			public bool HasBorder;
			public int Height;
			public string Help;
			public bool Hidden;
			public bool Icon;
			public int IconSize;
			public bool Image;
			public bool Indirect;
			public bool Integer;
			public bool LeftScroll;
			public bool Multiline;
			public bool NoPrefix;
			public bool NoWrap;
			public bool Password;
			public bool ProgressBlocks;
			public string Property;
			public bool PushLike;
//            public bool RAMDisk;
//            public bool Remote;
//            public bool Removable;
			public bool RightAligned;
			public bool RightToLeft;
//            public bool ShowRollbackCost;
			public bool Sorted;
			public bool Sunken;
			public bool TabSkip;
			public string Text;
			public bool Transparent;
			public bool UserLanguage;
			public int Width;
			public int X;
			public int Y;

			public Control(string Id, ControlType type, int X, int Y, int Width, int Height)
			{
				this.Id = Id;
				this.type = type;
				this.X = X;
				this.Y = Y;
				this.Width = Width;
				this.Height = Height;

				// Non-Required, Control Specific
				switch (this.type)
				{
					case ControlType.Text:
						this.FormatSize = false;
						this.NoPrefix = false;
						this.NoWrap = false;
						this.Transparent = false;
						this.UserLanguage = false;
						break;

					case ControlType.Edit:
						this.Multiline = false;
						this.Password = false;
						break;

					case ControlType.PushButton:
						this.Bitmap = false;
						this.FixedSize = false;
						this.Icon = false;
						this.IconSize = 32;            // 16, 32, or 48
						this.Image = false;
						break;

					case ControlType.RadioButtonGroup:
						this.Bitmap = false;
						this.FixedSize = false;
						this.HasBorder = false;
						this.Icon = false;
						this.IconSize = 32;            // 16, 32, or 48
						this.Image = false;
						this.PushLike = false;
						break;

					case ControlType.CheckBox:
						this.CheckBoxValue = null;
						this.PushLike = false;
						break;

					case ControlType.Icon:
						this.FixedSize = false;
						this.IconSize = 32;            // 16, 32, or 48
						this.Image = false;
						break;

					case ControlType.ComboBox:
						this.ComboList = false;
						this.Sorted = false;
						break;

					case ControlType.ListBox:
						this.Sorted = false;
						break;

					case ControlType.ProgressBar:
						this.ProgressBlocks = false;
						break;

					default:
						this.FormatSize = false;
						this.NoPrefix = false;
						this.NoWrap = false;
						this.Transparent = false;
						this.UserLanguage = false;
						this.Multiline = false;
						this.Password = false;
						this.Bitmap = false;
						this.FixedSize = false;
						this.Icon = false;
						this.IconSize = 32;            // 16, 32, or 48
						this.Image = false;
						this.Bitmap = false;
						this.FixedSize = false;
						this.HasBorder = false;
						this.Icon = false;
						this.IconSize = 32;            // 16, 32, or 48
						this.Image = false;
						this.PushLike = false;
						this.CheckBoxValue = null;
						this.PushLike = false;
						this.FixedSize = false;
						this.IconSize = 32;            // 16, 32, or 48
						this.Image = false;
						this.ComboList = false;
						this.Sorted = false;
						this.Sorted = false;
						this.ProgressBlocks = false;
						break;

				}

				// Non-Required
				this.Cancel = false;
				this.Default = false;
				this.Disabled = false;
				this.First = false;
				this.Help = null;
				this.Hidden = false;
				this.Indirect = false;
				this.Integer = false;
				this.LeftScroll = false;
				this.Property = null;
				this.RightAligned = false;
				this.RightToLeft = false;
				this.Sunken = false;
				this.TabSkip = false;
				this.Text = null;
			}

			public void GenerateOutput(XmlWriter writer)
			{
				string[] controlTypes =
					{
						"Billboard",
						"Bitmap",
						"CheckBox",
						"ComboBox",
						"DirectoryCombo",
						"DirectoryList",
						"Edit",
						"GroupBox",
						"Icon",
						"Line",
						"ListBox",
						"ListView",
						"MaskedEdit",
						"PathEdit",
						"ProgressBar",
						"PushButton",
						"RadioButtonGroup",
						"ScrollableText",
						"SelectionTree",
						"Text",
						"VolumeCostList",
						"VolumeSelectCombo",
						"ERROR"
					};

				writer.WriteStartElement("Control");
				writer.WriteAttributeString("Id", this.Id);
				writer.WriteAttributeString("Type", controlTypes[(int)this.type]);
				writer.WriteAttributeString("X", this.X.ToString());
				writer.WriteAttributeString("Y", this.Y.ToString());
				writer.WriteAttributeString("Width", this.Width.ToString());
				writer.WriteAttributeString("Height", this.Height.ToString());

				switch (this.type)
				{
					case ControlType.Text:
						writer.WriteAttributeString("FormatSize", BoolToYesNo(this.FormatSize));
						writer.WriteAttributeString("NoPrefix", BoolToYesNo(this.NoPrefix));
						writer.WriteAttributeString("NoWrap", BoolToYesNo(this.NoWrap));
						writer.WriteAttributeString("Transparent", BoolToYesNo(this.Transparent));
						writer.WriteAttributeString("UserLanguage", BoolToYesNo(this.UserLanguage));
						break;
					case ControlType.Edit:
						//                        fprintf(fp, " Multiline='%s' Password='%s' ",
						//                            BOOLTOSTR(control.Multiline),
						//                            BOOLTOSTR(control.Password));
						break;
					case ControlType.PushButton:
						//                        if (control.Bitmap)
						//                            fprintf(fp, " Bitmap='%s'", BOOLTOSTR(control.Bitmap));
						//                        if (control.FixedSize)
						//                            fprintf(fp, " FixedSize='%s'", BOOLTOSTR(control.FixedSize));
						//                        if (control.Icon)
						//                            fprintf(fp, " Icon='%s' IconSize='%i'", BOOLTOSTR(control.Icon), control.IconSize);
						//                        if (control.Image)
						//                            fprintf(fp, " Image='%s' ", BOOLTOSTR(control.Image));
						break;
					case ControlType.RadioButtonGroup:
						//                        if (control.Bitmap)
						//                            fprintf(fp, " Bitmap='%s'", BOOLTOSTR(control.Bitmap));
						//                        if (control.FixedSize)
						//                            fprintf(fp, " FixedSize='%s'", BOOLTOSTR(control.FixedSize));
						//                        if (control.HasBorder)
						//                            fprintf(fp, " HasBorder='%s'", BOOLTOSTR(control.HasBorder));
						//                        if (control.Icon)
						//                            fprintf(fp, " Icon='%s' IconSize='%i'", BOOLTOSTR(control.Icon), control.IconSize);
						//                        if (control.Image)
						//                            fprintf(fp, " Image='%s' ", BOOLTOSTR(control.Image));
						//                        if (control.PushLike)
						//                            fprintf(fp, " PushLike='%s'", BOOLTOSTR(control.PushLike));
						break;
					case ControlType.CheckBox:
						//                        fprintf(fp, " CheckBoxValue='%s' PushLike='%s'",
						//                            BOOLTOSTR(control.CheckBoxValue),
						//                            BOOLTOSTR(control.PushLike));
						break;
					case ControlType.Icon:
						//                        fprintf(fp, " FixedSize='%s' IconSize='%i' Image='%s'",
						//                            BOOLTOSTR(control.FixedSize),
						//                            control.IconSize,
						//                            BOOLTOSTR(control.Image));
						break;
					case ControlType.ComboBox:
						//                        fprintf(fp, " ComboList='%s' Sorted='%s'",
						//                            BOOLTOSTR(control.ComboList),
						//                            BOOLTOSTR(control.Sorted));
						break;
					case ControlType.ListBox:
						//                        fprintf(fp, " Sorted='%s'",
						//                            BOOLTOSTR(control.Sorted));
						break;
					case ControlType.ProgressBar:
						//                        fprintf(fp, " ProgressBlocks='%s'",
						//                            BOOLTOSTR(control.ProgressBlocks));
						break;
				}

				if (this.First)
				{
					writer.WriteAttributeString("First", BoolToYesNo(this.First));
				}
				/*
				// Non-Required
				fprintf(fp, " Cancel='%s' Default='%s' Disabled='%s' Help='%s' Hidden='%s' Indirect='%s' Integer='%s' LeftScroll='%s' Property='%s' RightAligned='%s' RightToLeft='%s' Sunken='%s' TabSkip='%s'",
					BOOLTOSTR(control.Cancel),
					BOOLTOSTR(control.Default),
					BOOLTOSTR(control.Disabled),
					NULLTOSTR(control.Help),
					BOOLTOSTR(control.Hidden),
					BOOLTOSTR(control.Indirect),
					BOOLTOSTR(control.Integer),
					BOOLTOSTR(control.LeftScroll),
					NULLTOSTR(control.Property),
					BOOLTOSTR(control.RightAligned),
					BOOLTOSTR(control.RightToLeft),
					BOOLTOSTR(control.Sunken),
					BOOLTOSTR(control.TabSkip));

				// NOTE: We only write the "Text" attribute if the control is not static text.  We write another line instead
				if (control.type == kWiXControl_Text)
					fprintf(fp, ">\n\t\t\t\t\t<Text>%s</Text>\n", NULLTOSTR(control.Text));
				else
					fprintf(fp, " Text='%s'>\n", NULLTOSTR(control.Text));

				fprintf(fp, "\t\t\t\t</Control>\n");
				*/
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Private class representing a dialog.
		/// </summary>
		private class Dialog
		{
			public string Id;
			public bool CustomPalette;
			public bool ErrorDialog;
			public int Height;
			public bool Hidden;
			public bool KeepModeless;
			public bool LeftScroll;
			public bool Modeless;
			public bool NoMinimize;
			public bool RightAligned;
			public bool RightToLeft;
			public bool SystemModal;
			public string Title;
			public bool TrackDiskSpace;
			public int Width;
			public int X;
			public int Y;
			public int numControls;

			public Dialog(string Id, int X, int Y, int Width, int Height)
			{
				this.Id = Id;
				this.X = X;
				this.Y = Y;
				this.Width = Width;
				this.Height = Height;

				this.CustomPalette = false;
				this.ErrorDialog = false;
				this.Hidden = false;
				this.KeepModeless = false;
				this.LeftScroll = false;
				this.Modeless = false;
				this.NoMinimize = false;
				this.RightAligned = false;
				this.RightToLeft = false;
				this.SystemModal = false;
				this.Title = null;
				this.TrackDiskSpace = false;
				this.numControls = 0;
			}

			public void GenerateOutput(XmlWriter writer)
			{

			}
		}
	}
}