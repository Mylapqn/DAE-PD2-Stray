using System;
using UnityEngine;

public class Interactable : MonoBehaviour
{
	public event Action OnInteract;
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
	}
}
