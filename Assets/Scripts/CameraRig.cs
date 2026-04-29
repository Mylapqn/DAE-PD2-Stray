using UnityEngine;

public class CameraRig : MonoBehaviour
{
	[SerializeField] PlayerInputHandler playerInput;
	[SerializeField] Transform targetTransform;
	[SerializeField] float sensitivity = 0.3f;
	[SerializeField] float rotationSpeed = 4f;
	[SerializeField] float minPitch = -30f;
	[SerializeField] float maxPitch = 60f;
	[SerializeField] float maxCameraDistance = 2f;
	[SerializeField] float cameraHeight = 0.5f;
	[SerializeField] float cameraZoomInSpeed = 10f;
	[SerializeField] float cameraZoomOutSpeed = 3f;
	[SerializeField] float positionSmoothing = 0.1f;
	public Transform cameraOrbitCenter;
	public Camera cam;
	float _pitch;
	float _yaw;
	float _targetPitch;
	float _targetYaw;
	float _cameraDistance;
	float _targetCameraDistance;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		HandleRotation();
		cameraOrbitCenter.localPosition = Vector3.up * cameraHeight;
		HandleCameraDistance();
	}
	private void LateUpdate()
	{
		HandlePosition();
	}

	void HandlePosition()
	{
		Vector3 targetPosition = targetTransform.position;
		transform.position = targetPosition;
		//transform.position = Vector3.Lerp(transform.position, targetPosition, 1 - positionSmoothing * Time.deltaTime);
	}

	void HandleRotation()
	{
		Vector2 lookInput = playerInput.LookInput * sensitivity;
		_targetYaw += lookInput.x;
		_targetPitch -= lookInput.y;
		_targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
		_pitch = Mathf.Lerp(_pitch, _targetPitch, rotationSpeed * Time.deltaTime);
		_yaw = Mathf.LerpAngle(_yaw, _targetYaw, rotationSpeed * Time.deltaTime);
		cameraOrbitCenter.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
	}
	void HandleCameraDistance()
	{
		if (Physics.Raycast(cameraOrbitCenter.position, -cameraOrbitCenter.forward, out RaycastHit hitInfo, maxCameraDistance))
		{
			float newDistance = hitInfo.distance - 0.2f;
			newDistance = Mathf.Clamp(newDistance, 0.5f, maxCameraDistance);
			_targetCameraDistance = newDistance;
		}
		else
		{
			_targetCameraDistance = maxCameraDistance;
		}
		if (_cameraDistance > _targetCameraDistance)
		{
			_cameraDistance = Mathf.Lerp(_cameraDistance, _targetCameraDistance, Time.deltaTime * cameraZoomInSpeed);
		}
		else
		{
			_cameraDistance = Mathf.Lerp(_cameraDistance, _targetCameraDistance, Time.deltaTime * cameraZoomOutSpeed);
		}
		cam.transform.localPosition = new Vector3(0f, 0f, -_cameraDistance);
	}
}
