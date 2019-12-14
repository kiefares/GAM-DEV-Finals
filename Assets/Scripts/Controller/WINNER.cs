using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WINNER : MonoBehaviour
{

    [SerializeField] private GameObject winner;
    // Start is called before the first frame update
    void Start()
    {
        winner.gameObject.SetActive(false);
    }

    // Update is called once per frame
    private void OnTriggerEnter(Collider other) { 
    winner.gameObject.SetActive(true);
        Time.timeScale = 0;
    }
}
