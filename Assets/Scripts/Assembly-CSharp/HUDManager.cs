using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
	[Header("Dependencies")]
	[SerializeField]
	private PlayerUnitManager unitManager;

	[SerializeField]
	private SwarmController swarmController;

	[Header("Configuration")]
	[SerializeField]
	private List<GroupUIProfile> groupProfiles;

	[Header("UI References - Global Bottom Bar")]
	[SerializeField]
	private TextMeshProUGUI globalCountText;

	[SerializeField]
	private Image globalFillBar;

	[Header("UI References - Selected Group")]
	[SerializeField]
	private Image selectedUnitIcon;

	[SerializeField]
	private TextMeshProUGUI selectedUnitCountText;

	[SerializeField]
	private TextMeshProUGUI selectedUnitNameText;

	private Dictionary<GroupSelection, GroupUIProfile> _profileLookup = new Dictionary<GroupSelection, GroupUIProfile>();

	private void Awake()
	{
		foreach (GroupUIProfile groupProfile in groupProfiles)
		{
			_profileLookup.TryAdd(groupProfile.group, groupProfile);
		}
		if (unitManager == null)
		{
			unitManager = Object.FindObjectOfType<PlayerUnitManager>();
		}
		if (swarmController == null)
		{
			swarmController = Object.FindObjectOfType<SwarmController>();
		}
	}

	private void OnEnable()
	{
		if (unitManager != null)
		{
			unitManager.OnUnitsChanged += UpdateUI;
		}
		if (swarmController != null)
		{
			swarmController.OnSelectionChanged += UpdateUI;
		}
	}

	private void OnDisable()
	{
		if (unitManager != null)
		{
			unitManager.OnUnitsChanged -= UpdateUI;
		}
		if (swarmController != null)
		{
			swarmController.OnSelectionChanged -= UpdateUI;
		}
	}

	private void Start()
	{
		UpdateUI();
	}

	public void UpdateUI()
	{
		if (swarmController == null || unitManager == null)
		{
			return;
		}
		GroupSelection currentSelectedGroup = swarmController.CurrentSelectedGroup;
		int num = 0;
		switch (currentSelectedGroup)
		{
		case GroupSelection.All:
			num = unitManager.TotalCount;
			break;
		case GroupSelection.Custom:
			foreach (UnitAgent customGroupUnit in swarmController.CustomGroupUnits)
			{
				if (customGroupUnit != null && customGroupUnit.CurrentStateEnum != UnitState.Dead)
				{
					num++;
				}
			}
			break;
		default:
		{
			UnitType type = (UnitType)(currentSelectedGroup - 1);
			num = unitManager.GetActiveUnitsByType(type).Count + unitManager.GetFreeUnitsByType(type).Count;
			break;
		}
		}
		if (selectedUnitNameText != null)
		{
			selectedUnitNameText.text = currentSelectedGroup.ToString();
		}
		if (selectedUnitCountText != null)
		{
			selectedUnitCountText.text = num.ToString();
		}
		if (_profileLookup.TryGetValue(currentSelectedGroup, out var value) && selectedUnitIcon != null)
		{
			selectedUnitIcon.sprite = value.icon;
		}
		if (globalCountText != null)
		{
			if (!globalCountText.gameObject.activeSelf)
			{
				globalCountText.gameObject.SetActive(value: true);
			}
			globalCountText.text = $"{unitManager.FreeCount}/{unitManager.TotalCount}";
		}
		if (globalFillBar != null)
		{
			if (!globalFillBar.gameObject.activeSelf)
			{
				globalFillBar.gameObject.SetActive(value: true);
			}
			float fillAmount = ((unitManager.TotalCount > 0) ? ((float)unitManager.FreeCount / (float)unitManager.TotalCount) : 0f);
			globalFillBar.fillAmount = fillAmount;
		}
	}
}
