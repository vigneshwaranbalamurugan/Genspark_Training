namespace GameApp.Exceptions.GuessWord{
    internal class EmptyInputException:Exception{
        private string _message;
        public EmptyInputException(){
            _message="Input cannot be empty. Please enter a valid word.";
        }

        public override string Message => _message;
    }
}