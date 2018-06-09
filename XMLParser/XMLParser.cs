/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// XMLParser.cs   : Demonstarte deserialization/parsing xml file & giving data/content stored in between xml tags  //
//                  Parses 2 xml files -                                                                           //
//                  1. accepts build request xml file & parses it [called from Chils Builder/s]                    // 
//                  2. accepts test request xml file & parses it [called by test harness]                          //
//                                                                                                                 //
// Platform       : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                              //-|_ 
// Language       : C# & .Net Framework                                                                            //-|  <----------Requirement 1---------->
// Application    : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17                          //
// Author         : Sonal Patil, Syracuse University                                                               //
//                  spatil06@syr.edu (408)-416-6291                                                                //  
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//----------------------------------------------------------------------------------------------------------------------------------------------//
//Prologues - 1. public static Dictionary<string, List<string>> xmlparser(string path) - parses buildrequest.xml & returns data for each node   //
//            2. public static List<string> xmlparser_testrequest(string path) - parses test request & returns data for each node in XML file   //
//----------------------------------------------------------------------------------------------------------------------------------------------//
/* Required Files:
* ---------------
* -
*
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017
* ver 1.1 : 5 Dec 2017 ----Final version
*/

using System;
using System.Collections.Generic;
using System.Xml;

namespace XMLParser
{
    public class XMLParser
    {
        /*-------------------------------------------------< Start of XMLParser >----------------------------------------------------------------------*/
        /*--------This xml parser function gets called by child builder/s to parse build request. it returns data for each node in the XML file--------*/
        public static Dictionary<string, List<string>> xmlparser(string path)
        {
            XmlDocument doc = new XmlDocument();
            Dictionary<string, List<string>> tests = new Dictionary<string, List<string>>();

            try
            {
                doc.Load(path);
                XmlNodeList nodes = doc.DocumentElement.SelectNodes("//buildRequest/test");
                foreach (XmlNode node in nodes)
                {
                    String test_id = node.Attributes["id"].Value;
                    String key = test_id;
                    List<string> filenames = new List<string>();
                    for(int i = 1;i <= node.ChildNodes.Count; i++)
                    {
                        filenames.Add(node.SelectSingleNode("test"+i+"file").InnerText);
                    }
                    tests.Add(key, filenames);
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            return tests;

        }

        /*--------This xml parser function gets called by test harness to parse test request. it returns data for each node in the XML file------------*/
        public static List<string> xmlparser_testrequest(string path)
        {
            XmlDocument doc = new XmlDocument();
            List<string> names = new List<string>();
            try
            {
                doc.Load(path);                                                                 //loads the xml file provided at the given path 
                XmlNodeList nodes = doc.DocumentElement.SelectNodes("//ArrayOfTestRequest/testRequest");
                foreach (XmlNode node in nodes)
                {
                    String testLibrary = node.SelectSingleNode("testLibrary").InnerText;        //gets the file names stored inside the tags of testLibrary
                    names.Add(testLibrary);
                }

            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            return names;                                                                       //returns the list of .dll file names to the test harness
        }

        class xml
        {
#if (xmlParser)
            static void Main(string[] args)
            {
                Console.WriteLine("-------------------------------------------Deserialization started to parse xml file---------------------------------------");
            }
#endif
        }
    }
    /*-------------------------------------------------< End of XMLParser >----------------------------------------------------------------------*/
}
