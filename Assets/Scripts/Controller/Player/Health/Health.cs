using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    public int health;
    public int healthbar;

    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;
    public GameObject gameOver;

    private void Start()
    {
        gameOver.gameObject.SetActive(false);
    }

    private void Update()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if(health > healthbar)
            {
                health = healthbar;
            }
            if (i < health)
            {
                hearts[i].sprite = fullHeart;
            }
            else if (health != 0)
            {
                hearts[i].sprite = emptyHeart;
            }
            else if (health == 0)
            {
                hearts[i].sprite = emptyHeart;
                gameOver.gameObject.SetActive(true);
                Time.timeScale = 0;
            }
            if (i < healthbar)
            {
                hearts[i].enabled = true;
            }
            else
            {
                hearts[i].enabled = false;
            }
        }
    }

    public void reduceHealth()
    {
        Debug.Log("Ouch");
        health--;
    }




}
