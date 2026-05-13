using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Barrel : MonoBehaviour
{
	[SerializeField] LayerMask _playerMask;
	[SerializeField] LayerMask _collisionMask;
	[SerializeField] float _barrelRadius = 2;
	CatMovement _currentCatInside = null;
	public bool _isCatInside = false;
	public Transform BarrelVisual;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if (_isCatInside)
		{
			Vector3 catVelocity = _currentCatInside.Velocity;
			float movementVelocity = Vector3.Dot(catVelocity, transform.right);
			Vector3 movementVector = movementVelocity * transform.right;
			Vector3 movementDirection = movementVector.normalized;
			if (Physics.Raycast(BarrelVisual.transform.position + movementDirection * _barrelRadius, movementDirection, 0.03f, _collisionMask))
			{

			}
			else
			{
				transform.position += movementVector * Time.deltaTime;
				BarrelVisual.Rotate(0, 0, -movementVelocity / _barrelRadius * Time.deltaTime * Mathf.Rad2Deg);
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if ((_playerMask & (1 << other.gameObject.layer)) == 0) return;
		_isCatInside = true;
		_currentCatInside = other.GetComponent<CatMovement>();
		_currentCatInside.IsInBarrel = true;
	}

	private void OnTriggerExit(Collider other)
	{
		if ((_playerMask & (1 << other.gameObject.layer)) == 0) return;
		_isCatInside = false;
		_currentCatInside.IsInBarrel = false;
		_currentCatInside = null;
	}

	private void OnDrawGizmosSelected()
	{
		if (BarrelVisual != null)
			Gizmos.DrawWireSphere(BarrelVisual.transform.position, _barrelRadius);
	}
}
