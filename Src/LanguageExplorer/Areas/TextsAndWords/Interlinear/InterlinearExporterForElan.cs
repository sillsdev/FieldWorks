// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This handles exporting interlinear data into an xml format that is friendly to ELAN's overlapping time sequences.
	/// (LT-9904)
	/// </summary>
	internal class InterlinearExporterForElan : InterlinearExporter
	{
		private const int kDocVersion = 2;
		protected internal InterlinearExporterForElan(LcmCache cache, XmlWriter writer, ICmObject objRoot, InterlinLineChoices lineChoices, InterlinVc vc)
			: base(cache, writer, objRoot, lineChoices, vc)
		{
		}

		public override void WriteBeginDocument()
		{
			base.WriteBeginDocument();
			m_writer.WriteAttributeString("version", kDocVersion.ToString());
		}

		protected override void WriteStartParagraph(int hvo)
		{
			base.WriteStartParagraph(hvo);
			WriteGuidAttributeForObj(hvo);
		}

		protected override void WriteStartPhrase(int hvo)
		{
			base.WriteStartPhrase(hvo);
			WriteGuidAttributeForObj(hvo);
			var phrase = m_repoObj.GetObject(hvo) as ISegment;
			if (phrase?.MediaURIRA == null)
			{
				return;
			}
			m_writer.WriteAttributeString("begin-time-offset", phrase.BeginTimeOffset);
			m_writer.WriteAttributeString("end-time-offset", phrase.EndTimeOffset);
			if (phrase.SpeakerRA != null)
			{
				m_writer.WriteAttributeString("speaker", phrase.SpeakerRA.Name.BestVernacularAlternative.Text);
			}
			m_writer.WriteAttributeString("media-file", phrase.MediaURIRA.Guid.ToString());
		}

		protected override void WriteStartWord(int hvo)
		{
			base.WriteStartWord(hvo);
			// Note that this guid may well not be unique in the file, since it refers to a
			// WfiWordform, WfiAnalysis, WfiGloss, or PunctuationForm (the last is not output),
			// any of which may be referred to repeatedly in an analyzed text.
			if (m_repoObj.GetClsid(hvo) != PunctuationFormTags.kClassId)
			{
				WriteGuidAttributeForObj(hvo);
			}
		}
	}
}