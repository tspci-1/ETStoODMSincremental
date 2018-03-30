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
