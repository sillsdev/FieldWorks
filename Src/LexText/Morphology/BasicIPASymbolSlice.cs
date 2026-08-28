// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Utils;
using XCore;

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
		/// <param name="persistenceProvider"></param>
		/// <param name="ws"></param>
		public BasicIPASymbolSlice(LcmCache cache, string editor, int flid,
						System.Xml.XmlNode node, ICmObject obj,
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
			foreach (CoreWritingSystemDefinition writingSystem in m_cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
			{
				int ws = writingSystem.Handle;
				ITsString tssDesc = phoneme.Description.get_String(ws);
				string sDesc = tssDesc.Text;
				if (string.IsNullOrEmpty(sDesc) || m_justChangedDescription)
				{
					XElement description = null;
					if (phoneme.BasicIPASymbol.Length > 0)
					{
						string sLocale = writingSystem.Id;
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
		/// Populates or clears the phoneme's features to match the BasicIPASymbol field.
		/// </summary>
		/// <remarks>
		/// WHEN to write belongs here because it depends on slice state: the latch records that
		/// this slice populated the features, which is what lets clearing the symbol clear them
		/// again without discarding features a user set through the chooser. The writing itself
		/// is in PhonemeFeaturePopulator, so the LT-22716 repair can reuse it.
		/// </remarks>
		public void SetFeaturesBasedOnIPA()
		{
			var phoneme = (IPhPhoneme)m_obj;

			if (phoneme.BasicIPASymbol.Length > 0 && (m_justChangedFeatures || phoneme.FeaturesOA == null || phoneme.FeaturesOA.FeatureSpecsOC.Count == 0))
			{
				if (PhonemeFeaturePopulator.ApplyFeaturesFromIpaSymbol(m_cache, phoneme,
						s_ipaInfoDocument) > 0)
				{
					m_justChangedFeatures = true;
				}
			}
			else if (phoneme.BasicIPASymbol.Length == 0 && m_justChangedFeatures)
			{
				// user has cleared the basic IPA symbol; clear the features
				PhonemeFeaturePopulator.ClearFeatures(phoneme);
				m_justChangedFeatures = true;
			}
			else
			{
				m_justChangedFeatures = false;
			}
		}
	}
}
