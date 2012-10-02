namespace SILConvertersOffice
{
	using System;
	using Extensibility;
	using System.Runtime.InteropServices;
	using System.Reflection;                // for InvokeMember
	using System.Diagnostics;               // for Process
	using Microsoft.Win32;                  // for RegistryKey

	#region Read me for Add-in installation and setup information.
	// When run, the Add-in wizard prepared the registry for the Add-in.
	// At a later time, if the Add-in becomes unavailable for reasons such as:
	//   1) You moved this project to a computer other than which is was originally created on.
	//   2) You chose 'Yes' when presented with a message asking if you wish to remove the Add-in.
	//   3) Registry corruption.
	// you will need to re-register the Add-in by building the SILConvertersOfficeSetup project,
	// right click the project in the Solution Explorer, then choose install.
	#endregion

	/// <summary>
	///   The object for implementing an Add-in.
	/// </summary>
	/// <seealso class='IDTExtensibility2' />
	[GuidAttribute("31B69269-4301-4522-8446-3C3D3ACF7ABE"), ProgId("SILConvertersOffice.Connect")]
	public class Connect : Object, Extensibility.IDTExtensibility2
	{
		/// <summary>
		///		Implements the constructor for the Add-in object.
		///		Place your initialization code within this method.
		/// </summary>
		public Connect()
		{
		}

		/// <summary>
		///      Implements the OnConnection method of the IDTExtensibility2 interface.
		///      Receives notification that the Add-in is being loaded.
		/// </summary>
		/// <param term='application'>
		///      Root object of the host application.
		/// </param>
		/// <param term='connectMode'>
		///      Describes how the Add-in is being loaded.
		/// </param>
		/// <param term='addInInst'>
		///      Object representing this Add-in.
		/// </param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, Extensibility.ext_ConnectMode connectMode, object addInInst, ref System.Array custom)
		{
			string strAppName = (string)application.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, application, null);
			if (strAppName == "Microsoft Word")
				 appThis = new WordApp(application);
			else if (strAppName == "Microsoft Excel")
				appThis = new ExcelApp(application);
			else if (strAppName == "Microsoft Publisher")
				appThis = new PubApp(application);
			else if (strAppName == "Microsoft Access")
				appThis = new AccessApp(application);
			else
			{
				string strError = String.Format("The '{0}' application is not supported!", strAppName);
				System.Windows.Forms.MessageBox.Show(strError, OfficeApp.cstrCaption);
				throw new Exception(strError);
			}

			if (connectMode != Extensibility.ext_ConnectMode.ext_cm_Startup)
			{
				OnStartupComplete(ref custom);
			}
		}

		/// <summary>
		///     Implements the OnDisconnection method of the IDTExtensibility2 interface.
		///     Receives notification that the Add-in is being unloaded.
		/// </summary>
		/// <param term='disconnectMode'>
		///      Describes how the Add-in is being unloaded.
		/// </param>
		/// <param term='custom'>
		///      Array of parameters that are host application specific.
		/// </param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(Extensibility.ext_DisconnectMode disconnectMode, ref System.Array custom)
		{
			if (disconnectMode != ext_DisconnectMode.ext_dm_HostShutdown)
			{
				OnBeginShutdown(ref custom);
			}

			// appThis = null;
		}

		/// <summary>
		///      Implements the OnAddInsUpdate method of the IDTExtensibility2 interface.
		///      Receives notification that the collection of Add-ins has changed.
		/// </summary>
		/// <param term='custom'>
		///      Array of parameters that are host application specific.
		/// </param>
		/// <seealso class='IDTExtensibility2' />
		public void OnAddInsUpdate(ref System.Array custom)
		{
		}

		/// <summary>
		///      Implements the OnStartupComplete method of the IDTExtensibility2 interface.
		///      Receives notification that the host application has completed loading.
		/// </summary>
		/// <param term='custom'>
		///      Array of parameters that are host application specific.
		/// </param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref System.Array custom)
		{
			Application.LoadMenu();
		}

		/// <summary>
		///      Implements the OnBeginShutdown method of the IDTExtensibility2 interface.
		///      Receives notification that the host application is being unloaded.
		/// </summary>
		/// <param term='custom'>
		///      Array of parameters that are host application specific.
		/// </param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref System.Array custom)
		{
			appThis.Release();
			appThis = null;
		}

		private OfficeApp appThis = null;
		internal OfficeApp Application
		{
			get { return appThis; }
		}

		#region Registration Methods
#if DEBUG
		protected static RegistryKey GetOfficeAppRegistryKey(RegistryKey keyComAddinRoot, string strOfficeApp, Type t)
		{
			string strSubKey = String.Format(@"{0}\Addins\{1}", strOfficeApp, t.FullName);
			return keyComAddinRoot.CreateSubKey(strSubKey);
		}

		protected static void RegisterOfficeApp(RegistryKey keyComAddinRoot, string strOfficeApp, Type t)
		{
			RegistryKey keySubKey = GetOfficeAppRegistryKey(keyComAddinRoot, strOfficeApp, t);
			if (keySubKey != null)
			{
				keySubKey.SetValue("Description", "Providing access to SILConverters from Office 2003 applications");
				keySubKey.SetValue("FriendlyName", "SILConverters for Office 2003");
				keySubKey.SetValue("LoadBehavior", 3, RegistryValueKind.DWord);
			}
		}

		[ComRegisterFunctionAttribute]
		public static void RegisterFunction(Type t)
		{
			// For debug, we don't use the Shim, so we have to tell the registry to use us (i.e.
			//  the managed com add-in) instead
			RegistryKey keyComAddinRoot = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Office");
			RegisterOfficeApp(keyComAddinRoot, "Word", t);
			RegisterOfficeApp(keyComAddinRoot, "Publisher", t);
			RegisterOfficeApp(keyComAddinRoot, "Excel", t);
			RegisterOfficeApp(keyComAddinRoot, "Access", t);
		}

		protected static RegistryKey GetOfficeAppRegistryKey(RegistryKey keyComAddinRoot, string strOfficeApp)
		{
			string strSubKey = String.Format(@"{0}\Addins", strOfficeApp);
			return keyComAddinRoot.CreateSubKey(strSubKey);
		}

		protected static void UnregisterOfficeApp(RegistryKey keyComAddinRoot, string strOfficeApp, Type t)
		{
			RegistryKey keySubKey = GetOfficeAppRegistryKey(keyComAddinRoot, strOfficeApp);
			if (keySubKey != null)
				keySubKey.DeleteSubKeyTree(t.FullName);
		}

		[ComUnregisterFunctionAttribute]
		public static void UnregisterFunction(Type t)
		{
			try
			{
				RegistryKey keyComAddinRoot = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Office");
				UnregisterOfficeApp(keyComAddinRoot, "Word", t);
				UnregisterOfficeApp(keyComAddinRoot, "Publisher", t);
				UnregisterOfficeApp(keyComAddinRoot, "Excel", t);
				UnregisterOfficeApp(keyComAddinRoot, "Access", t);
			}
			catch { }
		}
#endif
		#endregion Registration Methods
	}
}