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
	/// The allomorph's Is Abstract Form row: the legacy <c>CheckBoxSlice</c> composed as an
	/// editable boolean row whose value stages through the shared option path as the literal
	/// "true"/"false".
	/// </summary>
	[TestFixture]
	public class AllomorphBooleanComposeTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private IMoStemAllomorph m_stem;

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

				m_stem = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.AlternateFormsOS.Add(m_stem);
				m_stem.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("barigi-stem", Cache.DefaultVernWs));
			});
		}

		private DetailField ComposeIsAbstractRow(ComposedDetail composed = null)
		{
			var model = (composed ?? DetailComposer.Compose(m_entry, Cache, showHiddenFields: true))
				.Model;
			return model.Fields.First(
				f => f.Field == "IsAbstract" && f.ObjectHvo == m_stem.Hvo);
		}

		/// <summary>
		/// Is Abstract Form composes as an editable checkbox carrying the allomorph's current
		/// value.
		/// </summary>
		[Test]
		public void Compose_IsAbstract_RendersEditableCheckbox_WhenShowHiddenFieldsIsOn()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_stem.IsAbstract = true);

			var row = ComposeIsAbstractRow();

			Assert.That(row.Kind, Is.EqualTo(DetailFieldKind.Boolean),
				"the checkbox editor composes as a boolean row");
			Assert.That(row.IsEditable, Is.True, "the row accepts a toggle");
			Assert.That(row.SelectedOptionKey, Is.EqualTo(bool.TrueString),
				"the row carries the allomorph's current abstract state");
		}

		/// <summary>
		/// Toggling Is Abstract Form writes through to the model and commits as ONE undoable
		/// step, staged through the same option path a chooser row uses.
		/// </summary>
		[Test]
		public void Edit_IsAbstract_CommitsAsOneUndoStep()
		{
			Assert.That(m_stem.IsAbstract, Is.False,
				"precondition: a new allomorph is not abstract");

			var composed = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true);
			var row = ComposeIsAbstractRow(composed);

			Assert.That(composed.EditContext.TrySetOption(row, bool.TrueString), Is.True,
				"the boolean row stages through the shared option path");
			composed.EditContext.Commit();

			Assert.That(m_stem.IsAbstract, Is.True, "the toggle reached the model");

			Cache.ActionHandlerAccessor.Undo();

			Assert.That(m_stem.IsAbstract, Is.False, "ONE undo restores the previous value");
		}

		/// <summary>
		/// A key that is not one of the two boolean literals is refused without staging, like a
		/// chooser key outside its list.
		/// </summary>
		[Test]
		public void Edit_IsAbstract_RejectsNonBooleanKey()
		{
			var composed = DetailComposer.Compose(m_entry, Cache, showHiddenFields: true);
			var row = ComposeIsAbstractRow(composed);

			Assert.That(composed.EditContext.TrySetOption(row, "yes"), Is.False,
				"only the boolean literals are accepted");
			Assert.That(composed.EditContext.IsOpen, Is.False,
				"a refused key must not open the fenced session");
		}
	}
}
