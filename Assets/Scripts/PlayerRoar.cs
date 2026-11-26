using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerRoar : MonoBehaviour
{
	[SerializeField] private KeyCode key = KeyCode.R;
	[SerializeField] private float radius = 8f;
	[SerializeField] private float cooldown = 12f;
	[SerializeField] private float knockback = 6f;
	[SerializeField] private float stagger = 1.5f;
	[SerializeField] private float damage = 5f;

	private PlayerController _pc;
	private float _readyAt;
	private float _noteUntil;
	private string _note = "";
	private GameObject _wave;
	private Material _waveMat;
	private float _waveStart;
	private GUIStyle _style;
	private static bool _hooked;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (_hooked) return;
		_hooked = true;
		SceneManager.sceneLoaded += (s, m) => AddToPlayer();
		AddToPlayer();
	}

	private static void AddToPlayer()
	{
		PlayerController pc = FindObjectOfType<PlayerController>();
		if (pc != null && pc.GetComponent<PlayerRoar>() == null) pc.gameObject.AddComponent<PlayerRoar>();
	}

	private void Awake() { _pc = GetComponent<PlayerController>(); }

	private void Update()
	{
		if (Input.GetKeyDown(key)) TryRoar();
		UpdateWave();
	}

	private void TryRoar()
	{
		if (_pc == null || !_pc.IsTurretMode)
		{
			Note("The roar only works in Turret Mode (4 or fewer troops with you).");
			return;
		}
		if (Time.time < _readyAt)
		{
			Note("Roar recharging: " + Mathf.CeilToInt(_readyAt - Time.time) + "s");
			return;
		}
		_readyAt = Time.time + cooldown;

		int hit = 0;
		foreach (EnemyBase e in FindObjectsOfType<EnemyBase>())
		{
			if (e.State == EnemyBase.EnemyState.Dead) continue;
			Vector3 d = e.transform.position - transform.position;
			d.y = 0f;
			if (d.magnitude > radius) continue;
			Vector3 dir = d.sqrMagnitude < 0.01f ? transform.forward : d.normalized;
			Health h = e.GetComponent<Health>();
			if (h != null)
			{
				h.TakeDamage(new DamageInfo
				{
					amount = damage,
					attackerReference = transform,
					type = DamageType.Normal,
					attackType = AttackType.Heavy,
					interruptSeconds = stagger
				});
			}
			e.Knockback(dir, knockback);
			hit++;
		}
		if (CombatDebug.Enabled) Debug.Log("[Roar] pushed back " + hit + " enemies");
		Note(hit > 0 ? "ROAR! " + hit + " enemies pushed back" : "ROAR! Nothing in range");
		StartWave();
	}

	private void Note(string text)
	{
		_note = text;
		_noteUntil = Time.time + 2f;
	}

	private void StartWave()
	{
		if (_wave != null) Destroy(_wave);
		_wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		Collider c = _wave.GetComponent<Collider>();
		if (c != null) Destroy(c);
		_waveMat = new Material(Shader.Find("Sprites/Default"));
		_wave.GetComponent<Renderer>().material = _waveMat;
		_waveStart = Time.time;
	}

	private void UpdateWave()
	{
		if (_wave == null) return;
		float t = (Time.time - _waveStart) / 0.5f;
		if (t >= 1f)
		{
			Destroy(_wave);
			if (_waveMat != null) Destroy(_waveMat);
			_wave = null;
			return;
		}
		float s = Mathf.Lerp(1f, radius * 2f, UiEase.OutCubic(t));
		_wave.transform.position = transform.position + Vector3.up * 0.15f;
		_wave.transform.localScale = new Vector3(s, 0.04f, s);
		_waveMat.color = new Color(1f, 0.6f, 0.2f, (1f - t) * 0.6f);
	}

	private void OnGUI()
	{
		bool turret = _pc != null && _pc.IsTurretMode;
		bool note = Time.time < _noteUntil;
		if (!turret && !note) return;
		if (_style == null)
		{
			_style = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
			_style.normal.textColor = Color.white;
		}
		string t = "";
		if (turret)
		{
			float left = _readyAt - Time.time;
			t = left <= 0f
				? "<color=#ffd966>TURRET MODE</color>   press <b>R</b> to ROAR"
				: "<color=#ffd966>TURRET MODE</color>   <color=#999999>roar recharging " + Mathf.CeilToInt(left) + "s</color>";
		}
		if (note) t += (t.Length > 0 ? "\n" : "") + _note;
		float w = 520f, h = note && turret ? 62f : 36f;
		Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height - h - 90f, w, h);
		GUI.DrawTexture(r, UiEase.Solid(new Color(0.05f, 0.07f, 0.10f, 0.85f)));
		GUI.Label(r, t, _style);
	}
}
