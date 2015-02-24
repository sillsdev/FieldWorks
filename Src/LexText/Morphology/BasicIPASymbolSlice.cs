using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	public class BasicIPASymbolSlice : StringSlice
	{
		private static readonly XDocument s_ipaInfoDocument;

		static BasicIPASymbolSlice()
		{
			s_ipaInfoDocument = XDocument.Load(Path.Combine(FwDirectoryFinder.TemplateDirectory, PhPhonemeTags.ksBasicIPAInfoFile));
		}

		private bool m_justChangedDescription;
		private bool m_justChangedFeatures;

		/// <summary>
		/// Constructor invoked via the editor="customWithParams" slice XML configuration
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="editor"></param>
		/// <param name="flid"></param>
		/// <param name="node"></param>
		/// <param name="obj"></param>
		/// <param name="stringTbl"></param>
		/// <param name="persistenceProvider"></param>
		/// <param name="ws"></param>
		public BasicIPASymbolSlice(FdoCache cache, string editor, int flid,
						System.Xml.XmlNode node, ICmObject obj, StringTable stringTbl,
						IPersistenceProvider persistenceProvider, int ws)
			: base(obj, flid, ws)
		{
			var phoneme = (IPhPhoneme)m_obj;
			phoneme.BasicIPASymbolChanged += UpdatePhoneme;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				var phoneme = (IPhPhoneme)m_obj;
				phoneme.BasicIPASymbolChanged -= UpdatePhoneme;
			}

			base.Dispose(disposing);
		}

		private void UpdatePhoneme(object sender, EventArgs e)
		{
			SetDescriptionBasedOnIPA();
			SetFeaturesBasedOnIPA();
		}

		/// <summary>
		/// Set description based on the content of the BasicIPASymbol field and the BasicIPAInfo document
		/// </summary>
		private void SetDescriptionBasedOnIPA()
		{
			var phoneme = (IPhPhoneme) m_obj;
			if (!m_justChangedDescription && phoneme.BasicIPASymbol.Length == 0)
				return;

			bool fADescriptionChanged = false;
			foreach (WritingSystem writingSystem in m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
			{
				int ws = writingSystem.Handle;
				ITsString tssDesc = phoneme.Description.get_String(ws);
				string sDesc = tssDesc.Text;
				if (string.IsNullOrEmpty(sDesc) || m_justChangedDescription)
				{
					XElement description = null;
					if (phoneme.BasicIPASymbol.Length > 0)
					{
						string sLocale = writingSystem.ID;
						// Mono XPath processing crashes when the expression starts out with // here.  See FWNX-730.
						string sXPath = "/SegmentDefinitions/SegmentDefinition[Representations/Representation[.='" +
							XmlUtils.MakeSafeXmlAttribute(phoneme.BasicIPASymbol.Text) +
							"']]/Descriptions/Description[@lang='" + sLocale + "']";
						description = s_ipaInfoDocument.XPathSelectElement(sXPath);
					}
					if (description != null)
					{
						phoneme.Description.set_String(ws, (string)description);
						fADescriptionChanged = true;
					}
					else if (phoneme.BasicIPASymbol.Length == 0)
					{
						phoneme.Description.set_String(ws, "");
						fADescriptionChanged = true;
					}
				}
			}
			m_justChangedDescription = fADescriptionChanged;
		}

		/// <summary>
		/// Set description based on the content of the BasicIPASymbol field and the BasicIPAInfo document
		/// </summary>
		public void SetFeaturesBasedOnIPA()
		{
			var phoneme = (IPhPhoneme)m_obj;

			if (phoneme.BasicIPASymbol.Length > 0 && (m_justChangedFeatures || phoneme.FeaturesOA == null || phoneme.FeaturesOA.FeatureSpecsOC.Count == 0))
			{
				// Mono XPath processing crashes when the expression starts out with // here.  See FWNX-730.
				string sXPath = "/SegmentDefinitions/SegmentDefinition[Representations/Representation[.='" +
					XmlUtils.MakeSafeXmlAttribute(phoneme.BasicIPASymbol.Text) +
					"']]/Features";
				XElement features = s_ipaInfoDocument.XPathSelectElement(sXPath);
				if (features != null)
				{
					bool fCreatedNewFS = false;
					foreach (XElement feature in features.Elements("FeatureValuePair"))
					{
						var sFeature = (string) feature.Attribute("feature");
						var sValue = (string) feature.Attribute("value");
						IFsFeatDefn featDefn = m_cache.LanguageProject.PhFeatureSystemOA.GetFeature(sFeature);
						if (featDefn == null)
							continue;

						IFsSymFeatVal symVal = m_cache.LanguageProject.PhFeatureSystemOA.GetSymbolicValue(sValue);
						if (symVal == null)
							continue;
						if (phoneme.FeaturesOA == null)
						{
							phoneme.FeaturesOA = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
							fCreatedNewFS = true;
						}
						IFsClosedValue value = m_cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
						phoneme.FeaturesOA.FeatureSpecsOC.Add(value);
						value.FeatureRA = featDefn;
						value.ValueRA = symVal;
						m_justChangedFeatures = true;
					}
				}
			}
			else if (phoneme.BasicIPASymbol.Length == 0 && m_justChangedFeatures)
			{
				if (phoneme.FeaturesOA != null)
					// user has cleared the basic IPA symbol; clear the features
					phoneme.FeaturesOA.FeatureSpecsOC.Clear();
				m_justChangedFeatures = true;
			}
			else
			{
				m_justChangedFeatures = false;
			}
		}
	}
}
