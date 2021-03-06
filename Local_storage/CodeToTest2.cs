///////////////////////////////////////////////////////////////////////////
// TestedLIb.cs - Simulates operation of a tested package                //
//                                                                       //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017       //
///////////////////////////////////////////////////////////////////////////

using System;

namespace DllLoaderDemo
{
    using calculator;
    public class CodeToTest2 : ITested
    {
        public CodeToTest2()
        {
            Console.Write("\n    constructing instance of Tested");
        }
        public Boolean say(string msg)
        {
                Console.Write("\n  Production Code: {0}", msg);
                //variable declaration
                float inputA = 1092, inputB = 56;
                char oper = '/';
                float result;

                //perform the math operations if inputs are correct by calling function
                result = calculator.PerformMathOperation(inputA, inputB, oper);

                //check the result 
                if (result == 19)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
      }
}
