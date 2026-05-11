namespace GameApp.Exceptions.GuessWord{
    internal class InputContainingSpecialException:Exception{
        private string _message;
        public InputContainingSpecialException(){
            _message="Input cannot contain special characters. Please enter a valid word.";
        }

        public InputContainingSpecialException(string word){
            _message=$"Input '{word}' contains special characters. Please enter a valid word without special characters.";
        }

        public override string Message => _message;
    }
}