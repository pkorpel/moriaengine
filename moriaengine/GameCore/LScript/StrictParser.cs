/*
 * StrictParser.cs
 * 
 * THIS FILE HAS BEEN GENERATED AUTOMATICALLY. DO NOT EDIT!
 * 
 * This software is released under GNU public license. See details in the URL: http://www.gnu.org/copyleft/gpl.html
 */

using System.IO;
using PerCederberg.Grammatica.Parser;

namespace SteamEngine.LScript {

    /**
     * <remarks>A token stream parser.</remarks>
     */
    internal class StrictParser : RecursiveDescentParser {

        /**
         * <summary>An enumeration with the generated production node
         * identity constants.</summary>
         */
        private enum SynteticPatterns {
            SUBPRODUCTION_1 = 3001,
            SUBPRODUCTION_2 = 3002,
            SUBPRODUCTION_3 = 3003,
            SUBPRODUCTION_4 = 3004,
            SUBPRODUCTION_5 = 3005,
            SUBPRODUCTION_6 = 3006,
            SUBPRODUCTION_7 = 3007,
            SUBPRODUCTION_8 = 3008,
            SUBPRODUCTION_9 = 3009,
            SUBPRODUCTION_10 = 3010,
            SUBPRODUCTION_11 = 3011,
            SUBPRODUCTION_12 = 3012,
            SUBPRODUCTION_13 = 3013,
            SUBPRODUCTION_14 = 3014,
            SUBPRODUCTION_15 = 3015,
            SUBPRODUCTION_16 = 3016,
            SUBPRODUCTION_17 = 3017,
            SUBPRODUCTION_18 = 3018,
            SUBPRODUCTION_19 = 3019,
            SUBPRODUCTION_20 = 3020,
            SUBPRODUCTION_21 = 3021,
            SUBPRODUCTION_22 = 3022,
            SUBPRODUCTION_23 = 3023,
            SUBPRODUCTION_24 = 3024,
            SUBPRODUCTION_25 = 3025,
            SUBPRODUCTION_26 = 3026,
            SUBPRODUCTION_27 = 3027,
            SUBPRODUCTION_28 = 3028,
            SUBPRODUCTION_29 = 3029,
            SUBPRODUCTION_30 = 3030,
            SUBPRODUCTION_31 = 3031,
            SUBPRODUCTION_32 = 3032,
            SUBPRODUCTION_33 = 3033,
            SUBPRODUCTION_34 = 3034,
            SUBPRODUCTION_35 = 3035,
            SUBPRODUCTION_36 = 3036,
            SUBPRODUCTION_37 = 3037,
            SUBPRODUCTION_38 = 3038,
            SUBPRODUCTION_39 = 3039,
            SUBPRODUCTION_40 = 3040,
            SUBPRODUCTION_41 = 3041,
            SUBPRODUCTION_42 = 3042,
            SUBPRODUCTION_43 = 3043,
            SUBPRODUCTION_44 = 3044,
            SUBPRODUCTION_45 = 3045,
            SUBPRODUCTION_46 = 3046,
            SUBPRODUCTION_47 = 3047,
            SUBPRODUCTION_48 = 3048,
            SUBPRODUCTION_49 = 3049,
            SUBPRODUCTION_50 = 3050,
            SUBPRODUCTION_51 = 3051,
            SUBPRODUCTION_52 = 3052,
            SUBPRODUCTION_53 = 3053,
            SUBPRODUCTION_54 = 3054,
            SUBPRODUCTION_55 = 3055,
            SUBPRODUCTION_56 = 3056,
            SUBPRODUCTION_57 = 3057,
            SUBPRODUCTION_58 = 3058,
            SUBPRODUCTION_59 = 3059,
            SUBPRODUCTION_60 = 3060,
            SUBPRODUCTION_61 = 3061
        }

        /**
         * <summary>Creates a new parser.</summary>
         * 
         * <param name='input'>the input stream to read from</param>
         * 
         * <exception cref='ParserCreationException'>if the parser
         * couldn't be initialized correctly</exception>
         */
        public StrictParser(TextReader input)
            : base(new StrictTokenizer(input)) {
	        this.CreatePatterns();
        }

        /**
         * <summary>Creates a new parser.</summary>
         * 
         * <param name='input'>the input stream to read from</param>
         * 
         * <param name='analyzer'>the analyzer to parse with</param>
         * 
         * <exception cref='ParserCreationException'>if the parser
         * couldn't be initialized correctly</exception>
         */
        public StrictParser(TextReader input, Analyzer analyzer)
            : base(new StrictTokenizer(input), analyzer) {
	        this.CreatePatterns();
        }

        /**
         * <summary>Initializes the parser by creating all the production
         * patterns.</summary>
         * 
         * <exception cref='ParserCreationException'>if the parser
         * couldn't be initialized correctly</exception>
         */
        private void CreatePatterns() {
            ProductionPattern             pattern;
            ProductionPatternAlternative  alt;

            pattern = new ProductionPattern((int) StrictConstants.SCRIPT,
                                            "Script");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_1, 1, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.SCRIPT_LINE,
                                            "ScriptLine");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_2, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.IF_BLOCK,
                                            "IfBlock");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.IF_BEGIN, 1, 1);
            alt.AddProduction((int) StrictConstants.ELSE_IF_BLOCK, 0, -1);
            alt.AddProduction((int) StrictConstants.ELSE_BLOCK, 0, 1);
            alt.AddToken((int) StrictConstants.ENDIF, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.IF_BEGIN,
                                            "IfBegin");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.IF, 1, 1);
            alt.AddProduction((int) StrictConstants.CODE, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            alt.AddProduction((int) StrictConstants.SCRIPT, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.ELSE_IF_BLOCK,
                                            "ElseIfBlock");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ELSEIF, 1, 1);
            alt.AddProduction((int) StrictConstants.CODE, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            alt.AddProduction((int) StrictConstants.SCRIPT, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.ELSE_BLOCK,
                                            "ElseBlock");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ELSE, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            alt.AddProduction((int) StrictConstants.SCRIPT, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOREACH_BLOCK,
                                            "ForeachBlock");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.FOREACH, 1, 1);
            alt.AddProduction((int) StrictConstants.FOREACH_HEADER, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            alt.AddProduction((int) StrictConstants.SCRIPT, 0, 1);
            alt.AddToken((int) StrictConstants.ENDFOREACH, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOREACH_HEADER,
                                            "ForeachHeader");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.FOREACH_HEADER_CODE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.FOREACH_HEADER_IN_PARENS, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOREACH_HEADER_CODE,
                                            "ForeachHeaderCode");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.FOREACH_HEADER_LOCAL_NAME, 1, 1);
            alt.AddToken((int) StrictConstants.IN, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOREACH_HEADER_LOCAL_NAME,
                                            "ForeachHeaderLocalName");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_3, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_4, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_5, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOREACH_HEADER_IN_PARENS,
                                            "ForeachHeaderInParens");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.FOREACH_HEADER, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOR_BLOCK,
                                            "ForBlock");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.FOR, 1, 1);
            alt.AddProduction((int) StrictConstants.FOR_HEADER, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            alt.AddProduction((int) StrictConstants.SCRIPT, 0, 1);
            alt.AddToken((int) StrictConstants.ENDFOR, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOR_HEADER,
                                            "ForHeader");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.FOR_HEADER_CODE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.FOR_HEADER_IN_PARENS, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOR_HEADER_CODE,
                                            "ForHeaderCode");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.FOREACH_HEADER_LOCAL_NAME, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.FOR_HEADER_IN_PARENS,
                                            "ForHeaderInParens");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.FOR_HEADER, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.WHILE_BLOCK,
                                            "WhileBlock");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHILE, 1, 1);
            alt.AddProduction((int) StrictConstants.CODE, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            alt.AddProduction((int) StrictConstants.SCRIPT, 0, 1);
            alt.AddToken((int) StrictConstants.ENDWHILE, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.SWITCH_BLOCK,
                                            "SwitchBlock");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.SWITCH, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            alt.AddProduction((int) StrictConstants.CASE_BLOCK, 1, -1);
            alt.AddToken((int) StrictConstants.ENDSWITCH, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.CASE_BLOCK,
                                            "CaseBlock");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.CASE, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_6, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_7, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.CODE,
                                            "Code");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.CODE_BODY, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_8, 0, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.CODE_BODY,
                                            "CodeBody");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.UNARY_OPERATOR, 0, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_9, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.CODE_BODY_PARENS,
                                            "CodeBodyParens");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.CODE, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.SIMPLE_CODE,
                                            "SimpleCode");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE_BODY, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_10, 0, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.SIMPLE_CODE_BODY,
                                            "SimpleCodeBody");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.UNARY_OPERATOR, 0, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_11, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.SIMPLE_CODE_BODY_PARENS,
                                            "SimpleCodeBodyParens");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.EXPRESSION,
                                            "Expression");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.DOTTED_EXPRESSION_CHAIN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.EVAL_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.STRONG_EVAL_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.QUOTED_STRING, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.RANDOM_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.NUMBER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.CROSSHASH, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.AT_KEY, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.TIMER_KEY, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.ADD_TIMER_EXPRESSION,
                                            "AddTimerExpression");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ADDTIMER, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_15, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.ADD_TIMER_BODY,
                                            "AddTimerBody");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.TIMER_KEY, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_16, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_17, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.AT_KEY,
                                            "AtKey");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.TRIGGER_KEY, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.PLUGIN_KEY, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.TRIGGER_KEY,
                                            "TriggerKey");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.AT, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.PLUGIN_KEY,
                                            "PluginKey");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.AT, 1, 1);
            alt.AddToken((int) StrictConstants.AT, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.TIMER_KEY,
                                            "TimerKey");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_MOD, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.QUOTED_STRING,
                                            "QuotedString");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.QUOTE, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_18, 0, -1);
            alt.AddToken((int) StrictConstants.QUOTE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.ARGUMENT,
                                            "Argument");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_24, 1, 1);
            alt.AddProduction((int) StrictConstants.INDEXER, 0, -1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_28, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.ARGS_LIST,
                                            "ArgsList");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_29, 0, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.STRONG_EVAL_EXPRESSION,
                                            "StrongEvalExpression");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_COMP_SMA, 1, 1);
            alt.AddToken((int) StrictConstants.QUERYMARK, 1, 1);
            alt.AddProduction((int) StrictConstants.DOTTED_EXPRESSION_CHAIN, 1, 1);
            alt.AddToken((int) StrictConstants.QUERYMARK, 1, 1);
            alt.AddToken((int) StrictConstants.OP_COMP_GRE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.RANDOM_EXPRESSION,
                                            "RandomExpression");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_BRACE, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_30, 0, -1);
            alt.AddToken((int) StrictConstants.RIGHT_BRACE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.ARGS_SEPARATOR,
                                            "ArgsSeparator");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.COMMA, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.EVAL_EXPRESSION,
                                            "EvalExpression");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_COMP_SMA, 1, 1);
            alt.AddProduction((int) StrictConstants.DOTTED_EXPRESSION_CHAIN, 1, 1);
            alt.AddToken((int) StrictConstants.OP_COMP_GRE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.EVAL_WORD_EXPRESSION,
                                            "EvalWordExpression");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.EVAL, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.DOTTED_EXPRESSION_CHAIN,
                                            "DottedExpressionChain");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_31, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_33, 0, -1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_36, 0, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.SIMPLE_EXPRESSION,
                                            "SimpleExpression");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_39, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.TYPE_OF_EXPRESSION,
                                            "TypeOfExpression");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_TYPEOF, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_42, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_45, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_48, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.CALLER,
                                            "Caller");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_LIST, 0, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.INDEXER,
                                            "Indexer");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_BRACKET, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_BRACKET, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.ASSIGNER,
                                            "Assigner");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.WHITE_SPACE_ASSIGNER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.OPERATOR_ASSIGNER, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.WHITE_SPACE_ASSIGNER,
                                            "WhiteSpaceAssigner");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_LIST, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.OPERATOR_ASSIGNER,
                                            "OperatorAssigner");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ASIG_PLAIN, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_LIST, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.NUMBER,
                                            "Number");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.HEXNUMBER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.INTEGER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.FLOAT, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.VAR_EXPRESSION,
                                            "VarExpression");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.VAR_KEY, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_57, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.LOCAL_KEY,
                                            "LocalKey");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARG, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LOCAL, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.VAR_KEY,
                                            "VarKey");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.TAG, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.VAR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.LOCAL_KEY, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.BINARY_OPERATOR,
                                            "BinaryOperator");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_58, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.BINARY_OPERATORS,
                                            "BinaryOperators");
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_61, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.TWO_CHARS_BIN_OPERATOR,
                                            "TwoCharsBinOperator");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 0, 1);
            alt.AddProduction((int) StrictConstants.COMPAR_OPERATOR, 1, 1);
            alt.AddToken((int) StrictConstants.OP_ASIG_PLAIN, 0, 1);
            alt.AddToken((int) StrictConstants.WHITESPACE, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.UNARY_OPERATOR,
                                            "UnaryOperator");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_SCREAMER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_BITCOMPLEMENT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ADD, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_SUB, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) StrictConstants.COMPAR_OPERATOR,
                                            "ComparOperator");
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_COMP_NOTEQ, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_COMP_SMA, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_COMP_GRE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_1,
                                            "Subproduction1");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.WHILE_BLOCK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.IF_BLOCK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.FOREACH_BLOCK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.FOR_BLOCK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SWITCH_BLOCK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SCRIPT_LINE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_2,
                                            "Subproduction2");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.DOTTED_EXPRESSION_CHAIN, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_3,
                                            "Subproduction3");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.LOCAL_KEY, 1, 1);
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_4,
                                            "Subproduction4");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.LOCAL_KEY, 1, 1);
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_5,
                                            "Subproduction5");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.LOCAL_KEY, 1, 1);
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_6,
                                            "Subproduction6");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DEFAULT, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_7,
                                            "Subproduction7");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SCRIPT, 0, 1);
            alt.AddToken((int) StrictConstants.BREAK, 1, 1);
            alt.AddToken((int) StrictConstants.COMEOL, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_8,
                                            "Subproduction8");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.BINARY_OPERATORS, 1, 1);
            alt.AddProduction((int) StrictConstants.CODE_BODY, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_9,
                                            "Subproduction9");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.CODE_BODY_PARENS, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_10,
                                            "Subproduction10");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.BINARY_OPERATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE_BODY, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_11,
                                            "Subproduction11");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE_BODY_PARENS, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_12,
                                            "Subproduction12");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ASIG_PLAIN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_13,
                                            "Subproduction13");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_12, 1, 1);
            alt.AddProduction((int) StrictConstants.ADD_TIMER_BODY, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_14,
                                            "Subproduction14");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.ADD_TIMER_BODY, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_15,
                                            "Subproduction15");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_13, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_14, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_16,
                                            "Subproduction16");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.TRIGGER_KEY, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_17,
                                            "Subproduction17");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_LIST, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_18,
                                            "Subproduction18");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.QUERYMARK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGCHK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGTXT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGNUM, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGO, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGV, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGNN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGON, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGVN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OTHERSYMBOLS, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.COMMA, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ASIG_PLAIN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_SCREAMER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_BITCOMPLEMENT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ADD, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_SUB, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_MUL, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_DIV, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_INTDIV, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_MOD, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_BITAND, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_BITOR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_AND, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_OR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_IS, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_TYPEOF, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.CROSSHASH, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.AT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.COMPAR_OPERATOR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.INTEGER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.HEXNUMBER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.FLOAT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_BRACKET, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.RIGHT_BRACKET, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_BRACE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.RIGHT_BRACE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.EVAL_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.STRONG_EVAL_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.VAR_KEY, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.IF, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ELSE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ELSEIF, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ENDIF, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ADDTIMER, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DEFAULT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.FOREACH, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.IN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ENDFOREACH, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.FOR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ENDFOR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.SWITCH, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.CASE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ENDSWITCH, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.BREAK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ESCAPEDCHAR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHILE, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ENDWHILE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_19,
                                            "Subproduction19");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGCHK, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGTXT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGNUM, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGO, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGNN, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGON, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGVN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_20,
                                            "Subproduction20");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_BRACKET, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_BRACKET, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_21,
                                            "Subproduction21");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_22,
                                            "Subproduction22");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_20, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_21, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_23,
                                            "Subproduction23");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.ARGV, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_22, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_24,
                                            "Subproduction24");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_19, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_23, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_25,
                                            "Subproduction25");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ASIG_PLAIN, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_26,
                                            "Subproduction26");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_27,
                                            "Subproduction27");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_28,
                                            "Subproduction28");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_25, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_26, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_27, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_29,
                                            "Subproduction29");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_30,
                                            "Subproduction30");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddProduction((int) StrictConstants.ARGS_SEPARATOR, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_31,
                                            "Subproduction31");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.TYPE_OF_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SIMPLE_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.VAR_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.ADD_TIMER_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.EVAL_WORD_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.ARGUMENT, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_32,
                                            "Subproduction32");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.TYPE_OF_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.SIMPLE_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.VAR_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.ADD_TIMER_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.EVAL_WORD_EXPRESSION, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.ARGUMENT, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_33,
                                            "Subproduction33");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_32, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_34,
                                            "Subproduction34");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ADD, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_35,
                                            "Subproduction35");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_34, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_36,
                                            "Subproduction36");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_IS, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_35, 0, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_37,
                                            "Subproduction37");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.INDEXER, 1, -1);
            alt.AddProduction((int) StrictConstants.ASSIGNER, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_38,
                                            "Subproduction38");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.CALLER, 1, 1);
            alt.AddProduction((int) StrictConstants.INDEXER, 0, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_39,
                                            "Subproduction39");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_37, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_38, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.ASSIGNER, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_40,
                                            "Subproduction40");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ADD, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_41,
                                            "Subproduction41");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_40, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_42,
                                            "Subproduction42");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_41, 0, -1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_43,
                                            "Subproduction43");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ADD, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_44,
                                            "Subproduction44");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_43, 1, 1);
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_45,
                                            "Subproduction45");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_44, 0, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_46,
                                            "Subproduction46");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ADD, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_47,
                                            "Subproduction47");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_46, 1, 1);
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_48,
                                            "Subproduction48");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ASIG_PLAIN, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_47, 0, -1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_49,
                                            "Subproduction49");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.COMMA, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_50,
                                            "Subproduction50");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            alt.AddProduction((int) StrictConstants.INDEXER, 0, -1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_49, 0, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_51,
                                            "Subproduction51");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.DOT, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_52,
                                            "Subproduction52");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ASIG_PLAIN, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_53,
                                            "Subproduction53");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.LEFT_PAREN, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            alt.AddToken((int) StrictConstants.RIGHT_PAREN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_54,
                                            "Subproduction54");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 1, 1);
            alt.AddProduction((int) StrictConstants.SIMPLE_CODE, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_55,
                                            "Subproduction55");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_52, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_53, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_54, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_56,
                                            "Subproduction56");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_51, 1, 1);
            alt.AddToken((int) StrictConstants.STRING, 1, 1);
            alt.AddProduction((int) StrictConstants.INDEXER, 0, -1);
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_55, 0, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_57,
                                            "Subproduction57");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_50, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_56, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_58,
                                            "Subproduction58");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_ADD, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_SUB, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_MUL, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_DIV, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_INTDIV, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_MOD, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_BITAND, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_BITOR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_AND, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.OP_OR, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_59,
                                            "Subproduction59");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 0, 1);
            alt.AddToken((int) StrictConstants.OP_COMP_EQ, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_60,
                                            "Subproduction60");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddToken((int) StrictConstants.WHITESPACE, 0, 1);
            alt.AddToken((int) StrictConstants.OP_SCREAMER, 1, 1);
            alt.AddToken((int) StrictConstants.OP_ASIG_PLAIN, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);

            pattern = new ProductionPattern((int) SynteticPatterns.SUBPRODUCTION_61,
                                            "Subproduction61");
            pattern.SetSyntetic(true);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.BINARY_OPERATOR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) StrictConstants.TWO_CHARS_BIN_OPERATOR, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_59, 1, 1);
            pattern.AddAlternative(alt);
            alt = new ProductionPatternAlternative();
            alt.AddProduction((int) SynteticPatterns.SUBPRODUCTION_60, 1, 1);
            pattern.AddAlternative(alt);
	        this.AddPattern(pattern);
        }
    }
}
