using UnityEngine;

public class killAfterAnim : MonoBehaviour
{
    Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
    }
    bool AnimatorIsPlaying()
    {
        return anim.GetCurrentAnimatorStateInfo(0).length >
               anim.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }

    void Update()
    {
        if (!AnimatorIsPlaying())
        {
            Destroy(gameObject);
        }
    }
}
