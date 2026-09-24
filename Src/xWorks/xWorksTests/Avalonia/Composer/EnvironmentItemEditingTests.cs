// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Retyping an environment that is already on an allomorph. An environment's IDENTITY is its
	/// text with spaces stripped, and everything here follows from that one fact: text that
	/// strips the same leaves the reference alone and RENAMES the shared object, text that strips
	/// differently re-points the reference and creates the target only when the project has none.
	///
	/// PhoneEnvReferenceView.ConnectToRealCache is the behaviour being matched, and no test of
	/// its own pins any of it -- these are the first. Each was confirmed by suppressing the
	/// behaviour and watching it fail.
	/// </summary>
	[TestFixture]
	public class EnvironmentItemEditingTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private IMoStemAllomorph m_allomorph;
		private IMoStemAllomorph m_otherAllomorph;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var lexemeForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = lexemeForm;
				lexemeForm.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("barigi", Cache.DefaultVernWs));

				m_allomorph = MakeAllomorph("barigi-one");
				m_otherAllomorph = MakeAllomorph("barigi-two");
			});
		}

		public override void TestTearDown()
		{
			// Editing mints environments too, and NonUndoableUnitOfWorkHelper bypasses UndoAll.
			var inventory = Cache.LanguageProject.PhonologicalDataOA?.EnvironmentsOS;
			if (inventory != null && inventory.Count > 0)
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
					() => { foreach (var env in inventory.ToList()) env.Delete(); });
			}
			base.TestTearDown();
		}

		private IMoStemAllomorph MakeAllomorph(string form)
		{
			var allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_entry.AlternateFormsOS.Add(allomorph);
			allomorph.Form.set_String(Cache.DefaultVernWs,
				TsStringUtils.MakeString(form, Cache.DefaultVernWs));
			return allomorph;
		}

		private IPhEnvironment GiveProjectAnEnvironment(string representation)
			=> GiveProjectAnEnvironment(representation, Cache.DefaultVernWs);

		private IPhEnvironment GiveProjectAnEnvironment(string representation, int ws)
		{
			IPhEnvironment env = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				Cache.LanguageProject.PhonologicalDataOA.EnvironmentsOS.Add(env);
				env.StringRepresentation = TsStringUtils.MakeString(representation, ws);
			});
			return env;
		}

		private void Attach(IMoStemAllomorph allomorph, IPhEnvironment env)
			=> NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => allomorph.PhoneEnvRC.Add(env));

		private int EnvironmentCount
			=> Cache.LanguageProject.PhonologicalDataOA?.EnvironmentsOS.Count ?? 0;

		// Retypes the item naming 'env' on m_allomorph, and commits.
		private bool Retype(IPhEnvironment env, string text)
		{
			var composed = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true);
			var row = composed.Model.Fields.Single(
				f => f.Field == "PhoneEnv" && f.ObjectHvo == m_allomorph.Hvo);
			var editing = (IReferenceTextEditing)composed.EditContext;

			var staged = editing.TrySetReferenceItemText(row, env.Guid.ToString(), text);
			if (staged)
				composed.EditContext.Commit();
			return staged;
		}

		/// <summary>
		/// The row shows what the user typed. The item's display text comes from ShortName,
		/// which is not always the whole of an object's text.
		/// </summary>
		[Test]
		public void AnEnvironmentItem_ShowsItsWholeStringRepresentation()
		{
			var env = GiveProjectAnEnvironment("/ _ zt");
			Attach(m_allomorph, env);

			var composed = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true);
			var row = composed.Model.Fields.Single(
				f => f.Field == "PhoneEnv" && f.ObjectHvo == m_allomorph.Hvo);

			Assert.That(row.Items.Single().Name, Is.EqualTo("/ _ zt"),
				"the item must display the whole environment, not an abbreviated form of it");
		}

		/// <summary>
		/// Environments are commonly stored in an analysis writing system rather than the
		/// vernacular. Retyping one must not retag it: the string is shared, so its font
		/// would change for every allomorph using it and in the dictionary export, while
		/// the row that was edited shows nothing different.
		/// </summary>
		[Test]
		public void RetypingAnEnvironment_KeepsTheWritingSystemItWasStoredIn()
		{
			Assume.That(Cache.DefaultAnalWs, Is.Not.EqualTo(Cache.DefaultVernWs),
				"the fixture needs two distinct writing systems or this cannot fail");
			var stored = GiveProjectAnEnvironment("/_#", Cache.DefaultAnalWs);
			Attach(m_allomorph, stored);

			Assert.That(Retype(stored, "/ _ #"), Is.True);

			var rep = m_allomorph.PhoneEnvRC.Single().StringRepresentation;
			Assert.That(rep.Text, Is.EqualTo("/ _ #"), "precondition: the text was retyped");
			Assert.That(rep.get_WritingSystem(0), Is.EqualTo(Cache.DefaultAnalWs),
				"the environment keeps the writing system it was stored in; forcing the "
				+ "vernacular here changes its font everywhere it is shown");
		}

		/// <summary>
		/// Clearing an item's text removes it, which is what
		/// EnvsBeingRequestedForThisEntry does with a blank line. The environment itself
		/// survives: it is shared, and other allomorphs may still be using it.
		/// </summary>
		[Test]
		public void ClearingAnItemsText_RemovesTheItem_AndKeepsTheEnvironment()
		{
			var shared = GiveProjectAnEnvironment("/_#");
			Attach(m_allomorph, shared);
			Attach(m_otherAllomorph, shared);
			var before = EnvironmentCount;

			Assert.That(Retype(shared, string.Empty), Is.True, "the edit staged");

			Assert.That(m_allomorph.PhoneEnvRC, Is.Empty,
				"an environment whose text the user cleared is no longer on the allomorph");
			Assert.That(EnvironmentCount, Is.EqualTo(before),
				"and clearing creates nothing -- the old path minted an empty environment");
			Assert.That(shared.IsValidObject, Is.True);
			Assert.That(m_otherAllomorph.PhoneEnvRC.Single(), Is.EqualTo(shared),
				"the environment is shared, so removing this reference must not disturb it");
		}

		/// <summary>Trim, not Length: spaces alone are as blank as nothing at all.</summary>
		[Test]
		public void ClearingAnItemToWhitespace_CountsAsCleared()
		{
			var env = GiveProjectAnEnvironment("/_#");
			Attach(m_allomorph, env);
			var before = EnvironmentCount;

			Assert.That(Retype(env, "   "), Is.True);

			Assert.That(m_allomorph.PhoneEnvRC, Is.Empty,
				"whitespace is not an environment; it removes the item like an empty string");
			Assert.That(EnvironmentCount, Is.EqualTo(before),
				"and must not mint an environment whose whole text is spaces");
		}

		/// <summary>
		/// Only the cleared item goes. A row-wide rebuild would drop the others with it.
		/// </summary>
		[Test]
		public void ClearingOneItem_LeavesTheOtherItemsAlone()
		{
			var first = GiveProjectAnEnvironment("/_#");
			var second = GiveProjectAnEnvironment("/_a");
			Attach(m_allomorph, first);
			Attach(m_allomorph, second);

			Assert.That(Retype(first, string.Empty), Is.True);

			Assert.That(m_allomorph.PhoneEnvRC.Single(), Is.EqualTo(second),
				"the item that was not cleared keeps its environment");
		}

		/// <summary>
		/// Case 1, the surprising one. Respacing makes no new environment and does not move the
		/// reference -- it renames the shared object, so every OTHER allomorph referencing it
		/// shows the new spelling too. Asserted from the second allomorph, which is what makes
		/// the project-wide reach visible.
		/// </summary>
		[Test]
		public void RetypingToTheSameStrippedText_RenamesTheSharedEnvironment()
		{
			var shared = GiveProjectAnEnvironment("/_#");
			Attach(m_allomorph, shared);
			Attach(m_otherAllomorph, shared);
			var before = EnvironmentCount;

			Assert.That(Retype(shared, "/ _ #"), Is.True, "the edit staged");

			Assert.That(EnvironmentCount, Is.EqualTo(before),
				"the identity did not change, so nothing was created");
			Assert.That(m_allomorph.PhoneEnvRC.Single(), Is.EqualTo(shared),
				"and the reference did not move");
			Assert.That(m_otherAllomorph.PhoneEnvRC.Single().StringRepresentation.Text,
				Is.EqualTo("/ _ #"),
				"the shared environment was renamed, so the OTHER allomorph shows it too -- this "
				+ "is the project-wide reach of an edit that looks local");
		}

		/// <summary>Case 2: a different identity that the project already has.</summary>
		[Test]
		public void RetypingToAnExistingEnvironment_MovesTheReference_AndKeepsTheOldOne()
		{
			var original = GiveProjectAnEnvironment("/_#");
			var other = GiveProjectAnEnvironment("/_a");
			Attach(m_allomorph, original);
			var before = EnvironmentCount;

			Assert.That(Retype(original, "/_a"), Is.True);

			Assert.That(EnvironmentCount, Is.EqualTo(before), "an existing match is reused");
			Assert.That(m_allomorph.PhoneEnvRC.Single(), Is.EqualTo(other),
				"the reference moved to the environment the typed text names");
			Assert.That(original.IsValidObject, Is.True,
				"and the one it left is untouched -- it stays in the project inventory");
			Assert.That(original.StringRepresentation.Text, Is.EqualTo("/_#"));
		}

		/// <summary>Case 3: an identity the project does not have yet.</summary>
		[Test]
		public void RetypingToAnUnknownEnvironment_CreatesItAndMovesTheReference()
		{
			var original = GiveProjectAnEnvironment("/_#");
			Attach(m_allomorph, original);
			var before = EnvironmentCount;

			Assert.That(Retype(original, "/_zz"), Is.True);

			Assert.That(EnvironmentCount, Is.EqualTo(before + 1), "the project gained one");
			Assert.That(m_allomorph.PhoneEnvRC.Single().StringRepresentation.Text,
				Is.EqualTo("/_zz"));
			Assert.That(original.StringRepresentation.Text, Is.EqualTo("/_#"),
				"the environment it left keeps its own text");
		}

		/// <summary>
		/// Case 4: retyping one item must not steal the environment another item on the same
		/// field is using. Resolution prefers a match this field already carries that no OTHER
		/// item claims, which is the rule that keeps the two apart.
		/// </summary>
		[Test]
		public void RetypingOneItem_DoesNotStealTheEnvironmentAnotherItemUses()
		{
			var first = GiveProjectAnEnvironment("/_#");
			var second = GiveProjectAnEnvironment("/ _ a");
			var third = GiveProjectAnEnvironment("/_a");
			Attach(m_allomorph, first);
			Attach(m_allomorph, second);

			Assert.That(Retype(first, "/_a"), Is.True);

			Assert.That(m_allomorph.PhoneEnvRC, Does.Contain(second),
				"the item that already named that identity keeps its environment");
			Assert.That(m_allomorph.PhoneEnvRC, Does.Contain(third),
				"and the retyped item took the other match rather than colliding");
			Assert.That(m_allomorph.PhoneEnvRC.Count, Is.EqualTo(2),
				"still two items, on two distinct environments");
		}

		/// <summary>
		/// Case 5: malformed text is staged and kept verbatim. PhoneEnvReferenceView
		/// annotates rather than blocking, so nothing here may correct or discard what the
		/// user typed.
		/// </summary>
		[Test]
		public void RetypingToAMalformedEnvironment_IsAcceptedAndKeptVerbatim()
		{
			var original = GiveProjectAnEnvironment("/_#");
			Attach(m_allomorph, original);

			Assert.That(Retype(original, "/ _ ["), Is.True,
				"a malformed environment is still staged -- rejecting it would discard typing");

			Assert.That(m_allomorph.PhoneEnvRC.Single().StringRepresentation.Text,
				Is.EqualTo("/ _ ["),
				"and it is stored exactly as typed, not corrected");
		}

		[Test]
		public void RetypingAnItemTheFieldDoesNotCarry_IsRejected_WithoutOpeningASession()
		{
			var attached = GiveProjectAnEnvironment("/_#");
			var elsewhere = GiveProjectAnEnvironment("/_b");
			Attach(m_allomorph, attached);

			var composed = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true);
			var row = composed.Model.Fields.Single(
				f => f.Field == "PhoneEnv" && f.ObjectHvo == m_allomorph.Hvo);
			var editing = (IReferenceTextEditing)composed.EditContext;

			Assert.That(editing.TrySetReferenceItemText(row, elsewhere.Guid.ToString(), "/_c"),
				Is.False, "the key names no item of this field");
			Assert.That(composed.EditContext.IsOpen, Is.False,
				"and a rejected edit must not leave a session open");
		}
	}
}
