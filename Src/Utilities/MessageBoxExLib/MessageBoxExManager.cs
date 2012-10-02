//From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp
using System;
using System.IO;
using System.Collections.Generic;
using System.Resources;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	/// Manages a collection of MessageBoxes. Basically manages the
	/// saved response handling for messageBoxes.
	/// </summary>
	public static class MessageBoxExManager
	{
		#region Fields
		private static Dictionary<string, MessageBoxEx> s_messageBoxes = new Dictionary<string, MessageBoxEx>();
		private static Dictionary<string, string> s_savedResponses = new Dictionary<string, string>();
		private static Dictionary<string, string> s_standardButtonsText = new Dictionary<string, string>();
		#endregion

		#region Static ctor
		static MessageBoxExManager()
		{
			try
			{
				//Assembly current = typeof(MessageBoxExManager).Assembly;
				//string[] resources = current.GetManifestResourceNames();
				ResourceManager rm = new ResourceManager("Utils.MessageBoxExLib.Resources.StandardButtonsText", typeof(MessageBoxExManager).Assembly);
				s_standardButtonsText[MessageBoxExButtons.OK.ToString()] = rm.GetString("Ok");
				s_standardButtonsText[MessageBoxExButtons.Cancel.ToString()] = rm.GetString("Cancel");
				s_standardButtonsText[MessageBoxExButtons.Yes.ToString()] = rm.GetString("Yes");
				s_standardButtonsText[MessageBoxExButtons.No.ToString()] = rm.GetString("No");
				s_standardButtonsText[MessageBoxExButtons.Abort.ToString()] = rm.GetString("Abort");
				s_standardButtonsText[MessageBoxExButtons.Retry.ToString()] = rm.GetString("Retry");
				s_standardButtonsText[MessageBoxExButtons.Ignore.ToString()] = rm.GetString("Ignore");
			}
			catch(Exception ex)
			{
				System.Diagnostics.Debug.Assert(false, "Unable to load resources for MessageBoxEx", ex.ToString());

				//Load default resources
				s_standardButtonsText[MessageBoxExButtons.OK.ToString()] = "OK";
				s_standardButtonsText[MessageBoxExButtons.Cancel.ToString()] = "Cancel";
				s_standardButtonsText[MessageBoxExButtons.Yes.ToString()] = "Yes";
				s_standardButtonsText[MessageBoxExButtons.No.ToString()] = "No";
				s_standardButtonsText[MessageBoxExButtons.Abort.ToString()] = "Abort";
				s_standardButtonsText[MessageBoxExButtons.Retry.ToString()] = "Retry";
				s_standardButtonsText[MessageBoxExButtons.Ignore.ToString()] = "Ignore";
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// Creates and returns a reference to a new message box with the specified name. The
		/// caller does not have to dispose the message box.
		/// </summary>
		/// <param name="name">The name of the message box</param>
		/// <returns>A new message box</returns>
		/// <remarks>If <c>null</c> is specified as the message name then the message box is not
		/// managed by the Manager and will be disposed automatically after a call to Show().
		/// Otherwise the message box is managed by MessageBoxExManager who will eventually
		/// dispose it.</remarks>
		public static MessageBoxEx CreateMessageBox(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name", "'name' parameter cannot be null");

			if (s_messageBoxes.ContainsKey(name))
			{
				string err = string.Format("A MessageBox with the name {0} already exists.",name);
				throw new ArgumentException(err,"name");
			}

			var msgBox = new MessageBoxEx {Name = name};
			s_messageBoxes[name] = msgBox;

			return msgBox;
		}

		public static Dictionary<string, string> SavedResponses
		{
			get
			{
				return s_savedResponses;
			}
		}

		/// <summary>
		/// Gets the message box with the specified name
		/// </summary>
		/// <param name="name">The name of the message box to retrieve</param>
		/// <returns>The message box with the specified name or null if a message box
		/// with that name does not exist</returns>
		public static MessageBoxEx GetMessageBox(string name)
		{
			MessageBoxEx result = null;
			if(s_messageBoxes.ContainsKey(name))
				result = s_messageBoxes[name];

			return result;
		}

		/// <summary>
		/// Deletes the message box with the specified name
		/// </summary>
		/// <param name="name">The name of the message box to delete</param>
		public static void DeleteMessageBox(string name)
		{
			if (name == null)
				return;

			MessageBoxEx msgBox = null;
			if (s_messageBoxes.TryGetValue(name, out msgBox))
			{
				s_messageBoxes.Remove(name);
				msgBox.Dispose();
			}
		}

		/// <summary>
		/// Disposes all stored message boxes
		/// </summary>
		public static void DisposeAllMessageBoxes()
		{
			foreach (var keyValuePair in s_messageBoxes)
			{
				keyValuePair.Value.Dispose();
			}
			s_messageBoxes.Clear();
		}

		public static void WriteSavedResponses(Stream stream)
		{
			throw new NotImplementedException("This feature has not yet been implemented");
		}

		public static void ReadSavedResponses(Stream stream)
		{
			throw new NotImplementedException("This feature has not yet been implemented");
		}

		/// <summary>
		/// Reset the saved response for the message box with the specified name.
		/// </summary>
		/// <param name="messageBoxName">The name of the message box whose response is to be reset.</param>
		public static void ResetSavedResponse(string messageBoxName)
		{
			if(messageBoxName == null)
				return;

			s_savedResponses.Remove(messageBoxName);
		}

		/// <summary>
		/// Resets the saved responses for all message boxes that are managed by the manager.
		/// </summary>
		public static void ResetAllSavedResponses()
		{
			s_savedResponses.Clear();
		}
		#endregion

		#region Internal Methods
		/// <summary>
		/// Set the saved response for the specified message box
		/// </summary>
		/// <param name="msgBox">The message box whose response is to be set</param>
		/// <param name="response">The response to save for the message box</param>
		internal static void SetSavedResponse(MessageBoxEx msgBox, string response)
		{
			if(msgBox.Name == null)
				return;

			s_savedResponses[msgBox.Name] = response;
		}

		/// <summary>
		/// Gets the saved response for the specified message box
		/// </summary>
		/// <param name="msgBox">The message box whose saved response is to be retrieved</param>
		/// <returns>The saved response if exists, null otherwise</returns>
		internal static string GetSavedResponse(MessageBoxEx msgBox)
		{
			string msgBoxName = msgBox.Name;
			if (msgBoxName == null)
				return null;

			string result = null;
			if (s_savedResponses.ContainsKey(msgBoxName))
				result = s_savedResponses[msgBoxName];

			return result;
		}

		/// <summary>
		/// Returns the localized string for standard button texts like,
		/// "Ok", "Cancel" etc.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		internal static string GetLocalizedString(string key)
		{
			string result = null;
			if (s_standardButtonsText.ContainsKey(key))
				result = s_standardButtonsText[key];
			return result;
		}
		#endregion
	}
}
