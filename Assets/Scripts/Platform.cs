using UnityEngine;

public class Platform : MonoBehaviour
{
	public LineRenderer lineRenderer;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		if (lineRenderer == null) lineRenderer = GetComponentInChildren<LineRenderer>();
	}

	// Update is called once per frame
	void Update()
	{

	}
}
