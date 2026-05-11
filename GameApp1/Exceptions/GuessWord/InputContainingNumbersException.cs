namespace GameApp.Exceptions.GuessWord{
    internal class InputContainingNumbersException:Exception{
        private string _message;
        public InputContainingNumbersException(){
            _message="Input cannot contain numbers. Please enter a valid word.";
        }

        public InputContainingNumbersException(string word){
            _message=$"Input '{word}' contains numbers. Please enter a valid word without numbers.";
        }

        public override string Message => _message;
    }
}