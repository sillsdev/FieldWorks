// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Rows of a grammatical category whose parts wrap their content in a conditional, composed
	/// from the shipped layouts: the row shows what the layout's part ref asks for.
	/// </summary>
	[TestFixture]
	public class DetailConditionalPartRefTests : MemoryOnlyBackendProviderTestBase
	{
		private IPartOfSpeech m_noun;
		private IPartOfSpeech m_subcategory;
		private IMoInflAffixTemplate m_template;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var services = Cache.ServiceLocator;
				if (Cache.LangProject.PartsOfSpeechOA == null)
				{
					Cache.LangProject.PartsOfSpeechOA =
						services.GetInstance<ICmPossibilityListFactory>().Create();
				}
				// The Subcategories part shows nothing in a list too shallow to nest.
				Cache.LangProject.PartsOfSpeechOA.Depth = 127;

				m_noun = services.GetInstance<IPartOfSpeechFactory>().Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(m_noun);
				m_noun.Name.set_String(Cache.DefaultAnalWs, Analysis("Noun"));

				m_subcategory = services.GetInstance<IPartOfSpeechFactory>().Create();
				m_noun.SubPossibilitiesOS.Add(m_subcategory);
				m_subcategory.Name.set_String(Cache.DefaultAnalWs, Analysis("Proper noun"));

				m_template = services.GetInstance<IMoInflAffixTemplateFactory>().Create();
				m_noun.AffixTemplatesOS.Add(m_template);
				m_template.Name.set_String(Cache.DefaultAnalWs, Analysis("Plural"));
			});
		}

		private ITsString Analysis(string text) => TsStringUtils.MakeString(text, Cache.DefaultAnalWs);

		// The name part holds one slice per state of the template's Active box.
		[TestCase(false)]
		[TestCase(true)]
		public void Compose_AnAffixTemplatesNameRow_TakesTheLayoutsLabelAndMenu(bool disabled)
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_template.Disabled = disabled);

			var fields = DetailComposer.Compose(m_noun, Cache, "default").Model.Fields;

			var name = fields.Single(f => f.ObjectHvo == m_template.Hvo && f.Field == "Name"
				&& f.Kind != DetailFieldKind.Header);
			Assert.That(name.Label, Is.EqualTo("Template Name"));
			Assert.That(name.MenuId, Is.EqualTo("mnuDataTree-POS-AffixTemplate"));
		}

		[Test]
		public void Compose_Subcategories_KeepOneHeading_AndOneNameRowPerSubcategory()
		{
			var fields = DetailComposer.Compose(m_noun, Cache, "default").Model.Fields;

			var headings = fields.Where(f => f.Kind == DetailFieldKind.Header).ToList();
			Assert.That(headings.Count(h => h.Label == "Subcategories"), Is.EqualTo(1));
			Assert.That(headings.Any(h => h.ObjectHvo == m_subcategory.Hvo), Is.False,
				"a subcategory has no numbered heading of its own");
			var name = fields.Single(f => f.ObjectHvo == m_subcategory.Hvo && f.Field == "Name");
			Assert.That(name.Label, Is.EqualTo("Subcategory"));
		}
	}
}
