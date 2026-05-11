using System;
using System.Collections.Generic;
using GameApp.Interfaces;
using GameApp.Repositories;

namespace GameApp.Services{
    internal class FeedbackService: IFeedbackService{
        private FeedbackRepository feedbackRepository = new FeedbackRepository();

        public void AddFeedback(string feedbackValue){
            feedbackRepository.Add(feedbackValue);
        }

        public void Clear(){
            feedbackRepository.Clear();
        }

        public string GetLastFeedback(){
            return feedbackRepository.GetLast();
        }

        public string GetFeedbackAt(int index){
            return feedbackRepository[index];
        }

        public void DisplayFeedback(string guessWord){
            string feedbackValue = feedbackRepository.GetLast();
            Console.WriteLine($"Feedback for your guess '{guessWord}':");
            for(int i = 0; i < feedbackValue.Length; i++){
                char c = feedbackValue[i];
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