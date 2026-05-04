namespace CalculatorApp{
    internal class Calculator{
        // addition, subtraction, multiplication and division
        internal double Add(int num1,int num2){
            return num1+num2;
        }
        internal double Substract(int num1,int num2){
            return num1-num2;
        }
        internal double multiply(int num1,int num2){
            return num1*num2;
        }
        internal double Divide(int num1,int num2){
            return (double)num1/num2;
        }
        // double because division can return a decimal value
        internal double Calculate(int num1,int num2,char operation){
            double result=0;
            switch(operation){
                case '+':
                    result=Add(num1,num2);
                    break;
                case '-':
                    result=Substract(num1,num2);
                    break;
                case '*':
                    result=multiply(num1,num2);
                    break;
                case '/':
                    result=Divide(num1,num2);
                    break;
                default:
                    Console.WriteLine("Invalid operation");
                    break;
            }
            return result;
        }
    }
}