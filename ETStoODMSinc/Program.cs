using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Windows.Forms;
using System.IO;

namespace ETStoODMSIncremental
{

	class Program
	{
        public static Boolean m_IncXmlFile; //Incremental XML File
        public static Boolean m_CsqlFile;   // Customer.sql File
        public static Boolean m_ExTextFile;  //Extensions Text File
        public static Boolean m_ExExcelFile;  //Extensions Excel Workbook File

        public static string incrementalFilename;
 
        public static IncrementalFile iF;   //Incremental File
        public static CalcEngine.CalcEngine calculationEngine;  //Calculation engine project
        public static ExtensionsOutput otF; //Extensions Text File

       [STAThread]
        static void Main(string[] args)
		{

            ConfigurationFile configurationFile;

            string configurationFilename;
            string configurationPath;
            string alstomETSFilename;
            string alstomETSPath;

            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

             using (Form1 frm = new Form1())
            {
                frm.ShowDialog();  //Kick-off the Front End form
                m_IncXmlFile = frm.IncXmlFile;
                m_CsqlFile = frm.CsqlFile;
                m_ExTextFile = frm.ExTextFile;
                m_ExExcelFile = frm.ExExcelFile;
                configurationFilename = frm.ConfigurationFilePath;
                alstomETSFilename = frm.ETSFIlePath;
            }
           
			configurationPath = Path.GetDirectoryName(configurationFilename);  //Just want this line for testing form1 usage

			Utils.CreateLog(new StreamWriter
							   (Path.Combine
								   (configurationPath,
									Path.GetFileNameWithoutExtension(configurationFilename)
										 + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + "_LOG.log")));

			alstomETSPath = Path.GetDirectoryName(alstomETSFilename);  //Just want this line for testing form1 usage

            Utils.WriteTimeToConsoleAndLog("ETStoODMSIncremental invoked with configuration file " + configurationFilename);
            Utils.WriteTimeToConsoleAndLog(" and ETS file " + alstomETSFilename);

			configurationFile = new ConfigurationFile(configurationFilename); 

			configurationFile.GenerateDictionaryOfNeededAttributesAndIsRDF();

			ETSFile etsFile = new ETSFile(alstomETSFilename);

			etsFile.GenerateSubsetFiles(ref configurationFile.listOfNeededAttributes);

			etsFile.AcquireSubsetContents(ref configurationFile.listOfNeededAttributes, ref configurationFile.neededAttributeIsRDF);

            if (m_IncXmlFile)
            {
                 incrementalFilename = Path.Combine
                                                (Path.GetDirectoryName(alstomETSFilename),
                                                 Path.GetFileNameWithoutExtension(alstomETSFilename)
                                                     + "_incremental_" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".xml");

                iF = new IncrementalFile(incrementalFilename, etsFile.GetNamespaceDefinition("rdf"));
                calculationEngine = new CalcEngine.CalcEngine();

            }

            if (m_ExTextFile)
            {                //Create the Extension output filename

                string ExtensionsOFilename = Path.Combine
                                                (Path.GetDirectoryName(alstomETSFilename),
                                                 "ExtensionsOutputFile-"
                                                     + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".txt");
                otF = new ExtensionsOutput(ExtensionsOFilename, "TEXT");  //create the Extensions text file
            }

            if (m_ExExcelFile)
            { //Create the Extension Excel output filename
                string excelFilename = Path.Combine
                                               (Path.GetDirectoryName(alstomETSFilename),
                                                "ExtensionsOutput- "
                                                 + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".xlsx");
                ExtensionsOutput oeF = new ExtensionsOutput(excelFilename, "EXCEL");  //create the Extensions Excel file
            }

            if (m_CsqlFile)
            { //Create the Customer.sql output filename
                string SQLOutputFilename = Path.Combine
                                               (Path.GetDirectoryName(alstomETSFilename),
                                                "Customer.sql");
                SQLOutput SQLOF1 = new SQLOutput(SQLOutputFilename);  //Create the sql output file used for ODMS
            }


            foreach (ETSClassConfiguration classConfiguration in configurationFile.ETSClassConfigurations)
			{
                if (classConfiguration.InputOnly || classConfiguration.IsMethod)
                {
                    continue;
                }

				string className = classConfiguration.ClassName;
				string classMappingName = classConfiguration.ClassMappingName;
                bool newUndefinedClass = classConfiguration.NewUndefinedClass;

                if (m_IncXmlFile)
                {
                    iF.NewClass(className, classMappingName);
                }

				Dictionary<string, string> attributeAndDestination = new Dictionary<string, string>();
				Dictionary<string, bool> attributeAndIsRDF = new Dictionary<string, bool>();

                Dictionary<string, string> destinationAndConstant = new Dictionary<string, string>();
                Dictionary<string, bool> destinationAndIsRDF = new Dictionary<string, bool>();

                Utils.WriteTimeToConsoleAndLog(String.Format("Adding instances to incremental file for {0}", className));

                if (m_CsqlFile || m_ExExcelFile || m_ExTextFile)
                {
                    ExtensionsOutput.LoadDictionary(classConfiguration, classMappingName); // Create entries in the output files and load the summary dictionary
                }

                if (m_IncXmlFile)
                {
                    foreach (Assignment assignment in classConfiguration.Assignments)
                    {
                        if (assignment.Attribute[0] != ':')
                        {
                            if (!attributeAndDestination.ContainsKey(assignment.Attribute))
                            {
                                attributeAndDestination.Add(assignment.Attribute, assignment.Destination);
                                attributeAndIsRDF.Add(assignment.Attribute, assignment.AttributeIsRDF);
                            }
                        }
                        else
                        {
                            if (!destinationAndConstant.ContainsKey(assignment.Destination))
                            {
                                destinationAndConstant.Add(assignment.Destination, assignment.Attribute.Substring(1));
                                destinationAndIsRDF.Add(assignment.Destination, assignment.DestinationIsRDF);
                            }
                        }
                    }

                    int instanceCount = 0;

                    foreach (KeyValuePair<String, Dictionary<string, string>> content in etsFile.GetContents(className).Contents)
                    {
                        string RDFid = content.Key;

                        iF.NewIncrementalInstance(RDFid, newUndefinedClass);

                        foreach (KeyValuePair<string, string> attributeAndValue in content.Value)
                        {
                            string value = attributeAndValue.Value;

                            if (value == string.Empty)
                                continue;

                            string attribute = attributeAndValue.Key;

                            if (attributeAndDestination.ContainsKey(attribute))
                            {
                                string destination = attributeAndDestination[attribute];

                                if (destination == "InputOnly")
                                {
                                    continue;
                                }

                                bool isRDF = attributeAndIsRDF[attribute];

                                iF.AddIncrementalAttribute(destination, value, isRDF, newUndefinedClass);
                            }
                        }

                        foreach (KeyValuePair<string, string> destination in destinationAndConstant)
                        {
                            iF.AddIncrementalAttribute(destination.Key,
                                                       destination.Value,
                                                       destinationAndIsRDF[destination.Key],
                                                       false);
                        }

                        // Add calculation here
                        foreach (Calculation calculation in classConfiguration.Calculations)
                        {
                            string equation = calculation.Equation;

                            foreach (string operand in calculation.Operands)
                            {
                                if (operand.Contains('.'))
                                {
                                    string replacement = content.Value[operand];

                                    if (replacement == String.Empty)
                                    {
                                        replacement = "0";
                                    }

                                    equation = equation.Replace(operand, replacement);
                                }
                            }

                            string value = calculationEngine.Evaluate(equation).ToString();

                            iF.AddIncrementalAttribute(calculation.Target, value, false, false);
                        }

                        // do logical part
                        foreach (Logic logic in classConfiguration.LogicExpressions)
                        {
                            if (logic.LogicTestPasses(content.Value[logic.Attribute]))
                            {
                                if (logic.Method == "CreateRegulatingControl" &&
                                    (content.Value[logic.Arguments[0]] == "" ||
                                     content.Value[logic.Arguments[1]] == ""))
                                {
                                    continue;
                                }

                                ETSClassConfiguration methodConfiguration = configurationFile.GetMethod(logic.Method);

                                int numberOfParameters = logic.Arguments.Count;
                                string[] parameterValues = new string[numberOfParameters];

                                string newRDFid = Guid.NewGuid().ToString();

                                iF.AddIncrementalAttribute(logic.Destination, newRDFid, true, true);

                                iF.NewMethodInstance(newRDFid, true, methodConfiguration.ClassMappingName);

                                for (int index = 0; index < numberOfParameters; index++)
                                {
                                    parameterValues[index] = content.Value[logic.Arguments[index]];
                                }

                                foreach (Assignment assignment in methodConfiguration.Assignments)
                                {
                                    if (assignment.Attribute[0] == ':')
                                    {
                                        string attribute = assignment.Attribute.Substring(1);

                                        iF.AddMethodAttribute(assignment.Destination, attribute, assignment.DestinationIsRDF, true);
                                    }
                                    else if (assignment.Attribute.IndexOfAny(new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' }) == 0)
                                    {
                                        if (assignment.Attribute.Length == 1)
                                        {
                                            iF.AddMethodAttribute(assignment.Destination,
                                                                  parameterValues[Int32.Parse(assignment.Attribute) - 1],
                                                                  assignment.DestinationIsRDF,
                                                                  true);
                                        }
                                        else
                                        {
                                            string[] attributeSplit = assignment.Attribute.Split(new char[] { '\\' });

                                            string thisDestination = assignment.Destination;

                                            string classRDFid = parameterValues[Int32.Parse(attributeSplit[0]) - 1];

                                            if (classRDFid == null || classRDFid == string.Empty)
                                            {
                                                continue;
                                            }

                                            if (classRDFid[0] == '#')
                                            {
                                                classRDFid = classRDFid.Substring(1);
                                            }

                                            string targetClass = methodConfiguration.GetMethodParameter(Int32.Parse(attributeSplit[0]) - 1);
                                            string thisValue =
                                                etsFile.GetContents(targetClass).Contents[classRDFid][attributeSplit[1]];

                                            bool thisIsRDF = assignment.DestinationIsRDF;
                                            bool thisNewClass = true;

                                            iF.AddMethodAttribute(thisDestination, thisValue, thisIsRDF, thisNewClass);
                                        }
                                    }
                                    else if (assignment.Attribute == "GUID()")
                                    {
                                        iF.AddMethodAttribute(assignment.Destination, Guid.NewGuid().ToString(), false, true);
                                    }
                                    else if (assignment.Attribute.Contains("FindTerminalBusBarSection"))
                                    {
                                        bool busbarFound = false;

                                        List<string> busbarList = new List<string>();

                                        busbarList.AddRange(etsFile.GetContents("BusBarSection").Contents.Keys);

                                        string parameterValue = parameterValues[2 - 1];

                                        bool cn_wasFound = false;
                                        string firstTerminalRDF = "";

                                        foreach (KeyValuePair<string, Dictionary<string, string>> terminalContents in etsFile.GetContents("Terminal").Contents)
                                        {
                                            string t_cn = terminalContents.Value["Terminal.ConnectivityNode"];
                                            bool cn_found = t_cn == parameterValue;

                                            if (cn_found)
                                            {
                                                cn_wasFound = true;

                                                string t_ce = terminalContents.Value["Terminal.ConductingEquipment"];
                                                if (t_ce[0] == '#')
                                                {
                                                    t_ce = t_ce.Substring(1);
                                                }
                                                bool inBusbarList = busbarList.Contains(t_ce);

                                                if (inBusbarList)
                                                {
                                                    iF.AddMethodAttribute(assignment.Destination, terminalContents.Key, true, true);
                                                    busbarFound = true;
                                                    break;
                                                }
                                                if (firstTerminalRDF == "")
                                                {
                                                    firstTerminalRDF = terminalContents.Key;
                                                }
                                            }
                                        }
                                        if (!cn_wasFound)
                                        {
                                            Utils.WriteToLog("ConnectivityNode not found.");
                                        }
                                        if (!busbarFound)
                                        {
                                            iF.AddMethodAttribute(assignment.Destination, firstTerminalRDF, true, true);
                                            Utils.WriteToLog("BusbarSection terminal not found for ConnectivityNode " + parameterValues[2 - 1] + " while creating RegulatingControl");
                                        }
                                    }
                                    else if (assignment.Attribute.Contains("FindIntervalSchedTimePointTargetValue"))
                                    {
                                        string parameterValue = parameterValues[1 - 1];
                                        //                                    if (parameterValue[0] == '#')
                                        //                                    {
                                        //                                        parameterValue = parameterValue.Substring(1);
                                        //                                    }

                                        foreach (KeyValuePair<string, Dictionary<string, string>> intervalSchedTimePointContents in etsFile.GetContents("IntervalSchedTimePoint").Contents)
                                        {
                                            if (intervalSchedTimePointContents.Value["IntervalSchedTimePoint.RegularIntervalSchedule"] == parameterValue &&
                                                Int32.Parse(intervalSchedTimePointContents.Value["IntervalSchedTimePoint.SequenceNumber"]) == 1)
                                            {
                                                iF.AddMethodAttribute(assignment.Destination,
                                                                      intervalSchedTimePointContents.Value["IntervalSchedTimePoint.Value1"],
                                                                      false,
                                                                      true);
                                                break;
                                            }
                                        }
                                    }
                                    else if (assignment.Attribute.Contains("FindIntervalSchedTimePointDeadBand"))
                                    {
                                        string parameterValue = parameterValues[1 - 1];
                                        //                                    if (parameterValue[0] == '#')
                                        //                                    {
                                        //                                        parameterValue = parameterValue.Substring(1);
                                        //                                    }

                                        foreach (KeyValuePair<string, Dictionary<string, string>> intervalSchedTimePointContents in etsFile.GetContents("IntervalSchedTimePoint").Contents)
                                        {
                                            if (intervalSchedTimePointContents.Value["IntervalSchedTimePoint.RegularIntervalSchedule"] == parameterValue &&
                                                Int32.Parse(intervalSchedTimePointContents.Value["IntervalSchedTimePoint.SequenceNumber"]) == 1)
                                            {
                                                iF.AddMethodAttribute(assignment.Destination,
                                                                      intervalSchedTimePointContents.Value["IntervalSchedTimePoint.Value2"],
                                                                      false,
                                                                      true);
                                                break;
                                            }
                                        }

                                    }
                                }

                                iF.AddToMethodElements();
                            }
                        }

                        //
                        iF.AddCurrentElement();
                    }

                    iF.MethodComplete();

                    Utils.WriteTimeToConsoleAndLog(String.Format("Processed {0} instances of class {1}.", instanceCount, className));
                }
            }

            if (m_IncXmlFile)
            {
                iF.Save();
            }

            if (m_ExTextFile)
            {
                ExtensionsOutput.DumpSummary();  //Top off the output file with the Summary Report of unique extensions
            }

            if (m_CsqlFile)
            {
                SQLOutput.Cleanup();  //Close the SQL output file
            }

            DialogResult result = MessageBox.Show("Delete ETS Subset file created for this conversion?",
                                                  "Incremental File Creation Complete",
                                                  MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {            
                etsFile.DeleteSubsetFiles();
                Utils.WriteTimeToConsoleAndLog("ETS Subset Files deleted");
            }

            Utils.WriteTimeToConsole(String.Format("Log file {0} created", Utils.LogFilename));

            Utils.CloseLog();

            Utils.WriteTimeToConsole("Incremental file update completed - exiting");

            System.Threading.Thread.Sleep(5000);
		}

        public string GetInstanceAttributeValue()
        {
            return "poop";
        }
		public static void WriteTimeToConsole()
		{
			Console.Write("{0:T}", DateTime.Now);
        }

    }

	public static class Utils
	{
		static StreamWriter logWriter;

		static bool logCreated = false;

        public static string LogFilename
        {
            get
            {
                return ((FileStream) logWriter.BaseStream).Name;
            }
        }
		public static void WriteTimeToConsole()
		{
            Console.WriteLine("{0:T}", DateTime.Now);
        }

		public static void WriteTimeToConsole(string message)
		{
            Console.WriteLine("{0:T}:{1}", DateTime.Now, message);
        }

		public static void WriteTimeToConsoleAndLog(string message)
		{
			WriteToLog(message);
			WriteTimeToConsole(message);
		}

		public static void WriteToLog(string message)
		{
			if (logCreated)
				logWriter.WriteLine("{0:T}:{1}", DateTime.Now, message);
		}

		public static void CreateLog(StreamWriter sw)
		{
			logWriter = sw;
			logCreated = true;
		}

		public static void CloseLog()
		{
			if (logCreated)
			{
				logWriter.Close();
				logWriter.Dispose();
				logCreated = false;
			}
		}
	}

}
