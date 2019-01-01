// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Matches if the embedded matcher fails.
	/// </summary>
	public class InvertMatcher : BaseMatcher, IStoresLcmCache, IStoresDataAccess
	{
		/// <summary />
		public InvertMatcher(IMatcher matcher)
		{
			MatcherToInvert = matcher;
		}

		/// <summary>
		/// default for persistence.
		/// </summary>
		public InvertMatcher()
		{
		}

		/// <summary>
		/// Gets the matcher to invert.
		/// </summary>
		public IMatcher MatcherToInvert { get; private set; }

		/// <summary />
		public override bool Matches(ITsString arg)
		{
			return arg != null && !MatcherToInvert.Matches(arg);
		}

		/// <summary>
		/// True if it is the same class and member vars match.
		/// </summary>
		public override bool SameMatcher(IMatcher other)
		{
			var other2 = other as InvertMatcher;
			return other2 != null && MatcherToInvert.SameMatcher(other2.MatcherToInvert);
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			DynamicLoader.PersistObject(MatcherToInvert, element, "invertMatcher");
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml(element);
			MatcherToInvert = DynamicLoader.RestoreFromChild(element, "invertMatcher") as IMatcher;
		}

		#region IStoresLcmCache Members

		LcmCache IStoresLcmCache.Cache
		{
			set
			{
				if (MatcherToInvert is IStoresLcmCache)
				{
					((IStoresLcmCache)MatcherToInvert).Cache = value;
				}
			}
		}

		ISilDataAccess IStoresDataAccess.DataAccess
		{
			set
			{
				if (MatcherToInvert is IStoresDataAccess)
				{
					((IStoresDataAccess)MatcherToInvert).DataAccess = value;
				}
			}
		}
		#endregion
	}
}