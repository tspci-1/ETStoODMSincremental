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
    class Calculation
    {
        string calculationText;

        string target;
        public string Target
        {
            get
            {
                return target;
            }
        }

        string equation;
        public string Equation
        {
            get
            {
                return equation;
            }
        }

        string[] operands;
        public string [] Operands
        {
            get
            {
                return operands;
            }
        }

        bool errorFound;
        public bool ErrorFound
        {
            get
            {
                return errorFound;
            }
        }

		public Calculation(string calculationText)
		{
            char[] operatorList = new char[] { '+', '-', '(', ')', '/', '\\', '^' };

			this.calculationText = calculationText.Trim();

            string[] targetEquation = calculationText.Split(new char[] { '=' });

            if (targetEquation.Length != 2)
            {
                Utils.WriteTimeToConsoleAndLog(String.Format("Bad calculation found: {0}", calculationText));
                errorFound = true;
            }
            else
            {
                target = targetEquation[0].Trim();
                equation = targetEquation[1].Trim();

                operands = targetEquation[1].Split(operatorList);

                for (int operandIndex = 0; operandIndex < operands.Length; operandIndex++)
                {
                    operands[operandIndex] = operands[operandIndex].Trim();
                }
            }
		}
	}
}
