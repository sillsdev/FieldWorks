// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Matches if the embedded matcher fails.
	/// </summary>
	internal sealed class InvertMatcher : BaseMatcher, IStoresDataAccess
	{
		/// <summary />
		internal InvertMatcher(IMatcher matcher)
		{
			MatcherToInvert = matcher;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal InvertMatcher(IPersistAsXmlFactory factory, XElement element)
			: base(element)
		{
			MatcherToInvert = factory.Create<IMatcher>(element.Element("matcher"));
		}

		/// <summary>
		/// Gets the matcher to invert.
		/// </summary>
		internal IMatcher MatcherToInvert { get; }

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
			return other is InvertMatcher invertMatcher && MatcherToInvert.SameMatcher(invertMatcher.MatcherToInvert);
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			LanguageExplorerServices.PersistObject(MatcherToInvert, element, "matcher");
		}

		#region IStoresLcmCache Members

		public override LcmCache Cache
		{
			set
			{
				if (MatcherToInvert is IStoresLcmCache storesLcmCache)
				{
					storesLcmCache.Cache = value;
				}
			}
		}

		ISilDataAccess IStoresDataAccess.DataAccess
		{
			set
			{
				if (MatcherToInvert is IStoresDataAccess storesDataAccess)
				{
					storesDataAccess.DataAccess = value;
				}
			}
		}
		#endregion
	}
}