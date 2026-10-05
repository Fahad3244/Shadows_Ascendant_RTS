using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitPool : MonoBehaviour
{
	[Serializable]
	public struct UnitPrefabConfig
	{
		public UnitType type;

		public UnitAgent prefab;
	}

	[Header("Pool Config")]
	[SerializeField]
	private List<UnitPrefabConfig> prefabConfigs;

	private Dictionary<UnitType, Queue<UnitAgent>> _poolDictionary = new Dictionary<UnitType, Queue<UnitAgent>>();

	private Dictionary<UnitType, UnitAgent> _prefabLookup = new Dictionary<UnitType, UnitAgent>();

	public HashSet<UnitAgent> ActiveAgents { get; private set; } = new HashSet<UnitAgent>();

	private void Awake()
	{
		InitializeLookup();
	}

	private void InitializeLookup()
	{
		foreach (UnitPrefabConfig prefabConfig in prefabConfigs)
		{
			_prefabLookup[prefabConfig.type] = prefabConfig.prefab;
			_poolDictionary[prefabConfig.type] = new Queue<UnitAgent>();
		}
	}

	public UnitAgent GetAgent(UnitType type, Vector3 position, Quaternion rotation)
	{
		if (!_poolDictionary.ContainsKey(type))
		{
			Debug.LogError($"Pool: No configuration for {type}");
			return null;
		}
		UnitAgent unitAgent = ((_poolDictionary[type].Count <= 0) ? UnityEngine.Object.Instantiate(_prefabLookup[type], base.transform) : _poolDictionary[type].Dequeue());
		unitAgent.transform.position = position;
		unitAgent.transform.rotation = rotation;
		unitAgent.gameObject.SetActive(value: true);
		ActiveAgents.Add(unitAgent);
		return unitAgent;
	}

	public void ReturnAgent(UnitAgent agent)
	{
		ActiveAgents.Remove(agent);
		agent.gameObject.SetActive(value: false);
		_poolDictionary[agent.Type].Enqueue(agent);
	}

	public bool ReactivateAgent(UnitAgent agent, Vector3 position, Quaternion rotation)
	{
		if (agent == null || ActiveAgents.Contains(agent)) return false;
		if (!_poolDictionary.TryGetValue(agent.Type, out var queue) || !queue.Contains(agent)) return false;

		_poolDictionary[agent.Type] = new Queue<UnitAgent>(queue.Where(a => a != agent));
		agent.transform.SetPositionAndRotation(position, rotation);
		agent.gameObject.SetActive(true);
		ActiveAgents.Add(agent);
		return true;
	}
}
