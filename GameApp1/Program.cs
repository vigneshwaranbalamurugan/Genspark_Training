using System;
using GameApp.Services;
using GameApp.Interfaces;

class Program{
    
    IGameInteract gameInteract;
    public Program(){
        gameInteract=new GameService();
    }

    internal static void Main(string[] args){
        new Program().gameInteract.StartGame();
    }
}