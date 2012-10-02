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
// File: NoOp.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// NoOp is a No Operation instruction used to test the test driver.
	/// </summary>
	public class NoOp : ActionBase
	{
		int m_data; // dummy data

		public NoOp()
		{
			m_data = 0;
			m_tag = "NoOp";
		}

		public int Data
		{
			get { return m_data; }
			set { m_data = value;}
		}
		public override void Execute()
		{
			// doesn't do anything
			// later, it may write "No Op" to the test log
		}
		public override string GetDataImage (string name)
		{
			return m_data.ToString();
		}
	}
}
