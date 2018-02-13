// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Summary description for PhEnvStrRepresentationSlice.
	/// </summary>
	internal sealed class PhEnvStrRepresentationSlice : ViewPropertySlice
	{
		public PhEnvStrRepresentationSlice(ICmObject obj)
			: base(new StringRepSliceView(obj.Hvo), obj, StringRepSliceVc.Flid)
		{
		}

		/// <summary>
		/// We want the persistence provider, and the easiest way to get it is to get all
		/// this other stuff we don't need or use.
		/// </summary>
		public PhEnvStrRepresentationSlice(LcmCache cache, string editor, int flid,
			XElement element, ICmObject obj,
			IPersistenceProvider persistenceProvider, int ws)
			: base(new StringRepSliceView(obj.Hvo), obj, StringRepSliceVc.Flid)
		{
			PersistenceProvider = persistenceProvider;
		}

		public PhEnvStrRepresentationSlice()
		{
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

#if RANDYTODO
		/// <summary>
		/// This menu item is turned off if a slash already exists in the environment string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertSlash(object commandObject,
			ref UIItemDisplayProperties display)
		{
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertSlash();
			return true;
		}
#endif

		public bool OnInsertSlash(object args)
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertEnvironmentSlash, AreaResources.ksInsertEnvironmentSlash);
			MyStringRepSliceView.RootBox.OnChar((int)'/');
			Cache.DomainDataByFlid.EndUndoTask();
			return true;
		}

#if RANDYTODO
		/// <summary>
		/// This menu item is turned off if an underscore already exists in the environment
		/// string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertEnvironmentBar(object commandObject,
			ref UIItemDisplayProperties display)
		{
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertEnvBar();
			return true;
		}
#endif

		public bool OnInsertEnvironmentBar(object args)
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertEnvironmentBar, AreaResources.ksInsertEnvironmentBar);
			MyStringRepSliceView.RootBox.OnChar('_');
			Cache.DomainDataByFlid.EndUndoTask();
			return true;
		}

		private StringRepSliceView MyStringRepSliceView => (StringRepSliceView)Control;

#if RANDYTODO
		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertNaturalClass(object commandObject,
			ref UIItemDisplayProperties display)
		{
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertItem();
			return true;
		}
#endif

		public bool OnInsertNaturalClass(object args)
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertNaturalClass, AreaResources.ksInsertNaturalClass);
			var fOk = ReallySimpleListChooser.ChooseNaturalClass(MyStringRepSliceView.RootBox, Cache, PersistenceProvider, PropertyTable, Publisher);
			Cache.DomainDataByFlid.EndUndoTask();
			return fOk;
		}

#if RANDYTODO
		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertOptionalItem(object commandObject,
			ref UIItemDisplayProperties display)
		{
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertItem();
			return true;
		}
#endif

		public bool OnInsertOptionalItem(object args)
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertOptionalItem, AreaResources.ksInsertOptionalItem);
			PhoneEnvReferenceSlice.InsertOptionalItem(MyStringRepSliceView.RootBox);
			Cache.DomainDataByFlid.EndUndoTask();
			return true;
		}

#if RANDYTODO
		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertHashMark(object commandObject,
			ref UIItemDisplayProperties display)
		{
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertHashMark();
			return true;
		}
#endif

		public bool OnInsertHashMark(object args)
		{
			Cache.DomainDataByFlid.BeginUndoTask(AreaResources.ksInsertWordBoundary, AreaResources.ksInsertWordBoundary);
			MyStringRepSliceView.RootBox.OnChar('#');
			Cache.DomainDataByFlid.EndUndoTask();
			return true;
		}
		#endregion
	}
}