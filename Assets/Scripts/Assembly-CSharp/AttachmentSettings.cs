using UnityEngine;

[CreateAssetMenu(fileName = "AttachmentSettings", menuName = "Combat/Attachment Settings")]
public class AttachmentSettings : ScriptableObject
{
	[Min(1)] public int maxAttachmentPoints = 3;
}
