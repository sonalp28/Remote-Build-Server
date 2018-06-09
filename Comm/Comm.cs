/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Comm.cs           : Demonstrate Message Passing Communication service created using Windows Communication Foundation//
//                     This Communicator service will get used by all clients & servers to communicate with each other //
//                     It mainly have 3 classes - Sender, Receiver and Comm                                            //
//                     GUI,Repository,Mother builder,Child Builder & Testharness communicates with each other using WCF// 
//                                                                                                                     //
// Platform          : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                               //-|_ 
// Language          : C# & .Net Framework                                                                             //-|  <----------Requirement 1---------->
// Application       : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17                           //
// Source            : Sonal Patil, Syracuse University                                                                //
//                     spatil06@syr.edu (408)-416-6291                                                                 //  
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//--------------------------------------------------------------------------------------------------------------------------------------------------------//
//Prologues - 
//class Receiver - 1. public Receiver() - Constructor creates new blocing queues one for build request and one for ready msg if they already son't exsits.
//                 2. public Thread start(ThreadStart rcvThreadProc) - starts the receivers' thread 
//                 3. public void Close() - closes the service
//                 4. public void CreateRecvChannel(string address) - creates the channel with ABC rule i.e using given type, binding and port address   
//                 5. public void PostMessage(Message msg) - Enqueues a message into the blocking queue.
//                 6. public Message GetMessage() - Dequeus a message from the blocking queue
//                 7. public Message GetReadyMsg() - Dequeus a ready message from the ready blocking queue
//                 8. public bool openFileForWrite(string name,string file_storage) - opens a file for writing
//                 9. public bool writeFileBlock(byte[] block) - writes into the opened files block by block where block sixe is 1024 which is constant
//                 10.public void closeFile() - closes the opened file for editing/copying
//class Sender -   1. public Sender() - Constructor creates new two blocking queues and starts the sender thread.
//                 2. public int getReadyblockingQ_size() - returns the current size of senders' Ready blocking queue [that holds ready messages]
//                 3. public int getblockingQ_size() - returns the current size of senders' blocking queue [that holds build requests]
//                 4. void ThreadProc() - processes the sender thread
//                 5. public void CreateSendChannel(string address) - creats the channel with ABC rule i.e using given type, binding and port address 
//                 6. public void PostMessage(Message msg) - Enqueues a message into the blocking queue.
//                 7. public void PostReadyMsg(Message msg)  - Enquesus a ready message into the blocking queue.
//                 8. public string GetLastError() - returns the error occured in the connection
//                 9. public void Close() - closes the temp created channel factory.
//class comm   -   1. public Comm() - Constructor sets the name of the receiver and sender for that particular channel
//                 2. public static string makeEndPoint(string url, int port) - creates and returns an endpoint with given url and port adddress
//                 3. public void close() - shutdowns comm
//--------------------------------------------------------------------------------------------------------------------------------------------------------//
/* Required Files:
* --------------------
* BlockingQueue.cs, IComm.cs
*
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017 ---First Release
* ver 1.1 : 5 Dec 2017 ----Final version
*/

using System;
using System.ServiceModel;
using System.Threading;
using System.IO;
using SWTools;

namespace Comm
{
/*--------------------------------------------------------------------------Receiver class-----------------------------------------------------------------------------*/
    public class Receiver<T> : IComm
    {
        /*-------------------------------------------------------------< Variable Declaration >------------------------------------------------------------------------*/
        static BlockingQueue<CommMessage> rcvBlockingQ = null;         //-_ blocking queue declaration to hold comm messages
        static BlockingQueue<CommMessage> ReadyrcvBlockingQ = null;    //-
        ServiceHost service = null;
        FileStream fs = null;
        string lastError = "";
        public string name { get; set; }

        /*------------------< Constructor that creates new blocking queues one for build request and one for ready msg if they already doesn't exsits >----------------*/
        public Receiver()
        {
            if (rcvBlockingQ == null)
            {
                rcvBlockingQ = new BlockingQueue<CommMessage>();                    //-_ created blocking quque that holds comm messages 
                ReadyrcvBlockingQ = new BlockingQueue<CommMessage>();               //-
            }
        }

        /*---------------------------------------------------------< starts the receivers' thread >--------------------------------------------------------------------*/ 
        public Thread start(ThreadStart rcvThreadProc)
        {
            Thread rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
            return rcvThread;
        }

        /*----------------------------------------------------------------< closes the service >-----------------------------------------------------------------------*/
        public void Close()
        {
            service.Close();
            (service as IDisposable).Dispose();
        }

        /*-----------------------------------------------------< Creates ServiceHost for Communication service >-------------------------------------------------------*/
        public void CreateRecvChannel(string address)
        {
            WSHttpBinding binding = new WSHttpBinding();                                //binding used - WSHttp
            Uri baseAddress = new Uri(address);
            service = new ServiceHost(typeof(Receiver<T>), baseAddress);
            service.AddServiceEndpoint(typeof(IComm), binding, baseAddress);
            service.Open();
            //Console.WriteLine("Service is open listening on {0}", address);
        }

        /*-------------------------------------------------< Implement service method to receive messages from other Peers >-------------------------------------------*/
        public void PostMessage(CommMessage msg)
        {
            rcvBlockingQ.enQ(msg);
        }

        /*--------------------------------------------------< Implement service method to extract messages from other Peers >------------------------------------------*/
        // This will often block on empty queue, so user should provide read thread.
        public CommMessage GetMessage()
        {
            CommMessage msg = rcvBlockingQ.deQ();
            return msg;
        }

        //extracts ready messages sent from child builders using ready blocking queue
        public CommMessage GetReadyMessage()
        {
            CommMessage msg = ReadyrcvBlockingQ.deQ();
            return msg;
        }

        /*------------------------------------------< file transfer from one peer to another peer block by block using Push model >------------------------------------*/
        public bool openFileForWrite(string name,string file_Storage)
        {
            try
            {
                string fileStorage = file_Storage;
                string writePath = Path.Combine(fileStorage, name);
                fs = File.OpenWrite(writePath);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }

        /*------------------------------------------------------< write a block received from Sender instance >--------------------------------------------------------*/
        public bool writeFileBlock(byte[] block)
        {
            try
            {
                fs.Write(block, 0, block.Length);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }

        /*-----------------------------------------------------------< close Receiver's uploaded file >----------------------------------------------------------------*/
        public void closeFile()
        {
            fs.Close();
        }
    }
/*-----------------------------------------------------------------------------------------------------------------------------------------------------*/


/*--------------------------------------------------------------------Sender class---------------------------------------------------------------------*/

    // Sender is client of another Peer's Communication service
    public class Sender
    {
        /*-------------------------------------------------------------< Variable Declaration >------------------------------------------------------------------------*/
        public string name { get; set; }
        IComm channel;
        string lastError = "";
        int tryCount = 0, MaxCount = 10;
        string currEndpoint = "";
        ChannelFactory<IComm> factory = null;
        BlockingQueue<CommMessage> sndBlockingQ = null;             //-_ blocking queue declaration to hold comm messages
        BlockingQueue<CommMessage> ReadysndBlockingQ = null;        //-
        Thread sndThrd = null;                                              //sender thread declaration

        /*----------------------------< Constructor creates new blocking queues for build request & for ready msg and starts sender thread >---------------------------*/
        public Sender()
        {
            sndBlockingQ = new BlockingQueue<CommMessage>();        //-_ blocking queue creation to hold comm messages
            ReadysndBlockingQ = new BlockingQueue<CommMessage>();   //-
            sndThrd = new Thread(ThreadProc);                               //initializes ThreadProc on sender thread      
            sndThrd.IsBackground = true;
            sndThrd.Start();                                                //starts sender thread
        }

        /*---------------------------------< returns the current size of senders' Ready blocking queue [that holds ready messages] >-----------------------------------*/
        public int getReadyblockingQ_size()
        {
            return ReadysndBlockingQ.size();
        }

        /*------------------------------------< returns the current size of senders' blocking queue [that holds build requests] >--------------------------------------*/
        public int getblockingQ_size()
        {
            return sndBlockingQ.size();
        }

        /*-----------------------------------------------------------< processing for send thread >--------------------------------------------------------------------*/
        void ThreadProc()
        {
            tryCount = 0;                                       //count for giving chance to connect
            while (true)
            {
                CommMessage msg = sndBlockingQ.deQ();
                if (msg.to != currEndpoint)
                {
                    currEndpoint = msg.to;                      //-_creates sender channel for endpoint where msg is going to gets send
                    CreateSendChannel(currEndpoint);            //-
                }
                while (true)
                {
                    try
                    {
                        channel.PostMessage(msg);               //posts message from one endpoint to sender's endpoint
                        //Console.WriteLine("\n Posted message from {0} to {1}", name, msg.to);
                        tryCount = 0;
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.Write("\nConnection failed {0}", ex.Message);
                        if (++tryCount < MaxCount)
                            Thread.Sleep(100);
                        else
                        {
                            Console.Write("\n{0}", "can't connect\n");
                            currEndpoint = "";
                            tryCount = 0;
                            break;
                        }
                    }
                }
                if (msg.body == "quit")
                    break;
            }
        }

        /*---------------------------------------------------< Create proxy to another Peer's Communicator >-----------------------------------------------------------*/
        public void CreateSendChannel(string address)
        {
            EndpointAddress baseAddress = new EndpointAddress(address);
            WSHttpBinding binding = new WSHttpBinding();
            factory = new ChannelFactory<IComm>(binding, address);
            channel = factory.CreateChannel();
        }

        /*-------------------------------------------------------< posts message to another Peer's queue >-------------------------------------------------------------*/
        //This is a non-service method that passes message to send thread for posting to service.
        public void PostMessage(CommMessage msg)
        {
            sndBlockingQ.enQ(msg);
        }

        //posts ready message from child builder to mother builder
        public void PostReadyMessage(CommMessage msg)
        {
            ReadysndBlockingQ.enQ(msg);
        }

        //returns the error occured in the connection
        public string GetLastError()
        {
            string temp = lastError;
            lastError = "";
            return temp;
        }

        /*-------------------------------------------------------------< closes the send channel >---------------------------------------------------------------------*/
        public void Close()
        {
            while (sndBlockingQ.size() > 0)
            {
                CommMessage msg = sndBlockingQ.deQ();
                channel.PostMessage(msg);
            }

            try
            {
                if (factory != null)
                    factory.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exeption - "+ ex.Message + " Already closed." );
            }
        }
    }
/*---------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

/*----------------------------------------------------------------------------Comm Class-------------------------------------------------------------------------------*/

    // Comm class combines Receiver and Sender
    public class Comm<T>
    {
        public string name { get; set; } = typeof(T).Name;
        public Receiver<T> rcvr { get; set; } = new Receiver<T>();         //creates recevicer for current comm channel
        public Sender sndr { get; set; } = new Sender();                   //creates sender for current comm channel

        /*--------------------------------------< Constructor sets the name of the receiver and sender for that current channel >--------------------------------------*/
        public Comm()
        {
            rcvr.name = name;
            sndr.name = name;
        }

        /*-------------------------------------------------< makes end points uding the given url and the port address >-----------------------------------------------*/
        public static string makeEndPoint(string url, int port, string name)
        {
            string endPoint = url + ":" + port.ToString() + @"/" + name;
            return endPoint;
        }
        public void close()
        {
            sndr.Close();
            rcvr.Close();
        }
    }
 /*--------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

}
