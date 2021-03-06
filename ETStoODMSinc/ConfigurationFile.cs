﻿/*
 
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
using Microsoft.Office.Interop.Excel;

namespace ETStoODMSIncremental
{
	class ConfigurationFile
	{
		Application configurationFileApplication;
		Workbook configurationWorkbook;

		List<ETSClassConfiguration> etsClassConfigurations;

		public List<ETSClassConfiguration> ETSClassConfigurations
		{
			get
			{
				return etsClassConfigurations;
			}
		}

		//         classname   neededattributes
		public Dictionary<string, List<string>> listOfNeededAttributes;
		public Dictionary<string, Dictionary<string, bool>> neededAttributeIsRDF;

		private string configurationExcelFilename;

		public ConfigurationFile(string configurationFilename)
		{
			Utils.WriteTimeToConsoleAndLog(String.Format("Processing configruation file {0}.", configurationFilename));

			listOfNeededAttributes = new Dictionary<string, List<string>>();
			neededAttributeIsRDF = new Dictionary<string, Dictionary<string, bool>>();

			InitializeAndPreprocess(configurationFilename);
		}

		private void InitializeAndPreprocess(string configurationFilename)
		{
			etsClassConfigurations = new List<ETSClassConfiguration>();

			const int numberOfCellsInRow = 4;

			string[] currentRowContents = new string[4];

			configurationExcelFilename = configurationFilename;

			bool firstRow = true;
			bool secondRow = false;
			bool methodFound = false;

            bool parameterRow = false;
            bool methodSeparatorRow = false;

			try
			{
				configurationFileApplication = new Application();

				configurationWorkbook = configurationFileApplication.Workbooks.Open(configurationExcelFilename);

				foreach (Worksheet worksheet in configurationWorkbook.Sheets)
				{
					string worksheetName = worksheet.Name;

					Utils.WriteTimeToConsoleAndLog("Processing tab " + worksheetName);

					if (worksheetName.Contains("Method"))
					{
						methodFound = true;
					}
					else
					{
						methodFound = false;
					}

					firstRow = true;

					foreach (Range row in worksheet.Rows)
					{

						foreach (Range cell in row.Cells)
						{
							string cellContents;

							if (cell.Value2 == null)
							{
								cellContents = "";
							}
							else
							{
								cellContents = cell.Value2.ToString().Trim();
							}

							currentRowContents[cell.Column - 1] = cellContents;

							if (cell.Column == numberOfCellsInRow)
							{
								break;
							}

						}

						if (firstRow)
						{
							//                            etsClassConfigurations = new List<ETSClassConfiguration>();
							etsClassConfigurations.Add(new ETSClassConfiguration(worksheetName, currentRowContents));
							firstRow = false;

                            if (methodFound)
                            {
                                parameterRow = true;
                            }
                            else
                            {
                                secondRow = true;
                            }
						}
						else if (secondRow)
						{
							etsClassConfigurations[etsClassConfigurations.Count - 1].ClassMapping(currentRowContents);
							secondRow = false;
						}
                        else if (parameterRow)
                        {
                            parameterRow = etsClassConfigurations[etsClassConfigurations.Count - 1].AddMethodParameter(currentRowContents);
                        }
						else
						{
							bool notAllNull = false;

							foreach (string s in currentRowContents)
							{
								if (s != "")
								{
									notAllNull = true;
									break;
								}
							}

							if (notAllNull)
							{
							    etsClassConfigurations[etsClassConfigurations.Count - 1].AddContent(currentRowContents);
							}
							else
							{
								break;
							}
						}
					}
					//
					//break;
					//
				}
			}
			catch (Exception e)
			{
                Utils.WriteTimeToConsoleAndLog(e.Message.ToString());
			}
			finally
			{
				configurationWorkbook.Close();
			}
		}
		public void GenerateDictionaryOfNeededAttributesAndIsRDF()
		{
			foreach (ETSClassConfiguration etsConfig in etsClassConfigurations)
			{
				listOfNeededAttributes.Add(etsConfig.ClassName, etsConfig.GetListOfNeededAttributes());
				neededAttributeIsRDF.Add(etsConfig.ClassName, etsConfig.GetAttributeIsRDFDictionary());
			}
		}
		public Dictionary<string, Dictionary<string, bool>> GenerateDictionaryOfAttributeIsRDF()
		{
			Dictionary<string, Dictionary<string, bool>> attributeIsRDF = new Dictionary<string, Dictionary<string, bool>>();

			return attributeIsRDF;
		}
        public ETSClassConfiguration GetMethod(string methodName)
        {
            ETSClassConfiguration methodClass = etsClassConfigurations[0];

            foreach (ETSClassConfiguration configuration in etsClassConfigurations)
            {
                if (configuration.ClassName == methodName)
                {
                    methodClass = configuration;
                    break;
                }
            }

            return methodClass;
        }
	}
}