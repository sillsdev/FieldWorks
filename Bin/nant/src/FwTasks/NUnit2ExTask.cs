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

// Mike Two (2@thoughtworks.com or mike2@nunit.org)
// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.NUnit2.Types;


namespace NAnt.NUnit2.Tasks {
	/// <summary>Runs tests using the NUnit framework.</summary>
	/// <remarks>
	///   <para>See the <a href="http://nunit.sf.net">NUnit home page</a> for more information.</para>
	/// </remarks>
	/// <example>
	///   <para>Run tests in the <c>MyProject.Tests.dll</c> assembly.</para>
	///   <code>
	/// <![CDATA[
	/// <nunit2>
	///     <test assemblyname="MyProject.Tests.dll"/>
	/// </nunit2>
	/// ]]>
	///   </code>
	///   <para>Run all tests in files listed in the <c>tests.txt</c> file.</para>
	///   <code>
	/// <![CDATA[
	/// <nunit2>
	///     <test>
	///         <includesList name="tests.txt" />
	///     </test>
	/// </nunit2>
	/// ]]>
	///   </code>
	/// </example>
	[TaskName("nunit2ex")]
	public class NUnit2ExTask : ExternalProgramBase
	{
		private string m_excludedCategories;
		private readonly NUnit2TestCollection m_tests = new NUnit2TestCollection();
		private string m_cmdLine;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the nunit-console executable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string NUnitName
		{
			get
			{
				if (Environment.OSVersion.Platform != PlatformID.Unix)
					return UseX86 ? "nunit-console-x86.exe" : "nunit-console.exe";
				return UseX86 ? "nunit-console-x86" : "nunit-console";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Excluded categories for tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("excludedCategories")]
		public string ExcludedCategories
		{
			get { return m_excludedCategories; }
			set { m_excludedCategories = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether to use the X86 version of nunit.
		/// </summary>
		/// <value></value>
		[TaskAttribute("useX86")]
		public bool UseX86 { get; set; }

		/// <summary>
		/// Tests to run.
		/// </summary>
		[BuildElementArray("test")]
		public NUnit2TestCollection Tests
		{
			get { return m_tests; }
		}

		/// <summary>
		/// Gets or sets the framework that will be used to run the tests.
		/// </summary>
		[TaskAttribute("framework")]
		public string Framework { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProgramFileName
		{
			get
			{
				string nunit = CheckNUnitPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NUnit")) ??
					(CheckNUnitPath(Assembly.GetExecutingAssembly()) ?? CheckNUnitPath(Assembly.GetEntryAssembly()));
				if (nunit == null)
				{
					// If we can't find nunit-console.exe in the directory where where NAnt is, we don't
					// specify a path and let the OS try to find a version of nunit-console.exe
					Log(Level.Verbose, NUnitName + " not found; let OS try to find it.");
					return ExeName;
				}
				Log(Level.Verbose, "Found " + NUnitName + " in " + nunit);
				return nunit;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check if nunit-console.exe exists in the location of <paramref name="assembly"/>
		/// and returns the path to it if it does. Otherwise return <c>null</c>.
		/// </summary>
		/// <param name="assembly">An assembly</param>
		/// <returns>The path to nunit-console.exe if it exists in the location where
		/// <paramref name="assembly"/> resides; otherwise <c>null</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private string CheckNUnitPath(Assembly assembly)
		{
			return CheckNUnitPath(Path.GetDirectoryName(assembly.Location));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check if nunit-console.exe exists in the specified <paramref name="directory"/>
		/// returns the path to the executable if it does. Otherwise return <c>null</c>.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <returns>
		/// The path to nunit-console.exe if it exists in <paramref name="directory"/>;
		/// otherwise <c>null</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private string CheckNUnitPath(string directory)
		{
			string nunit = Path.Combine(directory, NUnitName);
			Log(Level.Verbose, "Checking for " + NUnitName + " in " + nunit);
			if (File.Exists(nunit))
				return nunit;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProgramArguments
		{
			get { return m_cmdLine; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			foreach (var test in m_tests)
			{
				var assemblies = GetTestAssemblies(test);
				foreach (var assembly in assemblies)
				{
					var bldr = new StringBuilder();
					if (m_excludedCategories != null)
					{
						bldr.AppendFormat(@"-exclude=""{0}"" ",
							m_excludedCategories);
					}

					bldr.AppendFormat(@"""{0}"" -xml=""{0}-results.xml""", assembly);
					if (!Verbose)
						bldr.Append(" -nologo");

					if (!string.IsNullOrEmpty(Framework))
						bldr.AppendFormat(" -framework={0}", Framework);

					m_cmdLine = bldr.ToString();
					base.ExecuteTask();
				}
			}
		}

		private static StringCollection GetTestAssemblies(NUnit2Test test)
		{
			var files = new StringCollection();

			if ( test.AssemblyFile.FullName != null )
				files.Add(test.AssemblyFile.FullName);
			else
				files = test.Assemblies.FileNames;

			return files;
		}
	}
}