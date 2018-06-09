using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace calculator
{
    public class calculator
    {
        static void Main(string[] args)
        {
        }
        public static float PerformMathOperation(float a, float b, char opera)
        {
            float answer;
            float input1 = a;
            float input2 = b;
            char opert = opera;
            //using switch, perform operations
            switch (opert)
            {
                case '+':
                    answer = input1 + input2;
                    break;
                case '-':
                    answer = input1 - input2;
                    break;
                case '*':
                    answer = input1 * input2;
                    break;
                case '/':
                    answer = input1 / input2;
                    break;
                case '%':
                    answer = input1 % input2;
                    break;
                default:
                    answer = 0;
                    break;
            }
            return answer;
        }

    }
}
