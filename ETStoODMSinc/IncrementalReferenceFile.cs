using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace ETStoODMSIncremental
{
    class IncrementalReferenceFile
    {
        XDocument incrementalReferenceDocument;

        string incrementalReferenceFileName;

        XNamespace cimNS = "http://iec.ch/TC57/2013/CIM-schema-cim16#";
        XNamespace dmNS = "http://iec.ch/2002/schema/CIM_difference_model#";
        XNamespace aepNS = "http://aep.com/2018/aep-extensions#";
        XNamespace rdfNS;

        //                             new
        //                             class.
        //         existing            attrib  created
        //         rdf                 ref     rdfid
        Dictionary<string, Dictionary<string, string>> existingReference;

        bool save;

        public IncrementalReferenceFile(string incrementalFileName, XNamespace rdfNamespace)
        {
            save = false;
            incrementalReferenceFileName = incrementalFileName;
            rdfNS = rdfNamespace;
            existingReference = new Dictionary<string, Dictionary<string, string>>();

            if (File.Exists(this.incrementalReferenceFileName))
            {
                XElement existingElements = XElement.Load(incrementalReferenceFileName);
 
                incrementalReferenceDocument = XDocument.Load(incrementalReferenceFileName);

                existingReference = new Dictionary<string, Dictionary<string, string>>();

                var existingReferences =
                    from references in existingElements.Descendants(rdfNS + "Description")
                    let rdfid = references.Attribute(rdfNS + "about").Value.ToString()
                    let elements = references.Elements()
                    select new
                    {
                        rdfid = rdfid,
                        elsments = elements,
                    };

                foreach (var eachOne in existingReferences)
                {
                    string RDFid = eachOne.rdfid.Substring(1);

                    foreach (XElement myElement in eachOne.elsments)
                    {
                        string classAttributeName = myElement.Name.ToString();
                        string attributeRDFid = myElement.Attribute(rdfNS + "resource").Value.ToString();

                        if (!existingReference.ContainsKey(RDFid))
                        {
                            existingReference.Add(RDFid, new Dictionary<string, string>());
                            existingReference[RDFid].Add(classAttributeName, attributeRDFid);
                        }
                        else if (!existingReference[RDFid].ContainsKey(classAttributeName))
                        {
                            existingReference[RDFid].Add(classAttributeName, attributeRDFid);
                        }
                    }
                }
            }
            else
            {
                incrementalReferenceDocument =
                    new XDocument(
                        new XDeclaration("1.0", "UTF-8", null),
                        new XElement(rdfNS + "RDF",
                            new XAttribute(XNamespace.Xmlns + "rdf", rdfNS),
                            new XAttribute(XNamespace.Xmlns + "cim", cimNS)));
            }
        }

        public bool FindReferenceRDFID (string existingClassRDFid, string referencedClassAttribute, out string referencedRDFID)
        {
            bool found = false;

            referencedRDFID = String.Empty;

            if (existingReference.ContainsKey(existingClassRDFid))
            {
                foreach (KeyValuePair<string,string> ReferencedRDFID in existingReference[existingClassRDFid])
                {
                    found = ReferencedRDFID.Key == referencedClassAttribute;

                    if (found)
                    {
                        referencedRDFID = ReferencedRDFID.Value;
                        break;
                    }
                }
            }
            return found;
        }
        public void AddReference(string classRDFid, string newReferenceClassAttribute, string newReferenceRDFid)
        {
            XElement element = new XElement(rdfNS + "Description", new XAttribute(rdfNS + "about", classRDFid.Insert(0, "#")));
            XElement currentAttribute = new XElement(newReferenceClassAttribute, new XAttribute(rdfNS + "resource", newReferenceRDFid));

            element.Add(currentAttribute);

            incrementalReferenceDocument.Element(rdfNS + "RDF").Add(element);

            save = true;
        }
        public void Save()
        {
            if (save)
                incrementalReferenceDocument.Save(incrementalReferenceFileName);
        }
    }
}
