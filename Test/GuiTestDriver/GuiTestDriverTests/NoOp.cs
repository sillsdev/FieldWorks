// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: NoOp.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

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
