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
	/// ITEM 3 (voice/sound writing systems): the composer resolves WS rows, and a voice/audio (IsVoice)
	/// writing system stores a recording rather than text. With no Avalonia sound player yet, such an
	/// alternative must compose as a READ-ONLY row carrying an explicit audio placeholder — visible and
	/// diagnosable — instead of a blank EDITABLE box whose first keystroke would corrupt the recording.
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
		public void Compose_VoiceWritingSystem_ComposesAReadOnlyAudioPlaceholderRow_NotAnEmptyEditableRow()
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

			foreach (var x in audioValues)
			{
				Assert.That(x.Value.IsAudio, Is.True,
					"a voice WS alternative is flagged IsAudio (read-only audio placeholder)");
				Assert.That(x.Value.Value, Is.EqualTo(FwAvaloniaStrings.AudioRecordingReadOnly),
					"it shows the localized audio placeholder, not a blank/raw value");
				Assert.That(x.Field.IsEditable, Is.False,
					"the row holding a voice alternative is read-only (no blank editable box that would corrupt the recording)");
			}
		}
	}
}
