// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LibXslt.cs
// Responsibility: Steve McConnel
// Last reviewed:
//
// <remarks>
// This makes available some functions from libxslt.so, which has some capabilities lacking in
// the Mono Xml/Xsl implementation.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace SIL.Utils
{
	/// <summary>
	/// Wrap libxslt.so functions for use in C# code (should we try to wrap MSXSL2 functionality similarly?)
	/// </summary>
	public static class LibXslt
	{
#if __MonoCS__
		[DllImport("libxml2.so.2")]
		static extern void xmlInitParser();
		[DllImport("libxml2.so.2")]
		static extern void xmlSubstituteEntitiesDefault(
			int flag);
		[DllImport("libxml2.so.2")]
		static extern void xmlCleanupParser();
		[DllImport("libxml2.so.2")]
		static extern IntPtr xmlParseFile(
			[MarshalAs(UnmanagedType.LPStr)] string filename);
		[DllImport("libxml2.so.2")]
		static extern void xmlFreeDoc(
			IntPtr doc);

		[DllImport("libxslt.so.1")]
		static extern void xsltSetXIncludeDefault(
			int flag);
		[DllImport("libxslt.so.1")]
		static extern void xsltCleanupGlobals();
		[DllImport("libxslt.so.1")]
		static extern IntPtr xsltParseStylesheetFile(
			[MarshalAs(UnmanagedType.LPStr)] string filename);
		[DllImport("libxslt.so.1")]
		static extern void xsltFreeStylesheet(
			IntPtr xsl);
		[DllImport("libxslt.so.1")]
		static extern IntPtr xsltApplyStylesheet(
			IntPtr xsl,
			IntPtr doc,
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1, ArraySubType=UnmanagedType.LPStr)] string[] parameters);
		[DllImport("libxslt.so.1")]
		static extern int xsltSaveResultToFilename(
			string outfile,
			IntPtr res,
			IntPtr xsl,
			int compress);

		[DllImport("libexslt.so.0")]
		static extern void exsltRegisterAll();

		/// <summary>
		/// This needs to be called at least once, but I don't think it hurts to call it
		/// more than once.  It is called by TransformFileToFile (the most basic one that
		/// all the others eventually call).
		/// </summary>
		public static void InitializeLibXslt()
		{
			xmlInitParser();
			xmlSubstituteEntitiesDefault(1);
			//xmlLoadExtDtdDefaultValue = 1;	// Can't handle in C# -- hope it's not important!
			xsltSetXIncludeDefault(1);
			exsltRegisterAll();
		}
		/// <summary>
		/// Call this once when the program is totally done using this wrapper.
		/// (Not too serious if it doesn't get called -- minor memory leak.)
		/// I don't think InitializeLibXslt() can overcome some of the effects of
		/// CloseLibXslt().
		/// </summary>
		public static void CloseLibXslt()
		{
			xsltCleanupGlobals();
			xmlCleanupParser();
		}

		/// <summary>
		/// Compile an XSLT transform.
		/// </summary>
		/// <param name='sTransformFile'>full path of the XSLT transform</param>
		/// <returns>handle to the compiled transform</returns>
		public static IntPtr CompileTransform(string sTransformFile)
		{
			IntPtr xsl = xsltParseStylesheetFile(sTransformFile);
			//Console.WriteLine("CompileTransform('{0}') => {1}", sTransformFile, xsl.ToString());
			return xsl;
		}

		/// <summary>
		/// Free a compiled XSLT transform.
		/// </summary>
		/// <param name='xsl'>handle to the compiled transform</param>
		public static void FreeCompiledTransform(IntPtr xsl)
		{
			xsltFreeStylesheet(xsl);
		}
#else
		// The following are defined to allow linking in Microsoft .Net.  They obviously
		// don't do anything useful!

		/// <summary>
		/// This needs to be called at least once, but I don't think it hurts to call it
		/// more than once.  It is called by TransformFileToFile (the most basic one that
		/// all the others eventually call).
		/// </summary>
		///<remarks>link placeholder-- should not be called!</remarks>
		public static void InitializeLibXslt()
		{
		}

		/// <summary>
		/// Call this once when the program is totally done using this wrapper.
		/// (Not too serious if it doesn't get called -- minor memory leak.)
		/// I don't think InitializeLibXslt() can overcome some of the effects of
		/// CloseLibXslt().
		/// </summary>
		///<remarks>link placeholder-- should not be called!</remarks>
		public static void CloseLibXslt()
		{
		}

		/// <summary>
		/// Compile an XSLT transform.
		/// </summary>
		/// <param name='sTransformFile'>full path of the XSLT transform</param>
		/// <returns>handle to the compiled transform</returns>
		///<remarks>link placeholder-- should not be called!</remarks>
		public static IntPtr CompileTransform(string sTransformFile)
		{
			return IntPtr.Zero;
		}

		/// <summary>
		/// Free a compiled XSLT transform.
		/// </summary>
		/// <param name='xsl'>handle to the compiled transform</param>
		///<remarks>link placeholder-- should not be called!</remarks>
		public static void FreeCompiledTransform(IntPtr xsl)
		{
		}
#endif

		/// <summary>
		/// Apply an XSLT transform on a DOM to produce a resulting file
		/// </summary>
		/// <param name="sTransformFile">full path name of the XSLT transform</param>
		/// <param name="inputDOM">XmlDocument DOM containing input to be transformed</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		public static void TransformDomToFile(string sTransformFile, XmlDocument inputDOM, string sOutputFile)
		{
			string sTempInput = FileUtils.GetTempFile("xml");
			try
			{
				inputDOM.Save(sTempInput);
				TransformFileToFile(sTransformFile, sTempInput, sOutputFile);
			}
			finally
			{
				if (File.Exists(sTempInput))
					File.Delete(sTempInput);
			}
		}

		/// <summary>
		/// Apply an XSLT transform on a DOM to produce a resulting file
		/// </summary>
		/// <param name="xsl">handle to a compiled XSLT transform</param>
		/// <param name="inputDOM">XmlDocument DOM containing input to be transformed</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		public static void TransformDomToFile(IntPtr xsl, XmlDocument inputDOM, string sOutputFile)
		{
			string sTempInput = FileUtils.GetTempFile("xml");
			try
			{
				inputDOM.Save(sTempInput);
				TransformFileToFile(xsl, new string[1] { null }, sTempInput, sOutputFile);
			}
			finally
			{
				if (File.Exists(sTempInput))
					File.Delete(sTempInput);
			}
		}

		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="sTransformFile">full path name of the XSLT transform</param>
		/// <param name="sInputFile">full path of the input file</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		public static void TransformFileToFile(string sTransformFile, string sInputFile, string sOutputFile)
		{
			TransformFileToFile(sTransformFile, new string[1] { null }, sInputFile, sOutputFile);
		}

		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="xsl">handle to a compiled XSLT transform</param>
		/// <param name="sInputFile">full path of the input file</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		public static void TransformFileToFile(IntPtr xsl, string sInputFile, string sOutputFile)
		{
			TransformFileToFile(xsl, new string[1] { null }, sInputFile, sOutputFile);
		}

		/// <summary>
		/// Convert the parameter list from an array of XmlUtils.XSLParameter objects to
		/// an array of strings suitable for feeding to libxslt functions.
		/// </summary>
		static string[] ConvertParameterList (XmlUtils.XSLParameter[] parameterList)
		{
			int paramCount = parameterList == null ? 0 : parameterList.Length;
			string[] parameters = new string[2 * paramCount + 1];
			int j = 0;
			for (int i = 0; i < paramCount; ++i)
			{
				parameters[j++] = parameterList[i].Name;
				// libxml2 requires "string" parameters to be quoted -- what other kind of parameter is there, anyway?
				parameters[j++] = String.Format("'{0}'", parameterList[i].Value.Replace("'", "&apos;"));
			}
			parameters[j] = null;
			return parameters;
		}


		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="sTransformFile">full path name of the XSLT transform</param>
		/// <param name="parameterList">list of parameters to pass to the transform</param>
		/// <param name="sInputFile">full path of the input file</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		public static void TransformFileToFile(string sTransformFile, XmlUtils.XSLParameter[] parameterList, string sInputFile, string sOutputFile)
		{
			string[] parameters = ConvertParameterList(parameterList);
			TransformFileToFile(sTransformFile, parameters, sInputFile, sOutputFile);
		}

#if __MonoCS__
		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="sTransformFile">full path name of the XSLT transform</param>
		/// <param name="parameters">list of parameters to pass to the transform</param>
		/// <param name="sInputFile">full path of the input file</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		public static void TransformFileToFile(string sTransformFile, string[] parameters, string sInputFile, string sOutputFile)
		{
			IntPtr xsl = xsltParseStylesheetFile(sTransformFile);
			if (xsl == IntPtr.Zero)
			{
				throw new Exception(String.Format("Unable to parse XSLT file '{0}' with libxslt.so", sTransformFile));
			}
			try
			{
				TransformFileToFile(xsl, parameters, sInputFile, sOutputFile);
			}
			finally
			{
				xsltFreeStylesheet(xsl);
			}
		}

		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="xsl">handle to a compiled XSLT transform</param>
		/// <param name="parameters">list of parameters to pass to the transform</param>
		/// <param name="sInputFile">full path of the input file</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		public static void TransformFileToFile(IntPtr xsl, string[] parameters, string sInputFile, string sOutputFile)
		{
			InitializeLibXslt();
			IntPtr doc = xmlParseFile(sInputFile);
			if (doc == IntPtr.Zero)
			{
				throw new Exception(String.Format("LibXslt.TransformFileToFile: Cannot parse XML file \"{0}\"\n", sInputFile));
			}
			IntPtr res = xsltApplyStylesheet(xsl, doc, parameters);
			xmlFreeDoc(doc);
			if (res == IntPtr.Zero)
			{
				throw new Exception(String.Format("LibXslt.TransformFileToFile: Applying stylesheet to \"{0}\" failed.\n", sInputFile));
			}
			int ok = xsltSaveResultToFilename(sOutputFile, res, xsl, 0);
			xmlFreeDoc(res);
			if (ok < 0)
			{
				throw new Exception(String.Format("LibXslt.TransformFileToFile: Cannot save result file \"{0}\"\n", sOutputFile));
			}
		}
#else
		// The following are defined to allow linking in Microsoft .Net.  They obviously
		// don't do anything useful!

		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="sTransformFile">full path name of the XSLT transform</param>
		/// <param name="parameters">list of parameters to pass to the transform</param>
		/// <param name="sInputFile">full path of the input file</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		///<remarks>link placeholder-- should not be called!</remarks>
		public static void TransformFileToFile(string sTransformFile, string[] parameters, string sInputFile, string sOutputFile)
		{
		}

		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="xsl">handle to a compiled XSLT transform</param>
		/// <param name="parameters">list of parameters to pass to the transform</param>
		/// <param name="sInputFile">full path of the input file</param>
		/// <param name="sOutputFile">full path of the resulting output file</param>
		///<remarks>link placeholder-- should not be called!</remarks>
		public static void TransformFileToFile(IntPtr xsl, string[] parameters, string sInputFile, string sOutputFile)
		{
		}
#endif
	}
}
