namespace GameApp.Data{
    internal class WordProvider{
        private List<string> words = new List<string>{
            "APPLE","MANGO","GRAPE","TRAIN","PLANT","BRAIN","CLOUD","STONE","RIVER","OCEAN","FLAME","BEACH","STORM","DANCE","MUSIC","HOUSE","HEART","PEACE","SMILE","LUCKY"
        };

        public string GetRandomWord(){
            return words[Random.Shared.Next(0,words.Count)];
        }
    }
}