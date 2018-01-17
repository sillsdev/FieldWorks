// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Areas.Grammar.Tools.PhonemeEdit
{
	internal class BasicIPASymbolSlice : StringSlice
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
		public BasicIPASymbolSlice(LcmCache cache, string editor, int flid, XElement node, ICmObject obj, IPersistenceProvider persistenceProvider, int ws)
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
			{
				return;
			}

			var fADescriptionChanged = false;
			foreach (var writingSystem in m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
			{
				var ws = writingSystem.Handle;
				var tssDesc = phoneme.Description.get_String(ws);
				var sDesc = tssDesc.Text;
				if (!string.IsNullOrEmpty(sDesc) && !m_justChangedDescription)
				{
					continue;
				}
				XElement description = null;
				if (phoneme.BasicIPASymbol.Length > 0)
				{
					var sLocale = writingSystem.Id;
					// Mono XPath processing crashes when the expression starts out with // here.  See FWNX-730.
					var sXPath = "/SegmentDefinitions/SegmentDefinition[Representations/Representation[.='" +
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
				var sXPath = "/SegmentDefinitions/SegmentDefinition[Representations/Representation[.='" +
					XmlUtils.MakeSafeXmlAttribute(phoneme.BasicIPASymbol.Text) +
					"']]/Features";
				var features = s_ipaInfoDocument.XPathSelectElement(sXPath);
				if (features == null)
				{
					return;
				}
				foreach (var feature in features.Elements("FeatureValuePair"))
				{
					var sFeature = feature.Attribute("feature").Value;
					var sValue = feature.Attribute("value").Value;
					var featDefn = m_cache.LanguageProject.PhFeatureSystemOA.GetFeature(sFeature);
					if (featDefn == null)
					{
						continue;
					}

					var symVal = m_cache.LanguageProject.PhFeatureSystemOA.GetSymbolicValue(sValue);
					if (symVal == null)
					{
						continue;
					}
					if (phoneme.FeaturesOA == null)
					{
						phoneme.FeaturesOA = m_cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
					}
					var value = m_cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
					phoneme.FeaturesOA.FeatureSpecsOC.Add(value);
					value.FeatureRA = featDefn;
					value.ValueRA = symVal;
					m_justChangedFeatures = true;
				}
			}
			else if (phoneme.BasicIPASymbol.Length == 0 && m_justChangedFeatures)
			{
				phoneme.FeaturesOA?.FeatureSpecsOC.Clear();
				m_justChangedFeatures = true;
			}
			else
			{
				m_justChangedFeatures = false;
			}
		}
	}
}