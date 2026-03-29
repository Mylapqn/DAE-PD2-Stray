using UnityEngine;
using static UnityEditor.PlayerSettings;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(CharacterController))]
public class CatMovement : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] float moveSpeed = 6f;
	[SerializeField] float acceleration = 12f;  // how fast we reach moveSpeed
	[SerializeField] float deceleration = 16f;  // how fast we stop

	[Header("Rotation")]
	[SerializeField] float rotationSpeed = 12f;  // higher = snappier turn

	[Header("Jump & Gravity")]
	[SerializeField] float jumpHeight = 1.5f;
	[SerializeField] float gravity = -20f; // tunable, not tied to Physics settings
	[SerializeField] float groundedGravity = -2f; // small constant to keep grounded reliable
	[SerializeField] Transform jumpPrompt;

	[Header("Camera")]
	[SerializeField] Transform cameraTransform; // drag main camera here

	// Private

	PlayerInputHandler input;
	CharacterController controller;

	Vector3 velocity;        // current XZ velocity (world space, smoothed)
	float verticalSpeed;   // Y component handled separately
	bool _jumpAvailable = false;
	Vector3 _jumpPoint = Vector3.zero;
	bool _isJumping = false;


	void Awake()
	{
		input = GetComponent<PlayerInputHandler>();
		controller = GetComponent<CharacterController>();
	}
	void Update()
	{
		HandleGravity();
		HandleMovement();
		ProcessPlatformEdges();
		HandleRotation();
		HandleJump();

		// Single Move call per frame — combines XZ + Y
		CollisionFlags collision = controller.Move((velocity + Vector3.up * verticalSpeed) * Time.deltaTime);
		if (collision.HasFlag(CollisionFlags.Sides))
		{
			//velocity = Vector3.zero;
		}
	}

	// Movement

	void HandleMovement()
	{
		// Raw input from handler
		Vector2 raw = input.MoveInput;

		// Build a world-space direction relative to camera
		// Flatten camera forward so we don't move up/down based on camera pitch
		Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
		Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

		Vector3 targetDirection = (camForward * raw.y + camRight * raw.x);

		// Target velocity this frame
		Vector3 targetVelocity = targetDirection * moveSpeed;

		// Smoothly accelerate toward target or decelerate to zero
		float rate = targetVelocity.magnitude > 0.01f ? acceleration : deceleration;
		velocity = Vector3.MoveTowards(velocity, targetVelocity, rate * Time.deltaTime);
	}

	// Gravity

	void HandleGravity()
	{
		if (controller.isGrounded)
		{
			// Small constant gravity keeps isGrounded reliable
			// Without this, isGrounded flickers on flat ground
			verticalSpeed = groundedGravity;
		}
		else
		{
			verticalSpeed += gravity * Time.deltaTime;
		}
	}

	void HandleJump()
	{
		if (input.Jump.Pressed && controller.isGrounded && !_isJumping)
		{
			if (_jumpAvailable)
			{
				_isJumping = true;
				_jumpAvailable = false;
				jumpPrompt.gameObject.SetActive(false);
				_jumpPoint.y += 0.2f;
				Vector3 diff = _jumpPoint - transform.position;
				diff.y = 0f;
				_jumpPoint += diff.normalized * 0.5f;
			}
		}

		if (_isJumping)
		{
			velocity = _jumpPoint - transform.position;
			velocity *= 3f;
			verticalSpeed = velocity.y;
			velocity.y = 0f;
			if (Vector3.SqrMagnitude(_jumpPoint - transform.position) < 0.1f)
			{
				_isJumping = false;
			}
		}

		// Jump Height formula: v = sqrt(h * -2 * g)
		//verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
	}

	// Rotation

	void HandleRotation()
	{
		// Only rotate when actually moving
		if (velocity.magnitude < 0.1f) return;

		Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			targetRotation,
			rotationSpeed * Time.deltaTime
		);
	}

	void ProcessPlatformEdges()
	{
		if (_isJumping) return;
		GameObject[] allPlatformEdges = GameObject.FindGameObjectsWithTag("PlatformEdge");
		float bestDist = float.MaxValue;
		Vector3 bestPoint = Vector3.zero;
		foreach (GameObject platform in allPlatformEdges)
		{
			if (platform.transform.position.y < transform.position.y + 0.5f) continue; // only consider platforms above us
			LineRenderer line = platform.GetComponent<LineRenderer>();
			Vector3[] positions = new Vector3[line.positionCount];
			line.GetPositions(positions);
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] += platform.transform.position;
			}
			//find closest line segment to player
			(int index, Vector3 point, float dist) closestIndex = FindNearestSegmentIndex(positions, transform.position);
			if (closestIndex.dist < bestDist)
			{
				bestDist = closestIndex.dist;
				bestPoint = closestIndex.point;
			}
		}
		if (bestDist < 3f)
		{
			jumpPrompt.gameObject.SetActive(true);
			jumpPrompt.position = bestPoint;
			_jumpAvailable = true;
			_jumpPoint = bestPoint;
		}
		else
		{
			jumpPrompt.gameObject.SetActive(false);
			_jumpAvailable = false;
		}
	}
	public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
	{
		Vector3 ab = b - a;
		float t = Vector3.Dot(p - a, ab) / ab.sqrMagnitude;
		t = Mathf.Clamp01(t);
		return a + ab * t;
	}

	public static (int index, Vector3 point, float dist) FindNearestSegmentIndex(Vector3[] points, Vector3 position)
	{
		float bestDist = float.MaxValue;
		int bestIndex = -1;
		Vector3 bestPoint = Vector3.zero;

		for (int i = 0; i < points.Length; i++)
		{
			Vector3 a = points[i];
			int nextIndex = (i + 1) % points.Length;
			Vector3 b = points[nextIndex];

			Vector3 closest = ClosestPointOnSegment(a, b, position);
			float dist = (position - closest).sqrMagnitude;

			if (dist < bestDist)
			{
				bestDist = dist;
				bestIndex = i;
				bestPoint = closest;
			}
		}

		return (bestIndex, bestPoint, bestDist);
	}
}
