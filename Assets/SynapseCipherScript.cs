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
	public TextMesh pageIxDisplay;

	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;
	private bool isActive;

	private bool cbActive;

	private float increment = 1f;
	private float step;

	Data data = new Data();
	private string word, encrypted, colorEncrypted;
	private string[] keywords = new string[2];
	private string[] binary = new string[2];
	private string rotatingSquareShiftKW;
	private string[] superPositionKW = new string[3];
	private bool[] jSub = new bool[6];
	private string sub;
	private int pageIx = 0;

	private static readonly string[] colorNames = { "Green", "Red", "Blue", "Yellow", "Jade" };

	private string baseConversion(int input, int ba)
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

	private string getKey(string kw, string alphabet, bool kwFirst)
	{
		return (kwFirst ? (kw + alphabet) : alphabet.Except(kw).Concat(kw)).Distinct().Join("");
	}

	private string xorBits(string bit1, string bit2)
	{
		string output = string.Empty;

		for (int i = 0; i < bit1.Length && i < bit2.Length; i++)
		{
			output += bit1[i] == bit2[i] ? "0" : "1";
		}

		return output;
	}

	private string xnorBits(string bit1, string bit2)
	{
		string output = string.Empty;

		for (int i = 0; i < bit1.Length && i < bit2.Length; i++)
		{
			output += bit1[i] == bit2[i] ? "1" : "0";
		}

		return output;
	}

	private string letterToTernary(char letter)
	{
        var ternaryLetters = new[] { "001", "002", "010", "011", "012", "020", "021", "022", "100", "101", "102", "110", "111", "112", "120", "121", "122", "200", "201", "202", "210", "211", "212", "220", "221", "222" };
		
		return ternaryLetters["ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(letter)];
    }

	private int baseTo10 (int input, int baseToConvert)
	{
		var total = 0;
		var numLength = input.ToString().Length;

		for (int i = 0; i < numLength; i++)
		{
			total += (int)Math.Pow(baseToConvert, numLength - (i + 1)) * int.Parse(input.ToString()[i].ToString());
		}
		return total;
	}

	bool isPrime(int input)
	{
		if (input <= 1)
		{
			return false;
		}
		if (input == 2)
		{
			return true;
		}

		var limit = (int) Math.Floor(Math.Sqrt(input));

		for (int i = 2; i <= limit; i++)
		{
			if (input % i == 0)
			{
				return false;
			}
		}
		return true;
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

		for (int i = 0; i < 2; i++)
		{
			keywords[i] = data.PickWord(3, 8);
		}
		
		for (int i = 0; i < 3; i++)
		{
            superPositionKW[i] = i == 0 ? data.PickWord(3,8) : data.PickWord(6);
        }

		for (int i = 0; i < 6; i++)
		{
			if (word[i] == 'J')
			{
				jSub[i] = true;
				encrypted += "ABCDEFGHIKLMNOPQRSTUVWXYZ"[rnd.Range(0, 25)];
			}
			else
			{
				encrypted += word[i];
			}
		}

		var base36Sum = Bomb.GetSerialNumber().Select(x => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(x)).Sum();

		string rotatingSqaureKeyA = getKey(keywords[0].Replace('J', 'I'), "ABCDEFGHIKLMNOPQRSTUVWXYZ", Bomb.GetModuleNames().Any(x => "ƎNA Cipher".Contains(x)) || base36Sum % 2 != 0);
		string rotatingSquareKeyB = getKey(keywords[1].Replace('J', 'I'), "ABCDEFGHIKLMNOPQRSTUVWXYZ", Bomb.GetModuleNames().Any(x => "Holographic Memory".Contains(x)) || Bomb.GetSerialNumberNumbers().Sum() % 2 == 0);

		string superPositionKey = getKey(superPositionKW[0], "ABCDEFGHIJKLMNOPQRSTUVWXYZ", isPrime(Bomb.GetPortCount()));

		encryptionStuff(rotatingSqaureKeyA, rotatingSquareKeyB);
	}

	void encryptionStuff(string keyA, string keyB)
	{
		/* 
		 * DOUBLE SQUARE ROTATION CIPHER
		 * 00 01 02 03 04
		 * 05 06 07 08 09
		 * 10 11 12 13 14
		 * 15 16 17 18 19
		 * 20 21 22 23 24
		 */
		Debug.Log(word);
		Debug.Log(keyA);
		Debug.Log(keyB);
		var idxesOuterSquare = new[] { 0, 5, 10, 15, 20, 21, 22, 23, 24, 19, 14, 9, 4, 3, 2, 1 };
		var idxesInnerSquare = new[] { 6, 11, 16, 17, 18, 13, 8, 7 };
		var valuesKeywordMod10 = rotatingSquareShiftKW.Select(a => "-ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(a) % 10);
		Debug.Log(valuesKeywordMod10.Join());
		for (var o = 0; o < word.Length; o++)
		{
			var curLetter = word[o];
			var idxCurLetterInGrid = (o % 2 == 0 ? keyA : keyB).IndexOf(curLetter);
			var replacementLetter = (o % 2 == 0 ? keyB : keyA)[12];
			Debug.Log(idxCurLetterInGrid);
			if (idxesInnerSquare.Contains(idxCurLetterInGrid))
			{
				var idxFromInner = idxesInnerSquare.IndexOf(a => a == idxCurLetterInGrid);
				replacementLetter = (o % 2 == 0 ? keyA : keyB)[idxesInnerSquare[(idxFromInner + valuesKeywordMod10.ElementAt(o)) % 8]];
			}
			else if (idxesOuterSquare.Contains(idxCurLetterInGrid))
			{
				var idxFromOuter = idxesOuterSquare.IndexOf(a => a == idxCurLetterInGrid);
				replacementLetter = (o % 2 == 0 ? keyA : keyB)[idxesOuterSquare[(idxFromOuter + valuesKeywordMod10.ElementAt(o)) % 16]];
			}
			Debug.LogFormat("{0} -> {1}", word[o], replacementLetter);
			encrypted = encrypted.Substring(0, o) + replacementLetter + encrypted.Substring(o + 1);
		}


		for (int i = 0; i < 6; i++)
		{
			if (jSub[i])
			{
				sub += encrypted[i];
				encrypted = encrypted.Substring(0, i) + "J" + encrypted.Substring(i + 1);
			}
			else
			{
				sub += "ABCDEFGHIKLMNOPQRSTUVWXYZ"[rnd.Range(0, 25)];
			}
		}

		// LOGICAL TERNARY MANUPULATION CIPHER

		for (int i = 0; i < 2; i++)
		{
			var range = rnd.Range(0, 63);
			binary[i] = Convert.ToString(range, 2).PadLeft(6, '0');
		}

		Debug.Log(binary.Join(", "));

		var binaryOutput = string.Empty;
		var logicFlip = Bomb.GetSerialNumberNumbers().First() % 2 != Bomb.GetSerialNumberNumbers().Last() % 2;

		for (int i = 0; i < 6; i+= 3)
		{
			string threeBit1 = binary[0].Substring(i, 3);
			string threeBit2 = binary[1].Substring(i, 3);

			binaryOutput += logicFlip ? xnorBits(threeBit1, threeBit2) : xorBits(threeBit1, threeBit2);
			logicFlip = !logicFlip;
		}
		string[] convertedLetters = new string[6];

		for (int i = 0; i < 6; i++)
		{
			convertedLetters[i] = letterToTernary(encrypted[i]);
		}

		int[][] convertNum = new int[6][];

		for (int i = 0; i < 6; i++)
		{
			convertNum[i] = new int[3];

			for (int j = 0; j < 3; j++)
			{
				int.TryParse(convertedLetters[i][j].ToString(), out convertNum[i][j]);
			}
		}

		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				if (binaryOutput[i] == '1')
				{
					convertNum[i][j]--;
					if (convertNum[i][j] < 0)
					{
						convertNum[i][j] = 2;
					}
					else if (convertNum[i][0] == 0 && convertNum[i][1] == 0 && convertNum[i][2] == 0)
					{
						convertNum[i][j] = 2;
					}
				}
			}
		}

		int[] concat = new int[6];

		for (int i = 0; i < 6; i++)
		{
			concat[i] = int.Parse(convertNum[i][0].ToString() + convertNum[i][1].ToString() + convertNum[i][2].ToString());
			concat[i] = baseTo10(concat[i], 3);
		}

		Debug.Log(concat.Join());

		string newEncrypt = string.Empty;

		for (int i = 0; i < 6; i++)
		{
			newEncrypt += "-ABCDEFGHIJKLMNOPQRSTUVWXYZ"[concat[i]];
		}

		encrypted = newEncrypt;





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
		button.AddInteractionPunch(0.4f);

		for (int i = 0; i < 3; i++)
		{
			if (button == mainButtons[i])
			{
				Audio.PlaySoundAtTransform(i == 0 || i == 2 ? "Arrow" : "Submit", transform);
			}
		}

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
                        pageIx = pageIx < 0 ? 0 : pageIx > 3 ? 3 : pageIx;
                        pageInfo();
						break;
					case 1:
						subMode();
						break;
					case 2:
						pageIx++;
                        pageIx = pageIx < 0 ? 0 : pageIx > 3 ? 3 : pageIx;
                        pageInfo();
						break;
				}
			}
		}       
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
		switch (pageIx)
		{
			case 0:
				screenText[0].text = encrypted;
				screenText[1].text = sub;
				screenText[2].text = string.Empty;
				break;
			case 1:
				screenText[0].text = superPositionKW[0];
				screenText[1].text = superPositionKW[1];
				screenText[2].text = superPositionKW[2];
				break;
			case 2:
				screenText[0].text = binary[0];
				screenText[1].text = binary[1];
				screenText[2].text = string.Empty;
				break;
			case 3:
                screenText[0].text = keywords[0];
                screenText[1].text = keywords[1];
                screenText[2].text = rotatingSquareShiftKW;
                break;
		}
        pageIxDisplay.text = (pageIx + 1).ToString();

		for (int i = 0; i < 3; i++)
		{
			switch (screenText[i].text.Length)
			{
				case 7:
					screenText[i].fontSize = 250;
					break;
				case 8:
					screenText[i].fontSize = 225;
					break;
				default:
					screenText[i].fontSize = 300;
					break;
			}
		}
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





