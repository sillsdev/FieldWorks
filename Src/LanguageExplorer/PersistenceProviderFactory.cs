// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Windows.Forms;

namespace LanguageExplorer
{
	/// <summary>
	/// Factory that creates an instance of IPersistenceProvider
	/// </summary>
	internal static class PersistenceProviderFactory
	{
		/// <summary>
		/// Create an instance of IPersistenceProvider
		/// </summary>
		/// <param name="propertyTable">The property table to use for persistence.</param>
		/// <param name="context">The persistence context</param>
		internal static IPersistenceProvider CreatePersistenceProvider(IPropertyTable propertyTable, string context)
		{
			return new PersistenceProvider(propertyTable, context);
		}

		/// <summary>
		/// Create an instance of IPersistenceProvider
		/// </summary>
		/// <param name="propertyTable">The property table to use for persistence.</param>
		internal static IPersistenceProvider CreatePersistenceProvider(IPropertyTable propertyTable)
		{
			return CreatePersistenceProvider(propertyTable, AreaServices.Default);
		}

		/// <summary>
		///  A PersistenceProvider which uses the IPropertyTable
		/// </summary>
		private sealed class PersistenceProvider : IPersistenceProvider
		{
			private string m_contextString;
			private IPropertyTable m_propertyTable;

			/// <summary>
			/// create a PersistenceProvider which uses the IPropertyTable.
			/// </summary>
			/// <param name="propertyTable"></param>
			/// <param name="context">used to provide persistence and access to settings
			/// limited to a particular context. For example, if they control is used in
			/// three different places, we don't necessarily want to control to use the
			/// same settings each time. So each case would need its own context string.</param>
			internal PersistenceProvider(IPropertyTable propertyTable, string context)
			{
				m_propertyTable = propertyTable;
				m_contextString = context;
			}

			void IPersistenceProvider.RestoreWindowSettings(string id, Form form)
			{
				var state = Get(id, LanguageExplorerConstants.windowState);
				//don't bother restoring the program to the minimized state.
				if (state != null && ((FormWindowState)state) != FormWindowState.Minimized)
				{
					form.WindowState = (FormWindowState)state;
				}
				var location = Get(id, LanguageExplorerConstants.windowLocation);
				var size = Get(id, LanguageExplorerConstants.windowSize);
				if (location != null)
				{
					form.Location = (Point)location;
					// The location restoration only works if the window startposition is set to
					// "manual" because the window is not visible yet, and the location will be
					// changed when it is Show()n.
					form.StartPosition = FormStartPosition.Manual;
				}
				if (size != null)
				{
					form.Size = (Size)size;
				}
				// Fix the stored position in case it is off the screen.  This can happen if the
				// user has removed a second monitor, or changed the screen resolution downward,
				// since the last time he ran the program.  (See LT-1078.)
				var rcNewWnd = form.DesktopBounds;
				ScreenHelper.EnsureVisibleRect(ref rcNewWnd);
				form.DesktopBounds = rcNewWnd;
			}

			private string GetPrefix(string id)
			{
				return $"{m_contextString}-{id}";
			}

			private object Get(string id, string label)
			{
				return m_propertyTable.GetValue<object>($"{GetPrefix(id)}-{label}");
			}

			private void Set(string id, string label, object value)
			{
				m_propertyTable.SetProperty($"{GetPrefix(id)}-{label}", value, true, true);
			}

			void IPersistenceProvider.PersistWindowSettings(string id, Form form)
			{
				Set(id, LanguageExplorerConstants.windowState, form.WindowState);
				if (form.WindowState == FormWindowState.Normal)
				{
					Set(id, LanguageExplorerConstants.windowSize, form.Size);
				}
				//don't bother storing the location if we are maximized or minimized.
				//if we did, then when the user exits the application and then runs it again,
				//	then switches to the normal state, we would be switching to 0,0 or something.
				if (form.WindowState == FormWindowState.Normal)
				{
					Set(id, LanguageExplorerConstants.windowLocation, form.Location);
				}
			}

			object IPersistenceProvider.GetInfoObject(string id, object defaultValue)
			{
				return m_propertyTable.GetValue(GetPrefix(id), defaultValue);
			}
			void IPersistenceProvider.SetInfoObject(string id, object info)
			{
				m_propertyTable.SetProperty(GetPrefix(id), info, true);
			}
		}
	}
}