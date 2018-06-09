///////////////////////////////////////////////////////////////////////////////////////////////////////
// IComm.cs       : Service for message passing communication                                        //
//                  Defines a service contract that describes the operations, or methods, that are   //
//                  available on the service endpoint, and exposed to the outside world.             //
//                                                                                                   //
// Platform       : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                //-|_ 
// Language       : C# & .Net Framework                                                              //-|  <----------Requirement 1---------->
// Application    : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17            //
// Author         : Sonal Patil, Syracuse University                                                 //
//                  spatil06@syr.edu (408)-416-6291                                                  //  
// Source         : Dr. Jim Fawcett, EECS, SU                                                        //
///////////////////////////////////////////////////////////////////////////////////////////////////////
//------------------------------------------------------------------------------------------------------------------------------------------------------------//
//Prologues - 1. public interface IComm - Interface that gives a contract for the given service.
//            2. public class CommMessage - Class that defines the data memebers, message types and methods to construct and show the communication message
//            3. public CommMessage(MessageType mt) - cConstructor that takes the message type and assignes it to current message
//            4. public void show() - displays the communication message shared betwwn two channels
//            5. public enum MessageType - Defines the enum with possible message types used while communicating between federartion 
//------------------------------------------------------------------------------------------------------------------------------------------------------------//
/* Required Files:
* ---------------
* -
*
* Maintenance History:
* ---------------------
* ver 1.0 : 19 Nov 2017
* - first release
*/

using System;
using System.ServiceModel;
using System.Runtime.Serialization;

namespace Comm
{
   
    [ServiceContract(Namespace = "Comm")]
    public interface IComm
    {
        /*--------------------------------------------------< support for message passing >-----------------------------------------------------*/

        [OperationContract(IsOneWay = true)]
        void PostMessage(CommMessage msg);              //posts the message to other endpoint 

        // private to receiver so not an OperationContract
        CommMessage GetMessage();                       //gets the message from other endpoint

        CommMessage GetReadyMessage();                  //gets the ready message from child Builder ReadyBlockingQ

        /*-----------------------------------------------< support for sending file in blocks >--------------------------------------------------*/
        [OperationContract]
        bool openFileForWrite(string name,string file_Storage);

        [OperationContract]
        bool writeFileBlock(byte[] block);

        [OperationContract(IsOneWay = true)]
        void closeFile();
    }

    [DataContract]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]
            processCount,
            [EnumMember]
            SendBuildRequest,           // GUI send message to Repository
            [EnumMember]
            BuildRequest,               // Repository sends Build Request to Builder
            [EnumMember]
            BuildLog,                   // Child Builders send build log to repository
            [EnumMember]
            TestRequest,                // Child Builders send test request to test harness
            [EnumMember]
            TestLog,                    // Test Harness sends test log to Repository
            [EnumMember]
            Ready,                      // Child Builders send ready message to Mother Builder
            [EnumMember]
            FileRequest,                // GUI, ChildBuilder asks dependency files from Repository
            [EnumMember]
            FileList,                   // List of files Repository sends to GUI on its request
            [EnumMember]
            quit ,                      // quit message
            [EnumMember]
            closeSender,                // close down client
            [EnumMember]
            closeReceiver               // close down server for graceful termination
        }

        /*----< constructor requires message type >--------------------*/
        public CommMessage(MessageType mt)
        {
            type = mt;
        }

        /*----< data members - all serializable public properties >----*/
        [DataMember]
        public MessageType type { get; set; }

        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string from { get; set; }

        [DataMember]
        public string author { get; set; } = "Sonal Patil";

        [DataMember]
        public string body { get; set; }

        [DataMember]
        public string command { get; set; }

        public void show()
        {
            Console.WriteLine("|----------------------------------------------------------------------------------------|");          
            Console.WriteLine("|To          : {0}", to);
            Console.WriteLine("|From        : {0}", from);
            Console.WriteLine("|MessageType : {0}", type.ToString());
            Console.WriteLine("|Command     : {0}", command);
            Console.WriteLine("|Body        : {0}", body);
            Console.WriteLine("|Author      : {0}", author);
            Console.WriteLine("|----------------------------------------------------------------------------------------|");
        }
    }
}
