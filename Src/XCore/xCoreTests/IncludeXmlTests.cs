// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for IncludeXmlTests.
	/// </summary>
	[TestFixture]
	public class IncludeXmlTests
	{
		protected XmlIncluder m_includer;

		[OneTimeSetUp]
		public void FixtureInit()
		{
			SimpleResolver resolver = new SimpleResolver();

			string source = FwDirectoryFinder.SourceDirectory;
			string path = System.IO.Path.Combine(source, "XCore", "xCoreTests");
			if (!System.IO.Directory.Exists(path))
			{
				Debug.Fail(path + " not found.");
			}

			resolver.BaseDirectory = path;
			m_includer= new XmlIncluder (resolver);
		}

		[Test]
		public void ReplaceNode()
		{
			XmlDocument doc =  new XmlDocument();
			doc.LoadXml(@"<blah><include path='IncludeXmlTestSource.xml' query='food/fruit/name'/></blah>");

			Dictionary<string, XmlDocument> cachedDoms = new Dictionary<string, XmlDocument>();
			m_includer.ReplaceNode(cachedDoms, doc.SelectSingleNode("//include"));
			Assert.That(doc.SelectSingleNode("include"), Is.Null);
			Assert.That(doc.SelectNodes("blah/name").Count, Is.EqualTo(2));
		}

		[Test]
		public void ProcessDomExplicit()
		{
			XmlDocument doc =  new XmlDocument();
			doc.LoadXml(@"<blah><include path='IncludeXmlTestSource.xml' query='food/fruit/name'/></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.That(doc.SelectSingleNode("include"), Is.Null);
			Assert.That(doc.SelectNodes("blah/name").Count, Is.EqualTo(2));
		}

		/// <summary>
		/// This document contains nodes to do the inclusion.
		/// </summary>
		[Test]
		public void ExplicitThisDocInclusionBase()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(@"<blah><ack><include path='$this' query='blah/drinks'/></ack><drinks><soda><name txt='pepsi'/><name txt='coke'/></soda></drinks></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.That(doc.SelectSingleNode("//includeBase"), Is.Null, "the processor should remove the <includeBase/>");
			Assert.That(doc.SelectSingleNode("include"), Is.Null);
			Assert.That(doc.SelectNodes("blah/drinks/soda/name").Count, Is.EqualTo(2));//should be two sodas
		}

		/// <summary>
		/// This document contains nodes to do the inclusion.
		/// </summary>
		[Test]
		public void TwoLevelThisDocInclusion()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(@"<blah><ack><includeBase path='$this'/><include query='blah/drinks'/></ack><drinks><soda><name txt='pepsi'/><name txt='coke'/></soda></drinks></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.That(doc.SelectSingleNode("//includeBase"), Is.Null, "the processor should remove the <includeBase/>");
			Assert.That(doc.SelectSingleNode("include"), Is.Null);
			Assert.That(doc.SelectNodes("blah/drinks/soda/name").Count, Is.EqualTo(2));//should be two sodas
		}

		/// <summary>
		/// override
		/// </summary>
		[Test]
		public void InclusionOverrides()
		{
			XmlDocument doc = new XmlDocument();
			string docXml = @"<blah>" +
					"<include path='IncludeXmlTestSource.xml' query='food/meats'>" +
						"<overrides>" +
							// add new attribute, keep existing
							"<name txt='pork' animal='pig'/>" +
							// modify attribute, keep childnodes
							"<name txt='chicken' brand='Swanson'/>" +
							// replace entire node, adding new nodes :p
							"<name txt='beef'>" +
								"<portions><name txt='tongue'/></portions>" +
							"</name>" +
						"</overrides>" +
					"</include></blah>";
			doc.LoadXml(docXml);

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.That(doc.SelectSingleNode("//includeBase"), Is.Null, "the processor should remove the <includeBase/>");
			Assert.That(doc.SelectSingleNode("include"), Is.Null);
			Assert.That(doc.SelectSingleNode("overrides"), Is.Null);
			Assert.That(doc.SelectNodes("blah/meats/name").Count, Is.EqualTo(3));
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='pork']").Attributes.Count, Is.EqualTo(3));
			// make sure existing attribute didn't change
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='pork']").Attributes["brand"].Value, Is.EqualTo("PilgrimsPride"));
			// ensure new attribute was created
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='pork']").Attributes["animal"].Value, Is.EqualTo("pig"));
			// ensure attribute was modified.
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='chicken']").Attributes.Count, Is.EqualTo(2));
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='chicken']").Attributes["brand"].Value, Is.EqualTo("Swanson"));
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='chicken']").ChildNodes.Count, Is.EqualTo(1));
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='chicken']").ChildNodes[0].ChildNodes.Count, Is.EqualTo(2));
			// ensure entire node was replaced
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='beef']").Attributes.Count, Is.EqualTo(1));
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='beef']").ChildNodes.Count, Is.EqualTo(1));
			Assert.That(doc.SelectSingleNode("blah/meats/name[@txt='beef']").ChildNodes[0].ChildNodes.Count, Is.EqualTo(1));
		}

		[Test]
		public void TwoLevelInclusion()
		{
			XmlDocument doc =  new XmlDocument();
			//this document itself does not include any veggies element... but it tries to include one that does.
			doc.LoadXml(@"<blah><includeBase path='IncludeXmlTestSource.xml'/><include query='food/veggies'/></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.That(doc.SelectSingleNode("//includeBase"), Is.Null, "the processor should remove the <includeBase/>");
			Assert.That(doc.SelectSingleNode("include"), Is.Null);
			Assert.That(doc.SelectNodes("blah/veggies/name").Count, Is.EqualTo(2));//should be two vegetables
		}

		[Test]
		public void ThreeLevelInclusionWithRelativeDirectory()
		{
			XmlDocument doc =  new XmlDocument();
			//this document itself does not include any veggies element... but it tries to include one that does.
			doc.LoadXml(@"<blah><includeBase path='IncludeXmlTestSourceB.xml'/><include query='food/veggies'/></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.That(doc.SelectSingleNode("//includeBase"), Is.Null, "the processor should remove the <includeBase/>");
			Assert.That(doc.SelectSingleNode("include"), Is.Null);
			Assert.That(doc.SelectNodes("blah/veggies/thing").Count, Is.EqualTo(2));//should be tomato and cooking banana
		}
	}
}
