#ifndef INC_ParserSharedInputState_hpp__
#define INC_ParserSharedInputState_hpp__

#include "Antlr/config.hpp"
#include "Antlr/TokenBuffer.hpp"
#include "Antlr/RefCount.hpp"
#include <string>

/** This object contains the data associated with an
 *  input stream of tokens.  Multiple parsers
 *  share a single ParserSharedInputState to parse
 *  the same stream of tokens.
 */
class ParserInputState {
public:
	ParserInputState(TokenBuffer* input_);
	ParserInputState(TokenBuffer& input_);
	~ParserInputState();

public:
	/** Are we guessing (guessing>0)? */
	int guessing; //= 0;
	/** What file (if known) caused the problem? */
	std::string filename;
	TokenBuffer& getInput();
private:
	/** Where to get token objects */
	TokenBuffer* input;
	bool inputResponsible;

	// we don't want these:
	ParserInputState(const ParserInputState&);
	ParserInputState& operator=(const ParserInputState&);
};

typedef RefCount<ParserInputState> ParserSharedInputState;

#endif //INC_ParserSharedInputState_hpp__
