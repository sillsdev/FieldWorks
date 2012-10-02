/** @file PXPerlWrap.h PXPerlWrap Include File */

//
//         //            \\
//         -- PXPerlWrap --
//         \\            //
//
//
//	Desc.:		Complete Perl embedding solution for your C++ applications
//
//	Compiler:	Visual C++ or Intel C++ Compiler
//	Tested on:	Visual C++ .NET 2003, Intel C++ Compiler 8
//
//	Version:	see PXPW_VERSION
//
//	Created:	13/July/2003
//	Updated:	see PXPW_VERSION_DATE
//
//	Author:		Grégoire Péan, aka PixiGreg
//              mailto:me@pixigreg.com
//
//              http://pixigreg.com
//              http://pixigreg.com/?pxperl
//              http://pixigreg.com/perldoc
//
//	Licensing information
//  ---------------------
/*
1. PXPerlWrap (Binaries) Single Developer Commercial License

	* The license gives you royalty free distribution rights,
	* The license does not allow you to transfer these rights,
	* The license is granted per developer,
	* You won't need to purchase  an additional license for the future  versions
	of the component.
	* A Single  Developer Commercial License  does not entitle  you to a  Source
	Code  License,  nor a  Source  Code License  entitles  you to  a  Commercial
	License.

The  PXPerlWrap commercial license  is  available  free  of  charge  for   using
PXPerlWrap component in:

	* freeware software (freeware is software that is available free of  charge,
	but which is copyrighted by the developer, who retains the right to  control
	its redistribution and to sell it in the future),

		  o excluded is software which is associated with an equipment that  the
		  same  company  manufactures and  which  has no  practical  usage value
		  without the mentioned equipment (e.g. software used for maintenace  of
		  machines) - unless the equipment is free as well,

		  o if you decide to sell the software in the future - you will need  to
		  purchase   the   commercial   license   for   using   the   PXPerlWrap
		  component.

There is  a  fee  for the  commercial license for   using the  component  in the
software not falling  into the  mentioned  category.  Please contact the  author
or  visit the author's web site to find out more  about pricing.

2. PXPerlWrap Source Code License

	* The  license is  granted per  developer, that  is, you  are not allowed to
	redistribute the PXPerlWrap  source code in  any form, but  binary, compiled
	form.
	* You won't need to purchase  an additional license for the future  versions
	of the component. That is, you  may ask the source code for  future releases
	of PXPerlWrap.
	* A Single  Developer Commercial License  does not entitle  you to a  Source
	Code  License,  nor a  Source  Code License  entitles  you to  a  Commercial
	License.

For both licenses, group pricing exists.
*/
//
//  Other Copyright Notices
//  -----------------------
//  Perl: Copyright © 1987-2004, Larry Wall.
//
//  The first release of PXPerlWrap was inspired by CPerlWrap
//  (http://www.codeproject.com/useritems/CPerlWrap.asp) by Harold Bamford.
//
//  The idea of persistent interpreter was found in the Perl doc `perlembed' ;
//  perlembed : copyright © 1995, 1996, 1997, 1998 Doug MacEachern and Jon Orwant.
//
//  The License is widely inspired from the one of ZipArchive by Tadeusz Dracz
//  (http://www.artpol-software.com).
//
//  Known Issues/Limitations:
//  -------------------------
//  - Perl non 0-terminated strings are currently not supported.
//    Unpredictable behaviour.
//
//  TODO
//  ----
//  - STL port
//  - See @todo tags
//  - Send me your suggestions :)
//
//  Miscellaneous
//  -------------
//  I would  like to  thank all  programmers from  codeproject.com for sharing their
//  knowledge with the community. I learned C++ thanks to them. And, a special thank
//  to  Vladimir  Schneider  (www.winpte.com)   for  his  great  help   for  getting
//  redirection to work :)
//
//  Comments are formatted for Doxygen, a great doc generator.
//
//  PLEASE NOTE:  functions check  for 0  strings, and  invalid indexes passed as
//  parameters (most of them  do, actually). If you  fail passing a valid  argument,
//  function won't tell you  about it, but false  will be returned.
//
//  Feedback/questions/answers greatly appreciated: mailto:me@pixigreg.com.
//
////////////////////////////////////////////////////////////////EOC/////////////////


#pragma once


#define PXPW_MFC /**< Define to enable use of MFC classes. Will be used when STL port is done. */


///////////////// Includes

#ifdef PXPW_MFC
#include <afxtempl.h>
#endif

#include <string>
#include <vector>
#include <queue>
#include <map>


///////////////// General Defines & typedef's, Library Stuff

#define PXPW_VERSION		"3.1.0.0" /**< Current version. */
#define PXPW_VERSION_DATE	"27 Aug 2009" /**< Version date. */


#ifndef PXPERL_API


//#error "You must set the library paths below according to where your Perls (release and debug Perl) are installed on your computer! Then remove this error directive."


#	ifdef _DEBUG
#		pragma message("  Linking with Debug version of Perl DLL")
#		pragma comment(lib, "perl510.lib")
#	else
#		pragma message("  Linking with Release version of Perl DLL")
#		pragma comment(lib, "perl510.lib")
#	endif

#	pragma comment(lib, "shlwapi.lib")
#	pragma comment(lib, "urlmon.lib")

#	ifdef PXPERL_EXPORTS
#		define PXPERL_API __declspec(dllexport)
#		ifdef _UNICODE
#			pragma message("  *** Unicode Build ***")
#		else
#			pragma message("  *** MBCS Build ***")
#		endif
#	else
#		define PXPERL_API __declspec(dllimport)
/*

// copy and paste this code in your target application, after setting the correct paths to PXPerlWrap DLL libraries

#		ifdef _DEBUG
#			ifdef _UNICODE
#				pragma message("  Linking with PXPerlWrap Debug Unicode")
#				pragma comment(lib, "../bin/PXPerlWrap-ud.lib")
#			else
#				pragma message("  Linking with PXPerlWrap Debug MBCS")
#				pragma comment(lib, "../bin/PXPerlWrap-d.lib")
#			endif
#		else
#			ifdef _UNICODE
#				pragma message("  Linking with PXPerlWrap Release Unicode")
#				pragma comment(lib, "../bin/PXPerlWrap-u.lib")
#			else
#				pragma message("  Linking with PXPerlWrap Release MBCS")
#				pragma comment(lib, "../bin/PXPerlWrap.lib")
#			endif
#		endif

*/

#	endif

#endif


#ifndef RT_PERL
#define RT_PERL					0x00850000 /**< Perl script resource type for loading a script from resources. */
#endif


#define DEFAULT_TIMEOUT			1000 /**< Default timeout for several redirection operation (used in ::WaitForSingleObject()). */
#define DEFAULT_PIPE_SIZE		4096 /**< Default redirection pipe size. */


#define WM_PXPW_OUTPUT			(WM_USER + 0x85) /**< @see PXIORedirect::Initialize() */
#define ON_PXPW_OUTPUT(fn)		ON_MESSAGE(WM_PXPW_OUTPUT, fn) /**< For your MFC dialog message map. */


#define PXPW_REDIR_OUTPUT		1 /**< wParam for WM_PXPW_OUTPUT message. Indicates data comes from standard output stream. */
#define PXPW_REDIR_ERRORS		2 /**< wParam for WM_PXPW_OUTPUT message. Indicates data comes from standard error stream. */


#define WM_PXPW_THREAD_NOTIFY	(WM_USER+0x86) /**< Notification message sent when using the threaded script execution. @see CPerlInterpreter::RunThread(). */
#define PXPW_THREAD_STARTED		1 /**< Sent upon thread start. */
#define PXPW_THREAD_ENDED		2 /**< Sent upon thread end. */
#define PXPW_THREAD_ABORTED		3 /**< Sent after thread aborted. */


/**
* Used internaly.
*/
typedef UINT_PTR PerlID;

typedef void (*fnXSInitProc)(LPVOID);


/////////////////


#ifdef _UNICODE
#define tstring std::wstring
#else
#define tstring std::string
#endif


///////////////// namespace PXIORedirect

/**
* @namespace PXIORedirect Redirection Standard Streams Redirection
* @version 2.0.3.0
*/
namespace PXIORedirect
{
	/**
	* Enumeration of the different redirection modes.
	*/
	typedef enum eRedirectDestination
	{
		DestNone	= 1 << 0, /**< Indicates there is no destination, i.e. we do not care about what is being printed in streams. */
		DestWindow	= 1 << 1, /**< Destination is a window. A WM_PXPW_OUTPUT message will be sent to window specified as pParam. The message wParam is the message type (either PXPW_REDIR_OUTPUT or PXPW_REDIR_ERRORS, or PXPW_INFO, PXPW_WARNING, PXPW_ERROR, PXPW_CRITICAL if the window specified for errors is the same). @warning you must construct a PXIORedirect::CIOBuffer object in your message procedure. @see PXIORedirect::CIOBuffer */
		DestFileFH	= 1 << 2, /**< Destination is a file which file handle (FILE*, returned by fopen() etc) is specified as pParam. */
		DestFileFD	= 1 << 3, /**< Destination is a file which file descriptor (int, low-level file functions such as _open() etc.) is specified as pParam. */
		DestCallback= 1 << 4  /**< Destination is a callback function. pParam will be the callback function, of fnPXIOCallback type. @version 2.0.5.0 @see fnPXIOCallback @warning You callback function must be as quick as possible since it prevents reading further data in the redirection pipe until it returns.*/
	} RedirectDestination;

	/**
	* The MUST-TO-USE class when receiving a redirection message.
	* It embeds safety checks, informs of the message type, and
	* ensures the proper deletion of buffers allocated on the heap
	* for each message posted by PXPW.
	* Even if you don't actually use a message, ALWAYS construct
	* a CIOBuffer in the message proc.
	* @version 2.0.3.0
	*/
	class PXPERL_API CIOBuffer
	{
	public:
		/*
		* @see PXSetErrorMsgDestWindow()
		*/
		typedef enum eType
		{
			TypeInvalid		= 0, /**< Type is invalid/unknown: buffer is invalid. No hope for recovering the data. */
			TypeOutputData	= 1, /**< STDOUT data. */
			TypeErrorsData	= 2, /**< STDERR data. */
			TypeInfo		= 10, /**< Indicates an information message. */
			TypeWarning		= 11, /**< Indicates a warning message. */
			TypeError		= 12, /**< Indicates an error message. */
			TypeCritical	= 13, /**< Indicates a critical error message. */
		} Type;

		typedef struct sBufferInfo // sizeof(sBufferInfo) = sizeof(WPARAM) = 4
		{
			BYTE cType;
			BYTE cSignature;
			USHORT nBufSize;
		} BufferInfo;

		enum { signature = 151 };

		/**
		* Construct a buffer. Pass the message wParam and lParam.
		*/
		CIOBuffer(WPARAM wParam, LPARAM lParam);

		~CIOBuffer();

		/**
		* @return the current buffer type.
		*/
		Type GetType(void) const;

		/**
		* @return the current buffer size, ie. the number of "char" or "wchar_t" if the buffer is by wide by default.
		*/
		USHORT GetSize(void) const;

		/**
		* @return true if the buffer is originately wide.
		*/
		bool IsDefaultWide(void) const;

		/**
		* @return the MBCS version of the buffer.
		*/
		operator LPCSTR();

#ifdef _UNICODE
		/**
		* Available only under Unicode builds.
		* @return the wide char version of the buffer.
		*/
		operator LPCWSTR();
#endif
	protected:
		BufferInfo m_info;
		LPVOID m_buffer;
#ifdef _UNICODE
		std::string m_strABuffer;
		std::wstring m_strWBuffer;
#endif
	};

	/**
	* Typedef for the redirection callback.
	* dwStream will be either PXPW_REDIR_OUTPUT or PXPW_REDIR_ERRORS.
	* sData is the data pointer.
	* nSize is the data size.
	* @version 2.0.5.0
	*/
	typedef void (CALLBACK *fnPXIOCallback)(DWORD dwStream, LPSTR sData, UINT nSize);

	/*
	* Typedef for using the DestCallback redirection type.
	* @version 2.0.5.0
	*/
	//typedef struct
	//{
	//	fnPXIOCallback callback; /*< The static callback function which will be called when data was printed to STDOUT/STDERR. @see fnPXIOCallback */
	//	DWORD dwUser; /*< User data which will be passed to the callback. */
	//} CallbackFunction;

	/**
	* Initializes standard streams redirection, i.e. STDOUT and STDERR.
	* @param nWindowMessage Window message (applies only when destination is set to DestWindow).
	* @param nPipeSize Pipes size.
	* @return true on success, false otherwise.
	* @version 2.0.3.0
	*/
	PXPERL_API bool Initialize(UINT nWindowMessage=WM_PXPW_OUTPUT, UINT nPipeSize=4096);

	/**
	* Stops redirection, stopping redirection threads. Function may be called at any time.
	* @param bFlush true to flush the pipe (redirect any data still in it), if false to terminate immediately.
	* @param nTimeout limit time allowed for flushing.
	* @version 2.0.3.0
	*/
	PXPERL_API void Uninitialize(bool bFlush=false, UINT nTimeout=DEFAULT_TIMEOUT);

	/**
	* Sets destination for redirected streams. Function may be called at any time.
	* @param dwStream a combination of PXPW_REDIR_OUTPUT and PXPW_REDIR_ERRORS.
	* @param dest a destination (one of the eRedirectDestination enum values).
	* @param pParam destination parameter.
	* @param nTimeout limit time allowed for setting.
	* @return true on success, false otherwise.
	* @see eRedirectDestination for available destinations and params.
	* @version 2.0.3.0
	*/
	PXPERL_API bool SetDestination(DWORD dwStream, RedirectDestination dest,
		LPVOID pParam=0, UINT nTimeout=DEFAULT_TIMEOUT);

	/**
	* Saves the current destination(s), then change them. Function may be called at any time.
	* @param dwStream a combination of PXPW_REDIR_OUTPUT and PXPW_REDIR_ERRORS.
	* @param dest a destination (one of the eRedirectDestination enum values).
	* @param pParam destination parameter.
	* @param nTimeout limit time allowed for changing.
	* @return true on success, false otherwise.
	* @see eRedirectDestination for available destinations and params.
	* @see RestoreDestination()
	* @version 2.0.3.0
	*/
	PXPERL_API bool ChangeDestination(DWORD dwStream, RedirectDestination dest,
		LPVOID pParam=0, UINT nTimeout=DEFAULT_TIMEOUT);

	/**
	* Restores the saved destination(s) from a previous call to ChangeDestination.
	* @param dwStream a combination of PXPW_REDIR_OUTPUT and PXPW_REDIR_ERRORS.
	* @param nTimeout limit time allowed for restoring.
	* @return true on success, false otherwise.
	* @see eRedirectDestination for available destinations and params.
	* @see ChangeDestination()
	* @version 2.0.3.0
	*/
	PXPERL_API bool RestoreDestination(DWORD dwStream, UINT nTimeout=DEFAULT_TIMEOUT);

	/**
	* Flushes the specified stream(s), redirecting all data still in pipe(s).
	* @warning on successful flush, redirection threads go in PAUSE state; you have to call Resume() to resume them.
	* @param dwStream a combination of PXPW_REDIR_OUTPUT and PXPW_REDIR_ERROR.
	* @param nTimeout limit time allowed for flushing.
	* @return true on success, false otherwise.
	* @version 2.0.3.0
	*/
	PXPERL_API bool Flush(DWORD dwStream, UINT nTimeout=DEFAULT_TIMEOUT);

	/**
	* Pauses redirection.
	* @param dwStream a combination of PXPW_REDIR_OUTPUT and PXPW_REDIR_ERROR.
	* @param nTimeout limit time allowed for pausing.
	* @return true on success, false otherwise.
	* @version 2.0.3.0
	*/
	PXPERL_API bool Pause(DWORD dwStream, UINT nTimeout=DEFAULT_TIMEOUT);

	/**
	* Resumes redirection.
	* @param dwStream a combination of PXPW_REDIR_OUTPUT and PXPW_REDIR_ERROR.
	* @return true on success, false otherwise.
	* @version 2.0.3.0
	*/
	PXPERL_API bool Resume(DWORD dwStream);

	/**
	* @return true if redirection is active.
	* @version 2.0.3.0
	*/
	PXPERL_API bool IsRedirecting(void);

	/**
	* Writes custom data to STDIN.
	* @param lpData memory pointer.
	* @param nSize data size.
	* @return true on success, false otherwise.
	* @version 2.0.3.0
	*/
	PXPERL_API bool Write(LPVOID lpData, UINT nSize);

	/**
	* Writes a string to STDIN.
	* @param szData text data.
	* @return true on success, false otherwise.
	* @version 2.0.3.0
	*/
	PXPERL_API bool Write(LPCTSTR szData);
};



///////////////// namespace PXPerlWrap

/**
* @namespace PXPerlWrap The Core Namespace
* @todo STL port
*/
namespace PXPerlWrap
{

	///////// UTF8

	/** Modes for PXSetUTF8(). Applies only to Unicode builds. */
	typedef enum eUTF8Mode
	{
		UTF8_off	= 1 << 0, /**< "No" conversion is made (strings are converted to MBCS then passed to Perl). */
		UTF8_on		= 1 << 1, /**< All strings are converted to UTF8 in Perl. */
		UTF8_auto	= 1 << 2 /**< If overwriting the value of an existing Perl scalar, check if it is an UTF8 encoded one, and in this case convert the string to UTF8. Otherwise, same behaviour as eUTF8Mode::UTF8_off. */
	} UTF8Mode;

	/**
	* Sets the PXPerlWrap behaviour regarding C++ strings to Perl strings conversion.
	* This doesn't affect Perl to C++ conversion: all UTF8 encoded Perl strings will
	* be converted to wide strings, i.e. Unicode ones; in MBCS build, an additional
	* conversion is made to pass from Unicode to MBCS string (ATL CW2A macro).
	* Function may be called at any time.
	* @param mode UTF8 mode. In MBCS builds this cannot be changed from eUTF8Mode::UTF8_off.
	* @return the actual changed mode; in MBCS builds, will be always eUTF8Mode::UTF8_off.
	* @see eUTF8Mode for an explanation on the different modes available.
	*/
	PXPERL_API UTF8Mode	PXSetUTF8(UTF8Mode mode=UTF8_on);

	/**
	* Sets the destination window for error/warning/info notifications. Function may be called at any time.
	* @param hWnd window where to send messages.
	* @param nWindowMessage the message sent.
	* @warning you must construct a PXIORedirect::CIOBuffer object in your message procedure.
	* @see PXIORedirect::CIOBuffer
	* @version 2.0.3.0
	*/
	PXPERL_API void		PXSetErrorMsgDestWindow(HWND hWnd=0, UINT nWindowMessage=WM_PXPW_OUTPUT);

	/**
	* Writes the error/warning/info notifications to a log file.
	* Can be used conjointly with window message notifications.
	* @param szFile file path. If the path ise relative, file will be created under the exe directory. If 0 passed, stops logging. Function may be called at any time.
	* @param bAppend wether to append text data to the file or not.
	* @warning you must construct a PXIORedirect::CIOBuffer object in your message procedure.
	* @see PXIORedirect::CIOBuffer
	* @version 2.0.3.0
	*/
	PXPERL_API bool		PXSetErrorMsgDestFile(LPCTSTR szFile=0, bool bAppend=true);

	/**
	* Lets you choose what notification to send to window/write to log file.
	* @param nLevel 0 = all notifications; 1 = no info notification; 2 = no info, no warning; 3 = no error, only critical errors caught.
	* @version 2.0.3.0
	*/
	PXPERL_API void		PXSetErrorMsgLevel(int nLevel=0);

	/**
	* Sends a custom info notification.
	* @return true if message successfuly sent.
	* @version 2.0.3.0
	*/
	PXPERL_API bool		PXInfo(LPCTSTR szText, ...);

	/**
	* Sends a custom warning notification.
	* @return true if message successfuly sent.
	* @version 2.0.3.0
	*/
	PXPERL_API bool		PXWarning(LPCTSTR szText, ...);

	/**
	* Sends a custom error notification.
	* @return true if message successfuly sent.
	* @version 2.0.3.0
	*/
	PXPERL_API bool		PXError(LPCTSTR szText, ...);

	/**
	* Sends a custom critical error notification.
	* @return true if message successfuly sent.
	* @version 2.0.3.0
	*/
	PXPERL_API bool		PXCriticalError(LPCTSTR szText, ...);

	/**
	* Utility function: returns the actual directory of the executable against which PXPerlWrap is linked.
	* @param strRet CString object to receive the exe dir.
	* @return the same object.
	* @version 2.0.3.0
	*/
	PXPERL_API CString& PXGetExeDir(CString &strRet);

	/**
	* @see PXSetXS().
	* @version 2.0.3.0
	*/
	class XSItem
	{
	public:
		XSItem(LPCSTR szName=0, LPVOID fn=0) { if (szName) strncpy_s(sName, szName, 256); else *sName = 0; lpFunc = fn; };
		const XSItem& operator=(const XSItem &item) { if (item.sName) strncpy_s(sName, item.sName, 256); else *sName = 0; lpFunc = item.lpFunc; return item; };
		CHAR sName[256];
		LPVOID lpFunc;
	};

	/**
	* Allows you to specify XS functions to be registered upon interpreters loading.
	* XS is the name for the glue between C++ and Perl. If you register a XS function with the alias
	* "boot_func" (see below), you'll be able to call this function from within your Perl scripts, without
	* requiring use'ing any other module.
	* This function may be called at any time; it won't affect the currently loaded interpreters.
	* Example: XSItem items[] = { XSItem("boot_func", pl_boot_func), ..., 0 }; PXSetXS(items);
	* @param pXSList a pointer to a XSItem array. Pass 0 to empty internal array.
	* @version 2.0.3.0
	* @see SWIG (http://www.swig.org) for an automated generation of XS functions, which enables you to export any of your program C/++ function.
	*/
	PXPERL_API void		PXSetXS(const XSItem* pXSList=0);

	/**
	* Sets what Perl modules to load upon interpreters loading.
	* Don't forget to specify a -Imymodulespath using PXSetCommandLineOptions()
	* to indicate where modules are located. Function may be called at any time.
	* You can load a module after loading using CPerlInterpreter::LoadModule() (same effect).
	* @param pStrAModules an array pointer to modules list (eg. "Win32", "LWP::Simple", ...). Pass 0 to empty internal array.
	* @version 2.0.3.0
	*/
	PXPERL_API void		PXSetDefaultModules(const CStringArray* pStrAModules=0);

	/**
	* Sets the interpreters command line options (the ones which are displayed when you run perl.exe -h).
	* Overrides the PXPerlWrap.opt file options.
	* Useful to specify a module location: -Imymodulespath
	* Function may be called at any time.
	* Use PXSetModules() or CPerlInterpreter::LoadModule() instead of passing -MModuleName to this function.
	* @param pStrAOptions an array pointer to options list. Pass 0 to empty internal array.
	* @version 2.0.3.0
	*/
	PXPERL_API void		PXSetCommandLineOptions(const CStringArray* pStrAOptions=0);


	//PXPERL_API void			PXSetLastError(LPCTSTR szText, ...);
	//PXPERL_API LPCTSTR		PXGetLastError();


	///////////////// Classes

	class CPerlInterpreter;
	class CInterpPool;
	class CScriptAttributes;
	class CScript;
	class CPerlVariable;
	class CPerlScalar;
	class CPerlArray;
	class CPerlHash;

	/** @class CPerlInterpreter
	* Represents an interpreter. All interpreter are independent, and all Perl variable objects refer to a single interpreter.
	* Besides, each script is parsed/run for a single interpreter.
	*/
	class PXPERL_API CPerlInterpreter
	{
		friend class CPerlVariable;

	public:
		/**
		* Constructs a Perl interpreter. Does not actually load it (yeah that's not pure OO code :P).
		* I find this more practical, as you may want to reload an interpreter.
		* @see CPerlInterpreter::Load()
		*/
		CPerlInterpreter();
		/**
		* Destruction. Nothing special done.
		*/
		~CPerlInterpreter();

		/**
		* Parses a script.
		* @param script a script object.
		* @return true if script is already parsed, or was parsed successfuly. False if the script is not valid (not correctly loaded), or if the parse failed.
		*/
		bool Parse(CScript& script);

		/**
		* Runs a script. Script must have been previously parsed with the same interpreter.
		* @param script a script object.
		* @return true if script was run successfuly. False if script is not parsed for the current interpreter, or if the run failed.
		*/
		bool Run(CScript& script);

		/**
		* Same as calling Parse(), then Run().
		* @return true if both operations succeeded.
		* @version 2.0.3.0
		*/
		bool ParseRun(CScript& script);

		/**
		* Runs a script in a separate thread. Only one script can be run in a thread at a time for each interpreter.
		* @return true if thread is successfuly launched.
		* @version 2.0.3.0 function added
		* @version 2.0.4.2 script object no longer needs to remain valid while the script is running
		*/
		bool RunThread(CScript& script, HWND hNotifyWnd=0, UINT nMessage=WM_PXPW_THREAD_NOTIFY);

		/**
		* Aborts the thread if it is running.
		* @version 2.0.3.0
		*/
		void StopThread(UINT nTimeout=0);

		/**
		* @return true if a script is running in a thread.
		* @version 2.0.3.0
		*/
		bool IsThreadRunning(void);

		/**
		* Loads a module. May be called at any time, if the interpreter is loaded.
		* Loading a bunch of modules increases memory usage.
		* @param szModuleName the module name (eg. "Image::Magick").
		* @return true is successful.
		* @version 2.0.3.0
		*/
		bool LoadModule(LPCTSTR szModuleName);

		/**
		* Cleans a script, freeing associated variables.
		* @param script a script object.
		* @return true if script was cleaned successfuly. False if script is not parsed for the current interpreter, or if the clean failed.
		*/
		bool Clean(CScript& script);

		/**
		* Evaluates a Perl snippet. The CScript object must contain a loaded and parsed script (it can have been run, too).
		* @param script a script object.
		* @param szEval the Perl code to be evaluated.
		* @param ... variable arguments.
		* @warning ALWAYS CHECK THE ARGUMENTS YOU PASS TO A VARIABLE ARGUMENTS FUNCTION.
		* @return a scalar object, containing result of the Perl eval. For example, if you do eval "$foo = 'bar'; $abc = join('', 'abc');", the eval result is the value of $abc. If the evaluation is unsuccessful, an invalid CPerlScalar is returned.
		* @see CPerlVariable::IsValid()
		*/
		CPerlScalar Eval(CScript& script, LPCTSTR szEval, ...);

		/**
		* The same as RunThread() is to Run(), but for Eval().
		* @warning only a single thread can be launched (by RunThread/EvalThread) per interpreter.
		* @version 2.0.4.2
		*/
		bool EvalThread(CScript& script, LPCTSTR szEval, HWND hNotifyWnd=0, UINT nMessage=WM_PXPW_THREAD_NOTIFY, ...);


		/**
		* Get a scalar from a script. Scalar can exist or not. If it doesn't, it will be created.
		* @param script a script object.
		* @param szVariable The variable name.
		* @param bCreateIfNotExisting Set to true to allow creating the scalar variable if it does not exist. Otherwise the object returned will be invalid.
		* @return a scalar object. On failure, an invalid CPerlScalar is returned.
		* @see CPerlVariable::IsValid()
		*/
		CPerlScalar GetScalar(CScript& script, LPCTSTR szVariable, bool bCreateIfNotExisting=true);

		/**
		* Get an array from a script. Array can exist or not. If it doesn't, it will be created.
		* @param script a script object.
		* @param szVariable The variable name.
		* @param bCreateIfNotExisting Set to true to allow creating the array variable if it does not exist. Otherwise the object returned will be invalid.
		* @return a scalar object. On failure, an invalid CPerlArray is returned.
		* @see CPerlVariable::IsValid()
		*/
		CPerlArray GetArray(CScript& script, LPCTSTR szVariable, bool bCreateIfNotExisting=true);

		/**
		* Get a hash from a script. Hash can exist or not. If it doesn't, it will be created.
		* @param script a script object.
		* @param szVariable The variable name.
		* @param bCreateIfNotExisting Set to true to allow creating the hash variable if it does not exist. Otherwise the object returned will be invalid.
		* @return a scalar object. On failure, an invalid CPerlHash is returned.
		* @see CPerlVariable::IsValid()
		*/
		CPerlHash GetHash(CScript& script, LPCTSTR szVariable, bool bCreateIfNotExisting=true);

		/**
		* Loads the interpreter.
		* @return true if successful, false otherwise.
		*/
		 bool Load(void);

		/**
		* @return true if the interpreter is loaded, false otherwise.
		*/
		bool IsLoaded(void) const;

		/**
		* Unloads the interpreter.
		* @return true if successful, false otherwise.
		*/
		void Unload(void);

		/**
		* Returns a pointer to the interpreter. Cast to PerlInterpreter*.
		* Intended for PXPerlWrap extension.
		* @warning Be CAREFUL handling this pointer ;)
		* @return a non-0 pointer if interpreter is properly loaded.
		*/
		void* GetMyPerl(void);

		/**
		* @return true if the interpreter is persistent, false otherwise.
		* @version 2.0.3.0 Function always returns true, since every interpreter has to be persistent.
		*/
		bool IsPersistent(void) const;

	public:
		static fnXSInitProc m_xs_init_addr;
		//static UINT __stdcall RunProc(LPVOID pParam);

	private:
		void	*m_pMyPerl;
		bool	m_bPersistent;
		CScript *m_pParsedScriptNonPersistent;
		PerlID	m_idCurPackage;
		HWND	m_hNotifyWnd;
		UINT	m_nMessage;
	};


	// private class for storing a script attributes
	class CScriptAttributes
	{
	public:
		typedef enum eScriptFlag
		{
			FlagNone	= 0,
			FlagParsed	= 1,
			FlagRun		= 2,
			FlagDebug	= 4
		} ScriptFlag;
		CScriptAttributes(LPCTSTR szPersistentPackageName=_T(""), DWORD dwFlags=FlagNone);
		CString m_strPersistentPackageName;
		DWORD m_dwFlags;
	};

	/** @class CScript
	* a script object. Holds the script content physically (that is, a string contains either the script file or script itself).
	* Holds also information about whether the script is parsed and/or run for a particular interpreter.
	*/
	class PXPERL_API CScript
	{
		friend class CPerlInterpreter;

	public:

		/** Script types for Load() */
		typedef enum eType
		{
			TypeNone		= 1 << 0, /**< Means type is undefined. */
			TypePlain		= 1 << 1, /**< Script is plain, good ol' ASCII text. */
			TypeBytecode	= 1 << 2 /**< Script is Perl bytecode. @version 2.0.3.0 support for bytecode removed. */
		} Type;

		/** Source types for Load() */
		typedef enum eSource
		{
			SourceNone		= 1 << 0, /**< Means source is undefined. */
			SourceInline	= 1 << 1, /**< The source is an inline, directly supplied, script. */
			SourceFile		= 1 << 2, /**< The source is a file path. */
			SourceResource	= 1 << 3, /**< The source is a RT_PERL resource. Use MAKEINTRESOURCE(id) to get a LPCTSTR. */
			SourceURL		= 1 << 4 /**< The source is an URL. */
		} Source;

		/**
		* Constructs a script object.
		* @see CScript::Load() to load/reload a script.
		*/
		CScript();

		/**
		* Constructs a script object.
		* @param szInlineScript an inline, plain text, script.
		* @version 2.0.3.0
		*/
		CScript(LPCTSTR szInlineScript);

		/**
		* Destroys a script object. Scripts cannot be unloaded, but can be reloaded.
		*/
		~CScript();

		/**
		* Clones a CScript object.
		* @version 2.0.3.0
		*/
		CScript& operator=(const CScript &script);

		/**
		* Loads an inline script.
		* @param szInlineScript an inline, plain text, script.
		* @version 2.0.3.0
		*/
		CScript& operator=(LPCTSTR szInlineScript);

		/**
		* Loads a script, from various sources. Loading from an URL relies on API function URLDownloadToFile().
		* @param szSource The source, that is, either the script itself, or a file containing the script, or an URL, or a resource (use MAKEINTRESOURCE in this case).
		* @param source Specifies source type.
		* @param type Specifies script type.
		* @return true on success, false otherwise.
		*/
		bool Load(LPCTSTR szSource, Source source=SourceInline, Type type=TypePlain);

		/**
		* @return true if the script is loaded, false otherwise.
		*/
		bool IsLoaded(void) const;

		/**
		* Saves the current script (either text or bytecode) to a file.
		* @param szFile File path.
		* @return true on success, false otherwise.
		*/
		bool SaveToFile(LPCTSTR szFile);

		/**
		* Tests the current script: it will tell you if the script is parsed successfuly, hides any error anyway (if you want the errors, parse a script the usual way).
		* @return true if script is parsed successfuly, false otherwise.
		*/
		bool Test(void);

		/**
		* Reformats the current script to STDOUT.
		* @return true if script is parsed and formatted successfuly, false otherwise.
		*/
		bool Reformat(void);

		/* REMOVED
		* Sets the arguments for the scripts (ie. the @ARGV array of the script).
		* @return the custom ARGV for the script. You can, for example, do "script.GetARGV().Add("Arg1"); ..."
		* @version 2.0.3.0 function HAS NO LONGER ANY EFFECT.
		*/
		//CStringArray& GetARGV(void);

		/**
		* Returns the script as a string. [Bytecode Support Removed]If the script is bytecode, "PLBCMSWin32-x86-multi-thread" is returned.[/Bytecode Support Removed]
		* @param strRet A CString to receive the script.
		* @return the same string than passed as param. Empty string is returned on failure.
		*/
		CString& GetScript(CString& strRet);

		/**
		* Get the script as a string. [Bytecode Support Removed]If the script is bytecode, "PLBCMSWin32-x86-multi-thread" is returned.[/Bytecode Support Removed]
		* @return a read-only CString object with script content. Empty string is returned on failure.
		*/
		const CString& GetScript(void);

		/**
		* Get the script as a memory pointer. [Bytecode Support Removed]Useful to access a bytecode script content.[/Bytecode Support Removed]
		* @return a pointer to the script, allocated on the heap, if successful, 0 otherwise. Free the pointer using delete [] pointer.
		*/
		LPVOID GetScript(DWORD &dwSize);

		/**
		* @return script type.
		*/
		Type GetType(void) const;

		/**
		* [Bytecode Support Removed]
		* Change type. Only Plain -> Bytecode is supported for the moment.
		* Use it to compile script to bytecode.
		* @todo !DONE, but BYTECODE SUPPORT REMOVED! Fix redirection problem. Surely will have to modify O/B Perl modules.
		* @param newType The new type, only Bytecode supported.
		* @return true on success, false otherwise.
		* [/Bytecode Support Removed]
		*/
		bool ChangeType(Type newType);

		/**
		* @param pInterp Pointer to an interpreter.
		* @return true if the script was parsed successfuly by the specified interpreter.
		*/
		bool IsParsed(CPerlInterpreter *pInterp) const;

		/**
		* @param pInterp Pointer to an interpreter.
		* @return true if the script was run successfuly (at least one time) by the specified interpreter.
		*/
		bool IsRun(CPerlInterpreter *pInterp) const;

		/**
		* @version 2.0.3.0
		*/
		bool IsFlagSet(CPerlInterpreter *pInterp, DWORD dwFlags);

		/**
		* @param pInterp Pointer to an interpreter.
		* @return a read-only CString object containing the package name created for this script by the specified interpreter. String may be empty if interpreter is not persistent or if script has not been parsed so far.
		*/
		CString GetPersistentPackage(CPerlInterpreter *pInterp) const;

	protected:
		void SetPersistentPackage(CPerlInterpreter *pInterp, LPCTSTR szPackage);
		void SetFlag(CPerlInterpreter *pInterp, DWORD dwFlags);
		void ResetFlag(CPerlInterpreter *pInterp, DWORD dwFlags);

		void SetParsed(CPerlInterpreter *pInterp, bool bParsed=true);
		void SetRun(CPerlInterpreter *pInterp, bool bRun=true);
//		void SetDebug(CPerlDebugInterpreter *pInterp, bool bDebug=true);

		void DestroyAttributes(CPerlInterpreter *pInterp);

		void Destroy(void);

		CString& GetScriptP(void);
		CString& Flush(void);
		void Unlink(void);

	private:
		bool m_bLoaded;
		bool m_bReadOnly;
		Type m_type;
		CString m_strScript;
		CString m_strFile;
		//CStringArray m_strA_ARGV;
		//CStringArray m_strACustomOpts;
		CMap<LPVOID, LPVOID, CScriptAttributes, CScriptAttributes&> m_interpAttrMap;
	};



	/** @class CPerlVariable
	* Father class for all Perl variable classes (CPerlScalar, CPerlArray and CPerlHash).
	* Some methods are available to the user.
	*/
	class PXPERL_API CPerlVariable
	{
		friend class CPerlScalar;
		friend class CPerlArray;
		friend class CPerlHash;
		friend class CPerlInterpreter;

	public:
		CPerlVariable();
		~CPerlVariable();

		/**
		* @return true if the current variable is valid, that is, associated with a valid interpreter; false otherwise.
		*/
		bool IsValid(void) const;

		/**
		* @return a read-only CString object containing PackageName::VariableName.
		*/
		const CString& GetName(void) const;

		/**
		* Resets the variable object. Useful if you want to re-use a variable name and do not want a copy, but a real clone of a vraiable object returned by a function.
		*/
		virtual void Destroy(void);

		/**
		* @return the associated Perl's variable pointer of the variable object (i.e. SV* for CPerlScalar, AV* for CPerlArray, HV* for CPerlHash).
		* @warning for PXPerlWrap extension. Use very carefully.
		* @version 2.0.3.0
		*/
		void* GetParam(void);

	protected:
		virtual void Create(CPerlInterpreter *pInterp=0, LPCTSTR szName=_T(""), void *pParam=0);
		void Clone(const CPerlVariable& var);

		void* GetMyPerl(void) const;
		CPerlInterpreter* GetInterp(void) const;
		const void* GetParam(void) const;

	private:
		PerlID	m_idInterp;
		CString m_strName;
		void	*m_pParam;
	};


	/** @class CPerlScalar
	* Represents a Perl scalar.
	*/
	class PXPERL_API CPerlScalar : public CPerlVariable
	{
		friend class CPerlArray;
		friend class CPerlHash;
		friend class CPerlInterpreter;

	public:
		CPerlScalar(); /**< Constructs a CPerlScalar object, marked as invalid, since not associated with a real Perl scalar. */
		/**
		* Constructs a scalar, cloning the passed CPerlScalar object if this one is invalid, or copying its value if valid.
		* @param scalar CPerlScalar object to be copied.
		* @see operator=()
		*/
		CPerlScalar(const CPerlScalar &scalar);
		~CPerlScalar(); /**< Destroys the object. Decrements reference count, freeing the scalar if possible. */

		/**
		* Clones the passed the passed CPerlScalar object if this one is invalid, or copying its value if valid.
		* @param scalar CPerlScalar object to be cloned/copied.
		* @return the CPerlScalar object passed.
		*/
		const CPerlScalar& operator= (const CPerlScalar &scalar);

		int length(void); /**< @return the length, in characters, or the string. */
		void undef(void); /**< Clears the Perl scalar value and frees the memory asscoiated with it. */
		//void clear(void); /**< Clears the Perl scalar value. */

		operator int() const; /**< @return the integer value of the Perl scalar. */
		int Int(int nDefault=0) const; /**< @return the integer value of the Perl scalar (explicit call). */
		int Int(int nDefault, int nMin, int nMax) const;
		int operator*= (int value); /**< Implements integer multiplication. @return the result value. */
		int operator/= (int value); /**< Implements integer division. @return the result value. */
		int operator+= (int value); /**< Implements integer addition. @return the result value. */
		int operator-= (int value); /**< Implements integer substraction. @return the result value. */
		int operator= (int value); /**< Implements integer assignment. @return the new value. */

		operator double() const; /**< @return the float value of the Perl scalar. */
		double Double(double fDefault=0.0) const; /**< @return the float value of the Perl scalar. */
		double Double(double fDefault, double fMin, double fMax) const;
		double operator*= (double value); /**< Implements float multiplication. @return the result value. */
		double operator/= (double value); /**< Implements float division. @return the result value. */
		double operator+= (double value); /**< Implements float addition. @return the result value. */
		double operator-= (double value); /**< Implements float substraction. @return the result value. */
		double operator= (double value); /**< Implements float assignment. @return the new value. */


		/*
		* @param strRet String to receive the string value of the Perl scalar.
		* @return the passed string.
		*/
		//CString& String(CString& strRet) const;
		CString String() const;
		std::string StdStringA() const;
		std::wstring StdStringW() const;
		tstring StdString() const;

		// no longer available, because it requires returning a temporary pointer, which is not safe and works anyway half the time

		//operator LPCTSTR() const; /**< @return the string value of the Perl scalar. @warning This is a temporary pointer which mustn't be stored for later use. */

#ifdef _UNICODE
		//operator LPCSTR() const; /**< @return the string value of the Perl scalar. @warning This is a temporary pointer which mustn't be stored for later use. */
#endif
		//const CString& operator= (const CString& value); /**< Implements string assignment. @return the new value, a read-only CString object. */

		//const std::string& operator= (const std::string& value);
		//const std::wstring& operator= (const std::wstring& value);

		LPCTSTR operator= (LPCTSTR value); /**< Implements string assignment. @return the new value, a read-only string. */
		/** Implements string concatenation.
		* @return the result string, read-only.
		*/
		LPCTSTR operator+= (LPCTSTR value);

		/**
		* @return true if the scalar is true as Perl means it. False otherwise.
		*/
		bool IsTrue() const;
		/**
		* @return true if the scalar native type for Perl is integer.
		*/
		bool IsInt() const;
		/**
		* @return true if the scalar native type for Perl is float.
		*/
		bool IsDouble() const;
		/**
		* @return true if the scalar native type for Perl is string.
		*/
		bool IsString() const;
		/**
		* @return true if the scalar is UTF8 encoded.
		*/
		bool IsUTF8() const;
		/**
		* The extra Perl-like function. However, there is no "sv_split" function exported by Perl. Therefore, calling this function is roughly the same as calling Eval("split...") in terms of performance but is more convenient.
		* @param szPattern The split pattern (eg. "m!!" or "/[abc]{2}/i").
		* @return a valid CPerlArray object upon success.
		* @see CPerlArray::join()
		*/
		CPerlArray split(LPCTSTR szPattern);


		void UTF8CheckSetFlag(); /**< If UTF8 flag is not set, performs a check on bytes to determine if the string is likely to be UTF8 encoded. Set the UTF8 flag in this case. */
		void UTF8SetForceFlag(bool bIsUTF8=true); /**< Force the UTF8 flag to be set or not. @param bIsUTF8 true to set the flag, false otherwise. @warning This function is intended for advanced users, since it lets you deal with UTF8 manually. */
		void UTF8Upgrade(); /**< Upgrades the Perl string to UTF8 encoding. Calls the Perl sv_utf8_upgrade() function. @warning This function is intended for advanced users, since it lets you deal with UTF8 manually. */
		void UTF8Downgrade(); /**<  Downgrade the Perl string from UTF8 encoding. Calls the Perl sv_utf8_downgrade() function. @warning This function is intended for advanced users, since it lets you deal with UTF8 manually. */
		char* GetPV(); /**< @return the string value of the Perl scalar, as SvPV() Perl function returns. No conversion is made. @warning This function is intended for advanced users. Be careful of what you do with the returned pointer; a misuse will result in a crash. */

	protected:
		virtual void Create(CPerlInterpreter *pInterp=0, LPCTSTR szName=_T(""), void *pParam=0);
	};


	/** @class CPerlArray
	* Represents a Perl array.
	* @warning Although methods are optimized, array operations can be lengthy. Therefore, even if class usage is rather simple, be careful not making redundant code.
	*/
	class PXPERL_API CPerlArray : public CPerlVariable
	{
		friend class CPerlHash;

		// TODO : add     keys %hash = 200;

	public:
		CPerlArray(); /**< Constructs a CPerlArray object, marked as invalid, since not associated with a real Perl array. */
		/**
		* Constructs a CPerlArray object, cloning the passed CPerlArray object if this one is invalid, or copying its values if valid.
		* @param array CPerlArray object to be copied.
		* @see operator=()
		*/
		CPerlArray(const CPerlArray &array);
		~CPerlArray(); /**< Destroys the object. Nothing is done concerning the Perl array itself.  */

		/**
		* Clones the passed the passed CPerlArray object if this one is invalid, or copies its value if valid.
		* @param array CPerlArray object to be cloned/copied.
		* @return the CPerlArray object passed.
		*/
		const CPerlArray& operator= (const CPerlArray& array);

		/**
		* Populates a CStringArray with the Perl array values. Values can be appended to the existing CStringArray passed.
		* @param strARet CStringArray to receive the values.
		* @param bAppend true to append values to the passed array, false otherwise.
		* @return the CStringArray object passed.
		*/
		CStringArray& StringArray(CStringArray &strARet, bool bAppend=false) const;

		/**
		* Copies passed CStringArray values onto Perl array, overwriting any existing value, and resizing the Perl array as necessary.
		* @param array CStringArray to be copied.
		* @return the CStringArray object passed.
		*/
		const CStringArray& operator= (const CStringArray& array);

		/**
		* Appends the CPerlArray's associated array values to the current Perl array.
		* @param array CPerlArray to be appended.
		* @return the new index for the last array element, -1 on failure.
		*/
		int operator+= (const CPerlArray& array);
		/**
		* @see Append()
		*/
		int operator+= (const CStringArray& array);
		/**
		* @see Add()
		*/
		int operator+= (LPCTSTR element);

		/**
		* @see GetAt()
		*/
		CPerlScalar operator[](int nIndex);
		/**
		* Returns the value of element at the given index.
		* @warning If the result scalar is cloned, then modifying it will also modify the array element (interesting behaviour!). BUT, if the result scalar is copied, this will not modify the array. Example: "CPerlScalar newscalar = a.GetAt(0);" => you'll be able to modify the array element; "CPerlScalar s = interp.GetScalar(...); s = a.GetAt(0);" => you won't.
		* @param nIndex Item index.
		* @param bCanCreate Set to true if you allow creation of the scalar element of array if it does not exist. Otherwise the CPerlScalar object returned will be invalid.
		* @return a valid CPerlScalar object upon success.
		*/
		CPerlScalar GetAt(int nIndex, bool bCanCreate=false);

		/**
		* @return the size of the array, -1 on failure.
		*/
		int GetSize() const;
		/**
		* @see GetSize()
		*/
		int GetCount() const;
		/**
		* @return true if array is empty, false otherwise.
		*/
		bool IsEmpty() const;
		/**
		* @return the last element index.
		*/
		int GetUpperBound() const;
		/**
		* Extends the array to desired size. Use it prior to adding several elements using Add().
		* @param nNewSize New size. If nNewSize is smaller than array actual size, overheading elements will be lost.
		*/
		void SetSize(int nNewSize);
		/**
		* Clears the array. Does not free memory.
		* @see undef()
		*/
		void RemoveAll();

		void SetAt(int nIndex, LPCTSTR newElement);
		//const CString& ElementAt(int nIndex) const;
		void SetAtGrow(int nIndex, LPCTSTR newElement);

		/**
		* Appends a single item to the current Perl array.
		* @param newElement String to be appended.
		* @return the new index for the last array element.
		*/
		int Add(LPCTSTR newElement);
		int Add(const CString& newElement);

		/**
		* Appends the CStringArray values to the current Perl array.
		* @param newArray CStringArray to be appended.
		* @return the new index for the last array element, -1 on failure.
		*/
		int Append(const CStringArray& newArray);
		/**
		* Appends the CPerlArray values to the current Perl array.
		* @param newArray CPerlArray to be appended.
		* @return the new index for the last array element, -1 on failure.
		*/
		int Append(const CPerlArray& newArray);

		/**
		* Copies the CStringArray values onto the current Perl array.
		* @param newArray CStringArray to be copied.
		*/
		void Copy(const CStringArray& newArray);

		/**
		* Pops a specified number of elements from the array. That is, remove the nCount last elements from the array.
		* @param nCount number of elements to pop.
		* @return the last pop'ed element, a valid CPerlScalar upon success.
		*/
		CPerlScalar pop(int nCount=1);

		/**
		* Pushes several elements to the array. That is, appends several elements to the array.
		* @param szFirst First element to push.
		* @param nCount Number of other elements to push (number of vararg arguments).
		* @param ... Other elements to be pushed.
		* @warning vararg arguments must be valid LPCTSTR pointers, otherwise your application may crash.
		* @return the index of the last array element, -1 on failure.
		* @see Add()
		*/
		int push(LPCTSTR szFirst, int nCount=0, ...);

		//int push(const CStringArray& array);
		//int push(const CPerlArray& array);

		/**
		* Shifts several elements from the array. That is, remove several elements from the array head.
		* @param nCount Number of elements to shift.
		* @return last element shifted, a valid CPerlScalar upon success.
		*/
		CPerlScalar shift(int nCount=1); // returns last unshift-ed

		/**
		* Unshifts several elements to the array. That is, add several elements at the head of the array.
		* @param szFirst First element to unshift.
		* @param nCount Number of other elements to unshift (number of vararg arguments).
		* @param ... Other elements to be unshifted.
		* @warning vararg arguments must be valid LPCTSTR pointers, otherwise your application may crash.
		* @return the index of the last array element, -1 on failure.
		*/
		int unshift(LPCTSTR szFirst, int nCount=0, ...);
		/**
		* Unshifts several elements to the array.
		* @param array CStringArray of elements to unshift.
		* @return the index of the last array element, -1 on failure.
		*/
		int unshift(const CStringArray& array);
		/**
		* Unshifts several elements to the array.
		* @param array CPerlArray of elements to unshift.
		* @return the index of the last array element, -1 on failure.
		*/
		int unshift(const CPerlArray& array);

		/**
		* Pushes, in reverse order, several elements to the array.
		* @param array CStringArray of elements to unshift.
		* @return the index of the last array element, -1 on failure.
		*/
		int reverse_push(const CStringArray& array);
		/**
		* Pushes, in reverse order, several elements to the array.
		* @param array CPerlArray of elements to unshift.
		* @return the index of the last array element, -1 on failure.
		*/
		int reverse_push(const CPerlArray& array);
		/**
		* Unshifts, in reverse order, several elements to the array.
		* @param array CStringArray of elements to unshift.
		* @return the index of the last array element, -1 on failure.
		*/
		int reverse_unshift(const CStringArray& array);
		/**
		* Unshifts, in reverse order, several elements to the array.
		* @param array CPerlArray of elements to unshift.
		* @return the index of the last array element, -1 on failure.
		*/
		int reverse_unshift(const CPerlArray& array);

		void undef(void); /**< Removes all the elements and frees the memory asscoiated with them. */
		void clear(void); /**< Removes all the elements. */

		/**
		* The extra Perl-like function. However, there is no "sv_join" function exported by Perl. Therefore, calling this function is roughly the same as calling Eval("join...") in terms of performance but is more convenient.
		* @param szGlue The joining glue. it can be an existing scalar variable (eg join(_T("$myglue"))), or the glue string itself, but in this case don't forget the quotes: join(_T("'glue'")).
		* @return a valid CPerlScalar object upon success.
		* @see CPerlScalar::split()
		*/
		CPerlScalar join(LPCTSTR szGlue);
	};


	/** @class CPerlHash
	* Represents a Perl hash.
	*/
	class PXPERL_API CPerlHash : public CPerlVariable
	{
	public:
		CPerlHash(); /**< Constructs a CPerlHash object, marked as invalid, since not associated with a real Perl hash. */

		/**
		* Populates a CMapStringToString with the Perl hash keys. Keys can be appended to the existing CMapStringToString passed.
		* @param mapRet CMapStringToString to receive the keys.
		* @param bAppend true to append values to the passed hash, false otherwise.
		* @return the CMapStringToString object passed.
		*/
		CMapStringToString& MapStringToString(CMapStringToString &mapRet, bool bAppend=false);

		/**
		* Copies passed CStringArray keys onto Perl hash, overwriting any existing key.
		* @param map CMapStringToString to be copied.
		* @return the CMapStringToString object passed.
		*/
		const CMapStringToString& operator= (const CMapStringToString& map);

		/**
		* Clones the passed the passed CPerlHash object if this one is invalid, or copies its keys if valid.
		* @param hash CPerlHash object to be cloned/copied.
		* @return the CPerlHash object passed.
		*/
		const CPerlHash& operator= (const CPerlHash& hash);

		/**
		* @return the number of keys of the hash.
		*/
		int GetCount() const;
		/**
		* @return the number of keys of the hash.
		*/
		int GetSize() const;
		/**
		* @return true if the hash is empty, false otherwise.
		*/
		bool IsEmpty() const;

		/**
		* Look up for a key in the hash.
		* @param key The key to look up for.
		* @param rValue A CString to receive the value associated with the key. If key if not found, rValue is not modified.
		* @return true if the key was found, false otherwise.
		*/
		bool Lookup(LPCTSTR key, CString& rValue) const;

		/**
		* Look up for a key in the hash.
		* @param key The key to look up for.
		* @param bCanCreate true if you allow the key to be created if it does not exist. Otherwise the CPerlScalar object will be invalid.
		* @return a valid CPerlScalar upon success.
		* @see CPerlArray::GetAt() for the same remark about the CPerlScalar object returned.
		*/
		CPerlScalar Lookup(LPCTSTR key, bool bCanCreate=false);

		/**
		* The key won't be created if it does not exist.
		* @param key Key.
		* @see Lookup()
		*/
		CPerlScalar operator[](LPCTSTR key);

		/**
		* Adds (or modifies) a key to the hash.
		* @param key Key.
		* @param newValue Value.
		*/
		void SetAt(LPCTSTR key, LPCTSTR newValue);

		/**
		* Removes specified key.
		* @param key Key.
		* @return true if key is found and deleted successfuly, false otherwise.
		*/
		bool RemoveKey(LPCTSTR key);

		/**
		* Removes all keys from the hash.
		* @see clear()
		*/
		void RemoveAll();

		/**
		* Iterates through the hash the same way Perl's each function do.
		* Each call returns next key/value pair, in an unpredictable order, but always the same order.
		* @param strKey Next key encountered in the hash.
		* @param strValue Value associated with the key.
		* @return true as long as all the keys have not been enumerated, false on end.
		*/
		bool each(CString &strKey, CString &strValue);

		/**
		* @version 2.0.4.0
		*/
		bool each(CPerlScalar & key, CPerlScalar & value);

		/**
		* Returns the hash keys the same way Perl's keys function do.
		* @warning Calling this function resets the iterator, so next call to each() will return the first key/value pair.
		* @param strARet CStringArray to receive the keys.
		* @return the passed CStringArray.
		*/
		CStringArray& keys(CStringArray &strARet);

		/**
		* can't get it to work, on TODO list
		*/
		//CPerlArray keys();

		/**
		* Returns the hash values the same way Perl's values function do.
		* @warning Calling this function resets the iterator, so next call to each() will return the first key/value pair.
		* @param strARet CStringArray to receive the values.
		* @return the passed CStringArray.
		*/
		CStringArray& values(CStringArray &strARet);

		/**
		* can't get it to work, on TODO list
		*/
		//CPerlArray values();

		/**
		* Tells if a key exists.
		* @param key Key.
		* @return true if key exists, false otherwise.
		*/
		bool exists(LPCTSTR key) const;

		void undef(void); /**< Removes all the key/value pairs and frees the memory asscoiated with them. */
		void clear(void); /**< Removes all the key/value pairs. */

	protected:
		void *m_he;
	};

	/////////////////////////////////////////////////

}; //namespace PXPerlWrap

// EOF
