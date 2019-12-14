using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChokeMe : MonoBehaviour
{

    public Health hp;
    // Start is called before the first frame update
    private void Start()
    {
    }
    private void OnTriggerEnter(Collider other)
    {
        
        hp.reduceHealth();
    }
}
