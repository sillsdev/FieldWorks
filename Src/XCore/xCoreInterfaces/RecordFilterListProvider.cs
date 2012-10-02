using System;
using System.Collections;
using System.Xml;
using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.Common.Utils;

namespace XCore
{
	/// <summary>
	/// concrete implementations of this provide a list of RecordFilters to offer to the user.
	/// </summary>
	public abstract class RecordFilterListProvider : IxCoreColleague, IFWDisposable
	{
		protected XmlNode m_configuration;
		protected Mediator m_mediator;

		public RecordFilterListProvider()
		{
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~RecordFilterListProvider()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;
			m_configuration = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// a factory method for RecordFilterListProvider
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		static public RecordFilterListProvider Create(Mediator mediator, XmlNode configuration)
		{
			RecordFilterListProvider p = (RecordFilterListProvider)DynamicLoader.CreateObject(configuration);
			if (p != null)
				p.Init(mediator, configuration);
			return p;
		}


		/// <summary>
		/// Initialize the filter list. this is called because we are an IxCoreColleague
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="configuration"></param>
		public virtual void Init(Mediator mediator, XmlNode configuration)
		{
			CheckDisposed();
			m_configuration = configuration;
			m_mediator = mediator;
		}

		/// <summary>
		/// reload the data items
		/// </summary>
		public virtual void ReLoad()
		{
			CheckDisposed();
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
			CheckDisposed();
			return new IxCoreColleague[]{this};
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
			CheckDisposed();
			return false;
		}
	}
}
