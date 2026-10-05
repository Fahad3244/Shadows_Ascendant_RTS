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
}
