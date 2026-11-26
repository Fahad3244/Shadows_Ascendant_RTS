using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
	[Header("Stats")]
	public string enemyName = "Grunt";

	public float maxHP = 100f;

	public float moveSpeed = 3.5f;

	[Header("Combat Configuration")]
	public float attackDamage = 10f;

	public float attackCooldown = 1f;

	public float attackInterval = 1.5f;

	public float attackRange = 2f;

	[Header("AI Perception")]
	public float aggroRange = 8f;

	public float patrolRadius = 10f;

	[Tooltip("Never reacts to anyone. Used for test dummies.")]
	public bool passive;

	[Header("Heavy Attack")]
	[Tooltip("If true, this enemy's hits on troops are heavy: they knock the troop back and stun it briefly.")]
	public bool heavyAttack;
	public float heavyInterrupt = 0.8f;
	public float heavyKnockback = 2f;
}
