/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Serialization.cs : Demonstarte serializing[building] xml file                                                   //
//                    Creates 2 xml files -                                                                        //
//                    1. accepts names of .cs files and creates build request xml file [called from GUI]           // 
//                    2. accepts names of .dll files and creates test request xml file [called by child builder/s] //
//                                                                                                                 //
// Platform          : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                           //-|_ 
// Language          : C# & .Net Framework                                                                         //-|  <----------Requirement 1---------->
// Application       : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17                       //
// Author            : Sonal Patil, Syracuse University                                                            //
//                     spatil06@syr.edu (408)-416-6291                                                             //  
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//-------------------------------------------------------------------------------------------------------------------------------------------------------------//
//Prologues - 1. void start_serializer(Dictionary<string, List<string>> testelement_dict, int count) - generates build request xml file using selected .cs file names in GUI's listbox#2
//            2. void start_test_serializer(string[] files,string path) - searches for dll files at given path & stores them into a list, passes it to Serialize function
//            3. void Serialize(List<testRequest> list,string storage_path) - generates test request xml file using given .dll file nanmes & store it at in the temporary directory                   //
//-------------------------------------------------------------------------------------------------------------------------------------------------------------//
/* Required Files:
* ---------------
* -
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017
*/
using System.Collections.Generic;
using System.Xml;
using System;
using System.IO;
using System.Xml.Serialization;

namespace Serialization
{
    public class Serializer
    {
        /*-------------------------------------------------< Start of Serialization >----------------------------------------------------------------------*/
        /*---------------------This function gets the .cs files selected in the ListBox 2 of GUI and Creates build request xml file------------------------*/
        //static int fCount = 1;
        public static string start_serializer(Dictionary<string, List<string>> testelement_dict, int count)
        {
            string filename = "";
            int fCount = count + 1;
            try
            {
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
                {
                    Indent = true,
                    IndentChars = "\t",
                    NewLineOnAttributes = true
                };
                string path = @"../../../Repository/Repo_storage/";                      //storagepath for build request..path where buildRequests will get saved
                filename = "buildRequest" + fCount + ".xml";                             //giving the filename for the build Request
                using (XmlWriter xmlWriter = XmlWriter.Create(path + filename, xmlWriterSettings))
                {
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteStartElement("buildRequest");
                    foreach (string key in testelement_dict.Keys)
                    {
                        xmlWriter.WriteStartElement("test");
                        xmlWriter.WriteAttributeString("id", key);
                        string[] filenames = testelement_dict[key].ToArray();
                        for (int i = 1; i <= filenames.Length; i++)
                        {
                            xmlWriter.WriteElementString("test"+i+"file", filenames[i-1]);
                        }
                        xmlWriter.WriteEndElement();                        
                    }
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                    xmlWriter.Close();
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            return filename;                                                    //returning the filename of the buildrequest to GUI's ListBox3 to show
        }


        /*---------------------This function search for the dll files at given path and stores them into a list, pass it to Serialize function-------------*/
        public static void start_test_serializer(string[] files,string path)
        {
            try
            {
                List<testRequest> testList = new List<testRequest>();
                for (int i = 0; i < files.Length; i++)
                {
                    testRequest test_i = new testRequest();
                    test_i.tl = Path.GetFileName(files[i]);
                    testList.Add(test_i);
                }
                Serialize(testList,path);
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        /*---------This function is used to create test request xml file and store it at the location know by test harness i.e. temporary directory--------*/
        static public void Serialize(List<testRequest> list,string storage_path)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<testRequest>));
                using (TextWriter writer = new StreamWriter(storage_path + @"/testRequest.xml"))
                {
                    serializer.Serialize(writer, list);
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }

        }
        public class testRequest
        {
            [XmlElement("testLibrary")]
            public string tl;
        }

        class serializer
        {
#if (serialize)
        public static void Main(string[] args)
        {
             Console.WriteLine("-------------------------------------------Serialization started to generate xml file---------------------------------------");
        }
#endif
        }

    }
    /*-------------------------------------------------< End of Serialization >----------------------------------------------------------------------*/
}
