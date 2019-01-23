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
        public static string multiplicity = "";
        public static string multiplicity1 = "";
        public static string multiplicity2 = "";
        public static string targetClass = "";
        public static Guid g, g1;
        public static SortedDictionary<string, string> ClassAttributes = new SortedDictionary<string, string>();  //used to filter out duplicates
        public static string target2MapTable = "";
        public static string col1MapName = "";
        public static string col2MapName = "";

        /* Create Table strings
         * 
         * First is for Parents in CIM: namespace
         * Seond is for Parents in PTI: namespace
         * See ODMS AddClass SP for usage
         * */

        public static string createTable1 = "\nEXEC AddClass '{{{2}}}','cim:{1}', 'aep:AEP_{0}', 1, 0;\n" +
                                             "GO\n ";

        public static string createTable2 = "\nEXEC AddClass '{{{2}}}','pti:{1}', 'aep:AEP_{0}', 0, 0;\n" +
                                             "GO\n  ";

        public static string createTable3 = "\nEXEC AddClass '{{{2}}}','aep:{1}', 'aep:{0}', 1, 0;\n" + // for the manyTOmany tables
                                             "GO\n ";


        public static string createTableName = "";
        public static Boolean createTableNameFlag = false;
        public static Boolean aepAttributeFlag = false;
        public static Boolean propertyConstraintFlag = false;

        /* * 
         * First is for when parent class has not been explicitly specified
         * Second is when Parent class has been specified
         * Third is when Parent class is a CIM Class
         * */

        public static string AssociationString1 = "EXEC AddAssociation '{0}', 'aep:{2}', 'aep:{2}.{3}','{1}','cim:{3}.{2}',NULL, 1,'{4}', '{5}', 'AEP';\nGO\n";

        public static string AssociationString2 = "EXEC AddAssociation '{0}', 'cim:{2}', 'cim:{2}.{3}','{1}','aep:{3}.{2}',NULL, 1,'{4}', '{5}', 'AEP';\nGO\n";

        public static string AssociationString3 = "EXEC AddAssociation '{0}', 'cim:{3}', 'cim:{3}.{2}','{1}','aep:{2}.{3}','{4}ID', 1,'{5}', '{6}', 'AEP';\nGO\n";

        public static string AssociationString4 = "UPDATE Resources \nSET URI = 'aep:{3}.{4}' \nWHERE URI = 'cim:{3}.{2}' \nGO\n\n" +
                                                  "UPDATE Resources \nSET URI = 'aep:{4}.{3}' \nWHERE URI = 'aep:{2}.{3}' \nGO\n\n" +
                                                  "UPDATE Property  \nSET rdf_ID = '{3}.{4}' \nWHERE rdf_ID = '{3}.{2}' \nGO\n\n" +
                                                  "UPDATE Property \nSET rdf_ID = '{4}.{3}', \ncims_multiplicity = '{5}', \n" +
                                                  "MultiplicityID = (Select OID FROM Resources r where r.URI = '{5}') \nWHERE rdf_ID = '{2}.{3}' \nGO\n\n";

        public static string AssociationString5 = "UPDATE Resources \nSET URI = 'aep:{3}.{4}' \nWHERE URI = 'cim:{3}.{2}' \nGO\n\n" +
                                                  "UPDATE Resources \nSET URI = 'aep:{4}.{3}s' \nWHERE URI = 'aep:{2}.{3}' \nGO\n\n" +
                                                  "UPDATE Property  \nSET rdf_ID = '{3}.{4}' \nWHERE rdf_ID = '{3}.{2}' \nGO\n\n" +
                                                  "UPDATE Property \nSET rdf_ID = '{4}.{3}s', \ncims_multiplicity = '{5}', \n" +
                                                  "MultiplicityID = (Select OID FROM Resources r where r.URI = '{5}') \nWHERE rdf_ID = '{2}.{3}' \nGO\n\n";

        public static string AssociationString6 = "EXEC AddAssociation '{0}', 'aep:{2}', 'aep:{2}.{3}','{1}','aep:{3}.{2}','{4}', 1,'{5}', '{6}', 'AEP';\nGO\n";

        public static string RAString60 = "INSERT INTO Resources (OID, URI, ResourceType, TableName, ColumnName)\n" +
                                                         "SELECT '{0}', 'aep:{2}.{3}', 3, '{4}', NULL \nGO\n\n" +

                                                         "INSERT INTO Resources (OID, URI, ResourceType, TableName, ColumnName)\n" +
                                                         "SELECT '{1}', 'aep:{3}.{2}', 3, '{2}', '{3}'\nGO\n\n" +

                                                         "ALTER TABLE Property NOCHECK CONSTRAINT ALL\nGO\n\n" +

                                                         "INSERT INTO Property (PropertyID, ClassID, RangeID, MultiplicityID, InverseRoleNameID, rdf_ID," +
                                                         " rdfs_label, rdfs_domain, rdfs_range, cims_profile, cims_multiplicity, cims_inverseRoleName)\n" +

                                                         "SELECT '{0}', Class.ClassID, rng.ClassID, mult.OID, '{1}'," +
                                                         "'{2}.{3}', '{3}', '{2}', '{3}','AEP'," +
                                                         "'M:0..*', '{3}.{2}' \nFROM Class, Class rng, Resources mult" +
                                                         "\n WHERE Class.rdf_ID = '{2}'" +
                                                         "\n AND rng.rdf_ID = '{4}'" +
                                                         "\n AND mult.URI = 'cims:' + 'M:0..*' \nGO\n\n" +

                                                         "INSERT INTO Property (PropertyID, ClassID, RangeID, MultiplicityID, InverseRoleNameID, rdf_ID, rdfs_label, rdfs_domain, rdfs_range," +
                                                         "cims_profile, cims_multiplicity, cims_inverseRoleName)\n" +

                                                         "SELECT '{1}', Class.ClassID, rng.ClassID, mult.OID, '{0}'," +
                                                         "'{3}.{2}', '{2}', '{3}', '{2}', 'AEP'," +
                                                         "'M:0..*', '{2}.{3}' \nFROM Class, Class rng, Resources mult" +
                                                         "\n WHERE Class.rdf_ID = '{4}'" +
                                                         "\n AND rng.rdf_ID = '{2}'" +
                                                         "\n AND mult.URI = 'cims:' + 'M:0..*' \nGO\n\n" +

                                                         "ALTER TABLE Property WITH CHECK CHECK CONSTRAINT ALL\nGO\n\n" +

                                                         "ALTER TABLE {2} ADD {3} uniqueidentifier NULL FOREIGN KEY REFERENCES {4} (OID)\nGO\n\n";

        public static string RAString61 = "INSERT INTO Resources (OID, URI, ResourceType, TableName, ColumnName)\n" +
                                                 "SELECT '{0}', 'aep:{2}.{3}', 3, '{4}', NULL \nGO\n\n" +

                                                 "INSERT INTO Resources (OID, URI, ResourceType, TableName, ColumnName)\n" +
                                                 "SELECT '{1}', 'aep:{3}.{2}', 3, '{2}', '{3}'\nGO\n\n" +

                                                 "ALTER TABLE Property NOCHECK CONSTRAINT ALL\nGO\n\n" +

                                                 "INSERT INTO Property (PropertyID, ClassID, RangeID, MultiplicityID, InverseRoleNameID, rdf_ID," +
                                                 " rdfs_label, rdfs_domain, rdfs_range, cims_profile, cims_multiplicity, cims_inverseRoleName)\n" +

                                                 "SELECT '{0}', Class.ClassID, rng.ClassID, mult.OID, '{1}'," +
                                                 "'{2}.{3}', '{3}', '{2}', '{3}','AEP'," +
                                                 "'M:0..*', '{3}.{2}' \nFROM Class, Class rng, Resources mult" +
                                                 "\n WHERE Class.rdf_ID = '{2}'" +
                                                 "\n AND rng.rdf_ID = '{4}'" +
                                                 "\n AND mult.URI = 'cims:' + 'M:0..*' \nGO\n\n" +

                                                 "INSERT INTO Property (PropertyID, ClassID, RangeID, MultiplicityID, InverseRoleNameID, rdf_ID, rdfs_label, rdfs_domain, rdfs_range," +
                                                 "cims_profile, cims_multiplicity, cims_inverseRoleName)\n" +

                                                 "SELECT '{1}', Class.ClassID, rng.ClassID, mult.OID, '{0}'," +
                                                 "'{3}.{2}', '{2}', '{3}', '{2}', 'AEP'," +
                                                 "'M:0..*', '{2}.{3}' \nFROM Class, Class rng, Resources mult" +
                                                 "\n WHERE Class.rdf_ID = '{4}'" +
                                                 "\n AND rng.rdf_ID = '{2}'" +
                                                 "\n AND mult.URI = 'cims:' + 'M:0..*' \nGO\n\n" +

                                                 "ALTER TABLE Property WITH CHECK CHECK CONSTRAINT ALL\nGO\n\n" +

                                                 "ALTER TABLE {2} ADD {3} uniqueidentifier NULL FOREIGN KEY REFERENCES {4} (OID)\nGO\n\n";

        public static string AssociationString7 = "UPDATE Resources \nSET URI = 'aep:{1}1.{0}' \nWHERE URI = 'aep:{1}.{0}' \nGO\n\n" +
                                                  "UPDATE Resources \nSET URI = 'aep:{0}.{1}1' \nWHERE URI = 'aep:{0}.{1}' \nGO\n\n" +
                                                  "UPDATE Property  \nSET rdf_ID = '{1}1.{0}' \nWHERE rdf_ID = '{1}.{0}' \nGO\n\n" +
                                                  "UPDATE Property  \nSET cims_inverseRoleName = '{1}1.{0}' \nWHERE cims_inverseRoleName = '{1}.{0}' \nGO\n\n" +
                                                  "UPDATE Property \nSET rdf_ID = '{0}.{1}1' \nWHERE rdf_ID = '{0}.{1}' \nGO\n\n" +
                                                  "UPDATE Property \nSET cims_inverseRoleName = '{0}.{1}1' \nWHERE cims_inverseRoleName = '{0}.{1}' \nGO\n\n";

        public static string AssociationString8 = "UPDATE Resources \nSET URI = 'aep:{1}2.{0}' \nWHERE URI = 'aep:{1}.{0}' \nGO\n\n" +
                                                  "UPDATE Resources \nSET URI = 'aep:{0}.{1}2' \nWHERE URI = 'aep:{0}.{1}' \nGO\n\n" +
                                                  "UPDATE Property  \nSET rdf_ID = '{1}2.{0}' \nWHERE rdf_ID = '{1}.{0}' \nGO\n\n" +
                                                  "UPDATE Property  \nSET cims_inverseRoleName = '{1}2.{0}' \nWHERE cims_inverseRoleName = '{1}.{0}' \nGO\n\n" +
                                                  "UPDATE Property \nSET rdf_ID = '{0}.{1}2' \nWHERE rdf_ID = '{0}.{1}' \nGO\n\n" +
                                                  "UPDATE Property \nSET cims_inverseRoleName = '{0}.{1}2' \nWHERE cims_inverseRoleName = '{0}.{1}' \nGO\n\n";

        /* public static string AssociationString8 = "UPDATE Resources \nSET URI = 'aep:{1}.{0}2' \nWHERE URI = 'aep:{1}.{0}' \nGO\n\n" +
                                                   "UPDATE Resources \nSET URI = 'aep:{0}2.{1}' \nWHERE URI = 'aep:{0}.{1}' \nGO\n\n" +
                                                   "UPDATE Property  \nSETcims_inverseRoleName = '{1}.{0}2' \nWHERE cims_inverseRoleName = '{1}.{0}' \nGO\n\n" +
                                                   "UPDATE Property \nSET rdf_ID = '{0}2.{1}' \nWHERE rdf_ID = '{0}.{1}' \nGO\n\n"; */

        public static string EnumerationStringClass1 = "EXEC AddClass '{0}',NULL, 'aep:{1}', 0, 1;\nGO\n\n"; //Enumerations have no parent
        public static string EnumerationStringItem1 = "EXEC AddEnum '{0}','aep:{1}','{2}';\nGO\n\n";

        public static string TapChangerString1 = "EXEC AddAssociation '{0}', 'cim:TapChanger', 'cim:TapChanger.TapChanger','{1}','aep:AEP_TapChanger.TapChanger','AEP_TapChangerID', 1,'M:0..*', 'M:0..1', 'AEP';\nGO\n\n" +
                                                 "UPDATE Resources\n SET URI = 'aep:TapChanger.AEP_TapChanger'   --changed URI\n" +
                                                 "WHERE URI = 'cim:TapChanger.TapChanger'--original URI\nGO\n\n" +
                                                 "UPDATE Resources\n SET URI = 'aep:TapChanger.AEP_TapChangers' --multiplicity URI 's' added\n WHERE URI = 'aep:TapChanger.AEP_TapChanger' --changed URI\nGO\n\n" +
                                                 "UPDATE Property\n SET rdf_ID = 'TapChanger.AEP_TapChanger' --modified URI\n WHERE rdf_ID = 'TapChanger.TapChanger'--original URI\nGO\n\n" +
                                                 "UPDATE Property\n SET cims_inverseRoleName = 'TapChanger.AEP_TapChangers'--, --multiplicity URI 's' added\n" +
                                                 "WHERE rdf_ID = 'AEP_TapChanger.TapChanger'--changed URI\nGO\n\n";

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

        public static string addProp2 = "EXEC AddProperty '{{{0}}}', 'aep:AEP_{1}', 'aep:AEP_{2}', '{3}', '{4}', NULL, 'M:0..1', 'AEP';\nGO\n";

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
            aepAttributeFlag = false;

            if (attribute.Contains("AEP_") && !attribute.Contains("GenerateM2M"))
            {
                attribute = attribute.Substring(4);
                aepAttributeFlag = true;
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

            //Check to see if we need to create a class table
            if (classNameChange)
            { // Depends on whether table is in cim namespace or pti namespace
                if (!(inheritsFrom.Equals("") || inheritsFrom.Contains("M:")) && !attribute.Contains(".")) //This ignores the multiplicity entries? Check this with walk-through!!!!*****
                                                                                                           //if (!(inheritsFrom.Equals("")) && !attribute.Contains(".")) - original code
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

            //Hardcode for tapchanger.tapchange, for now.
            if (attribute.Contains("TapChanger.TapChanger"))
            {
                g = Guid.NewGuid();
                g1 = Guid.NewGuid();
                //Feed in the canned sql
                SQLOF.WriteLine("\n-- Start of hardcoded TapChanger.TapChanger association\n\n");
                SQLOF.WriteLine(TapChangerString1, g.ToString(), g1.ToString());
                SQLOF.WriteLine("\n-- End of hardcoded TapChanger.TapChanger association\n\n");
                return;
            }

            //Check to see if we want to create a manyTOmany mapping table
            if (attribute.Contains("GenerateM2M"))
            {
                target2MapTable = attribute.Split(':').Last().Trim();
                col1MapName = inheritsFrom.Split(':').First().TrimStart('|'); //the leading '|' is in case we ever want to stuff something in front of the column names
                col2MapName = inheritsFrom.Split(':').Last().Trim();
                g = Guid.NewGuid();
                createTableName = tClassName + target2MapTable + "Map"; //{2}
                SQLOF.WriteLine(createTable3, createTableName, tClassName, g.ToString());
                g = Guid.NewGuid();
                g1 = Guid.NewGuid();
                SQLOF.WriteLine("\n-- Start of a ManyToMany class and associations\n\n");
                // SQLOF.WriteLine(AssociationString6, g.ToString(), g1.ToString(), createTableName, tClassName, col1MapName, "M:0..*", "M:0..*"); //Always manyTOmany
                SQLOF.WriteLine(RAString60, g.ToString(), g1.ToString(), createTableName, col1MapName, tClassName);
                // SQLOF.WriteLine(AssociationString7, createTableName, tClassName); //Clean up the Resource and Property tables - No longer needed?
                g = Guid.NewGuid();
                g1 = Guid.NewGuid();
                SQLOF.WriteLine(RAString61, g.ToString(), g1.ToString(), createTableName, col2MapName, tClassName); //, col2MapName, "M:0..*", "M:0..*"); //Always manyTOmany
                //  SQLOF.WriteLine(AssociationString8, createTableName, tClassName); //Clean up the Resource and Property tables - No longer needed?
                SQLOF.WriteLine("-- End of a ManyToMany class and associations\n\n\n");
                return;
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

            if (dataType.Equals("association")) //This may need to be reviewed and tested to see when assocstr-2 would be used
            {
                g = Guid.NewGuid();
                g1 = Guid.NewGuid();
                targetClass = inheritsFrom.Split('&').First().Trim();
                multiplicity = inheritsFrom.Split('&').Last().Trim();
                multiplicity1 = multiplicity.Split('-').First();
                multiplicity2 = multiplicity.Split('-').Last();
                /*   if (inheritsFrom.Equals("") || inheritsFrom.Contains("M:"))  //This may need tweaking for when to use */
                if (targetClass.Equals("") && RetainedClassName.Contains("AEP_"))  //This may need tweaking for when to use
                                                                                   //From now on it will never be empty as every association must have a multiplicity entry and possibly a target class
                                                                                   //This could test for a blank and throw an error indicating it ran into a wrongly entered row....
                                                                                   //Change the format to &M:0..1-M:0..* or similar and split on the & to separate multiplicity from any target class entered
                {//Do the default parent class

                    SQLOF.WriteLine(AssociationString1, g.ToString(), g1.ToString(), RetainedClassName, subattribute, multiplicity1, multiplicity2);
                    return;
                }
                else if (targetClass.Equals(""))
                {//Do the parent class explicitly specified
                 //    SQLOF.WriteLine(AssociationString2, g.ToString(), RetainedClassName, subattribute, inheritsFrom);
                 /* SQLOF.WriteLine(AssociationString2, RetainedClassName, subattribute, inheritsFrom); */
                    SQLOF.WriteLine(AssociationString2, g.ToString(), g1.ToString(), RetainedClassName, subattribute, multiplicity1, multiplicity2);
                    return;
                }
                else
                {// Right now just pointing to a CIM class name as the target
                    SQLOF.WriteLine("\n-- Start of an association with a class name that is different!!!\n");

                    SQLOF.WriteLine(AssociationString3, g.ToString(), g1.ToString(), targetClass, RetainedClassName, subattribute, multiplicity1, multiplicity2);
                    if (!multiplicity1.Contains("*"))
                    {
                        SQLOF.WriteLine(AssociationString4, g.ToString(), g1.ToString(), targetClass, RetainedClassName, subattribute, multiplicity1, multiplicity2);
                    }
                    else
                    { //Add 's' to end for '*' multiplicity
                        SQLOF.WriteLine(AssociationString5, g.ToString(), g1.ToString(), targetClass, RetainedClassName, subattribute, multiplicity1, multiplicity2);
                    }

                    SQLOF.WriteLine("\n-- End of an association with a class name that is different!!!\n\n");
                    return;
                }
            }

            /*
             * Here is where to put the code for newEnum.
             * You're interested in the dtatype = newEnum, the attribute and the inheritsFrom strings.
             * Then get out.
             * */

            if (dataType.Equals("newEnum"))
            {
                //Create the enumeration class
                g = Guid.NewGuid();
                SQLOF.WriteLine(EnumerationStringClass1, g.ToString(), attribute);

                //Create the actual enumerations
                String[] items = inheritsFrom.TrimStart('(').TrimEnd(')').Split(',');

                foreach (string item in items)
                {
                    g = Guid.NewGuid();
                    SQLOF.WriteLine(EnumerationStringItem1, g.ToString(), attribute, item);
                }

                return;
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
