// PyScriptAutoConfigDlg.cpp : implementation file
//

#include "stdafx.h"
#include "PythonEC.h"
#include "PyScriptAutoConfigDlg.h"
#include "Resource.h"
#include "PyScriptEncConverter.h"   // for clpszPyScriptDefFuncName

// CPyScriptAutoConfigDlg dialog

extern LPCTSTR clpszPyScriptImplType;
extern LPCTSTR clpszPyScriptProgID;
const CString clpszPythonInstallPathKey = _T("SOFTWARE\\Python\\PythonCore\\2.5\\InstallPath");

IMPLEMENT_DYNAMIC(CPyScriptAutoConfigDlg, CAutoConfigDlg)
CPyScriptAutoConfigDlg::CPyScriptAutoConfigDlg
(
	IEncConverters* pECs,
	const CString&  strFriendlyName,
	const CString&  strConverterIdentifier,
	ConvType        eConversionType,
	const CString&  strLhsEncodingId,
	const CString&  strRhsEncodingId,
	long            lProcessTypeFlags,
	BOOL            m_bIsInRepository
)
  : CAutoConfigDlg
	(
		pECs,
		CPyScriptAutoConfigDlg::IDD,
		strFriendlyName,
		strConverterIdentifier,
		eConversionType,
		strLhsEncodingId,
		strRhsEncodingId,
		lProcessTypeFlags,
		m_bIsInRepository
	)
  , m_strFuncName(_T(""))
  , m_pModule(0)
  , m_pDictionary(0)
{
	if( m_strConverterIdentifier.IsEmpty() )
	{
		// HKEY_LOCAL_MACHINE\SOFTWARE\Python\PythonCore\2.5\InstallPath
		CRegKey keyPythonInstallPath;
		if( keyPythonInstallPath.Open(HKEY_LOCAL_MACHINE, clpszPythonInstallPathKey, KEY_READ) == ERROR_SUCCESS )
		{
			TCHAR szPath[_MAX_PATH]; szPath[0] = 0;
			ULONG ulLen = _MAX_PATH;
			keyPythonInstallPath.QueryStringValue(_T(""), szPath, &ulLen);
			if( ulLen > 0 )
				m_strConverterFilespec = szPath;
		}
	}
	else
	{
		DeconstructConverterSpec(m_strConverterIdentifier,m_strConverterFilespec,m_strFuncName,m_strAddlParams);
		ASSERT(!m_strConverterFilespec.IsEmpty());
	}
}

CPyScriptAutoConfigDlg::~CPyScriptAutoConfigDlg()
{
	ResetPython();
}

BOOL CPyScriptAutoConfigDlg::OnInitDialog()
{
	BOOL bRet = CAutoConfigDlg::OnInitDialog();

	if( bRet && !m_strFuncName.IsEmpty() )
	{
		// now that the combobox is created, initialize the function name
		InitFuncNameComboBox();

		// since the previous function results in the addl parameters window being cleared,
		//  reset it if it was present
		if( !m_strAddlParams.IsEmpty() )
			m_ctlAddlParams.SetWindowText(m_strAddlParams);
	}

	return bRet;
}

BOOL CPyScriptAutoConfigDlg::InitPython()
{
	// in case we've been here before, reset it now.
	if( m_pModule != 0 )
		ResetPython();

	// search in that file for the available function names
	Py_Initialize();

	// next add the path to the sys.path
	CStringA strCmd;
	strCmd.Format("import sys\nsys.path.append('%s')", CT2A(GetDirNoSlash(m_strConverterFilespec)));
	PyRun_SimpleString(strCmd);

	// turn the filename into a Python object (Python import doesn't like .py extension)
	CString strFilespec = m_strConverterFilespec;
	if( !strFilespec.Right(3).CompareNoCase(_T(".py")) )
		strFilespec = strFilespec.Left(strFilespec.GetLength() - 3);

	int nIndex = strFilespec.ReverseFind('\\');
	strFilespec = strFilespec.Right(strFilespec.GetLength() - nIndex - 1);

	// get the module point by the name
	m_pModule = PyImport_ImportModule(CT2A(strFilespec));
	if( m_pModule == 0 )
	{
		CString strError;

		// first check to see if the Python reg keys exist. If not, then tell them they need a distribution
		CRegKey keyPythonInstallPath;
		if( keyPythonInstallPath.Open(HKEY_LOCAL_MACHINE, clpszPythonInstallPathKey, KEY_READ) != ERROR_SUCCESS )
			strError = _T("It doesn't appear that you have a Python distribution installed. See the About tab for details of where to get one.");
		else
			strError.Format(_T("Unable to import script module '%s'! Is it locked? Does it have a syntax error? Is a Python distribution installed?"),strFilespec);

		MessageBox(strError);
		// gracefully disconnect from Python
		if( PyErr_Occurred() )
			PyErr_Clear();  // without this, the Finalize normally throws a fatal exception.
		Py_Finalize();
		return false;
	}

	m_pDictionary = PyModule_GetDict(m_pModule);
	return true;
}

void CPyScriptAutoConfigDlg::ResetPython()
{
	if( m_pModule != 0 )
	{
		m_pDictionary = 0;
		Py_DecRef(m_pModule);
		m_pModule = 0;

		if( PyErr_Occurred() )
			PyErr_Clear();  // without this, the Finalize normally throws a fatal exception.

		Py_Finalize();
	}
}

CString strPyScriptDelimiter = _T(";");

void CPyScriptAutoConfigDlg::DeconstructConverterSpec(const CString& strScriptPathAndArgs, CString& strScriptSpec, CString& strFuncName, CString& strAddlParams)
{
	int nIndex = 0;
	strScriptSpec = strScriptPathAndArgs.Tokenize(strPyScriptDelimiter, nIndex);
	if( !strScriptSpec.IsEmpty() )
	{
		strFuncName = strScriptPathAndArgs.Tokenize(strPyScriptDelimiter, nIndex);
		if( strFuncName.IsEmpty() )
			strFuncName = clpszPyScriptDefFuncName;
		else if( nIndex != -1 )
			strAddlParams = strScriptPathAndArgs.Right(strScriptPathAndArgs.GetLength() - nIndex);
	}
}

void CPyScriptAutoConfigDlg::DoDataExchange(CDataExchange* pDX)
{
	CAutoConfigDlg::DoDataExchange(pDX);
	DDX_Control(pDX,IDC_EDIT_SCRIPT_PATH,m_ctlScriptFilespec);
	DDX_Control(pDX,IDC_CB_FUNC_NAME,m_cbFunctionNames);
	DDX_Control(pDX,IDC_EDIT_ADDL_PARAMS,m_ctlAddlParams);
	DDX_Control(pDX,IDC_STATIC_FUNC_PROTOTYPE,m_ctlFuncPrototype);

	DDX_Text(pDX,IDC_EDIT_SCRIPT_PATH,m_strConverterFilespec);

	if( pDX->m_bSaveAndValidate )
	{
		// make sure the file exists
		CFileStatus fstat;
		if( !CFile::GetStatus(m_strConverterFilespec,fstat) || (fstat.m_attribute & CFile::directory) )
		{
			MessageBox(_T("Choose a Python script file first!"));
			pDX->m_idLastControl = IDC_EDIT_SCRIPT_PATH;
			pDX->Fail();
		}
	}

	DDX_CBString(pDX,IDC_CB_FUNC_NAME,m_strFuncName);

	if( pDX->m_bSaveAndValidate )
	{
		if( m_strFuncName.IsEmpty() )
		{
			MessageBox(_T("Choose a function from the drop-down list"));
			pDX->m_idLastControl = IDC_CB_FUNC_NAME;
			pDX->Fail();
		}

		// if the function name has a '\', then the code for parsing the ConverterSpec will fail
		else if( m_strFuncName.Find(';') != -1 )
		{
			MessageBox(_T("The character ';' is not allowed in the Function name parameter field"));
			pDX->m_idLastControl = IDC_CB_FUNC_NAME;
			pDX->Fail();
		}
	}

	DDX_Text(pDX,IDC_EDIT_ADDL_PARAMS,m_strAddlParams);

	if( pDX->m_bSaveAndValidate )
	{
		// configure the converter spec:
		//  <Script Filespec> <Func Name> (<Addl params>)
		m_strConverterIdentifier.Format(_T("%s;%s"), m_strConverterFilespec, m_strFuncName);

		if( !m_strAddlParams.IsEmpty() )
			m_strConverterIdentifier.Format(_T("%s;%s"), m_strConverterIdentifier, m_strAddlParams);
	}
}

CString CPyScriptAutoConfigDlg::ImplType()
{
	return clpszPyScriptImplType;
}

CString CPyScriptAutoConfigDlg::ProgramID()
{
	return clpszPyScriptProgID;
}

BEGIN_MESSAGE_MAP(CPyScriptAutoConfigDlg, CAutoConfigDlg)
	ON_BN_CLICKED(IDC_BTN_BROWSE, OnBnClickedBtnBrowse)
	ON_CBN_SELCHANGE(IDC_CB_FUNC_NAME, OnCbnSelchangeCbFuncName)
	ON_EN_CHANGE(IDC_EDIT_ADDL_PARAMS, OnEnChangeEditAddlParams)
END_MESSAGE_MAP()

CString GetType(PyObject* pObj)
{
	PyTypeObject* pType = (PyTypeObject*)PyObject_Type(pObj);
	int nLen = 255;
	char buf[255];
	buf[0] = 0;
	strcpy_s(buf,pType->tp_name);
	return CString(CA2T(buf));
}

#define PyFunction_GET_NAME(func) \
		(((PyFunctionObject *)func) -> func_name)
#define PyFunction_GET_DOC(func) \
		(((PyFunctionObject *)func) -> func_doc)
#define PyFunction_GET_DICT(func) \
		(((PyFunctionObject *)func) -> func_dict)
#define PyFunction_GET_WEAKREFLIST(func) \
		(((PyFunctionObject *)func) -> func_weakreflist)

#define PyTuple_GET_ITEM(op, i) (((PyTupleObject *)(op))->ob_item[i])
#define PyTuple_GET_SIZE(op)    (((PyTupleObject *)(op))->ob_size)

CString ToString(const CStringArray& astr)
{
	CString strRet;
	for(int i = 0; i < astr.GetCount(); i++)
	{
		CString strValue;
		strValue.Format(_T("%s, "), astr[i]);
		strRet += strValue;
	}

	strRet.Delete(strRet.GetLength() - 2,2);
	return strRet;
}

void EnumerateTuple(PyObject* pTuple, CStringArray& astrRet, int argcountlimit = 100)
{
	Py_ssize_t nNumItems = min(PyTuple_GET_SIZE(pTuple),argcountlimit);
	astrRet.SetSize(nNumItems);
	for(int i = 0; i < nNumItems; i++)
	{
		PyObject* pItem = PyTuple_GET_ITEM( pTuple, i );
		astrRet.SetAt(i, CA2T(PyString_AsString(pItem)));
	}
}

CString EnumerateTuple(PyObject* pTuple)
{
	CStringArray astr;
	EnumerateTuple(pTuple, astr);
	return ToString(astr);
}

CString EnumerateDictionary(PyObject* pDict)
{
	CString strRet;
	if( pDict == 0 )
		return strRet;

	PyObject* pItems = PyDict_Values(pDict);
	Py_ssize_t nNumItems = PyList_GET_SIZE(pItems);
	for(int i = 0; i < nNumItems; i++)
	{
		PyObject* pItem = PyList_GET_ITEM( pItems, i );
		if( MyPyTuple_Check(pItem) )
			strRet += EnumerateTuple(pItem);
		else
		{
			CString strType = GetType(pItem);
			strRet += CA2T(PyString_AsString(pItem));
		}
	}

	return strRet;
}

CString EnumerateList(PyObject* pList)
{
	CString strRet;
	if( pList == 0 )
		return strRet;

	Py_ssize_t nNumItems = PyList_GET_SIZE(pList);
	for(int i = 0; i < nNumItems; i++)
	{
		PyObject* pItem = PyList_GET_ITEM( pList, i );
		CString strType = GetType(pItem);
		CString strValue = CA2T(PyString_AsString(pItem));
		strRet += strValue;
	}

	return strRet;
}

void CPyScriptAutoConfigDlg::ResetFields()
{
	CAutoConfigDlg::ResetFields();
	m_strConverterFilespec.Empty();
}

void CPyScriptAutoConfigDlg::InitFuncNameComboBox()
{
	// initialize Python for the following
	if( !InitPython() )
		return;

	PyObject* pItems = PyDict_Values(m_pDictionary);
	Py_ssize_t nNumItems = PyList_GET_SIZE(pItems);
	for(int i = 0; i < nNumItems; i++)
	{
		PyObject* pItem = PyList_GET_ITEM( pItems, i );
		CString strType = GetType(pItem);
		if( MyPyFunction_Check(pItem) )
		{
			PyFunctionObject* pFunc = (PyFunctionObject *)pItem;
			PyObject* pName = PyFunction_GET_NAME(pFunc);
			CStringA str(PyString_AsString(pName));
			m_cbFunctionNames.AddString(CA2T(str));
		}

#define rdeTesting
#if defined(DEBUG) && !defined(rdeTesting)
		else if( MyPyClass_Check(pItem) )
		{
			PyClassObject* pClass = (PyClassObject*)pItem;
			PyObject* pDict = pClass->cl_dict;
			if( MyPyDict_Check(pDict) )
			{
				CString str = EnumerateDictionary(pDict);
			}
			else if( MyPyString_Check(((PyClassObject*)pItem)->cl_name) )
			{
				CString str = CA2T(PyString_AsString(((PyClassObject*)pItem)->cl_name));
				TRACE(str);
			}
		}
		else if( MyPyType_Check(pItem) )
		{
			PyTypeObject* pType = (PyTypeObject*)pItem;
			TRACE(_T("type: name: (%s)\n"), CA2T(pType->tp_name) );
			CString str = EnumerateDictionary(pType->tp_dict);
			PyMethodDef* pMD = pType->tp_methods;
			if( (pMD != 0) && (pMD->ml_name != 0) )
				TRACE(_T("method name: (%s)\n"), CA2T(pMD->ml_name));
			PyMemberDef* pMbD = pType->tp_members;
			if( (pMbD != 0) && (pMbD->name != 0) )
				TRACE(_T("member name: (%s)\n"), CA2T(pMbD->name));
			_typeobject* pBase = pType->tp_base;
			if( (pBase != 0) && (pBase->tp_name != 0) )
				TRACE(_T("base name: (%s)\n"), CA2T(pBase->tp_name) );

			str = EnumerateTuple(pType->tp_bases);
			str = EnumerateTuple(pType->tp_mro);
			if( pType->tp_repr != 0 )
			{
				PyObject* pRepr = (pType->tp_repr)(pItem);
				CString str(CA2T(PyString_AsString(pRepr)));
				TRACE(str);
			}

			i = 0;
			while(pType->tp_getset[i].get != 0)
			{
				PyObject* pGet = (pType->tp_getset[i++].get)(pItem,0);
				if( MyPyDict_Check(pGet) )
				{
					CString str = EnumerateDictionary(pGet);
					TRACE(str);
				}
				else
				{
					CString str(CA2T(PyString_AsString(pGet)));
					TRACE(str);
				}
			}
		}
		else if( MyPyDict_Check(pItem) )
		{
			CString str = EnumerateDictionary(pItem);
			TRACE(str);
		}
		else if( MyPyList_Check(pItem) )
		{
			CString str = EnumerateList(pItem);
			TRACE(str);
		}
		else if( MyPyString_Check(pItem) )
		{
			CString str(CA2T(PyString_AsString(pItem)));
			TRACE(str);
		}
/*
			// what else can we figure out
			// RESULT: we could get the argument-list, but not the types (i.e. legacy vs. unicode)
			PyCodeObject* pCode = (PyCodeObject*)PyFunction_GET_CODE(pFunc);
			strType = GetType((PyObject*)pCode);
			// IterateList(pCode->co_names);
			IterateTuple(pCode->co_varnames);

			PyObject* pGlobals = PyFunction_GET_GLOBALS(pFunc); // a dictionary
			strType = GetType(pGlobals);
			IterateDictionary(pGlobals);
			// CStringA strGlobals(PyString_AsString(pGlobals));

			PyObject* pModule = PyFunction_GET_MODULE(pFunc);   // module name (we already know)
			strType = GetType(pModule);
			CStringA strModule(PyString_AsString(pModule));

			// PyObject* pDefaults = PyFunction_GET_DEFAULTS(pFunc);
			// strType = GetType(pDefaults);
			// CStringA strDefaults(PyString_AsString(pDefaults));

			PyObject* pClosure = PyFunction_GET_CLOSURE(pFunc);
			strType = GetType(pClosure);
			CStringA strClosure(PyString_AsString(pClosure));

			PyObject* pDoc = PyFunction_GET_DOC(pFunc);
			strType = GetType(pDoc);
			CStringA strDoc(PyString_AsString(pDoc));

			PyObject* pDict = PyFunction_GET_DICT(pFunc);
			strType = GetType(pDict);
			CStringA strDict(PyString_AsString(pDict));

			PyObject* pWRF = PyFunction_GET_WEAKREFLIST(pFunc);
			strType = GetType(pWRF);
			CStringA strWRF(PyString_AsString(pWRF));

			m_cbFunctionNames.SetCurSel(0);
*/
#endif
	}

	if( m_cbFunctionNames.GetCount() != 0 )
	{
		if( m_strFuncName.IsEmpty() )
			m_cbFunctionNames.SetCurSel(0);
		else
			m_cbFunctionNames.SelectString(0, m_strFuncName);

		OnCbnSelchangeCbFuncName();
	}
}

#include "structmember.h"

void CPyScriptAutoConfigDlg::OnBnClickedBtnBrowse()
{
	CFileDialog dlgFile(true,_T(".py"),_T("*.py"),0,_T("Python Script Files (*.py)|*.py||"));
	CString str = GetDir(m_strConverterFilespec);
	dlgFile.m_ofn.lpstrInitialDir = str;

	if (dlgFile.DoModal() != IDOK)
		return;

	// since this is a new table, reset all the relevant fields
	ResetFields();

	m_strConverterFilespec = dlgFile.GetPathName( );

	if( m_strConverterFilespec.IsEmpty() )
		return;

	m_ctlScriptFilespec.SetWindowText(m_strConverterFilespec);

	// initialize the combo box of function names
	InitFuncNameComboBox();

	m_cbFunctionNames.SetFocus();
}

BOOL CPyScriptAutoConfigDlg::OnKillActive( )
{
	// in case we go to the testing page, we don't want to keep the link to
	//  Python open, so close it now.
	ResetPython();
	return CAutoConfigDlg::OnKillActive();
}

void CPyScriptAutoConfigDlg::OnCbnSelchangeCbFuncName()
{
	m_ctlAddlParams.SetWindowText(_T(""));

	if( m_pDictionary == 0 )
		if( !InitPython() )
			return;

	SetModified();    // in any case, this changes things, so enable the Apply button
	int nIndex = m_cbFunctionNames.GetCurSel();
	if( nIndex < 0 )
		m_cbFunctionNames.GetWindowText(m_strFuncName);
	else
		m_cbFunctionNames.GetLBText(nIndex,m_strFuncName);

	if( !m_strFuncName.IsEmpty() )
	{
		PyObject* pFunc = PyDict_GetItemString(m_pDictionary, CT2A(m_strFuncName));
		if( MyPyFunction_Check(pFunc) )
		{
			PyCodeObject* pCode = (PyCodeObject*)PyFunction_GET_CODE(pFunc);
			CStringArray astrArgs;
			EnumerateTuple(pCode->co_varnames, astrArgs, pCode->co_argcount);
			CString strFuncPrototype;
			strFuncPrototype.Format(_T(" def %s(%s):"), m_strFuncName, ToString(astrArgs));
			m_ctlFuncPrototype.SetWindowText(strFuncPrototype);

			// if the argument count is greater than 1, then allow additional parameters
			BOOL bAddlParams = pCode->co_argcount > 1;
			m_ctlAddlParams.EnableWindow(bAddlParams);
			if( bAddlParams )
			{
				CString strPrototypeParams;
				int nExtraArgs = (int)astrArgs.GetCount() - 1;
				for(int i = 0; i < nExtraArgs; i++)
				{
					CString strFormat;  strFormat.Format(_T("<%s>;"), astrArgs[i]);
					strPrototypeParams += strFormat;
				}
				m_ctlAddlParams.SetWindowText(strPrototypeParams.Left(strPrototypeParams.GetLength() - 1));
			}
			return;
		}
	}

	m_ctlAddlParams.EnableWindow(); // just in case, we can't access the file for whatever reasons
	m_ctlFuncPrototype.SetWindowText(_T(""));
}


void CPyScriptAutoConfigDlg::OnEnChangeEditAddlParams()
{
	SetModified();
}
