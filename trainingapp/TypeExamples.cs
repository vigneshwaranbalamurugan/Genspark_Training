using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnderstandingTypesApp
{
    internal class TypeExamples
    {
        internal void ShowingConvertions()
        {
            int iNum1 = 100;
            float fNum2 = 123.6f;
            string strNum3 = null;
            fNum2 = iNum1; //implict type casting
            Console.WriteLine($"The value of fNum2 is {fNum2}");
            //iNum1 = (int)fNum2; //Explicit type casting
            iNum1 = Convert.ToInt32(Math.Round(fNum2)); //Explicit type casting
            Console.WriteLine("Please enter a number");
            iNum1 = Convert.ToInt32(Console.ReadLine());//Unboxing
            strNum3 = iNum1.ToString();//Boxing
            Console.WriteLine("The value of  num1 is "+iNum1);
        }
        internal void ShowingLimits()
        {
            checked
            {
                int num1 = int.MaxValue;
                Console.WriteLine($"The value of num1 before change is {num1}");
                // num1++;
                Console.WriteLine($"The value of num1 after change is {num1}");
            }
        }
    }
}
