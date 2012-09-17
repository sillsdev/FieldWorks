// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IDomainObjectDTORepository.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// <summary>
	/// Repository for accessing all DomainObjectDTO instances,
	/// during a data migration.
	/// </summary>
	internal interface IDomainObjectDTORepository
	{
		/// <summary>
		/// Get or set the model version number of the starting point
		/// of the data in the repository.
		///
		/// The original starting point is set in BEP-land.
		/// The data migration stes *must* increment it after it has done its work.
		/// </summary>
		int CurrentModelVersion { get; set; }

		/// <summary>
		/// Get the metadata cache for the repository.
		/// </summary>
		IFwMetaDataCacheManaged MDC { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the <see cref="DomainObjectDTO"/> with the specified Guid (as string).
		/// </summary>
		/// <param name="guid">The guid of the <see cref="DomainObjectDTO"/> as a string.</param>
		/// <returns>
		/// The <see cref="DomainObjectDTO"/> with the given <paramref name="guid"/>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the requested object is not in the repository.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		DomainObjectDTO GetDTO(string guid);

		/// <summary>
		/// Try to get the <see cref="DomainObjectDTO"/> with the given
		/// <paramref name="guid"/>.
		/// </summary>
		/// <param name="guid">The guid for the sought after <see cref="DomainObjectDTO"/>.</param>
		/// <param name="dtoWithGuid">The sought after <see cref="DomainObjectDTO"/>,
		/// or null, if not found.</param>
		/// <returns>'true' if the object exists, otherwise 'false'.</returns>
		bool TryGetValue(string guid, out DomainObjectDTO dtoWithGuid);

		/// <summary>
		/// Try to get the owning DTO <see cref="DomainObjectDTO"/> of the given
		/// <paramref name="guid"/>.
		/// </summary>
		/// <param name="guid">The guid for the sought after owning <see cref="DomainObjectDTO"/>.</param>
		/// <param name="owningDto">The sought after <see cref="DomainObjectDTO"/>,
		/// or null, if no onwer at all.</param>
		/// <returns>'true' if the owner exists, otherwise 'false'.</returns>
		bool TryGetOwner(string guid, out DomainObjectDTO owningDto);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the owner <see cref="DomainObjectDTO"/> for the specified object.
		/// </summary>
		/// <param name="ownedObj">The owned <see cref="DomainObjectDTO"/>.</param>
		/// <returns>
		/// The owner <see cref="DomainObjectDTO"/> for the given <paramref name="ownedObj"/>,
		/// or null, if there is no owner.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the owned object is not in the repository.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		DomainObjectDTO GetOwningDTO(DomainObjectDTO ownedObj);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all directly owned objects of the specified <paramref name="guid"/>.
		/// </summary>
		/// <param name="guid">The owning guid.</param>
		/// <returns>
		/// An enumeration of zero, or more <see cref="DomainObjectDTO"/> owned objects.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the Guid is not in the repository.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		IEnumerable<DomainObjectDTO> GetDirectlyOwnedDTOs(string guid);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all instances of the specified <paramref name="classname"/>,
		/// but *not* its subclasses.
		/// </summary>
		/// <param name="classname">The class of instances to get.</param>
		/// <returns>
		/// An enumeration of zero, or more <see cref="DomainObjectDTO"/> instances
		/// of the given class.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		IEnumerable<DomainObjectDTO> AllInstancesSansSubclasses(string classname);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all instances of the specified <paramref name="classname"/>,
		/// but *with* its subclasses.
		/// </summary>
		/// <param name="classname">The class of instances to get, including subclasses.</param>
		/// <returns>
		/// An enumeration of zero, or more <see cref="DomainObjectDTO"/> instances
		/// of the given class.
		/// </returns>
		/// <remarks>
		/// NB: The subclass structure is that of the current model version,
		/// which may not match that of the data bieng migrated.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		IEnumerable<DomainObjectDTO> AllInstancesWithSubclasses(string classname);

		/// <summary>
		/// Equivalent to AllInstancesWithSubclasses("CmObject") but more efficient and less
		/// likely to cause out-of-memory through large object heap fragmentation.
		/// </summary>
		/// <returns></returns>
		IEnumerable<DomainObjectDTO> AllInstancesWithValidClasses();

		/// <summary>
		/// Get all instances, including ones that are no longer in the model.
		/// This will not return DTOs that have been deleted, however.
		/// </summary>
		IEnumerable<DomainObjectDTO> AllInstances();

		/// <summary>
		/// Add a new <see cref="DomainObjectDTO"/> to the repository.
		/// </summary>
		/// <param name="newby">The new object to add.</param>
		void Add(DomainObjectDTO newby);

		/// <summary>
		/// Remove a 'deleted' <see cref="DomainObjectDTO"/> from the repository.
		///
		/// The deletion of the underlying CmObject object won't happen,
		/// until the entire current migration is finished.
		/// </summary>
		/// <param name="goner">The object being deleted.</param>
		void Remove(DomainObjectDTO goner);

		/// <summary>
		/// Let the Repository know that <paramref name="dirtball"/> has been modified.
		/// </summary>
		/// <param name="dirtball">The object that was modified.</param>
		/// <remarks>
		/// The underlying CmObject won't be changed, until the end of the current
		/// migration is finished.
		/// </remarks>
		void Update(DomainObjectDTO dirtball);

		/// <summary>
		/// Let the Repository know that <paramref name="dirtball"/> has been modified,
		/// by at least a change of class.
		/// </summary>
		/// <param name="dirtball">The object that was modified.</param>
		/// <param name="oldClassStructure">Previous superclass structure</param>
		/// <param name="newClassStructure">New class structure</param>
		/// <remarks>
		/// The underlying CmObject won't be changed, until the end of the current
		/// migration is finished.
		/// </remarks>
		void Update(DomainObjectDTO dirtball, ClassStructureInfo oldClassStructure, ClassStructureInfo newClassStructure);

		void ChangeClass(DomainObjectDTO dirtball, string oldClassName);

		/// <summary>
		/// Get the count of dtos in the repository.
		/// </summary>
		int Count
		{ get; }

		/// <summary>
		/// Merge the three internal sets (newbies, dirtballs, and goners) into correct sets,
		/// so a DTO is only in one of the sets.
		/// </summary>
		void EnsureItemsInOnlyOneSet();

		/// <summary>
		/// Check whether the fieldName is in use.
		/// </summary>
		/// <returns></returns>
		bool IsFieldNameUsed(string className, string fieldName);

		/// <summary>
		/// Create a custom field using the given values.
		/// </summary>
		void CreateCustomField(string className, string fieldName, SIL.CoreImpl.CellarPropertyType cpt,
			int destClid, string helpString, int wsSelector, Guid listRoot);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path of the local project folder (typically a subdirectory of the Projects
		/// parent folder).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ProjectFolder { get; }
	}

	internal sealed class DomainObjectDtoRepository : IDomainObjectDTORepository
	{
		private readonly HashSet<DomainObjectDTO> m_dtos;
		private readonly Dictionary<string, DomainObjectDTO> m_dtoByGuid;
		private readonly Dictionary<string, HashSet<string>> m_classesAndTheirDirectSubclasses = new Dictionary<string, HashSet<string>>();
		/// <summary>Class name is the key, superclass is the value of that.</summary>
		private readonly Dictionary<string, string> m_classAndSuperClass = new Dictionary<string, string>();
		/// <summary>
		/// For each class name that occurs on an element in the DTO collection, store a hash set of the instances
		/// which have exactly that class. (Unlike an earlier version, does NOT include instances of subclasses.)
		/// </summary>
		private readonly Dictionary<string, HashSet<DomainObjectDTO>> m_dtosByClass = new Dictionary<string, HashSet<DomainObjectDTO>>();
		private readonly HashSet<DomainObjectDTO> m_newbies = new HashSet<DomainObjectDTO>();
		private readonly HashSet<DomainObjectDTO> m_dirtballs = new HashSet<DomainObjectDTO>();
		private readonly HashSet<DomainObjectDTO> m_goners = new HashSet<DomainObjectDTO>();
		private int m_currentModelVersionNumber;
		private readonly HashSet<DomainObjectDTO> m_oldTimers = new HashSet<DomainObjectDTO>();
		private readonly string m_projectFolder;

		private readonly IFwMetaDataCacheManaged m_mdc;	// needed for some data migrations changing over to custom fields.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="startingModelVersionNumber">The starting model version number for the
		/// migration.</param>
		/// <param name="dtos">DTOs from BEP-land.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="projectFolder">The project folder (don't even think about trying to
		/// pass a path on a server other than the local machine, and -- yes -- I CAN control
		/// your thoughts!).</param>
		/// ------------------------------------------------------------------------------------
		internal DomainObjectDtoRepository(int startingModelVersionNumber, HashSet<DomainObjectDTO> dtos,
			IFwMetaDataCacheManaged mdc, string projectFolder)
		{
			if (dtos == null) throw new ArgumentNullException("dtos");
			if (mdc == null) throw new ArgumentNullException("mdc");

			m_currentModelVersionNumber = startingModelVersionNumber;
			m_dtos = dtos;
			m_mdc = mdc;
			m_projectFolder = projectFolder;

			// Add classes from MDC
			foreach (var clsid in mdc.GetClassIds())
			{
				// Leaf classes will have nothing in 'subclasses'.
				var className = mdc.GetClassName(clsid);
				m_dtosByClass.Add(className, new HashSet<DomainObjectDTO>());
				if (className == "CmObject")
					m_classAndSuperClass.Add(className, null);
				var subclasses = new HashSet<string>();
				m_classesAndTheirDirectSubclasses.Add(className, subclasses);
				var subclassIds = mdc.GetDirectSubclasses(clsid);
				if (subclassIds.Count() == 0)
				{
					if (!m_classAndSuperClass.ContainsKey(className))
						m_classAndSuperClass.Add(className, mdc.GetBaseClsName(clsid));
					continue;
				}
				foreach (var directSubClsid in subclassIds)
				{
					var directSubclassName = mdc.GetClassName(directSubClsid);
					subclasses.Add(directSubclassName);

					// added ContainsKey check because of mono bug https://bugzilla.novell.com/show_bug.cgi?id=539288
					// see also change in FdoIFwMetaDataCache.cs (AddClass methods replaced with AddClass1, AddClass2)
					// for simular reasons (order of types obtains via reflection)https://bugzilla.novell.com/show_bug.cgi?id=539288.
					if (!m_classAndSuperClass.ContainsKey(directSubclassName))
					{
						m_classAndSuperClass.Add(directSubclassName, className);
					}
				}
			}

#if ORIGINAL
			foreach (var classname in m_classesAndTheirDirectSubclasses.Keys)
			{
				// Some may have no instances.
				m_dtosByClass.Add(classname, (from dto in m_dtos
											  where dto.Classname == classname
											  select dto).ToList());
			}

			foreach (var dto in m_dtos)
				m_dtoByGuid.Add(dto.Guid.ToLower(), dto);
#else
			m_dtoByGuid = new Dictionary<string, DomainObjectDTO>(m_dtos.Count);
			foreach (var dto in m_dtos)
			{
				m_dtoByGuid.Add(dto.Guid.ToLower(), dto);
				AddToClassList(dto);
			}
#endif
		}

		private IDomainObjectDTORepository AsInterface
		{
			get { return this; }
		}

		private void AddClassnamesRecursively(ICollection<string> classnames, string classname)
		{
			classnames.Add(classname);
			foreach (var name in m_classesAndTheirDirectSubclasses[classname])
				AddClassnamesRecursively(classnames, name);
		}

		/// <summary>
		/// Only to be called by BEP.
		/// </summary>
		internal HashSet<DomainObjectDTO> Newbies
		{
			get { return m_newbies; }
		}

		/// <summary>
		/// Only to be called by BEP.
		/// </summary>
		internal HashSet<DomainObjectDTO> Dirtballs
		{
			get { return m_dirtballs; }
		}

		/// <summary>
		/// Only to be called by BEP.
		/// </summary>
		internal HashSet<DomainObjectDTO> Goners
		{
			get { return m_goners; }
		}

		#region Implementation of IDomainObjectDTORepository

		/// <summary>
		/// Get or set the model version number of the starting point of the data in the repository
		/// </summary>
		int IDomainObjectDTORepository.CurrentModelVersion
		{
			get { return m_currentModelVersionNumber; }
			set { m_currentModelVersionNumber = value; }
		}

		/// <summary>
		/// Get the metadata cache for the repository.
		/// </summary>
		public IFwMetaDataCacheManaged MDC
		{
			get { return m_mdc; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the <see cref="DomainObjectDTO"/> with the specified Guid (as string).
		/// </summary>
		/// <param name="guid">The guid of the <see cref="DomainObjectDTO"/> as a string.</param>
		/// <returns>
		/// The <see cref="DomainObjectDTO"/> with the given <paramref name="guid"/>.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the requested object is not in the repository.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		DomainObjectDTO IDomainObjectDTORepository.GetDTO(string guid)
		{
			DomainObjectDTO retval;
			if (!m_dtoByGuid.TryGetValue(guid.ToLower(), out retval))
				throw new ArgumentException("No object with the given guid", "guid");
			return retval;
		}

		/// <summary>
		/// Try to get the <see cref="DomainObjectDTO"/> with the given
		/// <paramref name="guid"/>.
		/// </summary>
		/// <param name="guid">The guid for the sought after <see cref="DomainObjectDTO"/>.</param>
		/// <param name="dtoWithGuid">The sought after <see cref="DomainObjectDTO"/>,
		/// or null, if not found.</param>
		/// <returns>'true' if the object exists, otherwise 'false'.</returns>
		bool IDomainObjectDTORepository.TryGetValue(string guid, out DomainObjectDTO dtoWithGuid)
		{
			return m_dtoByGuid.TryGetValue(guid.ToLower(), out dtoWithGuid);
		}

		/// <summary>
		/// Try to get the owning DTO <see cref="DomainObjectDTO"/> of the given
		/// <paramref name="guid"/>.
		/// </summary>
		/// <param name="guid">The guid for the sought after owning <see cref="DomainObjectDTO"/>.</param>
		/// <param name="owningDto">The sought after <see cref="DomainObjectDTO"/>,
		/// or null, if no onwer at all.</param>
		/// <returns>'true' if the owner exists, otherwise 'false'.</returns>
		bool IDomainObjectDTORepository.TryGetOwner(string guid, out DomainObjectDTO owningDto)
		{
			DomainObjectDTO ownedDto;
			if (!m_dtoByGuid.TryGetValue(guid.ToLower(), out ownedDto))
			{
				// DTO of 'guid' not found.
				owningDto = null;
				return false;
			}
			var ownIdx = ownedDto.XmlBytes.IndexOfSubArray(OwnerGuid);
			if (ownIdx < 0)
			{
				// Ownerless object.
				owningDto = null;
				return true;
			}

			return m_dtoByGuid.TryGetValue(
				Encoding.UTF8.GetString(ownedDto.XmlBytes.SubArray(ownIdx + 11, 36)).ToLower(),
				out owningDto);
		}

		private static readonly byte[] OwnerGuid = Encoding.UTF8.GetBytes("ownerguid=");
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the owner <see cref="DomainObjectDTO"/> for the specified object.
		/// </summary>
		/// <param name="ownedObj">The owned <see cref="DomainObjectDTO"/>.</param>
		/// <returns>
		/// The owner <see cref="DomainObjectDTO"/> for the given <paramref name="ownedObj"/>,
		/// or null, if there is no owner.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the owned object is not in the repository.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		DomainObjectDTO IDomainObjectDTORepository.GetOwningDTO(DomainObjectDTO ownedObj)
		{
			if (ownedObj == null) throw new ArgumentNullException("ownedObj");

			var ownIdx = ownedObj.XmlBytes.IndexOfSubArray(OwnerGuid);
			return (ownIdx < 0)
				? null
				: AsInterface.GetDTO(Encoding.UTF8.GetString(ownedObj.XmlBytes.SubArray(ownIdx + 11, 36)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all directly owned objects of the specified <paramref name="guid"/>.
		/// </summary>
		/// <param name="guid">The owning guid.</param>
		/// <returns>
		/// An enumeration of zero, or more <see cref="DomainObjectDTO"/> owned objects.
		/// </returns>
		/// <exception cref="ArgumentException">
		/// Thrown if the Guid is not in the repository.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		IEnumerable<DomainObjectDTO> IDomainObjectDTORepository.GetDirectlyOwnedDTOs(string guid)
		{
			var dto = AsInterface.GetDTO(guid);
			var rootElement = XElement.Parse(dto.Xml);
			return (from ownedSurrogates in rootElement.Descendants("objsur")
					where ownedSurrogates.Attribute("t").Value == "o"
					select AsInterface.GetDTO(ownedSurrogates.Attribute("guid").Value)).ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all instances of the specified <paramref name="classname"/>,
		/// but *not* its subclasses.
		/// </summary>
		/// <param name="classname">The class of instances to get.</param>
		/// <returns>
		/// An enumeration of zero, or more <see cref="DomainObjectDTO"/> instances
		/// of the given class.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		IEnumerable<DomainObjectDTO> IDomainObjectDTORepository.AllInstancesSansSubclasses(string classname)
		{
			HashSet<DomainObjectDTO> dtos;
			return m_dtosByClass.TryGetValue(classname, out dtos) ? dtos : Enumerable.Empty<DomainObjectDTO>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all instances of the specified <paramref name="classname"/>,
		/// but *with* its subclasses.
		/// </summary>
		/// <param name="classname">The class of instances to get, including subclasses.</param>
		/// <returns>
		/// An enumeration of zero, or more <see cref="DomainObjectDTO"/> instances
		/// of the given class.
		/// </returns>
		/// <remarks>
		/// NB: The subclass structure is that of the current model version,
		/// which may not match that of the data being migrated.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		IEnumerable<DomainObjectDTO> IDomainObjectDTORepository.AllInstancesWithSubclasses(string classname)
		{
			int cobj = 0;
			var classList = new HashSet<string>();
			AddClassnamesRecursively(classList, classname);
			foreach (var name in classList)
				cobj += m_dtosByClass[name].Count;
			List<DomainObjectDTO> retval = new List<DomainObjectDTO>(cobj);
			foreach (var name in classList)
				retval.AddRange(m_dtosByClass[name]);
			return retval;
		}

		List<DomainObjectDTO> m_dtosCopy = new List<DomainObjectDTO>();

		/// <summary>
		/// Equivalent to AllInstancesWithSubclasses("CmObject") but slightly more efficient and
		/// less likely to cause out-of-memory through large object heap fragmentation.
		/// </summary>
		/// <returns></returns>
		IEnumerable<DomainObjectDTO> IDomainObjectDTORepository.AllInstancesWithValidClasses()
		{
			m_dtosCopy.Clear();
			int needed = m_dtos.Count - m_oldTimers.Count;
			// In practice, tne number of valid objects tends to decrease from version 7000001
			// on, so the capacity will usually only be increased once.
			if (m_dtosCopy.Capacity < needed)
				m_dtosCopy.Capacity = needed;
			var classList = new HashSet<string>();
			AddClassnamesRecursively(classList, "CmObject");
			foreach (var name in classList)
				m_dtosCopy.AddRange(m_dtosByClass[name]);
			return m_dtosCopy;
		}

		/// <summary>
		/// Get all instances, including ones that are no longer in the model.
		/// This will not return DTOs that have been deleted, however.
		/// </summary>
		IEnumerable<DomainObjectDTO> IDomainObjectDTORepository.AllInstances()
		{
			return m_dtos;
		}

		/// <summary>
		/// Add a new <see cref="DomainObjectDTO"/> to the repository.
		/// </summary>
		/// <param name="newby">The new object to add.</param>
		void IDomainObjectDTORepository.Add(DomainObjectDTO newby)
		{
			if (newby == null) throw new ArgumentNullException("newby");

			// Will throw an exception, if it is already present,
			// which is just fine.
			m_dtoByGuid.Add(newby.Guid.ToLower(), newby);
			m_dtos.Add(newby);
			AddToClassList(newby);
			m_newbies.Add(newby);
		}

		private void AddToClassList(DomainObjectDTO dto)
		{
			var className = dto.Classname;
			string superclassName;
			if (!m_classAndSuperClass.TryGetValue(className, out superclassName))
			{
				// Can't determine old superclass, without going through the DTO's xml.
				m_classAndSuperClass.Add(className, null);
				// Discovering old direct subclasses may not be possible.
				m_classesAndTheirDirectSubclasses.Add(className, new HashSet<string>());
			}
			if (superclassName == null)
				// Unknown class, so must be obsolete.
				m_oldTimers.Add(dto);
			HashSet<DomainObjectDTO> instances;
			if (!m_dtosByClass.TryGetValue(className, out instances))
			{
				instances = new HashSet<DomainObjectDTO>();
				m_dtosByClass.Add(className, instances);
			}
			instances.Add(dto);
		}

		/// <summary>
		/// Remove a 'deleted' <see cref="DomainObjectDTO"/> from the repository.
		///
		/// The deletion of the underlying CmObject object won't happen,
		/// until the entire current migration is finished.
		/// </summary>
		/// <param name="goner">The object being deleted.</param>
		void IDomainObjectDTORepository.Remove(DomainObjectDTO goner)
		{
			if (goner == null) throw new ArgumentNullException("goner");

			m_goners.Add(goner);
			m_dtoByGuid.Remove(goner.Guid.ToLower());
			m_dtos.Remove(goner);
			RemoveFromClassList(goner);
		}

		private void RemoveFromClassList(DomainObjectDTO obj)
		{
			RemoveFromClassList(obj, obj.Classname);
		}
		private void RemoveFromClassList(DomainObjectDTO obj, string oldClassName)
		{
			HashSet<DomainObjectDTO> instances;
			if (m_dtosByClass.TryGetValue(oldClassName, out instances))
				instances.Remove(obj);
		}

		/// <summary>
		/// Let the Repository know that <paramref name="dirtball"/> has been modified.
		/// </summary>
		/// <param name="dirtball">The object that was modified.</param>
		/// <remarks>
		/// The underlying CmObject won't be changed, until the end of the current
		/// migration is finished.
		/// </remarks>
		void IDomainObjectDTORepository.Update(DomainObjectDTO dirtball)
		{
			if (dirtball == null) throw new ArgumentNullException("dirtball");
			if (!m_dtoByGuid.ContainsKey(dirtball.Guid.ToLower())) throw new InvalidOperationException("Can't update DTO that isn't in the system.");

			m_dirtballs.Add(dirtball);
		}

		/// <summary>
		/// Let the Repository know that <paramref name="dirtball"/> has been modified,
		/// by at least a change of class.
		/// </summary>
		/// <param name="dirtball">The object that was modified.</param>
		/// <param name="oldClassStructure">Previous superclass structure</param>
		/// <param name="newClassStructure">New class structure</param>
		/// <remarks>
		/// The underlying CmObject won't be changed, until the end of the current
		/// migration is finished.
		/// </remarks>
		public void Update(DomainObjectDTO dirtball, ClassStructureInfo oldClassStructure, ClassStructureInfo newClassStructure)
		{
			if (oldClassStructure == null) throw new ArgumentNullException("oldClassStructure");
			if (newClassStructure == null) throw new ArgumentNullException("newClassStructure");
			if (dirtball.Classname != newClassStructure.m_className) throw new ArgumentException("Mis-match between class names in 'dirtball' and 'newClassStructure'");

			AsInterface.Update(dirtball);
			RemoveFromClassList(dirtball, oldClassStructure.m_className);
			AddToClassList(dirtball);
		}

		public void ChangeClass(DomainObjectDTO dirtball, string oldClassName)
		{
			RemoveFromClassList(dirtball, oldClassName);
			AddToClassList(dirtball);
		}

		/// <summary>
		/// Get the count of dtos in the repository.
		/// </summary>
		public int Count
		{
			get { return m_dtos.Count; }
		}

		/// <summary>
		/// Merge the three internal sets (newbies, dirtballs, and goners) into correct sets,
		/// so a DTO is only in one of the sets.
		/// </summary>
		public void EnsureItemsInOnlyOneSet()
		{
			foreach (var newbie in m_newbies.ToArray()) // Copy with 'ToArray', because loop may modify newbies.
			{
				m_dirtballs.Remove(newbie); // New trumps dirty.
				if (!m_goners.Contains(newbie))
					continue;

				// Created and nuked in same main DM (perhaps in different individual steps),
				// so don't bother messing with it.
				m_newbies.Remove(newbie);
				m_goners.Remove(newbie);
			}
			foreach (var goner in m_goners)
				m_dirtballs.Remove(goner); // Deletion trumps dirty.
		}

		/// <summary>
		/// Check whether the fieldName is in use.
		/// </summary>
		/// <returns></returns>
		public bool IsFieldNameUsed(string className, string fieldName)
		{
			return m_mdc.FieldExists(className, fieldName, true);
		}

		/// <summary>
		/// Create a custom field using the given values.
		/// </summary>
		public void CreateCustomField(string className, string fieldName, CellarPropertyType cpt,
			int destClid, string helpString, int wsSelector, Guid listRoot)
		{
			m_mdc.AddCustomField(className, fieldName, cpt, destClid, helpString, wsSelector, listRoot);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path of the local project folder (typically a subdirectory of the Projects
		/// parent folder).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectFolder
		{
			get { return m_projectFolder; }
		}

		#endregion
	}
}
