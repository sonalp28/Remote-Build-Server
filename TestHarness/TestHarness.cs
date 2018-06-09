//////////////////////////////////////////////////////////////////////////////////////////////////////////
// TestHarness.cs : Demonstarte test harness that accepts test request sent from child builder through  //
//                  WCF and uses XML parser to parse test request to get dll files. It then loads the   //
//                  dll files and search for the ITest interface. Once it founds it, the run process on //
//                  test starts. After successful simulation of run process, test harness will give the // 
//                  output of the test as 'Passed' or 'Failed' and send the testlog.txt file containing // 
//                  test results to the Repository using WCF to store so that user can access them.     //
//                                                                                                      //
// Platform       : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                   //-|_ 
// Language       : C# & .Net Framework                                                                 //-|  <----------Requirement 1---------->
// Application    : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17               //
// Author         : Sonal Patil, Syracuse University                                                    //
//                  spatil06@syr.edu (408)-416-6291                                                     //  
// Source         : Dr. Jim Fawcett, EECS, SU                                                           //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
//--------------------------------------------------------------------------------------------------------------------------------------------------------------------//
//Prologues - 1. public TestHarness() - constructor Creates communication channel for Test Harness at port add 8084
//            2. public void wait() - Receiver thread waiting function
//            3. void rcvThreadProc() - receivers' thread that extracts messages from testharness's receiver blocking queue 
//            4. static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args) - It is event handler for binding errors when loading libraries
//            5. string loadAndExerciseTesters() - loads assemblies from testersLocation and run their tests                                              
//            6. bool runSimulatedTest(Type t, Assembly asm) - run tester t from assembly asm
//            7. void sendTestLogto_Repo(string filename, string file_storage_path) - Sends a communication message to Repository with msg.body = test log file name
//            8. string GuessTestersParentDir() - extracts name of current directory without its parents if path provided to test harness doesn't exsist  
//            9. public void quitting() - quit message
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------//
/* Required Files:
* ---------------
* BlockingQueue.cs, Comm.cs, IComm.cs, XMLParser.cs
*
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017
* ver 1.1 : 5 Dec 2017 ----Final version
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Comm;

namespace TestHarness
{
    using XMLParser;
    public class TestHarness
    {
        /*-------------------------------------------------------------< Variables declaration >--------------------------------------------------------*/
        static string testlog = null;
        static string testersLocation { get; set; } = ".";
        static List<string> libraries { get; set; } = new List<string>();

        public Comm<TestHarness> comm { get; set; } = new Comm<TestHarness>();                          //--
        public string endPoint { get; } = Comm<TestHarness>.makeEndPoint("http://localhost", 8084, "TestHarness");     //  | - Comm channel variable declaration
        private Thread rcvThread = null;                                                                //--

        static string pathforTR;
        string test_result;
        int port_addr;
        TextWriter tsw;

        /*---------------------------< constructor Creates communication channel for Test Harness at port add 8084 >-----------------------------------*/
        public TestHarness()
        {
            Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 2\n"); Console.ResetColor();
            Console.WriteLine("Shall include a Message-Passing Communication Service built with WCF");
            comm.sndr.CreateSendChannel(endPoint);                                                      //Created sender channel to send messages at port 8084
            Console.WriteLine("\nSender Channel Created for Mother Builder at port: {0}", endPoint);
            comm.rcvr.CreateRecvChannel(endPoint);                                                      //Created receiver channel to send messages at port 8084
            Console.WriteLine("Receiver Channel Created for Mother Builder at port: {0}", endPoint);
            rcvThread = comm.rcvr.start(rcvThreadProc);
        }
        /*--------------------------------------------< Receiver thread waiting function >------------------------------------------------------------*/
        public void wait()
        {
            rcvThread.Join();
        }

        /*-------------------------< receivers' thread that extracts messages from MotherBuilder's receiver blocking queue >--------------------------*/
        void rcvThreadProc()
        {
            while (true)
            {
                try
                {
                    CommMessage msg = comm.rcvr.GetMessage();               //dequeues a comm message from Test Harness' blocking queue
                    Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received from from ChildBuilder at TestHarness. Purpose - TestRequest containing dll files for testing");
                    Console.ResetColor(); msg.show();
                    port_addr = Convert.ToInt32(msg.command);
                    string pathTR = @"../../../ChildBuilder/ChildBuilder_tempDir" + port_addr;
                    pathforTR = pathTR;
                    libraries = XMLParser.xmlparser_testrequest(msg.body);  //sends the full path of test request storage to xml parser and parses the test request
                    if (pathTR.Length > 0)
                        TestHarness.testersLocation = pathTR;
                    else
                        TestHarness.testersLocation = GuessTestersParentDir() + "/Testers";
                    TestHarness.testersLocation = Path.GetFullPath(TestHarness.testersLocation);
                    Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 9 [Part 1]\n"); Console.ResetColor();
                    Console.WriteLine("The Test Harness shall attempt to load each test library it receives and execute it");
                    testlog += "\n  Loading Test Libraries from:\n " + TestHarness.testersLocation;
                    string result = loadAndExerciseTesters();                   //calls the function to start loading dll file and gets the result of testing
                    sendTestLogto_Repo("TestLog" + port_addr + ".txt", pathforTR, test_result);
                    testlog += result;
                    testlog = null;
                    if (msg.command == "quit")
                        break;
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }
        }

        /*-----------------------------< This function is an event handler for binding errors when loading libraries >---------------------------------------*/
        // This occurs when a loaded library has dependent libraries that are not located in the directory where the Executable is running.
        static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = testersLocation;
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;            
        }

        /*-----------------------------------< load assemblies from testersLocation and run their tests >---------------------------------------------------*/
        string loadAndExerciseTesters()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromComponentLibFolder);
            string testlog_filename = "TestLog" + port_addr + ".txt";
            tsw = new StreamWriter(pathforTR + @"/" + testlog_filename, true);
            try
            {
                /*----------------------------------------< load each dll found in test request >-------------------------------------------------*/
                foreach (string lib in libraries)
                {
                    string file = testersLocation + @"\" + lib;
                    Assembly asm = Assembly.Load(File.ReadAllBytes(file));
                    string fileName = Path.GetFileName(file);
                    Console.ForegroundColor = ConsoleColor.White; Console.WriteLine("\n Loaded {0}", fileName);
                    testlog += "\n Loaded \n" + fileName;
                    /*--------------------------------< gets all types and search for ITest Interface to start running test >-------------------- */
                    Type[] types = asm.GetTypes();
                    foreach (Type t in types)
                    {
                        if (t.GetInterface("DllLoaderDemo.ITest", true) != null)
                            if (!runSimulatedTest(t, asm))
                                Console.WriteLine("Test {0} failed to run", t.ToString());
                    }
                }
                tsw.Close();
                testlog = null;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return ex.Message; }
            return "Testing completed";
        }

        /*---------------------------------------------------< run tester t from assembly asm >------------------------------------------------------------*/
        bool runSimulatedTest(Type t, Assembly asm)
        {
            try
            {                
                object obj = asm.CreateInstance(t.ToString());
                // 1. Test announced
                MethodInfo method = t.GetMethod("say");
                if (method != null)
                    method.Invoke(obj, new object[0]);
                //2. Run the test
                bool status = false;
                method = t.GetMethod("test");
                if (method != null)
                    status = (bool)method.Invoke(obj, new object[0]);
                //3. Give back the test result
                Func<bool, string> act = (bool pass) =>
                {
                    if (pass) { test_result = "true";  Console.ForegroundColor = ConsoleColor.Green; return "Passed"; }
                    else { test_result = "false";  Console.ForegroundColor = ConsoleColor.Red; return "Failed"; }
                };
                Console.ResetColor(); Console.WriteLine("\n  Test Result - Test {0}", act(status)); Console.ResetColor();
                testlog += "\n Test Result - Test" + act(status) + "\n";
                tsw.Write(testlog);
                 }
            catch (Exception ex)
            {
                Console.WriteLine("\n Test failed with message \"{0}\"", ex.Message);
                return false;
            }
            return true;
        }

        /*----------------------------------< Sends a communication message to Repository with msg.body = test log file name >--------------------------------*/
        void sendTestLogto_Repo(string filename, string file_storage_path,string testresult)
        {
            try
            {
                CommMessage msg = new CommMessage(CommMessage.MessageType.TestLog);
                string remoteEndPoint = Comm<TestHarness>.makeEndPoint("http://localhost", 8081, "Repository");
                msg.to = remoteEndPoint;
                msg.from = endPoint;
                msg.command = "SendTestLog";
                msg.body = filename + "," + file_storage_path + "," + testresult;                      //sends test log file name with it's storage path
                comm.sndr.PostMessage(msg);
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 9 [Part 2]\n"); Console.ResetColor();
                Console.WriteLine("It shall submit the results of testing to the Repository.\n");
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from TestHarness to Repository. Purpose - Sending {0} to Repository stored at{1}", filename, file_storage_path);
                Console.ResetColor();
                msg.show();
            }
            catch(Exception e) { Console.WriteLine(e.Message); }
            
        }
        /*--------------------------------------------------------< quit message >-----------------------------------------------------------------*/
        public void quitting()
        {
            try {
                Console.Write("\nPress key to exit: ");
                Console.ReadKey();
                CommMessage msg = new CommMessage(CommMessage.MessageType.quit);            //new comm message of type BuildRequest
                msg.from = endPoint;
                msg.to = endPoint;
                msg.command = "quit";
                msg.body = "Quit message";                                                                   //sends build request file/s name/s 
                comm.sndr.PostMessage(msg);
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from TestHarness to TestHarness. Purpose - After finishing everything on command sends quit message");
                Console.ResetColor();
                msg.show();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        /*------< This function extract name of current directory without its parents if the path provided to test harness doesn't exsist or invaild >---------*/
        string GuessTestersParentDir()
        {
            string dir = Directory.GetCurrentDirectory();
            int pos = dir.LastIndexOf(Path.DirectorySeparatorChar);
            string name = dir.Remove(0, pos + 1).ToLower();
            if (name == "debug")
                return "../..";
            else
                return ".";
        }
    }

    class THarness
    {
#if (TEST_testHarness)
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "Test Harness";
                TestHarness loader = new TestHarness();             //starts the test harness
                
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }  
#endif
    }
}
