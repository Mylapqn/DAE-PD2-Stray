using System.Linq;
using UnityEngine;

public class CatInteraction : MonoBehaviour
{
	private PlayerInputHandler input;
	private CatMovement catMovement;
	public RectTransform interactPrompt;
	public float interactRange = 1f;	
	void Awake()
	{
		input = GetComponent<PlayerInputHandler>();
		catMovement = GetComponent<CatMovement>();
	}
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		bool showPrompt = false;
		if (!catMovement.IsJumping)
		{
			Collider selectedCollider = Physics.OverlapSphere(transform.position, interactRange, LayerMask.GetMask("Interactable"), QueryTriggerInteraction.Collide)
				.OrderBy(c => Vector3.SqrMagnitude(c.transform.position - transform.position))
				.FirstOrDefault();
			if (selectedCollider != null && selectedCollider.TryGetComponent(out Interactable selectedInteractable))
			{
				Vector2 screenPos = Camera.main.WorldToScreenPoint(selectedInteractable.transform.position);
				interactPrompt.position = screenPos;
				showPrompt = true;
				if (input.Interact.Pressed)
				{
					selectedInteractable.Interact();
				}

			}
		}
		interactPrompt.gameObject.SetActive(showPrompt);
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, interactRange);
	}
}
