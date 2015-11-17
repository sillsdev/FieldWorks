// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using Accessibility;
using System.Diagnostics.CodeAnalysis;

namespace SIL.Utils
{
	/// <summary>
	/// ScriptMaker creates scripts reflecting what the user does. They are designed for replay using
	/// Michael Lastufka's script engine.
	/// A ScriptMaker
	/// </summary>
	public class ScriptMaker : IFWDisposable, IMessageFilter
	{
		XmlWriter m_output;
		StreamWriter m_StreamWriterToDispose;

		Set<Control> m_controls = new Set<Control>();
		/// <summary>
		/// Create a ScriptMaker for the specified control (typically some type of main window)
		/// and all its children.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="destination"></param>
		public ScriptMaker(Control root, StreamWriter destination)
		{
			Init(root, destination);
		}

		/// <summary>
		/// Create one that writes on the default 'Generated Script' file.
		/// </summary>
		/// <param name="root"></param>
		public ScriptMaker(Control root)
		{
			m_StreamWriterToDispose = new StreamWriter("Generated Script.xml");
			Init(root, m_StreamWriterToDispose);
		}

		void Init(Control root, StreamWriter destination)
		{
			m_output = new XmlTextWriter(destination);
			AttachTo(root);
			m_output.WriteStartDocument(true);
			m_output.WriteStartElement("instructions");
			Application.AddMessageFilter(this);
			var form = root as Form;
			if (form != null)
			{
				form.Closed += ScriptMaker_Closed;
			}
		}

		/// <summary>
		/// Insert a command to switch to the given URL (typically a FieldWorks one).
		/// </summary>
		/// <param name="url"></param>
		public void GoTo(string url)
		{
			CheckDisposed();

			m_output.WriteStartElement("goto");
			m_output.WriteAttributeString("url", url);
			m_output.WriteEndElement();
			m_output.WriteWhitespace(Environment.NewLine);
			m_output.Flush();
		}

		/// <summary>
		/// This method causes the specified control to participate in the logging process.
		/// If the control can have children, its children will also participate, and
		/// if we can trap an event for adding children, any children subsequently added
		/// will participate also.
		/// Other things are done for particular kinds of control.
		/// </summary>
		/// <param name="root"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "form.Menu is reference")]
		void AttachTo(Control root)
		{
			if (!m_controls.Contains(root))
			{
				m_controls.Add(root);
				foreach (Control c in root.Controls)
				{
					AttachTo(c);
					c.ControlAdded += new ControlEventHandler(c_ControlAdded);
				}
				root.MouseDown += new MouseEventHandler(root_MouseDown);
				root.KeyPress += new KeyPressEventHandler(root_KeyPress);
				var form = root as Form;
				if (form != null)
					foreach (MenuItem item in form.Menu.MenuItems)
						AttachTo(item);
			}
		}

		void AttachTo(MenuItem menu)
		{
			foreach (MenuItem item in menu.MenuItems)
				AttachTo(item);
			menu.Click += new EventHandler(menu_Click); // Review: do we want this for non-leaves?
		}

		/// <summary>
		/// Given a control, generates the complete path that the script language uses to identify it.
		/// This is reliably useful only if all parents have meaningful and sufficiently unique names.
		/// </summary>
		/// <param name="cLeaf"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Control c is reference")]
		public static string AccessPath(Control cLeaf)
		{
			StringBuilder sb = new StringBuilder();
			// Build the string from the back, as a sequence of role:name pairs separated by /
			for (Control c = cLeaf; c != null; c = c.Parent)
			{
				if (sb.Length != 0)
					sb.Insert(0, "/");
				System.Reflection.PropertyInfo pi = c.GetType().GetProperty("AccessibleRootObject");
				if (pi == null)
				{
					AccessibleObject ao = c.AccessibilityObject;
					sb.Insert(0, ao.Name);
					sb.Insert(0, ":");
					sb.Insert(0, ao.Role);
				}
				else
				{
					IAccessible acc = (IAccessible)pi.GetValue(c, new object[0]);
					sb.Insert(0, acc.get_accName(null));
					sb.Insert(0, ":");
					sb.Insert(0, InterpretRole(acc.get_accRole(null)));
				}
			}
			return sb.ToString();
		}

		static string InterpretRole(object role)
		{
			return ((AccessibleRole) role).ToString();
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "e.Control is reference")]
		private void c_ControlAdded(object sender, ControlEventArgs e)
		{
			AttachTo(e.Control);
		}

		/*
		private void root_Click(object sender, EventArgs e)
		{
			m_output.WriteStartElement("click");
			m_output.WriteAttributeString("path", AccessPath(sender as Control));
			m_output.WriteEndElement();
			m_output.WriteWhitespace("\n");
			m_output.Flush();
		}
		*/

		private void root_KeyPress(object sender, KeyPressEventArgs e)
		{
			m_output.WriteStartElement("press");
			m_output.WriteAttributeString("key", e.KeyChar.ToString());
			m_output.WriteAttributeString("path", AccessPath(sender as Control));
			m_output.WriteEndElement();
			m_output.WriteWhitespace(Environment.NewLine);
			m_output.Flush();
		}

		private void menu_Click(object sender, EventArgs e)
		{
			m_output.WriteStartElement("click");
			m_output.WriteAttributeString("path", AccessPath(sender as Control));
			m_output.WriteEndElement();
			m_output.WriteWhitespace(Environment.NewLine);
			m_output.Flush();
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ScriptMaker()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				Cleanup();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_output = null;
			m_StreamWriterToDispose = null;
			m_controls = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Cleanup and stop logging.
		/// </summary>
		public void Cleanup()
		{
			CheckDisposed();

			if (m_output == null)
				return; // already cleaned up.
			m_output.WriteEndElement();
			m_output.Close();
			m_output = null;
			if (m_StreamWriterToDispose != null)
				m_StreamWriterToDispose.Dispose();
			m_StreamWriterToDispose = null;
			Application.RemoveMessageFilter(this);
		}

		private void root_MouseDown(object sender, MouseEventArgs e)
		{
			Control c = sender as Control;
			AccessibleObject ao = c.AccessibilityObject;
			Point screenPoint = c.PointToScreen(new Point(e.X, e.Y));
			StringBuilder sb = new StringBuilder();
			System.Reflection.PropertyInfo pi = c.GetType().GetProperty("AccessibleRootObject");
			if (pi == null)
			{
				AccessibleObject aoPrev = null; // Some HitTests keep returning the leaf object.
				for (AccessibleObject aoCur = ao.HitTest(screenPoint.X, screenPoint.Y);
					aoCur != null && aoCur != ao && aoCur != aoPrev;
					aoCur = aoCur.HitTest(screenPoint.X, screenPoint.Y))
				{
					if (sb.Length > 0)
						sb.Append("/");
					sb.Append(aoCur.Role);
					sb.Append(":");
					sb.Append(aoCur.Name);
					aoPrev = aoCur;
				}
			}
			else
			{
				IAccessible acc = (IAccessible)pi.GetValue(c, new object[0]);

				for (IAccessible accCurr = (IAccessible)acc.accHitTest(screenPoint.X, screenPoint.Y);
					accCurr != null;
					accCurr = accCurr.accHitTest(screenPoint.X, screenPoint.Y) as IAccessible)
				{
					if (sb.Length > 0)
						sb.Append("/");
					sb.Append(InterpretRole(accCurr.get_accRole(null)));
					sb.Append(":");
					sb.Append(accCurr.get_accName(null));
				}
			}

			string leafPath = sb.ToString();

			m_output.WriteStartElement("click");
			m_output.WriteAttributeString("path", AccessPath(sender as Control));
			m_output.WriteAttributeString("at", "(" + e.X + "," + e.Y + ")");
			if (leafPath.Length > 0)
				m_output.WriteAttributeString("child", leafPath);
			m_output.WriteEndElement();
			m_output.WriteWhitespace(Environment.NewLine);
			m_output.Flush();

		}
		#region IMessageFilter Members

		/// <summary>
		/// Every message in the system comes through here while we're logging!!
		/// We want to catch window creation and attach our event handlers.
		/// </summary>
		/// <param name="m"></param>
		/// <returns></returns>
		public bool PreFilterMessage(ref Message m)
		{
			CheckDisposed();

			// WM_CREATE doesn't work. Apparently it isn't passed through the message filter at all.
			// Same for WM_SIZE.
			//Debug.WriteLine("prefiltering " + Convert.ToString(m.Msg, 16));
			if (m.Msg == (int)Win32.WinMsgs.WM_PAINT) // Review: what message??
			{
				Control c = Control.FromHandle(m.HWnd);
				if (c != null)
				{
					AttachTo(c);
				}
			}
			return false;
		}

		#endregion

		private void ScriptMaker_Closed(object sender, EventArgs e)
		{
			// Our original form is closed. Clean up.
			Cleanup();
		}
	}
}
