//////////////////////////////////////////////////////////////////////////////////////////////////////////
// ChildBuilder.cs : Demonstrate implementation of building build request received from mother builder  //
//                   through communication channel created using WCF. Mother builder spawns multiple    //
//                   .net processes of this process [Process Pool concept]                              //
//                   Child Builder service gets open on particular port address and it communicates with//
//                   Mother builder for 2 purpose -                                                     // 
//                   1. Accepts the build request from Mother Builder's Sender Blocking Queue           // 
//                   2. Sends a Ready message to Mother Builder's receiver Blocking Queue when ready to //
//                      accept next build request from Mother Builder                                   //
//                                                                                                      //
// Platform         : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                 //-|_ 
// Language         : C# & .Net Framework                                                               //-|  <----------Requirement 1---------->
// Application      : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17             //
// Author           : Sonal Patil, Syracuse University                                                  //
//                    spatil06@syr.edu (408)-416-6291                                                   //  
// Source           : Dr. Jim Fawcett, EECS, SU                                                         //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
//---------------------------------------------------------------------------------------------------------------------------------------------------------------------//
//Prologues - 1. public ChildBuilder(int port_addr) - constructor accepts port address and Creates communication channel for ChildBuilder at that port addr
//            2. public void wait() - Receiver thread waiting function
//            3. void rcvThreadProc() - receivers' thread that extracts messages from childBuilder's receiver blocking queue
//            4. public Boolean buildFiles(string[] filenames) - builds the '.dll' file for given build request
//            5. void sendfileto_testharness(string[] files) - Sends a communication message to Test Harness with msg.body = test request file name
//            6. void sendBuildLogto_repo(string filename, string file_storage_path) - Sends a communication message to Repository with msg.body = build log file name
//            7. public bool postFile(string fileName, string file_storage_path, string file_transfer_path) - Copies files from one directory to another directory block by block [Push Model]
//            8. public void quitting() - quit message
//---------------------------------------------------------------------------------------------------------------------------------------------------------------------//
/* Required Files:
* ---------------
* BlockingQueue.cs, Comm.cs, IComm.cs, Serializer.cs, XMLParser.cs
*
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017
* ver 1.1 : 5 Dec 2017 ----Final version
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Comm;

namespace ChildBuilder
{
    using XMLParser;
    using Serialization;
    class ChildBuilder
    {
        /*-------------------------------------------------< Start of ChildBuilder >------------------------------------------------------------------*/
        /*--------------------------------------------------< Variable Declaration >------------------------------------------------------------------*/
        public Comm<ChildBuilder> comm { get; set; } = new Comm<ChildBuilder>();                        //--
        public string endPoint { get; }         //varibale delcaration for storing endpoint address     //  | - Comm channel variable declaration
        private Thread rcvThread = null;        //receiver thread                                       //--
        string temp_directory_path = "";
        int port_address;
        int testelement_count = 0;

        /*------------------------< accepts port address and Creates communication channel for ChildBuilder at that port addr >----------------------*/
        public ChildBuilder(int port_addr)
        {
            port_address = port_addr;
            try
            {
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 2\n"); Console.ResetColor();
                Console.WriteLine("Shall include a Message-Passing Communication Service built with WCF");
                int i = port_address - 9000;
                string child_builder = "ChildBuilder" + i.ToString();
                endPoint = Comm<ChildBuilder>.makeEndPoint("http://localhost", port_addr, child_builder);          //creates endpoint for given port address                
                comm.sndr.CreateSendChannel(endPoint);                                              //Created sender channel to send messages atgiven port address
                Console.WriteLine("\nSender Channel Created for Child Builder at port: {0}", endPoint);
                comm.rcvr.CreateRecvChannel(endPoint);                                              //Created sender channel to send messages atgiven port address
                Console.WriteLine("Receiver Channel Created for Child Builder at port: {0}", endPoint);
                rcvThread = comm.rcvr.start(rcvThreadProc);
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 6\n"); Console.ResetColor();
                Console.WriteLine("Pool Processes shall use message-passing communication to access messages from the mother Builder process.\n");
                //creates a temp directory for each build request if it already doesn't exsits
                temp_directory_path = @"../../../ChildBuilder/ChildBuilder_tempDir" + port_addr;
                if (!Directory.Exists(temp_directory_path))
                    Directory.CreateDirectory(temp_directory_path);
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        /*--------------------------------------------< Receiver thread waiting function >------------------------------------------------------------*/
        public void wait()
        {
            rcvThread.Join();
        }

        /*-------------------------< receivers' thread that extracts messages from childBuilder's receiver blocking queue >---------------------------*/
        void rcvThreadProc()
        {
            while (true){
                try{
                    CommMessage msg = comm.rcvr.GetMessage();               //dequeues a comm message from ChildBuilders' blocking queue
                    if (msg.body == null) {    Console.WriteLine("Waiting for Build Request.........."); break;   }
                    postFile(msg.body, @"../../../Repository/Repo_storage/", temp_directory_path);  //copies the build request file at temp directory through Push model
                    Dictionary<string, List<string>> testfiles = new Dictionary<string, List<string>>();
                    int count = 1;
                    testfiles = XMLParser.xmlparser(temp_directory_path + @"/" + msg.body);    //gets buildrequest stored in the temp directory and parses it 
                    testelement_count = testfiles.Count;
                    Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 3 [Part 1]\n"); Console.ResetColor();
                    Console.WriteLine("The Communication Service shall support accessing build requests by Pool Processes from the mother Builder process, receiving build requests, sending and receiving files.\n");
                    Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received from from MotherBuilder at ChildBuilder. Purpose - buildrequest to build");
                    Console.ResetColor(); msg.show();
                    string xmlString = null;
                    if (msg.body != "quit")                            //if the msg is not quit but it's a build request then it prints it's content on the console
                         xmlString = File.ReadAllText(temp_directory_path + @"/" + msg.body);
                    Console.WriteLine(xmlString);
                    foreach (string key in testfiles.Keys)
                    {
                        string[] filenames = testfiles[key].ToArray();
                        for(int i = 0; i < filenames.Length; i++)
                            postFile(filenames[i], @"../../../Repository/Repo_storage/", temp_directory_path);
                        if (buildFiles(filenames, count))                                                       //builds the dll file for given build request
                            Console.WriteLine("Stroing generated dll file at : {0}",temp_directory_path);
                        count++;
                    }                                                
                    if (msg.command == "quit")
                        break;  
                    if(msg.command == "SendFreemsg")
                    {
                        Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received from TestHarness at ChildBuilder. Purpose - relasing child builder from current build request so that it can send ready msg to mother builder to accept next buildrequest");
                        Console.ResetColor();   msg.show();
                        sendReadymsg(true);
                    }                    
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }
        }
        /*-------------------------------------------<Testing completed sending ready msg to mother builder > ------------------------------------------------*/
        void sendReadymsg(bool command)
        {
            try
            {
                if (command)
                {
                    CommMessage msg = new CommMessage(CommMessage.MessageType.BuildRequest);
                    string remoteEndPoint = Comm<ChildBuilder>.makeEndPoint("http://localhost", 8082, "MotherBuilder");
                    msg.to = remoteEndPoint;
                    msg.from = endPoint;
                    msg.command = "ready";
                    msg.body = port_address.ToString();
                    comm.sndr.PostMessage(msg);                                         //sends ready message to mother builder 
                    Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from ChildBuilder to MotherBuilder. Purpose - After completion of testing, on command from testharness sends ready msg to mother builder to accept next buildrequest");
                    Console.ResetColor();
                    msg.show();
                }
            }
            catch(Exception e) { Console.WriteLine(e.Message); }
            
        }

        /*------------------------------------< builds the '.dll' file for given build request >------------------------------------------------------*/
        public Boolean buildFiles(string[] filenames, int count){
            string buildlog = null;             //string that holds build log details
            Process p = new Process();
            string buildlog_name = "BuildLog" + port_address + ".txt";      //buildlog file name for current build request
            TextWriter tsw = new StreamWriter(temp_directory_path + @"/" + buildlog_name, true);    //created buildlog file at current temp directory for current build request 
            try{
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.EnvironmentVariables["PATH"] = "%path%;C:/Windows/Microsoft.NET/Framework64/v4.0.30319";         //setting environment variables
                string buildfilenames = null;
                for (int i = 0; i < filenames.Length; i++){                                                                                              //taking all .cs files that need to build
                    if (String.IsNullOrEmpty(filenames[i]) == false)
                        buildfilenames = buildfilenames + " " + filenames[i];
                }
                p.StartInfo.Arguments = "/Ccsc /target:library" + buildfilenames;                              //giving the command to build .cs files into one .dll file
                p.StartInfo.WorkingDirectory = temp_directory_path;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;  p.Start();                                                //starts the child builder process
                string errors = p.StandardError.ReadToEnd() + " \n";                                            //getting the output of build
                string output = p.StandardOutput.ReadToEnd() + " \n";
                buildlog = errors + output;
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 7\n"); Console.ResetColor();
                Console.WriteLine("Each Pool Process shall attempt to build each library, cited in a retrieved build request, logging warnings and errors.\n");
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            if (p.ExitCode == 0)
            {
                buildlog += "Build Result: Success \n";      Console.WriteLine(buildlog);                                                //printing build result
                tsw.Write(buildlog); tsw.Close();
                string[] files = Directory.GetFiles(temp_directory_path, "*.dll");          //getting file name of generated dll file
                Serializer.start_test_serializer(files, temp_directory_path);            //creating test request for current build request using generated dll file
                string[] xml_files = Directory.GetFiles(temp_directory_path, "testRequest.xml");
                if (count == testelement_count){
                    sendfileto_testharness(xml_files);                                          //sending test request to test harness
                    sendBuildLogto_repo(buildlog_name, temp_directory_path);                    //sending build log file to repository for storage
                    buildlog = null;
                } return true;
            }
            else
            {
                buildlog += "Build Result: Failure [See the error message for further information]";    Console.WriteLine(buildlog);             //printing build result
                tsw.Write(buildlog); tsw.Close();
                if (count == testelement_count) {
                    sendBuildLogto_repo(buildlog_name, temp_directory_path);                    //sending build log file to repository for storage
                    buildlog = null;
                } return false;
            }
        }

        /*-------------------------< Sends a communication message to Test Harness with msg.body = test request file name >---------------------------*/
        void sendfileto_testharness(string[] files)
        {
            try
            {
                CommMessage msg = new CommMessage(CommMessage.MessageType.TestRequest);
                string remoteEndPoint = Comm<ChildBuilder>.makeEndPoint("http://localhost", 8084, "TestHarness");
                msg.to = remoteEndPoint;
                msg.from = endPoint;
                msg.command = port_address.ToString();
                msg.body = files[0];                                            //sends test request file name generated for current build request to test harness
                comm.sndr.PostMessage(msg);
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 8 [Part 1]\n"); Console.ResetColor();
                Console.WriteLine("If the build succeeds, Child Builder/s shall send a test request and libraries to the Test Harness for execution. \n ");
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from ChildBuilder to TestHarness. Purpose - Sending test request file name to TestHarness");
                Console.ResetColor();
                msg.show();
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            
        }

        /*-------------------------< Sends a communication message to Repository with msg.body = build log file name >------------------------*/
        void sendBuildLogto_repo(string filename, string file_storage_path)
        {
            try
            {
                CommMessage msg = new CommMessage(CommMessage.MessageType.BuildLog);
                string remoteEndPoint = Comm<ChildBuilder>.makeEndPoint("http://localhost", 8081, "Repository");
                msg.to = remoteEndPoint;                        //sends message to Child builder's given particular port address 
                msg.from = endPoint;
                msg.command = "SendBuildLog";
                msg.body = filename + "," + file_storage_path;                  //sends build log file name generated for current build request to test harness
                comm.sndr.PostMessage(msg);
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 8 [Part 2]\n"); Console.ResetColor();
                Console.WriteLine("Child Builder/s shall send the build log to the repository. \n");
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from ChildBuilder to Repository. Purpose - Sending {0} to Repository stored at{1}", filename, file_storage_path);
                Console.ResetColor();
                msg.show();
            }
            catch (Exception ex) { Console.WriteLine(ex); }
                     
        }

        /*--------------------------------------------------------< quit message >-----------------------------------------------------------------*/
        public void quitting()
        {
            try {
                Console.Write("\nPress key to exit: ");
                Console.ReadKey();
                CommMessage qmsg = new CommMessage(CommMessage.MessageType.quit);            //new comm message of type BuildRequest
                qmsg.from = endPoint;
                qmsg.to = endPoint;
                qmsg.command = "quit";
                qmsg.body = "Quit message";                                                                   //sends build request file/s name/s 
                comm.sndr.PostMessage(qmsg);
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from ChildBuilder to ChildBuilder. Purpose - After finishing everything on command, sends quit message");
                Console.ResetColor();
                qmsg.show();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        /*--------------------------------< Copies files from one directory to another directory block by block [Push Model] >-----------------------*/
        public bool postFile(string fileName, string file_storage_path, string file_transfer_path)
        {
            FileStream fs = null;
            string lastError = null;
            long bytesRemaining;

            try
            {
                string fileStored_at = file_storage_path;
                string transfer_path = file_transfer_path;
                long blockSize = 1024;
                string path = Path.Combine(fileStored_at, fileName);                //combines sender/source the file path with the file name
                fs = File.OpenRead(path);
                bytesRemaining = fs.Length;
                comm.rcvr.openFileForWrite(fileName, transfer_path);                               //creates and opens a fileat destination  to write or copy the data from source file.
                while (true)
                {
                    long bytesToRead = Math.Min(blockSize, bytesRemaining);
                    byte[] blk = new byte[bytesToRead];
                    long numBytesRead = fs.Read(blk, 0, (int)bytesToRead);
                    bytesRemaining -= numBytesRead;

                    comm.rcvr.writeFileBlock(blk);                                  //writes the content into destination file from source file
                    if (bytesRemaining <= 0)
                        break;
                }
                comm.rcvr.closeFile();                                              //closes the file after completetion of copying files
                fs.Close();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
            return true;
        }
    }

    class cBuilder
    {
#if (TEST_ChildBuilder)
        static void Main(string[] args)
        {
            try
            {
                int port_addr = Int32.Parse(args[0]);
                Console.Title = "Child Builder "+ port_addr.ToString();
                ChildBuilder cBuilder = new ChildBuilder(port_addr);            //starts child builder at port address given by mother builder
                if (cBuilder.comm.sndr.getReadyblockingQ_size() <= 0)
                {
                    CommMessage msg = new CommMessage(CommMessage.MessageType.BuildRequest);
                    string remoteEndPoint = Comm<ChildBuilder>.makeEndPoint("http://localhost", 8082, "MotherBuilder");
                    msg.to = remoteEndPoint;                        
                    msg.from = cBuilder.endPoint;
                    msg.command = "ready";
                    msg.body = port_addr.ToString();
                    cBuilder.comm.sndr.PostMessage(msg);                //sends ready message to mother builder 
                    cBuilder.comm.sndr.PostReadyMessage(msg);
                    Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from ChildBuilder to MotherBuilder. Purpose - At start after spawning, sends ready msg to mother builder to accept next buildrequest");
                    Console.ResetColor();
                    msg.show();
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
            Console.ReadKey();
        }
#endif
    }

}
