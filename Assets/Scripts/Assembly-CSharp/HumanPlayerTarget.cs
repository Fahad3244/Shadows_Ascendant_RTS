using UnityEngine;

public class HumanPlayerTarget : MonoBehaviour
{
	[SerializeField] private bool aware = true;
	[SerializeField] private float maxHealth = 600f;
	[Tooltip("Dot product above this = direct frontal attempt.")]
	[SerializeField] private float directFrontDot = 0.7f;
	[SerializeField] private float lightDamage = 5f;

	public string LastResult { get; private set; } = "";
	private float _lastTime = -999f;
	private GUIStyle _style;

	public bool IsAware => aware;

	private void Awake()
	{
		Health h = GetComponent<Health>();
		if (h != null && h.MaxHP <= 0f) h.Initialize(500f);
	}

	public bool IsDirectFront(Vector3 attackerPos)
	{
		Vector3 to = attackerPos - transform.position;
		to.y = 0f;
		if (to.sqrMagnitude < 0.01f) return true;
		Vector3 f = transform.forward;
		f.y = 0f;
		return Vector3.Dot(f.normalized, to.normalized) > directFrontDot;
	}

	public float DotFrom(Vector3 attackerPos)
	{
		Vector3 to = attackerPos - transform.position;
		to.y = 0f;
		if (to.sqrMagnitude < 0.01f) return 1f;
		Vector3 f = transform.forward;
		f.y = 0f;
		return Vector3.Dot(f.normalized, to.normalized);
	}

	// Returns true if the attach attempt is blocked (troop must not attach)
	public bool TryBlock(UnitAgent troop)
	{
		bool front = IsDirectFront(troop.transform.position);
		bool blocked = aware && front;
		float dot = DotFrom(troop.transform.position);
		string yaw = transform.eulerAngles.y.ToString("F0");
		if (blocked)
		{
			Health h = GetComponent<Health>();
			if (h != null && !h.IsDead)
			{
				h.TakeDamage(new DamageInfo
				{
					amount = lightDamage,
					attackerReference = troop.transform,
					type = DamageType.Normal,
					attackType = AttackType.Light
				});
			}
			LastResult = "FRONT BLOCKED -> light attack";
		}
		else
		{
			LastResult = !aware ? "ATTACH OK (unaware)" : "ATTACH OK (not frontal)";
		}
		LastResult += $"  dot={dot:F2} yaw={yaw}";
		_lastTime = Time.time;
		Debug.Log($"[Human] {name} vs {troop.name}: {LastResult}");
		return blocked;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.U)) aware = !aware;
	}

	private void OnGUI()
	{
		Camera cam = Camera.main;
		if (cam == null) return;
		Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 4.5f);
		if (sp.z < 0f) return;
		if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
		string t = "HUMAN PLAYER (" + (aware ? "AWARE" : "UNAWARE") + ")  U = toggle";
		if (Time.time - _lastTime < 4f) t += "\n" + LastResult;
		_style.normal.textColor = Color.cyan;
		GUI.Label(new Rect(sp.x - 160f, Screen.height - sp.y - 20f, 320f, 50f), t, _style);
	}
}
