/*-----------------------------------------------------------------------*//*:Ignore in Surveyor
Copyright (C) 2000, 2001 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: FwDbChangeOverlayTags.cpp
Responsibility: Paul Panek
Last reviewed: Not yet.

Description:
	These classes provide String Crawlers for changing overlay tag GUIDs in the database.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
	Scan the serialized string format (vbFmt) for runs containing the kstpTags string property.
	If any are found, change the guids from any guidTag value to the corresponding guidPss value
	as found in the hash map m_hmguidTagguidPss.

	This is too tricky to get right, keeping the formatting information in canonical form.

	@param vbFmt Reference to a byte vector containing the formatting information for the
					string.

	@return True if one or more runs had their internal properties changed, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwDbChangeOverlayTags::ProcessBytes(Vector<byte> & vbFmt)
{
	Assert(false);
	return false;
}

/*----------------------------------------------------------------------------------------------
	Scan the string text properties (vqttp) for runs containing the kstpTags string property.
	If any are found, change the guids from any guidTag value to the corresponding guidPss value
	as found in the hash map m_hmguidTagguidPss.

	@param vqttp Reference to a vector containing the text properties, one element per run.

	@return True if one or more runs had their internal properties changed, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwDbChangeOverlayTags::ProcessFormatting(ComVector<ITsTextProps> & vqttp)
{
	bool fAnyChanged = false;
	for (int ittp = 0; ittp < vqttp.Size(); ittp++)
	{
		SmartBstr sbstr;
		HRESULT hr = vqttp[ittp]->GetStrPropValue(kstpTags, &sbstr);
		if (hr == S_OK && sbstr.Length() > 0)
		{
			Assert((sbstr.Length() * isizeof(OLECHAR)) % isizeof(GUID) == 0);
			int cguid = (sbstr.Length() * isizeof(OLECHAR)) / isizeof(GUID);
			Vector<GUID> vguid;
			vguid.Resize(cguid);
			memcpy(vguid.Begin(), sbstr.Chars(), cguid * isizeof(GUID));
			GUID guidNew;
			bool fGuidChanged = false;
			int iguid;
			for (iguid = 0; iguid < cguid; ++iguid)
			{
				if (m_hmguidTagguidPss.Retrieve(vguid[iguid], &guidNew))
				{
					vguid[iguid] = guidNew;
					fGuidChanged = true;
				}
			}
			if (fGuidChanged)
			{
				// Eliminate duplicate GUIDs (unlikely, but possible).
				for (iguid = 1; iguid < vguid.Size(); ++iguid)
				{
					for (int ig = iguid - 1; ig >= 0; --ig)
					{
						if (vguid[ig] == vguid[iguid])
						{
							vguid.Delete(iguid);
							--iguid;
							break;
						}
					}
				}
				// Set the new value.  Use a StrUni to convert the Vector<GUID> to a BSTR.
				StrUni stu;
				stu.Assign(reinterpret_cast<OLECHAR *>(vguid.Begin()),
					(vguid.Size() * isizeof(GUID)) / isizeof(OLECHAR));
				ITsPropsBldrPtr qtpb;
				ITsTextPropsPtr qttpNew;
				CheckHr(vqttp[ittp]->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(kstpTags, stu.Bstr()));
				CheckHr(qtpb->GetTextProps(&qttpNew));
				vqttp[ittp] = qttpNew;
				fAnyChanged = true;
			}
		}
	}
	return fAnyChanged;
}

/*----------------------------------------------------------------------------------------------
	Scan the serialized string format (vbFmt) for runs containing the kstpTags string property.
	If any are found, delete any guid that matches up to m_pguidTag.

	This is too tricky to get right, keeping the formatting information in canonical form.

	@param vbFmt Reference to a byte vector containing the formatting information for the
				string.

	@return True if one or more runs had their internal properties changed, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwDbDeleteOverlayTags::ProcessBytes(Vector<byte> & vbFmt)
{
	Assert(false);
	return false;
}

/*----------------------------------------------------------------------------------------------
	Scan the string text properties (vqttp) for runs containing the kstpTags string property.
	If any are found, change the guids from any guidTag value to the corresponding guidPss value
	as found in the hash map m_hmguidTagguidPss.

	@param vqttp Reference to a vector containing the text properties, one element per run.

	@return True if one or more runs had their internal properties changed, otherwise false.
----------------------------------------------------------------------------------------------*/
bool FwDbDeleteOverlayTags::ProcessFormatting(ComVector<ITsTextProps> & vqttp)
{
	AssertPtr(m_pguidTag);

	bool fAnyDeleted = false;
	for (int ittp = 0; ittp < vqttp.Size(); ittp++)
	{
		SmartBstr sbstr;
		HRESULT hr = vqttp[ittp]->GetStrPropValue(kstpTags, &sbstr);
		if (hr == S_OK && sbstr.Length() > 0)
		{
			Assert((sbstr.Length() * isizeof(OLECHAR)) % isizeof(GUID) == 0);
			int cguid = (sbstr.Length() * isizeof(OLECHAR)) / isizeof(GUID);
			Vector<GUID> vguid;
			vguid.Resize(cguid);
			memcpy(vguid.Begin(), sbstr.Chars(), cguid * isizeof(GUID));
			bool fGuidDeleted = false;
			for (int iguid = 0; iguid < vguid.Size(); ++iguid)
			{
				if (!memcmp(m_pguidTag, &vguid[iguid], sizeof(GUID)))
				{
					fGuidDeleted = true;
					vguid.Delete(iguid);
					--iguid;
				}
			}
			if (fGuidDeleted)
			{
				// Set the new value.  Use a StrUni to convert the Vector<GUID> to a BSTR.
				StrUni stu;
				if (vguid.Size())
				{
					stu.Assign(reinterpret_cast<OLECHAR *>(vguid.Begin()),
						(vguid.Size() * isizeof(GUID)) / isizeof(OLECHAR));
				}
				ITsPropsBldrPtr qtpb;
				ITsTextPropsPtr qttpNew;
				CheckHr(vqttp[ittp]->GetBldr(&qtpb));
				CheckHr(qtpb->SetStrPropValue(kstpTags, stu.Bstr()));
				CheckHr(qtpb->GetTextProps(&qttpNew));
				vqttp[ittp] = qttpNew;
				fAnyDeleted = true;
			}
		}
	}
	return fAnyDeleted;
}


// Semi-Explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
