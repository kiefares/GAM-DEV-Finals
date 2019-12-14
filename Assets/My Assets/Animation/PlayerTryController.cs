using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTryController : MonoBehaviour
{
    public Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Profile.Input.moveRight.IsHeld() || Profile.Input.moveLeft.IsHeld() || Profile.Input.moveUp.IsHeld() || Profile.Input.moveDown.IsHeld()) {
            if (Profile.Input.run.IsHeld()) {
                anim.SetBool("isRunning", true);
                anim.SetBool("isWalking", false);
            } else {
                anim.SetBool("isRunning", false);
                anim.SetBool("isWalking", true);
            }
        }
        else {
            anim.SetBool("isRunning", false);
            anim.SetBool("isWalking", false);
        }
    }
}
