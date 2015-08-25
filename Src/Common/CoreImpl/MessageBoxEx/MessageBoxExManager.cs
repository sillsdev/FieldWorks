//From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using SIL.CoreImpl.Properties;
using SIL.Utils;

namespace SIL.CoreImpl.MessageBoxEx
{
	/// <summary>
	/// Manages a collection of MessageBoxes. Basically manages the
	/// saved response handling for messageBoxes.
	/// </summary>
	public static class MessageBoxExManager
	{
		#region Fields
		private static readonly Dictionary<string, MessageBoxEx> s_messageBoxes = new Dictionary<string, MessageBoxEx>();
		private static readonly Dictionary<string, string> s_savedResponses = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> s_standardButtonsText = new Dictionary<string, string>();
		#endregion

		#region Static ctor
		static MessageBoxExManager()
		{
			s_standardButtonsText[MessageBoxExButtons.OK.ToString()] = Resources.Ok;
			s_standardButtonsText[MessageBoxExButtons.Cancel.ToString()] = Resources.Cancel;
			s_standardButtonsText[MessageBoxExButtons.Yes.ToString()] = Resources.Yes;
			s_standardButtonsText[MessageBoxExButtons.No.ToString()] = Resources.No;
			s_standardButtonsText[MessageBoxExButtons.Abort.ToString()] = Resources.Abort;
			s_standardButtonsText[MessageBoxExButtons.Retry.ToString()] = Resources.Retry;
			s_standardButtonsText[MessageBoxExButtons.Ignore.ToString()] = Resources.Ignore;
		}
		#endregion

		#region Methods

		/// <summary/>
		public static void DefineMessageBox(string triggerName, string caption, string text,
			bool displayDontShowAgainButton, string iconName)
		{
			// Don't dispose msgBox here. MessageBoxExManager holds a reference to it and will dispose it eventually.
			MessageBoxEx msgBox;
			try
			{
				msgBox = CreateMessageBox(triggerName);
			}
			catch (ArgumentException)
			{
				return;
				//this message box library throws an exception if you have already defined this triggerName.
				//for us, this might just mean that you opened another window the same kind or something like that.
			}

			msgBox.Caption = caption;
			msgBox.Text = text;
			msgBox.SaveResponseText = Resources.DonTShowThisAgain;
			msgBox.AllowSaveResponse = displayDontShowAgainButton;
			msgBox.UseSavedResponse = displayDontShowAgainButton;
			switch (iconName)
			{
				case "info":
					msgBox.Icon = MessageBoxExIcon.Information;
					break;
				case "exclamation":
					msgBox.Icon = MessageBoxExIcon.Exclamation;
					break;
			}
		}

		/// <summary/>
		public static void DefineMessageBox(string triggerName, string caption, string text,
			bool displayDontShowAgainButton, Icon icon)
		{
			// Don't dispose msgBox here. MessageBoxExManager holds a reference to it and will dispose it eventually.
			MessageBoxEx msgBox;
			try
			{
				msgBox = CreateMessageBox(triggerName);
			}
			catch (ArgumentException)
			{
				return;
				//this message box library throws an exception if you have already defined this triggerName.
				//for us, this might just mean that you opened another window the same kind or something like that.
			}

			msgBox.Caption = caption;
			msgBox.Text = text;
			msgBox.SaveResponseText = Resources.DonTShowThisAgain;
			msgBox.AllowSaveResponse = displayDontShowAgainButton;
			msgBox.UseSavedResponse = displayDontShowAgainButton;
			msgBox.CustomIcon = icon;
		}

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

		/// <summary>
		/// load with properties stored
		///  in the settings file, if that file is found.
		/// </summary>
		public static void ReadSettingsFile()
		{
			string path = DirectoryFinder.CommonAppDataFolder("FieldWorks");

			if (!File.Exists(path))
				return;

			StreamReader reader = null;
			try
			{
				XmlSerializer szr = new XmlSerializer(typeof(StringPair[]));
				reader = new StreamReader(path);

				StringPair[] list = (StringPair[])szr.Deserialize(reader);
				ReadStringPairArrayForDeserializing(list);
			}
			catch (System.IO.FileNotFoundException)
			{
				//don't do anything
			}
			catch (Exception)
			{
				var activeForm = Form.ActiveForm;
				if (activeForm == null)
				{
					MessageBoxUtils.Show(Resources.CannotRestoreSavedResponses);
				}
				else
				{
					// Make sure as far as possible it comes up in front of any active window, including the splash screen.
					activeForm.Invoke((Func<DialogResult>)(() => MessageBoxUtils.Show(activeForm, Resources.CannotRestoreSavedResponses, string.Empty)));
				}
			}
			finally
			{
				if (reader != null)
					reader.Dispose();
			}
		}

		private static void ReadStringPairArrayForDeserializing(StringPair[] list)
		{
			foreach (StringPair pair in list)
			{
				//I know it is strange, but the serialization code will give us a
				//	null property if there were no other properties.
				if (pair != null)
				{
					AddSavedResponsesSafely(SavedResponses, pair);
				}
			}
		}

		private static void AddSavedResponsesSafely(Dictionary<string, string> responseDict, StringPair pair)
		{
			// The original code here threw an exception if the pair key was already in the dictionary.
			// We don't want to overwrite what's in memory with what's on disk, so we'll skip them in that case.
			string dummyValue;
			if (responseDict.TryGetValue(pair.key, out dummyValue))
				return;
			responseDict.Add(pair.key, pair.value);
		}

		/// <summary />
		public static Dictionary<string, string> SavedResponses
		{
			get
			{
				return s_savedResponses;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="triggerName"></param>
		/// <returns></returns>
		static public string Trigger(string triggerName)
		{
			// Don't dispose the msgbox here - we store it and re-use it. Will be disposed in our Dispose() method.
			var msgBox = GetMessageBox(triggerName);
			if (msgBox == null)
			{
				throw new ApplicationException("Could not find the message box with trigger name = " + triggerName);
			}
			return msgBox.Show();
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

		/// <summary />
		public static void WriteSavedResponses(Stream stream)
		{
			throw new NotImplementedException("This feature has not yet been implemented");
		}

		/// <summary />
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
		/// <summary />
		[Serializable]
		internal class StringPair
		{
			/// <summary>
			/// required for XML serialization
			/// </summary>
			public StringPair()
			{
			}

			/// <summary />
			public string key;
			/// <summary />
			public string value;
		}
	}
}
