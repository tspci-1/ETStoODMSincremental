/*
 
Copyright© 2018 Project Consultants, LLC
 
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.”
 
*/

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
        public static string incrementalReferenceFilename;

        public static IncrementalFile iF;   //Incremental File
        public static IncrementalReferenceFile irf; //Incremental Reference File
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
                XNamespace rdfNS = etsFile.GetNamespaceDefinition("rdf");

                string etsDirectory = Path.GetDirectoryName(alstomETSFilename);

                incrementalFilename = 
                    Path.Combine (etsDirectory,
                                    Path.GetFileNameWithoutExtension(alstomETSFilename)
                                        + "_incremental_" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".xml");

                iF = new IncrementalFile(incrementalFilename, rdfNS);

                incrementalReferenceFilename =
                    Path.Combine(etsDirectory, "incrementalReference" + ".xml");

                irf = new IncrementalReferenceFile(incrementalReferenceFilename, rdfNS);

                calculationEngine = new CalcEngine.CalcEngine();
            }

            if (m_ExTextFile)
            {                //Create the Extension output filename

                string ExtensionsOFilename = Path.Combine
                                                (Path.GetDirectoryName(alstomETSFilename),
                                                 "ExtensionsOutputText-"
                                                     + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".txt");
                otF = new ExtensionsOutput(ExtensionsOFilename, "TEXT");  //create the Extensions text file
            }

            if (m_ExExcelFile)
            { //Create the Extension Excel output filename
                string excelFilename = Path.Combine
                                               (Path.GetDirectoryName(alstomETSFilename),
                                                "ExtensionsOutputExcel- "
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

            int instanceCount = 0;

            List<string> busbarList = new List<string>();
            bool busbarListLoaded = false;

            List<string> transformerWindingList = new List<string>();
            bool transformerWindingListLoaded = false;

            Dictionary<string, Dictionary<string, string>> terminalContents = 
                                          new Dictionary<string, Dictionary<string, string>>();
            bool terminalContentsLoaded = false;

            Dictionary<string, Dictionary<string, string>> transformerWindingContents =
                                          new Dictionary<string, Dictionary<string, string>>();
            bool transformerWindingContentsLoaded = false;

            Dictionary<string, Dictionary<string, string>> intervalSchedTimePointContents = 
                                          new Dictionary<string, Dictionary<string, string>>();
            bool intervalSchedTimePointContentsLoaded = false;

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

                    instanceCount = 0;
                }

				Dictionary<string, string> attributeAndDestination = new Dictionary<string, string>();
				Dictionary<string, bool> attributeAndIsRDF = new Dictionary<string, bool>();

                Dictionary<string, string> destinationAndConstant = new Dictionary<string, string>();
                Dictionary<string, bool> destinationAndIsRDF = new Dictionary<string, bool>();

                Utils.WriteTimeToConsoleAndLog(String.Format("Adding instance attribute additions/updates" +
                    " to incremental file for {0}", className));

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

                    foreach (KeyValuePair<String, Dictionary<string, string>> content in etsFile.GetContents(className).Contents)
                    {
                        string RDFid = content.Key;

                        iF.NewIncrementalInstance(RDFid, newUndefinedClass);

                        instanceCount++;

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
                                if ( logic.Method == "CreateRegulatingControl"  &&
                                    (content.Value[logic.Arguments[0]] == "" ||
                                     content.Value[logic.Arguments[1]] == ""))
                                {
                                    continue;
                                }

                                ETSClassConfiguration methodConfiguration = configurationFile.GetMethod(logic.Method);

                                int numberOfParameters = logic.Arguments.Count;
                                string[] parameterValues = new string[numberOfParameters];

                                for (int index = 0; index < numberOfParameters; index++)
                                {
                                    parameterValues[index] = content.Value[logic.Arguments[index]];
                                }

                                if (logic.Method == "CreateTapChangerNeutralU")
                                {
                                    string transformerWindingRDFID = parameterValues[1 - 1];

                                    string ratedKV = 
                                        GetInstanceAttributeValue(ref etsFile, "TransformerWinding", transformerWindingRDFID, "TransformerWinding.RatedKV");

                                    iF.AddIncrementalAttribute(logic.Destination, ratedKV, false, false);

                                    continue;
                                }

                                string referencedRDFid;
                                bool referenceFound = irf.FindReferenceRDFID(RDFid, logic.Destination, out referencedRDFid);

                                if (!referenceFound)
                                {
                                    referencedRDFid = Guid.NewGuid().ToString();
                                    iF.AddPossibleAttribute(logic.Destination, referencedRDFid, true, true);
                                }

                                bool addMethodInstance = true;  // assume we're adding unless we find a problem.

                                Utils.WriteToLog("--Beginning of logic for class " + className + ", RDFID = " + RDFid);

                                Utils.WriteToLog("----Possible attribute associating Regulating Control to " + className);

                                iF.NewMethodInstance(referencedRDFid, !referenceFound, methodConfiguration.ClassMappingName);
                                if (!referenceFound)
                                {
                                    irf.AddReference(RDFid, logic.Destination, referencedRDFid);
                                }

                                // Add calculation here
                                foreach (Calculation calculation in methodConfiguration.Calculations)
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
                                        else if (operand.Length > 1 && operand[0] == '$' && Char.IsNumber(operand[1]))
                                        {
                                            equation = equation.Replace(operand, parameterValues[Int32.Parse(operand.Substring(1)) - 1]);
                                        }
                                    }

                                    string value = calculationEngine.Evaluate(equation).ToString().ToLower();

//                                    iF.AddIncrementalAttribute(calculation.Target, value, false, false);
                                    iF.AddMethodAttribute(calculation.Target, value, false, false);

                                    Utils.WriteToLog("----Calculation results added for " + calculation.Target + " with value of " + value);
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
                                        Utils.WriteToLog("----Trying to find Terminal attached to busbarsection");

                                        bool busbarFound = false;

                                        if (!busbarListLoaded)
                                        {
                                            busbarList.AddRange(etsFile.GetContents("BusBarSection").Contents.Keys);
                                            busbarListLoaded = true;
                                        }

                                        string parameterValue = parameterValues[2 - 1];

                                        bool cn_wasFound = false;
                                        string firstTerminalRDF = "";

                                        if (!terminalContentsLoaded)
                                        {
                                            terminalContents = etsFile.GetContents("Terminal").Contents;
                                            terminalContentsLoaded = true;
                                        }

                                        string theTerminal = string.Empty;

                                        if (parameterValue == string.Empty)
                                        {
                                            foreach (KeyValuePair<string, Dictionary<string, string>> terminalContent in terminalContents)
                                            {
                                                if (terminalContent.Value["Terminal.ConductingEquipment"].Substring(1) == RDFid)
                                                {
                                                    theTerminal = terminalContent.Key;

                                                    Utils.WriteToLog("----TapChanger.ConnectivityNode is null. Using terminal attached to TapChanger");
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (KeyValuePair<string, Dictionary<string, string>> terminalContent in terminalContents)
                                            {
                                                string t_cn = terminalContent.Value["Terminal.ConnectivityNode"];
                                                bool cn_found = t_cn == parameterValue;

                                                if (cn_found)
                                                {
                                                    cn_wasFound = true;

                                                    string t_ce = terminalContent.Value["Terminal.ConductingEquipment"];
                                                    if (t_ce[0] == '#')
                                                    {
                                                        t_ce = t_ce.Substring(1);
                                                    }
                                                    bool inBusbarList = busbarList.Contains(t_ce);

                                                    if (inBusbarList)
                                                    {
                                                        theTerminal = terminalContent.Key;
                                                        busbarFound = true;
                                                        break;
                                                    }
                                                    if (firstTerminalRDF == "")
                                                    {
                                                        firstTerminalRDF = terminalContent.Key;
                                                    }
                                                }
                                            }
                                            if (!cn_wasFound)
                                            {
                                                Utils.WriteToLog("ConnectivityNode not found.");
                                            }
                                            if (!busbarFound)
                                            {
                                                theTerminal = firstTerminalRDF;
                                                Utils.WriteToLog("BusbarSection terminal not found for ConnectivityNode " + parameterValues[2 - 1] + " while creating RegulatingControl");
                                            }
                                        }
                                        iF.AddMethodAttribute(assignment.Destination, theTerminal, true, true);
                                    }
                                    else if (assignment.Attribute.Contains("FindTerminalTransformerWinding"))
                                    {
/*                                        if (RDFid == "1939985237293649459-tc")
                                        {
                                            bool hotcrap = true;
                                        } */
                                        bool transformerWindingFound = false;

                                        Utils.WriteToLog("----Trying to find Terminal attached to transformerwinding");

                                        if (!transformerWindingListLoaded)
                                        {
                                            transformerWindingList.AddRange(etsFile.GetContents("TransformerWinding").Contents.Keys);
                                            transformerWindingListLoaded = true;
                                        }

                                        string connectivityNodeRDFID = parameterValues[2 - 1];
                                        string transformerWindingRDFID = parameterValues[4 - 1];

                                        bool cn_wasFound = false;
                                        // string firstTerminalRDF = "";

                                        if (!terminalContentsLoaded)
                                        {
                                            terminalContents = etsFile.GetContents("Terminal").Contents;
                                            terminalContentsLoaded = true;
                                        }

                                        string theTerminal = string.Empty;

                                        if (connectivityNodeRDFID == string.Empty)
                                        {
                                            foreach (KeyValuePair<string, Dictionary<string, string>> terminalContent in terminalContents)
                                            {
                                                if (terminalContent.Value["Terminal.ConductingEquipment"] == transformerWindingRDFID)
                                                {
                                                    theTerminal = terminalContent.Key;
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Dictionary<string, string> foundTransformerWindingTerminalPair = new Dictionary<string, string>();

                                            foreach (KeyValuePair<string, Dictionary<string, string>> terminalContent in terminalContents)
                                            {
                                                string t_cn = terminalContent.Value["Terminal.ConnectivityNode"];
                                                bool cn_found = t_cn == connectivityNodeRDFID;

                                                if (cn_found)
                                                {
                                                    cn_wasFound = true;

                                                    string t_ce = terminalContent.Value["Terminal.ConductingEquipment"];
                                                    if (t_ce[0] == '#')
                                                    {
                                                        t_ce = t_ce.Substring(1);
                                                    }
                                                    bool inTransformerWindingList = transformerWindingList.Contains(t_ce);

                                                    if (inTransformerWindingList)
                                                    {
                                                        theTerminal = terminalContent.Key;
                                                        transformerWindingFound = true;
                                                        foundTransformerWindingTerminalPair.Add(t_ce, theTerminal);
                                                    }
                                                }
                                            }
                                            if (!cn_wasFound)
                                            {
                                                Utils.WriteToLog("ConnectivityNode not found.");
                                            }
                                            if (!transformerWindingFound)
                                            {
                                                Utils.WriteToLog("TransformerWinding terminal for TapChangercontrol not found for ConnectivityNode " + parameterValues[2 - 1] + " while creating RegulatingControl");
                                            }
                                            if (foundTransformerWindingTerminalPair.Count > 1)
                                            {
                                                foreach (KeyValuePair<string, string> tw_t in foundTransformerWindingTerminalPair)
                                                {
                                                    if (tw_t.Key == transformerWindingRDFID.Substring(1))
                                                    {
                                                        theTerminal = tw_t.Value;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if (theTerminal != string.Empty)
                                        {
                                            iF.AddMethodAttribute(assignment.Destination, theTerminal, true, true);
                                        }
                                        else
                                        {
                                            Utils.WriteToLog("Could not find TransformerWinding to determine RegulatingControl.Terminal for");
                                            Utils.WriteToLog("    ConnectivityNode " + RDFid + " - No RegulatingControl created");
                                            addMethodInstance = false;
                                            break;
                                        }
                                    }
                                    else if (assignment.Attribute.Contains("FindTransformerWindingAVR"))
                                    {
                                        string transformerWindingRDFID = parameterValues[4 - 1];

                                        if (!transformerWindingContentsLoaded)
                                        {
                                            transformerWindingContents = etsFile.GetContents("TransformerWinding").Contents;
                                            transformerWindingContentsLoaded = true;
                                        }

                                        string avr = 
                                            transformerWindingContents[transformerWindingRDFID.Substring(1)]["TransformerWinding.AVR"];

                                        Utils.WriteToLog("----Setting controlEnabled according to TransformerWinding.AVR");

                                        iF.AddMethodAttribute(assignment.Destination, avr, false, true);
                                        iF.AddIncrementalAttribute("TapChanger.controlEnabled", avr, false, false);
                                    }
                                    else if (assignment.Attribute.Contains("FindTransformerWindingRatedKV"))
                                    {
                                        Utils.WriteToLog("----Using RatedKV to set NeutralU");

                                        string transformerWindingRDFID = parameterValues[4 - 1];

                                        if (!transformerWindingContentsLoaded)
                                        {
                                            transformerWindingContents = etsFile.GetContents("TransformerWinding").Contents;
                                            transformerWindingContentsLoaded = true;
                                        }

                                        string neutralU =
                                            transformerWindingContents[transformerWindingRDFID.Substring(1)]["TransformerWinding.RatedKV"];

                                        iF.AddIncrementalAttribute("TapChanger.neutralU", neutralU, false, false);
                                    }
                                    else if (assignment.Attribute.Contains("FindIntervalSchedTimePointTargetValue"))
                                    {
                                        string regulationScheduleRDFID = parameterValues[1 - 1];

                                        string connectivityNodeRDFID = parameterValues[2 - 1];

                                        if (connectivityNodeRDFID == string.Empty)
                                        {
                                            if (!terminalContentsLoaded)
                                            {
                                                terminalContents = etsFile.GetContents("Terminal").Contents;
                                                terminalContentsLoaded = true;
                                            }

                                            foreach (KeyValuePair<string, Dictionary<string, string>> terminalContent in terminalContents)
                                            {
                                                if (terminalContent.Value["Terminal.ConductingEquipment"].Substring(1) == RDFid)
                                                {
                                                    connectivityNodeRDFID = terminalContent.Value["Terminal.ConnectivityNode"];
                                                    break;
                                                }
                                            }
                                        }

                                        string voltageLevelRDFID =
                                            GetInstanceAttributeValue(ref etsFile,
                                                                      "ConnectivityNode",
                                                                      connectivityNodeRDFID,
                                                                      "ConnectivityNode.ConnectivityNodeContainer");
                                        string baseVoltageRDFID =
                                            GetInstanceAttributeValue(ref etsFile,
                                                                      "VoltageLevel",
                                                                      voltageLevelRDFID,
                                                                      "VoltageLevel.BaseVoltage");
                                        string voltageBaseValue =
                                            GetInstanceAttributeValue(ref etsFile,
                                                                      "BaseVoltage",
                                                                      baseVoltageRDFID,
                                                                      "BaseVoltage.VoltageBase");

                                        if (!intervalSchedTimePointContentsLoaded)
                                        {
                                            intervalSchedTimePointContents = etsFile.GetContents("IntervalSchedTimePoint").Contents;
                                            intervalSchedTimePointContentsLoaded = true;
                                        }

                                        foreach (KeyValuePair<string, Dictionary<string, string>> intervalSchedTimePointContent
                                                 in intervalSchedTimePointContents)
                                        {
                                            if (intervalSchedTimePointContent.Value["IntervalSchedTimePoint.RegularIntervalSchedule"]
                                                                             == regulationScheduleRDFID &&
                                                Int32.Parse(intervalSchedTimePointContent.Value["IntervalSchedTimePoint.SequenceNumber"])
                                                                             == 1)
                                            {
                                                string destinationValue =
                                                    calculationEngine.Evaluate
                                                        (voltageBaseValue +
                                                            " * " +
                                                         intervalSchedTimePointContent.Value["IntervalSchedTimePoint.Value1"]).ToString();

                                                iF.AddMethodAttribute(assignment.Destination,
                                                                      destinationValue,
                                                                      //                                                                      intervalSchedTimePointContent.Value["IntervalSchedTimePoint.Value1"],
                                                                      false,
                                                                      true);
                                                break;
                                            }
                                        }
                                    }
                                    else if (assignment.Attribute.Contains("FindIntervalSchedTimePointDeadBand"))
                                    {
                                        string regulationScheduleRDFID = parameterValues[1 - 1];

                                        string connectivityNodeRDFID = parameterValues[2 - 1];

                                        if (connectivityNodeRDFID == string.Empty)
                                        {
                                            if (!terminalContentsLoaded)
                                            {
                                                terminalContents = etsFile.GetContents("Terminal").Contents;
                                                terminalContentsLoaded = true;
                                            }

                                            foreach (KeyValuePair<string, Dictionary<string, string>> terminalContent in terminalContents)
                                            {
                                                if (terminalContent.Value["Terminal.ConductingEquipment"].Substring(1) == RDFid)
                                                {
                                                    connectivityNodeRDFID = terminalContent.Value["Terminal.ConnectivityNode"];
                                                    break;
                                                }
                                            }
                                        }

                                        string voltageLevelRDFID =
                                            GetInstanceAttributeValue(ref etsFile,
                                                                      "ConnectivityNode",
                                                                      connectivityNodeRDFID,
                                                                      "ConnectivityNode.ConnectivityNodeContainer");
                                        string baseVoltageRDFID =
                                            GetInstanceAttributeValue(ref etsFile,
                                                                      "VoltageLevel",
                                                                      voltageLevelRDFID,
                                                                      "VoltageLevel.BaseVoltage");
                                        string voltageBaseValue =
                                            GetInstanceAttributeValue(ref etsFile,
                                                                      "BaseVoltage",
                                                                      baseVoltageRDFID,
                                                                      "BaseVoltage.VoltageBase");

                                        if (!intervalSchedTimePointContentsLoaded)
                                        {
                                            intervalSchedTimePointContents = etsFile.GetContents("IntervalSchedTimePoint").Contents;
                                            intervalSchedTimePointContentsLoaded = true;
                                        }

                                        foreach (KeyValuePair<string, Dictionary<string, string>> intervalSchedTimePointContent
                                                 in intervalSchedTimePointContents)
                                        {
                                            if (intervalSchedTimePointContent.Value["IntervalSchedTimePoint.RegularIntervalSchedule"]
                                                                             == regulationScheduleRDFID &&
                                                Int32.Parse(intervalSchedTimePointContent.Value["IntervalSchedTimePoint.SequenceNumber"])
                                                                             == 1)
                                            {
                                                string destinationValue =
                                                    calculationEngine.Evaluate
                                                        (voltageBaseValue +
                                                            " * " +
                                                         intervalSchedTimePointContent.Value["IntervalSchedTimePoint.Value2"]).ToString();

                                                iF.AddMethodAttribute(assignment.Destination,
                                                                      destinationValue,
                                                                      //                                                                      intervalSchedTimePointContent.Value["IntervalSchedTimePoint.Value2"],
                                                                      false,
                                                                      true);
                                                break;
                                            }
                                        }

                                    }
                                    else if (assignment.Attribute.Contains("FindTapChangerIntervalSchedTimePointTargetValue"))
                                    {
                                        string manualValue = parameterValues[3 - 1];

                                        if (manualValue.ToLower() == "true")
                                        {
                                            iF.AddMethodAttribute(assignment.Destination,
                                                                  parameterValues[5 - 1],
                                                                  false,
                                                                  true);

                                        }
                                        else
                                        {
                                            string regulationScheduleRDFID = parameterValues[1 - 1];
                                            string connectivityNodeRDFID = parameterValues[2 - 1];
                                            string transformerWindingRDFID = parameterValues[4 - 1];

                                            if (connectivityNodeRDFID == string.Empty)
                                            {
                                                if (!terminalContentsLoaded)
                                                {
                                                    terminalContents = etsFile.GetContents("Terminal").Contents;
                                                    terminalContentsLoaded = true;
                                                }

                                                foreach (KeyValuePair<string, Dictionary<string, string>> terminalContent in terminalContents)
                                                {
                                                    if (terminalContent.Value["Terminal.ConductingEquipment"] == transformerWindingRDFID)
                                                    {
                                                        connectivityNodeRDFID = terminalContent.Value["Terminal.ConnectivityNode"];
                                                        break;
                                                    }
                                                }
                                            }

                                            if (connectivityNodeRDFID == string.Empty)
                                                continue;

                                            string voltageLevelRDFID =
                                                GetInstanceAttributeValue(ref etsFile,
                                                                          "ConnectivityNode",
                                                                          connectivityNodeRDFID,
                                                                          "ConnectivityNode.ConnectivityNodeContainer");
                                            string baseVoltageRDFID =
                                                GetInstanceAttributeValue(ref etsFile,
                                                                          "VoltageLevel",
                                                                          voltageLevelRDFID,
                                                                          "VoltageLevel.BaseVoltage");
                                            string voltageBaseValue =
                                                GetInstanceAttributeValue(ref etsFile,
                                                                          "BaseVoltage",
                                                                          baseVoltageRDFID,
                                                                          "BaseVoltage.VoltageBase");

                                            if (!intervalSchedTimePointContentsLoaded)
                                            {
                                                intervalSchedTimePointContents = etsFile.GetContents("IntervalSchedTimePoint").Contents;
                                                intervalSchedTimePointContentsLoaded = true;
                                            }

                                            foreach (KeyValuePair<string, Dictionary<string, string>> intervalSchedTimePointContent
                                                     in intervalSchedTimePointContents)
                                            {
                                                if (intervalSchedTimePointContent.Value["IntervalSchedTimePoint.RegularIntervalSchedule"]
                                                                                 == regulationScheduleRDFID &&
                                                    Int32.Parse(intervalSchedTimePointContent.Value["IntervalSchedTimePoint.SequenceNumber"])
                                                                                 == 1)
                                                {
                                                    string destinationValue =
                                                        calculationEngine.Evaluate
                                                            (voltageBaseValue +
                                                                " * " +
                                                            intervalSchedTimePointContent.Value["IntervalSchedTimePoint.Value1"]).ToString();

                                                    iF.AddMethodAttribute(assignment.Destination,
                                                                          destinationValue,
                                                                          false,
                                                                          true);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    else if (assignment.Attribute.Contains("FindTapChangerIntervalSchedTimePointDeadBand"))
                                    {
                                        string manualValue = parameterValues[3 - 1];

                                        if (manualValue.ToLower() == "true")
                                        {
                                            iF.AddMethodAttribute(assignment.Destination,
                                                                  parameterValues[6 - 1],
                                                                  false,
                                                                  true);

                                        }
                                        else
                                        {
                                            string regulationScheduleRDFID = parameterValues[1 - 1];
                                            string connectivityNodeRDFID = parameterValues[2 - 1];
                                            string transformerWindingRDFID = parameterValues[4 - 1];

                                            if (connectivityNodeRDFID == string.Empty)
                                            {
                                                if (!terminalContentsLoaded)
                                                {
                                                    terminalContents = etsFile.GetContents("Terminal").Contents;
                                                    terminalContentsLoaded = true;
                                                }

                                                foreach (KeyValuePair<string, Dictionary<string, string>> terminalContent in terminalContents)
                                                {
                                                    if (terminalContent.Value["Terminal.ConductingEquipment"] == transformerWindingRDFID)
                                                    {
                                                        connectivityNodeRDFID = terminalContent.Value["Terminal.ConnectivityNode"];
                                                        break;
                                                    }
                                                }
                                            }

                                            if (connectivityNodeRDFID == string.Empty)
                                                continue;

                                            string voltageLevelRDFID =
                                                GetInstanceAttributeValue(ref etsFile,
                                                                          "ConnectivityNode",
                                                                          connectivityNodeRDFID,
                                                                          "ConnectivityNode.ConnectivityNodeContainer");
                                            string baseVoltageRDFID =
                                                GetInstanceAttributeValue(ref etsFile,
                                                                          "VoltageLevel",
                                                                          voltageLevelRDFID,
                                                                          "VoltageLevel.BaseVoltage");
                                            string voltageBaseValue =
                                                GetInstanceAttributeValue(ref etsFile,
                                                                          "BaseVoltage",
                                                                          baseVoltageRDFID,
                                                                          "BaseVoltage.VoltageBase");

                                            if (!intervalSchedTimePointContentsLoaded)
                                            {
                                                intervalSchedTimePointContents = etsFile.GetContents("IntervalSchedTimePoint").Contents;
                                                intervalSchedTimePointContentsLoaded = true;
                                            }

                                            foreach (KeyValuePair<string, Dictionary<string, string>> intervalSchedTimePointContent
                                                     in intervalSchedTimePointContents)
                                            {
                                                if (intervalSchedTimePointContent.Value["IntervalSchedTimePoint.RegularIntervalSchedule"]
                                                                                 == regulationScheduleRDFID &&
                                                    Int32.Parse(intervalSchedTimePointContent.Value["IntervalSchedTimePoint.SequenceNumber"])
                                                                                 == 1)
                                                {
                                                    string destinationValue =
                                                        calculationEngine.Evaluate
                                                            (voltageBaseValue +
                                                                " * " +
                                                             intervalSchedTimePointContent.Value["IntervalSchedTimePoint.Value2"]).ToString();

                                                    iF.AddMethodAttribute(assignment.Destination,
                                                                          destinationValue,
                                                                          false,
                                                                          true);
                                                    break;
                                                }
                                            }

                                        }
                                    }
                                }

                                if (addMethodInstance)
                                {
                                    iF.PossibleAttributeComplete(); // add reference to new method created attribute;
                                    iF.AddToMethodElements();       // add the newly created instance to the list of instances.
                                }
                            }
                        }

                        //
                        iF.AddCurrentElement();
                    }

                    iF.MethodComplete();  // Add all the newly created instances

                    Utils.WriteTimeToConsoleAndLog(String.Format("Processed {0} instances of class {1}.", instanceCount, className));
                }
            }

            if (m_IncXmlFile)
            {
                iF.Save();
                irf.Save();
            }

            if (m_ExTextFile)
            {
                ExtensionsOutput.DumpSummary();  //Top off the output file with the Summary Report of unique extensions
            }

            if (m_ExExcelFile)
            {
                ExtensionsOutput.CloseWorkbook();  //Close out the workbook
            }

            if (m_CsqlFile)
            {
                SQLOutput.Cleanup();  //Close the SQL output file
            }

            DialogResult result = MessageBox.Show("Delete ETS Subset files created for this conversion?",
                                                  "ETStoODMSIncremental Output File Creation Complete",
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

        public static string GetInstanceAttributeValue(ref ETSFile etsFile, string className, string rdfID, string attributeDefinition)
        {
            ETSSubsetContents contents = etsFile.GetContents(className);

            if (rdfID[0] == '#')
            {
                rdfID = rdfID.Substring(1);
            }
            Dictionary<string, string> attributeNameAndValue = contents.Contents[rdfID];

            string instanceAttributeValue = attributeNameAndValue[attributeDefinition];

            return instanceAttributeValue;
//            return etsFile.GetContents(className).Contents[rdfID][attributeDefinition];
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
