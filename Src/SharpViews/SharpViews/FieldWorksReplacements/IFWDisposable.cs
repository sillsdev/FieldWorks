using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple interface to extend the IDisposable interface by adding the IsDisposed
	/// and CheckDisposed properties.  These methods exist so that we can make sure
	/// that objects are being used only while they are valid, not disposed.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFWDisposable : IDisposable
	{
		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		bool IsDisposed { get; }

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		void CheckDisposed();
		// Sample implementation:
		// {
		//    if (IsDisposed)
		//        throw new ObjectDisposedException("ObjectName",
		//            "This object is being used after it has been disposed: this is an Error.");
		// }
	}
}
