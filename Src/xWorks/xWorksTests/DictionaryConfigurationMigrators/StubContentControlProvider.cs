// Copyright (c) 2016-2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)using System;

using System;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	/// <summary>
	/// This class is for use in unit tests that need to use the ActivateClerk functionality. For instance to imitate switching
	/// tools for testing export functionality.
	/// </summary>
	/// <remarks>To use add the following to your TestFixtureSetup: m_mediator.AddColleague(new StubContentControlProvider());</remarks>
	internal class StubContentControlProvider : IxCoreColleague, IDisposable
	{
		private const string m_contentControlDictionary =
			@"<control>
					<parameters PaneBarGroupId='PaneBar-Dictionary'>
						<control>
							<parameters area='lexicon' clerk='entries' />
						</control>
						<!-- The following configureLayouts node is only required to help migrate old configurations to the new format -->
						<configureLayouts>
							<layoutType label='Lexeme-based (complex forms as main entries)' layout='publishStem'>
								<configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />
								<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' />
							</layoutType>
							<layoutType label='Root-based (complex forms as subentries)' layout='publishRoot'>
								<configure class='LexEntry' label='Main Entry' layout='publishRootEntry' />
								<configure class='LexEntry' label='Minor Entry' layout='publishRootMinorEntry' hideConfig='true' />
							</layoutType>
						</configureLayouts>
					</parameters>
				</control>";
		private readonly XmlNode m_testControlDictNode;

		private const string m_contentControlReversal =
			@"<control>
					<parameters id='reversalIndexEntryList' PaneBarGroupId='PaneBar-ReversalIndicesMenu'>
						<control>
							<parameters area='lexicon' clerk='AllReversalEntries' />
						</control>
						<configureLayouts>
							<layoutType label='All Reversal Indexes' layout='publishReversal'>
								<configure class='ReversalIndexEntry' label='Reversal Entry' layout='publishReversalIndexEntry' />
							</layoutType>
							<layoutType label='$wsName' layout='publishReversal-$ws'>
								<configure class='ReversalIndexEntry' label='Reversal Entry' layout='publishReversalIndexEntry-$ws' />
							</layoutType>
						</configureLayouts>
					</parameters>
				</control>";
		private readonly XmlNode m_testControlRevNode;

		public StubContentControlProvider()
		{
			var doc = new XmlDocument();
			doc.LoadXml(m_contentControlDictionary);
			m_testControlDictNode = doc.DocumentElement;
			var reversalDoc = new XmlDocument();
			reversalDoc.LoadXml(m_contentControlReversal);
			m_testControlRevNode = reversalDoc.DocumentElement;
			Subscriber.Subscribe(EventConstants.GetContentControlParameters, GetContentControlParameters);
		}

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		/// <summary>
		/// This is called by reflection through the mediator. We need so that we can migrate through the PreHistoricMigrator.
		/// </summary>
		// ReSharper disable once UnusedMember.Local
		private void GetContentControlParameters(object parameterObj)
		{
			var param = parameterObj as Tuple<string, string, XmlNode[]>;
			if (param == null)
				return;
			var result = param.Item3;
			Assert.That(param.Item2 == "lexiconDictionary" || param.Item2 == "reversalToolEditComplete", "No params for tool: " + param.Item2);
			result[0] = param.Item2 == "lexiconDictionary" ? m_testControlDictNode : m_testControlRevNode;
		}

		public bool ShouldNotCall => false;
		public int Priority => 1;

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		public void Dispose(bool disposeCalled)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposeCalled, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (disposeCalled)
			{
				Subscriber.Unsubscribe(EventConstants.GetContentControlParameters, GetContentControlParameters);
			}
		}

		~StubContentControlProvider()
		{
			Dispose(false);
		}
	}
}