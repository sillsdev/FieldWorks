// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryConfigurationManagerControllerTests : MemoryOnlyBackendProviderTestBase
	{
		private DictionaryConfigurationManagerController _controller;

		[SetUp]
		public void Setup()
		{
			var configurations = new List<DictionaryConfigurationModel>
			{
				new DictionaryConfigurationModel { Label = "configuration0", Publications = new List<string>() },
				new DictionaryConfigurationModel { Label = "configuration1", Publications = new List<string>() }
			};

			var publications = new List<string>
			{
				"publicationA",
				"publicationB"
			};

			_controller = new DictionaryConfigurationManagerController()
			{
				Configurations = configurations,
				Publications = publications,
			};
		}

		[TearDown]
		public void TearDown()
		{

		}

		[Test]
		public void GetPublication_UsesAssociations()
		{
			_controller.Configurations[0].Publications.Add("publicationA");
			// SUT
			var pubs = _controller.GetPublications(_controller.Configurations[0]);
			Assert.That(pubs, Contains.Item("publicationA"));
			Assert.That(pubs, Has.Count.EqualTo(1));

			// SUT
			Assert.Throws<ArgumentNullException>(() => _controller.GetPublications(null));
		}

		[Test]
		public void AssociatePublication_BadArgsTests()
		{
			Assert.Throws<ArgumentNullException>(() => _controller.AssociatePublication(null, null), "No configuration to associate with");
			Assert.Throws<ArgumentNullException>(() => _controller.AssociatePublication("publicationA", null), "No configuration to associate with");
			Assert.Throws<ArgumentNullException>(() => _controller.AssociatePublication(null, _controller.Configurations[0]), "Don't allow trying to add null");

			Assert.Throws<ArgumentOutOfRangeException>(() => _controller.AssociatePublication("unknown publication", _controller.Configurations[0]), "Don't associate with an invalid/unknown publication");
		}

		[Test]
		public void AssociatesPublication()
		{
			// SUT
			_controller.AssociatePublication("publicationA", _controller.Configurations[0]);
			Assert.That(_controller.Configurations[0].Publications, Contains.Item("publicationA"), "failed to associate");
			Assert.That(_controller.Configurations[0].Publications, Is.Not.Contains("publicationB"), "should not have associated with publicationB");

			// SUT
			_controller.AssociatePublication("publicationA", _controller.Configurations[1]);
			_controller.AssociatePublication("publicationB", _controller.Configurations[1]);
			Assert.That(_controller.Configurations[1].Publications, Contains.Item("publicationA"), "failed to associate");
			Assert.That(_controller.Configurations[1].Publications, Contains.Item("publicationB"), "failed to associate");
		}

		[Test]
		public void DisassociatePublication_BadArgsTests()
		{
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication(null, null), "No configuration to disassociate. No publication to disassociate from.");
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication("publicationA", null), "No configuration");
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication(null, _controller.Configurations[0]), "No publication");

			Assert.Throws<ArgumentOutOfRangeException>(() => _controller.DisassociatePublication("unknown publication", _controller.Configurations[0]), "Don't try to operate using an invalid/unknown publication");
		}

		[Test]
		public void DisassociatesPublication()
		{
			_controller.AssociatePublication("publicationA", _controller.Configurations[1]);
			_controller.AssociatePublication("publicationB", _controller.Configurations[1]);
			// SUT
			_controller.DisassociatePublication("publicationA", _controller.Configurations[1]);

			Assert.That(_controller.Configurations[1].Publications, Contains.Item("publicationB"), "Should not have disassociated unrelated publication");
			Assert.That(_controller.Configurations[1].Publications, Is.Not.Contains("publicationA"), "failed to disassociate");
		}
	}
}
