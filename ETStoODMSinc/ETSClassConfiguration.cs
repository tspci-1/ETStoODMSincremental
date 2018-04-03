using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETStoODMSIncremental
{
	class ETSClassConfiguration
    {
        private bool inputOnly;
        public bool InputOnly
        {
            get { return inputOnly; }
        }

        private bool isMethod;
        public bool IsMethod
        {
            get { return isMethod; }
        }

		private string className;
		public string ClassName
		{
			get { return className; }
		}

		private string classMappingName;
        public string ClassMappingName
        {
            get { return classMappingName; }
        }

        private bool newUndefinedClass;
        public bool NewUndefinedClass
        {
            get { return newUndefinedClass; }
        }

        private bool headerError;
		private bool mappingRowError;
		private bool contentError;

		private List<Assignment> assignments;
        public List<Assignment> Assignments
        {
            get
            {
                return assignments;
            }
        }

        private List<Calculation> calculations;
        public List<Calculation> Calculations
        {
            get
            {
                return calculations;
            }
        }
 
		private List<Extension> extensions;
        public List<Extension> Extensions
        {
            get
            {
                return extensions;
            }
        }

        private List<Logic> logicExpressions;
        public List<Logic> LogicExpressions
        {
            get
            {
                return logicExpressions;
            }
        }

        private List<string> parameters;
        public List<string> Parameters
        {
            get
            {
                return parameters;
            }
        }
        public string GetMethodParameter(int index)
        {
            string returnValue = String.Empty;

            if (index <= parameters.Count)
            {
                returnValue = parameters[index];
            }

            return returnValue;
        }

        private List<string> parameterType;
        public List<string> ParameterType
        {
            get { return parameterType; }
        }
        public string GetMethodParameterType(int index)
        {
            string returnValue = String.Empty;

            if (index <= parameterType.Count)
            {
                returnValue = parameterType[index];
            }

            return returnValue;
        }
        // constructor
		public ETSClassConfiguration(string className, string[] headerContents)
		{
			headerError = false;
			mappingRowError = false;
			contentError = false;
            newUndefinedClass = false;

            if (className.ToLower().Contains("method"))
            {
                isMethod = true;

                string[] nameSplit = className.Split(new char[] { ' ' });

                if (nameSplit.Length != 2)
                {
                    contentError = true;
                }
                else
                {
                    this.className = nameSplit[1];
                }

                ValidateMethodHeader(headerContents);
            }
            else
            {
                this.className = className;

                ValidateHeader(headerContents);
            }

			assignments = new List<Assignment>();
			calculations = new List<Calculation>();
			extensions = new List<Extension>();
			logicExpressions = new List<Logic>();
            parameters = new List<string>();
            parameterType = new List<string>();
		}

		void ValidateHeader(string[] headerContents)
		{
			if (!(headerContents[attributeColumnIndex] == attribute &&
				  headerContents[operationColumnIndex] == operation &&
				  headerContents[destinationColumnIndex] == destination &&
				  headerContents[etsOnlyColumnIndex] == etsOnly))
			{
				headerError = true;
			}
		}

        void ValidateMethodHeader(string[] headerContents)
        {
            if (!(headerContents[methodNameColumnIndex].Length > 0 &&
                  headerContents[parameterColumnIndex] == parameter &&
                  headerContents[parameterDescriptionColumnIndex] == parameterDescription))
            {
                headerError = true;
            }
        }

        bool HeaderError()
		{
			return headerError;
		}

		public void ClassMapping(string[] secondRow)
		{
			if (secondRow[operationColumnIndex] == operationKeywords[classMappingIndex] &&
				secondRow[destinationColumnIndex] != "")
			{
				classMappingName = secondRow[destinationColumnIndex];

                inputOnly = secondRow[attributeColumnIndex] == "InputOnly";
            }
			else
				mappingRowError = true;
		}

		bool SecondRowError()
		{
			return mappingRowError;
		}

		public void AddContent(string[] contentRow)
		{
			string operation = contentRow[operationColumnIndex];

            if (operation == operationKeywords[assignmentIndex])
            {
                string firstCell = contentRow[0];

                if (firstCell.Contains("rdf:") &&
                    (firstCell.LastIndexOf('.') == firstCell.IndexOf('.')))
                {
                    newUndefinedClass = true;
                }
                else
                {
                    assignments.Add(new Assignment(contentRow));
                }
			}
			else if (operation == operationKeywords[calculationIndex])
			{
				calculations.Add(new Calculation(contentRow[destinationColumnIndex]));
			}
			else if (operation == operationKeywords[extensionIndex])
			{
				extensions.Add(new Extension(contentRow[attributeColumnIndex], contentRow[destinationColumnIndex]));
			}
			else if (operation.ToLower().Contains("if"))
			{
				logicExpressions.Add(new Logic(contentRow[attributeColumnIndex], contentRow[operationColumnIndex], contentRow[destinationColumnIndex]));
			}
            else if (operation == operationKeywords[createIndex])
            {
                classMappingName = contentRow[destinationColumnIndex];
            }
			else
			{
				contentError = true;
			}
		}

        public bool AddMethodParameter(string[] contentRow)
        {
            bool parameterFound = true;
 
            if (!(contentRow[attributeColumnIndex] == attribute &&
                  contentRow[operationColumnIndex] == operation &&
                  contentRow[destinationColumnIndex] == destination))
            {
                string [] parameterSplit = contentRow[destinationColumnIndex].Split(new char[] { ' ' });

                parameters.Add(parameterSplit[0]);
                parameterType.Add(parameterSplit[1]);

                parameterFound = true;
            }
            else
            {
                parameterFound = false;
            }

            return parameterFound;
        }

        public List<string> GetListOfNeededAttributes()
		{
			List<string> neededAttributes = new List<string>();

			foreach (Assignment assignment in assignments)
			{
				string attribute = assignment.Attribute;

				if (!neededAttributes.Contains(attribute))
				{
                    //
                    if (attribute[0] != ':')
                        //
					neededAttributes.Add(attribute);
				}
			}

            foreach (Calculation calculation in calculations)
            {
                foreach (string operand in calculation.Operands)
                {
                    if (operand.Contains('.'))
                    {
                        if (!neededAttributes.Contains(operand))
                        {
                            neededAttributes.Add(operand);
                        }
                    }
                }
            }
			return neededAttributes;
		}

        public Dictionary<string, bool> GetAttributeIsRDFDictionary()
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();

            foreach (Assignment assignment in assignments)
            {
                if (dictionary.ContainsKey(assignment.Attribute))
                {
                    Utils.WriteTimeToConsoleAndLog(String.Format("Found duplicate assignment: {0} - ignoring", assignment.Attribute));
                }
                else
                {
                    //
                    if (assignment.Attribute[0] != ':')
                        //
                    dictionary.Add(assignment.Attribute, assignment.AttributeIsRDF);
                }
            }

            foreach (Calculation calculation in calculations)
            {
                foreach (string operand in calculation.Operands)
                {
                    if (operand.Contains('.'))
                    {
                        if (!dictionary.ContainsKey(operand))
                        {
                            dictionary.Add(operand, false);
                        }
                    }
                }
            }

            return dictionary;
		}

		static string attribute = "Attribute";
		static string operation = "Operation";
		static string destination = "Destination";
		static string etsOnly = "ETS Only";

        static string parameter = "Parameter";
        static string parameterDescription = "ParameterDescription";

		public static int attributeColumnIndex = 0;
		public static int operationColumnIndex = 1;
		public static int destinationColumnIndex = 2;
		public static int etsOnlyColumnIndex = 3;

        public static int methodNameColumnIndex = 0;
        public static int parameterColumnIndex = 1;
        public static int parameterDescriptionColumnIndex = 2;

        static string[] operationKeywords =
			new string[] { "ClassMapping", "Assignment", "Calculation", "Extension", "Create" };

		static int classMappingIndex = 0;
		static int assignmentIndex = 1;
		static int calculationIndex = 2;
		static int extensionIndex = 3;
        static int createIndex = 4;
	}
}
