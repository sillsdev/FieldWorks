// Copyright (c) 2004-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EntryDlgListener.cs
// Responsibility: Randy Regnier
using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.LexText
{
#if RANDYTODO
	// TODO: Likely disposition: Dump MergeEntryDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the merge.
	//
	public class MergeEntryDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		protected string PersistentLabel
		{
			get { return "MergeEntry"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region Construction and Initialization

		public MergeEntryDlgListener()
		{
		}

	#endregion Construction and Initialization

	#region XCORE Message Handlers

		/// <summary>
		/// Determines in which menus the Merge Entries command item can show up in.
		/// Should only be in the Lexicon area.
		/// </summary>
		/// <remarks>Obviously copied from another area that had more complex criteria for displaying its menu items.</remarks>
		/// <returns>true if Merge Entry ought to be displayed, false otherwise.</returns>
		protected bool InFriendlyArea
		{
			get
			{
				string areaChoice = PropertyTable.GetValue<string>("areaChoice");
				if (areaChoice == null) return false; // happens at start up
				if (AreaServices.InitialAreaMachineName == areaChoice)
				{
					return PropertyTable.GetValue<string>("toolChoice") == AreaServices.LexiconEditMachineName;
				}
				return false; //we are not in an area that wants to see the merge command
			}
		}
	#endregion XCORE Message Handlers
	}
#endif
}
