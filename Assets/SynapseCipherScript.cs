using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Words;
using rnd = UnityEngine.Random;

public class SynapseCipherScript : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMColorblindMode Colorblind;

	public KMSelectable[] colorButtons;
	public KMSelectable back, submit;

	public GameObject[] squares, buttonObj;
	public GameObject mainWindow, submissionWindow;

	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;

	private bool cbActive;

	Data data = new Data();
	private string word, encrypted, colorEncrypted;
	private string[] keywords = new string[2];
	private string rotatingSquareShiftKW;

	private static string baseConversion(int input, int ba)
	{
		string ix = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		string current = string.Empty;

		while (input != 0)
		{
			current = ix[input % ba] + current;
			input /= ba;
		}

		return current;
	}

	private static string getKey(string kw, string alphabet, bool kwFirst)
	{
		return (kwFirst ? (kw + alphabet) : alphabet.Except(kw).Concat(kw)).Distinct().Join("");
	}

	void Awake()
    {

		moduleId = moduleIdCounter++;

		foreach (KMSelectable color in colorButtons)
		{
			color.OnInteract += delegate () { colorPress(color); return false; };
		}
		back.OnInteract += delegate () { backPress(); return false; };
		submit.OnInteract += delegate () { submitPress(); return false; };

		cbActive = Colorblind.ColorblindModeActive;
    }

	
	void Start()
    {
		wordSelection();
    }

	string encodingColorSeq(string word)
	{
		int[] num = word.Select(x => "-ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(x)).ToArray();
		int[] reverse = word.Select(x => ("-ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(x)) % 4 + 2).ToArray();
		Array.Reverse(reverse);

		string[] baseConverted = new string[word.Length];

		for (int i = 0; i < word.Length; i++)
		{
			baseConverted[i] = baseConversion(num[i], reverse[i]);
		}

		return baseConverted.Join("");
	}

	void wordSelection()
	{
		word = data.PickWord(6);
		colorEncrypted = encodingColorSeq(word);

		rotatingSquareShiftKW = data.PickWord(6);

		for (int i = 0; i < 2; i++)
		{
			keywords[i] = data.PickWord(3, 8);
		}

		var base36Sum = Bomb.GetSerialNumber().Select(x => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(x)).Sum();

		string rotatingSqaureKey = getKey(keywords[1], "ABCDEFGHIJKLMNOPQRSTUVWYZ", Bomb.GetModuleNames().Any(x => "ƎNA Cipher".Contains(x) || "Holographic Memory".Contains(x)) || base36Sum % 2 != 0);
	}

	void colorPress(KMSelectable color)
	{
		if (moduleSolved)
		{
			return;
		}

		for (int i = 0; i < 5; i++)
		{
			if (color == colorButtons[i])
			{

			}
		}
	}

	void backPress()
	{
		if (moduleSolved)
		{
			return;
		}
	}

	void submitPress()
	{
		if (moduleSolved)
		{
			return;
		}
	}
	
	
	void Update()
    {

    }

	// Twitch Plays


#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use <!{0} foobar> to do something.";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string command)
    {
		command = command.Trim().ToUpperInvariant();
		List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
		yield return null;
    }

	IEnumerator TwitchHandleForcedSolve()
    {
		yield return null;
    }


}





