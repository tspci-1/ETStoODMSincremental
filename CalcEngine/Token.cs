/*
 
Copyright© 2018 Project Consultants, LLC
 
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.”
 
*/

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;

namespace CalcEngine
{
    /// <summary>
	/// Represents a node in the expression tree.
    /// </summary>
    internal class Token
	{
        // ** fields
		public TKID ID;
        public TKTYPE Type;
        public object Value;

        // ** ctor
        public Token(object value, TKID id, TKTYPE type)
        {
            Value = value;
            ID = id;
			Type = type;
		}
    }
    /// <summary>
    /// Token types (used when building expressions, sequence defines operator priority)
    /// </summary>
    internal enum TKTYPE
    {
        COMPARE,	// < > = <= >=
        ADDSUB,		// + -
        MULDIV,		// * /
        POWER,		// ^
        GROUP,		// ( ) , .
        LITERAL,	// 123.32, "Hello", etc.
        IDENTIFIER  // functions, external objects, bindings
    }
    /// <summary>
    /// Token ID (used when evaluating expressions)
    /// </summary>
    internal enum TKID
    {
        GT, LT, GE, LE, EQ, NE, // COMPARE
        ADD, SUB, // ADDSUB
        MUL, DIV, DIVINT, MOD, // MULDIV
        POWER, // POWER
        OPEN, CLOSE, END, COMMA, PERIOD, // GROUP
        ATOM, // LITERAL, IDENTIFIER
    }
}
