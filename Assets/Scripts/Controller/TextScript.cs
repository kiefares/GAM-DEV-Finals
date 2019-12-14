using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextScript : MonoBehaviour
{

    [SerializeField] private Text txt;
    [SerializeField] private Text countdown;
    [SerializeField] private GameObject panel;
    [SerializeField] private float time;
    [SerializeField] private GameObject gameOver;
    private float timer;
    private bool canCount = true;
    private bool doOnce = false;
    // Start is called before the first frame update
    void Start()
    {
        gameOver.SetActive(false);
        panel.SetActive(false);
        Destroy(countdown, 1);
        Destroy(txt, 2);
        timer = time;
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if(timer >= 0.0f && canCount)
        {
            timer -= Time.deltaTime;
            countdown.text = timer.ToString("F");
        } else if (timer <= 0.0f && !doOnce)
        {
            canCount = false;
            doOnce = true;
            countdown.text = "0.00";
            timer = 0.0f;
            ///gameOver.gameObject.SetActive(true);
            //Time.timeScale = 0;
        }*/
    }
}
