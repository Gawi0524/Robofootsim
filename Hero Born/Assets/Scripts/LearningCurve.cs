using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LearningCurve : MonoBehaviour
{
    public Transform camTransform;

   


   void Start()
    {
      Character hero = new Character();
        Character hero2 = hero;
        hero.PrintStatsInfo();
        hero2.PrintStatsInfo();
        hero2.Reset();
      Character heroine = new Character("Agatha");
        heroine.PrintStatsInfo();

        Weapon huntingBow = new Weapon("Hunting Bow", 105);

        camTransform = this.GetComponent<Transform>();
        Debug.Log(camTransform.localPosition);
    }
    void Update()
    {
        
    }
}


/*
public int currentAge = 30;
//создание переменной. public - модификатор доступа. Их шесть
Debug.Log(currentAge + 1);
//метод для отладки, используется для простого текста
Debug.LogFormat("Text goes here, add {0} and {1} as variable placeholders", variable1, variable2);
//метод класса дебаг для сложной отладки. Можно размещать переменные внутри текста через заполнители
Debug.Log($"A string can have variebles like {variable} inserted directly!");
//Интерполированная строка. Здесь переменные размещаются сразу на своих местах, без необходимости смотреть на индексы

string characterAction = "Attack";
switch(characterAction)
{
    case "Heal":
        Debug.Log("Potion sent.");
        break;
    case "Attack":
        Debug.Log("To arms!");
        break;
    default:
        Debug.Log("Shields up.");
        break;
}

//оператор свич для выбора одного действия из нескольких

int[] topPlayerScores = new int[3];
int[] topPlayerScores = { 713, 549, 984 };
//объявление и инициализация массива

List<string> questPartyMembers = new List<string>()
{"Grim the Barbarian", "Merlin the Mage", "Sterling the Knight"};
Debug.LogFormat("Party Members: {0}", questPartyMembers.Count);
//Объявление и инициализация списка, аналога динамического массива из плюсов.


    Dictionary<string, int> itemInventory = new Dictionary<string, int>()
    {
        {"Potion", 5 },
        {"Antidote", 7 },
        {"Aspirin", 1 }

    };
Debug.LogFormat(Items: {0}, itemInventory.Count);
//Словари. Хранилища для пары ключ-значение

 и


    */