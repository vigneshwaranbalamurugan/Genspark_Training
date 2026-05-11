namespace GameApp.Exceptions.GuessWord{
    internal class MoreThanFiveLetterException:Exception{
        private string _message;
        public MoreThanFiveLetterException(){
            _message="Input must be no more than 5 letters long. Please enter a valid word.";
        }

        public MoreThanFiveLetterException(string word){
            _message=$"Input '{word}' is too long. Please enter a word with no more than 5 letters.";
        }

        public override string Message => _message;
    }
}