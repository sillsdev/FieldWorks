// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FilterStrategy.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
//	"Strategy" comes from the design pattern of the name ("Design patterns" (gang of 4))
//	Filters will be used to decide on whether to dump a particular object based on such
//		things as
//			Different sensitivities to the completeness/correctness of the object (especially
//			when feeding the parser).
//			User-defined properties, such as which lexical items are to be
//				part of a particular dictionary.
// </remarks>

using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// A FilterStrategy is a class which determines whether the FXT Dumper should output an object or not
	/// </summary>



	/// <summary>
	/// EngineDumpFilterStrategy is what we want when the dumped material is going to some engine, for example, XAmple.
	/// it will filter out anything that
	/// 1) admits to being invalid
	/// 2) (eventually) has an annotation specifically disabling it
	/// </summary>
	public interface IFilterStrategy
	{
		/// <summary>
		/// determine whether the object should be included in the output
		/// </summary>
		/// <param name="obj">the object in question</param>
		/// <param name="filterReason">any explanation of why the object should not be included</param>
		/// <returns>true if the object should be included in the output</returns>
		bool DoInclude(ICmObject obj, out string filterReason);
		string Label
		{
			get;
		}
	}

	/// <summary>
	/// This filters out objects which do not pass of their "SatisfiesConstraint" method.
	/// </summary>
	public class ConstraintFilterStrategy : IFilterStrategy
	{
		public ConstraintFilterStrategy()
		{

		}

		public string Label
		{
			get
			{
				return "Constraint-Filter";
			}
		}

		/// <summary>
		/// determine whether the object should be included in the output
		/// </summary>
		/// <param name="obj">the object in question</param>
		/// <param name="filterReason">any explanation of why the object should not be included</param>
		/// <returns>true if the object should be included in the output</returns>
		public bool DoInclude (ICmObject obj, out string filterReason)
		{
			ConstraintFailure failure;
			//discussion: should we were first check can to see if there are already errors?
			//  pro: it would be faster on the relatively few objects that already have error annotations
			//	con: it would allow a error which is no longer true to live on, causing problems
			//	decision: just go ahead in check them every time.
			if (!obj.CheckConstraints(0, false, out failure))
			{
				filterReason = failure.GetMessage();
				return false;
			}
			filterReason=null;
			return true;
		}
	}
}
