using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FixData
{
	/// <summary>
	/// This class ensures that we don't have two StStyles with the same name in the same owning list.
	/// This is crucial since FLEx will fail to start if it happens.
	/// If it does happen, we arbitrarily remove all but the first one with a given name and owner.
	/// This fix must be run before removing dangling references, since it may create dangling refs,
	/// if some other style specifies a deleted one as its Next or Base.
	/// Return false if we need to delete this root element.
	/// </summary>
	class DuplicateStyleFixer : RtFixer
	{
		// Key is Name, owning guid
		HashSet<Tuple<string,string>> m_stylesSeen = new HashSet<Tuple<string, string>>();
		HashSet<string> m_deletedGuids = new HashSet<string>();
		internal override void InspectElement(XElement rt)
		{
			var xaClass = rt.Attribute("class");
			if (xaClass == null || xaClass.Value != "StStyle")
				return; // keep this as far as we're concerned; we're not interested in anything but styles!
			var ownerAttr = rt.Attribute("ownerguid");
			if (ownerAttr == null)
				return; // Can't be in a stylesheet.
			var owner = ownerAttr.Value;
			var nameElt = rt.Element("Name");
			if (nameElt == null)
				return; // weird, but not a case of duplicate names, hopefully
			var uniElt = nameElt.Element("Uni");
			if (uniElt == null)
				return; // also weird
			var name = uniElt.Value;
			var key = Tuple.Create(name, owner);
			if (m_stylesSeen.Contains(key))
			{
				var guid = rt.Attribute("guid").Value;
				m_deletedGuids.Add(guid); // so we can log it later
			}
			m_stylesSeen.Add(key); // if we see another delete it
		}

		internal override void FinalFixerInitialization(Dictionary<Guid, Guid> owners, HashSet<Guid> guids, Dictionary<string, HashSet<string>> parentToOwnedObjsur,
			HashSet<string> rtElementsToDelete)
		{
			base.FinalFixerInitialization(owners, guids, parentToOwnedObjsur, rtElementsToDelete);
			// We must remove the guids we want to delete from this list so that they will not be considered
			// valid targets for any surviving references.
			foreach (var guid in m_deletedGuids)
				guids.Remove(new Guid(guid));
		}

		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger errorLogger)
		{
			var xaClass = rt.Attribute("class").Value;
			switch (xaClass)
			{
				case "StStyle":
					var guid = rt.Attribute("guid").Value;
					if (!m_deletedGuids.Contains(guid))
						return true; // keep it as far as we're concerned.
					var name = rt.Element("Name").Element("Uni").Value; // none can be null or we wouldn't have listed it for deletion
					errorLogger(guid.ToString(), DateTime.Now.ToShortDateString(),
						String.Format(Strings.ksRemovingDuplicateStyle, name));
					return false; // This element must go away!
				case "LangProject":
				case "Scripture":
					var styles = rt.Element("Styles");
					if (styles == null)
						return true;
					// Removing these here prevents additional error messages about missing objects, since the
					// targets of these objsurs are no longer present.
					foreach (var objsur in styles.Elements().ToArray()) // ToArray so as not to modify collection we're iterating over
					{
						var surGuid = objsur.Attribute("guid").Value;
						if (m_deletedGuids.Contains(surGuid))
							objsur.Remove();
					}
					break;
			}
			return true; // we're not deleting it.
		}
	}
}
