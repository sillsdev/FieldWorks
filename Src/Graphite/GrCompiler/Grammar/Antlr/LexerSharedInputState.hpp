#ifndef INC_LexerSharedInputState_hpp__
#define INC_LexerSharedInputState_hpp__

#include "Antlr/config.hpp"
#include "Antlr/InputBuffer.hpp"
#include "Antlr/RefCount.hpp"
#include <istream>
#include <string>

/** This object contains the data associated with an
 *  input stream of characters.  Multiple lexers
 *  share a single LexerSharedInputState to lex
 *  the same input stream.
 */
class LexerInputState {
public:
	LexerInputState(InputBuffer* inbuf);
	LexerInputState(InputBuffer& inbuf);
	LexerInputState(std::istream& in);
	~LexerInputState();

	int line;
	int guessing;
	/** What file (if known) caused the problem? */
	std::string filename;
	InputBuffer& getInput();
private:
	InputBuffer* input;
	bool inputResponsible;

	// we don't want these:
	LexerInputState(const LexerInputState&);
	LexerInputState& operator=(const LexerInputState&);
};

typedef RefCount<LexerInputState> LexerSharedInputState;

#endif //INC_LexerSharedInputState_hpp__
