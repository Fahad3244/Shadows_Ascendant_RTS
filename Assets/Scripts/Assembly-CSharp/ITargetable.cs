using UnityEngine;

public interface ITargetable
{
	bool IsTargetable { get; }

	TargetTeam Team { get; }

	Vector3 GetTargetPosition();

	Transform GetTransform();

	void OnTargetSelected();

	void OnTargetDeselected();
}
