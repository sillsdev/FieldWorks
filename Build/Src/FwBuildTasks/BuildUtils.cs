using System;
using System.Collections.Generic;
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
			Regex r = new Regex("^/[A-Z]:");
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
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				ApplyLinuxXslt(stylesheet, resxPath, localizedResxPath, parameters);
			}
			else
			{
				ApplyDotNetXslt(stylesheet, resxPath, localizedResxPath, parameters);
			}
		}

		[DllImport("libxml2.so.2")] static extern void xmlInitParser();
		[DllImport("libxml2.so.2")] static extern void xmlSubstituteEntitiesDefault(int flag);
		[DllImport("libxml2.so.2")] static extern void xmlCleanupParser();
		[DllImport("libxml2.so.2")] static extern IntPtr xmlParseFile([MarshalAs(UnmanagedType.LPStr)] string filename);
		[DllImport("libxml2.so.2")] static extern void xmlFreeDoc(IntPtr doc);
		[DllImport("libxslt.so.1")] static extern void xsltSetXIncludeDefault(int flag);
		[DllImport("libxslt.so.1")] static extern void xsltCleanupGlobals();
		[DllImport("libxslt.so.1")] static extern IntPtr xsltParseStylesheetFile([MarshalAs(UnmanagedType.LPStr)] string filename);
		[DllImport("libxslt.so.1")] static extern void xsltFreeStylesheet(IntPtr xsl);
		[DllImport("libxslt.so.1")] static extern IntPtr xsltApplyStylesheet(IntPtr xsl, IntPtr doc, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1, ArraySubType=UnmanagedType.LPStr)] string[] parameters);
		[DllImport("libxslt.so.1")] static extern int xsltSaveResultToFilename(string outfile, IntPtr res, IntPtr xsl, int compress);
		[DllImport("libexslt.so.0")] static extern void exsltRegisterAll();
		private static void ApplyLinuxXslt(string stylesheet, string inputFile, string outputFile, List<XsltParam> xparams)
		{
			IntPtr xsl = xsltParseStylesheetFile(stylesheet);
			if (xsl == IntPtr.Zero)
				throw new Exception(String.Format("ApplyLinuxXslt: Cannot parse XSLT file \"{0}\"", stylesheet));
			try
			{
				xmlInitParser();
				xmlSubstituteEntitiesDefault(1);
				xsltSetXIncludeDefault(1);
				exsltRegisterAll();
				IntPtr doc = xmlParseFile(inputFile);
				if (doc == IntPtr.Zero)
					throw new Exception(String.Format("ApplyLinuxXslt: Cannot parse XML file \"{0}\"", inputFile));
				string[] parameters = new string[2 * xparams.Count + 1];
				int i = 0;
				foreach (var xparam in xparams)
				{
					parameters[i++] = xparam.Name;
					parameters[i++] = String.Format("'{0}'", xparam.Value);
				}
				parameters[i] = null;
				IntPtr res = xsltApplyStylesheet(xsl, doc, parameters);
				xmlFreeDoc(doc);
				if (res == IntPtr.Zero)
					throw new Exception(String.Format("ApplyLinuxXslt: Applying stylesheet to \"{0}\" failed.", inputFile));
				int ok = xsltSaveResultToFilename(outputFile, res, xsl, 0);
				xmlFreeDoc(res);
				if (ok < 0)
					throw new Exception(String.Format("ApplyLinuxXslt: Cannot save result file \"{0}\"", outputFile));
			}
			finally
			{
				xsltFreeStylesheet(xsl);
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
