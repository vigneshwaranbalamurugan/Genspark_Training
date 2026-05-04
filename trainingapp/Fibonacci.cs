namespace FibonacciApp{
    class Fibonacci{
        internal int FibonacciNum(int num){
            if(num==1 || num ==0){
                return num;
            }
            return FibonacciNum(n-1)+FibonacciNum(n-2);
        }
    }
}