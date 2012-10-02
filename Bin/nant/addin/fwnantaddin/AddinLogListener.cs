using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using EnvDTE80;
using NAnt.Core;

namespace FwNantAddin2
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Log messages
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class AddinLogListener : DefaultLogger
	{
		private PaneWriter m_buildOutputWindow;
		private bool m_fUseDebugPane;
		private NAntBuild m_Parent;

		/// <summary>
		/// Use this PaneWriter for all output messages.
		/// </summary>
		public PaneWriter OutputBuild
		{
			get
			{
				if (m_buildOutputWindow == null && m_Parent != null)
				{
#if DEBUG
					if (m_fUseDebugPane)
						m_buildOutputWindow = new DebugPaneWriter(m_Parent.DTE, m_Parent.DebugPaneName);
					else
#endif
						m_buildOutputWindow = new PaneWriter(m_Parent.DTE, m_Parent.BuildPaneName);
				}
				return m_buildOutputWindow;
			}
		}

		/// <summary>
		/// Creates a new Addin Log Listener.
		/// </summary>
		/// <param name="BuildOutputWindow">The Output Pane to write messages to</param>
		internal AddinLogListener(NAntBuild parent, bool fUseDebugPane)
		{
			m_Parent = parent;
			m_fUseDebugPane = fUseDebugPane;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes a string to the output. If it's an error or warning message, a task is added
		/// to the task list
		/// </summary>
		/// <param name="message">The message to output</param>
		/// <param name="fNewLine"><c>true</c> to write a new line after the message</param>
		/// ------------------------------------------------------------------------------------
		private void OutputString(string message, bool fNewLine)
		{
			lock(this)
			{
				if (OutputBuild == null)
					return;

				bool fError = false;
				if (message.IndexOf("error") >= 0 || message.IndexOf("warning") >= 0)
				{
					// C:\Documents and Settings\Eberhard\TrashBin\Simple\Simple.cs(4,4): error CS1002: ; expected
					Regex regex = new Regex("\\s*(?<filename>[^(]+)\\((?<line>\\d+),(?<column>\\d+)\\):[^:]+: (?<description>.*)");
					Match match = regex.Match(message);
					if (match.Value.Length > 0)
					{
						fError = true;
						string filename = match.Groups["filename"].Value;
						string line = match.Groups["line"].Value;
						string descr = match.Groups["description"].Value;
						OutputBuild.OutputTaskItemString(message,
							EnvDTE.vsTaskPriority.vsTaskPriorityHigh,
							EnvDTE.vsTaskCategories.vsTaskCategoryBuildCompile,
							EnvDTE.vsTaskIcon.vsTaskIconCompile, filename, Convert.ToInt32(line)-1,
							descr, true);
					}
				}

				if (!fError)
					OutputBuild.Write(message);

				if (fNewLine)
					OutputBuild.WriteLine();
			}
		}

		/// <summary>
		/// Occurs when a Message is written to the Log.
		/// </summary>
		/// <param name="message">The Message that was written</param>
		public void Write(string message)
		{
			OutputString(message, false);
		}

		/// <summary>
		/// Occurs when a Line is written to the Log.
		/// </summary>
		/// <param name="line">The Line that was written.</param>
		public void WriteLine(string line)
		{
			OutputString(line, true);
		}

		/// <summary>
		/// Gets or sets the <see cref="TextWriter" /> to which the logger is
		/// to send its output.
		/// </summary>
		/// <value>
		/// The <see cref="TextWriter" /> to which the logger sends its output.
		/// </value>
		public override TextWriter OutputWriter
		{
			get { return OutputBuild; }
			set { base.OutputWriter = value; }
		}
	}
}
