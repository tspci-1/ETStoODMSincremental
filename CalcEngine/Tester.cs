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
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace CalcEngine
{
#if DEBUG
    public partial class CalcEngine
    {
        public void Test()
        {
            // adjust culture
            var cultureInfo = this.CultureInfo;
            this.CultureInfo = System.Globalization.CultureInfo.InvariantCulture;

            // test internal operators
            Test("0", 0.0);
            Test("+1", 1.0);
            Test("-1", -1.0);
            Test("1+1", 1 + 1.0);
            Test("1*2*3*4*5*6*7*8*9", 1 * 2 * 3 * 4 * 5 * 6 * 7 * 8 * 9.0);
            Test("1/(1+1/(1+1/(1+1/(1+1/(1+1/(1+1/(1+1/(1+1/(1+1/(1+1))))))))))", 1 / (1 + 1 / (1 + 1 / (1 + 1 / (1 + 1 / (1 + 1 / (1 + 1 / (1 + 1 / (1 + 1 / (1 + 1 / (1 + 1.0)))))))))));
            Test("((1+2)*(2+3)/(4+5))^0.123", Math.Pow((1 + 2) * (2 + 3) / (4 + 5.0), 0.123));
            Test("10%", 0.1);
            Test("1e+3", 1000.0);

            // test simple variables
            Variables.Add("one", 1);
            Variables.Add("two", 2);
            Test("one + two", 3);
            Test("(two + two)^2", 16);
            Variables.Clear();

            // test DataContext
            var dc = DataContext;
            var p = Person.CreateTestPerson();
            DataContext = p;
            Test("Name", "Test Person");
            Test("Name.Length * 2", p.Name.Length * 2);
            Test("Children.Count", p.Children.Count);
            Test("Children(2).Name", p.Children[2].Name);
            Test("ChildrenDct(\"Test Child 2\").Name", p.ChildrenDct["Test Child 2"].Name);
            Test("ChildrenDct.Count", p.ChildrenDct.Count);
            DataContext = dc;

            // test functions
            Logical.Test(this);
            MathTrig.Test(this);
            Text.Test(this);
            Statistical.Test(this);

            // restore culture
            this.CultureInfo = cultureInfo;

        }
        public void Test(string expression, object expectedResult)
        {
            try
            {
                var result = Evaluate(expression);
                if (result is double && expectedResult is int)
                {
                    expectedResult = (double)(int)expectedResult;
                }
                if (!object.Equals(result, expectedResult))
                {
                    var msg = string.Format("error: {0} gives {1}, should give {2}", expression, result, expectedResult);
                    Debug.Assert(false, msg);
                }
            }
            catch (Exception x)
            {
                Debug.Assert(false, x.Message);
            }
        }
        public class Person
        {
            public Person()
            {
                Children = new List<Person>();
                ChildrenDct = new Dictionary<string, Person>();
            }
            public string Name { get; set; }
            public bool Male { get; set; }
            public DateTime Birth { get; set; }
            public List<Person> Children { get; private set; }
            public Dictionary<string, Person> ChildrenDct { get; private set; }
            public int Age { get { return DateTime.Today.Year - Birth.Year; } }

            public static Person CreateTestPerson()
            {
                var p = new Person();
                p.Name = "Test Person";
                p.Birth = DateTime.Today.AddYears(-30);
                p.Male = true;
                for (int i = 0; i < 5; i++)
                {
                    var c = new Person();
                    c.Name = "Test Child " + i.ToString();
                    c.Birth = DateTime.Today.AddYears(-i);
                    c.Male = i % 2 == 0;
                    p.Children.Add(c);
                    p.ChildrenDct.Add(c.Name, c);
                }
                return p;
            }
        }
    }
#endif
}
