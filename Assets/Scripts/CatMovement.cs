using UnityEngine;
using static UnityEditor.PlayerSettings;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(CharacterController))]
public class CatMovement : MonoBehaviour
{
	[Header("Movement")]
	[SerializeField] float moveSpeed = 6f;
	[SerializeField] float acceleration = 12f;
	[SerializeField] float deceleration = 16f;

	[Header("Rotation")]
	[SerializeField] float rotationSpeed = 12f;

	[Header("Jump & Gravity")]
	[SerializeField] float jumpHeight = 1.5f;
	[SerializeField] float gravity = -20f;
	[SerializeField] float groundedGravity = -2f; // small constant to keep grounded reliable
	[SerializeField] Transform jumpPrompt;
	[SerializeField] float minJumpHeight = 0.5f;
	[SerializeField] float maxJumpDistance = 4f;

	[Header("Camera")]
	[SerializeField] Transform cameraTransform; // drag main camera here

	// Private

	PlayerInputHandler input;
	CharacterController controller;

	Vector3 velocity;
	float velocityMultiplier = 1;
	float verticalSpeed;
	bool _jumpAvailable = false;
	Vector3 _jumpTarget = Vector3.zero;
	bool _isJumping = false;
	float jumpTime = 0f;




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
		CollisionFlags collision = controller.Move((velocity * velocityMultiplier + Vector3.up * verticalSpeed) * Time.deltaTime);
		if (collision.HasFlag(CollisionFlags.Sides))
		{
			//velocity = Vector3.zero;
		}
	}

	// Movement

	void HandleMovement()
	{
		if (!_isJumping)
		{
			Vector2 raw = input.MoveInput;

			Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
			Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

			Vector3 targetDirection = (camForward * raw.y + camRight * raw.x);

			Vector3 targetVelocity = targetDirection * moveSpeed;



			float rate = targetVelocity.magnitude > 0.01f ? acceleration : deceleration;
			velocity = Vector3.MoveTowards(velocity, targetVelocity, rate * Time.deltaTime);

			Vector3 predictedPosition = transform.position + transform.forward * 0.2f;
			// If there's no platform surface below and in front of the player, stop movement to prevent walking off edges
			if (!Physics.Raycast(predictedPosition + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.4f, LayerMask.GetMask("Platform")))
			{
				velocityMultiplier = 0;
			}
			else
			{
				velocityMultiplier = 1;
			}
		}
		else
		{
			velocityMultiplier = 1;
		}
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
		if (input.Jump.Held && controller.isGrounded && !_isJumping)
		{
			if (_jumpAvailable)
			{
				// Start jump
				controller.excludeLayers = ~0; // disable collisions
				_isJumping = true;
				_jumpAvailable = false;
				jumpPrompt.gameObject.SetActive(false);
				// Move jump target up to arc over edge
				_jumpTarget.y += 0.2f;
				// Move jump target away from player to ensure reaching platform
				Vector3 diff = _jumpTarget - transform.position;
				diff.y = 0f;
				_jumpTarget += diff.normalized * 0.5f;
			}
		}

		if (_isJumping)
		{
			velocity = _jumpTarget - transform.position;
			velocity *= 3f;
			verticalSpeed = velocity.y;
			velocity.y = 0f;
			jumpTime += Time.deltaTime;
			if (Vector3.SqrMagnitude(_jumpTarget - transform.position) < 0.1f || jumpTime >= 4f)
			{
				// End jump
				controller.excludeLayers = 0; // re-enable collisions
				_isJumping = false;
				jumpTime = 0f;
			}
		}

		// Jump Height formula: v = sqrt(h * -2 * g)
		// verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
	}

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

		Vector3 predictedPosition = transform.position + transform.forward * 0.3f;
		// If there's a platform surface below and in front of the player
		if (Physics.Raycast(predictedPosition + Vector3.down * 0.1f, Vector3.ProjectOnPlane(transform.forward, Vector3.up) * 0.3f + Vector3.down, out RaycastHit hit, 1.3f, LayerMask.GetMask("Platform")))
		{
			// only consider jumping down to flat surfaces
			if (hit.normal.y > 0.85f)
			{
				// penalize juumping down
				bestDist = hit.distance * 2;
				bestPoint = hit.point;

			}
		}
		if (velocityMultiplier != 0)
		{
			foreach (GameObject platform in allPlatformEdges)
			{
				if (platform.transform.position.y < transform.position.y + minJumpHeight) continue; // only platforms above - should remove later for horizontal jumps
				LineRenderer line = platform.GetComponent<LineRenderer>();
				Vector3[] positions = new Vector3[line.positionCount];
				line.GetPositions(positions);
				for (int i = 0; i < positions.Length; i++)
				{

					positions[i] = platform.transform.TransformPoint(positions[i]);
				}
				//find closest line segment to player
				(int index, Vector3 point, float dist) closestSegment = FindNearestLineSegment(positions, predictedPosition);
				float dot = Vector3.Dot((closestSegment.point - transform.position).normalized, transform.forward);
				closestSegment.dist *= dot < 0.1f ? 2f : 1f; // penalize points that aren't mostly in front of the player
				if (closestSegment.dist < bestDist)
				{
					bestDist = closestSegment.dist;
					bestPoint = closestSegment.point;
				}
			}
		}
		// set jump prompt based on best point
		if (bestDist < maxJumpDistance)
		{
			SetJumpPromptAndTarget(bestPoint);
		}
		else
		{
			SetJumpPromptAndTarget();

		}

	}
	void SetJumpPromptAndTarget()
	{
		jumpPrompt.gameObject.SetActive(false);
		_jumpAvailable = false;
	}
	void SetJumpPromptAndTarget(Vector3 position)
	{
		_jumpTarget = position;
		_jumpAvailable = true;
		jumpPrompt.position = position;
		jumpPrompt.gameObject.SetActive(true);
	}

	public static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
	{
		Vector3 ab = b - a;
		float t = Vector3.Dot(p - a, ab) / ab.sqrMagnitude;
		t = Mathf.Clamp01(t);
		return a + ab * t;
	}

	public static (int index, Vector3 point, float dist) FindNearestLineSegment(Vector3[] points, Vector3 position)
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
			float squareDist = (position - closest).sqrMagnitude;

			if (squareDist < bestDist)
			{
				//bestDist = Mathf.Sqrt(squareDist);
				bestDist = squareDist;
				bestIndex = i;
				bestPoint = closest;
			}
		}

		return (bestIndex, bestPoint, bestDist);
	}
}
