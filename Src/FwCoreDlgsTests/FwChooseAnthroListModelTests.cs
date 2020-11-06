// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	public class FwChooseAnthroListModelTests
	{
		/// <summary/>
		[TestCase(AnthroListChoice.OCM, "OCM.xml")]
		[TestCase(AnthroListChoice.FRAME, "OCM-Frame.xml")]
		public void AnthroFileNameReturnsCorrectData(AnthroListChoice choice, string expectedFileName)
		{
			var model = new FwChooseAnthroListModel
			{
				CurrentAnthroListChoice = choice
			};
			Assert.That(model.AnthroFileName, Does.EndWith(expectedFileName));
		}

		/// <summary/>
		[Test]
		public void AnthroFileName_UserDefined_Returns_Null()
		{
			var model = new FwChooseAnthroListModel
			{
				CurrentAnthroListChoice = AnthroListChoice.UserDef
			};
			Assert.That(model.AnthroFileName, Is.Null);
		}
	}
}
