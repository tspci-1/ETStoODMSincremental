using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETStoODMSIncremental
{
	class ETSClassConfiguration
	{
		private string className;
		public string ClassName
		{
			get { return className; }
		}

		private string classMappingName;
        public string ClassMappingName
        {
            get
            {
                return classMappingName;
            }
        }

        private bool newUndefinedClass;
        public bool NewUndefinedClass
        {
            get
            {
                return newUndefinedClass;
            }
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

		public ETSClassConfiguration(string className, string[] headerContents)
		{
			headerError = false;
			mappingRowError = false;
			contentError = false;
            newUndefinedClass = false;

			this.className = className;

			ValidateHeader(headerContents);

			assignments = new List<Assignment>();
			calculations = new List<Calculation>();
			extensions = new List<Extension>();
			logicExpressions = new List<Logic>();
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

		bool HeaderError()
		{
			return headerError;
		}

		public void ClassMapping(string[] secondRow)
		{
			if (secondRow[attributeColumnIndex] == "" &&
				secondRow[operationColumnIndex] == operationKeywords[classMappingIndex] &&
				secondRow[destinationColumnIndex] != "")
			{
				classMappingName = secondRow[destinationColumnIndex];
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
				logicExpressions.Add(new Logic(contentRow[attributeColumnIndex], contentRow[operationColumnIndex]));
			}
			else
			{
				contentError = true;
			}
		}

		public List<string> GetListOfNeededAttributes()
		{
			List<string> neededAttributes = new List<string>();

			foreach (Assignment assignment in assignments)
			{
				string attribute = assignment.Attribute;

				if (!neededAttributes.Contains(attribute))
				{
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

		public static int attributeColumnIndex = 0;
		public static int operationColumnIndex = 1;
		public static int destinationColumnIndex = 2;
		public static int etsOnlyColumnIndex = 3;

		static string[] operationKeywords =
			new string[] { "ClassMapping", "Assignment", "Calculation", "Extension" };

		static int classMappingIndex = 0;
		static int assignmentIndex = 1;
		static int calculationIndex = 2;
		static int extensionIndex = 3;
	}
}
