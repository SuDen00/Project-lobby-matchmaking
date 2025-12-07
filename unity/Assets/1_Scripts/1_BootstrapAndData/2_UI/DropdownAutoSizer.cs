using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class DropdownAutoSizer : MonoBehaviour
{
	[SerializeField] private TMP_Dropdown dropdown;
	[SerializeField] private float padding = 20f; 

	private void Start()
	{
		if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
		AdjustWidth();
	}

	private void AdjustWidth()
	{
		float maxWidth = 0f;
		var textComponent = dropdown.captionText;

		foreach (var option in dropdown.options)
		{
			Vector2 size = textComponent.GetPreferredValues(option.text);
			maxWidth = Mathf.Max(maxWidth, size.x);
		}

		var rt = GetComponent<RectTransform>();
		rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth + padding);
	}
}
