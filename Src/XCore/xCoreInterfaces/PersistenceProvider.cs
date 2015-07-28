// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	///  A PersistenceProvider which uses the XCore PropertyTable
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "variable is a reference; it is owned by parent")]
	public class PersistenceProvider : IPersistenceProvider
	{
		protected string m_contextString;
		protected PropertyTable m_propertyTable;

		/// <summary>
		/// create a PersistenceProvider which uses the XCore PropertyTable.
		/// </summary>
		/// <param name="context">used to provide persistence and access to settings
		/// limited to a particular context. For example, if they control is used in
		/// three different places, we don't necessarily want to control to use the
		/// same settings each time. So each case would need its own context string.</param>
		/// <param name="propertyTable"></param>
		public PersistenceProvider(string context, PropertyTable propertyTable)
		{
			m_contextString= context;
			m_propertyTable = propertyTable;
		}
		/// <summary>
		/// create a PersistenceProvider which uses the XCore PropertyTable.
		/// </summary>
		/// <param name="propertyTable"></param>
		public PersistenceProvider(PropertyTable propertyTable)
		{
			m_contextString= "Default";
			m_propertyTable = propertyTable;
		}

		public void RestoreWindowSettings(string id,Form form)
		{
			object state = Get(id,"windowState");
			//don't bother restoring the program to the minimized state.
			if (state != null && ((FormWindowState)state) !=
				FormWindowState.Minimized)
			{
				form.WindowState = (FormWindowState)state;
			}

			object location = Get(id,"windowLocation");
			object size = Get(id,"windowSize");

			if (location != null)
			{
				form.Location = (Point)location;
				// The location restoration only works if the window startposition is set to
				// "manual" because the window is not visible yet, and the location will be
				// changed when it is Show()n.
				form.StartPosition = FormStartPosition.Manual;
			}
			if (size != null)
				form.Size = (Size)size;

			// Fix the stored position in case it is off the screen.  This can happen if the
			// user has removed a second monitor, or changed the screen resolution downward,
			// since the last time he ran the program.  (See LT-1078.)
			Rectangle rcNewWnd = form.DesktopBounds;
//			Rectangle rcScrn = System.Windows.Forms.Screen.FromRectangle(rcNewWnd).WorkingArea;
			ScreenUtils.EnsureVisibleRect(ref rcNewWnd);
			form.DesktopBounds = rcNewWnd;
		}

		protected string GetPrefix(string id)
		{
			return m_contextString+"-"+id;
		}

		protected object Get(string id,string label)
		{
			return m_propertyTable.GetValue(GetPrefix(id)+"-"+label);
		}

		protected void Set(string id,string label, object value)
		{
			m_propertyTable.SetProperty(GetPrefix(id)+"-"+label, value);
		}

		public void PersistWindowSettings(string id,Form form)
		{
			Set(id,"windowState", form.WindowState);

			if (form.WindowState == FormWindowState.Normal)
				Set(id,"windowSize", form.Size);

			//don't bother storing the location if we are maximized or minimized.
			//if we did, then when the user exits the application and then runs it again,
			//	then switches to the normal state, we would be switching to 0,0 or something.
			if (form.WindowState == FormWindowState.Normal)
				Set(id, "windowLocation", form.Location);
		}

		public Object GetInfoObject(string id, Object defaultValue)
		{
			return m_propertyTable.GetValue(GetPrefix(id), defaultValue);
		}
		public void SetInfoObject(string id, Object info)
		{
			m_propertyTable.SetProperty(GetPrefix(id), info, false);
		}

	}
}
