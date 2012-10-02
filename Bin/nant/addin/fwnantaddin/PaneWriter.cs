using System;
using System.IO;
using System.Text;
using EnvDTE;
using EnvDTE80;

namespace FwNantAddin2
{
	/// <summary>
	/// Manages output to Output pane
	/// </summary>
	public class PaneWriter : TextWriter
	{
		[ThreadStatic]
		private static OutputWindowPane m_pane;
		protected readonly string m_name;
		protected readonly DTE2 m_dte;
		private bool m_fEmptyLine = false;

		public PaneWriter(DTE2 dte, string name)
		{
			m_dte = dte;
			m_name = name;
		}

		protected override void Dispose(bool disposing)
		{
			m_pane = null;
			base.Dispose(disposing);
		}

		public void Clear()
		{
			Pane.Clear();
			Pane.Activate();
		}

		public void OutputTaskItemString(string text, vsTaskPriority priority, string category,
			vsTaskIcon icon, string fileName, int line, string description, bool force)
		{
			Pane.OutputTaskItemString(text, priority, category, icon, fileName, line, description, force);
		}

		// A TextWriter derived class must minimally implement the TextWriter(Char)
		// method in order to make a useful instance of TextWriter.
		public override void Write(char c)
		{
			Pane.OutputString(c.ToString());
		}

		override public void Write(string s)
		{
			if (s.Trim().Length <= 0 && s != NewLine)
				m_fEmptyLine = true;
			else
			{
				m_fEmptyLine = false;
				Pane.OutputString(s);
			}
		}

		override public void WriteLine()
		{
			if (!m_fEmptyLine)
				Write(NewLine);
		}

		override public void WriteLine(string s)
		{
			Write(s + NewLine);
		}

		override public Encoding Encoding
		{
			get { return Encoding.Default; }
		}

		public void Activate()
		{
			Pane.Activate();
		}

		/// <summary>
		/// NOTE: Ensure that each thread gets its own copy of the RCW/COM object.
		/// This is to avoid problems with the COM object becoming divorced
		/// from its RCW when the creating thread gets GCed.
		/// </summary>
		protected virtual OutputWindowPane Pane
		{
			get
			{
				if(m_pane == null)
				{
					OutputWindow outputWindow = m_dte.ToolWindows.OutputWindow;
					try
					{
						m_pane = outputWindow.OutputWindowPanes.Item(m_name);
					}
					catch(ArgumentException)
					{
						m_pane = outputWindow.OutputWindowPanes.Add(m_name);
					}
				}
				return m_pane;
			}
		}
	}

	public class DebugPaneWriter: PaneWriter
	{
		public DebugPaneWriter(DTE2 dte, string name): base(dte, name)
		{
		}

		protected override void Dispose(bool disposing)
		{
			m_DebugPane = null;
			base.Dispose(disposing);
		}

		protected override OutputWindowPane Pane
		{
			get
			{
				if(m_DebugPane == null)
				{
					OutputWindow outputWindow = m_dte.ToolWindows.OutputWindow;
					try
					{
						m_DebugPane = outputWindow.OutputWindowPanes.Item(m_name);
					}
					catch(Exception)
					{
						m_DebugPane = outputWindow.OutputWindowPanes.Add(m_name);
					}
				}
				return m_DebugPane;
			}
		}
		[ThreadStatic]
		private static OutputWindowPane m_DebugPane;
	}
}
