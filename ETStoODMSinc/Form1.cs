using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ETStoODMSIncremental
{
    public partial class Form1 : Form
    {

        public Boolean IncXmlFile { get; set; }
        public Boolean CsqlFile { get; set; }
        public Boolean ExTextFile { get; set; }
        public Boolean ExExcelFile { get; set; }

        public string ConfigurationFilePath { get; set; }
        public string ETSFIlePath { get; set; }

        public Form1()
        {
            InitializeComponent();
            //Need to initialize these - if initial check box state is changed (which is very unlikely) 
            //these need to be altered to reflect that, or write a chuck of code to always test the 
            //initial state at startup.  This is quick and dirty, but you have been warned.
            IncXmlFile = true;
            CsqlFile = true;
            ExTextFile = true;
            ExExcelFile = true;
            ConfigurationFilePath = "";
            ETSFIlePath = "";
        }

        private void Button1_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select ETS to ODMS Incremental Configuration File",
                Filter = "Excel File (*.xlsx)|*.xlsx",
                Multiselect = false
            };
            OpenFileDialog Configuration_file = openFileDialog;

            if (Configuration_file.ShowDialog() == DialogResult.OK)
                {
                 textBox1.Text = Configuration_file.FileName;
                ConfigurationFilePath = Configuration_file.FileName;
            }
                else
                {
                ETStoODMSIncremental.Utils.WriteTimeToConsoleAndLog("Configuration file selection canceled. Exiting");
                //Console.WriteLine("Configuration file selection canceled. Exiting");
                System.Threading.Thread.Sleep(3000);
                    return;
                }
            ETStoODMSIncremental.Utils.WriteTimeToConsoleAndLog("Selected: " + ConfigurationFilePath);
           // Console.WriteLine("Selected: " + ConfigurationFilePath);
            Configuration_file.Dispose();
        }

        private void Button2_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select ETS export File",
                Filter = "ETS File (*.ets)|*.ets",
                Multiselect = false
            };
            OpenFileDialog ETS_file = openFileDialog;

            if (ETS_file.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = ETS_file.FileName;
                ETSFIlePath = ETS_file.FileName;
            }
            else
            {
                ETStoODMSIncremental.Utils.WriteTimeToConsoleAndLog("ETS file selection canceled. Exiting");
                //Console.WriteLine("ETS file selection canceled. Exiting");
                System.Threading.Thread.Sleep(3000);
                return;
            }
            ETStoODMSIncremental.Utils.WriteTimeToConsoleAndLog("Selected: "+ETSFIlePath);
            //Console.WriteLine("Selected: " + ETSFIlePath);
            ETS_file.Dispose();
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void TextBox3_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {// Incremental XML File
            if (checkBox1.Checked)
            {
                IncXmlFile = true;
            }
            else
            {
                IncXmlFile = false;
            }
            ETStoODMSIncremental.Utils.WriteTimeToConsoleAndLog(checkBox1.Text + " Boolean:" + IncXmlFile);
            //Console.WriteLine(checkBox1.Text + " Boolean:" + IncXmlFile);
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {//Customer SQL File
            if (checkBox2.Checked)
            {
                CsqlFile = true;
            }
            else
            {
                CsqlFile = false;
            }
            ETStoODMSIncremental.Utils.WriteTimeToConsoleAndLog(checkBox2.Text + " Boolean:" + CsqlFile);
            //Console.WriteLine(checkBox2.Text + " Boolean:" + CsqlFile);
        }

        private void CheckBox3_CheckedChanged(object sender, EventArgs e)
        {// Extensions TExt File
            if (checkBox3.Checked)
            {
                ExTextFile = true;
            }
            else
            {
                ExTextFile = false;
            }
            ETStoODMSIncremental.Utils.WriteTimeToConsoleAndLog(checkBox3.Text + " Boolean:" + ExTextFile);
            //Console.WriteLine(checkBox3.Text + " Boolean:" + ExTextFile);
        }

        private void CheckBox4_CheckedChanged(object sender, EventArgs e)
        {//Extensions Workbook File
            if (checkBox4.Checked)
            {
                ExExcelFile = true;
            }
            else
            {
                ExExcelFile = false;
            }
            ETStoODMSIncremental.Utils.WriteTimeToConsoleAndLog(checkBox4.Text + " Boolean:" + ExExcelFile);
            //Console.WriteLine(checkBox4.Text + " Boolean:" + ExExcelFile);
        }

        private void Button3_Click_1(object sender, EventArgs e)
        {
            if (!IncXmlFile && !CsqlFile && !ExTextFile && !ExExcelFile)
            {
                MessageBox.Show("You must select at least one output file.");
                return;
            }

            if (ConfigurationFilePath.Equals(""))
            {
                MessageBox.Show("You must select a configuration input file.");
                return;
            }

            if  (ETSFIlePath.Equals(""))
            {
                MessageBox.Show("You must select an Alstom ETS input file.");
                return;
            }
           
            Console.WriteLine("Hit the Begin Processing Button");
            Form1.ActiveForm.Dispose();
        }
    }
}
