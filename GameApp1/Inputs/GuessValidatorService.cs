using System;
using System.Linq;
using GameApp.Exceptions.GuessWord;

namespace GameApp.Inputs{
    internal class GuessInputValidator{
        public bool ValidateGuessInput(string guessWord){
            if(string.IsNullOrWhiteSpace(guessWord)){
                throw new EmptyInputException();
            }
    
            if(guessWord.Length>5){
                throw new MoreThanFiveLetterException();
            }

            if(guessWord.Length<5){
                throw new LessThanFiveLetterException();
            }

            if(guessWord.Any(ch=>!char.IsLetter(ch))){
                if(guessWord.Any(char.IsDigit)){
                    throw new InputContainingNumbersException();
                }

                throw new InputContainingSpecialException();
            }

            return true;
        }
    }
}