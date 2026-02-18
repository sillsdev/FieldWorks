// Copyright (c) 2008-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using NUnit.Framework;
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
	public class CaseSensitiveListBoxTests
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
				Assert.That(lb.FindString("b"), Is.EqualTo(1));
				Assert.That(lb.FindString("bl"), Is.EqualTo(1));
				Assert.That(lb.FindString("bL"), Is.EqualTo(2));
				Assert.That(lb.FindString("B"), Is.EqualTo(0));
				Assert.That(lb.FindString("Bl"), Is.EqualTo(3));
				Assert.That(lb.FindString("blAH"), Is.EqualTo(ListBox.NoMatches));
				Assert.That(lb.FindString("B\u00e1".Normalize(NormalizationForm.FormC)), Is.EqualTo(0));
				Assert.That(lb.FindString("B\u00e1".Normalize(NormalizationForm.FormD)), Is.EqualTo(0));
				Assert.That(lb.FindString("B\u00e1".Normalize(NormalizationForm.FormKC)), Is.EqualTo(0));
				Assert.That(lb.FindString("B\u00e1".Normalize(NormalizationForm.FormKD)), Is.EqualTo(0));
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
				Assert.That(lb.FindStringExact("b"), Is.EqualTo(ListBox.NoMatches));
				Assert.That(lb.FindStringExact("blah"), Is.EqualTo(1));
				Assert.That(lb.FindStringExact("bLAh"), Is.EqualTo(2));
				Assert.That(lb.FindStringExact("Blah"), Is.EqualTo(3));
				Assert.That(lb.FindStringExact("blAH"), Is.EqualTo(ListBox.NoMatches));
				Assert.That(lb.FindStringExact("cabbage"), Is.EqualTo(ListBox.NoMatches));
				Assert.That(lb.FindStringExact("B\u00e1".Normalize(NormalizationForm.FormC)), Is.EqualTo(0));
				Assert.That(lb.FindStringExact("B\u00e1".Normalize(NormalizationForm.FormD)), Is.EqualTo(0));
				Assert.That(lb.FindStringExact("B\u00e1".Normalize(NormalizationForm.FormKC)), Is.EqualTo(0));
				Assert.That(lb.FindStringExact("B\u00e1".Normalize(NormalizationForm.FormKD)), Is.EqualTo(0));
			}
		}
	}
}
