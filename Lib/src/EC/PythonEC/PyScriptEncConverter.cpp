// PyScript.cpp : Implementation of CPyScriptEncConverter

#include "stdafx.h"

#include "PyScriptEncConverter.h"

/////////////////////////////////////////////////////////////////////////////
// CPyScriptEncConverter
#define PythonInactivityWarningTimeOut 60000   // 60 seconds of inactivity means clean up resources

LPCTSTR clpszPyScriptProgID = _T("SilEncConverters40.PyScriptEncConverter.27");
LPCTSTR clpszPyScriptImplType = _T("SIL.PyScript");
LPCTSTR clpszPyScriptDefFuncName = _T("Convert");

CPyScriptEncConverter::CPyScriptEncConverter()
  : CEncConverter(clpszPyScriptProgID,clpszPyScriptImplType)  // from PyScriptEncConverter.rgs)
  , m_pFunc(0)
  , m_pModule(0)
  , m_pArgs(0)
  , m_nArgCount(0)
{
	m_timeLastModified = CTime::GetCurrentTime();
	m_eStringDataTypeIn = m_eStringDataTypeOut = eUCS2;
}

void CPyScriptEncConverter::ResetPython()
{
	m_strScriptName.Empty();
	m_strFuncName.Empty();

	if( m_pArgs != 0 )
	{
		Py_DecRef(m_pArgs);
		m_pArgs = 0;
	}

	if( IsModuleLoaded() )
	{
		// this means we *were* doing something, so release everything (not the
		//  func itself, but the module and then Finalize)
		Py_DecRef(m_pModule);
		m_pModule = 0;

		// reset the function pointer as well (just good practice)
		m_pFunc = 0;

		if( PyErr_Occurred() )
			PyErr_Clear();  // without this, the Finalize normally throws a fatal exception.

		Py_Finalize();
	}
}

STDMETHODIMP CPyScriptEncConverter::Initialize
(
	BSTR            ConverterName,
	BSTR            converterSpec,
	BSTR*           LhsEncodingId,
	BSTR*           RhsEncodingId,
	ConvType*       peConversionType,
	long*           plProcessTypeFlags,
	long            CodePageInput,
	long            CodePageOutput,
	VARIANT_BOOL    bAdding // if adding, then check whether transliterators exist (otherwise
							// wait until needed.
)
{
	// it might fail if the user has hard-coded the .tec file extension, but we haven't
	//  compiled it yet.
	HRESULT hr = CEncConverter::Initialize(ConverterName, converterSpec, LhsEncodingId, RhsEncodingId, peConversionType, plProcessTypeFlags, CodePageInput, CodePageOutput, bAdding);

	if( bAdding )
	{
		// do the load at this point; not that we need it, but for checking that everything's okay.
		hr = Load(CString(m_strConverterID));
		if( FAILED(hr) )
			return hr;

		// if the user doesn't tell us, then at least set the Encoding IDs as "UNICODE" if we can
		//  determine this from the ConvType
		if( m_eConversionType != ConvType_Unknown )
		{
			if( NormalizeLhsConversionType(m_eConversionType) == NormConversionType_eUnicode )
			{
				if(     (LhsEncodingId != 0)
					&&  (IsEmpty(*LhsEncodingId)) )
				{
					m_strLhsEncodingID = _T("UNICODE");
					get_LeftEncodingID(LhsEncodingId);  // use this to get a copy
				}
			}
			if( NormalizeRhsConversionType(m_eConversionType) == NormConversionType_eUnicode )
			{
				if(     (RhsEncodingId != 0)
					&&  (IsEmpty(*RhsEncodingId)) )
				{
					m_strRhsEncodingID = _T("UNICODE");
					get_RightEncodingID(RhsEncodingId);  // use this to get a copy
				}
			}
		}

		// make sure, it has the correct process type
		m_lProcessType |= ProcessTypeFlags_PythonScript;
		if( plProcessTypeFlags != 0 )
			*plProcessTypeFlags |= ProcessTypeFlags_PythonScript;
	}

	return hr;
}

HRESULT CPyScriptEncConverter::GetAttributeKeys(CComSafeArray<BSTR>& rSa)
{
	// do the load at this point.
	HRESULT hr = Load(CString(m_strConverterID));
	if( SUCCEEDED(hr) )
	{
		CComBSTR strValue = Py_GetVersion();
		WriteAttributeDefault(rSa, _T("Python Version"), strValue);
		strValue = Py_GetPlatform();
		WriteAttributeDefault(rSa, _T("Platform"), strValue);
		strValue = Py_GetCopyright();
		WriteAttributeDefault(rSa, _T("Copyright"), strValue);
		strValue = Py_GetCompiler();
		WriteAttributeDefault(rSa, _T("Compiler"), strValue);
		strValue = Py_GetBuildInfo();
		WriteAttributeDefault(rSa, _T("Build Info"), strValue);
	}
	return hr;
}

HRESULT CPyScriptEncConverter::Load(const CString& strScriptPathAndArgs)
{
	HRESULT hr = S_OK;

	// if we've already been initialized, check for a change to the map
	if( !m_strFileSpec.IsEmpty() )
	{
		CFileStatus fstat;
		if( !CFile::GetStatus(m_strFileSpec,fstat) || (fstat.m_attribute & CFile::directory) )
			return Error(_T("PyScript: Invalid script path"), __uuidof(IEncConverter), ErrStatus_NameNotFound);

		// see if the file has been changed (and reload if so)
		if( fstat.m_mtime > m_timeLastModified )
		{
			// unload it and then we'll reload it later
			ResetPython();
		}
	}

	// otherwise, if we've already initialized Python, then we're done.
	if( IsModuleLoaded() )
		return hr;

	// the Script Path may also have extra parameters, first let's Tokenize it.
	// e.gs:
	//  "C:\Python\TestPyScript.py;ToLower" // the "ToLower" function in TestPyScript.py
	//  "C:\Python\TestPyScript.py"         // defaults to "Convert" method
	//  "C:\Python\TestPyScript.py;ToLower;Chinese" // to pass addl fixed parameter 'Chinese'
	//  "TestPyScript.py..."    // defaults to searching in PYTHONPATH env. var.
#define NewConverterSpecApproach
#ifdef  NewConverterSpecApproach
	// initially, I was going to use spaces to delimit the tokens, but this makes the logic
	//  for allowing long file names more complicated, and more significantly, doesn't allow
	//  optional additional parameters to be file specs, which is clearly unacceptable. Since
	//  configuring a "converter spec" is now taken care of for us by the UI, make it easy
	//  and just use ";" to delimit the pieces/parts.
	// put the rest of the arguments into an array for later processing
	CStringArray astrArgs;
	int nIndex = 0;
	CString strArg = strScriptPathAndArgs.Tokenize(_T(";"), nIndex);
	while (strArg != "")
	{
		astrArgs.Add(strArg);
		strArg = strScriptPathAndArgs.Tokenize(_T(";"),nIndex);
	}

	if( astrArgs.IsEmpty() )
	{
		CString strError;
		strError.Format(_T("PythonScript: The converter identifier '%s' is invalid!\n\nUsage: <ScriptPath>;(<FunctionName>);(<addl params>)\n\n\twhere the parts are delimited by ';' and <FunctionName> defaults to 'Convert' if omitted"), strScriptPathAndArgs);
		return Error(strError, __uuidof(IEncConverter), ErrStatus_InvalidConversionType);
	}

	m_strFileSpec = astrArgs[0];
	nIndex = m_strFileSpec.ReverseFind('\\');
	CString strScriptPath;
	if( nIndex == 0 )       // e.g. "\blah.py"
	{
		strScriptPath = m_strFileSpec.Left(++nIndex); // grab off just the slash
		m_strScriptName = m_strFileSpec.Right(m_strFileSpec.GetLength() - nIndex);
	}
	else if( nIndex > 0 )   // e.g. C:\...\blah.ph
	{
		strScriptPath = m_strFileSpec.Left(nIndex++);
		m_strScriptName = m_strFileSpec.Right(m_strFileSpec.GetLength() - nIndex);
	}
	else
		;   // no path at all; just the filename

	if( !strScriptPath.IsEmpty() )
	{
		// make sure the full file spec has the extension (for GetStatus testing)
		if( m_strFileSpec.Right(3).CompareNoCase(_T(".py")) )
			m_strFileSpec += _T(".py");

		CFileStatus fstat;
		if( !CFile::GetStatus(m_strFileSpec,fstat) || (fstat.m_attribute & CFile::directory) )
			return Error(_T("PyScript: Invalid script path"), __uuidof(IEncConverter), ErrStatus_NameNotFound);

		// keep track of the modified date, so we can detect a new version to reload
		m_timeLastModified = fstat.m_mtime;
	}
#elif defined(ConverterSpecSpacesQuotesApproach)
	// the Script Path may also have extra parameters, first let's Tokenize it.
	// e.gs:
	//  C:\Python\TestPyScript.py ToLower               // the "ToLower" function in TestPyScript.py
	//  "C:\Python\long dir\TestPyScript.py" ToLower    // script path needs "s if long filename
	//  C:\Python25\TestPyScript.py                    // defaults to "Convert" method
	//  C:\Python\TestPyScript.py ToLower Chinese"      // to pass addl fixed parameter 'Chinese'
	//  TestPyScript.py    // defaults to searching in PYTHONPATH env. var.
	CString strScriptPath;
	BOOL bErrorShowUsage = false;
	int nStartOfScriptPath = strScriptPathAndArgs.Find('"');
	// this could be a double quote in an additional parameter, so make sure it starts at 0
	if( nStartOfScriptPath != -1 )
	{
		// we found a double-quote, but it may be for an optional, additional parameter
		if( nStartOfScriptPath == 0 )
		{
			// this is for the script path... better have another double-quote
			int nEndOfScriptPath = strScriptPathAndArgs.Find('"',nStartOfScriptPath + 1);
			if( nEndOfScriptPath != -1 )
				// the script path is between these two
				strScriptPath = strScriptPathAndArgs.Mid(nStartOfScriptPath,nEndOfScriptPath - nStartOfScriptPath);
			else
				bErrorShowUsage = true;
		}
		else
		{
			// this must be for an optional parameter, so just assume that there are no "s around the script path
			//  (therefore, look for a space to delimit the script path)
			int nEndOfScriptPath = strScriptPathAndArgs.Find(" ");
			if( nEndOfScriptPath == -1 )
			{
				// this means there is no function name or addl parameters, so the script path is the whole thing
				strScriptPath = strScriptPathAndArgs;
			}
			else
			{
				strScriptPath = strScriptPathAndArgs.Mid(nStartOfScriptPath,nEndOfScriptPath - nStartOfScriptPath);
			}
		}
	}
	else    // if( nStartOfScriptPath == -1 )
	{
		// otherwise, no "s, so just assume that there are no "s around the script path
		//  (therefore, look for a space to delimit the script path)
		int nEndOfScriptPath = strScriptPathAndArgs.Find(" ");
		if( nEndOfScriptPath == -1 )
		{
			// this means there is no function name or addl parameters, so the script path is the whole thing
			strScriptPath = strScriptPathAndArgs;
		}
		else
		{
			strScriptPath = strScriptPathAndArgs.Mid(nStartOfScriptPath,nEndOfScriptPath - nStartOfScriptPath);
		}
	}
#else
	// but the path may have spaces, so we have to tokenize on spaces *after* the path is found.
	int nIndex = strScriptPathAndArgs.ReverseFind('\\');

	// if the user gives a path, then add it to Python's sys.path (see 6.2 Standard Modules)
	CString strScriptPath;
	if( nIndex == 0 )   // i.e. "\blah.py", which is probably "C:\blah.py"
		strScriptPath = strScriptPathAndArgs.Left(++nIndex);
	else if( nIndex > 0 )
		strScriptPath = strScriptPathAndArgs.Left(nIndex++);
	else
		nIndex = 0;

	// then search forward for the space to delimit the script name
	int nLength = strScriptPathAndArgs.Find(' ',nIndex);
	if( nLength < 0 ) // no space; perhaps "Test.py"
		nLength = strScriptPathAndArgs.GetLength() - nIndex;
	else
		nLength -= nIndex;  // space; perhaps "Test.py ToUpper"

	if( nLength <= 0 )
		return Error(_T("PyScript: Invalid script path"), __uuidof(IEncConverter), ErrStatus_InvalidConversionType);

	// see if such a file exists.
	m_strScriptName = strScriptPathAndArgs.Mid(nIndex, nLength);

	if( !strScriptPath.IsEmpty() )
	{
		m_strFileSpec = strScriptPath + _T("\\") + m_strScriptName;
		if( m_strFileSpec.Right(3).CompareNoCase(_T(".py")) )
			m_strFileSpec += _T(".py");

		CFileStatus fstat;
		if( !CFile::GetStatus(m_strFileSpec,fstat) || (fstat.m_attribute & CFile::Attribute::directory) )
		{
			CString strError;
			strError.Format(_T("PyScript: Invalid script path: '%s'"), m_strFileSpec);
			return Error(strError, __uuidof(IEncConverter), ErrStatus_NameNotFound);
		}

		// keep track of the modified date, so we can detect a new version to reload
		m_timeLastModified = fstat.m_mtime;
	}

	// put the rest of the arguments into an array for later processing
	CStringArray astrArgs;
	nIndex += nLength;
	CString strArg = strScriptPathAndArgs.Tokenize(_T(" "), ++nIndex);
	while (strArg != "")
	{
		astrArgs.Add(strArg);
		strArg = strScriptPathAndArgs.Tokenize(_T(" "),nIndex);
	}
#endif  // ConverterSpecOldWay

	// hook up to the Python DLL
	Py_Initialize();

	// next add the path to the sys.path
	if( !strScriptPath.IsEmpty() )
	{
		CStringA strCmd;
		strCmd.Format("import sys\nsys.path.append('%s')", CT2A(strScriptPath));
		PyRun_SimpleString(strCmd);
	}

	// turn the filename into a Python object (Python import doesn't like .py extension)
	if( !m_strScriptName.Right(3).CompareNoCase(_T(".py")) )
		m_strScriptName = m_strScriptName.Left(m_strScriptName.GetLength() - 3);

	// get the module point by the name
	m_pModule = PyImport_ImportModule(CT2A(m_strScriptName));
	if( m_pModule == 0 )
	{
		// gracefully disconnect from Python
		if( PyErr_Occurred() )
			PyErr_Clear();  // without this, the Finalize normally throws a fatal exception.
		Py_Finalize();

		CString strError;
		strError.Format(_T("PyScript: Unable to import script module '%s'! Is it locked? Does it have a syntax error? Is a Python distribution installed?"),m_strScriptName);
		return Error(strError, __uuidof(IEncConverter), ErrStatus_CantOpenReadMap);
	}

	PyObject* pDict = PyModule_GetDict(m_pModule);

	// if the user didn't give us a function name, then use the
	//  default function name, 'Convert'
	m_strFuncName = clpszPyScriptDefFuncName;
	if( astrArgs.GetCount() > 1 )
		m_strFuncName = astrArgs[1];

	m_pFunc = PyDict_GetItemString(pDict, CT2A(m_strFuncName));

	if( !MyPyCallable_Check(m_pFunc) )
	{
		// gracefully disconnect from Python
		CString strError;
		strError.Format(_T("PyScript: no callable function named '%s' in script module '%s'!"), m_strFuncName, m_strScriptName);
		ResetPython();
		return Error(strError, __uuidof(IEncConverter), ErrStatus_NameNotFound);
	}

	// finally, if the user configured any additional parameters to be passed
	//  directly to the module, add those to the arguments we're going to pass.
	if( astrArgs.GetCount() > 2 )
	{
		// make it size + 1 (for the data as the last value--filled in by
		//  PreConvert)
		m_nArgCount = astrArgs.GetCount() - 1;
		m_pArgs = PyTuple_New((int)m_nArgCount);
		for(int i = 2; i < astrArgs.GetCount(); i++)
		{
			CString strArg = astrArgs[i];
			PyObject* pValue = PyUnicode_FromUnicode((const Py_UNICODE*)(LPCTSTR)strArg,strArg.GetLength());
			if (pValue == 0)
			{
				// gracefully disconnect from Python
				ResetPython();
				CString strError;
				strError.Format(_T("PyScript: Can't convert optional fixed parameter '%s' to a Python unicode string"), strArg);
				return Error(strError, __uuidof(IEncConverter), ErrStatus_OutOfMemory);
			}

			// put it into the argument tuple (pValue reference is "stolen" here)
			PyTuple_SetItem(m_pArgs, i - 2, pValue);
		}
	}
	else
	{
		// we need at least one for the data value to be passed
		m_nArgCount = 1;
		m_pArgs = PyTuple_New((int)m_nArgCount);
	}

	return hr;
}

// call to clean up resources when we've been inactive for some time.
void CPyScriptEncConverter::InactivityWarning()
{
	TRACE(_T("CPyScriptEncConverter::InactivityWarning\n"));
	FinalRelease();
}

HRESULT CPyScriptEncConverter::PreConvert
(
	EncodingForm    eInEncodingForm,
	EncodingForm&	eInFormEngine,
	EncodingForm    eOutEncodingForm,
	EncodingForm&	eOutFormEngine,
	NormalizeFlags& eNormalizeOutput,
	BOOL            bForward,
	UINT            nInactivityWarningTimeOut
)
{
	HRESULT hr = CEncConverter::PreConvert(eInEncodingForm, eInFormEngine,
									eOutEncodingForm, eOutFormEngine,
									eNormalizeOutput, bForward, PythonInactivityWarningTimeOut);

	if( SUCCEEDED(hr) )
	{
		// I don't think Python does "bi-directional", so this code ignores the
		//  the direction.
		switch(eInEncodingForm)
		{
			case EncodingForm_LegacyBytes:
			case EncodingForm_LegacyString:
				eInFormEngine = EncodingForm_LegacyBytes;
				m_eStringDataTypeIn = eBytes;
				break;

			case EncodingForm_UTF8Bytes:
			case EncodingForm_UTF8String:
			case EncodingForm_UTF16BE:
			case EncodingForm_UTF16:
				eInFormEngine = EncodingForm_UTF16;
				m_eStringDataTypeIn = eUCS2;
				break;

			case EncodingForm_UTF32BE:
			case EncodingForm_UTF32:
				eInFormEngine = EncodingForm_UTF32;
				m_eStringDataTypeIn = eUCS4;
				break;
		};

		switch(eOutEncodingForm)
		{
			case EncodingForm_LegacyBytes:
			case EncodingForm_LegacyString:
				eOutFormEngine = EncodingForm_LegacyBytes;
				m_eStringDataTypeOut = eBytes;
				break;

			case EncodingForm_UTF8Bytes:
			case EncodingForm_UTF8String:
			case EncodingForm_UTF16BE:
			case EncodingForm_UTF16:
				eOutFormEngine = EncodingForm_UTF16;
				m_eStringDataTypeOut = eUCS2;
				break;

			case EncodingForm_UTF32BE:
			case EncodingForm_UTF32:
				eOutFormEngine = EncodingForm_UTF32;
				m_eStringDataTypeOut = eUCS4;
				break;
		};

		// do the load at this point.
		hr = Load(CString(m_strConverterID));
	}

	return hr;
}

extern CString EnumerateDictionary(PyObject* pDict);

HRESULT CPyScriptEncConverter::ErrorOccurred()
{
	CString strError;
	if( PyErr_Occurred() )
	{
		PyObject *err_type, *err_value, *err_traceback;
		PyErr_Fetch(&err_type, &err_value, &err_traceback);

		if( MyPyClass_Check(err_type) )
		{
			PyObject *pName = ((PyClassObject*)err_type)->cl_name;
			if( pName != 0 )
				strError += CA2T(PyString_AsString(pName)) + CString(_T("; "));
		}

		if( MyPyInstance_Check(err_value) )
		{
			strError += EnumerateDictionary(((PyInstanceObject*)err_value)->in_dict);
		}
		else if( MyPyString_Check(err_value) )
		{
			strError += CA2T(PyString_AsString(err_value)) + CString(_T("; "));
		}

		if( MyPyTraceBack_Check(err_traceback) )
		{
			PyTracebackObject* pTb = (PyTracebackObject*)err_traceback;
			// if( PyTraceBack_Print(err_traceback,pTb) )
				strError += CA2T(PyString_AsString(err_traceback)) + CString(_T("; "));
		}

		PyErr_Clear();
	}

	CString strErrorHeader;
	strErrorHeader.Format(_T("While executing the function, '%s', in the python script, '%s', the following error occurred:"), m_strFuncName, m_strScriptName );

	if( strError.IsEmpty() )
		strErrorHeader += _T("\n\n\tPyScript: No data return from Python! Perhaps there's a syntax error in the Python function");
	else
		strErrorHeader += _T("\n\n\t") + strError;

	// reset python before we go
	ResetPython();

	return Error(strErrorHeader, __uuidof(IEncConverter), ErrStatus_NoReturnDataBadOutForm);
}

HRESULT CPyScriptEncConverter::DoConvert
(
	LPBYTE  lpInBuffer,
	UINT    nInLen,
	LPBYTE  lpOutBuffer,
	UINT&   rnOutLen
)
{
	PyObject* pValue = 0;
	switch(m_eStringDataTypeIn)
	{
		case eBytes:
			pValue = PyString_FromStringAndSize((LPCSTR)lpInBuffer,nInLen);
			break;
		case eUCS2:
			pValue = PyUnicodeUCS2_FromUnicode((const Py_UNICODE*)(LPCWSTR)lpInBuffer,nInLen / 2);
			break;
/*      // apparently, UTF32 isn't available concurrently with UTF16... so for now... comment it out
		case eUCS4:
			pValue = PyUnicodeUCS4_FromUnicode(lpInBuffer,nInLen / 4);
			break;
*/
	}

	if( pValue == 0 )
	{
		ResetPython();
		CString strError;
		strError.Format(_T("PyScript: Can't convert input data '%s' to a form that Python can read!?"),
			((m_eStringDataTypeIn == eBytes) ? CA2T((LPCSTR)lpInBuffer) : (LPCWSTR)lpInBuffer) );
		return Error(strError, __uuidof(IEncConverter), ErrStatus_InvalidCharFound);
	}

	// put the value to convert into the last argument slot
	PyTuple_SetItem(m_pArgs, (int)m_nArgCount - 1, pValue);

	// do the call
	pValue = PyObject_CallObject(m_pFunc, m_pArgs);

	int nOut;

	if( pValue == 0 )
	{
		return ErrorOccurred();
	}

	HRESULT hr = S_OK;
	void* lpOutValue = 0;
	switch(m_eStringDataTypeOut)
	{
		case eBytes:
			PyString_AsStringAndSize(pValue,(LPSTR*)&lpOutValue,&nOut);
			if( nOut < 0 )
			{
				// at least, this is what happens if the python function returns a string, but the ConvType is
				//  incorrectly configured as returning Unicode. ErrStatus
				hr = Error(_T("PyScript: Are you sure that the Python function returns non-Unicode-encoded (bytes) data?"), __uuidof(IEncConverter), ErrStatus_InvalidConversionType);
			}
			break;
		case eUCS2:
			nOut = (int)PyUnicode_GetSize(pValue) * sizeof(WCHAR);
			if( nOut < 0 )
			{
				// at least, this is what happens if the python function returns a string, but the ConvType is
				//  incorrectly configured as returning Unicode. ErrStatus
				hr = Error(_T("PyScript: Are you sure that the Python function returns Unicode-encoded (wide) data?"), __uuidof(IEncConverter), ErrStatus_InvalidConversionType);
			}
			lpOutValue = PyUnicode_AsUnicode(pValue);   // PyUnicodeUCS2_AsUnicode(pValue);
			break;
/*
		case eUCS4:
			lpOutValue = PyUnicodeUCS4_AsUnicode(pValue);
			break;
*/
	}

	// don't let it be longer than out output buffer (I had a problem with the UnicodeName function
	//  and a *very long* clipboard sequence!).
	if( SUCCEEDED(hr) )
	{
		if( nOut > (int)rnOutLen )
		{
			hr = Error((LPCTSTR)lpOutValue, __uuidof(IEncConverter), ErrStatus_OutputBufferFull);
		}
		else
		{
	rnOutLen = nOut;
	if( nOut > 0 )
		memcpy(lpOutBuffer,lpOutValue,nOut);
	}
	}

	Py_DecRef(pValue);

	return hr;
}
