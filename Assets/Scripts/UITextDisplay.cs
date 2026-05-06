using TMPro;
using UnityEngine;

public class UITextDisplay : MonoBehaviour
{
	public TextMeshProUGUI MainText;
	public float charactersPerSecond;
	public float waitAfterSeconds;
	private string currentText;
	private string wholeText;
	private char[] wholeTextArray;
	private float timeElapsed;
	private float wholeTextTime;
	private float charProgress;
	private int arrayIndex;
	private Vector3 displayPosition;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		Hide();
	}

	// Update is called once per frame
	void Update()
	{
		UpdatePosition();
		timeElapsed += Time.deltaTime;
		charProgress += Time.deltaTime * charactersPerSecond;
		if (timeElapsed > wholeTextTime + waitAfterSeconds)
		{
			Hide();
			return;
		}
		else if (arrayIndex < wholeTextArray.Length)
		{
			while (charProgress > 1 && arrayIndex < wholeTextArray.Length)
			{
				charProgress -= 1;
				currentText += wholeTextArray[arrayIndex];
				arrayIndex++;
			}
		}
		MainText.text = currentText;
	}

	public void DisplayText(string text, Vector3 position)
	{
		gameObject.SetActive(true);
		wholeText = text;
		wholeTextArray = text.ToCharArray();
		wholeTextTime = text.Length / charactersPerSecond;
		timeElapsed = 0;
		currentText = "";
		MainText.text = currentText;
		arrayIndex = 0;
		charProgress = 0;
		displayPosition = position;
		UpdatePosition();
	}
	void UpdatePosition()
	{
		Vector2 screenPos = Camera.main.WorldToScreenPoint(displayPosition);
		screenPos.y += 200;
		transform.position = screenPos;
	}
	public void Hide()
	{
		gameObject.SetActive(false);
	}
}
