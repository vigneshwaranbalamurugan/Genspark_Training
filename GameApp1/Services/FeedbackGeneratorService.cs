namespace GameApp.Services{
    internal class FeedbackGenerator{
        public string GenerateFeedback(string guessWord, string hiddenWord){
            char[] feedback = new char[guessWord.Length];
            HashSet<char> correctLetters = new HashSet<char>();

            for(int i = 0; i < guessWord.Length; i++){
                if(guessWord[i] == hiddenWord[i]){
                    feedback[i] = 'G';
                    correctLetters.Add(guessWord[i]);
                }
            }

            for(int i = 0; i < guessWord.Length; i++){
                if(feedback[i] == 'G'){
                    continue;
                }

                char c = guessWord[i];

                if(hiddenWord.Contains(c)){
                    if(correctLetters.Contains(c)){
                        feedback[i] = 'X';
                    }else{
                        feedback[i] = 'Y';
                    }
                }else{
                    feedback[i] = 'X';
                }
            }

            return new string(feedback);
        }
    }
}