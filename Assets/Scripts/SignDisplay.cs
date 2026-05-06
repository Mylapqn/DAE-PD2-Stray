using UnityEngine;

public class SignDisplay : InteractableRecieverBehaviour
{
	public UITextDisplay TextDisplay;
	public string TextToShow;
	protected override void OnInteract()
	{
		base.OnInteract();
		TextDisplay.DisplayText(TextToShow, transform.position);
	}
}