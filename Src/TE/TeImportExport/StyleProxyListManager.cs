// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StyleProxyListManager.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class StyleProxyListManager
	{
		private static Dictionary<string, ImportStyleProxy> s_styleProxies;
		private static FwStyleSheet s_styleSheet;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the list of style proxies.
		/// </summary>
		/// <param name="styleSheet">The style sheet.</param>
		/// ------------------------------------------------------------------------------------
		public static void Initialize(FwStyleSheet styleSheet)
		{
			if (s_styleProxies == null)
				s_styleProxies = new Dictionary<string, ImportStyleProxy>();

			s_styleSheet = styleSheet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the resources used by the proxy list manager
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void Cleanup()
		{
			s_styleProxies = null;
			s_styleSheet = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find (or create and store) the ImportStyleProxy for the given style and context.
		/// </summary>
		/// <param name="sStyle">name of a paragraph style</param>
		/// <param name="context">ignored in XML import</param>
		/// <param name="defaultWs">The default writing system to use when creating a new style
		/// proxy.</param>
		/// <returns>The style proxy</returns>
		/// ------------------------------------------------------------------------------------
		public static ImportStyleProxy GetXmlParaStyleProxy(string sStyle, ContextValues context,
			int defaultWs)
		{
			ImportStyleProxy proxy;
			if (string.IsNullOrEmpty(sStyle))
				sStyle = ScrStyleNames.NormalParagraph;
			if (!s_styleProxies.TryGetValue(sStyle, out proxy))
			{
				proxy = new ImportStyleProxy(sStyle, StyleType.kstParagraph, defaultWs,
					context, s_styleSheet);
				s_styleProxies.Add(sStyle, proxy);
			}
			return proxy;
		}
	}
}
