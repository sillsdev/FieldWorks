// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: OnApplication.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
//using System.Diagnostics;
using System.Threading;
//using NUnit.Framework;
//using System.Windows.Forms;

namespace GuiTestDriver
{
	/// <summary>
	/// A OnStartup only exists in an OnApplication.
	/// There is only one in each.
	/// </summary>
	public class OnStartup : Context
	{
		public OnStartup()
		{
			m_tag = "on-startup";
		}

		/// <summary>
		/// Execute does nothing.
		/// The real work is invoked via ExecuteOnDemand,
		/// which is invoked from its OnApplication.
		/// </summary>
		public override void Execute()
		{
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// ExecuteOnDemand is a pass through for handling startup dialogs.
		/// It is invoked from its OnApplication.
		/// </summary>
		/// <param name="ts">The TestState object</param>
		public void ExecuteOnDemand()
		{
			base.Execute();
		}
	}
}
