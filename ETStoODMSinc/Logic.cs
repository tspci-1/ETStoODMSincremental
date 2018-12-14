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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETStoODMSIncremental
{
	class Logic
	{
		private string attribute;
        public string Attribute
        {
            get { return attribute; }
        }

        private string destination;
        public string Destination
        {
            get { return destination; }
        }

		private string logicExpression;
        private string operandExpression;
        private string operand;
        private string operandLiteral;

        private string method;
        public string Method
        {
            get { return method; }
        }

        private string argumentExpression;
        private string[] splitArguments;

        private List<string> arguments;
        public List<string> Arguments
        {
            get { return arguments; }
        }

        public string GetArgument(int argumentPosition) // for first argument use 1, etc.
        {
            string wantedArgument;

            if (argumentPosition > arguments.Count || argumentPosition < 1)
            {
                wantedArgument = String.Empty;
            }
            else
            {
                wantedArgument = arguments[argumentPosition - 1];
            }

            return wantedArgument;
        }

		public Logic(string attribute, string logicExpression, string destination)
		{
			this.attribute = attribute;
			this.logicExpression = logicExpression;
            this.destination = destination;

            arguments = new List<string>();

            ProcessExpression();
		}

        private void ProcessExpression()
        {
            int positionFirstLeftParenthesis;
            int positionFirstRightParenthesis;
            int positionSecondLeftParenthesis;
            int positionSecondRightParenthesis;

            positionFirstLeftParenthesis = logicExpression.IndexOf('(');
            positionFirstRightParenthesis = logicExpression.IndexOf(')');

            operandExpression = logicExpression.Substring(positionFirstLeftParenthesis + 1, 
                                                          positionFirstRightParenthesis - positionFirstLeftParenthesis - 1)
                                                          .Trim();

            string[] splitOperandExpression = operandExpression.Split(new char[] { ' ' });

            operand = splitOperandExpression[1];

            if (splitOperandExpression.Length == 3)
            {
                operandLiteral = splitOperandExpression[2];
            }
            else
            {
                operandLiteral = string.Empty;
            }

            positionSecondLeftParenthesis = logicExpression.Substring(positionFirstRightParenthesis).IndexOf('(') + positionFirstRightParenthesis;

            method = logicExpression.Substring(positionFirstRightParenthesis + 1,
                                               positionSecondLeftParenthesis - positionFirstRightParenthesis - 1).Trim();

            positionSecondRightParenthesis = logicExpression.Substring(positionSecondLeftParenthesis).IndexOf(')') + positionSecondLeftParenthesis;

            argumentExpression = logicExpression.Substring(positionSecondLeftParenthesis + 1,
                                                           positionSecondRightParenthesis - positionSecondLeftParenthesis - 1)
                                                           .Trim();

            splitArguments = argumentExpression.Split(new char[] { ',' });
            
            for (int index = 0; index < splitArguments.Length; index++)
            {
                arguments.Add(splitArguments[index].Trim());
            }
        }

        public bool LogicTestPasses(string testValue)
        {
            bool passed = false;

            if (operand == "=")
            {
                passed = testValue == operandLiteral;
            }
            else if (operand == "EXISTS")
            {
                passed = testValue != string.Empty;
            }

            return passed;
        }
	}
}
