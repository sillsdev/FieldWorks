// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Grammar
{
	/// <summary>
	/// A browse view which has the select column hooked to an Active boolean
	///  (which is the UI name of the Disabled property of phonological rules,
	///   compound rules, ad hoc rules, and inflectional affix templates).  We
	///  only use this view with phonological rules and compound rules.
	/// </summary>
	internal class RecordBrowseActiveView : RecordBrowseView
	{
		internal RecordBrowseActiveView(XElement browseViewDefinitions, LcmCache cache, IRecordList recordList)
			: base(browseViewDefinitions, cache, recordList)
		{
		}

		protected override BrowseViewer CreateBrowseViewer(XElement nodeSpec, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			var viewer = new BrowseActiveViewer(nodeSpec, hvoRoot, cache, sortItemProvider, sda);
			viewer.CheckBoxActiveChanged += OnCheckBoxActiveChanged;
			return viewer;
		}

		/// <summary>
		/// Event handler, which makes any changes to the Active flag.
		/// </summary>
		public void OnCheckBoxActiveChanged(object sender, CheckBoxActiveChangedEventArgs e)
		{
			OnCheckBoxChanged(sender, e);
			var changedHvos = e.HvosChanged;
			UndoableUnitOfWorkHelper.Do(e.UndoMessage, e.RedoMessage, Cache.ActionHandlerAccessor, () => ChangeAnyDisabledFlags(changedHvos));
		}
		private void ChangeAnyDisabledFlags(int[] changedHvos)
		{
			foreach (var hvo in changedHvos)
			{
				var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				switch (obj.ClassID)
				{
					case PhRegularRuleTags.kClassId: // fall through
					case PhMetathesisRuleTags.kClassId:
						var segmentRule = (IPhSegmentRule)obj;
						segmentRule.Disabled = !segmentRule.Disabled;
						break;
					case MoEndoCompoundTags.kClassId: // fall through
					case MoExoCompoundTags.kClassId:
						var compoundRule = (IMoCompoundRule)obj;
						compoundRule.Disabled = !compoundRule.Disabled;
						break;
				}
			}
		}
	}
}