/*
 * StrictTokenizer.cs
 * 
 * THIS FILE HAS BEEN GENERATED AUTOMATICALLY. DO NOT EDIT!
 * 
 * This software is released under GNU public license. See details in the URL: http://www.gnu.org/copyleft/gpl.html
 */

using System.IO;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {

    /**
     * <remarks>A character stream tokenizer.</remarks>
     */
    internal class StrictTokenizer : Tokenizer {

        /**
         * <summary>Creates a new tokenizer for the specified input
         * stream.</summary>
         * 
         * <param name='input'>the input stream to read</param>
         * 
         * <exception cref='ParserCreationException'>if the tokenizer
         * couldn't be initialized correctly</exception>
         */
        public StrictTokenizer(TextReader input)
            : base(input) {
	        this.CreatePatterns();
        }

        /**
         * <summary>Initializes the tokenizer by creating all the token
         * patterns.</summary>
         * 
         * <exception cref='ParserCreationException'>if the tokenizer
         * couldn't be initialized correctly</exception>
         */
        private void CreatePatterns() {
            TokenPattern  pattern;

            pattern = new TokenPattern((int) StrictConstants.IF,
                                       "IF",
                                       TokenPattern.PatternType.REGEXP,
                                       "[iI][fF][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ELSE,
                                       "ELSE",
                                       TokenPattern.PatternType.REGEXP,
                                       "[eE][lL][sS][eE]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ELSEIF,
                                       "ELSEIF",
                                       TokenPattern.PatternType.REGEXP,
                                       "[eE][lL][sS][eE][iI][fF][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ENDIF,
                                       "ENDIF",
                                       TokenPattern.PatternType.REGEXP,
                                       "[eE][nN][dD][iI][fF]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.WHILE,
                                       "WHILE",
                                       TokenPattern.PatternType.REGEXP,
                                       "[wW][hH][iI][lL][eE][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ENDWHILE,
                                       "ENDWHILE",
                                       TokenPattern.PatternType.REGEXP,
                                       "[eE][nN][dD][wW][hH][iI][lL][eE]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ADDTIMER,
                                       "ADDTIMER",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][dD][dD][tT][iI][mM][eE][rR]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.FOR,
                                       "FOR",
                                       TokenPattern.PatternType.REGEXP,
                                       "[fF][oO][rR][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ENDFOR,
                                       "ENDFOR",
                                       TokenPattern.PatternType.REGEXP,
                                       "[eE][nN][dD][fF][oO][rR]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.FOREACH,
                                       "FOREACH",
                                       TokenPattern.PatternType.REGEXP,
                                       "[fF][oO][rR][eE][aA][cC][hH][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.IN,
                                       "IN",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*[iI][nN][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ENDFOREACH,
                                       "ENDFOREACH",
                                       TokenPattern.PatternType.REGEXP,
                                       "[eE][nN][dD][fF][oO][rR][eE][aA][cC][hH]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.SWITCH,
                                       "SWITCH",
                                       TokenPattern.PatternType.REGEXP,
                                       "[sS][wW][iI][tT][cC][hH][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.CASE,
                                       "CASE",
                                       TokenPattern.PatternType.REGEXP,
                                       "[cC][aA][sS][eE][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ENDSWITCH,
                                       "ENDSWITCH",
                                       TokenPattern.PatternType.REGEXP,
                                       "[eE][nN][dD][sS][wW][iI][tT][cC][hH]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.BREAK,
                                       "BREAK",
                                       TokenPattern.PatternType.REGEXP,
                                       "[bB][rR][eE][aA][kK]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.DEFAULT,
                                       "DEFAULT",
                                       TokenPattern.PatternType.REGEXP,
                                       "[dD][eE][fF][aA][uU][lL][tT]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGCHK,
                                       "ARGCHK",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][cC][hH][kK]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGTXT,
                                       "ARGTXT",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][tT][xX][tT]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGNUM,
                                       "ARGNUM",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][nN][uU][mM]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGO,
                                       "ARGO",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][oO]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGN,
                                       "ARGN",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][nN]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGV,
                                       "ARGV",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][vV]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGON,
                                       "ARGON",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][oO][0-9]+");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGNN,
                                       "ARGNN",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][nN][0-9]+");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARGVN,
                                       "ARGVN",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG][vV][0-9]+");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.TAG,
                                       "TAG",
                                       TokenPattern.PatternType.REGEXP,
                                       "[tT][aA][gG]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ARG,
                                       "ARG",
                                       TokenPattern.PatternType.REGEXP,
                                       "[aA][rR][gG]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.VAR,
                                       "VAR",
                                       TokenPattern.PatternType.REGEXP,
                                       "[vV][aA][rR]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.LOCAL,
                                       "LOCAL",
                                       TokenPattern.PatternType.REGEXP,
                                       "[lL][oO][cC][aA][lL]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.EVAL,
                                       "EVAL",
                                       TokenPattern.PatternType.REGEXP,
                                       "[eE][vV][aA][lL][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_ADD,
                                       "OP_ADD",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*?\\+[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_SUB,
                                       "OP_SUB",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*?\\-[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_MUL,
                                       "OP_MUL",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*?\\*[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_DIV,
                                       "OP_DIV",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*?/[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_INTDIV,
                                       "OP_INTDIV",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*?[dD][iI][vV][ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_MOD,
                                       "OP_MOD",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*?%[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_IS,
                                       "OP_IS",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*?[iI][sS][ \\t]+");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_TYPEOF,
                                       "OP_TYPEOF",
                                       TokenPattern.PatternType.REGEXP,
                                       "[tT][yY][pP][eE][oO][fF]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_SCREAMER,
                                       "OP_SCREAMER",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*![ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_BITAND,
                                       "OP_BITAND",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*&[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_BITCOMPLEMENT,
                                       "OP_BITCOMPLEMENT",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*~[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_BITOR,
                                       "OP_BITOR",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*\\|[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_AND,
                                       "OP_AND",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*&&[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_OR,
                                       "OP_OR",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*\\|\\|[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.DOT,
                                       "DOT",
                                       TokenPattern.PatternType.STRING,
                                       ".");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.COMMA,
                                       "COMMA",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*,[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_ASIG_PLAIN,
                                       "OP_ASIG_PLAIN",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*=[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_COMP_GRE,
                                       "OP_COMP_GRE",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*>");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_COMP_SMA,
                                       "OP_COMP_SMA",
                                       TokenPattern.PatternType.REGEXP,
                                       "<[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_COMP_NOTEQ,
                                       "OP_COMP_NOTEQ",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*!=[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OP_COMP_EQ,
                                       "OP_COMP_EQ",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*==[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.LEFT_PAREN,
                                       "LEFT_PAREN",
                                       TokenPattern.PatternType.REGEXP,
                                       "\\([ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.RIGHT_PAREN,
                                       "RIGHT_PAREN",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*\\)");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.LEFT_BRACKET,
                                       "LEFT_BRACKET",
                                       TokenPattern.PatternType.REGEXP,
                                       "\\[[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.RIGHT_BRACKET,
                                       "RIGHT_BRACKET",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*\\]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.LEFT_BRACE,
                                       "LEFT_BRACE",
                                       TokenPattern.PatternType.REGEXP,
                                       "\\{[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.RIGHT_BRACE,
                                       "RIGHT_BRACE",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*\\}");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.QUOTE,
                                       "QUOTE",
                                       TokenPattern.PatternType.REGEXP,
                                       "\\\"");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.HEXNUMBER,
                                       "HEXNUMBER",
                                       TokenPattern.PatternType.REGEXP,
                                       "0[xX]?[0-9a-fA-F]+");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.INTEGER,
                                       "INTEGER",
                                       TokenPattern.PatternType.REGEXP,
                                       "[0-9]+");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.FLOAT,
                                       "FLOAT",
                                       TokenPattern.PatternType.REGEXP,
                                       "[0-9]*\\.[0-9]+");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.STRING,
                                       "STRING",
                                       TokenPattern.PatternType.REGEXP,
                                       "[A-Za-z0-9_]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.WHITESPACE,
                                       "WHITESPACE",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]+");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.CROSSHASH,
                                       "CROSSHASH",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*#[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.AT,
                                       "AT",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*@[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.QUERYMARK,
                                       "QUERYMARK",
                                       TokenPattern.PatternType.REGEXP,
                                       "[ \\t]*\\?[ \\t]*");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.ESCAPEDCHAR,
                                       "ESCAPEDCHAR",
                                       TokenPattern.PatternType.REGEXP,
                                       "\\\\.");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.OTHERSYMBOLS,
                                       "OTHERSYMBOLS",
                                       TokenPattern.PatternType.REGEXP,
                                       "[^a-zA-Z_0-9]");
	        this.AddPattern(pattern);

            pattern = new TokenPattern((int) StrictConstants.COMEOL,
                                       "COMEOL",
                                       TokenPattern.PatternType.REGEXP,
                                       "([ \\t]*;[ \\t]*(//.*)?[\\n\\r]*)|([ \\t]*(//.*)?[\\n\\r]+)");
	        this.AddPattern(pattern);
        }
    }
}
