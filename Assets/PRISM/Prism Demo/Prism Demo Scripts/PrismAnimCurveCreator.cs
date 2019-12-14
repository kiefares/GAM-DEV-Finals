using UnityEngine;
using System.Collections;

namespace Prism.Utils {

public class PrismAnimCurveCreator : MonoBehaviour {
	
	public float[] curvePointsX;
	public float[] curvePointsY;
	public AnimationCurve thisCurve;

	[ContextMenu("Generate curve")]
	void GenerateCurve()
	{
		thisCurve = new AnimationCurve();

		for(int i = 0; i < curvePointsX.Length; i++)
		{
			thisCurve.AddKey(curvePointsX[i], curvePointsY[i]);
		}
	}

}
}