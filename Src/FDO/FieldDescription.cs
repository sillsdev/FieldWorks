// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FieldDescription.cs
// Responsibility: Randy Regnier

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using System.Reflection;
using System.Collections;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Class that represents one row in what used to be the Field$ table.
	/// </summary>
	public class FieldDescription
	{
		#region Data members
/*
Field$
	[Id] [int] NOT NULL , ********
	[Type] [int] NOT NULL , ********
	[Class] [int] NOT NULL , ********
	[DstCls] [int],
	[Name] [nvarchar] (100) NOT NULL , ********
	[Custom] [tinyint] NOT NULL, ********
	[CustomId] [uniqueidentifier],
	[Min] [bigint],
	[Max] [bigint],
	[Big] [bit],
	[UserLabel] [nvarchar] (100),
	[HelpString] [nvarchar] (100),
	[ListRootId] [int],
	[WsSelector] [int],
	[XmlUI] [ntext],
bigint - Integer (whole number) data from -2^63 (-9223372036854775808) through 2^63-1 (9223372036854775807). Storage size is 8 bytes.
int - Integer (whole number) data from -2^31 (-2,147,483,648) through 2^31 - 1 (2,147,483,647). Storage size is 4 bytes. The SQL-92 synonym for int is integer.
smallint - Integer data from -2^15 (-32,768) through 2^15 - 1 (32,767). Storage size is 2 bytes.
tinyint - Integer data from 0 through 255. Storage size is 1 byte.
*/
		private int m_id;
		private CellarPropertyType m_type;
		private int m_class;
		private int m_dstCls;
		private string m_name; // max length is 100.
		private byte m_custom;
		private Guid m_customId = Guid.Empty;
		private long m_min;
		private long m_max;
		private bool m_big;
		private string m_userlabel; // max length is 100.
		private string m_helpString; // max length is 100.
		private Guid m_listRootId = Guid.Empty;
		private int m_wsSelector;
		private string m_xmlUI;
		private bool m_isDirty = false;
		private bool m_doDelete = false;
		private readonly FdoCache m_cache;

		#endregion Data members

		#region Properties

		/// <summary>
		///
		/// </summary>
		public bool IsCustomField
		{
			get { return (m_custom != 0); }
		}

		/// <summary>
		///
		/// </summary>
		public bool IsDirty
		{
			get { return m_isDirty; }
		}

		/// <summary>
		///
		/// </summary>
		public bool IsInstalled
		{
			get { return m_id > 0; }
		}

		/// <summary>
		/// Mark a row for deletion from the database
		/// </summary>
		/// <exception cref="ApplicationException">
		/// Thrown if the row is a builtin field.
		/// </exception>
		public bool MarkForDeletion
		{
			get { return m_doDelete; }
			set
			{
				if (!IsCustomField)
					throw new ApplicationException("Builtin fields cannot be deleted.");
				m_doDelete = value;
				m_isDirty = true;
			}
		}

		/// <summary>
		/// Id of the field description.
		/// </summary>
		public int Id
		{
			get { return m_id; }
		}

		/// <summary>
		/// The type of field.
		/// </summary>
		public CellarPropertyType Type
		{
			get { return m_type; }
			set
			{
				CheckNotNull(value);
				if (m_type != value)
					m_isDirty = true;
				m_type = value;
			}
		}

		/// <summary>
		/// Class of the field.
		/// </summary>
		public int Class
		{
			get { return m_class; }
			set
			{
				CheckNotNull(value);
				if (m_class != value)
					m_isDirty = true;
				m_class = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int DstCls
		{
			get { return m_dstCls; }
			set
			{
				if (m_dstCls != value)
					m_isDirty = true;
				m_dstCls = value;
			}
		}

		/// <summary>
		/// Field name.  Should be unique within a class (and its subclasses).
		/// </summary>
		public string Name
		{
			get { return m_name; }
			set
			{
				if (string.IsNullOrEmpty(value))
					throw new ArgumentException("The name cannot be null or empty!");
				string sOldName = m_name;
				m_name = value.Length > 100 ? value.Substring(0, 100) : value;
				if (sOldName != m_name)
					m_isDirty = true;
			}
		}

		/// <summary>
		///
		/// </summary>
		public byte Custom
		{
			get { return m_custom; }
		}

		/// <summary>
		/// Guid for custom field.
		/// </summary>
		public Guid CustomId
		{
			get { return m_customId; }
		}

		/// <summary>
		///
		/// </summary>
		public long Min
		{
			get { return m_min; }
			set
			{
				if (m_min != value)
					m_isDirty = true;
				m_min = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public long Max
		{
			get { return m_max; }
			set
			{
				if (m_max != value)
					m_isDirty = true;
				m_max = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool Big
		{
			get { return m_big; }
			set
			{
				if (m_big != value)
					m_isDirty = true;
				m_big = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public string Userlabel
		{
			get { return m_userlabel; }
			set
			{
				if (m_userlabel != value)
					m_isDirty = true;
				m_userlabel = value.Length > 100 ? value.Substring(0, 100) : value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public string HelpString
		{
			get { return m_helpString; }
			set
			{
				if (m_helpString != value)
					m_isDirty = true;
				m_helpString = value.Length > 100 ? value.Substring(0, 100) : value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public Guid ListRootId
		{
			get { return m_listRootId; }
			set
			{
				if (m_listRootId != value)
					m_isDirty = true;
				m_listRootId = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int WsSelector
		{
			get { return m_wsSelector; }
			set
			{
				if (m_wsSelector != value)
					m_isDirty = true;
				m_wsSelector = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public string XmlUI
		{
			get { return m_xmlUI; }
			set
			{
				if (m_xmlUI != value)
					m_isDirty = true;
				m_xmlUI = value;
			}
		}

		#endregion Properties

		#region Construction

		/// <summary>
		/// Constructor
		/// </summary>
		public FieldDescription(FdoCache cache)
		{
			Debug.Assert(cache != null);

			m_id = 0;
			m_custom = 1;
			m_cache = cache;
		}

		#endregion Construction

		#region Methods

		/// <summary>
		/// Update modified field or add a new one, but only if it is a custom field.
		/// </summary>
		public void UpdateCustomField()
		{
			// We do nothing for builtin fields or rows that have not been modified.
			if (m_isDirty && IsCustomField)
			{
				var mdc = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
				// TODO: Maybe check for required columns for custom fields.
				if (IsInstalled)
				{
					var uowService = ((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService;
					var sda = m_cache.ServiceLocator.GetInstance<ISilDataAccessManaged>();
					foreach (ICmObject obj in Objects)
					{
						//Register all objects as modified in case any contain blank versions of the custom property.
						//(blank versions are not added to the CustomProperties map and would otherwise be left in the file LT-12451)
						uowService.RegisterObjectAsModified(obj);
						if (m_cache.CustomProperties.ContainsKey(Tuple.Create(obj, m_id)))
						{
							// register the custom field as modified for this object, so that it is properly
							// updated on commit
							uowService.RegisterCustomFieldAsModified(obj, m_id);

							// delete all owned objects for this custom field if it is being deleted
							if (m_doDelete)
							{
								switch (m_type)
								{
									case CellarPropertyType.OwningAtomic:
										{
											int hvo = sda.get_ObjectProp(obj.Hvo, m_id);
											if (hvo > 0)
												sda.DeleteObjOwner(obj.Hvo, hvo, m_id, 0);
										}
										break;

									case CellarPropertyType.OwningCollection:
									case CellarPropertyType.OwningSequence:
										foreach (int hvo in sda.VecProp(obj.Hvo, m_id))
											sda.DeleteObjOwner(obj.Hvo, hvo, m_id, 0);
										break;
									default:
										m_cache.CustomProperties.Remove(Tuple.Create(obj, m_id));
										break;
								}
							}
						}
					}

					// Update (or delete) existing row.
					if (m_doDelete)
						mdc.DeleteCustomField(m_id);
					else
						// Only update changeable fields.
						// Id, Type, Class, Name, Custom, and CustomId are not changeable by
						// the user, once they have been placed in the DB, so we won't
						// update them here no matter what.
						mdc.UpdateCustomField(m_id, m_helpString, m_wsSelector, m_userlabel);
				}
				else
				{
					string sClass = mdc.GetClassName(m_class);
					var ft = m_type;
					if (string.IsNullOrEmpty(m_name) && !string.IsNullOrEmpty(m_userlabel))
					{
						// AddCustomField (below) will need to make sure that this particular
						// name isn't already used, since the user may have changed another field's
						// user label and we check that they are unique, but the name won't have been changed.
						// see FWR-2804.
						m_name = m_userlabel;
					}
					m_id = mdc.AddCustomField(sClass, m_name, ft, m_dstCls, m_helpString, m_wsSelector, m_listRootId);
					mdc.UpdateCustomField(m_id, m_helpString, m_wsSelector, m_userlabel);
					if (mdc.IsValueType(ft))
					{
						// Basic data properties must be written out on all objects. Mark them dirty to ensure this.
						var uowService = ((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService;
						foreach (var obj in Objects)
							uowService.RegisterObjectAsModified(obj);
					}
				}
			}
		}

		/// <summary>
		/// Count the number of times this custom field contains data.
		/// </summary>
		/// <returns></returns>
		public int DataOccurrenceCount
		{
			get
			{
				int count = 0;
				if (IsInstalled && IsCustomField)
					count = Objects.Count(obj => m_cache.CustomProperties.ContainsKey(Tuple.Create(obj, m_id)) && DataNotNull(m_cache.CustomProperties[Tuple.Create(obj, m_id)]));
				return count;
			}
		}

		private bool DataNotNull(object o)
		{
			if (o is CmObjectIdWithHvo)
				o = (o as CmObjectIdWithHvo).GetObject(m_cache.ServiceLocator.GetInstance<ICmObjectRepository>());
			if (o is MultiUnicodeAccessor)
			{
				return (o as MultiUnicodeAccessor).StringCount > 0;
			}
			else if (o is IStText)
			{
				IStText txt = o as IStText;
				if (txt.ParagraphsOS.Count > 1)
					return true;
				IStTxtPara para = txt.ParagraphsOS[0] as IStTxtPara;
				return para != null && para.Contents != null && para.Contents.Length > 0;
			}
			else if (o is FdoSet<ICmObject>)
			{
				return (o as FdoSet<ICmObject>).Count > 0;
			}
			else if (o is GenDate)
			{
				return !((GenDate)o).IsEmpty;
			}
			else if (o is int)
			{
				return (int)o != 0;
			}
			return true;
		}

		private IEnumerable<ICmObject> Objects
		{
			get
			{
				var mdc = (IFwMetaDataCacheManaged) m_cache.MetaDataCache;
				Type repoType0 = GetServicesFromFWClass.GetRepositoryTypeFromFWClassID(mdc, m_class);
				var repoInternal = m_cache.ServiceLocator.GetInstance(repoType0);
				Type repoType = repoInternal.GetType();
				MethodInfo miAllInstances = repoType.GetMethod("AllInstances", System.Type.EmptyTypes);
				var objs = (IEnumerable) miAllInstances.Invoke(repoInternal, null);
				return objs.Cast<ICmObject>();
			}
		}

		private static void CheckNotNull(object value)
		{
			if (value == null)
				throw new ArgumentNullException("value", "The value for the property cannot be null.");
		}

		#endregion Methods

		#region Static methods

		static List<FieldDescription> s_fieldDescriptors;

		/// <summary>
		/// Forget anything you know about
		/// </summary>
		public static void ClearDataAbout()
		{
			s_fieldDescriptors = null;
		}

		/// <summary>
		/// Static method that returns an array of FieldDescriptor objects,
		/// where each one represents a row in the Field$ table.
		/// </summary>
		/// <param name="cache">FDO cache to collect the data from.</param>
		/// <returns>A List of FieldDescription objects,
		/// where each object in the array represents a row in the Field$ table.</returns>
		public static IEnumerable<FieldDescription> FieldDescriptors(FdoCache cache)
		{
			Debug.Assert(cache != null);
			if (s_fieldDescriptors != null)
				return s_fieldDescriptors;

			var mdc = cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			s_fieldDescriptors = (from flid in mdc.GetFieldIds()
				select new FieldDescription(cache)
				{
					  m_id = flid,
					  m_type = (CellarPropertyType)mdc.GetFieldType(flid),
					  m_class = mdc.GetOwnClsId(flid),
					  m_dstCls = mdc.GetDstClsId(flid),
					  m_name = mdc.GetFieldName(flid),
					  m_custom = mdc.IsCustom(flid) ? (byte)1 : (byte)0,
					  m_helpString = mdc.GetFieldHelp(flid),
					  m_wsSelector = mdc.GetFieldWs(flid),
					  m_listRootId = mdc.GetFieldListRoot(flid),
					  // TODO (DamienD): The following are not used in the new system. Get rid of them?
					  m_customId = Guid.Empty,
					  m_min = 0,
					  m_max = 0,
					  m_big = false,
					  m_userlabel = mdc.GetFieldLabel(flid),
					  m_xmlUI = mdc.GetFieldXml(flid)
				}).ToList();
			return s_fieldDescriptors;
		}

		#endregion Static methods
	}
}
