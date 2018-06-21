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
		public PhEnvStrRepresentationSlice(ICmObject obj, IPersistenceProvider persistenceProvider)
			: base(new StringRepSliceView(obj.Hvo), obj, StringRepSliceVc.Flid)
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
#if RANDYTODO
		/// <summary>
		/// This menu item is turned off if a slash already exists in the environment string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayShowEnvironmentError(object commandObject,
			ref UIItemDisplayProperties display)
		{
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanShowEnvironmentError();
			return true;
		}
#endif

		public bool OnShowEnvironmentError(object args)
		{
			MyStringRepSliceView.ShowEnvironmentError();
			return true;
		}

		/// <summary>
		/// This menu item is turned off if a slash already exists in the environment string.
		/// </summary>
		public bool CanInsertSlash
		{
			get
			{
				var view = Control as StringRepSliceView;
				return view != null && view.CanInsertSlash;
			}
		}

		public void InsertSlash()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertEnvironmentSlash, AreaResources.ksInsertEnvironmentSlash);
			MyStringRepSliceView.RootBox.OnChar('/');
			Cache.DomainDataByFlid.EndUndoTask();
		}

		/// <summary>
		/// This menu item is turned off if an underscore already exists in the environment
		/// string.
		/// </summary>
		public bool CanInsertEnvironmentBar
		{
			get
			{
				var view = Control as StringRepSliceView;
				return view != null && view.CanInsertEnvBar;
			}
		}

		public void InsertEnvironmentBar()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertEnvironmentBar, AreaResources.ksInsertEnvironmentBar);
			MyStringRepSliceView.RootBox.OnChar('_');
			Cache.DomainDataByFlid.EndUndoTask();
		}

		private StringRepSliceView MyStringRepSliceView => (StringRepSliceView)Control;

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		public bool CanInsertNaturalClass
		{
			get
			{
				var view = Control as StringRepSliceView;
				return view != null && view.CanInsertItem;
			}
		}

		public void InsertNaturalClass()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertNaturalClass, AreaResources.ksInsertNaturalClass);
			var fOk = ReallySimpleListChooser.ChooseNaturalClass(MyStringRepSliceView.RootBox, Cache, PersistenceProvider, PropertyTable, Publisher, Subscriber);
			Cache.DomainDataByFlid.EndUndoTask();
		}

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		public bool CanInsertOptionalItem
		{
			get
			{
				var view = Control as StringRepSliceView;
				return view != null && view.CanInsertItem;
			}
		}

		public void InsertOptionalItem()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertOptionalItem, AreaResources.ksInsertOptionalItem);
			PhoneEnvReferenceSlice.InsertOptionalItem(MyStringRepSliceView.RootBox);
			Cache.DomainDataByFlid.EndUndoTask();
		}

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		public bool CanInsertHashMark
		{
			get
			{
				var view = Control as StringRepSliceView;
				return view != null && view.CanInsertHashMark;
			}
		}

		public void InsertHashMark()
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertWordBoundary, AreaResources.ksInsertWordBoundary);
			MyStringRepSliceView.RootBox.OnChar('#');
			Cache.DomainDataByFlid.EndUndoTask();
		}
		#endregion
	}
}