// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Boundary coverage for the LCModel-backed edit context's write routing and validation: the
	/// citation-form fallback when there is no lexeme form, the no-sense gloss guard, blank/unknown
	/// writing-system and field handling (no silent write to a default), whitespace-only validation,
	/// and commit/cancel idempotency — plus the browse ws-spec normalizer.
	/// </summary>
	[TestFixture]
	public class LexicalEditRegionEditContextEdgeCaseTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry MakeEntryWithLexemeAndSense(string lexeme, string gloss)
		{
			ILexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(lexeme, Cache.DefaultVernWs));
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			});
			return entry;
		}

		private ILexEntry MakeEntryNoLexemeForm(string citation)
		{
			ILexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				entry.CitationForm.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(citation, Cache.DefaultVernWs));
			});
			return entry;
		}

		private ILexEntry MakeEntryNoSense(string lexeme)
		{
			ILexEntry entry = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(lexeme, Cache.DefaultVernWs));
			});
			return entry;
		}

		private static LexicalEditRegionField Field(string field) => new LexicalEditRegionField(
			"test/" + field, field, field, null, RegionFieldKind.Text, EditorClassification.Known,
			null, null, SurfaceRouting.Product, null, null, null);

		[Test]
		public void TrySetText_Gloss_WhenNoSense_IsRejected()
		{
			var entry = MakeEntryNoSense("casa");
			var context = new LexicalEditRegionEditContext(entry, Cache);
			Assert.That(context.TrySetText(Field("Gloss"), "anal", "house"), Is.False,
				"an entry with no sense has no gloss to write");
			Assert.That(context.IsOpen, Is.False, "a rejected write opens no session");
		}

		[Test]
		public void TrySetText_Form_WhenNoLexemeForm_WritesCitationForm()
		{
			var entry = MakeEntryNoLexemeForm("kanji");
			var context = new LexicalEditRegionEditContext(entry, Cache);
			Assert.That(context.TrySetText(Field("Form"), "vern", "kana"), Is.True);
			context.Commit();
			Assert.That(entry.CitationForm.get_String(Cache.DefaultVernWs).Text, Is.EqualTo("kana"),
				"with no lexeme form the write falls back to the citation form (mirrors the read fallback)");
		}

		[Test]
		public void TrySetText_BlankWs_IsRejected_NoSilentDefaultWrite()
		{
			var entry = MakeEntryWithLexemeAndSense("casa", "house");
			var context = new LexicalEditRegionEditContext(entry, Cache);
			Assert.That(context.TrySetText(Field("Form"), "", "x"), Is.False, "a blank ws does not resolve");
			Assert.That(entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text, Is.EqualTo("casa"),
				"no write leaked to the default writing system");
		}

		[Test]
		public void TrySetText_UnknownField_IsRejected()
		{
			var entry = MakeEntryWithLexemeAndSense("casa", "house");
			var context = new LexicalEditRegionEditContext(entry, Cache);
			Assert.That(context.TrySetText(Field("Bogus"), "vern", "x"), Is.False);
		}

		[Test]
		public void Validate_WhitespaceOnlyLexeme_IsAnError()
		{
			var entry = MakeEntryWithLexemeAndSense("casa", "house");
			var context = new LexicalEditRegionEditContext(entry, Cache);
			Assert.That(context.TrySetText(Field("Form"), "vern", "   "), Is.True, "whitespace stages");
			Assert.That(context.Validate(), Is.Not.Empty, "a whitespace-only lexeme form fails validation");
		}

		[Test]
		public void CommitOrCancel_WithoutAnyEdit_AreNoOps()
		{
			var entry = MakeEntryWithLexemeAndSense("casa", "house");
			var context = new LexicalEditRegionEditContext(entry, Cache);
			Assert.That(context.IsOpen, Is.False);
			Assert.DoesNotThrow(() => context.Commit(), "commit without an open session is a no-op");
			Assert.DoesNotThrow(() => context.Cancel(), "cancel without an open session is a no-op");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False, "no undo step was created");
		}

	}
}
