using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PrismEffects))]
[ExecuteInEditMode]
public class PrismSetDofFocusPointToCameraRay : MonoBehaviour {

	public float lerpSpeed = 2f;
	public float sphereRadius = 0.2f;
	public GameObject currentHitObject;

	// Update is called once per frame
	void Update () {

		RaycastHit hitInfo;
		if(Physics.Raycast(transform.position, transform.forward, out hitInfo))
		{
			var prism = GetComponent<PrismEffects>();
			prism.dofFocusPoint = 
								Mathf.MoveTowards(prism.dofFocusPoint, 
      							Vector3.Distance(transform.position, hitInfo.point), 
				                  Time.deltaTime * lerpSpeed)
							;
			//Debug.Log("HIT SOMETHING" + hitInfo.collider.gameObject.name);
			currentHitObject = hitInfo.collider.gameObject;
			Debug.DrawRay(transform.position, transform.TransformDirection(transform.forward) * hitInfo.distance);
		}
	}
}
