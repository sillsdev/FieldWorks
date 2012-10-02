//From:http://www.codeproject.com/cs/miscctrl/MessageBoxEx.asp
using System;
using System.IO;
using System.Collections.Generic;
using System.Resources;
using System.Reflection;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace Utils.MessageBoxExLib
{
	/// <summary>
	/// Manages a collection of MessageBoxes. Basically manages the
	/// saved response handling for messageBoxes.
	/// </summary>
	public class MessageBoxExManager : IFWDisposable
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
				Assembly current = typeof(MessageBoxExManager).Assembly;
				string[] resources = current.GetManifestResourceNames();
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

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~MessageBoxExManager()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach (MessageBoxEx mb in s_messageBoxes.Values)
				{
					mb.Dispose();
				}
				if (s_messageBoxes != null)
					s_messageBoxes.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			s_messageBoxes = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Methods
		/// <summary>
		/// Creates a new message box with the specified name. If null is specified
		/// in the message name then the message box is not managed by the Manager and
		/// will be disposed automatically after a call to Show()
		/// </summary>
		/// <param name="name">The name of the message box</param>
		/// <returns>A new message box</returns>
		public static MessageBoxEx CreateMessageBox(string name)
		{
			if (name == null)
				throw new ArgumentNullException("'name' parameter cannot be null");

			if (s_messageBoxes.ContainsKey(name))
			{
				string err = string.Format("A MessageBox with the name {0} already exists.",name);
				throw new ArgumentException(err,"name");
			}

			MessageBoxEx msgBox = new MessageBoxEx();
			msgBox.Name = name;
			s_messageBoxes[name] = msgBox;

			return msgBox;
		}

		protected static Dictionary<string, string> SavedResponses
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
