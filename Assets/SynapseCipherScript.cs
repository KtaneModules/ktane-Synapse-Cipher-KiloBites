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
	public KMSelectable back, submit, reset;

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
	private bool inSubmission;

	private bool cbActive;

	private readonly float increment = 1f;
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
	private int colorIx = 0;
	private string submissionString = string.Empty;

	private static readonly string[] colorNames = { "Green", "Red", "Blue", "Yellow", "Jade" };

	private List<int> colorCBIx = new List<int>();

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
		reset.OnInteract += delegate () { resetPress(); return false; };

		cbActive = Colorblind.ColorblindModeActive;

		Module.OnActivate += onActivate;
    }
	void QuickLog(string toLog, params object[] args)
    {
		Debug.LogFormat("[Synapse Cipher #{0}] {1}", moduleId, string.Format(toLog, args));
    }
	
	void Start()
    {
		wordSelection();

		foreach (TextMesh square in squareCB)
		{
			square.text = "";
		}
		foreach (TextMesh screen in screenText)
		{
			screen.text = "";
		}
		foreach (var squareObj in squares)
		{
			squareObj.SetActive(false);
		}
		StartCoroutine(startUp());
		int[] colorIx = colorEncrypted.Select(x => "01234".IndexOf(x)).ToArray();
		string colorSeq = "";
		for (int i = 0; i <  colorIx.Length; i++)
		{
			colorSeq += "GRBYJ"[colorIx[i]];
		}
		QuickLog("The following color sequence must be submitted: {0}", colorSeq);
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

		encryptionStuff(rotatingSqaureKeyA, rotatingSquareKeyB, superPositionKey);
	}

	void encryptionStuff(string keyA, string keyB, string keyC)
	{
		/* 
		 * DOUBLE SQUARE ROTATION CIPHER
		 * 00 01 02 03 04
		 * 05 06 07 08 09
		 * 10 11 12 13 14
		 * 15 16 17 18 19
		 * 20 21 22 23 24
		 */
		QuickLog("The decrypted word is: {0}", word);
		QuickLog("KEY A: {0}" , keyA);
		QuickLog("KEY B: {0}" , keyB);
		var idxesOuterSquare = new[] { 0, 5, 10, 15, 20, 21, 22, 23, 24, 19, 14, 9, 4, 3, 2, 1 };
		var idxesInnerSquare = new[] { 6, 11, 16, 17, 18, 13, 8, 7 };
		var valuesKeywordMod10 = rotatingSquareShiftKW.Select(a => "-ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(a) % 10);
		QuickLog("Alphabetic positions of {1}, mod 10: {0}", valuesKeywordMod10.Join(), rotatingSquareShiftKW);
		//Debug.Log(valuesKeywordMod10.Join());
		for (var o = 0; o < word.Length; o++)
		{
			var curLetter = word[o];
			var idxCurLetterInGrid = (o % 2 == 0 ? keyA : keyB).IndexOf(curLetter);
			var replacementLetter = (o % 2 == 0 ? keyA : keyB)[12];
			//Debug.Log(idxCurLetterInGrid);
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
			QuickLog("{0} -> {1}", word[o], replacementLetter);
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

		QuickLog("After Double Square Rotation Cipher: {0}", encrypted);

		// LOGICAL TERNARY MANUPULATION CIPHER

		for (int i = 0; i < 2; i++)
		{
			var range = rnd.Range(0, 63);
			binary[i] = Convert.ToString(range, 2).PadLeft(6, '0');
		}

		QuickLog("Binary displayed: {0}",binary.Join(", "));

		var binaryOutput = string.Empty;
		var logicFlip = Bomb.GetSerialNumberNumbers().First() % 2 != Bomb.GetSerialNumberNumbers().Last() % 2;

		for (int i = 0; i < 6; i+= 3)
		{
			string threeBit1 = binary[0].Substring(i, 3);
			string threeBit2 = binary[1].Substring(i, 3);

			binaryOutput += logicFlip ? xnorBits(threeBit1, threeBit2) : xorBits(threeBit1, threeBit2);
			logicFlip = !logicFlip;
		}

		QuickLog(binaryOutput);
		string[] convertedLetters = new string[6];

		for (int i = 0; i < 6; i++)
		{
			convertedLetters[i] = letterToTernary(encrypted[i]);
		}

		QuickLog(convertedLetters.Join(", "));

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
			if (binaryOutput[i] == '1')
			{
				for (int j = 0; j < 3; j++)
				{
				
					convertNum[i][j]--;
					if (convertNum[i][j] < 0)
						convertNum[i][j] = 2;
				}
				if (convertNum[i].All(a => a == 0))
					for (int j = 0; j < 3; j++)
							convertNum[i][j] = 2;
			}
		}

		QuickLog("After manipulation: {0}", convertNum.Select(x => x.Join("")).Join(", "));

		int[] concat = new int[6];

		for (int i = 0; i < 6; i++)
		{
			concat[i] = int.Parse(convertNum[i][0].ToString() + convertNum[i][1].ToString() + convertNum[i][2].ToString());
			concat[i] = baseTo10(concat[i], 3);
		}

		string newEncrypt = string.Empty;

		for (int i = 0; i < 6; i++)
		{
			newEncrypt += "-ABCDEFGHIJKLMNOPQRSTUVWXYZ"[concat[i]];
		}

		encrypted = newEncrypt;

		QuickLog("After Logical Ternary Manipulation Cipher: {0}", encrypted);

		// SUPERPOSITION CIPHER
		//var offsetsAll = new int[word.Length];
		var finalEncrypted = "";
		QuickLog("KEY C: {0}" ,keyC);
		for (var p = 0; p < word.Length; p++)
        {
			var offsetCurLetter = (keyC.IndexOf(superPositionKW[1][p]) - keyC.IndexOf(superPositionKW[2][p]) + 26) % 26;
			QuickLog("{0} -> {1} ({2})", superPositionKW[1][p], superPositionKW[2][p], offsetCurLetter);
			QuickLog("{0} -> {1}", encrypted[p], keyC[(keyC.IndexOf(encrypted[p]) + offsetCurLetter) % 26]);
			finalEncrypted += keyC[(keyC.IndexOf(encrypted[p]) + offsetCurLetter) % 26];
		}
		encrypted = finalEncrypted;

		QuickLog("After Superposition Cipher: {0}", encrypted);
	}

	void colorPress(KMSelectable color)
	{
		color.AddInteractionPunch(0.4f);

		if (moduleSolved || !isActive || !inSubmission || colorIx > 24)
		{
			return;
		}

		for (int i = 0; i < 5; i++)
		{
			if (color == colorButtons[i] && colorIx < 24)
			{
				Audio.PlaySoundAtTransform(colorNames[i], transform);
				submissionString += i;
				squares[colorIx].SetActive(true);
				squareRender[colorIx].material = colors[i];
				squareCB[colorIx].text = cbActive ? colorNames[i][0].ToString() : "";
				colorCBIx.Add(i);
				colorIx++;
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
                        pageIx = (pageIx % 4 + 4) % 4;
                        pageInfo();
						break;
					case 1:
						subMode();
						break;
					case 2:
						pageIx++;
                        pageIx %= 4;
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
			screenText[i].fontSize = screenText[i].text.Length == 7 ? 250 : screenText[i].text.Length == 8 ? 225 : 300;
		}
    }

	void backPress()
	{
		if (moduleSolved || !isActive || !inSubmission || colorIx == 0)
		{
			return;
		}

		Audio.PlaySoundAtTransform("Clear", transform);

		if (submissionString.Length > 0)
		{
            submissionString = submissionString.Remove(submissionString.Length - 1);
			colorCBIx.RemoveAt(colorCBIx.Count - 1);
        }
        colorIx--;
        squares[colorIx].SetActive(false);

    }

	void submitPress()
	{
		if (moduleSolved || !isActive || !inSubmission)
		{
			return;
		}

		if (submissionString == colorEncrypted)
		{
			giveSolve();
		}
		else
		{
			StartCoroutine(giveStrike());
		}

	}

	void giveSolve()
	{
		inSubmission = false;
		foreach (var text in buttonCB)
		{
			text.text = "";
		}
		foreach (var squareText in squareCB)
		{
			squareText.text = "";
		}

		Audio.PlaySoundAtTransform("Solve", transform);
		StartCoroutine(solveStatusLight());
		StartCoroutine(solveSquarePatterns());
		moduleSolved = true;
		Module.HandlePass();
	}

	IEnumerator solveStatusLight()
	{
		yield return null;

		while (true)
		{
			for (int i = 0; i < 5; i++)
			{
				statusLightObj.material = colors[i];
				yield return new WaitForSeconds(0.2f);
			}
		}
	}

	IEnumerator solveSquarePatterns()
	{
		yield return null;

		var ix = 0;

		while (true)
		{
			for (int i = 0; i < submissionString.Length; i++)
			{
				squareRender[i].material = colors[ix];
				yield return new WaitForSeconds(0.015f);
			}
			ix++;
			ix %= 5;
			for (int j = submissionString.Length - 1; j >= 0; j--)
			{
				squareRender[j].material = colors[ix];
				yield return new WaitForSeconds(0.015f);
			}
			ix++;
			ix %= 5;
		}
	}

	IEnumerator giveStrike()
	{
		yield return null;
		colorCBIx.Clear();
		colorIx = 0;
		foreach (var square in squares)
		{
			square.gameObject.SetActive(false);
		}
        foreach (TextMesh square in squareCB)
        {
            square.text = "";
        }
		Audio.PlaySoundAtTransform("Strike", transform);
		Module.HandleStrike();
		submissionWindow.SetActive(false);
		mainWindow.SetActive(true);
		inSubmission = false;
        int[] colorSolIx = colorEncrypted.Select(x => "01234".IndexOf(x)).ToArray();
        string colorSeq = "";
		string submittedSeq = "";
        for (int i = 0; i < colorSolIx.Length; i++)
        {

            colorSeq += "GRBYJ"[colorSolIx[i]];			
        }

        if (submissionString.Length > 0)
        {
			for (int i = 0; i < submissionString.Length; i++)
			{
                int submittedIx;
                int.TryParse(submissionString[i].ToString(), out submittedIx);
                submittedSeq += "GRBYJ"[submittedIx];
            }            
        }
        QuickLog("Expected {0}, but inputted {1}. Strike!", colorSeq, submittedSeq.Length == 0 ? "nothing" : submittedSeq);
        submissionString = string.Empty;
        var ix = 0;
		while (ix != 3)
		{
			statusLightObj.material = colors[1];
			yield return new WaitForSeconds(0.33f);
			statusLightObj.material = statusLightUnsolvedColor;
			yield return new WaitForSeconds(0.33f);
			ix++;
		}
	}


	void subMode()
	{
		inSubmission = true;
		mainWindow.SetActive(false);
		submissionWindow.SetActive(true);
        for (int i = 0; i < 5; i++)
        {
            buttonCB[i].text = cbActive ? colorNames[i].ToUpperInvariant() : "";
        }
    }

	void resetPress()
	{
		Audio.PlaySoundAtTransform("Click", transform);
		reset.AddInteractionPunch(0.4f);

		if (moduleSolved || !inSubmission)
		{
			return;
		}

		inSubmission = false;
		submissionWindow.SetActive(false);
		mainWindow.SetActive(true);
	}
	
	
	void FixedUpdate()
    {
		step += increment;
		statusLight.transform.localRotation = Quaternion.Euler((step * Mathf.PI) / 2f, (step * Mathf.PI) / 4f, (step * Mathf.PI) / 6f);
    }

	// Twitch Plays

	void updateCBTP()
	{
		cbActive = !cbActive;

		for (int i = 0; i < colorCBIx.Count; i++)
		{
			squareCB[i].text = cbActive ? colorNames[colorCBIx[i]][0].ToString() : "";
		}

		for (int i = 0; i < 5; i++)
		{
			buttonCB[i].text = cbActive ? colorNames[i].ToUpperInvariant() : "";
		}
	}


#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} CB to toggle colorblind mode. || !{0} submit to go into submission mode or submit your answer if it's already in submission mode. || !{0} input grbyj to input your submission. || !{0} clear 1234567890/all to clear the number of squares or clear all of the squares.";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string command)
    {
		yield return null;

		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (!isActive)
		{
			yield return "sendtochaterror You cannot interact with the module yet!";
			yield break;
		}

		if (split[0].EqualsIgnoreCase("CB"))
		{
			updateCBTP();
			yield break;
		}

		if (split[0].EqualsIgnoreCase("L") || split[0].EqualsIgnoreCase("LEFT") || split[0].EqualsIgnoreCase("R") || split[0].EqualsIgnoreCase("RIGHT"))
		{
			if (inSubmission)
			{
				yield return "sendtochaterror You cannot go to any page while in submission mode!";
				yield break;
			}

			switch (split[0])
			{
				case "L":
				case "LEFT":
					mainButtons[0].OnInteract();
					yield return new WaitForSeconds(0.1f);
					break;
				case "R":
				case "RIGHT":
					mainButtons[2].OnInteract();
					yield return new WaitForSeconds(0.1f);
					break;
			}
			yield break;
		}

		if (split[0].EqualsIgnoreCase("SUBMIT"))
		{
			if (!inSubmission)
			{
				mainButtons[1].OnInteract();
				yield return new WaitForSeconds(0.1f);
				yield break;
			}
			else
			{
				submit.OnInteract();
				yield return new WaitForSeconds(0.1f);
				yield break;
			}
		}

		if (split[0].EqualsIgnoreCase("INPUT"))
		{
			if (!inSubmission)
			{
				yield return "sendtochaterror You cannot input anything when it's not in submission mode!";
				yield break;
			}
			else if (split.Length == 1)
			{
				yield return "sendtochaterror Please input your colors!";
				yield break;
			}
			else if (split[1].Any(x => !"GRBYJ".Contains(x)))
			{
				var filter = split[1].Where(x => !"GRBYJ".Contains(x)).ToArray();
				var statement = filter.Length > 1 ? "aren't actual color names" : "isn't an actual color name";
				yield return $"sendtochaterror {filter.Join(", ")} {statement}!";
				yield break;
			}
			else if (split[1].Length > 24 - submissionString.Length || submissionString.Length == 24)
			{
				int length = 24 - submissionString.Length;
				var statement = submissionString.Length == 24 ? "You cannot input anymore squares. Clear some before reinputting!" : $"Your input is more than {length} squares already inputted!";
				yield return $"sendtochaterror {statement}";
				yield break;
			}

			var colorsToPress = split[1].Select(x => "GRBYJ".IndexOf(x)).ToList();

			for (int i = 0; i < colorsToPress.Count; i++)
			{
				colorButtons[colorsToPress[i]].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			yield break;
		}

		if (split[0].EqualsIgnoreCase("CLEAR"))
		{
			if (split.Length == 1)
			{
				yield return "sendtochaterror Please specify the number of squares to clear, or clear all to reset your input!";
				yield break;
			}
			else if (split[1].EqualsIgnoreCase("ALL"))
			{
				while (submissionString.Length != 0)
				{
					back.OnInteract();
					yield return new WaitForSeconds(0.1f);
				}
				yield break;
			}
			else if (split[1].Any(x => !"1234567890".Contains(x)))
			{
				var invalids = split[1].Where(x => !"1234567890".Contains(x)).ToArray();
				var statement = invalids.Length > 1 ? "aren't actual numbers" : "isn't an actual number";
				yield return $"sendtochaterror {invalids.Join(", ")} {statement}!";
				yield break;
			}
			int numberOfClears;
			int.TryParse(split[1], out numberOfClears);

			if (numberOfClears > 24 - submissionString.Length || numberOfClears > 24 || numberOfClears == 0)
			{
				yield break;
			}

			for (int i = numberOfClears - 1; i >= 0; i--)
			{
				back.OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			yield break;
		}
		
		if (split[0].EqualsIgnoreCase("BACK"))
		{
			if (!inSubmission)
			{
				yield return "sendtochaterror You cannot go back since you've already returned to the main window!";
				yield break;
			}
			reset.OnInteract();
			yield return new WaitForSeconds(0.1f);
			yield break;
		}

    }

	IEnumerator TwitchHandleForcedSolve()
    {
		yield return null;

		while (!isActive)
		{
			yield return true;
		}

		if (!inSubmission)
		{
			mainButtons[1].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}

		if (inSubmission)
		{
			while (!colorEncrypted.StartsWith(submissionString))
			{
				back.OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
		}

		int start = inSubmission ? submissionString.Length : 0;
		var solutionColors = colorEncrypted.Select(x => "01234".IndexOf(x)).ToArray();

		for (int i = start; i < colorEncrypted.Length; i++)
		{
			colorButtons[solutionColors[i]].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		submit.OnInteract();

    }


}





