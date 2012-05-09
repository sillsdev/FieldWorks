using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.FwUtils;
using System.IO;
using SIL.Utils;
using Utils.MessageBoxExLib;

namespace XCore
{
	/// <summary>
	/// a subclass of the (open-source) message box manager which interfaces it to XCore
	/// </summary>
	public class XMessageBoxExManager : IFWDisposable
	{
		protected static Dictionary<string, XMessageBoxExManager> s_singletonMessageBoxExManager =
			new Dictionary<string, XMessageBoxExManager>();
		private readonly string m_appName;

		/// <summary>
		/// this can be called repeatedly, but only one will be made.
		/// </summary>
		/// <returns></returns>
		public static XMessageBoxExManager CreateXMessageBoxExManager(string appName)
		{
			if (!s_singletonMessageBoxExManager.ContainsKey(appName))
				s_singletonMessageBoxExManager.Add(appName, new XMessageBoxExManager(appName));

			return s_singletonMessageBoxExManager[appName];
		}

		/// <summary>
		/// only the factory method can make one of these
		/// </summary>
		/// <param name="appName">The application name</param>
		private XMessageBoxExManager(string appName)
		{
			m_appName = appName;
		}

		/// <summary/>
		public void DefineMessageBox(string triggerName, string caption, string text,
			bool displayDontShowAgainButton, string iconName)
		{
			// Don't dispose msgBox here. MessageBoxExManager holds a reference to it and will dispose it eventually.
			MessageBoxEx msgBox;
			try
			{
				msgBox = MessageBoxExManager.CreateMessageBox(triggerName);
			}
			catch (ArgumentException)
			{
					return;
					//this message box library throws an exception if you have already defined this triggerName.
						//for us, this might just mean that you opened another window the same kind or something like that.
			}

				msgBox.Caption = caption;
				msgBox.Text = text;
				msgBox.SaveResponseText = xCoreInterfaces.DonTShowThisAgain;
				msgBox.AllowSaveResponse = displayDontShowAgainButton;
				msgBox.UseSavedResponse = displayDontShowAgainButton;
			switch(iconName)
			{
				default:
						msgBox.CustomIcon = GetIcon(iconName);
						//should be a bmp in the resources
					break;
				case "info":
					msgBox.Icon = MessageBoxExIcon.Information;
					break;
				case "exclamation":
					msgBox.Icon = MessageBoxExIcon.Exclamation;
					break;
			}
		}

		protected System.Drawing.Icon GetIcon(string name)
		{
				name+=".ico";

				Stream iconStream = null;
				System.Reflection.Assembly assembly = System.Reflection.Assembly.GetAssembly(GetType());
				foreach(string resourcename in assembly.GetManifestResourceNames())
				{
					if(resourcename.EndsWith(name))
					{
						iconStream = assembly.GetManifestResourceStream(resourcename);
						Debug.Assert(iconStream != null, "Could not load the " + name + " resource.");
						break;
					}
				}
				Debug.Assert(iconStream != null, "Could not load the " + name + " resource.");
				return  new System.Drawing.Icon(iconStream);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="triggerName"></param>
		/// <returns></returns>
		static public string Trigger(string triggerName)
		{
			// Don't dispose the msgbox here - we store it and re-use it. Will be disposed in our Dispose() method.
			var msgBox = MessageBoxExManager.GetMessageBox(triggerName);
			if(msgBox==null)
			{
				throw new ApplicationException ("Could not find the message box with trigger name = "+triggerName);
			}
			return msgBox.Show();
		}

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
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~XMessageBoxExManager()
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				WriteSavedResponses();
				if (s_singletonMessageBoxExManager.Count == 1)
					MessageBoxExManager.DisposeAllMessageBoxes();

				s_singletonMessageBoxExManager.Remove(m_appName);
			}

			IsDisposed = true;
		}
		#endregion IDisposable & Co. implementation

		//This is complete overkill, using the xmlserializer. But I already had similar code for the xcore property table
		#region persistUserChoices

		private static StringPair[] MakeArrayForSerializing()
		{
			var list = new StringPair[MessageBoxExManager.SavedResponses.Count];
			int i = 0;
			foreach (string key in MessageBoxExManager.SavedResponses.Keys)
			{
				var pair = new StringPair {key = key, value = MessageBoxExManager.SavedResponses[key]};
				list.SetValue(pair, i);
				i++;
			}

			return list;
		}

		public void WriteSavedResponses()
		{
			CheckDisposed();

			StreamWriter writer = null;
			try
			{
				writer = new StreamWriter(SettingsPath());
				XmlSerializer szr = new XmlSerializer(typeof(StringPair[]));

				szr.Serialize(writer, MakeArrayForSerializing());
			}
			catch(Exception err)
			{
				throw new ApplicationException ("There was a problem saving the dialog responses.", err);
			}
			finally
			{
				if (writer != null)
					writer.Dispose();
			}
		}

		/// <summary>
		/// load with properties stored
		///  in the settings file, if that file is found.
		/// </summary>
		/// <param name="settingsId">e.g. "itinerary"</param>
		/// <returns></returns>
		public void  ReadSettingsFile ()
		{
			CheckDisposed();

			string path = SettingsPath();

			if(!System.IO.File.Exists(path))
				return;

			System.IO.StreamReader reader =null;
			try
			{
				XmlSerializer szr = new XmlSerializer(typeof(StringPair[]));
				reader = new System.IO.StreamReader(path);

				StringPair[] list = (StringPair[])szr.Deserialize(reader);
				ReadStringPairArrayForDeserializing(list);
			}
			catch(System.IO.FileNotFoundException)
			{
				//don't do anything
			}
			catch(Exception )
			{
				var activeForm = Form.ActiveForm;
				if (activeForm == null)
					MessageBoxUtils.Show(xCoreInterfaces.CannotRestoreSavedResponses);
				else
				{
					// Make sure as far as possible it comes up in front of any active window, including the splash screen.
					activeForm.Invoke((Func<DialogResult>)(() => MessageBoxUtils.Show(activeForm, xCoreInterfaces.CannotRestoreSavedResponses, string.Empty)));
				}
			}
			finally
			{
				if (reader != null)
					reader.Dispose();
			}
		}

		private void ReadStringPairArrayForDeserializing(StringPair[] list)
		{
			foreach (StringPair pair in list)
			{
				//I know it is strange, but the serialization code will give us a
				//	null property if there were no other properties.
				if (pair != null)
				{
					AddSavedResponsesSafely(MessageBoxExManager.SavedResponses, pair);
				}
			}
		}

		private void AddSavedResponsesSafely(Dictionary<string, string> responseDict, StringPair pair)
		{
			// The original code here threw an exception if the pair key was already in the dictionary.
			// We don't want to overwrite what's in memory with what's on disk, so we'll skip them in that case.
			string dummyValue;
			if(responseDict.TryGetValue(pair.key, out dummyValue))
				return;
			responseDict.Add(pair.key, pair.value);
		}

		private string SettingsPath()
		{
			string path = DirectoryFinder.UserAppDataFolder(m_appName);
			Directory.CreateDirectory(path);
			path = Path.Combine(path, "DialogResponses.xml");
			return path;
		}

		#endregion
	}
	[Serializable]
	public class StringPair
	{
		/// <summary>
		/// required for XML serialization
		/// </summary>
		public StringPair()
		{
		}

		public string key;
		public string value;
	}
}
