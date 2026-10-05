using UnityEngine;

public class UnitInteractState : UnitStateBase
{
	private AttachableTarget _target;

	private float _interactTimer;

	private bool _isInSlot;

	private float _rangedTooCloseTimer;

	private float _currentRangedDistance;

	private float _currentReactDelay;

	private Vector3 _rangedHoldPosition;

	private bool _hasRangedHoldPosition;

	private const float ATTACK_INTERVAL = 1f;

	private const float UNIT_DAMAGE = 10f;

	private const float RANGED_MIN_DISTANCE = 3f;

	private const float RANGED_MAX_DISTANCE = 6f;

	private const float RANGED_STEP_BACK_DISTANCE = 1f;

	private const float RANGED_RETREAT_ANGLE_RANGE = 70f;

	private const float RANGED_REACT_DELAY_MIN = 1.5f;

	private const float RANGED_REACT_DELAY_MAX = 2.5f;

	public UnitInteractState(UnitAgent agent)
		: base(agent)
	{
	}

	public void SetTarget(AttachableTarget target)
	{
		_target = target;
		_isInSlot = false;
		_hasRangedHoldPosition = false;
		_rangedTooCloseTimer = 0f;
		_currentReactDelay = UnityEngine.Random.Range(RANGED_REACT_DELAY_MIN, RANGED_REACT_DELAY_MAX);
	}

	public override void Enter()
	{
		_agent.NavAgent.isStopped = true;
		_agent.NavAgent.updatePosition = false;
		_agent.NavAgent.updateRotation = false;
		_agent.ApplyMovementDynamics(_agent.BaseMoveSpeed * 3f, _agent.BaseAcceleration * 3f);
	}

	public override void Update()
	{
		if (_target == null || _target.gameObject == null)
		{
			_agent.ReturnToPlayer();
			return;
		}
		Health component = _target.GetComponent<Health>();
		TargetableEntity component2 = _target.GetComponent<TargetableEntity>();
		TargetTeam targetTeam = ((!(component2 != null)) ? TargetTeam.Object : component2.Team);
		if (targetTeam == TargetTeam.Enemy && (component == null || component.IsDead || !CombatValidation.IsValidTarget(component, TargetTeam.Friendly, targetTeam)))
		{
			_agent.ReturnToPlayer();
			return;
		}
		Vector3 attachmentPosition = _target.GetAttachmentPosition(_agent);
		bool isRangedVsEnemy = _agent.Type == UnitType.Ranged && targetTeam == TargetTeam.Enemy;
		if (isRangedVsEnemy)
		{
			if (!_hasRangedHoldPosition)
			{
				Vector3 direction = _agent.transform.position - _target.transform.position;
				direction.y = 0f;
				if (direction.sqrMagnitude < 0.01f)
				{
					direction = _agent.transform.forward;
				}
				_currentRangedDistance = RANGED_MAX_DISTANCE;
				_rangedHoldPosition = _target.transform.position + direction.normalized * _currentRangedDistance;
				_rangedHoldPosition.y = _agent.transform.position.y;
				_hasRangedHoldPosition = true;
			}

			float currentDistance = Vector3.Distance(_agent.transform.position, _target.transform.position);
			if (currentDistance < RANGED_MIN_DISTANCE)
			{
				_rangedTooCloseTimer += Time.deltaTime;
				if (_rangedTooCloseTimer >= _currentReactDelay)
				{
					Vector3 direction = _agent.transform.position - _target.transform.position;
					direction.y = 0f;
					if (direction.sqrMagnitude < 0.01f)
					{
						direction = _agent.transform.forward;
					}
					float randomAngle = UnityEngine.Random.Range(-RANGED_RETREAT_ANGLE_RANGE, RANGED_RETREAT_ANGLE_RANGE);
					direction = Quaternion.Euler(0f, randomAngle, 0f) * direction;
					_currentRangedDistance += RANGED_STEP_BACK_DISTANCE;
					_rangedHoldPosition = _target.transform.position + direction.normalized * _currentRangedDistance;
					_rangedHoldPosition.y = _agent.transform.position.y;
					_rangedTooCloseTimer = 0f;
					_currentReactDelay = UnityEngine.Random.Range(RANGED_REACT_DELAY_MIN, RANGED_REACT_DELAY_MAX);
					_isInSlot = false;
				}
			}
			else
			{
				_rangedTooCloseTimer = 0f;
			}

			attachmentPosition = _rangedHoldPosition;
		}
		if (!_isInSlot)
		{
			if (Vector3.Distance(_agent.transform.position, attachmentPosition) > 0.05f)
			{
				Vector3 normalized = (attachmentPosition - _agent.transform.position).normalized;
				_agent.transform.position += normalized * (10f * Time.deltaTime);
			}
			else
			{
				_isInSlot = true;
			}
		}
		if (_isInSlot)
		{
			_agent.transform.position = attachmentPosition;
			if (component != null && !component.IsDead && CombatValidation.IsValidTarget(component, TargetTeam.Friendly, targetTeam))
			{
				_interactTimer += Time.deltaTime;
				if (_interactTimer >= 1f)
				{
					_interactTimer = 0f;
					DamageInfo info = new DamageInfo
					{
						amount = 10f,
						attackerReference = _agent.transform,
						type = DamageType.Normal
					};
					component.TakeDamage(info);
					if (_agent.Type == UnitType.Ranged)
					{
						FireProjectile();
					}
				}
			}
		}
		Vector3 normalized2 = (_target.transform.position - _agent.transform.position).normalized;
		normalized2.y = 0f;
		if (normalized2 != Vector3.zero)
		{
			_agent.transform.rotation = Quaternion.Slerp(_agent.transform.rotation, Quaternion.LookRotation(normalized2), 25f * Time.deltaTime);
		}
	}

	public override void Exit()
	{
		_agent.NavAgent.updatePosition = true;
		_agent.NavAgent.updateRotation = true;
		_agent.NavAgent.isStopped = false;
		_agent.ApplyMovementDynamics();
		_agent.NavAgent.Warp(_agent.transform.position);
	}

	private void FireProjectile()
	{
		GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		projectile.transform.position = _agent.transform.position + Vector3.up * 0.5f;
		projectile.transform.localScale = Vector3.one * 0.3f;
		Collider col = projectile.GetComponent<Collider>();
		if (col != null)
		{
			Object.Destroy(col);
		}
		Renderer rend = projectile.GetComponent<Renderer>();
		if (rend != null)
		{
			rend.material.color = Color.yellow;
		}
		ProjectileVisual visual = projectile.AddComponent<ProjectileVisual>();
		visual.Launch(_target.transform.position + Vector3.up * 1f, 20f);
	}
}
