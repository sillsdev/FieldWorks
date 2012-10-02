// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2005' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: OnApplication.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms; // AccessibleRole
using System.Xml;
using System.Runtime.InteropServices; // DllImport

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for OnApplication.
	/// </summary>
	public class OnApplication : Context
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		static public extern bool SetForegroundWindow(IntPtr hWnd);

		AppHandle   m_app;
		Instruction m_source; // id of instruction where we get the application
		string      m_srcName;
		string      m_run;
		string      m_title;
		string      m_exeName;
		string      m_args;
		string      m_work; // working path
		string      m_gui; // id of application gui model.
		string      m_GuiModel;
		string      m_GuiModelPath;
		bool        m_close = false;
		XmlElement  m_model_root;

		public OnApplication()
		{
			m_tag      = "on-application";
			m_source   = null;
			m_srcName  = null;
			m_app      = null;
			m_run      = null;
			m_title    = null;
			m_exeName  = null;
			m_GuiModel = null;
			m_GuiModelPath = null;
			m_close    = false;
			m_args     = null;
			m_work     = null;
			m_gui      = null;
			m_model_root = null;
			// get the model root if there is one
		}

		/// <summary>
		/// Called to finish construction when an instruction has been instantiated by
		/// a factory and had its properties set.
		/// This can check the integrity of the instruction or perform other initialization tasks.
		/// </summary>
		/// <param name="xn">XML node describing the instruction</param>
		/// <param name="con">Parent xml node instruction</param>
		/// <returns></returns>
		public override bool finishCreation(XmlNode xn, Context con)
		{  // finish factory construction
			if (m_run == null || m_run == "") Run = "ok";
			if (m_srcName != null) SetSource(m_srcName);
			Configure(); // set model root for use by children
			ModelNode = con.ModelNode;
			return true;
		}

		public override AppHandle Application
		{
			get { return m_app; }
			set { m_app = value; }
		}

		public XmlElement ModelRoot
		{
			get { return m_model_root; }
		}

		public string Run
		{
			get {return m_run;}
			set {m_run = value;}
		}

		public string Title
		{
			get {return m_title;}
			set {m_title = value;}
		}

		public string Exe
		{
			get {return m_exeName;}
			set {m_exeName = value;}
		}

		public string Args
		{
			get {return m_args;}
			set {m_args = value;}
		}

		public string Work
		{
			get {return m_work;}
			set {m_work = value;}
		}

		public string Gui
		{
			get { return m_gui; }
			set { m_gui = value; }
		}

		public string Source
		{
			get { return m_srcName; }
			set { m_srcName = value; }
		}

		public string Close
		{
			get {if (m_close) return "yes";
				 else         return "no";}
			set {if (value != null && "yes" == value) m_close = true;
				 else                                 m_close = false;}
		}

		public void SetSource(string name)
		{
			// It has to be the name of another on-application element already encountered.
			TestState ts = TestState.getOnly();
			Instruction insSource = ts.Instruction(name);
			m_log.isTrue(insSource != null, "Source is null.");
			m_log.isTrue(insSource is OnApplication, "Source is not an on-application instruction."); // eventually or other types.
			m_source = insSource;
		}
		/// <summary>
		/// For Testing Only:
		/// Gets the Instruction that ties this context to an application.
		/// </summary>
		/// <returns>The Instruction that ties this context to an application.</returns>
		public Instruction TestGetOfSource()
		{
			return m_source;
		}

		/// <summary>
		/// Initialize this context parameters and gui model access.
		/// Call after all parameters have been set and before execution of any
		/// script instructions.
		/// </summary>
		public void Configure()
		{
			TestState ts = null;
			if (m_gui != null && m_gui != "")
			{ // use the specified Gui Config data
				ts = new TestState(m_gui);
			}
			else ts = TestState.getOnly(); // the test fixture state
			if (m_path == null) m_path = ts.getAppPath();
			if (m_exeName == null) m_exeName = ts.getAppExe();
			m_GuiModel = ts.getModelName();
			m_GuiModelPath = ts.getModelPath();
			m_model_root = getXmlModelRoot();
		}

		/// <summary>
		/// Execute the context instructions
		/// </summary>
		public override void Execute()
		{
			WaitMsec();

			m_Rest = 0; // don't wait later when base.Execute is invoked
			string failed = null;
			m_path    = Utilities.evalExpr(m_path);
			m_exeName = Utilities.evalExpr(m_exeName);
			m_title   = Utilities.evalExpr(m_title);
			if (m_args != null) m_args = Utilities.evalExpr(m_args);
			if (m_work != null) m_work = Utilities.evalExpr(m_work);
			string fullPath = m_path + @"\" + m_exeName;
			bool batch = false; // true if launched a batch file

			// Read and load the top-level variables from the model
			// as child instructions of this context.
			// Some may replace previously defined vars.
			ReadModelVars();
			// make "shells" for the child instructions.
			// start-up is needed when the app is launched.
			base.PrepareChildren(true); // add more children

			bool found = false;
			if ("top" == m_run)
			{ // get the top window as the application context
				m_ah = new AccessibilityHelper();
				m_log.isNotNull(m_ah, "Top window is not accessible");
				Process proc = Process.GetCurrentProcess();
				m_log.isNotNull(proc, "Top window is not the current process");
				m_title = proc.MainWindowTitle;
				m_app = new AppHandle(null,proc,m_ah);
				m_log.isNotNull(m_app, "Lost top window application handle for window with title" + m_title);
				found = true;
			}
			// Is the app running already?
			if (found == false) found = FindApp(1000);
			string contextPass, contextFail;
			PassFailInContext(OnPass, OnFail, out contextPass, out contextFail);	// out m_onPass, out m_onFail);
			if ("no" == m_run && !found)
			{
				if (m_title != null)        failed = "Application '"+ m_title +"' not found.";
				else if (m_exeName != null) failed = "Application '"+ m_exeName +"' not found.";
				else                        failed = "Application not found.";
			}
			if ("yes" == m_run && found)
			{
				m_app.Exit(false); // close all app windows
				m_app = null;
				found = false;
			}
			if (!found && failed == null && "no" != m_run)
			{
				if (m_exeName != null && m_exeName != "")
				{
					string [] parts = m_exeName.Split('.');
					if (parts[parts.GetLength(0) - 1].ToLower().Equals("bat"))
						batch = LaunchBat();
				}
				if (!batch && !LaunchApp())
					failed = "Application '"+ fullPath +"' not launched.";
			}

			if (contextFail == "assert" && failed != null && failed != "") m_log.fail(makeNameTag() + failed);
			if (contextPass == "assert" && failed == null) m_log.fail(makeNameTag() + "run='" + m_run + "' exe='" + m_exeName + "' passed");
			if (failed != null && failed != "")
			{ // OK to fail, but should note it
				m_log.paragraph(failed);
			}
			else if (!batch)
			{
				base.Execute();
				if (m_close) m_app.Exit(false); // close all app windows
				m_finished = true;
				resultImage();
			}
			// return focus back to the previous application if there was one
			AppHandle aph = base.Application;
			if (aph != null)
			{
				m_log.isNotNull(aph.Process, "Parent application lost its process");
				aph.Process.WaitForInputIdle();
				SetForegroundWindow(aph.Process.MainWindowHandle);
			}
		}

		private bool LaunchBat()
		{  // launch a batch file
			bool fStarted = true;
			m_log.isNotNull(m_path, "No application @path specified or in GtdConfig.xml for this batch file");
			string fullPath = m_path + @"\" + m_exeName;
			m_log.paragraph("Launching "+fullPath + " as a batch file");
			Process proc = new Process();
			m_log.isNotNull(proc, "Could not create a new process for this batch file.");
			proc.StartInfo.FileName = fullPath;
			if (m_args != null) proc.StartInfo.Arguments = m_args;
			proc.StartInfo.UseShellExecute = true;
			// since UseShellExecute = true, m_work is the *.bat path
			proc.StartInfo.WorkingDirectory = m_path;
			proc.Start();

			if (proc == null) fStarted = false;

			return fStarted;
		}

		private bool LaunchApp ()
		{
			// Launch the Application
			bool fStarted = true;
			m_log.isNotNull(m_path, "No application @path specified or in GtdConfig.xml");
			m_log.isNotNull(m_exeName, "No application executable name @exe specified or in GtdConfig.xml");
			string fullPath = m_path + @"\" + m_exeName;
			//Process proc = Process.Start(fullPath);
			// Need to set the working directory to m_path
			m_log.paragraph("Launching " + fullPath + " as an Application");
			Process proc = new Process();
			// can proc ever be null?
			m_log.isNotNull(proc, "Could not create a new process for this application.");
			proc.StartInfo.FileName = fullPath;
			if (m_args != null) proc.StartInfo.Arguments = m_args;
			//proc.StartInfo.Arguments = "/r:System.dll /out:sample.exe stdstr.cs";
			proc.StartInfo.UseShellExecute = false;
			//compiler.StartInfo.RedirectStandardOutput = true;
			if (m_work != null) proc.StartInfo.WorkingDirectory = m_work;
			else                proc.StartInfo.WorkingDirectory = m_path;
			proc.Start();

			if (proc == null) fStarted = false;
			// can this happen?
			m_log.isNotNull(proc, "Process became null when launched.");

			if (proc.WaitForInputIdle()) // true iff proc has a window (*.bat don't)
				m_log.isNotNull(proc.MainWindowHandle, "Window handle was not grasped");

			// proc.MainWindowHandle is always IntPtr.Zero
			// so, get another process that has proc.Id
			Process pOfId = Process.GetProcessById(proc.Id);
			m_log.isNotNull(pOfId, "Grabbed null process");
			m_log.isNotNull(pOfId.MainWindowHandle, "Grabbed process with no handle");
////			while (pOfId.MainWindowHandle == IntPtr.Zero)
////			{
////				Thread.Sleep(100);
////				pOfId = Process.GetProcessById(proc.Id);
////				isNotNull(pOfId,"Grabbed null process");
////				isNotNull(pOfId.MainWindowHandle,"Grabbed process with no handle");
////			}
			if (proc.HasExited) fStarted = false;

			// make the window show itself
			pOfId.WaitForInputIdle();
			m_log.isNotNull(pOfId.MainWindowTitle, "Window has no title");
			SetForegroundWindow(pOfId.MainWindowHandle);

			// get a new accessibility object as it has more nodes in it now.
			m_ah = new AccessibilityHelper(pOfId.MainWindowHandle);
			m_log.isNotNull(m_ah, "Can't access app with title" + pOfId.MainWindowTitle);
			m_app = new AppHandle(null,pOfId,m_ah);
			m_log.isNotNull(m_app, "Null application handle for window with title" + pOfId.MainWindowTitle);

			// Call OnStartup.ExecuteOnDemand(ts) if there is one.
			// Otherwise, wait for and find the main app window.
			OnStartup startUp = null;
			foreach ( Instruction ins in m_instructions)
			{
				string stType = ins.GetType().ToString();
				if (stType == "GuiTestDriver.OnStartup")
				{
					startUp = (OnStartup)ins;
					break;
				}
			}
			if (startUp != null)
			{// pass higher log levels to this child
				if (startUp.Log < Log)
					startUp.Log = Log;
				startUp.ExecuteOnDemand();
			}

			// Now find the App in case we got the splash screen or something
			fStarted = FindApp(60000); // look for upto a minute

			return fStarted;
		}

		/// <summary>
		/// Find the application main window accessibility object.
		/// </summary>
		/// <param name="maxWait">The maximum time to find it in milliseconds.</param>
		/// <returns>True if the app was found.</returns>
		private bool FindApp (int maxWait)
		{
			bool found = false;
			if (m_source != null)
			{
				// We had a ref to another Application Context element.
				if (m_source is OnApplication)
				{
					m_app = ((OnApplication)m_source).Application;
					found = m_app != null;
					m_log.isNotNull(m_app, "Source for on-application is null");
					m_ah = m_app.MainAccessibilityHelper;
					m_log.isNotNull(m_app.Process, "Source for on-application lost its process");
					m_app.Process.WaitForInputIdle();
					SetForegroundWindow(m_app.Process.MainWindowHandle);
				}
			}
			if (!found && m_exeName != null)
			{
				int pos = m_exeName.IndexOf('.',m_exeName.Length-5);
				string procId = m_exeName.Substring(0,pos);
				Process[] proc = Process.GetProcessesByName(procId);
				if (proc != null && proc.Length > 0)
				{
					Logger.getOnly().paragraph(procId+": has "+(proc.Length-1)+" siblings.");
					proc[0].WaitForInputIdle(); // doesn't wait long enough
					IntPtr hwnd = waitForWindow(maxWait, proc[0]);
					if (0 == hwnd.ToInt32()) return false;
					m_title = proc[0].MainWindowTitle;
					//AccessibilityHelper ah = new AccessibilityHelper(m_title);
					AccessibilityHelper ah = new AccessibilityHelper(hwnd);
					found = ah != null;
					if(found)
					{
						m_app = new AppHandle(null,proc[0],ah);
						found = m_app != null;
						if (found)
						{
							m_ah = ah;

							// make the found window show itself
							proc[0].WaitForInputIdle();
							string image;
							if (m_ah != null)
							{
								AccessibleRole ar;
								string ars, an;
								try {ar = m_ah.Role; ars = ar.ToString();}
								catch(Exception e){ars = e.Message;}
								try {an = m_ah.Name;}
								catch(Exception e){an = e.Message;}
								image = @" ah="""+ars+@":"+an+@"""";
							}
							else image = @"ah=""null""";
							Logger.getOnly().paragraph("about to set focus on "+image);
							SetForegroundWindow(proc[0].MainWindowHandle);
							Logger.getOnly().paragraph("Focus set on "+m_title);
						}
					}
				}
			}
			return found;
		}

		/// <summary>
		/// Waits for the process's main window to show up to the maxWait time.
		/// </summary>
		/// <param name="maxWait">Max time to wait for the window in milliseconds.</param>
		/// <param name="proc">The process whose window is displayed or is forming.</param>
		/// <returns>The main window handle.</returns>
		public IntPtr waitForWindow(int maxWait, Process proc)
		{   /*A process has a main window associated with it
			 * only if the process has a graphical interface.
			 * If the associated process does not have a
			 * main window (so that MainWindowHandle is zero),
			 * MainWindowTitle is an empty string ("").
			 * If you have just started a process and want to
			 * use its main window title, consider using the
			 * WaitForInputIdle method to allow the process
			 * to finish starting, ensuring that the main
			 * window handle has been created. Otherwise,
			 * the system throws an exception.
			 */
			IntPtr hwnd = proc.MainWindowHandle;
			Logger log = Logger.getOnly();
			log.paragraph("Waiting hwnd = "+hwnd.ToInt32()+" for proc = "+proc.Id);
			int startTick = System.Environment.TickCount;
			while (0 == hwnd.ToInt32() && !proc.HasExited)
			{
				if (Utilities.NumTicks(startTick, System.Environment.TickCount) > maxWait + 500)
					break;	// time is up
				System.Threading.Thread.Sleep(500);
				/* When a Process component is associated with a process resource,
				 * the property values of the Process are immediately populated
				 * according to the status of the associated process.
				 * If the information about the associated process subsequently changes,
				 * those changes are not reflected in the Process component's cached
				 * values. The Process component is a snapshot of the process resource
				 * at the time they are associated. To view the current values for the
				 * associated process, call the Refresh method.*/
				proc.Refresh();
				hwnd = proc.MainWindowHandle;
				log.paragraph("Waiting hwnd = "+hwnd.ToInt32()+" for proc = "+proc.Id);
			}
			return hwnd;
		}

		/// <summary>
		/// Opens and reads the GUI Model XML file returning the root element.
		/// The GUI Model name was obtained from the test script.
		/// </summary>
		/// <param name="caller">The name of the caller for use in asserts.</param>
		/// <returns>The root of the XML tree</returns>
		private XmlElement getXmlModelRoot()
		{
			XmlElement root = null;
			if (m_GuiModelPath != null && m_GuiModelPath != "")
			{
				XmlDocument gmDoc = new XmlDocument();
				try { gmDoc.Load(m_GuiModelPath + @"\" + m_GuiModel); }
				catch (XmlException e)
				{ // log and leave
					Logger.getOnly().paragraph("Failed to read a section of the GUI Model because: " + e.Message);
					return null;
				}
				root = gmDoc.DocumentElement;
				m_log.isNotNull(root, "Application cannot find GuiModel root");

				// get the section nodes and read the content of their files
				XmlNodeList sections = gmDoc.SelectNodes("fwuiml/section");
				if (sections != null)
				{
					foreach (XmlNode sec in sections)
					{
						try
						{
							XmlDocument secDoc = new XmlDocument();
							string file = sec.Attributes["file"].Value;
							string sRoot = null; // is there a section root?
							if (sec.Attributes["root"] != null) sRoot = sec.Attributes["root"].Value;

							string path = m_GuiModelPath + @"\" + file;
							try { secDoc.Load(path); }
							catch (Exception e)
							{ m_log.fail("Model file " + path + ": " + e.Message); }
							m_log.isNotNull(secDoc, "XML Document '" + path + "' is empty");

							XmlDocumentFragment secFrag = gmDoc.CreateDocumentFragment();
							m_log.isNotNull(secFrag, "XML Fragment '" + path + "' is empty");
							XmlNode top = secDoc.SelectSingleNode("*").Clone();
							// since the node is from a different doc, it must be converted to text,
							// then inserted into a fragment and finally appended to the root.
							secFrag.InnerXml = top.OuterXml;
							XmlNode child = secFrag.FirstChild;
							m_log.isNotNull(child, "XML Fragment '" + path + "' has no child");

							if (sRoot == null) root.AppendChild(child);
							else
							{ // Drop the root element, use the children
								XmlNodeList children = child.ChildNodes;
								foreach (XmlNode chosen in children)
									// AppendChild seems to delete chosen from children unless you clone!!
									root.AppendChild(chosen.CloneNode(true));
							}
						}
						catch (Exception e)
						{
							m_log.fail("Failed to read a section of the GUI Model because: " + e.Message);
						}
					}
				}
			}
			return root;
		}

		/// <summary>
		/// Read the var nodes from the model and add them as
		/// child instructions to this context.
		/// </summary>
		private void ReadModelVars()
		{
			XmlNodeList vars = m_model_root.SelectNodes("var");
			if (vars != null)
			{
				foreach (XmlNode varNode in vars)
				{
					Var varObj = (Var)Instructionator.MakeShell(varNode, this);
				}
			}
		}

		/// <summary>
		/// Gets the image of the specified data. If name is null,
		/// the instruction's sequence number is returned.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "number";
			switch (name)
			{
				case "source":	return m_source.Id;
				case "run":		return m_run;
				case "exe":		return m_exeName;
				case "args":	return m_args;
				case "work":	return m_work;
				case "gui":		return m_gui;
				case "title":	return m_title;
				case "guiModel":return m_GuiModel;
				case "guiPath":	return m_GuiModelPath;
				case "app":		return m_app.ToString();
				case "close":	if (m_close) return "yes";
								else         return "no";
				default:		return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			if (m_source != null)       image += @" source="""+m_source.Id+@"""";
			if (m_run != null)          image += @" run="""+m_run+@"""";
			if (m_exeName != null)      image += @" exe="""+m_exeName+@"""";
			if (m_args != null)         image += @" args=""" + Utilities.attrText(m_args) + @"""";
			if (m_work != null)         image += @" work="""+m_work+@"""";
			if (m_gui != null)          image += @" gui="""+m_gui+@"""";
			if (m_title != null)        image += @" title="""+Utilities.attrText(m_title)+@"""";
			if (m_close)                image += @" close=""yes""";
			else                        image += @" close=""no""";
			if (m_GuiModel != null)     image += @" model="""+m_GuiModel+@"""";
			if (m_GuiModelPath != null) image += @" modPath="""+m_GuiModelPath+@"""";
			if (m_app != null)          image += @" appHandle="""+m_app+@"""";
			return image;
		}
	}
}
