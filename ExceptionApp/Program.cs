using System;

class Program{
    public static void Main(){
        try{
         checked{
            int num1 = int.MaxValue;
            num1--;num1++;
            Console.WriteLine("The updated value is " + num1);
            Console.WriteLine("Now you can enter a number");
            num1 = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Please enter the dinominator");
            int num2 = Convert.ToInt32(Console.ReadLine());
            var result = num1 / num2;
            Console.WriteLine("The final result is "+result);
            }
        }catch(OverflowException ex){
            Console.WriteLine("Overflow exception occurred: " + ex.Message);
        }catch(FormatException ex){
            Console.WriteLine("Format exception occurred: " + ex.Message);
            Console.WriteLine("Please enter a valid number");
        }catch(DivideByZeroException ex){
            Console.WriteLine("Divide by zero exception occurred: " + ex.Message);
            Console.WriteLine("Please enter a non-zero dinominator");
        }catch(Exception ex){
            Console.WriteLine("An unexpected exception occurred: " + ex.Message);
            Console.WriteLine("Please try again");
        }
        Console.WriteLine("Byee bye");
    }
}