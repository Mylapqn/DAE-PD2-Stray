using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CatInteraction : MonoBehaviour
{
	private PlayerInputHandler input;
	private CatMovement catMovement;
	public RectTransform interactPrompt;
	TextMeshProUGUI interactText;
	public float interactRange = 1f;
	void Awake()
	{
		input = GetComponent<PlayerInputHandler>();
		catMovement = GetComponent<CatMovement>();
	}
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		interactText = interactPrompt.GetComponentInChildren<TextMeshProUGUI>();
	}

	// Update is called once per frame
	void Update()
	{
		bool showPrompt = false;
		string interactName = "Interact";
		if (!catMovement.IsJumping)
		{
			Collider[] selectedColliders = Physics.OverlapSphere(transform.position, interactRange, LayerMask.GetMask("Interactable"), QueryTriggerInteraction.Collide)
				.OrderBy(c => Vector3.SqrMagnitude(c.transform.position - transform.position)).ToArray();
			foreach (Collider collider in selectedColliders)
			{
				if (collider != null && collider.TryGetComponent(out Interactable selectedInteractable))
				{
					if (selectedInteractable.AutoTrigger) continue;
					Vector2 screenPos = Camera.main.WorldToScreenPoint(selectedInteractable.transform.position);
					interactPrompt.position = screenPos;
					showPrompt = true;
					interactName = selectedInteractable.InteractionName;
					if (input.Interact.Pressed)
					{
						selectedInteractable.Interact();
					}
				}
			}
		}
		interactPrompt.gameObject.SetActive(showPrompt);
		if (showPrompt)
		{
			interactText.text = "[E] " + interactName;
		}
		//if fell out of world
		if (transform.position.y < -10)
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, interactRange);
	}
}
