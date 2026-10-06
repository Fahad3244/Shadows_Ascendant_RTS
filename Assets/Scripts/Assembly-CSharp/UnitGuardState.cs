using UnityEngine;

public class UnitGuardState : UnitStateBase
{
	private GuardFlag _flag;

	private float _scanTimer;

	private const float SCAN_INTERVAL = 0.5f;

	public UnitGuardState(UnitAgent agent)
		: base(agent)
	{
	}

	public void SetFlag(GuardFlag flag)
	{
		_flag = flag;
	}

	public override void Enter()
	{
		_agent.NavAgent.isStopped = false;
		_agent.NavAgent.stoppingDistance = 0.1f;
		_agent.ApplyMovementDynamics();
	}

	public override void Update()
	{
		if (_flag == null || _flag.gameObject == null)
		{
			_agent.ReturnToPlayer();
			return;
		}
		Vector3 formationPosition = _flag.GetFormationPosition(_agent);
		_agent.NavAgent.SetDestination(formationPosition);
		Vector3 normalized = (_agent.transform.position - _flag.transform.position).normalized;
		if (normalized != Vector3.zero)
		{
			_agent.transform.rotation = Quaternion.Slerp(_agent.transform.rotation, Quaternion.LookRotation(normalized), 5f * Time.deltaTime);
		}
		if (_agent.Type == UnitType.Melee || _agent.Type == UnitType.Ranged)
		{
			HandleAggroScan();
		}
	}

	private void HandleAggroScan()
	{
		_scanTimer += Time.deltaTime;
		if (_scanTimer >= 0.5f)
		{
			_scanTimer = 0f;
			ScanForEnemies();
		}
	}

	private void ScanForEnemies()
	{
		float guardRadius = _flag.guardRadius;
		Collider[] array = Physics.OverlapSphere(_flag.transform.position, guardRadius, _agent.targetLayer);
		AttachableTarget attachableTarget = null;
		float num = float.MaxValue;
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			AttachableTarget componentInParent = array2[i].GetComponentInParent<AttachableTarget>();
			if (componentInParent != null && componentInParent.Team == TargetTeam.Enemy && componentInParent.CanBeEngaged)
			{
				float num2 = Vector3.Distance(_agent.transform.position, componentInParent.transform.position);
				if (num2 < num)
				{
					num = num2;
					attachableTarget = componentInParent;
				}
			}
		}
		if (attachableTarget != null && attachableTarget.TryReserveSlot(_agent))
		{
			_agent.SetReservedTarget(attachableTarget);
			_agent.MoveToTarget(attachableTarget);
		}
	}

	public override void Exit()
	{
		if (_flag != null)
		{
			_flag.RemoveUnit(_agent);
			_flag = null;
		}
	}
}
