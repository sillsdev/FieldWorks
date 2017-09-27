// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Xsl;
using System.Text;

namespace FwBuildTasks
{
	/// <summary>
	/// Useful utility functions.
	/// </summary>
	public static class BuildUtils
	{
		public static bool IsUnix => Environment.OSVersion.Platform == PlatformID.Unix;

		/// <summary>
		/// Return the executing assembly's location as a directory path.
		/// </summary>
		public static string GetAssemblyFolder()
		{
			var codebase = Assembly.GetExecutingAssembly().CodeBase;
			string path;
			if (codebase.StartsWith("file:///"))
				path = codebase.Substring(7);
			else
				path = codebase;
			// Handle Windows style absolute paths.
			var r = new Regex("^/[A-Za-z]:");
			if (r.IsMatch(path))
				path = path.Substring(1);
			return Path.GetDirectoryName(path);
		}

		/// <summary>
		/// Store the information for an XSLT input parameter.
		/// </summary>
		public struct XsltParam
		{
			public string Name { get; set; }
			public string Value { get; set; }
		}

		/// <summary>
		/// Apply an xslt stylesheet to a file.
		/// </summary>
		public static void ApplyXslt(string stylesheet, string resxPath, string localizedResxPath, List<XsltParam> parameters)
		{
			if (parameters == null)
				parameters = new List<XsltParam>();
			// The DotNet XSLT implementation in Mono can be rather slow, so we'll call into
			// libxslt.so on Linux.
			if (IsUnix)
			{
				//ApplyLinuxXslt(stylesheet, resxPath, localizedResxPath, parameters);
				ApplyLinuxXsltCommandLine(stylesheet, resxPath, localizedResxPath, parameters);
			}
			else
			{
				ApplyDotNetXslt(stylesheet, resxPath, localizedResxPath, parameters);
			}
		}

		[DllImport("libxml2.so.2")]
		private static extern void xmlInitParser();
		[DllImport("libxml2.so.2")]
		private static extern void xmlSubstituteEntitiesDefault(int flag);
		[DllImport("libxml2.so.2")]
		private static extern void xmlCleanupParser();
		[DllImport("libxml2.so.2")]
		private static extern IntPtr xmlParseFile([MarshalAs(UnmanagedType.LPStr)] string filename);
		[DllImport("libxml2.so.2")]
		private static extern void xmlFreeDoc(IntPtr doc);

		[DllImport("libxslt.so.1")]
		private static extern void xsltSetXIncludeDefault(int flag);
		[DllImport("libxslt.so.1")]
		private static extern void xsltCleanupGlobals();
		[DllImport("libxslt.so.1")]
		private static extern IntPtr xsltParseStylesheetFile([MarshalAs(UnmanagedType.LPStr)] string filename);
		[DllImport("libxslt.so.1")]
		private static extern void xsltFreeStylesheet(IntPtr xsl);
		[DllImport("libxslt.so.1")]
		private static extern IntPtr xsltApplyStylesheet(IntPtr xsl, IntPtr doc, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1, ArraySubType=UnmanagedType.LPStr)] string[] parameters);
		[DllImport("libxslt.so.1")]
		private static extern int xsltSaveResultToFilename(string outfile, IntPtr res, IntPtr xsl, int compress);

		[DllImport("libexslt.so.0")]
		private static extern void exsltRegisterAll();

		private static void ApplyLinuxXslt(string stylesheet, string inputFile, string outputFile, List<XsltParam> xparams)
		{
			var xsl = xsltParseStylesheetFile(stylesheet);
			if (xsl == IntPtr.Zero)
				throw new Exception($"ApplyLinuxXslt: Cannot parse XSLT file \"{stylesheet}\"");
			try
			{
				xmlInitParser();
				xmlSubstituteEntitiesDefault(1);
				xsltSetXIncludeDefault(1);
				exsltRegisterAll();
				var doc = xmlParseFile(inputFile);
				if (doc == IntPtr.Zero)
					throw new Exception($"ApplyLinuxXslt: Cannot parse XML file \"{inputFile}\"");
				var parameters = new string[2 * xparams.Count + 1];
				var i = 0;
				foreach (var xparam in xparams)
				{
					parameters[i++] = xparam.Name;
					parameters[i++] = $"'{xparam.Value}'";
				}
				parameters[i] = null;
				var res = xsltApplyStylesheet(xsl, doc, parameters);
				xmlFreeDoc(doc);
				if (res == IntPtr.Zero)
					throw new Exception($"ApplyLinuxXslt: Applying stylesheet to \"{inputFile}\" failed.");
				var ok = xsltSaveResultToFilename(outputFile, res, xsl, 0);
				xmlFreeDoc(res);
				if (ok < 0)
					throw new Exception($"ApplyLinuxXslt: Cannot save result file \"{outputFile}\"");
			}
			finally
			{
				xsltFreeStylesheet(xsl);
			}
		}

		private static void ApplyLinuxXsltCommandLine(string stylesheet, string inputFile,
			string outputFile, IEnumerable<XsltParam> xparams)
		{
			var stringParams = new StringBuilder();
			foreach (var xparam in xparams)
			{
				stringParams.Append($"--stringparam {xparam.Name} '{xparam.Value}' ");
			}

			using (var process = new Process())
			{
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.FileName = "xsltproc";
				process.StartInfo.Arguments = $"--output {outputFile} {stringParams} {stylesheet} {inputFile}";
				process.Start();

				var stdError = process.StandardError.ReadToEnd();
				process.WaitForExit();
				if (process.ExitCode != 0)
				{
					throw new ApplicationException($"xsltproc returned error {process.ExitCode} for {inputFile}. Output:\n{stdError}.");
				}
			}
		}


		private static void ApplyDotNetXslt(string stylesheet, string inputFile, string outputFile, List<XsltParam> xparams)
		{
			var transform = new XslCompiledTransform();
			// Settings are required to allow the stylesheet to use the document function to load the translation file.
			transform.Load(stylesheet, new XsltSettings(true, true), null);
			var arguments = new XsltArgumentList();
			foreach (var xparam in xparams)
				arguments.AddParam(xparam.Name, "", xparam.Value);
			var writer = new StreamWriter(outputFile, false, Encoding.UTF8);
			transform.Transform(inputFile, arguments, writer);
			writer.Close();
		}
	}
}
