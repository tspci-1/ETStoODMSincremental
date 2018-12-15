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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*Per ODMS SME--
 * 
 * For resourceType:
 * 1 - is for Class
 * 2 - is Enumeration
 * 3 - is Property or Association
 * */



namespace ETStoODMSIncremental
{
    class SQLOutput
    {
        string SQLOutputFilename;
        public static StreamWriter SQLOF = null;

        public static string className = "";
        public static string TabClassName = "";
        public static string RetainedClassName = "";
        public static string subattribute = "";
        public static string subClassOf = "";
        public static Guid g, g1;
        public static SortedDictionary<string, string> ClassAttributes = new SortedDictionary<string, string>();  //used to filter out duplicates

        /* Create Table strings
         * 
         * First is for Parents in CIM: namespace
         * Seond is for Parents in PTI: namespace
         * See ODMS AddClass SP for usage
         * */

        /*      public static string createTable1 = "\nCREATE TABLE AEP_{0}\n" +
                                                  "(OID uniqueidentifier, CONSTRAINT PK_AEP_{0} PRIMARY KEY NONCLUSTERED (OID))\n" +
                                                  "ALTER TABLE  [AEP_{0}] ADD CONSTRAINT FK_AEP_{0}Base FOREIGN KEY(OID) REFERENCES {1}(OID)\n" +
                                                  "ON DELETE CASCADE;\n" +
                                                  "EXEC AddClass '{{{2}}}','cim:{1}', 'aep:AEP_{0}', 0, 0;\n" +
                                                  "GO\n "; */
        public static string createTable1 =  "\nEXEC AddClass '{{{2}}}','cim:{1}', 'aep:AEP_{0}', 1, 0;\n" +
                                             "GO\n ";


        /* public static string createTable2 = "\nCREATE TABLE AEP_{0}\n" +
                                             "(OID uniqueidentifier, CONSTRAINT PK_AEP_{0} PRIMARY KEY NONCLUSTERED (OID))\n" +
                                             "ALTER TABLE  [AEP_{0}] ADD CONSTRAINT FK_AEP_{0}Base FOREIGN KEY(OID) REFERENCES {1}(OID)\n" +
                                             "ON DELETE CASCADE;\n" +
                                             "EXEC AddClass '{{{2}}}','pti:{1}', 'aep:AEP_{0}', 0, 0;\n" +
                                             "GO\n "; */
        public static string createTable2 =  "\nEXEC AddClass '{{{2}}}','pti:{1}', 'aep:AEP_{0}', 0, 0;\n" +
                                             "GO\n  ";

     /*   public static string alterTable = "ALTER TABLE {0} \nADD CONSTRAINT FK_{1} FOREIGN KEY(OID) REFERENCES {0}(OID) ON DELETE CASCADE;\nGO\n"; */

        public static string createTableName="";
        public static Boolean createTableNameFlag = false;

        /*Alter Table strings
         * 
         * First is for when parent class has not been explicitly specified
         * Second is when Parent class has been specified
         * */

        /* public static string AssociationString1 = "ALTER TABLE  [{0}] \n ADD [FK_{0}_{1}] uniqueidentifier NULL;\n ALTER TABLE  [{0}] \n" +
                                                                               "ADD CONSTRAINT [FK_{0}_{1}] FOREIGN KEY(OID) REFERENCES {1}(OID);\nGO\n"; */
        public static string AssociationString1 = "EXEC AddAssociation '{0}', 'aep:{2}', 'aep:{2}.{3}','{1}','cim:{3}.{2}',NULL, 1,'M:0..1', 'M:0..1', 'AEP';";


        public static string AssociationString2 = "ALTER TABLE  [{0}] \n ADD [FK_{0}_{1}] uniqueidentifier NULL;\n ALTER TABLE  [{0}] \n" +
                                                                                "ADD CONSTRAINT [FK_{0}_{1}] FOREIGN KEY(OID) REFERENCES {2}(OID);\nGO\n";

        /* Args for addProp
    * {0} = NEWID()
    * {1} = className
    * {2} = className.attribute
    * {3} = attribute
    * {4} = dataType
    * */
        /* AddProperty strings
        * 
        * First is for when aep attribute is going into existing CIM class
        * Second is for when aep attribute is going into existing AEP class
        * */

        public static string addProp1 = "EXEC AddProperty '{{{0}}}', 'cim:{1}', 'aep:{2}', '{3}', '{4}', NULL, 'M:0..1', 'AEP';" +
                                                              "\nUPDATE Resources SET URI = 'aep:AEP_{2}' WHERE URI = 'aep:{2}';\nGO\n";

        public static string addProp2 = "EXEC AddProperty '{{{0}}}', 'aep:AEP_{1}', 'aep:AEP_{2}', '{3}', '{4}', NULL, 'M:0..1', 'AEP'; \nGO\n\n";

        /* Constructor
         * 
         * create output file for the SQL commands to get stuffed into
         * */

        public SQLOutput(string SQLOutputFilename)
        {
            this.SQLOutputFilename = SQLOutputFilename;

            SQLOF = new StreamWriter(SQLOutputFilename);
        }

        /* Main workhorse loop
         * 
         * */

        public static void BuildOutTable(string attribute, string dataType, string inheritsFrom, Boolean classNameChange, string tClassName)
        { // This code will assume the inherit statement is with the first extension element for the class. 

            /* Removed the need to edit the entire configuration workbook
             * 
             * */

            if (attribute.Contains("AEP_"))   
            {
                attribute = attribute.Substring(4);
            }

            /*Trigger on a callsname change
             * Happens everytime you go to another tab in the workbook
             * 
             * */

            if (classNameChange)
            { //Each time there's a classmapping change
                createTableNameFlag = false;
                createTableName = "";
                RetainedClassName = tClassName;
            }

            /*ODMS frowns on duplicate property/resource entries
             * 
             * */

            try
            {
                ClassAttributes.Add(attribute, RetainedClassName);  //Should filter out duplicate attributes
            }
            catch (ArgumentException)
            {
                return;
            }

            /* Special case: Substation.Region already exists
             * ODMS considers .region a match for .Region and complains about duplicates
             * 
             * */

            if (attribute.Equals("Substation.region")) //This is already in the CIM Substation.Region
            {
                return;
            }


            if (classNameChange)
            { // Depends on whether table is in cim namespace or pti namespace
                if (!inheritsFrom.Equals("") && !attribute.Contains("."))
                { //This is when we are creating a table

                    g = Guid.NewGuid();
                    createTableNameFlag = true;
                    createTableName = attribute;
                    subClassOf = inheritsFrom.Trim().Substring(14);
                    SQLOF.WriteLine(createTable1, createTableName, subClassOf, g.ToString());
                    return;
                }
                else
                    if (!attribute.Contains("."))
                {
                    g = Guid.NewGuid(); 
                    createTableNameFlag = true;
                    createTableName = attribute;
                    subClassOf = "Resources";  //Sort of like the Elephant graveyard, where all classes without parents go to link...
                    SQLOF.WriteLine(createTable2, createTableName, subClassOf, g.ToString());
                    return;
                }

                createTableNameFlag = false;  //Remains true while we're still adding the same class stuff.  
                createTableName = "";         //When we change classNames, if we don't need to create a table then it goes false again.

            }

            /*Grab the first and last
             * Not supposed to have attributes with more than a classname 
             * and attribute name separated by a dot.  Or so I've been told.
             * If the code upstream doesn't take care of it, here's where you
             * could throw a flag and call for a timeout to fix it.  5 yd penalty.
             * 
             * */

            className = attribute.Split('.').First();
            subattribute = attribute.Split('.').Last();

            if (dataType.Equals("association")) //This may need to be reviewed and tested to see when assocstr-2 would be usedop
            {
                g = Guid.NewGuid();
                g1 = Guid.NewGuid();
                if (inheritsFrom.Equals(""))
                {//Do the default parent class
                    SQLOF.WriteLine(AssociationString1, g.ToString(), g1.ToString(),  RetainedClassName, subattribute);
                    return;
                }
                else
                {//Do the parent class explicitly specified
                 //    SQLOF.WriteLine(AssociationString2, g.ToString(), RetainedClassName, subattribute, inheritsFrom);
                    SQLOF.WriteLine(AssociationString2, RetainedClassName, subattribute, inheritsFrom);
                    return;
                }
            }

            /* CIM dataTypes are not SQL Server dataTypes
             * Must perform compatibilty transform
             * 
             * */

            switch (dataType) //Convert dataType from CIM to SQL Server
            {
                case "ActivePower": { dataType = "float"; break; }
                case "Boolean": { dataType = "bit"; break; }
                case "Float": { dataType = "float"; break; }
                case "Integer": { dataType = "int"; break; }
                case "PerCent": { dataType = "float"; break; }
                case "Reactance": { dataType = "float"; break; }
                case "ReactivePower": { dataType = "float"; break; }
                case "Resistance": { dataType = "float"; break; }
                case "String": { dataType = "varchar(256)"; break; }
                case "Voltage": { dataType = "float"; break; }
                case "VoltageLevel": { dataType = "float"; break; }
            }

            g = Guid.NewGuid();
            if (!createTableNameFlag)
            { //Also deals with two common duplicates as well as whether its a new 'AEP_ ' extension or already in CIM
                if (className.Equals("AlarmableObject"))
                {
                    SQLOF.WriteLine(addProp2, g.ToString(), className, attribute, subattribute, dataType);
                }
                else
                {
                    SQLOF.WriteLine(addProp1, g.ToString(), className, attribute, subattribute, dataType);
                }
            }
            else
            {
                if (className.Equals("IdentifiedObject"))
                {
                    SQLOF.WriteLine(addProp1, g.ToString(), className, attribute, subattribute, dataType);
                }
                else
                {
                    SQLOF.WriteLine(addProp2, g.ToString(), className, attribute, subattribute, dataType);
                }
            }
            return;
        }

        /*Method to tidy up the output stream
         * 
         * */

        public static void Cleanup()
        {
            SQLOF.Close();
        }
    }
}
