/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: MacroBase.cpp
Responsibility: Luke Ulrich
Last reviewed: never

Description:
	This class provides the base for file I/O specifically relating to view test scripts.
----------------------------------------------------------------------------------------------*/
#include "main.h"
#pragma hdrstop

#undef THIS_FILE
DEFINE_THIS_FILE

MacroBase::MacroBase()
{
}

MacroBase::~MacroBase()
{
	// Close any open file objects
	if (m_infile.is_open())
		m_infile.close();
	if (m_outfile.is_open())
		m_outfile.close();
}

bool MacroBase::SetMacroIn(StrAnsi staMcr)
{
	// Open file "staMcr" for reading unless no name given
	if (!strcmp(staMcr.Chars(), ""))
		return false;

	// Assign file to open to class member
	m_stafilein = staMcr;

	// If the file is already open, close it before opening it again
	if (m_infile.is_open())
		m_infile.close();
	// Now open the file
	m_infile.open(m_stafilein.Chars(), ios_base::in);
	return m_infile.is_open();
}
bool MacroBase::SetMacroOut(StrAnsi staMcr)
{
	// Same functionality as SetMacroIn, except used for writing to files
	if (!strcmp(staMcr.Chars(),""))
		return false;

	m_stafileout = staMcr;

	if (m_outfile.is_open())
		m_outfile.close();
	m_outfile.open(m_stafileout.Chars(), ios_base::out);
	return m_outfile.is_open();
}
void MacroBase::CloseMacroIn()
{
	m_infile.clear();
	m_infile.close();
}
void MacroBase::CloseMacroOut()
{
	m_outfile.clear();
	m_outfile.close();
}
// The parameter passed must be of the string class because istringstream
// is part of the string class hierarchy and uses the string functionality
// for manipulation
// To retreive a char pointer from the string class, use the c_str() function
void MacroBase::SetStringIn(string &str)
{
	m_instr.clear();
	m_instr.str(str);
}

void MacroBase::AddTstFunc(StrAnsi name, int ID)
{
	// If no valid file object
	if (!m_outfile.is_open())	return;
	// name is just for visual reference, ID actually identifies the function
	m_outfile << name.Chars() << " " << ID << " ";
}

void MacroBase::WriteRect(RECT rect)
{
	if (!m_outfile.is_open())	return;
	m_outfile << rect.left << " " << rect.top << " " << rect.right << " " << rect.bottom << " ";
}
void MacroBase::WriteAnsi(StrAnsi param)
{
	if (!m_outfile.is_open())	return;
	// All strings are terminated by the ALT-255 character. It is doubtful the user will type this
	// character and so terminate the actual log prematurely. Using this obscure character
	// safeguards against that
	m_outfile << param.Chars() << "_" << " ";
}
void MacroBase::WriteUni(StrUni param)
{
	if (!m_outfile.is_open())	return;
	// All unicode strings are first transformed into ansi strings and then written to file as
	// an ansi
	StrAnsi staAnsi = param.Chars();
	m_outfile << staAnsi.Chars() << "_" << " ";
}
void MacroBase::WriteInt(int ivar)
{
	if (!m_outfile.is_open())	return;
	m_outfile << ivar << " ";
}

void MacroBase::EndTstFunc()
{
	if (!m_outfile.is_open())	return;
	// endl signals that the function is over
	m_outfile << endl;
}

int MacroBase::GetTstFunc()
{
	if (!m_infile.is_open())	return NULL;
	// Eat up the function name - hopefully no person has a function name longer than 96 chars!
	char funcname[96];
	m_infile >> funcname;
	// Get the id number of the function call
	// This id allows the caller to know which kind and how many parameters to read
	return ReadInt();
}
RECT MacroBase::ReadRect()
{
	RECT rcRct = {0,0,0,0};
	if (!m_infile.is_open())	return rcRct;

	m_infile >> rcRct.left;
	m_infile >> rcRct.top;
	m_infile >> rcRct.right;
	m_infile >> rcRct.bottom;
	return rcRct;
}
StrAnsi MacroBase::ReadAnsi()
{
	if (!m_infile.is_open())	return NULL;

	// Read characaters (including spaces) until reach a ALT-255 character (assuming user will
	// not type this)
	string temp;
	char ch;

	// Eat up the extra space
	m_infile.get(ch);

	while(m_infile.get(ch))	{
		if (ch == '_')	// Special char that marks end of strings: ALT-255, not an underscore!!
			break;
		temp += ch;
	}
	return temp.c_str();
}
StrUni MacroBase::ReadUni()
{
	if (!m_infile.is_open())	return NULL;

	// Both Unicode and Ansi are written as Ansi to file. Convert to Unicode after retrieiving
	// ansi interpretation
	StrAnsi ans = ReadAnsi();
	StrUni stuUni = ans.Chars();
	return stuUni;
}
int MacroBase::ReadInt()
{
	if (!m_infile.is_open())	return NULL;

	int ivar = 0;
	m_infile >> ivar;
	return ivar;
}

// Here are the same read/write functions just specialized for incore formatting
int MacroBase::StrGetTstFunc()
{
	char funcname[96];
	m_instr >> funcname;

	return StrReadInt();
}
RECT MacroBase::StrReadRect()
{
	RECT rcRect;
	m_instr >> rcRect.left;
	m_instr >> rcRect.top;
	m_instr >> rcRect.right;
	m_instr >> rcRect.bottom;

	return rcRect;
}
StrAnsi MacroBase::StrReadAnsi()
{
	// Read characaters (including spaces) until reach a ALT-255 character (assuming user will
	// not type this)
	string temp;
	char ch;

	// Eat up the extra space
	m_instr.get(ch);

	while(m_instr.get(ch))	{
		if (ch == '_')	// Special char that marks end of strings: ALT-255, not an underscore!!
			break;
		temp += ch;
	}
	return temp.c_str();
}
StrUni MacroBase::StrReadUni()
{
	// Both Unicode and Ansi are written as Ansi to file. Convert to Unicode after retrieiving
	// ansi interpretation
	StrUni stuUni = StrReadAnsi().Chars();
	return stuUni;
}
int MacroBase::StrReadInt()
{
	int ivar = 0;
	m_instr >> ivar;
	return ivar;
}


bool MacroBase::OutfileIsOpen()
{
	return m_outfile.is_open();
}
bool MacroBase::InfileIsOpen()
{
	return m_infile.is_open();
}

bool MacroBase::GetLine(char *szFileLine)
{
	// This function retrieves an entire line up to 256 characters long
	// Useful in cases where displaying the entire function call and arguments is necessary
	if (!m_infile.is_open())	return false;

	char buf[256];

	// Get up to 256 characters or until a newline constant is reached
	bool result = m_infile.getline(buf, 256, '\n');
	strcpy(szFileLine, buf);
	return result;
}
void MacroBase::WriteLine(char *szFileLine)
{
	// Write an entire string to the file - note: no newline character is needed in the
	// call to this function
	if (!m_outfile.is_open())	return;
	m_outfile << szFileLine << endl;
}
