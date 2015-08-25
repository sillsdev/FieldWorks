// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Command.cs
// Authorship History: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.IO;

using SIL.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Class that shows a button (or hyperlink someday) that
	/// runs some arbitrary XCore command, based on its ID.
	/// </summary>
	public class CommandSlice : Slice
	{
		/// <summary>
		/// Store the Command object that knows what to do.
		/// </summary>
		private XmlNode m_cmdNode;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="node">The "deParams" node in some XDE file.</param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "btn gets assigned to Control")]
		public CommandSlice(XmlNode node)
		{
			Debug.Assert(node != null);
			XmlNode cmdNode = node["command"];
			Debug.Assert(cmdNode != null);
			m_cmdNode = cmdNode;
			Button btn = new Button();

			btn.FlatStyle = FlatStyle.Popup;
			btn.Click += new EventHandler(btn_Click);
			Control = btn;
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				Button btn = Control as Button;
				if (btn != null)
					btn.Click -= new EventHandler(btn_Click);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cmdNode = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

//		// Overhaul Aug 05: want all Window backgrounds in Detail controls.
//		/// <summary>
//		/// Override to set it to the Control color.
//		/// </summary>
//		/// <param name="clr"></param>
//		public override void OverrideBackColor(String backColorName)
//		{
//			CheckDisposed();
//
//			if (this.Control == null)
//				return;
//			this.Control.BackColor = System.Drawing.SystemColors.Control;
//		}

		public override void RegisterWithContextHelper()
		{
			CheckDisposed();
			if (Control != null)//grouping nodes do not have a control
			{
				Publisher.Publish("RegisterHelpTargetWithId", new object[]{Control, ConfigurationNode.Attributes["label"].Value, HelpId});
			}
		}

#if RANDYTODO
		/// <summary>
		/// Override, so we can get the command object.
		/// </summary>
		public override XCore.Mediator Mediator
		{
			get
			{
				CheckDisposed();
				return base.Mediator;
			}
			set
			{
				CheckDisposed();
				base.Mediator = value;
				m_command = (Command)value.CommandSet[m_cmdNode.Attributes["cmdID"].Value];
				Debug.Assert(m_command != null);
				Control.Text = m_command.Label.Replace("_", null);
				Button b = (Button)Control;

				b.Width = 130;
			}
		}
#endif

		/// <summary>
		/// Handle click event on the button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btn_Click(object sender, EventArgs e)
		{
#if RANDYTODO
			m_command.InvokeCommand();
#endif
		}
	}
}
