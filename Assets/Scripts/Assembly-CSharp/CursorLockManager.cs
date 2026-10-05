using UnityEngine;

public class CursorLockManager : MonoBehaviour
{
	[Header("Settings")]
	[Tooltip("Should the cursor be locked immediately when the game starts?")]
	[SerializeField]
	private bool lockOnStart = true;

	private void Start()
	{
		if (lockOnStart)
		{
			SetCursorState(isLocked: true);
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			SetCursorState(isLocked: false);
		}
		if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
		{
			SetCursorState(isLocked: true);
		}
	}

	private void SetCursorState(bool isLocked)
	{
		if (isLocked)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}
}
