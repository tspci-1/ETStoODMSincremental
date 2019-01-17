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
using System.Xml.Linq;
using System.IO;

namespace ETStoODMSIncremental
{
    class IncrementalFile
    {
        XDocument incrementalDocument;

        string incrementalFileName;
        string incrementalReferenceFileName;

        XNamespace cimNS = "http://iec.ch/TC57/2013/CIM-schema-cim16#";
        XNamespace dmNS = "http://iec.ch/2002/schema/CIM_difference_model#";
        XNamespace aepNS = "http://aep.com/2018/aep-extensions#";
        XNamespace rdfNS;

        string currentClassName;
        string currentClassMappingName;
        string currentRDFid;

        XElement currentElement;
        XElement methodElement;
        XElement currentAttribute;

        List<XElement> methodElements;

        public IncrementalFile(string incrementalFileName, XNamespace rdfNamespace)
        {
            rdfNS = rdfNamespace;

            this.incrementalFileName = incrementalFileName;
            incrementalReferenceFileName = Path.Combine(
                Path.GetDirectoryName(incrementalFileName),
                Path.GetFileNameWithoutExtension(incrementalFileName),
                "_reference",
                Path.GetExtension(incrementalFileName));

            IncrementalReferenceFile irf = new IncrementalReferenceFile(incrementalReferenceFileName, rdfNS);

            methodElements = new List<XElement>();

            Utils.WriteTimeToConsoleAndLog(String.Format("Initializing incremental file {0}.", incrementalFileName));

            incrementalDocument =
                new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement(rdfNS + "RDF",
                        new XAttribute(XNamespace.Xmlns + "rdf", rdfNS),
                        new XAttribute(XNamespace.Xmlns + "cim", cimNS),
                        new XAttribute(XNamespace.Xmlns + "aep", aepNS),
                        new XAttribute(XNamespace.Xmlns + "dm", dmNS),
                        new XElement(dmNS + "DifferenceModel",
                            new XAttribute(rdfNS + "about", "urn:uuid:" + Guid.NewGuid()),
                            new XElement(dmNS + "preconditions",
                                new XAttribute(rdfNS + "parseType", "Statements")),
                            new XElement(dmNS + "reverseDifferences",
                                new XAttribute(rdfNS + "parseType", "Statements")),
                            new XElement(dmNS + "forwardDifferences",
                                new XAttribute(rdfNS + "parseType", "Statements")))));
        }

        public void NewClass(string className, string classMappingName)
        {
            currentClassName = className;
            currentClassMappingName = classMappingName;

            Utils.WriteTimeToConsoleAndLog(String.Format("Adding instance data for class {0} to incremental file.", currentClassName));

            string commentText = " " + currentClassName + " ";

            if (currentClassName != classMappingName)
            {
                commentText += "in EMS exported ETS file is mapped to " + classMappingName + " in EMS exported CIM XML file ";
            }

            incrementalDocument
                .Element(rdfNS + "RDF")
                    .Element(dmNS + "DifferenceModel")
                        .Element(dmNS + "forwardDifferences")
                            .Add(new XComment(commentText));
        }

        public void NewIncrementalInstance(string RDFid, bool newUndefinedClass)
        {
            NewInstance(ref currentElement, RDFid, newUndefinedClass);
        }
        public void NewMethodInstance(string RDFid, bool newUndefinedClass, string newClassName)
        {
            NewInstance(ref methodElement, RDFid, newUndefinedClass, newClassName);
        }
        private void NewInstance(ref XElement element, string RDFid, bool newUndefinedClass, string newClassName = "")
        {
            currentRDFid = RDFid;

            if (!newUndefinedClass)
            {
                element = new XElement(rdfNS + "Description", new XAttribute(rdfNS + "about", RDFid.Insert(0, "#")));
            }
            else
            {
                element = new XElement(cimNS + newClassName, new XAttribute(rdfNS + "ID", RDFid));
            }
        }

        public void AddPossibleAttribute(string destination, string value, bool isRDF, bool newUndefinedClass)
        {
            if (value == null || value == string.Empty)
            {
                return;            // null value - no need to add this.
            }

            if (value.Contains("http:"))
            {
                isRDF = true;
            }

            XName currentXName;

            if (destination.Contains("AEP_"))
            {
                currentXName = aepNS + destination;
            }
            else
            {
                currentXName = cimNS + destination;
            }

            if (isRDF)
            {
                if (value[0] != '#' && !value.Contains("http:"))
                {
                    value = "#" + value;
                }
                currentAttribute = new XElement(currentXName, new XAttribute(rdfNS + "resource", value));
            }
            else
            {
                currentAttribute = new XElement(currentXName, value);
            }
        }
        public void PossibleAttributeComplete()
        {
            currentElement.Add(currentAttribute);
        }

        public void AddIncrementalAttribute(string destination, string value, bool isRDF, bool newUndefinedClass)
        {
            AddAttribute(ref currentElement, destination, value, isRDF, newUndefinedClass);
        }
        public void AddMethodAttribute(string destination, string value, bool isRDF, bool newUndefinedClass)
        {
            AddAttribute(ref methodElement, destination, value, isRDF, newUndefinedClass);
        }
        private void AddAttribute(ref XElement element, string destination, string value, bool isRDF, bool newUndefinedClass)
        {
            if (value == null || value == string.Empty)
            {
                return;            // null value - no need to add this.
            }

            if (value.Contains("http:"))
            {
                isRDF = true;
                //                value = "rdf:resource=" + value;
            }

            XName currentXName;

            if (destination.Contains("AEP_"))
            {
                currentXName = aepNS + destination;
            }
            else
            {
                currentXName = cimNS + destination;
            }

            if (isRDF)
            {
                if (value[0] != '#' && !value.Contains("http:"))
                {
                    value = "#" + value;
                }
                element.Add(new XElement(currentXName, new XAttribute(rdfNS + "resource", value)));
            }
            else
            {
                element.Add(new XElement(currentXName, value));
            }
        }

        public void AddCurrentElement()
        {
            AddElement(currentElement);
        }
        private void AddElement(XElement element)
        {
            incrementalDocument
                .Element(rdfNS + "RDF")
                    .Element(dmNS + "DifferenceModel")
                        .Element(dmNS + "forwardDifferences")
                            .Add(element);
        }

        private void InitializeMethodElements()
        {
            if (methodElements.Count > 0)
                methodElements.Clear();
        }

        public void AddToMethodElements()
        {
            methodElements.Add(methodElement);
        }

        public void MethodComplete()
        {
            foreach (XElement element in methodElements)
            {
                AddElement(element);
            }

            InitializeMethodElements();
        }

        public void Save()
        {
            RemoveEmptyElements();

            incrementalDocument.Save(incrementalFileName);
        }

        private void RemoveEmptyElements()
        {
            List<XElement> elList =
                incrementalDocument
                    .Element(rdfNS + "RDF")
                        .Element(dmNS + "DifferenceModel")
                            .Element(dmNS + "forwardDifferences")
                                .Elements().ToList<XElement>();

            foreach (XElement el in elList)
            {
                if (!el.HasElements)
                {
                    el.Remove();
                }
            }
        }
        public void WriteDocToConsole()
        {
            Console.WriteLine();
            Console.WriteLine(incrementalDocument);
        }
    }
}
