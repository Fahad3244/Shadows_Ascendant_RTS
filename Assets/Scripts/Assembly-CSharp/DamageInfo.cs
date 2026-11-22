using UnityEngine;

public struct DamageInfo
{
	public float amount;

	public DamageType type;

	public Transform attackerReference;

	public AttackType attackType;

	// Attachment is classified as a heavy interaction
	public bool IsHeavy => attackType == AttackType.Heavy || attackType == AttackType.Attachment;
}
