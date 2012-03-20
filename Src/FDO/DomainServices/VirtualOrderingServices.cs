// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: VirtualOrderingServices.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class provides support code for virtual properties that can be manually re-ordered.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class VirtualOrderingServices
	{

		static string CheckParentAndGetFieldName(ICmObject parent, int flid)
		{
			if (!parent.IsValidObject || parent.Cache == null)
				throw new FDOCacheUnusableException("There is no FdoCache for the parent object.");

			// TODO GJM: will this change when we're using virtual properties?
			var mdc = parent.Cache.MetaDataCacheAccessor;
			var fieldName = mdc.GetFieldName(flid);
			if (mdc.GetFieldId2(parent.ClassID, fieldName, true) == 0)
				throw new FDOInvalidFieldException("'flid' not found for this 'parent'.");

			return fieldName;
		}

		/// <summary>
		/// Reports true if the parent has the virtual field fieldName and there is a virtual
		/// ordering of it's objects. The virtual order is kept in the fwdata file xml in
		/// an <rt/> with class="VirtualOrdering".
		/// Example: Virtual ordering of the "VisibleComplexFormBackRefs" field of a LexEntry
		/// happens when the user "moves" one of the sequence's Complex Form references to the left or right.
		/// By default they are not ordered "virtually", but by Complex Form Type.
		/// This method allows XmlVcDisplayVec.Display() to know when to use this sort or the default.
		/// </summary>
		/// <param name="parent">The source of the field with name fieldName.</param>
		/// <param name="fieldName">The field with the sequence that might have a virtual order.</param>
		/// <returns>true if the field's sequence was ordered virtually.</returns>
		public static bool HasVirtualOrdering(ICmObject parent, string fieldName)
		{
			IVirtualOrdering virtualOrder;
			return TryGetVO(parent, fieldName, out virtualOrder);
		}

		static bool TryGetVO(ICmObject parent, string fieldName, out IVirtualOrdering myvo)
		{
			var voRepo = parent.Cache.ServiceLocator.GetInstance<IVirtualOrderingRepository>();
			myvo = voRepo.AllInstances().Where(
				vo => vo.SourceRA == parent && vo.Field == fieldName).FirstOrDefault();
			return myvo != null;
		}

		/// <summary>
		/// Template version to handle other types.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static IEnumerable<T> GetOrderedValue<T>(ICmObject parent, int flid, IEnumerable<T> unmodifiedSeq)
		{
			return GetOrderedValue(parent, flid, unmodifiedSeq.Cast<ICmObject>()).Cast<T>();
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Takes a parent object, a flid, and the sequence of objects that would otherwise be
		/// returned for the value. It checks for a VirtualOrdering with matching parent and field.
		/// If none is found, it returns the value passed in.
		/// If there is an override, it returns the items of the virtual ordering (removing any that
		/// are not part of the sequence passed in), followed by any of the items passed in that are
		/// not in the items of the virtual ordering.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="flid"></param>
		/// <param name="unmodifiedSeq"></param>
		/// ----------------------------------------------------------------------------------------
		public static IEnumerable<ICmObject> GetOrderedValue(ICmObject parent, int flid, IEnumerable<ICmObject> unmodifiedSeq)
		{
			var fieldName = CheckParentAndGetFieldName(parent, flid);

			IVirtualOrdering myvo;
			if (!TryGetVO(parent, fieldName, out myvo))
				return unmodifiedSeq; // No VO exists. Pass sequence through.
			var voList = myvo.ItemsRS.ToList();
			var inputList = unmodifiedSeq.ToList();
			var result = voList.Where(inputList.Contains).ToList();
			// convert to non-LINQ to debug.
			result.AddRange(inputList.Where(inputObj => !voList.Contains(inputObj)));
			return result;
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Takes a parent object, a flid, and a virtual sequence of objects from that property.
		/// It checks for a VirtualOrdering with matching parent and field.
		/// If none is found, it creates one with the desired sequence of objects.
		/// If there is an override, it replaces the items of the virtual ordering with the new
		/// desired sequence.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="flid"></param>
		/// <param name="desiredVirtualSeq">If null, this will call ResetVO to delete the
		/// Virtual Ordering and thereby return the property to its default ordering.</param>
		/// ----------------------------------------------------------------------------------------
		public static void SetVO(ICmObject parent, int flid, IEnumerable<ICmObject> desiredVirtualSeq)
		{
			if (desiredVirtualSeq == null)
			{
				ResetVO(parent, flid);
				return;
			}
			var fieldName = CheckParentAndGetFieldName(parent, flid);

			// See if this parent/flid combo has a Virtual Ordering already
			IVirtualOrdering myvo;
			if (TryGetVO(parent, fieldName, out myvo))
			{
				// Replace existing Virtual Ordering with new desired sequence.
				var cobjs = myvo.ItemsRS.Count;
				myvo.ItemsRS.Replace(0, cobjs, desiredVirtualSeq);
			}
			else
			{
				// No existing Virtual Ordering; create one.
				var voFact = parent.Cache.ServiceLocator.GetInstance<IVirtualOrderingFactory>();
				voFact.Create(parent, fieldName, desiredVirtualSeq);
			}
			RegisterVirtualChanged(parent, flid, fieldName);
		}

		private static void RegisterVirtualChanged(ICmObject parent, int flid, string fieldName)
		{
			var currentValue = (from hvo in ((ISilDataAccessManaged) parent.Cache.DomainDataByFlid).VecProp(parent.Hvo, flid)
				select parent.Services.GetObject(hvo)).ToArray();
			parent.Services.GetInstance<IUnitOfWorkService>().RegisterVirtualAsModified(parent,
				fieldName, currentValue);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Reset this virtual property to its default ordering by deleting any virtual ordering
		/// object for this parent-flid combination.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="flid"></param>
		/// ----------------------------------------------------------------------------------------
		public static void ResetVO(ICmObject parent, int flid)
		{
			var fieldName = CheckParentAndGetFieldName(parent, flid);

			IVirtualOrdering myvo;
			if (!TryGetVO(parent, fieldName, out myvo))
				return; // Nothing to reset
			myvo.Delete();
			RegisterVirtualChanged(parent, flid, fieldName);
		}

	}
}
