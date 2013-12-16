// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CaseSensitiveListBoxTests.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the CaseSensistiveListBox class to make sure it treats the items as case sensitive
	/// when looking for string matches.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CaseSensitiveListBoxTests : BaseTest
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindString
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindString()
		{
			using (CaseSensitiveListBox lb = new CaseSensitiveListBox())
			{
				lb.Items.Add("B\u00e1".Normalize(NormalizationForm.FormC));
				lb.Items.Add("blah");
				lb.Items.Add("bLAh");
				lb.Items.Add("Blah");
				lb.Items.Add("Blah");
				Assert.AreEqual(1, lb.FindString("b"));
				Assert.AreEqual(1, lb.FindString("bl"));
				Assert.AreEqual(2, lb.FindString("bL"));
				Assert.AreEqual(0, lb.FindString("B"));
				Assert.AreEqual(3, lb.FindString("Bl"));
				Assert.AreEqual(ListBox.NoMatches, lb.FindString("blAH"));
				Assert.AreEqual(0, lb.FindString("B\u00e1".Normalize(NormalizationForm.FormC)));
				Assert.AreEqual(0, lb.FindString("B\u00e1".Normalize(NormalizationForm.FormD)));
				Assert.AreEqual(0, lb.FindString("B\u00e1".Normalize(NormalizationForm.FormKC)));
				Assert.AreEqual(0, lb.FindString("B\u00e1".Normalize(NormalizationForm.FormKD)));
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindStringExact
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindStringExact()
		{
			using (CaseSensitiveListBox lb = new CaseSensitiveListBox())
			{
				lb.Items.Add("B\u00e1".Normalize(NormalizationForm.FormC));
				lb.Items.Add("blah");
				lb.Items.Add("bLAh");
				lb.Items.Add("Blah");
				lb.Items.Add("Blah");
				Assert.AreEqual(ListBox.NoMatches, lb.FindStringExact("b"));
				Assert.AreEqual(1, lb.FindStringExact("blah"));
				Assert.AreEqual(2, lb.FindStringExact("bLAh"));
				Assert.AreEqual(3, lb.FindStringExact("Blah"));
				Assert.AreEqual(ListBox.NoMatches, lb.FindStringExact("blAH"));
				Assert.AreEqual(ListBox.NoMatches, lb.FindStringExact("cabbage"));
				Assert.AreEqual(0, lb.FindStringExact("B\u00e1".Normalize(NormalizationForm.FormC)));
				Assert.AreEqual(0, lb.FindStringExact("B\u00e1".Normalize(NormalizationForm.FormD)));
				Assert.AreEqual(0, lb.FindStringExact("B\u00e1".Normalize(NormalizationForm.FormKC)));
				Assert.AreEqual(0, lb.FindStringExact("B\u00e1".Normalize(NormalizationForm.FormKD)));
			}
		}
	}
}
