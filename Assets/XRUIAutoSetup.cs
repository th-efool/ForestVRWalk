using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;
using Unity.XR.CoreUtils;

[System.Serializable]
public class XRUIAutoSetup : MonoBehaviour
{
	[Header("Auto Setup on Awake")]
	[SerializeField]
	private bool m_SetupOnAwake = true;
	[SerializeField]
	private bool m_LogSetup = true;

	[Header("Ray Interactor Settings")]
	[SerializeField]
	private LayerMask m_UILayerMask = 1 << 5; // UI layer by default
	[SerializeField]
	private float m_RayLength = 10f;
	[SerializeField]
	private bool m_EnableUIInteraction = true;

	private void Awake()
	{
		if (m_SetupOnAwake)
		{
			SetupXRUI();
		}
	}

	[ContextMenu("Setup XR UI Now")]
	public void SetupXRUI()
	{
		SetupEventSystem();
		SetupRayInteractors();
		
		if (m_LogSetup)
		{
			Debug.Log("[XRUIAutoSetup] XR UI setup complete. Check controllers have ray interactors and EventSystem has XRUIInputModule.");
		}
	}

	private void SetupEventSystem()
	{
		var eventSystem = FindFirstObjectByType<EventSystem>();
		if (eventSystem == null)
		{
			var esGO = new GameObject("EventSystem");
			eventSystem = esGO.AddComponent<EventSystem>();
			if (m_LogSetup) Debug.Log("[XRUIAutoSetup] Created EventSystem.");
		}

		// Remove StandaloneInputModule if present (conflicts with XRUIInputModule)
		var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
		if (standalone != null)
		{
			DestroyImmediate(standalone);
			if (m_LogSetup) Debug.Log("[XRUIAutoSetup] Removed StandaloneInputModule.");
		}

		// Add XRUIInputModule if missing
		var xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
		if (xrInputModule == null)
		{
			xrInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
			xrInputModule.enableXRInput = true;
			xrInputModule.enableMouseInput = true;
			if (m_LogSetup) Debug.Log("[XRUIAutoSetup] Added XRUIInputModule to EventSystem.");
		}
		else if (!xrInputModule.enableXRInput)
		{
			xrInputModule.enableXRInput = true;
			if (m_LogSetup) Debug.Log("[XRUIAutoSetup] Enabled XR input on XRUIInputModule.");
		}
	}

	private void SetupRayInteractors()
	{
		var xrOrigin = FindFirstObjectByType<XROrigin>();
		if (xrOrigin == null)
		{
			if (m_LogSetup) Debug.LogWarning("[XRUIAutoSetup] No XROrigin found in scene. Cannot setup ray interactors.");
			return;
		}

		// Find controllers (typically under CameraOffset/LeftHand, RightHand)
		var leftHand = FindChildRecursive(xrOrigin.transform, "LeftHand");
		var rightHand = FindChildRecursive(xrOrigin.transform, "RightHand");

		if (leftHand == null && rightHand == null)
		{
			// Try alternative naming
			leftHand = FindChildRecursive(xrOrigin.transform, "LeftController");
			rightHand = FindChildRecursive(xrOrigin.transform, "RightController");
		}

		SetupRayInteractorOnHand(leftHand, "Left");
		SetupRayInteractorOnHand(rightHand, "Right");
	}

	private void SetupRayInteractorOnHand(Transform handTransform, string handName)
	{
		if (handTransform == null)
		{
			if (m_LogSetup) Debug.LogWarning($"[XRUIAutoSetup] {handName} hand not found. Skipping.");
			return;
		}

		var rayInteractor = handTransform.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
		if (rayInteractor == null)
		{
			rayInteractor = handTransform.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
			if (m_LogSetup) Debug.Log($"[XRUIAutoSetup] Added XRRayInteractor to {handName} hand.");
		}

		// Configure for UI
		if (m_EnableUIInteraction)
		{
			// Set interaction layer mask to include UI
			rayInteractor.interactionLayers = InteractionLayerMask.GetMask("Default", "UI");
			
			// Ensure ray can hit UI
			rayInteractor.raycastMask = rayInteractor.raycastMask | m_UILayerMask;
			
			// Set reasonable ray length
			if (rayInteractor.maxRaycastDistance < m_RayLength)
			{
				rayInteractor.maxRaycastDistance = m_RayLength;
			}

			// Enable UI interaction
			rayInteractor.enableUIInteraction = true;

			if (m_LogSetup) Debug.Log($"[XRUIAutoSetup] Configured {handName} ray interactor for UI (layer mask includes UI, raycast mask updated).");
		}
	}

	private Transform FindChildRecursive(Transform parent, string name)
	{
		foreach (Transform child in parent)
		{
			if (child.name.Contains(name))
			{
				return child;
			}
			var found = FindChildRecursive(child, name);
			if (found != null) return found;
		}
		return null;
	}
}

