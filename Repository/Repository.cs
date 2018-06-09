////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Repository.cs       : Demonstarte storage of files required for build server for build process and     //
//                       result files received from child builders and test harness                       //
//                                                                                                        //
// Platform            : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                //-|_ 
// Language            : C# & .Net Framework                                                              //-|  <----------Requirement 1---------->
// Application         : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17            //
// Author              : Sonal Patil, Syracuse University                                                 //
//                       spatil06@syr.edu (408)-416-6291                                                  //  
// Source              : Dr. Jim Fawcett, EECS, SU                                                        //
////////////////////////////////////////////////////////////////////////////////////////////////////////////
//---------------------------------------------------------------------------------------------------------------------------------------------------------------------//
//Prologues - 1. public void establish_channel() - Creates communication channel for Repository at port add 8081
//            2. public void wait() - Receiver thread waiting function
//            3. public static void show_file(bool command) - On button click from GUI, it sends file names of current files present in repository storage directory
//            4. void rcvThreadProc() - receivers' thread that extracts messages from Repository's receiver blocking queue
//            5. public void sendfileToBuilder(string files) - Sends a communication message to Mother Builder with msg.body = name of file build request file/s
//            6. public bool postFile(string fileName,string file_storage_path) - Copies files from one directory to another directory block by block [Push Model]
//            7. public void quitting() - quit message
//---------------------------------------------------------------------------------------------------------------------------------------------------------------------//
/* Required Files:
* ---------------
* Comm.cs, IComm.cs
*
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017
*/
using System;
using Comm;
using System.Threading;
using System.IO;

namespace Repository
{
    public class Repository
    {
        /*--------------------------------------------------< Variable Declaration >------------------------------------------------------------------*/
        public Comm<Repository> comm { get; set; } = new Comm<Repository>();                                                 //--
        public string endPoint { get; set; } = Comm<Repository>.makeEndPoint("http://localhost", 8081, "Repository"); //  | - Comm channel variable declaration
        private Thread rcvThread = null;                                                                                     //--

        /*--------------------------------< Creates communication channel for Repository at port add 8081 >-------------------------------------------*/
        public void establish_channel()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 2\n"); Console.ResetColor();
                Console.WriteLine("Shall include a Message-Passing Communication Service built with WCF");
                comm.sndr.CreateSendChannel(endPoint);                                            //Created sender channel to send messages at port 8081
                Console.WriteLine("\nSender Channel Created for Repository at port: {0}", endPoint);
                comm.rcvr.CreateRecvChannel(endPoint);                                            //Created receiver channel to receive messages at port 8081
                Console.WriteLine("Receiver Channel Created for Repository at port: {0}", endPoint);
                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = ConsoleColor.Gray; Console.Write("\n Requirement 4\n"); Console.ResetColor();
                Console.WriteLine("Shall provide a Repository server that supports client browsing to find files to build, builds an XML build request string and sends that and the cited files to the Build Server. \n");
                rcvThread = comm.rcvr.start(rcvThreadProc);
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }

        /*--------------------------------------------< Receiver thread waiting function >------------------------------------------------------------*/
        public void wait()
        {
            rcvThread.Join();
        }

        /*--------------< Sends a communication message to GUI with msg.body = name of files present in teh Repository storage directory >------------*/
        public void show_file()
        {
            string sendendPoint = Comm<Repository>.makeEndPoint("http://localhost", 8080, "GUI");          //creates senders' endpoint at port addr - 8080
            try
            {
                CommMessage msg = new CommMessage(CommMessage.MessageType.FileList);            //new comm message of type FileTransfer
                msg.from = endPoint;
                msg.to = sendendPoint;
                msg.command = "SendFilestoRepo";
                string path = @"../../../Repository/Repo_storage/";
                string[] files = Directory.GetFiles(path, "*.cs");
                string file_names = "";
                foreach (string file in files)
                {
                    file_names += Path.GetFileName(file) + ",";
                }
                msg.body = file_names;                                                              //sends all file names of extension .cs to GUI as a string
                comm.sndr.PostMessage(msg);
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from Repository to GUI. Purpose - List of file names of currently present files in the Repository");
                Console.ResetColor();
                msg.show();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        

        /*----------------------------< receivers' thread that extracts messages from Repository's receiver blocking queue >-------------------------*/
        void rcvThreadProc(){
            while (true){
                try{
                    CommMessage msg = comm.rcvr.GetMessage();                               //dequeues a comm message from Repositorys' blocking queue
                    switch (msg.command){                                                 //switch statement that gets operated based on command given in the comm message
                        case "SendFilestoRepo":
                            string[] files = msg.body.Split(',');         //gets file names that user wants to transfer from his local storage to Repository 
                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received at Repository from GUI. Purpose - List of file names that user wants to transfer from local storage to Reposiotry Storage"); Console.ResetColor();
                            msg.show();  
                            foreach (var item in files)
                                postFile(item, @"../../../Local_storage/");      //Repository copies those files using Push model
                            break;
                        case "AskRepotoSendFilestoBuilder":
                            sendfileToBuilder(msg.body);    //gets comm message from GUI where GUI asks repository to send selected build requests to mother builder
                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received at Repository from GUI. Purpose - Gets request & file names of selected build request to send to MotherBuilder"); Console.ResetColor();
                            msg.show();                            break;
                        case "SendBuildLog":                                               //gets comm message from child builder containing build log file name 
                            string[] transfer_data = msg.body.Split(',');
                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received at Repository from ChildBuilder. Purpose - Gets BuildLog file name/s after building build request"); Console.ResetColor();
                            msg.show();
                            postFile(transfer_data[0], transfer_data[1]);              //Repository copies that build log file using Push model
                            break;
                        case "SendTestLog":                                 //gets comm message from Test Harness containing test log file name    
                            string[] file_data = msg.body.Split(',');
                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received at Repository from TestHarness. Purpose - Gets TestLog file name/s after testing build request"); Console.ResetColor();
                            msg.show();
                            if (postFile(file_data[0], file_data[1])){                       //Repository copies that test log file using Push model
                                if (file_data[2] == "true") {
                                    Console.ForegroundColor = ConsoleColor.Magenta; Console.WriteLine("Testing Successful! Deleting Temporary Directory"); Console.ResetColor();
                                    try {
                                        Directory.Delete(file_data[1], true);
                                    } catch (Exception e) { Console.WriteLine(e.Message); }
                                }
                            }                                                  
                            break;
                        case "RequestToGetFiles":
                            Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine("\nMessage Received at Repository from GUI. Purpose - Request from GUI to get List of file names of currently present files in the Repository"); Console.ResetColor();
                            msg.show();  show_file();                                       //calling show_file() function which will send present file names to GUI
                            break;
                        case "quit":
                            break;                                                 //if command is to quit then break
                        default: break;
                    }
                } catch (Exception e) { Console.WriteLine(e.Message); }
            }
        }

        /*---------------< Sends a communication message to Mother Builder with msg.body = name of file build request file/s >-----------------------*/
        public void sendfileToBuilder(string files)
        {
            string sendendPoint = Comm<Repository>.makeEndPoint("http://localhost", 8082, "MotherBuilder");          //creates sender endpoint at port addr - 8082
            try
            {
                CommMessage msg = new CommMessage(CommMessage.MessageType.BuildRequest);            //new comm message of type BuildRequest
                msg.from = endPoint;
                msg.to = sendendPoint;
                msg.command = "SendBRstoBuilder";
                msg.body = files;                                                                   //sends build request file/s name/s 
                comm.sndr.PostMessage(msg);
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from Repository to MotherBuilder. Purpose - forwarding build request file names got from GUI to MotherBuilder"); Console.ResetColor();
                msg.show();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
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
                Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine("\nMessage Sent from Repository to Repository. Purpose - After finishing everything on command sends quit message");
                Console.ResetColor();
                msg.show();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        /*--------------------------------< Copies files from one directory to another directory block by block [Push Model] >-----------------------*/
        public bool postFile(string fileName, string file_storage_path)
        {
            FileStream fs = null;
            string lastError = null;
            long bytesRemaining;

            try
            {
                string fileStored_at = file_storage_path;                           //storage path of file that is going to get copied
                string storage_path = @"../../../Repository/Repo_storage/";
                Console.WriteLine("File {0} getting copied at {1} from {2} status - Successful",fileName, storage_path ,fileStored_at);
                long blockSize = 1024;
                string path = Path.Combine(fileStored_at, fileName);                //combines sender/sources' file path with the file name
                fs = File.OpenRead(path);
                bytesRemaining = fs.Length;
                comm.rcvr.openFileForWrite(fileName, storage_path);                 //creates and opens a file at destination to write or copy the data from source file.
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
                Console.WriteLine(lastError);
                return false;
            }
            return true;
        }

    }

    class repo
    {
#if (TEST_Repository)
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "Repository";
                Repository repo = new Repository();
                repo.establish_channel();               //Creates communication channel for Repository at port add 8081
                
            }
            catch(Exception e) { Console.WriteLine(e.Message); }       

        }
#endif
    }
}
