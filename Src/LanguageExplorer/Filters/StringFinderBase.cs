// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Filters
{
	/// <summary />
	internal abstract class StringFinderBase : IStringFinder
	{
		/// <summary />
		protected StringFinderBase()
		{
		}

		/// <summary />
		protected StringFinderBase(ISilDataAccess sda)
		{
			DataAccess = sda;
		}

		/// <summary>
		/// Default is to return the strings for the key object.
		/// </summary>
		public string[] Strings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			var result = Strings(item.KeyObject);
			if (sortedFromEnd)
			{
				for (var i = 0; i < result.Length; i++)
				{
					result[i] = TsStringUtils.ReverseString(result[i]);
				}
			}

			return result;
		}

		/// <summary>
		/// For most of these we want to return the same thing.
		/// </summary>
		public string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			return Strings(item, sortedFromEnd);
		}

		public virtual ITsString Key(IManyOnePathSortItem item)
		{
			throw new NotSupportedException("Don't have new Key function implemented on class " + GetType());
		}

		public ISilDataAccess DataAccess { get; set; }

		/// <summary>
		/// Strings the specified hvo.
		/// </summary>
		public abstract string[] Strings(int hvo);

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		public abstract bool SameFinder(IStringFinder other);

		/// <summary>
		/// Add to collector the IManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		public virtual void CollectItems(int hvo, List<IManyOnePathSortItem> collector)
		{
			collector.Add(new ManyOnePathSortItem(hvo, null, null));
		}

		/// <summary>
		/// Called in advance of 'finding' strings for many instances, typically all or most
		/// of the ones in existence. May preload data to make such a large succession of finds
		/// more efficient. Also permitted to do nothing, as in this default implementation.
		/// </summary>
		public void Preload(ICmObject rootObj)
		{
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public abstract void PersistAsXml(XElement element);

		#endregion

		#region IStoresLcmCache
		/// <summary>
		/// Set the cache. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		public LcmCache Cache
		{
			set => DataAccess = value.DomainDataByFlid;
		}
		#endregion
	}
}