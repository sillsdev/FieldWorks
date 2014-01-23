// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TePublicationsInit.cs
// Responsibility: FieldWorks Team

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security;
using System.Xml;
using System.Globalization;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class providing static method that TE calls to perform initialization of
	/// publications and publication-related parameters in a Scripture project
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TePublicationsInit : SettingsXmlAccessorBase
	{
		#region Constants
		private const string ksPublicationSrc = "TePublications";
		//constants (also used in tests, therefore public)
		//  these constants are in millipoints per unit
		/// <summary></summary>
		public const int kMpPerInch = 72000;
		/// <summary></summary>
		public const double kMpPerCm = 72000 / 2.54;
		// however, some sources list millipt=.351mm
		#endregion

		#region Member variables
		/// <summary>The FDO Scripture object which will own the publications</summary>
		protected IScripture m_scr;
		/// <summary>multiplier for converting specified measurements into millipoints</summary>
		protected double m_conversion;
		/// <summary>empty string used for empty header/footer elements</summary>
		protected ITsString m_tssEmpty;
		/// <summary>dummy XmlDocument used for getting an empty attribute collection, a dummy
		/// node, etc; it appears impossible to create those objects individually, we must them
		/// from an XmlDocument</summary>
		protected XmlDocument m_dummyXmlDoc;
		private FdoCache m_cache;
		private int m_defUserWs;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TePublicationsInit"/> class. This
		/// constructor does not fully initialize the class for the purpose of processing
		/// factory settings and savng them in the database. It merely allows the XML file to
		/// be loaded for the purpose of accessing its data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected TePublicationsInit()
		{
			m_scr = null;
			m_defUserWs = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TePublicationsInit"/> class.
		/// </summary>
		/// <param name="scr">The Scripture object.</param>
		/// ------------------------------------------------------------------------------------
		protected TePublicationsInit(IScripture scr)
		{
			m_scr = scr;
			m_cache = scr.Cache;
			m_defUserWs = scr.Cache.WritingSystemFactory.UserWs;
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the resources (e.g., create styles or add publication info).
		/// </summary>
		/// <param name="dlg">The progress dialog manager.</param>
		/// <param name="doc">The loaded XML document that has the settings.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ProcessResources(IThreadedProgress dlg, XmlNode doc)
		{
			dlg.RunTask(CreateFactoryPublicationInfo, doc);
		}
		#endregion

		#region Public/internal static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that all factory publications exist
		/// </summary>
		/// <param name="lp">language project</param>
		/// <param name="existingProgressDlg">The existing progress dialog, if any.</param>
		/// ------------------------------------------------------------------------------------
		public static void EnsureFactoryPublications(ILangProject lp,
			IThreadedProgress existingProgressDlg)
		{
			TePublicationsInit pubInit = new TePublicationsInit(lp.TranslatedScriptureOA);
			pubInit.EnsureCurrentResource(existingProgressDlg);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create factory publications from the XML file.
		/// </summary>
		/// <param name="progressDlg">Progress dialog so the user can cancel</param>
		/// <param name="scr">The Scripture</param>
		/// -------------------------------------------------------------------------------------
		internal static void CreatePublicationInfo(IProgress progressDlg,
			IScripture scr)
		{
			TePublicationsInit pubInit = new TePublicationsInit(scr);
			pubInit.CreateFactoryPublicationInfo(progressDlg, pubInit.LoadDoc());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create factory publications and header/footer sets from the TE Publications XML file.
		/// </summary>
		/// <param name="progressDlg">Progress dialog so the user can cancel</param>
		/// <param name="parameters">Only parameter is the XmlNode that holds the publication
		/// information.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected object CreateFactoryPublicationInfo(IProgress progressDlg,
			params object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			CreatePublicationInfo(progressDlg, (XmlNode)parameters[0]);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of names of factory Header/Footer sets
		/// </summary>
		/// <returns>List of names of factory Header/Footer sets</returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public static List<string> FactoryHeaderFooterSets
		{
			get
			{
				TePublicationsInit pubInit = new TePublicationsInit();
				List<string> headerFooterSets = new List<string>();
				foreach (XmlNode hfSetNode in HeaderFooterSetNodes(pubInit.LoadDoc()))
				{
					XmlAttributeCollection attributes = hfSetNode.Attributes;
					headerFooterSets.Add(GetString(attributes, "Name"));
				}
				return headerFooterSets;
			}
		}
		#endregion

		#region Main processing methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the XML node list representing the factory Header/Footer sets
		/// </summary>
		/// <param name="rootNode">The XmlNode from which to read the publication info</param>
		/// <returns>the XML node list representing the factory Header/Footer sets</returns>
		/// ------------------------------------------------------------------------------------
		private static XmlNodeList HeaderFooterSetNodes(XmlNode rootNode)
		{
			return rootNode.SelectNodes("HeaderFooterSets/HeaderFooterSet");
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create publications and header/footer sets (in the DB) from the given XML document.
		/// </summary>
		/// <remarks>tests are able to call this method</remarks>
		/// <param name="progressDlg">Progress dialog</param>
		/// <param name="rootNode">The XmlNode from which to read the publication info</param>
		/// -------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected void CreatePublicationInfo(IProgress progressDlg, XmlNode rootNode)
		{
			// init static stuff we may need
			m_dummyXmlDoc = new XmlDocument();
			m_dummyXmlDoc.LoadXml("<dummy/>");

			// Set up progress dialog
			progressDlg.Position = 0;
			progressDlg.Title = TeResourceHelper.GetResourceString("kstidCreatingPublicationsCaption");
			progressDlg.Message = TeResourceHelper.GetResourceString("kstidCreatingPublicationsStatusMsg");

			//Get all publications and header/footer set nodes.
			XmlNodeList publicationNodes = rootNode.SelectNodes("Publications/Publication");
			XmlNodeList hfSetNodes = HeaderFooterSetNodes(rootNode);

			//we require at least one publication be created
			if (publicationNodes.Count == 0)
			{
				string message;
#if DEBUG
				message = "Error reading TePublications.xml: Missing <Publications> or <Publication> node";
#else
				message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
				throw new Exception(message);
			}

			// now set the progress dialog max
			progressDlg.Minimum = 0;
			progressDlg.Maximum = publicationNodes.Count + hfSetNodes.Count;

			// Create the Publications from those nodes
			CreatePublications(progressDlg, publicationNodes, hfSetNodes);

			//Create the Header/Footer Sets from those nodes
			CreateHfSets(progressDlg, hfSetNodes);

			// Finally, update publications version in database.
			SetNewResourceVersion(GetVersion(rootNode));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a Publication for each publication node in the given xml node list.
		/// </summary>
		/// <param name="progressDlg">Progress dialog</param>
		/// <param name="publicationNodes">the xml nodes to read</param>
		/// <param name="hfSetNodes">The header/footer set nodes.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void CreatePublications(IProgress progressDlg, XmlNodeList publicationNodes,
			XmlNodeList hfSetNodes)
		{
			// Remove any previously-defined publications.
			m_scr.PublicationsOC.Clear();

			//create each publication
			foreach (XmlNode publicationNode in publicationNodes)
			{
				progressDlg.Step(0);

				XmlAttributeCollection attributes = publicationNode.Attributes;
				string pubName = GetString(attributes, "Name");

				// Determine the measurement unit to be used for all measurements in this pub
				m_conversion = GetUnitConversion(attributes, "MeasurementUnits", kMpPerInch);

				// Create the new Publication object and set non-variable properties
				IPublication pub = m_cache.ServiceLocator.GetInstance<IPublicationFactory>().Create();
				m_scr.PublicationsOC.Add(pub);

				pub.Name = pubName;
				// We'll build a TsString to populate it.
				ITsStrFactory strFactory = TsStrFactoryClass.Create();
				pub.Description =
					strFactory.MakeString(GetString(attributes, "Description"),
					m_defUserWs);
				pub.IsLandscape = GetBoolean(attributes, "IsLandscape", false);
				GetPageHeightAndWidth(attributes, pub,
					publicationNode.SelectSingleNode("SupportedPublicationSizes"));
				pub.PaperHeight = 0;
				pub.PaperWidth = 0;
				pub.GutterMargin = GetMeasurement(attributes, "GutterMargin", 0, m_conversion);
				pub.BindingEdge = GetGutterLoc(attributes, "BindingSide", BindingSide.Left);
				pub.BaseFontSize = GetMeasurement(attributes, "BaseCharSize", 0, 1000);
				// Line spacing < 0 means "exact", which is all we support currently
				pub.BaseLineSpacing = -Math.Abs(GetMeasurement(attributes, "BaseLineSize", 0, 1000));
				pub.SheetLayout = GetSheetLayout(attributes, MultiPageLayout.Simplex);

				XmlNodeList divisionNodes = publicationNode.SelectNodes("Divisions/Division");
				CreateDivisions(pub, divisionNodes, hfSetNodes);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a PubDivision for each division node in the given xml node list.
		/// </summary>
		/// <param name="pub">the Publication object which will own the new divsions</param>
		/// <param name="divisionNodes">the xml nodes to read;  if this list has no nodes,
		/// one division will be initialized with default values</param>
		/// <param name="hfSetNodes">The header/footer set nodes.</param>
		/// ------------------------------------------------------------------------------------
		private void CreateDivisions(IPublication pub, XmlNodeList divisionNodes,
			XmlNodeList hfSetNodes)
		{
			if (divisionNodes.Count == 0)
			{
				// Create a dummy node, to force creation of a default division
				divisionNodes = m_dummyXmlDoc.SelectNodes("dummy");
				Debug.Assert(divisionNodes.Count == 1);
			}

			// Remove previously defined divisions.
			pub.DivisionsOS.Clear();

			foreach (XmlNode divisionNode in divisionNodes)
			{
				XmlAttributeCollection attributes = divisionNode.Attributes;

				IPubDivision division = m_cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create();
				pub.DivisionsOS.Add(division);
				division.StartAt = GetDivisionStart(attributes, "StartAt",
					DivisionStartOption.NewPage);
				division.NumColumns = GetInt(attributes, "NumberOfColumns", 1);
				division.DifferentFirstHF = false; // default
				division.DifferentEvenHF = false;  // default

				// Create the division's PageLayout
				XmlNode pageLayoutNode = divisionNode.SelectSingleNode("PageLayout");
				IPubPageLayout pageLayout = m_cache.ServiceLocator.GetInstance<IPubPageLayoutFactory>().Create();
				division.PageLayoutOA = pageLayout;
				ReadPageLayout(pageLayout, pageLayoutNode);

				// Create the division's HeaderFooterSet
				XmlNode HfSetRef = attributes.GetNamedItem("HeaderFooterSetRef");
				XmlNode hfSetNode = null;
				if (HfSetRef != null && HfSetRef.Value != string.Empty)
				{
					foreach (XmlNode hfNode in hfSetNodes)
					{
						if (GetString(hfNode.Attributes, "id") == HfSetRef.Value)
						{
							hfSetNode = hfNode;
							break;
						}
					}
				}
				division.HFSetOA = m_cache.ServiceLocator.GetInstance<IPubHFSetFactory>().Create();
				ReadHeaderFooterSet(division.HFSetOA, hfSetNode,
					false, false);

				// Update division's bools to reflect existing Headers and Footers
				if (division.HFSetOA.FirstHeaderOA != null)
				{
					Debug.Assert(division.HFSetOA.FirstFooterOA != null);
					division.DifferentFirstHF = true;
				}
				if (division.HFSetOA.EvenHeaderOA != null)
				{
					Debug.Assert(division.HFSetOA.EvenFooterOA != null);
					division.DifferentEvenHF = true;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init the given PubPageLayout by reading the given PageLayout XML node.
		/// </summary>
		/// <param name="pageLayout">the PubPageLayout object to initialize</param>
		/// <param name="pageLayoutNode">the given PageLayout XML node to read.
		/// If null, the PubPageLayout will be initialized to default values.</param>
		/// ------------------------------------------------------------------------------------
		private void ReadPageLayout(IPubPageLayout pageLayout, XmlNode pageLayoutNode)
		{
			XmlAttributeCollection attributes;

			//get attributes from the node
			if (pageLayoutNode != null)
				attributes = pageLayoutNode.Attributes;
			else
			{
				// or get empty attributes from our dummyXmlDoc
				attributes = m_dummyXmlDoc.FirstChild.Attributes;
				Debug.Assert(attributes.Count == 0);
			}

			pageLayout.Name = GetString(attributes, "OriginalPageLayoutName");
			pageLayout.MarginTop =
				GetMeasurement(attributes, "MarginTop", kMpPerInch, m_conversion);
			pageLayout.MarginBottom =
				GetMeasurement(attributes, "MarginBottom", kMpPerInch, m_conversion);
			pageLayout.MarginInside =
				GetMeasurement(attributes, "MarginInside", kMpPerInch, m_conversion);
			pageLayout.MarginOutside =
				GetMeasurement(attributes, "MarginOutside", kMpPerInch, m_conversion);
			pageLayout.PosHeader =
				GetMeasurement(attributes, "PositionHeader", (int)(0.75 * kMpPerInch), m_conversion);
			pageLayout.PosFooter =
				GetMeasurement(attributes, "PositionFooter", (int)(0.75 * kMpPerInch), m_conversion);

			pageLayout.IsBuiltIn = true;
			pageLayout.IsModified = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init the given Header/Footer Set by reading the given header/footer set XML node.
		/// </summary>
		/// <param name="hfSet">the PubHFSet object to initialize</param>
		/// <param name="hfSetNode">the given header/footer set XML node to read.</param>
		/// <param name="fMustInitFirstHf">if true, the First h and f must be initialized</param>
		/// <param name="fMustInitEvenHf">if true, the Even h and f must be initialized</param>
		/// <remarks>If the node is null, or any child nodes are absent, the results are:
		/// defaultHeader and defaultFooter will always be initialized. Other headers and footers
		/// are initialized if the fMustInit params are true, otherwise set to null.</remarks>
		/// ------------------------------------------------------------------------------------
		private void ReadHeaderFooterSet(IPubHFSet hfSet, XmlNode hfSetNode,
			bool fMustInitFirstHf, bool fMustInitEvenHf)
		{
			//Set defaults, in case any nodes are missing
			hfSet.Name = "default headers & footers";
			XmlNode defaultHeaderNode = null;
			XmlNode defaultFooterNode = null;
			XmlNode firstHeaderNode = null;
			XmlNode firstFooterNode = null;
			XmlNode evenHeaderNode = null;
			XmlNode evenFooterNode = null;

			//Gather all info in the h/f settings node, if it exists
			if (hfSetNode != null)
			{
				XmlAttributeCollection attributes = hfSetNode.Attributes;
				string name = GetString(attributes, "Name");
				if (name != null)
					hfSet.Name = name;
				string description = GetString(attributes, "Description");
				if (description != null)
				{
					// We'll build a TsString to populate it.
					hfSet.Description =
						m_scr.Cache.TsStrFactory.MakeString(description, m_defUserWs);
				}

				//get the header/footer xml nodes, if they exist
				defaultHeaderNode = hfSetNode.SelectSingleNode("DefaultHeader");
				defaultFooterNode = hfSetNode.SelectSingleNode("DefaultFooter");
				firstHeaderNode = hfSetNode.SelectSingleNode("FirstHeader");
				firstFooterNode = hfSetNode.SelectSingleNode("FirstFooter");
				evenHeaderNode = hfSetNode.SelectSingleNode("EvenHeader");
				evenFooterNode = hfSetNode.SelectSingleNode("EvenFooter");
			}

			// read the default header - required
			hfSet.DefaultHeaderOA = m_cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
			ReadHeaderOrFooter(hfSet.DefaultHeaderOA, defaultHeaderNode);

			// read the default footer - required
			hfSet.DefaultFooterOA = m_cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
			ReadHeaderOrFooter(hfSet.DefaultFooterOA, defaultFooterNode);

			// first header & footer are optional
			if (firstHeaderNode != null || firstFooterNode != null || fMustInitFirstHf)
			{
				hfSet.FirstHeaderOA = m_cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
				ReadHeaderOrFooter(hfSet.FirstHeaderOA, firstHeaderNode);
				hfSet.FirstFooterOA = m_cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
				ReadHeaderOrFooter(hfSet.FirstFooterOA, firstFooterNode);
			}
			else
			{
				hfSet.FirstHeaderOA = null;
				hfSet.FirstFooterOA = null;
			}

			// even header & footer are optional
			if (evenHeaderNode != null || evenFooterNode != null || fMustInitEvenHf)
			{
				hfSet.EvenHeaderOA = m_cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
				ReadHeaderOrFooter(hfSet.EvenHeaderOA, evenHeaderNode);
				hfSet.EvenFooterOA = m_cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
				ReadHeaderOrFooter(hfSet.EvenFooterOA, evenFooterNode);
			}
			else
			{
				hfSet.EvenHeaderOA = null;
				hfSet.EvenFooterOA = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init the given Header or Footer by reading the given header/footer XML node.
		/// </summary>
		/// <param name="headerFooter">the header or footer object to initialize</param>
		/// <param name="hfNode">the given header/footer XML node to read.</param>
		/// <remarks>If the node is null, the header/footer will be initialized to empty
		/// strings.</remarks>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void ReadHeaderOrFooter(IPubHeader headerFooter, XmlNode hfNode)
		{
			//Set defaults, in case any nodes are missing
			// for now, null will suffice as default strings
			XmlNodeList elementNodesOutside = null;
			XmlNodeList elementNodesCentered = null;
			XmlNodeList elementNodesInside = null;

			//Gather all info in the h/f node, if it exists
			if (hfNode != null)
			{
				//get the Outside element xml nodes, if they exist
				//note: if the nodes don't exist, the result is an empty XmlNodeList
				elementNodesOutside = hfNode.SelectNodes("Outside/Element");
				//get the Centered element xml nodes, if they exist
				elementNodesCentered = hfNode.SelectNodes("Center/Element");
				//get the Inside element xml nodes, if they exist
				elementNodesInside = hfNode.SelectNodes("Inside/Element");
			}

			//Read the Outside Elements, if they exist
			headerFooter.OutsideAlignedText =
				ReadHfElements(elementNodesOutside);
			//Read the Centered Elements, if they exist
			headerFooter.CenteredText =
				ReadHfElements(elementNodesCentered);
			//Read the Inside Elements, if they exist
			headerFooter.InsideAlignedText =
				ReadHfElements(elementNodesInside);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a TsString which concatenates the header/footer elements listed in the
		/// given xml node list.
		/// </summary>
		/// <param name="hfElementNodes">the list of header/footer element nodes</param>
		/// <returns>the resulting TsString</returns>
		/// <remarks>If there are no xml nodes in the list, or the list is null,
		/// an empty tss is returned.
		/// Returned tss will use the UI writing system for ORCs and empty stings. We probably
		/// can't know the real vernacular WS yet, so the UI WS is the best temporary value.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected ITsString ReadHfElements(XmlNodeList hfElementNodes)
		{
			// Build the tss from the xml nodes
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			if (hfElementNodes != null)
			{
				foreach(XmlNode elementNode in hfElementNodes)
				{
					XmlAttributeCollection attributes = elementNode.Attributes;
					string sType = GetString(attributes, "type");
					if (sType != null)
					{
						if (sType != string.Empty)
						{
							// Append the ORC for this type
							Guid guid = Guid.Empty;
							if (sType == "FirstReference")
								guid = HeaderFooterVc.FirstReferenceGuid;
							else if (sType == "LastReference")
								guid = HeaderFooterVc.LastReferenceGuid;
							else if (sType == "PageNumber")
								guid = HeaderFooterVc.PageNumberGuid;
							else if (sType == "TotalPageCount")
								guid = HeaderFooterVc.TotalPagesGuid;
							else if (sType == "PrintDate")
								guid = HeaderFooterVc.PrintDateGuid;
							else if (sType == "DivisionName")
								guid = HeaderFooterVc.DivisionNameGuid;
							else if (sType == "PublicationTitle")
								guid = HeaderFooterVc.PublicationTitleGuid;
							else if (sType == "PageReference")
								guid = HeaderFooterVc.PageReferenceGuid;
							else if (sType == "ProjectName")
								guid = HeaderFooterVc.ProjectNameGuid;

							if (guid != Guid.Empty)
							{
								byte[] objData =
									TsStringUtils.GetObjData(guid,
									(byte)FwObjDataTypes.kodtContextString);
								ITsPropsBldr propsBldr =
									TsPropsBldrClass.Create();
								propsBldr.SetStrPropValueRgch(
									(int)FwTextPropType.ktptObjData, objData,
									objData.Length);
								// Use the UI writing system for ORCs. See remarks above.
								// REVIEW: Do we need/want a WS set for this? Things that
								// actually generate strings with words (e.g., publication
								// titles) will usually want to display in the vernacular. Do we
								// need to support different WS's for each guid, or maybe
								// different guids for each WS (i.e., one for User, one for
								// default anal, and one for vern)?
								propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
									0, m_defUserWs);
								strBldr.Replace(strBldr.Length, strBldr.Length,
									new string(StringUtils.kChObject, 1),
									propsBldr.GetTextProps());
							}
						}
					}
					else
					{
						//append literal text in the given writing system
						int ws = GetWritingSystem(attributes, "ws", m_defUserWs);
						string sLiteral = elementNode.InnerText; //get the literal text
						strBldr.Replace(strBldr.Length, strBldr.Length,
							sLiteral, StyleUtils.CharStyleTextProps(null, ws));
					}
				}
			}

			//Return the accumulated structured string
			if (strBldr.Length > 0)
				return strBldr.GetString();
			else
				return EmptyTsString;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create a HeaderFooterSet for each HeaderFooterSet node in the given xml node list.
		/// </summary>
		/// <param name="progressDlg">Progress dialog</param>
		/// <param name="hfSetNodes">the xml nodes to read</param>
		/// -------------------------------------------------------------------------------------
		protected void CreateHfSets(IProgress progressDlg, XmlNodeList hfSetNodes)
		{
			//create each HeaderFooterSet
			foreach (XmlNode hfSetNode in hfSetNodes)
			{
				progressDlg.Step(0);

				IPubHFSet hfSet = m_scr.FindHeaderFooterSetByName(GetString(hfSetNode.Attributes, "Name"));
				if (hfSet == null)
				{
					hfSet = m_cache.ServiceLocator.GetInstance<IPubHFSetFactory>().Create();
					m_scr.HeaderFooterSetsOC.Add(hfSet);
				}
				// ENHANCE(TE-5897): If a user has modified a header/footer set, then changes
				// to it in the factory H/F set should not be applied
				ReadHeaderFooterSet(hfSet, hfSetNode, true, true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks for a header/footer set with the specified name in the DB
		/// </summary>
		/// <param name="name">The name of the header/footer set</param>
		/// <returns>
		/// The header/footer set with the given name if it was found, null otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private IPubHFSet GetHfSetFromDB(string name)
		{
			foreach (IPubHFSet hfSet in m_scr.HeaderFooterSetsOC)
			{
				if (hfSet.Name == name)
					return hfSet;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an empty TsString, to be used if there are no element nodes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ITsString EmptyTsString
		{
			get
			{
				if (m_tssEmpty == null)
					m_tssEmpty = m_scr.Cache.TsStrFactory.MakeString(string.Empty, m_defUserWs);
				return m_tssEmpty;
			}
		}
		#endregion

		#region GetPubPageSizes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves a list of publication page sizes which are allowable for the given type of
		/// publication.
		/// </summary>
		/// <param name="publicationName">Name of the publication whose supported sizes are to
		/// be retrieved. ENHANCE: Ideally, this should use the id instead of the name because it
		/// is guaranteed to be unique, but currently the Publication doesn't store the id, just
		/// the Name.</param>
		/// <param name="wsId">The UI writing system identifier.</param>
		/// <returns>A list of supported publication page sizes for the given type of
		/// publication.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static List<PubPageInfo> GetPubPageSizes(string publicationName, string wsId)
		{
			TePublicationsInit pubInit = new TePublicationsInit();
			return pubInit.GetPubPageSizes(pubInit.LoadDoc(), publicationName, wsId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves a list of publication page sizes which are allowable for the given type of
		/// publication.
		/// </summary>
		/// <param name="pubsRootNode">The XML node with the factory publication info.</param>
		/// <param name="publicationName">Name of the publication whose supported sizes are to
		/// be retrieved. ENHANCE: Ideally, this should use the id instead of the name because it
		/// is guaranteed to be unique, but currently the Publication doesn't store the id, just
		/// the Name.</param>
		/// <param name="wsId">The UI writing system identifier.</param>
		/// <returns>A list of supported publication page sizes for the given type of
		/// publication.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		protected List<PubPageInfo> GetPubPageSizes(XmlNode pubsRootNode,
			string publicationName, string wsId)
		{
			List<PubPageInfo> pubPageInfo = new List<PubPageInfo>();

			XmlNode pubNode = pubsRootNode.SelectSingleNode("Publications/Publication[@Name='" +
				publicationName + "']");
			// Determine the measurement unit to be used for all measurements in this pub
			m_conversion = GetUnitConversion(pubNode.Attributes, "MeasurementUnits", kMpPerInch);

			foreach (XmlNode pageSizeInfo in
				pubNode.SelectNodes("SupportedPublicationSizes/PublicationPageSize"))
			{
				XmlNode nameNode = pageSizeInfo.SelectSingleNode("Name[@wsId='" + wsId + "']");
				if (nameNode == null)
					nameNode = pageSizeInfo.SelectNodes("Name").Item(0);
				pubPageInfo.Add(new PubPageInfo(nameNode.InnerText,
					GetMeasurement(pageSizeInfo.Attributes, "Height", 0, m_conversion),
					GetMeasurement(pageSizeInfo.Attributes, "Width", 0, m_conversion)));
			}
			return pubPageInfo;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the required DTD version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string DtdRequiredVersion
		{
			get { return "61A20AFA-56A9-4717-9014-0CBF99C9F368"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the root element in the XmlDocument that contains the root element that
		/// has the DTDVer attribute.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string RootNodeName
		{
			get { return "PublicationDefaultsForNewProject"; }
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract property to get the relative path to publication
		/// configuration file from the FieldWorks install folder.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override string ResourceFilePathFromFwInstall
		{
			get { return Path.DirectorySeparatorChar + FwDirectoryFinder.ksTeFolderName +
				Path.DirectorySeparatorChar + ResourceFileName; }
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Required implementation of abstract property to get the name of the publication
		/// configuration file.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		protected override string ResourceName
		{
			get { return "TePublications"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the resource list in which the CmResources are owned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IFdoOwningCollection<ICmResource> ResourceList
		{
			get { return m_scr.ResourcesOC; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FdoCache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache Cache
		{
			get { return m_scr.Cache; }
		}
		#endregion

		#region methods to read XML attributes of publication info
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the measurement units for publications and returns a multiplier for
		/// converting into millipoints.
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="attrName">name of the attribute</param>
		/// <param name="defaultVal">a default conversion factor, to be used if the specified
		/// attribute is not present in the XML</param>
		/// <returns>the multiplier for converting into millipoints</returns>
		/// -------------------------------------------------------------------------------------
		protected double GetUnitConversion(XmlAttributeCollection attributes, string attrName,
			double defaultVal)
		{
			XmlNode measUnitsNode = attributes.GetNamedItem(attrName);

			if (measUnitsNode != null)
			{
				string sMeasUnits = measUnitsNode.Value;
				switch (sMeasUnits.ToLowerInvariant())
				{
					case "inch":
						return kMpPerInch;

					case "cm":
						return kMpPerCm;

					default:
						{
							string message;
#if DEBUG
							message = "Error reading TePublications.xml: Unrecognized units for "
								+ attrName + "=\"" + measUnitsNode.Value + "\"";
#else
					message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
							throw new Exception(message);
						}
				}
			}
			else
				return defaultVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height and width of the page.
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="pub">The publication object in which the height and width are to be
		/// stored.</param>
		/// <param name="supportedPubPageSizes">The supported publication page sizes.</param>
		/// ------------------------------------------------------------------------------------
		private void GetPageHeightAndWidth(XmlAttributeCollection attributes, IPublication pub,
			XmlNode supportedPubPageSizes)
		{
			XmlNode pageSize = attributes.GetNamedItem("PageSize");

			if (pageSize != null)
			{
				string sPageSize = pageSize.Value;

				try
				{
					XmlNode pubPageSize =
						supportedPubPageSizes.SelectSingleNode("PublicationPageSize[@id='" + sPageSize + "']");
					pub.PageHeight = GetMeasurement(pubPageSize.Attributes, "Height", 0, m_conversion);
					pub.PageWidth = GetMeasurement(pubPageSize.Attributes, "Width", 0, m_conversion);
				}
				catch
				{
#if DEBUG
					throw new Exception("Error reading TePublications.xml: Problem reading PageSize. value was \"" +
						sPageSize + "\"");
#else
					pub.PageHeight = 0;
					pub.PageWidth = 0;
#endif
				}
			}
			else
			{
				pub.PageHeight = 0;
				pub.PageWidth = 0;
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the specified measurement attribute and computes the value in millipoints
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="attrName">name of the attribute</param>
		/// <param name="defaultVal">a default measurement in millipoints, to be used if
		/// no value is present in the XML</param>
		/// <param name="conversion">multiplier (based on the publication units) to convert
		/// retrieved value into millipoints</param>
		/// <returns>the integer measurement in millipoints from the named item</returns>
		/// -------------------------------------------------------------------------------------
		private int GetMeasurement(XmlAttributeCollection attributes, string attrName,
			int defaultVal, double conversion)
		{
			double measurementInSourceUnits;
			XmlNode measurementAttrib = attributes.GetNamedItem(attrName);

			if (measurementAttrib != null && measurementAttrib.Value != string.Empty)
			{
				try
				{
					// Always read measurements with "en" culture because they have been formatted
					// in that way.
					measurementInSourceUnits = Double.Parse(measurementAttrib.Value,
						CultureInfo.CreateSpecificCulture("en"));
				}
				catch
				{
					string message;
#if DEBUG
					message = "Error reading TePublications.xml: Unrecognized measurement for "
						+ attrName + "=\"" + measurementAttrib.Value + "\"";
#else
					message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
					throw new Exception(message);
				}
			}
			else
				return defaultVal;
			return (int)(measurementInSourceUnits * conversion);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the string value from the named attribute.
		/// If named item is not present, null is returned.
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="attrName">name of the attribute</param>
		/// <returns>the string value, or null</returns>
		/// -------------------------------------------------------------------------------------
		protected static string GetString(XmlAttributeCollection attributes, string attrName)
		{
			XmlNode attrib = attributes.GetNamedItem(attrName);
			if (attrib != null)
				return attrib.Value;
			else
				return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the integer value from the named attribute.
		/// If named item is not present, the default value is returned.
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="attrName">name of the attribute</param>
		/// <param name="defaultVal">The default value.</param>
		/// <returns>the integer value, or null</returns>
		/// ------------------------------------------------------------------------------------
		protected static int GetInt(XmlAttributeCollection attributes, string attrName,
			int defaultVal)
		{
			XmlNode attrib = attributes.GetNamedItem(attrName);
			if (attrib != null && attrib.Value != string.Empty)
			{
				try
				{
					return System.Int32.Parse(attrib.Value);
				}
				catch
				{
					string message;
#if DEBUG
					message = "Error reading TePublications.xml: Unrecognized integer attribute: "
						+ attrName + "=\"" + attrib.Value + "\"";
#else
					message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
					throw new Exception(message);
				}
			}
			else
				return defaultVal;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the bool value from the named attribute.
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="attrName">name of the attribute</param>
		/// <param name="defaultVal">default value if no value is present in the XML</param>
		/// <returns>the bool value</returns>
		/// -------------------------------------------------------------------------------------
		protected bool GetBoolean(XmlAttributeCollection attributes, string attrName,
			bool defaultVal)
		{
			XmlNode boolAttrib = attributes.GetNamedItem(attrName);

			if (boolAttrib != null && boolAttrib.Value != string.Empty)
			{
				try
				{
					return System.Boolean.Parse(boolAttrib.Value);
				}
				catch
				{
					string message;
#if DEBUG
					message = "Error reading TePublications.xml: Unrecognized boolean attribute: "
						+ attrName + "=\"" + boolAttrib.Value + "\"";
#else
					message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
					throw new Exception(message);
				}
			}
			else
				return defaultVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sheet layout.
		/// </summary>
		/// <param name="attributes">The attributes.</param>
		/// <param name="defaultMultiPageLayout">The default multi page layout, whether simplex,
		/// duplex or booklet.</param>
		/// <returns>multi page layout value from the XML file, or the default value if not
		/// specified</returns>
		/// ------------------------------------------------------------------------------------
		private MultiPageLayout GetSheetLayout(XmlAttributeCollection attributes,
			MultiPageLayout defaultMultiPageLayout)
		{
			XmlNode sheetLayoutAttrib = attributes.GetNamedItem("SheetLayout");

			if (sheetLayoutAttrib != null && sheetLayoutAttrib.Value != string.Empty)
			{
				try
				{
					return (MultiPageLayout)Enum.Parse(typeof(MultiPageLayout),
						sheetLayoutAttrib.Value, true);
				}
				catch
				{
					string message;
#if DEBUG
					message = "Error reading TePublications.xml: Unrecognized SheetLayout attribute: " +
						"SheetLayout=\"" + sheetLayoutAttrib.Value + "\"";
#else
					message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
					throw new Exception(message);
				}
			}
			return defaultMultiPageLayout;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a GutterLocation from the named attribute.
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="attrName">name of the attribute</param>
		/// <param name="defaultVal">a default to be used if no value present in the XML</param>
		/// <returns>the GutterLocation enum</returns>
		/// -------------------------------------------------------------------------------------
		protected BindingSide GetGutterLoc(XmlAttributeCollection attributes,
			string attrName, BindingSide defaultVal)
		{
			XmlNode gutterLocAttrib = attributes.GetNamedItem(attrName);

			if (gutterLocAttrib != null && gutterLocAttrib.Value != string.Empty)
			{
				try
				{
					return (BindingSide)Enum.Parse(typeof(BindingSide), gutterLocAttrib.Value, true);
				}
				catch
				{
					string message;
#if DEBUG
					message = "Error reading TePublications.xml: Unrecognized GutterLocation attribute: "
						+ attrName + "=\"" + gutterLocAttrib.Value + "\"";
#else
					message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
					throw new Exception(message);
				}
			}
			else
				return defaultVal;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a DivisionStartOption from the named attribute.
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="attrName">name of the attribute</param>
		/// <param name="defaultVal">a default to be used if no value present in the XML</param>
		/// <returns>the DivisionStartOption enum</returns>
		/// -------------------------------------------------------------------------------------
		protected DivisionStartOption GetDivisionStart(XmlAttributeCollection attributes,
			string attrName, DivisionStartOption defaultVal)
		{
			XmlNode divStartAttrib = attributes.GetNamedItem(attrName);

			if (divStartAttrib != null && divStartAttrib.Value != string.Empty)
			{
				try
				{
					return (DivisionStartOption)Enum.Parse(typeof(DivisionStartOption),
						divStartAttrib.Value, true);
				}
				catch
				{
					string message;
#if DEBUG
					message = "Error reading TePublications.xml: Unrecognized DivisionStartOption attribute: "
						+ attrName + "=\"" + divStartAttrib.Value + "\"";
#else
					message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
					throw new Exception(message);
				}
			}
			else
				return defaultVal;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a writing system ID from the named attribute
		/// </summary>
		/// <param name="attributes">collection of XML attributes for current tag</param>
		/// <param name="attrName">name of the attribute</param>
		/// <param name="defaultVal">a default to be used if no value present in the XML</param>
		/// <returns>the writing system ID integer</returns>
		/// -------------------------------------------------------------------------------------
		protected int GetWritingSystem(XmlAttributeCollection attributes,
			string attrName, int defaultVal)
		{
			XmlNode WsAttrib = attributes.GetNamedItem(attrName);

			if (WsAttrib != null && WsAttrib.Value != string.Empty)
			{
				ILgWritingSystemFactory wsf = m_scr.Cache.WritingSystemFactory;
				int wsResult = wsf.GetWsFromStr(WsAttrib.Value);

				if (wsResult != 0)
					return wsResult;

#if DEBUG
				string message = "Error reading TePublications.xml: Unrecognized writing system attribute: "
					+ attrName + "=\"" + WsAttrib.Value + "\"" + Environment.NewLine + " Default writing system will be used.";
#else
				string message = TeResourceHelper.GetResourceString("kstidInvalidInstallation");
#endif
				throw new XmlSyntaxException("Invalid Writing System attribute found for " + attrName);
			}
			return defaultVal;
		}
		#endregion
	}
}
