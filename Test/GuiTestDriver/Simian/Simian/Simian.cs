// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Scribe.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Xml;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Simian
{
	/// <summary>
	/// Simian is a SIMulated Input ANimal for testing applications via a Gui Model.
	///
	/// </summary>
	class Simian : ISensorExec, IActionExec, IPathVisitor
	{
		private Configure m_Config = null;
		private GuiModel  m_GuiModel = null;
		private Log       m_log = null;
		private AccessibilityHelper m_ah = null;
		private Process   m_proc = null;
		private MarkedList m_views = null;
		private VarNodes  m_varNodes = null;


		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("User32.dll")]
		public static extern IntPtr FindWindow(string strClassName, string strWindowName);

		/// <summary>
		/// Creates the Simian from the config file and sets up the rules file.
		/// If null, a default is used for that file.
		/// </summary>
		/// <param name="ConfigFile">The name of the configuration XML file or null</param>
		public Simian(string ConfigFile)
		{
			m_Config = Configure.getOnly(ConfigFile);
			m_log = new Log(m_Config.getLogFile(), null);
			m_GuiModel = m_Config.getGuiModel();
			m_varNodes = VarNodes.getOnly();
		}

		/// <summary>
		/// Determines the result of a Simian sensation.
		/// Problems with sensors are not generally logged
		/// since they are frequently called and most are not
		/// fatal.
		/// </summary>
		/// <param name="sensorRef">A sensor expression in a rule.</param>
		/// <returns>true if the sensor detected its target.</returns>
		public bool sensation(EmptyElement sensorRef)
		{
			if (sensorRef.getName().Equals("window"))
			{   // is the window showing?
				string id = sensorRef.getValue("id"); // a VarNode
				string title = sensorRef.getValue("title");
				MarkedNode mn = null;
				if (Utilities.isGoodStr(id) && Utilities.isGoodStr(title))
				{  // fetch the title from the model node via id + title
					mn = m_varNodes.get(id);
					// bail out if id not defined yet.
					if (mn == null) return false;
					if (mn.node != null)
						title = m_GuiModel.selectToString(mn.node, title, "title");
					if (!Utilities.isGoodStr(title)) return false;
				}
				else if (id == null && title == null)
				{   // get the main window title from the model
					title = m_Config.getDataBase()+m_GuiModel.getTitle();
				}
				if (title == null) return false;  // model lacks title
				if (Utilities.isGoodStr(title))
				{
					IntPtr winHand = FindWindow(null, title);
					if ((int)winHand != 0)
					{
						AccessibilityHelper ah = new AccessibilityHelper(winHand);
						// look for a titlebar
						GuiPath gPath = new GuiPath("1:NAMELESS");
						AccessibilityHelper tah = ah.SearchPath(gPath, this);
						if (tah == null || !tah.Value.Equals(title)) return false;
						m_ah = ah;
						return true;
					}
				}
			}
			if (sensorRef.getName().Equals("tested"))
			{
				string id = sensorRef.getValue("id"); // which controls
				string control = sensorRef.getValue("control"); // which controls
				string count = sensorRef.getValue("count"); // indicates how many
				if (control != null && count == null) return false;
				if (control == null && count != null) return false;
				if (control == null && count == null && id == null) return false;
				if (id != null)
				{
					MarkedNode mn = m_varNodes.get(id);
					if (mn != null)
					{
						if (mn.mark == null) return false;
						return mn.mark.Equals("tested");
					}
				}
				// if id fails to return a marked node, try a control count
				if (control != null)
				{
					int n = 0; int k = 0;
					if (control.Equals("view"))
					{
						if (m_views == null) m_views = new MarkedList(m_GuiModel, "//*[@role='view']");
						n = m_views.Count();
						k = m_views.Count("tested");
					}
					if (count.Equals("all") && n == k) return true;
					if (count.Equals("not-all") && k < n) return true;
					return false;
				}
				return false;
			}
			if (sensorRef.getName().Equals("glimpse"))
			{
				string id = sensorRef.getValue("id"); // a VarNode
				string appPath = sensorRef.getValue("on"); // an appPath
				string guiPath = sensorRef.getValue("at"); // a giuPath
				string property = sensorRef.getValue("prop"); // an ah property
				string expect = sensorRef.getValue("expect"); // value expected
				// Id provides a context ah that must be used to find the rest of the path!
				// can't just use the appPath from it. What if it's a dialog?
				XmlNode context = null;
				MarkedNode mn = null;
				if (Utilities.isGoodStr(id))
				{
					mn = m_varNodes.get(id);
					// bail out if id not defined yet.
					if (mn == null) return false;
					if (mn.node != null) context = mn.node;
				}
				return glimpse(context, appPath, guiPath, property, expect);
			}
			return false;
		}

		/// <summary>
		/// Determines the result of a Simian action.
		/// When actions can't be performed the problem is logged
		/// and false is returned.
		/// </summary>
		/// <param name="actionRef">An action in a rule.</param>
		/// <returns>true if the action was initiated successfully.</returns>
		public bool doAction(EmptyElement actionRef)
		{
			if (actionRef.getName().Equals("launch"))
			{   // launch the specified application
				bool usedModel = false;
				string path = actionRef.getValue("path");
				if (path == null)
				{
					path = m_Config.getExePath();
					usedModel = true;
				}
				string name = actionRef.getValue("name");
				if (name == null) name = m_Config.getExeName();
				string args = actionRef.getValue("args");
				if (args == null && usedModel) args = m_Config.getExeArgs();
				string work = actionRef.getValue("work");
				if (work == null && usedModel) work = m_Config.getWorkDir();
				return LaunchApp(path, name, args, work);
			}
			if (actionRef.getName().Equals("mark"))
			{   // mark the node as indicated
				string id = actionRef.getValue("id"); // a VarNode
				if (id == null) return false;
				string As = actionRef.getValue("as"); // How to mark the node
				// As == null is valid for removing the mark.
				return m_views.Mark(id, As);
			}
			if (actionRef.getName().Equals("free"))
			{   // free the VarNode named
				string id = actionRef.getValue("id"); // the VarNode to free
				if (id == null) return false;
				m_varNodes.add(id, null);
			}
			if (actionRef.getName().Equals("choose"))
			{   // choose the control via the method and name it via id
				string control = actionRef.getValue("control"); // Type of the control to choose
				string id = actionRef.getValue("id"); // a VarNode
				string exclude = actionRef.getValue("exclude"); // How to choose
				string method = actionRef.getValue("method"); // How to choose
				if (!Utilities.isGoodStr(control)) return false;
				if (!Utilities.isGoodStr(id)) return false;
				if (!Utilities.isGoodStr(method)) return false;
				MarkedNode mn = null;
				if (control.Equals("view")) mn = m_views.Choose(method, exclude);
				if (mn == null)
				{
					m_log.writeEltTime("fail");
					m_log.writeAttr(control, mn.node.Name);
					m_log.writeAttr("was", "not known");
					m_log.endElt();
					return false;
				}
				else
				{
					m_varNodes.add(id, mn);
					m_log.writeEltTime("selected");
					m_log.writeAttr(control, mn.node.Name);
					m_log.writeAttr("as", id);
					m_log.endElt();
				}
			}
			if (actionRef.getName().Equals("nav"))
			{   // find a model path to the referenced node
				string to = actionRef.getValue("to"); // a VarNode
				string via = actionRef.getValue("via"); // a VarNode name, not set yet
				if (!Utilities.isGoodStr(to)) return false;
				MarkedNode mn = m_varNodes.get(to);
				if (mn == null) return false;
				// What kind of node is this?
				string role = XmlFiler.getStringAttr((XmlElement)mn.node, "role", "*not Found*");
				if (role.Equals("*not Found*")) return false;
				MarkedNode viaN = null;
				if (role.Equals("view"))
				{  // get the menu node via mn and name it "via"
					string xPath = "menubar//" + mn.node.Name + "[@role='menu']";
					XmlPath mPath = m_GuiModel.selectToXmlPath(null, xPath);
					if (mPath == null) return false; // really bad!
					XmlNode menuNode = mPath.ModelNode;
					if (menuNode == null) return false; // really bad again!
					viaN = new MarkedNode(menuNode, null);
					m_varNodes.add(via, viaN);
				}
				if (viaN == null) return false; // nothing more to do at the moment
			}
			if (actionRef.getName().Equals("click"))
			{   // click the specified control
				string id = actionRef.getValue("id"); // a VarNode
				string appPath = actionRef.getValue("on"); // an appPath
				string guiPath = actionRef.getValue("at"); // a giuPath
				string side = actionRef.getValue("side"); // "left" or "right"
				bool leftSide = true;
				if (side != null && side.Equals("right")) leftSide = false;
				// Id provides a context ah that must be used to find the rest of the path!
				// can't just use the appPath from it. What if it's a dialog?
				XmlNode context = null;
				MarkedNode mn = null;
				if (Utilities.isGoodStr(id))
				{
					mn = m_varNodes.get(id);
					// bail out if id not defined yet.
					if (mn == null || mn.node == null) return false;
					context = mn.node;
				}
				return click(context, appPath, guiPath, leftSide);
			}
			if (actionRef.getName().Equals("close"))
			{   // close the specified application
				if (m_proc != null) closeWindow(m_proc);
			}
			return true;
		}

		/// <summary>
		/// Gets an app-path xPath string from a VarNode id.
		/// It includes all the elements from the root.
		/// The root may not be the application, but a dialog or view, etc..
		/// If there is a problem, null is returned.
		/// </summary>
		/// <param name="id">A VarNode name.</param>
		/// <returns>xPath to the model node.</returns>
		private string getAppPathFromId(string id)
		{
			if (!Utilities.isGoodStr(id)) return null;
			MarkedNode mn = m_varNodes.get(id);
			if (mn == null) return null;
			XmlPath xp = new XmlPath(mn.node);
			return xp.xPath();
		}

		/// <summary>
		/// Launch the application to test.
		/// </summary>
		/// <returns>true if the application launched successfully.</returns>
		private bool LaunchApp(string exePath, string exeName, string args, string workDir)
		{
			// Launch the Application
			bool fStarted = true;
			if (!Utilities.isGoodStr(exePath))
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("exe-path", "null or empty");
				m_log.endElt();
				return false;
			}
			if (!Utilities.isGoodStr(exeName))
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("exe-name", "null or empty");
				m_log.endElt();
				return false;
			}
			string fullPath = exePath + @"\" + exeName + @".exe";
			// Need to set the working directory to m_path
			Process proc = new Process();
			if (!Utilities.isGoodStr(exeName))
			{   // can proc ever be null?
				m_log.writeEltTime("fail");
				m_log.writeAttr("splash-screen", "could not create");
				m_log.writeAttr("exe-full-path", fullPath);
				m_log.endElt();
				return false;
			}
			proc.StartInfo.FileName = fullPath;
			if (args != null) proc.StartInfo.Arguments = args;
			//proc.StartInfo.Arguments = "/r:System.dll /out:sample.exe stdstr.cs";
			proc.StartInfo.UseShellExecute = false;
			//compiler.StartInfo.RedirectStandardOutput = true;
			if (workDir != null) proc.StartInfo.WorkingDirectory = workDir;
			else proc.StartInfo.WorkingDirectory = exePath;
			proc.Start();

			if (proc == null)
			{   // can this happen?
				m_log.writeEltTime("fail");
				m_log.writeAttr("splash-screen", "null process");
				m_log.writeAttr("exe-full-path", fullPath);
				m_log.endElt();
				return false;
			}

			proc.WaitForInputIdle();
			m_log.writeEltTime("splash-exited");
			m_log.endElt();
			proc.Refresh(); // sometimes the proc gets stale.

			// proc.MainWindowHandle is always IntPtr.Zero
			// so, get another process that has proc.Id
			// Process pOfId = Process.GetProcessById(proc.Id);
			Process pOfId = proc;
			if (pOfId == null)
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("main-window", "null process");
				m_log.writeAttr("exe-full-path", fullPath);
				m_log.endElt();
				return false;
			}

			pOfId.WaitForInputIdle();

			IntPtr hWnd;
			try { hWnd = pOfId.MainWindowHandle; }
			catch (InvalidOperationException e)
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("main-window", "handle not grasped");
				m_log.writeAttr("exe-full-path", fullPath);
				m_log.writeAttr("message", e.Message);
				m_log.endElt();
				return false;
			}

			// tried Form.FromHandle(hWnd), Form.FromChildHandle(hWnd), NativeWindow.FromHandle(hWnd);
			// They return null because the hWnd comes from another process.

			// make the window show itself

			if (pOfId.MainWindowTitle == null)
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("main-window", "no title");
				m_log.writeAttr("exe-full-path", fullPath);
				m_log.endElt();
				return false;
			}

			SetForegroundWindow(pOfId.MainWindowHandle);

			// get a new accessibility object as it has more nodes in it now.
			m_ah = new AccessibilityHelper(pOfId.MainWindowHandle);
			if (m_ah == null)
			{
				m_log.writeEltTime("fail");
				m_log.writeAttr("main-window", "not accessible");
				m_log.writeAttr("title", pOfId.MainWindowTitle);
				m_log.endElt();
				return false;
			}
			m_proc = proc;
			return fStarted;
		}

		/// <summary>
		/// Find an Active Accessible object on an appPath via the
		/// gui model and/or a guiPath via Active Accessibility.
		/// The appPath is applied first if not null, then the guiPath.
		/// </summary>
		/// <param name="xmlPath">the xmlPath to a control or null</param>
		/// <param name="guiPath">the guiPath to click or null</param>
		/// <returns>The Active Accessible object or null</returns>
		private AccessibilityHelper findInGui(XmlPath xmlPath, string guiPath)
		{
			string path = null;
			if (xmlPath != null && xmlPath.isValid()) path = xmlPath.Path;
			if (Utilities.isGoodStr(guiPath)) path += guiPath;
			if (!Utilities.isGoodStr(path)) return null;
			GuiPath gp = new GuiPath(Utilities.evalExpr(path));
			if (gp == null || !gp.isValid()) return null;
			return gp.FindInGui(m_ah, this);
		}

		/// <summary>
		/// Click on an appPath via the gui model and/or a guiPath via
		/// Active Accessibility.
		/// If only the context is not null, it's value is used.
		/// If the context is null, the model root is used with appPath.
		/// The appPath is applied first, if any, then the guiPath.
		/// If there is no context or appPath, the guiPath is used.
		/// </summary>
		/// <param name="context">Model node to start appPath from or null</param>
		/// <param name="appPath">the appPath to a control or null</param>
		/// <param name="guiPath">the guiPath to click or null</param>
		/// <param name="leftClick">true if left-click, false on right-click</param>
		/// <returns>true if the path was valid and a click attempted</returns>
		private bool click(XmlNode context, string appPath, string guiPath, bool leftClick)
		{
			// if only a context, select it to click on.
			XmlPath xp = m_GuiModel.selectToXmlPath(context, appPath);
			if (xp == null || !xp.isValid()) return false;
			AccessibilityHelper ah = findInGui(xp, guiPath);
			if (ah == null) return false;
			if (leftClick) ah.SimulateClickRelative(10, 10);
			else           ah.SimulateRightClickRelative(10, 10);
			return true;
		}

		/// <summary>
		/// Look at a window control using Active Accessibility via an appPath
		/// and the gui model and/or a guiPath.
		/// The property of the control is matched against the expected value.
		/// If only the context is not null, it's value is used as a path to the control.
		/// If the context is null, the model root is used with appPath to find the path.
		/// The appPath is applied first, if any, then the guiPath to create a path.
		/// If there is no context or appPath, the guiPath is used for the path.
		/// </summary>
		/// <param name="context">Model node to start appPath from or null</param>
		/// <param name="appPath">the appPath to a control or null</param>
		/// <param name="guiPath">the guiPath to glimpse or null</param>
		/// <param name="property">names the property to look at - "present" is default</param>
		/// <param name="expect">text to match against the property value</param>
		/// <returns>true if the expected value matched what was found.</returns>
		private bool glimpse(XmlNode context, string appPath, string guiPath, string property, string expect)
		{
			// if only a context, select it to glimpse.
			if (appPath == null && guiPath == null) property = "value";
			XmlPath xp = m_GuiModel.selectToXmlPath(context, appPath);
			if (xp == null || !xp.isValid()) return false;
			AccessibilityHelper ah = findInGui(xp, guiPath);
			if (ah == null) return false;
			if (!Utilities.isGoodStr(expect) && xp != null)
			{
				if (xp.ModelNode is XmlAttribute || xp.ModelNode is XmlText)
				{
					expect = xp.ModelNode.Value;
					if (Utilities.isGoodStr(expect) && !Utilities.isGoodStr(property))
						property = "value";
				}
			}
			return GlimpseGUI(ah, property, expect);
		}

		/// <summary>
		/// method to apply when a non-terminal node has been found
		/// </summary>
		/// <param name="ah"></param>
		public void visitNode(AccessibilityHelper ah)
		{ // does this ah need to be clicked or something to get to its children?
			if (ah.Role == AccessibleRole.MenuItem)
			{
				bool isFocused = (ah.States & AccessibleStates.Focused) == AccessibleStates.Focused;
				if (!isFocused) ah.SimulateClickRelative(10, 10);
				else            ah.MoveMouseOverMe(); // hover
			}
		}

		/// <summary>
		/// Method to apply when a path node is not found.
		/// No path above it and none below will call this method;
		/// only the one not found.
		/// </summary>
		/// <param name="path">The path step last tried.</param>
		public void notFound(GuiPath path)
		{
			m_log.writeElt("not-found");
			m_log.writeAttr(path.Role.ToString(), path.Name);
			m_log.endElt();
		}

		private void closeWindow(Process proc)
		{
			if (proc == null) return;
			proc.WaitForInputIdle();
			proc.Refresh();
			if (proc.CloseMainWindow())
			{
				proc.Close();
			}
			try
			{
				if (!proc.HasExited)
				{  // didn't send the close message
					m_log.writeElt("fail");
					m_log.writeAttr("close", "message not sent");
					m_log.writeAttr("kill", "issued");
					m_log.endElt();
					proc.Kill();
				}
			}
			catch (InvalidOperationException){}
		}

		/// <summary>
		/// Examines the GUI for the value of the property specified via @prop.
		/// If @expect is set, the @prop value is compared to it.
		/// The result is true when @expect = the @prop GUI value.
		/// When @prop names a boolean property, and @expect is not set, then
		/// the boolean value is the result.
		/// </summary>
		/// <param name="ah">Context accessibility helper, taken as the starting place for gpath.</param>
		/// <param name="property">The property value to look at.</param>
		/// <param name="expect">The expected value of the prop attribute.</param>
		/// <returns>True if @expect = @prop GUI value, false otherwise, or the boolean value of @prop in the GUI.</returns>
		bool GlimpseGUI(AccessibilityHelper ah, string property, string expect)
		{
			bool result = true;
			string got = null;
			if (ah == null)
			{
				if (property == "absent") return true;
				return false;
			}
			if (property == null) property = "present";
			switch (property)
			{
			case "children":
				{
					if (expect == null) expect = "0";
					if (Utilities.IsNumber(expect) == false) return false;
						// fail("children requires an integer not '" + expect + "'.");
					int val = ah.ChildCount;
					got = val.ToString();
					result = val.Equals((int)Utilities.GetNumber(expect));
					break;
				}
			case "handle":
				{
					if (expect == null) expect = "0";
					if (Utilities.IsNumber(expect) == false) return false;
						// fail("handle requires a big integer not '" + expect + "'.");
					int val = ah.HWnd;
					got = val.ToString();
					result = val.Equals((int)Utilities.GetNumber(expect));
					break;
				}
			case "hotkey":
				{
					got = ah.Shortcut;
					if (got == null || got == "") got = "NONE";
					result = got == expect; // neither can be null
					break;
				}
			case "name":
				{
					got = ah.Name;
					if (got == null) got = "NAMELESS";
					result = got == expect; // neither can be null
					break;
				}
			case "role":
				{
					if (expect == null) expect = "none";
					got = ah.Role.ToString();
					result = got == expect;
					break;
				}
			case "value":
				{
					got = ah.Value;
					if (expect != null && expect.StartsWith("rexp#"))
					{
						Regex rx = new Regex(expect.Substring(5));
						result = rx.IsMatch(got);
						m_log.writeElt("regular-expression");
						m_log.writeAttr("expect", expect.Substring(5));
						m_log.writeAttr("on", got);
						m_log.writeAttr("was", result.ToString());
						m_log.endElt();
					}
					else result = got == expect; // either can be null
					break;
				}
			case "visible":
				{
					if (expect == null) expect = "True";
					result = !(((AccessibleStates.Invisible & ah.States) == AccessibleStates.Invisible) ||
						((AccessibleStates.Offscreen & ah.States) == AccessibleStates.Offscreen));
					got = result.ToString();
					result = got.ToLower() == expect.ToLower();
					break;
				}
			case "checked":
				{
					if (expect == null) expect = "True";
					result = ((AccessibleStates.Checked & ah.States) == AccessibleStates.Checked);
					got = result.ToString();
					result = got.ToLower() == expect.ToLower();
					break;
				}
			case "selected":
				{
					if (expect == null) expect = "True";
					result = ((AccessibleStates.Selected & ah.States) == AccessibleStates.Selected);
					got = result.ToString();
					result = got.ToLower() == expect.ToLower();
					break;
				}
			case "present": break;
			case "unavailable":
				{
					if (expect == null) expect = "True";
					result = ((AccessibleStates.Unavailable & ah.States) == AccessibleStates.Unavailable);
					got = result.ToString();
					result = got.ToLower() == expect.ToLower();
					break;
				}
			default:
				{
					m_log.writeElt("glimpse-fail");
					m_log.writeAttr("prop", property);
					m_log.writeAttr("was", "not understood");
					m_log.endElt();
					got = result.ToString();
					result = false;
					break;
				}
			}
			return result;
		}

		/// <summary>
		/// The main entry point to activate the Simian.
		/// To run type Simian.exe ConfigName RulesName
		/// </summary>
		/// <param name="args">The exeName followed by config and script</param>
		public static void Main(string[] args)
		{ // set up this simian from the default config file or that specified.
			string ConfigFile = null;
			if (args.Length > 1)
			{
				if (args[1].Length > 0 && args[1] != "")
					ConfigFile = args[1];
			}
			Simian monkey = new Simian(ConfigFile);
			//Sensact testing = new Sensact(@"C:\Testing\XP\Simian\SimianRules.xml", monkey, monkey);
			Sensact testing = new Sensact(@"C:\Testing\XP\Simian\NavRules.xml", monkey, monkey);
			bool instruct = testing.setGoal(null); // use the one in the rules file
			bool done = testing.act();
			Log log = Log.getOnly();
			log.endElt(); // end the log
		}
	}
}
