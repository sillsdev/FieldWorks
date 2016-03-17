// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Concrete implementations of this provide a list of RecordFilters to offer to the user.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification = "variable is a reference; it is owned by parent")]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "variable is a reference; it is owned by parent")]
	public abstract class RecordFilterListProvider : IFlexComponent
	{
		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameterObject">Parameter object that contains the required three interfaces.</param>
		public virtual void InitializeFlexComponent(FlexComponentParameterObject flexComponentParameterObject)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameterObject, new FlexComponentParameterObject(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameterObject.PropertyTable;
			Publisher = flexComponentParameterObject.Publisher;
			Subscriber = flexComponentParameterObject.Subscriber;
		}

		#endregion

		/// <summary>
		/// reload the data items
		/// </summary>
		public virtual void ReLoad()
		{
		}

		/// <summary>
		/// the list of filters.
		/// </summary>
		public abstract ArrayList Filters
		{
			get;
		}

		//
		/// <summary>
		/// Get a filter
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <remarks>This has a signature of object just because is confined to CoreImpl, so does not know about FDO RecordFilters</remarks>
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
