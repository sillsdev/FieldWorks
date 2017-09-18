// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.SendReceive
{
	/// <summary>
	/// Interface that allows for running different kinds of S/R bridges.
	/// </summary>
	internal interface IBridge : IFlexComponent, IDisposable
	{
		/// <summary>
		/// Get the name of the bridge.
		/// </summary>
		/// <remarks>
		/// Names of all implementations, *must* be unique!
		/// </remarks>
		string Name { get; }

		/// <summary>
		/// Run the bridge.
		/// </summary>
		void RunBridge();

		/// <summary>
		/// Install the currently specified round of menus in <paramref name="mainSendReceiveToolStripMenuItem"/>.
		/// </summary>
		/// <param name="currentInstallRound">The current round for adding new menu items in <paramref name="mainSendReceiveToolStripMenuItem"/>.</param>
		/// <param name="mainSendReceiveToolStripMenuItem">The top level S/R menu in which to add the new menu items for the given <paramref name="currentInstallRound"/>.</param>
		void InstallMenus(BridgeMenuInstallRound currentInstallRound, ToolStripMenuItem mainSendReceiveToolStripMenuItem);

		/// <summary>
		/// Enable/Disable menus.
		/// </summary>
		void SetEnabledStatus();
	}
}