// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The layout <see cref="LayoutResolutionWalk.Resolve{T}"/> found, the class whose layout
	/// it is, and the base-class map part resolution climbs from the requested class up to
	/// <c>CmObject</c>.
	/// </summary>
	internal sealed class LayoutResolutionResult<T> where T : class
	{
		internal LayoutResolutionResult(T layout, int classId, string className,
			Dictionary<string, string> baseClassMap)
		{
			Layout = layout;
			ClassId = classId;
			ClassName = className;
			BaseClassMap = baseClassMap;
		}

		internal T Layout { get; }

		internal int ClassId { get; }

		internal string ClassName { get; }

		internal Dictionary<string, string> BaseClassMap { get; }
	}

	/// <summary>
	/// The detail-layout fallback walk in the legacy <c>DataTree.GetTemplateForObjLayout</c>
	/// order: the requested name on the concrete class and each base class, then
	/// <c>default</c> starting at the concrete class's base.
	/// </summary>
	internal static class LayoutResolutionWalk
	{
		/// <summary>
		/// Walks the class chain calling <paramref name="lookup"/> with each class name and the
		/// name to try until it returns non-null. The base-class map keys are compared
		/// case-insensitively because <c>ViewDefinitionSourceSnapshot</c> copies them into a
		/// case-insensitive dictionary anyway.
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="mdc"/>,
		/// <paramref name="requestedName"/>, or <paramref name="lookup"/> is null.</exception>
		/// <exception cref="LayoutNotFoundException">Neither the requested layout nor the
		/// default layout exists in the class hierarchy.</exception>
		/// <exception cref="InvalidOperationException">The metadata class hierarchy contains a
		/// cycle.</exception>
		internal static LayoutResolutionResult<T> Resolve<T>(IFwMetaDataCache mdc,
			int classId, string requestedName, Func<string, string, T> lookup) where T : class
		{
			if (mdc == null)
				throw new ArgumentNullException(nameof(mdc));
			if (requestedName == null)
				throw new ArgumentNullException(nameof(requestedName));
			if (lookup == null)
				throw new ArgumentNullException(nameof(lookup));

			var baseClassMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var useName = requestedName;
			var clsid = classId;
			while (true)
			{
				var className = mdc.GetClassName(clsid);
				var layout = lookup(className, useName);
				if (layout != null)
				{
					AddAncestors(mdc, clsid, baseClassMap);
					return new LayoutResolutionResult<T>(layout, clsid, className, baseClassMap);
				}

				if (clsid == 0 && !string.Equals(useName, "default", StringComparison.Ordinal))
				{
					useName = "default";
					clsid = classId;
					className = mdc.GetClassName(clsid);
				}
				if (clsid == 0)
				{
					throw new LayoutNotFoundException("No matching layout found for class "
						+ className + " detail layout " + requestedName + ".");
				}
				var baseId = mdc.GetBaseClsId(clsid);
				if (baseId == clsid)
					throw new InvalidOperationException("The metadata class hierarchy contains a cycle.");
				baseClassMap[className] = mdc.GetClassName(baseId);
				clsid = baseId;
			}
		}

		// Part resolution may still need to climb from the layout's class upward.
		private static void AddAncestors(IFwMetaDataCache mdc, int classId,
			Dictionary<string, string> baseClassMap)
		{
			var ancestorClassId = classId;
			while (ancestorClassId != 0)
			{
				var baseId = mdc.GetBaseClsId(ancestorClassId);
				if (baseId == ancestorClassId)
					break;
				baseClassMap[mdc.GetClassName(ancestorClassId)] = mdc.GetClassName(baseId);
				if (baseId == 0)
					break;
				ancestorClassId = baseId;
			}
		}
	}
}
