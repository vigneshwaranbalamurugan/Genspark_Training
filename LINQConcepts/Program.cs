// using LINQConcepts;

// namespace LINQDemo
// {
//     class Program
//     {
//         static void Main(string[] args)
//         {
//             // Linq Query types
//             // new LINQTypes().Main(args);

//             // Linq Enumerable
//             // new EnumerableExample().Main(args);

//             // Linq Queriable
//             new QueriableExample().Main(args);
//         }
//     }
// }


using System;
using System.Collections.Generic;
using System.Linq;
namespace LINQConcepts
{
    class Program
    {
        static void Main(string[] args)
        {
            //Using Query Syntax
            // List<Employee> basicQuery = (from emp in Employee.GetEmployees()
            //                   select emp).ToList();
            // foreach (Employee emp in basicQuery)
            // {
            //  Console.WriteLine($"ID: {emp.ID}, Name: {emp.FirstName} {emp.LastName}");   
            // }

            // IEnumerable<Employee> basicMethod = Employee.GetEmployees().ToList();
            // foreach(Employee emp in basicMethod)
            // {
            //     Console.WriteLine($"ID: {emp.ID}, Name: {emp.FirstName} {emp.LastName}");
            // }

            IEnumerable<Employee> selectQuery = (from emp in Employee.GetEmployees( 
            )select new Employee(){
                FirstName=emp.FirstName,
                LastName=emp.LastName,
                Salary=emp.Salary
            });

            foreach(var emp in selectQuery)
            {
                Console.WriteLine($"Name: {emp.FirstName} {emp.LastName}, Salary: {emp.Salary}");
            }

        }
    }
}