// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AdapterBase.cs
// Authorship History: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using SIL.Utils;

 // for ImageCollection

namespace XCore
{
	/// <summary>
	/// Base class for all adapters
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Fields are references")]
	public abstract class AdapterBase : IUIAdapter
	{
		#region Data members

		/// <summary>
		/// The XCore mediator.
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		/// The main XWindow form.
		/// </summary>
		protected Form m_window;
		/// <summary>
		/// Collection of small images.
		/// </summary>
		protected ImageCollection m_smallImages;
		/// <summary>
		/// Collection of large images.
		/// </summary>
		protected ImageCollection m_largeImages;
		/// <summary>
		/// The subclass specific main control that is given back to the adapter library client.
		/// </summary>
		protected System.Windows.Forms.Control m_control;

		#endregion Data members

		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public virtual void PersistLayout()
		{
		}

		protected string SettingsPath()
		{
			string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			path = Path.Combine(path, Path.Combine(Application.CompanyName, Application.ProductName));
			Directory.CreateDirectory(path);
			return path;
		}

		#region Properties

		/// <summary>
		/// Gets the main control that is given to the adapter library client.
		/// </summary>
		protected virtual Control MyControl
		{
			get { return m_control; }
		}

		/// <summary>
		/// The manager for whatever bars can be docked on any of the four main window edges.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="manager is a reference")]
		protected ToolStripManager Manager
		{
			get
			{
				ToolStripManager manager = (ToolStripManager)m_mediator.PropertyTable.GetValue("DotNetBarManager");
				if (manager == null)
				{
					manager = new ToolStripManager();
					m_window.Controls.Add(manager);
					m_mediator.PropertyTable.SetProperty("DotNetBarManager", manager);
					m_mediator.PropertyTable.SetPropertyPersistence("DotNetBarManager", false);
				}

				return manager;
			}

		}
		#endregion Properties

		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		public AdapterBase()
		{
		}

		#endregion Construction

		#region IUIAdapter implementation

		/// <summary>
		/// Initializes the adapter.
		/// </summary>
		/// <param name="window">The main form.</param>
		/// <param name="smallImages">Collection of small images.</param>
		/// <param name="largeImages">Collection of large images.</param>
		/// <param name="mediator">XCore Mediator.</param>
		/// <returns>A Control for use by client.</returns>
		public virtual System.Windows.Forms.Control Init(System.Windows.Forms.Form window,
			ImageCollection smallImages, ImageCollection largeImages, Mediator mediator)
		{
			m_window = window;
			m_smallImages = smallImages;
			m_largeImages = largeImages;
			m_mediator = mediator;
			if(this is IxCoreColleague)
				((IxCoreColleague)this).Init(mediator, null/*I suppose we could get these to the adapter if someone needs that someday*/);
			return MyControl;
		}

		/// <summary>
		/// Implement do-nothing method to keep the compiler happy.
		/// Subclasses override this method to do useful things.
		/// </summary>
		/// <param name="groupCollection">Collection of choices.</param>
		public virtual void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
		}


		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		/// <param name="group">The group that is the basis for this menu</param>
		public virtual void CreateUIForChoiceGroup(ChoiceGroup group)
		{
		}

		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		public virtual void OnIdle()
		{
		}

		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		public virtual void FinishInit()
		{
		}

		#endregion IUIAdapter implementation
	}
}
