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

	public KMSelectable[] colorButtons, mainButtons;
	public KMSelectable back, submit;

	public GameObject[] squares;
	public GameObject mainWindow, submissionWindow;
	public GameObject statusLight;

	public Material[] colors;
	public Material[] screenColors;
	public Material statusLightUnsolvedColor;
	public MeshRenderer[] squareRender;
	public MeshRenderer screen, statusLightObj;
	public TextMesh[] buttonCB, squareCB, screenText;

	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;
	private bool isActive;

	private bool cbActive;

	private float increment = 1f;
	private float step;

	Data data = new Data();
	private string word, encrypted, colorEncrypted;
	private string[] keywords = new string[3];
	private string rotatingSquareShiftKW;
	private bool[] xSub = new bool[6];
	private string sub;
	private int pageIx = 0;

	private static readonly string[] colorNames = { "Green", "Red", "Blue", "Yellow", "Jade" };

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
		foreach (KMSelectable mainButton in mainButtons)
		{
			mainButton.OnInteract += delegate () { mainPress(mainButton); return false; };
		}
		back.OnInteract += delegate () { backPress(); return false; };
		submit.OnInteract += delegate () { submitPress(); return false; };

		cbActive = Colorblind.ColorblindModeActive;

		Module.OnActivate += onActivate;
    }

	
	void Start()
    {
		wordSelection();

		foreach (TextMesh square in squareCB)
		{
			square.text = "";
		}
		for (int i = 0; i < 5; i++)
		{
			buttonCB[i].text = cbActive ? colorNames[i].ToUpperInvariant() : "";
		}
		foreach (TextMesh screen in screenText)
		{
			screen.text = "";
		}
		StartCoroutine(startUp());
    }

	IEnumerator startUp()
	{
		mainWindow.SetActive(false);
		submissionWindow.SetActive(false);

		yield return new WaitForSeconds(1);
		screen.material = screenColors[1];
		yield return null;
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

		for (int i = 0; i < 3; i++)
		{
			keywords[i] = data.PickWord(3, 8);
		}

		for (int i = 0; i < 6; i++)
		{
			if (word[i] == 'X')
			{
				xSub[i] = true;
				encrypted += "ABCDEFGHIJKLMNOPQRSTUVWYZ"[rnd.Range(0, 25)];
			}
			else
			{
				encrypted += word[i];
			}
		}

		var base36Sum = Bomb.GetSerialNumber().Select(x => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(x)).Sum();

		string rotatingSqaureKeyA = getKey(keywords[1].Replace('X', 'Y'), "ABCDEFGHIJKLMNOPQRSTUVWYZ", Bomb.GetModuleNames().Any(x => "ƎNA Cipher".Contains(x)) || base36Sum % 2 != 0);
		string rotatingSquareKeyB = getKey(keywords[2].Replace('X', 'Y'), "ABCDEFGHIJKLMNOPQRSTUVWYZ", Bomb.GetModuleNames().Any(x => "Holographic Memory".Contains(x)) || Bomb.GetSerialNumberNumbers().Sum() % 2 == 0);

		encryptionStuff(rotatingSqaureKeyA, rotatingSquareKeyB);
	}

	void encryptionStuff(string keyA, string keyB)
	{


		for (int i = 0; i < 6; i++)
		{
			if (xSub[i])
			{
				sub += encrypted[i];
				encrypted = encrypted.Substring(0, i) + "X" + encrypted.Substring(i + 1);
			}
			else
			{
				sub += "ABCDEFGHIJKLMNOPQRSTUVWYZ"[rnd.Range(0, 25)];
			}
		}
	}

	void colorPress(KMSelectable color)
	{
		if (moduleSolved || !isActive)
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

	void mainPress(KMSelectable button)
	{
		if (moduleSolved || !isActive)
		{
			return;
		}

		for (int i = 0; i < 3; i++)
		{
			if (button == mainButtons[i])
			{
				switch (i)
				{
					case 0:
						pageIx--;
						pageInfo();
						break;
					case 1:
						subMode();
						break;
					case 2:
						pageIx++;
						pageInfo();
						break;
				}
			}
		}

        pageIx = pageIx < 0 ? 0 : pageIx > 1 ? 1 : pageIx;
    }

	void onActivate()
	{
		mainWindow.SetActive(true);
		screen.material = screenColors[2];
		isActive = true;
		pageInfo();
	}

	void pageInfo()
	{

	}

	void backPress()
	{
		if (moduleSolved || !isActive)
		{
			return;
		}
	}

	void submitPress()
	{
		if (moduleSolved || !isActive)
		{
			return;
		}
	}

	void subMode()
	{

	}
	
	
	void FixedUpdate()
    {
		step += increment;
		statusLight.transform.localRotation = Quaternion.Euler((step * Mathf.PI) / 2f, (step * Mathf.PI) / 4f, (step * Mathf.PI) / 6f);
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





