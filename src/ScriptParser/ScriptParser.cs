using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace ShenGu.Script
{
    public partial class ScriptParser
    {
        #region 变量

        #region 静态变量
        private readonly static int[] CharFlags, SingleFlags;
        private readonly static short[] StatusFlags;
        private readonly static char[] EscapeChars;
        private const char InvalidChar = (char)0x100;
        private const int CharFlagCount = 26, StatusFlagCount = 34;
        private readonly static int[] LexicalFlags = new int[] {
            FG_NONE, FG_Numeber64, FG_Decimal, FG_Number, FG_Add, FG_Increment, FG_AddAssign, FG_Substract, FG_Decrement, FG_SubstractAssign,
            FG_Multiply, FG_MultiplyAssign, FG_Divide, FG_DivideAssign, FG_Comment, FG_MultiComment, FG_Modulus, FG_ModulusAssign, FG_LogicNot, FG_NotEqualValue,
            FG_NotEqual, FG_Assign, FG_EqualValue, FG_Equal, FG_Less, FG_LessEqual, FG_ShiftLeft, FG_ShiftLeftAssign, FG_Greater, FG_GreaterEqual,
            FG_ShiftRight, FG_ShiftRightAssign, FG_UnsignedShiftRight, FG_UnsignedShiftRightAssign, FG_BitAnd, FG_BitAndAssign, FG_LogicAnd, FG_BitOr, FG_BitOrAssign, FG_LogicOr, 
            FG_BitXOr, FG_BitXOrAssign, FG_EscapeString, FG_String, FG_KeyVariable, FG_Variable, FG_SingleChar
        };
        private readonly static KeywordFlag[] Keywords = new KeywordFlag[] {
            new KeywordFlag("instanceof", OPT_InstanceOf), new KeywordFlag("delete", OPT_Delete), new KeywordFlag("new", OPT_New), new KeywordFlag("typeof", OPT_Typeof)
            ,new KeywordFlag("undefined", CT_Undefined),new KeywordFlag("null", CT_Null),new KeywordFlag("true", CT_True),new KeywordFlag("false", CT_False)
            ,new KeywordFlag("Infinity", CT_Infinity),new KeywordFlag("NaN", CT_NaN),new KeywordFlag("function", CT_Function),new KeywordFlag("this", CT_This)
            ,new KeywordFlag("var", LG_VarDefine),new KeywordFlag("if", LG_If),new KeywordFlag("else", LG_Else),new KeywordFlag("for", LG_For),new KeywordFlag("in", OPT_In), new KeywordFlag("of", LG_Of)
            ,new KeywordFlag("while", LG_While),new KeywordFlag("do", LG_Do),new KeywordFlag("switch", LG_Switch),new KeywordFlag("case", LG_Case),new KeywordFlag("default", LG_Default)
            ,new KeywordFlag("continue", LG_Continue),new KeywordFlag("break", LG_Break),new KeywordFlag("try", LG_Try),new KeywordFlag("catch", LG_Catch),new KeywordFlag("finally", LG_Finally)
            ,new KeywordFlag("throw", LG_Throw),new KeywordFlag("with", LG_With),new KeywordFlag("return", LG_Return), new KeywordFlag("debugger", LG_Debugger)
        };
        private const int OPD_None = 0, OPD_CONST = 1, OPD_VARIABLE = 2;
        private readonly static int[] OptLevels = new int[] {0
            , 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
            , 2, 3, 4, 5, 6
            , 7, 7, 7, 7
            , 8, 8, 8, 8, 8, 8
            , 9, 9, 9
            , 10, 10
            , 11, 11, 11
            , 12, 12, 12, 12, 12, 12, 12, 12
            , 13, 13, 13
        };

        #region 关键字

        #region 元素类型
        private const int OPT_Start = 0;
        private const int OPT_Assign = OPT_Start + (int)OperatorType.Assign;
        private const int OPT_In = OPT_Start + (int)OperatorType.In;
        private const int OPT_BitOrAssign = OPT_Start + (int)OperatorType.BitOrAssign;
        private const int OPT_BitXOrAssign = OPT_Start + (int)OperatorType.BitXOrAssign;
        private const int OPT_BitAndAssign = OPT_Start + (int)OperatorType.BitAndAssign;
        private const int OPT_AddAssign = OPT_Start + (int)OperatorType.AddAssign;
        private const int OPT_SubstractAssign = OPT_Start + (int)OperatorType.SubstractAssign;
        private const int OPT_MultiplyAssign = OPT_Start + (int)OperatorType.MultiplyAssign;
        private const int OPT_DivideAssign = OPT_Start + (int)OperatorType.DivideAssign;
        private const int OPT_ModulusAssign = OPT_Start + (int)OperatorType.ModulusAssign;
        private const int OPT_ShiftLeftAssign = OPT_Start + (int)OperatorType.ShiftLeftAssign;
        private const int OPT_ShiftRightAssign = OPT_Start + (int)OperatorType.ShiftRightAssign;
        private const int OPT_UnsignedShiftRightAssign = OPT_Start + (int)OperatorType.UnsignedShiftRightAssign;
        private const int OPT_LogicOr = OPT_Start + (int)OperatorType.LogicOr;
        private const int OPT_LogicAnd = OPT_Start + (int)OperatorType.LogicAnd;
        private const int OPT_BitOr = OPT_Start + (int)OperatorType.BitOr;
        private const int OPT_BitXor = OPT_Start + (int)OperatorType.BitXOr;
        private const int OPT_BitAnd = OPT_Start + (int)OperatorType.BitAnd;
        private const int OPT_EqualsValue = OPT_Start + (int)OperatorType.EqualsValue;
        private const int OPT_NotEqualsValue = OPT_Start + (int)OperatorType.NotEqualsValue;
        private const int OPT_Equals = OPT_Start + (int)OperatorType.Equals;
        private const int OPT_NotEquals = OPT_Start + (int)OperatorType.NotEquals;
        private const int OPT_Less = OPT_Start + (int)OperatorType.Less;
        private const int OPT_LessEquals = OPT_Start + (int)OperatorType.LessEquals;
        private const int OPT_Greater = OPT_Start + (int)OperatorType.Greater;
        private const int OPT_GreaterEquals = OPT_Start + (int)OperatorType.GreaterEquals;
        private const int OPT_InstanceOf = OPT_Start + (int)OperatorType.InstanceOf;
        private const int OPT_ShiftLeft = OPT_Start + (int)OperatorType.ShiftLeft;
        private const int OPT_ShiftRight = OPT_Start + (int)OperatorType.ShiftRight;
        private const int OPT_UnsignedShiftRight = OPT_Start + (int)OperatorType.UnsignedShiftRight;
        private const int OPT_Add = OPT_Start + (int)OperatorType.Add;
        private const int OPT_Substract = OPT_Start + (int)OperatorType.Substract;
        private const int OPT_Multiply = OPT_Start + (int)OperatorType.Multiply;
        private const int OPT_Divide = OPT_Start + (int)OperatorType.Divide;
        private const int OPT_Modulus = OPT_Start + (int)OperatorType.Modulus;
        private const int OPT_Increment = OPT_Start + (int)OperatorType.Increment;
        private const int OPT_Decrement = OPT_Start + (int)OperatorType.Decrement;
        private const int OPT_Negative = OPT_Start + (int)OperatorType.Negative;
        private const int OPT_BitNot = OPT_Start + (int)OperatorType.BitNot;
        private const int OPT_LogicNot = OPT_Start + (int)OperatorType.LogicNot;
        private const int OPT_Delete = OPT_Start + (int)OperatorType.Delete;
        private const int OPT_New = OPT_Start + (int)OperatorType.New;
        private const int OPT_Typeof = OPT_Start + (int)OperatorType.Typeof;
        private const int OPT_GetObjectMember = OPT_Start + (int)OperatorType.GetObjectMember;
        private const int OPT_GetArrayMember = OPT_Start + (int)OperatorType.GetArrayMember;
        private const int OPT_InvokeMethod = OPT_Start + (int)OperatorType.InvokeMethod;
        private const int OPT_End = OPT_InvokeMethod;

        private const int CT_Start = OPT_End + 1;
        private const int CT_Number64 = CT_Start;
        private const int CT_Decimal = CT_Start + 1;
        private const int CT_Number = CT_Start + 2;
        private const int CT_String = CT_Start + 3;
        private const int CT_EscapeString = CT_Start + 4;
        private const int CT_Undefined = CT_Start + 5;
        private const int CT_Null = CT_Start + 6;
        private const int CT_True = CT_Start + 7;
        private const int CT_False = CT_Start + 8;
        private const int CT_NaN = CT_Start + 9;
        private const int CT_Infinity = CT_Start + 10;
        private const int CT_Variable = CT_Start + 11;
        private const int CT_This = CT_Start + 12;
        private const int CT_RegExpr = CT_Start + 13;
        private const int CT_Function = CT_Start + 14;
        private const int CT_End = CT_Function;

        private const int LG_Start = CT_End + 1;
        private const int LG_VarDefine = LG_Start;
        private const int LG_If = LG_Start + 1;
        private const int LG_Else = LG_Start + 2;
        private const int LG_For = LG_Start + 3;
        private const int LG_While = LG_Start + 4;
        private const int LG_Do = LG_Start + 5;
        private const int LG_Switch = LG_Start + 6;
        private const int LG_Case = LG_Start + 7;
        private const int LG_Default = LG_Start + 8;
        private const int LG_Continue = LG_Start + 9;
        private const int LG_Break = LG_Start + 10;
        private const int LG_Try = LG_Start + 11;
        private const int LG_Catch = LG_Start + 12;
        private const int LG_Finally = LG_Start + 13;
        private const int LG_Throw = LG_Start + 14;
        private const int LG_With = LG_Start + 15;
        private const int LG_Return = LG_Start + 16;
        private const int LG_Of = LG_Start + 17;
        private const int LG_Debugger = LG_Start + 18;
        private const int LG_End = LG_Debugger;

        private const int SC_Start = LG_End + 1;
        private const int SC_Condition = SC_Start;      // ?
        private const int SC_Condition2 = SC_Start + 1; // :
        private const int SC_LeftBracket1 = SC_Start + 2; // (
        private const int SC_RightBracket1 = SC_Start + 3; // )
        private const int SC_LeftBracket2 = SC_Start + 4; // [
        private const int SC_RightBracket2 = SC_Start + 5; // ]
        private const int SC_LeftBracket3 = SC_Start + 6; // {
        private const int SC_RightBracket3 = SC_Start + 7; // }
        private const int SC_Comma = SC_Start + 8; // ,
        private const int SC_Semicolon = SC_Start + 9; // ;
        private const int SC_NewLine = SC_Start + 10; //换行
        private const int SC_End = SC_NewLine;

        private const int MaxElem = SC_End + 1;
        #endregion

        #region ParsingExprCache.ParsingFlag的标识

        private const int PF_Object = MaxElem + 1;
        private const int PF_Array = MaxElem + 2;
        private const int PF_InvokeMethod = MaxElem + 3;
        private const int PF_Var = LG_VarDefine;
        private const int PF_If = LG_If;
        private const int PF_For = LG_For;
        private const int PF_While = LG_While;
        private const int PF_Switch = LG_Switch;

        #endregion

        #region 解析后的标识
        private const int FG_NONE = 0;
        private const int FG_Numeber64 = CT_Number64;   //16进制整数
        private const int FG_Decimal = CT_Decimal;  //小数
        private const int FG_Number = CT_Number;    //整数
        private const int FG_Add = OPT_Add; // +
        private const int FG_Increment = OPT_Increment; // ++
        private const int FG_AddAssign = OPT_AddAssign; // +=
        private const int FG_Substract = OPT_Substract; // -
        private const int FG_Decrement = OPT_Decrement; // --
        private const int FG_SubstractAssign = OPT_SubstractAssign; // -=

        private const int FG_Multiply = OPT_Multiply;   // *
        private const int FG_MultiplyAssign = OPT_MultiplyAssign; // *=
        private const int FG_Divide = OPT_Divide;   // /
        private const int FG_DivideAssign = OPT_DivideAssign; // /=
        private const int FG_Comment = MaxElem + 1;  // //
        private const int FG_MultiComment = MaxElem + 2; // /*
        private const int FG_Modulus = OPT_Modulus;  // %
        private const int FG_ModulusAssign = OPT_ModulusAssign; // %=
        private const int FG_LogicNot = OPT_LogicNot; // !
        private const int FG_NotEqualValue = OPT_NotEqualsValue; // !==

        private const int FG_NotEqual = OPT_NotEquals; // !===
        private const int FG_Assign = OPT_Assign; // =
        private const int FG_EqualValue = OPT_EqualsValue; // ==
        private const int FG_Equal = OPT_Equals; // ===
        private const int FG_Less = OPT_Less; // <
        private const int FG_LessEqual = OPT_LessEquals; // <=
        private const int FG_ShiftLeft = OPT_ShiftLeft; // <<
        private const int FG_ShiftLeftAssign = OPT_ShiftLeftAssign; // <<=
        private const int FG_Greater = OPT_Greater; // >
        private const int FG_GreaterEqual = OPT_GreaterEquals; // >=

        private const int FG_ShiftRight = OPT_ShiftRight; // >>
        private const int FG_ShiftRightAssign = OPT_ShiftRightAssign; // >>=
        private const int FG_UnsignedShiftRight = OPT_UnsignedShiftRight; // >>>
        private const int FG_UnsignedShiftRightAssign = OPT_UnsignedShiftRightAssign; // >>>=
        private const int FG_BitAnd = OPT_BitAnd; // &
        private const int FG_BitAndAssign = OPT_BitAndAssign; // &=
        private const int FG_LogicAnd = OPT_LogicAnd; // &&
        private const int FG_BitOr = OPT_BitOr; // |
        private const int FG_BitOrAssign = OPT_BitOrAssign; // |=
        private const int FG_LogicOr = OPT_LogicOr; // ||

        private const int FG_BitXOr = OPT_BitXor; // ^
        private const int FG_BitXOrAssign = OPT_BitXOrAssign; // ^=
        private const int FG_EscapeString = CT_EscapeString; //带转义符的字符串
        private const int FG_String = CT_String;   //字符串
        private const int FG_KeyVariable = MaxElem + 3; // 可能是关键字的变量
        private const int FG_Variable = CT_Variable; //普通变量
        private const int FG_SingleChar = MaxElem + 4;  //单字符
        #endregion

        #region 表达式的结果

        private const int ER_Error = 0; //异常结束
        private const int ER_None = 0;      //读取到内容的结束
        private const int ER_Continue = 1;  //继续解析表达式
        private const int ER_Operand = 2;//操作元结束
        private const int ER_FF = 0xFFF;    //结束符部分
        private const int ER_Expr = 0x1000;  //表达式（可能是子表达式）结束
        private const int ER_EndChar = 0x2000;  //单纯的结束符号
        private const int ER_Logic = 0x4000;    //逻辑语法结束

        private const int EF_CheckLastVisitState = 0x10;    //检查最后访问状态
        private const int EF_EndCharFlag = 0x3;             //结束符的处理
        private const int EF_NormalEndChar = 0x0;           //只带普通的表达式结束符，如：,、;、换行符
        private const int EF_AttachEndChar = 0x1;           //带所有的表达式结束符，包括：)、]、}、：
        private const int EF_IgnoreEndChar = 0x2;           //不事任何的表达式结束符

        #endregion

        #region 解析表达式时的步骤

        private const int STP_First = 0;            //解析第一个元素
        private const int STP_ParseOperand = 1;     //将要解析操作元
        private const int STP_ParseOperator = 2;    //将要解析操作符
        private const int STP_ParseState = 7;       //解析过程中
        private const int STP_ReadOperand = 8;      //在表达式外部解析到操作元

        #endregion

        #endregion

        #endregion

        #region 普通变量

        private long beginObjectId;
        private int contextCounter;
        private int keyIndex = 0, keyLength = 0;
        private int lineIndex = -1;
        private string text;
        private ScriptContext evalContext;
        private ParsingCacheBase cacheStack;
        private DefineContext contextStack;
        private ParsingExprCache exprCache;

        #endregion

        #endregion

        static ScriptParser()
        {
            #region 字符

            CharFlags = new int[]{
                25, 0, 0, 0, 0, 0, 0, 0,             //0,SOH,STX,ETX,EOT,ENQ,ACK,BEL
                0, 0, 23, 0, 0, 0, 0, 0,             //BS,HT,LF,VT,FF,CR,SO,SL
                0, 0, 0, 0, 0, 0, 0, 0,             //DLE,DC1,DC2,DC3,DC4,NAK,SYN,ETB
                0, 0, 0, 0, 0, 0, 0, 0,             //CAN,EM,SUB,ESC,FS,GS,RS,US
                0, 9, 17, 0, 22,  8,  14, 18,      //SPACE, !, ", #, $, %, &, '
                24, 24,  6,  4, 24,  5,  13,  7,     //(, ), *, +, ,, -, ., /
                1,  3,  3,  3,  3,  3,  3,  3,     //0, 1, 2, 3, 4, 5, 6, 7
                3,  3, 24, 24,  11,  10,  12, 24,     //8, 9, :, ;, <, =, >, ?
                0,  21,  21,  21,  21,  21,  21,  22,     //@, A, B, C, D, E, F, G
                22,  22,  22,  22,  22,  22,  22,  22,     //H, I, J, K, L, M, N, O
                22,  22,  22,  22,  22,  22,  22,  22,     //P, Q, R, S, T, U, V, W
                2,  22,  22, 24, 19, 24, 16, 22,     //X, Y, Z, [, \, ], ^, _,
                0,  21,  21,  21,  21,  21,  21,  22,     //`,a, b, c, d, e, f, g,
                22,  22,  22,  22,  22,  22,  22,  22,     //h, i, j, k, l, m, n, o
                22,  22,  22,  22,  22,  22,  22,  22,     //p, q, r, s, t, u, v, w,
                2,  22,  22, 24, 15, 24, 24, 0,     //x, y, z, {, |, }, ~, DEL
            };

            SingleFlags = new int[] {
                -1, -1, -1, -1, -1, -1, -1, -1,             //0,SOH,STX,ETX,EOT,ENQ,ACK,BEL
                -1, -1, -1, -1, -1, -1, -1, -1,             //BS,HT,LF,VT,FF,CR,SO,SL
                -1, -1, -1, -1, -1, -1, -1, -1,             //DLE,DC1,DC2,DC3,DC4,NAK,SYN,ETB
                -1, -1, -1, -1, -1, -1, -1, -1,             //CAN,EM,SUB,ESC,FS,GS,RS,US
                -1, -1, -1, -1, -1, -1, -1, -1,      //SPACE, !, ", #, $, %, &, '
                SC_LeftBracket1, SC_RightBracket1, -1, -1, SC_Comma, -1, OPT_GetObjectMember, -1,     //(, ), *, +, ,, -, ., /
                -1, -1, -1, -1, -1, -1, -1, -1,     //0, 1, 2, 3, 4, 5, 6, 7
                -1, -1, SC_Condition2, SC_Semicolon, -1, -1, -1, SC_Condition,     //8, 9, :, ;, <, =, >, ?
                -1, -1, -1, -1, -1, -1, -1, -1,     //@, A, B, C, D, E, F, G
                -1, -1, -1, -1, -1, -1, -1, -1,     //H, I, J, K, L, M, N, O
                -1, -1, -1, -1, -1, -1, -1, -1,     //P, Q, R, S, T, U, V, W
                -1, -1, -1, SC_LeftBracket2, -1, SC_RightBracket2, -1, -1,     //X, Y, Z, [, \, ], ^, _,
                -1, -1, -1, -1, -1, -1, -1, -1,     //`,a, b, c, d, e, f, g,
                 -1, -1, -1, -1, -1, -1, -1, -1,     //h, i, j, k, l, m, n, o
                 -1, -1, -1, -1, -1, -1, -1, -1,     //p, q, r, s, t, u, v, w,
                 -1, -1, -1, SC_LeftBracket3, -1, SC_RightBracket3, OPT_BitNot, -1,     //x, y, z, {, |, }, ~, DEL
            };

            #endregion

            #region 字符关系图

            short[][] relations = new short[][]{
                new short[] { 0, 1, 1 },
                new short[] { 1, 2, 2 },
                new short[] { 2, 4, 1, 3, 21 },
                new short[] { 2, -0x200, 0xFF },
                new short[] { 4, 4, 1, 3, 21 },
                new short[] { 4, 5, 13 },
                new short[] { 4, -1, 0xFF },
                new short[] { 1, 3, 13 },
                new short[] { 3, 3, 1, 3 },
                new short[] { 3, 5, 6 },
                new short[] { 5, 5, 1, 3, 13 },
                new short[] { 5, -0x200, 0xFF },
                new short[] { 3, -2, 0xFF },
                new short[] { 1, 6, 1, 3 },
                new short[] { 1, -3, 0xFF },

                new short[] { 0, 6, 3 },
                new short[] { 6, 6, 1, 3 },
                new short[] { 6, 3, 13 },
                new short[] { 6, -3, 0xFF },

                new short[] { 0, 7, 4 },
                new short[] { 7, -0x105, 4 },
                new short[] { 7, -0x106, 10 },
                new short[] { 7, -4, 0xFF },

                new short[] { 0, 8, 5 },
                new short[] { 8, -0x109, 10 },
                new short[] { 8, -0x108, 5 },
                new short[] { 8, -7, 0xFF },

                new short[] { 0, 9, 6 },
                new short[] { 9, -0x10B, 10 },
                new short[] { 9, -10, 0xFF },

                new short[] { 0, 10, 7 },
                new short[] { 10, -0x10D, 10 },
                new short[] { 10, -12, 0xFF },

                new short[] { 0, 11, 8 },
                new short[] { 11, -0x111, 10 },
                new short[] { 11, -16, 0xFF },

                new short[] { 0, 12, 9 },
                new short[] { 12, 13, 10 },
                new short[] { 13, -0x114, 10 },
                new short[] { 13, -19, 0xFF },
                new short[] { 12, -18, 0xFF },

                new short[] { 0, 14, 10 },
                new short[] { 14, 15, 10 },
                new short[] { 15, -0x117, 10 },
                new short[] { 15, -22, 0xFF },
                new short[] { 14, -21, 0xFF },

                new short[] { 0, 16, 11 },
                new short[] { 16, 17, 11 },
                new short[] { 17, -0x11B, 10},
                new short[] { 17, -26, 0xFF},
                new short[] { 16, -0x119, 10 },
                new short[] { 16, -24, 0xFF },

                new short[] { 0, 18, 12 },
                new short[] { 18, 19, 12 },
                new short[] { 19, 20, 12 },
                new short[] { 20, -0x121, 10 },
                new short[] { 20, -32, 0xFF },
                new short[] { 19, -0x11F, 10 },
                new short[] { 19, -30, 0xFF },
                new short[] { 18, -0x11D, 10 },
                new short[] { 18, -28, 0xFF },

                new short[] { 0, 21, 14 },
                new short[] { 21, -0x124, 14 },
                new short[] { 21, -0x123, 10 },
                new short[] { 21, -34, 0xFF },

                new short[] { 0, 22, 15 },
                new short[] { 22, -0x127, 15 },
                new short[] { 22, -0x126, 10 },
                new short[] { 22, -37, 0xFF },

                new short[] { 0, 23, 16 },
                new short[] { 23, -0x129, 10 },
                new short[] { 23, -40, 0xFF },

                new short[] { 0, 24, 17 },
                new short[] { 24, -0x12B, 17 },
                new short[] { 24, 25, 19 },
                new short[] { 25, -0x201, 23, 25 },
                new short[] { 25, 27, 0xFF },
                new short[] { 27, 25, 19 },
                new short[] { 27, -0x12A, 17 },
                new short[] { 27, -0x201, 23, 25 },
                new short[] { 27, 27, 0xFF },
                new short[] { 24, -0x201, 23 },
                new short[] { 24, 26, 0xFF },
                new short[] { 26, -0x12B, 17 },
                new short[] { 26, 25, 19 },
                new short[] { 26, -0x201, 23, 25 },
                new short[] { 26, 26, 0xFF },

                new short[] { 0, 28, 18 },
                new short[] { 28, -0x12B, 18 },
                new short[] { 28, 29, 19 },
                new short[] { 29, -0x201, 23, 25 },
                new short[] { 29, 31, 0xFF },
                new short[] { 31, 29, 19 },
                new short[] { 31, -0x201, 23, 25 },
                new short[] { 31, -0x12A, 18 },
                new short[] { 31, 31, 0xFF },
                new short[] { 28, -0x201, 23 },
                new short[] { 28, 30, 0xFF },
                new short[] { 30, -0x12B, 18 },
                new short[] { 30, 29, 19 },
                new short[] { 30, -0x201, 23, 25 },
                new short[] { 30, 30, 0xFF },

                new short[] { 0, 32, 2, 20, 21 },
                new short[] { 32, 32, 2, 20, 21 },
                new short[] { 32, 33, 1, 3, 22 },
                new short[] { 32, -44, 0xFF },

                new short[] { 0, 33, 22 },
                new short[] { 33, 33, 1, 2, 3, 20, 21, 22 },
                new short[] { 33, -45, 0xFF },

                new short[] { 0, -0x12E, 13, 24 },
            };

            StatusFlags = new short[CharFlagCount * StatusFlagCount];
            foreach (short[] relation in relations)
            {
                int index = relation[0] * CharFlagCount;
                short value = relation[1];
                if (relation[2] == 0xFF)
                {
                    int count = index + CharFlagCount;
                    for (int i = index; i < count; i++)
                        if (StatusFlags[i] == 0) StatusFlags[i] = value;
                }
                else
                {
                    int count = relation.Length;
                    for (int i = 2; i < count; i++)
                        StatusFlags[index + relation[i]] = value;
                }
            }

            #endregion

            #region 关键字初始化

            Array.Sort<KeywordFlag>(Keywords, new Comparison<KeywordFlag>(CompareKeyword));
            foreach (KeywordFlag kf in Keywords)
                foreach (char ch in kf.Keyword)
                {
                    int v = CharFlags[(int)ch];
                    if (v == 22) CharFlags[(int)ch] = 20;
                }

            #endregion

            #region 转义符

            char[] chs1 = new char[] { 'a', 'b', 'f', 'n', 'r', 't', 'v', '\\', '\'', '"', '0' }
                , chs2 = new char[] { '\a', '\b', '\f', '\n', '\r', '\t', '\v', '\\', '\'', '"', '\0' };
            EscapeChars = new char[128];
            for(int i = 0; i < 128; i++) EscapeChars[i] = InvalidChar;
            for(int i = chs1.Length - 1; i >= 0; i--)
                EscapeChars[(int)chs1[i]] = chs2[i];

            #endregion
        }

        private ScriptParser(string script)
        {
            this.text = script;
            this.beginObjectId = ScriptHelper.CustomObjectId;
        }

        #region 内部方法

        #region 工具方法

        private static int FindKeywordIndex(KeywordFlag[] sortKeywords, string keyword, int index, int length)
        {
            int l = 0, h = sortKeywords.Length - 1, sub, m;
            while (l <= h)
            {
                m = (l + h) >> 1;
                sub = CompareKeywordString(sortKeywords[m].Keyword, keyword, index, length);
                if (sub == 0) return m;
                else if (sub > 0) h = m - 1;
                else l = m + 1;
            }
            return ~l;
        }

        private static int CompareKeyword(KeywordFlag k1, KeywordFlag k2)
        {
            string str1 = k1.Keyword, str2 = k2.Keyword;
            int sub = str1.Length - str2.Length;
            if (sub == 0)
                sub = string.Compare(str1, 0, str2, 0, str1.Length, true);
            return sub;
        }

        private static int CompareKeywordString(string str1, string str2, int index2, int length2)
        {
            int sub = str1.Length - length2;
            if (sub == 0)
                sub = string.Compare(str1, 0, str2, index2, length2, true);
            return sub;
        }

        private void PushCache(ParsingCacheBase cache)
        {
            if (cache.InStack)
                throw new ArgumentOutOfRangeException("cache", "Cache is in stack, can't add stack.");
            cache.Parent = cacheStack;
            cacheStack = cache;
            cache.InStack = true;
        }

        private ParsingCacheBase PopCache(Type cacheType)
        {
            if (cacheStack != null && (cacheType == null || cacheType == cacheStack.GetType()))
            {
                ParsingCacheBase result = cacheStack;
                cacheStack = result.Parent;
                result.InStack = false;
                return result;
            }
            return null;
        }

        private T PopCache<T>() where T : ParsingCacheBase
        {
            ParsingCacheBase result = PopCache(typeof(T));
            return (T)result;
        }

        private bool CheckPopCache(ParsingCacheBase cache)
        {
            if (cacheStack == cache)
            {
                cacheStack.InStack = false;
                cacheStack = cacheStack.Parent;
                return true;
            }
            return false;
        }

        private ParsingCacheBase PeekCache()
        {
            return cacheStack;
        }

        private ParsingExprCache CreateExprCache(bool useDefaultInsert)
        {
            ParsingExprCache result = new ParsingExprCache();
            if (useDefaultInsert)
            {
                ElementBase elem = PeekLastInsert();
                if (elem != null)
                    result.LastInsert = result.Insert = elem;
            }
            return result;
        }

        private ParsingExprCache PushCurrentExprCache()
        {
            ParsingExprCache result = exprCache;
            PushCache(result);
            exprCache = null;
            return result;
        }

        private ParsingExprCache PushNewExprCache(ParseAbility ability, ElementBase lastElement)
        {
            ParsingExprCache cache = CreateExprCache(false);
            cache.Ability = ability;
            cache.Insert = cache.LastInsert = lastElement;
            PushCache(cache);
            return cache;
        }

        private ParsingExprCache PushNewExprCache(ParseAbility ability)
        {
            return PushNewExprCache(ability, exprCache == null ? null : exprCache.Insert);
        }

        private DefineContext CreateContext(FunctionElement element, DefineContext parentContext)
        {
            DefineContext result = new DefineContext(++beginObjectId, parentContext);
            if (element != null) element.Context = result;
            return result;
        }

        private void AddContextVariable(string name)
        {
            contextStack[name] = ScriptUndefined.Instance;
        }

        private DefineContext PeekContext()
        {
            return contextStack;
        }

        private DefineContext PopContext()
        {
            if (contextStack != null)
            {
                DefineContext result = contextStack;
                contextStack = result.ParentContext;
                result.ParentContext = null;
                result.FinishParsing();
                return result;
            }
            return null;
        }

        private void PushContext(DefineContext context)
        {
            context.ParentContext = contextStack;
            if (contextCounter >= 0)
                context.ContextIndex = contextCounter++;
            else
            {
                context.ContextIndex = -1;
                if (contextCounter == -2) context.UseVariableSaved();
                else context.DisableVariable();
            }
            contextStack = context;
        }

        private ElementBase PeekLastInsert()
        {
            if (contextStack != null) return contextStack.PeekLastInsert();
            return null;
        }

        private void PushLastInsert(ElementBase elem)
        {
            if (contextStack != null) contextStack.PushLastInsert(elem);
        }

        private bool CheckPopLastInsert(ElementBase elem)
        {
            if (contextStack != null) return contextStack.CheckPopLastInsert(elem);
            return false;
        }
        
        private void ThrowParseError()
        {
            throw new ScriptParseException(text, keyIndex, string.Format("读取到错误的标识：{0}", GetName()));
        }

        private void ThrowParseError(string message)
        {
            throw new ScriptParseException(text, keyIndex, string.Format("解析错误：{0}", message));
        }

        private void AcceptIndex()
        {
            if (keyLength > 0)
            {
                keyIndex += keyLength;
                keyLength = 0;
            }
        }

        private bool HasNewLine { get { return lineIndex >= 0; } }

        private bool SkipToLine()
        {
            return SkipToLine(false);
        }

        private bool SkipToLine(bool accept)
        {
            if (lineIndex >= 0)
            {
                if (accept)
                {
                    keyIndex = lineIndex + 1;
                    keyLength = 0;
                }
                else
                {
                    keyIndex = lineIndex;
                    keyLength = 1;
                }
                return true;
            }
            return false;
        }

        private void RejectIndex()
        {
            if (keyLength > 0)
                keyLength = 0;
        }

        private long SuspendIndex()
        {
            return ((long)keyLength << 32) | (long)keyIndex;
        }

        private void ResumeIndex(long value)
        {
            keyIndex = (int)(value & 0xFFFFFFFF);
            keyLength = (int)((value >> 32) & 0xFFFFFFFF);
        }

        private void SkipEmptyChars(bool checkLine)
        {
            AcceptIndex();
            int count = text.Length;
            bool isContinued;
            for (; keyIndex < count; keyIndex++)
            {
                isContinued = false;
                char ch = text[keyIndex];
                if (ch < 128)
                {
                    switch(ch)
                    {
                        case ' ':
                        case '\t':
                        case '\r':
                            isContinued = true;
                            break;
                        case '\n':
                            if (checkLine) lineIndex = keyIndex;
                            isContinued = true;
                            break;
                    }
                }
                if (!isContinued) break;
            }
        }

        private string GetName()
        {
            if (keyLength > 0) return text.Substring(keyIndex, keyLength);
            return string.Empty;
        }

        private string GetString(bool escape)
        {
            if (escape)
            {
                StringBuilder sb = new StringBuilder(keyLength);
                int len = keyLength - 1;
                for(int i = 1; i < len; i++)
                {
                    char ch = text[keyIndex + i];
                    if(ch == '\\')
                    {
                        i++;
                        ch = text[keyIndex + i];
                        if(ch == InvalidChar) sb.Append(ch);
                        else sb.Append(EscapeChars[(int)ch]);
                    }
                    else sb.Append(ch);
                }
                return sb.ToString();
            }
            else
                return text.Substring(keyIndex + 1, keyLength - 2);
        }

        private void SkipComment(bool isMulti)
        {
            int count = text.Length;
            if (!isMulti) count--;
            for (int i = keyIndex; i < count; i++)
            {
                char ch = text[i];
                if (isMulti)
                {
                    if (ch == '*' && text[i + 1] == '/')
                    {
                        keyIndex = i + 2;
                        break;
                    }
                }
                else if (ch == '\n')
                {
                    keyIndex = i + 1;
                    break;
                }
            }
        }

        private void SkipEmptyAndComment(bool checkLine)
        {
            AcceptIndex();
            int count = text.Length;
            while (keyIndex < count)
            {
                SkipEmptyChars(checkLine);
                if (keyIndex < count && text[keyIndex] == '/' && keyIndex + 1 < count)
                {
                    char ch = text[keyIndex + 1];
                    if (ch == '*' || ch == '/')
                    {
                        keyIndex++;
                        SkipComment(ch == '*');
                        continue;
                    }
                }
                break;
            }
        }

        private bool CheckNextChar(char ch)
        {
            return CheckNextChar(ch, false);
        }

        private bool CheckNextChar(char ch, bool allowEnd)
        {
            SkipEmptyAndComment(false);
            if (keyIndex >= text.Length)
            {
                if (allowEnd) return true;
                ThrowParseError();
            }
            if (text[keyIndex] == ch)
            {
                keyIndex++;
                return true;
            }
            return false;
        }

        private int ReadFlag()
        {
            lineIndex = -1;
            int count = text.Length;
            SkipEmptyAndComment(true);
            if (keyIndex >= count)
            {
                keyLength = 0;
                return 0;
            }
            int statusIndex = 0;
            int charIndex;
            for (int i = keyIndex; i <= count; i++)
            {
                if (i == count) charIndex = 25;
                else
                {
                    char ch = text[i];
                    if (ch > 127) charIndex = 22;
                    else charIndex = CharFlags[(int)ch];
                }
                statusIndex = StatusFlags[statusIndex * CharFlagCount + charIndex];
                if (statusIndex < 0)
                {
                    keyLength = i - keyIndex + 1;
                    break;
                }
            }
            if (statusIndex >= 0) ThrowParseError();
            statusIndex = -statusIndex;
            if ((statusIndex & 0x200) != 0)
                ThrowParseError();
            if ((statusIndex & 0x100) == 0) keyLength--;
            else statusIndex = statusIndex & 0xFF;
            int result = LexicalFlags[statusIndex];
            switch(result)
            {
                case FG_KeyVariable:
                    {
                        int i = FindKeywordIndex(Keywords, text, keyIndex, keyLength);
                        result = i >= 0 ? Keywords[i].Flag : FG_Variable;
                    }
                    break;
                case FG_SingleChar:
                    result = SingleFlags[text[keyIndex]];
                    if (result < 0) ThrowParseError();
                    break;
            }
            return result;
        }

        private void AddElement(ElementBase element)
        {
            AddElement(element, null);
        }

        private void AddElement(ElementBase element, ElementBase lastElement)
        {
            if (lastElement != null)
            {
                if (lastElement.Prev != null)
                    LinkElement(lastElement.Prev, element);
                else if (contextStack != null && contextStack.First == lastElement)
                    contextStack.First = element;
                LinkElement(element, lastElement);
            }
            else if (exprCache != null && exprCache.Insert != null)
            {
                if (exprCache.Insert.Prev != null)
                    LinkElement(exprCache.Insert.Prev, element);
                else if (contextStack != null && contextStack.First == exprCache.Insert)
                    contextStack.First = element;
                LinkElement(element, exprCache.Insert);
            }
            else if (contextStack != null)
            {
                if (contextStack.Last == null)
                    contextStack.First = contextStack.Last = element;
                else
                {
                    contextStack.Last.Next = element;
                    element.Prev = contextStack.Last;
                    contextStack.Last = element;
                }
            }
            else
                ThrowParseError("无法添加新的元素。");
        }

        private void RemoveElement(ElementBase element)
        {
            if (element.Prev != null && element.Prev.Next == element)
                element.Prev.Next = element.Next;
            if (element.Next != null && element.Next.Prev == element)
                element.Next.Prev = element.Prev;
            if (contextStack != null)
            {
                if (contextStack.First == element)
                    contextStack.First = element.Next;
                if (contextStack.Last == element)
                    contextStack.Last = element.Prev;
            }
        }

        private void LinkElement(ElementBase elem1, ElementBase elem2)
        {
            if (elem1 != null && elem2 != null)
            {
                elem1.Next = elem2;
                elem2.Prev = elem1;
            }
        }

        private void CheckAssignOperatorValid(OperatorElementBase elem)
        {
            ResultVisitFlag visitFlag = elem.GetArgusResultVisit(false);
            if (visitFlag == ResultVisitFlag.Set || visitFlag == ResultVisitFlag.GetSet)
                CheckAssignOperatorValid(elem, PeekContext(), visitFlag);
        }

        private void CheckAssignOperatorValid(OperatorElementBase elem, DefineContext context, ResultVisitFlag visitFlag)
        {
            ElementBase prev = elem.Prev;
            if (!(prev is VariableElement || prev is GetObjectMemberElement || prev is GetArrayMemberElement))
                ThrowParseError(string.Format("操作符“{0}”之前，必须是变量表达式。", Enum.GetName(typeof(OperatorType), elem.Type)));
            if (visitFlag == ResultVisitFlag.GetSet)
            {
                if (prev is GetObjectMemberElement)
                    ((GetObjectMemberElement)prev).SetVarIndex2(context.NewVarIndex());
                else if (prev is VariableElement)
                    ((VariableElement)prev).SetVarIndex2(context.NewVarIndex());
            }
        }

        private void CheckAssignOperatorOnFinished(DefineContext context, object[] argus)
        {
            OperatorElementBase elem = (OperatorElementBase)argus[0];
            CheckAssignOperatorValid((OperatorElementBase)argus[0], context, (ResultVisitFlag)argus[1]);
        }

        private void CheckAddAssignOperatorCallback(OperatorElementBase elem)
        {
            ResultVisitFlag visitFlag = elem.GetArgusResultVisit(false);
            if (visitFlag == ResultVisitFlag.Set || visitFlag == ResultVisitFlag.GetSet)
                PeekContext().AddFinishCallback(new ContextFinishedCallback(CheckAssignOperatorOnFinished), elem, visitFlag);
        }

        private long NewObjectId()
        {
            return beginObjectId >= 0 ? beginObjectId++ : beginObjectId;
        }

        #endregion

        #region 解析特定符号

        private int CheckExprEnd(ParsingExprCache cache, int flag)
        {
            if (flag == ER_Operand)
            {
                cache.Step |= STP_ReadOperand;
                return ER_Continue;
            }
            ThrowParseError();
            return ER_Error;
        }

        private void ParseObjectStart()
        {
            ObjectStartElement startElem = new ObjectStartElement();
            startElem.ResultVisit = ResultVisitFlag.Get;
            AddElement(startElem);
            ObjectEndElement lastElement = new ObjectEndElement();
            lastElement.ObjectId = NewObjectId();
            AddElement(lastElement);
            ElementBase elem = ParseObjectField(lastElement);
            if (elem == null)
                exprCache.Step |= STP_ReadOperand;
            else
            {
                PushCurrentExprCache();
                ParsingInnerCache cache = new ParsingInnerCache();
                cache.Type = ParsingInnerCache.TYPE_Object;
                cache.Elem = lastElement;
                PushCache(cache);
                exprCache = CreateExprCache(false);
                exprCache.Ability = ParseAbility.Expr;
                exprCache.LastInsert = exprCache.Insert = elem;
                exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
            }
        }

        private ElementBase ParseObjectField(ElementBase lastElem)
        {
            int flag = ReadFlag();
            string name = null;
            if (flag == CT_Variable)
                name = GetName();
            else if (flag == CT_String || flag == CT_EscapeString)
                name = GetString(flag == CT_EscapeString);
            else if (flag == SC_RightBracket3)
                return null;
            else
                ThrowParseError("解析对象错误。");
            flag = ReadFlag();
            if (flag != SC_Condition2)
                ThrowParseError();
            ObjectFieldElement elem = new ObjectFieldElement(name);
            AddElement(elem, lastElem);
            return elem;
        }

        private int CheckParseObjectEnd(ParsingInnerCache cache, int flag)
        {
            if (flag == (ER_Expr | SC_Comma))
            {
                ElementBase elem = ParseObjectField(cache.Elem);
                if (elem == null) flag = ER_Expr | SC_RightBracket3;
                else
                {
                    PushNewExprCache(ParseAbility.Expr, elem).Flag = EF_CheckLastVisitState | EF_AttachEndChar;
                    return ER_Continue;
                }
            }
            if (flag == (ER_Expr | SC_RightBracket3))
            {
                if (!CheckPopCache(cache)) ThrowParseError();
                return ER_Operand;
            }
            ThrowParseError();
            return ER_Error;
        }

        private void ParseArrayStart()
        {
            ArrayStartElement startElem = new ArrayStartElement();
            startElem.ResultVisit = ResultVisitFlag.Get;
            AddElement(startElem);
            ArrayEndElement endElem = new ArrayEndElement();
            AddElement(endElem);
            if (!CheckNextChar(']'))
            {
                ArrayItemElement elem = new ArrayItemElement();
                AddElement(elem, endElem);
                PushCurrentExprCache();
                ParsingInnerCache cache = new ParsingInnerCache();
                cache.Elem = endElem;
                cache.Type = ParsingInnerCache.TYPE_Array;
                PushCache(cache);

                exprCache = CreateExprCache(false);
                exprCache.Ability = ParseAbility.Expr;
                exprCache.LastInsert = exprCache.Insert = elem;
                exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
            }
            else
                exprCache.Step |= STP_ReadOperand;
        }

        private int CheckParseArrayEnd(ParsingInnerCache cache, int flag)
        {
            switch (flag)
            {
                case ER_Expr | SC_Comma:
                    {
                        ArrayItemElement elem = new ArrayItemElement();
                        AddElement(elem, cache.Elem);
                        PushNewExprCache(ParseAbility.Expr, elem).Flag = EF_CheckLastVisitState | EF_AttachEndChar;
                        return ER_Continue;
                    }
                case ER_Expr | SC_RightBracket2:
                    {
                        if (!CheckPopCache(cache)) ThrowParseError();
                        return ER_Operand;
                    }
            }
            ThrowParseError();
            return ER_Error;
        }

        private void ParseInnerExprStart()
        {
            ParsingExprCache lastExprCache = PushCurrentExprCache();

            ParsingInnerCache cache = new ParsingInnerCache();
            cache.Type = ParsingInnerCache.TYPE_InnerExpr;
            PushCache(cache);

            exprCache = CreateExprCache(false);
            exprCache.Ability = ParseAbility.Expr;
            exprCache.LastInsert = exprCache.Insert = lastExprCache.Insert;
            exprCache.Flag = EF_AttachEndChar;
        }

        private int CheckParseInnerExprEnd(ParsingInnerCache cache, int flag)
        {
            if (flag == (ER_Expr | SC_RightBracket1))
            {
                if (!CheckPopCache(cache)) ThrowParseError();
                return ER_Operand;
            }
            ThrowParseError();
            return ER_Error;
        }

        private void ParseFunctionDefined()
        {
            int flag = ReadFlag();
            FunctionElement element = new FunctionElement();
            DefineContext parentContext = PeekContext();
            DefineContext context = CreateContext(element, parentContext);
            if (flag == CT_Variable)
            {
                string funcName = GetName();
                parentContext[funcName] = new ScriptFunctionProxy(context);
                flag = ReadFlag();
            }
            if (flag != SC_LeftBracket1)
                ThrowParseError();
            do
            {
                flag = ReadFlag();
                if (flag == CT_Variable)
                {
                    string name = GetName();
                    context[name] = ScriptUndefined.Instance;
                    context.ArgusCount++;
                }
                else if (flag == SC_RightBracket1)
                {
                    AcceptIndex();
                    break;
                }
                else if (flag != SC_Comma)
                    ThrowParseError();
            } while (true);
            if (ReadFlag() != SC_LeftBracket3)
                ThrowParseError();
            AddElement(element);
            PushCurrentExprCache();
            ParsingInnerCache cache = new ParsingInnerCache();
            cache.Type = ParsingInnerCache.TYPE_Function;
            PushCache(cache);
            PushContext(context);
        }

        private int CheckParseFunctionDefinedEnd(ParsingInnerCache cache, int flag)
        {
            if (flag == (ER_EndChar | SC_RightBracket3))
            {
                if (!CheckPopCache(cache)) ThrowParseError();
                PopContext();
                return ER_Operand;
            }
            else if ((flag & (ER_Expr | ER_Logic)) != 0)
                return ER_Continue;
            ThrowParseError();
            return ER_Error;
        }

        private void ParseInvokeMethod()
        {
            CheckOperatorLevel(OperatorType.InvokeMethod);
            InvokeMethodElement elem = exprCache.Insert as InvokeMethodElement;
            if (elem != null && elem.Type == OperatorType.New)
            {
                if (elem != exprCache.LastInsert) exprCache.Insert = elem.Next;
            }
            else
            {
                elem = new InvokeMethodElement(false);
                AddElement(elem);
                ElementBase prevElem = elem.Prev;
                elem.ResultVisit = prevElem.ResultVisit;
                if (prevElem is GetObjectMemberElement || prevElem is GetArrayMemberElement)
                    prevElem.ResultVisit = ResultVisitFlag.ObjectMember;
                else
                    prevElem.ResultVisit = ResultVisitFlag.Get;
            }
            if (!CheckNextChar(')'))
            {
                PushCurrentExprCache();
                ParsingInnerCache cache = new ParsingInnerCache();
                cache.Type = ParsingInnerCache.TYPE_InvokeMethod;
                cache.Elem = elem;
                PushCache(cache);

                exprCache = CreateExprCache(false);
                exprCache.Ability = ParseAbility.Expr;
                exprCache.LastInsert = exprCache.Insert = elem;
                exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
            }
        }

        private int CheckParseInvokeMethodEnd(ParsingInnerCache cache, int flag)
        {
            InvokeMethodElement elem = (InvokeMethodElement)cache.Elem;
            switch (flag)
            {
                case ER_Expr | SC_Comma:
                    {
                        elem.ArgCount++;
                        PushNewExprCache(ParseAbility.Expr, elem).Flag = EF_CheckLastVisitState | EF_AttachEndChar;
                    }
                    return ER_Continue;
                case ER_Expr | SC_RightBracket1:
                    {
                        elem.ArgCount++;
                        if (!CheckPopCache(cache)) ThrowParseError();
                    }
                    return ER_Operand;
            }
            ThrowParseError();
            return ER_Error;
        }

        private void ParseGetArrayMember()
        {
            CheckOperatorLevel(OperatorType.GetArrayMember);
            GetArrayMemberElement elem = new GetArrayMemberElement();
            AddElement(elem);
            elem.Prev.ResultVisit = elem.ArgusResultVisit;
            exprCache.CheckLastOperandVisitState();
            PushCurrentExprCache();

            ParsingInnerCache cache = new ParsingInnerCache();
            cache.Type = ParsingInnerCache.TYPE_GetArrayMember;
            PushCache(cache);

            exprCache = CreateExprCache(false);
            exprCache.Ability = ParseAbility.Expr;
            exprCache.LastInsert = exprCache.Insert = elem;
            exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
        }

        private int CheckParseGetArrayMemberEnd(ParsingInnerCache cache, int flag)
        {
            if (flag == (ER_Expr | SC_RightBracket2))
            {
                if (!CheckPopCache(cache)) ThrowParseError();
                return ER_Operand;
            }
            ThrowParseError();
            return ER_Error;
        }

        private void CheckOperatorLevel(OperatorType type)
        {
            if (exprCache.Insert != exprCache.LastInsert)
            {
                int level = OptLevels[(int)type];
                bool isAssign = level == 1;
                do
                {
                    OperatorElementBase optElem = exprCache.Insert as OperatorElementBase;
                    if (optElem == null) break;
                    int level2 = OptLevels[(int)optElem.Type];
                    if (isAssign)
                    {
                        if (level >= level2) break;
                    }
                    else if (level > level2) break;
                    exprCache.Insert = exprCache.Insert.Next;
                } while (exprCache.Insert != exprCache.LastInsert);
            }
        }

        private void ParseCondition()
        {
            CheckOperatorLevel(OperatorType.LogicOr);
            CheckElement elem = new CheckElement();
            AddElement(elem);
            elem.Prev.ResultVisit = ResultVisitFlag.Get;
            IgnoreElement ignoreElem = new IgnoreElement();
            ignoreElem.ArgusCounter = 1;
            AddElement(ignoreElem);
            exprCache.Step = STP_ParseOperand;
            PushCurrentExprCache();
            ParsingInnerCache cache = new ParsingInnerCache();
            cache.Type = ParsingInnerCache.TYPE_Condition;
            cache.Elem = elem;
            cache.Elem2 = ignoreElem;
            PushCache(cache);
            exprCache = CreateExprCache(false);
            exprCache.Ability = ParseAbility.Expr;
            exprCache.LastInsert = exprCache.Insert = ignoreElem;
            exprCache.Flag = EF_AttachEndChar;
        }

        private int CheckParseConditionEnd(ParsingInnerCache cache, int flag)
        {
            if((flag & ER_Expr) != 0)
            {
                flag &= ~ER_Expr;
                if (cache.Elem2.Prev != cache.Elem) cache.Elem2.Prev.ResultVisit = ResultVisitFlag.Get;
                if (cache.ArgCount == 0)
                {
                    if (flag != SC_Condition2) ThrowParseError();
                    ((CheckElement)cache.Elem).TruePointer = cache.Elem.Next;
                    LinkElement(cache.Elem, cache.Elem2);
                    PushNewExprCache(ParseAbility.Expr, cache.Elem2).Flag = EF_IgnoreEndChar;
                    cache.ArgCount = 1;
                    return ER_Continue;
                }
                else
                {
                    if (!CheckPopCache(cache)) ThrowParseError();
                    return ER_Operand;
                }
            }
            ThrowParseError();
            return ER_Error;
        }

        private int ReadVarName()
        {
            int flag = ReadFlag();
            if (flag != CT_Variable) ThrowParseError();
            string varName = GetName();
            AddContextVariable(varName);
            flag = ReadFlag();
            return flag;
        }

        private int ParseVarStart()
        {
            long indexInfo = SuspendIndex();
            bool isContinued;
            do
            {
                isContinued = false;
                int flag = ReadVarName();
                switch (flag)
                {
                    case OPT_Assign:
                        {
                            ResumeIndex(indexInfo);
                            ParsingInnerCache cache = new ParsingInnerCache();
                            cache.Type = ParsingInnerCache.TYPE_Var;
                            cache.Elem = exprCache.Insert;
                            PushCache(cache);
                            exprCache.Ability = ParseAbility.Expr;
                            exprCache.Step = STP_First;
                            exprCache.Flag = EF_NormalEndChar;
                            return -1;
                        }
                    case FG_NONE:
                        return ER_Expr | SC_NewLine;
                    case SC_Semicolon:
                        return ER_Expr | SC_Semicolon;
                    case SC_RightBracket1:
                    case SC_RightBracket2:
                    case SC_RightBracket3:
                        RejectIndex();
                        return ER_Expr | SC_NewLine;
                    case SC_Comma:
                        AcceptIndex();
                        indexInfo = SuspendIndex();
                        isContinued = true;
                        break;
                    default:
                        if (HasNewLine)
                        {
                            SkipToLine();
                            return ER_Expr | SC_NewLine;
                        }
                        break;
                }
            } while (isContinued);
            ThrowParseError();
            return ER_Error;
        }

        private int CheckParseVarEnd(ParsingInnerCache cache, int flag)
        {
            if ((flag & ER_Expr) != 0)
            {
                flag &= ~ER_Expr;
                switch(flag)
                {
                    case SC_Semicolon:
                    case SC_NewLine:
                        {
                            if (!CheckPopCache(cache)) ThrowParseError();
                            return ER_Expr | flag;
                        }
                    case SC_Comma:
                        {
                            AcceptIndex();
                            long indexInfo = SuspendIndex();
                            bool isContinued;
                            do
                            {
                                isContinued = false;
                                int flag2 = ReadVarName();
                                switch (flag2)
                                {
                                    case OPT_Assign:
                                        ResumeIndex(indexInfo);
                                        PushNewExprCache(ParseAbility.Expr, cache.Elem);
                                        return ER_Continue;
                                    case FG_NONE: return ER_Expr | SC_NewLine;
                                    case SC_Semicolon: return ER_Expr | SC_Semicolon;
                                    case SC_Comma:
                                        AcceptIndex();
                                        indexInfo = SuspendIndex();
                                        isContinued = true;
                                        break;
                                    default:
                                        if (HasNewLine)
                                        {
                                            SkipToLine();
                                            return ER_Expr | SC_NewLine;
                                        }
                                        ThrowParseError();
                                        break;
                                }
                            }
                            while (isContinued);
                            break;
                        }
                }
            }
            ThrowParseError();
            return ER_Error;
        }

        private void ParseBlockStart()
        {
            ParsingLogicCache cache = new ParsingLogicCache();
            cache.Type = ParsingLogicCache.TYPE_BlockStart;
            PushCache(cache);
        }

        private int CheckParseBlockStartEnd(ParsingLogicCache cache, int flag)
        {
            if (CheckBlockContinue(true, flag)) return ER_Continue;
            if (!CheckPopCache(cache)) ThrowParseError();
            return ER_Logic;
        }

        private void ParseIf()
        {
            if (!CheckNextChar('(')) ThrowParseError();

            CheckElement elem = new CheckElement();
            AddElement(elem);

            IgnoreElement ignoreElem = new IgnoreElement();
            AddElement(ignoreElem);
            PushLastInsert(ignoreElem);

            ParsingLogicCache cache = new ParsingLogicCache();
            cache.Type = ParsingLogicCache.TYPE_If;
            cache.Elem = elem;
            cache.LastElem = ignoreElem;
            PushCache(cache);

            exprCache.Ability = ParseAbility.Expr;
            exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
            exprCache.LastInsert = exprCache.Insert = elem;
        }

        private bool CheckBlockContinue(bool isBlock, int flag)
        {
            if (isBlock)
            {
                if ((flag & (ER_Expr | ER_Logic)) != 0) return true;
                if (flag != (ER_EndChar | SC_RightBracket3)) ThrowParseError();
            }
            else if ((flag & (ER_Expr | ER_Logic)) == 0) ThrowParseError();
            return false;
        }

        private int CheckParseIfEnd(ParsingLogicCache cache, int flag)
        {
            int step = cache.Step & 3;
            if (step == 0)
            {
                if (flag != (ER_Expr | SC_RightBracket1)) ThrowParseError();
                if (CheckNextChar('{')) cache.Step = 1;
                else
                {
                    cache.Step = 2;
                    PushNewExprCache(ParseAbility.Expr | ParseAbility.Flow, cache.LastElem);
                }
                return ER_Continue;
            }
            else
            {
                if (CheckBlockContinue(step == 1, flag)) return ER_Continue;
                if (cache.Elem != null)
                {
                    CheckElement checkElem = (CheckElement)cache.Elem;
                    checkElem.TruePointer = cache.Elem.Next;
                }
                LinkElement(cache.Elem, cache.LastElem);
                int flag2 = ReadFlag();
                if (flag2 == LG_Else)
                {
                    if ((cache.Step & 4) != 0) ThrowParseError();
                    flag2 = ReadFlag();
                    if (flag2 == LG_If)
                    {
                        if (!CheckNextChar('(')) ThrowParseError();
                        CheckElement elem = new CheckElement();
                        AddElement(elem, cache.LastElem);
                        cache.Elem = elem;
                        cache.Step = 0;
                        PushNewExprCache(ParseAbility.Expr, elem).Flag = EF_CheckLastVisitState | EF_AttachEndChar;
                        return ER_Continue;
                    }
                    else
                    {
                        cache.Elem = null;
                        if (flag2 == SC_LeftBracket3) cache.Step = 5;
                        else
                        {
                            RejectIndex();
                            cache.Step = 6;
                            PushNewExprCache(ParseAbility.Expr | ParseAbility.Flow, cache.LastElem);
                        }
                        return ER_Continue;
                    }
                }
                else
                {
                    if (!CheckPopLastInsert(cache.LastElem)) ThrowParseError();
                    if (!CheckPopCache(cache)) ThrowParseError();
                    RejectIndex();
                    return ER_Logic;
                }
            }
        }

        private void ParseFor()
        {
            if (!CheckNextChar('(')) ThrowParseError();
            long oldInfo = SuspendIndex();
            int flag = ReadFlag();
            ParsingLogicCache cache;
            IgnoreElement ignoreElem;
            if (flag == LG_VarDefine)
            {
                flag = ReadFlag();
                if (flag == CT_Variable)
                {
                    string name = GetName();
                    flag = ReadFlag();
                    if (flag == OPT_In || flag == LG_Of)
                    {
                        AddContextVariable(name);
                        EnumInitElement initElem = new EnumInitElement(flag == OPT_In);
                        AddElement(initElem);
                        initElem.ResultVisit = ResultVisitFlag.Get;
                        EnumEachElement eachElem = new EnumEachElement(evalContext, name, PeekContext().NewVarIndex());
                        AddElement(eachElem);
                        ignoreElem = new IgnoreElement();
                        ignoreElem.ArgusCounter = 1;
                        AddElement(ignoreElem);
                        cache = new ParsingLogicCache();
                        cache.Type = ParsingLogicCache.TYPE_ForIn;
                        cache.Elem = eachElem;
                        cache.LastElem = ignoreElem;
                        PushCache(cache);
                        exprCache.Ability = ParseAbility.Expr;
                        exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
                        exprCache.LastInsert = exprCache.Insert = initElem;
                        PushLastInsert(cache.LastElem);
                        return;
                    }
                }
            }
            ResumeIndex(oldInfo);
            LoopCheckElement checkElem = new LoopCheckElement();
            AddElement(checkElem);
            ignoreElem = new IgnoreElement();
            AddElement(ignoreElem);
            cache = new ParsingLogicCache();
            cache.Type = ParsingLogicCache.TYPE_For;
            cache.Elem = checkElem;
            cache.LastElem = ignoreElem;
            PushCache(cache);
            exprCache.Ability = ParseAbility.Declare | ParseAbility.Expr;
            exprCache.Flag = EF_AttachEndChar;
            exprCache.LastInsert = exprCache.Insert = checkElem;
        }

        private int CheckParseForInEnd(ParsingLogicCache cache, int flag)
        {
            if (cache.Step == 0)
            {
                if (flag != (ER_Expr | SC_RightBracket1)) ThrowParseError();
                if (CheckNextChar('{')) cache.Step = 1;
                else
                {
                    cache.Step = 2;
                    PushNewExprCache(ParseAbility.Expr | ParseAbility.Flow, cache.LastElem);
                }
                return ER_Continue;
            }
            else
            {
                if (CheckBlockContinue(cache.Step == 1, flag)) return ER_Continue;
                if (cache.Elem.Next == cache.LastElem)
                    AddElement(new IgnoreElement(), cache.LastElem);
                cache.LastElem.Prev.Next = cache.Elem;
                ((EnumEachElement)cache.Elem).AvailablePointer = cache.Elem.Next;
                LinkElement(cache.Elem, cache.LastElem);
                cache.Elem.Next = cache.LastElem;
                if (!CheckPopLastInsert(cache.LastElem)) ThrowParseError();
                if (!CheckPopCache(cache)) ThrowParseError();
                return ER_Logic;
            }
        }

        private int CheckParseForEnd(ParsingLogicCache cache, int flag)
        {
            if (cache.Step == 0)
            {
                if ((flag & (ER_Expr | ER_EndChar)) == 0) ThrowParseError();
                int f = flag & ER_FF;
                if (f == SC_Comma)
                {
                    PushNewExprCache(ParseAbility.Expr, cache.Elem);
                    return ER_Continue;
                }
                if (f != SC_Semicolon) ThrowParseError();
                cache.Step = 1;
                PushNewExprCache(ParseAbility.Expr, cache.LastElem).Flag = EF_AttachEndChar;
                return ER_Continue;
            }
            else if (cache.Step == 1)
            {
                int f = flag & (ER_Expr | ER_EndChar);
                if (f == 0 || (flag & ER_FF) != SC_Semicolon) ThrowParseError();
                if (f == ER_Expr)
                {
                    CheckElement checkElem = new CheckElement();
                    AddElement(checkElem, cache.LastElem);
                    checkElem.Prev.ResultVisit = ResultVisitFlag.Get;
                    cache.Elem2 = checkElem;
                }
                cache.Step = 2;
                PushNewExprCache(ParseAbility.Expr, cache.LastElem).Flag = EF_AttachEndChar;
                return ER_Continue;
            }
            else if (cache.Step == 2)
            {
                if ((flag & (ER_Expr | ER_EndChar)) == 0) ThrowParseError();
                int f = flag & ER_FF;
                if (f == SC_Comma)
                {
                    PushNewExprCache(ParseAbility.Expr, cache.Elem).Flag = EF_AttachEndChar;
                    return ER_Continue;
                }
                if (f != SC_RightBracket1) ThrowParseError();
                ElementBase elem = cache.Elem2 != null ? cache.Elem2 : cache.Elem;
                if (elem.Next == cache.LastElem)
                {
                    cache.Elem3 = new IgnoreElement();
                    AddElement(cache.Elem3, cache.LastElem);
                }
                else
                    cache.Elem3 = elem.Next;
                if (CheckNextChar('{'))
                {
                    PushLastInsert(cache.Elem3);
                    cache.Step = 3;
                }
                else
                {
                    cache.Step = 4;
                    PushNewExprCache(ParseAbility.Expr | ParseAbility.Flow, cache.Elem3);
                }
                return ER_Continue;
            }
            else
            {
                if (CheckBlockContinue(cache.Step == 3, flag)) return ER_Continue;
                cache.LastElem.Prev.Next = cache.Elem.Next;
                CheckElement checkElem = cache.Elem2 as CheckElement;
                if (checkElem != null)
                {
                    checkElem.TruePointer = checkElem.Next;
                    LinkElement(checkElem, cache.LastElem);
                }
                ((LoopCheckElement)cache.Elem).Finish(this, null);
                if (cache.Step == 3 && !CheckPopLastInsert(cache.Elem3)) ThrowParseError();
                if (!CheckPopCache(cache)) ThrowParseError();
                return ER_Logic;
            }
        }

        private void ParseWhile()
        {
            if (!CheckNextChar('(')) ThrowParseError();
            LoopCheckElement loopElem = new LoopCheckElement();
            AddElement(loopElem);
            CheckElement checkElem = new CheckElement();
            AddElement(checkElem);
            IgnoreElement ignoreElem = new IgnoreElement();
            AddElement(ignoreElem);

            ParsingLogicCache cache = new ParsingLogicCache();
            cache.Type = ParsingLogicCache.TYPE_While;
            cache.Elem = loopElem;
            cache.Elem2 = checkElem;
            cache.LastElem = ignoreElem;
            PushCache(cache);

            exprCache.Ability = ParseAbility.Expr;
            exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
            exprCache.LastInsert = exprCache.Insert = checkElem;
        }

        private int CheckParseWhileEnd(ParsingLogicCache cache, int flag)
        {
            if (cache.Step == 0)
            {
                if (flag != (ER_Expr | SC_RightBracket1)) ThrowParseError();
                if (CheckNextChar('{'))
                {
                    PushLastInsert(cache.LastElem);
                    cache.Step = 1;
                }
                else
                {
                    PushNewExprCache(ParseAbility.Expr | ParseAbility.Flow, cache.LastElem);
                    cache.Step = 2;
                }
                return ER_Continue;
            }
            else
            {
                if (CheckBlockContinue(cache.Step == 1, flag)) return ER_Continue;
                if (cache.Elem2.Next == cache.LastElem)
                    ((CheckElement)cache.Elem2).TruePointer = cache.Elem.Next;
                else
                {
                    ((CheckElement)cache.Elem2).TruePointer = cache.Elem2.Next;
                    cache.LastElem.Prev.Next = cache.Elem.Next;
                }
                LinkElement(cache.Elem2, cache.LastElem);
                ((LoopCheckElement)cache.Elem).Finish(this, null);
                if (cache.Step == 1 && !CheckPopLastInsert(cache.LastElem)) ThrowParseError();
                if (!CheckPopCache(cache)) ThrowParseError();
                return ER_Logic;
            }
        }

        private void ParseDoWhile()
        {
            if (!CheckNextChar('{')) ThrowParseError();

            LoopCheckElement loopElem = new LoopCheckElement();
            AddElement(loopElem);
            CheckElement checkElem = new CheckElement();
            AddElement(checkElem);
            IgnoreElement ignoreElem = new IgnoreElement();
            AddElement(ignoreElem);

            ParsingLogicCache cache = new ParsingLogicCache();
            cache.Type = ParsingLogicCache.TYPE_DoWhile;
            cache.Elem = loopElem;
            cache.Elem2 = checkElem;
            cache.LastElem = ignoreElem;
            PushCache(cache);

            PushLastInsert(ignoreElem);
        }

        private int CheckParseDoWhileEnd(ParsingLogicCache cache, int flag)
        {
            if (cache.Step == 0)
            {
                if (!CheckBlockContinue(true, flag))
                {
                    if (!CheckPopLastInsert(cache.LastElem)) ThrowParseError();
                    int flag2 = ReadFlag();
                    if (flag2 != LG_While || !CheckNextChar('(')) ThrowParseError();
                    PushNewExprCache(ParseAbility.Expr, cache.Elem2).Flag = EF_CheckLastVisitState | EF_AttachEndChar;
                    cache.Step = 1;
                }
                return ER_Continue;
            }
            else
            {
                if (flag != (ER_Expr | SC_RightBracket1) || !CheckPopCache(cache)) ThrowParseError();
                CheckNextChar(';', true);
                LoopCheckElement loopElem = (LoopCheckElement)cache.Elem;
                CheckElement checkElem = (CheckElement)cache.Elem2;
                ElementBase checkBeginElem = loopElem.Next;
                cache.LastElem.Prev.Next = checkBeginElem;
                LinkElement(loopElem, checkElem.Next);
                checkElem.TruePointer = checkElem.Next;
                LinkElement(checkElem, cache.LastElem);
                loopElem.Finish(this, checkBeginElem);
                return ER_Logic;
            }
        }

        private void ParseSwitch()
        {
            if (!CheckNextChar('(')) ThrowParseError();
            CheckElement checkElem = new CheckElement();
            AddElement(checkElem);
            IgnoreElement ignoreElem = new IgnoreElement();
            ignoreElem.ArgusCounter = 1;
            AddElement(ignoreElem);

            ParsingLogicCache cache = new ParsingLogicCache();
            cache.Type = ParsingLogicCache.TYPE_Switch;
            cache.Elem = checkElem;
            cache.LastElem = ignoreElem;
            PushCache(cache);

            exprCache.Ability = ParseAbility.Expr;
            exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
            exprCache.LastInsert = exprCache.Insert = checkElem;
        }

        private void ParseCase()
        {
            ParsingLogicCache cache = PeekCache() as ParsingLogicCache;
            if (cache == null || cache.Type != ParsingLogicCache.TYPE_Switch || cache.Step != 2) ThrowParseError();
            cache.Step = 1;
            CaseElement caseElem = new CaseElement();
            AddElement(caseElem, cache.LastElem);
            CaseBeginElement beginElem = new CaseBeginElement();
            beginElem.CaseElem = caseElem;
            AddElement(beginElem, caseElem);
            cache.CaseList.Add(beginElem);
            exprCache.Ability = ParseAbility.Expr;
            exprCache.Flag = EF_CheckLastVisitState | EF_AttachEndChar;
            exprCache.LastInsert = exprCache.Insert = caseElem;
        }

        private void ParseDefault()
        {
            ParsingLogicCache cache = PeekCache() as ParsingLogicCache;
            if (cache == null || cache.Type != ParsingLogicCache.TYPE_Switch || cache.Step != 2) ThrowParseError();
            if (!CheckNextChar(':')) ThrowParseError();
            cache.Step = 3;
            CaseBeginElement beginElem = new CaseBeginElement();
            AddElement(beginElem, cache.LastElem);
            cache.CaseList.Add(beginElem);
        }

        private int CheckParseSwitchEnd(ParsingLogicCache cache, int flag)
        {
            int step = cache.Step;
            if (step == 0)
            {
                if (flag != (ER_Expr | SC_RightBracket1) || !CheckNextChar('{')) ThrowParseError();
                PushLastInsert(cache.LastElem);
                int flag2 = ReadFlag();
                CaseBeginElement beginElem = null;
                if (flag2 == LG_Case)
                {
                    cache.Step = 1;
                    CaseElement caseElem = new CaseElement();
                    AddElement(caseElem, cache.LastElem);
                    beginElem = new CaseBeginElement();
                    beginElem.CaseElem = caseElem;
                    AddElement(beginElem, caseElem);
                    PushNewExprCache(ParseAbility.Expr, caseElem).Flag = EF_CheckLastVisitState | EF_AttachEndChar;
                }
                else if (flag2 == LG_Default)
                {
                    if (!CheckNextChar(':')) ThrowParseError();
                    cache.Step = 3;
                    beginElem = new CaseBeginElement();
                    AddElement(beginElem, cache.LastElem);
                }
                else ThrowParseError();
                cache.CaseList = new List<ElementBase>();
                cache.CaseList.Add(beginElem);
                return ER_Continue;
            }
            else if (step == 1)
            {
                if (flag != (ER_Expr | SC_Condition2)) ThrowParseError();
                cache.Step = 2;
                return ER_Continue;
            }
            else
            {
                if (CheckBlockContinue(true, flag)) return ER_Continue;
                if (!CheckPopLastInsert(cache.LastElem) || !CheckPopCache(cache)) ThrowParseError();
                if (cache.CaseList != null)
                {
                    int index = cache.CaseList.Count - 1;
                    for (int i = index; i >= 0; i--)
                    {
                        CaseBeginElement elem = (CaseBeginElement)cache.CaseList[i];
                        if (elem.CaseElem != null)
                        {
                            CaseBeginElement nextCaseElem = elem.CaseElem.Next as CaseBeginElement;
                            if (nextCaseElem != null)
                                elem.CaseElem.EqualPointer = nextCaseElem.CaseElem == null ? nextCaseElem.Next : nextCaseElem.CaseElem.EqualPointer;
                            else
                                elem.CaseElem.EqualPointer = elem.CaseElem.Next;
                        }
                        if (i > 0 && !(elem.Prev is CaseElement))
                            elem.Prev.Next = elem.CaseElem != null ? elem.CaseElem.EqualPointer : elem.Next;
                        if (elem.CaseElem != null)
                        {
                            if (i < index)
                                LinkElement(elem.CaseElem, cache.CaseList[i + 1].Next);
                            else
                                LinkElement(elem.CaseElem, cache.LastElem);
                        }
                    }
                    for (int i = index; i >= 0; i--)
                        RemoveElement(cache.CaseList[i]);
                }
                RemoveElement(cache.Elem);
                return ER_Logic;
            }
        }

        private int ParseBreakOrContinue(bool isBreak)
        {
            JumpElement elem = new JumpElement();
            AddElement(elem);
            JumpNode lastNode = null;
            ParsingLogicCache cache = null;
            ParsingCacheBase c = PeekCache();
            do
            {
                cache = c as ParsingLogicCache;
                if (cache == null) break;
                switch(cache.Type)
                {
                    case ParsingLogicCache.TYPE_For:
                    case ParsingLogicCache.TYPE_ForIn:
                    case ParsingLogicCache.TYPE_While:
                    case ParsingLogicCache.TYPE_DoWhile:
                        break;
                    case ParsingLogicCache.TYPE_Switch:
                        if (!isBreak)
                        {
                            if (lastNode == null)
                                elem.Path = lastNode = new JumpNode(JumpNode.TYPE_Switch);
                            else
                            {
                                lastNode.Parent = new JumpNode(JumpNode.TYPE_Switch);
                                lastNode = lastNode.Parent;
                            }
                            cache = null;
                        }
                        break;
                    case ParsingLogicCache.TYPE_Try:
                        if (lastNode == null)
                            elem.Path = lastNode = new JumpNode(JumpNode.TYPE_TryFinally);
                        else
                        {
                            lastNode.Parent = new JumpNode(JumpNode.TYPE_TryFinally);
                            lastNode = lastNode.Parent;
                        }
                        cache = null;
                        break;
                    default:
                        cache = null;
                        break;
                }
                if (cache != null) break;
                else
                {
                    c = c.Parent;
                    if (c == null) break;
                }
            } while (true);
            if (cache == null) ThrowParseError();
            if (isBreak) elem.GotoPointer = cache.LastElem;
            else
            {
                switch(cache.Type)
                {
                    case ParsingLogicCache.TYPE_ForIn: elem.GotoPointer = cache.Elem; break;
                    case ParsingLogicCache.TYPE_For: elem.GotoPointer = cache.Elem3; break;
                    case ParsingLogicCache.TYPE_DoWhile: ((LoopCheckElement)cache.Elem).AddContinueElem(elem); break;
                    default: elem.GotoPointer = cache.Elem.Next; break;
                }
            }
            int flag = ReadFlag();
            switch(flag)
            {
                case SC_Semicolon:
                case SC_Comma:
                case SC_RightBracket1:
                case SC_RightBracket2:
                case SC_RightBracket3:
                    break;
                default:
                    if (HasNewLine)
                    {
                        SkipToLine();
                        flag = SC_NewLine;
                        break;
                    }
                    ThrowParseError();
                    break;
            }
            return ER_Expr | flag;
        }

        private void ParseTry()
        {
            if (!CheckNextChar('{')) ThrowParseError();

            TryStartElement startElem = new TryStartElement();
            AddElement(startElem);
            TryEndElement endElem = new TryEndElement();
            AddElement(endElem);

            PushLastInsert(endElem);

            ParsingLogicCache cache = new ParsingLogicCache();
            cache.Type = ParsingLogicCache.TYPE_Try;
            cache.Elem = startElem;
            cache.LastElem = endElem;
            PushCache(cache);
        }

        private int CheckParseTryEnd(ParsingLogicCache cache, int flag)
        {
            if (CheckBlockContinue(true, flag)) return ER_Continue;
            int step = cache.Step;
            TryStartElement tryElem = (TryStartElement)cache.Elem;
            if (step == 0)
            {
                int f = ReadFlag();
                if (f == LG_Catch)
                {
                    string varName;
                    if (!CheckNextChar('(')) ThrowParseError();
                    f = ReadFlag();
                    if (f != CT_Variable) ThrowParseError();
                    varName = GetName();
                    if (!CheckNextChar(')') || !CheckNextChar('{')) ThrowParseError();
                    CatchStartElement catchElem = new CatchStartElement();
                    catchElem.VarName = varName;
                    AddElement(catchElem, cache.LastElem);
                    tryElem.CatchPointer = catchElem;
                    PeekContext().PushCatchVariable(catchElem);
                    cache.Step = 1;
                }
                else if (f == LG_Finally)
                {
                    if (!CheckNextChar('{')) ThrowParseError();
                    FinallyStartElement finallyElem = new FinallyStartElement();
                    AddElement(finallyElem, cache.LastElem);
                    tryElem.FinallyPointer = finallyElem;
                    cache.Step = 2;
                }
                else ThrowParseError();
                return ER_Continue;
            }
            else if (step == 1)
            {
                if (tryElem.CatchPointer != PeekContext().PopCatchVariable()) ThrowParseError();
                int f = ReadFlag();
                if (f == LG_Finally)
                {
                    if (!CheckNextChar('{')) ThrowParseError();
                    FinallyStartElement finallyElem = new FinallyStartElement();
                    AddElement(finallyElem, cache.LastElem);
                    tryElem.FinallyPointer = finallyElem;
                    cache.Step = 2;
                    return ER_Continue;
                }
                RejectIndex();
            }
            if (tryElem.CatchPointer != null)
                tryElem.CatchPointer.Prev.Next = tryElem.FinallyPointer != null ? tryElem.FinallyPointer : cache.LastElem;
            if (!CheckPopLastInsert(cache.LastElem) || !CheckPopCache(cache)) ThrowParseError();
            return ER_Logic;
        }

        private void ParseThrow()
        {
            ThrowElement elem = new ThrowElement();
            AddElement(elem);

            ParsingInnerCache cache = new ParsingInnerCache();
            cache.Type = ParsingInnerCache.TYPE_Throw;
            cache.Elem = elem;
            PushCache(cache);

            exprCache.Ability = ParseAbility.Expr;
            exprCache.Flag = EF_CheckLastVisitState;
            exprCache.LastInsert = exprCache.Insert = elem;
        }

        private int CheckParseThrowEnd(ParsingInnerCache cache, int flag)
        {
            if ((flag & (ER_Expr | ER_EndChar)) == 0) ThrowParseError();
            ThrowElement elem = (ThrowElement)cache.Elem;
            elem.HasError = (flag & ER_Expr) != 0;
            if (!CheckPopCache(cache)) ThrowParseError();
            return ER_Expr | (flag & ER_FF);
        }

        private void ParseReturn()
        {
            ReturnElement elem = new ReturnElement();
            AddElement(elem);

            ParsingInnerCache cache = new ParsingInnerCache();
            cache.Type = ParsingInnerCache.TYPE_Return;
            cache.Elem = elem;
            PushCache(cache);

            exprCache.Ability = ParseAbility.Expr;
            exprCache.Flag = EF_CheckLastVisitState;
            exprCache.LastInsert = exprCache.Insert = elem;
        }

        private int CheckParseReturnEnd(ParsingInnerCache cache, int flag)
        {
            if ((flag & (ER_Expr | ER_EndChar)) == 0) ThrowParseError();
            ReturnElement elem = (ReturnElement)cache.Elem;
            elem.HasResult = (flag & ER_Expr) != 0;
            if (!CheckPopCache(cache)) ThrowParseError();
            return ER_Expr | (flag & ER_FF);
        }

        private int ParseDebugger()
        {
            DebuggerElement elem = new DebuggerElement();
            AddElement(elem);
            return CheckNextChar(';', true) ? ER_Expr | SC_Semicolon : ER_Expr | SC_NewLine;
        }

        #endregion

        #region 解析表达式

        private void ParseRegExpr()
        {
            int i1 = keyIndex + 1;
            int len = text.Length;
            int i;
            for (i = i1; i < len; i++)
            {
                char ch = text[i];
                if (ch == '\\') i++;
                else if (ch == '/') break;
                else if (ch == '\n') break;
            }
            if (i == len || text[i] != '/')
            {
                keyLength = i - keyIndex;
                ThrowParseError();
            }
            else
            {
                string str = text.Substring(i1, i - i1);
                int type = 0;
                if (++i < len)
                {
                    bool c = true;
                    do
                    {
                        switch (text[i])
                        {
                            case 'g': type |= RegExprElement.TYPE_Global; break;
                            case 'i': type |= RegExprElement.TYPE_IgnoreCase; break;
                            case 'm': type |= RegExprElement.TYPE_MultiLine; break;
                            default: c = false; break;
                        }
                    } while (c && ++i < len);
                }
                keyLength = i - keyIndex;
                AddElement(new RegExprElement(type, str));
            }
        }

        //返回值：1-固定值，2-变量，0x1000-操作符，0x2000-遇到结束符（“;”“,”“{”(块开始符)），-1-遇到“{”(对象开始符)，-2遇到“[”，-3遇到“(”，-4-方法定义，-8-块起始符、流程控制（if...else...、for、do、while、switch、case）
        private int ParseOperand(ParseAbility ability)
        {
            int flag = ReadFlag();
            if (flag == FG_NONE) return 0;
            if (flag <= OPT_End)
            {
                switch (flag)
                {
                    case OPT_Increment:
                    case OPT_Decrement: // todo: 自增
                    case OPT_Typeof: // todo: 类型
                    case OPT_New:
                    case OPT_Delete:
                    case OPT_LogicNot:
                    case OPT_BitNot:
                        break;
                    case OPT_Substract: // todo: 负数
                        flag = OPT_Negative;
                        break;
                    case OPT_Divide: // todo: 正则表达式
                        ParseRegExpr();
                        return 1;
                    default:
                        ThrowParseError();
                        break;
                }
                return 0x1000 | flag;
            }
            else if (flag <= CT_End)
            {
                ScriptObjectBase item = null;
                switch (flag)
                {
                    case CT_Number64:
                        {
                            long value = Convert.ToInt64(GetName(), 16);
                            item = ScriptNumber.Create(value);
                            break;
                        }
                    case CT_Number:
                        {
                            long value = long.Parse(GetName());
                            item = ScriptNumber.Create(value);
                            break;
                        }
                    case CT_Decimal:
                        {
                            decimal value = decimal.Parse(GetName());
                            item = ScriptNumber.Create(value);
                            break;
                        }
                    case CT_String:
                    case CT_EscapeString:
                        {
                            string value = GetString(flag == CT_EscapeString);
                            item = ScriptString.Create(value);
                            break;
                        }
                    case CT_Undefined:
                        item = ScriptUndefined.Instance;
                        break;
                    case CT_Null:
                        item = ScriptNull.Instance;
                        break;
                    case CT_True:
                    case CT_False:
                        item = ScriptBoolean.Create(text[keyIndex] == 't');
                        break;
                    case CT_NaN:
                        item = ScriptNumber.NaN;
                        break;
                    case CT_Infinity:
                        item = ScriptNumber.Infinity;
                        break;
                    case CT_Variable:
                        {
                            string varName = GetName();
                            CatchStartElement catchElem = PeekContext().FindCatchVariable(varName);
                            ElementBase varElem;
                            if (catchElem != null)
                                varElem = new CatchVariableElement(catchElem);
                            else
                                varElem = new VariableElement(evalContext, varName, PeekContext().NewVarIndex());
                            AddElement(varElem);
                            return 2;
                        }
                    case CT_This:
                        {
                            ThisElement elem = new ThisElement();
                            AddElement(elem);
                            return 2;
                        }
                    case CT_Function:
                        return -4;
                }
                if (item == null) ThrowParseError();
                ConstElement element = new ConstElement(item);
                AddElement(element);
                return 1;
            }
            else if (flag <= LG_End)
            {
                if (flag == LG_VarDefine)
                {
                    if ((ability & ParseAbility.Declare) == ParseAbility.None) ThrowParseError();
                }
                else if ((ability & ParseAbility.Flow) == ParseAbility.None)
                    ThrowParseError();
                return -flag;
            }
            else
            {
                switch (flag)
                {
                    case SC_LeftBracket1: return -3;
                    case SC_LeftBracket2: return -2;
                    case SC_LeftBracket3:
                        {
                            if (exprCache.Step == STP_First)
                            {
                                bool isObject = false;
                                long indexInfo = SuspendIndex();
                                switch (ReadFlag())
                                {
                                    case CT_Variable:
                                    case CT_String:
                                    case CT_EscapeString:
                                        SkipEmptyAndComment(false);
                                        if (text[keyIndex] == ':') isObject = true;
                                        break;
                                }
                                ResumeIndex(indexInfo);
                                if (!isObject)
                                {
                                    if ((ability & ParseAbility.BlockStart) == 0) ThrowParseError();
                                    return -8;
                                }
                            }
                            return -1;
                        }
                    case SC_RightBracket1:
                    case SC_RightBracket2:
                    case SC_RightBracket3:
                        if (exprCache.Step != STP_First) ThrowParseError();
                        return 0x2000 | flag;
                    case SC_Comma:
                    case SC_Semicolon:
                        return 0x2000 | flag;
                }
            }
            ThrowParseError();
            return 0;
        }

        //返回值：0-结束符（没有读到操作符，读到换行），0x1000-表示读取到正常的操作符，0x2000-表示读取到需要特殊处理的操作符（如：(、[、?、: ）
        private int ParseOperator()
        {
            int flag = ReadFlag();
            if (flag == FG_NONE) return SC_NewLine;
            if (flag <= OPT_End)
            {
                return 0x1000 | (flag - OPT_Start);
            }
            else if (flag >= SC_Start)
            {
                int optType = -1;
                switch (flag)
                {
                    case SC_LeftBracket1: optType = OPT_InvokeMethod; break;
                    case SC_LeftBracket2: optType = OPT_GetArrayMember; break;
                    case SC_Condition: optType = flag; break;
                }
                return optType > 0 ? (0x2000 | optType) : flag;
            }
            else if (HasNewLine)
            {
                SkipToLine();
                return SC_NewLine;
            }
            ThrowParseError();
            return 0;
        }

        private int ParseExpression()
        {
            exprCache = PopCache<ParsingExprCache>();
            if (exprCache == null)
            {
                exprCache = CreateExprCache(true);
                exprCache.Ability = ParseAbility.All;
            }
            bool continued;
            int flag2;
            do
            {
                continued = false;
                int flag = 0;
                flag2 = 0; //-1:一开始就读取到结束字符，0:普通表达式结束，1:{，2:[，3(，4:方法定义、5:方法调用、6:数组调用、7:条件表达式（?）、8:块起始符、流程控制（if...else...、for、do、while、switch、case）
                if ((exprCache.Step & STP_ReadOperand) != 0)
                {
                    exprCache.Step &= STP_ParseState;
                    if (exprCache.Step < STP_ParseOperator)
                    {
                        if (exprCache.Step == STP_ParseOperand)
                            exprCache.CheckLastOperandVisitState();
                        exprCache.Step = STP_ParseOperator;
                    }
                }
                do
                {
                    if (exprCache.Step < STP_ParseOperator)
                    {
                        flag = ParseOperand(exprCache.Ability);
                        if (flag == 0)
                        {
                            if (exprCache.Step != STP_First) ThrowParseError();
                            exprCache = null;
                            return ER_None;
                        }
                        if (flag < 0)
                        {
                            flag2 = -flag;
                            if (exprCache.Step != STP_First && flag2 >= LG_Start && flag2 <= LG_End) ThrowParseError();
                            break;
                        }
                        else if ((flag & 0x1000) != 0)
                        {
                            OperatorElementBase elem = OperatorElementBase.Create((OperatorType)(flag & (~0x1000)), false);
                            AddElement(elem);
                            CheckAddAssignOperatorCallback(elem);
                            if (exprCache.Step == STP_ParseOperand)
                                exprCache.CheckLastOperandVisitState();
                            exprCache.Insert = elem;
                            exprCache.Step = STP_ParseOperand;
                            continue;
                        }
                        else if ((flag & 0x2000) != 0)
                        {
                            flag2 = flag & (~0x2000);
                            flag2 = -(exprCache.Step == STP_First ? (ER_EndChar | flag2) : (ER_Expr | flag2));
                            break;
                        }
                        if (exprCache.Step == STP_ParseOperand)
                            exprCache.CheckLastOperandVisitState();
                        exprCache.Step = STP_ParseOperator;
                    }
                    if (exprCache.Step == STP_ParseOperator)
                    {
                        int optFlag = ParseOperator();
                        if ((optFlag & 0x1000) != 0)
                        {
                            OperatorType optType = (OperatorType)(optFlag & (~0x1000));
                            CheckOperatorLevel(optType);
                            OperatorElementBase elem = OperatorElementBase.Create(optType, true);
                            if (optType == OperatorType.GetObjectMember)
                            {
                                int nameFlag = ReadFlag();
                                if (nameFlag != CT_Variable) ThrowParseError();
                                GetObjectMemberElement getObjectElem = ((GetObjectMemberElement)elem);
                                getObjectElem.Name = GetName();
                                getObjectElem.SetVarIndex(evalContext, PeekContext().NewVarIndex());
                                AddElement(elem);
                                elem.ResultVisit = elem.Prev.ResultVisit;
                                elem.Prev.ResultVisit = ResultVisitFlag.Get;
                                exprCache.Insert = elem.Next;
                                exprCache.Step = STP_ParseOperator;
                                continue;
                            }
                            else if (optType == OperatorType.Increment || optType == OperatorType.Decrement)
                            {
                                AddElement(elem);
                                CheckAssignOperatorValid(elem);
                                elem.ResultVisit = elem.Prev.ResultVisit;
                                elem.Prev.ResultVisit = elem.ArgusResultVisit;
                                exprCache.Insert = elem.Next;
                                exprCache.Step = STP_ParseOperator;
                                continue;
                            }
                            else if (optType == OperatorType.LogicOr || optType == OperatorType.LogicAnd)
                            {
                                AddElement(elem);
                                LogicFirstElement firstElem = (LogicFirstElement)elem;
                                LogicSecondElement secondElem = new LogicSecondElement(firstElem.IsOr);
                                firstElem.SecondPointer = secondElem;
                                AddElement(secondElem);
                                secondElem.ResultVisit = elem.Prev.ResultVisit;
                                elem.Prev.ResultVisit = elem.ArgusResultVisit;
                                exprCache.Insert = secondElem;
                            }
                            else
                            {
                                AddElement(elem);
                                CheckAssignOperatorValid(elem);
                                elem.ResultVisit = elem.Prev.ResultVisit;
                                exprCache.Insert = elem;
                                exprCache.CheckLastOperandVisitState();
                            }
                        }
                        else if ((optFlag & 0x2000) != 0)
                        {
                            switch (optFlag & (~0x2000))
                            {
                                case OPT_InvokeMethod: flag2 = 5; break;
                                case OPT_GetArrayMember: flag2 = 6; break;
                                case SC_Condition: flag2 = 7; break;
                                default: ThrowParseError(); break;
                            }
                            break;
                        }
                        else
                        {
                            flag2 = -(ER_Expr | optFlag);
                            break;
                        }
                        exprCache.Step = STP_ParseOperand;
                    }
                } while (true);
                if (flag2 > 0)
                {
                    switch (flag2)
                    {
                        case 1:
                            ParseObjectStart();
                            continued = true;
                            break;
                        case 2:
                            ParseArrayStart();
                            continued = true;
                            break;
                        case 3:
                            ParseInnerExprStart();
                            continued = true;
                            break;
                        case 4:
                            ParseFunctionDefined();
                            flag2 = ER_Continue;
                            break;
                        case 5:
                            ParseInvokeMethod();
                            continued = true;
                            break;
                        case 6:
                            ParseGetArrayMember();
                            continued = true;
                            break;
                        case 7:
                            ParseCondition();
                            continued = true;
                            break;
                        case 8:
                            ParseBlockStart();
                            flag2 = ER_Continue;
                            break;
                        case LG_VarDefine:
                            flag2 = ParseVarStart();
                            if (flag2 < 0) continued = true;
                            break;
                        case LG_If:
                            ParseIf();
                            continued = true;
                            break;
                        case LG_For:
                            ParseFor();
                            continued = true;
                            break;
                        case LG_While:
                            ParseWhile();
                            continued = true;
                            break;
                        case LG_Do:
                            ParseDoWhile();
                            flag2 = ER_Continue;
                            break;
                        case LG_Switch:
                            ParseSwitch();
                            continued = true;
                            break;
                        case LG_Case:
                            ParseCase();
                            continued = true;
                            break;
                        case LG_Default:
                            ParseDefault();
                            flag2 = ER_Continue;
                            break;
                        case LG_Continue:
                        case LG_Break:
                            flag2 = ParseBreakOrContinue(flag2 == LG_Break);
                            break;
                        case LG_Try:
                            ParseTry();
                            flag2 = ER_Continue;
                            break;
                        case LG_Return:
                            ParseReturn();
                            continued = true;
                            break;
                        case LG_Debugger:
                            flag2 = ParseDebugger();
                            break;
                        case LG_Throw:
                            ParseThrow();
                            continued = true;
                            break;
                        default:
                            ThrowParseError();
                            break;
                    }
                }
                else
                {
                    flag2 = -flag2;
                    break;
                }
            } while (continued);
            if ((flag2 & ER_Expr) != 0)
            {
                if ((exprCache.Flag & EF_CheckLastVisitState) != 0)
                    exprCache.LastInsert.Prev.ResultVisit = exprCache.LastInsert.ArgusResultVisit;
                int flag3 = exprCache.Flag & EF_EndCharFlag;
                if (flag3 != EF_AttachEndChar)
                {
                    switch (flag2 & (~ER_Expr))
                    {
                        case SC_Condition2:
                        case SC_RightBracket1:
                        case SC_RightBracket2:
                        case SC_RightBracket3:
                            RejectIndex();
                            flag2 = ER_Expr | SC_NewLine;
                            break;
                        case SC_Semicolon:
                        case SC_Comma:
                        case SC_NewLine:
                            if (flag3 == EF_IgnoreEndChar)
                            {
                                RejectIndex();
                                flag2 = ER_Expr | SC_NewLine;
                            }
                            break;

                    }
                }
            }
            exprCache = null;
            return flag2;
        }

        private void ParseBlock()
        {
            int flag;
            do
            {
                flag = ParseExpression();
                if (flag != ER_Continue)
                {
                    ParsingCacheBase cache = PeekCache();
                    if (flag == ER_None)
                    {
                        if (cache != null) ThrowParseError();
                        break;
                    }
                    while (cache != null)
                    {
                        flag = cache.CheckExprEnd(this, flag);
                        if (flag == ER_Continue) break;
                        else cache = PeekCache();
                    }
                }
            } while (true);
        }

        #endregion

        #region 其它

        // flag: 1-是否缓存最后一个元素的值；2-是否添加return元素
        internal static ScriptParser ParseForEval(ScriptContext context, string script, long beginObjectId, int flag)
        {
            ScriptParser parser = new ScriptParser(script);
            parser.beginObjectId = beginObjectId;
            parser.evalContext = context;
            parser.contextCounter = -2;
            parser.contextStack = parser.CreateContext(null, null);
            parser.contextStack.ContextIndex = -1;
            parser.contextStack.ObjectId = parser.NewObjectId();
            parser.contextStack.DisableVariable();
            parser.ParseBlock();
            parser.contextStack.FinishParsing();
            if ((flag & 1) != 0)
            {
                ElementBase last = parser.contextStack.Last;
                if (last != null && last.AllowGetLastResult) last.ResultVisit = ResultVisitFlag.Get;
                else
                {
                    last = new ConstElement(ScriptUndefined.Instance) { ResultVisit = ResultVisitFlag.Get };
                    parser.AddElement(last);
                }
                if ((flag & 2) != 0)
                    parser.AddElement(new ReturnElement() { HasResult = true });
            }
            return parser;
        }

        internal DefineContext Context { get { return contextStack; } }

        internal long BeginObjectId { get { return beginObjectId; } }

        private static void InternalExecute(ScriptContext context, ScriptExecuteContext rootContext)
        {
            if (context.CurrentContext.Current != null)
            {
                do
                {
                    try
                    {
                        context.CurrentContext.Current.Execute(context);
                    }
                    catch (Exception ex)
                    {
                        if (!context.CheckMoveCatch(ex)) throw;
                    }
                    if (context.CurrentContext.Current == null)
                    {
                        bool finished = false;
                        do
                        {
                            if (context.CurrentContext == rootContext)
                            {
                                finished = true;
                                break;
                            }
                            ScriptExecuteContext prevContext = context.PopContext();
                            if (prevContext.ResultVisit != ResultVisitFlag.None)
                                context.CurrentContext.PushVariable(prevContext.IsNewObject ? prevContext.ThisObject : prevContext.Result);
                        } while (context.CurrentContext.Current == null);
                        if (finished) break;
                    }
                } while (true);
            }
        }

        internal static void InnerExecute(ScriptContext context, ScriptExecuteContext rootContext)
        {
            ScriptContext old = ScriptContext.Current;
            ScriptContext.Current = context;
            try
            {
                InternalExecute(context, rootContext);
            }
            finally
            {
                ScriptContext.Current = old;
            }
        }

        #endregion

        #endregion

        #region 公共方法

        public static ScriptParser Parse(string script)
        {
            ScriptParser parser = new ScriptParser(script);
            parser.contextStack = parser.CreateContext(null, null);
            parser.contextStack.ObjectId = parser.NewObjectId();
            parser.contextStack.ContextIndex = parser.contextCounter++;
            parser.ParseBlock();
            parser.contextStack.FinishParsing();
            return parser;
        }

        public void Execute(ScriptContext context)
        {
            if (context == null) context = new ScriptContext();
            RootExecuteContext rootContext = new RootExecuteContext(context);
            this.PeekContext().CreateExecuteContext(rootContext, null);
            Thread thread = Thread.CurrentThread;
            if (Interlocked.CompareExchange<Thread>(ref context.executingThread, thread, null) != null)
                throw new ScriptExecuteException("脚本的上下文对象不允许多线程执行。");
            ScriptContext old = ScriptContext.Current;
            ScriptContext.Current = context;
            try
            {
                context.Init(contextCounter, beginObjectId, rootContext);
                InternalExecute(context, rootContext);
            }
            finally
            {
                context.Finish();
                ScriptContext.Current = old;
                Interlocked.CompareExchange(ref context.executingThread, null, thread);
            }
        }

        #endregion
    }

    public enum OperatorType : int
    {
        None,
        // 赋值（=），|=，^=，&=，+=，-=，*=，/=，%=，<<=，>>=，>>>=
        Assign, BitOrAssign, BitXOrAssign, BitAndAssign, AddAssign, SubstractAssign, MultiplyAssign, DivideAssign, ModulusAssign, ShiftLeftAssign, ShiftRightAssign, UnsignedShiftRightAssign,
        // 逻辑或（||）
        LogicOr,
        // 逻辑与（&&）
        LogicAnd,
        // 按位或（|）
        BitOr,
        // 按位异或（^）
        BitXOr,
        // 按位与（&）
        BitAnd,
        // 值相等（==），值不等（!=），完全相等（===），完全不相等（!==）
        EqualsValue, NotEqualsValue, Equals, NotEquals,
        // 小于（<），小于等于（<=），大于（>），大于等于（>=）, In操作符，instanceof
        Less, LessEquals, Greater, GreaterEquals, In, InstanceOf,
        // 左移位（<<），右移位（>>），无符号右移位（>>>）
        ShiftLeft, ShiftRight, UnsignedShiftRight,
        // 加（+），减（-）
        Add, Substract,
        // 乘（*），除（/），取模（%）
        Multiply, Divide, Modulus,
        // 自增（++），自减（--），负（-），取反（~），非（!），delete，new，typeof
        Increment, Decrement, Negative, BitNot, LogicNot, Delete, New, Typeof,
        // 取成员（.），取数组元素（[]），调用方法（()），
        GetObjectMember, GetArrayMember, InvokeMethod,
    }
}
