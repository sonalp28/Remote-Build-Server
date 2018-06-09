///////////////////////////////////////////////////////////////////////////
// TestLib.cs - Simulates testing production packages                    //
//                                                                       //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017       //
///////////////////////////////////////////////////////////////////////////
/*
 * Note:
 * Since both Tests and the production code they test are application
 * specific, tester classes will know the names and locations of the
 * tested classes, so there is no need to use dynamic invocation here.
 *
 * The project for this code simply makes a reference to the tested
 * Library and calls new on the relevent classes and invokes the
 * resulting instances methods directly.
 * 
 */
using System;
using System.Reflection;
using System.IO;

namespace DllLoaderDemo
{
  public class TestDriver1 : ITest
  {

    public TestDriver1()
    {
      
    }
    public virtual void say()
    {
      Console.Write("\n  Test #1:");
    }
    private ITested getTested()
    {
      CodeToTest1 tested = new CodeToTest1();
      return tested;
    }
    public virtual bool test()
    {
      ITested tested = getTested();
      return tested.say("Testing CodeToTest1.dll");
    }
  }
}
