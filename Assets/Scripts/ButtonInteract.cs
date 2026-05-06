using UnityEngine;

public class ButtonInteract : InteractableRecieverBehaviour
{
	public Transform doorHinge;
	float doorRotation;
	float targetRotation;
	public float speed = 40f;
	public bool IsOpen = false;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		targetRotation = IsOpen ? 90 : 0;
		if (doorRotation != targetRotation)
		{
			doorRotation = Mathf.MoveTowards(doorRotation, targetRotation, Time.deltaTime * speed);
			doorHinge.rotation = Quaternion.Euler(0, doorRotation, 0);
		}
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		IsOpen = !IsOpen;
	}
}
