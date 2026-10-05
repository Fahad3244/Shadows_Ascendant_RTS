using UnityEngine;

public class VirtualCursor : MonoBehaviour
{
	[Header("Input Configuration")]
	[SerializeField]
	private string mouseX = "Mouse X";

	[SerializeField]
	private string mouseY = "Mouse Y";

	[Header("Responsiveness")]
	[Tooltip("Sensitivity for Mouse (Pixels per frame)")]
	[SerializeField]
	private float mouseSensitivity = 20f;

	[Header("World Projection")]
	[SerializeField]
	private LayerMask groundLayer;

	[SerializeField]
	private float verticalOffset = 0.1f;

	[Header("Visuals")]
	[SerializeField]
	private GameObject cursorVisualObject;

	private Camera _mainCamera;

	private bool _visualsEnabled = true;

	public static VirtualCursor Instance { get; private set; }

	public Vector2 VirtualScreenPosition { get; private set; }

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Object.Destroy(this);
			return;
		}
		Instance = this;
		_mainCamera = Camera.main;
		VirtualScreenPosition = new Vector2((float)Screen.width / 2f, (float)Screen.height / 2f);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void Start()
	{
		UpdateCursorProjection();
	}

	private void Update()
	{
		HandleInput();
		UpdateCursorProjection();
	}

	private void HandleInput()
	{
		Vector2 zero = Vector2.zero;
		float axisRaw = Input.GetAxisRaw(mouseX);
		float axisRaw2 = Input.GetAxisRaw(mouseY);
		if (Mathf.Abs(axisRaw) > 0.01f || Mathf.Abs(axisRaw2) > 0.01f)
		{
			zero += new Vector2(axisRaw, axisRaw2) * mouseSensitivity;
		}
		VirtualScreenPosition += zero;
		VirtualScreenPosition = new Vector2(Mathf.Clamp(VirtualScreenPosition.x, 0f, Screen.width), Mathf.Clamp(VirtualScreenPosition.y, 0f, Screen.height));
	}

	private void UpdateCursorProjection()
	{
		if (cursorVisualObject == null)
		{
			return;
		}
		RaycastHit hitInfo;
		if (!_visualsEnabled)
		{
			if (cursorVisualObject.activeSelf)
			{
				cursorVisualObject.SetActive(value: false);
			}
		}
		else if (Physics.Raycast(_mainCamera.ScreenPointToRay(VirtualScreenPosition), out hitInfo, 1000f, groundLayer))
		{
			if (!cursorVisualObject.activeSelf)
			{
				cursorVisualObject.SetActive(value: true);
			}
			cursorVisualObject.transform.position = hitInfo.point + Vector3.up * verticalOffset;
			cursorVisualObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		}
		else if (cursorVisualObject.activeSelf)
		{
			cursorVisualObject.SetActive(value: false);
		}
	}

	public void ToggleVisuals(bool show)
	{
		if (_visualsEnabled != show)
		{
			_visualsEnabled = show;
			if (cursorVisualObject != null)
			{
				cursorVisualObject.SetActive(show);
			}
		}
	}

	public bool GetWorldPosition(LayerMask mask, out Vector3 worldPos)
	{
		if (Physics.Raycast(_mainCamera.ScreenPointToRay(VirtualScreenPosition), out var hitInfo, 1000f, mask))
		{
			worldPos = hitInfo.point;
			return true;
		}
		worldPos = Vector3.zero;
		return false;
	}

	public void WarpToWorldPosition(Vector3 worldPos)
	{
		if (!(_mainCamera == null))
		{
			Vector3 vector = _mainCamera.WorldToScreenPoint(worldPos);
			if (vector.z > 0f)
			{
				VirtualScreenPosition = new Vector2(vector.x, vector.y);
			}
		}
	}
}
