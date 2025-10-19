using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordInfo : MonoBehaviour
{
    [SerializeField, TextArea] public string gameStory;
    [SerializeField, TextArea] public string gameWord;

    public string GetPrompt()
    {
        return $"Game Story: {gameStory}\n" +
               $"Game Word: {gameWord}\n";
    }
}

