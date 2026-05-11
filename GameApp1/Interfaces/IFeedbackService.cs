using System.Collections.Generic;

namespace GameApp.Interfaces{
    internal interface IFeedbackService{
        void AddFeedback(string feedback);
        void Clear();
        string GetLastFeedback();
        string GetFeedbackAt(int index);
        void DisplayFeedback(string guessWord);
    }
}