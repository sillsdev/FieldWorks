// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Summary description for PhEnvStrRepresentationSlice.
	/// </summary>
	internal sealed class PhEnvStrRepresentationSlice : ViewPropertySlice, IPhEnvSliceCommon
	{
		/// <summary>
		/// We want the persistence provider, and the easiest way to get it is to get all
		/// this other stuff we don't need or use.
		/// </summary>
		public PhEnvStrRepresentationSlice(ICmObject obj, IPersistenceProvider persistenceProvider, ISharedEventHandlers sharedEventHandlers)
			: base(new StringRepSliceView(sharedEventHandlers, obj.Hvo), obj, StringRepSliceVc.Flid)
		{
			PersistenceProvider = persistenceProvider;
		}

		/// <summary>
		/// Therefore this method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			MyStringRepSliceView.Cache = PropertyTable.GetValue<LcmCache>("cache");
			MyStringRepSliceView.ResetValidator();

			if (MyStringRepSliceView.RootBox == null)
			{
				MyStringRepSliceView.MakeRoot();
			}
		}

		#region Special menu item methods

		/// <inheritdoc />
		public bool CanShowEnvironmentError => MyStringRepSliceView.CanShowEnvironmentError();

		/// <inheritdoc />
		public void ShowEnvironmentError()
		{
			MyStringRepSliceView.ShowEnvironmentError();
		}

		/// <inheritdoc />
		public bool CanInsertSlash => MyStringRepSliceView.CanInsertSlash;

		/// <inheritdoc />
		public void InsertSlash()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertEnvironmentSlash, AreaResources.ksInsertEnvironmentSlash);
			MyStringRepSliceView.RootBox.OnChar('/');
			Cache.DomainDataByFlid.EndUndoTask();
		}

		/// <inheritdoc />
		public bool CanInsertEnvironmentBar => MyStringRepSliceView.CanInsertEnvBar;

		/// <inheritdoc />
		public void InsertEnvironmentBar()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertEnvironmentBar, AreaResources.ksInsertEnvironmentBar);
			MyStringRepSliceView.RootBox.OnChar('_');
			Cache.DomainDataByFlid.EndUndoTask();
		}

		private StringRepSliceView MyStringRepSliceView => (StringRepSliceView)Control;

		/// <inheritdoc />
		public bool CanInsertNaturalClass => MyStringRepSliceView.CanInsertItem;

		/// <inheritdoc />
		public void InsertNaturalClass()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertNaturalClass, AreaResources.ksInsertNaturalClass);
			var fOk = ReallySimpleListChooser.ChooseNaturalClass(MyStringRepSliceView.RootBox, Cache, PersistenceProvider, PropertyTable, Publisher, Subscriber);
			Cache.DomainDataByFlid.EndUndoTask();
		}

		/// <inheritdoc />
		public bool CanInsertOptionalItem => MyStringRepSliceView.CanInsertItem;

		/// <inheritdoc />
		public void InsertOptionalItem()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertOptionalItem, AreaResources.ksInsertOptionalItem);
			PhoneEnvReferenceSlice.InsertOptionalItem(MyStringRepSliceView.RootBox);
			Cache.DomainDataByFlid.EndUndoTask();
		}

		/// <inheritdoc />
		public bool CanInsertHashMark => MyStringRepSliceView.CanInsertHashMark;

		/// <inheritdoc />
		public void InsertHashMark()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertWordBoundary, AreaResources.ksInsertWordBoundary);
			MyStringRepSliceView.RootBox.OnChar('#');
			Cache.DomainDataByFlid.EndUndoTask();
		}
		#endregion
	}
}