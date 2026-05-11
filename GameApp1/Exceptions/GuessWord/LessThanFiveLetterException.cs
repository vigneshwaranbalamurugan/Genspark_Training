namespace GameApp.Exceptions.GuessWord{
    internal class LessThanFiveLetterException:Exception{
        private string _message;
        public LessThanFiveLetterException(){
            _message="Input must be at least 5 letters long. Please enter a valid word.";
        }

        public LessThanFiveLetterException(string word){
            _message=$"Input '{word}' is too short. Please enter a word with at least 5 letters.";
        }

        public override string Message => _message;
    }
}