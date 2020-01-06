// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks
{
	/// <summary />
	internal enum ButtonPress
	{
		/// <summary />
		Open,
		/// <summary />
		New,
		/// <summary />
		Restore,
		/// <summary />
		Exit,
		/// <summary>
		/// Receive a project through S/R LiftBridge or FLExBridge
		/// </summary>
		Receive,
		/// <summary>
		/// Import an SFM data set into a new FLEx project
		/// </summary>
		Import,
		/// <summary>
		/// Clicked the Sample/LastEdited project link
		/// </summary>
		Link
	}
}