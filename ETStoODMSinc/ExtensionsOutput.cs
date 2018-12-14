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
using System.IO;
using System.Text;
using System.Collections.Generic;
using Excel = Microsoft.Office.Interop.Excel;

namespace ETStoODMSIncremental
{
    class ExtensionsOutput
    {
        public static string extensionsOFilename; //Output Text File
        public static string excelFilename; //Output Excel File

        public static Boolean FSheet = true;
        public static Boolean classNameChange = false;
        public static string className = "";

        public static int row = 1;
        public static int col = 1;

        public static StreamWriter EOF = null;
        public static Excel.Application excel_app = new Excel.Application(); //Here is where the new Excel object instance gets created
        public static SortedDictionary<string, string> defAndDetails = new SortedDictionary<string, string>(); //Using this makes the text file summary section of all attributes easier for a human to read through

        /* newKind/NewClass are for furture use
         * See David M.'s config file rules document
         * 
         * */
        public static string scomp1 = "newKind";
        public static string scomp2 = "NewClass";
        public static string dataType;

        public static Excel.Worksheet sheet = null;
        public static Excel.Workbook workbook = null;
        public static Excel.Worksheet excelFirstSheet;

        /*Constructor for both file types
         * 
         * */

        public ExtensionsOutput(string FileName, string FileType)
        {
            switch (FileType)
            {
                case "TEXT":
                    {
                        extensionsOFilename = FileName;
                        EOF = new StreamWriter(extensionsOFilename);
                        EOF.WriteLine("NameSpace:  <http://iec.ch/TC57/2013/CIM-schema-cim16#>");
                        break;
                    }
                case "EXCEL":
                    {
                        excelFilename = FileName;
                        workbook = excel_app.Workbooks.Add(Type.Missing);
                        excelFirstSheet = workbook.Worksheets[1];
                        workbook.SaveAs(excelFilename);
                        break;
                    }
                default:
                    {
                        Utils.WriteTimeToConsoleAndLog("Invalid type of file passed to ExtensionsOutput module.  FileType: " + FileType);
                        //Need to think about how to politely terminate program if this ever happened.  Nothing in place for it, yet.
                        break;
                    }
            }
        }

        /* Main Workhorse loop
         * 
         * */

        public static void LoadDictionary(ETSClassConfiguration classConfiguration, string classMappingName)
        {

            string interimClassName = ""; //Used for adjusting the tab name when necessary.  See special cases comment for config file
            interimClassName = classMappingName;

            if (Program.m_ExExcelFile)
            {
                workbook = excel_app.Workbooks.Open(
                 excelFilename,
                 Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                 Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                 Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                 Type.Missing, Type.Missing);

                if (!FSheet) //Actually start on the very first worksheet that is created by default when workbook is created
                {
                    sheet = (Excel.Worksheet)workbook.Sheets.Add(
                   Type.Missing, workbook.Sheets[workbook.Sheets.Count],
                   1, Excel.XlSheetType.xlWorksheet);
                }
                else
                {
                    sheet = excelFirstSheet;
                    FSheet = false;
                }
            }

            /* Two special cases
             * The worksheet needed to have the AEP_ stipped off
             * This was the quick way to do it
             * 
             * */

            if (classMappingName.Equals("AEP_CBType"))
            {
                interimClassName = "CBType";
            }

            if (classMappingName.Equals("AEP_LoadType"))
            {
                interimClassName = "LoadType";
            }

            /* Special case
             * May need to be removed at some point
             * 
             * */

            if (classMappingName.Equals("ConformLoad"))
            {
                interimClassName = "EquipmentLoad";
            }

            if (Program.m_ExExcelFile)
            {
                sheet.Name = interimClassName;
            }

            if (Program.m_ExTextFile)
            {
                EOF.WriteLine("Class Mapping Name: " + interimClassName);  //Flag the Class
            }


            /* Peel them out of the object we were passed
             * 
             * */

            foreach (Extension extension in classConfiguration.Extensions)
            {

                if (className.Equals(classMappingName))  //Determine when we've changed classes aka tabs in the worksheet
                {
                    classNameChange = false;
                }
                else
                {
                    classNameChange = true;
                    className = classMappingName;
                }

                /* This just filters per tab/class, not for the entire config file 
                 * 
                 * */
                try
                {
                    defAndDetails.Add(extension.Definition, extension.Details); //Filter out duplicates
                }
                catch (ArgumentException)
                {
                    Utils.WriteTimeToConsoleAndLog("Duplicate Extension Found: " + extension.Definition);
                }

                Utils.WriteTimeToConsoleAndLog("");
                Utils.WriteTimeToConsoleAndLog("Class Mapping Name: " + interimClassName);

                //Strip out any multiple spaces from the attribute
                string cString = CollapseSpaces.CSpaces(extension.Definition);
                string[] elemnt = cString.Split(' ');

                if (elemnt.Length.Equals(1))
                {
                    dataType = "association";  // If the attribute doesn't have a dataType assume an association
                }
                else
                {
                    dataType = elemnt[1];
                }

                if (Program.m_ExExcelFile)
                {
                    sheet.Rows[row].Cells[col].Value = elemnt[0].ToString();
                }

                if (dataType.Equals("association"))  //Just some adjusting to alter how both extension output files display the association entries
                {
                    if (extension.Details.Length < 13)
                    {
                        if (Program.m_ExTextFile)
                        {
                            EOF.Write("    " + elemnt[0] + "    dataType: " + dataType);
                        }

                        if (Program.m_ExExcelFile)
                        {
                            sheet.Rows[row].Cells[col + 1].Value = dataType.ToString();
                        }

                    }
                    else
                    {
                        if (Program.m_ExTextFile)
                        {
                            EOF.Write("    " + elemnt[0] + "    ");
                        }
                    }
                }
                else
                {
                    if (Program.m_ExTextFile)
                    {
                        EOF.Write("    " + elemnt[0] + "    dataType: " + dataType);
                    }

                    if (Program.m_ExExcelFile)
                    {
                        sheet.Rows[row].Cells[col + 1].Value = dataType.ToString();
                    }
                }

                if (Program.m_ExExcelFile)
                {
                    sheet.Rows[row].Cells[col + 2].Value = extension.Details.ToString();
                }

                /*Toss the current object and info to the SQL output file builder
                 * 
                 * */

                if (Program.m_CsqlFile)
                {
                    SQLOutput.BuildOutTable(elemnt[0].ToString(), dataType.ToString(), extension.Details.ToString(), classNameChange, classMappingName);
                }

                if (Program.m_ExExcelFile)
                {
                    row++;
                }

                if (Program.m_ExTextFile)
                {
                    EOF.WriteLine("    " + extension.Details);                       //Spit out as-is for right now

                    EOF.WriteLine(); //Line spacer
                }

                Utils.WriteTimeToConsoleAndLog(" Extension: " + extension.Definition);
                Utils.WriteTimeToConsoleAndLog(" Details: " + extension.Details);
            }

            if (Program.m_ExTextFile)
            {
                EOF.WriteLine("\n\n");  //Spacer lines
            }

            if (Program.m_ExExcelFile)
            {
                row = 1; //reset for the next tab in the output workbook
            }
        }

        /* Method to close the Excel workbook
            * 
            * */

        public static void CloseWorkbook()
        {
            workbook.Close(true, Type.Missing, Type.Missing);  //Wrap up the excel workbook
        }

        /* Method the writeout the summary list and close the text file
         * 
         * */

        public static void DumpSummary()
        {
            EOF.WriteLine(" \n\n\nSummary Output");

            foreach (KeyValuePair<string, string> att in defAndDetails)
            {
                EOF.WriteLine(att.Key + "   ", att.Value);
            }

            EOF.Close(); //Close the extensions listing file
        }
    }

    /* Utility to squeegee out the extra spaces
     * 
     * */

    public static class CollapseSpaces
    {
        public static string CSpaces(this string str_input)
        {
            if (str_input == null)
            {
                return null;
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder(str_input.Length);
                int i = 0;
                foreach (char c in str_input)
                {
                    if (c != ' ' || i == 0 || str_input[i - 1] != ' ')
                        stringBuilder.Append(c);
                    i++;
                }
                return stringBuilder.ToString();
            }
        }
    }

}

