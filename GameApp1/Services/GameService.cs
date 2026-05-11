using System;
using System.Linq;
using GameApp.Data;
using GameApp.Models;
using GameApp.Interfaces;
using GameApp.Exceptions.GuessWord;
using GameApp.Inputs;

namespace GameApp.Services{
    internal class GameService:IGameInteract{
        private GuessInputValidator guessValidator;
        private FeedbackGenerator feedbackGenerator;
        private IFeedbackService feedbackService;
        private WordProvider wordProvider;
        private Game game;

        public GameService(){
            guessValidator = new GuessInputValidator();
            feedbackGenerator = new FeedbackGenerator();
            feedbackService = new FeedbackService();
            wordProvider = new WordProvider();
            game = new Game(wordProvider.GetRandomWord());
        }

        private void Reset(){
            feedbackService.Clear();
            game.Reset(wordProvider.GetRandomWord());
        }

        public void PlayRound(){
            string guessWord = GetUserInput();
            string feedbackValue = feedbackGenerator.GenerateFeedback(guessWord, game.HiddenWord);
            game.AddGuess(guessWord);
            feedbackService.AddFeedback(feedbackValue);

            if(ValidateWord(guessWord)){
                game.score = game.calculateScore();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Your score: {game.score}");
                Console.ResetColor();
                Console.WriteLine("Would you like to replay? (Y/N)");
                string replayChoice = Console.ReadLine()??"";
                if(replayChoice.ToUpper().Equals("Y")){
                    Replay();
                }
                Console.WriteLine("Would you like to play again? (Y/N)");
                string playAgainChoice = Console.ReadLine()??"";
                if(playAgainChoice.ToUpper().Equals("Y")){
                    Reset();
                    StartGame();
                }
                game.CurrAttempt = game.MaxAttempts; // End the game loop
                Console.WriteLine("Thank you for playing! Goodbye!");
            }else if(game.CurrAttempt == game.MaxAttempts - 1){
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Game Over! The hidden word was '{game.HiddenWord}'.");
                Console.ResetColor();
                Console.WriteLine("Would you like to replay? (Y/N)");
                string replayChoice = Console.ReadLine()??"";
                if(replayChoice.ToUpper().Equals("Y")){
                    Replay();
                }
                Console.WriteLine("Would you like to play again? (Y/N)");
                string playAgainChoice = Console.ReadLine()??"";
                if(playAgainChoice.ToUpper().Equals("Y")){
                    Reset();
                    StartGame();
                }
                Console.WriteLine("Thank you for playing! Goodbye!");
                Console.WriteLine("------------------------------------------------");
            }
        }
    
        public void StartGame(){
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Welcome to the Word Guessing Game!");
            Console.WriteLine($"You have {game.MaxAttempts} attempts to guess the hidden 5-letter word.\nGood luck!\n");
            // Console.WriteLine($"The hidden word is: {game.HiddenWord}\n");
            Console.WriteLine("Instructions:");
            Console.WriteLine("G - Correct letter in the correct position (Green)");
            Console.WriteLine("Y - Correct letter in the wrong position (Yellow)");
            Console.WriteLine("X - Incorrect letter (Gray)\n");
            Console.WriteLine("Let's start the game!\n------------------------------------------------");
            while(game.CurrAttempt < game.MaxAttempts){
                Console.WriteLine($"Attempt {game.CurrAttempt + 1} of {game.MaxAttempts}");
                PlayRound();
                game.CurrAttempt++;
            }
        }

        private string GetUserInput()
        {
            try
            {
                Console.Write("Enter your guess: ");
                string guessWord = Console.ReadLine() ?? "";
                guessValidator.ValidateGuessInput(guessWord);
                if(game.IsGuessedAlready(guessWord)){
                    Console.WriteLine("You have already guessed that word. That is not a correct word. Please try a different word.");
                    return GetUserInput();
                }
                return guessWord.ToUpper();
            }catch (EmptyInputException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
            catch (LessThanFiveLetterException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
            catch (MoreThanFiveLetterException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
            catch (InputContainingNumbersException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
            catch (InputContainingSpecialException ex){
                Console.WriteLine(ex.Message);
                return GetUserInput();
            }
    
        }

        private bool ValidateWord(string guessWord){
            if(game.IsCorrectGuess(guessWord)){
                string message = (game.CurrAttempt + 1) switch
                {
                    1 => "Genius!",
                    2 => "Excellent!",
                    3 => "Great job!",
                    4 => "Good work!",
                    5 => "Nice try!",
                    6 => "Well done!",
                    _ => "Congratulations!"};
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{message} Congratulations!\nYou guessed the word '{game.HiddenWord}' in {game.CurrAttempt + 1} attempts!");
                Console.ResetColor();
                return true;
            }
            feedbackService.DisplayFeedback(game.PreviousGuesses.Last());
            return false;
        }

        public void Replay(){
            Console.WriteLine("Feedback for your guesses:");
            for(int i = 0; i < game.PreviousGuesses.Count; i++){
                var guess = game.PreviousGuesses[i];
                var feedbackValue = feedbackService.GetFeedbackAt(i);
                Console.Write($"Attempt {i + 1} ({guess}): ");
                for(int j = 0; j < feedbackValue.Length; j++){
                    char c = feedbackValue[j];
                    switch(char.ToUpper(c)){
                        case 'G':
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                        case 'Y':
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Gray;
                            break;
                    }
                    Console.Write(c);
                }
                Console.ResetColor();
                Console.WriteLine();
            }
        }
    }
}