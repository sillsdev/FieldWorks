//-------------------------------------------------------------------------------------------------
// <copyright file="xmlutil.cpp" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
//    XML helper funtions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// intialization globals
CLSID vclsidXMLDOM = { 0, 0, 0, { 0, 0, 0, 0, 0, 0, 0, 0} };
static LONG vcXmlInitialized = 0;
static BOOL vfMsxml40 = FALSE;

/********************************************************************
 XmlInitialize - finds an appropriate version of the XML DOM

*********************************************************************/
extern "C" HRESULT DAPI XmlInitialize(
	)
{
	HRESULT hr = S_OK;

	hr = ::CoInitialize(0);
	ExitOnFailure(hr, "failed to initialize COM");

	::InterlockedIncrement(&vcXmlInitialized);
	if (1 == vcXmlInitialized)
	{
		// NOTE: 4.0 behaves differently than 3.0 so there may be problems doing this
#if 0
		hr = ::CLSIDFromProgID(L"Msxml2.DOMDocument.4.0", &vclsidXMLDOM);
		if (S_OK == hr)
		{
			vfMsxml40 = TRUE;
			Trace(REPORT_VERBOSE, "found Msxml2.DOMDocument.4.0");
			ExitFunction();
		}
#endif
		hr = ::CLSIDFromProgID(L"Msxml2.DOMDocument", &vclsidXMLDOM);
		if (FAILED(hr))
		{
			// try to fall back to old MSXML
			hr = ::CLSIDFromProgID(L"MSXML.DOMDocument", &vclsidXMLDOM);
		}
		ExitOnFailure(hr, "failed to get CLSID for XML DOM");

		Assert(IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument) || IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument26) ||
			IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument20) ||
			   IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument30) || IsEqualCLSID(vclsidXMLDOM, XmlUtil_CLSID_DOMDocument50));
	}

	hr = S_OK;
LExit:
	return hr;
}


/********************************************************************
 XmUninitialize -

*********************************************************************/
extern "C" void DAPI XmlUninitialize(
	)
{
	AssertSz(vcXmlInitialized, "XmlUninitialize called when not initialized");

	::InterlockedDecrement(&vcXmlInitialized);

	if (0 == vcXmlInitialized)
		memset(&vclsidXMLDOM, 0, sizeof(vclsidXMLDOM));
	::CoUninitialize();
}

extern "C" HRESULT DAPI XmlCreateElement(
	__in IXMLDOMDocument *pixdDocument,
	__in LPCWSTR wzElementName,
	__out IXMLDOMElement **ppixnElement
	)
{
	if (!ppixnElement || !pixdDocument)
	{
		return E_INVALIDARG;
	}

	HRESULT hr = S_OK;
	BSTR bstrElementName = ::SysAllocString(wzElementName);
	ExitOnNull(bstrElementName, hr, E_OUTOFMEMORY, "failed SysAllocString");
	hr = pixdDocument->createElement(bstrElementName, ppixnElement);
LExit:
	ReleaseBSTR(bstrElementName);
	return hr;
}


/********************************************************************
 XmlCreateDocument -

*********************************************************************/
extern "C" HRESULT DAPI XmlCreateDocument(
	__in_opt LPCWSTR pwzElementName,
	__out IXMLDOMDocument** ppixdDocument,
	__out_opt IXMLDOMElement** ppixeRootElement
	)
{
	HRESULT hr = S_OK;

	// RELEASEME
	IXMLDOMElement* pixeRootElement = NULL;
	IXMLDOMDocument *pixdDocument = NULL;

	// create the top level XML document
	AssertSz(vcXmlInitialized, "XmlInitialize() was not called");
	hr = ::CoCreateInstance(vclsidXMLDOM, NULL, CLSCTX_INPROC_SERVER, XmlUtil_IID_IXMLDOMDocument, (void**)&pixdDocument);
	ExitOnFailure(hr, "failed to create XML DOM Document");
	Assert(pixdDocument);

	if (pwzElementName)
	{
		hr = XmlCreateElement(pixdDocument, pwzElementName, &pixeRootElement);
		ExitOnFailure(hr, "failed XmlCreateElement");
		hr = pixdDocument->appendChild(pixeRootElement, NULL);
		ExitOnFailure(hr, "failed appendChild");
	}

	*ppixdDocument = pixdDocument;
	pixdDocument = NULL;

	if (ppixeRootElement)
	{
		*ppixeRootElement = pixeRootElement;
		pixeRootElement = NULL;
	}

LExit:
	ReleaseObject(pixeRootElement);
	ReleaseObject(pixdDocument);
	return hr;
}


/********************************************************************
 XmlLoadDocument -

*********************************************************************/
extern "C" HRESULT DAPI XmlLoadDocument(
	__in LPCWSTR wzDocument,
	__out IXMLDOMDocument** ppixdDocument
	)
{
	HRESULT hr;
	VARIANT_BOOL vbSuccess = 0;

	// RELEASEME
	IXMLDOMDocument* pixd = NULL;
	IXMLDOMParseError* pixpe = NULL;
	BSTR bstrLoad = NULL;

	if (!wzDocument || !*wzDocument)
		ExitOnFailure(hr = E_UNEXPECTED, "string must be non-null");

	hr = XmlCreateDocument(NULL, &pixd);
	if (hr == S_FALSE) hr = E_FAIL;
	ExitOnFailure(hr, "failed XmlCreateDocument");

	// Security issue.  Avoid triggering anything external.
	hr = pixd->put_validateOnParse(VARIANT_FALSE);
	ExitOnFailure(hr, "failed put_validateOnParse");
	hr = pixd->put_resolveExternals(VARIANT_FALSE);
	ExitOnFailure(hr, "failed put_resolveExternals");

	bstrLoad = ::SysAllocString(wzDocument);
	hr = pixd->loadXML(bstrLoad, &vbSuccess);
	if (S_FALSE == hr)
		hr = HRESULT_FROM_WIN32(ERROR_OPEN_FAILED);
	// if (S_OK != hr && S_OK == pixd->get_parseError(&pixpe))
	//    XmlReportParseError(pixpe, TRUE);
	ExitOnFailure(hr, "failed loadXML");


	hr = S_OK;
LExit:
	if (ppixdDocument)
	{
		*ppixdDocument = pixd;
		pixd = NULL;
	}
	ReleaseBSTR(bstrLoad);
	ReleaseObject(pixd);
	ReleaseObject(pixpe);
	return hr;
}


/*******************************************************************
 XmlLoadDocumentFromFile

********************************************************************/
extern "C" HRESULT DAPI XmlLoadDocumentFromFile(
	__in LPCWSTR wzPath,
	__out IXMLDOMDocument** ppixdDocument
	)
{
	HRESULT hr;
	VARIANT varPath;
	VARIANT_BOOL vbSuccess = 0;

	IXMLDOMDocument* pixd = NULL;
	IXMLDOMParseError* pixpe = NULL;

	::VariantInit(&varPath);
	varPath.vt = VT_BSTR;
	varPath.bstrVal = ::SysAllocString(wzPath);

	hr = XmlCreateDocument(NULL, &pixd);
	if (hr == S_FALSE)
		hr = E_FAIL;
	ExitOnFailure(hr, "failed XmlCreateDocument");

	pixd->put_async(VARIANT_FALSE);
	hr = pixd->load(varPath, &vbSuccess);
	if (S_FALSE == hr)
		hr = HRESULT_FROM_WIN32(ERROR_OPEN_FAILED);

	 // if (S_OK != hr && S_OK == pixd->get_parseError(&pixpe))
	//    XmlReportParseError(pixpe, TRUE);

	ExitOnFailure1(hr, "failed to load XML from: %S", wzPath);

	if (ppixdDocument)
	{
		*ppixdDocument = pixd;
		pixd = NULL;
	}

	hr = S_OK;
LExit:
	ReleaseVariant(varPath);
	ReleaseObject(pixd);
	ReleaseObject(pixpe);

	return hr;
}


/********************************************************************
 XmlSetAttribute -

*********************************************************************/
extern "C" HRESULT DAPI XmlSetAttribute(
	__in IXMLDOMNode* pixnNode,
	__in LPCWSTR pwzAttribute,
	__in LPCWSTR pwzAttributeValue
	)
{
	HRESULT hr = S_OK;
	VARIANT varAttributeValue;

	// RELEASEME
	IXMLDOMDocument* pixdDocument = NULL;
	IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
	IXMLDOMAttribute* pixaAttribute = NULL;
	BSTR bstrAttributeName = ::SysAllocString(pwzAttribute);
	varAttributeValue.bstrVal = NULL;

	hr = pixnNode->get_attributes(&pixnnmAttributes);
	ExitOnFailure1(hr, "failed get_attributes in XmlSetAttribute(%S)", pwzAttribute);

	hr = pixnNode->get_ownerDocument(&pixdDocument);
	if (hr == S_FALSE)
		hr = E_FAIL;
	ExitOnFailure(hr, "failed get_ownerDocument in XmlSetAttribute");

	hr = pixdDocument->createAttribute(bstrAttributeName, &pixaAttribute);
	ExitOnFailure1(hr, "failed createAttribute in XmlSetAttribute(%S)", pwzAttribute);

	varAttributeValue.vt = VT_BSTR;
	varAttributeValue.bstrVal = ::SysAllocString(pwzAttributeValue);
	if (!varAttributeValue.bstrVal)
		hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
	ExitOnFailure1(hr, "failed SysAllocString in XmlSetAttribute(%S)", pwzAttribute);

	hr = pixaAttribute->put_nodeValue(varAttributeValue);
	ExitOnFailure1(hr, "failed put_nodeValue in XmlSetAttribute(%S)", pwzAttribute);

	hr = pixnnmAttributes->setNamedItem(pixaAttribute, NULL);
	ExitOnFailure1(hr, "failed setNamedItem in XmlSetAttribute(%S)", pwzAttribute);

LExit:
	ReleaseObject(pixdDocument);
	ReleaseObject(pixnnmAttributes);
	ReleaseObject(pixaAttribute);
	ReleaseBSTR(varAttributeValue.bstrVal);
	ReleaseBSTR(bstrAttributeName);

	return hr;
}


/********************************************************************
 XmlSelectSingleNode -

*********************************************************************/
extern "C" HRESULT DAPI XmlSelectSingleNode(
	__in IXMLDOMNode* pixnParent,
	__in LPCWSTR wzXPath,
	__out IXMLDOMNode **ppixnChild
	)
{
	HRESULT hr = S_OK;

	BSTR bstrXPath = NULL;

	ExitOnNull(pixnParent, hr, E_UNEXPECTED, "pixnParent parameter was null in XmlSelectSingleNode");
	ExitOnNull(ppixnChild, hr, E_UNEXPECTED, "ppixnChild parameter was null in XmlSelectSingleNode");

	bstrXPath = ::SysAllocString(wzXPath ? wzXPath : L"");
	ExitOnNull(bstrXPath, hr, E_OUTOFMEMORY, "failed to allocate bstr for XPath expression in XmlSelectSingleNode");

	hr = pixnParent->selectSingleNode(bstrXPath, ppixnChild);

LExit:
	ReleaseBSTR(bstrXPath);

	return hr;
}


/********************************************************************
 XmlCreateTextNode -

*********************************************************************/
extern "C" HRESULT DAPI XmlCreateTextNode(
	__in IXMLDOMDocument *pixdDocument,
	__in LPCWSTR wzText,
	__out IXMLDOMText **ppixnTextNode
	)
{
	if (!ppixnTextNode || !pixdDocument)
	{
		return E_INVALIDARG;
	}

	HRESULT hr = S_OK;
	BSTR bstrText = ::SysAllocString(wzText);
	ExitOnNull(bstrText, hr, E_OUTOFMEMORY, "failed SysAllocString");
	hr = pixdDocument->createTextNode(bstrText, ppixnTextNode);
LExit:
	ReleaseBSTR(bstrText);
	return hr;
}


/********************************************************************
 XmlGetText

*********************************************************************/
extern "C" HRESULT DAPI XmlGetText(
	__in IXMLDOMNode* pixnNode,
	__out BSTR* pbstrText
	)
{
	return pixnNode->get_text(pbstrText);
}


/********************************************************************
 XmlGetAttribute

*********************************************************************/
extern "C" HRESULT DAPI XmlGetAttribute(
	__in IXMLDOMNode* pixnNode,
	__in LPCWSTR pwzAttribute,
	__out BSTR* pbstrAttributeValue
	)
{
	Assert(pixnNode);
	HRESULT hr = S_OK;

	// RELEASEME
	IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
	IXMLDOMNode* pixnAttribute = NULL;
	VARIANT varAttributeValue;
	BSTR bstrAttribute = SysAllocString(pwzAttribute);

	// INIT
	::VariantInit(&varAttributeValue);

	// get attribute value from source
	hr = pixnNode->get_attributes(&pixnnmAttributes);
	ExitOnFailure(hr, "failed get_attributes");

	hr = XmlGetNamedItem(pixnnmAttributes, bstrAttribute, &pixnAttribute);
	if (S_FALSE == hr)
	{
		// hr = E_FAIL;
		goto LExit;
	}
	ExitOnFailure1(hr, "failed getNamedItem in XmlGetAttribute(%S)", pwzAttribute);

	hr = pixnAttribute->get_nodeValue(&varAttributeValue);
	ExitOnFailure1(hr, "failed get_nodeValue in XmlGetAttribute(%S)", pwzAttribute);

	// steal the BSTR from the VARIANT
	if (S_OK == hr && pbstrAttributeValue)
	{
		*pbstrAttributeValue = varAttributeValue.bstrVal;
		varAttributeValue.bstrVal = NULL;
	}

LExit:
	ReleaseObject(pixnnmAttributes);
	ReleaseObject(pixnAttribute);
	ReleaseVariant(varAttributeValue);
	ReleaseBSTR(bstrAttribute);

	return hr;
}


/********************************************************************
 XmlGetAttribute

*********************************************************************/
extern "C" HRESULT DAPI XmlGetAttributeNumber(
	__in IXMLDOMNode* pixnNode,
	__in LPCWSTR pwzAttribute,
	__out DWORD* dwValue
	)
{
	HRESULT hr;
	BSTR bstrPointer = NULL;

	hr = XmlGetAttribute(pixnNode, pwzAttribute, &bstrPointer);
	ExitOnFailure(hr, "failed XmlGetAttribute");
	if (S_OK == hr)
	{
		*dwValue = (DWORD) wcstoul(bstrPointer, NULL, 10);
	}
LExit:
	ReleaseBSTR(bstrPointer);
	return hr;
}


/********************************************************************
 XmlGetNamedItem -

*********************************************************************/
extern "C" HRESULT DAPI XmlGetNamedItem(
	__in IXMLDOMNamedNodeMap *pixnmAttributes,
	__in_opt LPCWSTR wzName,
	__out IXMLDOMNode **ppixnNamedItem
	)
{
	if (!pixnmAttributes || !ppixnNamedItem)
	{
		return E_INVALIDARG;
	}

	HRESULT hr = S_OK;
	BSTR bstrName = ::SysAllocString(wzName);
	ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString");

	hr = pixnmAttributes->getNamedItem(bstrName, ppixnNamedItem);

LExit:
	ReleaseBSTR(bstrName);
	return hr;
}


/********************************************************************
 XmlSetText -

*********************************************************************/
extern "C" HRESULT DAPI XmlSetText(
	__in IXMLDOMNode *pixnNode,
	__in LPCWSTR pwzText
	)
{
	Assert(pixnNode && pwzText);
	HRESULT hr = S_OK;
	DOMNodeType dnType;

	// RELEASEME
	IXMLDOMDocument* pixdDocument = NULL;
	IXMLDOMNodeList* pixnlNodeList = NULL;
	IXMLDOMNode* pixnChildNode = NULL;
	IXMLDOMText* pixtTextNode = NULL;
	VARIANT varText;

	::VariantInit(&varText);

	// find the text node
	hr = pixnNode->get_childNodes(&pixnlNodeList);
	ExitOnFailure(hr, "failed to get child nodes");

	while (S_OK == (hr = pixnlNodeList->nextNode(&pixnChildNode)))
	{
		hr = pixnChildNode->get_nodeType(&dnType);
		ExitOnFailure(hr, "failed to get node type");

		if (NODE_TEXT == dnType)
			break;
		ReleaseNullObject(pixnChildNode);
	}
	if (S_FALSE == hr)
		hr = S_OK;

	if (pixnChildNode)
	{
		varText.vt = VT_BSTR;
		varText.bstrVal = ::SysAllocString(pwzText);
		if (!varText.bstrVal)
			hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
		ExitOnFailure(hr, "failed SysAllocString in XmlSetText");

		hr = pixnChildNode->put_nodeValue(varText);
		ExitOnFailure(hr, "failed IXMLDOMNode::put_nodeValue");
	}
	else
	{
		hr = pixnNode->get_ownerDocument(&pixdDocument);
		if (hr == S_FALSE)
			hr = E_FAIL;
		ExitOnFailure(hr, "failed get_ownerDocument in XmlSetAttribute");

		hr = XmlCreateTextNode(pixdDocument, pwzText, &pixtTextNode);
		ExitOnFailure1(hr, "failed createTextNode in XmlSetText(%S)", pwzText);

		hr = pixnNode->appendChild(pixtTextNode, NULL);
		ExitOnFailure1(hr, "failed appendChild in XmlSetText(%S)", pwzText);
	}

	hr = *pwzText ? S_OK : S_FALSE;

LExit:
	ReleaseObject(pixnlNodeList);
	ReleaseObject(pixnChildNode);
	ReleaseObject(pixdDocument);
	ReleaseObject(pixtTextNode);
	ReleaseVariant(varText);
	return hr;
}


/********************************************************************
 XmlSetTextNumber -

*********************************************************************/
extern "C" HRESULT DAPI XmlSetTextNumber(
	__in IXMLDOMNode *pixnNode,
	__in DWORD dwValue
	)
{
	HRESULT hr = S_OK;
	WCHAR wzValue[12];

	hr = ::StringCchPrintfW(wzValue, countof(wzValue), L"%u", dwValue);
	ExitOnFailure(hr, "Failed to format numeric value as string.");

	hr = XmlSetText(pixnNode, wzValue);

LExit:
	return hr;
}


/********************************************************************
 XmlCreateChild -

*********************************************************************/
extern "C" HRESULT DAPI XmlCreateChild(
	__in IXMLDOMNode* pixnParent,
	__in LPCWSTR pwzElementType,
	__out IXMLDOMNode** ppixnChild
	)
{
	HRESULT hr;

	// RELEASEME
	IXMLDOMDocument* pixdDocument = NULL;
	IXMLDOMNode* pixnChild = NULL;

	hr = pixnParent->get_ownerDocument(&pixdDocument);
	if (hr == S_FALSE)
		hr = E_FAIL;
	ExitOnFailure(hr, "failed get_ownerDocument");

	hr = XmlCreateElement(pixdDocument, (LPWSTR) pwzElementType, (IXMLDOMElement**) &pixnChild);
	if (hr == S_FALSE)
		hr = E_FAIL;
	ExitOnFailure(hr, "failed createElement");

	pixnParent->appendChild(pixnChild,NULL);
	if (hr == S_FALSE)
		hr = E_FAIL;
	ExitOnFailure(hr, "failed appendChild");

	if (ppixnChild)
	{
		*ppixnChild = pixnChild;
		pixnChild = NULL;
	}

LExit:
	ReleaseObject(pixdDocument);
	ReleaseObject(pixnChild);
	return hr;
}

/********************************************************************
 XmlRemoveAttribute -

*********************************************************************/
extern "C" HRESULT DAPI XmlRemoveAttribute(
	__in IXMLDOMNode* pixnNode,
	__in LPCWSTR pwzAttribute
	)
{
	HRESULT hr = S_OK;

	// RELEASEME
	IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
	BSTR bstrAttribute = ::SysAllocString(pwzAttribute);

	hr = pixnNode->get_attributes(&pixnnmAttributes);
	ExitOnFailure1(hr, "failed get_attributes in RemoveXmlAttribute(%S)", pwzAttribute);

	hr = pixnnmAttributes->removeNamedItem(bstrAttribute, NULL);
	ExitOnFailure1(hr, "failed removeNamedItem in RemoveXmlAttribute(%S)", pwzAttribute);

LExit:
	ReleaseObject(pixnnmAttributes);
	ReleaseBSTR(bstrAttribute);

	return hr;
}


/********************************************************************
 XmlSelectNodes -

*********************************************************************/
extern "C" HRESULT DAPI XmlSelectNodes(
	__in IXMLDOMNode* pixnParent,
	__in LPCWSTR wzXPath,
	__out IXMLDOMNodeList **ppixnlChildren
	)
{
	HRESULT hr = S_OK;

	BSTR bstrXPath = NULL;

	ExitOnNull(pixnParent, hr, E_UNEXPECTED, "pixnParent parameter was null in XmlSelectNodes");
	ExitOnNull(ppixnlChildren, hr, E_UNEXPECTED, "ppixnChild parameter was null in XmlSelectNodes");

	bstrXPath = ::SysAllocString(wzXPath ? wzXPath : L"");
	ExitOnNull(bstrXPath, hr, E_OUTOFMEMORY, "failed to allocate bstr for XPath expression in XmlSelectNodes");

	hr = pixnParent->selectNodes(bstrXPath, ppixnlChildren);

LExit:
	ReleaseBSTR(bstrXPath);
	return hr;
}


/********************************************************************
 XmlNextElement - returns the next element in a node list

 NOTE: pbstrElement is optional
	   returns S_OK if found an element
	   returns S_FALSE if no element found
	   returns E_* if something went wrong
********************************************************************/
extern "C" HRESULT DAPI XmlNextElement(
	__in IXMLDOMNodeList* pixnl,
	__out IXMLDOMNode** pixnElement,
	__out BSTR* pbstrElement
	)
{
	Assert(pixnl && pixnElement);

	HRESULT hr = S_OK;
	IXMLDOMNode* pixn = NULL;
	DOMNodeType nt;

	// null out the return values
	*pixnElement = NULL;
	if (pbstrElement)
		*pbstrElement = NULL;

	//
	// find the next element in the list
	//
	while (S_OK == (hr = pixnl->nextNode(&pixn)))
	{
		hr = pixn->get_nodeType(&nt);
		ExitOnFailure(hr, "failed to get node type");

		if (NODE_ELEMENT == nt)
			break;

		ReleaseNullObject(pixn);
	}
	ExitOnFailure(hr, "failed to get next element");

	// if we have a node and the caller asked for the element name
	if (pixn && pbstrElement)
	{
		hr = pixn->get_baseName(pbstrElement);
		ExitOnFailure(hr, "failed to get element name");
	}

	*pixnElement = pixn;
	pixn = NULL;

	hr = *pixnElement ? S_OK : S_FALSE;
LExit:
	ReleaseObject(pixn);
	return hr;
}


/********************************************************************
 XmlRemoveChildren -

*********************************************************************/
extern "C" HRESULT DAPI XmlRemoveChildren(
	__in IXMLDOMNode* pixnSource,
	__in LPCWSTR pwzXPath
	)
{
	HRESULT hr;

	// RELEASEME
	IXMLDOMNodeList* pixnlNodeList = NULL;
	IXMLDOMNode* pixnNode = NULL;

	if (pwzXPath)
	{
		hr = XmlSelectNodes(pixnSource, pwzXPath, &pixnlNodeList);
		ExitOnFailure(hr, "failed XmlSelectNodes");
	}
	else
	{
		hr = pixnSource->get_childNodes(&pixnlNodeList);
		ExitOnFailure(hr, "failed childNodes");
	}
	if (S_FALSE == hr)
		ExitFunction();

	while (S_OK == (hr = pixnlNodeList->nextNode(&pixnNode)))
	{
		hr = pixnSource->removeChild(pixnNode,NULL);
		ExitOnFailure(hr, "failed removeChild");

		ReleaseNullObject(pixnNode);
	}
	if (S_FALSE == hr)
		hr = S_OK;

LExit:
	ReleaseObject(pixnlNodeList);
	ReleaseObject(pixnNode);
	return hr;
}


/********************************************************************
 XmlSaveDocument -

*********************************************************************/
extern "C" HRESULT DAPI XmlSaveDocument(
	__in IXMLDOMDocument* pixdDocument,
	__inout LPCWSTR wzPath
	)
{
	HRESULT hr = S_OK;

	// RELEASEME
	VARIANT varsDestPath;

	::VariantInit(&varsDestPath);
	varsDestPath.vt = VT_BSTR;
	varsDestPath.bstrVal = ::SysAllocString(wzPath);
	if (!varsDestPath.bstrVal)
		hr = HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY);
	ExitOnFailure(hr, "failed to create BSTR");

	hr = pixdDocument->save(varsDestPath);
	if (hr == S_FALSE)
		hr = E_FAIL;
	ExitOnFailure(hr, "failed save in WriteDocument");

LExit:
	ReleaseVariant(varsDestPath);
	return hr;
}
