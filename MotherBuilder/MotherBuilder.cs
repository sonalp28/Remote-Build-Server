//////////////////////////////////////////////////////////////////////////////////////////////////////////
// MotherBuilder.cs : Demonstrate creation of multiple .net processes [Process Pool] with communation   //
//                    channel to communicate with Child Builder.                                        //
//                    Mother Builder service gets open on one particular port address & it communicates //
//                    with multiple child builders for 2 purpose -                                      // 
//                    1. Sends the build request to child Builder's Receiver                            // 
//                    2. Accepts a Ready message from child Builder's sender when it's ready to accept  //
//                       next build request from Mother Builder                                         //
//                                                                                                      //
// Platform         : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                 //-|_ 
// Language         : C# & .Net Framework                                                               //-|  <----------Requirement 1---------->
// Application      : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17             //
// Author           : Sonal Patil, Syracuse University                                                  //
//                    spatil06@syr.edu (408)-416-6291                                                   //  
// Source           : Dr. Jim Fawcett, EECS, SU                                                         //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
//--------------------------------------------------------------------------------------------------------------------------------------------------------------------//
//Prologues - 1. public MotherBuilder() - constructor creates the end point using that port address and creates receiver comm channel. Starts the receiver's thread.
//            2. public void wait() - Receiver thread waiting function
//            3. void rcvThreadProc() - receivers' thread that extracts messages from MotherBuilder's receiver blocking queue
//            4. public void sendfilesto_childBuilder(string port_addr) - Sends a communication message to child Builder with msg.body = build request file name
//            5. public bool createProcess(int i, string portaddr) - Spawns multiple process [Process pool concept] & starts therir communication at given port address 
//            6. public void quitting() - quit message
//--------------------------------------------------------------------------------------------------------------------------------------------------------------------//
/* Required Files:
* ---------------
* BlockingQueue.cs, Comm.cs, IComm.cs
*
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Comm;
using System.Threading;

namespace MotherBuilder
{
    public class MotherBuilder
    {
        /*-------------------------------------------------< Start of MotherBuilder >------------------------------------------------------------------*/
        /*--------------------------------------------------< Variable Declaration >-------------------------------------------------------------------*/

        public Comm<MotherBuilder> comm { get; set; } = new Comm<MotherBuilder>();                      //--
        public string endPoint { get; } = Comm<MotherBuilder>.makeEndPoint("http://localhost", 8082, "MotherBuilder");   //  | - Comm channel variable declaration
        private Thread rcvThread = null;                                                                //--

        List<string> build_request_filenames = new List<string>();
        int process_count = 0;

        /*------------------------< constructor Creates communication channel for Mother Builder at port add 8082 >-----------------------------------*/
        public MotherBuilder()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 2\n"); Console.ResetColor();
                Console.WriteLine("Shall include a Message-Passing Communication Service built with WCF");
                comm.sndr.CreateSendChannel(endPoint);                                              //Created sender channel to send messages at port 8082
                Console.WriteLine("\nSender Channel Created for Mother Builder at port: {0}", endPoint);
                comm.rcvr.CreateRecvChannel(endPoint);                                              ////Created receiver channel to send messages at port 8082
                Console.WriteLine("Receiver Channel Created for Mother Builder at port: {0}", endPoint);
                rcvThread = comm.rcvr.start(rcvThreadProc);
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
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
                    CommMessage msg = comm.rcvr.GetMessage();                   //dequeues a comm message from MotherBuilders' blocking queue
                    switch (msg.command)                                        //switch statement that gets operated based on command given in the comm message
                    {
                        case "processCountfromGUI":
                            process_count = Convert.ToInt32(msg.body);          //gets a number for how many child builder processes to spawn
                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received from GUI at MotherBuilder. Purpose - gets count from GUI for number of child builder processes to spawn");
                            Console.ResetColor(); msg.show();
                            Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 5\n"); Console.ResetColor();
                            Console.WriteLine("Shall provide a Process Pool component that creates a specified number of processes on command. \n");
                            for (int i = 0; i < process_count; i++)
                            {
                                if (createProcess(i, Convert.ToString(9000 + i)))   //spawns child builder processes of count given by GUI
                                {
                                    Console.WriteLine("Status - Successful");
                                }
                                else { Console.WriteLine("Status - Failed"); }
                            }
                            break;
                        case "ready":
                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received from ChildBuilder at MotherBuilder. Purpose - gets ready msg from spawned child builder/s so that it can decide where to send the build request");
                            Console.ResetColor(); msg.show();
                            sendfilesto_childBuilder(msg.body);            //send build request file/s to child builder/s only after getting ready message from them
                            break;
                        case "SendBRstoBuilder":
                            string file_names = msg.body;                       //receives build request/s file name/s from Repository [sent on command from GUI]
                            string[] files = file_names.Split(',');
                            build_request_filenames = files.ToList();           //saves those file names into a list
                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received from Repository at MotherBuilder. Purpose - accepts the buildrequest file name/s from repository on which user wants to perform build operation");
                            Console.ResetColor(); msg.show();
                            break;
                        case "quit": break;                                           //if command is to quit then break
                        default: break;
                    }
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }
        }

        /*-------------------------< Sends a communication message to child Builder with msg.body = build request file name >------------------------*/
        public void sendfilesto_childBuilder(string port_addr)
        {
            try
            {
                int addr = Convert.ToInt32(port_addr);
                CommMessage msg = new CommMessage(CommMessage.MessageType.BuildRequest);
                int i = addr - 9000;
                string child_builder = "ChildBuilder" + i.ToString();
                string remoteEndPoint = Comm<MotherBuilder>.makeEndPoint("http://localhost", addr, child_builder);
                msg.to = remoteEndPoint;                                //sends message to Child builder's given particular port address 
                msg.from = endPoint;
                msg.command = "BuildRequest";
                msg.body = build_request_filenames[i].TrimEnd(',');     //sends build request file name to start building that request
                comm.sndr.PostMessage(msg);
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 3 [Part 2]\n"); Console.ResetColor();
                Console.WriteLine("The Communication Service shall support accessing build requests by Pool Processes from the mother Builder process, sending build requests \n");
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from MotherBuilder to ChildBuilder. Purpose - On ready msg, send build request file to available ChildBuilder");
                Console.ResetColor();
                msg.show();
             }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        /*--------------------------------------------------------< quit message >-----------------------------------------------------------------*/
        public void quitting()
        {
            try
            {
                Console.Write("\nPress key to exit: ");
                Console.ReadKey();
                CommMessage msg = new CommMessage(CommMessage.MessageType.quit);            //new comm message of type BuildRequest
                msg.from = endPoint;
                msg.to = endPoint;
                msg.command = "quit";
                msg.body = "Quit message";                                                                   //sends build request file/s name/s 
                comm.sndr.PostMessage(msg);
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from MotherBuilder to MotherBuilder. Purpose - After finishing everything on command sends quit message");
                Console.ResetColor();
                msg.show();
                wait();
                Console.Write("\n\n");
                comm.close();                
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }
        /*---------------< Spawns multiple process[Process pool concept] and starts therir communication channel at given port address >--------------*/
        public bool createProcess(int i, string portaddr)
        {
            Process proc = new Process();
            string fileName = @"..\..\..\ChildBuilder\bin\Debug\ChildBuilder.exe";    //gives the filename using which we'll spawn multiple processes
            string absFileSpec = Path.GetFullPath(fileName);
            Console.WriteLine("Attempting to start {0}", absFileSpec);
            try
            {
                Process.Start(fileName, portaddr);                                             //starts the process with given filename and portaddress
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }
    }

    class MBuilder
    {
#if (TEST_MotherBuilder)
        static void Main(string[] args)
        {
            Console.Title = "Mother Builder";
            MotherBuilder mbuilder = new MotherBuilder();   //starts mother builder 
            
        }
#endif
    }

}
