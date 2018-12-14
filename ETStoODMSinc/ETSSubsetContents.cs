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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ETStoODMSIncremental
{
	class ETSSubsetContents
	{
		string className;
		string subsetFileName;

		//         RDFID           attrbiute  value
		Dictionary<string, Dictionary<string, string>> contents;

		public Dictionary<string, Dictionary<string, string>> Contents
		{
			get
			{
				return contents;
			}
		}

		public ETSSubsetContents(string className, string subsetFileName)
		{
			this.className = className;
			this.subsetFileName = subsetFileName;

			//                        RDFID          attribtue   value
			contents = new Dictionary<string, Dictionary<string, string>>();
		}

		public void StoreContents(Dictionary<string, bool> attributes, XNamespace etsv1, XNamespace rdf)
		{
			Dictionary<string, bool> localCopy = new Dictionary<string, bool>(attributes);
			List<string> rdfAttributes = new List<string>();

			string emptyString = string.Empty;

			XElement xElement = null;

			try
			{
				xElement = XElement.Load(subsetFileName);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message.ToString());
			}

			foreach (KeyValuePair<string, bool> pair in localCopy)
			{
				if (pair.Value)
				{
					rdfAttributes.Add(pair.Key);
				}
			}

			// Do associations first 
			while (rdfAttributes.Count > 0)
			{
				string[] attribList = new string[4];

				int countEm = 0;

				if (rdfAttributes.Count > 3)
				{
					foreach (string attrib in rdfAttributes)
					{
						attribList[countEm++] = attrib;

						if (countEm == 4)
						{
							var fourAssociations =
								from theFour in xElement.Descendants(etsv1 + className)
								let rdfid = theFour.Attribute(rdf + "ID").Value.ToString()
								let r1 = theFour.Element(etsv1 + attribList[0]) == null
										 ? emptyString : theFour.Element(etsv1 + attribList[0]).Attribute(rdf + "resource").Value.ToString()
								let r2 = theFour.Element(etsv1 + attribList[1]) == null
										 ? emptyString : theFour.Element(etsv1 + attribList[1]).Attribute(rdf + "resource").Value.ToString()
								let r3 = theFour.Element(etsv1 + attribList[2]) == null
										 ? emptyString : theFour.Element(etsv1 + attribList[2]).Attribute(rdf + "resource").Value.ToString()
								let r4 = theFour.Element(etsv1 + attribList[3]) == null
										 ? emptyString : theFour.Element(etsv1 + attribList[3]).Attribute(rdf + "resource").Value.ToString()
								select new
								{
									rdfid = rdfid,
									r1 = r1,
									r2 = r2,
									r3 = r3,
									r4 = r4
								};

							foreach (var one in fourAssociations)
							{
								if (!contents.ContainsKey(one.rdfid))
								{
									contents.Add(one.rdfid, new Dictionary<string, string>());
								}

								contents[one.rdfid].Add(attribList[0], one.r1);
								contents[one.rdfid].Add(attribList[1], one.r2);
								contents[one.rdfid].Add(attribList[2], one.r3);
								contents[one.rdfid].Add(attribList[3], one.r4);
							}

							break;
						}
					}
				}
				else if (rdfAttributes.Count > 2)
				{
					foreach (string attrib in rdfAttributes)
					{
						attribList[countEm++] = attrib;

						if (countEm == 3)
						{
							var threeAssociations =
								from theThree in xElement.Descendants(etsv1 + className)
								let rdfid = theThree.Attribute(rdf + "ID").Value.ToString()
								let r1 = theThree.Element(etsv1 + attribList[0]) == null
										 ? emptyString : theThree.Element(etsv1 + attribList[0]).Attribute(rdf + "resource").Value.ToString()
								let r2 = theThree.Element(etsv1 + attribList[1]) == null
										 ? emptyString : theThree.Element(etsv1 + attribList[1]).Attribute(rdf + "resource").Value.ToString()
								let r3 = theThree.Element(etsv1 + attribList[2]) == null
										 ? emptyString : theThree.Element(etsv1 + attribList[2]).Attribute(rdf + "resource").Value.ToString()
								select new
								{
									rdfid = rdfid,
									r1 = r1,
									r2 = r2,
									r3 = r3
								};

							foreach (var one in threeAssociations)
							{
								if (!contents.ContainsKey(one.rdfid))
								{
									contents.Add(one.rdfid, new Dictionary<string, string>());
								}

								contents[one.rdfid].Add(attribList[0], one.r1);
								contents[one.rdfid].Add(attribList[1], one.r2);
								contents[one.rdfid].Add(attribList[2], one.r3);
							}

							break;
						}
					}
				}
				else if (rdfAttributes.Count > 1)
				{
					foreach (string attrib in rdfAttributes)
					{
						attribList[countEm++] = attrib;

						if (countEm == 2)
						{
							var twoAssociations =
								from theTwo in xElement.Descendants(etsv1 + className)
								let rdfid = theTwo.Attribute(rdf + "ID").Value.ToString()
								let r1 = theTwo.Element(etsv1 + attribList[0]) == null
										 ? emptyString : theTwo.Element(etsv1 + attribList[0]).Attribute(rdf + "resource").Value.ToString()
								let r2 = theTwo.Element(etsv1 + attribList[1]) == null
										 ? emptyString : theTwo.Element(etsv1 + attribList[1]).Attribute(rdf + "resource").Value.ToString()
								select new
								{
									rdfid = rdfid,
									r1 = r1,
									r2 = r2
								};

							foreach (var one in twoAssociations)
							{
								if (!contents.ContainsKey(one.rdfid))
								{
									contents.Add(one.rdfid, new Dictionary<string, string>());
								}

								contents[one.rdfid].Add(attribList[0], one.r1);
								contents[one.rdfid].Add(attribList[1], one.r2);
							}

							break;
						}
					}
				}
				else if (rdfAttributes.Count > 0)
				{
					foreach (string attrib in rdfAttributes)
					{
						attribList[countEm++] = attrib;

						if (countEm == 1)
						{
							var oneAssociation =
								from theOne in xElement.Descendants(etsv1 + className)
								let rdfid = theOne.Attribute(rdf + "ID").Value.ToString()
								let r1 = theOne.Element(etsv1 + attribList[0]) == null
										 ? emptyString : theOne.Element(etsv1 + attribList[0]).Attribute(rdf + "resource").Value.ToString()
								select new
								{
									rdfid = rdfid,
									r1 = r1
								};

							foreach (var one in oneAssociation)
							{
								if (!contents.ContainsKey(one.rdfid))
								{
									contents.Add(one.rdfid, new Dictionary<string, string>());
								}

								contents[one.rdfid].Add(attribList[0], one.r1);

							}

							break;
						}
					}
				}

				for (int n = 0; n < countEm; n++)
				{
					rdfAttributes.Remove(attribList[n]);
					localCopy.Remove(attribList[n]);
				}

			}

			// now all the attribute values
			while (localCopy.Count > 0)
			{
				string[] attribList = new string[4];

				int countEm = 0;

				if (localCopy.Count > 3)
				{
					foreach (string attrib in localCopy.Keys)
					{
						attribList[countEm++] = attrib;

						if (countEm == 4)
						{
							var fourAttrbutes =
								from theFour in xElement.Descendants(etsv1 + className)
								let rdfid = theFour.Attribute(rdf + "ID").Value.ToString()
								let a1 = theFour.Element(etsv1 + attribList[0]) == null
										 ? emptyString : theFour.Element(etsv1 + attribList[0]).Value.ToString()
								let a2 = theFour.Element(etsv1 + attribList[1]) == null
										 ? emptyString : theFour.Element(etsv1 + attribList[1]).Value.ToString()
								let a3 = theFour.Element(etsv1 + attribList[2]) == null
										 ? emptyString : theFour.Element(etsv1 + attribList[2]).Value.ToString()
								let a4 = theFour.Element(etsv1 + attribList[3]) == null
										 ? emptyString : theFour.Element(etsv1 + attribList[3]).Value.ToString()
								select new
								{
									rdfid = rdfid,
									a1 = a1,
									a2 = a2,
									a3 = a3,
									a4 = a4
								};

							foreach (var one in fourAttrbutes)
							{
								if (!contents.ContainsKey(one.rdfid))
								{
									contents.Add(one.rdfid, new Dictionary<string, string>());
								}

								contents[one.rdfid].Add(attribList[0], one.a1);
								contents[one.rdfid].Add(attribList[1], one.a2);
								contents[one.rdfid].Add(attribList[2], one.a3);
								contents[one.rdfid].Add(attribList[3], one.a4);
							}

							break;
						}
					}
				}
				else if (localCopy.Count > 2)
				{
					foreach (string attrib in localCopy.Keys)
					{
						attribList[countEm++] = attrib;

						if (countEm == 3)
						{
							var threeAttributes =
								from theThree in xElement.Descendants(etsv1 + className)
								let rdfid = theThree.Attribute(rdf + "ID").Value.ToString()
								let a1 = theThree.Element(etsv1 + attribList[0]) == null
										 ? emptyString : theThree.Element(etsv1 + attribList[0]).Value.ToString()
								let a2 = theThree.Element(etsv1 + attribList[1]) == null
										 ? emptyString : theThree.Element(etsv1 + attribList[1]).Value.ToString()
								let a3 = theThree.Element(etsv1 + attribList[2]) == null
										 ? emptyString : theThree.Element(etsv1 + attribList[2]).Value.ToString()
								select new
								{
									rdfid = rdfid,
									a1 = a1,
									a2 = a2,
									a3 = a3
								};

							foreach (var one in threeAttributes)
							{
								if (!contents.ContainsKey(one.rdfid))
								{
									contents.Add(one.rdfid, new Dictionary<string, string>());
								}

								contents[one.rdfid].Add(attribList[0], one.a1);
								contents[one.rdfid].Add(attribList[1], one.a2);
								contents[one.rdfid].Add(attribList[2], one.a3);
							}

							break;
						}
					}
				}
				else if (localCopy.Count > 1)
				{
					foreach (string attrib in localCopy.Keys)
					{
						attribList[countEm++] = attrib;

						if (countEm == 2)
						{
							var twoAttributes =
								from theTwo in xElement.Descendants(etsv1 + className)
								let rdfid = theTwo.Attribute(rdf + "ID").Value.ToString()
								let a1 = theTwo.Element(etsv1 + attribList[0]) == null
										 ? emptyString : theTwo.Element(etsv1 + attribList[0]).Value.ToString()
								let a2 = theTwo.Element(etsv1 + attribList[1]) == null
										 ? emptyString : theTwo.Element(etsv1 + attribList[1]).Value.ToString()
								select new
								{
									rdfid = rdfid,
									a1 = a1,
									a2 = a2
								};

							foreach (var one in twoAttributes)
							{
								if (!contents.ContainsKey(one.rdfid))
								{
									contents.Add(one.rdfid, new Dictionary<string, string>());
								}

								contents[one.rdfid].Add(attribList[0], one.a1);
								contents[one.rdfid].Add(attribList[1], one.a2);
							}

							break;
						}
					}
				}
				else if (localCopy.Count > 0)
				{
					foreach (string attrib in localCopy.Keys)
					{
						attribList[countEm++] = attrib;

						if (countEm == 1)
						{
							var oneAttribute =
								from theOne in xElement.Descendants(etsv1 + className)
								let rdfid = theOne.Attribute(rdf + "ID").Value.ToString()
								let a1 = theOne.Element(etsv1 + attribList[0]) == null
										 ? emptyString : theOne.Element(etsv1 + attribList[0]).Value.ToString()
								select new
								{
									rdfid = rdfid,
									a1 = a1
								};

							foreach (var one in oneAttribute)
							{
								if (!contents.ContainsKey(one.rdfid))
								{
									contents.Add(one.rdfid, new Dictionary<string, string>());
								}

								contents[one.rdfid].Add(attribList[0], one.a1);
							}

							break;
						}
					}
				}

				for (int n = 0; n < countEm; n++)
				{
					localCopy.Remove(attribList[n]);
				}
			}

		}
	}
}
