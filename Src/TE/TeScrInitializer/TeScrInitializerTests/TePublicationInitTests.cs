// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2004' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TePublicationInitTests.cs
// Responsibility: TE team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.PrintLayout;

namespace SIL.FieldWorks.TE
{
	#region TestTePublicationsInit class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TestTePublicationsInit class exposes aspects of <see cref="TePublicationsInit"/> class
	/// for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class TestTePublicationsInit : TePublicationsInit
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init the base class TePublicationsInit for testing.
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		internal TestTePublicationsInit(Scripture scr)
			: base(scr)
		{
			m_fUnderTest = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the TePublicationsInit.CreateFactoryPublicationInfo method
		/// </summary>
		/// <param name="doc">The XML document containing publications and HF sets</param>
		/// ------------------------------------------------------------------------------------
		internal void CallCreatePublicationInfo(XmlDocument doc)
		{
			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				progressDlg.RunTask(false, new BackgroundTaskInvoker(CreateFactoryPublicationInfo),
					doc.SelectSingleNode("PublicationDefaultsForNewProject"));
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the TePublicationsInit.CreateHfSets method to create a HeaderFooterSet for each
		/// HeaderFooterSet node in the given xml node list.
		/// </summary>
		/// <param name="hfSetNodes">the xml nodes to read</param>
		/// -------------------------------------------------------------------------------------
		internal void CallCreateHfSets(XmlNodeList hfSetNodes)
		{
			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				base.CreateHfSets(progressDlg, hfSetNodes);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the TePublicationsInit.GetPubPageSizes method
		/// </summary>
		/// <param name="doc">The XML document with the factory publication info.</param>
		/// <param name="publicationTypeName">Name of the type of publication whose supported
		/// sizes are to be retrieved.</param>
		/// <param name="icuLocale">The UI icu locale.</param>
		/// <returns>A list of supported publication page sizes for the given type of
		/// publication.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal List<PubPageInfo> CallGetPubPageSizes(XmlDocument doc,
			string publicationTypeName, string icuLocale)
		{
			return GetPubPageSizes(doc.SelectSingleNode("PublicationDefaultsForNewProject"),
				publicationTypeName, icuLocale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TePublicationsInit.GetUnitConversion"/> method
		/// </summary>
		/// <param name="attributes">The attributes.</param>
		/// <param name="attrName">Name of the attr.</param>
		/// <param name="defaultVal">The default val.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal double CallGetUnitConversion(XmlAttributeCollection attributes, string attrName,
			double defaultVal)
		{
			return GetUnitConversion(attributes, attrName, defaultVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TePublicationsInit.GetString"/> method
		/// </summary>
		/// <param name="attributes"></param>
		/// <param name="attrName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public new string GetString(XmlAttributeCollection attributes, string attrName)
		{
			return TePublicationsInit.GetString(attributes, attrName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TePublicationsInit.GetInt"/> method
		/// </summary>
		/// <param name="attributes">The attributes.</param>
		/// <param name="attrName">Name of the attribute.</param>
		/// <param name="defaultVal">The default value.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public new int GetInt(XmlAttributeCollection attributes, string attrName,
			int defaultVal)
		{
			return TePublicationsInit.GetInt(attributes, attrName, defaultVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TePublicationsInit.GetBoolean"/> method
		/// </summary>
		/// <param name="attributes"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultVal"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal new bool GetBoolean(XmlAttributeCollection attributes, string attrName,
			bool defaultVal)
		{
		   return base.GetBoolean(attributes, attrName, defaultVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TePublicationsInit.GetGutterLoc"/> method
		/// </summary>
		/// <param name="attributes">The collection of attributes.</param>
		/// <param name="attrName">Name of the attribute containing the gutter location</param>
		/// <param name="defaultVal">The default val.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal new BindingSide GetGutterLoc(XmlAttributeCollection attributes,
			string attrName, BindingSide defaultVal)
		{
			return base.GetGutterLoc(attributes, attrName, defaultVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TePublicationsInit.GetDivisionStart"/> method
		/// </summary>
		/// <param name="attributes"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultVal"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal new DivisionStartOption GetDivisionStart(XmlAttributeCollection attributes,
			string attrName, DivisionStartOption defaultVal)
		{
			return base.GetDivisionStart(attributes, attrName, defaultVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TePublicationsInit.GetWritingSystem"/> method
		/// </summary>
		/// <param name="attributes"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultVal"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal new int GetWritingSystem(XmlAttributeCollection attributes,
			string attrName, int defaultVal)
		{
			return base.GetWritingSystem(attributes, attrName, defaultVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TePublicationsInit.ReadHfElements"/> method
		/// </summary>
		/// <param name="hfElementNodes">the list of header/footer element nodes</param>
		/// <returns>the resulting TsString</returns>
		/// ------------------------------------------------------------------------------------
		internal ITsString CallReadHfElements(XmlNodeList hfElementNodes)
		{
			return ReadHfElements(hfElementNodes);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a GUID based on the version attribute node. For testing purposes, if the
		/// node doesn't have a version number, just return a new GUID.
		/// </summary>
		/// <param name="baseNode">The base node (by default, this is the node directly
		/// containing the version node, but subclasses can interpret this differently).</param>
		/// <returns>A GUID based on the version attribute node</returns>
		/// ------------------------------------------------------------------------------------
		protected override Guid GetVersion(XmlNode baseNode)
		{
			try
			{
				return new Guid(baseNode.Attributes.GetNamedItem("version").Value);
			}
			catch
			{
				return Guid.NewGuid();
			}
		}
	}
	#endregion

	#region TePublicationInitTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TePublicationInitTests is a collection of tests for static methods of the
	/// <see cref="TePublicationsInit"/> class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TePublicationInitTests : ScrInMemoryFdoTestBase
	{
		private TestTePublicationsInit m_pubInitializer;

		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Connect to TestLangProj and start an undo task;
		/// init the base class we are testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_pubInitializer = new TestTePublicationsInit(m_scr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_pubInitializer = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			base.InitializeCache();
			m_scrInMemoryCache.InitializeWritingSystemEncodings();
		}
		#endregion

		#region test reading of XML attributes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test TescrInitializer.GetUnitConversion method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetUnitConversionTest()
		{
			CheckDisposed();

			// get an empty collection of xml attributes
			XmlDocument dummyXmlDoc = new XmlDocument();
			dummyXmlDoc.LoadXml("<dummy/>");
			XmlAttributeCollection attributes = dummyXmlDoc.FirstChild.Attributes;


			//Create a new attribute, and set it in our attribute collection
			XmlNode measAttrib = dummyXmlDoc.CreateNode(XmlNodeType.Attribute, "MeasurementUnits",
				string.Empty);
			measAttrib.Value = "inch"; // inch, cm
			attributes.SetNamedItem(measAttrib);

			// test GetUnitConversion
			Assert.AreEqual(TePublicationsInit.kMpPerInch, (int)m_pubInitializer.CallGetUnitConversion(attributes,
				"MeasurementUnits", 0));

			//change the attribute.
			measAttrib.Value = "cm"; // inch, cm
			attributes.SetNamedItem(measAttrib);

			// test GetUnitConversion
			Assert.AreEqual(TePublicationsInit.kMpPerCm, m_pubInitializer.CallGetUnitConversion(attributes,
				"MeasurementUnits", 0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetUnitConversion"/> method for invalid data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(Exception))]
		public void GetUnitConversionBrokenTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-junk=\"junk\"/>");
			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;

			// attribute has invalid value; exception should be thrown
			m_pubInitializer.CallGetUnitConversion(attribs, "test-junk", 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetString"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStringAttributeTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-remy=\"Remy\" test-bryan=\"bryan\" "+
				"test-empty=\"\" />");

			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;
			// get existing attributes
			Assert.AreEqual("Remy", TestTePublicationsInit.GetString(attribs, "test-remy"));
			Assert.AreEqual("bryan", TestTePublicationsInit.GetString(attribs, "test-bryan"));

			//attribute has empty value
			Assert.AreEqual(string.Empty, TestTePublicationsInit.GetString(attribs, "test-empty"));

			// attribute does not exist; null should be returned
			Assert.AreEqual(null, TestTePublicationsInit.GetString(attribs, "missing"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetInt"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetIntAttributeTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-100=\"100\" test-1=\"1\" " +
				"test-empty=\"\" />");

			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;
			// get existing attributes
			Assert.AreEqual(1, TestTePublicationsInit.GetInt(attribs, "test-1", -3));
			Assert.AreEqual(100, TestTePublicationsInit.GetInt(attribs, "test-100", -3));

			//attribute has empty value
			Assert.AreEqual(-3, TestTePublicationsInit.GetInt(attribs, "test-empty", -3));

			// attribute does not exist; default should be returned
			Assert.AreEqual(-3, TestTePublicationsInit.GetInt(attribs, "missing", -3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetInt"/> method for invalid data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(Exception))]
		public void GetIntAttributeBrokenTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-junk=\"junk\"/>");
			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;

			// attribute has invalid value; exception should be thrown
			TestTePublicationsInit.GetInt(attribs, "test-junk", -3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetBoolean"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBooleanAttributeTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-true=\"true\" test-true2=\"True\" "+
				"test-false=\"false\" test-false2=\"False\" "+
				"test-empty=\"\" />");

			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;
			// get existing attributes
			Assert.IsTrue(m_pubInitializer.GetBoolean(attribs, "test-true", false));
			Assert.IsTrue(m_pubInitializer.GetBoolean(attribs, "test-true2", false));
			Assert.IsFalse(m_pubInitializer.GetBoolean(attribs, "test-false", true));
			Assert.IsFalse(m_pubInitializer.GetBoolean(attribs, "test-false2", true));

			// attribute is empty; default should be returned
			Assert.IsTrue(m_pubInitializer.GetBoolean(attribs, "test-empty", true));
			Assert.IsFalse(m_pubInitializer.GetBoolean(attribs, "test-empty", false));

			// attribute does not exist; default should be returned
			Assert.IsTrue(m_pubInitializer.GetBoolean(attribs, "missing", true));
			Assert.IsFalse(m_pubInitializer.GetBoolean(attribs, "missing", false));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetBoolean"/> method for invalid data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(Exception))]
		public void GetBooleanAttributeBrokenTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-junk=\"junk\"/>");
			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;

			// attribute has invalid value; exception should be thrown
			m_pubInitializer.GetBoolean(attribs, "test-junk", true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetGutterLoc"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetGutterLocTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-left=\"Left\" test-right=\"RIGHT\" "+
				"test-top=\"toP\" "+
				"test-empty=\"\" />");

			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;
			// get existing attributes
			Assert.AreEqual(BindingSide.Left,
				m_pubInitializer.GetGutterLoc(attribs, "test-left", BindingSide.Top));
			Assert.AreEqual(BindingSide.Right,
				m_pubInitializer.GetGutterLoc(attribs, "test-right", BindingSide.Left));
			Assert.AreEqual(BindingSide.Top,
				m_pubInitializer.GetGutterLoc(attribs, "test-top", BindingSide.Left));

			// attribute is empty; default should be returned
			Assert.AreEqual(BindingSide.Top,
				m_pubInitializer.GetGutterLoc(attribs, "test-empty", BindingSide.Top));
			Assert.AreEqual(BindingSide.Left,
				m_pubInitializer.GetGutterLoc(attribs, "test-empty", BindingSide.Left));

			// attribute does not exist; default should be returned
			Assert.AreEqual(BindingSide.Top,
				m_pubInitializer.GetGutterLoc(attribs, "missing", BindingSide.Top));
			Assert.AreEqual(BindingSide.Right,
				m_pubInitializer.GetGutterLoc(attribs, "missing", BindingSide.Right));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetGutterLoc"/> method for invalid data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(Exception))]
		public void GetGutterLocBrokenTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-junk=\"junk\"/>");
			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;

			// attribute has invalid value; exception should be thrown
			m_pubInitializer.GetGutterLoc(attribs, "test-junk", BindingSide.Top);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetDivisionStart"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDivisionStartTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-continuous=\"Continuous\" test-new=\"NEWpage\" " +
				"test-odd=\"oDDpagE\" " +
				"test-empty=\"\" />");

			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;
			// get existing attributes
			Assert.AreEqual(DivisionStartOption.Continuous,
				m_pubInitializer.GetDivisionStart(attribs, "test-continuous", DivisionStartOption.NewPage));
			Assert.AreEqual(DivisionStartOption.NewPage,
				m_pubInitializer.GetDivisionStart(attribs, "test-new", DivisionStartOption.Continuous));
			Assert.AreEqual(DivisionStartOption.OddPage,
				m_pubInitializer.GetDivisionStart(attribs, "test-odd", DivisionStartOption.Continuous));

			// attribute is empty; default should be returned
			Assert.AreEqual(DivisionStartOption.Continuous,
				m_pubInitializer.GetDivisionStart(attribs, "test-empty", DivisionStartOption.Continuous));
			Assert.AreEqual(DivisionStartOption.NewPage,
				m_pubInitializer.GetDivisionStart(attribs, "test-empty", DivisionStartOption.NewPage));

			// attribute does not exist; default should be returned
			Assert.AreEqual(DivisionStartOption.Continuous,
				m_pubInitializer.GetDivisionStart(attribs, "missing", DivisionStartOption.Continuous));
			Assert.AreEqual(DivisionStartOption.OddPage,
				m_pubInitializer.GetDivisionStart(attribs, "missing", DivisionStartOption.OddPage));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetDivisionStart"/> method for invalid data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(Exception))]
		public void GetDivisionStartBrokenTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-junk=\"junk\"/>");
			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;

			// attribute has invalid value; exception should be thrown
			m_pubInitializer.GetDivisionStart(attribs, "test-junk", DivisionStartOption.NewPage);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TePublicationsInit.GetWritingSystem"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWritingSystemTest()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<mytag test-english=\"en\" test-kalaba=\"fr\" " +
				"test-empty=\"\" " +
				"test-junk=\"junk\" />");

			XmlNode tag = doc.SelectSingleNode("mytag");
			XmlAttributeCollection attribs = tag.Attributes;
			int wsDefault = 2;

			// get existing attributes
			Assert.AreEqual(Cache.DefaultAnalWs, //TestLangProj DefaultAnalWs is english
				m_pubInitializer.GetWritingSystem(attribs, "test-english", wsDefault));
			Assert.AreEqual(Cache.DefaultVernWs, //TestLangProj DefaultVernWs is french
				m_pubInitializer.GetWritingSystem(attribs, "test-kalaba", wsDefault));

			// attribute is empty; default should be returned
			Assert.AreEqual(wsDefault,
				m_pubInitializer.GetWritingSystem(attribs, "test-empty", wsDefault));

			// attribute does not exist; default should be returned
			Assert.AreEqual(wsDefault,
				m_pubInitializer.GetWritingSystem(attribs, "missing", wsDefault));

			// attribute is invalid; default should be returned
			Assert.AreEqual(wsDefault,
				m_pubInitializer.GetWritingSystem(attribs, "test-junk", wsDefault));
		}

		#endregion

		#region CreatePublicationInfo tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublications method where everything is explicit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationInfo_Inches_ExplicitValues()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with one publication and two
			// Header/Footer sets
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject date=\"2007-08-20\" " +
					"DTDver=\"9E835785-F508-4b58-844B-02A3D6EC3579\" " +
					"label=\"Scripture\" version=\"3BED2E67-781F-4e46-93E8-AE81A454668D\">" +

				"<Publications>" +

				"<Publication id=\"Default_Scripture\" Name=\"Test\" Description=\"Dummy\" " +
				"MeasurementUnits=\"inch\" " +
				"IsLandscape=\"true\" PageSize=\"full\" " +
				"GutterMargin=\"0.2\" BindingSide=\"right\" " +
				"BaseCharSize=\"13\" BaseLineSize=\"15\" " +
				"SheetLayout=\"duplex\"> " +

				"<SupportedPublicationSizes>" +
				"<PublicationPageSize id=\"full\">"  +
					"<Name iculocale=\"en\">Full Page</Name>"  +
				"</PublicationPageSize>"  +
				"<PublicationPageSize id=\"IPUBLargerBible\" Height=\"8.7\" Width=\"5.8\">"  +
					"<Name iculocale=\"en\">5.8 x 8.7in</Name>"  +
				"</PublicationPageSize>"  +
				"<PublicationPageSize id=\"IPUBSmallerBible\" Height=\"8.25\" Width=\"5.25\">"  +
					"<Name iculocale=\"en\">5.25 x 8.25in</Name>"  +
				"</PublicationPageSize>"  +
				"</SupportedPublicationSizes>" +

				"<Divisions>" +
				"<Division id=\"Scripture\" StartAt=\"oddPage\" HeaderFooterSetRef=\"TestLit\">" +
				"<PageLayout OriginalPageLayoutName=\"Default Scripture\" " +
				"MarginTop=\"1.1\" MarginBottom=\"1.2\"	MarginInside=\".8\" " +
				"MarginOutside=\".9\" PositionHeader=\"0.85\" PositionFooter=\"0.65\"/>" +
				"</Division>" +
				"</Divisions>" +
				"</Publication>" +
				"</Publications>" +

				"<HeaderFooterSets>" +
				"<HeaderFooterSet id=\"TestLit\" Name=\"Test Literals\" Description=\"Test Description\">" +
				"<DefaultHeader>" +
				"<Outside>" +
				"<Element ws=\"en\">Right top</Element>" +
				"</Outside>" +
				"<Center>" +
				"<Element ws=\"en\">Center top</Element>" +
				"</Center>" +
				"<Inside>" +
				"<Element ws=\"en\">Left top</Element>" +
				"</Inside>" +
				"</DefaultHeader>" +

				"<DefaultFooter>" +
				"<Outside>" +
				"<Element ws=\"en\">Right bottom</Element>" +
				"</Outside>" +
				"<Center>" +
				"<Element ws=\"en\">Center bottom</Element>" +
				"</Center>" +
				"<Inside>" +
				"<Element ws=\"en\">Left bottom</Element>" +
				"</Inside>" +
				"</DefaultFooter>" +

				"<FirstHeader>" +
				"<Outside>" +
				"<Element ws=\"en\">Right top1</Element>" +
				"</Outside>" +
				//				"<Center>" +
				//				"<Element ws=\"en\">Center top1</Element>" +
				//				"</Center>" +
				"<Inside>" +
				"<Element ws=\"en\">Left top1</Element>" +
				"</Inside>" +
				"</FirstHeader>" +

				"<FirstFooter>" +
				"<Outside>" +
				"<Element ws=\"en\">Right bottom1</Element>" +
				"</Outside>" +
				"<Center>" +
				"<Element ws=\"en\">Center bottom1</Element>" +
				"</Center>" +
				"<Inside>" +
				"<Element ws=\"en\">Left bottom1</Element>" +
				"</Inside>" +
				"</FirstFooter>" +

				"<EvenHeader>" +
				"<Outside>" +
				"<Element ws=\"en\">Left top E</Element>" +
				"</Outside>" +
				"<Center>" +
				"<Element ws=\"en\">Center top E</Element>" +
				"</Center>" +
				"<Inside>" +
				"<Element ws=\"en\">Right top E</Element>" +
				"</Inside>" +
				"</EvenHeader>" +

				"<EvenFooter>" +
				"<Outside>" +
				"<Element ws=\"en\">Left bottom E</Element>" +
				"</Outside>" +
				"<Center>" +
				"<Element ws=\"en\">Center bottom E</Element>" +
				"</Center>" +
				"<Inside>" +
				"<Element ws=\"en\">Right bottom E</Element>" +
				"</Inside>" +
				"</EvenFooter>" +

				"</HeaderFooterSet>" +
				"</HeaderFooterSets>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublicationInfo()
			int cExistingHfSets = m_scr.HeaderFooterSetsOC.Count;
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify publication
			Assert.AreEqual(1, m_scr.PublicationsOC.Count, "Wrong number of publications was created");
			FdoOwningCollection<IPublication> oc = m_scr.PublicationsOC;
			IPublication pub = Publication.CreateFromDBObject(Cache, oc.HvoArray[0]);
			Assert.AreEqual("Test", pub.Name);
			Assert.AreEqual("Dummy", pub.Description.Text);
			Assert.IsTrue(pub.IsLandscape);
			Assert.AreEqual(0, pub.PageHeight, "Full-page is stored in DB as 0");
			Assert.AreEqual(0, pub.PageWidth, "Full-page is stored in DB as 0");
			Assert.AreEqual((int)(0.2 * TePublicationsInit.kMpPerInch), pub.GutterMargin);
			Assert.AreEqual(0, pub.PaperHeight, "Paper Height will be based on default paper size for default printer");
			Assert.AreEqual(0, pub.PaperWidth, "Paper Height will be based on default paper size for default printer");
			Assert.AreEqual(BindingSide.Right, pub.BindingEdge);
			Assert.AreEqual(13000, pub.BaseFontSize);
			Assert.AreEqual(-15000, pub.BaseLineSpacing);
			Assert.AreEqual(MultiPageLayout.Duplex, pub.SheetLayout);

			//verify pub's division
			Assert.AreEqual(1, pub.DivisionsOS.Count, "Publication should have 1 division.");
			IPubDivision division = pub.DivisionsOS[0];
			Assert.AreEqual(DivisionStartOption.OddPage, division.StartAt);

			//verify division's pagelayout
			Assert.AreEqual("Default Scripture", division.PageLayoutOA.Name);
			Assert.AreEqual((int)(1.1 * TePublicationsInit.kMpPerInch), division.PageLayoutOA.MarginTop);
			Assert.AreEqual((int)(1.2 * TePublicationsInit.kMpPerInch), division.PageLayoutOA.MarginBottom);
			Assert.AreEqual((int)(0.8 * TePublicationsInit.kMpPerInch), division.PageLayoutOA.MarginInside);
			Assert.AreEqual((int)(0.9 * TePublicationsInit.kMpPerInch), division.PageLayoutOA.MarginOutside);
			Assert.AreEqual((int)(0.85 * TePublicationsInit.kMpPerInch), division.PageLayoutOA.PosHeader);
			Assert.AreEqual((int)(0.65 * TePublicationsInit.kMpPerInch), division.PageLayoutOA.PosFooter);

			//verify division's HeaderFooterSet
			IPubHFSet hfSet = division.HFSetOA;
			VerifyHeaderFooterSetTestLiterals(hfSet);

			//verify division's header/footer bools
			Assert.IsTrue(division.DifferentFirstHF, "DifferentFirstHF should be True.");
			Assert.IsTrue(division.DifferentEvenHF, "DifferentEvenHF should be True.");

			//verify scripture's HeaderFooterSets
			Assert.AreEqual(cExistingHfSets + 1, m_scr.HeaderFooterSetsOC.Count,
				"Wrong number of HeaderFooterSets was created");

			IPubHFSet hfSet2 = m_scr.FindHeaderFooterSetByName("Test Literals");
			Assert.IsNotNull(hfSet2, "Didn't find the newly created HF set");
			VerifyHeaderFooterSetTestLiterals(hfSet2);

			// verify TePublication Version
			CmResource pubResource = CmResource.GetResource(m_inMemoryCache.Cache, m_scr.Hvo,
				(int)Scripture.ScriptureTags.kflidResources, "TePublications");
			Assert.IsNotNull(pubResource, "Resource for TePublications was not found.");
			Assert.AreEqual(new Guid("3BED2E67-781F-4e46-93E8-AE81A454668D"), pubResource.Version);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublications method where everything is explicit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationInfo_ImplicitValues()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with one publication and one
			// Header/Footer set
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject date=\"2007-08-20\" " +
					"DTDver=\"9E835785-F508-4b58-844B-02A3D6EC3579\" " +
					"label=\"Scripture\" version=\"3BEC2E67-781F-4e46-73E8-AE81A456668D\">" +

				"<Publications>" +
				"<Publication id=\"whatever\" Name=\"Good Publication\"> " +

				"<SupportedPublicationSizes>" +
				"<PublicationPageSize id=\"full\">" +
					"<Name iculocale=\"en\">Full Page</Name>" +
				"</PublicationPageSize>" +
				"</SupportedPublicationSizes>" +

				"<Divisions>" +
				"<Division/>" +
				"<PageLayout/>" +
				"<HeaderFooterSet/>" +
				"</Divisions>" +
				"</Publication> " +
				"</Publications>" +
				"<HeaderFooterSets>" +
				"<HeaderFooterSet/>" +
				"</HeaderFooterSets>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublicationInfo()
			int cExistingHfSets = m_scr.HeaderFooterSetsOC.Count;
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify publication
			Assert.AreEqual(1, m_scr.PublicationsOC.Count, "Wrong number of publications was created");
			FdoOwningCollection<IPublication> oc = m_scr.PublicationsOC;
			IPublication pub = Publication.CreateFromDBObject(Cache, oc.HvoArray[0]);
			Assert.AreEqual("Good Publication", pub.Name);
			Assert.IsNull(pub.Description.Text);
			Assert.IsFalse(pub.IsLandscape);
			Assert.AreEqual(0, pub.PageHeight, "Full page (0) is the implied default");
			Assert.AreEqual(0, pub.PageWidth, "Full page (0) is the implied default");
			Assert.AreEqual(0, pub.GutterMargin, "No gutter (0) is the implied default");
			Assert.AreEqual(0, pub.PaperHeight, "Paper Height will be based on default paper size for default printer");
			Assert.AreEqual(0, pub.PaperWidth, "Paper Height will be based on default paper size for default printer");
			Assert.AreEqual(BindingSide.Left, pub.BindingEdge);
			Assert.AreEqual(0, pub.BaseFontSize, "Based on Normal (0) is the implied default");
			Assert.AreEqual(0, pub.BaseLineSpacing, "Based on Normal (0) is the implied default");

			//verify pub's division
			Assert.AreEqual(1, pub.DivisionsOS.Count, "Publication should have 1 division.");
			IPubDivision division = pub.DivisionsOS[0];
			Assert.AreEqual(DivisionStartOption.NewPage, division.StartAt);

			//verify division has all default settings
			VerifyDivisionDefault(division);

			//verify Scripture's HeaderFooterSets
			Assert.AreEqual(cExistingHfSets + 1, m_scr.HeaderFooterSetsOC.Count,
				"Wrong number of HeaderFooterSets was created");

			IPubHFSet hfSet = m_scr.FindHeaderFooterSetByName("default headers & footers");
			Assert.IsNotNull(hfSet, "Didn't find the newly created HF set");
			VerifyHeaderFooterSetDefault(hfSet, false);

			// verify TePublication Version
			CmResource pubResource = CmResource.GetResource(m_inMemoryCache.Cache, m_scr.Hvo,
				(int)Scripture.ScriptureTags.kflidResources, "TePublications");
			Assert.IsNotNull(pubResource, "Resource for TePublications was not found.");
			Assert.AreEqual(new Guid("3BEC2E67-781F-4e46-73E8-AE81A456668D"), pubResource.Version);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// helper function verifies a complete HeaderFooterSet containing "Test Literals" data
		/// </summary>
		/// <param name="hfSet">the HeaderFooterSet to verify</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyHeaderFooterSetTestLiterals(IPubHFSet hfSet)
		{
			//verify header/footer set name
			Assert.AreEqual("Test Literals", hfSet.Name);
			// verify header/footer set description
			Assert.AreEqual("Test Description", hfSet.Description.Text);

			//verify DefaultHeader
			IPubHeader hf = hfSet.DefaultHeaderOA;
			Assert.AreEqual("Right top", hf.OutsideAlignedText.Text);
			Assert.AreEqual("Center top", hf.CenteredText.Text);
			Assert.AreEqual("Left top", hf.InsideAlignedText.Text);

			//verify DefaultFooter
			hf = hfSet.DefaultFooterOA;
			Assert.AreEqual("Right bottom", hf.OutsideAlignedText.Text);
			Assert.AreEqual("Center bottom", hf.CenteredText.Text);
			Assert.AreEqual("Left bottom", hf.InsideAlignedText.Text);

			//verify FirstHeader
			hf = hfSet.FirstHeaderOA;
			Assert.AreEqual("Right top1", hf.OutsideAlignedText.Text);
			Assert.IsNotNull(hf.CenteredText.UnderlyingTsString, "Should retrieve an empty tss");
			Assert.AreEqual(0, hf.CenteredText.UnderlyingTsString.Length);
			Assert.AreEqual("Left top1", hf.InsideAlignedText.Text);

			//verify FirstFooter
			hf = hfSet.FirstFooterOA;
			Assert.AreEqual("Right bottom1", hf.OutsideAlignedText.Text);
			Assert.AreEqual("Center bottom1", hf.CenteredText.Text);
			Assert.AreEqual("Left bottom1", hf.InsideAlignedText.Text);

			//verify EvenHeader
			hf = hfSet.EvenHeaderOA;
			Assert.AreEqual("Left top E", hf.OutsideAlignedText.Text);
			Assert.AreEqual("Center top E", hf.CenteredText.Text);
			Assert.AreEqual("Right top E", hf.InsideAlignedText.Text);

			//verify EvenFooter
			hf = hfSet.EvenFooterOA;
			Assert.AreEqual("Left bottom E", hf.OutsideAlignedText.Text);
			Assert.AreEqual("Center bottom E", hf.CenteredText.Text);
			Assert.AreEqual("Right bottom E", hf.InsideAlignedText.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.GetPubPageSizes method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPubPageSizes()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with metric measurements
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject date=\"2007-08-20\" " +
					"DTDver=\"9E835785-F508-4b58-844B-02A3D6EC3579\" " +
					"label=\"Scripture\" version=\"3BED2E67-781F-4e46-93E8-AE81A454668D\">" +

				"<Publications>" +

				"<Publication id=\"Default_Scripture\" Name=\"Def Scripture\" Description=\"Dummy\" " +
				"MeasurementUnits=\"inch\" " +
				"IsLandscape=\"true\" PageSize=\"full\" " +
				"GutterMargin=\"0.2\" BindingSide=\"right\" " +
				"BaseCharSize=\"13\" BaseLineSize=\"15\" " +
				"SheetLayout=\"duplex\"> " +

				"<SupportedPublicationSizes>" +
				"<PublicationPageSize id=\"full\">" +
					"<Name iculocale=\"en\">Full Page</Name>" +
				"</PublicationPageSize>" +
				"<PublicationPageSize id=\"IPUBLargerBible\" Height=\"8.7\" Width=\"5.8\">" +
					"<Name iculocale=\"en\">5.8 x 8.7in</Name>" +
				"</PublicationPageSize>" +
				"<PublicationPageSize id=\"IPUBSmallerBible\" Height=\"8.25\" Width=\"5.25\">" +
					"<Name iculocale=\"en\">5.25 x 8.25in</Name>" +
				"</PublicationPageSize>" +
				"</SupportedPublicationSizes>" +

				"</Publication>" +

				"<Publication id=\"Trial Publication\" Name=\"Trial Pub\" Description=\"Dummy\" " +
				"MeasurementUnits=\"cm\" " +
				"IsLandscape=\"true\" PageSize=\"full\" " +
				"GutterMargin=\"0.2\" BindingSide=\"right\" " +
				"BaseCharSize=\"13\" BaseLineSize=\"15\" " +
				"SheetLayout=\"duplex\"> " +

				"<SupportedPublicationSizes>" +
				"<PublicationPageSize id=\"IPUBLargerBible\" Height=\"22.098\" Width=\"14.732\">" +
					"<Name iculocale=\"en\">5.8 x 8.7in</Name>" +
					"<Name iculocale=\"es\">Biblia grande</Name>" +
				"</PublicationPageSize>" +
				"<PublicationPageSize id=\"IPUBSmallerBible\" Height=\"20.955\" Width=\"13.335\">" +
					"<Name iculocale=\"en\">5.25 x 8.25in</Name>" +
					"<Name iculocale=\"es\">Biblia chica</Name>" +
				"</PublicationPageSize>" +
				"</SupportedPublicationSizes>" +

				"</Publication>" +
				"</Publications>" +
				"</PublicationDefaultsForNewProject>");

			List<PubPageInfo> pubPageSizes = m_pubInitializer.CallGetPubPageSizes(doc, "Def Scripture", "en");
			Assert.AreEqual(3, pubPageSizes.Count, "Wrong number of publication page sizes was returned");
			PubPageInfo info = pubPageSizes[0];
			Assert.AreEqual("Full Page", info.Name);
			Assert.AreEqual(0, info.Width);
			Assert.AreEqual(0, info.Height);
			info = pubPageSizes[1];
			Assert.AreEqual("5.8 x 8.7in", info.Name);
			Assert.AreEqual(5.8 * 72000, info.Width);
			Assert.AreEqual(8.7 * 72000, info.Height);
			info = pubPageSizes[2];
			Assert.AreEqual("5.25 x 8.25in", info.Name);
			Assert.AreEqual(5.25 * 72000, info.Width);
			Assert.AreEqual(8.25 * 72000, info.Height);

			pubPageSizes = m_pubInitializer.CallGetPubPageSizes(doc, "Trial Pub", "es");
			Assert.AreEqual(2, pubPageSizes.Count, "Wrong number of publication page sizes was returned");
			info = pubPageSizes[0];
			Assert.AreEqual("Biblia grande", info.Name);
			Assert.AreEqual(5.8 * 72000, info.Width);
			Assert.AreEqual(8.7 * 72000, info.Height);
			info = pubPageSizes[1];
			Assert.AreEqual("Biblia chica", info.Name);
			Assert.AreEqual(5.25 * 72000, info.Width);
			Assert.AreEqual(8.25 * 72000, info.Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with metric
		/// measurements and a few other variations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationInfo_Centimeters()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with metric measurements
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject date=\"2007-08-20\" " +
					"DTDver=\"9E835785-F508-4b58-844B-02A3D6EC3579\" " +
					"label=\"Scripture\" version=\"87AC2E67-781F-4e46-73E8-AE81A457778D\">" +
				"<Publications>" +

				"<Publication id=\"Default_Scripture A4\" Name=\"Test\" Description=\"Dummy\" " +
				"MeasurementUnits=\"cm\" " +
				"IsLandscape=\"true\" PageSize=\"IPUBLargerBible\" " +
				"GutterMargin=\"0.5\" BindingSide=\"Top\" " +
				"SheetLayout=\"booklet\"> " +

				"<SupportedPublicationSizes>" +
				"<PublicationPageSize id=\"full\">" +
					"<Name iculocale=\"en\">Full Page</Name>" +
				"</PublicationPageSize>" +
				"<PublicationPageSize id=\"IPUBLargerBible\" Height=\"22.098\" Width=\"14.732\">" +
					"<Name iculocale=\"en\">5.8 x 8.7in</Name>" +
				"</PublicationPageSize>" +
				"<PublicationPageSize id=\"IPUBSmallerBible\" Height=\"20.955\" Width=\"13.335\">" +
					"<Name iculocale=\"en\">5.25 x 8.25in</Name>" +
				"</PublicationPageSize>" +
				"</SupportedPublicationSizes>" +

				"<Divisions>" +
				"<Division id=\"Scripture\" StartAt=\"continuous\">" +
				"<PageLayout OriginalPageLayoutName=\"Default Scripture\" " +
				"MarginTop=\"2.1\" MarginBottom=\"2.2\"	MarginInside=\"1.5\" " +
				"MarginOutside=\"1.6\" PositionHeader=\"1.9\" PositionFooter=\"2.05\"/>"  +
				"</Division>" +
				"</Divisions>" +
				"</Publication>" +
				"</Publications>" +
				"</PublicationDefaultsForNewProject>" );

			//Run CreatePublicationInfo()
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify publication
			Assert.AreEqual(1, m_scr.PublicationsOC.Count, "Wrong number of publications was created");
			FdoOwningCollection<IPublication> oc = m_scr.PublicationsOC;
			IPublication pub = Publication.CreateFromDBObject(Cache, oc.HvoArray[0]);
			Assert.IsTrue(pub.IsLandscape);
			Assert.AreEqual((int)(8.7 * TePublicationsInit.kMpPerInch), pub.PageHeight); // For now, this enumeration is always expressed in inches
			Assert.AreEqual((int)(5.8 * TePublicationsInit.kMpPerInch), pub.PageWidth); // For now, this enumeration is always expressed in inches
			Assert.AreEqual(0, pub.PaperHeight, "Paper Height will be based on default paper size for default printer");
			Assert.AreEqual(0, pub.PaperWidth, "Paper Height will be based on default paper size for default printer");
			Assert.AreEqual((int)(0.5 * TePublicationsInit.kMpPerCm), pub.GutterMargin);
			Assert.AreEqual(BindingSide.Top, pub.BindingEdge);
			Assert.AreEqual(MultiPageLayout.Booklet, pub.SheetLayout);

			//verify pub's division
			Assert.AreEqual(1, pub.DivisionsOS.Count, "Publication should have 1 division.");
			IPubDivision division = pub.DivisionsOS[0];
			Assert.AreEqual(DivisionStartOption.Continuous, division.StartAt);

			//verify division's pagelayout
			Assert.AreEqual((int)(2.1 * TePublicationsInit.kMpPerCm), division.PageLayoutOA.MarginTop);
			Assert.AreEqual((int)(2.2 * TePublicationsInit.kMpPerCm), division.PageLayoutOA.MarginBottom);
			Assert.AreEqual((int)(1.5 * TePublicationsInit.kMpPerCm), division.PageLayoutOA.MarginInside);
			Assert.AreEqual((int)(1.6 * TePublicationsInit.kMpPerCm), division.PageLayoutOA.MarginOutside);
			Assert.AreEqual((int)(1.9 * TePublicationsInit.kMpPerCm), division.PageLayoutOA.PosHeader);
			Assert.AreEqual((int)(2.05 * TePublicationsInit.kMpPerCm), division.PageLayoutOA.PosFooter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with header
		/// and footer info missing in the xml file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationsMissingHeaderFooterDetail()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				"<Publications>" +
				"<Publication>" +
				"<Divisions>" +
				"<Division HeaderFooterSetRef=\"MissingDetail\">" +
				"<PageLayout/>" +
				"</Division>" +
				"</Divisions>" +
				"</Publication>" +
				"</Publications>" +
				"<HeaderFooterSets>" +
				"<HeaderFooterSet id=\"MissingDetail\">" +
				// no OriginalHeaderFooterSetName attribute
				// no <DefaultHeader>
					"<DefaultFooter>" +
				// no <Outside>
						"<Center>" +  // no <Element>
						"</Center>" +
						"<Inside>" +
						"<Element></Element>" + // no content in <Element>
						"</Inside>" +
						"</DefaultFooter>" +
				// no <FirstHeader>
				// no <FirstFooter>
				// no <EvenHeader>
				// no <EvenFooter>
				"</HeaderFooterSet>" +
				"</HeaderFooterSets>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//get publication and division
			Assert.AreEqual(1, m_scr.PublicationsOC.Count, "Wrong number of publications was created");
			FdoOwningCollection<IPublication> oc = m_scr.PublicationsOC;
			IPublication pub = Publication.CreateFromDBObject(Cache, oc.HvoArray[0]);
			Assert.AreEqual(1, pub.DivisionsOS.Count, "Publication should have 1 division.");
			IPubDivision division = pub.DivisionsOS[0];

			//verify the header/footer set
			IPubHFSet hfSet = division.HFSetOA;
			VerifyHeaderFooterSetDefault(hfSet, true);

			//verify division's hf bools
			Assert.IsFalse(division.DifferentFirstHF, "DifferentFirstHF should be False.");
			Assert.IsFalse(division.DifferentEvenHF, "DifferentEvenHF should be False.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// helper function verifies a complete HeaderFooterSet containing "Default" data
		/// </summary>
		/// <param name="hfSet">the HeaderFooterSet to verify</param>
		/// <param name="fOwnedByDivision">if set to <c>true</c> then this H/F sett is owned by
		/// a division.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyHeaderFooterSetDefault(IPubHFSet hfSet, bool fOwnedByDivision)
		{
			//verify name
			Assert.AreEqual("default headers & footers", hfSet.Name);

			//verify Default Header and Footer
			VerifyDefaultHeaderOrFooter(hfSet.DefaultHeaderOA);
			VerifyDefaultHeaderOrFooter(hfSet.DefaultFooterOA);

			//verify First and Even
			if (fOwnedByDivision)
			{
				Assert.IsNull(hfSet.FirstHeaderOA);
				Assert.IsNull(hfSet.FirstFooterOA);
				Assert.IsNull(hfSet.EvenHeaderOA);
				Assert.IsNull(hfSet.EvenFooterOA);
			}
			else
			{
				VerifyDefaultHeaderOrFooter(hfSet.FirstHeaderOA);
				VerifyDefaultHeaderOrFooter(hfSet.FirstFooterOA);
				VerifyDefaultHeaderOrFooter(hfSet.EvenHeaderOA);
				VerifyDefaultHeaderOrFooter(hfSet.EvenFooterOA);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// helper function verifies a Header or Footer containing "Default" data
		/// </summary>
		/// <param name="hf">the Header or Footer to verify</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyDefaultHeaderOrFooter(IPubHeader hf)
		{
			Assert.IsNotNull(hf);
			Assert.IsNotNull(hf.InsideAlignedText.UnderlyingTsString);
			Assert.AreEqual(0, hf.InsideAlignedText.UnderlyingTsString.Length);
			Assert.IsNotNull(hf.CenteredText.UnderlyingTsString);
			Assert.AreEqual(0, hf.CenteredText.UnderlyingTsString.Length);
			Assert.IsNotNull(hf.OutsideAlignedText.UnderlyingTsString);
			Assert.AreEqual(0, hf.OutsideAlignedText.UnderlyingTsString.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// header &amp; footer literal strings with specified writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationsHeaderFooterLiteralWS()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				"<Publications>" +
				"<Publication>" +
				"</Publication>" +
				"</Publications>" +
				"<HeaderFooterSets>" +
				"<HeaderFooterSet>" +

				"<DefaultHeader>" +
				"<Outside>" +
				"<Element ws=\"en\">Right top</Element>" +
				"</Outside>" +
				"<Center>" +
				"<Element ws=\"fr\">Center top</Element>" +
				"</Center>" +
				"</DefaultHeader>" +

				"<FirstFooter>" +
				"<Outside>" +
				"</Outside>" +
				"<Center>" +
				"<Element></Element>" +
				"</Center>" +
				"</FirstFooter>" +

				"</HeaderFooterSet>" +
				"</HeaderFooterSets>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.HeaderFooterSetsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.HeaderFooterSetsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//get HeaderFooterSets
			Assert.AreEqual(1, m_scr.HeaderFooterSetsOC.Count,
				"Wrong number of HeaderFooterSets was created");
			FdoOwningCollection<IPubHFSet> oc = m_scr.HeaderFooterSetsOC;
			IPubHFSet hfSet = PubHFSet.CreateFromDBObject(Cache, oc.HvoArray[0]);

			//get DefaultHeader
			IPubHeader hf = hfSet.DefaultHeaderOA;

			//verify Outside
			ITsString tss = hf.OutsideAlignedText.UnderlyingTsString;
			Assert.AreEqual(1, tss.RunCount);
			Assert.AreEqual("Right top", tss.get_RunText(0));
			ITsTextProps ttpRun = tss.get_Properties(0);

			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			int wsExpected = wsf.GetWsFromStr("en");
			ITsTextProps ttpExpected = StyleUtils.CharStyleTextProps(null, wsExpected);
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(ttpExpected, ttpRun, out sWhy))
				Assert.Fail(sWhy);

			//verify Center
			tss = hf.CenteredText.UnderlyingTsString;
			Assert.AreEqual(1, tss.RunCount);
			Assert.AreEqual("Center top", tss.get_RunText(0));
			ttpRun = tss.get_Properties(0);
			wsExpected = wsf.GetWsFromStr("fr");
			ttpExpected = StyleUtils.CharStyleTextProps(null, wsExpected);
			if (!TsTextPropsHelper.PropsAreEqual(ttpExpected, ttpRun, out sWhy))
				Assert.Fail(sWhy);

			//get FirstFooter
			hf = hfSet.FirstFooterOA;

			//verify Outside
			tss = hf.OutsideAlignedText.UnderlyingTsString;
			Assert.AreEqual(1, tss.RunCount);
			Assert.AreEqual(0, tss.Length);
			ttpRun = tss.get_Properties(0);
			wsExpected = Cache.DefaultUserWs;
			ttpExpected = StyleUtils.CharStyleTextProps(null, wsExpected);
			if (!TsTextPropsHelper.PropsAreEqual(ttpExpected, ttpRun, out sWhy))
				Assert.Fail(sWhy);

			//verify Center
			tss = hf.CenteredText.UnderlyingTsString;
			Assert.AreEqual(1, tss.RunCount);
			Assert.AreEqual(0, tss.Length);
			ttpRun = tss.get_Properties(0);
			wsExpected = Cache.DefaultUserWs;
			ttpExpected = StyleUtils.CharStyleTextProps(null, wsExpected);
			if (!TsTextPropsHelper.PropsAreEqual(ttpExpected, ttpRun, out sWhy))
				Assert.Fail(sWhy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// header &amp; footer literal strings with specified writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestReadHfElementsWithORCs()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<Outside>" +
				"<Element type=\"PageNumber\"></Element>" +
				"<Element type=\"TotalPageCount\"></Element>" +
				"<Element type=\"FirstReference\"></Element>" +
				"<Element type=\"LastReference\"></Element>" +
				"<Element type=\"DivisionName\"></Element>" +
				"<Element type=\"PublicationTitle\"></Element>" +
				"<Element type=\"PrintDate\"></Element>" +
				"<Element type=\"PageReference\"></Element>" +
				"</Outside>");

			ITsString tss =
				m_pubInitializer.CallReadHfElements(doc.SelectNodes("/Outside/Element"));

			//verify the runs of the string
			Assert.AreEqual(8, tss.RunCount);

			VerifyHfORCRun(tss, 0, "644DF48A-3B60-45f4-80C7-739BE6E56A96", "page number");
			VerifyHfORCRun(tss, 1, "E0EF9EDA-E4E2-4fcf-8720-5BC361BCE110", "total page count");
			VerifyHfORCRun(tss, 2, "397F43AE-E2B2-4f20-928A-1DF193C07674", "first reference");
			VerifyHfORCRun(tss, 3, "85EE15C6-0799-46c6-8769-F9B3CE313AE2", "last reference");
			VerifyHfORCRun(tss, 4, "2277B85F-47BB-45c9-BC7A-7232E26E901C", "division name");
			VerifyHfORCRun(tss, 5, "C8136D98-6957-43bd-BEA9-7DCE35200900", "publication name");
			VerifyHfORCRun(tss, 6, "C4556A21-41A8-4675-A74D-59B2C1A7E2B8", "print date");
			VerifyHfORCRun(tss, 7, "8978089A-8969-424e-AE54-B94C554F882D", "page reference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify the given run of a Header/Footer string, which should contain an ORC for the
		/// specified GUID.
		/// </summary>
		/// <param name="tss">The TS String</param>
		/// <param name="iRun">the 0-based index of the run to check</param>
		/// <param name="sGuid">The expected guid, represented as a string</param>
		/// <param name="sType">Phrase describing the type of element expected, used for error
		/// reporting</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyHfORCRun(ITsString tss, int iRun, string sGuid, string sType)
		{
			Assert.AreEqual(StringUtils.kchObject, tss.get_RunText(iRun)[0],
				"Run " + iRun + " is not an ORC.");
			ITsTextProps ttp = tss.get_Properties(iRun);
			string sObjData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
			Assert.AreEqual((byte)FwObjDataTypes.kodtContextString,
				Convert.ToByte(sObjData[0]));
			Assert.AreEqual(new Guid(sGuid), MiscUtils.GetGuidFromObjData(sObjData.Substring(1)),
				"Run " + iRun + " does not have the correct guid to represent " + sType + ".");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// page layout and header/footer set info missing in the xml file. The data items for
		/// the missing info should receive default values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationsMissingDivisionItems()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				"<Publications>" +
				"<Publication>" +
				"<Divisions>" +
				"<Division>" +
				//no PageLayout
				//no HeaderFooterSet
				"</Division>" +
				"</Divisions>" +
				"</Publication>" +
				"</Publications>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//get publication and division
			Assert.AreEqual(1, m_scr.PublicationsOC.Count, "Wrong number of publications was created");
			FdoOwningCollection<IPublication> oc = m_scr.PublicationsOC;
			IPublication pub = Publication.CreateFromDBObject(Cache, oc.HvoArray[0]);

			Assert.AreEqual(1, pub.DivisionsOS.Count, "Publication should have 1 division.");
			IPubDivision division = pub.DivisionsOS[0];

			VerifyDivisionDefault(division);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// helper function verifies a complete division containing "Default" data
		/// </summary>
		/// <param name="division">the PubDivision to verify</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyDivisionDefault(IPubDivision division)
		{
			//verify the division info
			Assert.AreEqual(DivisionStartOption.NewPage, division.StartAt);

			//verify division's pagelayout
			IPubPageLayout pageLayout = division.PageLayoutOA;
			VerifyPageLayoutDefault(pageLayout);

			//verify the division's header/footer set
			IPubHFSet hfSet = division.HFSetOA;
			VerifyHeaderFooterSetDefault(hfSet, true);

			//verify division's hf bools
			Assert.IsFalse(division.DifferentFirstHF, "DifferentFirstHF should be False.");
			Assert.IsFalse(division.DifferentEvenHF, "DifferentEvenHF should be False.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// helper function verifies a complete page layout containing "Default" data
		/// </summary>
		/// <param name="pageLayout">the PubPageLayout to verify</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyPageLayoutDefault(IPubPageLayout pageLayout)
		{
			Assert.IsNull(pageLayout.Name, "Name should be null");
			Assert.AreEqual(TePublicationsInit.kMpPerInch, pageLayout.MarginTop);
			Assert.AreEqual(TePublicationsInit.kMpPerInch, pageLayout.MarginBottom);
			Assert.AreEqual(TePublicationsInit.kMpPerInch, pageLayout.MarginInside);
			Assert.AreEqual(TePublicationsInit.kMpPerInch, pageLayout.MarginOutside);
			Assert.AreEqual((int)(0.75 * TePublicationsInit.kMpPerInch), pageLayout.PosHeader);
			Assert.AreEqual((int)(0.75 * TePublicationsInit.kMpPerInch), pageLayout.PosFooter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// Division tag missing in the xml file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationsMissingDivision()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				"<Publications>" +
				"<Publication>" +
				"<Divisions>" +
				// no <Division> tag
				"</Divisions>" +
				"</Publication>" +
				"</Publications>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify publication and division
			Assert.AreEqual(1, m_scr.PublicationsOC.Count, "Wrong number of publications was created");
			FdoOwningCollection<IPublication> oc = m_scr.PublicationsOC;
			IPublication pub = Publication.CreateFromDBObject(Cache, oc.HvoArray[0]);
			Assert.AreEqual(1, pub.DivisionsOS.Count, "Publication should have 1 division.");
			IPubDivision division = pub.DivisionsOS[0];

			VerifyDivisionDefault(division);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// Divisions tag missing in the xml file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationsMissingDivisions()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				"<Publications>" +
				"<Publication>" +
				// no <Divisions> tag
				"</Publication>" +
				"</Publications>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify publication and division
			Assert.AreEqual(1, m_scr.PublicationsOC.Count, "Wrong number of publications was created");
			FdoOwningCollection<IPublication> oc = m_scr.PublicationsOC;
			IPublication pub = Publication.CreateFromDBObject(Cache, oc.HvoArray[0]);
			Assert.AreEqual(1, pub.DivisionsOS.Count, "Publication should have 1 division.");
			IPubDivision division = pub.DivisionsOS[0];

			VerifyDivisionDefault(division);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// Publication tag missing in the xml file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(WorkerThreadException))]
		public void CreatePublicationsMissingPublication()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				"<Publications>" +
				// no <Publication> tag
				"</Publications>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify publication and division
			//Assert.AreEqual(0, scr.PublicationsOC.Count, "Wrong number of publications was created");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// Publications tag missing in the xml file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(WorkerThreadException))]
		public void CreatePublicationsMissingPublications()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				// no <Publications> tag
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.PublicationsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.PublicationsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify publication and division
			//Assert.AreEqual(0, scr.PublicationsOC.Count, "Wrong number of publications was created");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// HeaderFooterSet tag missing in the xml file under the HeaderFooterSets tag.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationsMissingHeaderFooterSet()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				"<Publications>" +
				"<Publication>" +
				"</Publication>" +
				"</Publications>" +
				"<HeaderFooterSets>" +
				// no <HeaderFooterSet> tag
				"</HeaderFooterSets>" +
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.HeaderFooterSetsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.HeaderFooterSetsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify HeaderFooterSets
			Assert.AreEqual(0, m_scr.HeaderFooterSetsOC.Count, "Wrong number of HeaderFooterSets was created");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreatePublicationInfo method, with
		/// HeaderFooterSets tag missing in the xml file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePublicationsMissingHeaderFooterSets()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<PublicationDefaultsForNewProject>" +
				"<Publications>" +
				"<Publication>" +
				"</Publication>" +
				"</Publications>" +
				// no <HeaderFooterSets> tag
				"</PublicationDefaultsForNewProject>");

			//Run CreatePublications()
			int[] hvos = m_scr.HeaderFooterSetsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.HeaderFooterSetsOC.Remove(hvo);
			m_pubInitializer.CallCreatePublicationInfo(doc);

			//verify HeaderFooterSets
			Assert.AreEqual(0, m_scr.HeaderFooterSetsOC.Count, "Wrong number of HeaderFooterSets was created");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the TescrInitializer.CreateFactoryPublicationInfo method, with
		/// an invalid xml file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This condition must be tested by hand.")]
		[ExpectedException(typeof(Exception))]
		public void CreateFactoryPublicationsInvalidXmlFile()
		{
			CheckDisposed();

			//Replace the DistFiles\Translation Editor\TePublications.xml with a junk plain text file.
			//Then call CreateFactoryPublicationInfo() - In TE do File-NewProject to do this
			//TE should throw an exception with a useful message
		}
		#endregion

		#region CreateHfSets tests
		[Test]
		public void OverwriteExistingHfSets()
		{
			CheckDisposed();

			XmlDocument doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<HeaderFooterSets>" +
				"<HeaderFooterSet id=\"HFDefaultScripture\" Name=\"Default Scripture\">" +
					"<DefaultHeader>" +
						"<Inside>" +
							"<Element type=\"PageNumber\"/>" +
						"</Inside>" +
						"<Outside>" +
							"<Element type=\"LastReference\"/>" +
						"</Outside>" +
					"</DefaultHeader>" +
					"<DefaultFooter>" +
						"<Center>" +
							"<Element type=\"PageNumber\"/>" +
						"</Center>" +
					"</DefaultFooter>" +
					"<FirstHeader>" +
					"</FirstHeader>" +
					"<FirstFooter>" +
						"<Center>" +
							"<Element type=\"PageNumber\"/>" +
						"</Center>" +
					"</FirstFooter>" +
					"<EvenHeader>" +
						"<Inside>" +
							"<Element type=\"PageNumber\"/>" +
						"</Inside>" +
						"<Outside>" +
							"<Element type=\"FirstReference\"/>" +
						"</Outside>" +
					"</EvenHeader>" +
					"<EvenFooter>" +
						"<Center>" +
							"<Element type=\"PageNumber\"/>" +
						"</Center>" +
					"</EvenFooter>" +
				"</HeaderFooterSet>" +
				"</HeaderFooterSets>");

			// Call CreateHfSets() the first time
			int[] hvos = m_scr.HeaderFooterSetsOC.HvoArray;
			foreach (int hvo in hvos)
				m_scr.HeaderFooterSetsOC.Remove(hvo);
			m_pubInitializer.CallCreateHfSets(doc.SelectNodes("HeaderFooterSets/HeaderFooterSet"));

			//verify HeaderFooterSets
			Assert.AreEqual(1, m_scr.HeaderFooterSetsOC.Count, "Wrong number of HeaderFooterSets was created");

			// Now prepare the updated version
			doc = new XmlDocument();
			// Create a fake temporary xml publications file with no header or footer details
			doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
				"<HeaderFooterSets>" +
				"<HeaderFooterSet id=\"HFDefaultScripture\" Name=\"Default Scripture\" " +
					"Description=\"Default Scripture H/F Set\">" +
					"<DefaultHeader>" +
						"<Center>" +
							"<Element type=\"LastReference\"/>" +
						"</Center>" +
						"<Outside>" +
							"<Element type=\"PageNumber\"/>" +
						"</Outside>" +
					"</DefaultHeader>" +
					"<FirstHeader>" +
					"</FirstHeader>" +
					"<FirstFooter>" +
						"<Center>" +
							"<Element type=\"PageNumber\"/>" +
						"</Center>" +
					"</FirstFooter>" +
					"<EvenHeader>" +
						"<Center>" +
							"<Element type=\"FirstReference\"/>" +
						"</Center>" +
						"<Outside>" +
							"<Element type=\"PageNumber\"/>" +
						"</Outside>" +
					"</EvenHeader>" +
				"</HeaderFooterSet>" +
				"<HeaderFooterSet id=\"HFDefaultBT\" Name=\"Default Back Translation\" " +
					"Description=\"Default BT H/F Set\">" +
					"<DefaultHeader>" +
						"<Outside>" +
							"<Element type=\"PageReference\"/>" +
						"</Outside>" +
					"</DefaultHeader>" +
					"<DefaultFooter>" +
						"<Inside>" +
							"<Element type=\"PrintDate\"/>" +
						"</Inside>" +
						"<Center>" +
							"<Element type=\"ProjectName\"/>" +
						"</Center>" +
						"<Outside>" +
							"<Element type=\"PageNumber\"/>" +
						"</Outside>" +
					"</DefaultFooter>" +
				"</HeaderFooterSet>" +
				"</HeaderFooterSets>");

			m_pubInitializer.CallCreateHfSets(doc.SelectNodes("HeaderFooterSets/HeaderFooterSet"));

			//verify updated/added HeaderFooterSets
			Assert.AreEqual(2, m_scr.HeaderFooterSetsOC.Count, "Wrong number of HeaderFooterSets was created when updating");
			IPubHFSet hfSet = m_scr.FindHeaderFooterSetByName("Default Scripture");
			Assert.IsNotNull(hfSet);
			Assert.IsNotNull(hfSet.DefaultHeaderOA.CenteredText);
			Assert.AreEqual(1, hfSet.DefaultHeaderOA.CenteredText.Length);
			Assert.AreEqual(StringUtils.kchObject, hfSet.DefaultHeaderOA.CenteredText.Text[0]);
			Assert.IsNull(hfSet.DefaultHeaderOA.InsideAlignedText.Text);
			hfSet = m_scr.FindHeaderFooterSetByName("Default Back Translation");
			Assert.IsNotNull(hfSet);
		}
		#endregion
	}
	#endregion
}
