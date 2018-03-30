using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETStoODMSIncremental
{
	class Assignment
	{
		string attribute;
		string operation;
		string destination;

		bool attributeIsRDF;
		bool destinationIsRDF;
		bool rdfError;

		public string Attribute
		{
			get { return attribute; }
		}
		public string Operation
		{
			get { return operation; }
		}
		public string Destination
		{
			get { return destination; }
		}
		public bool AttributeIsRDF
		{
			get { return attributeIsRDF; }
		}
		public bool DestinationIsRDF
		{
			get { return destinationIsRDF; }
		}
		public bool RDFError
		{
			get { return rdfError; }
		}

		public Assignment(string[] contentRow)
		{
			DetermineIfRDFPresent(contentRow[ETSClassConfiguration.attributeColumnIndex], out attribute, out attributeIsRDF);

			operation = contentRow[ETSClassConfiguration.operationColumnIndex];

			DetermineIfRDFPresent(contentRow[ETSClassConfiguration.destinationColumnIndex], out destination, out destinationIsRDF);

			rdfError = attributeIsRDF != destinationIsRDF;
		}

		private void DetermineIfRDFPresent(string a, out string attrib, out bool present)
		{
			char dot = '.';

            if (a.Contains("rdf:"))
            {
                attrib = a.Substring(0, a.LastIndexOf(dot));
                present = true;
            }
            else
            {
				attrib = a;
				present = false;
			}
		}

	}
}
