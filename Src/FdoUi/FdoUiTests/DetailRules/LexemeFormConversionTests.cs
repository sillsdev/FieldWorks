// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The lexeme-form commands: when a swap or a conversion is offered, what each does to
	/// the entry in one undoable step, and the warning a conversion needs.
	/// </summary>
	[TestFixture]
	public class LexemeFormConversionTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private IMoStemAllomorph _lexemeForm;
		private IMoStemAllomorph _allomorph;

		// A stem entry whose lexeme form has one allomorph.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				_lexemeForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.LexemeFormOA = _lexemeForm;
				SetForm(_lexemeForm, "casa");
				_allomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.AlternateFormsOS.Add(_allomorph);
				SetForm(_allomorph, "cas");
			});
		}

		private void SetForm(IMoForm form, string text)
			=> form.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(text, Cache.DefaultVernWs));

		[Test]
		public void CanSwap_NeedsALexemeForm_AndAnAllomorph()
		{
			Assert.That(LexemeFormConversion.CanSwap(_entry), Is.True);
			Assert.That(LexemeFormConversion.CanSwap(null), Is.False);

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _entry.AlternateFormsOS.Clear());
			Assert.That(LexemeFormConversion.CanSwap(_entry), Is.False, "nothing to swap with");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry.AlternateFormsOS.Add(Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create());
				_entry.LexemeFormOA = null;
			});
			Assert.That(LexemeFormConversion.CanSwap(_entry), Is.False, "nothing to swap");
		}

		[Test]
		public void Swap_MakesTheAllomorphTheLexemeForm_AndPutsTheOldOneInItsPlace_InOneUndoableStep()
		{
			IMoStemAllomorph second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				second = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.AlternateFormsOS.Add(second);
			});

			LexemeFormConversion.Swap(_entry, second, "Undo Swap", "Redo Swap");

			Assert.That(_entry.LexemeFormOA, Is.SameAs(second));
			Assert.That(_entry.AlternateFormsOS, Is.EqualTo(new IMoForm[] { _allomorph, _lexemeForm }),
				"the old lexeme form takes the swapped allomorph's position");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Swap"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_entry.LexemeFormOA, Is.SameAs(_lexemeForm), "one Undo restores both changes");
			Assert.That(_entry.AlternateFormsOS, Is.EqualTo(new IMoForm[] { _allomorph, second }));
		}

		[Test]
		public void CanConvert_NeedsAFormOfTheClassTheCommandConvertsFrom()
		{
			Assert.That(LexemeFormConversion.CanConvert(_lexemeForm, MoStemAllomorphTags.kClassId), Is.True);
			Assert.That(LexemeFormConversion.CanConvert(_lexemeForm, MoAffixAllomorphTags.kClassId), Is.False);
			Assert.That(LexemeFormConversion.CanConvert(_lexemeForm, 0), Is.False, "an unknown class name");
			Assert.That(LexemeFormConversion.CanConvert(null, MoStemAllomorphTags.kClassId), Is.False, "no lexeme form");
		}

		[Test]
		public void DataLossMessage_NamesWhatAnAffixAllomorphLoses_AndNothingForAStem()
		{
			IMoAffixAllomorph affix = null;
			IPhEnvironment environment = null;
			IPartOfSpeech pos = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				affix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				_entry.AlternateFormsOS.Add(affix);
				environment = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(environment);
				pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			});
			void Set(bool environments, bool positions, bool grammar) => NonUndoableUnitOfWorkHelper.Do(
				Cache.ActionHandlerAccessor, () =>
				{
					affix.PhoneEnvRC.Clear();
					affix.PositionRS.Clear();
					affix.MsEnvPartOfSpeechRA = null;
					if (environments)
						affix.PhoneEnvRC.Add(environment);
					if (positions)
						affix.PositionRS.Add(environment);
					if (grammar)
						affix.MsEnvPartOfSpeechRA = pos;
				});

			Assert.That(LexemeFormConversion.DataLossMessage(_lexemeForm), Is.Null, "a stem allomorph loses nothing");
			Assert.That(LexemeFormConversion.DataLossMessage(null), Is.Null);
			Assert.That(LexemeFormConversion.DataLossMessage(affix), Is.Null, "an empty affix allomorph loses nothing");
			Set(true, false, false);
			Assert.That(LexemeFormConversion.DataLossMessage(affix), Is.EqualTo("The environment will be lost! Do you still want to continue?"));
			Set(false, true, false);
			Assert.That(LexemeFormConversion.DataLossMessage(affix), Is.EqualTo("The infix position will be lost! Do you still want to continue?"));
			Set(false, false, true);
			Assert.That(LexemeFormConversion.DataLossMessage(affix), Is.EqualTo("Some grammatical information will be lost! Do you still want to continue?"));
			Set(true, true, false);
			Assert.That(LexemeFormConversion.DataLossMessage(affix), Is.EqualTo("The environment and infix position will be lost! Do you still want to continue?"));
			Set(true, false, true);
			Assert.That(LexemeFormConversion.DataLossMessage(affix), Is.EqualTo("The environment and some grammatical information will be lost! Do you still want to continue?"));
			Set(false, true, true);
			Assert.That(LexemeFormConversion.DataLossMessage(affix), Is.EqualTo("The infix position and some grammatical information will be lost! Do you still want to continue?"));
			Set(true, true, true);
			Assert.That(LexemeFormConversion.DataLossMessage(affix), Is.EqualTo("The environment, infix position, and some grammatical information will be lost! Do you still want to continue?"));
			Assert.That(LexemeFormConversion.DataLossCaption, Is.EqualTo("Warning!"));
		}

		[Test]
		public void DataLossMessage_AnAffixProcess_LosesItsRule()
		{
			IMoAffixProcess process = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				process = Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
				_entry.AlternateFormsOS.Add(process);
			});

			Assert.That(LexemeFormConversion.DataLossMessage(process), Is.EqualTo("The affix process rule will be lost! Do you still want to continue?"));
		}

		[Test]
		public void Convert_ReplacesTheFormWithOneOfTheNewClass_KeepingItsText_InOneUndoableStep()
		{
			IMoAffixAllomorph affix = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				affix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				_entry.LexemeFormOA = affix;
				SetForm(affix, "-ing");
			});

			var converted = LexemeFormConversion.Convert(_entry, affix, MoAffixProcessTags.kClassId, "Undo Convert", "Redo Convert");

			Assert.That(converted, Is.InstanceOf<IMoAffixProcess>());
			Assert.That(_entry.LexemeFormOA, Is.SameAs(converted));
			Assert.That(converted.Form.get_String(Cache.DefaultVernWs).Text, Is.EqualTo("-ing"));
			Assert.That(affix.IsValidObject, Is.False, "the old form is gone");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Convert"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_entry.LexemeFormOA, Is.InstanceOf<IMoAffixAllomorph>(), "one Undo restores the old form");
			Assert.That(_entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text, Is.EqualTo("-ing"));
		}

		[Test]
		public void Convert_RefusesAClassNoConversionProduces()
		{
			Assert.That(() => LexemeFormConversion.Convert(_entry, _lexemeForm, LexEntryTags.kClassId, "u", "r"),
				Throws.InstanceOf<ArgumentOutOfRangeException>());
			Assert.That(_entry.LexemeFormOA, Is.SameAs(_lexemeForm), "the entry is untouched");
		}
	}
}
