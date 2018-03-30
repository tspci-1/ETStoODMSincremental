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
using CalcEngine;

namespace ETStoODMSIncremental
{
	class Program
	{
        [STAThread]
        static void Main(string[] args)
		{
			ConfigurationFile configurationFile;

			string configurationFilename;
			string configurationPath;
			string alstomETSFilename;
			string alstomETSPath;

			OpenFileDialog configuration_ofd = new OpenFileDialog();

			configuration_ofd.Title = "Select ETS to ODMS Incremental Configuration File";
			configuration_ofd.Filter = "Excel File (*.xlsx)|*.xlsx";
			configuration_ofd.Multiselect = false;

			if (configuration_ofd.ShowDialog() == DialogResult.OK)
			{
				configurationFilename = configuration_ofd.FileName;
				configurationPath = Path.GetDirectoryName(configurationFilename);
			}
			else
			{
				Console.WriteLine("Configuration file selection canceled. Exiting");
				System.Threading.Thread.Sleep(5000);
				return;
			}

			configuration_ofd.Dispose();

			Utils.CreateLog(new StreamWriter
							   (Path.Combine
								   (configurationPath,
									Path.GetFileNameWithoutExtension(configurationFilename)
										 + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + "_LOG.log")));

			OpenFileDialog ets_ofd = new OpenFileDialog();

			ets_ofd.Title = "Select Alstom ETS file";
			ets_ofd.Filter = "ETS File (*.ets)|*.ets";
			ets_ofd.Multiselect = false;

			if (ets_ofd.ShowDialog() == DialogResult.OK)
			{
				alstomETSFilename = ets_ofd.FileName;
				alstomETSPath = Path.GetDirectoryName(alstomETSFilename);
			}
			else
			{
				Console.WriteLine("Alstom ETS file selection canceled. Exiting");
				System.Threading.Thread.Sleep(5000);
				return;
			}

			ets_ofd.Dispose();

            Utils.WriteTimeToConsoleAndLog("ETStoODMSIncremental invoked with configuration file " + configurationFilename);
            Utils.WriteTimeToConsoleAndLog(" and ETS file " + alstomETSFilename);

			configurationFile = new ConfigurationFile(configurationFilename); 

			configurationFile.GenerateDictionaryOfNeededAttributesAndIsRDF();

			ETSFile etsFile = new ETSFile(alstomETSFilename);

			etsFile.GenerateSubsetFiles(ref configurationFile.listOfNeededAttributes);

			etsFile.AcquireSubsetContents(ref configurationFile.listOfNeededAttributes, ref configurationFile.neededAttributeIsRDF);

			string incrementalFilename = Path.Combine
											(Path.GetDirectoryName(alstomETSFilename),
											 Path.GetFileNameWithoutExtension(alstomETSFilename)
												 + "_incremental_" + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".xml");

			IncrementalFile iF = new IncrementalFile(incrementalFilename, etsFile.GetNamespaceDefinition("rdf"));

            //Create the Extension output filename
            string ExtensionsOFilename = Path.Combine
                                            (Path.GetDirectoryName(alstomETSFilename),
                                             "ExtensionsOutputFile-"
                                                 + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".txt");

            string excelFilename = Path.Combine
                                           (Path.GetDirectoryName(alstomETSFilename),
                                            "ExtensionsOutput- "
                                             + DateTime.Now.ToString("_yyyyMMdd_HHmmss") + ".xlsx");

            string SQLOutputFilename = Path.Combine
                                           (Path.GetDirectoryName(alstomETSFilename),
                                            "Customer.sql");

            ExtensionsOutput oF = new ExtensionsOutput(ExtensionsOFilename, excelFilename);  //create the Extensions output files
            SQLOutput SQLOF1 = new SQLOutput(SQLOutputFilename);  //Create the sql output file used for ODMS

            CalcEngine.CalcEngine calculationEngine = new CalcEngine.CalcEngine();


            foreach (ETSClassConfiguration classConfiguration in configurationFile.ETSClassConfigurations)
			{
				string className = classConfiguration.ClassName;
				string classMappingName = classConfiguration.ClassMappingName;
                bool newUndefinedClass = classConfiguration.NewUndefinedClass;

				iF.NewClass(className, classMappingName);

				Dictionary<string, string> attributeAndDestination = new Dictionary<string, string>();
				Dictionary<string, bool> attributeAndIsRDF = new Dictionary<string, bool>();

                Utils.WriteTimeToConsoleAndLog(String.Format("Adding instances to incremental file for {0}", className));

                ExtensionsOutput.LoadDictionary(classConfiguration, classMappingName, excelFilename); // Create entries in the output file and load the summary dictionary

                foreach (Assignment assignment in classConfiguration.Assignments)
				{
                    if (!attributeAndDestination.ContainsKey(assignment.Attribute))
                    {
                        attributeAndDestination.Add(assignment.Attribute, assignment.Destination);
                        attributeAndIsRDF.Add(assignment.Attribute, assignment.AttributeIsRDF);
                    }
				}

                int instanceCount = 0;

                foreach (KeyValuePair<String, Dictionary<string,string>> content in etsFile.GetContents(className).Contents)
                {
                    string RDFid = content.Key;

					iF.NewInstance(RDFid, newUndefinedClass);

					foreach (KeyValuePair<string, string> attributeAndValue in content.Value)
					{
						string value = attributeAndValue.Value;

						if (value == string.Empty)
							continue;

						string attribute = attributeAndValue.Key;

                        if (attributeAndDestination.ContainsKey(attribute))
                        {
                            string destination = attributeAndDestination[attribute];
                            bool isRDF = attributeAndIsRDF[attribute];

                            iF.AddAttribute(destination, value, isRDF, newUndefinedClass);
                        }
					}

                    // Add calculation here
                    foreach (Calculation calculation in classConfiguration.Calculations)
                    {
                        string equation = calculation.Equation;

                        foreach (string operand in calculation.Operands)
                        {
                            if (operand.Contains('.'))
                            {
                                equation = equation.Replace(operand, content.Value[operand]);
                            }
                        }

                        string value = calculationEngine.Evaluate(equation).ToString();

                        iF.AddAttribute(calculation.Target, value, false, false);
                    }
                    
                    //
                    iF.AddCurrentElement();
                }
                Utils.WriteTimeToConsoleAndLog(String.Format("Processed {0} instances of class {1}.", instanceCount, className));
			}

			iF.Save();

            ExtensionsOutput.DumpSummary();     //Top off the output file with the Summary Report of unique extensions
            SQLOutput.Cleanup();  //Close the SQL output file

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
