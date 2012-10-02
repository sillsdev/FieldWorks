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
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Xml.Serialization;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Util;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

using NAnt.NUnit.Types;
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
	[TaskName("nunit2exworkaround")]
	public class NUnit2ExTaskWorkaround : NUnit2ExTask
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProgramFileName
		{
			get { return "RunAsUser.exe"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProgramArguments
		{
			get { return base.ProgramFileName + " " + base.ProgramArguments; }
		}
	}
}