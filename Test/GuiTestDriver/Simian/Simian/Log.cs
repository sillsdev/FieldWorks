using System;
using System.Collections;
using System.IO;

namespace Simian
{
/**
 * Writes log data to an XML file with a time stamp in milliseconds.
 * The log can be used in one of two ways.
 * 1) collect all the data then write to a file when done logging.
 *    A lot of memmory is used if there is a lot of data.
 *    If the program crashes, all logged data is lost.
 * 2) specify a file up front and flush the data at the end of each logged element.
 *    This ensures that most data ends up in the file in case of a crash.
 *    When a lot of data is logged, it can noticeably decrease performance.
 *    Not thread safe - if two threads write to it, boom!
 *    Create a different Log global to each thread
 *    - don't use Log.getOnly() unless you want the last one created;
 * This class cannot garantee that the XML ouput to the file is well formed or valid.
 * If there is a crash, it won't be well formed.
 *
 * This is a singleton.
 *
 * Useage: Method 1
 * Log log = Log.getOnly(file); // first access creates log - recording is on, sysout is off. Otherwise gets current log.
 * log.doEnable(true|false); // true enables logging (default). false disables it.
 * log.doSysOut(true|false); // true - logged data also goes to a terminal window. false no terminal output.
 * created = log.writeElt(name); // starts a new element with name.
 * dTime = log.writeEltTime(name); // starts a new element with a deltaT.log="xxx" attribute in millisec from log init.
 * created = log.writeAttr(name, value); // writes a string attribute.
 * created = log.writeAttr(name, decimal); // writes a decimal with precission, never X.XXXXEE+/-NN.
 * log.write("text"); // write text as content to the log.
 * log.writeln("text"); // ends a line of log text.
 * log.endElt(); // call to close each element you create.
 * int status = log.writeToFile("fileName", "xsltName")
 *
 * Useage: Method 2
 * Log log = new Log(outputFileName,xsltFileName); // if outputFileName is null, log reverts to method 1.
 * Log log = Log.getOnly(null); // gets current log.
 * log.doEnable(true|false); // true enables logging (default). false disables it.
 * log.doSysOut(true|false); // true - logged data also goes to a terminal window. false no terminal output.
 * created = log.writeElt(name); // starts a new element with name.
 * dTime = log.writeEltTime(name); // starts a new element with a deltaT.log="xxx" attribute in millisec from log init.
 * created = log.writeAttr(name, value); // writes a string attribute.
 * created = log.writeAttr(name, decimal); // writes a decimal with precission, never X.XXXXEE+/-NN.
 * log.write("text"); // write text as content to the log.
 * log.writeln("text"); // ends a line of log text.
 * log.endElt(); // call an extra time to close the document element and close the file.
 *
 * @author Michael Lastufka
 * @version 10-23-2008
 */
	class Log
	{
		private ArrayList m_remember;
		private ArrayList m_eltStack;

		private static Log m_log = null;
		private readonly int m_start;

		private const string noExp = "{0:D2}";
		private bool    m_doLog;
		private bool    m_doSysOut;
		private string  m_buffer = "";

		private string  m_fileName;
		private string  m_xsltName;
		private bool    m_doFlush;
		private StreamWriter m_pw;

		/// <summary>
		/// Gets the only Log if there is one (use null parameter value).
		/// If there isn't one, it sets one up.
		/// A default Log is used when a null output file name is given.
		/// </summary>
		/// <param name="LogFile">The name of the Log file to write or null</param>
		public static Log getOnly()
		{
			if (m_log == null) m_log = new Log();
			return m_log;
		}

		/// <summary>
		/// Constructor for objects of class Log
		/// </summary>
		/// <param name="logFile">The name of the Log file to write or null</param>
		private Log()
		{   // set initial capacity
			m_doLog = true; // the log is on by default
			m_doSysOut = false; // don't copy data to System.out
			m_start = System.Environment.TickCount;
			m_doFlush = false;
			m_eltStack = new ArrayList(10);
			m_remember = new ArrayList(1000);
			writeElt("log");
		}

		/**
		 * Constructor for objects of class Log
		 * @param fileName name of the file to write to.
		 * @param xsltName Name of the XSLT script file to format this or null.
		 */
		public Log( string fileName, string xsltName) : this()
		{
			m_fileName = fileName; // can be null or empty, but won't flush if it is
			m_xsltName = xsltName; // can be null
			if (m_fileName != null && !m_fileName.Equals(""))
			{
				m_doFlush  = true;
				int errorFlags = prepFile(m_fileName, m_xsltName); // sets m_pw
				if (errorFlags == 0 ) m_pw.Flush();
			}
			else m_remember = new ArrayList(1000); // no flushing until writeToFile()
			m_log = this;
		}

		/**
		 * Enable or disable the log recording.
		 * @param on set to true to enable, false to disable
		 */
		public void doEnable(bool on) {m_doLog = on;}

		/**
		 * Enable or disable sending log data to System.out - use for debugging.
		 * @param on set to true to enable, false to disable
		 */
		public void doSysOut(bool on) {m_doSysOut = on;}

		/**
		 * Starts writing a new element with name to the log and
		 * encloses all writeAttr(), write() and writeln() elements
		 * until the endElt() is called and </name> is written.
		 * @param name non-null, non-empty designation of this element.
		 * @return true if the element was written; false if no element was started.
		 */
		public bool writeElt(string name)
		{
			if (!m_doLog || name == null || name.Equals("")) return false;
			setHasContent();
			ElementInfo elt = new ElementInfo(name);
			m_eltStack.Add(elt);
			if (m_buffer.Length > 0)
			{
				m_remember.Add(m_buffer);
				if (m_doSysOut) Console.Out.WriteLine(m_buffer);
			}
			m_buffer = "<" + name;
			if (m_doSysOut == true) Console.Out.WriteLine(m_buffer);
			return true;
		}

		/**
		 * Sets the last element hasContent to true.
		 * @return true if the start tag was closed.
		 */
		private bool setHasContent()
		{
			bool closedTag = false;
			int size = m_eltStack.Count;
			if (size == 0) return false;
			ElementInfo elt = (ElementInfo)m_eltStack[size-1];
			if (!elt.hasContent)
			{
				m_buffer = m_buffer + ">";
				closedTag = true;
			}
			elt.hasContent = true;
			return closedTag;
		}

		/**
		 * Checks if the last element hasContent.
		 * @returns true if the last element has content.
		 */
		private bool checkHasContent()
		{
			int size = m_eltStack.Count;
			if (size == 0) return false;
			ElementInfo elt = (ElementInfo)m_eltStack[size - 1];
			return elt.hasContent;
		}

		/**
		 * Starts writing a new element with name to the log and
		 * encloses all writeAttr(), write() and writeln() elements
		 * until the endElt() is called.
		 * Adds a time deltaT.log="xxx" attribute in millisec from log init.
		 * @param name non-null, non-empty designation of this element.
		 * @return the time since the log started in milliseconds or -1 if no element was started.
		 */
		public long writeEltTime(string name)
		{
			int dTime = -1;
			if (!writeElt(name)) return -1;
			int timeValue = System.Environment.TickCount;
			dTime = timeValue - m_start;
			writeAttr("deltaT.log", dTime);
			return dTime;
		}

		/**
		 * Writes a string attribute.
		 * @param name non-null, non-empty desigation of this attribute.
		 * @param value value - can be null or empty.
		 * @return true if created, false otherwise.
		 */
		public bool writeAttr(string name, string value)
		{
			if (!m_doLog || name == null || name.Equals("") || checkHasContent()) return false;
			string image = " " + name + "=\"" + value + "\"";
			m_buffer = m_buffer + image;
			if (m_doSysOut == true) Console.Out.WriteLine(image);
			return true;
		}

		/**
		 * Writes a decimal with precission, never X.XXXXEE+/-NN.
		 * @param name non-null, non-empty desigation of this attribute.
		 * @param value A decimal value to be free formatted.
		 * @return true if created, false otherwise.
		 */
		public bool writeAttr(string name, double value)
		{
		//	return writeAttr(name, String.Format(noExp, value));
			return writeAttr(name, String.Format("{0:G}", value));
		}

		/**
		 * Ends an element with an end tag if there is content. </name>
		 * Ends it as an empty element if no content. ... />
		 * The file is flushed.
		 */
		public void endElt ()
		{
			if (!m_doLog) return;
			int size = m_eltStack.Count;
			if (size == 0) return; // <log> should be in the stack
			string image = null;
			ElementInfo elt = (ElementInfo)m_eltStack[size - 1];
			m_eltStack.Remove(elt);
			if (elt.hasContent) image = "</"+elt.name+">";
			else                image = " />";
			m_buffer = m_buffer + image;
			m_remember.Add(m_buffer);
			m_buffer = "";
			if (m_doSysOut == true) Console.Out.WriteLine(image);
			if (!m_doFlush) return;
			writeList();
			m_pw.Flush();
			if (size - 1 <= 0)
			{
				m_pw.Close(); // the first ElementInfo is <log>
				m_doFlush = false;
			}
		}

		/**
		 * Adds text to a record in the log.
		 * @param text the text to record.
		 */
		public void write(string text)
		{
			if (!m_doLog) return;
			// if buffer is null, 'buffer +=' asserts
			bool closedElt = setHasContent();
			m_buffer = m_buffer + text;
			if (m_doSysOut == true)
			{
				if (closedElt) Console.Out.Write(">");
				Console.Out.WriteLine(text);
			}
		}

		/**
		 * Ends a text record in the log.
		 * @param text the text to record.
		 */
		public void writeln(string text)
		{
			if (!m_doLog) return;
			write(text);
			m_remember.Add(m_buffer);
			m_buffer = ""; //m_buffer = new String();
		}

		/**
		 * Prepare to write to an XML file. If the file name is null or
		 * empty, an error is returned. If it's ok, a file is opened and
		 * its header and document element is written.
		 * @param fileName name of the file to write to.
		 * @param scriptFileName Name of the XSLT script file to format this or null.
		 * @return errorFlags like OPENERROR.
		 */
		private int prepFile(string fileName, string scriptFileName)
		{
			StreamWriter pw = null;
			int errorFlags = 0;
			try
			{
				pw = File.CreateText(fileName);
			}
			catch (IOException e)
			{
				 Console.Out.WriteLine(e.Message);
				 pw = null;
				 errorFlags = -1;
			}
			// write the new *.xml file
			m_pw = pw;
			writeHead(scriptFileName);
			return errorFlags;
		}

		/**
		 * Write the list to an XML file.
		 * @param fileName name of the file to write to.
		 * @param scriptFileName Name of the XSLT script file to format this or null.
		 * @return errorFlags like OPENERROR.
		 */
		public int writeToFile(String fileName, String scriptFileName)
		{
			int errorFlags = prepFile(fileName, scriptFileName);
			if (errorFlags != 0) return errorFlags;
			writeList();
			// m_eltStack should at least have <log> in it.
			while (m_eltStack.Count > 0) endElt ();
			writeList(); // to write out the end tags
			m_pw.Close();
			return 0;
		}

		/**
		 * Write an XML header with a default style sheet, StarLog.xsl.
		 * @param pw the PrintWriter to use.
		 * @param scriptFileName Name of the XSLT script file to format this or null.
		 */
		private void writeHead(String scriptFileName)
		{
			m_pw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			m_pw.Write("<?xml-stylesheet type=\"text/xsl\"");
			if (scriptFileName != null && !scriptFileName.Equals(""))
				m_pw.Write(" href=\"" + scriptFileName + "\"");
			m_pw.WriteLine("?>");
			m_pw.WriteLine();
		}

		/**
		 * Write the list of log entry strings to the XML file.
		 * @param pw the PrintWriter to use.
		 */
		private void writeList()
		{
			foreach (string data in m_remember) m_pw.WriteLine(data);
			m_remember.Clear();
		}

		private class ElementInfo
		{
			public string name = null;
			public bool hasContent = false;
			public ElementInfo(string name) {this.name = name;}
		}
	}
}