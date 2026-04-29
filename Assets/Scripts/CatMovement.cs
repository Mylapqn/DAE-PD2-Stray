using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
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
	[SerializeField] float jumpArcHeight = 0.5f;

	[Header("Camera")]
	[SerializeField] Transform cameraTransform; // drag main camera here

	// Private

	PlayerInputHandler input;
	CharacterController controller;

	Vector3 velocity;
	Vector3 facingDirection;
	bool stuckAtEdge = false;
	float velocityMultiplier = 1;
	float verticalSpeed;
	bool _jumpAvailable = false;
	Vector3 _jumpTarget = Vector3.zero;
	Vector3 _jumpStart = Vector3.zero;
	bool _isJumping = false;
	float jumpTime = 0f;
	float maxJumpTime = 0f;




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
		Vector3 finalVelocity = (velocity) * velocityMultiplier;
		Vector3 finalMove = (finalVelocity + Vector3.up * verticalSpeed) * Time.deltaTime;
		CollisionFlags collision = controller.Move(finalMove);
		if (collision.HasFlag(CollisionFlags.Sides))
		{
			//velocity = Vector3.zero;
		}
	}

	// Movement

	void HandleMovement()
	{
		stuckAtEdge = false;
		if (!_isJumping)
		{
			Vector2 raw = input.MoveInput;

			Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
			Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

			Vector3 targetDirection = (camForward * raw.y + camRight * raw.x);

			Vector3 targetVelocity = targetDirection * moveSpeed;



			float rate = targetVelocity.magnitude > 0.01f ? acceleration : deceleration;
			velocity = Vector3.MoveTowards(velocity, targetVelocity, rate * Time.deltaTime);
			facingDirection = Vector3.MoveTowards(facingDirection, targetDirection, rate * Time.deltaTime);
			facingDirection.y = 0;

			Vector3 predictedPosition = transform.position + targetVelocity * 0.1f;

			// If there's no platform surface below and in front of the player, stop movement to prevent walking off edges
			if (!Physics.Raycast(predictedPosition + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.4f, LayerMask.GetMask("Platform")))
			{
				//get nearest line segment on current platform
				GameObject[] filteredEdges = GameObject.FindGameObjectsWithTag("PlatformEdge")
					.Where(platform => platform.transform.position.y > transform.position.y - .1f && platform.transform.position.y < transform.position.y + .1f)
					.ToArray();
				(int index, Vector3 point, float dist) closestPlatform = FindNearestLineSegmentFromPlatforms(filteredEdges, transform.position);
				if (closestPlatform.dist < 0.2f)
				{
					Vector3 velDir = velocity.normalized;
					float velMag = velocity.magnitude;
					// remove velocity component towards edge to prevent walking off
					Vector3 toEdge = (closestPlatform.point - transform.position);
					toEdge.y = 0f;
					toEdge.Normalize();
					float dot = Vector3.Dot(toEdge, velDir);
					if (dot > 0)
					{
						velDir -= toEdge * dot * 1.1f;
					}
					velocity = velDir * velMag;
					stuckAtEdge = true;
				}
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
				_jumpTarget.y += 0.1f;
				// Move jump target away from player to ensure reaching platform
				Vector3 horizontalDiff = _jumpTarget - transform.position;
				horizontalDiff.y = 0f;
				_jumpTarget += horizontalDiff.normalized * 0.3f;
				_jumpStart = transform.position;
				maxJumpTime = Vector3.Distance(_jumpStart, _jumpTarget) * 0.2f;
			}
		}

		if (_isJumping)
		{
			jumpTime += Time.deltaTime;
			float jumpProgress = jumpTime / maxJumpTime;
			Vector3 arcOffset = Vector3.up * Mathf.Sin(jumpProgress * Mathf.PI) * jumpArcHeight;
			Vector3 currentTarget = Vector3.Lerp(_jumpStart, _jumpTarget, jumpProgress) + arcOffset;
			velocity = currentTarget - transform.position;
			velocity *= 10;
			verticalSpeed = velocity.y;
			velocity.y = 0f;
			facingDirection = velocity.normalized;
			if (jumpProgress >= 1)
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
		if (facingDirection.magnitude < 0.01f) return;
		Vector3 finalDirection = facingDirection.normalized;
		if (_isJumping) finalDirection += Vector3.up * verticalSpeed;

		Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
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
		//if player is not at platform edge
		if (!stuckAtEdge)
		{
			//go through all platform edges above, available for jump
			GameObject[] filteredEdges = allPlatformEdges.Where(platform => platform.transform.position.y > transform.position.y + minJumpHeight).ToArray();
			(int index, Vector3 point, float dist) closestPlatform = FindNearestLineSegmentFromPlatforms(filteredEdges, predictedPosition);
			if (closestPlatform.dist < bestDist)
			{
				bestDist = closestPlatform.dist;
				bestPoint = closestPlatform.point;
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

	public (int index, Vector3 point, float dist) FindNearestLineSegmentFromPlatforms(GameObject[] platformEdges, Vector3 comparisonPosition)
	{
		float bestDist = float.MaxValue;
		int bestIndex = -1;
		Vector3 bestPoint = Vector3.zero;
		foreach (GameObject platform in platformEdges)
		{
			LineRenderer line = platform.GetComponent<LineRenderer>();
			Vector3[] linePositions = new Vector3[line.positionCount];
			line.GetPositions(linePositions);
			for (int i = 0; i < linePositions.Length; i++)
			{

				linePositions[i] = platform.transform.TransformPoint(linePositions[i]);
			}
			//find closest line segment to player
			(int index, Vector3 point, float dist) closestSegment = FindNearestLineSegment(linePositions, comparisonPosition);
			float dot = Vector3.Dot((closestSegment.point - transform.position).normalized, transform.forward);
			closestSegment.dist *= dot < 0.1f ? 2f : 1f; // penalize points that aren't mostly in front of the player
			if (closestSegment.dist < bestDist)
			{
				if (Physics.Linecast(closestSegment.point + Vector3.up * 0.2f, comparisonPosition + Vector3.up * 0.1f, out RaycastHit hit, LayerMask.GetMask("Platform")))
				{
					// if there's a platform in the way, ignore this jump point
				}
				else
				{
					bestDist = closestSegment.dist;
					bestPoint = closestSegment.point;
					bestIndex = closestSegment.index;
				}
			}
		}
		return (bestIndex, bestPoint, bestDist);
	}
}
