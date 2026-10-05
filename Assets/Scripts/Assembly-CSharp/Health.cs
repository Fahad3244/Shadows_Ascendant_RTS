using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
	public UnityEvent OnDeath;

	public float MaxHP { get; private set; }

	public float CurrentHP { get; private set; }

	public bool IsDead { get; private set; }

	public event Action<DamageInfo> OnDamaged;

	public event Action<float, float> OnHealthChanged;

	public void Initialize(float maxHP)
	{
		MaxHP = maxHP;
		CurrentHP = maxHP;
		IsDead = false;
		OnHealthChanged?.Invoke(CurrentHP, MaxHP);
	}

	public void TakeDamage(DamageInfo info)
	{
		if (!IsDead)
		{
			CurrentHP -= info.amount;
			this.OnDamaged?.Invoke(info);
			OnHealthChanged?.Invoke(CurrentHP, MaxHP);
			if (CurrentHP <= 0f)
			{
				CurrentHP = 0f;
				IsDead = true;
				OnDeath?.Invoke();
			}
		}
	}

	public void Heal(float amount)
	{
		if (!IsDead)
		{
			CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
			OnHealthChanged?.Invoke(CurrentHP, MaxHP);
		}
	}

	public void RestoreFull()
	{
		if (!IsDead)
		{
			CurrentHP = MaxHP;
		}
	}
}
