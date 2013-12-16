// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeAppInteract.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.AcceptanceTests.Framework;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// <summary>
	/// <see cref="AppInteract"/> for TE.
	/// </summary>
	public class TeAppInteract: AppInteract
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeAppInteract"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TeAppInteract(): base(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(
			Assembly.GetExecutingAssembly().CodeBase.Substring("file:///".Length)),
			@"TE.exe")))
		{

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Switch to the concordance view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GoToConcordanceView()
		{
			// TODO (EberhardB): Views/Concordance is missing shortcut
			SendKeys("%v{RIGHT}{ENTER}");		// Alt-V (View menu)

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Switch to the draft view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GoToDraftView()
		{
			// TODO (EberhardB): Views/Draft is missing shortcut
			SendKeys("%v{RIGHT}{DOWN}{ENTER}");		// Alt-V (Views menu)

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string that the Information Bar displays
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InfoBarValue
		{
			get
			{
				AccessibilityHelper infoBar =
					MainAccessibilityHelper.FindChild("InfoBarLabel", AccessibleRole.None);

				return infoBar.Value;
			}
		}

	}
}
