using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// ViewBuilderFdo extends ViewBuilder with capabilities appropriate to building views of FDO objects.
	/// </summary>
	public class ViewBuilderFdo : ViewBuilder
	{
		public ViewBuilderFdo(GroupBox destination) : base(destination)
		{
		}

		/// <summary>
		/// A ViewBuilderFdo always has a root box that is a RootBoxFdo. This is a shortcut for getting it.
		/// </summary>
		public RootBoxFdo RootFdo {get { return (RootBoxFdo) Root;}}

		private Tuple<int, int> GetPropChangedKey(string name, ICmObject cmObj)
		{
			var mdcName = name;
			// Strip the silly FDO suffixes so we can look up in MDC.
			if (name.EndsWith("OS") || name.EndsWith("OA") || name.EndsWith("OC")
				|| name.EndsWith("RS") || name.EndsWith("RA") || name.EndsWith("RC"))
			{
				mdcName = name.Substring(0, name.Length - 2);
			}
			int tag = cmObj.Services.MetaDataCache.GetFieldId2(cmObj.ClassID, mdcName, true);
			return new Tuple<int, int>(cmObj.Hvo, tag);
		}

		/// <summary>
		/// Given a function that fetches strings, a run which represents the initial value of that string
		/// already inserted into a particular paragraph box, and that we have identified the fetcher as
		/// a property with the specified name of the specified target object, but we have not been able to find
		/// a ''Name'Changed' event on the target object, this stub provides a possible place for a subclass to
		/// use an alternative strategy for hooking something up to notify the view of changes to the property.
		/// </summary>
		protected override void MakeHookupForString(Func<ITsString> fetcher, TssClientRun run, string name, object target, ParaBox para)
		{
			var cmObj = target as ICmObject;
			if (cmObj != null)
			{
				// Set up for PropChanged notification.
				RootFdo.DataAccess = cmObj.Cache.DomainDataByFlid; // ensures hooked up to receive PropChanged.
				Tuple<int, int> key = GetPropChangedKey(name, cmObj);
				var stringHookup = new TssHookup(target, fetcher,
					hookup => RootFdo.AddHookupToPropChanged(key, hookup),
					hookup => RootFdo.RemoveHookupFromPropChanged(key, hookup),
					para);
				stringHookup.Tag = key.Item2;
				AddHookupToRun(run, stringHookup);
				// Enhance JohnT: consider doing this by reflection.
				stringHookup.Writer = newVal => RootFdo.DataAccess.SetString(cmObj.Hvo, key.Item2, newVal);
			}
		}
		/// <summary>
		/// Given that we are displaying property name of the target object, we would like to obtain actions that will connect and disconnect
		/// us from receiving notifications when that property changes. We have determined that there is no "'Name'Changed" event on the
		/// object. This hook method allows subclasses to provide an alternative strategy for setting up notifications.
		/// </summary>
		protected override void GetHookupEventActions(string name, object target, out Action<IReceivePropChanged> hookEventAction, out Action<IReceivePropChanged> unhookEventAction)
		{
			var cmObj = target as ICmObject;
			if (cmObj != null && Root != null)
			{
				RootFdo.DataAccess = cmObj.Cache.DomainDataByFlid; // ensures hooked up to receive PropChanged.
				Tuple<int, int> key = GetPropChangedKey(name, cmObj);
				hookEventAction = hookup => RootFdo.AddHookupToPropChanged(key, hookup);
				unhookEventAction = hookup => RootFdo.RemoveHookupFromPropChanged(key, hookup);
			}
			else
			{
				hookEventAction = null;
				unhookEventAction = null;
			}
		}

	}
}
