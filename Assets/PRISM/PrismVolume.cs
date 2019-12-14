using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[AddComponentMenu(MENU_PATH)]
[DisallowMultipleComponent]
public class PrismVolume : MonoBehaviour {

	private const string MENU_PATH = "PRISM/Prism Volume";

	/// <summary>
	/// Types of shapes for the volume
	/// </summary>
	public enum VolumeShape
	{
		BOX = 1,
		SPHERE = 2
	}

	/// <summary>
	/// TODO
	/// </summary>
	public enum VolumeLerpType
	{
		INSTANT = 1,
		LERP_WHEN_TRIGGERED = 2,
		LERP_OVER_DISTANCE = 3
	}
	[Header("Volume Settings")]
	/// <summary>
	/// The shape of the volume
	/// </summary>
	[Tooltip("Shape of the PRISM volume, used for collision")]
	public VolumeShape volumeShape = VolumeShape.SPHERE;

	[Tooltip("PRISM preset that the volume will use")]
	public Prism.Utils.PrismPreset volumePreset;
	private Prism.Utils.PrismPreset lastPreset;

	/// <summary>
	/// Do we want to update PRISM's values each frame, or just once?
	/// </summary>
	[Tooltip("Do we want to update PRISM's values and check collision each frame, or manually through ManualUpdate()?")]
	public bool updateEachFrame = true;
	
	/// <summary>
	/// The range of the volume
	/// </summary>
	[Tooltip("The radius of the sphere")]
	public float volumeRange = 10f;

	[Tooltip("The size of the volume box")]
	public Vector3 volumeSize = new Vector3(10f, 10f, 10f);
	private float volumeWidth {
		get {
			return volumeSize.x;
		}
	}
	private float volumeHeight {
		get {
			return volumeSize.y;
		}
	}
	private float volumeDepth {
		get {
			return volumeSize.z;
		}
	}

	[Space]
	[Header("Target Camera Settings")]
	/// <summary>
	/// The target camera (Requires PRISM on it)
	/// </summary>
	[Tooltip("The target camera (requires PRISM on it)")]
	public Camera targetCamera;

	private PrismEffects targetPRISM;

	private bool isMainVolume = false;

	/// <summary>
	/// Do we want to just grab the main camera? (Ease of Use)
	/// </summary>
	[Tooltip("Ease of use check, tick if we should just use the main camera")]
	public bool useMainCamera;

	[Space]
	[Header("Timing Settings")]
	/// <summary>
	/// Do we want to instantly switch preset, or lerp over time
	/// </summary>
	[Tooltip("Do we want to instantly switch preset, or lerp over time")]
	public bool instantSwitch = false;

	/// <summary>
	/// The time it should take to lerp between the current preset and the volume preset once the volume is triggered
	/// </summary>
	[Tooltip("The time it should take to lerp between the current preset and the volume preset once the volume is triggered")]
	public float switchDuration = 1f;
	private float currentSwitchTime = 0f;
	private bool hasCompletedSwitch = true;

	[Space]
	[Header("Tether Settings")]
	/// <summary>
	/// Upon entering the volume, the volume range is extended by this amount for volumeTetherTime seconds, to prevent instant-switching between volumes
	/// </summary>
	[Tooltip("Upon entering the volume, the volume range is extended by this amount for volumeTetherTime seconds, to prevent instant-flickering between volumes")]
	public float volumeTetherRange = 2f;

	/// <summary>
	/// The amount of time to apply the volume tether range increase for upon triggering of a volume
	/// </summary>
	[Tooltip("The amount of time to apply the volume tether range increase for upon triggering of a volume")]
	public float volumeTetherDuration = 1f;
	private float volumeTriggerTime;

	private bool isTethered = false;
	private bool isActiveVolume = false;

	private Camera _targetCamera {
		get {
			if(Camera.main != null && useMainCamera == true) 
			{
				return Camera.main;
			} else return targetCamera;
		}
	}

	void OnEnable()
	{
		ResetTargetCameraToMain();
		GetTargetPRISM();
	}

	void Reset()
	{
		OnEnable();
	}

	void ResetTargetCameraToMain()
	{
		if(Camera.main != null && targetCamera == null)
		{
			targetCamera = Camera.main;
		}
	}

	void GetTargetPRISM()
	{
		targetPRISM = targetCamera.GetComponent<PrismEffects>();

		if(targetPRISM == null)
		{
			Debug.LogWarning("Could not find a PRISM Effects script on your target/main camera!");
			return;
		}

		lastPreset = targetPRISM.currentPrismPreset;

		if(GetComponent<PrismEffects>() == targetPRISM && isMainVolume == false)
		{
#if UNITY_EDITOR
			//Debug.Log("Main volume set to: " + gameObject.name + ". All new PRISM Volumes will take initial settings from this volume.");
#endif
			volumeRange = 1f;
			volumeSize = new Vector3(1f, 1f, 1f);
			isMainVolume = true;
		}
	}
	
	// Update is called once per frame
	void Update () {

		if(updateEachFrame)
		{
			ManualUpdate();
		}

		//Early exit
		if(isTethered == false) return;

		if(Time.time > volumeTriggerTime + volumeTetherDuration && isTethered == true)
		{
			DeTether();
		}

		volumeTriggerTime -= Time.deltaTime;
	}

	/// <summary>
	/// Call this in place of Update() To check for collision and also lerp PRISM.
	/// </summary>
	public void ManualUpdate()
	{
		//If we're the camera volume (main), exit us out 
		if(isMainVolume == true && targetPRISM.currentlyInsideVolume == true)
		{
			//Debug.Log("Exit" + gameObject.name);
			ExitVolume();
		}
		
		if(isActiveVolume)
		{
			bool shouldBeActive = false;
			if(isTethered)
			{
				shouldBeActive = CheckIfShouldTrigger(volumeTetherRange);
			} else {
				shouldBeActive = CheckIfShouldTrigger(0f);
			}
			
			if(shouldBeActive)
			{
				HandlePresetLerping(volumePreset);
			} else {
				//Debug.Log("Exit2 " + gameObject.name);
				ExitVolume();
			}
		} else {
			isActiveVolume = CheckIfShouldTrigger(0f);
			if(isActiveVolume)
			{
				TriggerVolume ();
			}
		}
	}
	
	void HandlePresetLerping(Prism.Utils.PrismPreset preset)
	{
		if(preset == null)
		{
			Debug.LogWarning("No preset found! Remember to set a Preset for each volume");
			return;
		}

		//If we need to lerp over time, lerp it
		if(instantSwitch == false && hasCompletedSwitch == false)
		{
			targetPRISM.LerpToPreset(preset, currentSwitchTime, isMainVolume);
			currentSwitchTime += Time.deltaTime;
			currentSwitchTime = Mathf.Clamp01(currentSwitchTime);// Mathf.Min(currentSwitchTime, 1f);
			
			if(currentSwitchTime == 1f)
			{
				hasCompletedSwitch = true;
				CallSetPrismPreset(preset);
			}
		}
	}

	/// <summary>
	/// Detethers the volume.
	/// </summary>
	private void DeTether()
	{
		isTethered = false;
		volumeTriggerTime = 0f;
	}

	public bool CheckIfShouldTrigger(float extraRange)
	{
		switch(volumeShape)
		{
		case VolumeShape.BOX:
			return PointIsInCube(GetCameraPosition(), GetVolumePosition(), new Vector3(volumeWidth + extraRange, volumeHeight + extraRange, volumeDepth + extraRange)); 

		case VolumeShape.SPHERE:
			return IsWithinRange(volumeRange + extraRange, GetVolumePosition(), GetCameraPosition());
		default:
			Debug.LogWarning("No volume shape specified");
			break;
		}

		return false;
	}

	/// <summary>
	/// Triggers the volume.
	/// </summary>
	public void TriggerVolume()
	{
		//If PRISM is already being controlled by a volume, don't try to control it.
		if(targetPRISM.currentlyInsideVolume == true)
		{
			//Debug.Log("Prism script is already inside a volume - not triggering this one (" + gameObject.name + ")");
			return;
		}

		//Upon triggering a volume, we need to set the tether to active
		TetherVolume();
		isActiveVolume = true;
		lastPreset = targetPRISM.currentPrismPreset;

		//The main volume should take the lowest priority of all. Hence, if we ARE the main volume, don't set this - it lets any other volume 'override' us
		if(isMainVolume == false)
		{
			targetPRISM.currentlyInsideVolume = true;
			//Debug.Log("Set to currently inside from " + gameObject.name);
		}

		if(lastPreset == null)
		{
			Debug.LogWarning("Could not find a PRISM.Preset on the target camera's PRISM component!");
		}

		//If we have the volume set to instant switch, switch the presets instantly
		if(instantSwitch)
		{
			CallSetPrismPreset(volumePreset);
		} else {
			//Otherwise, start this, which in Update() lerps the presets
			hasCompletedSwitch = false;
			currentSwitchTime = 0f;
		}
	}

	public void TetherVolume()
	{
		volumeTriggerTime = Time.time;
		isTethered = true;
	}

	public void ExitVolume()
	{
		TetherVolume();
		isActiveVolume = false;
		hasCompletedSwitch = false;
		currentSwitchTime = 0f;

		//The main camera volume never decides this
		if(!isMainVolume)
		{
			targetPRISM.currentlyInsideVolume = false;
		}
	}

	/// <summary>
	/// Sets the PRISM Preset on our linked PRISM script
	/// </summary>
	/// <param name="preset">Preset.</param>
	void CallSetPrismPreset(Prism.Utils.PrismPreset preset)
	{
		//Debug.Log("SET to " + preset.name + " from " + gameObject.name);
		targetPRISM.SetPrismPreset(preset);
	}
	
	private Vector3 GetCameraPosition()
	{
		return _targetCamera.transform.position;
	}
	
	private Vector3 GetVolumePosition()
	{
		return transform.position;
	}

	void OnDrawGizmos()
	{
		//Draw if something interesting
		if(CheckIfShouldTrigger(volumeTetherRange))
		{
			OnDrawGizmosSelected();
		}
	}

	/// <summary>
	/// Draw green if the volume is not in use, yellow if the volume is almost triggered, and red if the volume is close enough to the camera to be triggered
	/// </summary>
	void OnDrawGizmosSelected()
	{
		Color gizmoColor = Color.green;
		bool isWithinTether = false;

		if(CheckIfShouldTrigger(0f))
		{
			gizmoColor = Color.red;
		} else if(CheckIfShouldTrigger(volumeTetherRange))
		{
			gizmoColor = Color.yellow;
			isWithinTether = true;
		}
		gizmoColor.a = 0.35f;
		Gizmos.color = gizmoColor;

		if(volumeShape == VolumeShape.SPHERE)
		{
			Gizmos.DrawSphere(GetVolumePosition(), volumeRange);
		} else if (volumeShape == VolumeShape.BOX)
		{
			float extraRange = 0f;
			if(isWithinTether) extraRange = volumeTetherRange;
			Gizmos.DrawCube(GetVolumePosition(), new Vector3(volumeWidth + extraRange, volumeHeight + extraRange, volumeDepth + extraRange));
		}

	}

	/// <summary>
	/// Determines if is within  the specified range, fast
	/// </summary>
	/// <returns><c>true</c> if is within range the specified range firstPosition secondPosition; otherwise, <c>false</c>.</returns>
	/// <param name="range">Range.</param>
	/// <param name="firstPosition">First position.</param>
	/// <param name="secondPosition">Second position.</param>
	public static bool IsWithinRange(float range, Vector3 firstPosition, Vector3 secondPosition)
	{
		if((firstPosition - secondPosition).sqrMagnitude < (range * range))
		{
			return true;
		}
		
		return false;
	}

	public static bool PointIsInCube( Vector3 point, Vector3 cubeCenter, Vector3 cubeSizes)
	{
		float x_max, x_min, y_min, y_max, z_min, z_max;
		cubeSizes.Scale(new Vector3(0.5f, 0.5f, 0.5f));
		Vector3 mins = cubeCenter - cubeSizes;
		Vector3 maxs = cubeCenter + cubeSizes;
		x_max = maxs.x;
		x_min = mins.x;

		y_max = maxs.y;
		y_min = mins.y;

		z_max = maxs.z;
		z_min = mins.z;
		//Debug.Log("X Max: " + x_max.ToString() + ", Y Min: " + y_min.ToString()+ ", Y Max: " + y_max.ToString());
		//Debug.Log((point.y >= y_min && point.y <= x_max));
		return (point.x <= x_max && point.x >= x_min) && (point.y >= y_min && point.y <= y_max) && (point.z >= z_min && point.z <= z_max);
	}
}
