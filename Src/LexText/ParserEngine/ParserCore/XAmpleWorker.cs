// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XAmpleWorker.cs
// Responsibility: FLEx Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Xml;
using System.IO;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using XAmpleManagedWrapper;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class XAmpleParserWorker : ParserWorker
	{
		private XAmpleWrapper m_xample;

		public XAmpleParserWorker(FdoCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue)
			: base(cache, taskUpdateHandler, idleQueue,
			cache.ServiceLocator.GetInstance<ICmAgentRepository>().GetObject(CmAgentTags.kguidAgentXAmpleParser))
		{
			m_xample = new XAmpleWrapper();
			m_xample.Init(FwDirectoryFinder.CodeDirectory);
			}

		protected override string ParseWord(string form, int hvoWordform)
		{
			return CompleteAmpleResults(m_xample.ParseWord(form), hvoWordform);
		}

		protected override string TraceWord(string form, string selectTraceMorphs)
		{
			return m_xample.TraceWord(form, selectTraceMorphs);
		}

		/// <summary>
		/// XAmple does not know the hvo of the Wordform.
		/// Thus it leaves a pattern which we need to replace with the actual hvo.
		/// </summary>
		/// <remarks>It would be nice if this was done down in the XAmple wrapper.
		/// However, I despaired of doing this simple replacement using bstrs, so I am doing it here.
		/// </remarks>
		/// <param name="rawAmpleResults"></param>
		/// <param name="hvoWordform"></param>
		/// <returns></returns>
		private static string CompleteAmpleResults(string rawAmpleResults, int hvoWordform)
		{
			// REVIEW Jonh(RandyR): This should probably be a simple assert,
			// since it is a programming error in the XAmple COM dll.
			if (rawAmpleResults == null)
				throw new ApplicationException("XAmpleCOM Dll failed to return any results. "
					+ "[NOTE: This is a programming error. See WPS-24 in JIRA.]");

			//find any instance of "<...>" which must be replaced with "[..]" - this indicates full reduplication
			const string ksFullRedupMarker = "<...>";
			var sTemp = rawAmpleResults.Replace(ksFullRedupMarker, "[...]");
			//find the "DB_REF_HERE" which must be replaced with the actual hvo
			const string kmatch = "DB_REF_HERE";
			Debug.Assert(sTemp.IndexOf(kmatch) > 0,
				"There was a problem interpretting the response from XAMPLE. " + kmatch + " was not found.");
			return sTemp.Replace(kmatch, "'" + hvoWordform + "'");
		}

		/// <summary>
		/// Loads the parser.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <param name="template">The template.</param>
		protected override void LoadParser(ref XmlDocument model, XmlDocument template)
		{
			var transformer = new M3ToXAmpleTransformer(m_projectName, m_taskUpdateHandler);
				var startTime = DateTime.Now;
				// PrepareTemplatesForXAmpleFiles adds orderclass elements to MoInflAffixSlot elements
			transformer.PrepareTemplatesForXAmpleFiles(ref model, template);
				var ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "GAFAWS prep took : " + ttlTicks);

			transformer.MakeAmpleFiles(model);

			int maxAnalCount = 20;
			XmlNode maxAnalCountNode = model.SelectSingleNode("/M3Dump/ParserParameters/XAmple/MaxAnalysesToReturn");
			if (maxAnalCountNode != null)
			{
				maxAnalCount = Convert.ToInt16(maxAnalCountNode.FirstChild.Value);
				if (maxAnalCount < 1)
					maxAnalCount = -1;
			}

			m_xample.SetParameter("MaxAnalysesToReturn", maxAnalCount.ToString());

			string tempPath = Path.GetTempPath();
			m_xample.LoadFiles(FwDirectoryFinder.CodeDirectory + @"/Language Explorer/Configuration/Grammar",
				tempPath, m_projectName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to dispose unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DisposeUnmanagedResources()
		{
			if (m_xample != null)
			{
				m_xample.Dispose();
				m_xample = null;
			}
		}
	}
}