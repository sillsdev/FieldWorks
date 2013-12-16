// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BitmapStatus.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Text;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	#region Class BitmapStatus

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// A status that can be converted either to an integer or to a bitmap.
	/// </summary>
	/// <remarks>Public for tests</remarks>
	/// ------------------------------------------------------------------------------------
	public abstract class BitmapStatus : IConvertible, IComparable
	{
		/// <summary></summary>
		protected int m_Status;

		#region Constructor

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BitmapStatus"/> struct.
		/// </summary>
		/// <param name="status">The status.</param>
		/// --------------------------------------------------------------------------------
		public BitmapStatus(int status)
		{
			m_Status = status;
		}
		#endregion

		#region Protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts to image.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected abstract Image ConvertToImage();
		#endregion

		#region Implicit cast methods

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from
		/// <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.BitmapStatus"/> to <see cref="T:System.Int32"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// --------------------------------------------------------------------------------
		public static implicit operator int(BitmapStatus status)
		{
			return status.m_Status;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Performs an implicit conversion from
		/// <see cref="T:SIL.FieldWorks.TE.TeEditorialChecks.BitmapStatus"/> to <see cref="T:Image"/>.
		/// </summary>
		/// <param name="status">The status.</param>
		/// <returns>The result of the conversion.</returns>
		/// --------------------------------------------------------------------------------
		public static implicit operator Image(BitmapStatus status)
		{
			return status.ConvertToImage();
		}
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the
		/// current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current
		/// <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current
		/// <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/>
		/// parameter is null.</exception>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			if (obj == null)
				throw new NullReferenceException();
			if (obj is BitmapStatus)
				return CompareTo(obj) == 0;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			// we need to override this method just to make the compiler happy. Since we don't
			// really add any member variables we can just as well use the base implementation.
			return base.GetHashCode();
		}
		#endregion

		#region IConvertible Members

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Returns the <see cref="T:System.TypeCode"></see> for this instance.
		/// </summary>
		/// <returns>
		/// The enumerated constant that is the <see cref="T:System.TypeCode"></see> of the
		/// class or value type that implements this interface.
		/// </returns>
		/// --------------------------------------------------------------------------------
		TypeCode IConvertible.GetTypeCode()
		{
			return TypeCode.Int32;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent Boolean value using the
		/// specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// A Boolean value equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		bool IConvertible.ToBoolean(IFormatProvider provider)
		{
			return (bool)((IConvertible)this).ToType(typeof(bool), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent 8-bit unsigned integer
		/// using the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An 8-bit unsigned integer equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		byte IConvertible.ToByte(IFormatProvider provider)
		{
			return (byte)((IConvertible)this).ToType(typeof(byte), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent Unicode character using the
		/// specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// A Unicode character equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		char IConvertible.ToChar(IFormatProvider provider)
		{
			return (char)((IConvertible)this).ToType(typeof(char), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent
		/// <see cref="T:System.DateTime"></see> using the specified culture-specific
		/// formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// A <see cref="T:System.DateTime"></see> instance equivalent to the value of this
		/// instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		DateTime IConvertible.ToDateTime(IFormatProvider provider)
		{
			return (DateTime)((IConvertible)this).ToType(typeof(DateTime), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent
		/// <see cref="T:System.Decimal"></see> number using the specified culture-specific
		/// formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// A <see cref="T:System.Decimal"></see> number equivalent to the value of this
		/// instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		decimal IConvertible.ToDecimal(IFormatProvider provider)
		{
			return (decimal)((IConvertible)this).ToType(typeof(decimal), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent double-precision
		/// floating-point number using the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// A double-precision floating-point number equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		double IConvertible.ToDouble(IFormatProvider provider)
		{
			return (double)((IConvertible)this).ToType(typeof(double), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent 16-bit signed integer using
		/// the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An 16-bit signed integer equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		short IConvertible.ToInt16(IFormatProvider provider)
		{
			return (short)((IConvertible)this).ToType(typeof(short), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent 32-bit signed integer using
		/// the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An 32-bit signed integer equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		int IConvertible.ToInt32(IFormatProvider provider)
		{
			return (int)m_Status;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent 64-bit signed integer using
		/// the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An 64-bit signed integer equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		long IConvertible.ToInt64(IFormatProvider provider)
		{
			return (long)((IConvertible)this).ToType(typeof(long), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent 8-bit signed integer using
		/// the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An 8-bit signed integer equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		sbyte IConvertible.ToSByte(IFormatProvider provider)
		{
			return (sbyte)((IConvertible)this).ToType(typeof(sbyte), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent single-precision
		/// floating-point number using the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// A single-precision floating-point number equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		float IConvertible.ToSingle(IFormatProvider provider)
		{
			return (float)((IConvertible)this).ToType(typeof(float), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent
		/// <see cref="T:System.String"></see> using the specified culture-specific
		/// formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// A <see cref="T:System.String"></see> instance equivalent to the value of this
		/// instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		string IConvertible.ToString(IFormatProvider provider)
		{
			return (string)((IConvertible)this).ToType(typeof(string), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an <see cref="T:System.Object"></see> of
		/// the specified <see cref="T:System.Type"></see> that has an equivalent value,
		/// using the specified culture-specific formatting information.
		/// </summary>
		/// <param name="conversionType">The <see cref="T:System.Type"></see> to which the
		/// value of this instance is converted.</param>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An <see cref="T:System.Object"></see> instance of type conversionType whose
		/// value is equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning a reference")]
		object IConvertible.ToType(Type conversionType, IFormatProvider provider)
		{
			if (conversionType == typeof(Image))
				return (Image)this;
			else if (conversionType.IsAssignableFrom(typeof(int)))
				return Convert.ChangeType((int)this, conversionType, provider);
			return null;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent 16-bit unsigned integer
		/// using the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An 16-bit unsigned integer equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		ushort IConvertible.ToUInt16(IFormatProvider provider)
		{
			return (ushort)((IConvertible)this).ToType(typeof(ushort), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent 32-bit unsigned integer
		/// using the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An 32-bit unsigned integer equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		uint IConvertible.ToUInt32(IFormatProvider provider)
		{
			return (uint)((IConvertible)this).ToType(typeof(uint), provider);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Converts the value of this instance to an equivalent 64-bit unsigned integer
		/// using the specified culture-specific formatting information.
		/// </summary>
		/// <param name="provider">An <see cref="T:System.IFormatProvider"></see> interface
		/// implementation that supplies culture-specific formatting information.</param>
		/// <returns>
		/// An 64-bit unsigned integer equivalent to the value of this instance.
		/// </returns>
		/// --------------------------------------------------------------------------------
		ulong IConvertible.ToUInt64(IFormatProvider provider)
		{
			return (ulong)((IConvertible)this).ToType(typeof(ulong), provider);
		}

		#endregion

		#region IComparable Members

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Compares the current instance with another object of the same type.
		/// </summary>
		/// <param name="obj">An object to compare with this instance.</param>
		/// <returns>
		/// A 32-bit signed integer that indicates the relative order of the objects being
		/// compared. The return value has these meanings:
		/// Less than zero = This instance is less than obj.
		/// Zero = This instance is equal to obj.
		/// Greater than zero = This instance is greater than obj.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">obj is not the same type as this
		/// instance. </exception>
		/// --------------------------------------------------------------------------------
		public int CompareTo(object obj)
		{
			if (obj == null)
				return 1;

			if (obj is BitmapStatus)
				return ((int)this).CompareTo((int)((BitmapStatus)obj));

			throw new ArgumentException();
		}

		#endregion
	}

	#endregion

}
