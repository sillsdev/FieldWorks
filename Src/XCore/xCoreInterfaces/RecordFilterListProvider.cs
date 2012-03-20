using System;
using System.Collections;
using System.Xml;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// concrete implementations of this provide a list of RecordFilters to offer to the user.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification = "variable is a reference; it is owned by parent")]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "variable is a reference; it is owned by parent")]
	public abstract class RecordFilterListProvider : IxCoreColleague
	{
		protected XmlNode m_configuration;
		protected Mediator m_mediator;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// a factory method for RecordFilterListProvider
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configuration">The configuration.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public RecordFilterListProvider Create(Mediator mediator, XmlNode configuration)
		{
			RecordFilterListProvider p = (RecordFilterListProvider)DynamicLoader.CreateObject(configuration);
			if (p != null)
				p.Init(mediator, configuration);
			return p;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter list. this is called because we are an IxCoreColleague
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configuration">The configuration.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Init(Mediator mediator, XmlNode configuration)
		{
			m_configuration = configuration;
			m_mediator = mediator;
		}

		/// <summary>
		/// reload the data items
		/// </summary>
		public virtual void ReLoad()
		{
		}


		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// this is called because we are an IxCoreColleague
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Never any reason not to call this instance.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return false; }
		}

		/// <summary>
		/// the list of filters.
		/// </summary>
		public abstract ArrayList Filters
		{
			get;
		}

		//this has a signature of object just because is confined to XCore, so does not know about FDO RecordFilters
		public abstract object GetFilter(string id);

		/// <summary>
		/// May want to update / reload the list based on user selection.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns>true if handled.</returns>
		public virtual bool OnAdjustFilterSelection(object argument)
		{
			return false;
		}
	}
}
