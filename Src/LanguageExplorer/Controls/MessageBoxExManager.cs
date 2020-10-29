// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
// From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Manages a collection of MessageBoxes. Basically manages the
	/// saved response handling for messageBoxes.
	/// </summary>
	internal static class MessageBoxExManager
	{
		#region Fields
		private static readonly Dictionary<string, MessageBoxEx> s_messageBoxes = new Dictionary<string, MessageBoxEx>();
		#endregion

		#region Methods

		/// <summary/>
		internal static void DefineMessageBox(string triggerName, string caption, string text, bool displayDontShowAgainButton, string iconName)
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
			msgBox.SaveResponseText = LanguageExplorerControls.DonTShowThisAgain;
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
		private static MessageBoxEx CreateMessageBox(string name)
		{
			Guard.AgainstNull(name, nameof(name));
			if (s_messageBoxes.ContainsKey(name))
			{
				throw new ArgumentException($"A MessageBox with the name {name} already exists.", nameof(name));
			}

			var msgBox = new MessageBoxEx
			{
				Name = name
			};
			s_messageBoxes[name] = msgBox;

			return msgBox;
		}

		/// <summary>
		/// load with properties stored
		///  in the settings file, if that file is found.
		/// </summary>
		internal static void ReadSettingsFile()
		{
			var path = FwDirectoryFinder.CommonAppDataFolder("FieldWorks");
			if (!File.Exists(path))
			{
				return;
			}
			StreamReader reader = null;
			try
			{
				var szr = new XmlSerializer(typeof(StringPair[]));
				reader = new StreamReader(path);
				var list = (StringPair[])szr.Deserialize(reader);
				ReadStringPairArrayForDeserializing(list);
			}
			catch (FileNotFoundException)
			{
				//don't do anything
			}
			catch (Exception)
			{
				var activeForm = Form.ActiveForm;
				if (activeForm == null)
				{
					MessageBoxUtils.Show(LanguageExplorerControls.CannotRestoreSavedResponses);
				}
				else
				{
					// Make sure as far as possible it comes up in front of any active window, including the splash screen.
					activeForm.Invoke((Func<DialogResult>)(() => MessageBoxUtils.Show(activeForm, LanguageExplorerControls.CannotRestoreSavedResponses, string.Empty)));
				}
			}
			finally
			{
				reader?.Dispose();
			}
		}

		private static void ReadStringPairArrayForDeserializing(StringPair[] list)
		{
			foreach (var pair in list)
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
			if (responseDict.TryGetValue(pair.key, out _))
			{
				return;
			}
			responseDict.Add(pair.key, pair.value);
		}

		/// <summary />
		private static Dictionary<string, string> SavedResponses { get; } = new Dictionary<string, string>();

		/// <summary />
		internal static string Trigger(string triggerName)
		{
			// Don't dispose the msgbox here - we store it and re-use it. Will be disposed in our Dispose() method.
			var msgBox = GetMessageBox(triggerName);
			if (msgBox == null)
			{
				throw new ApplicationException($"Could not find the message box with trigger name = {triggerName}");
			}
			return msgBox.Show();
		}

		/// <summary>
		/// Gets the message box with the specified name
		/// </summary>
		/// <param name="name">The name of the message box to retrieve</param>
		/// <returns>The message box with the specified name or null if a message box
		/// with that name does not exist</returns>
		private static MessageBoxEx GetMessageBox(string name)
		{
			s_messageBoxes.TryGetValue(name, out var result);
			return result;
		}

		/// <summary>
		/// Disposes all stored message boxes
		/// </summary>
		internal static void DisposeAllMessageBoxes()
		{
			foreach (var messageBox in s_messageBoxes.Values)
			{
				messageBox.Dispose();
			}
			s_messageBoxes.Clear();
		}

		/// <summary>
		/// Reset the saved response for the message box with the specified name.
		/// </summary>
		/// <param name="messageBoxName">The name of the message box whose response is to be reset.</param>
		private static void ResetSavedResponse(string messageBoxName)
		{
			if (messageBoxName == null)
			{
				return;
			}
			SavedResponses.Remove(messageBoxName);
		}
		#endregion

		#region Internal Methods
		/// <summary>
		/// Set the saved response for the specified message box
		/// </summary>
		/// <param name="msgBox">The message box whose response is to be set</param>
		/// <param name="response">The response to save for the message box</param>
		private static void SetSavedResponse(MessageBoxEx msgBox, string response)
		{
			if (msgBox.Name == null)
			{
				return;
			}
			SavedResponses[msgBox.Name] = response;
		}

		/// <summary>
		/// Gets the saved response for the specified message box
		/// </summary>
		/// <param name="msgBox">The message box whose saved response is to be retrieved</param>
		/// <returns>The saved response if exists, null otherwise</returns>
		private static string GetSavedResponse(MessageBoxEx msgBox)
		{
			var msgBoxName = msgBox.Name;
			if (msgBoxName == null)
			{
				return null;
			}
			string result = null;
			if (SavedResponses.ContainsKey(msgBoxName))
			{
				result = SavedResponses[msgBoxName];
			}
			return result;
		}
		#endregion

		/// <summary>
		/// Standard MessageBoxEx icons
		/// </summary>
		private enum MessageBoxExIcon
		{
			/// <summary />
			Exclamation,
			/// <summary />
			Information
		}

		/// <summary />
		[Serializable]
		private sealed class StringPair
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

		/// <summary>
		/// An extended MessageBox with lot of customizing capabilities.
		/// </summary>
		private sealed class MessageBoxEx : IDisposable
		{
			private MessageBoxExForm _msgBox = new MessageBoxExForm();

			#region Properties

			/// <summary>
			/// Get/Set the name of the message box.
			/// </summary>
			internal string Name { get; set; }

			/// <summary>
			/// Sets the caption of the message box
			/// </summary>
			internal string Caption
			{
				set => _msgBox.Caption = value;
			}

			/// <summary>
			/// Sets the text of the message box
			/// </summary>
			internal string Text
			{
				set => _msgBox.Message = value;
			}

			/// <summary>
			/// Sets the icon to show in the message box
			/// </summary>
			internal MessageBoxExIcon Icon
			{
				set => _msgBox.StandardIcon = (MessageBoxIcon)Enum.Parse(typeof(MessageBoxIcon), value.ToString());
			}

			/// <summary>
			/// Sets or Gets the ability of the  user to save his/her response
			/// </summary>
			internal bool AllowSaveResponse
			{
				set => _msgBox.AllowSaveResponse = value;
			}

			/// <summary>
			/// Sets the text to show to the user when saving his/her response
			/// </summary>
			internal string SaveResponseText
			{
				set => _msgBox.SaveResponseText = value;
			}

			/// <summary>
			/// Sets or Gets whether the saved response if available should be used
			/// </summary>
			internal bool UseSavedResponse { get; set; } = true;

			#endregion

			#region Methods
			/// <summary>
			/// Shows the message box
			/// </summary>
			internal string Show()
			{
				return Show(null);
			}

			/// <summary>
			/// Shows the message box with the specified owner
			/// </summary>
			private string Show(IWin32Window owner)
			{
				if (UseSavedResponse && Name != null)
				{
					var savedResponse = GetSavedResponse(this);
					if (savedResponse != null)
					{
						return savedResponse;
					}
				}
				if (owner == null)
				{
					_msgBox.Name = Name;//needed for nunitforms support
					_msgBox.ShowDialog();
				}
				else
				{
					_msgBox.ShowDialog(owner);
				}
				if (Name != null)
				{
					if (_msgBox.AllowSaveResponse && _msgBox.SaveResponse)
					{
						SetSavedResponse(this, _msgBox.Result);
					}
					else
					{
						ResetSavedResponse(this.Name);
					}
				}
				else
				{
					Dispose();
				}

				return _msgBox.Result;
			}

			#endregion

			~MessageBoxEx()
			{
				Dispose(false);
			}

			/// <inheritdoc />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private bool IsDisposed { get; set; }

			private void Dispose(bool disposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					_msgBox?.Dispose();
				}

				_msgBox = null;

				IsDisposed = true;
			}
		}
	}
}