using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Excel = Microsoft.Office.Interop.Excel;

namespace ETStoODMSIncremental
{
    class ExtensionsOutput
    {
        string extensionsOFilename;
        string excelFilename;

        public static Boolean CName = true;
        public static Boolean FSheet = true;
        public static Boolean classNameChange = false;
        public static string className = "";

        public static int row = 1;
        public static int col = 1;

        public static StreamWriter EOF = null;
        public static Excel.Application excel_app = new Excel.Application();
        public static SortedDictionary<string, string> defAndDetails = new SortedDictionary<string, string>();

        public static string scomp1 = "newKind";
        public static string scomp2 = "NewClass";
        public static string dataType;

        public static Excel.Worksheet sheet = null;
        public static Excel.Workbook workbook = null;
        public static Excel.Worksheet excelFirstSheet;

        public ExtensionsOutput(string extensionsOFilename, string excelFilename)
        {
            this.extensionsOFilename = extensionsOFilename;
            this.excelFilename = excelFilename;

            EOF = new StreamWriter(extensionsOFilename);
            EOF.WriteLine("NameSpace:  <http://iec.ch/TC57/2013/CIM-schema-cim16#>");

            //Create the workbook
            workbook = excel_app.Workbooks.Add(Type.Missing);
            excelFirstSheet = workbook.Worksheets[1];
            workbook.SaveAs(excelFilename);

        }

        public static void LoadDictionary(ETSClassConfiguration classConfiguration, string classMappingName, string excelFilename)
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

            sheet.Name = classMappingName;

            if (CName && classMappingName.Equals("GeneratingUnit"))
            {
                sheet.Name = "SynchronousMachine";  //This is to handle that SynchMach/GeneratingUnit situation in the Workbook, since we're using the classmapping name
                CName = false;
            }

            if (classMappingName.Equals("AEP_CBType"))
            {
                sheet.Name = "CBType";
            }

            if (classMappingName.Equals("AEP_LoadType"))
            {
                sheet.Name = "LoadType";
            }

            if (classMappingName.Equals("ConformLoad"))
            {
                sheet.Name = "EquipmentLoad";
            }

            EOF.WriteLine("Class Mapping Name: " + sheet.Name);  //Flag the Class

            foreach (Extension extension in classConfiguration.Extensions)
            {

                if (className.Equals(classMappingName))
                {
                    classNameChange = false;
                }
                else
                {
                    classNameChange = true;
                    className = classMappingName;
                }


                try
                {
                    defAndDetails.Add(extension.Definition, extension.Details);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Duplicate Extension Found: " + extension.Definition);
                }

                Console.WriteLine();
                Console.WriteLine("Class Mapping Name: " + sheet.Name);

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

                sheet.Rows[row].Cells[col].Value = elemnt[0].ToString();
                if (dataType.Equals("association"))
                    {
                    if (extension.Details.Length < 13 )
                    {
                        EOF.Write("    " + elemnt[0] + "    dataType: " + dataType);
                        sheet.Rows[row].Cells[col + 1].Value = dataType.ToString();
                    }else
                    {
                        EOF.Write("    " + elemnt[0] + "    ");
                    }
                }
                else
                {
                    EOF.Write("    " + elemnt[0] + "    dataType: " + dataType);
                    sheet.Rows[row].Cells[col + 1].Value = dataType.ToString();
                }

            sheet.Rows[row].Cells[col + 2].Value = extension.Details.ToString();

                SQLOutput.BuildOutTable(elemnt[0].ToString(), dataType.ToString(), extension.Details.ToString(), classNameChange, classMappingName);

                row++;

                EOF.WriteLine("    " + extension.Details);                       //Spit out as-is for right now

                EOF.WriteLine(); //Line spacer

                Console.WriteLine(" Extension: " + extension.Definition);
                Console.WriteLine(" Details: " + extension.Details);
            }

            EOF.WriteLine("\n\n");  //Spacer lines
            row = 1;
            workbook.Close(true, Type.Missing, Type.Missing);
        }       

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

