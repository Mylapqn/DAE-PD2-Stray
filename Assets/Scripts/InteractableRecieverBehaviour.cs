using UnityEngine;

public class InteractableRecieverBehaviour : MonoBehaviour
{
	public Interactable LinkedInteractable;
	private void Awake()
	{
		if (LinkedInteractable == null)
		{
			LinkedInteractable = GetComponent<Interactable>();
		}
		if (LinkedInteractable != null)
		{
			LinkedInteractable.OnInteract += OnInteract;
		}
	}
	protected virtual void OnInteract()
	{

	}
}
