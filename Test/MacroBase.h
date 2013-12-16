/*----------------------------------------------------------------------------------------------
Copyright (c) 2000-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: MacroBase.h
Responsibility: Luke Ulrich
Last reviewed: Not yet.

Description: A utility file designed to wrap the process of reading and writing to files
	especially in conjunction with executing view test scripts

	How the reading and writing of functions works:
	Each function and its respective arguments will take up a single line in the test script.
	A new line indicated that a new function has been called.
	AddTstFunc - adds a string name and unique (user controlled uniqueness) function ID to the
				beginning of a line
	Write(option) - appends a variable to the end of caret in character format. Each variable
					is separated by a space and strings are terminated by an ALT-255 character
					which looks like an underscore - but is not: _
	EndTstFunc - appends a newline character to signal that the function call is over.

	GetTstFunc - returns the unique function ID while ignoring the string function name. NOTE:
				 the function name string is given only for reference
	Read(option) - takes the first argument
	**Based on the function ID returned to GetTstFunc, the user should read the correct amount
	  of arguments and then call GetTstFunc again. If the user reads an incorrect amount of
	  times, all subsequent file input will be corrupted. GetTstFunc will return 0 when there
	  is nothing else to read

	In addition to reading and writing files, incore formatting has been added so that string
	buffers can be parsed and executed. This facilitates executing single lines of text from
	controls such as listboxes. The concept is the same as described above, but the methods
	are prefixed by "Str". Accomplishing this task is extremely simplifed by the istringstream
	class. This class has the << operator overloaded which assists in formatting variables
	from a string.

----------------------------------------------------------------------------------------------*/

#pragma once
#ifndef MACROBASE_INCLUDED
#define MACROBASE_INCLUDED

/*----------------------------------------------------------------------------------------------
Class: MacroBase
Description:
Hungarian:
----------------------------------------------------------------------------------------------*/
// This is for ifstream, ofstream file I/O
using namespace std;
class MacroBase
{
private:
	// File I/O objects to read test scripts
	ifstream m_infile;
	ofstream m_outfile;
	// String reading object
	istringstream m_instr;

	// Filenames of infile and outfile
	StrAnsi m_stafilein, m_stafileout;
protected:
public:
	MacroBase();
	~MacroBase();

	bool SetMacroIn(StrAnsi strName);
	bool SetMacroOut(StrAnsi strName);
	void CloseMacroIn();
	void CloseMacroOut();
	// This initializes the istringstream object
	void SetStringIn(string &str);

	// Writing information to file
	void AddTstFunc(StrAnsi name, int ID);
	void WriteRect(RECT rect);
	void WriteAnsi(StrAnsi param);
	void WriteUni(StrUni param);
	void WriteInt(int ivar);
	void EndTstFunc();

	// Reading macro from file
	int GetTstFunc();
	RECT ReadRect();
	StrAnsi ReadAnsi();
	StrUni ReadUni();
	int ReadInt();

	// Reading macro line from string - using istringstream
	int StrGetTstFunc();
	RECT StrReadRect();
	StrAnsi StrReadAnsi();
	StrUni StrReadUni();
	int StrReadInt();

	// Utility functions
	bool OutfileIsOpen();
	bool InfileIsOpen();
	bool GetLine(char * szFileLine);
	void WriteLine(char * szFileLine);
};

#endif