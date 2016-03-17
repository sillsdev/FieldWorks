// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Check to make sure an IFlexComponent can be initialized.
	/// </summary>
	public static class FlexComponentCheckingService
	{
		/// <summary>
		/// Check to make sure an IFlexComponent can be initialized.
		/// </summary>
		public static void CheckInitializationValues(FlexComponentParameterObject sourceFlexComponentParameterObject,
			FlexComponentParameterObject targetFlexComponentParameterObject)
		{
			if (sourceFlexComponentParameterObject == null)
				throw new ArgumentNullException("sourceFlexComponentParameterObject");
			if (targetFlexComponentParameterObject == null)
				throw new ArgumentNullException("targetFlexComponentParameterObject");

			// The three source values must not be null.
			if (sourceFlexComponentParameterObject.PropertyTable == null) throw new InvalidOperationException("No source property table.");
			if (sourceFlexComponentParameterObject.Publisher == null) throw new InvalidOperationException("No source publisher.");
			if (sourceFlexComponentParameterObject.Subscriber == null) throw new InvalidOperationException("No source subscriber.");

			// Three target values must be null.
			if (targetFlexComponentParameterObject.PropertyTable != null) throw new InvalidOperationException("target property table must be null");
			if (targetFlexComponentParameterObject.Publisher != null) throw new InvalidOperationException(" target publisher must be null");
			if (targetFlexComponentParameterObject.Subscriber != null) throw new InvalidOperationException("target subscriber must be null");
		}
	}
}
