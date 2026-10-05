public static class CombatValidation
{
	public static bool IsValidTarget(Health targetHealth, TargetTeam attackerTeam, TargetTeam targetTeam)
	{
		if (targetHealth == null || targetHealth.IsDead)
		{
			return false;
		}
		if (attackerTeam == targetTeam)
		{
			return false;
		}
		_ = 1;
		return true;
	}
}
