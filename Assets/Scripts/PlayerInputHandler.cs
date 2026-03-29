using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputHandler : MonoBehaviour, Controls.IPlayerActions
{
	public ButtonState Jump;
	public ButtonState Interact;
	public Vector2 MoveInput;
	public Vector2 LookInput;
	Controls controls;

	void Awake()
	{
		controls = new Controls();
		controls.Player.SetCallbacks(this);
	}

	void OnEnable() => controls.Player.Enable();
	void OnDisable() => controls.Player.Disable();

	void LateUpdate()
	{
		Jump.Flush();
		Interact.Flush();
	}
	public void OnInteract(InputAction.CallbackContext context)
	{
		Interact.Update(context);
	}

	public void OnJump(InputAction.CallbackContext context)
	{
		Jump.Update(context);
	}

	public void OnLook(InputAction.CallbackContext context)
	{
		if(context.phase == InputActionPhase.Performed)
			LookInput = context.ReadValue<Vector2>();
		else if(context.phase == InputActionPhase.Canceled)
			LookInput = Vector2.zero;
	}

	public void OnMove(InputAction.CallbackContext context)
	{
		if (context.phase == InputActionPhase.Performed)
			MoveInput = context.ReadValue<Vector2>();
		else if (context.phase == InputActionPhase.Canceled)
			MoveInput = Vector2.zero;
	}

	public struct ButtonState
	{
		public bool Pressed;
		public bool Held;
		public bool Released;

		public void Update(InputAction.CallbackContext ctx)
		{
			switch (ctx.phase)
			{
				case InputActionPhase.Started:
					Pressed = true;
					Held = true;
					break;
				case InputActionPhase.Canceled:
					Released = true;
					Held = false;
					break;
			}
		}

		public void Flush()
		{
			Pressed = false;
			Released = false;
		}
	}
}
