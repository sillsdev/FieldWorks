// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// The WinForms Writing Systems menu is answered by the rule the Avalonia detail view also
	/// uses. These pin that rule against the view's own writing-system query, so the two detail
	/// views cannot offer different writing systems.
	/// </summary>
	[TestFixture]
	public class MultiStringSliceWritingSystemsTests : MemoryOnlyBackendProviderTestBase
	{
		private const int MoFormFormFlid = 5035001;

		[TestCase(WritingSystemServices.kwsAnals, 0)]
		[TestCase(WritingSystemServices.kwsVerns, 0)]
		[TestCase(WritingSystemServices.kwsAnalVerns, 0)]
		// The Pronunciation form, the one shipped part that offers a second set on request.
		[TestCase(WritingSystemServices.kwsPronunciations, WritingSystemServices.kwsVerns)]
		public void SharedRule_OffersExactlyWhatTheViewOffered(int wsMagic, int optionalMagic)
		{
			var hvo = Cache.LangProject.Hvo;
			using (var host = new Form())
			using (var view = new LabeledMultiStringView(hvo, MoFormFormFlid, wsMagic, optionalMagic,
				false, true, true))
			{
				host.Controls.Add(view);
				view.InnerView.Cache = Cache;
				var spec = WritingSystemFieldSpec.FromMagicIds(hvo, wsMagic, optionalMagic, false);

				Assert.That(FieldWritingSystemOptions.Options(Cache, spec).Select(ws => ws.Id),
					Is.EqualTo(view.WritingSystemOptions.Select(ws => ws.Id)),
					"the menu must offer the writing systems the view offered, in the same order");
				Assert.That(FieldWritingSystemOptions.DefaultShown(Cache, spec).Select(ws => ws.Id),
					Is.EqualTo(view.GetWritingSystemOptions(false).Select(ws => ws.Id)),
					"and default to the same subset when the field stores no selection");
			}
		}

		[Test]
		public void SharedRule_OffersMoreThanItShows_WhenTheProjectHasAnUncheckedWritingSystem()
		{
			var hvo = Cache.LangProject.Hvo;
			var spec = WritingSystemFieldSpec.FromMagicIds(hvo, WritingSystemServices.kwsAnals, 0, false);
			var offered = FieldWritingSystemOptions.Options(Cache, spec).Select(ws => ws.Id).ToList();
			var shown = FieldWritingSystemOptions.DefaultShown(Cache, spec).Select(ws => ws.Id).ToList();

			Assert.That(shown, Is.SubsetOf(offered),
				"everything shown must be offerable; the menu may offer more than the field shows");
		}
	}
}
