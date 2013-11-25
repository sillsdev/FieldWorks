// Copyright 2007 Herre Kuijpers - <herre@xs4all.nl>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IFloaty.cs
// Responsibility: TE Team

using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ControlExtenders
{
	#region DockingEventArgs class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides data for the Docking event.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DockingEventArgs : EventArgs
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DockingEventArgs"/> class.
		/// </summary>
		/// <param name="origFloatingBounds">The orig floating bounds.</param>
		/// <param name="dockStyle">The dock style being applied or that has just been
		/// applied.</param>
		/// ------------------------------------------------------------------------------------
		public DockingEventArgs(Rectangle origFloatingBounds, DockStyle dockStyle)
		{
			OriginalFloatingBounds = origFloatingBounds;
			DockStyle = dockStyle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the original floating bounds.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle OriginalFloatingBounds;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the DockStyle about to be applied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DockStyle DockStyle = DockStyle.None;
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>Represents the method that will the Docking event of the IFloaty interface.
	/// </summary>
	/// <param name="sender">The source of the event</param>
	/// <param name="e">A <see cref="DockingEventArgs"/> that contains the event data.</param>
	/// ----------------------------------------------------------------------------------------
	public delegate void DockingEventHandler(object sender, DockingEventArgs e);

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// this is the publicly exposed interface of the floating window (floaty)
	/// add more methods/properties here for your own needs, so these are exposed to the client
	/// the main goal is to keep the floaty form internal
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFloaty
	{
		/// <summary>
		/// show the floaty
		/// </summary>
		void Show();

		/// <summary>
		/// hide the floaty
		/// </summary>
		void Hide();

		/// <summary>
		/// Show the floaty and make control undocked
		/// </summary>
		void Float();

		/// <summary></summary>
		void LoadSettings(RegistryKey key);

		/// <summary></summary>
		void SaveSettings(RegistryKey key);

		/// <summary>Requires that SettingsKey is set before hand.</summary>
		void LoadSettings();

		/// <summary>Requires that SettingsKey is set before hand.</summary>
		void SaveSettings();

		/// <summary></summary>
		RegistryKey SettingsKey { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Docks the control
		/// </summary>
		/// <param name="dockStyle">The dock style.</param>
		/// ------------------------------------------------------------------------------------
		void Dock(DockStyle dockStyle);

		/// <summary>
		/// set a caption for the floaty
		/// </summary>
		string Text { get; set; }

		/// <summary>
		/// indicates if a floaty may dock only on the host docking control (e.g. the form)
		/// and not inside other floaties
		/// </summary>
		bool DockOnHostOnly { get; set; }

		/// <summary>
		/// indicates if a floaty may dock on the inside or on the outside of a form/control
		/// default is true
		/// </summary>
		bool DockOnInside { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value that indicates whether to hide the handle when floating or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool HideHandle { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value that indicates where the floaty might be docked. Default is
		/// Left | Top | Right | Bottom.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		AnchorStyles AllowedDocking { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the size of the floating container.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Rectangle FloatingContainerBounds { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to show or hide the splitter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ShowSplitter { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether to show the close button (red X) in the title bar
		/// of the floaty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ShowCloseButton { set; }

		/// <summary>
		/// Answer whether it is visible.
		/// </summary>
		bool Visible { get; }

		/// <summary>
		/// Return how the control is docked. None is used for 'floating'.
		/// </summary>
		DockStyle DockMode { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value that prefixes all registry value settings for the floaty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string RegistryValuePrefix { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating the default dock location if the previous dock
		/// location isn't found in the registry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		DockStyle DefaultLocation { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating the user can make the window floating
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool AllowFloating { get; set; }

		/// <summary>Occurs when the floating window is docked.</summary>
		event DockingEventHandler Docking;

		/// <summary>Occurs when the window is undocked, i.e. floating.</summary>
		event EventHandler Undocked;

		/// <summary>Occurs when the docking or undocking begins but before anything with the
		/// floating has taken place.</summary>
		event DockingEventHandler DockUndockBegin;

		/// <summary>Occurs when the docking or undocking is complete.</summary>
		event DockingEventHandler DockUndockEnd;

		/// <summary>Occurs when the docked window's splitter moved.</summary>
		event SplitterEventHandler SplitterMoved;
	}
}
