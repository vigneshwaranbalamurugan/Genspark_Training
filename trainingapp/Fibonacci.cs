namespace FibonacciApp{
    class Fibonacci{
        internal int FibonacciNum(int num){
            if(num==1 || num ==0){
                return num;
            }
            return FibonacciNum(num-1)+FibonacciNum(num-2);
        }
    }
}