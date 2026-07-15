// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;
using SIL.WritingSystems;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// §19d (voice/sound writing systems): the composer resolves WS rows, and a voice/audio (IsVoice)
	/// writing system stores a recording (its filename) rather than text. The alternative is flagged
	/// IsAudio and composes its REAL filename on an EDITABLE row — the owned audio field renders
	/// play/record/clear affordances and writes/clears the filename through the text setter. It is no
	/// longer a blanket read-only placeholder.
	/// </summary>
	[TestFixture]
	public class FullEntryRegionVoiceWsTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private CoreWritingSystemDefinition m_audioWs;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				// A voice/audio writing system, added to the vernacular set so multi-vernacular text
				// fields (e.g. Citation Form) compose a row for it.
				var wsManager = Cache.ServiceLocator.WritingSystemManager;
				var language = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Language;
				m_audioWs = wsManager.Create(language, WellKnownSubtags.AudioScript, null,
					new VariantSubtag[] { WellKnownSubtags.AudioPrivateUse });
				m_audioWs.IsVoice = true;
				wsManager.Set(m_audioWs);
				Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(m_audioWs);

				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				// Give the citation form an audio "recording" (its file name) in the voice WS.
				m_entry.CitationForm.set_String(m_audioWs.Handle,
					TsStringUtils.MakeString("casa.wav", m_audioWs.Handle));
			});
		}

		[Test]
		public void Compose_VoiceWritingSystem_ComposesAnEditableAudioRow_CarryingTheRealFilename()
		{
			Assert.That(m_audioWs.IsVoice, Is.True, "precondition: the writing system is a voice WS");

			var composed = FullEntryRegionComposer.Compose(m_entry, Cache);
			Assert.That(composed, Is.Not.Null);

			// Find a composed text row that carries a value for the voice writing system.
			var audioValues = composed.Model.Fields
				.Where(f => f.Kind == RegionFieldKind.Text)
				.SelectMany(f => f.Values.Select(v => new { Field = f, Value = v }))
				.Where(x => x.Value.WsTag == m_audioWs.Id)
				.ToList();

			Assert.That(audioValues, Is.Not.Empty,
				"the composer must surface the voice writing system's alternative, not drop it");

			// Every voice-WS alternative is flagged IsAudio (so the view renders play/record/clear) and sits
			// on an EDITABLE row (record/clear write the filename through the text setter). §19d removed the
			// blanket read-only placeholder.
			foreach (var x in audioValues)
			{
				Assert.That(x.Value.IsAudio, Is.True,
					"a voice WS alternative is flagged IsAudio so the view renders play/record affordances");
				Assert.That(x.Value.Value, Is.Not.EqualTo(FwAvaloniaStrings.AudioRecordingReadOnly),
					"§19d: the value is the REAL recording filename (or empty), never the old read-only placeholder");
				Assert.That(x.Field.IsEditable, Is.True,
					"§19d: the audio row is editable — record/clear write the filename through the text setter");
			}

			// The Citation Form's voice alternative carries the actual recording filename (so play/clear work).
			Assert.That(audioValues.Any(x => x.Value.Value == "casa.wav"), Is.True,
				"§19d: the field with a recording carries its real filename, not a placeholder");
		}
	}
}
