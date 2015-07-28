// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// <summary>
	/// This interface provides some of the functionality of TextWriter with out the IDisposable baggage.
	/// The implementation wraps a TextWriter so more of that interface can readily be added as needed.
	/// It also manages an indentation.
	/// </summary>
	public interface ISimpleLogger
	{
		/// <summary>
		/// For logging nested structures, increments the current indent level.
		/// </summary>
		void IncreaseIndent();

		/// <summary>
		/// For logging nested structures, decrements the current indent level.
		/// </summary>
		void DecreaseIndent();

		/// <summary>
		/// Write a line of text to the log (preceded by the current indent).
		/// </summary>
		/// <param name="value"></param>
		void WriteLine(string value);
	}
	/// <summary>
	/// An ISimpleLogger can be temporarily passed to a class which is not Dispsable in place of a TextWriter.
	/// This makes it unambiguous that the class using the logger is not responsible to dispose of it.
	/// The actual class is disposable and should normally be created in a Using clause.
	/// The logger can also track an indent.
	/// </summary>
	public class SimpleLogger : ISimpleLogger, IDisposable
	{
		/// <summary>
		/// Make one (on a memory stream the logger is responsible for).
		/// </summary>
		public SimpleLogger()
		{
			m_stream = new MemoryStream();
			m_writer = new StreamWriter(m_stream);
		}

		private MemoryStream m_stream;
		private int m_indent;
		private TextWriter m_writer;
		/// <summary>
		/// For logging nested structures, increments the current indent level.
		/// </summary>
		public void IncreaseIndent()
		{
			m_indent++;
		}

		/// <summary>
		/// For logging nested structures, decrements the current indent level.
		/// </summary>
		public void DecreaseIndent()
		{
			m_indent--;
		}

		/// <summary>
		/// Write a line of text to the log (preceded by the current indent).
		/// </summary>
		public void WriteLine(string text)
		{
			for (int i = 0; i < m_indent; i++)
				m_writer.Write("    ");
			m_writer.WriteLine(text);
		}

		/// <summary>
		/// Get the text that has been written to the stream.
		/// </summary>
		public string Content
		{
			get
			{
				m_writer.Flush();
				using (StreamReader sr = new StreamReader(m_stream))
				{
					m_stream.Seek(0, SeekOrigin.Begin);
					return sr.ReadToEnd();
				}
			}
		}

		#if DEBUG
		/// <summary/>
		~SimpleLogger()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// As a special case, this class does not HAVE to be disposed if it does not allow pictures.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing,
							  "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				m_writer.Dispose();
				m_stream.Dispose();
			}
		}
	}
}
