using System;
using UnityEngine;

public class Interactable : MonoBehaviour
{
	public event Action OnInteract;
	public string InteractionName;
	public bool OnlyOnce = false;
	public bool AutoTrigger = false;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}
	public void Interact()
	{
		Debug.Log("Interacted with " + gameObject.name);
		OnInteract?.Invoke();
		if (OnlyOnce) gameObject.SetActive(false);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!AutoTrigger) return;
		if (other.CompareTag("Player"))
		{
			Interact();
		}
	}
}
