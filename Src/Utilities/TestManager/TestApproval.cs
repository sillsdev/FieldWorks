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
// File: TestManager.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// Notice that so far, the only public methods are static.  Keep it this way if you can.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Specialized;
using NUnit.Framework;
using Microsoft.Win32;

namespace SIL
{
	/// <summary>
	/// automated tests use this class to determine whether they should run.
	/// </summary>
	public class TestManager
	{
		static protected TestManager s_manager;
		protected System.Collections.Specialized.StringCollection m_tokens;

		//protected because we want a Singleton and clients only need to use static methods.
		protected TestManager()
		{
			string tokens="";
			try
			{
				RegistryKey key =Registry.CurrentUser.OpenSubKey(@"Software\SIL\Fieldworks\TestManager");
				tokens = (string)key.GetValue("Approve");
			}
			catch	//if the registry isn't set up right, then we just treat the list of tokens as empty
			{
			}

			string[] tokenList = tokens.Split(new char[]{' ', ','});
			m_tokens = new StringCollection();
			//ensure that capitalization does not mess anyone up
			foreach(string token in tokenList)
			{
				m_tokens.Add(token.ToLower());
			}
		}

		/// <summary>
		/// use this check if an individual test has different requirements than the rest of the fixture
		/// </summary>
		/// <param name="group">e.g. "WW", "TE", etc. </param>
		/// <param name="token">e.g. "UI", "1Min" </param>
		/// <param name="label">the label of your test that you would like to show up in the console message if this fails</param>
		/// <returns></returns>
		static public bool ApproveTest(string group, string token, string label)
		{
			bool ok=  Approver.TestOk(group,token);
			if (!ok)
				Console.WriteLine("Skipping test \""+ label +"\" ("+ group + ", " + token +").");
			return ok;
		}

		/// <summary>
		/// use this check if possible, in order to avoid the entire fixture
		/// </summary>
		/// <param name="group">e.g. "WW", "TE", etc. </param>
		/// <param name="token">e.g. "UI", "1Min" </param>
		static public void ApproveFixture(string group, string token)
		{
			Assert.IsTrue(Approver.TestOk(group,token),
				"Fixture not compatible with current test manager settings ("+ group + ", " + token +")");
		}

		protected bool TestOk(string group, string token)
		{
			//nb: ensure that capitalization does not mess anyone up

			//we approve it if we find either just a token (UI), or that token preceded by the group name (WW:UI)
			return m_tokens.Contains (group.ToLower() + ":" + token.ToLower()) ||  m_tokens.Contains (token.ToLower());
		}

		//this gives us a Singleton
		static protected TestManager Approver
		{
			get
			{
				if (s_manager== null)
					s_manager = new TestManager();
				return s_manager;
			}
		}
	}
}
