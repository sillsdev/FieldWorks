// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This is a base class for matchers; so far it just implements storing the label.
	/// </summary>
	public abstract class BaseMatcher : IMatcher, IPersistAsXml, IStoresLcmCache
	{
		// Todo: get this initialized somehow.
		// This is used only to save the value restored by InitXml until the Cache is set
		// so that m_tssLabel can be computed.
		private string m_xmlLabel;

		#region IMatcher Members

		/// <summary />
		public abstract bool Matches(ITsString arg);

		/// <summary />
		public abstract bool SameMatcher(IMatcher other);

		/// <summary>
		/// No specific writing system for most of these matchers.
		/// </summary>
		public virtual int WritingSystem => 0;

		/// <summary />
		public ITsString Label { get; set; }

		/// <summary />
		/// <remarks>
		/// Most matchers won't have to override this - regex is one that does though
		/// </remarks>
		public virtual bool IsValid()
		{
			return true;
		}

		/// <summary />
		public virtual string ErrorMessage()
		{
			return string.Format(FiltersStrings.ksErrorMsg, IsValid());
		}

		/// <summary />
		public virtual bool CanMakeValid()
		{
			return false;
		}

		/// <summary />
		public virtual ITsString MakeValid()
		{
			return null;    // should only be called if CanMakeValid it true and that class should implement it.
		}
		#endregion

		/// <summary />
		public ILgWritingSystemFactory WritingSystemFactory { set; get; }

		/// <summary>
		/// This is overridden only by BlankMatcher currently, which matches
		/// on an empty list of strings.
		/// </summary>
		public virtual bool Accept(ITsString tss)
		{
			return Matches(tss);
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public virtual void PersistAsXml(XElement element)
		{
			if (Label != null)
			{
				var contents = TsStringUtils.GetXmlRep(Label, WritingSystemFactory, 0, false);
				XmlUtils.SetAttribute(element, "label", contents);
			}
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public virtual void InitXml(XElement element)
		{
			m_xmlLabel = XmlUtils.GetOptionalAttributeValue(element, "label");
		}

		#endregion

		#region Implementation of IStoresLcmCache

		/// <summary>
		/// Set the cache. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		public virtual LcmCache Cache
		{
			set
			{
				WritingSystemFactory = value.WritingSystemFactory;
				if (m_xmlLabel != null)
				{
					Label = TsStringSerializer.DeserializeTsStringFromXml(m_xmlLabel, WritingSystemFactory);
				}
			}
		}

		#endregion
	}
}