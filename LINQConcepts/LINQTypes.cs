using System;
using System.Collections.Generic;
using System.Linq;
namespace LINQConcepts
{
    class LINQTypes
    {
        public void Main(string[] args)
        {
            //Step1: Data Source
            List<int> integerList = new List<int>()
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            };
            // //Step2: Query
            // //LINQ Query using Query Syntax to fetch all numbers which are > 5
            // var QuerySyntax = from number in integerList //Data Source
            //                   where number > 5 //Condition
            //                   select number; //Selection
            
            // LINQ Query using Method Syntax to fetch all numbers which are > 5
            // var QuerySyntax = integerList.Where(number => number>5).ToList();

            //LINQ Query using Mixed Syntax
            var MethodSyntax = (from obj in integerList
                                where obj > 5
                                select obj).Sum();


            //Step3: Execution
            // foreach (var item in QuerySyntax)
            // {
            //     Console.Write(item + " ");
            // }

            Console.WriteLine("Sum of numbers greater than 5 is: " + MethodSyntax);
            Console.ReadKey();
        }
    }
}