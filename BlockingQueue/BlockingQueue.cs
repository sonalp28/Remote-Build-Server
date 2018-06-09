//////////////////////////////////////////////////////////////////////////////////////////////////////////
// BlockingQueue.cs  : Demonstrate threads communicating via Queue                                      //
//                                                                                                      //
// Platform          : Dell Inspiron 13 - Windows 10, Visual Studio 2017                                //-|_ 
// Language          : C# & .Net Framework                                                              //-|  <----------Requirement 1---------->
// Application       : Project 4 [Build Server] Software Modeling & Analysis CSE-681 Fall'17            //
// Author            : Sonal Patil, Syracuse University                                                 //
//                     spatil06@syr.edu (408)-416-6291                                                  //  
//Source             : Dr. Jim Fawcett, EECS, SU                                                        //
//////////////////////////////////////////////////////////////////////////////////////////////////////////
//------------------------------------------------------------------------------------------------------//
//Prologues - 1. public BlockingQueue() - Creates new blocking queue.                                   //
//            2. public void enQ(T msg) - It enqueues string message into the blocking queue.           //
//            3. public T deQ()         - It dequeues a string message from the blocking queue.         //
//            4. public int size()      - returns number of elements/items in the blocking queue.       //
//            5. public void clear()    - purges elements from the blocking queue                       //
//------------------------------------------------------------------------------------------------------//

/*
 *   Module Operations
 *   -----------------
 *   This package implements a generic blocking queue and demonstrates 
 *   communication between two threads using an instance of the queue. 
 *   If the queue is empty when a reader attempts to deQ an item then the
 *   reader will block until the writing thread enQs an item.  Thus waiting
 *   is efficient.
 * 
 *   NOTE:
 *   This blocking queue is implemented using a Monitor and lock, which is
 *   equivalent to using a condition variable with a lock.
 * 
 *   Public Interface
 *   ----------------
 *   BlockingQueue<string> bQ = new BlockingQueue<string>();
 *   bQ.enQ(msg);
 *   string msg = bQ.deQ();
 * 
 */
/* Required Files:
* ---------------
* -
*
* Maintenance History:
* --------------------
* ver 1.0 : 19 Nov 2017
*/
using System;
using System.Collections;
using System.Threading;

namespace SWTools
{
    public class BlockingQueue<T>
    {
        private Queue blockingQ;
        object locker_ = new object();

        //----< constructor >--------------------------------------------

        public BlockingQueue()
        {
            blockingQ = new Queue();
        }
        //----< enqueue a string >---------------------------------------

        public void enQ(T msg)
        {
            lock (locker_)  // uses Monitor
            {
                blockingQ.Enqueue(msg);
                Monitor.Pulse(locker_);
            }
        }
        //----< dequeue a T >---------------------------------------
        //
        // Note that the entire deQ operation occurs inside lock.
        // You need a Monitor (or condition variable) to do this.

        public T deQ()
        {
            T msg = default(T);
            lock (locker_)
            {
                while (this.size() == 0)
                {
                    Monitor.Wait(locker_);
                }
                msg = (T)blockingQ.Dequeue();
                return msg;
            }
        }
        //
        //----< return number of elements in queue >---------------------

        public int size()
        {
            int count;
            lock (locker_) { count = blockingQ.Count; }
            return count;
        }
        //----< purge elements from queue >------------------------------

        public void clear()
        {
            lock (locker_) { blockingQ.Clear(); }
        }
    }



  class Program
  {
#if (TEST_BLOCKINGQUEUE)
        static void Main(string[] args)
        {
            Console.WriteLine("============================Start of Blocking Queue=====================================================");
        }
#endif
    }

}
