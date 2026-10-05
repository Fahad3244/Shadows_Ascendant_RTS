using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerController))]
public class PlayerHealthHandler : MonoBehaviour
{
	[Header("Health Settings")]
	[SerializeField]
	private int maxHealth = 100;

	private Health _health;

	private PlayerController _controller;

	private void Awake()
	{
		_health = GetComponent<Health>();
		_controller = GetComponent<PlayerController>();
		_health.Initialize(maxHealth);
	}

	private void OnEnable()
	{
		_health.OnDeath.AddListener(HandlePlayerDeath);
	}

	private void OnDisable()
	{
		_health.OnDeath.RemoveListener(HandlePlayerDeath);
	}

	private void HandlePlayerDeath()
	{
		Debug.Log("Player Down! Placeholder state triggered.");
		_controller.SetControlLock(isLocked: true);
	}
}
