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
// File: StringTableTests.cs
// Authorship History: John Hatton
// --------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using System.Xml;
using System.Collections.Generic;
using SIL.Utils;
using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;

namespace XCore
{
	/// <summary>
	/// Summary description for IncludeXmlTests.
	/// </summary>
	[TestFixture]
	public class IncludeXmlTests : TestBaseForTestsThatCreateTempFilesBasedOnResources
	{
		protected XmlIncluder m_includer;

		[TestFixtureSetUp]
		public void FixtureInit()
		{
			SimpleResolver resolver = new SimpleResolver();

			string source = DirectoryFinder.FwSourceDirectory;
			string path = System.IO.Path.Combine(source, @"Utilities/XMLUtils/XMLUtilsTests");
			if (!System.IO.Directory.Exists(path))
			{
				Debug.Fail(path + " not found.  Have the XmlUtilsTests been moved?");
			}

			resolver.BaseDirectory = path;
			m_includer= new XmlIncluder (resolver);
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ReplaceNode()
		{
			XmlDocument doc =  new XmlDocument();
			doc.LoadXml(@"<blah><include path='IncludeXmlTestSource.xml' query='food/fruit/name'/></blah>");

			Dictionary<string, XmlDocument> cachedDoms = new Dictionary<string, XmlDocument>();
			m_includer.ReplaceNode(cachedDoms, doc.SelectSingleNode("//include"));
			Assert.IsNull(doc.SelectSingleNode("include"));
			Assert.AreEqual(2, doc.SelectNodes("blah/name").Count);
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ProcessDomExplicit()
		{
			XmlDocument doc =  new XmlDocument();
			doc.LoadXml(@"<blah><include path='IncludeXmlTestSource.xml' query='food/fruit/name'/></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.IsNull(doc.SelectSingleNode("include"));
			Assert.AreEqual(2, doc.SelectNodes("blah/name").Count);
		}

		/// <summary>
		/// This document contains nodes to do the inclusion.
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ExplicitThisDocInclusionBase()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(@"<blah><ack><include path='$this' query='blah/drinks'/></ack><drinks><soda><name txt='pepsi'/><name txt='coke'/></soda></drinks></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.IsNull(doc.SelectSingleNode("//includeBase"), "the processor should remove the <includeBase/>");
			Assert.IsNull(doc.SelectSingleNode("include"));
			Assert.AreEqual(2, doc.SelectNodes("blah/drinks/soda/name").Count);//should be two sodas
		}

		/// <summary>
		/// This document contains nodes to do the inclusion.
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void TwoLevelThisDocInclusion()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(@"<blah><ack><includeBase path='$this'/><include query='blah/drinks'/></ack><drinks><soda><name txt='pepsi'/><name txt='coke'/></soda></drinks></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.IsNull(doc.SelectSingleNode("//includeBase"), "the processor should remove the <includeBase/>");
			Assert.IsNull(doc.SelectSingleNode("include"));
			Assert.AreEqual(2, doc.SelectNodes("blah/drinks/soda/name").Count);//should be two sodas
		}

		/// <summary>
		/// override
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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
			Assert.IsNull(doc.SelectSingleNode("//includeBase"), "the processor should remove the <includeBase/>");
			Assert.IsNull(doc.SelectSingleNode("include"));
			Assert.IsNull(doc.SelectSingleNode("overrides"));
			Assert.AreEqual(3, doc.SelectNodes("blah/meats/name").Count);
			Assert.AreEqual(3, doc.SelectSingleNode("blah/meats/name[@txt='pork']").Attributes.Count);
			// make sure existing attribute didn't change
			Assert.AreEqual("PilgrimsPride", doc.SelectSingleNode("blah/meats/name[@txt='pork']").Attributes["brand"].Value);
			// ensure new attribute was created
			Assert.AreEqual("pig", doc.SelectSingleNode("blah/meats/name[@txt='pork']").Attributes["animal"].Value);
			// ensure attribute was modified.
			Assert.AreEqual(2, doc.SelectSingleNode("blah/meats/name[@txt='chicken']").Attributes.Count);
			Assert.AreEqual("Swanson", doc.SelectSingleNode("blah/meats/name[@txt='chicken']").Attributes["brand"].Value);
			Assert.AreEqual(1, doc.SelectSingleNode("blah/meats/name[@txt='chicken']").ChildNodes.Count);
			Assert.AreEqual(2, doc.SelectSingleNode("blah/meats/name[@txt='chicken']").ChildNodes[0].ChildNodes.Count);
			// ensure entire node was replaced
			Assert.AreEqual(1, doc.SelectSingleNode("blah/meats/name[@txt='beef']").Attributes.Count);
			Assert.AreEqual(1, doc.SelectSingleNode("blah/meats/name[@txt='beef']").ChildNodes.Count);
			Assert.AreEqual(1, doc.SelectSingleNode("blah/meats/name[@txt='beef']").ChildNodes[0].ChildNodes.Count);
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void TwoLevelInclusion()
		{
			XmlDocument doc =  new XmlDocument();
			//this document itself does not include any veggies element... but it tries to include one that does.
			doc.LoadXml(@"<blah><includeBase path='IncludeXmlTestSource.xml'/><include query='food/veggies'/></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.IsNull(doc.SelectSingleNode("//includeBase"), "the processor should remove the <includeBase/>");
			Assert.IsNull(doc.SelectSingleNode("include"));
			Assert.AreEqual(2, doc.SelectNodes("blah/veggies/name").Count);//should be two vegetables
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void ThreeLevelInclusionWithRelativeDirectory()
		{
			XmlDocument doc =  new XmlDocument();
			//this document itself does not include any veggies element... but it tries to include one that does.
			doc.LoadXml(@"<blah><includeBase path='IncludeXmlTestSourceB.xml'/><include query='food/veggies'/></blah>");

			m_includer.ProcessDom("TestMainFile", doc);
			Assert.IsNull(doc.SelectSingleNode("//includeBase"), "the processor should remove the <includeBase/>");
			Assert.IsNull(doc.SelectSingleNode("include"));
			Assert.AreEqual(2, doc.SelectNodes("blah/veggies/thing").Count);//should be tomato and cooking banana
		}
	}
}
