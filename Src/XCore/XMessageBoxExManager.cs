using System;
using SIL.Utils;
using System.Diagnostics;
using System.Xml.Serialization;

namespace XCore
{
	/// <summary>
	/// a subclass of the (open-source) message box manager which interfaces it to XCore
	/// </summary>
	public class XMessageBoxExManager : Utils.MessageBoxExLib.MessageBoxExManager
	{
		static protected XMessageBoxExManager s_singletonMessageBoxExManager;

		/// <summary>
		/// this can be called repeatedly, but only one will be made.
		/// </summary>
		/// <returns></returns>
		public static XMessageBoxExManager CreateXMessageBoxExManager()
		{
			if(s_singletonMessageBoxExManager == null)
				s_singletonMessageBoxExManager = new XMessageBoxExManager();

			return s_singletonMessageBoxExManager;
		}

		/// <summary>
		/// only the factory method can make one of these
		/// </summary>
		/// <param name="mediator"></param>
		private XMessageBoxExManager()
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public void DefineMessageBox(string triggerName, string caption, string text,
			bool displayDontShowAgainButton, string iconName)
		{
			CheckDisposed();

			Utils.MessageBoxExLib.MessageBoxEx b;
			try
			{
				b = Utils.MessageBoxExLib.MessageBoxExManager.CreateMessageBox(triggerName);
			}
			catch (ArgumentException)
			{
				return;//this message box library throws an exception if you have already defined this triggerName.
						//for us, this might just mean that you opened another window the same kind or something like that.
			}

			b.Caption=caption;
			b.Text=text;
			b.SaveResponseText = xCoreInterfaces.DonTShowThisAgain;
			b.AllowSaveResponse = displayDontShowAgainButton;
			b.UseSavedResponse = displayDontShowAgainButton;
			switch(iconName)
			{
				default:
					b.CustomIcon = GetIcon(iconName);//should be a bmp in the resources
					break;
				case "info":
					b.Icon =Utils.MessageBoxExLib.MessageBoxExIcon.Information;
					break;
				case "exclamation":
					b.Icon = Utils.MessageBoxExLib.MessageBoxExIcon.Exclamation;
					break;
			}
		}

		protected System.Drawing.Icon GetIcon(string name)
		{
				name+=".ico";

				System.IO.Stream iconStream = null;
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
		/// <param name="name"></param>
		/// <returns></returns>
		static public string Trigger(string triggerName)
		{
			Utils.MessageBoxExLib.MessageBoxEx b = Utils.MessageBoxExLib.MessageBoxExManager.GetMessageBox(triggerName);
			if(b==null)
			{
				throw new ApplicationException ("Could not find the message box with trigger name = "+triggerName);
			}
			return b.Show();
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				WriteSavedResponses();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			s_singletonMessageBoxExManager = null;

			base.Dispose(disposing);

			// Make sure we aren't collected before we get done disposing,
			// since we get freed from the static data member.
			GC.KeepAlive(this);
		}

		#endregion IDisposable override


		//This is complete overkill, using the xmlserializer. But I already had similar code for the xcore property table
		#region persistUserChoices

		private StringPair[] MakeArrayForSerializing()
		{
			StringPair[] list = new StringPair[SavedResponses.Count];
			int i = 0;
			foreach (string key in SavedResponses.Keys)
			{
				StringPair pair = new StringPair();
				pair.key = key;
				pair.value = SavedResponses[key];
				list.SetValue(pair, i);
				i++;
			}

			return list;
		}

		public void WriteSavedResponses()
		{
			CheckDisposed();

			System.IO.StreamWriter writer = null;
			try
			{
				writer = new System.IO.StreamWriter(SettingsPath());
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
					writer.Close();
				writer = null;
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
				System.Windows.Forms.MessageBox.Show(xCoreInterfaces.CannotRestoreSavedResponses);
			}
			finally
			{
				if (reader != null)
					reader.Close();
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
					SavedResponses.Add(pair.key, pair.value);
				}
			}
		}

		private string SettingsPath()
		{
			string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			path = System.IO.Path.Combine(path,System.Windows.Forms.Application.CompanyName+"\\"+ System.Windows.Forms.Application.ProductName);
			System.IO.Directory.CreateDirectory(path);
			path = System.IO.Path.Combine(path,"DialogResponses.xml");
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
