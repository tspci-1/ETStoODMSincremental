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
