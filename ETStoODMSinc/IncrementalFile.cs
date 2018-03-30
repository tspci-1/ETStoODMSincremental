using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ETStoODMSIncremental
{
	class IncrementalFile
	{
		XDocument incrementalDocument;

		string incrementalFileName;

		XNamespace cimNS = "http://iec.ch/TC57/2013/CIM-schema-cim16#";
		XNamespace dmNS = "http://iec.ch/2002/schema/CIM_difference_model#";
        XNamespace aepNS = "http://aep.com/2018/aep-extensions#";
		XNamespace rdfNS;

		string currentClassName;
        string currentClassMappingName;
		string currentRDFid;

        XElement currentElement;

		public IncrementalFile(string incrementalFileName, XNamespace rdfNamespace)
		{
			rdfNS = rdfNamespace;

			this.incrementalFileName = incrementalFileName;

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

        public void NewInstance(string RDFid, bool newUndefinedClass)
		{
			currentRDFid = RDFid;

            if (!newUndefinedClass)
            {
                currentElement = new XElement(rdfNS + "Description", new XAttribute(rdfNS + "about", RDFid.Insert(0, "#")));
            }
            else
            {
                currentElement = new XElement(cimNS + currentClassMappingName, new XAttribute(rdfNS + "ID", RDFid));
            }

/*            incrementalDocument
                .Element(rdfNS + "RDF")
					.Element(dmNS + "DifferenceModel")
						.Element(dmNS + "forwardDifferences")
							.Add(currentElement); */
		}

		public void AddAttribute(string destination, string value, bool isRDF, bool newUndefinedClass)
		{
            /*            XName descendantsToFind;
                        string rdfIDStringToFind;

                        if (!newUndefinedClass)
                        {
                            descendantsToFind = rdfNS + "Description";
                            rdfIDStringToFind = currentRDFid.Insert(0, "#");
                        }
                        else
                        {
                            descendantsToFind = cimNS + currentClassMappingName;
                            rdfIDStringToFind = currentRDFid;
                        }

                        var ce =
                            from theElement in incrementalDocument.Descendants(descendantsToFind)
                            where theElement.FirstAttribute.Value.ToString() == rdfIDStringToFind
                            select theElement;

                        foreach (XElement el in ce)
                        {
                            if (isRDF)
                            {
                                el.Add(new XElement(cimNS + destination, new XAttribute(rdfNS + "resource", value.Insert(1, "#"))));
                            }
                            else
                            {
                                el.Add(new XElement(cimNS + destination, value));
                            }
                        } */
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
                currentElement.Add(new XElement(currentXName, new XAttribute(rdfNS + "resource", value)));
            }
            else
            {
                currentElement.Add(new XElement(currentXName, value));
            }
        }

        public void AddCurrentElement()
        {
            incrementalDocument
                .Element(rdfNS + "RDF")
                    .Element(dmNS + "DifferenceModel")
                        .Element(dmNS + "forwardDifferences")
                            .Add(currentElement);
        }

        public void Save()
		{
			incrementalDocument.Save(incrementalFileName);
		}
        public void WriteDocToConsole()
        {
            Console.WriteLine();
            Console.WriteLine(incrementalDocument);
        }
    }
}
