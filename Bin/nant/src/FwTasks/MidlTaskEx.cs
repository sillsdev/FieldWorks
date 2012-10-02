//
// NAntContrib
// Copyright (C) 2002 Tomas Restrepo (tomasr@mvps.org)
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

namespace NAnt.Contrib.Tasks
{

	/// <summary>
	/// This tasks allows you to run MIDL.exe.
	/// </summary>
	/// <remarks>
	/// This task only supports a small subset of the
	/// MIDL.EXE command line switches, but you can use
	/// the options element to specify any other
	/// unsupported commands you want to specify.
	/// </remarks>
	/// <example>
	///   <code><![CDATA[
	///   <midl
	///      env="win32"
	///      Oi="cf"
	///      tlb="${outputdir}\TempAtl.tlb"
	///      header="${outputdir}\TempAtl.h"
	///      iid="${outputdir}\TempAtl_i.c"
	///      proxy="${outputdir}\TempAtl_p.c"
	///      filename="TempAtl.idl"
	///   >
	///      <defines>
	///         <option name="_DEBUG"/>
	///         <option name="WIN32" value="1"/>
	///      </defines>
	///      <options>
	///         <option name="/mktyplib203"/>
	///         <option name="/error" value="allocation"/>
	///      </options>
	///   </midl>
	///   ]]></code>
	/// </example>
	[TaskName("midlex")]
	public class MidlExTask : ExternalProgramBase
	{
		const string PROG_FILE_NAME = "midl.exe";

	  #region Private Variables
		private string _acf;
		private string _align;
		private bool _appConfig;
		private string _args;
		private string _char;
		private string _client;
		private string _cstub;
		// TODO: /D!!!!!
		private string _dlldata;
		private string _env = "win32";
		// TODO: /error
		private string _header;
		private string _iid;
		private string _Oi = null;
		private string _proxy;
		private string _tlb;
		private string _filename;
		private OptionCollection _options = new OptionCollection();
		private OptionCollection _defines = new OptionCollection();
		private string _rawoptions;
		private string _output;
	  #endregion // Private Variables

		/// <summary>
		/// The /acf switch allows the user to supply an
		/// explicit ACF file name. The switch also
		/// allows the use of different interface names in
		/// the IDL and ACF files.
		/// </summary>
		[TaskAttribute("acf")]
		public string Acf
		{
			get { return _acf; }
			set { _acf = value; }
		}

		/// <summary>
		/// The /align switch is functionally the same as the
		/// MIDL /Zp option and is recognized by the MIDL compiler
		/// solely for backward compatibility with MkTypLib.
		/// </summary>
		/// <remarks>The alignment value can be 1, 2, 4, or 8.</remarks>
		[TaskAttribute("align")]
		public string Align
		{
			get { return _align; }
			set { _align = value; }
		}

		/// <summary>
		/// The /app_config switch selects application-configuration
		/// mode, which allows you to use some ACF keywords in the
		/// IDL file. With this MIDL compiler switch, you can omit
		/// the ACF and specify an interface in a single IDL file.
		/// </summary>
		[TaskAttribute("app_config"), BooleanValidator()]
		public bool AppConfig
		{
			get { return _appConfig; }
			set { _appConfig = value; }
		}

		/// <summary>
		/// The /char switch helps to ensure that the MIDL compiler
		/// and C compiler operate together correctly for all char
		/// and small types.
		/// </summary>
		/// <remarks>Can be one of signed | unsigned | ascii7 </remarks>
		[TaskAttribute("char")]
		public string Char
		{
			get { return _char; }
			set { _char = value; }
		}

		/// <summary>
		/// The /client switch directs the MIDL compiler to generate
		/// client-side C source files for an RPC interface
		/// </summary>
		/// <remarks>can be one of stub | none</remarks>
		[TaskAttribute("client")]
		public string Client
		{
			get { return _client; }
			set { _client = value; }
		}

		/// <summary>
		/// The /cstub switch specifies the name of the client
		/// stub file for an RPC interface.
		/// </summary>
		[TaskAttribute("cstub")]
		public string CStub
		{
			get { return _cstub; }
			set { _cstub = value; }
		}

		/// <summary>
		/// The /dlldata switch is used to specify the file
		/// name for the generated dlldata file for a proxy
		/// DLL. The default file name Dlldata.c is used if
		/// the /dlldata switch is not specified.
		/// </summary>
		[TaskAttribute("dlldata")]
		public string DllData
		{
			get { return _dlldata; }
			set { _dlldata = value; }
		}

		/// <summary>
		/// The /env switch selects the
		/// environment in which the application runs.
		/// </summary>
		/// <remarks>It can take the values win32 and win64</remarks>
		[TaskAttribute("env")]
		public string Env
		{
			get { return _env; }
			set { _env = value; }
		}

		/// <summary>
		/// The /Oi switch directs the MIDL compiler to
		/// use a fully-interpreted marshaling method.
		/// The /Oic and /Oicf switches provide additional
		/// performance enhancements.
		/// </summary>
		/// <remarks>
		/// If you specify the Oi attribute, you must set it to
		/// one of the values:
		/// - Oi=""
		/// - Oi="c"
		/// - Oi="f"
		/// - Oi="cf"
		/// </remarks>
		[TaskAttribute("Oi")]
		public string Oi
		{
			get { return _Oi; }
			set { _Oi = value; }
		}

		/// <summary>
		/// The /tlb switch specifies a file name
		/// for the type library generated by the MIDL compiler.
		/// </summary>
		[TaskAttribute("tlb", Required=true)]
		public string Tlb
		{
			get { return _tlb; }
			set { _tlb = value; }
		}

		/// <summary>
		/// The /header switch specifies the name of the header file.
		/// </summary>
		[TaskAttribute("header")]
		public string Header
		{
			get { return _header; }
			set { _header = value; }
		}

		/// <summary>
		/// The /iid switch specifies the name of the
		/// interface identifier file for a COM interface,
		/// overriding the default name obtained by
		/// adding _i.c to the IDL file name.
		/// </summary>
		[TaskAttribute("iid")]
		public string Iid
		{
			get { return _iid; }
			set { _iid = value; }
		}

		/// <summary>
		/// The /proxy switch specifies the name of
		/// the interface proxy file for a COM interface.
		/// </summary>
		[TaskAttribute("proxy")]
		public string Proxy
		{
			get { return _proxy; }
			set { _proxy = value; }
		}

		/// <summary>
		/// Name of .IDL file to process.
		/// </summary>
		[TaskAttribute("filename", Required=true)]
		public string Filename
		{
			get { return _filename; }
			set { _filename = value; }
		}

		/// <summary>
		/// Raw options
		/// </summary>
		[TaskAttribute("rawoptions")]
		public string Rawoptions
		{
			get { return _rawoptions; }
			set { _rawoptions = value; }
		}

		/// <summary>Output directory for the compilation target.</summary>
		[TaskAttribute("outputdir", Required=true)]
		public string OutputDir
		{
			get { return _output; }
			set { _output = value; }
		}

		/// <summary>
		/// Additional options to pass to midl.exe.
		/// </summary>
		//[OptionSetAttribute("options")]
		[BuildElementCollection("options", "option")]
		public OptionCollection Options
		{
			get { return _options; }
		}

		/// <summary>
		/// Macro definitions to pass to mdil.exe.
		/// Each entry will generate a /D
		/// </summary>
		//[OptionSetAttribute("defines")]
		[BuildElementCollection("defines", "define")]
		public OptionCollection Defines
		{
			get { return _defines; }
		}

		/// <summary>
		/// Filename of program to execute
		/// </summary>
		public override string ProgramFileName
		{
			get { return PROG_FILE_NAME; }
		}

		/// <summary>
		/// Arguments of program to execute
		/// </summary>
		public override string ProgramArguments
		{
			get { return _args; }
		}

		///<summary>
		///Initializes task and ensures the supplied attributes are valid.
		///</summary>
		protected override void Initialize()
		{
		}

		/// <summary>
		/// This is where the work is done
		/// </summary>
		protected override void ExecuteTask()
		{
			if ( NeedsCompiling() )
			{
				string rspFile = Path.GetTempFileName();
				StreamWriter writer = new StreamWriter(rspFile);

				using ( writer )
				{
					WriteRSP(writer);
				}

				if (Verbose)
				{
					// display response file contents
					Log(Level.Info, "Contents of " + rspFile);
					StreamReader reader = File.OpenText(rspFile);
					Log(Level.Info, reader.ReadToEnd());
					reader.Close();
				}

				_args = "@" + rspFile;
				base.ExecuteTask();
			}
		}


		/// <summary>
		/// Check output files to see if we need rebuilding.
		/// </summary>
		/// <returns>true if a rebuild is needed</returns>
		protected bool NeedsCompiling()
		{
			//
			// we should check out against all four
			// output file
			//
			if ( NeedsCompiling(_tlb) )
				return true;
			if ( NeedsCompiling(_header) )
				return true;
			if ( NeedsCompiling(_iid) )
				return true;
			/*
					 if ( NeedsCompiling(_proxy) )
						return true;
			*/
			return false;

		}

		/// <summary>
		///
		/// </summary>
		/// <param name="outputFile"></param>
		/// <returns></returns>
		protected bool NeedsCompiling(string outputFile)
		{
			string fullpath = Path.GetFullPath(Path.Combine(BaseDirectory.FullName, outputFile));
			FileInfo outputFileInfo = new FileInfo(fullpath);
			if (!outputFileInfo.Exists)
			{
				return true;
			}
			StringCollection sources = new StringCollection();
			sources.Add(Path.GetFullPath(Path.Combine(BaseDirectory.FullName, _filename)));
			string fileName = FileSet.FindMoreRecentLastWriteTime(sources, outputFileInfo.LastWriteTime);
			if (fileName != null)
			{
				Log(Level.Verbose, "{0} is out of date, recompiling.", fileName);
				return true;
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="writer"></param>
		protected void WriteRSP(TextWriter writer)
		{
			writer.WriteLine("/nologo");
			writer.WriteLine("/env " + _env);

			if ( _acf != null && _acf.Length > 0)
				writer.WriteLine("/acf " + _acf);
			if ( _align != null && _align.Length > 0)
				writer.WriteLine("/align " + _align);
			if ( _appConfig )
				writer.WriteLine("/app_config");
			if ( _char != null && _char.Length > 0)
				writer.WriteLine("/char " + _char);
			if ( _client != null && _client.Length > 0)
				writer.WriteLine("/client " + _client);
			if ( _cstub != null && _cstub.Length > 0)
				writer.WriteLine("/cstub " + _cstub);
			if ( _dlldata != null && _dlldata.Length > 0)
				writer.WriteLine("/dlldata " + _dlldata);

			if ( _Oi != null && _Oi.Length > 0)
				writer.WriteLine("/Oi" + _Oi);
			if ( _tlb != null && _tlb.Length > 0)
				writer.WriteLine("/tlb " + _tlb);
			if ( _header != null && _header.Length > 0)
				writer.WriteLine("/header " + _header);
			if ( _iid != null && _iid.Length > 0)
				writer.WriteLine("/iid " + _iid);
			if ( _proxy != null && _proxy.Length > 0)
				writer.WriteLine("/proxy " + _proxy);
			if (_rawoptions != null && _rawoptions.Length > 0)
				writer.WriteLine(_rawoptions);
			if (_output != null && _output.Length > 0)
				writer.WriteLine("/out " + _output);

			foreach ( Option define in _defines )
			{
				if ( define.Value == null )
					writer.WriteLine("/D " + define.Name);
				else
					writer.WriteLine("/D " + define.Name + "=" + define.Value);
			}

			foreach ( Option option in _options )
			{
				if ( option.Value == null )
					writer.WriteLine(option.Name);
				else
					writer.WriteLine(option.Name + " " + option.Value);
			}

			writer.WriteLine(_filename);
		}


	} // class MidlTask

} // namespace NAnt.Contrib.Tasks
