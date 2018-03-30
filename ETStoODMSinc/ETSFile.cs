using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace ETStoODMSIncremental
{
	class ETSFile
	{
		//
		// By itself the ets file is too large for XElement to load.
		// So there is a need to read and separate the ets into subsets
		// based on needed class names and attributes.  Each class will
		// have its own subset file.
		//
		// class name     XElement
		Dictionary<string, XElement> etsSubsets;
		// class name     subset filename
		Dictionary<string, string> etsSubsetFilenames;
		//  namespace name   url
		Dictionary<string, XNamespace> etsXNamespaces;
		//       className
		Dictionary<string, ETSSubsetContents> subsetContents;

		string etsFilename;

		public string etsFileName
		{
			get
			{
				return etsFilename;
			}
		}

		public Dictionary<string, ETSSubsetContents> SubsetContents
		{
			get
			{
				return subsetContents;
			}
		}

		public XNamespace GetNamespaceDefinition(string name)
		{
			XNamespace ns;

			try
			{
				ns = etsXNamespaces[name];
			}
			catch (Exception e)
			{
				ns = "";
			}

			return ns;
		}

		public ETSFile(string etsfilename)
		{
			etsFilename = etsfilename;

			etsSubsets = new Dictionary<string, XElement>();
			etsSubsetFilenames = new Dictionary<string, string>();
			etsXNamespaces = new Dictionary<string, XNamespace>();
			subsetContents = new Dictionary<string, ETSSubsetContents>();
		}

		public ETSSubsetContents GetContents(string className)
		{
			return subsetContents[className];
		}

		public void GenerateSubsetFiles (ref Dictionary<string, List<string>> listOfNeededAttributes)
		{
			// class name     subset StreamWriter
			Dictionary<string, StreamWriter> etsSubsetStreamWriters = new Dictionary<string, StreamWriter>();

			// generate files and StreamWriters
			string currentDateTime = DateTime.Now.ToString("_yyyyMMdd_HHmmss");
			string etsFilenamePath = Path.GetDirectoryName(etsFilename);
			string etsFilenamewoExt = Path.GetFileNameWithoutExtension(etsFilename);
			string etsFilenameExt = Path.GetExtension(etsFilename);

            Utils.WriteTimeToConsoleAndLog(String.Format("Generating subset files for ETS file {0}.", etsFilename));

            StreamReader etsFilenameReader = new StreamReader(etsFilename);

			string etsHeaderLine = etsFilenameReader.ReadLine();

			etsHeaderLine = etsHeaderLine.Substring(etsHeaderLine.IndexOf('<'));

			string etsRDFHeaderLine = etsFilenameReader.ReadLine();

            int lineCount = 2;

			etsRDFHeaderLine = etsRDFHeaderLine.Substring(etsRDFHeaderLine.IndexOf('<'));

			GenerateNamespaceList(etsRDFHeaderLine);

			foreach (KeyValuePair<string, List<string>> entry in listOfNeededAttributes)
			{
				etsSubsetFilenames.Add(entry.Key, Path.Combine(etsFilenamePath, etsFilenamewoExt + "_" + entry.Key + currentDateTime) + etsFilenameExt);

				etsSubsetStreamWriters.Add(entry.Key, new StreamWriter(etsSubsetFilenames[entry.Key]));

				etsSubsetStreamWriters[entry.Key].WriteLine(etsHeaderLine);
				etsSubsetStreamWriters[entry.Key].WriteLine(etsRDFHeaderLine);
			}

			bool importantClassFound = false;

            List<string> classInstanceFound = new List<string>();
			List<string> currentAttributes = new List<string>();
			string currentClass = "";
            string rdfSearchString = "rdf:ID";

            while (!etsFilenameReader.EndOfStream)
            {
                if (++lineCount % 1000000 == 0)
                {
                    Utils.WriteTimeToConsoleAndLog(String.Format("Reading line {0} of ETS File.", lineCount));
                }

                string currentLine = etsFilenameReader.ReadLine();

                if (currentLine.Length == 0)
                    continue;

                if (!importantClassFound)
                {
                    if (currentLine.Contains(rdfSearchString))
                    {
                        foreach (KeyValuePair<string, List<string>> entry in listOfNeededAttributes)
                        {
                            string classHeader = "<etsv1:" + entry.Key + " ";

                            if (currentLine.Contains(classHeader))
                            {
                                importantClassFound = true;
                                etsSubsetStreamWriters[entry.Key].WriteLine(currentLine);
                                currentClass = entry.Key;
                                currentAttributes = entry.Value;

                                if (!classInstanceFound.Contains(entry.Key))
                                {
                                    classInstanceFound.Add(entry.Key);
                                    Utils.WriteTimeToConsoleAndLog(String.Format("First instance of {0} found.", entry.Key));
                                }
                                break;
                            }
                        }
                    }
                }
                else if (currentLine.Trim().Substring(0, 2) == "</" && currentLine.IndexOf('.') == -1)
                {
                    etsSubsetStreamWriters[currentClass].WriteLine(currentLine);
                    importantClassFound = false;
                }
                else
                {
                    char[] endOfAttribute = new char[] { ' ', '>' };
                    int colon;

                    string trimmedLine = currentLine.Trim();

                    string attribute = trimmedLine.Substring(colon = trimmedLine.IndexOf(":") + 1, trimmedLine.IndexOfAny(endOfAttribute) - colon);

                    if (currentAttributes.Contains(attribute))
                        etsSubsetStreamWriters[currentClass].WriteLine(currentLine);
                }
            }
 
			// close all the subset files
			foreach (KeyValuePair<string, StreamWriter> swEntry in etsSubsetStreamWriters)
			{
				swEntry.Value.WriteLine("</rdf:RDF>");
				swEntry.Value.Close();
				swEntry.Value.Dispose();
			}
		}

		public void AcquireSubsetContents(ref Dictionary<string, List<string>> listOfNeededAttributes,
										  ref Dictionary<string, Dictionary<string, bool>> attributeIsRDF)
		{
			// acquire contents
			foreach (KeyValuePair<string, List<string>> entry in listOfNeededAttributes)
			{
                Utils.WriteTimeToConsoleAndLog(String.Format("Storing configured values for instance {0}", entry.Key));

				subsetContents.Add(entry.Key, new ETSSubsetContents(entry.Key, etsSubsetFilenames[entry.Key]));

				subsetContents[entry.Key].StoreContents(attributeIsRDF[entry.Key], etsXNamespaces["etsv1"], etsXNamespaces["rdf"]);
			}
		}

        public void DeleteSubsetFiles()
        {
            foreach (string filename in etsSubsetFilenames.Values)
            {
                File.Delete(filename);
            }
        }

		private void GenerateNamespaceList(string rdfHeaderLine)
		{
			string[] nsArray = rdfHeaderLine.Split(' ');

			int colon;
			int doublequote;

			string nsName;
			XNamespace xnamespacename;

			foreach (string ns in nsArray)
			{
				if (ns.Contains("xmlns:"))
				{
					nsName = ns.Substring(colon = ns.IndexOf(":") + 1, ns.IndexOf("=") - colon);
					xnamespacename = ns.Substring(doublequote = ns.IndexOf("\"") + 1, ns.IndexOf("#") - doublequote + 1);

					etsXNamespaces.Add(nsName, xnamespacename);
				}
			}
		}
	}
}
