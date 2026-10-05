using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DelayCommandManager : MonoBehaviour
{
	[Header("Visuals")]
	[SerializeField]
	private Color previewColor = new Color(1f, 0.1f, 1f, 1f);

	[SerializeField]
	private Color committedColor = new Color(0.1f, 1f, 0.1f, 1f);

	[SerializeField]
	private float lineWidth = 1.4f;

	[SerializeField]
	private float startMarkerSize = 0.4f;

	[SerializeField]
	private float verticalOffset = 0.5f;

	[SerializeField]
	private float pathResolution = 1f;

	private LineRenderer _lineRenderer;

	private List<Vector3> _sweepPath = new List<Vector3>();

	private Vector3 _liveCursorPos;

	private ITargetable _finalTarget;

	private GroupSelection _storedGroup;

	private bool _isPreviewing;

	private bool _hasStoredCommand;

	private GameObject _startMarker;

	private GameObject _endMarker;

	public event Action<GroupSelection, List<Vector3>, ITargetable> OnDelayCommandExecuted;

	private void Awake()
	{
		_lineRenderer = GetComponent<LineRenderer>();
		_lineRenderer.startWidth = lineWidth;
		_lineRenderer.endWidth = lineWidth;
		_lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
		_lineRenderer.positionCount = 0;
	}

	private void OnEnable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnDelayExecuteTapped += ExecuteStoredCommand;
		}
	}

	private void OnDisable()
	{
		if (PlayerInputManager.Instance != null)
		{
			PlayerInputManager.Instance.OnDelayExecuteTapped -= ExecuteStoredCommand;
		}
	}

	private GameObject CreateMarkerIfNeeded(GameObject marker, Color color)
	{
		if (marker == null)
		{
			marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			UnityEngine.Object.Destroy(marker.GetComponent<Collider>());
			marker.transform.localScale = Vector3.one * startMarkerSize;
			marker.GetComponent<Renderer>().material.color = color;
		}
		marker.GetComponent<Renderer>().material.color = color;
		return marker;
	}

	public void StartPreview(GroupSelection group, Vector3 startPos)
	{
		_sweepPath.Clear();
		_finalTarget = null;
		_storedGroup = group;
		_sweepPath.Add(startPos);
		_liveCursorPos = startPos;
		_isPreviewing = true;
		_hasStoredCommand = false;
		_lineRenderer.positionCount = 2;
		_lineRenderer.SetPosition(0, startPos + Vector3.up * verticalOffset);
		_lineRenderer.SetPosition(1, startPos + Vector3.up * verticalOffset);
		_lineRenderer.startColor = previewColor;
		_lineRenderer.endColor = previewColor;
		_startMarker = CreateMarkerIfNeeded(_startMarker, previewColor);
		_startMarker.SetActive(true);
		_startMarker.transform.position = startPos + Vector3.up * verticalOffset;
		_endMarker = CreateMarkerIfNeeded(_endMarker, previewColor);
		_endMarker.SetActive(true);
		_endMarker.transform.position = startPos + Vector3.up * verticalOffset;
	}

	public void UpdatePreview(Vector3 liveCursorPosition)
	{
		if (_isPreviewing)
		{
			_liveCursorPos = liveCursorPosition;
			Vector3 a = _sweepPath[_sweepPath.Count - 1];
			_lineRenderer.SetPosition(_lineRenderer.positionCount - 1, liveCursorPosition + Vector3.up * verticalOffset);
			if (Vector3.Distance(a, liveCursorPosition) >= pathResolution)
			{
				_sweepPath.Add(liveCursorPosition);
				_lineRenderer.positionCount++;
				_lineRenderer.SetPosition(_lineRenderer.positionCount - 1, liveCursorPosition + Vector3.up * verticalOffset);
			}
			if (_endMarker != null)
			{
				_endMarker.transform.position = liveCursorPosition + Vector3.up * verticalOffset;
			}
		}
	}

	public void CommitCommand(ITargetable entityTarget = null)
	{
		if (_isPreviewing)
		{
			if (_sweepPath.Count > 0 && Vector3.Distance(_sweepPath[_sweepPath.Count - 1], _liveCursorPos) > 0.1f)
			{
				_sweepPath.Add(_liveCursorPos);
			}
			_finalTarget = entityTarget;
			_isPreviewing = false;
			_hasStoredCommand = true;
			_lineRenderer.startColor = committedColor;
			_lineRenderer.endColor = committedColor;
			if (_startMarker != null)
			{
				_startMarker.GetComponent<Renderer>().material.color = committedColor;
			}
			if (_endMarker != null)
			{
				_endMarker.GetComponent<Renderer>().material.color = committedColor;
			}
		}
	}

	private void ExecuteStoredCommand()
	{
		if (_hasStoredCommand)
		{
			this.OnDelayCommandExecuted?.Invoke(_storedGroup, new List<Vector3>(_sweepPath), _finalTarget);
		}
	}

	public void ClearCommand()
	{
		_isPreviewing = false;
		_hasStoredCommand = false;
		_sweepPath.Clear();
		_finalTarget = null;
		_lineRenderer.positionCount = 0;
		if (_startMarker != null)
		{
			_startMarker.SetActive(false);
		}
		if (_endMarker != null)
		{
			_endMarker.SetActive(false);
		}
	}
}
