// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
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
		/// Verify that data from the default HomographConfiguration is used when there is no data in the dictionary model
		/// </summary>
		[Test]
		public void SetsViewDataFromDefaultsIfNoHomographConfigurationInConfigurationModel()
		{
			var hc = new HomographConfiguration();
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
			var model = new DictionaryConfigurationModel {  HomographConfiguration = testConfig };
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
			var model = new DictionaryConfigurationModel { WritingSystem = "en", HomographConfiguration = testConfig };
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
				ShowSenseNumber = false,
			};
			var view = new TestHeadwordNumbersView { HomographBefore = false, ShowHomograph = true, ShowSenseNumber = true };
			var model = new DictionaryConfigurationModel { HomographConfiguration = testConfig };
			// ReSharper disable once UnusedVariable
			var testController = new HeadwordNumbersController(view, model, Cache);
			view.Show();
			view.HomographBefore = false;
			view.ShowHomographOnCrossRef = true;
			view.ShowSenseNumber = true;
			// SUT
			testController.Save();
			// Verify save in Dictionary Config
			Assert.IsFalse(model.HomographConfiguration.HomographNumberBefore);
			Assert.IsTrue(model.HomographConfiguration.ShowHwNumInCrossRef);
			Assert.IsTrue(model.HomographConfiguration.ShowSenseNumber);
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
			var model = new DictionaryConfigurationModel { WritingSystem = "en", HomographConfiguration = testConfig };
			// ReSharper disable once UnusedVariable
			var testController = new HeadwordNumbersController(view, model, Cache);
			view.Show();
			view.HomographBefore = false;
			view.ShowHomographOnCrossRef = true;
			view.ShowSenseNumber = true;
			// SUT
			testController.Save();
			// Verify save in Dictionary Config
			Assert.IsFalse(model.HomographConfiguration.HomographNumberBefore);
			Assert.IsTrue(model.HomographConfiguration.ShowHwNumInReversalCrossRef);
			Assert.IsTrue(model.HomographConfiguration.ShowSenseNumberReversal);
		}

		[Test]
		public void Ok_Enabled_WithNoCustomNumbers()
		{
			// Test enabled on initial setup
			var view = new TestHeadwordNumbersView {OkButtonEnabled = false};
			var model = new DictionaryConfigurationModel();
			var controller = new HeadwordNumbersController(view, model, Cache);
			// verify ok button enabled on setup with no numbers
			Assert.IsTrue(view.OkButtonEnabled, "Ok not enabled by controller constructor");
			view.OkButtonEnabled = false;
			// verify ok button enabled when event is triggered and there are no custom numbers
			view.TriggerCustomDigitsChanged();
			Assert.IsTrue(view.OkButtonEnabled, "Ok button not enabled when event is fired");
		}

		[Test]
		public void Ok_Enabled_WithAllTenNumbers()
		{
			// Test enabled on initial setup
			var view = new TestHeadwordNumbersView { OkButtonEnabled = false };
			var model = new DictionaryConfigurationModel
			{
				HomographConfiguration = new DictionaryHomographConfiguration
				{
					CustomHomographNumbers = "a,b,c,d,e,f,g,h,i,j"
				}
			};
			var controller = new HeadwordNumbersController(view, model, Cache);
			// verify ok button enabled on setup with 10 numbers
			Assert.IsTrue(view.OkButtonEnabled, "Ok not enabled by controller constructor");
			view.OkButtonEnabled = false;
			// verify ok button enabled when event is triggered and there are 10 custom numbers
			view.TriggerCustomDigitsChanged();
			Assert.IsTrue(view.OkButtonEnabled, "Ok button not enabled when event is fired");
		}

		[Test]
		public void Ok_Disabled_WhenNotAllTenNumbersSet()
		{
			// Test enabled on initial setup
			var view = new TestHeadwordNumbersView();
			var model = new DictionaryConfigurationModel();
			var controller = new HeadwordNumbersController(view, model, Cache);
			view.OkButtonEnabled = true;
			view.CustomDigits = new List<string> { "1" };
			view.TriggerCustomDigitsChanged();
			Assert.IsFalse(view.OkButtonEnabled, "Ok button still enabled after event is fired");
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

		[Test]
		public void ConstructorSetsWritingSystemInView()
		{
			var view = new TestHeadwordNumbersView();
			var model = new DictionaryConfigurationModel { HomographConfiguration = new DictionaryHomographConfiguration { HomographWritingSystem = "fr"} };
			// ReSharper disable once UnusedVariable
			// SUT
			var testController = new HeadwordNumbersController(view, model, Cache);

			Assert.That(view.HomographWritingSystem, Is.StringContaining("French"),
				"The writing system in the view should match the model (but show the pretty name).");
		}

		[Test]
		public void ConstructorSetsDefaultWritingSystemInView()
		{
			var view = new TestHeadwordNumbersView();
			var model = new DictionaryConfigurationModel { HomographConfiguration = new DictionaryHomographConfiguration { HomographWritingSystem = "pt" } };
			// SUT
			var testController = new HeadwordNumbersController(view, model, Cache);

			Assert.That(view.HomographWritingSystem, Is.StringContaining("English"),
				"The default writing system 'English' should be in the view when given HomographWritingSystem is missing.");
		}

		[Test]
		public void ConstructorSetsCustomHeadwordNumbersInView()
		{
			var view = new TestHeadwordNumbersView();
			var model = new DictionaryConfigurationModel
			{
				HomographConfiguration = new DictionaryHomographConfiguration
				{
					CustomHomographNumbers = "a;b;c;d;e;f;g;h;i;j;k"
				}
			};
			// ReSharper disable once UnusedVariable
			// SUT
			var testController = new HeadwordNumbersController(view, model, Cache);
			CollectionAssert.AreEqual(model.HomographConfiguration.CustomHomographNumberList, view.CustomDigits);
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
			public string HomographWritingSystem { get; set; }

			public event EventHandler RunStylesDialog = delegate(object sender, EventArgs args) {  };
			public IEnumerable<IWritingSystem> AvailableWritingSystems { private get; set; }
			public IEnumerable<string> CustomDigits { get; set; }
			public event EventHandler CustomDigitsChanged;
			public bool OkButtonEnabled { get; set; }

			internal void TriggerCustomDigitsChanged()
			{
				CustomDigitsChanged(this, null);
			}
			public void SetWsFactoryForCustomDigits(ILgWritingSystemFactory factory) { }
		}
	}
}
