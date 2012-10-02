// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeAppInteract.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
