using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UIElements;
using static LineSegmentMath;
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
	[SerializeField] float maxJumpHeight = 1.5f;
	[SerializeField] float gravity = -20f;
	[SerializeField] float groundedGravity = -2f; // small constant to keep grounded reliable
	[SerializeField] Transform jumpPrompt;
	[SerializeField] float minJumpHeight = 0.5f;
	[SerializeField] float maxJumpDistance = 4f;
	[SerializeField] float maxJumpArcHeight = 0.5f;
	[SerializeField] LayerMask _platformLayerMask;

	[Header("Camera")]
	[SerializeField] Transform cameraTransform;

	//Public
	public bool IsJumping => _isJumping;
	public Vector3 Velocity => velocity;

	// Private

	PlayerInputHandler input;
	CharacterController controller;

	Vector3 velocity;
	Vector3 facingDirection;
	bool standingAtEdge = false;
	float verticalSpeed;
	bool _jumpAvailable = false;
	Vector3 _jumpTarget = Vector3.zero;
	Vector3 _jumpStart = Vector3.zero;
	bool _isJumping = false;
	float jumpTime = 0f;
	float maxJumpTime = 0f;
	float _currentJumpArcHeight = 0f;
	public bool IsInBarrel = false;

	Vector3 _raycastYOffset = Vector3.up * 0.1f; // to prevent raycast hitting ground




	void Awake()
	{
		input = GetComponent<PlayerInputHandler>();
		controller = GetComponent<CharacterController>();
	}
	void Update()
	{
		HandleGravity();
		HandleMovement();
		FindJumpTargets();
		HandleRotation();
		HandleJump();

		// Single Move call per frame — combines XZ + Y
		Vector3 finalVelocity = velocity;
		Vector3 finalMove = (finalVelocity + Vector3.up * verticalSpeed) * Time.deltaTime;
		CollisionFlags collision = controller.Move(finalMove);
	}

	// Movement

	void HandleMovement()
	{
		standingAtEdge = false;
		if (!_isJumping)
		{
			Vector2 raw = input.MoveInput;

			Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
			Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

			Vector3 targetDirection = (camForward * raw.y + camRight * raw.x);

			Vector3 targetVelocity = targetDirection * moveSpeed;
			if (IsInBarrel) targetVelocity *= 0.3f;



			float rate = targetVelocity.magnitude > 0.01f ? acceleration : deceleration;
			velocity = Vector3.MoveTowards(velocity, targetVelocity, rate * Time.deltaTime);
			if (targetVelocity.magnitude > 0.01f)
				facingDirection = Vector3.MoveTowards(facingDirection, targetDirection, rate * Time.deltaTime);
			facingDirection.y = 0;

			Vector3 predictedPosition = transform.position + targetVelocity * 0.1f;

			TestCurrentPlatformEdge(predictedPosition);

		}
	}

	bool TestCurrentPlatformEdge(Vector3 predictedPosition)
	{
		// If there's no platform surface below and in front of the player, stop movement to prevent walking off edges
		if (!Physics.Raycast(predictedPosition + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.2f, _platformLayerMask))
		{
			//get nearest line segment on the same height (current platform)
			GameObject[] edgesOnSameLevel = GameObject.FindGameObjectsWithTag("PlatformEdge")
				.Where(platform => platform.transform.position.y > transform.position.y - .1f && platform.transform.position.y < transform.position.y + .1f)
				.ToArray();
			LinePoint closestPlatform = FindNearestPointOnPlatformEdges(edgesOnSameLevel, transform.position, requireLineOfSight: false, requireGap: false);
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
				standingAtEdge = true;
				return true;
			}
		}
		return false;
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
				// Move jump target up to arc over edge
				_jumpTarget.y += 0.01f;
				// Move jump target away from player to ensure reaching platform
				Vector3 horizontalDiff = _jumpTarget - transform.position;
				horizontalDiff.y = 0f;
				horizontalDiff += horizontalDiff.normalized * 0.1f;
				_jumpTarget += horizontalDiff.normalized * 0.1f;
				_jumpStart = transform.position;
				float jumpDistance = Vector3.Distance(_jumpStart, _jumpTarget);
				float jumpDistanceRatio = jumpDistance / maxJumpDistance;
				_currentJumpArcHeight = Mathf.Lerp(maxJumpArcHeight, 0f, jumpDistanceRatio);
				_currentJumpArcHeight = 0;
				float verticalDiff = (_jumpTarget - _jumpStart).y;

				if (verticalDiff > 0)
				{
					// jump up to reach the target height plus arc height
					verticalSpeed = Mathf.Sqrt((verticalDiff + _currentJumpArcHeight + 0.1f) * -2f * gravity);
				}
				else
				{
					// jump up only to reach arc height, then fall down to target height
					verticalSpeed = Mathf.Sqrt(_currentJumpArcHeight * -2f * gravity);
				}
				// calculate time to reach the target horizontally
				float horizontalTime = horizontalDiff.magnitude / moveSpeed / 2f;

				// calculate time to reach the target height using quadratic formula (formula: t = (-v0 - sqrt(v0^2 + 2 * g * h)) / g)
				float discriminant = verticalSpeed * verticalSpeed + 2f * gravity * verticalDiff;

				if (discriminant < 0f)
				{
					maxJumpTime = 0f;
					Debug.LogWarning("Unreachable jump height: " + verticalDiff);
					verticalSpeed = 0f;
				}
				else
				{
					maxJumpTime = (-verticalSpeed - Mathf.Sqrt(discriminant)) / gravity;
					if (maxJumpTime < horizontalTime)
					{
						maxJumpTime = horizontalTime;
						// recalculate vertical speed to ensure player doesn't fall before reaching the target horizontally (formula: v0 = (h - 0.5 * g * t^2) / t)
						verticalSpeed = (verticalDiff - 0.5f * gravity * maxJumpTime * maxJumpTime + 0.1f) / maxJumpTime;
					}
					Debug.Log($"Jumping with initial vertical speed {verticalSpeed} and max jump time {maxJumpTime}");

					// start jump
					velocity = horizontalDiff / maxJumpTime;
					controller.excludeLayers = ~0; // disable collisions
												   //controller.enabled = false; // disable character controller to allow manual movement
					_isJumping = true;
					_jumpAvailable = false;
					jumpPrompt.gameObject.SetActive(false);
				}

			}
		}

		if (_isJumping)
		{
			jumpTime += Time.deltaTime;
			float jumpProgress = jumpTime / maxJumpTime;
			Vector3 arcOffset = Vector3.up * Mathf.Sin(jumpProgress * Mathf.PI) * _currentJumpArcHeight;
			Vector3 currentTarget = Vector3.Lerp(_jumpStart, _jumpTarget, jumpProgress) + arcOffset;
			//transform.position = currentTarget;
			/*velocity = currentTarget - transform.position;
			velocity /= Time.deltaTime;
			verticalSpeed = velocity.y;
			velocity.y = 0f;*/
			facingDirection = (velocity + verticalSpeed * Vector3.up).normalized;
			if (jumpProgress >= 1)
			{
				// End jump
				controller.excludeLayers = 0; // re-enable collisions
											  //controller.enabled = true;
				_isJumping = false;
				jumpTime = 0f;
				velocity *= 0.1f;
				verticalSpeed = 0f;
				Debug.Log("Landed from jump");
				facingDirection.y = 0;
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

		Quaternion targetRotation = Quaternion.LookRotation(finalDirection);
		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			targetRotation,
			rotationSpeed * Time.deltaTime
		);
	}

	void FindJumpTargets()
	{
		if (_isJumping) return;

		float maxDetectionDistance = 5f;
		GameObject[] nearbyPlatformEdges = GameObject.FindGameObjectsWithTag("PlatformEdge")
			.Where(platform => Vector3.SqrMagnitude(platform.transform.position - transform.position) < (maxDetectionDistance * maxDetectionDistance))
			.ToArray();
		float bestDist = float.MaxValue;
		Vector3 bestPoint = Vector3.zero;

		// use predicted position to reward jumps towards the front
		Vector3 predictedPosition = transform.position + transform.forward * 0.25f;

		//If there's free space in front of the player, consider jumping down
		if (!Physics.Raycast(transform.position + _raycastYOffset, predictedPosition + _raycastYOffset, 0.4f, _platformLayerMask))
		{
			// If there's a platform surface below and in front of the player
			if (Physics.Raycast(predictedPosition + Vector3.up * 0.1f, Vector3.ProjectOnPlane(transform.forward, Vector3.up) * 0.3f + Vector3.down, out RaycastHit hit, 2f, _platformLayerMask))
			{
				// ignore very short drops, and only consider jumping down to flat surfaces
				if (hit.distance > 0.2f && hit.normal.y > 0.85f)
				{
					// penalize jumping down
					bestDist = hit.distance * 1.4f;
					bestPoint = hit.point;

				}
			}
		}
		//if player is not at platform edge, consider jumping up
		if (!standingAtEdge)
		{
			//go through all platform edges above, available for jump
			GameObject[] platformEdgesAbove = nearbyPlatformEdges.Where(platform => platform.transform.position.y > transform.position.y + minJumpHeight).ToArray();
			LinePoint closestPlatform = FindNearestPointOnPlatformEdges(platformEdgesAbove, predictedPosition, requireLineOfSight: true, requireGap: false);
			if (closestPlatform.dist < bestDist)
			{
				bestDist = closestPlatform.dist;
				bestPoint = closestPlatform.point;
			}
		}
		// consider jumping across gaps on same level
		{
			// go through all platform edges at the same height, available for jump
			GameObject[] platformEdgesSameLevel = nearbyPlatformEdges.Where(platform => Mathf.Abs(platform.transform.position.y - transform.position.y) < minJumpHeight).ToArray();
			LinePoint closestPlatform = FindNearestPointOnPlatformEdges(platformEdgesSameLevel, transform.position, requireLineOfSight: false, requireGap: true);
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
		// clear jump target
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

	// finds nearest point on given platforms satisfying jump conditions
	LinePoint FindNearestPointOnPlatformEdges(GameObject[] platformEdges, Vector3 comparisonPosition, bool requireLineOfSight, bool requireGap)
	{
		float bestDist = float.MaxValue;
		int bestIndex = -1;
		Vector3 bestPoint = Vector3.zero;
		foreach (GameObject platform in platformEdges)
		{
			LineRenderer line = platform.GetComponent<LineRenderer>();
			if (line == null) continue;
			LinePoint closestSegment = line.NearestLineRendererSegment(comparisonPosition);

			// penalize points that aren't in front of the player
			float dot = Vector3.Dot((closestSegment.point - transform.position).normalized, transform.forward);
			float dotPenalty = 1f - Mathf.Clamp01(dot * 0.5f + 0.5f);
			// strengthen penalty using power so that points behind the player are much less likely to be chosen
			dotPenalty = Mathf.Pow(dotPenalty, 2f) * 2f;
			closestSegment.dist *= 1f + dotPenalty;

			if (closestSegment.dist < bestDist)
			{
				if (requireLineOfSight)
				{
					// if there's a platform in the way, ignore this jump point
					if (Physics.Linecast(transform.position + _raycastYOffset, closestSegment.point + _raycastYOffset, out RaycastHit hit, _platformLayerMask))
					{
						continue;
					}
				}
				if (requireGap)
				{
					// if there is no gap inbetween, ignore this jump point
					// bias the midpoint towards the target because gaps are more likely to be just before the edge of the platform
					Vector3 midPoint = Vector3.Lerp(transform.position, closestSegment.point, 0.8f);
					if (Physics.Raycast(midPoint + Vector3.up * maxJumpArcHeight, Vector3.down, out RaycastHit hit2, maxJumpArcHeight + 0.4f, _platformLayerMask))
					{
						continue;
					}
				}
				bestDist = closestSegment.dist;
				bestPoint = closestSegment.point;
				bestIndex = closestSegment.index;
			}
		}
		return new LinePoint(bestIndex, bestPoint, bestDist);
	}
}
