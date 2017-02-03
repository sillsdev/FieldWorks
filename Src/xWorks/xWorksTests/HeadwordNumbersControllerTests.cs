// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class HeadwordNumbersControllerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{

		[Test]
		public void ConstructorRejectsNulls()
		{
			var view = new TestHeadwordNumbersView();
			var model = new DictionaryConfigurationModel();
			Assert.Throws<ArgumentNullException>(() => new HeadwordNumbersController(view, model, null));
			Assert.Throws<ArgumentNullException>(() => new HeadwordNumbersController(null, model, Cache));
			Assert.Throws<ArgumentNullException>(() => new HeadwordNumbersController(view, null, Cache));
		}

		/// <summary>
		/// Verify that data from the singleton HomographConfiguration is used when there is no data in the dictionary model
		/// </summary>
		[Test]
		public void SetsViewDataFromSingletonIfNoHomographConfigurationInConfigurationModel()
		{
			var hc = Cache.ServiceLocator.GetInstance<HomographConfiguration>();
			var view = new TestHeadwordNumbersView
			{
				HomographBefore = !hc.HomographNumberBefore,
				ShowHomograph = !hc.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef),
				ShowSenseNumber = !hc.ShowSenseNumberRef
			};
			var model = new DictionaryConfigurationModel();
			// ReSharper disable once UnusedVariable
			var testController = new HeadwordNumbersController(view, model, Cache);
			view.Show();
			Assert.AreEqual(view.HomographBefore, hc.HomographNumberBefore);
			Assert.AreEqual(view.ShowHomographOnCrossRef, hc.ShowHomographNumber(HomographConfiguration.HeadwordVariant.DictionaryCrossRef));
			Assert.AreEqual(view.ShowSenseNumber, hc.ShowSenseNumberRef);
		}

		[Test]
		public void ViewReflectsModelContents()
		{
			var testConfig = new DictionaryHomographConfiguration
			{
				HomographNumberBefore = true, ShowHwNumInCrossRef = false, ShowSenseNumber = false
			};
			var view = new TestHeadwordNumbersView { HomographBefore = false, ShowHomograph = true, ShowSenseNumber = true};
			var model = new DictionaryConfigurationModel {  HomographNumbers = testConfig };
			// ReSharper disable once UnusedVariable
			var testController = new HeadwordNumbersController(view, model, Cache);
			view.Show();
			Assert.IsTrue(view.HomographBefore);
			Assert.IsFalse(view.ShowHomographOnCrossRef);
			Assert.IsFalse(view.ShowSenseNumber);
		}

		[Test]
		public void ViewReflectsModelContents_Reversal()
		{
			var testConfig = new DictionaryHomographConfiguration
			{
				HomographNumberBefore = true,
				ShowHwNumInReversalCrossRef = false,
				ShowSenseNumberReversal = false
			};
			var view = new TestHeadwordNumbersView();
			var model = new DictionaryConfigurationModel { WritingSystem = "en", HomographNumbers = testConfig };
			// ReSharper disable once UnusedVariable
			var testController = new HeadwordNumbersController(view, model, Cache);
			view.Show();
			Assert.IsTrue(view.HomographBefore);
			Assert.IsFalse(view.ShowHomographOnCrossRef);
			Assert.IsFalse(view.ShowSenseNumber);
		}

		[Test]
		public void Save_SetsModelContents()
		{
			var testConfig = new DictionaryHomographConfiguration
			{
				HomographNumberBefore = true,
				ShowHwNumInCrossRef = false,
				ShowSenseNumber = false
			};
			var view = new TestHeadwordNumbersView { HomographBefore = false, ShowHomograph = true, ShowSenseNumber = true };
			var model = new DictionaryConfigurationModel { HomographNumbers = testConfig };
			// ReSharper disable once UnusedVariable
			var testController = new HeadwordNumbersController(view, model, Cache);
			view.Show();
			view.HomographBefore = false;
			view.ShowHomographOnCrossRef = true;
			view.ShowSenseNumber = true;
			// SUT
			testController.Save();
			// Verify save in Dictionary Config
			Assert.IsFalse(model.HomographNumbers.HomographNumberBefore);
			Assert.IsTrue(model.HomographNumbers.ShowHwNumInCrossRef);
			Assert.IsTrue(model.HomographNumbers.ShowSenseNumber);
		}

		[Test]
		public void Save_SetsModelContents_Reversal()
		{
			var testConfig = new DictionaryHomographConfiguration
			{
				HomographNumberBefore = true,
				ShowHwNumInReversalCrossRef = false,
				ShowSenseNumberReversal = false
			};
			var view = new TestHeadwordNumbersView { HomographBefore = false, ShowHomograph = true, ShowSenseNumber = true };
			var model = new DictionaryConfigurationModel { WritingSystem = "en", HomographNumbers = testConfig };
			// ReSharper disable once UnusedVariable
			var testController = new HeadwordNumbersController(view, model, Cache);
			view.Show();
			view.HomographBefore = false;
			view.ShowHomographOnCrossRef = true;
			view.ShowSenseNumber = true;
			// SUT
			testController.Save();
			// Verify save in Dictionary Config
			Assert.IsFalse(model.HomographNumbers.HomographNumberBefore);
			Assert.IsTrue(model.HomographNumbers.ShowHwNumInReversalCrossRef);
			Assert.IsTrue(model.HomographNumbers.ShowSenseNumberReversal);
		}

		[Test]
		public void ConstructorSetsDescriptionTextInView()
		{
			var view = new TestHeadwordNumbersView();
			var model = new DictionaryConfigurationModel { Label = "Webster" };
			// ReSharper disable once UnusedVariable
			// SUT
			var testController = new HeadwordNumbersController(view, model, Cache);

			Assert.That(view.Description, Is.StringContaining("Dictionary"), "Description should say current 'Dictionary' configuration");
			Assert.That(view.Description, Is.StringContaining("Webster"), "Description should include the current configuration label");
		}

		[Test]
		public void ConstructorSetsDescriptionTextInView_Reversal()
		{
			var view = new TestHeadwordNumbersView();
			var model = new DictionaryConfigurationModel { WritingSystem = "en", Label = "PrincePauper"};
			// ReSharper disable once UnusedVariable
			// SUT
			var testController = new HeadwordNumbersController(view, model, Cache);

			Assert.That(view.Description, Is.StringContaining("Reversal Index"), "Description should say current 'Reversal Index' configuration");
			Assert.That(view.Description, Is.StringContaining("PrincePauper"), "Description should include the current configuration label");
		}

		public class TestHeadwordNumbersView : IHeadwordNumbersView
		{
			public event EventHandler Shown = delegate(object sender, EventArgs args) {  };
			public bool HomographBefore { get; set; }
			public bool ShowHomograph { get; set; }
			public bool ShowHomographOnCrossRef { get; set; }
			public bool ShowSenseNumber { get; set; }
			public void Show() { Shown.Invoke(this, new EventArgs());}
			public string Description { get; set; }
			public event EventHandler RunStylesDialog = delegate(object sender, EventArgs args) {  };
		}
	}
}
