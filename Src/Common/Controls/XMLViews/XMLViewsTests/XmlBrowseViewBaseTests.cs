// Copyright (c) 2012, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2012-11-05 XmlBrowseViewBaseTests.cs

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Keyboarding;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using XCore;

namespace XMLViewsTests
{
	[TestFixture]
	public class XmlBrowseViewBaseTests : BaseTest
	{
		/// <summary>
		/// Needs enough scroll height to scroll down to the last element. FWNX-858
		/// </summary>
		[Test]
		public void HasEnoughScrollHeight()
		{
			FakeKeyboardController.Install();

			using (var area = new XmlBrowseViewBase())
			{
				var doc = new XmlDocument();
				doc.LoadXml(@"<parameters/>");

				using (var mediator = new Mediator())
				{
					area.Init(mediator, doc.DocumentElement);
					var rootbox = VwRootBoxClass.Create();
					rootbox.SetSite(area);
					ReflectionHelper.SetField(area, "m_rootb", rootbox);

					var scrollRange = (Size)ReflectionHelper.GetProperty(area, "AdjustedScrollRange");

					int reasonablyEnoughSpace = Convert.ToInt32(2.5 * SystemInformation.HorizontalScrollBarHeight);

					Assert.That(scrollRange.Height, Is.GreaterThanOrEqualTo(reasonablyEnoughSpace));
				}
			}
		}
	}
}
