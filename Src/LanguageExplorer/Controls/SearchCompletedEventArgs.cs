// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// The event args for the SearchCompleted async event.
	/// </summary>
	internal sealed class SearchCompletedEventArgs : AsyncCompletedEventArgs
	{
		private readonly List<SearchField> m_fields;
		private readonly List<int> m_results;

		/// <summary />
		internal SearchCompletedEventArgs(IEnumerable<SearchField> fields, IEnumerable<int> results)
			: base(null, false, null)
		{
			m_fields = fields.ToList();
			m_results = results.ToList();
		}

		/// <summary>
		/// Gets the fields.
		/// </summary>
		internal IEnumerable<SearchField> Fields => m_fields;

		/// <summary>
		/// Gets the results.
		/// </summary>
		internal IEnumerable<int> Results => m_results;
	}
}
