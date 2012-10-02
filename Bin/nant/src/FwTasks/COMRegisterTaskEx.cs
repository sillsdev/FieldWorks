// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

// Ian MacLean (ian@maclean.ms)
//
// originally from NantContrib. Modified by Eberhard Beilharz

using System;
using System.Collections.Specialized;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Types;

namespace NAnt.Contrib.Tasks
{
	/// <summary>Register COM servers or typelibraries.</summary>
	/// <remarks>
	///     <para>COM register task will try and register any type of COM related file that needs registering. .exe files will be registered as exe servers, .tlb files registered with RegisterTypeLib and for all other filetypes it will attempt to register them as dll servers.</para>
	/// </remarks>
	/// <example>
	///   <para>Register a single dll server.</para>
	///   <code><![CDATA[<comregister file="myComServer.dll"/>]]></code>
	///   <para>Register a single exe server </para>
	///   <code><![CDATA[<comregister file="myComServer.exe"/>]]></code>
	///   <para>Register a set of COM files at once.</para>
	///   <code>
	/// <![CDATA[
	/// <comregister unregister="false">
	///     <fileset>
	///         <includes name="an_ExeServer.exe"/>
	///         <includes name="a_TypeLibrary.tlb"/>
	///         <includes name="a_DllServer.dll"/>
	///         <includes name="an_OcxServer.ocx"/>
	///     </fileset>
	/// </comregister>
	/// ]]>
	///   </code>
	/// </example>
	[TaskName("comregisterex")]
	public class COMRegisterExTask : Task
	{
		//-----------------------------------------------------------------------------
		// Typelib Imports
		//-----------------------------------------------------------------------------

		/// <summary></summary>
		[ DllImport("oleaut32.dll", EntryPoint = "LoadTypeLib",
			  CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError=true) ]
		public static extern int LoadTypeLib ( string filename, [MarshalAs(UnmanagedType.IUnknown)]ref object pTypeLib );

		/// <summary></summary>
		[ DllImport("oleaut32.dll", EntryPoint = "RegisterTypeLib",
			  CharSet=System.Runtime.InteropServices.CharSet.Auto, SetLastError=true) ]
		public static extern int RegisterTypeLib ( [MarshalAs(UnmanagedType.IUnknown)] object pTypeLib, string fullpath,  string helpdir );

		//-----------------------------------------------------------------------------
		// Kernel 32 imports
		//-----------------------------------------------------------------------------
		/// <summary></summary>
		[ DllImport("Kernel32.dll", EntryPoint = "LoadLibrary",
			  CharSet=System.Runtime.InteropServices.CharSet.Unicode, SetLastError=true) ]
		public static extern IntPtr LoadLibrary ( string fullpath );

		/// <summary></summary>
		[ DllImport("Kernel32.dll",  SetLastError=true) ]
		public static extern int FreeLibrary ( IntPtr hModule );

		/// <summary></summary>
		[ DllImport("Kernel32.dll", SetLastError=true) ]
		public static extern IntPtr GetProcAddress ( IntPtr handle, string lpprocname  );

		const int WIN32ERROR_ProcNotFound = 127;
		const int WIN32ERROR_FileNotFound = 2;

		string _fileName = null;
		bool _unregister = false;
		FileSet _fileset = new FileSet( );

		/// <summary>The name of the file to register.  This is provided as an alternate to using the task's fileset.</summary>
		[TaskAttribute("file")]
		public string FileName
		{
			get { return _fileName; }
			set { _fileName = value; }
		}
		/// <summary>Unregistering this time. ( /u paramater )Default is "false".</summary>
		[TaskAttribute("unregister")]
		[BooleanValidator()]
		public bool Unregister
		{
			get { return _unregister; }
			set { _unregister = value; }
		}

		/// <summary>the set of files to register..</summary>
		[BuildElement("fileset")]
		public FileSet COMRegisterFileSet
		{
			get { return _fileset; }
			set { _fileset = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the job.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			// add the filename to the file set
			if (FileName != null)
			{
				try
				{
					string path = Project.GetFullPath(FileName);
					COMRegisterFileSet.Includes.Add(path);
				}
				catch (Exception e)
				{
					string msg = String.Format("Could not find file '{0}'", FileName);
					if (FailOnError)
						throw new BuildException(msg, Location, e);
					Log(Level.Verbose, msg);
				}
			}
			// gather the information needed to perform the operation
			StringCollection fileNames = COMRegisterFileSet.FileNames;

			// display build log message
			Log(Level.Info, "{0} {1} files",
				Unregister ? "Unregistering" : "Registering", fileNames.Count);

			string currentDir = Directory.GetCurrentDirectory();
			try
			{
				// perform operation
				foreach (string path in fileNames)
				{
					if ( ! File.Exists(path) )
					{
						string msg = "File : " + path + " does not exist";
						if (FailOnError)
							throw new BuildException(msg, Location );

						Log(Level.Verbose, msg);
					}
					else
					{
						Log(Level.Verbose, "{0} {1}",
							Unregister ? "Unregistering" : "Registering", Path.GetFileName(path));
						Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
						try
						{
							switch(Path.GetExtension(path))
							{
								case ".tlb" :
									RegisterTypelib(path);
									break;
								case ".exe" :
									RegisterExeServer(path);
									break;
								case ".dll" :
								case ".ocx" :
								default:
									RegisterDllServer(path);
									break;
							}
						}
						catch(Exception e)
						{
							if (FailOnError)
							{
								throw new BuildException(e.Message, Location, e);
							}
							Log(Level.Verbose, e.Message);
						}
					}
				}
			}
			finally
			{
				Directory.SetCurrentDirectory(currentDir);
			}
		}

		/// <summary>
		/// Register an inproc COM server, usually a .dll or .ocx
		/// </summary>
		/// <param name="path"></param>
		void RegisterDllServer(string path)
		{

			IntPtr handle = new IntPtr();

			handle = LoadLibrary( path );
			int error = Marshal.GetLastWin32Error();
			if ( handle.ToInt32() == 0 && error != 0 )
			{
				throw new BuildException("Error loading dll : " + path, Location );
			}
			string entryPoint = "DllRegisterServer";
			string action = "register";
			if ( Unregister )
			{
				entryPoint = "DllUnregisterServer";
				action = "unregister";
			}
			IntPtr address = GetProcAddress( handle, entryPoint );
			error = Marshal.GetLastWin32Error();

			if ( address.ToInt32() == 0 && error != 0 )
			{
				string message = string.Format("Error {0}ing dll. {1} has no {2} function.", action, path, entryPoint );
				FreeLibrary(handle);
				throw new BuildException( message, Location );
			}
			// unload the library
			FreeLibrary(handle);
			error = Marshal.GetLastWin32Error();
			try
			{
				// Do the actual registration here
				DynamicPInvoke.DynamicDllFuncInvoke(path, entryPoint );
			}
			catch(Exception e)
			{
				string message = string.Format("Error during registration: {0}; {1}", e.Message,
					e.InnerException.Message);
				throw new BuildException(message, Location);
			}
		}

		/// <summary>
		/// Register a COM type library
		/// </summary>
		/// <param name="path"></param>
		void RegisterTypelib(string path)
		{
			object Typelib = new object();
			int error = 0;

			// Load typelib
			LoadTypeLib(path, ref Typelib );
			error = Marshal.GetLastWin32Error();

			if (error != 0 || (Typelib == null) )
			{
				throw new BuildException("Error loading typelib " + path, Location );
			}
			if (Unregister)
			{
				// TODO need to get access to ITypeLib interface from c#
			}
			else
			{

				//Perform registration
				RegisterTypeLib( Typelib, path, null);
				error = Marshal.GetLastWin32Error();

				if (error != 0)
				{
					throw new BuildException("Error registering typelib " + path, Location );
				}
			}
		}

		/// <summary>
		/// Register exe servers
		/// </summary>
		/// <param name="path"></param>
		void RegisterExeServer(string path)
		{

			// Create process with the /regserver flag
			Process process = new Process();

			process.StartInfo.FileName = path;
			if ( this.Unregister )
			{
				process.StartInfo.Arguments = path + " /unregserver";
			}
			else
			{
				process.StartInfo.Arguments = path + " /regserver";
			}

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WorkingDirectory = Path.GetDirectoryName(path);

			try
			{
				process.Start();
			}
			catch (Exception e)
			{
				throw new BuildException(" Error Registering "  + path + e.Message, Location );
			}

			bool exited = process.WaitForExit(5000);

			// kill if it doesn't terminate after 5s
			if (!exited || ! process.HasExited)
			{
				process.Kill();
				throw new BuildException(" Error "  + path + " is not a COM server", Location );
			}

			// check for error output. COM exe servers should not ouput to stdio on register
			StreamReader stdErr = process.StandardError;
			string errors = stdErr.ReadToEnd();
			if (errors.Length > 0)
			{
				throw new BuildException(" Error "  + path + " doesn't support the /regserver option", Location );
			}

			StreamReader stdOut = process.StandardOutput;
			string output = stdOut.ReadToEnd();
			if (output.Length > 0)
			{
				throw new BuildException(" Error "  + path + " doesn't support the /regserver option", Location );
			}
		}


		/// <summary>
		/// Helper class to dynamically build an assembly with the correct
		/// P/Invoke signature
		/// </summary>
		private class DynamicPInvoke
		{

			/// <summary>
			/// register a given dll path
			/// </summary>
			/// <param name="dll"></param>
			/// <param name="entrypoint"></param>
			/// <returns></returns>
			public static object DynamicDllFuncInvoke( string dll, string entrypoint )
			{
				Type returnType = typeof(int);
				Type [] parameterTypes = null;
				object[] parameterValues = null;
				string entryPoint = entrypoint;

				// Create a dynamic assembly and a dynamic module
				AssemblyName asmName = new AssemblyName();
				asmName.Name = "dllRegAssembly";
				AssemblyBuilder dynamicAsm =
					AppDomain.CurrentDomain.DefineDynamicAssembly(asmName,
					AssemblyBuilderAccess.Run);
				ModuleBuilder dynamicMod =
					dynamicAsm.DefineDynamicModule("tempModule");

				// Dynamically construct a global PInvoke signature
				// using the input information
				MethodBuilder dynamicMethod = dynamicMod.DefinePInvokeMethod(
					entryPoint, dll, MethodAttributes.Static | MethodAttributes.Public
					| MethodAttributes.PinvokeImpl, CallingConventions.Standard,
					returnType, parameterTypes, CallingConvention.Winapi,
					CharSet.Ansi);

				// This global method is now complete
				dynamicMod.CreateGlobalFunctions();

				// Get a MethodInfo for the PInvoke method
				MethodInfo mi = dynamicMod.GetMethod(entryPoint);

				// Invoke the static method and return whatever it returns
				return mi.Invoke(null, parameterValues);
			}
		}
	}
}
