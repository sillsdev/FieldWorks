using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Base class for transforming an M3 model to files needed by a parser
	/// </summary>
	abstract internal class M3ToParserTransformerBase
	{
		protected string m_outputDirectory;
		protected string m_database;
		protected Action<TaskReport> m_taskUpdateHandler;
		protected readonly TraceSwitch m_tracingSwitch = new TraceSwitch("ParserCore.TracingSwitch", "Just regular tracking", "Off");
		protected readonly string m_appInstallDir;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToParserTransformerBase"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToParserTransformerBase(string database, Action<TaskReport> taskUpdateHandler, string appInstallDir)
		{
			m_database = database;
			m_taskUpdateHandler = taskUpdateHandler;
			m_appInstallDir = appInstallDir;
			m_outputDirectory = Path.GetTempPath();
		}

		protected void TransformDomToFile(string transformName, XmlDocument inputDOM, string outputName, TaskReport task)
		{
			using (task.AddSubTask(String.Format(ParserCoreStrings.ksCreatingX, outputName)))
			{
				XmlUtils.TransformDomToFile(Path.Combine(m_appInstallDir + "/Language Explorer/Transforms/", transformName),
				inputDOM, Path.Combine(m_outputDirectory, outputName));
			}
		}
	}
}
