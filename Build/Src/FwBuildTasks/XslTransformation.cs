// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Xsl;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// This class implements a missing task from Mono/xbuild.  Once it is finished properly,
	/// tested, and reformatted, etc., it should be submitted to the Mono team.
	/// </summary>
	/// <remarks>
	/// In our experience, the Mono XSLT implementation was so slow, we replaced it by
	/// dynamically linking to libxslt.so on Linux in our C# code.
	/// </remarks>
	public class XslTransformation : Task
	{
		List<string> m_tempFiles = new List<string>();
		List<string> m_inputFiles = new List<string>();
		List<string> m_outputFiles = new List<string>();
		string m_xslFile;
		XsltArgumentList m_xargs = new XsltArgumentList();

		/// <summary>
		/// Get or set the paths to the output files.
		/// </summary>
		[Required]
		public ITaskItem[] OutputPaths { get; set; }

		/// <summary>
		/// Get or set the parameters to the XSLT.
		/// </summary>
		public string Parameters { get; set; }

		/// <summary>
		/// Get or set the XML input as a string.
		/// </summary>
		public string XmlContent { get; set; }

		/// <summary>
		/// Get or set the paths to the XML input files.
		/// </summary>
		public ITaskItem[] XmlInputPaths { get; set; }

		/// <summary>
		/// Get or set the path to the compiled XSLT.
		/// </summary>
		public ITaskItem XslCompiledDllPath { get; set; }

		/// <summary>
		/// Get or set the XSLT as a string.
		/// </summary>
		public string XslContent { get; set; }

		/// <summary>
		/// Get or set the path to the XSLT file.
		/// </summary>
		public ITaskItem XslInputPath { get; set; }

		public XslTransformation()
		{
		}

		public override bool Execute()
		{
			try
			{
				if (!ValidateAttributes())
					return false;
				if (!PrepareFilesFromAttributes())
					return false;
				PreprocessParameters();
				ApplyXslt();
				return true;
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
				return false;
			}
			finally
			{
				foreach (var path in m_tempFiles)
					File.Delete(path);
			}
		}

		/// <summary>
		/// Check that the provided attributes make sense.
		/// </summary>
		/// <returns>
		/// True if successful, false if an error occurs.
		/// </returns>
		protected bool ValidateAttributes()
		{
			// We want only one source of XML input (this may be too stringent).
			int cInput = 0;
			if (!string.IsNullOrEmpty(XmlContent))
				++cInput;
			if (XmlInputPaths != null && XmlInputPaths.Length > 0)
				++cInput;
			if (cInput != 1)
			{
				Log.LogError("XslTransformation: either XmlContent or XmlInputPaths must be specified, but not both.");
				return false;
			}
			// We want only one source of XSLT.
			int cXsl = 0;
			if (!string.IsNullOrEmpty(XslContent))
				++cXsl;
			if (XslInputPath != null)
				++cXsl;
			if (XslCompiledDllPath != null)
				++cXsl;
			if (cXsl != 1)
			{
				Log.LogError("XslTransformation: one of XslContent, XslInputPath, and XslCompiledDllPath must be specified, but not more than one.");
				return false;
			}
			// We (I) don't know how to use a compiled XSL DLL.
			if (XslCompiledDllPath != null)
			{
				Log.LogError("XslTransformation: XslCompiledDllPath is not (yet?) supported.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Process the attributes to provide input and output file lists, and an XSLT file.
		/// </summary>
		/// <returns>
		/// True if successful, false if an error occurs.
		/// </returns>
		protected bool PrepareFilesFromAttributes()
		{
			if (!string.IsNullOrEmpty(XmlContent))
			{
				var xmlpath = Path.GetTempFileName();
				using (var writer = new StreamWriter(xmlpath))
				{
					writer.Write(XmlContent);
					writer.Flush();
					writer.Close();
				}
				m_tempFiles.Add(xmlpath);
				m_inputFiles.Add(xmlpath);
			}
			else
			{
				foreach (var item in XmlInputPaths)
					m_inputFiles.Add(item.ItemSpec);
			}
			foreach (var item in OutputPaths)
				m_outputFiles.Add(item.ItemSpec);
			if (!string.IsNullOrEmpty(XslContent))
			{
				var xslpath = Path.GetTempFileName();
				using (var writer = new StreamWriter(xslpath))
				{
					writer.Write(XslContent);
					writer.Flush();
					writer.Close();
				}
				m_tempFiles.Add(xslpath);
				m_xslFile = xslpath;
			}
			else
			{
				m_xslFile = XslInputPath.ItemSpec;
			}
			if (m_outputFiles.Count > 1 && m_outputFiles.Count != m_inputFiles.Count)
			{
				Log.LogError("XslTransformation: The number of output files does not equal the number of input files.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Convert the Parameters attribute into proper inputs to the XSLT processor.
		/// </summary>
		void PreprocessParameters()
		{
			if (string.IsNullOrEmpty(Parameters))
				return;
			// Ensure that multiple parameters are enclosed by an outer element.
			var paramXml = string.Format("<Params>{0}</Params>", Parameters);
			XDocument xdoc = XDocument.Parse(paramXml);
			foreach (var xe in xdoc.Descendants("Parameter"))
				m_xargs.AddParam(xe.Attribute("Name").Value, string.Empty, xe.Attribute("Value").Value);
		}

		/// <summary>
		/// Applies the XSLT to the input file(s) to produce the output file(s).
		/// </summary>
		protected void ApplyXslt()
		{
			var settings = new XsltSettings();
			settings.EnableDocumentFunction = true;
			settings.EnableScript = true;
			var xsl = new XslCompiledTransform();
			xsl.Load(m_xslFile, settings, null);
			if (m_outputFiles.Count == 1)
			{
				using (var writer = new StreamWriter(m_outputFiles[0]))
				{
					for (int i = 0; i < m_inputFiles.Count; ++i)
					{
						xsl.Transform(m_inputFiles[i], m_xargs, writer);
						writer.Flush();
					}
					writer.Close();
				}
			}
			else
			{
				System.Diagnostics.Debug.Assert(m_inputFiles.Count == m_outputFiles.Count);
				for (int i = 0; i < m_inputFiles.Count; ++i)
				{
					using (var writer = new StreamWriter(m_outputFiles[i]))
					{
						xsl.Transform(m_inputFiles[i], m_xargs, writer);
						writer.Flush();
						writer.Close();
					}
				}
			}
		}
	}
}
